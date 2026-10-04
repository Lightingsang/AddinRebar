using System.Security.Cryptography;
using System.Text;
using HPRebar.Mcp.Server.Registry;
using HPRebar.McpBridge.Core.Scripting;
using HPEtabs.Mcp.Server.Hosts;
using Xunit;

namespace HPEtabs.Mcp.Server.Tests;

/// <summary>
///     Every embedded seed passes the script-quality check the bridge runs on proposed tools: no blocking finding
///     unless the seed is in <see cref="Baseline"/> with the hash of its current code. Editing a baselined seed takes
///     it out of the baseline, so new and edited seeds must be clean. Warnings are review triggers, not asserted.
/// </summary>
public sealed class SeedQualityTests
{
    /// <summary>Blocking findings of seeds written before the check: seed, rule, first 12 hex of the code's sha256, reason.</summary>
    private static readonly (string Seed, string RuleId, string CodeHash, string Reason)[] Baseline =
    [];

    private static IReadOnlyList<SeedInstaller.SeedContent> Seeds() => SeedInstaller.LoadSeeds(typeof(EtabsHostProfile).Assembly);

    [Fact]
    public void SeedLibrary_IsEmbedded()
    {
        Assert.NotEmpty(Seeds());
    }

    [Fact]
    public void EverySeed_HasNoBlockingQualityFinding_OutsideTheBaseline()
    {
        var unexpected = Seeds()
            .SelectMany(seed => ScriptQuality.Find(seed.Code)
                .Where(finding => finding.Severity == ScriptQuality.Error && !IsBaselined(seed, finding.RuleId))
                .Select(finding => $"{Key(seed)} {finding.RuleId} {finding.Line}:{finding.Column} {finding.Message}"))
            .ToList();

        Assert.True(unexpected.Count == 0, string.Join("\n", unexpected));
    }

    [Fact]
    public void EveryBaselineEntry_StillMatchesItsSeed()
    {
        var stale = Baseline
            .Where(entry => !Seeds().Any(seed => IsBaselined(seed, entry.RuleId) && Key(seed) == entry.Seed
                && ScriptQuality.Find(seed.Code).Any(finding => finding.RuleId == entry.RuleId)))
            .Select(entry => $"{entry.Seed} {entry.RuleId}: the seed changed or is clean now — make it clean and delete the entry")
            .ToList();

        Assert.True(stale.Count == 0, string.Join("\n", stale));
    }

    private static bool IsBaselined(SeedInstaller.SeedContent seed, string ruleId) =>
        Baseline.Any(entry => entry.Seed == Key(seed) && entry.RuleId == ruleId && entry.CodeHash == Hash(seed.Code));

    private static string Key(SeedInstaller.SeedContent seed) => seed.Category + "/" + seed.Name;

    /// <summary>Hash of the code with LF line ends, so a CRLF checkout or a generator run does not change it.</summary>
    private static string Hash(string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code.Replace("\r\n", "\n"))))[..12].ToLowerInvariant();
}
