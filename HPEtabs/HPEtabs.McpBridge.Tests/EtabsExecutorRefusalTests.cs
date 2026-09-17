using System.IO;
using System.Runtime.InteropServices;
using HPEtabs.McpBridge.Service;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

namespace HPEtabs.McpBridge.Tests;

/// <summary>
///     Everything the executor decides before it needs ETABS, driven through the real pipeline with no attachment:
///     the JSON-RPC codes (opt-in, no document), the static preview, the path refusal, the analysis verdicts, the
///     guard's coverage of this assembly, and the liveness rules. The STA worker runs; nothing ever reaches ETABS.
/// </summary>
public sealed class EtabsExecutorRefusalTests : IDisposable
{
    private readonly string _temp = Path.Combine(Path.GetTempPath(), "hpetabs-exec-" + Guid.NewGuid().ToString("N"));
    private readonly BridgeSettings _settings = new() { ExecutionEnabled = true };
    private readonly EtabsExecutor _executor;

    public EtabsExecutorRefusalTests()
    {
        var runner = new EtabsScriptRunner(_settings, new EtabsResultSerializer(_settings.MaxOutputBytes), new EtabsSnapshotManager(Path.Combine(_temp, "snapshots")));
        _executor = new EtabsExecutor(_settings, EtabsScriptEnvironment.Compiler, EtabsScriptEnvironment.Analyzer, runner, new EtabsAttachment(),
            new TypeInspector([EtabsScriptEnvironment.Wrapper], "ETABS"), new AuditLogger(Path.Combine(_temp, "audit")), "22", TimeSpan.FromSeconds(1));
    }

    private Task<ExecuteResult> Execute(string code, string transaction = TransactionModes.Auto, bool dryRun = false) =>
        _executor.ExecuteAsync(new ExecuteRequest(code, transaction, dryRun, 10, "test"), null, CancellationToken.None);

    [Fact]
    public async Task A_destructive_member_with_the_second_opt_in_off_is_a_json_rpc_refusal_not_a_run()
    {
        var exception = await Assert.ThrowsAsync<BridgeRequestException>(() => Execute("return sapModel.Analyze.RunAnalysis();"));

        Assert.Equal(BridgeErrorCode.ExecutionDisabled, exception.Code);
        Assert.Contains("Allow destructive operations", exception.Message);
        Assert.False(_executor.DestructiveOperationsEnabled);
    }

    [Fact]
    public void The_destructive_flag_cannot_be_on_while_execution_is_off()
    {
        _settings.ExecutionEnabled = false;
        _executor.DestructiveOperationsEnabled = true;
        Assert.False(_executor.DestructiveOperationsEnabled);

        _settings.ExecutionEnabled = true;
        _executor.DestructiveOperationsEnabled = true;
        Assert.True(_executor.DestructiveOperationsEnabled);
        _executor.DestructiveOperationsEnabled = false;
    }

    [Theory]
    [InlineData(TransactionModes.None, false)]
    [InlineData(TransactionModes.Auto, true)]
    [InlineData(TransactionModes.Manual, true)]
    public async Task A_writing_script_under_none_or_dry_run_is_a_static_preview_that_never_needs_etabs(string transaction, bool dryRun)
    {
        var result = await Execute("return sapModel.FrameObj.SetSection(\"F1\", \"C40x40\");", transaction, dryRun);

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Contains("static preview", result.Message);
        Assert.Contains("nothing ran", result.Message);
        Assert.Single(result.Diagnostics);
        Assert.Equal("PREVIEW", result.Diagnostics[0].Id);
        Assert.Equal("cFrameObj.SetSection (W)", result.Diagnostics[0].Message);
    }

    [Theory]
    [InlineData(TransactionModes.None, false)]
    [InlineData(TransactionModes.Auto, true)]
    public async Task A_destructive_script_under_none_or_dry_run_is_previewed_without_the_second_opt_in(string transaction, bool dryRun)
    {
        var result = await Execute("return sapModel.Analyze.RunAnalysis();", transaction, dryRun);

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Contains(result.Diagnostics, d => d.Id == "PREVIEW" && d.Message == "cAnalyze.RunAnalysis (D)");
    }

    [Fact]
    public async Task A_read_only_script_without_an_attachment_is_refused_with_the_no_document_code_at_once()
    {
        var exception = await Assert.ThrowsAsync<BridgeRequestException>(() => Execute("return sapModel.GetModelFilename();", TransactionModes.None));

        Assert.Equal(BridgeErrorCode.NoActiveDocument, exception.Code);
        Assert.Contains("click Attach", exception.Message);
    }

    [Fact]
    public async Task A_writing_script_under_auto_without_an_attachment_is_refused_before_any_save()
    {
        var exception = await Assert.ThrowsAsync<BridgeRequestException>(() => Execute("return sapModel.FrameObj.SetSection(\"F1\", \"C40x40\");"));

        Assert.Equal(BridgeErrorCode.NoActiveDocument, exception.Code);
        Assert.False(Directory.Exists(Path.Combine(_temp, "snapshots")));
    }

    [Fact]
    public async Task A_path_the_policy_cannot_read_is_rejected_with_a_path_diagnostic_whatever_the_opt_ins()
    {
        _executor.DestructiveOperationsEnabled = true;
        var result = await Execute("var p = args.Str(\"k\"); return sapModel.File.Save(p);");

        Assert.True(result.IsError);
        Assert.Contains("file path", result.Message);
        Assert.Contains(result.Diagnostics, d => d.Id == EtabsTierAnalyzer.PathDiagnosticId);
    }

