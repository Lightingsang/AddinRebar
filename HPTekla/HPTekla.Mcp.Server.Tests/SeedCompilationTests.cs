using System.Text;
using System.Text.Json;
using HPRebar.Mcp.Contracts;
using HPRebar.McpBridge.Core.Scripting;
using HPTekla.Mcp.Server.Hosts.Tekla;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Win32;
using Xunit;

namespace HPTekla.Mcp.Server.Tests;

/// <summary>
///     Every seed compiles against the installed Tekla Structures 2025 assemblies with the bridge's exact imports and globals.
///     Gracefully and visibly skipped on machines without Trimble Tekla Structures 2025.0 installed.
/// </summary>
public sealed class SeedCompilationTests
{
    private const string Version = "2025.0";
    private const string EnvVar = "HPTEKLA_TEKLA_DIR";

    public sealed record Seed(string Category, string Name, JsonElement Tool, string Code, JsonElement Examples);

    private static readonly Lazy<IReadOnlyList<Seed>> Cache = new(LoadSeedsCore);

    public static IReadOnlyList<Seed> LoadSeeds() => Cache.Value;

    public static IEnumerable<object[]> Seeds() => LoadSeeds().Select(s => new object[] { s.Category + "/" + s.Name });

    public static Seed Get(string key) => LoadSeeds().Single(s => s.Category + "/" + s.Name == key);

