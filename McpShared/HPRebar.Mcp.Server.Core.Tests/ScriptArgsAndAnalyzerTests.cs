using System.Reflection;
using System.Text.Json;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>Globals with an `args` slot, mirroring the bridge's ScriptGlobals without Revit types.</summary>
public sealed class ArgsGlobals
{
    // ReSharper disable InconsistentNaming — script-facing names
    public ScriptArgs args = ScriptArgs.Empty;
    // ReSharper restore InconsistentNaming
}

public sealed class ScriptArgsTests
{
    private static ScriptArgs Parse(string json) => new ScriptArgs(JsonSerializer.Deserialize<JsonElement>(json));

    [Fact]
    public void Scalars_read_with_fallbacks_and_case_insensitive_keys()
    {
        var args = Parse("""{"Spacing": 150, "name": "C_300x300", "count": "4", "flag": 1, "ratio": "0.25"}""");

        Assert.Equal(150, args.Double("spacing"));
        Assert.Equal("C_300x300", args.Str("NAME"));
        Assert.Equal(4, args.Int("count"));
        Assert.True(args.Bool("flag"));
        Assert.Equal(0.25, args.Double("ratio"));
        Assert.Equal(99, args.Int("missing", 99));
        Assert.Null(args.DoubleOrNull("missing"));
        Assert.False(args.Has("missing"));
        Assert.True(args.Has("spacing"));
        Assert.Equal(["Spacing", "name", "count", "flag", "ratio"], args.Keys);
    }

    [Fact]
    public void Nested_objects_and_lists_wrap_without_throwing()
    {
        var args = Parse("""{"p0": {"x": 1.5, "y": -2}, "items": [{"id": 7}, {"id": 8}], "names": ["a", "b"], "ids": [1, 2, 3]}""");

        Assert.Equal(1.5, args.Obj("p0").Double("x"));
        Assert.Equal(-2, args.Obj("p0").Double("y"));
        Assert.Equal(0, args.Obj("nope").Double("x"));
        Assert.Equal([7L, 8L], args.List("items").Select(i => i.Long("id")));
        Assert.Equal(["a", "b"], args.Strings("names"));
        Assert.Equal([1L, 2L, 3L], args.Longs("ids"));
        Assert.Empty(args.List("missing"));
    }

    [Fact]
    public void Require_names_the_missing_key()
    {
        var args = Parse("""{"a": ""}""");

        var ex = Assert.Throws<ArgumentException>(() => args.Require("a"));
        Assert.Contains("args.a", ex.Message);
        Assert.Contains("args.b", Assert.Throws<ArgumentException>(() => args.RequireDouble("b")).Message);
    }

    [Fact]
    public void Null_and_empty_are_empty()
    {
        Assert.True(ScriptArgs.Empty.IsEmpty);
        Assert.True(new ScriptArgs(null).IsEmpty);
        Assert.True(Parse("null").IsEmpty);
        Assert.True(Parse("{}").IsEmpty);
        Assert.False(Parse("""{"a":1}""").IsEmpty);
        Assert.Equal("{}", ScriptArgs.Empty.ToString());
    }

    [Fact]
    public async Task Script_reads_args_through_the_global()
    {
        var compiler = new ScriptCompiler(
            [typeof(object).Assembly, typeof(Enumerable).Assembly, typeof(List<>).Assembly, Assembly.Load("System.Runtime"), Assembly.Load("System.Collections"), typeof(ScriptArgs).Assembly, typeof(JsonElement).Assembly],
            ["System", "System.Linq", "System.Collections.Generic"],
            typeof(ArgsGlobals),
            4);

        var outcome = compiler.GetOrCompile("""return args.Double("spacing") * args.List("items").Count + (args.Has("x") ? 1 : 0);""");
        Assert.True(outcome.Succeeded, string.Join("; ", outcome.Diagnostics.Select(d => d.Message)));

        var globals = new ArgsGlobals { args = Parse("""{"spacing": 150, "items": [1, 2, 3]}""") };
        var state = await outcome.Script!.RunAsync(globals, TestContext.Current.CancellationToken);

        Assert.Equal(450d, state.ReturnValue);
    }
}

public sealed class ScriptAnalyzerTests
{
    private const string Sample = """
        double spacing = 150;
        var names = new List<string> { "A", "B" };
        int count = args.Int("count", 4);
        var label = args.Str("label");
        for (var i = 0; i < count; i++) log(names[0] + spacing);
        return count * 2;
        """;

    [Fact]
    public void Finds_literals_with_positions_and_bound_names()
    {
        var result = ScriptAnalyzer.Analyze(Sample);

        var spacing = Assert.Single(result.Literals, l => l.Value == "150");
        Assert.Equal(1, spacing.Line);
        Assert.Equal("number", spacing.Kind);
        Assert.Equal("spacing", spacing.BoundTo);
        Assert.Contains("double spacing = 150;", spacing.Context);

        Assert.Contains(result.Literals, l => l.Kind == "string" && l.Value == "A" && l.BoundTo is null);
        // args keys, loop bookkeeping (0, 2) and the default 4 inside args.Int are not candidates
        Assert.DoesNotContain(result.Literals, l => l.Value is "count" or "label" or "0" or "2");
        Assert.True(result.HasLoops);
        Assert.False(result.UsesTransaction);
        Assert.Equal(6, result.LineCount);
    }

    [Fact]
    public void Lists_args_keys_with_accessor()
    {
        var result = ScriptAnalyzer.Analyze(Sample);

        Assert.Equal([("count", "Int", 3), ("label", "Str", 4)], result.ArgKeys.Select(a => (a.Key, a.Accessor, a.Line)));
    }

    [Fact]
    public void Detects_own_transactions()
    {
        Assert.True(ScriptAnalyzer.Analyze("using var t = new Transaction(doc, \"x\"); t.Start(); t.Commit();").UsesTransaction);
        Assert.True(ScriptAnalyzer.Analyze("var g = new Autodesk.Revit.DB.TransactionGroup(doc, \"x\");").UsesTransaction);
    }

    [Fact]
    public void Run_adds_guard_and_compiler_verdict()
    {
        var compiler = new ScriptCompiler(
            [typeof(object).Assembly, typeof(Enumerable).Assembly, typeof(List<>).Assembly, Assembly.Load("System.Runtime"), Assembly.Load("System.Collections"), typeof(ScriptArgs).Assembly, typeof(JsonElement).Assembly],
            ["System", "System.Linq", "System.Collections.Generic"],
            typeof(ArgsGlobals),
            4);

        var ok = ScriptAnalyzer.Run(compiler, "return args.Int(\"n\", 3) + 150;");
        Assert.True(ok.Compiles);
        Assert.Empty(ok.GuardViolations);
        Assert.Single(ok.Literals, l => l.Value == "150");

        var guarded = ScriptAnalyzer.Run(compiler, "System.IO.File.WriteAllText(\"a\", \"b\"); return 1;");
        Assert.False(guarded.Compiles);
        Assert.NotEmpty(guarded.GuardViolations);
        Assert.Empty(guarded.Diagnostics); // compiler is not consulted after a guard hit

        var broken = ScriptAnalyzer.Run(compiler, "return undefinedThing + 1;");
        Assert.False(broken.Compiles);
        Assert.NotEmpty(broken.Diagnostics);
    }
}
