using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.Core.Scripting;
using Serilog;
using Tekla.Structures.Drawing;
using Tekla.Structures.Model;
using ModelObject = Tekla.Structures.Model.ModelObject;
using RefAssembly = System.Reflection.Assembly;
using TeklaOperation = Tekla.Structures.ModelInternal.Operation;
using UIModelObjectSelector = Tekla.Structures.Model.UI.ModelObjectSelector;

namespace HPTekla.McpBridge;

/// <summary>
///     Implements IBridgeExecutor for Trimble Tekla Structures 2025.0.
///     Coordinates Roslyn guard rules, 3-tier safety gating (Read / Write / Destructive),
///     atomic dryRun rollback via SetTestSavePoint/RollbackToTestSavePoint,
///     pre-mutation model database snapshots, and main-thread execution via TeklaThreadDispatcher.
/// </summary>
public sealed class TeklaBridgeExecutor : IBridgeExecutor, IDisposable
{
    public const string HostName = "Tekla Structures";

    private readonly Model _model;
    private readonly TeklaThreadDispatcher _dispatcher;
    private readonly TeklaSnapshotManager _snapshots;
    private readonly ScriptCompiler _compiler;
    private readonly TypeInspector _inspector;
    private readonly BridgeSettings _settings;
    private readonly string _hostVersion;

    private int _busy;
    private bool _allowHeavyOperations;
    private volatile CancellationTokenSource? _currentCancel;
    private string? _activeDocumentTitle;

    public TeklaBridgeExecutor(
        Model model,
        TeklaThreadDispatcher dispatcher,
        BridgeSettings settings,
        TeklaSnapshotManager? snapshots = null,
        string hostVersion = "2025",
        ScriptCompiler? compiler = null)
    {
        _model = model;
        _dispatcher = dispatcher;
        _settings = settings;
        _snapshots = snapshots ?? new TeklaSnapshotManager();
        _hostVersion = hostVersion;

        var teklaAssemblies = GetTeklaApiAssemblies();
        _compiler = compiler ?? CreateDefaultCompiler(settings.ScriptCacheSize, teklaAssemblies);
        _inspector = new TypeInspector(teklaAssemblies, HostName);

        UpdateActiveDocumentTitle();
    }

    public bool IsBusy => Volatile.Read(ref _busy) == 1;

    public int CompiledScriptCount => _compiler.CompiledCount;

    public string? ActiveDocumentTitle => _activeDocumentTitle;

    public bool AllowHeavyOperations
    {
        get => _allowHeavyOperations;
        set
        {
            if (_allowHeavyOperations == value) return;
            _allowHeavyOperations = value;
            Log.Information("HPTekla bridge: Allow Heavy Operations set to {Value}", value);
            StateChanged?.Invoke();
        }
    }

    public event Action? StateChanged;

    public event Action<LastRunInfo>? RunCompleted;

