using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPRobot.McpBridge.Com;
using HPRobot.McpBridge.Safety;
using HPRobot.McpBridge.Units;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.Core.Scripting;
using RobotOM;
using Serilog;

namespace HPRobot.McpBridge.Host;

/// <summary>
///     Implements IBridgeExecutor for Autodesk Robot Structural Analysis Professional 2026.
///     Coordinates Roslyn guard checks, 3-tier safety gating, pre-mutation .rtd snapshots,
///     Metric units standardization, and script execution on the dedicated STA worker thread.
/// </summary>
public sealed class RobotBridgeExecutor : IBridgeExecutor, IDisposable
{
    public const string HostName = "Robot";
    public const int DefaultMaxTimeoutSeconds = 120;

    private readonly RobotAttachment _attachment;
    private readonly RobotStaWorker _staWorker;
    private readonly RobotSafetyGuard _guard;
    private readonly RobotSnapshotManager _snapshots;
    private readonly ScriptCompiler _compiler;
    private readonly TypeInspector _inspector;
    private readonly string _hostVersion;
    private int _busy;
    private volatile CancellationTokenSource? _currentCancel;

    public RobotBridgeExecutor(
        RobotAttachment attachment,
        RobotStaWorker staWorker,
        RobotSafetyGuard guard,
        RobotSnapshotManager? snapshots = null,
        string hostVersion = "2026",
        ScriptCompiler? compiler = null)
    {
        _attachment = attachment;
        _staWorker = staWorker;
        _guard = guard;
        _snapshots = snapshots ?? new RobotSnapshotManager();
        _hostVersion = hostVersion;

        _compiler = compiler ?? CreateDefaultCompiler();
        _inspector = new TypeInspector(new[]
        {
            typeof(IRobotApplication).Assembly
        }, HostName);

        _attachment.StateChanged += () => StateChanged?.Invoke();
        _guard.StateChanged += () => StateChanged?.Invoke();
    }

    public RobotAttachment Attachment => _attachment;
    public RobotStaWorker StaWorker => _staWorker;
    public RobotSafetyGuard Guard => _guard;
    public RobotSnapshotManager Snapshots => _snapshots;

    public bool IsBusy => Volatile.Read(ref _busy) == 1;

    public int CompiledScriptCount => _compiler.CompiledCount;

    public string? ActiveDocumentTitle => _attachment.ActiveModelFileName;

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
        string? snapshotFileName = null;

        try
        {
            StateChanged?.Invoke();

            // 1. Guard Check (blocking Quit, Process.Start, reflection, etc.)
            var guardDiagnostics = ScriptGuard.Check(request.Code, GuardProfile.Robot);
            if (guardDiagnostics.Count > 0)
            {
                var diag = guardDiagnostics.Select(g => new ScriptDiagnostic(g.Line, g.Column, "GUARD", g.Message)).ToArray();
                return Finish(request, new ExecuteResult
                {
                    IsError = true,
                    Message = "Script violates Robot safety guard rules.",
                    Diagnostics = diag
                }, sw, label);
            }

            // 2. 3-Tier Safety Analysis
            var tierResult = RobotTierAnalyzer.Analyze(request.Code);
            var tier = tierResult.HighestTier;

            // Normalize transaction mode: "auto" escalates Read to Write
            if (tier == RobotTier.Read &&
                string.Equals(TransactionModes.Normalize(request.Transaction), TransactionModes.Auto, StringComparison.OrdinalIgnoreCase))
            {
                tier = RobotTier.Write;
            }

            // Enforce UI gating permissions
            _guard.EnsureTierAllowed(tier);

            // 3. Compile script
            var compiled = _compiler.GetOrCompile(request.Code);
            if (!compiled.Succeeded)
            {
                return Finish(request, new ExecuteResult
                {
                    IsError = true,
                    Message = "Compilation failed.",
                    Diagnostics = compiled.Diagnostics
                }, sw, label);
            }

            // 4. Dry Run Preview
            if (request.DryRun)
            {
                var members = tier == RobotTier.DeleteHeavy
                    ? string.Join(", ", tierResult.DeleteHeavyMembers)
                    : (tier == RobotTier.Write ? string.Join(", ", tierResult.WriteMembers) : "none");

                return Finish(request, new ExecuteResult
                {
                    IsError = false,
                    Message = $"Static preview: Tier {tier} script ({members}) compiled cleanly without execution.",
                    DurationMs = sw.ElapsedMilliseconds
                }, sw, label);
            }

            // 5. Automatic Pre-mutation Snapshot if Write or DeleteHeavy
            if (tier != RobotTier.Read)
            {
                try
                {
                    var snapshotFullPath = await _staWorker.RunOnControlLaneAsync("snapshot-capture", () =>
                    {
                        IRobotApplication? app = null;
                        try { app = _attachment.GetApplication(); } catch { }
                        string? path = _attachment.ActiveModelPath;
                        return _snapshots.CreateSnapshot(app, path, request.Label);
                    }).ConfigureAwait(false);

                    snapshotFileName = Path.GetFileName(snapshotFullPath);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to capture pre-mutation model snapshot");
                }
            }

            // 6. Execute on STA worker thread with units standardization
            var timeoutSeconds = Math.Clamp(request.TimeoutSeconds, 5, HostScriptContracts.RobotHeavyMaxTimeoutSeconds);
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            _currentCancel = linkedCts;

            var logs = new List<string>();

            var scriptTask = _staWorker.RunAsync("script-exec", () =>
            {
                IRobotApplication? app = null;
                IRobotStructure? str = null;
                IRobotUnitMngr? unitMngr = null;

                try
                {
                    app = _attachment.GetApplication();
                    if (app != null && app.Project != null && app.Project.IsActive != 0)
                    {
                        str = app.Project.Structure;
                        try { unitMngr = app.Project.Preferences?.Units; } catch { }
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Could not acquire Robot project or structure objects");
                }

                var globals = new RobotScriptGlobals
                {
                    robot = app,
                    structure = str,
                    units = unitMngr,
                    args = new ScriptArgs(request.Args),
                    log = msg => { lock (logs) logs.Add(msg); },
                    progress = (cur, tot, msg) => progress?.Report(new ScriptProgress(cur, tot, msg)),
                    ct = linkedCts.Token
                };

                object? rawResult = null;

                // Run within standardized Metric units policy
                RobotUnitsPolicy.Run(unitMngr, logs, () =>
                {
                    var state = compiled.Script!.RunAsync(globals, linkedCts.Token).GetAwaiter().GetResult();
                    rawResult = state.ReturnValue;
                });

                return rawResult;
            });

            var scriptResult = await scriptTask.ConfigureAwait(false);
            sw.Stop();

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

            var result = new ExecuteResult
            {
                IsError = false,
                Value = jsonValue,
                ValueType = scriptResult?.GetType().Name,
                Logs = logs.ToArray(),
                DurationMs = sw.ElapsedMilliseconds,
                Snapshot = snapshotFileName
            };

            return Finish(request, result, sw, label);
        }
        catch (BridgeRequestException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Finish(request, ExecuteResult.Failure("Script execution was cancelled or timed out."), sw, label);
        }
        catch (Exception ex)
        {
            var refusal = _attachment.DetachIfGone(ex);
            if (refusal != null) throw refusal;

            Log.Error(ex, "Robot script execution error");
            return Finish(request, ExecuteResult.Failure(SafeText.StripPaths($"{ex.GetType().Name}: {ex.Message}")), sw, label);
        }
        finally
        {
            _currentCancel = null;
            Volatile.Write(ref _busy, 0);
            StateChanged?.Invoke();
        }
    }

