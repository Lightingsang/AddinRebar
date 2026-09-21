using System.Reflection;
using System.Text.Json;
using HPExcel.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

namespace HPExcel.Mcp.Server.Tests;

public sealed class SeedLibraryQualityVerificationTests
{
    private static readonly Assembly ServerAssembly = typeof(ExcelHostProfile).Assembly;

    [Fact]
    public void All_12_seeds_load_correctly_from_embedded_resources()
    {
        var seeds = SeedInstaller.LoadSeeds(ServerAssembly);
        Assert.Equal(12, seeds.Count);

        var categories = seeds.Select(s => s.Category).Distinct().OrderBy(c => c).ToArray();
        Assert.Equal(["Automation", "Calculation", "Chart", "Data", "Export", "Format", "Workbook"], categories);
    }

    [Fact]
    public void Seed_tools_all_pass_registry_validator_and_guard()
    {
        var seeds = SeedInstaller.LoadSeeds(ServerAssembly);
        var profile = ExcelHostProfile.Instance;

        foreach (var seed in seeds)
        {
            var record = RegistryJson.Deserialize<ToolRecord>(seed.ToolJson)!;
            record.Code = seed.Code;
            record.Examples = RegistryJson.Deserialize<List<ToolExample>>(seed.ExamplesJson)!;

            var report = ToolValidator.Validate(record, null, [], false, profile);
            Assert.True(report.IsValid, $"Seed '{seed.Category}/{seed.Name}' failed ToolValidator: {string.Join("; ", report.Errors)}");

            var guardViolations = ScriptGuard.Check(seed.Code, GuardProfile.Excel);
            Assert.True(guardViolations.Count == 0, $"Seed '{seed.Category}/{seed.Name}' failed ScriptGuard: {string.Join("; ", guardViolations.Select(g => $"{g.Line}:{g.Column} {g.Message}"))}");

            var analysis = ScriptAnalyzer.Analyze(seed.Code, AnalyzerProfile.Excel);
            var declaredArgs = record.InputSchema.GetProperty("properties").EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var readArgs = analysis.ArgKeys.Select(a => a.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var unread = declaredArgs.Except(readArgs).ToArray();
            Assert.True(unread.Length == 0, $"Seed '{seed.Category}/{seed.Name}' has unused args in schema: {string.Join(", ", unread)}");
        }
    }

    [Fact]
    public void Host_profile_has_correct_metadata()
    {
        var profile = ExcelHostProfile.Instance;
        Assert.Equal(PipeNaming.ExcelHost, profile.HostId);
        Assert.Equal("excel.", profile.MethodPrefix);
        Assert.Equal("execute_excel_code", profile.ExecuteToolName);
        Assert.Equal("get_excel_context", profile.ContextToolName);
        Assert.Equal(2026, profile.DefaultVersion);
        Assert.Equal(600, profile.MaxTimeoutSeconds);
        Assert.Contains("Data", profile.Categories);
        Assert.Contains("Workbook", profile.Categories);
        Assert.Contains("Format", profile.Categories);
        Assert.Contains("Chart", profile.Categories);
        Assert.Contains("Calculation", profile.Categories);
        Assert.Contains("Export", profile.Categories);
        Assert.Contains("Automation", profile.Categories);
    }
}
