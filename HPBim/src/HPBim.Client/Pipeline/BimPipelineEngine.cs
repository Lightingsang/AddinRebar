namespace HPBim.Client.Pipeline;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Client.ScriptGenerators;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Models;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;

/// <summary>
///     Engine orchestrating 8-stage BIM model creation or atomic dryRun rollback over Revit Named Pipe.
/// </summary>
public sealed partial class BimPipelineEngine : IBimPipelineEngine
{
    private readonly IRevitPipeClient _pipeClient;
    private readonly IReadOnlyList<IStageScriptGenerator> _generators;
    private readonly CompositeBimGenerator _compositeGenerator;

    public event Action<StageResult>? StageCompleted;

    public BimPipelineEngine(
        IRevitPipeClient pipeClient,
        IEnumerable<IStageScriptGenerator>? generators = null,
        CompositeBimGenerator? compositeGenerator = null,
        IIntentJournal? intentJournal = null,
        TimeoutPolicy? timeoutPolicy = null)
    {
        _pipeClient = pipeClient ?? throw new ArgumentNullException(nameof(pipeClient));
        _compositeGenerator = compositeGenerator ?? new CompositeBimGenerator();
        IntentJournal = intentJournal;
        TimeoutPolicy = timeoutPolicy;

        _generators = generators != null
            ? generators.OrderBy(g => g.StageIndex).ToList()
            : GetDefaultGenerators();
    }

    public async Task<BimPipelineResult> ExecuteAsync(
        BimModel model,
        BimPipelineOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new BimPipelineOptions();
        var sw = Stopwatch.StartNew();

        if (!_pipeClient.IsConnected)
        {
            await _pipeClient.ConnectAsync(cancellationToken).ConfigureAwait(false);
        }

        if (options.UseExecutionPlan)
        {
            var plan = await BuildExecutionPlanAsync(model, options, cancellationToken).ConfigureAwait(false);
            return IntentJournal != null
                ? await ExecutePlanJournaledAsync(plan, cancellationToken).ConfigureAwait(false)
                : await ExecutePlanAsync(plan, cancellationToken).ConfigureAwait(false);
        }

        if (options.DryRun && options.AtomicDryRun)
        {
            return await ExecuteAtomicDryRunAsync(model, options, sw, cancellationToken).ConfigureAwait(false);
        }

        return await ExecuteSequentialStagesAsync(model, options, sw, cancellationToken).ConfigureAwait(false);
    }

    private async Task<BimPipelineResult> ExecuteAtomicDryRunAsync(
        BimModel model,
        BimPipelineOptions options,
        Stopwatch sw,
        CancellationToken cancellationToken)
    {
        var payload = _compositeGenerator.Generate(model);
        var request = payload.ToExecuteRequest(dryRun: true, timeoutSeconds: options.TimeoutSecondsPerStage * 2);
        var execRes = await _pipeClient.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        var stageRes = BuildStageResult(0, "CompositeDryRun", execRes, isDryRun: true);
        StageCompleted?.Invoke(stageRes);

        return new BimPipelineResult
        {
            Success = stageRes.Success,
            RolledBack = execRes.RolledBack,
            TotalDurationMs = sw.ElapsedMilliseconds,
            StageResults = new[] { stageRes },
            ErrorMessage = stageRes.ErrorMessage,
            Warnings = execRes.Warnings ?? Array.Empty<string>()
        };
    }

    private static string? FormatErrorMessage(ExecuteResult res)
    {
        if (!res.IsError) return null;
        string msg = res.Message ?? string.Empty;
        int payloadIdx = msg.IndexOf("||PAYLOAD:", StringComparison.Ordinal);
        if (payloadIdx >= 0)
        {
            msg = msg.Substring(0, payloadIdx).Trim();
        }

        if (res.Diagnostics == null || res.Diagnostics.Count == 0) return string.IsNullOrEmpty(msg) ? null : msg;
        string diags = string.Join("; ", res.Diagnostics.Select(d => $"[L{d.Line}:C{d.Column}] {d.Id}: {d.Message}"));
        return $"{msg} -> {diags}";
    }

