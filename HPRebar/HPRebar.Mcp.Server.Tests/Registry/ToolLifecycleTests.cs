using System.Text.Json;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.Mcp.Server.Services;
using HPRebar.Mcp.Server.Tools;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Server;
using Xunit;

namespace HPRebar.Mcp.Server.Tests.Registry;

public sealed class ToolValidatorTests
{
    private static ToolRecord Candidate(string code = "return args.Int(\"count\") * 2;", string schema = """{"type":"object","properties":{"count":{"type":"integer"}},"required":["count"]}""") =>
        new()
        {
            Name = "count_things",
            Description = "Counts things twice for testing purposes",
            Category = "Generic",
            Transaction = "auto",
            TimeoutSeconds = 30,
            InputSchema = JsonSerializer.Deserialize<JsonElement>(schema),
            Code = code,
            Examples = [Example("two", """{"count":2}"""), Example("three", """{"count":3}""")],
        };

    private static ToolExample Example(string title, string args) => new() { Title = title, Args = JsonSerializer.Deserialize<JsonElement>(args) };

    private static AnalyzeResult Analyse(string code)
    {
        var r = ScriptAnalyzer.Analyze(code);
        r.GuardViolations = ScriptGuard.Check(code);
        r.Compiles = r.GuardViolations.Count == 0;
        return r;
    }

    [Fact]
    public void Clean_candidate_passes()
    {
        var c = Candidate();
        var report = ToolValidator.Validate(c, Analyse(c.Code), [], false);
        Assert.True(report.IsValid, string.Join("; ", report.Errors));
        Assert.Empty(report.Warnings);
    }

    [Theory]
    [InlineData("Bad Name", "snake_case")]
    [InlineData("execute_revit_code", "reserved")]
    public void Name_rules(string name, string expectedFragment)
    {
        var c = Candidate();
        c.Name = name;
        var report = ToolValidator.Validate(c, Analyse(c.Code), [], false);
        Assert.Contains(report.Errors, e => e.Contains(expectedFragment));
    }

    [Fact]
    public void Args_must_match_schema_both_ways()
    {
        var c = Candidate(code: "return args.Int(\"count\") + args.Double(\"spacing\");");
        var report = ToolValidator.Validate(c, Analyse(c.Code), [], false);
        Assert.Contains(report.Errors, e => e.Contains("args.spacing"));

        var unused = Candidate(schema: """{"type":"object","properties":{"count":{"type":"integer"},"extra":{"type":"string"}}}""");
        var report2 = ToolValidator.Validate(unused, Analyse(unused.Code), [], false);
        Assert.True(report2.IsValid);
        Assert.Contains(report2.Warnings, w => w.Contains("'extra'"));
    }

    [Fact]
    public void Guard_compile_and_transaction_mismatch_are_errors()
    {
        var guarded = Candidate(code: "System.IO.File.Delete(\"x\"); return args.Int(\"count\");");
        Assert.Contains(ToolValidator.Validate(guarded, Analyse(guarded.Code), [], false).Errors, e => e.StartsWith("guard"));

        var manual = Candidate(code: "using var t = new Transaction(doc, \"x\"); t.Start(); t.Commit(); return args.Int(\"count\");");
        Assert.Contains(ToolValidator.Validate(manual, Analyse(manual.Code), [], false).Errors, e => e.Contains("transaction=\"manual\""));
        manual.Transaction = "manual";
        Assert.True(ToolValidator.Validate(manual, Analyse(manual.Code), [], false).IsValid);
    }

    [Fact]
    public void Hard_coded_values_and_thin_examples_are_warnings()
    {
        var c = Candidate(code: "double spacing = 8000; var name = \"C_300x300\"; return args.Int(\"count\") * spacing;");
        c.Examples = [Example("only", """{"count":1}""")];
        var report = ToolValidator.Validate(c, Analyse(c.Code), [], false);
        Assert.True(report.IsValid);
        Assert.Contains(report.Warnings, w => w.Contains("8000") && w.Contains("spacing"));
        Assert.Contains(report.Warnings, w => w.Contains("C_300x300"));
        Assert.Contains(report.Warnings, w => w.Contains("two or more examples"));
    }

