using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPPowerBi.McpBridge.Safety;
using HPPowerBi.McpBridge.Tabular;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.AnalysisServices.AdomdClient;
using Microsoft.AnalysisServices.Tabular;
using Serilog;
using Assembly = System.Reflection.Assembly;

namespace HPPowerBi.McpBridge.Host;

/// <summary>
///     Implements IBridgeExecutor for the Power BI MCP subsystem.
///     Coordinates script guard checks, compilation, snapshot capturing, and execution
///     against Power BI's AMO-TOM model and ADOMD connection.
/// </summary>
public sealed class PowerBiBridgeExecutor : IBridgeExecutor, IDisposable
{
    public const string HostName = "Power BI";
    public const int DefaultMaxTimeoutSeconds = 120;

    private readonly PbiConnectionManager _connection;
    private readonly PbiSafetyGuard _guard;
    private readonly PbiSnapshotManager _snapshots;
    private readonly ScriptCompiler _compiler;
    private readonly TypeInspector _inspector;
    private readonly string _hostVersion;
    private int _busy;
    private volatile CancellationTokenSource? _currentCancel;

    public PowerBiBridgeExecutor(
        PbiConnectionManager connection,
        PbiSafetyGuard guard,
        PbiSnapshotManager? snapshots = null,
        string hostVersion = "2026",
        ScriptCompiler? compiler = null)
    {
        _connection = connection;
        _guard = guard;
        _snapshots = snapshots ?? new PbiSnapshotManager();
        _hostVersion = hostVersion;

        _compiler = compiler ?? CreateDefaultCompiler();
        _inspector = new TypeInspector(new[] { typeof(Microsoft.AnalysisServices.Tabular.Model).Assembly, typeof(AdomdConnection).Assembly }, HostName);

        _connection.StateChanged += () => StateChanged?.Invoke();
        _guard.StateChanged += () => StateChanged?.Invoke();
    }

    public PbiConnectionManager Connection => _connection;
    public PbiSafetyGuard Guard => _guard;
    public PbiSnapshotManager Snapshots => _snapshots;

    public bool IsBusy => Volatile.Read(ref _busy) == 1;

    public int CompiledScriptCount => _compiler.CompiledCount;

    public string? ActiveDocumentTitle => _connection.IsConnected
        ? $"{_connection.Database?.Name ?? "Model"} (Port {_connection.CurrentPort})"
        : null;

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
        string? snapshotName = null;

