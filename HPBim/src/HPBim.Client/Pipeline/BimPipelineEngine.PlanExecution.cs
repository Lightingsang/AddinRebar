namespace HPBim.Client.Pipeline;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;

public sealed partial class BimPipelineEngine
{
    public BimCoordinationLedgerService? LedgerService { get; set; }
    public string? LedgerFilePath { get; set; }

    public Task<BimPipelineResult> ExecutePlanAsync(
        BimExecutionPlan plan, CancellationToken cancellationToken = default) =>
        ExecutePlanAsync(plan, LedgerService, cancellationToken);

    public async Task<BimPipelineResult> ExecutePlanAsync(
        BimExecutionPlan plan,
        BimCoordinationLedgerService? ledgerService,
        CancellationToken cancellationToken = default)
    {
        if (plan == null) throw new ArgumentNullException(nameof(plan));

        var sw = Stopwatch.StartNew();
        if (!_pipeClient.IsConnected) await _pipeClient.ConnectAsync(cancellationToken).ConfigureAwait(false);
        if (ledgerService != null) plan = PopulateTargetUniqueIds(plan, ledgerService);

        var request = CreatePlanExecuteRequest(plan);
        var execRes = await _pipeClient.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
        var elementRecords = ParseElementRecords(execRes, plan);

        ResolvePlanExecutionOutcome(execRes, plan, elementRecords, out bool success, out bool isOutcomeUnknown, out string? errorMsg);

        if (success && !plan.Options.DryRun && ledgerService != null)
        {
            ledgerService.SyncFromExecutionRecords(elementRecords, plan.PlanHash);
            (success, isOutcomeUnknown, errorMsg) = await PersistLedgerToDiskAsync(
                ledgerService, success, isOutcomeUnknown, errorMsg, cancellationToken).ConfigureAwait(false);
        }

        var stageResults = BuildPlanStageResults(plan, elementRecords, execRes, success, errorMsg);
        foreach (var sr in stageResults) StageCompleted?.Invoke(sr);

        return CreatePipelineResult(plan, execRes, elementRecords, stageResults, success, isOutcomeUnknown, errorMsg, sw.ElapsedMilliseconds);
    }

    private static BimPipelineResult CreatePipelineResult(
        BimExecutionPlan plan,
        ExecuteResult execRes,
        IReadOnlyList<ElementExecutionRecord> elementRecords,
        IReadOnlyList<StageResult> stageResults,
        bool success,
        bool isOutcomeUnknown,
        string? errorMsg,
        long elapsedMs) =>
        new()
        {
            Success = success,
            RolledBack = execRes.RolledBack,
            IsOutcomeUnknown = isOutcomeUnknown,
            TotalDurationMs = elapsedMs,
            StageResults = stageResults,
            ElementRecords = elementRecords,
            ExecutionPlan = plan,
            ErrorMessage = errorMsg,
            Warnings = execRes.Warnings ?? Array.Empty<string>()
        };

    private static BimExecutionPlan PopulateTargetUniqueIds(BimExecutionPlan plan, BimCoordinationLedgerService ledgerService)
    {
        var itemsWithTargets = plan.Items.Select(item =>
        {
            if (!string.IsNullOrEmpty(item.TargetUniqueId)) return item;
            if (ledgerService.TryGetTarget(item.SourceKey, out var uid) && !string.IsNullOrEmpty(uid))
            {
                return item with { TargetUniqueId = uid };
            }
            return item;
        }).ToList();
        return plan with { Items = itemsWithTargets };
    }

    private static ExecuteRequest CreatePlanExecuteRequest(BimExecutionPlan plan)
    {
        return new ExecuteRequest(
            Code: BimPlanRunnerScriptTemplate.GetScriptCode(),
            Transaction: TransactionModes.Auto,
            DryRun: plan.Options.DryRun,
            TimeoutSeconds: plan.Options.TimeoutSeconds,
            Label: plan.Options.TransactionLabel,
            Args: BridgeJson.ToElement(plan));
    }

    private static void ResolvePlanExecutionOutcome(
        ExecuteResult execRes,
        BimExecutionPlan plan,
        IReadOnlyList<ElementExecutionRecord> elementRecords,
        out bool success,
        out bool isOutcomeUnknown,
        out string? errorMsg)
    {
        success = !execRes.IsError;
        isOutcomeUnknown = false;

        if (execRes.Truncated)
        {
            success = false;
            isOutcomeUnknown = true;
            errorMsg = "Execution outcome unknown: Revit response was truncated (>64KB / output limit). Auto-reconciliation required.";
            return;
        }

        if (plan.Options.DryRun && !execRes.RolledBack) success = false;
        if (!plan.Options.DryRun && execRes.RolledBack) success = false;
        if (elementRecords.Any(r => r.Status == ElementExecutionStatus.Failed)) success = false;
        if (elementRecords.Any(r => r.Status == ElementExecutionStatus.OutcomeUnknown))
        {
            success = false;
            isOutcomeUnknown = true;
        }

        errorMsg = FormatErrorMessage(execRes);
        if (!success && string.IsNullOrEmpty(errorMsg))
        {
            errorMsg = ResolveFallbackErrorMessage(execRes, plan, elementRecords, isOutcomeUnknown);
        }
    }

