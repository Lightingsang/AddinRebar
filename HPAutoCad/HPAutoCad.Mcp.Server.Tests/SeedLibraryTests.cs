using System.Text;
using System.Text.Json;
using HPAutoCad.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace HPAutoCad.Mcp.Server.Tests;

/// <summary>
///     Every seed tool shipped inside the AutoCAD server: well-formed record for this host, guard-clean
///     code under the AutoCAD profile that reads only the parameters its schema declares, and — when the
///     AutoCAD.NET 25.1.0 assemblies are in the NuGet cache — code that compiles against AutoCAD 2026
///     without acad.exe (metadata only). The wrapper mirrors the bridge exactly: the default usings come
///     from <see cref="HostScriptContracts.AutocadImports"/> and the fields are the bridge's globals, so
///     a seed that passes here compiles inside AutoCAD too.
/// </summary>
public sealed class SeedLibraryTests
{
    private const int MaxCodeBytes = 32 * 1024;
    private const string PackageVersion = "25.1.0";
    private static readonly string[] Modes = ["auto", "manual", "none"];

    public sealed record Seed(string Category, string Name, JsonElement Tool, string Code, JsonElement Examples);

    public static IEnumerable<object[]> Seeds() => LoadSeeds().Select(s => new object[] { s.Category + "/" + s.Name });

    private static readonly Lazy<IReadOnlyList<Seed>> Cache = new(LoadSeedsCore);

    private static IReadOnlyList<Seed> LoadSeeds() => Cache.Value;

