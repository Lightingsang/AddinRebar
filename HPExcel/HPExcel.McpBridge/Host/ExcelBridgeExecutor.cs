using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using HPExcel.McpBridge.Com;
using HPExcel.McpBridge.Headless;
using HPExcel.McpBridge.Safety;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.Core.Scripting;
using Serilog;

namespace HPExcel.McpBridge.Host;

/// <summary>
///     Implements IBridgeExecutor for the Microsoft Excel MCP subsystem.
///     Coordinates Roslyn guard checks, 3-tier safety gating, pre-mutation snapshots,
///     and script execution on the STA worker or ClosedXML engine.
/// </summary>
public sealed class ExcelBridgeExecutor : IBridgeExecutor, IDisposable
{
    public const string HostName = "Excel";
    public const int DefaultMaxTimeoutSeconds = 120;

    private readonly ExcelAttachment _attachment;
    private readonly ExcelStaWorker _staWorker;
    private readonly ExcelSafetyGuard _guard;
    private readonly ExcelSnapshotManager _snapshots;
    private readonly ClosedXmlWorkbookService _closedXml;
    private readonly ScriptCompiler _compiler;
    private readonly TypeInspector _inspector;
    private readonly string _hostVersion;
    private int _busy;
    private volatile CancellationTokenSource? _currentCancel;

    public ExcelBridgeExecutor(
        ExcelAttachment attachment,
        ExcelStaWorker staWorker,
        ExcelSafetyGuard guard,
        ExcelSnapshotManager? snapshots = null,
        ClosedXmlWorkbookService? closedXml = null,
        string hostVersion = "2026",
        ScriptCompiler? compiler = null)
    {
        _attachment = attachment;
        _staWorker = staWorker;
        _guard = guard;
        _snapshots = snapshots ?? new ExcelSnapshotManager();
        _closedXml = closedXml ?? new ClosedXmlWorkbookService();
        _hostVersion = hostVersion;

        _compiler = compiler ?? CreateDefaultCompiler();
        _inspector = new TypeInspector(new[]
        {
            typeof(Microsoft.Office.Interop.Excel.Application).Assembly,
            typeof(XLWorkbook).Assembly
        }, HostName);

        _attachment.StateChanged += () => StateChanged?.Invoke();
        _guard.StateChanged += () => StateChanged?.Invoke();
    }

    public ExcelAttachment Attachment => _attachment;
    public ExcelStaWorker StaWorker => _staWorker;
    public ExcelSafetyGuard Guard => _guard;
    public ExcelSnapshotManager Snapshots => _snapshots;
    public ClosedXmlWorkbookService ClosedXml => _closedXml;

    public bool IsBusy => Volatile.Read(ref _busy) == 1;

    public int CompiledScriptCount => _compiler.CompiledCount;

    public string? ActiveDocumentTitle => _attachment.ActiveWorkbookName;

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

            // 1. Guard Check (deny forbidden namespaces, Process.Start, Quit, etc.)
            var guardDiagnostics = ScriptGuard.Check(request.Code, GuardProfile.Excel);
            if (guardDiagnostics.Count > 0)
            {
                var diag = guardDiagnostics.Select(g => new ScriptDiagnostic(g.Line, g.Column, "GUARD", g.Message)).ToArray();
                return Finish(request, new ExecuteResult
                {
                    IsError = true,
                    Message = "Script violates Excel safety guard rules.",
                    Diagnostics = diag
                }, sw, label);
            }

            // 2. 3-Tier Safety Analysis
            var tierResult = ExcelTierAnalyzer.Analyze(request.Code);
            var tier = tierResult.HighestTier;

            // Also check transaction override (auto implies write)
            if (tier == ExcelTier.ReadOnly &&
                string.Equals(TransactionModes.Normalize(request.Transaction), TransactionModes.Auto, StringComparison.OrdinalIgnoreCase))
            {
                tier = ExcelTier.Write;
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
                var members = tier == ExcelTier.Destructive
                    ? string.Join(", ", tierResult.DestructiveMembers)
                    : (tier == ExcelTier.Write ? string.Join(", ", tierResult.WriteMembers) : "none");

                return Finish(request, new ExecuteResult
                {
                    IsError = false,
                    Message = $"Static preview: Tier {tier} script ({members}) compiled cleanly without execution.",
                    DurationMs = sw.ElapsedMilliseconds
                }, sw, label);
            }