    private async Task<BimPipelineResult> ExecuteSequentialStagesAsync(
        BimModel model,
        BimPipelineOptions options,
        Stopwatch sw,
        CancellationToken cancellationToken)
    {
        var activeGenerators = options.SelectedStages != null
            ? _generators.Where(g => options.SelectedStages.Contains(g.StageIndex)).ToList()
            : _generators;

        var stageResults = new List<StageResult>();
        foreach (var generator in activeGenerators)
        {
            var stageRes = await ExecuteStageStepAsync(generator, model, options, cancellationToken).ConfigureAwait(false);
            stageResults.Add(stageRes);
            StageCompleted?.Invoke(stageRes);

            if (!stageRes.Success)
            {
                return CreateSequentialStageFailedResult(generator, stageRes, stageResults, sw.ElapsedMilliseconds);
            }
        }

        return CreateSequentialStageSuccessResult(options, stageResults, sw.ElapsedMilliseconds);
    }

    private async Task<StageResult> ExecuteStageStepAsync(
        IStageScriptGenerator generator,
        BimModel model,
        BimPipelineOptions options,
        CancellationToken cancellationToken)
    {
        var payload = generator.Generate(model);
        var request = payload.ToExecuteRequest(dryRun: options.DryRun, timeoutSeconds: options.TimeoutSecondsPerStage);
        var execRes = await _pipeClient.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
        return BuildStageResult(generator.StageIndex, generator.StageName, execRes, options.DryRun);
    }

    private static StageResult BuildStageResult(
        int stageIndex,
        string stageName,
        ExecuteResult execRes,
        bool isDryRun)
    {
        bool stageSuccess = !execRes.IsError;
        string? errorMsg = FormatErrorMessage(execRes);
        if (isDryRun && stageSuccess && !execRes.RolledBack)
        {
            stageSuccess = false;
            errorMsg = "Dry-run failed: Revit bridge did not confirm transaction rollback (RolledBack == false).";
        }
        else if (!isDryRun && stageSuccess && execRes.RolledBack)
        {
            stageSuccess = false;
            errorMsg = "Apply execution failed: Transaction was unexpectedly rolled back by Revit bridge.";
        }

        return new StageResult
        {
            StageIndex = stageIndex,
            StageName = stageName,
            Success = stageSuccess,
            ErrorMessage = errorMsg,
            DurationMs = execRes.DurationMs,
            Changed = execRes.Changed,
            Logs = execRes.Logs,
            RolledBack = execRes.RolledBack,
            Warnings = execRes.Warnings ?? Array.Empty<string>()
        };
    }

    private static BimPipelineResult CreateSequentialStageFailedResult(
        IStageScriptGenerator generator,
        StageResult stageRes,
        List<StageResult> stageResults,
        long elapsedMs) =>
        new()
        {
            Success = false,
            ErrorMessage = $"Stage {generator.StageIndex} ({generator.StageName}) failed: {stageRes.ErrorMessage}",
            StageResults = stageResults,
            TotalDurationMs = elapsedMs,
            RolledBack = stageResults.Any(s => s.RolledBack),
            Warnings = stageResults.SelectMany(s => s.Warnings).Distinct().ToList()
        };

    private static BimPipelineResult CreateSequentialStageSuccessResult(
        BimPipelineOptions options,
        List<StageResult> stageResults,
        long elapsedMs) =>
        new()
        {
            Success = true,
            RolledBack = options.DryRun ? stageResults.All(s => s.RolledBack) : stageResults.Any(s => s.RolledBack),
            TotalDurationMs = elapsedMs,
            StageResults = stageResults,
            Warnings = stageResults.SelectMany(s => s.Warnings).Distinct().ToList()
        };

    private async Task<BimExecutionPlan> BuildExecutionPlanAsync(
        BimModel model,
        BimPipelineOptions options,
        CancellationToken ct)
    {
        var targetDoc = options.TargetDoc;
        if (targetDoc == null || targetDoc.IsEmpty)
        {
            var query = new DocumentIdentityQuery(_pipeClient);
            targetDoc = await query.QueryActiveDocumentAsync(options.DryRun, ct).ConfigureAwait(false);
        }

        return ExecutionPlanBuilder.Build(model, targetDoc: targetDoc, options: new ExecutionOptions
        {
            DryRun = options.DryRun,
            TimeoutSeconds = options.TimeoutSecondsPerStage * 2,
            CreatePlanViews = true
        });
    }

    private static IReadOnlyList<IStageScriptGenerator> GetDefaultGenerators() =>
    [
        new Stage1LevelGenerator(), new Stage2GridGenerator(), new Stage3StructureGenerator(),
        new Stage4WallGenerator(), new Stage5OpeningGenerator(), new Stage6RoomGenerator(),
        new Stage7MepGenerator(), new Stage8RebarGenerator()
    ];
}
