using System.Reflection;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>Globals the tests hand to scripts — same shape idea as the bridge's, without Revit types.</summary>
public sealed class TestGlobals
{
    // ReSharper disable InconsistentNaming — script-facing names
    public string title = "Project1";
    public List<string> logs = [];
    public CancellationToken ct = CancellationToken.None;
    // ReSharper restore InconsistentNaming
}

public sealed class ScriptCompilerTests
{
    private static ScriptCompiler NewCompiler(int cacheSize = 10) => new ScriptCompiler(
        [typeof(object).Assembly, typeof(Enumerable).Assembly, typeof(List<>).Assembly, Assembly.Load("System.Runtime"), Assembly.Load("System.Collections")],
        ["System", "System.Linq", "System.Collections.Generic"],
        typeof(TestGlobals),
        cacheSize);

    [Fact]
    public async Task Compiles_runs_with_globals_and_returns_value()
    {
        var compiler = NewCompiler();

        var outcome = compiler.GetOrCompile("logs.Add(\"hi\"); return title.ToUpper() + \" \" + Enumerable.Range(1, 3).Sum();");

        Assert.True(outcome.Succeeded);
        var globals = new TestGlobals();
        var state = await compiler.GetOrCompile("logs.Add(\"hi\"); return title.ToUpper() + \" \" + Enumerable.Range(1, 3).Sum();").Script!.RunAsync(globals);
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
    public void Cache_evicts_least_recently_used_beyond_capacity()
    {
        var compiler = NewCompiler(cacheSize: 2);

        compiler.GetOrCompile("return 1;");
        compiler.GetOrCompile("return 2;");
        compiler.GetOrCompile("return 1;"); // touch 1 so 2 is the oldest
        compiler.GetOrCompile("return 3;"); // evicts 2

        Assert.True(compiler.GetOrCompile("return 1;").CacheHit);
        Assert.False(compiler.GetOrCompile("return 2;").CacheHit);
        Assert.Equal(2, compiler.CachedCount);
    }

    [Fact]
    public void Compile_errors_come_back_as_line_numbered_diagnostics()
    {
        var compiler = NewCompiler();

        var outcome = compiler.GetOrCompile("var x = 1;\nreturn x.NoSuchMember;");

        Assert.False(outcome.Succeeded);
        var diagnostic = Assert.Single(outcome.Diagnostics);
        Assert.Equal(2, diagnostic.Line);
        Assert.StartsWith("CS", diagnostic.Id);
        Assert.Contains("NoSuchMember", diagnostic.Message);
        Assert.Equal(0, compiler.CompiledCount);
    }

    [Fact]
    public async Task Cancellation_token_reaches_the_script()
    {
        var compiler = NewCompiler();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var globals = new TestGlobals { ct = cts.Token };

        var script = compiler.GetOrCompile("ct.ThrowIfCancellationRequested(); return 1;").Script!;

        await Assert.ThrowsAsync<OperationCanceledException>(() => script.RunAsync(globals));
    }
}

public sealed class TypeInspectorTests
{
    private static readonly TypeInspector Inspector = new TypeInspector([typeof(string).Assembly]);

    [Fact]
    public void Finds_type_by_simple_name_and_lists_members_with_kinds()
    {
        // System.Version lives in CoreLib (typeof(string).Assembly); Uri does not.
        var result = Inspector.Inspect(new InspectRequest("Version", null, 500));

        Assert.Equal("System.Version", result.FullName);
        Assert.Null(result.Message);
        Assert.Contains(result.Members, m => m.Kind == "property" && m.Signature.Contains("Int32 Major"));
        Assert.Contains(result.Members, m => m.Kind == "method" && m.Signature.StartsWith("static") && m.Signature.Contains("Parse("));
        Assert.DoesNotContain(result.Members, m => m.Signature.Contains(" ToString("));
    }

    [Fact]
    public void Filters_and_truncates_and_suggests_close_matches()
    {
        var filtered = Inspector.Inspect(new InspectRequest("System.Version", "parse", 2));
        Assert.True(filtered.Truncated);
        Assert.Equal(2, filtered.Members.Count);
        Assert.All(filtered.Members, m => Assert.Contains("arse", m.Signature));

        var missing = Inspector.Inspect(new InspectRequest("Versio"));
        Assert.Empty(missing.Members);
        Assert.Contains("System.Version", missing.Message);
    }
}

public sealed class AuditLoggerTests
{
    [Fact]
    public void Writes_one_json_line_per_entry_with_full_source()
    {
        var directory = Path.Combine(Path.GetTempPath(), "hprebar-audit-" + Guid.NewGuid().ToString("N"));

        try
        {
            using (var audit = new AuditLogger(directory))
            {
                audit.Write(new AuditEntry(DateTimeOffset.Now, "tester", "Project1", null, "abc", "return doc.Title;\nreturn 2;", "auto", true, "ok", 12, 1, 0, 0, null));
                audit.Write(new AuditEntry(DateTimeOffset.Now, "tester", null, null, "def", "x", "none", false, "error", 3, 0, 0, 0, "boom"));
            }

            var lines = Directory.GetFiles(directory, "audit-*.log").SelectMany(File.ReadAllLines).Where(l => l.Length > 0).ToArray();

            Assert.Equal(2, lines.Length);
            Assert.All(lines, l => Assert.StartsWith("{", l));
            Assert.Contains("\"source\":\"return doc.Title;\\nreturn 2;\"", lines[0]);
            Assert.Contains("\"outcome\":\"error\"", lines[1]);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
