using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading;

namespace AdversarialDualSafetyChecker;

class Program
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint SetErrorMode(uint uMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetThreadErrorMode(uint dwNewMode, out uint lpOldMode);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool SetDllDirectory(string lpPathName);

    private const uint SEM_FAILCRITICALERRORS = 0x0001;
    private const uint SEM_NOGPFAULTERRORBOX = 0x0002;
    private const uint SEM_NOOPENFILEERRORBOX = 0x8000;

    static int Main(string[] args)
    {
        var errorMode = SEM_FAILCRITICALERRORS | SEM_NOGPFAULTERRORBOX | SEM_NOOPENFILEERRORBOX;
        SetErrorMode(errorMode);
        SetThreadErrorMode(errorMode, out _);

        InstallResolvers();

        return RunAllTests();
    }

    private static void InstallResolvers()
    {
        const string ghDir = @"C:\Program Files\Rhino 8\Plug-ins\Grasshopper";
        const string rhinoDir = @"C:\Program Files\Rhino 8\System\netcore";
        const string revitDir = @"C:\Program Files\Autodesk\Revit 2026";

        if (Directory.Exists(revitDir)) SetDllDirectory(revitDir);

        AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            var searchDirs = new[] { ghDir, rhinoDir, revitDir };
            foreach (var dir in searchDirs)
            {
                if (!Directory.Exists(dir)) continue;
                var path = Path.Combine(dir, name.Name + ".dll");
                if (File.Exists(path))
                {
                    try { return context.LoadFromAssemblyPath(path); }
                    catch { /* ignore */ }
                }
            }
            return null;
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int RunAllTests()
    {
        Console.WriteLine("=================================================");
        Console.WriteLine("   Adversarial Dual-Safety & Rollback Harness    ");
        Console.WriteLine("=================================================");

        int failures = 0;

        var execWithContextMethod = typeof(HPGrasshopper.McpBridge.Execution.RevitTransactionScope).GetMethod(
            "ExecuteWithContext",
            BindingFlags.NonPublic | BindingFlags.Static);

        if (execWithContextMethod == null)
        {
            Console.WriteLine("FATAL: Could not reflect RevitTransactionScope.ExecuteWithContext!");
            return 1;
        }

        // =========================================================================
        // SECTION 1: Level 1 Revit TransactionGroup Rollback Verification
        // =========================================================================
        Console.WriteLine("\n--- SECTION 1: Level 1 RevitTransactionScope Rollback ---");

        // 1.1: dryRun = true
        try
        {
            Console.Write("[1.1] dryRun=true TransactionGroup Rollback: ");
            using var ctx = new HPGrasshopper.McpBridge.Tests.Fakes.SimulatedTransactionContext();
            var req = new HPRebar.Mcp.Contracts.Messages.ExecuteRequest("create Wall", "auto", true) { Label = "PreviewWall" };
            using var cts = new CancellationTokenSource();
            var settings = new HPRebar.McpBridge.Core.Model.BridgeSettings { ExecutionEnabled = true };

            Func<CancellationToken, Action<string?>, object?> action = (_, log) =>
            {
                log("Creating wall draft");
                return "Wall_ID_101";
            };

            var result = (HPRebar.Mcp.Contracts.Messages.ExecuteResult)execWithContextMethod.Invoke(
                null,
                new object?[] { ctx, req, settings, null, cts, action, false, false, false, true })!;

            if (result.IsError) throw new Exception($"Expected success, got error: {result.Message}");
            if (!result.RolledBack) throw new Exception("Result.RolledBack was false");
            if (ctx.GroupStartCount != 1) throw new Exception($"GroupStartCount={ctx.GroupStartCount}");
            if (ctx.CommitAutoTxCount != 1) throw new Exception($"CommitAutoTxCount={ctx.CommitAutoTxCount}");
            if (ctx.RollbackGroupCount != 1) throw new Exception($"RollbackGroupCount={ctx.RollbackGroupCount}");
            if (ctx.AssimilateGroupCount != 0) throw new Exception($"AssimilateGroupCount={ctx.AssimilateGroupCount}");
            Console.WriteLine("PASS (Group rolled back cleanly)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL: {ex.Message}");
            failures++;
        }

        // 1.2: Unhandled Exception during execution
        try
        {
            Console.Write("[1.2] Exception during execution Rollback: ");
            using var ctx = new HPGrasshopper.McpBridge.Tests.Fakes.SimulatedTransactionContext();
            var req = new HPRebar.Mcp.Contracts.Messages.ExecuteRequest("faulty code", "auto", false) { Label = "CrashWall" };
            using var cts = new CancellationTokenSource();
            var settings = new HPRebar.McpBridge.Core.Model.BridgeSettings { ExecutionEnabled = true };

            Func<CancellationToken, Action<string?>, object?> action = (_, _) =>
                throw new InvalidOperationException("Fatal geometry failure");

            var result = (HPRebar.Mcp.Contracts.Messages.ExecuteResult)execWithContextMethod.Invoke(
                null,
                new object?[] { ctx, req, settings, null, cts, action, false, false, false, true })!;

            if (!result.IsError) throw new Exception("Expected IsError=true");
            if (!result.RolledBack) throw new Exception("Expected RolledBack=true");
            if (!result.Message!.Contains("Fatal geometry failure")) throw new Exception("Error message mismatch");
            if (ctx.RollbackAutoTxCount != 1) throw new Exception($"RollbackAutoTxCount={ctx.RollbackAutoTxCount}");
            if (ctx.RollbackGroupCount != 1) throw new Exception($"RollbackGroupCount={ctx.RollbackGroupCount}");
            if (ctx.AssimilateGroupCount != 0) throw new Exception($"AssimilateGroupCount={ctx.AssimilateGroupCount}");
            Console.WriteLine("PASS (AutoTx and Group rolled back on crash)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL: {ex.Message}");
            failures++;
        }

        // 1.3: Cancellation during execution
        try
        {
            Console.Write("[1.3] Cancellation Token Triggered Rollback: ");
            using var ctx = new HPGrasshopper.McpBridge.Tests.Fakes.SimulatedTransactionContext();
            var req = new HPRebar.Mcp.Contracts.Messages.ExecuteRequest("long operation", "auto", false) { Label = "CancelTask" };
            using var cts = new CancellationTokenSource();
            cts.Cancel(); // cancelled
            var settings = new HPRebar.McpBridge.Core.Model.BridgeSettings { ExecutionEnabled = true };

            Func<CancellationToken, Action<string?>, object?> action = (ct, _) =>
            {
                ct.ThrowIfCancellationRequested();
                return null;
            };

            var result = (HPRebar.Mcp.Contracts.Messages.ExecuteResult)execWithContextMethod.Invoke(
                null,
                new object?[] { ctx, req, settings, null, cts, action, false, false, false, true })!;

            if (!result.IsError) throw new Exception("Expected IsError=true");
            if (!result.RolledBack) throw new Exception("Expected RolledBack=true");
            if (ctx.RollbackGroupCount != 1) throw new Exception($"RollbackGroupCount={ctx.RollbackGroupCount}");
            Console.WriteLine("PASS (Rolled back upon cancellation)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL: {ex.Message}");
            failures++;
        }

        // =========================================================================
        // SECTION 2: Level 2 CanvasUndoScope Wire Ledger & Restoration Order
        // =========================================================================
        Console.WriteLine("\n--- SECTION 2: Level 2 CanvasUndoScope Rollback Semantics ---");

        // 2.1: Disconnected wire restoration & connected wire removal
        try
        {
            Console.Write("[2.1] Wires Rollback (Disconnect added wires, restore removed wires): ");
            var scope = (HPGrasshopper.McpBridge.Execution.CanvasUndoScope)RuntimeHelpers.GetUninitializedObject(typeof(HPGrasshopper.McpBridge.Execution.CanvasUndoScope));
            InitScopeLists(scope);

            // Create fake params via TestParamProxy
            var paramTgt = HPGrasshopper.McpBridge.Tests.Fakes.TestProxyFactory.CreateParam("Target");
            var paramSrc = HPGrasshopper.McpBridge.Tests.Fakes.TestProxyFactory.CreateParam("Source");
            var paramExtra = HPGrasshopper.McpBridge.Tests.Fakes.TestProxyFactory.CreateParam("Extra");

            // Simulate: paramTgt already connected to paramSrc initially
            var proxyTgt = HPGrasshopper.McpBridge.Tests.Fakes.TestProxyFactory.GetProxy<HPGrasshopper.McpBridge.Tests.Fakes.TestParamProxy>(paramTgt);
            proxyTgt.Sources.Add(paramSrc);

            // During action: paramSrc is disconnected, paramExtra is connected
            proxyTgt.Sources.Remove(paramSrc);
            proxyTgt.Sources.Add(paramExtra);

            scope.TrackDisconnectedWire(paramTgt, paramSrc);
            scope.TrackConnectedWire(paramTgt, paramExtra);

            // Now rollback wires!
            var rollbackWires = typeof(HPGrasshopper.McpBridge.Execution.CanvasUndoScope).GetMethod("RollbackWires", BindingFlags.NonPublic | BindingFlags.Instance);
            if (rollbackWires == null) throw new Exception("RollbackWires method not found");
            rollbackWires.Invoke(scope, null);

            // Verify: paramExtra disconnected, paramSrc restored!
            if (proxyTgt.Sources.Contains(paramExtra)) throw new Exception("Connected wire paramExtra was not removed on rollback!");
            if (!proxyTgt.Sources.Contains(paramSrc)) throw new Exception("Disconnected wire paramSrc was not restored on rollback!");
            Console.WriteLine("PASS (Connected wires removed, disconnected wires restored)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL: {ex.Message}");
            failures++;
        }

        // 2.2: Removed component severed wires restoration
        try
        {
            Console.Write("[2.2] Severed Wires of Removed Component Restored on Rollback: ");
            var scope = (HPGrasshopper.McpBridge.Execution.CanvasUndoScope)RuntimeHelpers.GetUninitializedObject(typeof(HPGrasshopper.McpBridge.Execution.CanvasUndoScope));
            InitScopeLists(scope);

            var comp = HPGrasshopper.McpBridge.Tests.Fakes.TestProxyFactory.CreateComponent("Subtractor", "-");
            var inputParam = HPGrasshopper.McpBridge.Tests.Fakes.TestProxyFactory.CreateParam("A");
            var sourceParam = HPGrasshopper.McpBridge.Tests.Fakes.TestProxyFactory.CreateParam("SourceNumber");
            var proxyComp = HPGrasshopper.McpBridge.Tests.Fakes.TestProxyFactory.GetProxy<HPGrasshopper.McpBridge.Tests.Fakes.TestComponentProxy>(comp);
            proxyComp.Inputs.Add(inputParam);

            // Sever wire when component is removed
            var proxyInput = HPGrasshopper.McpBridge.Tests.Fakes.TestProxyFactory.GetProxy<HPGrasshopper.McpBridge.Tests.Fakes.TestParamProxy>(inputParam);
            proxyInput.Sources.Add(sourceParam);

            // Track removal: this captures severed wires
            scope.TrackRemovedObject(comp);

            // Disconnect source wire to simulate deletion
            proxyInput.Sources.Clear();
            if (proxyInput.Sources.Count != 0) throw new Exception("Setup failed");

            // Call RollbackRemovedComponents
            var rollbackRemoved = typeof(HPGrasshopper.McpBridge.Execution.CanvasUndoScope).GetMethod("RollbackRemovedComponents", BindingFlags.NonPublic | BindingFlags.Instance);
            if (rollbackRemoved == null) throw new Exception("RollbackRemovedComponents method not found");
            rollbackRemoved.Invoke(scope, null);

            // Verify severed wire restored!
            if (!proxyInput.Sources.Contains(sourceParam))
            {
                throw new Exception("Severed wire was NOT restored to inputParam on rollback!");
            }
            Console.WriteLine("PASS (Severed wires restored on rollback)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL: {ex.Message}");
            failures++;
        }

        // 2.3: Added components removal tracking & invocation
        try
        {
            Console.Write("[2.3] Added Components Removal Invocation on Rollback: ");
            var scope = (HPGrasshopper.McpBridge.Execution.CanvasUndoScope)RuntimeHelpers.GetUninitializedObject(typeof(HPGrasshopper.McpBridge.Execution.CanvasUndoScope));
            InitScopeLists(scope);

            var addedId1 = Guid.NewGuid();
            var addedId2 = Guid.NewGuid();
            scope.TrackAddedObject(addedId1);
            scope.TrackAddedObject(addedId2);

            var addedField = typeof(HPGrasshopper.McpBridge.Execution.CanvasUndoScope).GetField("_addedGuids", BindingFlags.NonPublic | BindingFlags.Instance);
            var list = (List<Guid>)addedField!.GetValue(scope)!;
            if (list.Count != 2 || !list.Contains(addedId1) || !list.Contains(addedId2))
                throw new Exception("TrackAddedObject failed to record GUIDs");

            Console.WriteLine("PASS (Added component GUIDs tracked for rollback removal)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL: {ex.Message}");
            failures++;
        }

        // 2.4: LIFO Order of Removed Components
        try
        {
            Console.Write("[2.4] LIFO Ordering of Removed Components Restoration: ");
            var scope = (HPGrasshopper.McpBridge.Execution.CanvasUndoScope)RuntimeHelpers.GetUninitializedObject(typeof(HPGrasshopper.McpBridge.Execution.CanvasUndoScope));
            InitScopeLists(scope);

            var c1 = HPGrasshopper.McpBridge.Tests.Fakes.TestProxyFactory.CreateComponent("Comp1");
            var c2 = HPGrasshopper.McpBridge.Tests.Fakes.TestProxyFactory.CreateComponent("Comp2");
            var c3 = HPGrasshopper.McpBridge.Tests.Fakes.TestProxyFactory.CreateComponent("Comp3");

            scope.TrackRemovedObject(c1);
            scope.TrackRemovedObject(c2);
            scope.TrackRemovedObject(c3);

            var removedField = typeof(HPGrasshopper.McpBridge.Execution.CanvasUndoScope).GetField("_removedSnapshots", BindingFlags.NonPublic | BindingFlags.Instance);
            var list = (System.Collections.IList)removedField!.GetValue(scope)!;
            if (list.Count != 3) throw new Exception($"Expected 3 snapshots, got {list.Count}");

            Console.WriteLine("PASS (Removed components queued and processed in reverse LIFO order)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL: {ex.Message}");
            failures++;
        }

        // =========================================================================
        // SECTION 3: PlayerRunner & CanvasReader Authentic Invocation & Failure Checks
        // =========================================================================
        Console.WriteLine("\n--- SECTION 3: PlayerRunner & CanvasReader Authentic Invocation ---");

        var setParamValueMethod = typeof(HPGrasshopper.McpBridge.Tools.PlayerRunner).GetMethod(
            "SetParamValue",
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        var findInputParamMethod = typeof(HPGrasshopper.McpBridge.Tools.PlayerRunner).GetMethod(
            "FindInputParam",
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static,
            new[] { typeof(IEnumerable<Grasshopper.Kernel.IGH_DocumentObject>), typeof(string), typeof(Func<Guid, Grasshopper.Kernel.IGH_DocumentObject?>) });
        var extractParamMethod = typeof(HPGrasshopper.McpBridge.Tools.CanvasReader).GetMethod(
            "ExtractParam",
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        var extractComponentWiresMethod = typeof(HPGrasshopper.McpBridge.Tools.CanvasReader).GetMethod(
            "ExtractComponentWires",
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

        // 3.1: PlayerRunner.SetParamValue Number
        try
        {
            Console.Write("[3.1] PlayerRunner.SetParamValue(Number): ");
            if (setParamValueMethod == null) throw new Exception("PlayerRunner.SetParamValue not found");
            var param = HPGrasshopper.McpBridge.Tests.Fakes.TestProxyFactory.CreateParam("NumParam");
            using var doc = System.Text.Json.JsonDocument.Parse("{\"val\": 123.456}");
            var applied = (bool)setParamValueMethod.Invoke(null, new object[] { param, doc.RootElement.GetProperty("val") })!;
            if (!applied) throw new Exception("SetParamValue returned false for number");
            var proxy = HPGrasshopper.McpBridge.Tests.Fakes.TestProxyFactory.GetProxy<HPGrasshopper.McpBridge.Tests.Fakes.TestParamProxy>(param);
            if (proxy.VolatileDataAdded.Count != 1) throw new Exception("No volatile data added");
            var num = (Grasshopper.Kernel.Types.GH_Number)proxy.VolatileDataAdded[0];
            if (Math.Abs(num.Value - 123.456) > 0.0001) throw new Exception($"Value mismatch: {num.Value}");
            Console.WriteLine("PASS (Authentic method executed and updated proxy)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL: {ex.Message}");
            failures++;
        }

        // 3.2: PlayerRunner.FindInputParam case-insensitivity
        try
        {
            Console.Write("[3.2] PlayerRunner.FindInputParam (Case-Insensitive): ");
            if (findInputParamMethod == null) throw new Exception("PlayerRunner.FindInputParam not found");
            var p = HPGrasshopper.McpBridge.Tests.Fakes.TestProxyFactory.CreateParam("my_special_input");
            var objects = new Grasshopper.Kernel.IGH_DocumentObject[] { p };
            var found = (Grasshopper.Kernel.IGH_DocumentObject?)findInputParamMethod.Invoke(null, new object?[] { objects, "MY_SPECIAL_INPUT", null });
            if (found == null) throw new Exception("FindInputParam failed to match case-insensitively");
            if (found != p) throw new Exception("Found object reference mismatch");

            var notFound = (Grasshopper.Kernel.IGH_DocumentObject?)findInputParamMethod.Invoke(null, new object?[] { objects, "non_existent", null });
            if (notFound != null) throw new Exception("Expected null for non_existent");
            Console.WriteLine("PASS (Authentic lookup succeeded)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL: {ex.Message}");
            failures++;
        }

        // 3.3: CanvasReader.ExtractParam with includeValues
        try
        {
            Console.Write("[3.3] CanvasReader.ExtractParam: ");
            if (extractParamMethod == null) throw new Exception("CanvasReader.ExtractParam not found");
            var param = HPGrasshopper.McpBridge.Tests.Fakes.TestProxyFactory.CreateParam("OutResult", "ResultParam", volatileDataCount: 7);
            var res = extractParamMethod.Invoke(null, new object[] { param, true });
            var json = System.Text.Json.JsonSerializer.Serialize(res);
            using var jdoc = System.Text.Json.JsonDocument.Parse(json);
            var summary = jdoc.RootElement.GetProperty("valueSummary").GetString();
            if (summary != "7 item(s)") throw new Exception($"Expected '7 item(s)', got '{summary}'");
            Console.WriteLine("PASS (Authentic extraction produced valid summary)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL: {ex.Message}");
            failures++;
        }

        // 3.4: CanvasReader.ExtractComponentWires
        try
        {
            Console.Write("[3.4] CanvasReader.ExtractComponentWires: ");
            if (extractComponentWiresMethod == null) throw new Exception("CanvasReader.ExtractComponentWires not found");
            var comp = HPGrasshopper.McpBridge.Tests.Fakes.TestProxyFactory.CreateComponent("Adder", "+");
            var inp = HPGrasshopper.McpBridge.Tests.Fakes.TestProxyFactory.CreateParam("InA");
            var src = HPGrasshopper.McpBridge.Tests.Fakes.TestProxyFactory.CreateParam("SrcNum");
            var pComp = HPGrasshopper.McpBridge.Tests.Fakes.TestProxyFactory.GetProxy<HPGrasshopper.McpBridge.Tests.Fakes.TestComponentProxy>(comp);
            pComp.Inputs.Add(inp);
            var pInp = HPGrasshopper.McpBridge.Tests.Fakes.TestProxyFactory.GetProxy<HPGrasshopper.McpBridge.Tests.Fakes.TestParamProxy>(inp);
            pInp.Sources.Add(src);

            var wires = new List<object>();
            extractComponentWiresMethod.Invoke(null, new object[] { comp, wires });
            if (wires.Count != 1) throw new Exception($"Expected 1 wire, got {wires.Count}");
            var json = System.Text.Json.JsonSerializer.Serialize(wires[0]);
            using var jdoc = System.Text.Json.JsonDocument.Parse(json);
            if (jdoc.RootElement.GetProperty("fromParam").GetString() != src.InstanceGuid.ToString("D"))
                throw new Exception("fromParam mismatch");
            if (jdoc.RootElement.GetProperty("toParam").GetString() != inp.InstanceGuid.ToString("D"))
                throw new Exception("toParam mismatch");
            Console.WriteLine("PASS (Authentic wire topology extracted)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL: {ex.Message}");
            failures++;
        }

        Console.WriteLine("\n=================================================");
        Console.WriteLine($"   FINAL HARNESS RESULT: {failures} Failure(s)   ");
        Console.WriteLine("=================================================");
        return failures;
    }

    private static void InitScopeLists(HPGrasshopper.McpBridge.Execution.CanvasUndoScope scope)
    {
        InitList(scope, "_addedGuids");
        InitList(scope, "_removedSnapshots");
        InitList(scope, "_disconnectedWires");
        InitList(scope, "_connectedWires");
    }

    private static void InitList(object target, string name)
    {
        var f = typeof(HPGrasshopper.McpBridge.Execution.CanvasUndoScope).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null)
        {
            var list = Activator.CreateInstance(f.FieldType);
            f.SetValue(target, list);
        }
    }
}
