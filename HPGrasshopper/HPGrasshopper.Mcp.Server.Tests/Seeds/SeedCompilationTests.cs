namespace HPGrasshopper.Mcp.Server.Tests.Seeds;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using HPGrasshopper.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

/// <summary>
///     Verifies all 12 published seed tools compile cleanly against standard
///     platform assemblies and Grasshopper/Revit reference assemblies using Roslyn CSharpCompilation.
///     Gracefully skips when host reference assemblies are not present in the runtime test environment.
/// </summary>
public sealed class SeedCompilationTests
{
    public sealed record Seed(string Category, string Name, JsonElement Tool, string Code, JsonElement Examples);

    private static readonly Lazy<IReadOnlyList<Seed>> Cache = new(LoadSeedsCore);

    public static IEnumerable<object[]> Seeds() => Cache.Value.Select(s => new object[] { s.Category + "/" + s.Name });

    public static Seed Get(string key) => Cache.Value.Single(s => s.Category + "/" + s.Name == key);

    private static IReadOnlyList<Seed> LoadSeedsCore()
    {
        var assembly = typeof(GrasshopperHostProfile).Assembly;
        var names = assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith("SeedLibrary/", StringComparison.OrdinalIgnoreCase))
            .ToArray();

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
                var parts = g.Key.Split('/');
                string Of(string file) => names.First(n => n.Replace('\\', '/') == g.Key + "/" + file);
                return new Seed(
                    parts[1],
                    parts[2],
                    JsonSerializer.Deserialize<JsonElement>(Read(Of("tool.json"))),
                    Read(Of("code.cs")),
                    JsonSerializer.Deserialize<JsonElement>(Read(Of("examples.json"))));
            })
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Name)
            .ToArray();
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void AllSeeds_CompileCleanlyAgainstGrasshopperAndRevitReferences(string key)
    {
        var seed = Get(key);
        var compiled = Compile(seed.Code);
        Assert.SkipWhen(compiled is null, "Autodesk Revit 2026 / Rhino 8 reference assemblies not found on this machine.");

        Assert.True(
            compiled!.Value.errors.Length == 0,
            $"Seed '{seed.Name}' has compilation errors:{Environment.NewLine}{string.Join(Environment.NewLine, compiled.Value.errors)}");
    }

    private static (CSharpCompilation compilation, string[] errors)? Compile(string code)
    {
        var hostRefs = ResolveHostReferences();
        if (hostRefs is null) return null;

        var source = BuildSeedSource(code);
        var trusted = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(p => Path.GetFileName(p) is var f && (f.StartsWith("System.", StringComparison.Ordinal) || f is "netstandard.dll" or "mscorlib.dll"))
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p));

        var sharedRefs = new[]
        {
            MetadataReference.CreateFromFile(typeof(ScriptArgs).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(HostScriptContracts).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(JsonElement).Assembly.Location),
        };

        var allReferences = trusted.Concat(sharedRefs).Concat(hostRefs).ToList();
        var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        var compilation = CSharpCompilation.Create(
            "SeedScriptCompilation_" + Guid.NewGuid().ToString("N"),
            [syntaxTree],
            allReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var diagnostics = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => $"{d.Id}: {d.GetMessage()}")
            .ToArray();

        return (compilation, diagnostics);
    }

    private static string BuildSeedSource(string code) => $$"""
        using System;
        using System.IO;
        using System.Linq;
        using System.Collections.Generic;
        using System.Text.Json;
        using HPRebar.McpBridge.Core.Scripting;
        using Autodesk.Revit.DB;
        using Autodesk.Revit.UI;

        public sealed class SeedHost
        {
            public object doc;
            public object canvas;
            public object rhinoDoc;
            public Document revitDoc;
            public UIApplication uiApp;
            public System.Threading.CancellationToken ct;
            public Action<string> log;
            public Action<int, int?, string?> progress;
            public ScriptArgs args;
            public bool dryRun;

            public object Run()
            {
        #line 1 "code.cs"
        {{code}}
            }
        }
        """;

    private static List<MetadataReference>? ResolveHostReferences()
    {
        var revitDir = Environment.GetEnvironmentVariable("HPREBAR_REVIT_DIR")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Autodesk", "Revit 2026");

        var revitApiDll = Path.Combine(revitDir, "RevitAPI.dll");
        var revitApiUiDll = Path.Combine(revitDir, "RevitAPIUI.dll");

        if (!File.Exists(revitApiDll) || !File.Exists(revitApiUiDll))
        {
            return null;
        }

        var list = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(revitApiDll),
            MetadataReference.CreateFromFile(revitApiUiDll)
        };

        AddRhinoReferences(list);
        return list;
    }

    private static void AddRhinoReferences(List<MetadataReference> list)
    {
        var rhinoDir = Environment.GetEnvironmentVariable("HPGRASSHOPPER_RHINO_DIR")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rhino 8");
        var rhinoCommon = Path.Combine(rhinoDir, "System", "netcore", "RhinoCommon.dll");
        if (File.Exists(rhinoCommon)) list.Add(MetadataReference.CreateFromFile(rhinoCommon));

        var ghDir = Path.Combine(rhinoDir, "Plug-ins", "Grasshopper");
        var ghDll = Path.Combine(ghDir, "Grasshopper.dll");
        var ghIo = Path.Combine(ghDir, "GH_IO.dll");
        if (File.Exists(ghDll)) list.Add(MetadataReference.CreateFromFile(ghDll));
        if (File.Exists(ghIo)) list.Add(MetadataReference.CreateFromFile(ghIo));

        var rirDir = Environment.GetEnvironmentVariable("HPGRASSHOPPER_RHINOINSIDE_DIR")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Autodesk", "Revit", "Addins", "2026", "RhinoInside.Revit", "R8");
        var rirDll = Path.Combine(rirDir, "RhinoInside.Revit.dll");
        if (File.Exists(rirDll)) list.Add(MetadataReference.CreateFromFile(rirDll));
    }
}