    [Fact]
    public void Examples_and_schema_shape_are_checked()
    {
        var c = Candidate();
        c.Examples = [Example("missing", "{}"), Example("stray", """{"count":1,"nope":2}""")];
        var report = ToolValidator.Validate(c, Analyse(c.Code), [], false);
        Assert.Contains(report.Errors, e => e.Contains("missing required 'count'"));
        Assert.Contains(report.Errors, e => e.Contains("args.nope"));

        var badSchema = Candidate(schema: """{"type":"object","properties":{"count":{"type":"integer"},"dryRun":{"type":"boolean"},"x":{"type":"date"}}}""");
        var report2 = ToolValidator.Validate(badSchema, Analyse(badSchema.Code), [], false);
        Assert.Contains(report2.Errors, e => e.Contains("dryRun"));
        Assert.Contains(report2.Errors, e => e.Contains("x.type"));
    }

    [Fact]
    public void Existing_name_needs_new_version()
    {
        var c = Candidate();
        var existing = RegistryFixture.NewRecord("count_things");
        Assert.Contains(ToolValidator.Validate(c, Analyse(c.Code), [existing], false).Errors, e => e.Contains("newVersion=true"));
        Assert.True(ToolValidator.Validate(c, Analyse(c.Code), [existing], true).IsValid);
    }

    [Fact]
    public void Without_analysis_only_warns()
    {
        var c = Candidate();
        var report = ToolValidator.Validate(c, null, [], false);
        Assert.True(report.IsValid);
        Assert.Contains(report.Warnings, w => w.Contains("bridge offline"));
    }
}

public sealed class ToolLifecycleTests
{
    private const string Code = """
        double spacing = args.Double("spacing", 150);
        int count = args.Int("count", 4);
        var ids = new List<long>();
        for (var i = 0; i < count; i++) ids.Add(i);
        return new { count, spacing, ids };
        """;

    private static readonly JsonElement Schema = JsonSerializer.Deserialize<JsonElement>("""
        {"type":"object","properties":{"spacing":{"type":"number","default":150,"description":"mm"},"count":{"type":"integer","default":4}}}
        """);

    private static ProposeInput Input(string name = "count_with_spacing", bool newVersion = false, string code = Code, long? sourceRunId = null) => new(
        name, "Count with spacing", "Counts items at a given spacing for lifecycle tests", "Structure", ["test", "spacing"], Schema, code,
        [new ToolExample { Title = "defaults", Args = JsonSerializer.Deserialize<JsonElement>("{}") }, new ToolExample { Title = "six", Args = JsonSerializer.Deserialize<JsonElement>("""{"count":6,"spacing":200}""") }],
        "auto", 30, sourceRunId, newVersion);

    private static ToolLifecycleService Lifecycle(RegistryFixture f) => new(f.Manager, f.Bridge, NullLogger<ToolLifecycleService>.Instance);

