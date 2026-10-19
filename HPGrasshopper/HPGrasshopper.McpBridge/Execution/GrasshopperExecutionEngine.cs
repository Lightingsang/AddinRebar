namespace HPGrasshopper.McpBridge.Execution;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using HPGrasshopper.McpBridge.Coordinator;
using HPGrasshopper.McpBridge.Tools;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Scripting;
using HPGrasshopper.McpBridge.Support;
using Rhino;

/// <summary>
///     Dispatches execution to PlayerRunner, CanvasEditor, ImageExporter, or Roslyn script compiler.
/// </summary>
internal sealed class GrasshopperExecutionEngine
{
    private readonly GrasshopperBridgeCoordinator _coordinator;
    private readonly CanvasReader _canvasReader = new();
    private readonly CanvasEditor _canvasEditor = new();
    private readonly ImageExporter _imageExporter = new();
    private readonly PlayerRunner _playerRunner = new();

    public GrasshopperExecutionEngine(GrasshopperBridgeCoordinator coordinator) => _coordinator = coordinator;

    public async Task<ExecuteResult> DispatchAsync(
        ExecuteRequest request, BridgeSettings settings, IProgress<ScriptProgress>? progress, CancellationTokenSource cancel)
    {
        return (request.Label?.Trim() ?? string.Empty).ToLowerInvariant() switch
        {
            "read_grasshopper_canvas" => await RunCanvasReaderAsync(request, settings, cancel).ConfigureAwait(false),
            "run_grasshopper_player" => await RunPlayerAsync(request, settings, cancel).ConfigureAwait(false),
            "batch_edit_grasshopper_canvas" => await RunBatchEditAsync(request, settings, cancel).ConfigureAwait(false),
            "export_grasshopper_canvas_image" => await RunImageExportAsync(request, settings, cancel).ConfigureAwait(false),
            _ => await RunRoslynCodeAsync(request, settings, progress, cancel).ConfigureAwait(false)
        };
    }

    private (string? Title, string? Path, Guid? DefId, bool HasExplicitTarget) ExtractTargetIdentity(ScriptArgs args)
    {
        var rawTitle = args.Str("targetDocTitle", null);
        var rawPath = args.Str("targetDocPath", null);
        Guid? parsedDefId = Guid.TryParse(args.Str("targetDefId", null), out var defG) ? defG : null;
        var normPath = string.IsNullOrWhiteSpace(rawPath) ? null : rawPath;
        bool hasExplicit = rawTitle != null || normPath != null || parsedDefId.HasValue;
        var targetDocTitle = rawTitle ?? _coordinator.ActiveDocumentTitle;
        var targetDocPath = normPath ?? _coordinator.ActiveDocumentPath;
        var targetDefId = parsedDefId ?? (hasExplicit ? null : _coordinator.ActiveDefinitionId);
        return (targetDocTitle, targetDocPath, targetDefId, hasExplicit);
    }

    private async Task<ExecuteResult> RunCanvasReaderAsync(ExecuteRequest request, BridgeSettings settings, CancellationTokenSource cancel)
    {
        var args = new ScriptArgs(request.Args);
        var (tTitle, tPath, tDef, _) = ExtractTargetIdentity(args);
        var work = new McpBridgeWorkRequest("read_canvas", (uiapp, ct) =>
        {
            var doc = Grasshopper.Instances.ActiveDocument;
            var read = _canvasReader.Read(doc, args.Bool("includeValues", false), args.Bool("selectedOnly", false), args.Str("categoryFilter", string.Empty));
            return MapToExecuteResult(read);
        }, cancel.Token, tTitle, tPath, tDef);

        var res = await _coordinator.RunOnRevitThreadAsync(work).ConfigureAwait(false);
        return res as ExecuteResult ?? ExecuteResult.Failure("Revit returned null");
    }

