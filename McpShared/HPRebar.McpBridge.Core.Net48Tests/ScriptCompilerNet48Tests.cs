using System.Text.Json;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>Globals the tests hand to scripts — the shape a bridge uses, without any host type.</summary>
public sealed class Net48TestGlobals
{
    // ReSharper disable InconsistentNaming — script-facing names
    public string title = "Project1";
    public List<string> logs = [];
    public ScriptArgs args = ScriptArgs.Empty;
    public CancellationToken ct = CancellationToken.None;
    // ReSharper restore InconsistentNaming
}

/// <summary>
///     Roslyn scripting on the .NET Framework runtime: the compiler, its cache, the guard and the analyzer
///     behave as they do on net8. References are taken from live types (mscorlib, System.Core, System) —
///     partial-name <c>Assembly.Load("System.Runtime")</c>, which the net10 tests use, does not resolve
///     on .NET Framework.
/// </summary>
public sealed class ScriptCompilerNet48Tests
{
    private static ScriptCompiler NewCompiler(int cacheSize = 10) => new ScriptCompiler(
        [typeof(object).Assembly, typeof(Enumerable).Assembly, typeof(List<>).Assembly, typeof(ScriptArgs).Assembly, typeof(JsonElement).Assembly],
        ["System", "System.Linq", "System.Collections.Generic", "HPRebar.McpBridge.Core.Scripting"],
        typeof(Net48TestGlobals),
        cacheSize);

