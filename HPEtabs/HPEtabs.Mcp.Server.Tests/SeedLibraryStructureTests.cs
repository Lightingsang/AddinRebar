using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HPEtabs.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

namespace HPEtabs.Mcp.Server.Tests;

/// <summary>
///     Every seed tool embedded in the ETABS server: a well-formed record for this host, accepted by the registry
///     validator under the ETABS profile, guard-clean code that reads exactly the parameters its schema declares,
///     the destructive marker on exactly the seed that calls destructive members, no file-taking or units-changing
///     member anywhere, and examples an AI can copy. Compilation against ETABSv1.dll and the tier check are
///     <see cref="SeedLibraryCompileTests"/> (they need the ETABS install).
/// </summary>
public sealed class SeedLibraryStructureTests
{
    private const int MaxCodeBytes = 32 * 1024;
    private const int MaxCodeLines = 120;
    private static readonly string[] Modes = ["auto", "manual", "none"];

    /// <summary>Members no seed may call: they change the API units the bridge forces, attach to ETABS, or touch files.</summary>
    private static readonly Regex Forbidden = new(@"\b(SetPresentUnits|Helper|OpenFile|ExportFile|ImportFile|ImportProp|\w*CSVFile|MergeAnalysisResults|ShowTablesInExcel|ApplyEditedTables|SetModelIsLocked|InitializeNewModel|CreateAnalysisModel|ModifyUndeformedGeometry\w*|NewBlank|NewGridOnly|NewSteelDeck)\b", RegexOptions.Compiled);

    /// <summary>The script must end with a top-level return statement, not merely contain one: nothing but that statement's own lines may follow the last `return` at column 0.</summary>
    private static bool EndsWithReturn(string code)
    {
        var lines = code.TrimEnd().Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
        var last = Array.FindLastIndex(lines, l => l.StartsWith("return ", StringComparison.Ordinal) || l == "return;");
        if (last < 0 || !lines[^1].TrimEnd().EndsWith(';')) return false;
        // Continuation lines of a multi-line return are indented or close the initializer; a new statement at column 0 is not.
        return lines.Skip(last + 1).All(l => l.Length == 0 || char.IsWhiteSpace(l[0]) || l[0] is '{' or '}' or ')');
    }

    /// <summary>Destructive members allowed in the one destructive seed only.</summary>
    private static readonly Regex DestructiveMember = new(@"\.(RunAnalysis|DeleteResults|Save|Delete\w*)\(", RegexOptions.Compiled);

    public sealed record Seed(string Category, string Name, JsonElement Tool, string Code, JsonElement Examples);

    public static IEnumerable<object[]> Seeds() => LoadSeeds().Select(s => new object[] { s.Category + "/" + s.Name });

    private static readonly Lazy<IReadOnlyList<Seed>> Cache = new(LoadSeedsCore);

    public static IReadOnlyList<Seed> LoadSeeds() => Cache.Value;

