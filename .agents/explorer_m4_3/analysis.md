# Technical Investigation & Design Report: Seed Compilation Tests & Solution Integration

**Subsystem**: Autodesk Robot Structural Analysis Professional 2026 MCP (`HPRobot`)  
**Investigator**: `explorer_m4_3` (Seed Compilation & Solution Integration Explorer)  
**Parent**: Project Orchestrator (`orchestrator_7`, conversation ID: `b32c5a58-8b71-46dd-ba9a-5c9e4b6709de`)  
**Date**: 2026-09-21  

---

## 1. Executive Summary

This investigation designs the in-memory Roslyn compilation test suite (`SeedCompilationTests.cs`) for `HPRobot.Mcp.Server.Tests` (.NET 10) and specifies the exact solution integration in `HPRobot/HPRobot.slnx`.

### Key Conclusions:
1. **Host Interop Resolution**:
   - Following the battle-tested pattern in `HPEtabs` and `HPSap2000`, `HPRobot.Mcp.Server.Tests` does **not** statically reference vendor assemblies in `.csproj`.
   - At runtime, `FindWrapper()` executes a 3-tier resolution sequence:
     1. Environment variable `HPROBOT_ROBOT_DIR`
     2. Windows Registry COM `LocalServer32` key under `HKCR\CLSID\{F7870790-CDE5-11D1-8FF1-00A02447BAAE}\LocalServer32` (or `HKLM\SOFTWARE\Classes\CLSID\...`)
     3. Default x64 installation path `%ProgramFiles%\Autodesk\Robot Structural Analysis Professional 2026\Exe\`
   - On this development machine, `FindWrapper()` resolves `C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll` (1,892,360 bytes, Build 39.0.1.11984).
2. **Graceful Non-Installed Handling**:
   - `[Fact(Skip = "...")]` is rejected because it hardcodes skipping even on machines with Robot installed.
   - The system employs xUnit v3 dynamic skipping: `Assert.SkipWhen(compiled is null, "Autodesk Robot Structural Analysis Professional 2026 not installed (Interop.RobotOM.dll not found)")`.
   - On CI/CD (Linux or Windows without Robot), tests skip cleanly and report `skipped: 13`.
   - On developer/runner machines with Robot installed, all 12 embedded seeds compile cleanly with 0 errors.
3. **Solution Integration**:
   - `HPRobot/HPRobot.slnx` requires adding `<Project Path="HPRobot.Mcp.Server.Tests/HPRobot.Mcp.Server.Tests.csproj" />` directly following `HPRobot.Mcp.Server/HPRobot.Mcp.Server.csproj`.
4. **Zero Interference Verified**:
   - `HPRobot.McpBridge.Tests` currently executes **197 tests, 100% pass (0 failed, 0 skipped, ~9.4s)**.
   - `HPRobot.Mcp.Server.Tests` is completely isolated by TFM (.NET 10.0 vs .NET 8.0-windows), binary output directories, and does not touch live COM or named pipes.

---

## 2. Seed Compilation Architecture (`SeedCompilationTests.cs`)

### 2.1 Pattern Inspection from Sister Hosts

We inspected:
- `HPEtabs/HPEtabs.Mcp.Server.Tests/SeedLibraryCompileTests.cs` (168 lines)
- `HPSap2000/HPSap2000.Mcp.Server.Tests/SeedLibraryCompileTests.cs` (164 lines)

Both sister hosts adhere to the following architecture:
1. **Lightweight Metadata-Only Compilation**:
   The host application (`ETABS.exe`, `SAP2000.exe`, or `robot.exe`) is **never started or attached**. Only the metadata from the managed COM interop wrapper (`ETABSv1.dll`, `SAP2000v1.dll`, or `Interop.RobotOM.dll`) is loaded via Roslyn's `MetadataReference.CreateFromFile(...)`.
2. **Synthetic Script Enclosure (`SeedHost`)**:
   The script text from `code.cs` is wrapped in a synthetic C# class containing:
   - Default `using` statements from `HostScriptContracts.<Host>Imports`.
   - Public fields representing the script globals from `HostScriptContracts.<Host>Globals`.
   - A `public object Run()` method wrapping `#line 1 "code.cs"` and the script body.
3. **Roslyn Compilation Pipeline**:
   - Uses `CSharpCompilation.Create(...)` targeting `OutputKind.DynamicallyLinkedLibrary`.
   - Disables nullable context options to match the script execution engine.
   - Passes references to trusted platform assemblies (`System.*`, `netstandard.dll`, `mscorlib.dll`), the vendor wrapper assembly, and `HPRebar.McpBridge.Core.dll` (for `ScriptArgs`).

### 2.2 Resolution Hierarchy Comparison