    [Fact]
    public void Runs_on_the_desktop_framework()
    {
        Assert.StartsWith(".NET Framework", System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
        Assert.Equal(4, Environment.Version.Major);
    }

    [Fact]
    public async Task Compiles_runs_with_globals_and_returns_value()
    {
        var compiler = NewCompiler();
        const string code = "logs.Add(\"hi\"); return title.ToUpper() + \" \" + Enumerable.Range(1, 3).Sum();";

        var outcome = compiler.GetOrCompile(code);

        Assert.True(outcome.Succeeded, string.Join("; ", outcome.Diagnostics.Select(d => d.Message)));
        var globals = new Net48TestGlobals();
        var state = await outcome.Script!.RunAsync(globals, TestContext.Current.CancellationToken);
        Assert.Equal("PROJECT1 6", state.ReturnValue);
        Assert.Equal(["hi"], globals.logs);
    }

    [Fact]
    public void Second_compile_of_same_source_is_a_cache_hit()
    {
        var compiler = NewCompiler();

        var first = compiler.GetOrCompile("return 1;");
        var second = compiler.GetOrCompile("return 1;");
        var third = compiler.GetOrCompile("return 2;");

        Assert.False(first.CacheHit);
        Assert.True(second.CacheHit);
        Assert.Same(first.Script, second.Script);
        Assert.False(third.CacheHit);
        Assert.Equal(2, compiler.CompiledCount);
        Assert.Equal(2, compiler.CachedCount);
    }

    [Fact]
    public void Compile_error_is_reported_with_line_and_id()
    {
        var compiler = NewCompiler();

        var outcome = compiler.GetOrCompile("var x = 1;\nreturn nope + x;");

        Assert.False(outcome.Succeeded);
        var diagnostic = Assert.Single(outcome.Diagnostics);
        Assert.Equal(2, diagnostic.Line);
        Assert.Equal("CS0103", diagnostic.Id);
        Assert.Equal(0, compiler.CompiledCount);
    }

    [Fact]
    public async Task Script_reads_args_through_the_shared_ScriptArgs()
    {
        var compiler = NewCompiler();
        var element = JsonSerializer.SerializeToElement(new { count = 3, name = "beam", flag = "true" });

        var outcome = compiler.GetOrCompile("return args.Int(\"count\") + \":\" + args.Str(\"Name\") + \":\" + args.Bool(\"flag\");");

        Assert.True(outcome.Succeeded, string.Join("; ", outcome.Diagnostics.Select(d => d.Message)));
        var state = await outcome.Script!.RunAsync(new Net48TestGlobals { args = new ScriptArgs(element) }, TestContext.Current.CancellationToken);
        Assert.Equal("3:beam:True", state.ReturnValue);
    }

    [Fact]
    public void Hash_is_hex_sha256_of_the_source()
    {
        var hash = ScriptCompiler.Hash("return 1;");

        Assert.Equal(64, hash.Length);
        Assert.True(hash.All(Uri.IsHexDigit));
        Assert.Equal(hash, ScriptCompiler.Hash("return 1;"));
    }

    [Fact]
    public void Guard_with_the_Navis_profile_rejects_transactions_sql_and_expressions()
    {
        var violations = ScriptGuard.Check(
            "var tx = doc.BeginTransaction(\"x\");\n" +
            "var cmd = new NavisworksCommand(\"select 1\", doc.Database.ToNavisworksConnection());\n" +
            "var call = Expression.Call(Expression.Constant(doc), \"SaveFile\", null, Expression.Constant(\"a.nwd\"));\n" +
            "doc.Undo();\n" +
            "return 1;",
            GuardProfile.Navis);

        var messages = string.Join("\n", violations.Select(v => v.Message));
        Assert.Contains(".BeginTransaction is not allowed", messages);
        Assert.Contains("NavisworksCommand is not allowed", messages);
        Assert.Contains(".Database is not allowed", messages);
        Assert.Contains("Expression is not allowed", messages);
        Assert.Contains(".Undo is not allowed", messages);
        Assert.All(violations, v => Assert.Contains("Navisworks", v.Message));
    }

    [Fact]
    public void Analyzer_with_the_Navis_profile_sees_both_ways_of_opening_a_transaction()
    {
        var compiler = NewCompiler();

        var viaMethod = ScriptAnalyzer.Run(compiler, "var t = doc.BeginTransaction(\"a\"); return 1;", GuardProfile.Navis, AnalyzerProfile.Navis);
        var viaCtor = ScriptAnalyzer.Run(compiler, "using (var t = new Transaction(doc, \"a\")) { } return 1;", GuardProfile.Navis, AnalyzerProfile.Navis);
        var readOnly = ScriptAnalyzer.Run(compiler, "return title.Length;", GuardProfile.Navis, AnalyzerProfile.Navis);

        Assert.True(viaMethod.UsesTransaction);
        Assert.True(viaCtor.UsesTransaction);
        Assert.False(readOnly.UsesTransaction);
        Assert.NotEmpty(viaMethod.GuardViolations); // and the guard still refuses it
        Assert.Empty(readOnly.GuardViolations);
    }

    [Fact]
    public async Task Default_monotonic_clock_ages_a_request_on_the_desktop_framework()
    {
        // No clock injected: exercises the net48 Stopwatch branch that MainThreadQueueTests never reach.
        var queue = new MainThreadQueue(() => false, "Navisworks", TimeSpan.FromMilliseconds(50));
        var task = queue.RunAsync(new MainThreadWorkItem("late", _ => 1, CancellationToken.None));

        queue.OnTick(); // too young to refuse yet, host busy
        Assert.False(task.IsCompleted);

        await Task.Delay(150, TestContext.Current.CancellationToken);
        queue.OnTick();

        var error = await Assert.ThrowsAsync<BridgeRequestException>(() => task);
        Assert.Contains("Navisworks", error.Message);
    }

    [Fact]
    public void Guard_and_analyzer_with_the_Robot_profile_work_on_desktop_framework()
    {
        var violations = ScriptGuard.Check(
            "robot.Quit();\n" +
            "MessageBox.Show(\"hi\");\n" +
            "return 1;",
            GuardProfile.Robot);

        Assert.Contains(violations, v => v.Message.Contains("Quit", StringComparison.Ordinal));
        Assert.Contains(violations, v => v.Message.Contains("MessageBox", StringComparison.Ordinal));
        Assert.All(violations, v => Assert.True(v.Message.IndexOf("Robot", StringComparison.OrdinalIgnoreCase) >= 0 || v.Message.Contains("bridge owns", StringComparison.Ordinal)));

        var compiler = NewCompiler();
        var analyzed = ScriptAnalyzer.Run(compiler, "return 1;", GuardProfile.Robot, AnalyzerProfile.Robot);
        Assert.False(analyzed.UsesTransaction);
        Assert.Empty(analyzed.GuardViolations);
    }

    [Fact]
    public void Guard_and_analyzer_with_the_Tekla_profile_work_on_desktop_framework()
    {
        var violations = ScriptGuard.Check(
            "model.CommitChanges();\n" +
            "MessageBox.Show(\"hi\");\n" +
            "var picker = new Picker();\n" +
            "return 1;",
            GuardProfile.Tekla);

        Assert.Contains(violations, v => v.Message.Contains("CommitChanges", StringComparison.Ordinal));
        Assert.Contains(violations, v => v.Message.Contains("MessageBox", StringComparison.Ordinal));
        Assert.Contains(violations, v => v.Message.Contains("Picker", StringComparison.Ordinal));
        Assert.All(violations, v => Assert.True(v.Message.IndexOf("Tekla", StringComparison.OrdinalIgnoreCase) >= 0 || v.Message.Contains("bridge owns", StringComparison.Ordinal)));

        var compiler = NewCompiler();
        var analyzed = ScriptAnalyzer.Run(compiler, "return 1;", GuardProfile.Tekla, AnalyzerProfile.Tekla);
        Assert.False(analyzed.UsesTransaction);
        Assert.Empty(analyzed.GuardViolations);

        var analyzedCommit = ScriptAnalyzer.Run(compiler, "model.CommitChanges(); return 1;", GuardProfile.Tekla, AnalyzerProfile.Tekla);
        Assert.True(analyzedCommit.UsesTransaction);
        Assert.NotEmpty(analyzedCommit.GuardViolations);
    }

    [Fact]
    public void Quality_check_runs_on_the_desktop_framework()
    {
        var result = ScriptAnalyzer.Run(NewCompiler(), "try { return 1; }\ncatch { }\nvar data = 2;\nreturn data;", GuardProfile.Navis, AnalyzerProfile.Navis);

        Assert.True(result.QualityAnalysed);
        Assert.Equal(new[] { "Q-B2", "Q-W3" }, result.QualityFindings.Select(f => f.RuleId).ToArray());
    }

    [Fact]
    public void Quality_fields_cross_the_pipe_and_are_absent_from_an_old_bridge()
    {
        var sent = ScriptAnalyzer.Analyze("// var x = 1;\nreturn 0;");

        var received = HPRebar.Mcp.Contracts.JsonRpc.BridgeJson.Deserialize<HPRebar.Mcp.Contracts.Messages.AnalyzeResult>(HPRebar.Mcp.Contracts.JsonRpc.BridgeJson.Serialize(sent))!;
        var fromOldBridge = HPRebar.Mcp.Contracts.JsonRpc.BridgeJson.Deserialize<HPRebar.Mcp.Contracts.Messages.AnalyzeResult>("{\"compiles\":true,\"lineCount\":2}")!;

        Assert.True(received.QualityAnalysed);
        var finding = Assert.Single(received.QualityFindings);
        Assert.Equal(("Q-B1", "error", 1, 1), (finding.RuleId, finding.Severity, finding.Line, finding.Column));
        Assert.False(fromOldBridge.QualityAnalysed);
        Assert.Empty(fromOldBridge.QualityFindings);
    }
}
