namespace HPBim.Client.Pipeline;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPRebar.Mcp.Contracts.JsonRpc;

public sealed partial class BimPipelineEngine
{
    public IIntentJournal? IntentJournal { get; set; }
    public TimeoutPolicy? TimeoutPolicy { get; set; }

    public Task<BimPipelineResult> ExecutePlanJournaledAsync(
        BimExecutionPlan plan,
        CancellationToken cancellationToken = default) =>
        ExecutePlanJournaledAsync(plan, LedgerService, cancellationToken);

    public Task<BimPipelineResult> ExecutePlanWithJournalAsync(
        BimExecutionPlan plan,
        CancellationToken cancellationToken = default) =>
        ExecutePlanJournaledAsync(plan, LedgerService, cancellationToken);

    public Task<BimPipelineResult> ExecutePlanWithJournalAsync(
        BimExecutionPlan plan,
        BimCoordinationLedgerService? ledgerService,
        CancellationToken cancellationToken = default) =>
        ExecutePlanJournaledAsync(plan, ledgerService, cancellationToken);

    public async Task<BimPipelineResult> ExecutePlanJournaledAsync(
        BimExecutionPlan plan,
        BimCoordinationLedgerService? ledgerService,
        CancellationToken cancellationToken = default)
    {
        if (plan == null) throw new ArgumentNullException(nameof(plan));

        var sw = Stopwatch.StartNew();
        plan = ApplyTimeoutBudget(plan);

        await RecordPreDispatchIntentAsync(plan, cancellationToken).ConfigureAwait(false);
        return await DispatchPlanSafelyAsync(plan, ledgerService, sw, cancellationToken).ConfigureAwait(false);
    }

    private BimExecutionPlan ApplyTimeoutBudget(BimExecutionPlan plan)
    {
        if (TimeoutPolicy != null && plan.Options.TimeoutSeconds <= 0)
        {
            int calculatedTimeout = (int)TimeoutPolicy.ComputeTotalTimeout(plan.Items.Count).TotalSeconds;
            return plan with { Options = plan.Options with { TimeoutSeconds = calculatedTimeout } };
        }
        return plan;
    }

    private async Task<BimPipelineResult> DispatchPlanSafelyAsync(
        BimExecutionPlan plan,
        BimCoordinationLedgerService? ledgerService,
        Stopwatch sw,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await ExecutePlanAsync(plan, ledgerService, cancellationToken).ConfigureAwait(false);

            bool outcomeUnknown = result.IsOutcomeUnknown || (!result.Success && !result.RolledBack && !plan.Options.DryRun);

            await RecordPostDispatchOutcomeAsync(
                plan,
                result.Success,
                result.RolledBack,
                outcomeUnknown,
                result.ErrorMessage,
                cancellationToken).ConfigureAwait(false);

            return result with
            {
                OperationId = plan.Options.OperationId,
                IsOutcomeUnknown = outcomeUnknown
            };
        }
        catch (Exception ex) when (ex is RevitPipeException or IOException or TimeoutException or OperationCanceledException)
        {
            await RecordExceptionOutcomeAsync(plan, ex).ConfigureAwait(false);
            return CreateOutcomeUnknownResult(plan, ex, sw.ElapsedMilliseconds);
        }
    }

    private static BimPipelineResult CreateOutcomeUnknownResult(BimExecutionPlan plan, Exception ex, long elapsedMs) =>
        new()
        {
            Success = false,
            RolledBack = false,
            IsOutcomeUnknown = true,
            OperationId = plan.Options.OperationId,
            TotalDurationMs = elapsedMs,
            ExecutionPlan = plan,
            ErrorMessage = $"Execution outcome unknown: Pipe timed out or severed while executing in Revit. Auto-reconciliation required ({ex.GetType().Name}: {ex.Message})"
        };

    private async Task RecordPreDispatchIntentAsync(BimExecutionPlan plan, CancellationToken ct)
    {
        if (IntentJournal == null || plan.Options.DryRun) return;

        var record = new IntentJournalRecord
        {
            OperationId = plan.Options.OperationId,
            PlanHash = plan.PlanHash,
            TimestampUtc = DateTime.UtcNow,
            Status = IntentStatus.InFlight,
            ItemCount = plan.Items.Count,
            SourceKeys = plan.Items.Select(item => item.SourceKey?.ToString() ?? string.Empty).ToList(),
            TargetDoc = plan.TargetDoc,
            ErrorReason = null
        };

        await IntentJournal.RecordIntentAsync(record, ct).ConfigureAwait(false);
    }

    private async Task RecordPostDispatchOutcomeAsync(
        BimExecutionPlan plan,
        bool success,
        bool rolledBack,
        bool outcomeUnknown,
        string? errorMsg,
        CancellationToken ct)
    {
        if (IntentJournal == null || plan.Options.DryRun) return;

        Guid opId = plan.Options.OperationId;
        if (success)
        {
            await IntentJournal.UpdateStatusAsync(opId, IntentStatus.Committed, null, ct).ConfigureAwait(false);
        }
        else if (outcomeUnknown)
        {
            await IntentJournal.UpdateStatusAsync(opId, IntentStatus.OutcomeUnknown, errorMsg, ct).ConfigureAwait(false);
        }
        else if (rolledBack)
        {
            await IntentJournal.UpdateStatusAsync(opId, IntentStatus.Failed, errorMsg, ct).ConfigureAwait(false);
        }
        else
        {
            await IntentJournal.UpdateStatusAsync(opId, IntentStatus.OutcomeUnknown, errorMsg, ct).ConfigureAwait(false);
        }
    }

    private async Task RecordExceptionOutcomeAsync(BimExecutionPlan plan, Exception ex)
    {
        if (IntentJournal == null || plan.Options.DryRun) return;

        // Use CancellationToken.None to guarantee journal write even if caller was cancelled
        await IntentJournal.UpdateStatusAsync(
            plan.Options.OperationId,
            IntentStatus.OutcomeUnknown,
            $"Pipe communication interruption: {ex.GetType().Name} - {ex.Message}",
            CancellationToken.None).ConfigureAwait(false);
    }
}
