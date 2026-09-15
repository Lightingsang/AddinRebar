using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HPNavis.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

namespace HPNavis.Mcp.Server.Tests;

/// <summary>
///     Every seed tool embedded in the Navisworks server: a well-formed record for this host, accepted by the
///     registry validator under the Navisworks profile, guard-clean code that reads exactly the parameters its
///     schema declares, the heavy marker on exactly the seeds that call heavy members, and examples an AI can
///     copy. Compilation against the Navisworks API is the net48 test project's job (HPNavis.McpBridge.Tests).
/// </summary>
public sealed class SeedLibraryStructureTests
{
    private const int MaxCodeBytes = 32 * 1024;
    private static readonly string[] Modes = ["auto", "manual", "none"];
    private static readonly Regex HeavyMember = new(@"\.(AppendFile|AppendFiles|TryAppendFile|MergeFile|MergeFiles|RemoveFile|OpenFile|UpdateFiles|SaveFile|ExportToNwd|PublishFile|ExportAsDwf|GenerateImage|TestsRunTest|TestsRunAllTests|TestsCompactAllTests|TestsCompactTest)\s*\(", RegexOptions.Compiled);

    public sealed record Seed(string Category, string Name, JsonElement Tool, string Code, JsonElement Examples);

    public static IEnumerable<object[]> Seeds() => LoadSeeds().Select(s => new object[] { s.Category + "/" + s.Name });

    private static readonly Lazy<IReadOnlyList<Seed>> Cache = new(LoadSeedsCore);

    private static IReadOnlyList<Seed> LoadSeeds() => Cache.Value;

    private static IReadOnlyList<Seed> LoadSeedsCore()
    {
        var assembly = typeof(NavisHostProfile).Assembly;
        var names = assembly.GetManifestResourceNames().Where(n => n.StartsWith("SeedLibrary/", StringComparison.Ordinal)).ToArray();
        string Read(string name)
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        return names
            .Select(n => n.Replace('\\', '/'))
            .GroupBy(n => n[..n.LastIndexOf('/')])
            .Select(g =>
            {
                var parts = g.Key.Split('/'); // SeedLibrary/<Category>/<name>
                string Of(string file) => names.First(n => n.Replace('\\', '/') == g.Key + "/" + file);
                return new Seed(parts[1], parts[2],
                    JsonSerializer.Deserialize<JsonElement>(Read(Of("tool.json"))),
                    Read(Of("code.cs")),
                    JsonSerializer.Deserialize<JsonElement>(Read(Of("examples.json"))));
            })
            .OrderBy(s => s.Category).ThenBy(s => s.Name)
            .ToArray();
    }

    private static Seed Get(string key) => LoadSeeds().Single(s => s.Category + "/" + s.Name == key);

