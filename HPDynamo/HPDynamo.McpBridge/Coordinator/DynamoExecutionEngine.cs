namespace HPDynamo.McpBridge.Coordinator;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HPDynamo.McpBridge.Execution;
using HPDynamo.McpBridge.Support;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.Core.Scripting;
using System.Text.Json;

/// <summary>
///     Dispatches and executes DesignScript, .dyn graph files, and C# Roslyn scripts.
/// </summary>
internal sealed class DynamoExecutionEngine
{
    private readonly DynamoBridgeCoordinator _coordinator;
    private readonly HeadlessGraphRunner _headlessRunner;
    private readonly CanvasGraphRunner _canvasRunner;

    public DynamoExecutionEngine(
        DynamoBridgeCoordinator coordinator,
        HeadlessGraphRunner headlessRunner,
        CanvasGraphRunner canvasRunner)
    {
        _coordinator = coordinator;
        _headlessRunner = headlessRunner;
        _canvasRunner = canvasRunner;
    }

    public async Task<ExecuteResult> DispatchAsync(
        ExecuteRequest request,
        BridgeSettings settings,
        IProgress<ScriptProgress>? progress,
        CancellationTokenSource cancel)
    {
        if (request.Label?.StartsWith("DesignScript", StringComparison.OrdinalIgnoreCase) == true)
        {
            return await RunDesignScriptAsync(request, settings, progress, cancel).ConfigureAwait(false);
        }

        if (request.Label?.StartsWith("DynFile", StringComparison.OrdinalIgnoreCase) == true)
        {
            return await RunDynFileAsync(request, settings, progress, cancel).ConfigureAwait(false);
        }

        return await RunRoslynCodeAsync(request, settings, progress, cancel).ConfigureAwait(false);
    }

    private async Task<ExecuteResult> RunDesignScriptAsync(
        ExecuteRequest request,
        BridgeSettings settings,
        IProgress<ScriptProgress>? progress,
        CancellationTokenSource cancel)
    {
        var work = new McpBridgeWorkRequest("designscript", (uiapp, _) =>
            RevitTransactionScope.Execute(uiapp, request, settings, progress, cancel, (_, linkedToken, log) =>
            {
                if (_coordinator.IsLiveCanvasAttached && _coordinator.ActiveViewModel != null)
                    return _canvasRunner.ExecuteDesignScript(_coordinator.ActiveViewModel, request.Code, request.Args, request.DryRun, linkedToken, log);
                return _headlessRunner.ExecuteDesignScript(uiapp, request.Code, request.Args, request.DryRun, linkedToken, log);
            }, startAutoTransaction: false), cancel.Token);

        var res = await _coordinator.RunOnRevitThreadAsync(work).ConfigureAwait(false);
        return res as ExecuteResult ?? ExecuteResult.Failure("Revit returned null");
    }

    private async Task<ExecuteResult> RunDynFileAsync(
        ExecuteRequest request,
        BridgeSettings settings,
        IProgress<ScriptProgress>? progress,
        CancellationTokenSource cancel)
    {
        var dynPath = request.Code;
        var work = new McpBridgeWorkRequest("dynfile", (uiapp, _) =>
            RevitTransactionScope.Execute(uiapp, request, settings, progress, cancel, (_, linkedToken, log) =>
            {
                if (_coordinator.IsLiveCanvasAttached && _coordinator.ActiveViewModel != null)
                    return _canvasRunner.RunDynFile(_coordinator.ActiveViewModel, dynPath, request.Args, request.DryRun, linkedToken, log);
                return _headlessRunner.RunDynFile(uiapp, dynPath, request.Args, request.DryRun, linkedToken, log);
            }, startAutoTransaction: false), cancel.Token);

        var res = await _coordinator.RunOnRevitThreadAsync(work).ConfigureAwait(false);
        return res as ExecuteResult ?? ExecuteResult.Failure("Revit returned null");
    }

    private async Task<ExecuteResult> RunRoslynCodeAsync(
        ExecuteRequest request,
        BridgeSettings settings,
        IProgress<ScriptProgress>? progress,
        CancellationTokenSource cancel)
    {
        var guard = ScriptGuard.Check(request.Code, GuardProfile.Dynamo);
        if (guard.Count > 0)
            return Diagnostics("guard", guard);

        var compiler = _coordinator.Compiler;
        if (compiler == null)
            return ExecuteResult.Failure("Roslyn compiler is not initialized.");

        var compiled = compiler.GetOrCompile(request.Code);
        if (!compiled.Succeeded)
            return Diagnostics("compile", compiled.Diagnostics);

        bool startAutoTransaction = ShouldStartAutoTransaction(request.Code, request.Transaction);

        var work = new McpBridgeWorkRequest("csharp", (uiapp, ct) =>
            RevitTransactionScope.Execute(uiapp, request, settings, progress, cancel, (doc, linkedToken, log) =>
            {
                object dynamoGlobal = _coordinator.IsLiveCanvasAttached && _coordinator.ActiveViewModel != null
                    ? (object)_coordinator.ActiveViewModel
                    : (object)Dynamo.Applications.DynamoRevit.RevitDynamoModel!;
                var effectiveArgs = MergeDryRun(request.Args, request.DryRun);
                var globals = new DynamoScriptGlobals(
                    dynamoGlobal, _coordinator.ActiveWorkspace, (Autodesk.Revit.DB.Document)doc, uiapp.ActiveUIDocument,
                    uiapp.Application, uiapp, linkedToken, log,
                    (cur, tot, msg) => progress?.Report(new ScriptProgress(cur, tot, msg)),
                    new ScriptArgs(effectiveArgs),
                    request.DryRun);

                return compiled.Script!.RunAsync(globals, linkedToken).GetAwaiter().GetResult().ReturnValue;
            }, startAutoTransaction: startAutoTransaction), cancel.Token);

        var res = await _coordinator.RunOnRevitThreadAsync(work).ConfigureAwait(false);
        return res as ExecuteResult ?? ExecuteResult.Failure("Revit returned null");
    }

    internal static bool ShouldStartAutoTransaction(string code, string? transactionMode)
    {
        var mode = TransactionModes.Normalize(transactionMode) ?? TransactionModes.Auto;
        if (!string.Equals(mode, TransactionModes.Auto, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !DynamoTransactionDetector.ManagesOwnTransaction(code);
    }

    internal static JsonElement MergeDryRun(JsonElement? args, bool dryRun)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (args.HasValue && args.Value.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in args.Value.EnumerateObject())
            {
                dict[prop.Name] = prop.Value;
            }
        }
        dict["dryRun"] = dryRun;
        return JsonSerializer.SerializeToElement(dict);
    }

    private static ExecuteResult Diagnostics(string stage, IReadOnlyList<ScriptDiagnostic> diagnostics) => new()
    {
        IsError = true,
        Message = stage == "guard"
            ? "The script uses APIs the bridge blocks. Fix the listed lines and retry."
            : "The script does not compile. Fix the listed errors and retry.",
        Diagnostics = diagnostics
    };
}
