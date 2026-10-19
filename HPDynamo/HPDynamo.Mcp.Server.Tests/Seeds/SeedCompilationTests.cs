namespace HPDynamo.Mcp.Server.Tests.Seeds;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using HPDynamo.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

/// <summary>
///     Verifies requirement R5: All 12 published seed tools compile cleanly against standard
///     platform assemblies and Dynamo/Revit reference assemblies using Roslyn CSharpCompilation.
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
        var assembly = typeof(DynamoHostProfile).Assembly;
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
    public void AllSeeds_CompileCleanlyAgainstDynamoAndRevitReferences(string key)
    {
        var seed = Get(key);
        var compiled = Compile(seed.Code);
        Assert.SkipWhen(compiled is null, "Autodesk Revit 2026 / Dynamo reference assemblies not found on this machine.");

        Assert.True(
            compiled!.Value.errors.Length == 0,
            $"Seed '{seed.Name}' has compilation errors:{Environment.NewLine}{string.Join(Environment.NewLine, compiled.Value.errors)}");
    }

    [Fact]
    public void CompileCheck_RejectsApiMisuseAndAcceptsRealApi()
    {
        var bad = Compile("return workspace.NonExistentProperty(args.Int(\"n\"));");
        Assert.SkipWhen(bad is null, "Autodesk Revit 2026 / Dynamo reference assemblies not found on this machine.");
        Assert.Contains(bad!.Value.errors, e => e.Contains("CS1061"));

        var good = Compile("int nodeCount = workspace != null ? workspace.Nodes.Count() : 0; return new { nodeCount, x = args.Double(\"x\") };");
        Assert.Empty(good!.Value.errors);
    }

    private static (CSharpCompilation compilation, string[] errors)? Compile(string code)
    {
        var hostRefs = ResolveHostReferences();
        if (hostRefs is null) return null;

        var source = $$"""
            using System;
            using System.IO;
            using System.Linq;
            using System.Collections.Generic;
            using System.Text.Json;
            using Autodesk.DesignScript.Geometry;
            using Dynamo.Graph.Nodes;
            using Dynamo.Graph.Workspaces;
            using Dynamo.Models;
            using Dynamo.ViewModels;
            using Dynamo.Applications;
            using Revit.Elements;
            using HPRebar.McpBridge.Core.Scripting;
            using Autodesk.Revit.DB;
            using Autodesk.Revit.UI;

            public sealed class SeedHost
            {
                public object dynamo;
                public WorkspaceModel workspace;
                public Document doc;
                public UIDocument uidoc;
                public UIApplication uiapp;
                public System.Threading.CancellationToken ct;
                public Action<string> log;
                public Action<int, int?, string?> progress;
                public ScriptArgs args;
                public bool dryRun;
                public SeedHost globals => this;

                public bool RunAndWait(Action runAction = null, TimeSpan? timeout = null) => true;

                public object Run()
                {
            #line 1 "code.cs"
            {{code}}
                }
            }
            """;

        var trusted = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(p => Path.GetFileName(p) is var f && (f.StartsWith("System.", StringComparison.Ordinal) || f is "netstandard.dll" or "mscorlib.dll"))
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p));

        var sharedRefs = new[]
        {
            MetadataReference.CreateFromFile(typeof(ScriptArgs).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(JsonElement).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(PipeNaming).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DynamoHostProfile).Assembly.Location)
        };

        var allReferences = trusted.Concat(sharedRefs).Concat(hostRefs);

        var compilation = CSharpCompilation.Create(
            "dynamo_seed_check",
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest))],
            allReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Disable));

        var errors = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => $"{d.Location.GetLineSpan().StartLinePosition.Line + 1}:{d.Location.GetLineSpan().StartLinePosition.Character + 1} {d.Id} {d.GetMessage()}")
            .ToArray();

        return (compilation, errors);
    }

    private static List<MetadataReference>? ResolveHostReferences()
    {
        var dirs = GetSearchDirectories();
        string[] requiredDlls =
        [
            "RevitAPI.dll", "RevitAPIUI.dll", "DynamoCore.dll",
            "DynamoCoreWpf.dll", "DynamoApplications.dll", "RevitNodes.dll"
        ];

        var resolvedPaths = new List<string>();
        foreach (var dll in requiredDlls)
        {
            var found = dirs.Select(d => Path.Combine(d, dll)).FirstOrDefault(File.Exists);
            if (found == null) return null;
            resolvedPaths.Add(found);
        }

        string[] optionalDlls = ["ProtoCore.dll", "ProtoGeometry.dll", "DynamoServices.dll", "DynamoRevitDS.dll", "DesignScriptBuiltin.dll"];
        foreach (var dll in optionalDlls)
        {
            var found = dirs.Select(d => Path.Combine(d, dll)).FirstOrDefault(File.Exists);
            if (found != null && !resolvedPaths.Contains(found))
            {
                resolvedPaths.Add(found);
            }
        }

        return resolvedPaths.Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)).ToList();
    }

    private static string[] GetSearchDirectories()
    {
        var list = new List<string>();
        var envRevit = Environment.GetEnvironmentVariable("HPREBAR_REVIT_DIR");
        if (!string.IsNullOrWhiteSpace(envRevit) && Directory.Exists(envRevit)) list.Add(envRevit);

        var envDynamo = Environment.GetEnvironmentVariable("HPDYNAMO_DYNAMO_DIR");
        if (!string.IsNullOrWhiteSpace(envDynamo) && Directory.Exists(envDynamo))
        {
            list.Add(envDynamo);
            list.Add(Path.Combine(envDynamo, "Revit"));
        }

        var progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var revitBase = Path.Combine(progFiles, "Autodesk", "Revit 2026");
        list.Add(revitBase);
        list.Add(Path.Combine(revitBase, "AddIns", "DynamoForRevit"));
        list.Add(Path.Combine(revitBase, "AddIns", "DynamoForRevit", "Revit"));

        return list.Where(Directory.Exists).Distinct().ToArray();
    }
}