    [Fact]
    public async Task Manual_policy_full_loop_propose_test_publish_approve()
    {
        await using var f = new RegistryFixture();
        var lifecycle = Lifecycle(f);
        var registrar = new DynamicToolRegistrar(f.Manager, new ResultFormatter(), Microsoft.Extensions.Options.Options.Create(new McpServerOptions()), NullLogger<DynamicToolRegistrar>.Instance);
        f.Manager.Changed += () => registrar.Sync();

        var runId = f.Manager.RecordAdhoc(Code, null, false, new ExecuteResult { DurationMs = 5 });
        var proposed = await lifecycle.ProposeAsync(Input(sourceRunId: runId), TestContext.Current.CancellationToken);
        Assert.True(proposed.Accepted, string.Join("; ", proposed.Report.Errors));
        Assert.Equal(ToolStatus.Draft, proposed.Record!.Status);
        Assert.Equal(1, proposed.Record.Version);
        Assert.Equal(runId, proposed.Record.CreatedFromRunId);
        Assert.True(File.Exists(Path.Combine(proposed.Record.Folder!, "tool.json")));
        Assert.Empty(registrar.RegisteredNames);

        f.Executor.ExecuteHandler = request => new ExecuteResult { Value = request.Args, DurationMs = 3, RolledBack = request.DryRun };
        var tested = await lifecycle.TestAsync("count_with_spacing", null, false, TestContext.Current.CancellationToken);
        Assert.Equal(2, tested.Passed);
        Assert.Equal("tested", tested.Status);
        Assert.All(tested.Cases, c => Assert.True(c.DryRun));
        Assert.All(f.Manager.Get("count_with_spacing")!.Record.Examples, e => Assert.NotNull(e.VerifiedRunId));
        Assert.True(f.Executor.LastExecuteRequest!.DryRun);

        var published = lifecycle.Publish("count_with_spacing");
        Assert.Equal("pending_approval", published.Status);
        Assert.True(File.Exists(published.ReviewFile));
        var review = File.ReadAllText(published.ReviewFile!);
        Assert.Contains("registry approve count_with_spacing", review);
        Assert.Contains("PASS dryRun", review);
        Assert.Empty(registrar.RegisteredNames);

        await Assert.ThrowsAsync<ToolNotRunnableException>(() => f.Manager.RunAsync("count_with_spacing", null, false, false, RunRecord.KindTool, TestContext.Current.CancellationToken));

        var approved = lifecycle.Approve("count_with_spacing", "tester");
        Assert.Equal(ToolStatus.Published, approved.Status);
        Assert.Equal("tester", approved.ApprovedBy);
        Assert.Equal(["count_with_spacing"], registrar.RegisteredNames);
        Assert.Contains("\"approvedBy\": \"tester\"", File.ReadAllText(Path.Combine(approved.Folder!, "tool.json")));

        var rejected = lifecycle.Reject("count_with_spacing", "tester", "needs a level parameter");
        Assert.Equal(ToolStatus.Draft, rejected.Status);
        Assert.Contains("needs a level parameter", rejected.Notes);
        Assert.Empty(registrar.RegisteredNames);
    }

    [Fact]
    public async Task Auto_policy_publishes_after_tests()
    {
        await using var f = new RegistryFixture(o => o.PublishPolicy = RegistryOptions.PolicyAuto);
        var lifecycle = Lifecycle(f);

        Assert.True((await lifecycle.ProposeAsync(Input(), TestContext.Current.CancellationToken)).Accepted);
        Assert.Throws<ToolNotRunnableException>(() => lifecycle.Publish("count_with_spacing")); // draft: test first
        await lifecycle.TestAsync("count_with_spacing", null, false, TestContext.Current.CancellationToken);

        var outcome = lifecycle.Publish("count_with_spacing");
        Assert.Equal("published", outcome.Status);
        Assert.Equal("policy:auto", f.Manager.Get("count_with_spacing")!.Record.ApprovedBy);
        Assert.Equal("published", lifecycle.Publish("count_with_spacing").Status);
    }

    [Fact]
    public async Task Invalid_proposal_saves_nothing()
    {
        await using var f = new RegistryFixture();
        var lifecycle = Lifecycle(f);

        var outcome = await lifecycle.ProposeAsync(Input(code: "return args.Int(\"count\") + args.Int(\"undeclared\") + COMPILE_ERROR;"), TestContext.Current.CancellationToken);

        Assert.False(outcome.Accepted);
        Assert.Contains(outcome.Report.Errors, e => e.Contains("args.undeclared"));
        Assert.Contains(outcome.Report.Errors, e => e.StartsWith("compile"));
        Assert.Empty(f.Manager.Tools);
        Assert.False(f.Store.Exists("count_with_spacing"));
    }

    [Fact]
    public async Task New_version_keeps_history_and_failed_tests_keep_draft()
    {
        await using var f = new RegistryFixture();
        var lifecycle = Lifecycle(f);
        Assert.True((await lifecycle.ProposeAsync(Input(), TestContext.Current.CancellationToken)).Accepted);

        var again = await lifecycle.ProposeAsync(Input(), TestContext.Current.CancellationToken);
        Assert.False(again.Accepted);
        Assert.Contains(again.Report.Errors, e => e.Contains("newVersion=true"));

        var v2 = await lifecycle.ProposeAsync(Input(newVersion: true, code: Code + "\n// v2"), TestContext.Current.CancellationToken);
        Assert.True(v2.Accepted);
        Assert.Equal(2, v2.Record!.Version);
        Assert.Contains("// v2", f.Manager.Get("count_with_spacing")!.Record.Code);

        f.Executor.ExecuteHandler = _ => ExecuteResult.Failure("ArgumentException: bad spacing");
        var tested = await lifecycle.TestAsync("count_with_spacing", null, false, TestContext.Current.CancellationToken);
        Assert.Equal(2, tested.Failed);
        Assert.Equal("draft", tested.Status);
        Assert.Contains("Fix the code", tested.Next);
        Assert.Throws<ToolNotRunnableException>(() => lifecycle.Publish("count_with_spacing"));
    }

