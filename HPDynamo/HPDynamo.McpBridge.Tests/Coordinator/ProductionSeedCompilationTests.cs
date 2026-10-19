namespace HPDynamo.McpBridge.Tests.Coordinator;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using HPDynamo.McpBridge.Coordinator;
using HPDynamo.McpBridge.Support;
using HPRebar.Mcp.Contracts;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

/// <summary>
///     Verifies Milestone B requirement: All 13 embedded seeds compile cleanly using
///     the production ScriptCompiler and DynamoCompilerReferences with 0 errors.
/// </summary>
public sealed class ProductionSeedCompilationTests
{
    private static readonly string[] RecoveredQuarantinedTools =
    [
        "batch_edit_graph",
        "search_nodes_library",
        "get_active_graph_info"
    ];

    [Theory]
    [MemberData(nameof(AllSeeds))]
    public void Seed_CompilesCleanly_UsingProductionScriptCompiler(string name, string code)
    {
        var refs = DynamoCompilerReferences.All;
        var imports = DynamoCompilerReferences.ImportsFor(refs);
        var compiler = new ScriptCompiler(refs, imports, typeof(DynamoScriptGlobals), 64);

        var outcome = compiler.GetOrCompile(code);

        Assert.True(
            outcome.Succeeded && outcome.Diagnostics.Count == 0,
            $"Seed '{name}' failed to compile using production ScriptCompiler:{Environment.NewLine}" +
            string.Join(Environment.NewLine, outcome.Diagnostics.Select(d => $"{d.Line}:{d.Column} {d.Id}: {d.Message}")));

        Assert.NotNull(outcome.Script);
        Assert.Empty(outcome.Diagnostics);
    }

    [Fact]
    public void QuarantinedSeeds_RecoveredAndCompileCleanly()
    {
        var refs = DynamoCompilerReferences.All;
        var imports = DynamoCompilerReferences.ImportsFor(refs);
        var compiler = new ScriptCompiler(refs, imports, typeof(DynamoScriptGlobals), 64);

        var seeds = LoadSeedDictionary();
        foreach (var tool in RecoveredQuarantinedTools)
        {
            Assert.True(seeds.TryGetValue(tool, out var code), $"Seed '{tool}' not found.");
            var outcome = compiler.GetOrCompile(code!);

            Assert.True(
                outcome.Succeeded && outcome.Diagnostics.Count == 0,
                $"Formerly quarantined tool '{tool}' failed to compile: " +
                string.Join("; ", outcome.Diagnostics.Select(d => $"{d.Id}: {d.Message}")));

            Assert.NotNull(outcome.Script);
        }
    }

    public static IEnumerable<object[]> AllSeeds() =>
        LoadSeedDictionary().Select(kv => new object[] { kv.Key, kv.Value });

    private static Dictionary<string, string> LoadSeedDictionary()
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var baseDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "HPDynamo.Mcp.Server", "Registry", "SeedLibrary"));
        if (!Directory.Exists(baseDir))
        {
            // Fallback to source tree search
            baseDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "HPDynamo", "HPDynamo.Mcp.Server", "Registry", "SeedLibrary"));
        }

        foreach (var codeFile in Directory.GetFiles(baseDir, "code.cs", SearchOption.AllDirectories))
        {
            var dirName = Path.GetFileName(Path.GetDirectoryName(codeFile)!);
            dict[dirName] = File.ReadAllText(codeFile);
        }

        return dict;
    }
}
