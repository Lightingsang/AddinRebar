using HPRebar.Mcp.Contracts;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Win32;
using Xunit;

namespace HPSap2000.Mcp.Server.Tests;

/// <summary>
///     Every seed compiles against the installed SAP2000v1.dll with the bridge's exact imports and globals, and the
///     tier its record declares matches what the bridge's tier table would decide for the members it binds
///     (read-only ⇔ `transaction: none`, destructive ⇔ the `destructive` tag). The wrapper is metadata only —
///     nothing of SAP2000 is loaded or started. Skipped, visibly, on a machine without SAP2000.
/// </summary>
public sealed class SeedLibraryCompileTests
{
    private const string WrapperFile = "SAP2000v1.dll";
    private const string Clsid = "{B6B21850-FB75-41DE-85EC-BC9DBEC69BD3}";

    public static IEnumerable<object[]> Seeds() => SeedLibraryStructureTests.Seeds();

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Seed_code_compiles_against_the_sap2000_wrapper(string key)
    {
        var seed = SeedLibraryStructureTests.Get(key);
        var compiled = Compile(seed.Code);
        Assert.SkipWhen(compiled is null, "SAP2000 not installed (SAP2000v1.dll not found)");

        Assert.True(compiled!.Value.errors.Length == 0, seed.Name + Environment.NewLine + string.Join(Environment.NewLine, compiled.Value.errors));
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Seed_tier_from_the_bridges_table_matches_its_record(string key)
    {
        var seed = SeedLibraryStructureTests.Get(key);
        var compiled = Compile(seed.Code);
        Assert.SkipWhen(compiled is null, "SAP2000 not installed (SAP2000v1.dll not found)");
        Assert.Empty(compiled!.Value.errors);

        var table = LoadTierTable();
        var members = BoundMembers(compiled.Value.compilation).ToArray();
        Assert.NotEmpty(members);
        var unknown = members.Where(m => !table.ContainsKey(m)).ToArray();
        Assert.True(unknown.Length == 0, "members not in the tier table (the bridge would refuse them as destructive): " + string.Join(", ", unknown));

        static int Rank(char tier) => tier switch { 'R' => 0, 'W' => 1, 'D' => 2, _ => 2 };
        var tier = members.Select(m => table[m]).MaxBy(Rank);
        var mode = seed.Tool.GetProperty("transaction").GetString();
        var destructive = seed.Tool.TryGetProperty("tags", out var tags) && tags.EnumerateArray().Any(t => t.GetString() == "destructive");

        Assert.Equal(mode == "none", tier == 'R');
        Assert.Equal(destructive, tier == 'D');
    }

    [Fact]
    public void Compile_check_rejects_api_misuse_and_accepts_the_real_api()
    {
        var bad = Compile("return sapModel.NoSuchMember(args.Int(\"n\"));");
        Assert.SkipWhen(bad is null, "SAP2000 not installed (SAP2000v1.dll not found)");
        Assert.Contains(bad!.Value.errors, e => e.Contains("CS1061") && e.Contains("NoSuchMember"));

        var good = Compile("int n = 0; string[] names = null; int ret = sapModel.FrameObj.GetNameList(ref n, ref names); return new { ret, n, m = units.Label, x = args.Double(\"x\") };");
        Assert.Empty(good!.Value.errors);

        Assert.Contains(ScriptGuard.Check("var h = new Helper(); return 1;", GuardProfile.Sap2000), d => d.Message.Contains("Helper"));
    }

    /// <summary>The bridge's script environment as a class: usings = the SAP2000 imports, fields = the SAP2000 globals.</summary>
    private static (CSharpCompilation compilation, string[] errors)? Compile(string code)
    {
        var wrapper = FindWrapper();
        if (wrapper is null) return null;

        var usings = string.Join(Environment.NewLine, HostScriptContracts.Sap2000Imports.Select(ns => $"using {ns};"));
        var source = $$"""
            {{usings}}

            public sealed class SeedHost
            {
                public cSapModel sapModel;
                public cOAPI sap;
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
            .Concat([MetadataReference.CreateFromFile(wrapper), MetadataReference.CreateFromFile(typeof(ScriptArgs).Assembly.Location)]);

        var compilation = CSharpCompilation.Create("seed_check",
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest))],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Disable));

        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => $"{d.Location.GetLineSpan().StartLinePosition.Line + 1}:{d.Location.GetLineSpan().StartLinePosition.Character + 1} {d.Id} {d.GetMessage()}")
            .ToArray();
        return (compilation, errors);
    }

    private static IEnumerable<string> BoundMembers(CSharpCompilation compilation)
    {
        var tree = compilation.SyntaxTrees.Single();
        var model = compilation.GetSemanticModel(tree);
        foreach (var access in tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (model.GetSymbolInfo(access).Symbol is not IMethodSymbol method) continue;
            if (method.ContainingType.ContainingNamespace?.Name != "SAP2000v1") continue;
            yield return method.ContainingType.Name + "." + method.Name;
        }
    }

    private static Dictionary<string, char> LoadTierTable()
    {
        var dir = AppContext.BaseDirectory;
        string? path = null;
        for (var i = 0; i < 8 && dir is not null; i++)
        {
            var candidate = Path.Combine(dir, "HPSap2000.McpBridge", "Resources", "sap2000-oapi-tiers.txt");
            if (File.Exists(candidate)) { path = candidate; break; }
            dir = Path.GetDirectoryName(dir);
        }
        Assert.True(path is not null, "sap2000-oapi-tiers.txt not found above " + AppContext.BaseDirectory);

        return File.ReadLines(path!)
            .Where(l => l.Length > 0 && l[0] != '#')
            .Select(l => l.Split('\t'))
            .ToDictionary(p => p[0], p => p[1][0], StringComparer.Ordinal);
    }

    private static string? FindWrapper()
    {
        var candidates = new List<string?> { Environment.GetEnvironmentVariable("HPSAP2000_SAP2000_DIR") };
        if (OperatingSystem.IsWindows())
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\CLSID\" + Clsid + @"\LocalServer32");
                if (key?.GetValue(null) is string server) candidates.Add(Path.GetDirectoryName(server.Trim('"')));
            }
            catch (Exception exception) when (exception is System.Security.SecurityException or IOException) { /* no registration */ }
        }
        candidates.Add(Path.Combine(Environment.GetEnvironmentVariable("ProgramW6432") ?? @"C:\Program Files", "Computers and Structures", "SAP2000 27"));

        return candidates.Where(c => !string.IsNullOrEmpty(c)).Select(c => Path.Combine(c!, WrapperFile)).FirstOrDefault(File.Exists);
    }
}