    private async Task<ExecuteResult> RunPlayerAsync(ExecuteRequest request, BridgeSettings settings, CancellationTokenSource cancel)
    {
        var args = new ScriptArgs(request.Args);
        var (tTitle, tPath, tDef, hasExplicit) = ExtractTargetIdentity(args);
        var work = new McpBridgeWorkRequest("player_runner", (uiapp, ct) =>
        {
            var path = args.Str("filePath", request.Code) ?? request.Code;
            var inputs = request.Args.HasValue && request.Args.Value.TryGetProperty("inputs", out var inp) ? (JsonElement?)inp : null;
            var outputs = args.Strings("outputs");
            var headless = args.Bool("headless", true);

            if (uiapp is not null && HasActiveRevitDoc(uiapp))
            {
                object? lastRun = null;
                var execRes = RevitTransactionScope.Execute(uiapp, request, settings, null, cancel, (doc, linkedToken, log) =>
                {
                    lastRun = _playerRunner.Run(path, inputs, outputs, headless, linkedToken);
                    var elem = BridgeJson.ToElement(lastRun) ?? default;
                    if (elem.TryGetProperty("success", out var s) && !s.GetBoolean())
                    {
                        var err = elem.TryGetProperty("error", out var e) ? e.GetString() : "Player execution failed";
                        throw new InvalidOperationException(err);
                    }
                    return lastRun;
                }, startAutoTransaction: false);

                if (execRes.IsError)
                    execRes.Value = MergeOutcomeValue(lastRun, execRes.RolledBack, execRes.Message);

                return execRes;
            }

            var standaloneResult = _playerRunner.Run(path, inputs, outputs, headless, cancel.Token);
            return MapToExecuteResult(standaloneResult);
        }, cancel.Token, tTitle, tPath, tDef, hasExplicit);

        var res = await _coordinator.RunOnRevitThreadAsync(work).ConfigureAwait(false);
        return res as ExecuteResult ?? ExecuteResult.Failure("Revit returned null");
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static bool HasActiveRevitDoc(Autodesk.Revit.UI.UIApplication app) { try { return app.ActiveUIDocument?.Document != null; } catch { return false; } }

    private async Task<ExecuteResult> RunBatchEditAsync(ExecuteRequest request, BridgeSettings settings, CancellationTokenSource cancel)
    {
        var args = new ScriptArgs(request.Args);
        var (tTitle, tPath, tDef, hasExplicit) = ExtractTargetIdentity(args);
        var work = new McpBridgeWorkRequest("batch_edit", (uiapp, ct) =>
        {
            var doc = Grasshopper.Instances.ActiveDocument;
            var batchArgs = request.Args ?? BridgeJson.ToElement(new object()) ?? default;

            if (uiapp is not null && HasActiveRevitDoc(uiapp))
            {
                var scope = doc != null ? new CanvasUndoScope(doc, "MCP Batch Edit Canvas") : null;
                object? lastBatch = null;
                var execRes = RevitTransactionScope.Execute(uiapp, request, settings, null, cancel, (revitDoc, linkedToken, log) =>
                {
                    linkedToken.ThrowIfCancellationRequested();
                    lastBatch = _canvasEditor.ExecuteBatch(doc, batchArgs, request.DryRun, linkedToken, scope);
                    var elem = BridgeJson.ToElement(lastBatch) ?? default;
                    if (elem.TryGetProperty("success", out var s) && !s.GetBoolean())
                    {
                        var err = elem.TryGetProperty("error", out var e) ? e.GetString() : "Batch edit execution failed";
                        throw new InvalidOperationException(err);
                    }
                    return lastBatch;
                }, startAutoTransaction: false, canvasScope: scope);

                if (execRes.IsError)
                    execRes.Value = MergeOutcomeValue(lastBatch, execRes.RolledBack, execRes.Message);

                return execRes;
            }

            var standaloneResult = _canvasEditor.ExecuteBatch(doc, batchArgs, request.DryRun, ct);
            return MapToExecuteResult(standaloneResult);
        }, cancel.Token, tTitle, tPath, tDef, hasExplicit);

        var res = await _coordinator.RunOnRevitThreadAsync(work).ConfigureAwait(false);
        return res as ExecuteResult ?? ExecuteResult.Failure("Revit returned null");
    }

    private async Task<ExecuteResult> RunImageExportAsync(ExecuteRequest request, BridgeSettings settings, CancellationTokenSource cancel)
    {
        var args = new ScriptArgs(request.Args);
        var (tTitle, tPath, tDef, _) = ExtractTargetIdentity(args);
        var work = new McpBridgeWorkRequest("export_image", (uiapp, ct) =>
        {
            var canvas = Grasshopper.Instances.ActiveCanvas;
            var mode = args.Str("mode", "FullCanvas") ?? "FullCanvas";
            var zoom = args.Double("zoom", 1.0);
            var outPath = args.Str("outputPath", string.Empty);
            var retBase64 = args.Bool("returnBase64", true);

            var exportResult = _imageExporter.Export(canvas, mode, zoom, string.IsNullOrEmpty(outPath) ? null : outPath, retBase64);
            return MapToExecuteResult(exportResult);
        }, cancel.Token, tTitle, tPath, tDef);

        var res = await _coordinator.RunOnRevitThreadAsync(work).ConfigureAwait(false);
        return res as ExecuteResult ?? ExecuteResult.Failure("Revit returned null");
    }

    internal static JsonElement MergeOutcomeValue(object? payload, bool rolledBack, string? fallbackError)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (payload != null)
        {
            var elem = payload is JsonElement je ? je : (BridgeJson.ToElement(payload) ?? default);
            if (elem.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in elem.EnumerateObject()) dict[prop.Name] = prop.Value;
            }
        }
        if (!dict.ContainsKey("success")) dict["success"] = false;
        if (!dict.ContainsKey("error") && !string.IsNullOrEmpty(fallbackError)) dict["error"] = fallbackError;
        dict["rolledBack"] = rolledBack;
        return BridgeJson.ToElement(dict) ?? default;
    }

    internal static ExecuteResult MapToExecuteResult(object? data)
    {
        if (data is null) return ExecuteResult.Failure("Execution returned null result");
        var element = data is JsonElement je ? je : (BridgeJson.ToElement(data) ?? default);
        var isRolledBack = element.ValueKind == JsonValueKind.Object &&
                           element.TryGetProperty("rolledBack", out var rbProp) &&
                           rbProp.ValueKind == JsonValueKind.True;

        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("success", out var successProp) &&
            successProp.ValueKind == JsonValueKind.False)
        {
            var errorMessage = element.TryGetProperty("error", out var errProp) ? errProp.GetString() : null;
            if (string.IsNullOrEmpty(errorMessage) && element.TryGetProperty("rollbackErrors", out var rbErr) && rbErr.ValueKind == JsonValueKind.Array)
            {
                var errs = rbErr.EnumerateArray().Select(x => x.GetString()).Where(s => !string.IsNullOrEmpty(s));
                var joined = string.Join("; ", errs);
                if (!string.IsNullOrEmpty(joined)) errorMessage = $"Canvas rollback failed: {joined}";
            }
            return new ExecuteResult { IsError = true, Message = errorMessage ?? "Operation failed", Value = element, RolledBack = isRolledBack };
        }
        return new ExecuteResult { Value = element, RolledBack = isRolledBack };
    }

    private async Task<ExecuteResult> RunRoslynCodeAsync(
        ExecuteRequest request, BridgeSettings settings, IProgress<ScriptProgress>? progress, CancellationTokenSource cancel)
    {
        var guard = ScriptGuard.Check(request.Code, GuardProfile.Grasshopper);
        if (guard.Count > 0) return Diagnostics("guard", guard);

        var mode = TransactionModes.Normalize(request.Transaction) ?? TransactionModes.Auto;
        if (string.Equals(mode, TransactionModes.None, StringComparison.OrdinalIgnoreCase))
        {
            var analysis = ScriptAnalyzer.Analyze(request.Code, AnalyzerProfile.Grasshopper);
            if (analysis.UsesTransaction)
                return ExecuteResult.Failure("Transaction mode 'none' enforces read-only execution. The script attempts to open a Revit Transaction/TransactionGroup/SubTransaction, which is forbidden under mode 'none'.");
        }

        var compiler = _coordinator.Compiler;
        if (compiler == null) return ExecuteResult.Failure("Roslyn compiler is not initialized.");
        var compiled = compiler.GetOrCompile(request.Code);
        if (!compiled.Succeeded) return Diagnostics("compile", compiled.Diagnostics);

        bool startAutoTransaction = ShouldStartAutoTransaction(request.Transaction);
        var args = new ScriptArgs(request.Args);
        var (tTitle, tPath, tDef, hasExplicit) = ExtractTargetIdentity(args);

        var work = CreateScriptWorkRequest(
            request, settings, progress, cancel, compiled,
            startAutoTransaction, tTitle, tPath, tDef, hasExplicit);

        var res = await _coordinator.RunOnRevitThreadAsync(work).ConfigureAwait(false);
        return res as ExecuteResult ?? ExecuteResult.Failure("Revit returned null");
    }

    private static McpBridgeWorkRequest CreateScriptWorkRequest(
        ExecuteRequest request, BridgeSettings settings, IProgress<ScriptProgress>? progress,
        CancellationTokenSource cancel, CompileOutcome compiled, bool startAutoTransaction,
        string? targetTitle, string? targetPath, Guid? targetDef, bool requireTargetIdentity = false)
    {
        return new McpBridgeWorkRequest("csharp", (uiapp, ct) =>
        {
            var ghDoc = Grasshopper.Instances.ActiveDocument;
            using var canvasScope = ghDoc != null ? new CanvasUndoScope(ghDoc, "MCP Roslyn Script Execution") : null;

            return RevitTransactionScope.Execute(uiapp, request, settings, progress, cancel, (doc, linkedToken, log) =>
            {
                var effectiveArgs = MergeDryRun(request.Args, request.DryRun);
                var globals = new GrasshopperScriptGlobals(
                    ghDoc, Grasshopper.Instances.ActiveCanvas, RhinoDoc.ActiveDoc, (Document)doc,
                    uiapp.ActiveUIDocument, uiapp, linkedToken, log,
                    (cur, tot, msg) => progress?.Report(new ScriptProgress(cur, tot, msg)),
                    new ScriptArgs(effectiveArgs), request.DryRun);

                var task = compiled.Script!.RunAsync(globals, linkedToken);
                return task.GetAwaiter().GetResult().ReturnValue;
            }, startAutoTransaction: startAutoTransaction, canvasScope: canvasScope);
        }, cancel.Token, targetTitle, targetPath, targetDef, requireTargetIdentity);
    }

    private static bool ShouldStartAutoTransaction(string? transactionMode) =>
        string.Equals(TransactionModes.Normalize(transactionMode) ?? TransactionModes.Auto, TransactionModes.Auto, StringComparison.OrdinalIgnoreCase);

    private static JsonElement MergeDryRun(JsonElement? args, bool dryRun)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (args.HasValue && args.Value.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in args.Value.EnumerateObject()) dict[prop.Name] = prop.Value;
        }
        dict["dryRun"] = dryRun;
        return BridgeJson.ToElement(dict) ?? default;
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
