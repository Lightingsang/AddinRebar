using System.Text;
using System.Text.Json;
using HPCivil3d.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

namespace HPCivil3d.Mcp.Server.Tests;

/// <summary>
///     Every seed tool shipped inside the Civil server: a well-formed record for this host, code that passes the
///     Civil guard and reads exactly the parameters its schema declares, units labelled the way the contract says,
///     and — when Civil 3D 2026 is installed and AutoCAD.NET 25.1.0 is in the NuGet cache — code that compiles
///     against the real API without acad.exe (metadata only). The wrapper mirrors the bridge: the default usings are
///     <see cref="HostScriptContracts.Civil3dImports"/> and the fields are the bridge's globals, `civil` included.
/// </summary>
public sealed class SeedLibraryTests
{
    private const int MaxCodeBytes = 32 * 1024;
    private const int MaxCodeLines = 120;
    private static readonly string[] Modes = ["auto", "manual", "none"];

    // Numeric schema keys that are not millimetres and say so through their name or the seed description:
    // stations / elevations / areas are drawing units, the rest are counts, indices or codes.
    private static readonly HashSet<string> NonMillimetreNumbers = new(StringComparer.Ordinal)
    {
        "x", "y", "elevation", "limit", "offset", "maxSamples", "partLimit", "numberFrom", "numberTo", "entityLimit", "entityOffset",
    };

    public sealed record Seed(string Category, string Name, JsonElement Tool, string Code, JsonElement Examples);

    public static IEnumerable<object[]> Seeds() => LoadSeeds().Select(s => new object[] { s.Category + "/" + s.Name });

    private static readonly Lazy<IReadOnlyList<Seed>> Cache = new(LoadSeedsCore);

    private static IReadOnlyList<Seed> LoadSeeds() => Cache.Value;

