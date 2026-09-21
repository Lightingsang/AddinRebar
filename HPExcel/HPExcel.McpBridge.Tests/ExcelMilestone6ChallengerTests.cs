using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using HPExcel.McpBridge.Com;
using HPExcel.McpBridge.Headless;
using HPExcel.McpBridge.Host;
using HPExcel.McpBridge.Safety;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Xunit;

namespace HPExcel.McpBridge.Tests;

/// <summary>
///     Adversarial verification and stress harness for Milestone M6 Challenger:
///     1. Named Pipe wire protocol: excel.ping, excel.context, excel.execute, excel.cancel, excel.analyze.
///     2. Strict 3-tier safety gating: Tier R, Tier W, Tier D permissions and cascading disablers.
///     3. PREVIEW mode: dryRun static preview, zero-execution guarantee, snapshot suppression.
///     4. Snapshot engine: Pre-mutation snapshots, 20-file pruning, lock/readonly resilience.
/// </summary>
public sealed class ExcelMilestone6ChallengerTests : IDisposable
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);

    private readonly string _testDir;
    private readonly string _pipeName;
    private readonly ExcelAttachment _attachment;
    private readonly ExcelStaWorker _staWorker;
    private readonly ExcelSafetyGuard _guard;
    private readonly ExcelSnapshotManager _snapshots;
    private readonly ClosedXmlWorkbookService _closedXml;
    private readonly ExcelBridgeExecutor _executor;
    private readonly BridgeSettings _settings;
    private readonly ExcelDispatcher _dispatcher;
    private readonly RequestDispatcher _requestDispatcher;
    private readonly PipeListener _listener;

    public ExcelMilestone6ChallengerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "M6AdvTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);

        _pipeName = "hpexcel-adv-m6-" + Guid.NewGuid().ToString("N");
        _attachment = new ExcelAttachment();
        _staWorker = new ExcelStaWorker();
        _staWorker.Start(); // Actively start STA worker for live execution

        _guard = new ExcelSafetyGuard();
        _snapshots = new ExcelSnapshotManager();
        _closedXml = new ClosedXmlWorkbookService();

        _executor = new ExcelBridgeExecutor(_attachment, _staWorker, _guard, _snapshots, _closedXml, "2026");
        _settings = new BridgeSettings { ExecutionEnabled = true };
        _dispatcher = new ExcelDispatcher(_executor, _settings, "2026");

        _requestDispatcher = new RequestDispatcher(
            _executor,
            _settings,
            "2026",
            "Excel",
            ExcelSafetyGuard.ExecutionDisabledMessage,
            _dispatcher.DispatchCustomAsync);

        _listener = new PipeListener(_pipeName, _requestDispatcher);
        _listener.Start();
    }

    public void Dispose()
    {
        _listener.Dispose();
        _executor.Dispose();
        _staWorker.Dispose();

        try
        {
            if (Directory.Exists(_testDir))
            {
                foreach (var file in Directory.GetFiles(_testDir, "*", SearchOption.AllDirectories))
                {
                    try { File.SetAttributes(file, FileAttributes.Normal); } catch { }
                }
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch { }
    }

    private async Task<NamedPipeClientStream> ConnectAsync(int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await client.ConnectAsync(500);
                return client;
            }
            catch
            {
                await Task.Delay(50);
            }
        }
        throw new TimeoutException($"Could not connect to pipe {_pipeName} within {timeoutMs}ms");
    }

    private async Task<JsonRpcEnvelope> SendRequestAsync(string jsonRpcLine)
    {
        using var client = await ConnectAsync();
        var bytes = Utf8NoBom.GetBytes(jsonRpcLine + "\n");
        await client.WriteAsync(bytes, 0, bytes.Length);
        await client.FlushAsync();

        using var reader = new StreamReader(client, Utf8NoBom, false, 65536, leaveOpen: true);
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            var text = await reader.ReadLineAsync();
            if (text is null) break;
            var envelope = BridgeJson.Deserialize<JsonRpcEnvelope>(text);
            if (envelope is not null) return envelope;
        }

        throw new TimeoutException($"No response received for request: {jsonRpcLine}");
    }

    #region 1. Named Pipe Wire Protocol Verification

    [Fact]
    public async Task WireProtocol_Ping_ReturnsExpectedContractPayload()
    {
        var reply = await SendRequestAsync("{\"jsonrpc\":\"2.0\",\"id\":10,\"method\":\"excel.ping\"}");

        Assert.Equal(10, reply.Id);
        Assert.Null(reply.Error);
        var ping = reply.ResultAs<BridgePingResult>();
        Assert.NotNull(ping);
        Assert.True(ping.Pong);
        Assert.Equal("2026", ping.RevitVersion);
        Assert.True(ping.ExecutionEnabled);
        Assert.False(ping.Busy);
    }

    [Fact]
    public async Task WireProtocol_Context_ReturnsExpectedContextWithExcelInfo()
    {
        var reply = await SendRequestAsync("{\"jsonrpc\":\"2.0\",\"id\":20,\"method\":\"excel.context\",\"params\":{\"includeSelection\":false}}");

        Assert.Equal(20, reply.Id);
        Assert.Null(reply.Error);
        var ctx = reply.ResultAs<ContextResult>();
        Assert.NotNull(ctx);
        Assert.Equal("excel", ctx.Host);
        Assert.Equal("2026", ctx.HostVersion);
        Assert.NotNull(ctx.Excel);
        Assert.False(ctx.Excel.WriteEnabled);
        Assert.False(ctx.Excel.DestructiveEnabled);
    }

    [Fact]
    public async Task WireProtocol_Analyze_RunsRoslynAnalysis_ReturnsLiteralsAndDiagnostics()
    {
        var code = "var x = 100; var title = \"TestSheet\"; log(title);";
        var payload = JsonSerializer.Serialize(new { jsonrpc = "2.0", id = 30, method = "excel.analyze", @params = new { code } });

        var reply = await SendRequestAsync(payload);

        Assert.Equal(30, reply.Id);
        Assert.Null(reply.Error);
        var analyze = reply.ResultAs<AnalyzeResult>();
        Assert.NotNull(analyze);
        Assert.True(analyze.Compiles);
        Assert.Empty(analyze.GuardViolations);
        Assert.Contains(analyze.Literals, l => l.Value == "100");
        Assert.Contains(analyze.Literals, l => l.Value == "TestSheet");
    }

    [Fact]
    public async Task WireProtocol_Execute_RunsRealScriptOnStaWorker_ReturnsResultAndLogs()
    {
        _guard.IsExecutionEnabled = true;

        var code = @"
log(""Execution step 1"");
log(""Execution step 2"");
return new { Result = ""Success"", Number = 42 };
";
        var payload = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = 40,
            method = "excel.execute",
            @params = new { code, label = "wire_exec_test", transaction = "none" }
        });

        var reply = await SendRequestAsync(payload);

        Assert.Equal(40, reply.Id);
        Assert.Null(reply.Error);
        var exec = reply.ResultAs<ExecuteResult>();
        Assert.NotNull(exec);
        Assert.False(exec.IsError);
        Assert.NotNull(exec.Value);

        var doc = JsonDocument.Parse(exec.Value.Value.GetRawText());
        Assert.Equal("Success", doc.RootElement.GetProperty("Result").GetString());
        Assert.Equal(42, doc.RootElement.GetProperty("Number").GetInt32());

        Assert.Contains("Execution step 1", exec.Logs);
        Assert.Contains("Execution step 2", exec.Logs);
    }

    [Fact]
    public async Task WireProtocol_Cancel_RunningExecution_AbortsPromptly()
    {
        _guard.IsExecutionEnabled = true;

        using var client = await ConnectAsync();
        using var writer = new StreamWriter(client, Utf8NoBom, 1024, leaveOpen: true) { AutoFlush = true, NewLine = "\n" };
        using var reader = new StreamReader(client, Utf8NoBom, false, 65536, leaveOpen: true);

        // 1. Send long-running script that honors ct (without denied Thread/Sleep identifiers)
        var longRunningCode = @"
long count = 0;
while (!ct.IsCancellationRequested)
{
    count++;
}
ct.ThrowIfCancellationRequested();
return count;
";
        var execPayload = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = 50,
            method = "excel.execute",
            @params = new { code = longRunningCode, label = "long_run", transaction = "none", timeoutSeconds = 30 }
        });

        await writer.WriteLineAsync(execPayload);

        // Allow script compilation and startup on STA thread
        await Task.Delay(1000);

        // 2. Send cancel on the same pipe stream (PipeListener has maxInstances=1 and dispatches fire-and-forget)
        await writer.WriteLineAsync("{\"jsonrpc\":\"2.0\",\"id\":51,\"method\":\"excel.cancel\"}");

        // 3. Read responses from the stream (cancel reply and execute reply)
        JsonRpcEnvelope? cancelReply = null;
        JsonRpcEnvelope? execReply = null;

        for (int i = 0; i < 2; i++)
        {
            var line = await reader.ReadLineAsync();
            Assert.NotNull(line);
            var envelope = BridgeJson.Deserialize<JsonRpcEnvelope>(line);
            if (envelope?.Id == 51) cancelReply = envelope;
            else if (envelope?.Id == 50) execReply = envelope;
        }

        Assert.NotNull(cancelReply);
        var cancelResult = cancelReply.ResultAs<CancelResult>();
        Assert.NotNull(cancelResult);
        Assert.True(cancelResult.Cancelled);
        Assert.True(cancelResult.WasRunning);

        Assert.NotNull(execReply);
        var execResult = execReply.ResultAs<ExecuteResult>();
        Assert.NotNull(execResult);
        Assert.True(execResult.IsError);
        Assert.Contains("cancelled or timed out", execResult.Message);
    }

    #endregion

    #region 2. Strict 3-Tier Safety Engine Verification

    [Fact]
    public async Task SafetyTier_AllGatesDisabled_RejectsAllTiers()
    {
        _guard.IsExecutionEnabled = false;

        // Tier R
        var rPayload = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = 61,
            method = "excel.execute",
            @params = new { code = "return 1;", transaction = "none" }
        });
        var rReply = await SendRequestAsync(rPayload);
        Assert.Equal(61, rReply.Id);
        Assert.Equal(BridgeErrorCode.ExecutionDisabled, rReply.Error?.Code);
        Assert.Contains("Allow AI execution", rReply.Error?.Message);

        // Tier W
        var wPayload = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = 62,
            method = "excel.execute",
            @params = new { code = "sheet.Cells[1, 1] = 100;", transaction = "auto" }
        });
        var wReply = await SendRequestAsync(wPayload);
        Assert.Equal(62, wReply.Id);
        Assert.Equal(BridgeErrorCode.ExecutionDisabled, wReply.Error?.Code);

        // Tier D
        var dPayload = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = 63,
            method = "excel.execute",
            @params = new { code = "sheet.Delete();", transaction = "auto" }
        });
        var dReply = await SendRequestAsync(dPayload);
        Assert.Equal(63, dReply.Id);
        Assert.Equal(BridgeErrorCode.ExecutionDisabled, dReply.Error?.Code);
    }

    [Fact]
    public async Task SafetyTier_ExecutionOnlyEnabled_AllowsTierR_RejectsTierWAndD()
    {
        _guard.IsExecutionEnabled = true;
        _guard.IsWriteEnabled = false;
        _guard.IsDestructiveEnabled = false;

        // Tier R: Permitted
        var rPayload = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = 71,
            method = "excel.execute",
            @params = new { code = "return 10 + 20;", transaction = "none" }
        });
        var rReply = await SendRequestAsync(rPayload);
        Assert.Equal(71, rReply.Id);
        Assert.Null(rReply.Error);
        var exec = rReply.ResultAs<ExecuteResult>();
        Assert.False(exec!.IsError);

        // Tier W: Rejected with Write message
        var wPayload = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = 72,
            method = "excel.execute",
            @params = new { code = "sheet.Cells[1, 1] = 100;", transaction = "auto" }
        });
        var wReply = await SendRequestAsync(wPayload);
        Assert.Equal(72, wReply.Id);
        Assert.Equal(BridgeErrorCode.ExecutionDisabled, wReply.Error?.Code);
        Assert.Contains("Allow write operations", wReply.Error?.Message);

        // Tier D: Rejected (Write disabled first)
        var dPayload = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = 73,
            method = "excel.execute",
            @params = new { code = "sheet.Delete();", transaction = "auto" }
        });
        var dReply = await SendRequestAsync(dPayload);
        Assert.Equal(73, dReply.Id);
        Assert.Equal(BridgeErrorCode.ExecutionDisabled, dReply.Error?.Code);
        Assert.Contains("Allow write operations", dReply.Error?.Message);
    }

    [Fact]
    public async Task SafetyTier_ExecutionAndWriteEnabled_AllowsTierW_RejectsTierD()
    {
        _guard.IsExecutionEnabled = true;
        _guard.IsWriteEnabled = true;
        _guard.IsDestructiveEnabled = false;

        // Tier W: Allowed (dryRun or no com workbook)
        var wPayload = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = 81,
            method = "excel.execute",
            @params = new { code = "var x = sheet.Name; sheet.AutoFit(); return x;", dryRun = true }
        });
        var wReply = await SendRequestAsync(wPayload);
        Assert.Equal(81, wReply.Id);
        Assert.Null(wReply.Error);
        var exec = wReply.ResultAs<ExecuteResult>();
        Assert.False(exec!.IsError);
        Assert.Contains("Static preview: Tier Write", exec.Message);

        // Tier D: Rejected with Destructive message
        var dPayload = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = 82,
            method = "excel.execute",
            @params = new { code = "sheet.Delete();" }
        });
        var dReply = await SendRequestAsync(dPayload);
        Assert.Equal(82, dReply.Id);
        Assert.Equal(BridgeErrorCode.ExecutionDisabled, dReply.Error?.Code);
        Assert.Contains("Allow destructive operations", dReply.Error?.Message);
    }

    [Fact]
    public async Task SafetyTier_TransactionAuto_PromotesReadOnlyToTierW()
    {
        _guard.IsExecutionEnabled = true;
        _guard.IsWriteEnabled = false;

        // Pure read code, but caller specified transaction: "auto"
        var payload = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = 91,
            method = "excel.execute",
            @params = new { code = "return 123;", transaction = "auto" }
        });

        var reply = await SendRequestAsync(payload);
        Assert.Equal(91, reply.Id);
        Assert.Equal(BridgeErrorCode.ExecutionDisabled, reply.Error?.Code);
        Assert.Contains("Allow write operations", reply.Error?.Message);
    }

    #endregion

    #region 3. PREVIEW Mode (DryRun) Verification

    [Fact]
    public async Task PreviewMode_DryRun_ReturnsPlannedMembers_WithoutExecutionOrSnapshot()
    {
        _guard.IsExecutionEnabled = true;
        _guard.IsWriteEnabled = true;

        var code = @"
sheet.Cells[1, 1] = 500;
sheet.AutoFit();
return 999;
";
        var payload = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = 101,
            method = "excel.execute",
            @params = new { code, dryRun = true }
        });

        var reply = await SendRequestAsync(payload);

        Assert.Equal(101, reply.Id);
        Assert.Null(reply.Error);
        var exec = reply.ResultAs<ExecuteResult>();
        Assert.NotNull(exec);
        Assert.False(exec.IsError);
        Assert.Contains("Static preview: Tier Write", exec.Message);
        Assert.Contains("AutoFit", exec.Message);
        Assert.Null(exec.Snapshot); // Zero snapshot captured
        Assert.Null(exec.Value);    // Did not execute -> no return value
    }

    [Fact]
    public async Task PreviewMode_DestructiveScript_ReportsDestructiveMembersInPreview()
    {
        _guard.IsExecutionEnabled = true;
        _guard.IsWriteEnabled = true;
        _guard.IsDestructiveEnabled = true;

        var code = "sheet.Range(\"A1:B10\").ClearContents(); sheet.Delete();";
        var payload = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = 102,
            method = "excel.execute",
            @params = new { code, dryRun = true }
        });

        var reply = await SendRequestAsync(payload);

        Assert.Equal(102, reply.Id);
        Assert.Null(reply.Error);
        var exec = reply.ResultAs<ExecuteResult>();
        Assert.NotNull(exec);
        Assert.False(exec.IsError);
        Assert.Contains("Static preview: Tier Destructive", exec.Message);
        Assert.Contains("Delete", exec.Message);
        Assert.Null(exec.Snapshot);
    }

    #endregion

    #region 4. Snapshot Engine & 20-File Pruning Verification

    [Fact]
    public void SnapshotEngine_PrunesToNewest20_AndHandlesLockedFilesSafely()
    {
        var snapDir = Path.Combine(_testDir, "PruningTest");
        Directory.CreateDirectory(snapDir);

        // Create 28 snapshot files
        for (int i = 1; i <= 28; i++)
        {
            var fileName = $"20260921-1000{i:D2}_TestBook.xlsx";
            File.WriteAllText(Path.Combine(snapDir, fileName), $"Snapshot content {i}");
        }

        // Lock file #2 (an older file) with FileShare.None to simulate active process lock
        var lockedFilePath = Path.Combine(snapDir, "20260921-100002_TestBook.xlsx");
        using (var lockStream = new FileStream(lockedFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            // Prune retaining 20 files -> should attempt to delete 8 files
            // 7 files should delete successfully; 1 file (locked) catches exception and survives
            var deletedCount = ExcelSnapshotManager.Prune(snapDir, maxRetained: 20);

            Assert.Equal(7, deletedCount);

            var remainingFiles = Directory.GetFiles(snapDir, "*.xlsx");
            Assert.Equal(21, remainingFiles.Length);
            Assert.Contains(lockedFilePath, remainingFiles);
        }
    }

    [Fact]
    public void SnapshotEngine_Sanitization_EnforcesSafeLengthAndCharacters()
    {
        var sanitized = ExcelSnapshotManager.Sanitize("Illegal/\\:*?\"<>|Name!@#$%^&*()");
        Assert.DoesNotContain("/", sanitized);
        Assert.DoesNotContain("\\", sanitized);
        Assert.DoesNotContain(":", sanitized);
        Assert.DoesNotContain("*", sanitized);
        Assert.DoesNotContain("?", sanitized);

        var longName = new string('A', 100);
        var sanitizedLong = ExcelSnapshotManager.Sanitize(longName);
        Assert.True(sanitizedLong.Length <= 40);

        Assert.Equal("snapshot", ExcelSnapshotManager.Sanitize(""));
        Assert.Equal("snapshot", ExcelSnapshotManager.Sanitize("   "));
    }

    [Fact]
    public void SnapshotEngine_HeadlessWorkbook_CapturesBackupCopyBeforeMutation()
    {
        var manager = new ExcelSnapshotManager();
        var wbPath = Path.Combine(_testDir, "RealBook.xlsx");

        // Create initial ClosedXML file
        using (var wb = new XLWorkbook())
        {
            wb.Worksheets.Add("Sheet1").Cell("A1").Value = "Initial Data";
            wb.SaveAs(wbPath);
        }

        // Capture snapshot
        var snapPath = manager.CreateSnapshot(null, wbPath, "pre_mutation");

        Assert.True(File.Exists(snapPath));
        Assert.Contains(".hpexcel_snapshots", snapPath);
        Assert.Contains("RealBook_pre_mutation", snapPath);

        // Verify snapshot content matches initial data
        using (var snapWb = new XLWorkbook(snapPath))
        {
            Assert.Equal("Initial Data", snapWb.Worksheets.First().Cell("A1").GetString());
        }
    }

    #endregion
}
