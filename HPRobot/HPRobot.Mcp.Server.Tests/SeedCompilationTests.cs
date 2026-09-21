using System.Text;
using System.Text.Json;
using HPRebar.Mcp.Contracts;
using HPRebar.McpBridge.Core.Scripting;
using HPRobot.Mcp.Server.Hosts.Robot;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Win32;
using Xunit;

namespace HPRobot.Mcp.Server.Tests;

/// <summary>
///     Every seed compiles against the installed Interop.RobotOM.dll with the bridge's exact imports and globals.
///     The wrapper is metadata only — nothing of Robot is loaded or started.
///     Gracefully and visibly skipped on machines without Autodesk Robot Structural Analysis Professional 2026 installed.
/// </summary>
public sealed class SeedCompilationTests
{
    private const string WrapperFile = "Interop.RobotOM.dll";
    private const string Clsid = "{F7870790-CDE5-11D1-8FF1-00A02447BAAE}";
    private const string EnvVar = "HPROBOT_ROBOT_DIR";

    public sealed record Seed(string Category, string Name, JsonElement Tool, string Code, JsonElement Examples);

    private static readonly Lazy<IReadOnlyList<Seed>> Cache = new(LoadSeedsCore);

    public static IReadOnlyList<Seed> LoadSeeds() => Cache.Value;

    public static IEnumerable<object[]> Seeds() => LoadSeeds().Select(s => new object[] { s.Category + "/" + s.Name });

    public static Seed Get(string key) => LoadSeeds().Single(s => s.Category + "/" + s.Name == key);

    private static IReadOnlyList<Seed> LoadSeedsCore()
    {
        var assembly = typeof(RobotHostProfile).Assembly;
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
    public void Seed_code_compiles_against_the_robot_wrapper(string key)
    {
        var seed = Get(key);
        var compiled = Compile(seed.Code);
        Assert.SkipWhen(compiled is null, "Autodesk Robot Structural Analysis Professional 2026 not installed (Interop.RobotOM.dll not found)");

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

        if (seed.Name == "run_calculations")
        {
            Assert.True(isDestructive, "run_calculations must declare 'destructive' tag");
            Assert.Equal("auto", mode);
        }
        else if (seed.Category is "Geometry" && seed.Name.StartsWith("assign_") ||
                 seed.Category is "Geometry" && seed.Name.StartsWith("draw_") ||
                 seed.Category is "Property" && seed.Name.StartsWith("assign_") ||
                 seed.Category is "Load" && seed.Name.StartsWith("assign_"))
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
        var bad = Compile("return robot.NoSuchMember(args.Int(\"n\"));");
        Assert.SkipWhen(bad is null, "Autodesk Robot Structural Analysis Professional 2026 not installed (Interop.RobotOM.dll not found)");
        Assert.Contains(bad!.Value.errors, e => e.Contains("CS1061") && e.Contains("NoSuchMember"));

        var good = Compile("int count = structure.Nodes.GetAll().Count; return new { count, type = robot.Project.Type.ToString(), x = args.Double(\"x\") };");
        Assert.Empty(good!.Value.errors);
    }

    [Fact]
    public void Guard_check_rejects_robot_exit_and_process_spawn()
    {
        var violations = ScriptGuard.Check("robot.Application.Quit(); return 1;", GuardProfile.Robot);
        Assert.NotEmpty(violations);
        Assert.Contains(violations, d => d.Message.Contains("Quit", StringComparison.OrdinalIgnoreCase) || d.Message.Contains("denied", StringComparison.OrdinalIgnoreCase));

        var procViolations = ScriptGuard.Check("System.Diagnostics.Process.Start(\"calc.exe\"); return 1;", GuardProfile.Robot);
        Assert.NotEmpty(procViolations);
    }

    /// <summary>The bridge's script environment as a class: usings = the Robot imports, fields = the Robot globals.</summary>
    private static (CSharpCompilation compilation, string[] errors)? Compile(string code)
    {
        var wrapper = FindWrapper();
        if (wrapper is null) return null;

        var usings = string.Join(Environment.NewLine, HostScriptContracts.RobotImports.Select(ns => $"using {ns};"));
        var source = $$"""
            {{usings}}

            public sealed class SeedHost
            {
                public IRobotApplication robot;
                public IRobotStructure structure;
                public IRobotUnitMngr units;
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

        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(p => Path.GetFileName(p) is var f && (f.StartsWith("System.", StringComparison.Ordinal) || f is "netstandard.dll" or "mscorlib.dll"))
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .Concat([
                MetadataReference.CreateFromFile(wrapper),
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
    ///     Resolves Interop.RobotOM.dll:
    ///     1. HPROBOT_ROBOT_DIR env var
    ///     2. HKCR / HKLM CLSID {F7870790-CDE5-11D1-8FF1-00A02447BAAE}\LocalServer32
    ///     3. %ProgramFiles%\Autodesk\Robot Structural Analysis Professional 2026\Exe\
    /// </summary>
    private static string? FindWrapper()
    {
        var candidates = new List<string?> { Environment.GetEnvironmentVariable(EnvVar) };

        if (OperatingSystem.IsWindows())
        {
            try
            {
                using var key = Registry.ClassesRoot.OpenSubKey($@"CLSID\{Clsid}\LocalServer32")
                    ?? Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Classes\CLSID\{Clsid}\LocalServer32");

                if (key?.GetValue(null) is string server)
                {
                    candidates.Add(Path.GetDirectoryName(server.Trim('"')));
                }
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or IOException)
            {
                // Registry read failure - fallback to candidate paths
            }
        }

        candidates.Add(Path.Combine(
            Environment.GetEnvironmentVariable("ProgramW6432") ?? @"C:\Program Files",
            "Autodesk", "Robot Structural Analysis Professional 2026", "Exe"));

        return candidates
            .Where(c => !string.IsNullOrEmpty(c))
            .Select(c => Path.Combine(c!, WrapperFile))
            .FirstOrDefault(File.Exists);
    }
}
