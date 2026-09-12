using System.Reflection;
using System.Text;
using System.Text.Json;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     Every seed tool shipped inside the server assembly: well-formed record, guard-clean code that reads
///     only the parameters its schema declares, and — when the Revit API reference assemblies are in the
///     NuGet cache — code that compiles against Revit 2026 without a Revit process (metadata only).
/// </summary>
public sealed class SeedLibraryTests
{
    private const int MaxCodeBytes = 32 * 1024;
    private static readonly string[] Modes = ["auto", "manual", "none"];

    public sealed record Seed(string Category, string Name, JsonElement Tool, string Code, JsonElement Examples);

    public static IEnumerable<object[]> Seeds() => LoadSeeds().Select(s => new object[] { s.Category + "/" + s.Name });

    private static readonly Lazy<IReadOnlyList<Seed>> Cache = new(LoadSeedsCore);

    private static IReadOnlyList<Seed> LoadSeeds() => Cache.Value;

    private static IReadOnlyList<Seed> LoadSeedsCore()
    {
        var assembly = typeof(Tools.ExecuteRevitCodeTool).Assembly;
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
    public void All_seeds_are_embedded()
    {
        var seeds = LoadSeeds();
        Assert.True(seeds.Count >= 8, "expected at least the query seeds, got " + seeds.Count);
        Assert.Equal(seeds.Count, seeds.Select(s => s.Name).Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Seed_record_is_well_formed(string key)
    {
        var seed = Get(key);
        var tool = seed.Tool;

        Assert.Equal(seed.Name, tool.GetProperty("name").GetString());
        Assert.Equal(seed.Category, tool.GetProperty("category").GetString());
        Assert.Matches("^[a-z][a-z0-9_]{2,63}$", seed.Name);
        Assert.False(string.IsNullOrWhiteSpace(tool.GetProperty("description").GetString()));
        Assert.Contains(tool.GetProperty("transaction").GetString(), Modes);
        Assert.InRange(tool.GetProperty("timeoutSeconds").GetInt32(), 5, 120);
        Assert.Equal("published", tool.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Object, tool.GetProperty("inputSchema").GetProperty("properties").ValueKind);
        Assert.True(Encoding.UTF8.GetByteCount(seed.Code) < MaxCodeBytes, "code too large");

        var properties = tool.GetProperty("inputSchema").GetProperty("properties").EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var required = tool.GetProperty("inputSchema").TryGetProperty("required", out var r) ? r.EnumerateArray().Select(e => e.GetString()!).ToArray() : [];
        Assert.All(required, name => Assert.Contains(name, properties));

        Assert.True(seed.Examples.GetArrayLength() >= 2, "need at least two examples");
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
    public void Seed_code_passes_guard_and_reads_only_declared_args(string key)
    {
        var seed = Get(key);

        var guard = ScriptGuard.Check(seed.Code);
        Assert.True(guard.Count == 0, string.Join("; ", guard.Select(g => $"{g.Line}:{g.Column} {g.Message}")));

        var facts = ScriptAnalyzer.Analyze(seed.Code);
        var declared = seed.Tool.GetProperty("inputSchema").GetProperty("properties").EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var read = facts.ArgKeys.Select(a => a.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.True(read.IsSubsetOf(declared), "code reads undeclared args: " + string.Join(", ", read.Except(declared)));
        Assert.True(declared.IsSubsetOf(read), "schema declares unused args: " + string.Join(", ", declared.Except(read)));

        var mode = seed.Tool.GetProperty("transaction").GetString();
        Assert.Equal(mode == "manual", facts.UsesTransaction);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Seed_code_compiles_against_revit_api(string key)
    {
        var seed = Get(key);
        var errors = CompileAgainstRevit(seed.Code);
        Assert.SkipWhen(errors is null, "Revit 2026 reference assemblies not in the NuGet cache");
        Assert.True(errors!.Length == 0, seed.Name + Environment.NewLine + string.Join(Environment.NewLine, errors));
    }

    [Fact]
    public void Compile_check_rejects_api_misuse_and_accepts_real_api()
    {
        var bad = CompileAgainstRevit("return doc.NoSuchMember(args.Int(\"n\"));");
        Assert.SkipWhen(bad is null, "Revit 2026 reference assemblies not in the NuGet cache");
        Assert.Contains(bad!, e => e.Contains("CS1061") && e.Contains("NoSuchMember"));

        var good = CompileAgainstRevit("var n = new FilteredElementCollector(doc).OfClass(typeof(Level)).GetElementCount(); return n + args.Int(\"n\");");
        Assert.Empty(good!);
    }

    /// <summary>Wraps a script body in a class with the bridge's globals and compiles it against the Revit reference assemblies (metadata only). Null when the assemblies are unavailable.</summary>
    private static string[]? CompileAgainstRevit(string code)
    {
        var revitApi = FindRevitReference("nice3point.revit.api.revitapi", "RevitAPI.dll");
        var revitApiUi = FindRevitReference("nice3point.revit.api.revitapiui", "RevitAPIUI.dll");
        if (revitApi is null || revitApiUi is null) return null;

        var source = $$"""
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using Autodesk.Revit.DB;
            using Autodesk.Revit.UI;
            using Autodesk.Revit.DB.Structure;
            // no `using HPRebar.McpBridge.Core.Scripting` here: the wrapper mirrors the bridge's default imports (Application.ScriptImports)

            public sealed class SeedHost
            {
                public Document doc;
                public UIDocument uidoc;
                public Autodesk.Revit.ApplicationServices.Application app;
                public UIApplication uiapp;
                public System.Threading.CancellationToken ct;
                public Action<string> log;
                public Action<int, int, string> progress;
                public HPRebar.McpBridge.Core.Scripting.ScriptArgs args;

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
                MetadataReference.CreateFromFile(revitApi),
                MetadataReference.CreateFromFile(revitApiUi),
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

    private static string? FindRevitReference(string package, string file)
    {
        var root = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (string.IsNullOrEmpty(root)) root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        var packageDir = Path.Combine(root, package);
        if (!Directory.Exists(packageDir)) return null;

        return Directory.GetDirectories(packageDir, "2026.*")
            .OrderByDescending(d => d, StringComparer.OrdinalIgnoreCase)
            .Select(d => Path.Combine(d, "ref", "net8.0-windows7.0", file))
            .FirstOrDefault(File.Exists);
    }
}