    private static string? ResolveFallbackErrorMessage(
        ExecuteResult execRes,
        BimExecutionPlan plan,
        IReadOnlyList<ElementExecutionRecord> elementRecords,
        bool isOutcomeUnknown)
    {
        if (plan.Options.DryRun && !execRes.RolledBack)
        {
            return "Dry-run failed: Revit bridge did not confirm transaction rollback (RolledBack == false).";
        }
        if (!plan.Options.DryRun && execRes.RolledBack)
        {
            return "Apply execution failed: Transaction was unexpectedly rolled back by Revit bridge.";
        }

        var firstFailed = elementRecords.FirstOrDefault(r => r.Status == ElementExecutionStatus.Failed);
        if (firstFailed != null)
        {
            int failCount = elementRecords.Count(r => r.Status == ElementExecutionStatus.Failed);
            return $"Execution failed for {failCount} element(s). First failure: [{firstFailed.ErrorCode}] {firstFailed.ErrorReason}";
        }

        if (isOutcomeUnknown)
        {
            int unknownCount = elementRecords.Count(r => r.Status == ElementExecutionStatus.OutcomeUnknown);
            return $"Execution outcome ambiguous: {unknownCount} element(s) marked OutcomeUnknown due to missing records, deficit, or truncation.";
        }

        return null;
    }

    private static IReadOnlyList<StageResult> BuildPlanStageResults(
        BimExecutionPlan plan,
        IReadOnlyList<ElementExecutionRecord> elementRecords,
        ExecuteResult execRes,
        bool overallSuccess,
        string? errorMsg)
    {
        var results = new List<StageResult>(8);
        for (int i = 1; i <= 8; i++)
        {
            int stageIdx = i;
            var recordsInStage = elementRecords.Where(r => MapCategoryToStage(r.Category) == stageIdx).ToList();
            var itemsInStage = plan.Items.Where(it => MapCategoryToStage(it.Category) == stageIdx).ToList();
            results.Add(CreateStageResult(stageIdx, execRes, recordsInStage, itemsInStage, overallSuccess, errorMsg));
        }
        return results;
    }

    private static StageResult CreateStageResult(
        int stageIdx,
        ExecuteResult execRes,
        IReadOnlyList<ElementExecutionRecord> recordsInStage,
        IReadOnlyList<ElementPlanItem> itemsInStage,
        bool overallSuccess,
        string? errorMsg)
    {
        bool stageSuccess = overallSuccess && !recordsInStage.Any(r => r.Status is ElementExecutionStatus.Failed or ElementExecutionStatus.OutcomeUnknown);
        int added = recordsInStage.Count(r => r.Status == ElementExecutionStatus.Created);
        int modified = recordsInStage.Count(r => r.Status == ElementExecutionStatus.Updated);

        return new StageResult
        {
            StageIndex = stageIdx,
            StageName = GetStageName(stageIdx),
            Success = stageSuccess,
            ErrorMessage = stageSuccess ? null : errorMsg,
            DurationMs = execRes.DurationMs / 8,
            Changed = new ChangedCounts(added, modified, 0),
            Logs = execRes.Logs,
            RolledBack = execRes.RolledBack,
            Warnings = execRes.Warnings ?? Array.Empty<string>()
        };
    }

    private static int MapCategoryToStage(CoordinationCategory cat) => cat switch
    {
        CoordinationCategory.Level => 1,
        CoordinationCategory.Grid => 2,
        CoordinationCategory.Column or CoordinationCategory.Beam or CoordinationCategory.GradeBeam or
        CoordinationCategory.Slab or CoordinationCategory.Footing or CoordinationCategory.Pile => 3,
        CoordinationCategory.Wall => 4,
        CoordinationCategory.Opening => 5,
        CoordinationCategory.Room => 6,
        CoordinationCategory.MepFixture or CoordinationCategory.PipeRun => 7,
        CoordinationCategory.Rebar => 8,
        _ => 3
    };

    private static string GetStageName(int stageIndex) => stageIndex switch
    {
        1 => "Levels", 2 => "Grids", 3 => "Structure", 4 => "Walls",
        5 => "Openings", 6 => "Rooms", 7 => "Mep", 8 => "Rebars", _ => "General"
    };

    private async Task<(bool Success, bool IsOutcomeUnknown, string? ErrorMsg)> PersistLedgerToDiskAsync(
        BimCoordinationLedgerService ledgerService,
        bool currentSuccess, bool currentOutcomeUnknown, string? currentErrorMsg,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(LedgerFilePath)) return (currentSuccess, currentOutcomeUnknown, currentErrorMsg);

        try
        {
            await ledgerService.SaveToFileAsync(LedgerFilePath!, ct).ConfigureAwait(false);
            return (currentSuccess, currentOutcomeUnknown, currentErrorMsg);
        }
        catch (Exception ex)
        {
            // Failure to persist ledger to disk prevents marking operation Committed
            return (false, true, $"Failed to persist coordination ledger to disk: {ex.Message}");
        }
    }
}
