using HPRebar.Mcp.Contracts;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HPCivil3d.Mcp.Server.Tests;

/// <summary>
///     Compiles a seed body the way the bridge does — the same usings (<see cref="HostScriptContracts.Civil3dImports"/>)
///     and the same eleven globals, `civil` included — against the AutoCAD 2026 assemblies from the NuGet cache plus the
///     installed Civil 3D assemblies, as metadata only (nothing is loaded, no acad.exe). Null when either set is missing,
///     so the caller can skip with a visible reason.
/// </summary>
internal static class SeedCompileProbe
{
    public const string PackageVersion = "25.1.0";

    public static string SkipReason() => Civil3dApiLocator.Assemblies() is null ? Civil3dApiLocator.SkipReason : $"AutoCAD.NET {PackageVersion} assemblies not in the NuGet cache";

    public static string[]? Compile(string code)
    {
        var acMgd = FindAutocadReference("autocad.net", "AcMgd.dll");
        var acCoreMgd = FindAutocadReference("autocad.net.core", "AcCoreMgd.dll");
        var acDbMgd = FindAutocadReference("autocad.net.model", "AcDbMgd.dll");
        var civil = Civil3dApiLocator.Assemblies();
        if (acMgd is null || acCoreMgd is null || acDbMgd is null || civil is null) return null;

        var usings = string.Join(Environment.NewLine, HostScriptContracts.Civil3dImports.Select(ns => $"using {ns};"));
        var source = $$"""
            {{usings}}

            public sealed class SeedHost
            {
                public Document doc;
                public Database db;
                public Editor ed;
                public DocumentCollection app;
                public Transaction tr;
                public CivilDocument civil;
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
            .Concat([MetadataReference.CreateFromFile(acMgd), MetadataReference.CreateFromFile(acCoreMgd), MetadataReference.CreateFromFile(acDbMgd), MetadataReference.CreateFromFile(typeof(ScriptArgs).Assembly.Location)])
            .Concat(civil.Select(path => (MetadataReference)MetadataReference.CreateFromFile(path)));

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
