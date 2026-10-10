using System.IO;
using HPNavis.McpBridge.Service;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

namespace HPNavis.McpBridge.Tests;

/// <summary>
///     Every seed tool of the Navisworks server compiled through the bridge's own script compiler — same
///     imports, same references, same globals type — on .NET Framework 4.8, so a seed that passes here compiles
///     inside Roamer.exe. Also the two host-side gates a seed meets before it runs: the Navisworks guard, and
///     the heavy pre-pass (only the one heavy seed may trip it, and only while heavy operations are off).
///     Seeds are read from the server's source tree; the server assembly itself is net10 and cannot be referenced.
/// </summary>
public sealed class SeedLibraryCompileTests
{
    private static readonly Lazy<ScriptCompiler> Compiler = new(() => BridgeEntry.CreateScriptCompiler(64));

    public static IEnumerable<object[]> Seeds() => SeedSources.All().Select(s => new object[] { s.Key });

    [Fact]
    public void All_twenty_seeds_are_present_in_the_source_tree()
    {
        var keys = SeedSources.All().Select(s => s.Key).ToArray();

        Assert.Equal(20, keys.Length);
        Assert.Contains("Clash/create_and_run_clash_test", keys);
        Assert.Contains("Search/find_items_by_property", keys);
        Assert.Contains("Report/summarize_by_category", keys);
        Assert.Contains("Coordination/bim_sync_clash_tests", keys);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Seed_compiles_through_the_bridge_compiler_on_net48(string key)
    {
        var seed = SeedSources.Get(key);

        var outcome = Compiler.Value.GetOrCompile(seed.Code);

        Assert.True(outcome.Succeeded, key + Environment.NewLine + string.Join(Environment.NewLine, outcome.Diagnostics.Select(d => $"{d.Line}:{d.Column} {d.Id} {d.Message}")));
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Seed_passes_the_navis_guard(string key)
    {
        var seed = SeedSources.Get(key);

        var violations = ScriptGuard.Check(seed.Code, GuardProfile.Navis);

        Assert.True(violations.Count == 0, key + ": " + string.Join("; ", violations.Select(v => $"{v.Line}:{v.Column} {v.Message}")));
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Only_the_heavy_seed_trips_the_heavy_gate_and_only_while_heavy_is_off(string key)
    {
        var seed = SeedSources.Get(key);
        var isHeavy = seed.Tags.Contains("heavy");

        var off = new NavisHeavyGate { Enabled = false }.Check(seed.Code, out var hasHeavyCallsOff);
        var on = new NavisHeavyGate { Enabled = true }.Check(seed.Code, out var hasHeavyCallsOn);

        Assert.Equal(isHeavy, hasHeavyCallsOff);
        Assert.Equal(isHeavy, hasHeavyCallsOn);
        Assert.Equal(isHeavy, off.Any(d => d.Id == NavisHeavyGate.DiagnosticId));
        Assert.Empty(on); // heavy allowed: no diagnostic, and no seed names a network or install path
    }

    [Fact]
    public void Only_the_two_clash_runs_are_heavy()
    {
        var heavy = SeedSources.All().Where(s => s.Tags.Contains("heavy")).Select(s => s.Key).ToArray();

        Assert.Equal(["Clash/create_and_run_clash_test", "Coordination/bim_run_canary_tests"], heavy.OrderBy(k => k, StringComparer.Ordinal).ToArray());
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Seeds_that_walk_descendants_bound_the_walk(string key)
    {
        // a whole-model walk without a bound is the one thing a review tool must never do on the main thread
        var seed = SeedSources.Get(key);
        if (!seed.Code.Contains("Descendants")) return;

        Assert.True(seed.Code.Contains("PruneBelowMatch") || seed.Code.Contains(".Take("), key + " walks Descendants without PruneBelowMatch or Take");
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Seeds_use_the_real_search_condition_api(string key)
    {
        var seed = SeedSources.Get(key);

        Assert.DoesNotContain(".Contains(\"", seed.Code.Replace("DisplayStringContains", "")); // SearchCondition has DisplayStringContains, not Contains
        Assert.DoesNotContain("EqualValue(\"", seed.Code); // EqualValue takes a VariantData
    }
}

/// <summary>The seed folders under the server's source tree, located relative to the test assembly.</summary>
internal static class SeedSources
{
    public sealed record Seed(string Category, string Name, string Code, string ToolJson, string ExamplesJson)
    {
        public string Key => Category + "/" + Name;

        public string[] Tags
        {
            get
            {
                using var document = System.Text.Json.JsonDocument.Parse(ToolJson);
                return document.RootElement.TryGetProperty("tags", out var tags) ? tags.EnumerateArray().Select(t => t.GetString()!).ToArray() : [];
            }
        }
    }

    private static readonly Lazy<IReadOnlyList<Seed>> Cache = new(Load);

    public static IReadOnlyList<Seed> All() => Cache.Value;

    public static Seed Get(string key) => All().Single(s => s.Key == key);

    private static IReadOnlyList<Seed> Load()
    {
        var root = FindSeedRoot();
        return Directory.GetDirectories(root)
            .SelectMany(category => Directory.GetDirectories(category).Select(seed => (category: Path.GetFileName(category), seed)))
            .Where(x => File.Exists(Path.Combine(x.seed, "tool.json")))
            .Select(x => new Seed(x.category, Path.GetFileName(x.seed),
                File.ReadAllText(Path.Combine(x.seed, "code.cs")),
                File.ReadAllText(Path.Combine(x.seed, "tool.json")),
                File.ReadAllText(Path.Combine(x.seed, "examples.json"))))
            .OrderBy(s => s.Key, StringComparer.Ordinal)
            .ToArray();
    }

    private static string FindSeedRoot()
    {
        // bin/Debug/net48 → HPNavis.McpBridge.Tests → HPNavis → HPNavis.Mcp.Server/Registry/SeedLibrary
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        for (var probe = directory; probe is not null; probe = probe.Parent)
        {
            var candidate = Path.Combine(probe.FullName, "HPNavis.Mcp.Server", "Registry", "SeedLibrary");
            if (Directory.Exists(candidate)) return candidate;
        }

        throw new DirectoryNotFoundException("HPNavis.Mcp.Server/Registry/SeedLibrary not found above " + AppContext.BaseDirectory);
    }
}