    private static IReadOnlyList<Seed> LoadSeedsCore()
    {
        var assembly = typeof(AutocadHostProfile).Assembly;
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
    public void All_fifty_seeds_are_embedded()
    {
        // 12 drawing/data seeds + 24 read-only AEC engine seeds (context, entity query, spatial query, measure, geometry issues, classification,
        // relationships, standards, audit, grids, members, connectivity, alignment, openings, rooms, room boundary, area schedule, MEP network,
        // MEP connectivity, MEP endpoints, clash check, change-set begin / preview / summary) + 14 AEC write seeds (batch create/update, blocks + attributes, annotations, hatches, xrefs, issue markup,
        // member tagging, member schedule, room tags, auto dimensions, opening requests, change-set commit / rollback)
        var seeds = LoadSeeds();
        Assert.Equal(50, seeds.Count);
        Assert.Equal(seeds.Count, seeds.Select(s => s.Name).Distinct().Count());
        Assert.Equal(30, seeds.Count(s => s.Tool.GetProperty("transaction").GetString() == "none"));
    }

    [Theory]
    [InlineData("get_drawing_context")]
    [InlineData("query_entities")]
    [InlineData("query_entities_spatial")]
    [InlineData("measure_geometry")]
    [InlineData("detect_geometry_issues")]
    [InlineData("classify_aec_entities")]
    [InlineData("get_entity_relationships")]
    [InlineData("cad_standards_check")]
    [InlineData("audit_aec_drawing")]
    [InlineData("structural_detect_grids")]
    [InlineData("structural_detect_members")]
    [InlineData("structural_member_connectivity_check")]
    [InlineData("structural_column_alignment_check")]
    [InlineData("structural_opening_conflict_check")]
    [InlineData("arch_detect_rooms")]
    [InlineData("arch_room_boundary_check")]
    [InlineData("arch_generate_area_schedule")]
    [InlineData("mep_detect_network")]
    [InlineData("mep_connectivity_check")]
    [InlineData("mep_endpoint_check")]
    [InlineData("aec_clash_check")]
    [InlineData("begin_change_set")]
    [InlineData("preview_change_set")]
    [InlineData("get_change_summary")]
    public void Aec_seed_is_a_thin_shim_over_the_engine(string name)
    {
        // The tool is data + a shim: every AEC seed is read-only, calls the AecTools facade exactly once and returns its envelope.
        var seed = LoadSeeds().Single(s => s.Name == name);
        Assert.Equal("none", seed.Tool.GetProperty("transaction").GetString());
        AssertThinShim(seed, maxLines: 12);
        Assert.Contains("Read-only", seed.Tool.GetProperty("description").GetString());
    }

    [Theory]
    [InlineData("create_entities_batch")]
    [InlineData("update_entities_batch")]
    [InlineData("manage_blocks_attributes")]
    [InlineData("manage_annotations")]
    [InlineData("manage_hatches")]
    [InlineData("manage_xrefs")]
    [InlineData("create_issue_markup")]
    [InlineData("structural_tag_members")]
    [InlineData("structural_generate_member_schedule")]
    [InlineData("arch_create_room_tags")]
    [InlineData("arch_auto_dimension_plan")]
    [InlineData("aec_create_opening_requests")]
    [InlineData("commit_change_set")]
    [InlineData("rollback_change_set")]
    public void Aec_write_seed_is_a_thin_shim_that_documents_its_side_effects(string name)
    {
        // Write seeds run under the bridge's auto transaction (dryRun rolls back), read every declared arg, and say what they change.
        var seed = LoadSeeds().Single(s => s.Name == name);
        Assert.Equal("auto", seed.Tool.GetProperty("transaction").GetString());
        Assert.True(seed.Tool.GetProperty("destructive").GetBoolean());
        AssertThinShim(seed, maxLines: 18);
        var description = seed.Tool.GetProperty("description").GetString()!;
        Assert.Contains("Side effects:", description);
        Assert.Contains("dryRun", description);
        Assert.Contains("envelope", description);
    }

    private static void AssertThinShim(Seed seed, int maxLines)
    {
        Assert.Contains("return AecTools.", seed.Code);
        Assert.Equal(1, seed.Code.Split("AecTools.").Length - 1);
        Assert.True(seed.Code.Split('\n').Length <= maxLines, "an AEC seed reads args and calls the engine — no logic of its own");
    }

    [Theory]
    [InlineData("classify_aec_entities", HPAutoCad.Aec.AecTools.MaxClassifyLimit)]
    [InlineData("get_entity_relationships", HPAutoCad.Aec.AecTools.MaxRelationshipLimit)]
    [InlineData("query_entities", HPAutoCad.Aec.AecTools.MaxLimit)]
    [InlineData("detect_geometry_issues", HPAutoCad.Aec.AecTools.MaxLimit)]
    [InlineData("manage_blocks_attributes", HPAutoCad.Aec.Cad.BlockService.MaxDefinitionLimit)]
    [InlineData("cad_standards_check", HPAutoCad.Aec.AecTools.MaxIssueLimit)]
    [InlineData("audit_aec_drawing", HPAutoCad.Aec.AecTools.MaxIssueLimit)]
    [InlineData("structural_detect_grids", HPAutoCad.Aec.AecTools.MaxGridLimit)]
    [InlineData("structural_detect_members", HPAutoCad.Aec.AecTools.MaxMemberLimit)]
    [InlineData("structural_member_connectivity_check", HPAutoCad.Aec.AecTools.MaxIssueLimit)]
    [InlineData("structural_column_alignment_check", HPAutoCad.Aec.AecTools.MaxIssueLimit)]
    [InlineData("structural_opening_conflict_check", HPAutoCad.Aec.AecTools.MaxIssueLimit)]
    [InlineData("arch_detect_rooms", HPAutoCad.Aec.AecTools.MaxRoomLimit)]
    [InlineData("arch_room_boundary_check", HPAutoCad.Aec.AecTools.MaxIssueLimit)]
    [InlineData("arch_generate_area_schedule", HPAutoCad.Aec.AecTools.MaxAreaRowLimit)]
    [InlineData("mep_detect_network", HPAutoCad.Aec.AecTools.MaxNetworkLimit)]
    [InlineData("mep_connectivity_check", HPAutoCad.Aec.AecTools.MaxIssueLimit)]
    [InlineData("mep_endpoint_check", HPAutoCad.Aec.AecTools.MaxEndpointLimit)]
    [InlineData("aec_clash_check", HPAutoCad.Aec.AecTools.MaxClashLimit)]
    [InlineData("preview_change_set", HPAutoCad.Aec.AecTools.MaxChangeOpLimit)]
    public void Aec_seed_page_limits_match_the_engine_caps_that_keep_a_page_under_64_KB(string name, int engineCap)
    {
        // The schema's `maximum` is what the AI sees; the engine clamps to the same number, so a request never silently returns less than promised.
        var limit = LoadSeeds().Single(s => s.Name == name).Tool.GetProperty("inputSchema").GetProperty("properties").GetProperty("limit");
        Assert.Equal(engineCap, limit.GetProperty("maximum").GetInt32());
        Assert.True(limit.GetProperty("default").GetInt32() <= engineCap);
    }

    [Theory]
    [InlineData("create_entities_batch", "items")]
    [InlineData("update_entities_batch", "items")]
    [InlineData("update_entities_batch", "handles")]
    [InlineData("manage_blocks_attributes", "items")]
    [InlineData("manage_blocks_attributes", "handles")]
    [InlineData("manage_annotations", "items")]
    [InlineData("manage_annotations", "handles")]
    [InlineData("manage_hatches", "handles")]
    public void Aec_write_seed_batch_sizes_match_the_engine_cap(string name, string key)
    {
        // 200 items with every one refused (error + grouped warning) still serialise under the 64 KB cap (EditResultTests pins the bytes).
        var property = LoadSeeds().Single(s => s.Name == name).Tool.GetProperty("inputSchema").GetProperty("properties").GetProperty(key);
        Assert.Equal(HPAutoCad.Aec.Cad.BatchEditService.MaxBatchItems, property.GetProperty("maxItems").GetInt32());
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Seed_record_is_well_formed_for_the_autocad_host(string key)
    {
        var seed = Get(key);
        var tool = seed.Tool;
        var profile = AutocadHostProfile.Instance;

        Assert.Equal(seed.Name, tool.GetProperty("name").GetString());
        Assert.Equal(seed.Category, tool.GetProperty("category").GetString());
        Assert.Contains(seed.Category, profile.Categories);
        Assert.Matches("^[a-z][a-z0-9_]{2,63}$", seed.Name);
        Assert.False(ToolValidatorIsReserved(seed.Name), "seed shadows a core tool");
        Assert.Equal("autocad", tool.GetProperty("host").GetString());
        Assert.Equal(["2026"], tool.GetProperty("hostVersions").EnumerateArray().Select(v => v.GetString()!).ToArray());
        Assert.Equal("hprebar", tool.GetProperty("author").GetString());
        Assert.Equal("published", tool.GetProperty("status").GetString());
        var description = tool.GetProperty("description").GetString()!;
        Assert.True(description.Length >= 20, "description too short");
        Assert.Contains(tool.GetProperty("transaction").GetString(), Modes);
        Assert.InRange(tool.GetProperty("timeoutSeconds").GetInt32(), 30, 60);
        Assert.Equal(tool.GetProperty("transaction").GetString() != "none", tool.GetProperty("destructive").GetBoolean());
        Assert.Equal(JsonValueKind.Object, tool.GetProperty("inputSchema").GetProperty("properties").ValueKind);
        Assert.True(Encoding.UTF8.GetByteCount(seed.Code) < MaxCodeBytes, "code too large");

        var properties = tool.GetProperty("inputSchema").GetProperty("properties").EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var required = tool.GetProperty("inputSchema").TryGetProperty("required", out var r) ? r.EnumerateArray().Select(e => e.GetString()!).ToArray() : [];
        Assert.All(required, name => Assert.Contains(name, properties));

        // A JSON string written as if inside a C# literal reaches the AI double-escaped (`\\P` instead of `\P`).
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
    public void Seed_record_passes_the_registry_validator_for_the_autocad_profile(string key)
    {
        var seed = Get(key);
        var record = RegistryJson.Deserialize<ToolRecord>(seed.Tool.GetRawText())!;
        record.Code = seed.Code;
        record.Examples = RegistryJson.Deserialize<List<ToolExample>>(seed.Examples.GetRawText())!;

        var report = ToolValidator.Validate(record, null, [], false, AutocadHostProfile.Instance);

        Assert.True(report.IsValid, string.Join("; ", report.Errors));
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Seed_code_passes_the_autocad_guard_and_reads_only_declared_args(string key)
    {
        var seed = Get(key);

        var guard = ScriptGuard.Check(seed.Code, GuardProfile.Autocad);
        Assert.True(guard.Count == 0, string.Join("; ", guard.Select(g => $"{g.Line}:{g.Column} {g.Message}")));

        var facts = ScriptAnalyzer.Analyze(seed.Code, AnalyzerProfile.Autocad);
        var declared = seed.Tool.GetProperty("inputSchema").GetProperty("properties").EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var read = facts.ArgKeys.Select(a => a.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.True(read.IsSubsetOf(declared), "code reads undeclared args: " + string.Join(", ", read.Except(declared)));
        Assert.True(declared.IsSubsetOf(read), "schema declares unused args: " + string.Join(", ", declared.Except(read)));
        Assert.False(facts.UsesTransaction, "AutoCAD seeds never start transactions of their own; `tr` is the bridge's");
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Seed_code_compiles_against_the_autocad_api(string key)
    {
        var seed = Get(key);
        var errors = CompileAgainstAutocad(seed.Code);
        Assert.SkipWhen(errors is null, $"AutoCAD.NET {PackageVersion} assemblies not in the NuGet cache");
        Assert.True(errors!.Length == 0, seed.Name + Environment.NewLine + string.Join(Environment.NewLine, errors));
    }

    [Fact]
    public void Compile_check_rejects_api_misuse_and_accepts_real_api()
    {
        var bad = CompileAgainstAutocad("return db.NoSuchMember(args.Int(\"n\"));");
        Assert.SkipWhen(bad is null, $"AutoCAD.NET {PackageVersion} assemblies not in the NuGet cache");
        Assert.Contains(bad!, e => e.Contains("CS1061") && e.Contains("NoSuchMember"));

        var good = CompileAgainstAutocad(
            "var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead); var n = 0; foreach (ObjectId id in lt) n++; " +
            "var sel = ed.SelectAll(); return new { n, selected = sel.Status == PromptStatus.OK ? sel.Value.Count : 0, mm = units.ToMm(1) + args.Double(\"x\") };");
        Assert.Empty(good!);

        // The guard, not the compiler, is what keeps prompts out: the wrapper must not hide that.
        Assert.Contains(ScriptGuard.Check("var p = ed.GetPoint(\"pick\"); return p.Value;", GuardProfile.Autocad), d => d.Message.Contains("GetPoint"));
    }

    private static bool ToolValidatorIsReserved(string name) => ToolValidator.IsReserved(name, AutocadHostProfile.Instance);

    private static IEnumerable<string> Strings(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => [element.GetString()!],
        JsonValueKind.Object => element.EnumerateObject().SelectMany(p => Strings(p.Value)),
        JsonValueKind.Array => element.EnumerateArray().SelectMany(Strings),
        _ => [],
    };

    /// <summary>
    ///     Wraps a script body in a class with the bridge's globals and compiles it against the AutoCAD 2026
    ///     assemblies from the NuGet cache (metadata only — they are mixed-mode and never loaded) plus the
    ///     AEC engine the bridge references (HPAutoCad.Aec). Null when the cache lacks them.
    /// </summary>
    internal static string[]? CompileProbe(string code) => CompileAgainstAutocad(code);
    private static string[]? CompileAgainstAutocad(string code)
    {
        var acMgd = FindAutocadReference("autocad.net", "AcMgd.dll");
        var acCoreMgd = FindAutocadReference("autocad.net.core", "AcCoreMgd.dll");
        var acDbMgd = FindAutocadReference("autocad.net.model", "AcDbMgd.dll");
        if (acMgd is null || acCoreMgd is null || acDbMgd is null) return null;

        var usings = string.Join(Environment.NewLine, HostScriptContracts.AutocadImports.Select(ns => $"using {ns};"));
        var source = $$"""
            {{usings}}

            public sealed class SeedHost
            {
                public Document doc;
                public Database db;
                public Editor ed;
                public DocumentCollection app;
                public Transaction tr;
                public ScriptUnits units;
                public System.Threading.CancellationToken ct;
                public Action<string> log;
                public Action<int, int, string> progress;
                public ScriptArgs args;

                public object Run()
                {
            #line 1 "code.cs"
            {{code}}
                }
            }
            """;

        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(p => Path.GetFileName(p) is var f && (f.StartsWith("System.", StringComparison.Ordinal) || f is "netstandard.dll" or "mscorlib.dll"))
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .Concat(
            [
                MetadataReference.CreateFromFile(acMgd),
                MetadataReference.CreateFromFile(acCoreMgd),
                MetadataReference.CreateFromFile(acDbMgd),
                MetadataReference.CreateFromFile(typeof(ScriptArgs).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(HPAutoCad.Aec.AecTools).Assembly.Location),
            ]);

        var compilation = CSharpCompilation.Create("seed_check",
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest))],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Disable));

        return compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => $"{d.Location.GetLineSpan().StartLinePosition.Line + 1}:{d.Location.GetLineSpan().StartLinePosition.Character + 1} {d.Id} {d.GetMessage()}")
            .ToArray();
    }

    private static string? FindAutocadReference(string package, string file)
    {
        var root = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (string.IsNullOrEmpty(root)) root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        var path = Path.Combine(root, package, PackageVersion, "lib", "net8.0", file);
        return File.Exists(path) ? path : null;
    }

    [Fact]
    public void Architecture_seeds_share_the_detection_block_so_room_ids_agree()
    {
        string[] tools = ["arch_detect_rooms", "arch_room_boundary_check", "arch_create_room_tags", "arch_generate_area_schedule", "arch_auto_dimension_plan"];
        foreach (var name in tools)
        {
            var seed = LoadSeeds().Single(s => s.Name == name);
            var properties = seed.Tool.GetProperty("inputSchema").GetProperty("properties");
            foreach (var key in new[] { "filter", "ruleSet", "tolerance", "detection", "maxCandidates" })
                Assert.True(properties.TryGetProperty(key, out _), $"{name} lacks {key}");
            var detection = properties.GetProperty("detection").GetProperty("properties").EnumerateObject().Select(p => p.Name).Order().ToArray();
            Assert.Equal(HPAutoCad.Aec.AecTools.DetectionKeys.Order(), detection);
        }
    }

    [Fact]
    public void Every_write_seed_can_be_recorded_into_a_change_set_and_the_replay_table_matches()
    {
        // A recorded call replays through WriteToolTable, which reads the arguments as the seed's shim does: the two lists must be the same set,
        // and every such seed declares changeSetId (the analyzer pin proves the shim reads it).
        var writeSeeds = LoadSeeds().Where(s => s.Tool.GetProperty("transaction").GetString() == "auto" && s.Code.Contains("AecTools.") && s.Name is not ("commit_change_set" or "rollback_change_set")).ToArray();
        Assert.Equal(HPAutoCad.Aec.Cad.WriteToolTable.Names, writeSeeds.Select(s => s.Name).Order(StringComparer.Ordinal).ToArray());
        foreach (var seed in writeSeeds)
        {
            Assert.True(seed.Tool.GetProperty("inputSchema").GetProperty("properties").TryGetProperty("changeSetId", out _), $"{seed.Name} lacks changeSetId");
            Assert.Contains("changeSetId", seed.Code);
            Assert.Contains("changeSetId, args);", seed.Code);
        }

        // a replay abort is the set's content — the caller's — so it must never count against commit_change_set's stability
        Assert.Contains("ArgumentException", LoadSeeds().Single(s => s.Name == "commit_change_set").Tool.GetProperty("description").GetString());
        foreach (var name in new[] { "begin_change_set", "rollback_change_set" })
            Assert.Contains("keep", LoadSeeds().Single(s => s.Name == name).Tool.GetProperty("description").GetString());
    }

    [Fact]
    public void Coordination_seeds_document_only_real_aec_types_and_share_the_set_shape()
    {
        // The AI reads the type list off the schema: every name there must be one ParseTypes accepts, and both tools take the same {filter, aecTypes} set.
        foreach (var (name, sets) in new[] { ("aec_clash_check", new[] { "setA", "setB" }), ("aec_create_opening_requests", new[] { "routes", "hosts" }) })
        {
            var properties = LoadSeeds().Single(s => s.Name == name).Tool.GetProperty("inputSchema").GetProperty("properties");
            foreach (var set in sets)
            {
                var setProperties = properties.GetProperty(set).GetProperty("properties");
                Assert.Equal(["aecTypes", "filter"], setProperties.EnumerateObject().Select(p => p.Name).Order());
                var description = setProperties.GetProperty("aecTypes").GetProperty("description").GetString()!;
                var listed = description[(description.IndexOf('(') + 1)..description.IndexOf('…')].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                Assert.True(listed.Length >= 10, $"{name}.{set}: the type list is short");
                Assert.All(listed, t => Assert.Contains(t, HPAutoCad.Aec.Classification.AecType.All));
            }
        }
    }

    [Fact]
    public void Mep_seeds_share_the_detection_block()
    {
        foreach (var name in new[] { "mep_detect_network", "mep_connectivity_check", "mep_endpoint_check" })
        {
            var properties = LoadSeeds().Single(s => s.Name == name).Tool.GetProperty("inputSchema").GetProperty("properties");
            foreach (var key in new[] { "filter", "ruleSet", "tolerance", "detection", "maxCandidates" })
                Assert.True(properties.TryGetProperty(key, out _), $"{name} lacks {key}");
            var detection = properties.GetProperty("detection").GetProperty("properties").EnumerateObject().Select(p => p.Name).Order().ToArray();
            Assert.Equal(HPAutoCad.Aec.AecTools.MepDetectionKeys.Order(), detection);
        }
    }
}
