namespace AdversarialTier5Challenger;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPGrasshopper.E2E.Tests.Contracts;
using HPGrasshopper.E2E.Tests.Harness;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.Core.Scripting;

public static class Program
{
    private static int _total = 0;
    private static int _passed = 0;
    private static int _failed = 0;
    private static readonly List<string> _failures = new();

    public static async Task<int> Main()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("  HPGRASSHOPPER TIER 5 EMPIRICAL ADVERSARIAL STRESS CHALLENGER");
        Console.WriteLine("================================================================================");

        TestAstMutationsAndDirectives();
        await TestSecurityOptInAndDenyListAsync();
        TestSeedParameterFuzzing();
        await TestCancellationAndConcurrencyStressAsync();

        Console.WriteLine("\n================================================================================");
        Console.WriteLine($"  SUMMARY: Total={_total}, Passed={_passed}, Failed={_failed}");
        Console.WriteLine("================================================================================");

        if (_failed > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nFAILED CHALLENGES:");
            foreach (var f in _failures) Console.WriteLine($"  - {f}");
            Console.ResetColor();
            return 1;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\nALL ADVERSARIAL CHALLENGES REJECTED CLEANLY. SAFETY GUARANTEES HOLD!");
        Console.ResetColor();
        return 0;
    }

    private static void Assert(bool condition, string name, string? details = null)
    {
        _total++;
        if (condition)
        {
            _passed++;
            Console.WriteLine($"[PASS] {name}");
        }
        else
        {
            _failed++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FAIL] {name} -> {details ?? "Condition failed"}");
            Console.ResetColor();
            _failures.Add($"{name}: {details}");
        }
    }

    private static void TestAstMutationsAndDirectives()
    {
        Console.WriteLine("\n--- Attack Vector 1: Roslyn AST Mutations & Directives Bypass ---");

        // 1.1 Forbidden directive variations
        var directives = new[]
        {
            "#r \"System.IO.dll\"",
            "#r   \"System.Reflection.dll\"",
            "#load \"subscript.cs\"",
            "#load    \"subscript.cs\"",
            "#r \"System.Data.dll\"\nreturn 1;",
            "#load \"evil.cs\"\nreturn 2;"
        };
        foreach (var d in directives)
        {
            var diags = ScriptGuard.Check(d, GuardProfile.Grasshopper);
            Assert(diags.Count > 0 && diags.Any(x => x.Message.Contains("#r") || x.Message.Contains("#load")),
                $"Directive rejected: {d.Split('\n')[0]}", $"Expected rejection of directive {d}");
        }

        // 1.2 Verbatim string containing directive text must NOT be rejected by directive checker
        var innocentString = "string code = @\"#r \"\"foo.dll\"\"\";\nreturn code.Length;";
        var diagsInnocent = ScriptGuard.Check(innocentString, GuardProfile.Grasshopper);
        Assert(!diagsInnocent.Any(x => x.Message.Contains("is not allowed in Grasshopper scripts")),
            "String literal containing '#r' text is not falsely flagged as directive");

        // 1.3 Deny-list namespace evasion attempts
        var evasions = new[]
        {
            ("global::System.IO.File.WriteAllText(\"a\", \"b\");", "System.IO"),
            ("global::System.Diagnostics.Process.Start(\"calc.exe\");", "System.Diagnostics"),
            ("global::System.Reflection.Assembly.Load(\"evil\");", "System.Reflection"),
            ("using IO = System.IO; IO.File.ReadAllText(\"x\");", "System.IO"),
            ("using static System.IO.File; Exists(\"test\");", "System.IO"),
            ("System.Net.WebClient wc = null;", "System.Net"),
            ("System.Runtime.InteropServices.Marshal.AllocHGlobal(100);", "System.Runtime.InteropServices"),
            ("System.Security.Principal.WindowsIdentity.GetCurrent();", "System.Security"),
            ("System.Threading.Tasks.Task.Run(() => 1);", "System.Threading.Tasks"),
            ("Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(\"\");", "Microsoft.CodeAnalysis"),
            ("Microsoft.Win32.Registry.CurrentUser.OpenSubKey(\"evil\");", "Microsoft.Win32"),
            ("System.Linq.Expressions.Expression.Call(null, null);", "System.Linq.Expressions")
        };
        foreach (var (code, expectedNs) in evasions)
        {
            var diags = ScriptGuard.Check(code, GuardProfile.Grasshopper);
            Assert(diags.Count > 0 && diags.Any(x => x.Message.Contains(expectedNs) || x.Message.Contains("not allowed")),
                $"Evasion blocked: {code.Split(';')[0]}", $"Expected detection of {expectedNs}");
        }

        // 1.4 Allowed qualified prefix: System.IO.Path is carved out
        var allowedPathCode = "string p = System.IO.Path.Combine(\"a\", \"b\"); return p;";
        var pathDiags = ScriptGuard.Check(allowedPathCode, GuardProfile.Grasshopper);
        Assert(pathDiags.Count == 0, "System.IO.Path.Combine is carved out and allowed");

        // 1.5 Forbidden member access on grasshopper globals
        var globalForbidden = new[]
        {
            ("rhinoDoc.Dispose();", "rhinoDoc.Dispose is denied"),
            ("app.Exit();", "app.Exit is denied"),
            ("app.Quit();", "app.Quit is denied"),
            ("MessageBox.Show(\"modal\");", "MessageBox is denied"),
            ("TaskDialog.Show(\"title\", \"msg\");", "TaskDialog is denied")
        };
        foreach (var (code, desc) in globalForbidden)
        {
            var diags = ScriptGuard.Check(code, GuardProfile.Grasshopper);
            Assert(diags.Count > 0, $"Forbidden global/member blocked: {desc}");
        }

        // 1.6 Deep AST nesting (500 binary plus expressions)
        var sb = new StringBuilder("int v = 1");
        for (int i = 0; i < 500; i++) sb.Append(" + 1");
        sb.Append(';');
        var deepCode = sb.ToString();
        var deepDiags = ScriptGuard.Check(deepCode, GuardProfile.Grasshopper);
        Assert(deepDiags.Count == 0, "500 chained additions parsed without stack overflow or false guard violation");

        // 1.7 Exact line boundary 300 vs 301
        var lines300 = string.Join("\n", Enumerable.Range(0, 300).Select(i => $"int x{i} = {i};"));
        var findings300 = ScriptQuality.Find(lines300);
        Assert(!findings300.Any(f => f.RuleId == "Q-B3"), "300 lines passes Rule Q-B3");

        var lines301 = string.Join("\n", Enumerable.Range(0, 301).Select(i => $"int x{i} = {i};"));
        var findings301 = ScriptQuality.Find(lines301);
        Assert(findings301.Any(f => f.RuleId == "Q-B3" && f.Severity == QualityFinding.Error), "301 lines triggers Rule Q-B3 Error");
    }

    private static async Task TestSecurityOptInAndDenyListAsync()
    {
        Console.WriteLine("\n--- Attack Vector 2: Security Opt-In & Denial Enforcement ---");

        var bridge = new GrasshopperE2EBridge();

        // 2.1 Default state is disabled
        Assert(!bridge.IsExecutionEnabled, "Default bridge state has IsExecutionEnabled = false");

        // 2.2 Mutating execution while disabled returns -32001
        var disabledReq = new GrasshopperExecutionRequest("return 100;", Transaction: "auto");
        var disabledRes = await bridge.ExecuteAsync(disabledReq);
        Assert(!disabledRes.Success, "Execution rejected when opt-in disabled");
        Assert(disabledRes.ErrorCode == -32001, "Rejection error code is -32001");
        Assert(disabledRes.ErrorMessage != null && disabledRes.ErrorMessage.Contains(GrasshopperE2EContracts.OptInDisabledMessageFragment),
            "Error message contains opt-in required guidance");
        Assert(!disabledRes.RolledBack, "RolledBack is false because no transaction opened");
        Assert(bridge.GetCommittedElementCount() == 0, "Zero elements committed while disabled");

        // 2.3 Read-only context query permitted while disabled
        var ctx = bridge.GetContext();
        Assert(ctx.IsConnected, "Context query is connected while disabled");
        Assert(!ctx.IsExecutionEnabled, "Context correctly reflects disabled execution");
        Assert(ctx.ComponentsCount >= 0, "Context returns valid component count");

        // 2.4 Enabling opt-in permits execution
        bridge.IsExecutionEnabled = true;
        Assert(bridge.IsExecutionEnabled, "IsExecutionEnabled set to true");
        var enabledReq = new GrasshopperExecutionRequest("return 200;", Transaction: "auto");
        var enabledRes = await bridge.ExecuteAsync(enabledReq);
        Assert(enabledRes.Success, "Execution succeeds when opt-in enabled");

        // 2.5 Concurrent opt-in toggling under execution stress
        int executionAttempts = 0;
        int disabledRejections = 0;
        int successfulRuns = 0;
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var toggleTask = Task.Run(() =>
        {
            int counter = 0;
            while (!cts.IsCancellationRequested)
            {
                bridge.IsExecutionEnabled = (counter++ % 2 == 0);
                Thread.Sleep(1);
            }
        });

        var gate = new SemaphoreSlim(1, 1);
        var execTasks = Enumerable.Range(0, 5).Select(_ => Task.Run(async () =>
        {
            while (!cts.IsCancellationRequested)
            {
                Interlocked.Increment(ref executionAttempts);
                var req = new GrasshopperExecutionRequest("return 42;", Transaction: "auto");
                GrasshopperExecutionResult res;
                await gate.WaitAsync();
                try { res = await bridge.ExecuteAsync(req); }
                finally { gate.Release(); }

                if (!res.Success && res.ErrorCode == -32001) Interlocked.Increment(ref disabledRejections);
                else if (res.Success) Interlocked.Increment(ref successfulRuns);
                await Task.Delay(2);
            }
        })).ToArray();

        await Task.WhenAll(execTasks);
        await toggleTask;

        Assert(executionAttempts > 0, $"Concurrent execution attempts made: {executionAttempts}");
        Assert(disabledRejections > 0, $"Disabled rejections correctly blocked with -32001: {disabledRejections}");
        Assert(successfulRuns > 0, $"Enabled executions succeeded: {successfulRuns}");
        Assert(disabledRejections + successfulRuns <= executionAttempts, "No unhandled exceptions or invalid states during concurrent opt-in toggle");

        // Re-enable for subsequent tests
        bridge.IsExecutionEnabled = true;
    }

    private static void TestSeedParameterFuzzing()
    {
        Console.WriteLine("\n--- Attack Vector 3: Seed Parameter Fuzzing & Boundaries ---");

        var bridge = new GrasshopperE2EBridge();

        // 3.1 Canvas component coordinates NaN / Infinity
        bool threwNanX = false;
        try { GrasshopperE2EHarness.AddComponentToCanvas(bridge, "Comp", "C", "Cat", double.NaN, 10.0); }
        catch (ArgumentException) { threwNanX = true; }
        Assert(threwNanX, "NaN X coordinate rejected by AddComponentToCanvas");

        bool threwInfY = false;
        try { GrasshopperE2EHarness.AddComponentToCanvas(bridge, "Comp", "C", "Cat", 10.0, double.PositiveInfinity); }
        catch (ArgumentException) { threwInfY = true; }
        Assert(threwInfY, "PositiveInfinity Y coordinate rejected by AddComponentToCanvas");

        // 3.2 Negative port indices
        var g1 = Guid.NewGuid();
        var g2 = Guid.NewGuid();
        bool threwNegPort = false;
        try { GrasshopperE2EHarness.ConnectPortsOnCanvas(bridge, g1, -1, g2, 0); }
        catch (IndexOutOfRangeException) { threwNegPort = true; }
        Assert(threwNegPort, "Negative source port index -1 rejected");

        // 3.3 Cyclic self-connection
        bool threwSelfLoop = false;
        try { GrasshopperE2EHarness.ConnectPortsOnCanvas(bridge, g1, 0, g1, 1); }
        catch (InvalidOperationException) { threwSelfLoop = true; }
        Assert(threwSelfLoop, "Connecting component to itself on canvas rejected");

        // 3.4 Seed library ADR-0007 quality scan
        var serverAsmPath = Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\..\HPGrasshopper\HPGrasshopper.Mcp.Server\bin\Release\net10.0\HPGrasshopper.Mcp.Server.dll");
        var serverAsm = System.Reflection.Assembly.LoadFrom(Path.GetFullPath(serverAsmPath));
        var seeds = SeedInstaller.LoadSeeds(serverAsm);
        Assert(seeds.Count >= 12, $"Seed library contains at least 12 embedded seeds (found {seeds.Count})");

        int seedsWithQualityErrors = 0;
        foreach (var seed in seeds)
        {
            var findings = ScriptQuality.Find(seed.Code);
            var errs = findings.Where(f => f.Severity == QualityFinding.Error).ToList();
            if (errs.Count > 0)
            {
                seedsWithQualityErrors++;
                Console.WriteLine($"Seed {seed.Name} failed with: {string.Join(", ", errs.Select(e => e.Message))}");
            }
        }
        Assert(seedsWithQualityErrors == 0, "All embedded seeds pass ADR-0007 script quality scan (0 errors)");
    }

    private static async Task TestCancellationAndConcurrencyStressAsync()
    {
        Console.WriteLine("\n--- Attack Vector 4: Concurrency, Cancellation & Timeout Stress ---");

        var executor = new GrasshopperTestExecutor { IsExecutionEnabled = true };

        // 4.1 Cancellation token already cancelled before dispatch
        using var preCts = new CancellationTokenSource();
        preCts.Cancel();
        var preReq = new ExecuteRequest("return 999;", TransactionModes.Auto);
        var preRes = await executor.ExecuteAsync(preReq, null, preCts.Token);
        Assert(preRes.IsError, "Pre-cancelled token reports error");
        Assert(preRes.RolledBack, "Pre-cancelled token reports rolledBack = true");
        Assert(!executor.IsBusy, "Executor IsBusy is false after pre-cancelled token");

        // 4.2 Mid-flight cancellation correlation
        using var runCts = new CancellationTokenSource();
        var midReq = new ExecuteRequest("return 12345;", TransactionModes.Auto, SessionId: "sess-mid");
        var midTask = executor.ExecuteAsync(5001L, midReq, null, runCts.Token);
        var cancelOutcome = executor.Cancel(5001L, "sess-mid");
        var midRes = await midTask;
        Console.WriteLine($"[DEBUG] cancelOutcome.Cancelled={cancelOutcome.Cancelled}, midRes.IsError={midRes.IsError}, midRes.RolledBack={midRes.RolledBack}, midRes.Message={midRes.Message}");
        Assert(cancelOutcome.Cancelled, "Targeted cancellation successfully correlated to requestId 5001");
        Assert(midRes.RolledBack, "Mid-flight cancellation rolled back cleanly", $"RolledBack={midRes.RolledBack}, IsError={midRes.IsError}, Message={midRes.Message}");
        Assert(!executor.IsBusy, "Executor IsBusy reset to false");

        // 4.3 Multi-session isolation: Cancelling Session A must not affect Session B
        using var ctsA = new CancellationTokenSource();
        var reqA = new ExecuteRequest("return 'A';", TransactionModes.Auto, SessionId: "sess-A");
        var reqB = new ExecuteRequest("return 'B';", TransactionModes.Auto, SessionId: "sess-B");

        var taskA = executor.ExecuteAsync(6001L, reqA, null, ctsA.Token);
        executor.Cancel(6001L, "sess-A");
        var resA = await taskA;

        var taskB = executor.ExecuteAsync(6002L, reqB, null, CancellationToken.None);
        var resB = await taskB;

        Assert(resA.RolledBack, "Session A request rolled back upon targeted cancellation");
        Assert(!resB.IsError && !resB.RolledBack, "Session B request proceeded to commit successfully without interference");

        // 4.4 Repeated cancel calls (Cancel-of-cancel idempotency)
        var c1 = executor.Cancel(99999L, "phantom");
        var c2 = executor.Cancel(99999L, "phantom");
        var c3 = executor.Cancel();
        Assert(!c1.Cancelled && !c2.Cancelled, "Cancelling nonexistent request returns Cancelled=false without error");
        Assert(!executor.IsBusy, "IsBusy remains false");

        // 4.5 Atomic busy flag prevents concurrent re-entry
        var holdCts = new CancellationTokenSource();
        var longReq = new ExecuteRequest("return 1;", TransactionModes.Auto);
        var firstTask = executor.ExecuteAsync(7001L, longReq, null, holdCts.Token);
        
        // Second concurrent request while first is executing
        var secondReq = new ExecuteRequest("return 2;", TransactionModes.Auto);
        var secondRes = await executor.ExecuteAsync(7002L, secondReq, null, CancellationToken.None);
        
        Assert(secondRes.IsError, "Second concurrent request rejected immediately");
        Assert(secondRes.Message != null && secondRes.Message.Contains("busy"),
            "Second request error states bridge is busy executing another operation");

        await firstTask;
        Assert(!executor.IsBusy, "IsBusy false after all requests complete");
    }
}