    private static IReadOnlyList<Seed> LoadSeedsCore()
    {
        var assembly = typeof(Civil3dHostProfile).Assembly;
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
    public void All_twelve_seeds_are_embedded()
    {
        var seeds = LoadSeeds();
        Assert.Equal(12, seeds.Count);
        Assert.Equal(seeds.Count, seeds.Select(s => s.Name).Distinct().Count());
        Assert.Equal(10, seeds.Count(s => s.Tool.GetProperty("transaction").GetString() == "none"));
        Assert.Equal(["create_alignment_from_polyline", "create_cogo_points"], seeds.Where(s => s.Tool.GetProperty("transaction").GetString() == "auto").Select(s => s.Name).Order().ToArray());
        Assert.Equal(["Alignment", "Corridor", "Document", "Parcel", "Pipe", "Point", "Profile", "Surface"], seeds.Select(s => s.Category).Distinct().Order().ToArray());
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Seed_record_is_well_formed_for_the_civil3d_host(string key)
    {
        var seed = Get(key);
        var tool = seed.Tool;
        var profile = Civil3dHostProfile.Instance;

        Assert.Equal(seed.Name, tool.GetProperty("name").GetString());
        Assert.Equal(seed.Category, tool.GetProperty("category").GetString());
        Assert.Contains(seed.Category, profile.Categories);
        Assert.Matches("^[a-z][a-z0-9_]{2,63}$", seed.Name);
        Assert.False(ToolValidator.IsReserved(seed.Name, profile), "seed shadows a core tool");
        Assert.Equal("civil3d", tool.GetProperty("host").GetString());
        Assert.Equal(["2026"], tool.GetProperty("hostVersions").EnumerateArray().Select(v => v.GetString()!).ToArray());
        Assert.Equal("hprebar", tool.GetProperty("author").GetString());
        Assert.Equal("published", tool.GetProperty("status").GetString());
        var tags = tool.GetProperty("tags").EnumerateArray().Select(t => t.GetString()).ToArray();
        Assert.Contains("seed", tags);
        Assert.Contains("civil3d", tags);
        var description = tool.GetProperty("description").GetString()!;
        Assert.True(description.Length >= 40, "description too short");
        Assert.Contains(tool.GetProperty("transaction").GetString(), Modes);
        Assert.InRange(tool.GetProperty("timeoutSeconds").GetInt32(), 30, 60);
        Assert.Equal(tool.GetProperty("transaction").GetString() != "none", tool.GetProperty("destructive").GetBoolean());
        Assert.Equal(JsonValueKind.Object, tool.GetProperty("inputSchema").GetProperty("properties").ValueKind);
        Assert.False(tool.GetProperty("inputSchema").GetProperty("additionalProperties").GetBoolean());
        Assert.True(Encoding.UTF8.GetByteCount(seed.Code) < MaxCodeBytes, "code too large");
        Assert.True(seed.Code.Split('\n').Length <= MaxCodeLines, $"code is {seed.Code.Split('\n').Length} lines; split the tool or move the logic");
        Assert.EndsWith(";", seed.Code.TrimEnd());
        Assert.Contains("return ", seed.Code);

        var properties = tool.GetProperty("inputSchema").GetProperty("properties").EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var required = tool.GetProperty("inputSchema").TryGetProperty("required", out var r) ? r.EnumerateArray().Select(e => e.GetString()!).ToArray() : [];
        Assert.All(required, name => Assert.Contains(name, properties));

        // A JSON string written as if inside a C# literal reaches the AI double-escaped (`\\P` instead of `\P`).
        Assert.All(SeedSchema.Strings(tool).Concat(SeedSchema.Strings(seed.Examples)), s => Assert.False(s.Contains("\\\\") || s.Contains("\\\""), "double-escaped: " + s));

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
    public void Seed_record_passes_the_registry_validator_for_the_civil3d_profile(string key)
    {
        var seed = Get(key);
        var record = RegistryJson.Deserialize<ToolRecord>(seed.Tool.GetRawText())!;
        record.Code = seed.Code;
        record.Examples = RegistryJson.Deserialize<List<ToolExample>>(seed.Examples.GetRawText())!;

        var report = ToolValidator.Validate(record, null, [], false, Civil3dHostProfile.Instance);

        Assert.True(report.IsValid, string.Join("; ", report.Errors));
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Seed_code_passes_the_civil3d_guard_and_reads_only_declared_args(string key)
    {
        var seed = Get(key);

        var guard = ScriptGuard.Check(seed.Code, GuardProfile.Civil3d);
        Assert.True(guard.Count == 0, string.Join("; ", guard.Select(g => $"{g.Line}:{g.Column} {g.Message}")));

        var facts = ScriptAnalyzer.Analyze(seed.Code, AnalyzerProfile.Civil3d);
        var declared = seed.Tool.GetProperty("inputSchema").GetProperty("properties").EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var read = facts.ArgKeys.Select(a => a.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.True(read.IsSubsetOf(declared), "code reads undeclared args: " + string.Join(", ", read.Except(declared)));
        Assert.True(declared.IsSubsetOf(read), "schema declares unused args: " + string.Join(", ", declared.Except(read)));
        Assert.False(facts.UsesTransaction, "Civil seeds never start transactions of their own; `tr` is the bridge's");

        // The analyzer sees `args.X("key")` only; keys read off a list item (`points[i].Double("x")`, `p.Str("name")`) are
        // matched here against the nested schemas, both ways, so a renamed item key cannot slip past the tests.
        var nestedDeclared = SeedSchema.NestedProperties(seed.Tool.GetProperty("inputSchema")).ToHashSet(StringComparer.Ordinal);
        var nestedRead = SeedSchema.NestedArgReads(seed.Code).ToHashSet(StringComparer.Ordinal);
        Assert.True(nestedRead.IsSubsetOf(nestedDeclared), "code reads undeclared item keys: " + string.Join(", ", nestedRead.Except(nestedDeclared)));
        Assert.True(nestedDeclared.IsSubsetOf(nestedRead), "schema declares unused item keys: " + string.Join(", ", nestedDeclared.Except(nestedRead)));
    }

    [Theory]
    [InlineData("list_alignments", "limit", 180)]
    [InlineData("list_profiles", "limit", 180)]
    [InlineData("list_surfaces", "limit", 500)]
    [InlineData("list_corridors", "limit", 500)]
    [InlineData("list_pipe_networks", "limit", 500)]
    [InlineData("list_pipe_networks", "partLimit", 100)]
    [InlineData("list_parcels", "limit", 250)]
    [InlineData("list_cogo_points", "limit", 300)]
    [InlineData("get_alignment_geometry", "entityLimit", 150)]
    [InlineData("get_alignment_geometry", "maxSamples", 200)]
    public void Page_maximums_keep_a_full_page_under_the_64_KB_result_cap(string name, string key, int maximum)
    {
        // Bytes per item measured on the wire (smoke run): alignment 325, profile 338, parcel 232, COGO point 182, alignment entity 322,
        // pipe 535 / structure 396 (partLimit is one budget across pipes and structures), surface ~400, corridor ~300 + baselines.
        // maximum × bytes stays under ~60 KB for every row above; a larger maximum needs a slimmer item first.
        var property = LoadSeeds().Single(s => s.Name == name).Tool.GetProperty("inputSchema").GetProperty("properties").GetProperty(key);
        Assert.Equal(maximum, property.GetProperty("maximum").GetInt32());
        Assert.True(property.GetProperty("default").GetInt32() <= maximum);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Seed_never_names_a_rebuild_data_shortcut_or_file_member(string key)
    {
        // Belt and braces over the guard: these names must not appear even in a comment or a string, so a future guard
        // relaxation cannot quietly turn a read-only seed into a rebuild.
        // Reading RebuildAutomatic / AutoRebuild is allowed (flags the AI needs to see); calling a rebuild is not.
        var code = Get(key).Code;
        foreach (var forbidden in new[] { "Rebuild(", "RebuildAll", "RebuildSnapshot", "DataShortcuts", "ExportTo", "CreateFrom", "ImportPoints", "ExportPoints", "SurveyProjects", "AeccUiMgd", "Interop" })
            Assert.DoesNotContain(forbidden, code, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Seed_labels_its_units_and_caps_its_page(string key)
    {
        var seed = Get(key);
        var description = seed.Tool.GetProperty("description").GetString()!;
        if (seed.Code.Contains("units.ToMm", StringComparison.Ordinal) || seed.Code.Contains("Elevation", StringComparison.Ordinal))
            Assert.True(description.Contains("mm", StringComparison.Ordinal) || description.Contains("drawing unit", StringComparison.Ordinal), "a seed that returns measures must say which unit they are in");
        Assert.Contains("drawingUnit", seed.Code);

        foreach (var (name, property) in SeedSchema.NumericProperties(seed.Tool.GetProperty("inputSchema")))
        {
            Assert.True(name.EndsWith("Mm", StringComparison.Ordinal) || NonMillimetreNumbers.Contains(name),
                $"{seed.Name}.{name}: a numeric input is millimetres (suffix Mm) or one of the documented drawing-unit / count keys");
            if (name is "limit" or "maxSamples" or "partLimit")
            {
                Assert.True(property.GetProperty("maximum").GetInt32() <= 500, $"{seed.Name}.{name}: a page over 500 items breaks the 64 KB cap");
                Assert.True(property.GetProperty("default").GetInt32() <= property.GetProperty("maximum").GetInt32());
            }
        }

        foreach (var array in SeedSchema.ArrayProperties(seed.Tool.GetProperty("inputSchema")))
            if (array.TryGetProperty("maxItems", out var maxItems)) Assert.True(maxItems.GetInt32() <= 500);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Schema_defaults_equal_the_code_fallbacks(string key)
    {
        // Nothing fills a missing argument: the code's own fallback is what runs, and the schema `default` is what the AI reads.
        // The two must be the same literal, or a caller who omits the key gets a different answer from the one the schema promised
        // (a lowered `maximum` once left `partLimit` falling back to 200 while the schema said 60 — the seed refused its own default).
        var seed = Get(key);
        foreach (var property in seed.Tool.GetProperty("inputSchema").GetProperty("properties").EnumerateObject())
        {
            if (!property.Value.TryGetProperty("default", out var schemaDefault)) continue;
            var reads = SeedSchema.ArgReadsWithFallback(seed.Code, property.Name).ToArray();
            Assert.True(reads.Length > 0, $"{seed.Name}.{property.Name} has a schema default but the code never reads it with a literal fallback (an expression fallback cannot be compared)");
            var expected = schemaDefault.ValueKind switch
            {
                JsonValueKind.True => "true", JsonValueKind.False => "false",
                JsonValueKind.String => "\"" + schemaDefault.GetString() + "\"",
                _ => schemaDefault.GetRawText(),
            };
            Assert.All(reads, fallback => Assert.True(string.Equals(fallback, expected, StringComparison.Ordinal), $"{seed.Name}.{property.Name}: schema default {expected} but code falls back to {fallback}"));
        }
    }

    [Theory]
    [InlineData("create_cogo_points")]
    [InlineData("create_alignment_from_polyline")]
    public void Write_seed_documents_its_side_effects_and_dry_run(string name)
    {
        var seed = LoadSeeds().Single(s => s.Name == name);
        Assert.Equal("auto", seed.Tool.GetProperty("transaction").GetString());
        Assert.True(seed.Tool.GetProperty("destructive").GetBoolean());
        var description = seed.Tool.GetProperty("description").GetString()!;
        Assert.Contains("Side effects:", description);
        Assert.Contains("dryRun", description);
        Assert.Contains("createdCount", seed.Code);
        Assert.Contains("affectedHandles", seed.Code);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Seed_code_compiles_against_the_civil3d_api(string key)
    {
        var seed = Get(key);
        var errors = SeedCompileProbe.Compile(seed.Code);
        Assert.SkipWhen(errors is null, SeedCompileProbe.SkipReason());
        Assert.True(errors!.Length == 0, seed.Name + Environment.NewLine + string.Join(Environment.NewLine, errors));
    }

    [Fact]
    public void Compile_check_rejects_api_misuse_and_accepts_real_civil_api()
    {
        var bad = SeedCompileProbe.Compile("return civil.NoSuchMember(args.Int(\"n\"));");
        Assert.SkipWhen(bad is null, SeedCompileProbe.SkipReason());
        Assert.Contains(bad!, e => e.Contains("CS1061") && e.Contains("NoSuchMember"));

        var good = SeedCompileProbe.Compile(
            "var n = 0; foreach (ObjectId id in civil.GetAlignmentIds()) { var a = (Alignment)tr.GetObject(id, OpenMode.ForRead); n += a.Entities.Count; } " +
            "var s = civil.GetSurfaceIds().Count > 0 ? (Autodesk.Civil.DatabaseServices.Surface)tr.GetObject(civil.GetSurfaceIds()[0], OpenMode.ForRead) : null; " +
            "return new { n, unit = units.Label, mm = units.ToMm(1) + args.Double(\"x\"), z = s == null ? 0 : s.FindElevationAtXY(0, 0) };");
        Assert.Empty(good!);

        // The guard, not the compiler, is what keeps rebuilds out: the wrapper must not hide that.
        Assert.Contains(ScriptGuard.Check("civil.CorridorCollection.RebuildAll(); return 1;", GuardProfile.Civil3d), d => d.Message.Contains("RebuildAll"));
    }
}