    private ExecuteResult Finish(ExecuteRequest request, ExecuteResult result, Stopwatch sw, string label)
    {
        sw.Stop();
        result.DurationMs = sw.ElapsedMilliseconds;

        RunCompleted?.Invoke(new LastRunInfo(
            Timestamp: DateTimeOffset.UtcNow,
            Label: label,
            Source: "Script",
            IsError: result.IsError,
            Message: result.IsError ? result.Message : null,
            DurationMs: sw.ElapsedMilliseconds,
            RolledBack: result.RolledBack,
            Added: 0,
            Modified: 0,
            Deleted: 0));

        return result;
    }

    public async Task<ContextResult> GetContextAsync(bool includeSelection, CancellationToken cancellationToken)
    {
        if (IsBusy)
            throw BridgeRequestException.Busy(HostName);

        if (_attachment.IsAttached)
        {
            await _staWorker.RunOnControlLaneAsync("context-refresh", () =>
            {
                _attachment.RefreshContext();
            }).ConfigureAwait(false);
        }

        var robotInfo = new RobotInfo(
            IsAttached: _attachment.IsAttached,
            AttachedPid: _attachment.AttachedPid,
            RobotVersion: _attachment.RobotVersion,
            StructureType: _attachment.StructureType,
            IsCalculated: _attachment.IsCalculated,
            HeavyOperationsEnabled: _guard.IsHeavyOperationsEnabled,
            NodeCount: _attachment.NodeCount,
            BarCount: _attachment.BarCount,
            PanelCount: _attachment.PanelCount,
            LoadCaseCount: _attachment.LoadCaseCount);

        var result = new ContextResult
        {
            Host = PipeNaming.RobotHost,
            HostVersion = _hostVersion,
            RevitVersion = _hostVersion,
            DocTitle = _attachment.ActiveModelFileName,
            DocPath = _attachment.ActiveModelPath,
            ExecutionEnabled = _guard.IsExecutionEnabled,
            Robot = robotInfo
        };

        return result;
    }

    public InspectResult Inspect(InspectRequest request) => _inspector.Inspect(request);

    public AnalyzeResult Analyze(AnalyzeRequest request)
    {
        return ScriptAnalyzer.Run(_compiler, request.Code, GuardProfile.Robot, AnalyzerProfile.Robot);
    }

    public CancelResult Cancel()
    {
        var cancel = _currentCancel;
        if (cancel == null)
            return new CancelResult(false, false);

        try
        {
            cancel.Cancel();
            return new CancelResult(true, true);
        }
        catch (ObjectDisposedException)
        {
            return new CancelResult(false, false);
        }
    }

    public static ScriptCompiler CreateDefaultCompiler(int cacheSize = 64)
    {
        var references = new List<Assembly>
        {
            typeof(object).Assembly,
            typeof(Enumerable).Assembly,
            typeof(List<>).Assembly,
            Assembly.Load("netstandard"),
            Assembly.Load("System.Runtime"),
            Assembly.Load("System.Collections"),
            typeof(ScriptArgs).Assembly,
            typeof(JsonElement).Assembly,
            typeof(IRobotApplication).Assembly,
            typeof(RobotScriptGlobals).Assembly,
        };

        return new ScriptCompiler(
            references,
            HostScriptContracts.RobotImports,
            typeof(RobotScriptGlobals),
            cacheSize);
    }

    public void Dispose()
    {
        try { _currentCancel?.Cancel(); } catch { }
        _attachment.Dispose();
        _staWorker.Dispose();
    }
}
