using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     Globals simulating Tekla Bridge runtime globals on net48.
/// </summary>
public sealed class TeklaNet48TestGlobals
{
    // ReSharper disable InconsistentNaming — matches Tekla script globals
    public object? model = new object();
    public CancellationToken ct = CancellationToken.None;
    public List<string> logs = [];
    public Action<string> log => msg => logs.Add(msg);
    public Action<int, int, string> progress => (cur, max, msg) => { };
    public ScriptArgs args = ScriptArgs.Empty;
    // ReSharper restore InconsistentNaming
}

/// <summary>
///     Challenger 2 Empirical Verification Suite on .NET Framework 4.8 Runtime (Desktop CLR v4.0.30319):
///     1. Verify execution on desktop CLR v4.0.30319 and framework identity.
///     2. Empirically challenge GuardProfile.Tekla and AnalyzerProfile.Tekla against edge cases and bypass attempts.
///     3. Test Roslyn compilation and execution on net48 with Tekla script imports and globals.
///     4. Empirically verify host neutrality: no host API leak into McpShared.
/// </summary>
public sealed class TeklaMilestone1Challenger2Net48Tests
{
    private const string TeklaBinDir = @"C:\Program Files\Tekla Structures\2025.0\bin";

    private static ScriptCompiler CreateNet48Compiler(IReadOnlyCollection<string> imports, IReadOnlyCollection<Assembly>? additionalRefs = null)
    {
        var refs = new List<Assembly>
        {
            typeof(object).Assembly,
            typeof(Enumerable).Assembly,
            typeof(List<>).Assembly,
            typeof(ScriptArgs).Assembly,
            typeof(JsonElement).Assembly,
        };

        if (additionalRefs != null)
        {
            refs.AddRange(additionalRefs);
        }

        return new ScriptCompiler(refs, imports, typeof(TeklaNet48TestGlobals), 10);
    }

    // =========================================================================
    // 1. DESKTOP CLR v4.0.30319 RUNTIME VERIFICATION
    // =========================================================================