            // 5. Automatic Pre-mutation Snapshot if Write or Destructive
            if (tier != ExcelTier.ReadOnly)
            {
                try
                {
                    var snapshotFullPath = await _staWorker.RunOnControlLaneAsync("snapshot-capture", () =>
                    {
                        dynamic? app = _attachment.GetApplication();
                        dynamic? wb = null;
                        string? path = _attachment.ActiveWorkbookPath;

                        if (app != null)
                        {
                            try { wb = app.ActiveWorkbook; } catch { }
                        }

                        return _snapshots.CreateSnapshot(wb, path, request.Label);
                    }).ConfigureAwait(false);

                    snapshotFileName = Path.GetFileName(snapshotFullPath);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to capture pre-mutation snapshot");
                }
            }

            // 6. Execute on STA worker thread
            var timeoutSeconds = Math.Clamp(request.TimeoutSeconds, 5, HostScriptContracts.ExcelHeavyMaxTimeoutSeconds);
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            _currentCancel = linkedCts;

            var logs = new List<string>();

            var scriptTask = _staWorker.RunAsync("script-exec", () =>
            {
                dynamic? app = _attachment.GetApplication();
                dynamic? wb = null;
                dynamic? ws = null;

                if (app != null)
                {
                    try { wb = app.ActiveWorkbook; } catch { }
                    try { ws = app.ActiveSheet; } catch { }
                }

                var globals = new ExcelScriptGlobals
                {
                    excel = app,
                    workbook = wb,
                    sheet = ws,
                    closedXml = _closedXml,
                    ct = linkedCts.Token,
                    log = msg => { lock (logs) logs.Add(msg); },
                    progress = (cur, tot, msg) => progress?.Report(new ScriptProgress(cur, tot, msg)),
                    args = new ScriptArgs(request.Args)
                };

                return compiled.Script!.RunAsync(globals, linkedCts.Token).GetAwaiter().GetResult();
            });

            var scriptResult = await scriptTask.ConfigureAwait(false);
            sw.Stop();

            var returnValue = scriptResult.ReturnValue;
            JsonElement? jsonValue = null;
            if (returnValue != null)
            {
                try
                {
                    var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(returnValue);
                    using var doc = JsonDocument.Parse(jsonBytes);
                    jsonValue = doc.RootElement.Clone();
                }
                catch
                {
                    var text = returnValue.ToString() ?? string.Empty;
                    var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(text);
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

            Log.Error(ex, "Excel script execution error");
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

        var excelInfo = new ExcelInfo(
            IsAttached: _attachment.IsAttached,
            AttachedPid: _attachment.AttachedPid,
            ExcelVersion: _attachment.ExcelVersion,
            ActiveWorkbookName: _attachment.ActiveWorkbookName,
            ActiveWorksheetName: _attachment.ActiveWorksheetName,
            SelectionAddress: _attachment.SelectionAddress,
            WriteEnabled: _guard.IsWriteEnabled,
            DestructiveEnabled: _guard.IsDestructiveEnabled,
            OpenWorkbookCount: _attachment.OpenWorkbookCount,
            WorksheetCount: _attachment.WorksheetCount,
            HasActiveWorkbook: _attachment.HasActiveWorkbook);

        var result = new ContextResult
        {
            Host = PipeNaming.ExcelHost,
            HostVersion = _hostVersion,
            RevitVersion = _hostVersion,
            DocTitle = _attachment.ActiveWorkbookName,
            DocPath = _attachment.ActiveWorkbookPath,
            ExecutionEnabled = _guard.IsExecutionEnabled,
            Excel = excelInfo
        };

        return result;
    }

    public InspectResult Inspect(InspectRequest request) => _inspector.Inspect(request);

    public AnalyzeResult Analyze(AnalyzeRequest request)
    {
        return ScriptAnalyzer.Run(_compiler, request.Code, GuardProfile.Excel, AnalyzerProfile.Excel);
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
            typeof(Microsoft.Office.Interop.Excel.Application).Assembly,
            typeof(Microsoft.Office.Core.MsoTriState).Assembly,
            typeof(XLWorkbook).Assembly,
            typeof(ExcelScriptGlobals).Assembly,
        };

        return new ScriptCompiler(
            references,
            HostScriptContracts.ExcelImports,
            typeof(ExcelScriptGlobals),
            cacheSize);
    }

    public void Dispose()
    {
        try { _currentCancel?.Cancel(); } catch { }
        _attachment.Dispose();
        _staWorker.Dispose();
    }
}