    [Fact]
    public async Task Manage_deprecate_and_restore()
    {
        await using var f = new RegistryFixture();
        var lifecycle = Lifecycle(f);
        f.Store.Write(RegistryFixture.NewRecord("old_tool"));
        await f.Manager.LoadAllAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ToolStatus.Deprecated, lifecycle.Manage("old_tool", "deprecate", "superseded", "tester").Status);
        await Assert.ThrowsAsync<ToolNotRunnableException>(() => f.Manager.RunAsync("old_tool", null, false, true, RunRecord.KindTool, TestContext.Current.CancellationToken));
        Assert.Equal(ToolStatus.Draft, lifecycle.Manage("old_tool", "restore", null, "tester").Status);
        Assert.Throws<ToolNotRunnableException>(() => lifecycle.Manage("old_tool", "restore", null, "tester"));
        Assert.Throws<ArgumentException>(() => lifecycle.Manage("old_tool", "explode", null, "tester"));
    }

    [Fact]
    public async Task Cli_lists_approves_and_rejects()
    {
        await using var f = new RegistryFixture();
        var lifecycle = Lifecycle(f);
        Assert.True((await lifecycle.ProposeAsync(Input(), TestContext.Current.CancellationToken)).Accepted);
        await lifecycle.TestAsync("count_with_spacing", null, false, TestContext.Current.CancellationToken);
        lifecycle.Publish("count_with_spacing");

        var services = new ServiceCollection()
            .AddSingleton(f.Store).AddSingleton(f.Db).AddSingleton(f.Manager).AddSingleton(lifecycle)
            .BuildServiceProvider();

        var output = new StringWriter();
        Assert.Equal(0, await RegistryCli.RunAsync(services, ["pending"], output));
        Assert.Contains("count_with_spacing v1 — review:", output.ToString());

        output = new StringWriter();
        Assert.Equal(0, await RegistryCli.RunAsync(services, ["approve", "count_with_spacing", "--by", "cli-user"], output));
        Assert.Contains("published by cli-user", output.ToString());
        Assert.Equal(ToolStatus.Published, f.Store.ReadAll().Single().Status);

        output = new StringWriter();
        Assert.Equal(0, await RegistryCli.RunAsync(services, ["list", "--status", "published"], output));
        Assert.Contains("count_with_spacing", output.ToString());

        output = new StringWriter();
        Assert.Equal(1, await RegistryCli.RunAsync(services, ["reject", "count_with_spacing"], output));
        Assert.Contains("--reason is required", output.ToString());
        Assert.Equal(0, await RegistryCli.RunAsync(services, ["reject", "count_with_spacing", "--reason", "no"], new StringWriter()));
        Assert.Equal(ToolStatus.Draft, f.Store.ReadAll().Single().Status);

        Assert.Equal(1, await RegistryCli.RunAsync(services, ["show", "nope"], new StringWriter()));
        Assert.Equal(2, await RegistryCli.RunAsync(services, ["frobnicate"], new StringWriter()));
    }
}

public sealed class ReusableHintTests
{
    [Fact]
    public void Heuristic_flags_loops_changes_and_size_only_on_success()
    {
        var ok = new ExecuteResult();
        Assert.False(ExecuteCodeService.LooksReusable("return 1;", ok));
        Assert.True(ExecuteCodeService.LooksReusable("foreach (var x in y) { }\nreturn 1;", ok));
        Assert.True(ExecuteCodeService.LooksReusable("return 1;", new ExecuteResult { Changed = new ChangedCounts(3, 0, 0) }));
        Assert.True(ExecuteCodeService.LooksReusable(string.Join("\n", Enumerable.Repeat("var a = 1;", 12)) + "\nreturn a;", ok));
        Assert.False(ExecuteCodeService.LooksReusable("foreach (var x in y) { }", ExecuteResult.Failure("boom")));
    }
}