    [Fact]
    public void Executes_strictly_on_desktop_clr_v4_and_framework_48()
    {
        var frameworkDesc = RuntimeInformation.FrameworkDescription;
        Assert.StartsWith(".NET Framework", frameworkDesc, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(4, Environment.Version.Major);

        var clrVersion = RuntimeEnvironment.GetSystemVersion();
        Assert.Equal("v4.0.30319", clrVersion);

        var runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();
        Assert.Contains("Microsoft.NET\\Framework", runtimeDir, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("v4.0.30319", runtimeDir, StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // 2. GUARDPROFILE.TEKLA EMPIRICAL CHALLENGES & BYPASS TESTS
    // =========================================================================

    [Theory]
    [InlineData("model.CommitChanges();", "CommitChanges")]
    [InlineData("model?.CommitChanges();", "CommitChanges")]
    [InlineData("MessageBox.Show(\"modal\");", "MessageBox")]
    [InlineData("System.Windows.Forms.MessageBox.Show(\"modal\");", "System.Windows.Forms")]
    [InlineData("var p = new Picker();", "Picker")]
    [InlineData("picker.PickObject();", "PickObject")]
    [InlineData("picker?.PickObject();", "PickObject")]
    [InlineData("picker.PickObjects();", "PickObjects")]
    [InlineData("picker.PickPoint();", "PickPoint")]
    [InlineData("picker.PickPoints();", "PickPoints")]
    [InlineData("picker.PickLine();", "PickLine")]
    [InlineData("picker.PickPolygon();", "PickPolygon")]
    [InlineData("picker.PickFace();", "PickFace")]
    [InlineData("picker?.PickFace();", "PickFace")]
    [InlineData("System.Diagnostics.Process.Start(\"cmd.exe\");", "System.Diagnostics.Process")]
    [InlineData("#r \"Tekla.Structures.Model.dll\"\nreturn 1;", "#r")]
    [InlineData("#load \"script.csx\"\nreturn 1;", "#load")]
    [InlineData("using Tekla.Structures.Dialog; return 1;", "Tekla.Structures.Dialog")]
    [InlineData("using Tekla.Structures.Drawing.UI; return 1;", "Tekla.Structures.Drawing.UI")]
    [InlineData("using HPTekla.McpBridge; return 1;", "HPTekla.McpBridge")]
    [InlineData("global::HPTekla.McpBridge.Something.Do();", "HPTekla.McpBridge")]
    [InlineData("global::Tekla.Structures.Dialog.UIFilterForm f = null;", "Tekla.Structures.Dialog")]
    [InlineData("global::HPRebar.McpBridge.Core.Host.McpBridgeHost.Current.Stop();", "HPRebar.McpBridge.Core.Host")]
    [InlineData("dynamic d = 1; return d;", "dynamic")]
    [InlineData("await Task.Delay(10); return 1;", "await")]
    [InlineData("unsafe { int* p = null; } return 1;", "unsafe")]
    public void GuardProfile_Tekla_blocks_forbidden_syntax_under_desktop_clr(string script, string expectedKeyword)
    {
        var diagnostics = ScriptGuard.Check(script, GuardProfile.Tekla);
        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, d => d.Message.Contains(expectedKeyword, StringComparison.Ordinal));
    }

    [Fact]
    public void GuardProfile_Tekla_vulnerability_parenthesized_receiver_bypasses_guard()
    {
        // REMEDIATED: Parenthesized receiver `(model).CommitChanges()` is blocked because
        // CommitChanges is now in DeniedMembers.
        var diagnostics = ScriptGuard.Check("(model).CommitChanges(); return 1;", GuardProfile.Tekla);
        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, d => d.Message.Contains("CommitChanges", StringComparison.Ordinal));
    }

    [Fact]
    public void GuardProfile_Tekla_vulnerability_aliased_receiver_bypasses_guard()
    {
        // REMEDIATED: Aliased receiver `var m = model; m.CommitChanges()` is blocked because
        // CommitChanges is now in DeniedMembers.
        var diagnostics = ScriptGuard.Check("var m = model; m.CommitChanges(); return 1;", GuardProfile.Tekla);
        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, d => d.Message.Contains("CommitChanges", StringComparison.Ordinal));
    }

    [Fact]
    public void GuardProfile_Tekla_vulnerability_PickFace_is_missing_from_denied_members()
    {
        // REMEDIATED: `picker.PickFace()` is an interactive picking method in Tekla.Structures.Model.UI.Picker
        // and is now denied in GuardProfile.Tekla.DeniedMembers.
        var diagnostics = ScriptGuard.Check("picker.PickFace();", GuardProfile.Tekla);
        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, d => d.Message.Contains("PickFace", StringComparison.Ordinal));
    }

    [Fact]
    public void GuardProfile_Tekla_parenthesized_receiver_bypass_test()
        => GuardProfile_Tekla_vulnerability_parenthesized_receiver_bypasses_guard();

    [Fact]
    public void GuardProfile_Tekla_aliased_receiver_bypass_test()
        => GuardProfile_Tekla_vulnerability_aliased_receiver_bypasses_guard();

    // =========================================================================
    // 3. ANALYZERPROFILE.TEKLA TRANSACTION DETECTION
    // =========================================================================

    [Theory]
    [InlineData("model.CommitChanges(); return 1;", true)]
    [InlineData("(model).CommitChanges(); return 1;", true)]
    [InlineData("var m = model; m.CommitChanges(); return 1;", true)]
    [InlineData("log(\"hello\"); return 42;", false)]
    public void AnalyzerProfile_Tekla_detects_transactions_on_net48(string script, bool expectedUsesTransaction)
    {
        var result = ScriptAnalyzer.Analyze(script, AnalyzerProfile.Tekla);
        Assert.Equal(expectedUsesTransaction, result.UsesTransaction);
    }

    // =========================================================================
    // 4. ROSLYN COMPILATION & EXECUTION ON NET48 WITH TEKLA SCRIPT CONTRACTS
    // =========================================================================