    public async Task<ExecuteResult> ExecuteAsync(
        ExecuteRequest request,
        IProgress<ScriptProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
            return ExecuteResult.Failure($"Another script is still running in {HostName}. Wait for it to finish or call cancel_execution.");

        var sw = Stopwatch.StartNew();
        var label = request.Label ?? "script";
        string? snapshotPath = null;
        var rolledBack = false;

        try
        {
            StateChanged?.Invoke();

            // 1. Guard check (forbidden namespaces, process launch, Reflection, etc.)
            var guardDiagnostics = ScriptGuard.Check(request.Code, GuardProfile.Tekla);
            if (guardDiagnostics.Count > 0)
            {
                var diag = guardDiagnostics.Select(g => new ScriptDiagnostic(g.Line, g.Column, "GUARD", g.Message)).ToArray();
                return Finish(request, new ExecuteResult
                {
                    IsError = true,
                    Message = "Script violates Tekla safety guard rules.",
                    Diagnostics = diag
                }, sw, label, rolledBack: false);
            }

            // 2. 3-Tier Safety Analysis
            var tierResult = TeklaTierAnalyzer.Analyze(request.Code);
            var tier = tierResult.HighestTier;

            if (tier == TeklaTier.Destructive && !AllowHeavyOperations)
            {
                var msg = "Destructive/Heavy operations (e.g. Delete, IFC export) require enabling the 'Allow Heavy Operations' checkbox on the HPTekla bridge window.";
                var diag = new[] { new ScriptDiagnostic(1, 1, "HEAVY", msg) };
                return Finish(request, new ExecuteResult
                {
                    IsError = true,
                    Message = msg,
                    Diagnostics = diag
                }, sw, label, rolledBack: false);
            }

            // 3. Compile script
            var compiled = _compiler.GetOrCompile(request.Code);
            if (!compiled.Succeeded)
            {
                return Finish(request, new ExecuteResult
                {
                    IsError = true,
                    Message = "Compilation failed.",
                    Diagnostics = compiled.Diagnostics
                }, sw, label, rolledBack: false);
            }

            // 4. Pre-mutation Snapshot if Write or Destructive
            if (tier >= TeklaTier.Write)
            {
                try
                {
                    snapshotPath = _snapshots.CreateSnapshot(_model, request.Label);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Could not create Tekla model database snapshot before mutation");
                }
            }

            var timeoutSeconds = Math.Clamp(request.TimeoutSeconds, 5, HostScriptContracts.TeklaHeavyMaxTimeoutSeconds);
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            _currentCancel = linkedCts;

            var logs = new List<string>();

            // 5. Dispatch onto Tekla main thread
            var scriptResult = await _dispatcher.InvokeAsync("execute: " + label, ct =>
            {
                if (!_model.GetConnectionStatus())
                    throw new InvalidOperationException("Tekla Structures model is not connected or no model is open.");

                // Set test save point for atomic dryRun or error rollback
                TeklaOperation.SetTestSavePoint();

                var globals = new TeklaScriptGlobals(
                    model: _model,
                    selector: new UIModelObjectSelector(),
                    ct: ct,
                    log: msg => { lock (logs) logs.Add(msg); },
                    progress: (cur, tot, msg) => progress?.Report(new ScriptProgress(cur, tot, msg)),
                    args: new ScriptArgs(request.Args));

                object? rawResult = null;
                try
                {
                    var state = compiled.Script!.RunAsync(globals, ct).GetAwaiter().GetResult();
                    rawResult = state.ReturnValue;

                    if (request.DryRun)
                    {
                        // Rollback all in-memory mutations natively
                        TeklaOperation.RollbackToTestSavePoint(resetSelection: true);
                        rolledBack = true;
                    }
                    else if (tier >= TeklaTier.Write)
                    {
                        // Live mutation: commit changes to Tekla undo stack
                        _model.CommitChanges(request.Label ?? "HPTekla AI Execution");
                    }

                    return rawResult;
                }
                catch
                {
                    // Always rollback on failure to prevent model corruption
                    try
                    {
                        TeklaOperation.RollbackToTestSavePoint(resetSelection: true);
                        rolledBack = true;
                    }
                    catch (Exception rbEx)
                    {
                        Log.Error(rbEx, "Failed to rollback to test save point on exception");
                    }
                    throw;
                }
            }, linkedCts.Token).ConfigureAwait(false);

            sw.Stop();

            // 6. Serialize result
            JsonElement? jsonValue = null;
            if (scriptResult != null)
            {
                try
                {
                    var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(scriptResult);
                    using var doc = JsonDocument.Parse(jsonBytes);
                    jsonValue = doc.RootElement.Clone();
                }
                catch
                {
                    var text = scriptResult.ToString() ?? string.Empty;
                    var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(text);
                    using var doc = JsonDocument.Parse(jsonBytes);
                    jsonValue = doc.RootElement.Clone();
                }
            }

            var snapshotFileName = !string.IsNullOrEmpty(snapshotPath) ? Path.GetFileName(snapshotPath) : null;

            var result = new ExecuteResult
            {
                IsError = false,
                Value = jsonValue,
                ValueType = scriptResult?.GetType().Name,
                Logs = logs.ToArray(),
                DurationMs = sw.ElapsedMilliseconds,
                RolledBack = rolledBack,
                Snapshot = snapshotFileName
            };

            return Finish(request, result, sw, label, rolledBack);
        }
        catch (BridgeRequestException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Finish(request, ExecuteResult.Failure("Execution timed out or was cancelled.", rolledBack: true, timedOut: true), sw, label, rolledBack: true);
        }
        catch (Exception ex)
        {
            return Finish(request, ExecuteResult.Failure(ex.Message, rolledBack: true), sw, label, rolledBack: true);
        }
        finally
        {
            _currentCancel = null;
            Interlocked.Exchange(ref _busy, 0);
            UpdateActiveDocumentTitle();
            StateChanged?.Invoke();
        }
    }

