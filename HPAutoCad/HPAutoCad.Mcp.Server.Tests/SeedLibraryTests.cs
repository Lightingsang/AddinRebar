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
    public void All_twelve_seeds_are_embedded()
    {
        var seeds = LoadSeeds();
        Assert.Equal(12, seeds.Count);
        Assert.Equal(seeds.Count, seeds.Select(s => s.Name).Distinct().Count());
        Assert.Equal(6, seeds.Count(s => s.Tool.GetProperty("transaction").GetString() == "none"));
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
    ///     assemblies from the NuGet cache (metadata only — they are mixed-mode and never loaded). Null when
    ///     the cache lacks them.
    /// </summary>
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
}