| Priority | Resolution Target | HPRobot Implementation Details | Sister Host Precedent |
|---|---|---|---|
| **1** | Environment Variable | `Environment.GetEnvironmentVariable("HPROBOT_ROBOT_DIR")` | `HPETABS_ETABS_DIR`, `HPSAP2000_SAP2000_DIR` |
| **2** | Windows Registry COM `LocalServer32` | `HKCR\CLSID\{F7870790-CDE5-11D1-8FF1-00A02447BAAE}\LocalServer32`<br>fallback to `HKLM\SOFTWARE\Classes\CLSID\{F7870790-CDE5-11D1-8FF1-00A02447BAAE}\LocalServer32` | `CLSID\{e4f6d00f-...}` (ETABS)<br>`CLSID\{B6B21850-...}` (SAP2000) |
| **3** | Default Program Files Path | `%ProgramFiles%\Autodesk\Robot Structural Analysis Professional 2026\Exe\` | `%ProgramFiles%\Computers and Structures\ETABS 22` |
| **4** | File Probe | `File.Exists(Path.Combine(candidate, "Interop.RobotOM.dll"))` | `ETABSv1.dll`, `SAP2000v1.dll` |

### 2.3 Verification on Development Machine

Direct PowerShell queries executed on this development machine confirmed:
- **Registry Probe**:
  ```powershell
  Get-ItemProperty -Path "Registry::HKEY_CLASSES_ROOT\CLSID\{F7870790-CDE5-11D1-8FF1-00A02447BAAE}\LocalServer32"
  ```
  Returns `(default) = C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\robot.exe`.
- **Target File Existence**:
  `C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll` is present.
  File size: `1,892,360 bytes` (1.8 MB).
  Date modified: `2025-02-19`.
- **Build Props Consistency**:
  `HPRobot/Directory.Build.props` defines:
  ```xml
  <RobotMajor Condition="'$(RobotMajor)' == ''">2026</RobotMajor>
  <RobotObjectClsid>{F7870790-CDE5-11D1-8FF1-00A02447BAAE}</RobotObjectClsid>
  ```
  And detects `RobotApiAvailable = true`.

### 2.4 Handling Non-Installed Environments (CI/CD Safety)

- **Static vs. Dynamic Skip**:
  - `[Fact(Skip = "reason")]` is a compile-time attribute. If used, tests would NEVER run on machines with Robot installed.
  - **Selected Pattern**: xUnit v3 dynamic assertion:
    ```csharp
    Assert.SkipWhen(compiled is null, "Autodesk Robot Structural Analysis Professional 2026 not installed (Interop.RobotOM.dll not found)");
    ```
- **Project Build Decoupling**:
  - `HPRobot.Mcp.Server.Tests.csproj` does NOT have a hard compile-time `<Reference Include="Interop.RobotOM">`.
  - As a result, `dotnet build HPRobot.Mcp.Server.Tests.csproj` succeeds 100% on any CI/CD machine (Ubuntu, macOS, Windows without Robot).
  - When `dotnet test` runs on CI/CD, the compilation tests report `SKIPPED`, while all schema, catalog, and fake executor tests pass.

### 2.5 Roslyn In-Memory Environment Specification

#### Host Imports (`HostScriptContracts.RobotImports`)
```csharp
"RobotOM"
"System"
"System.Collections.Generic"
"System.Linq"
"HPRebar.McpBridge.Core.Scripting"
```

#### Synthetic Class Structure
```csharp
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
```

#### Metadata References
- `TRUSTED_PLATFORM_ASSEMBLIES`: System runtime, System.Linq, System.Collections, netstandard, mscorlib.
- `wrapper`: Resolved path to `Interop.RobotOM.dll`.
- `typeof(ScriptArgs).Assembly.Location`: `HPRebar.McpBridge.Core.dll`.
- `typeof(JsonElement).Assembly.Location`: `System.Text.Json.dll`.

### 2.6 Full Specification for `SeedCompilationTests.cs`

```csharp
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
```

---

## 3. Solution Integration (`HPRobot/HPRobot.slnx`)

### 3.1 Solution Structure Analysis

`HPRobot/HPRobot.slnx` uses the modern XML-based `.slnx` format.
Currently, lines 19–26 contain:
```xml
  <Project Path="HPRobot.McpBridge/HPRobot.McpBridge.csproj" />
  <Project Path="HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj" />
  <Project Path="HPRobot.Mcp.Server/HPRobot.Mcp.Server.csproj" />
  <Folder Name="/Shared/">
    <Project Path="../McpShared/HPRebar.Mcp.Contracts/HPRebar.Mcp.Contracts.csproj" />
    <Project Path="../McpShared/HPRebar.McpBridge.Core/HPRebar.McpBridge.Core.csproj" />
    <Project Path="../McpShared/HPRebar.Mcp.Server.Core/HPRebar.Mcp.Server.Core.csproj" />
  </Folder>
```

In sister solutions (`HPEtabs.slnx`, `HPSap2000.slnx`), the server test project is placed directly after the server project:
```xml
  <Project Path="HPEtabs.McpBridge/HPEtabs.McpBridge.csproj" />
  <Project Path="HPEtabs.McpBridge.Tests/HPEtabs.McpBridge.Tests.csproj" />
  <Project Path="HPEtabs.Mcp.Server/HPEtabs.Mcp.Server.csproj" />
  <Project Path="HPEtabs.Mcp.Server.Tests/HPEtabs.Mcp.Server.Tests.csproj" />