    private static IReadOnlyList<Seed> LoadSeedsCore()
    {
        var assembly = typeof(EtabsHostProfile).Assembly;
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

    public static Seed Get(string key) => LoadSeeds().Single(s => s.Category + "/" + s.Name == key);

    [Fact]
    public void All_twelve_seeds_are_embedded_eight_read_only_three_writes_one_destructive()
    {
        var seeds = LoadSeeds();

        Assert.Equal(12, seeds.Count);
        Assert.Equal(8, seeds.Count(s => s.Tool.GetProperty("transaction").GetString() == "none"));
        Assert.Equal(4, seeds.Count(s => s.Tool.GetProperty("transaction").GetString() == "auto"));
        Assert.Equal(["run_analysis"], seeds.Where(s => Tags(s).Contains("destructive")).Select(s => s.Name).ToArray());
        Assert.Equal(["Analysis", "Geometry", "Load", "Model", "Property", "Results"], seeds.Select(s => s.Category).Distinct().OrderBy(c => c, StringComparer.Ordinal).ToArray());
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Seed_record_is_well_formed_for_the_etabs_host(string key)
    {
        var seed = Get(key);
        var tool = seed.Tool;
        var profile = EtabsHostProfile.Instance;

        Assert.Equal(seed.Name, tool.GetProperty("name").GetString());
        Assert.Equal(seed.Category, tool.GetProperty("category").GetString());
        Assert.Contains(seed.Category, profile.Categories);
        Assert.Matches("^[a-z][a-z0-9_]{2,63}$", seed.Name);
        Assert.False(ToolValidator.IsReserved(seed.Name, profile), "seed shadows a core tool");
        Assert.Equal("etabs", tool.GetProperty("host").GetString());
        Assert.Equal(["22"], tool.GetProperty("hostVersions").EnumerateArray().Select(v => v.GetString()!).ToArray());
        Assert.Equal("hprebar", tool.GetProperty("author").GetString());
        Assert.Equal("published", tool.GetProperty("status").GetString());
        Assert.False(string.IsNullOrWhiteSpace(tool.GetProperty("notes").GetString()), "notes must name the OAPI members (documentation topics) the seed relies on");
        var description = tool.GetProperty("description").GetString()!;
        Assert.True(description.Length >= 40, "description too short");
        var mode = tool.GetProperty("transaction").GetString();
        Assert.Contains(mode, Modes);
        var timeout = tool.GetProperty("timeoutSeconds").GetInt32();
        Assert.InRange(timeout, 30, profile.MaxTimeoutSeconds);
        Assert.Equal(mode != "none", tool.GetProperty("destructive").GetBoolean());
        Assert.Equal(JsonValueKind.Object, tool.GetProperty("inputSchema").GetProperty("properties").ValueKind);
        Assert.True(Encoding.UTF8.GetByteCount(seed.Code) < MaxCodeBytes, "code too large");
        Assert.True(seed.Code.Split('\n').Length <= MaxCodeLines, $"seed code longer than {MaxCodeLines} lines");
        Assert.True(EndsWithReturn(seed.Code), "the script must end with a top-level return statement");
        Assert.DoesNotMatch(Forbidden, seed.Code);
        // The compile-time tier check binds member accesses only; keep the seeds inside what it mirrors of the bridge's analyzer.
        Assert.DoesNotContain("?.", seed.Code);

        var isDestructive = Tags(seed).Contains("destructive");
        Assert.Equal(isDestructive, DestructiveMember.IsMatch(seed.Code)); // the marker and the destructive calls go together
        if (isDestructive)
        {
            Assert.Equal("auto", mode);
            Assert.Equal(profile.MaxTimeoutSeconds, timeout);
            Assert.StartsWith("DESTRUCTIVE", description);
            Assert.Contains("Allow destructive operations", description);
            Assert.Contains("timeout", description); // the timeout-does-not-abort warning
        }
        else
        {
            Assert.True(timeout <= 120, "only the destructive seed may need more than the normal ceiling");
        }

        if (mode == "none") Assert.Contains("Read-only", description);
        else Assert.Contains("snapshot", description); // a writing seed tells the AI the model is saved and copied first

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
    public void Seed_record_passes_the_registry_validator_for_the_etabs_profile(string key)
    {
        var seed = Get(key);
        var record = RegistryJson.Deserialize<ToolRecord>(seed.Tool.GetRawText())!;
        record.Code = seed.Code;
        record.Examples = RegistryJson.Deserialize<List<ToolExample>>(seed.Examples.GetRawText())!;

        var report = ToolValidator.Validate(record, null, [], false, EtabsHostProfile.Instance);

        Assert.True(report.IsValid, string.Join("; ", report.Errors));
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Seed_code_passes_the_etabs_guard_and_reads_only_declared_args(string key)
    {
        var seed = Get(key);

        var guard = ScriptGuard.Check(seed.Code, GuardProfile.Etabs);
        Assert.True(guard.Count == 0, string.Join("; ", guard.Select(g => $"{g.Line}:{g.Column} {g.Message}")));

        var facts = ScriptAnalyzer.Analyze(seed.Code, AnalyzerProfile.Etabs);
        var declared = seed.Tool.GetProperty("inputSchema").GetProperty("properties").EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var read = facts.ArgKeys.Select(a => a.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.True(read.IsSubsetOf(declared), "code reads undeclared args: " + string.Join(", ", read.Except(declared)));
        Assert.True(declared.IsSubsetOf(read), "schema declares unused args: " + string.Join(", ", declared.Except(read)));
        Assert.False(facts.UsesTransaction, "ETABS has no transaction; a seed never opens one");
    }

    [Fact]
    public void The_forbidden_member_pattern_matches_the_real_file_taking_members()
    {
        Assert.Matches(Forbidden, "sapModel.DatabaseTables.GetTableForDisplayCSVFile(k, ref f, g, ref v, \"x\");");
        Assert.Matches(Forbidden, "sapModel.DatabaseTables.SetTableForEditingCSVFile(k, ref v, \"x\");");
        Assert.Matches(Forbidden, "sapModel.Analyze.ModifyUndeformedGeometryModeShape(\"x\", 1, 1, 1, false);");
        Assert.Matches(Forbidden, "sapModel.SetPresentUnits(eUnits.kN_mm_C);");
        Assert.DoesNotMatch(Forbidden, "sapModel.Results.Setup.DeselectAllCasesAndCombosForOutput();");
        Assert.DoesNotMatch(Forbidden, "var helperText = \"x\";");
        Assert.Matches(DestructiveMember, "sapModel.FrameObj.Delete(\"F1\", eItemType.Objects);");
        Assert.DoesNotMatch(DestructiveMember, "sapModel.Results.Setup.DeselectAllCasesAndCombosForOutput();");
    }

    [Fact]
    public void Only_the_destructive_seed_names_destructive_members_and_no_seed_takes_a_path()
    {
        foreach (var seed in LoadSeeds().Where(s => s.Name != "run_analysis"))
            Assert.False(DestructiveMember.IsMatch(seed.Code), seed.Name + " calls a destructive member");

        // No seed hands ETABS a file path: the path-taking members are all in the forbidden list above, and `File.` never appears.
        Assert.All(LoadSeeds(), s => Assert.DoesNotContain(".File.", s.Code));
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
