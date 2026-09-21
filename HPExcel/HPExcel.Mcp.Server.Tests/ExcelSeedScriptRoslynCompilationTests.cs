using System.Reflection;
using System.Text.Json;
using HPExcel.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Server.Registry;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Xunit;

namespace HPExcel.Mcp.Server.Tests;

/// <summary>
///     Globals contract used for compiling Excel seed scripts.
///     Provides dynamic excel, workbook, and sheet handles along with execution context.
/// </summary>
public sealed class ExcelSeedCompilationGlobals
{
    public dynamic? excel { get; set; }
    public dynamic? workbook { get; set; }
    public dynamic? sheet { get; set; }
    public CancellationToken ct { get; set; }
    public Action<string> log { get; set; } = _ => { };
    public Action<int, int?, string?> progress { get; set; } = (_, _, _) => { };
    public ScriptArgs args { get; set; } = ScriptArgs.Empty;
}

/// <summary>
///     Verifies that all 12 embedded Excel seed scripts compile cleanly with Roslyn
///     against the Excel script environment contract (imports + globals) with 0 errors.
/// </summary>
public sealed class ExcelSeedScriptRoslynCompilationTests
{
    private static readonly Assembly ServerAssembly = typeof(ExcelHostProfile).Assembly;

    public static IEnumerable<object[]> AllSeedNames()
    {
        var seeds = SeedInstaller.LoadSeeds(ServerAssembly);
        return seeds.Select(s => new object[] { s.Name });
    }

    private static ScriptCompiler CreateTestCompiler()
    {
        var references = new List<Assembly>
        {
            typeof(object).Assembly,
            typeof(Enumerable).Assembly,
            typeof(List<>).Assembly,
            typeof(Dictionary<,>).Assembly,
            Assembly.Load("netstandard"),
            Assembly.Load("System.Runtime"),
            Assembly.Load("System.Collections"),
            typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo).Assembly,
            typeof(Microsoft.Office.Interop.Excel.Application).Assembly,
            typeof(Microsoft.Office.Core.MsoTriState).Assembly,
            typeof(ClosedXML.Excel.XLWorkbook).Assembly,
            typeof(ScriptArgs).Assembly,
            typeof(JsonElement).Assembly,
            typeof(ExcelSeedCompilationGlobals).Assembly
        };

        var imports = HostScriptContracts.ExcelImports
            .Concat(new[] { "System.IO" })
            .Distinct()
            .ToArray();

        return new ScriptCompiler(
            references,
            imports,
            typeof(ExcelSeedCompilationGlobals),
            cacheSize: 32);
    }

    [Theory]
    [MemberData(nameof(AllSeedNames))]
    public void SeedScript_CompilesWithZeroErrors(string seedName)
    {
        var seeds = SeedInstaller.LoadSeeds(ServerAssembly);
        var seed = seeds.Single(s => s.Name == seedName);

        var compiler = CreateTestCompiler();
        var outcome = compiler.GetOrCompile(seed.Code);

        Assert.True(outcome.Succeeded,
            $"Seed '{seed.Category}/{seed.Name}' failed Roslyn compilation:{Environment.NewLine}" +
            string.Join(Environment.NewLine, outcome.Diagnostics.Select(d => $"{d.Line}:{d.Column} {d.Id}: {d.Message}")));

        Assert.Empty(outcome.Diagnostics);
        Assert.NotNull(outcome.Script);
    }

    [Fact]
    public void All12SeedScripts_CompileSuccessfullyInBatch()
    {
        var seeds = SeedInstaller.LoadSeeds(ServerAssembly);
        Assert.Equal(12, seeds.Count);

        var compiler = CreateTestCompiler();
        var failureMessages = new List<string>();

        foreach (var seed in seeds)
        {
            var outcome = compiler.GetOrCompile(seed.Code);
            if (!outcome.Succeeded)
            {
                failureMessages.Add($"[{seed.Name}]: " + string.Join(", ", outcome.Diagnostics.Select(d => $"{d.Id} {d.Message}")));
            }
        }

        Assert.Empty(failureMessages);
        Assert.Equal(12, compiler.CompiledCount);
    }
}
