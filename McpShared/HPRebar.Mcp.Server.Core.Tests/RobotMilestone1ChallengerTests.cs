using System.Reflection;
using System.Text.Json;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.Mcp.Server.Services;
using HPRebar.Mcp.Server.Tests.Fakes;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using static HPRebar.Mcp.Server.Tests.RobotTestProfile;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     Adversarial security challenger test suite for Milestone M1 (Robot MCP Shared Integration).
///     Empirically validates:
///     1. Denied methods (Quit, ApplicationExit, Interactive, MessageBox, etc.) across receivers and syntax forms.
///     2. Forbidden script directives (#r, #load) with formatting variations.
///     3. File deletion and process creation attempts (File.Delete, Process.Start, Directory.Delete, streams).
///     4. Reflection, dynamic dispatch, unsafe blocks, and threading/async attacks.
///     5. Timeout clamping and validation against RobotHeavyMaxTimeoutSeconds (300s).
///     6. Legitimate RobotOM script execution (positive controls).
///     7. Host neutrality and multi-host wire isolation.
/// </summary>
public sealed class RobotMilestone1ChallengerTests
{
    // =========================================================================
    // 1. DENIED METHOD INVOCATIONS & EVASION PATTERNS
    // =========================================================================

    [Theory]
    // Direct invocation on standard globals
    [InlineData("robot.Quit();", "Quit")]
    [InlineData("app.Quit();", "Quit")]
    [InlineData("robot.ApplicationExit();", "ApplicationExit")]
    [InlineData("app.ApplicationExit();", "ApplicationExit")]
    // Any receiver calling Quit or ApplicationExit is denied by DeniedMembers
    [InlineData("var r = robot; r.Quit();", "Quit")]
    [InlineData("var a = app; a.Quit();", "Quit")]
    [InlineData("myObj.Quit();", "Quit")]
    [InlineData("myObj.ApplicationExit();", "ApplicationExit")]
    // Null-conditional invocations
    [InlineData("robot?.Quit();", "Quit")]
    [InlineData("app?.Quit();", "Quit")]
    [InlineData("robot?.ApplicationExit();", "ApplicationExit")]
    [InlineData("foo?.Quit();", "Quit")]
    [InlineData("foo?.ApplicationExit();", "ApplicationExit")]
    // Interactive property setter / getter
    [InlineData("robot.Interactive = 0;", "Interactive")]
    [InlineData("robot.Interactive = 1;", "Interactive")]
    [InlineData("app.Interactive = 0;", "Interactive")]
    [InlineData("var mode = robot.Interactive;", "Interactive")]
    [InlineData("robot?.Interactive = 0;", "Interactive")]
    [InlineData("var mode = robot?.Interactive;", "Interactive")]
    [InlineData("foo.Interactive = 0;", "Interactive")]
    // Delegate reference assignment & wrapper
    [InlineData("Action a = robot.Quit;", "Quit")]
    [InlineData("Action a = app.Quit;", "Quit")]
    [InlineData("Action a = () => robot.Quit();", "Quit")]
    [InlineData("Action a = () => robot.Interactive = 0;", "Interactive")]
    // Modal dialogs & Windows Forms UI
    [InlineData("MessageBox.Show(\"modal\");", "MessageBox")]
    [InlineData("System.Windows.Forms.MessageBox.Show(\"modal\");", "System.Windows.Forms")]
    [InlineData("System.Windows.Forms.Form f = null;", "System.Windows.Forms")]
    [InlineData("using System.Windows.Forms;", "System.Windows.Forms")]
    [InlineData("using global::System.Windows.Forms;", "System.Windows.Forms")]
    // Bridge internals access
    [InlineData("using HPRobot.McpBridge;", "HPRobot.McpBridge")]
    [InlineData("using global::HPRobot.McpBridge;", "HPRobot.McpBridge")]
    [InlineData("HPRobot.McpBridge.BridgeEntry.Stop();", "HPRobot.McpBridge")]
    [InlineData("global::HPRobot.McpBridge.BridgeEntry.Stop();", "HPRobot.McpBridge")]
    [InlineData("HPRebar.McpBridge.Core.Host.McpBridgeHost.Current.Stop();", "HPRebar.McpBridge.Core.Host")]
    [InlineData("global::HPRebar.McpBridge.Core.Host.McpBridgeHost.Current.Stop();", "HPRebar.McpBridge.Core.Host")]
    public void Denied_methods_dialogs_and_bridge_internals_are_blocked(string code, string expectedViolation)
    {
        var diagnostics = ScriptGuard.Check(code, GuardProfile.Robot);
        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, d => d.Message.Contains(expectedViolation, StringComparison.Ordinal));
    }

    // =========================================================================
    // 2. FORBIDDEN SCRIPT DIRECTIVES (#r, #load)
    // =========================================================================

    [Theory]
    [InlineData("#r \"System.IO.dll\"\nreturn 1;")]
    [InlineData("#load \"malicious.csx\"\nreturn 1;")]
    [InlineData("   #r   \"untrusted.dll\"\nreturn 1;")]
    [InlineData("\t#load   \"helper.csx\"\nreturn 1;")]
    [InlineData("#r \"foo.dll\"\n#load \"bar.csx\"\nreturn 1;")]
    [InlineData("#r \"nuget:Newtonsoft.Json, 13.0.1\"\nreturn 1;")]
    public void Script_directives_are_unconditionally_prohibited_under_robot_profile(string script)
    {
        var diagnostics = ScriptGuard.Check(script, GuardProfile.Robot);
        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, d =>
            d.Message.Contains("#r", StringComparison.Ordinal) ||
            d.Message.Contains("#load", StringComparison.Ordinal));
        Assert.All(diagnostics, d => Assert.Contains("Robot Structural Analysis scripts", d.Message, StringComparison.Ordinal));
    }

    // =========================================================================
    // 3. FILE DELETION, IO & PROCESS CREATION ATTEMPTS
    // =========================================================================

    [Theory]
    // Process creation
    [InlineData("System.Diagnostics.Process.Start(\"cmd.exe\");", "System.Diagnostics.Process")]
    [InlineData("Process.Start(\"calc.exe\");", "Process")]
    [InlineData("var psi = new ProcessStartInfo(\"cmd\");", "ProcessStartInfo")]
    [InlineData("global::System.Diagnostics.Process.Start(\"notepad\");", "System.Diagnostics.Process")]
    // File deletion
    [InlineData("System.IO.File.Delete(\"critical.rtd\");", "System.IO")]
    [InlineData("File.Delete(\"model.rtd\");", "File")]
    [InlineData("global::System.IO.File.Delete(\"structure.rtd\");", "System.IO")]
    [InlineData("File.WriteAllText(\"payload.txt\", \"data\");", "File")]
    // Directory deletion
    [InlineData("Directory.Delete(\"C:\\Data\", true);", "Directory")]
    [InlineData("System.IO.Directory.Delete(\"C:\\Data\");", "System.IO")]
    [InlineData("global::System.IO.Directory.Delete(\"C:\\Data\");", "System.IO")]
    // Streams & Info
    [InlineData("var fs = new FileStream(\"test.dat\", FileMode.Create);", "FileStream")]
    [InlineData("var sw = new StreamWriter(\"test.txt\");", "StreamWriter")]
    [InlineData("var sr = new StreamReader(\"test.txt\");", "StreamReader")]
    [InlineData("var fi = new FileInfo(\"test.txt\");", "FileInfo")]
    [InlineData("var di = new DirectoryInfo(\"test\");", "DirectoryInfo")]
    // Namespace using
    [InlineData("using System.IO;", "System.IO")]
    [InlineData("using global::System.IO;", "System.IO")]
    [InlineData("using System.Diagnostics.Process;", "System.Diagnostics.Process")]
    public void File_IO_deletion_and_process_creation_are_blocked(string code, string expectedViolation)
    {
        var diagnostics = ScriptGuard.Check(code, GuardProfile.Robot);
        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, d => d.Message.Contains(expectedViolation, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("var p = System.IO.Path.Combine(\"dir\", \"model.rtd\"); return p;")]
    [InlineData("var f = System.IO.Path.GetFileName(@\"C:\\Projects\\Model.rtd\"); return f;")]
    [InlineData("var ext = System.IO.Path.GetExtension(\"file.rtd\"); return ext;")]
    [InlineData("var dir = System.IO.Path.GetDirectoryName(@\"C:\\Models\\1.rtd\"); return dir;")]
    public void Path_carveout_is_permitted_for_path_formatting_without_io(string benignCode)
    {
        var diagnostics = ScriptGuard.Check(benignCode, GuardProfile.Robot);
        Assert.Empty(diagnostics);
    }

    // =========================================================================
    // 4. REFLECTION, DYNAMIC DISPATCH, UNSAFE & THREADING ATTEMPTS
    // =========================================================================

    [Theory]
    // Type lookup by string & reflection
    [InlineData("Type.GetType(\"System.Diagnostics.Process\");", "Type.GetType")]
    [InlineData("typeof(string).Assembly.GetType(\"System.Diagnostics.Process\");", "Assembly")]
    [InlineData("typeof(object).GetMethod(\"ToString\");", ".GetMethod")]
    [InlineData("typeof(object).GetMethods();", ".GetMethods")]
    [InlineData("typeof(object).GetProperty(\"Length\");", ".GetProperty")]
    [InlineData("typeof(object).GetProperties();", ".GetProperties")]
    [InlineData("typeof(object).GetField(\"Empty\");", ".GetField")]
    [InlineData("typeof(object).GetFields();", ".GetFields")]
    [InlineData("typeof(object).GetMember(\"ToString\");", ".GetMember")]
    [InlineData("typeof(object).GetMembers();", ".GetMembers")]
    [InlineData("typeof(object).GetConstructor(Type.EmptyTypes);", ".GetConstructor")]
    [InlineData("typeof(object).GetConstructors();", ".GetConstructors")]
    // Dynamic dispatch & unsafe
    [InlineData("dynamic d = robot; d.Quit();", "dynamic")]
    [InlineData("unsafe { int* p = null; }", "unsafe")]
    // Delegate & activator evasion
    [InlineData("Activator.CreateInstance(typeof(string));", "Activator")]
    [InlineData("Delegate.CreateDelegate(typeof(Action), null, \"Quit\");", "Delegate")]
    // Threading and async
    [InlineData("Thread.Sleep(1000);", "Thread")]
    [InlineData("new Thread(() => {}).Start();", "Thread")]
    [InlineData("ThreadPool.QueueUserWorkItem(_ => {});", "ThreadPool")]
    [InlineData("Task.Run(() => {});", "Task")]
    [InlineData("Parallel.For(0, 10, i => {});", "Parallel")]
    [InlineData("new Timer(_ => {}, null, 0, 1000);", "Timer")]
    [InlineData("await Task.Delay(100);", "await")]
    [InlineData("Func<Task> f = async () => await Task.Yield();", "async lambdas")]
    [InlineData("Action a = async () => { await Task.Delay(1); };", "async lambdas")]
    // String literals mentioning denied namespaces (reflection string attacks)
    [InlineData("var s = \"System.Reflection.BindingFlags\";", "System.Reflection")]
    [InlineData("var s = \"System.IO.File\";", "System.IO")]
    [InlineData("var s = \"System.Net.Sockets\";", "System.Net")]
    [InlineData("var s = \"System.Diagnostics.Process\";", "System.Diagnostics.Process")]
    public void Reflection_dynamic_unsafe_and_threading_are_blocked(string code, string expectedViolation)
    {
        var diagnostics = ScriptGuard.Check(code, GuardProfile.Robot);
        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, d => d.Message.Contains(expectedViolation, StringComparison.Ordinal));
    }

    // =========================================================================
    // 5. TIMEOUT CLAMPING & VALIDATION (RobotHeavyMaxTimeoutSeconds = 300)
    // =========================================================================

    [Fact]
    public void Robot_heavy_timeout_is_pinned_to_300_seconds_and_profile_matches()
    {
        Assert.Equal(300, HostScriptContracts.RobotHeavyMaxTimeoutSeconds);
        Assert.Equal(300, Robot().MaxTimeoutSeconds);
    }

    [Theory]
    [InlineData(5, true)]
    [InlineData(30, true)]
    [InlineData(120, true)]
    [InlineData(300, true)]
    [InlineData(4, false)]
    [InlineData(301, false)]
    [InlineData(600, false)]
    public void ToolValidator_enforces_timeout_bounds_between_5_and_300_for_robot(int timeoutSeconds, bool shouldPass)
    {
        var candidate = Candidate(timeoutSeconds);
        var result = ToolValidator.Validate(candidate, null, [], false, Robot());

        if (shouldPass)
        {
            Assert.DoesNotContain(result.Errors, e => e.Contains("timeoutSeconds"));
        }
        else
        {
            Assert.Contains(result.Errors, e => e.Contains("between 5 and 300"));
        }
    }

    [Fact]
    public void HostProfile_rejects_MaxTimeoutSeconds_below_minimum_of_5()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new HostProfile
        {
            HostId = "robot",
            DisplayName = "Robot",
            ServerName = "Robot Server",
            ProductFolder = "HPRobot",
            EnvPrefix = "HPROBOT_",
            DefaultVersion = 2026,
            ValidVersions = [2026],
            MethodPrefix = "robot.",
            ExecuteToolName = "execute_robot_code",
            ContextToolName = "get_robot_context",
            ResourceScheme = "robot",
            Categories = ["Model"],
            CoreToolNames = ["execute_robot_code"],
            ScriptImports = [],
            ScriptContractSummary = "",
            HostAssembly = typeof(RobotTestProfile).Assembly,
            MaxTimeoutSeconds = 4, // Invalid! Minimum is 5
        });

        Assert.Contains("must be at least 5 seconds", ex.Message);
    }

    [Fact]
    public void HostProfile_honors_custom_max_timeout_clamping()
    {
        var clampedProfile = Robot(maxTimeout: 180);
        Assert.Equal(180, clampedProfile.MaxTimeoutSeconds);

        // 180 passes
        Assert.DoesNotContain(ToolValidator.Validate(Candidate(180), null, [], false, clampedProfile).Errors, e => e.Contains("timeoutSeconds"));

        // 181 fails with "between 5 and 180"
        Assert.Contains(ToolValidator.Validate(Candidate(181), null, [], false, clampedProfile).Errors, e => e.Contains("between 5 and 180"));
    }

    // =========================================================================
    // 6. POSITIVE CONTROLS: LEGITIMATE ROBOTOM SCRIPTS PASS CLEANLY
    // =========================================================================

    [Fact]
    public void Legitimate_RobotOM_model_creation_and_analysis_scripts_pass_cleanly()
    {
        const string script = """
            var project = robot.Project;
            var structure = robot.Project.Structure;
            var nodes = structure.Nodes;
            var bars = structure.Bars;

            // Create nodes
            int n1 = 1;
            int n2 = 2;
            nodes.Create(n1, 0.0, 0.0, 0.0);
            nodes.Create(n2, 0.0, 0.0, 3.5);

            // Create bar
            bars.Create(1, n1, n2);

            // Assign section and material
            var labels = structure.Labels;
            var secName = args.Str("section", "IPE 300");
            bars.Get(1).SetLabel(RobotOM.IRobotLabelType.I_LT_BAR_SECTION, secName);

            // Read units and report
            var unitLabel = units.Label;
            log($"Bar 1 created between node {n1} and {n2} with section {secName}");
            progress(1, 1, "Creation complete");

            if (ct.IsCancellationRequested) return new { cancelled = true };

            return new
            {
                success = true,
                nodes = nodes.GetAll().Count,
                bars = bars.GetAll().Count,
                activeFile = project.FileName
            };
            """;

        var diagnostics = ScriptGuard.Check(script, GuardProfile.Robot);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Legitimate_RobotOM_results_extraction_passes_cleanly()
    {
        const string extractResultsScript = """
            var str = robot.Project.Structure;
            var res = str.Results;
            var reactions = res.Nodes.Reactions;
            int nodeNum = args.Int("nodeNumber", 1);
            int caseNum = args.Int("caseNumber", 1);

            var val = reactions.Value(nodeNum, caseNum);
            double fx = val.FX;
            double fy = val.FY;
            double fz = val.FZ;
            double mx = val.MX;
            double my = val.MY;
            double mz = val.MZ;

            return new { nodeNum, caseNum, fx, fy, fz, mx, my, mz };
            """;

        var diagnostics = ScriptGuard.Check(extractResultsScript, GuardProfile.Robot);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Legitimate_LINQ_and_collection_operations_pass_cleanly()
    {
        const string linqScript = """
            var list = new List<double> { 1.0, 2.5, 3.5, 4.0 };
            var filtered = list.Where(x => x > 2.0).Select(x => x * 10.0).ToList();
            var dict = new Dictionary<string, int> { ["A"] = 1, ["B"] = 2 };
            return new { count = filtered.Count, sum = filtered.Sum(), bVal = dict["B"] };
            """;

        var diagnostics = ScriptGuard.Check(linqScript, GuardProfile.Robot);
        Assert.Empty(diagnostics);
    }

    // =========================================================================
    // 7. HOST NEUTRALITY & MULTI-HOST WIRE ISOLATION REGRESSION TESTS
    // =========================================================================

    [Fact]
    public void McpShared_assemblies_never_reference_RobotOM_assemblies()
    {
        var forbiddenRobotPrefixes = new[] { "RobotOM", "Interop.RobotOM" };

        var sharedAssemblies = new[]
        {
            typeof(PipeNaming).Assembly,
            typeof(PipeListener).Assembly,
            typeof(ResultFormatter).Assembly,
        };

        foreach (var assembly in sharedAssemblies)
        {
            var referenced = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToArray();
            foreach (var prefix in forbiddenRobotPrefixes)
            {
                Assert.DoesNotContain(referenced, name => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    [Fact]
    public void All_nine_hosts_have_distinct_pipe_naming_constants()
    {
        var hostConstants = new[]
        {
            PipeNaming.RevitHost,
            PipeNaming.AutocadHost,
            PipeNaming.NavisHost,
            PipeNaming.EtabsHost,
            PipeNaming.Civil3dHost,
            PipeNaming.Sap2000Host,
            PipeNaming.PowerBiHost,
            PipeNaming.ExcelHost,
            PipeNaming.RobotHost,
        };

        Assert.Equal(9, hostConstants.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void All_nine_hosts_have_distinct_jsonrpc_prefixes()
    {
        var prefixes = new[]
        {
            JsonRpcMethods.RevitPrefix,
            JsonRpcMethods.AutocadPrefix,
            JsonRpcMethods.NavisPrefix,
            JsonRpcMethods.EtabsPrefix,
            JsonRpcMethods.Civil3dPrefix,
            JsonRpcMethods.Sap2000Prefix,
            JsonRpcMethods.PowerBiPrefix,
            JsonRpcMethods.ExcelPrefix,
            JsonRpcMethods.RobotPrefix,
        };

        Assert.All(prefixes, p => Assert.EndsWith(".", p));
        Assert.Equal(9, prefixes.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [InlineData("execute", "robot.execute")]
    [InlineData("context", "robot.context")]
    [InlineData("ping", "robot.ping")]
    [InlineData("cancel", "robot.cancel")]
    [InlineData("analyze", "robot.analyze")]
    [InlineData("progress", "robot.progress")]
    [InlineData("log", "robot.log")]
    [InlineData("status", "robot.status")]
    public void JsonRpcMethods_For_and_Suffix_are_bijective_for_robot(string suffix, string expectedFull)
    {
        var full = JsonRpcMethods.For(JsonRpcMethods.RobotPrefix, suffix);
        Assert.Equal(expectedFull, full);
        Assert.Equal(suffix, JsonRpcMethods.Suffix(full));
    }

    [Fact]
    public void JsonRpcMethods_notification_detectors_work_for_robot()
    {
        Assert.True(JsonRpcMethods.IsProgress("robot.progress"));
        Assert.True(JsonRpcMethods.IsLog("robot.log"));
        Assert.True(JsonRpcMethods.IsStatus("robot.status"));

        Assert.False(JsonRpcMethods.IsProgress("robot.execute"));
        Assert.False(JsonRpcMethods.IsLog("robot.context"));
        Assert.False(JsonRpcMethods.IsStatus("robot.ping"));
    }

    [Fact]
    public void ContextResult_when_robot_is_null_never_contains_robot_in_serialized_json()
    {
        var context = new ContextResult
        {
            Host = "revit",
            HostVersion = "2026",
            RevitVersion = "2026",
            DocTitle = "Building.rvt",
            Robot = null,
        };

        var json = BridgeJson.Serialize(context);
        Assert.DoesNotContain("\"robot\"", json);
    }

    [Fact]
    public void ContextResult_for_sibling_hosts_never_leaks_robot_payload()
    {
        var excelContext = new ContextResult
        {
            Host = "excel", HostVersion = "2026",
            Excel = new ExcelInfo(true, 101, "16.0", "Book.xlsx", "Sheet1", "$A$1", true, false, 1, 1, true),
        };
        Assert.DoesNotContain("\"robot\"", BridgeJson.Serialize(excelContext));

        var powerbiContext = new ContextResult
        {
            Host = "powerbi", HostVersion = "2026",
            PowerBi = new PowerBiInfo(true, 102, 50000, "Model", "1600", true, 5, 10, 4),
        };
        Assert.DoesNotContain("\"robot\"", BridgeJson.Serialize(powerbiContext));

        var sapContext = new ContextResult
        {
            Host = "sap2000", HostVersion = "27",
            Sap2000 = new Sap2000Info(true, 103, "27.0", false, "kN, m, C", "kN, m, C", false, 10, 20, 5),
        };
        Assert.DoesNotContain("\"robot\"", BridgeJson.Serialize(sapContext));
    }
}
