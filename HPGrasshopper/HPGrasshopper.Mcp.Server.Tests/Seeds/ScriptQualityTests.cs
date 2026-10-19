namespace HPGrasshopper.Mcp.Server.Tests.Seeds;

using System.Collections.Generic;
using System.Linq;
using HPGrasshopper.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Registry;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

public sealed class ScriptQualityTests
{
    private static IReadOnlyList<SeedInstaller.SeedContent> Seeds() =>
        SeedInstaller.LoadSeeds(typeof(GrasshopperHostProfile).Assembly);

    [Fact]
    public void SeedLibrary_IsEmbeddedWithExactlyTwelveSeeds()
    {
        var seeds = Seeds();
        Assert.Equal(12, seeds.Count);
    }

    [Fact]
    public void AllSeeds_PassScriptQualityWithZeroErrors()
    {
        var errors = Seeds()
            .SelectMany(seed => ScriptQuality.Find(seed.Code)
                .Where(finding => finding.Severity == ScriptQuality.Error)
                .Select(finding => $"{seed.Category}/{seed.Name} {finding.RuleId} {finding.Line}:{finding.Column} {finding.Message}"))
            .ToList();

        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    [Fact]
    public void AllSeeds_PassScriptQualityWithZeroWarnings()
    {
        var warnings = Seeds()
            .SelectMany(seed => ScriptQuality.Find(seed.Code)
                .Where(finding => finding.Severity == ScriptQuality.Warning)
                .Select(finding => $"{seed.Category}/{seed.Name} {finding.RuleId} {finding.Line}:{finding.Column} {finding.Message}"))
            .ToList();

        Assert.True(warnings.Count == 0, string.Join("\n", warnings));
    }
}