    [Fact]
    public void All_twelve_seeds_are_embedded_eight_read_only_three_review_edits_one_heavy()
    {
        var seeds = LoadSeeds();

        Assert.Equal(12, seeds.Count);
        Assert.Equal(8, seeds.Count(s => s.Tool.GetProperty("transaction").GetString() == "none"));
        Assert.Equal(4, seeds.Count(s => s.Tool.GetProperty("transaction").GetString() == "auto"));
        Assert.Equal(["create_and_run_clash_test"], seeds.Where(s => Tags(s).Contains("heavy")).Select(s => s.Name).ToArray());
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Seed_record_is_well_formed_for_the_navis_host(string key)
    {
        var seed = Get(key);
        var tool = seed.Tool;
        var profile = NavisHostProfile.Instance;

        Assert.Equal(seed.Name, tool.GetProperty("name").GetString());
        Assert.Equal(seed.Category, tool.GetProperty("category").GetString());
        Assert.Contains(seed.Category, profile.Categories);
        Assert.Matches("^[a-z][a-z0-9_]{2,63}$", seed.Name);
        Assert.False(ToolValidator.IsReserved(seed.Name, profile), "seed shadows a core tool");
        Assert.Equal("navis", tool.GetProperty("host").GetString());
        Assert.Equal(["2026"], tool.GetProperty("hostVersions").EnumerateArray().Select(v => v.GetString()!).ToArray());
        Assert.Equal("hprebar", tool.GetProperty("author").GetString());
        Assert.Equal("published", tool.GetProperty("status").GetString());
        var description = tool.GetProperty("description").GetString()!;
        Assert.True(description.Length >= 40, "description too short");
        Assert.Contains(tool.GetProperty("transaction").GetString(), Modes);
        var timeout = tool.GetProperty("timeoutSeconds").GetInt32();
        Assert.InRange(timeout, 30, profile.MaxTimeoutSeconds);
        Assert.Equal(tool.GetProperty("transaction").GetString() != "none", tool.GetProperty("destructive").GetBoolean());
        Assert.Equal(JsonValueKind.Object, tool.GetProperty("inputSchema").GetProperty("properties").ValueKind);
        Assert.True(Encoding.UTF8.GetByteCount(seed.Code) < MaxCodeBytes, "code too large");
        Assert.True(seed.Code.Split('\n').Length <= 80, "seed code longer than 80 lines");
        Assert.Matches(@"return\s", seed.Code);

        var isHeavy = Tags(seed).Contains("heavy");
        Assert.Equal(isHeavy, HeavyMember.IsMatch(seed.Code)); // the heavy marker and the heavy calls go together
        if (isHeavy)
        {
            Assert.Equal(profile.MaxTimeoutSeconds, timeout);
            Assert.StartsWith("HEAVY", description);
            Assert.Contains("Allow heavy operations", description);
        }
        else
        {
            Assert.True(timeout <= 120, "a non-heavy seed cannot need more than the normal ceiling");
        }
        if (tool.GetProperty("transaction").GetString() == "none") Assert.Contains("Read-only", description);

        var properties = tool.GetProperty("inputSchema").GetProperty("properties").EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var required = tool.GetProperty("inputSchema").TryGetProperty("required", out var r) ? r.EnumerateArray().Select(e => e.GetString()!).ToArray() : [];
        Assert.All(required, name => Assert.Contains(name, properties));

        // A JSON string written as if inside a C# literal reaches the AI double-escaped.
        Assert.All(Strings(tool).Concat(Strings(seed.Examples)), s => Assert.False(s.Contains("\\\\") || s.Contains("\\\""), "double-escaped: " + s));

        Assert.True(seed.Examples.GetArrayLength() >= 2, "need at least two examples");
        var distinct = seed.Examples.EnumerateArray().Select(e => JsonSerializer.Serialize(e.GetProperty("args"))).Distinct().Count();
        Assert.True(distinct >= 2, "examples must differ in args");
        foreach (var example in seed.Examples.EnumerateArray())
        {
            Assert.False(string.IsNullOrWhiteSpace(example.GetProperty("title").GetString()));
            var argsKeys = example.GetProperty("args").EnumerateObject().Select(p => p.Name).ToArray();
            Assert.All(argsKeys, k => Assert.Contains(k, properties));
            Assert.All(required, k => Assert.Contains(k, argsKeys, StringComparer.OrdinalIgnoreCase));
        }
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Seed_record_passes_the_registry_validator_for_the_navis_profile(string key)
    {
        var seed = Get(key);
        var record = RegistryJson.Deserialize<ToolRecord>(seed.Tool.GetRawText())!;
        record.Code = seed.Code;
        record.Examples = RegistryJson.Deserialize<List<ToolExample>>(seed.Examples.GetRawText())!;

        var report = ToolValidator.Validate(record, null, [], false, NavisHostProfile.Instance);

        Assert.True(report.IsValid, string.Join("; ", report.Errors));
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Seed_code_passes_the_navis_guard_and_reads_only_declared_args(string key)
    {
        var seed = Get(key);

        var guard = ScriptGuard.Check(seed.Code, GuardProfile.Navis);
        Assert.True(guard.Count == 0, string.Join("; ", guard.Select(g => $"{g.Line}:{g.Column} {g.Message}")));

        var facts = ScriptAnalyzer.Analyze(seed.Code, AnalyzerProfile.Navis);
        var declared = seed.Tool.GetProperty("inputSchema").GetProperty("properties").EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var read = facts.ArgKeys.Select(a => a.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.True(read.IsSubsetOf(declared), "code reads undeclared args: " + string.Join(", ", read.Except(declared)));
        Assert.True(declared.IsSubsetOf(read), "schema declares unused args: " + string.Join(", ", declared.Except(read)));
        Assert.False(facts.UsesTransaction, "Navisworks seeds never open a Transaction; the bridge owns the only one");
    }

    private static string[] Tags(Seed seed) => seed.Tool.TryGetProperty("tags", out var tags) ? tags.EnumerateArray().Select(t => t.GetString()!).ToArray() : [];

    private static IEnumerable<string> Strings(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => [element.GetString()!],
        JsonValueKind.Object => element.EnumerateObject().SelectMany(p => Strings(p.Value)),
        JsonValueKind.Array => element.EnumerateArray().SelectMany(Strings),
        _ => [],
    };
}