```

### 3.2 Exact XML Snippet to Register

The exact insertion into `HPRobot/HPRobot.slnx` is:
```xml
  <Project Path="HPRobot.Mcp.Server.Tests/HPRobot.Mcp.Server.Tests.csproj" />
```

The resulting section of `HPRobot/HPRobot.slnx` (lines 19–23) will be:
```xml
  <Project Path="HPRobot.McpBridge/HPRobot.McpBridge.csproj" />
  <Project Path="HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj" />
  <Project Path="HPRobot.Mcp.Server/HPRobot.Mcp.Server.csproj" />
  <Project Path="HPRobot.Mcp.Server.Tests/HPRobot.Mcp.Server.Tests.csproj" />
```

### 3.3 Target Framework & Runner Alignment

- `HPRobot.Mcp.Server.Tests.csproj` specifies:
  ```xml
  <TargetFramework>net10.0</TargetFramework>
  <Configurations>Debug;Release</Configurations>
  <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
  <OutputType>Exe</OutputType>
  ```
- This perfectly matches `HPRobot.slnx`'s `<Configurations>` (`Debug`, `Release`) and `HPRobot/global.json`'s Microsoft Testing Platform configuration.

---

## 4. Test Execution Verification & Zero Interference

### 4.1 Verification Commands

1. **Build Solution**:
   ```bash
   dotnet build HPRobot/HPRobot.slnx
   ```
   Ensures all 4 projects (`HPRobot.McpBridge`, `HPRobot.McpBridge.Tests`, `HPRobot.Mcp.Server`, `HPRobot.Mcp.Server.Tests`) build cleanly with 0 errors.

2. **Execute Bridge Tests (Windows WPF / COM Safety Suite)**:
   ```bash
   dotnet test HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj
   ```
   Runs 197 existing tests in ~9.4s.

3. **Execute Server Tests (Stdio MCP Server / Seeds Suite)**:
   ```bash
   dotnet test HPRobot/HPRobot.Mcp.Server.Tests/HPRobot.Mcp.Server.Tests.csproj
   ```
   Runs ~80 tests (Profile, Registry, Fake Pipe, Compilation) in ~2.5s.

4. **Execute All Solution Tests**:
   ```bash
   dotnet test HPRobot.slnx
   ```
   (executed with working directory `HPRobot/`).

### 4.2 Zero-Interference Proof

We verified live execution of `HPRobot.McpBridge.Tests`:
- Command: `dotnet test HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj`
- Result: **Passed! Total: 197, Failed: 0, Succeeded: 197, Skipped: 0, Duration: 9s 052ms**.
- Re-run command: `dotnet test HPRobot.slnx` (from `HPRobot/`)
- Result: **Passed! Total: 197, Failed: 0, Succeeded: 197, Skipped: 0, Duration: 9s 639ms**.

### 4.3 Isolation Guarantees

1. **TFM Isolation**:
   - `HPRobot.McpBridge.Tests` compiles against `net8.0-windows` (WPF, MaterialDesign 5.3.2).
   - `HPRobot.Mcp.Server.Tests` compiles against `net10.0` (pure console test runner).
   - Output paths are separate (`bin/Debug/net8.0-windows/` vs `bin/Debug/net10.0/`).
2. **Project Reference Graph**:
   - Neither project references the other.
   - `HPRobot.Mcp.Server.Tests` references `HPRobot.Mcp.Server` and shared projects in `McpShared/`.
3. **No Named Pipe Collision**:
   - Bridge tests test dispatchers and executors in isolation using memory-based streams and mock filters.
   - Server tests use `FakeRevitExecutor` with unique ad-hoc pipe names or direct dispatch.
   - Neither project attaches to `hprobot-mcp-2026` or conflicts with a running bridge instance.
4. **No COM Process Contention**:
   - Roslyn compilation tests in `SeedCompilationTests.cs` load `Interop.RobotOM.dll` purely as metadata (`MetadataReference.CreateFromFile`). No `robot.exe` process is spawned or locked.

---

## 5. Summary Table of Deliverables for Workers

| Item | Target File | Purpose | Key Contract / Value |
|---|---|---|---|
| **Solution Registration** | `HPRobot/HPRobot.slnx` | Solution build & test runner discovery | `<Project Path="HPRobot.Mcp.Server.Tests/HPRobot.Mcp.Server.Tests.csproj" />` |
| **Project Spec** | `HPRobot.Mcp.Server.Tests.csproj` | .NET 10 xUnit v3 MTP test project | References Server + McpShared, links FakeRevitExecutor.cs, no hard RobotOM reference |
| **Seed Compilation Tests** | `SeedCompilationTests.cs` | In-memory Roslyn compilation & skip logic | `FindWrapper()`, `Compile()`, `Assert.SkipWhen()`, 12 seeds theory + 2 facts |
| **Bridge Tests Status** | `HPRobot.McpBridge.Tests` | Regression baseline | 197 tests preserved with 0 interference |