    public async Task<ContextResult> GetContextAsync(bool includeSelection, CancellationToken cancellationToken)
    {
        if (IsBusy)
            throw BridgeRequestException.Busy(HostName);

        return await _dispatcher.InvokeAsync("context", _ =>
        {
            var isConnected = _model.GetConnectionStatus();
            string? modelName = null;
            string? modelPath = null;
            string? projectName = null;
            int partCount = 0;
            int rebarCount = 0;
            int drawingCount = 0;

            if (isConnected)
            {
                try
                {
                    var info = _model.GetInfo();
                    modelName = info.ModelName;
                    modelPath = info.ModelPath;

                    var proj = _model.GetProjectInfo();
                    projectName = proj.Name;
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to read Tekla model info");
                }

                try
                {
                    var selector = _model.GetModelObjectSelector();
                    var beams = selector.GetAllObjectsWithType(ModelObject.ModelObjectEnum.BEAM);
                    partCount += beams != null ? beams.GetSize() : 0;
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Failed to count parts in Tekla model");
                }

                try
                {
                    var selector = _model.GetModelObjectSelector();
                    var rebars = selector.GetAllObjectsWithType(ModelObject.ModelObjectEnum.REBARGROUP);
                    rebarCount += rebars != null ? rebars.GetSize() : 0;
                    var singleRebars = selector.GetAllObjectsWithType(ModelObject.ModelObjectEnum.SINGLEREBAR);
                    rebarCount += singleRebars != null ? singleRebars.GetSize() : 0;
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Failed to count rebars in Tekla model");
                }

                try
                {
                    var drawingHandler = new DrawingHandler();
                    var drawings = drawingHandler.GetDrawings();
                    drawingCount = drawings != null ? drawings.GetSize() : 0;
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Failed to count drawings in Tekla model");
                }
            }

            var teklaInfo = new TeklaInfo(
                IsConnected: isConnected,
                ModelName: modelName,
                ModelPath: modelPath,
                ProjectName: projectName,
                TeklaVersion: "2025.0",
                HeavyOperationsEnabled: AllowHeavyOperations,
                PartCount: partCount,
                RebarCount: rebarCount,
                DrawingCount: drawingCount);

            return new ContextResult
            {
                Host = "tekla",
                HostVersion = _hostVersion,
                DocTitle = modelName,
                DocPath = modelPath,
                Tekla = teklaInfo
            };
        }, cancellationToken).ConfigureAwait(false);
    }

    public InspectResult Inspect(InspectRequest request) => _inspector.Inspect(request);

    public AnalyzeResult Analyze(AnalyzeRequest request)
    {
        var result = ScriptAnalyzer.Run(_compiler, request.Code, GuardProfile.Tekla, AnalyzerProfile.Tekla);
        var tier = TeklaTierAnalyzer.Analyze(request.Code);
        if (tier.HighestTier == TeklaTier.Destructive)
        {
            var heavyMsg = "Destructive/Heavy operations (Delete, IFC export) require 'Allow Heavy Operations'.";
            var diag = new ScriptDiagnostic(1, 1, "HEAVY", heavyMsg);
            result.GuardViolations = new[] { diag }.Concat(result.GuardViolations).ToArray();
        }
        return result;
    }

    public CancelResult Cancel()
    {
        var current = _currentCancel;
        if (current is null) return new CancelResult(false, false);

        try
        {
            current.Cancel();
            return new CancelResult(true, true);
        }
        catch (ObjectDisposedException)
        {
            return new CancelResult(false, false);
        }
    }

    private void UpdateActiveDocumentTitle()
    {
        try
        {
            if (_model.GetConnectionStatus())
            {
                _activeDocumentTitle = _model.GetInfo().ModelName;
            }
            else
            {
                _activeDocumentTitle = null;
            }
        }
        catch
        {
            _activeDocumentTitle = null;
        }
    }

    private ExecuteResult Finish(ExecuteRequest request, ExecuteResult result, Stopwatch sw, string label, bool rolledBack)
    {
        result.DurationMs = sw.ElapsedMilliseconds;
        result.RolledBack = rolledBack;

        var lastRun = new LastRunInfo(
            Timestamp: DateTimeOffset.Now,
            Label: label,
            Source: request.Code,
            IsError: result.IsError,
            Message: result.Message,
            DurationMs: result.DurationMs,
            RolledBack: rolledBack,
            Added: 0,
            Modified: 0,
            Deleted: 0);

        RunCompleted?.Invoke(lastRun);
        return result;
    }

    private static RefAssembly[] GetTeklaApiAssemblies()
    {
        var list = new List<RefAssembly>
        {
            typeof(Model).Assembly,
            typeof(Tekla.Structures.Identifier).Assembly,
            typeof(Tekla.Structures.Geometry3d.Point).Assembly,
            typeof(Tekla.Structures.Catalogs.ProfileItem).Assembly,
            typeof(DrawingHandler).Assembly,
            typeof(Tekla.Structures.Plugins.PluginBase).Assembly,
            typeof(TeklaScriptGlobals).Assembly
        };
        return list.Distinct().ToArray();
    }

    private static ScriptCompiler CreateDefaultCompiler(int cacheSize, RefAssembly[] assemblies)
    {
        var refs = new List<RefAssembly>
        {
            typeof(object).Assembly,
            typeof(Enumerable).Assembly,
            typeof(List<>).Assembly,
            typeof(ScriptArgs).Assembly,
            typeof(JsonElement).Assembly,
        };
        refs.AddRange(assemblies);

        return new ScriptCompiler(refs.Distinct().ToArray(), HostScriptContracts.TeklaImports, typeof(TeklaScriptGlobals), cacheSize);
    }

    public void Dispose()
    {
        _currentCancel?.Dispose();
    }
}