    [Fact]
    public async Task Roslyn_compilation_and_execution_with_Tekla_globals_on_net48()
    {
        var compiler = CreateNet48Compiler(["System", "System.Linq", "System.Collections.Generic", "HPRebar.McpBridge.Core.Scripting"]);
        const string script = """
            log("Tekla script started");
            var x = 10;
            var y = 20;
            log($"Sum: {x + y}");
            return x + y;
            """;

        var outcome = compiler.GetOrCompile(script);
        Assert.True(outcome.Succeeded, string.Join("; ", outcome.Diagnostics.Select(d => d.Message)));

        var globals = new TeklaNet48TestGlobals();
        var state = await outcome.Script!.RunAsync(globals, CancellationToken.None);

        Assert.Equal(30, state.ReturnValue);
        Assert.Equal(["Tekla script started", "Sum: 30"], globals.logs);
    }

    [Fact]
    public void Roslyn_compilation_with_real_Tekla_assemblies_if_installed_on_net48()
    {
        if (!Directory.Exists(TeklaBinDir))
        {
            return; // Skip if Tekla Structures is not installed on the machine
        }

        var teklaModelPath = Path.Combine(TeklaBinDir, "Tekla.Structures.Model.dll");
        var teklaCorePath = Path.Combine(TeklaBinDir, "Tekla.Structures.dll");
        var teklaGeomPath = Path.Combine(TeklaBinDir, "Tekla.Structures.Catalogs.dll");

        if (!File.Exists(teklaModelPath) || !File.Exists(teklaCorePath))
        {
            return;
        }

        var teklaModelAssembly = Assembly.LoadFrom(teklaModelPath);
        var teklaCoreAssembly = Assembly.LoadFrom(teklaCorePath);
        var teklaCatalogsAssembly = File.Exists(teklaGeomPath) ? Assembly.LoadFrom(teklaGeomPath) : null;

        var additionalRefs = new List<Assembly> { teklaCoreAssembly, teklaModelAssembly };
        if (teklaCatalogsAssembly != null) additionalRefs.Add(teklaCatalogsAssembly);

        var compiler = CreateNet48Compiler(HostScriptContracts.TeklaImports, additionalRefs);

        // Test compiling clean Tekla syntax using Tekla.Structures.Geometry3d.Point
        const string cleanCode = """
            var p1 = new Tekla.Structures.Geometry3d.Point(0, 0, 0);
            var p2 = new Tekla.Structures.Geometry3d.Point(1000, 0, 0);
            return p1.X + p2.X;
            """;

        var outcome = compiler.GetOrCompile(cleanCode);
        Assert.True(outcome.Succeeded, string.Join("; ", outcome.Diagnostics.Select(d => d.Message)));

        // Test ScriptAnalyzer.Run with GuardProfile.Tekla and AnalyzerProfile.Tekla
        var analyzed = ScriptAnalyzer.Run(compiler, cleanCode, GuardProfile.Tekla, AnalyzerProfile.Tekla);
        Assert.True(analyzed.Compiles);
        Assert.Empty(analyzed.GuardViolations);
        Assert.False(analyzed.UsesTransaction);

        // Test that forbidden code is stopped by GuardProfile before compilation
        const string forbiddenCode = "MessageBox.Show(\"test\"); return 1;";
        var analyzedForbidden = ScriptAnalyzer.Run(compiler, forbiddenCode, GuardProfile.Tekla, AnalyzerProfile.Tekla);
        Assert.NotEmpty(analyzedForbidden.GuardViolations);
        Assert.False(analyzedForbidden.Compiles);
    }

    // =========================================================================
    // 5. HOST NEUTRALITY EMPIRICAL VERIFICATION (SHARED ASSEMBLIES ON NET48)
    // =========================================================================

    [Theory]
    [InlineData(typeof(PipeNaming))]
    [InlineData(typeof(PipeListener))]
    public void McpShared_net48_assemblies_reference_zero_host_apis(Type typeFromAssembly)
    {
        var assembly = typeFromAssembly.Assembly;
        var referencedAssemblies = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToArray();

        string[] forbiddenPrefixes =
        [
            "Tekla", "RevitAPI", "AcDbMgd", "AcMgd", "Autodesk.Navisworks", "ETABSv1",
            "SAP2000v1", "RobotOM", "Microsoft.Office.Interop.Excel", "Microsoft.AnalysisServices",
        ];

        foreach (var prefix in forbiddenPrefixes)
        {
            Assert.DoesNotContain(referencedAssemblies, name => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }
    }
}