        try
        {
            StateChanged?.Invoke();

            // 1. Layer 1: Check Execution Enabled opt-in
            _guard.EnsureExecutionAllowed();

            // 2. Layer 2: Guard Check (deny forbidden namespaces, identifiers, methods)
            var guardDiagnostics = ScriptGuard.Check(request.Code, GuardProfile.PowerBi);
            if (guardDiagnostics.Count > 0)
            {
                var diag = guardDiagnostics.Select(g => new ScriptDiagnostic(g.Line, g.Column, "GUARD", g.Message)).ToArray();
                return Finish(request, new ExecuteResult
                {
                    IsError = true,
                    Message = "Script violates Power BI safety guard rules.",
                    Diagnostics = diag
                }, sw, label);
            }

            // 3. Detect mutation & check Layer 1 second opt-in
            var isMutation = PbiSafetyGuard.IsMutationScript(request.Code) ||
                             string.Equals(TransactionModes.Normalize(request.Transaction), TransactionModes.Auto, StringComparison.OrdinalIgnoreCase);

            if (isMutation)
            {
                _guard.EnsureMutationAllowed();
            }

            // 4. Compile script
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

            // 5. Dry run preview
            if (request.DryRun)
            {
                return Finish(request, new ExecuteResult
                {
                    IsError = false,
                    Message = "Static preview: script compiled and passed all guard rules cleanly without execution.",
                    DurationMs = sw.ElapsedMilliseconds
                }, sw, label);
            }

            // 6. Ensure connected
            if (!_connection.IsConnected || _connection.Model == null)
            {
                throw new BridgeRequestException(BridgeErrorCode.InternalError,
                    "Not connected to Power BI Desktop. Please connect to a running instance before executing scripts.");
            }

            // 7. Layer 3: Capture Pre-mutation snapshot if modifying model
            if (isMutation && _connection.Database != null)
            {
                try
                {
                    snapshotName = _snapshots.CreateSnapshot(_connection.Database, request.Label);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to capture pre-mutation snapshot");
                    throw new BridgeRequestException(BridgeErrorCode.InternalError,
                        $"Failed to create safety snapshot before mutation: {ex.Message}. Mutation aborted.");
                }
            }

            // 8. Execute script on background thread with timeout
            var timeoutSeconds = Math.Clamp(request.TimeoutSeconds, 5, HostScriptContracts.PowerBiHeavyMaxTimeoutSeconds);
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            _currentCancel = linkedCts;

            var logs = new List<string>();
            var globals = new PowerBiScriptGlobals
            {
                model = _connection.Model,
                server = _connection.Server!,
                adomd = _connection.Adomd!,
                ct = linkedCts.Token,
                log = msg => { lock (logs) logs.Add(msg); },
                progress = (cur, tot, msg) => progress?.Report(new ScriptProgress(cur, tot, msg)),
                args = new ScriptArgs(request.Args)
            };

            var scriptResult = await Task.Run(async () =>
            {
                return await compiled.Script!.RunAsync(globals, linkedCts.Token).ConfigureAwait(false);
            }, linkedCts.Token).ConfigureAwait(false);

            sw.Stop();

            var returnValue = scriptResult.ReturnValue;
            JsonElement? jsonValue = null;
            if (returnValue != null)
            {
                try
                {
                    var jsonBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(returnValue);
                    using var doc = JsonDocument.Parse(jsonBytes);
                    jsonValue = doc.RootElement.Clone();
                }
                catch
                {
                    // Fallback to simple string JSON element if object is not directly serializable
                    var text = returnValue.ToString() ?? string.Empty;
                    var jsonBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(text);
                    using var doc = JsonDocument.Parse(jsonBytes);
                    jsonValue = doc.RootElement.Clone();
                }
            }

            var result = new ExecuteResult
            {
                IsError = false,
                Value = jsonValue,
                ValueType = returnValue?.GetType().Name,
                Logs = logs.ToArray(),
                DurationMs = sw.ElapsedMilliseconds,
                Snapshot = snapshotName
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
            Log.Error(ex, "Power BI script execution error");
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

    public Task<ContextResult> GetContextAsync(bool includeSelection, CancellationToken cancellationToken)
    {
        if (IsBusy)
            throw BridgeRequestException.Busy(HostName);

        var pbiInfo = new PowerBiInfo(
            IsConnected: _connection.IsConnected,
            AttachedPid: _connection.ActiveInstance?.ProcessId,
            LocalPort: _connection.CurrentPort,
            DatabaseName: _connection.Database?.Name,
            CompatibilityLevel: _connection.Database?.CompatibilityLevel.ToString(),
            MutationEnabled: _guard.IsMutationEnabled,
            TableCount: _connection.Model?.Tables.Count ?? 0,
            MeasureCount: _connection.Model != null ? _connection.Model.Tables.Sum(t => t.Measures.Count) : 0,
            RelationshipCount: _connection.Model?.Relationships.Count ?? 0);

        var result = new ContextResult
        {
            Host = HostName,
            HostVersion = _hostVersion,
            RevitVersion = _hostVersion,
            DocTitle = _connection.Database?.Name,
            ExecutionEnabled = _guard.IsExecutionEnabled,
            PowerBi = pbiInfo
        };

        return Task.FromResult(result);
    }

    public InspectResult Inspect(InspectRequest request) => _inspector.Inspect(request);

    public AnalyzeResult Analyze(AnalyzeRequest request)
    {
        return ScriptAnalyzer.Run(_compiler, request.Code, GuardProfile.PowerBi, AnalyzerProfile.PowerBi);
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
            typeof(System.Text.Json.JsonElement).Assembly,
            typeof(Microsoft.AnalysisServices.Tabular.Model).Assembly,
            typeof(AdomdConnection).Assembly,
            typeof(PowerBiScriptGlobals).Assembly,
        };

        return new ScriptCompiler(
            references,
            HostScriptContracts.PowerBiImports,
            typeof(PowerBiScriptGlobals),
            cacheSize);
    }

    public void Dispose()
    {
        try { _currentCancel?.Cancel(); } catch { }
        _connection.Dispose();
    }
}