    [Fact]
    public async Task Guard_and_compile_rejections_come_first()
    {
        var guarded = await Execute("HPEtabs.McpBridge.BridgeEntry.Dispose(); return 1;", TransactionModes.None);
        Assert.True(guarded.IsError);
        Assert.Contains(guarded.Diagnostics, d => d.Id == "GUARD");

        var host = await Execute("var c = HPRebar.McpBridge.Core.Host.McpBridgeHost.Current; return 1;", TransactionModes.None);
        Assert.Contains(host.Diagnostics, d => d.Id == "GUARD");

        // A DLL or a file from disk would run outside the tier table: the engine guard refuses the directives themselves.
        var reference = await Execute("#r \"C:\\x\\evil.dll\"\nreturn 1;", TransactionModes.None);
        Assert.Contains(reference.Diagnostics, d => d.Id == "GUARD" && d.Message.Contains("#r"));
        var load = await Execute("#load \"C:\\x\\part.csx\"\nreturn 1;", TransactionModes.None);
        Assert.Contains(load.Diagnostics, d => d.Id == "GUARD" && d.Message.Contains("#load"));

        var broken = await Execute("return sapModel.NoSuchMember();", TransactionModes.None);
        Assert.True(broken.IsError);
        Assert.Contains("does not compile", broken.Message);
    }

    [Fact]
    public void Analyze_flags_destructive_members_and_a_none_declaration_on_a_writing_script()
    {
        var destructive = _executor.Analyze(new AnalyzeRequest("return sapModel.SetModelIsLocked(false);", TransactionModes.Auto));
        Assert.Contains(destructive.GuardViolations, d => d.Id == "DESTRUCTIVE" && d.Message.Contains("cSapModel.SetModelIsLocked"));

        var declaredNone = _executor.Analyze(new AnalyzeRequest("return sapModel.FrameObj.SetSection(\"F1\", \"C40x40\");", TransactionModes.None));
        Assert.Contains(declaredNone.GuardViolations, d => d.Id == "PREVIEW" && d.Message.Contains("declared transaction: none"));

        var fine = _executor.Analyze(new AnalyzeRequest("return sapModel.FrameObj.SetSection(\"F1\", \"C40x40\");", TransactionModes.Auto));
        Assert.Empty(fine.GuardViolations);

        var readOnly = _executor.Analyze(new AnalyzeRequest("return sapModel.GetModelFilename();", TransactionModes.None));
        Assert.True(readOnly.Compiles);
        Assert.Empty(readOnly.GuardViolations);
    }

    [Fact]
    public async Task Context_answers_without_an_attachment_because_not_attached_counts_as_quiescent()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var context = await _executor.GetContextAsync(false, timeout.Token);

        Assert.NotNull(context.Etabs);
        Assert.False(context.Etabs!.IsAttached);
        Assert.Null(context.DocTitle);
        Assert.False(context.IsModifiable);
        Assert.Equal("etabs", context.Host);
    }

    [Fact]
    public void A_com_disconnect_drops_the_attachment_and_becomes_the_not_attached_refusal()
    {
        var attachment = new EtabsAttachment();

        Assert.True(EtabsAttachment.IsDisconnectError(new COMException("gone", unchecked((int)0x800706BA))));
        Assert.True(EtabsAttachment.IsDisconnectError(new COMException("gone", unchecked((int)0x80010108))));
        Assert.False(EtabsAttachment.IsDisconnectError(new COMException("ret", unchecked((int)0x80004005))));
        Assert.False(EtabsAttachment.IsDisconnectError(new InvalidOperationException()));

        var refusal = attachment.DetachIfGone(new COMException("gone", unchecked((int)0x800706BA)));
        Assert.NotNull(refusal);
        Assert.Equal(BridgeErrorCode.NoActiveDocument, refusal!.Code);
        Assert.False(attachment.Attached);
        Assert.Null(attachment.DetachIfGone(new InvalidOperationException()));
    }

    [Fact]
    public void The_guard_denies_few_oapi_member_names_and_none_of_the_everyday_ones()
    {
        // Member names the base guard would refuse in member position: recorded for the report, and pinned so a guard change that swallows the OAPI surface is noticed.
        var collisions = EtabsTierTable.Embedded.Entries
            .Select(e => e.Key[(e.Key.IndexOf('.') + 1)..])
            .Distinct(StringComparer.Ordinal)
            .Where(member => ScriptGuard.Check($"return x.{member}();", GuardProfile.Etabs).Count > 0)
            .OrderBy(m => m, StringComparer.Ordinal)
            .ToArray();

        Assert.True(collisions.Length <= 12, "guard collisions: " + string.Join(", ", collisions));
        foreach (var everyday in new[] { "GetNameList", "SetSection", "AddByCoord", "Save", "RunAnalysis", "GetModelFilename", "FrameForce", "SetCaseSelectedForOutput", "GetTableForDisplayArray" })
            Assert.DoesNotContain(everyday, collisions);
    }

    public void Dispose()
    {
        _executor.Dispose();
        try { Directory.Delete(_temp, true); } catch { /* temp */ }
    }
}
