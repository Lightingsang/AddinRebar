using HPRebar.Mcp.Contracts;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Win32;
using Xunit;

namespace HPEtabs.Mcp.Server.Tests;

/// <summary>
///     Every seed compiles against the installed ETABSv1.dll with the bridge's exact imports and globals, and the
///     tier its record declares matches what the bridge's tier table would decide for the members it binds
///     (read-only ⇔ `transaction: none`, destructive ⇔ the `destructive` tag). The wrapper is metadata only —
///     nothing of ETABS is loaded or started. Skipped, visibly, on a machine without ETABS 22.
/// </summary>
public sealed class SeedLibraryCompileTests
{
    private const string WrapperFile = "ETABSv1.dll";
    private const string Clsid = "{e4f6d00f-51a5-4d65-a09a-ba00fcbf1f82}";

    public static IEnumerable<object[]> Seeds() => SeedLibraryStructureTests.Seeds();

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Seed_code_compiles_against_the_etabs_wrapper(string key)
    {
        var seed = SeedLibraryStructureTests.Get(key);
        var compiled = Compile(seed.Code);
        Assert.SkipWhen(compiled is null, "ETABS 22 not installed (ETABSv1.dll not found)");

        Assert.True(compiled!.Value.errors.Length == 0, seed.Name + Environment.NewLine + string.Join(Environment.NewLine, compiled.Value.errors));
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Seed_tier_from_the_bridges_table_matches_its_record(string key)
    {
        var seed = SeedLibraryStructureTests.Get(key);
        var compiled = Compile(seed.Code);
        Assert.SkipWhen(compiled is null, "ETABS 22 not installed (ETABSv1.dll not found)");
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
        Assert.SkipWhen(bad is null, "ETABS 22 not installed (ETABSv1.dll not found)");
        Assert.Contains(bad!.Value.errors, e => e.Contains("CS1061") && e.Contains("NoSuchMember"));

        var good = Compile("int n = 0; string[] names = null; int ret = sapModel.FrameObj.GetNameList(ref n, ref names); return new { ret, n, mm = units.Label, x = args.Double(\"x\") };");
        Assert.Empty(good!.Value.errors);

        // The guard, not the compiler, keeps the attachment API out: the wrapper must not hide that.
        Assert.Contains(ScriptGuard.Check("var h = new Helper(); return 1;", GuardProfile.Etabs), d => d.Message.Contains("Helper"));
    }

    /// <summary>The bridge's script environment as a class: usings = the ETABS imports, fields = the ETABS globals.</summary>
    private static (CSharpCompilation compilation, string[] errors)? Compile(string code)
    {
        var wrapper = FindWrapper();
        if (wrapper is null) return null;

        var usings = string.Join(Environment.NewLine, HostScriptContracts.EtabsImports.Select(ns => $"using {ns};"));
        var source = $$"""
            {{usings}}

            public sealed class SeedHost
            {
                public cSapModel sapModel;
                public cOAPI etabs;
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

    /// <summary>The bridge's rule in miniature: every method bound on an `ETABSv1` type, as `cInterface.Member`.</summary>
    private static IEnumerable<string> BoundMembers(CSharpCompilation compilation)
    {
        var tree = compilation.SyntaxTrees.Single();
        var model = compilation.GetSemanticModel(tree);
        foreach (var access in tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (model.GetSymbolInfo(access).Symbol is not IMethodSymbol method) continue;
            if (method.ContainingType.ContainingNamespace?.Name != "ETABSv1") continue;
            yield return method.ContainingType.Name + "." + method.Name;
        }
    }

    /// <summary>`Resources/etabs-oapi-tiers.txt` of the bridge project, read through the repo (the server never ships the table).</summary>
    private static Dictionary<string, char> LoadTierTable()
    {
        var dir = AppContext.BaseDirectory;
        string? path = null;
        for (var i = 0; i < 8 && dir is not null; i++)
        {
            var candidate = Path.Combine(dir, "HPEtabs.McpBridge", "Resources", "etabs-oapi-tiers.txt");
            if (File.Exists(candidate)) { path = candidate; break; }
            dir = Path.GetDirectoryName(dir);
        }
        Assert.True(path is not null, "etabs-oapi-tiers.txt not found above " + AppContext.BaseDirectory);

        return File.ReadLines(path!)
            .Where(l => l.Length > 0 && l[0] != '#')
            .Select(l => l.Split('\t'))
            .ToDictionary(p => p[0], p => p[1][0], StringComparer.Ordinal);
    }

    /// <summary>Env `HPETABS_ETABS_DIR` → the COM registration of ETABS 22 → the default Program Files folder; null when none has the wrapper.</summary>
    private static string? FindWrapper()
    {
        var candidates = new List<string?> { Environment.GetEnvironmentVariable("HPETABS_ETABS_DIR") };
        if (OperatingSystem.IsWindows())
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\CLSID\" + Clsid + @"\LocalServer32");
                if (key?.GetValue(null) is string server) candidates.Add(Path.GetDirectoryName(server.Trim('"')));
            }
            catch (Exception exception) when (exception is System.Security.SecurityException or IOException) { /* no registration */ }
        }
        candidates.Add(Path.Combine(Environment.GetEnvironmentVariable("ProgramW6432") ?? @"C:\Program Files", "Computers and Structures", "ETABS 22"));

        return candidates.Where(c => !string.IsNullOrEmpty(c)).Select(c => Path.Combine(c!, WrapperFile)).FirstOrDefault(File.Exists);
    }
}