    private static IReadOnlyList<Seed> LoadSeedsCore()
    {
        var assembly = typeof(TeklaHostProfile).Assembly;
        var names = assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith("SeedLibrary/", StringComparison.Ordinal))
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

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Seed_code_compiles_against_the_tekla_assemblies(string key)
    {
        var seed = Get(key);
        var compiled = Compile(seed.Code);
        Assert.SkipWhen(compiled is null, "Trimble Tekla Structures 2025 not installed (Tekla Open API assemblies not found)");

        Assert.True(compiled!.Value.errors.Length == 0,
            seed.Name + Environment.NewLine + string.Join(Environment.NewLine, compiled.Value.errors));
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Seed_transaction_mode_and_tags_match_operation_intent(string key)
    {
        var seed = Get(key);
        var tool = seed.Tool;
        var mode = tool.GetProperty("transaction").GetString();
        var isDestructive = tool.TryGetProperty("tags", out var tags) &&
                            tags.EnumerateArray().Any(t => t.GetString() == "destructive");

        if (seed.Name == "export_ifc")
        {
            Assert.True(isDestructive, "export_ifc must declare 'destructive' tag");
            Assert.Equal("auto", mode);
        }
        else if (seed.Name.StartsWith("create_") || seed.Name.StartsWith("modify_"))
        {
            Assert.Equal("auto", mode);
            Assert.False(isDestructive, "Standard write seed should not be marked destructive");
        }
        else
        {
            Assert.Equal("none", mode);
            Assert.False(isDestructive, "Read-only seed must have transaction: none and no destructive tag");
        }
    }

    [Fact]
    public void Compile_check_rejects_api_misuse_and_accepts_the_real_api()
    {
        var bad = Compile("return model.NoSuchMember(args.Int(\"n\"));");
        Assert.SkipWhen(bad is null, "Trimble Tekla Structures 2025 not installed (Tekla Open API assemblies not found)");
        Assert.Contains(bad!.Value.errors, e => e.Contains("CS1061") && e.Contains("NoSuchMember"));

        var good = Compile("var info = model.GetInfo(); return new { name = info.ModelName, x = args.Double(\"x\") };");
        Assert.Empty(good!.Value.errors);
    }

    [Fact]
    public void Guard_check_rejects_tekla_dialogs_quit_and_process_spawn()
    {
        var violations = ScriptGuard.Check("MessageBox.Show(\"hi\"); return 1;", GuardProfile.Tekla);
        Assert.NotEmpty(violations);
        Assert.Contains(violations, d => d.Message.Contains("MessageBox", StringComparison.OrdinalIgnoreCase));

        var pickerViolations = ScriptGuard.Check("var p = new Picker(); p.PickObject(); return 1;", GuardProfile.Tekla);
        Assert.NotEmpty(pickerViolations);

        var procViolations = ScriptGuard.Check("System.Diagnostics.Process.Start(\"calc.exe\"); return 1;", GuardProfile.Tekla);
        Assert.NotEmpty(procViolations);
    }

    /// <summary>The bridge's script environment as a class: usings = the Tekla imports, fields = the Tekla globals.</summary>
    private static (CSharpCompilation compilation, string[] errors)? Compile(string code)
    {
        var teklaDir = FindTeklaBinDir();
        if (teklaDir is null) return null;

        var usings = string.Join(Environment.NewLine, HostScriptContracts.TeklaImports.Select(ns => $"using {ns};"));
        var source = $$"""
            {{usings}}

            public sealed class SeedHost
            {
                public Tekla.Structures.Model.Model model;
                public Tekla.Structures.Model.UI.ModelObjectSelector selector;
                public System.Threading.CancellationToken ct;
                public Action<string> log;
                public Action<int, int?, string?> progress;
                public ScriptArgs args;

                public object Run()
                {
            #line 1 "code.cs"
            {{code}}
                }
            }
            """;

        var teklaDlls = Directory.GetFiles(teklaDir, "Tekla.Structures*.dll")
            .Where(f =>
            {
                var name = Path.GetFileName(f);
                if (name.EndsWith(".resources.dll", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains(".Native.", StringComparison.OrdinalIgnoreCase))
                    return false;
                try
                {
                    System.Reflection.AssemblyName.GetAssemblyName(f);
                    return true;
                }
                catch
                {
                    return false;
                }
            })
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p));

        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(p => Path.GetFileName(p) is var f && (f.StartsWith("System.", StringComparison.Ordinal) || f is "netstandard.dll" or "mscorlib.dll"))
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .Concat(teklaDlls)
            .Concat([
                MetadataReference.CreateFromFile(typeof(ScriptArgs).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(JsonElement).Assembly.Location)
            ]);

        var compilation = CSharpCompilation.Create("seed_check",
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest))],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Disable));

        var errors = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => $"{d.Location.GetLineSpan().StartLinePosition.Line + 1}:{d.Location.GetLineSpan().StartLinePosition.Character + 1} {d.Id} {d.GetMessage()}")
            .ToArray();

        return (compilation, errors);
    }

    /// <summary>
    ///     Resolves the Tekla Structures bin directory containing Tekla.Structures.Model.dll:
    ///     1. HPTEKLA_TEKLA_DIR env var
    ///     2. HKLM\SOFTWARE\Trimble\Tekla Structures\2025.0\setup -> MainDir + 2025.0\bin
    ///     3. %ProgramW6432%\Tekla Structures\2025.0\bin\
    ///     4. C:\Program Files\Tekla Structures\2025.0\bin\
    /// </summary>
    private static string? FindTeklaBinDir()
    {
        var candidates = new List<string?> { Environment.GetEnvironmentVariable(EnvVar) };

        if (OperatingSystem.IsWindows())
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Trimble\Tekla Structures\{Version}\setup");
                if (key?.GetValue("MainDir") is string mainDir)
                {
                    candidates.Add(Path.Combine(mainDir, Version, "bin"));
                }
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or IOException)
            {
                // Registry read failure - fallback to candidate paths
            }
        }

        var progFiles = Environment.GetEnvironmentVariable("ProgramW6432") ?? @"C:\Program Files";
        candidates.Add(Path.Combine(progFiles, "Tekla Structures", Version, "bin"));
        candidates.Add(@"C:\Program Files\Tekla Structures\2025.0\bin");

        return candidates
            .Where(c => !string.IsNullOrEmpty(c))
            .FirstOrDefault(d => Directory.Exists(d) && File.Exists(Path.Combine(d!, "Tekla.Structures.Model.dll")));
    }
}
