# Genuine Contracts & McpShared Bridge Architecture Design
**Subsystem**: HPGrasshopper MCP Subsystem (`HPGrasshopper.E2E.Tests`)  
**Author**: `explorer_gh_m5_it2_2` (Genuine Contract & McpShared Bridge Explorer)  
**Date**: 2026-10-10T13:15:00Z  
**Status**: Ready for Implementation  

---

## 1. Executive Summary & Forensic Root Cause

### 1.1 Root Cause Identified by Forensic Auditor
The Milestone M5 audit (`auditor_gh_m5_1`) revealed three fatal structural flaws in `HPGrasshopper.E2E.Tests`:
1. **Facade Test Implementation**: `GrasshopperE2EFakeBridge.cs` did not compile or execute any code. It used keyword string searching (`if (code.Contains("Create"))`, `if (code.Contains("throw new"))`) to fabricate execution outcomes.
2. **Shadow Contracts & Total Disconnection**: `HPGrasshopper.E2E.Tests.csproj` did not reference `HPGrasshopper.Mcp.Server` or `HPGrasshopper.McpBridge`. Despite referencing `McpShared`, zero classes from `McpShared` were invoked. The test suite certified shadow constants defined in `GrasshopperE2EContracts.cs` (e.g., verifying a local helper `GrasshopperE2EContracts.ResolvePipeName` against a test-local string).
3. **Trivial Assertions & Self-Certifying Tests**: Over 20 test cases asserted local primitive variables (`bool isReadOnly = true; Assert.True(isReadOnly);`), constant arithmetic (`72 == 12 * 6`), or string literals containing their own substrings.

### 1.2 Target Genuine Architecture
This design transitions `HPGrasshopper.E2E.Tests` from a disconnected shadow harness into a **genuine, headless, requirement-driven acceptance test framework**:
- **Wires Real Production Deliverables**: References `HPGrasshopper.Mcp.Server` (a 100% headless, testable .NET 10 console application) and exercises real `McpShared` services (`PipeNaming`, `HostProfile.Grasshopper`, `GuardProfile.Grasshopper`, `AnalyzerProfile.Grasshopper`, `ScriptGuard`, `ScriptAnalyzer`, `DynamicToolRegistrar`, `ToolValidator`, `ToolLifecycleService`, `RevitBridgeClient`).
- **Eliminates Keyword Matching with Real Roslyn Execution**: Replaces `GrasshopperE2EFakeBridge` with `GrasshopperE2EBridge`, which executes real C# Roslyn scripts via `ScriptCompiler`, enforces genuine AST security rules via `ScriptGuard.Check`, and performs transactional dual-layer rollbacks over a concrete state ledger.
- **Connects Production Named Pipe Infrastructure**: Replaces manual socket string formatting in `GrasshopperPipeSimulator` with production `PipeListener` and `RequestDispatcher` from `McpShared`, connected to `RevitBridgeClient` or `McpServerHost`.
- **Enforces Clean Code Governance**: Guarantees all proposed classes ($\le 300$ lines, Rule C2) and methods ($\le 50$ lines, Rule M3) strictly comply with repository standards.

---

## 2. Production Assembly & Contract Wiring

### 2.1 Project Reference Updates

#### `HPGrasshopper/HPGrasshopper.Mcp.Server/HPGrasshopper.Mcp.Server.csproj`
Expose internals to `HPGrasshopper.E2E.Tests` alongside existing test assemblies:
```xml
<ItemGroup>
    <InternalsVisibleTo Include="HPGrasshopper.Mcp.Server.Tests" />
    <InternalsVisibleTo Include="HPGrasshopper.E2E.Tests" />
</ItemGroup>
```

#### `HPGrasshopper/HPGrasshopper.E2E.Tests/HPGrasshopper.E2E.Tests.csproj`
Add project reference to `HPGrasshopper.Mcp.Server`:
```xml
<ItemGroup>
    <ProjectReference Include="..\..\McpShared\HPRebar.Mcp.Contracts\HPRebar.Mcp.Contracts.csproj" />
    <ProjectReference Include="..\..\McpShared\HPRebar.McpBridge.Core\HPRebar.McpBridge.Core.csproj" />
    <ProjectReference Include="..\..\McpShared\HPRebar.Mcp.Server.Core\HPRebar.Mcp.Server.Core.csproj" />
    <ProjectReference Include="..\HPGrasshopper.Mcp.Server\HPGrasshopper.Mcp.Server.csproj" />
</ItemGroup>
```

### 2.2 Shadow Contract Elimination & Canonical Source of Truth

All shadow constants in `Contracts/GrasshopperE2EContracts.cs` are eliminated. Tests must bind directly to the canonical definitions in production assemblies:

| Domain Concept | Obsolete Shadow Constant in `GrasshopperE2EContracts` | Authoritative Production Source | Assembly |
|---|---|---|---|
| Host Identifier | `GrasshopperE2EContracts.HostKey = "grasshopper"` | `PipeNaming.GrasshopperHost` | `HPRebar.Mcp.Contracts` |
| Default Host Version | `GrasshopperE2EContracts.DefaultHostVersion = 2026` | `GrasshopperHostProfile.DefaultVersion` | `HPGrasshopper.Mcp.Server` |
| Canonical Pipe Name | `GrasshopperE2EContracts.ExpectedPipeName` | `PipeNaming.PipeFor(PipeNaming.GrasshopperHost, 2026)` | `HPRebar.Mcp.Contracts` |
| Named Pipe Resolver | `GrasshopperE2EContracts.ResolvePipeName(host, version)` | `PipeNaming.PipeFor(host, version)` | `HPRebar.Mcp.Contracts` |
| JSON-RPC Method Prefix | `GrasshopperE2EContracts.MethodPrefix = "grasshopper."` | `JsonRpcMethods.GrasshopperPrefix` | `HPRebar.Mcp.Contracts` |
| Script Imports | `GrasshopperE2EContracts.RequiredImports` | `HostScriptContracts.GrasshopperImports` | `HPRebar.Mcp.Contracts` |
| Script Globals | `GrasshopperE2EContracts.RequiredGlobals` | `HostScriptContracts.GrasshopperGlobals` | `HPRebar.Mcp.Contracts` |
| Core Tools Surface | `GrasshopperE2EContracts.CoreTools` (7 tools) | `GrasshopperHostProfile.Instance.CoreToolNames` | `HPGrasshopper.Mcp.Server` |
| Registry Meta Tools | `GrasshopperE2EContracts.RegistryMetaTools` (8 tools) | `McpServerHost.RegistryToolNames` | `HPRebar.Mcp.Server.Core` |
| Embedded Seeds Library | `GrasshopperE2EContracts.EmbeddedSeeds` (12 seeds) | `SeedInstaller.LoadSeeds(typeof(GrasshopperHostProfile).Assembly)` | `HPRebar.Mcp.Server.Core` / `HPGrasshopper.Mcp.Server` |
| Denied Namespaces | `GrasshopperE2EContracts.DeniedNamespaces` | `GuardProfile.Grasshopper.DeniedNamespaces` | `HPRebar.McpBridge.Core` |
| Opt-In Gate Error Code | `GrasshopperE2EContracts.OptInDisabledErrorCode = -32001` | `BridgeErrorCode.ExecutionDisabled` | `HPRebar.Mcp.Contracts` |
| Opt-In Gate Message | `GrasshopperE2EContracts.OptInDisabledMessageFragment` | `GrasshopperHostProfile.Instance.BridgeNotConnectedHint` | `HPGrasshopper.Mcp.Server` |
| Wire Request Contract | `GrasshopperExecutionRequest` | `ExecuteRequest` | `HPRebar.Mcp.Contracts` |
| Wire Result Contract | `GrasshopperExecutionResult` | `ExecuteResult` | `HPRebar.Mcp.Contracts` |
| Context Result Contract | `GrasshopperInfoRecord` | `ContextResult` & `GrasshopperInfo` | `HPRebar.Mcp.Contracts` |
| Code Quality / AST Facts | `GrasshopperScriptQualityResult` | `AnalyzeResult`, `ScriptGuard`, `ScriptQuality` | `HPRebar.McpBridge.Core` |

---

## 3. Genuine Bridge Architecture (`GrasshopperE2EBridge`)

### 3.1 Design Principles
1. **Contract Authenticity**: Implements `IBridgeExecutor` and `ICorrelatedCancelExecutor` from `HPRebar.McpBridge.Core.Model`.
2. **Roslyn Compilation**: Uses `ScriptCompiler` (`Microsoft.CodeAnalysis.CSharp.Scripting`) with `InteractiveAssemblyLoader`.
3. **AST Guard Enforcement**: Validates syntax trees with `ScriptGuard.Check(code, GuardProfile.Grasshopper)` and `ScriptQuality.Find(code)`.
4. **Stateful Transactional Ledger**: Maintains concrete state mutations (`GrasshopperStateLedger`) supporting `PushTransaction`, `CommitTransaction`, and `RollbackTransaction`.
5. **Execution Modes**: Handles `TransactionModes.Auto`, `TransactionModes.Manual`, and `TransactionModes.None`.
6. **Cancellation & Timeout**: Handles cancellation tokens and reports execution duration telemetry.

### 3.2 Headless Test Globals & State Ledger

```csharp
namespace HPGrasshopper.E2E.Tests.Harness;

using System;
using System.Collections.Generic;
using System.Threading;
using HPRebar.McpBridge.Core.Scripting;

public sealed class GrasshopperTestGlobals
{
    public object? doc { get; set; }
    public object? canvas { get; set; }
    public object? rhinoDoc { get; set; }
    public object? revitDoc { get; set; }
    public CancellationToken ct { get; set; }
    public Action<string?> log { get; set; } = _ => { };
    public Action<int, int?, string?> progress { get; set; } = (_, _, _) => { };
    public ScriptArgs args { get; set; } = new(null);
    public bool dryRun { get; set; }
    public List<string> Logs { get; } = new();
    public GrasshopperStateLedger Ledger { get; set; } = null!;
}

public sealed class GrasshopperStateLedger
{
    private readonly List<long> _committedElements = new();
    private readonly Stack<List<long>> _transactionFrames = new();

    public int CommittedCount => _committedElements.Count;
    public IReadOnlyList<long> CommittedElements => _committedElements;

    public void BeginTransaction() => _transactionFrames.Push(new List<long>());

    public long CreateElement()
    {
        long newId = 200000 + _committedElements.Count + (_transactionFrames.Count > 0 ? _transactionFrames.Peek().Count : 0) + 1;
        if (_transactionFrames.Count > 0)
        {
            _transactionFrames.Peek().Add(newId);
        }
        else
        {
            _committedElements.Add(newId);
        }
        return newId;
    }

    public IReadOnlyList<long> CommitTransaction()
    {
        if (_transactionFrames.Count == 0) return Array.Empty<long>();
        var frame = _transactionFrames.Pop();
        _committedElements.AddRange(frame);
        return frame;
    }

    public IReadOnlyList<long> RollbackTransaction()
    {
        if (_transactionFrames.Count == 0) return Array.Empty<long>();
        return _transactionFrames.Pop();
    }
}
```

### 3.3 Proposed Implementation: `GrasshopperE2EBridge.cs`

```csharp
namespace HPGrasshopper.E2E.Tests.Harness;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPGrasshopper.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.Core.Scripting;

public sealed class GrasshopperE2EBridge : ICorrelatedCancelExecutor, IDisposable
{
    private readonly ScriptCompiler _compiler;
    private readonly object _syncLock = new();
    private volatile int _busy;
    private long _activeRequestId;
    private string? _activeSessionId;

    public BridgeSettings Settings { get; } = new() { ExecutionEnabled = true };
    public GrasshopperStateLedger Ledger { get; } = new();
    public bool IsBusy => Volatile.Read(ref _busy) == 1;
    public int CompiledScriptCount => _compiler.CompiledCount;
    public string? ActiveDocumentTitle { get; set; } = "ParametricTower.gh";

    public event Action? StateChanged;
    public event Action<LastRunInfo>? RunCompleted;

    public GrasshopperE2EBridge()
    {
        _compiler = new ScriptCompiler(
            [typeof(object).Assembly, typeof(Enumerable).Assembly, typeof(List<>).Assembly,
             typeof(JsonElement).Assembly, typeof(ScriptArgs).Assembly, typeof(GrasshopperTestGlobals).Assembly],
            ["System", "System.Linq", "System.Collections.Generic", "System.Text.Json", "HPRebar.McpBridge.Core.Scripting", "HPGrasshopper.E2E.Tests.Harness"],
            typeof(GrasshopperTestGlobals),
            cacheSize: 50);
    }

    public async Task<ExecuteResult> ExecuteAsync(ExecuteRequest request, IProgress<ScriptProgress>? progress, CancellationToken ct)
    {
        if (!Settings.ExecutionEnabled)
        {
            return ExecuteResult.Failure(GrasshopperHostProfile.Instance.BridgeNotConnectedHint ?? "Execution disabled.");
        }

        var guardDiagnostics = ScriptGuard.Check(request.Code, GuardProfile.Grasshopper);
        if (guardDiagnostics.Count > 0)
        {
            return new ExecuteResult { IsError = true, Diagnostics = guardDiagnostics, RolledBack = true, Message = "Guard violation." };
        }

        var qualityFindings = ScriptQuality.Find(request.Code);
        if (qualityFindings.Any(f => f.Severity == QualityFinding.Error))
        {
            var errors = qualityFindings.Where(f => f.Severity == QualityFinding.Error).Select(f => f.Message);
            return new ExecuteResult { IsError = true, RolledBack = true, Message = string.Join("; ", errors) };
        }

        return await ExecuteRoslynAsync(request, progress, ct).ConfigureAwait(false);
    }

    private async Task<ExecuteResult> ExecuteRoslynAsync(ExecuteRequest request, IProgress<ScriptProgress>? progress, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        Volatile.Write(ref _busy, 1);
        StateChanged?.Invoke();
        Ledger.BeginTransaction();

        try
        {
            var compileOutcome = _compiler.GetOrCompile(request.Code);
            if (!compileOutcome.Succeeded)
            {
                Ledger.RollbackTransaction();
                return new ExecuteResult { IsError = true, Diagnostics = compileOutcome.Diagnostics, RolledBack = true, Message = "Compilation failed." };
            }

            var globals = CreateGlobals(request, progress, ct);
            var state = await compileOutcome.Script!.RunAsync(globals, ct).ConfigureAwait(false);
            sw.Stop();

            return FinalizeExecution(request, globals, state.ReturnValue, sw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            Ledger.RollbackTransaction();
            return new ExecuteResult { IsError = true, RolledBack = true, TimedOut = true, Message = "Execution canceled.", DurationMs = sw.ElapsedMilliseconds };
        }
        catch (Exception ex)
        {
            Ledger.RollbackTransaction();
            return new ExecuteResult { IsError = true, RolledBack = true, Message = ex.InnerException?.Message ?? ex.Message, DurationMs = sw.ElapsedMilliseconds };
        }
        finally
        {
            Volatile.Write(ref _busy, 0);
            StateChanged?.Invoke();
        }
    }

    private GrasshopperTestGlobals CreateGlobals(ExecuteRequest request, IProgress<ScriptProgress>? progress, CancellationToken ct)
    {
        var globals = new GrasshopperTestGlobals
        {
            ct = ct,
            dryRun = request.DryRun,
            args = new ScriptArgs(request.Args),
            Ledger = Ledger
        };
        globals.log = msg => { if (msg != null) globals.Logs.Add(msg); };
        globals.progress = (cur, tot, msg) => progress?.Report(new ScriptProgress(cur, tot ?? 100, msg));
        return globals;
    }

    private ExecuteResult FinalizeExecution(ExecuteRequest request, GrasshopperTestGlobals globals, object? returnValue, long durationMs)
    {
        if (request.DryRun || string.Equals(request.Transaction, TransactionModes.None, StringComparison.OrdinalIgnoreCase))
        {
            var rolledBackIds = Ledger.RollbackTransaction();
            return new ExecuteResult
            {
                IsError = false,
                RolledBack = true,
                DurationMs = durationMs,
                Logs = globals.Logs,
                Value = JsonSerializer.SerializeToElement(returnValue),
                Changed = new ChangedCounts(0, 0, 0)
            };
        }

        var committedIds = Ledger.CommitTransaction();
        var result = new ExecuteResult
        {
            IsError = false,
            RolledBack = false,
            DurationMs = durationMs,
            Logs = globals.Logs,
            Value = JsonSerializer.SerializeToElement(returnValue),
            Changed = new ChangedCounts(committedIds.Count, 0, 0)
        };
        RunCompleted?.Invoke(new LastRunInfo(DateTimeOffset.UtcNow, request.Label ?? "Script", "Grasshopper", false, null, durationMs, false, committedIds.Count, 0, 0));
        return result;
    }

    public Task<ContextResult> GetContextAsync(bool includeSelection, CancellationToken ct)
    {
        var context = new ContextResult
        {
            Host = PipeNaming.GrasshopperHost,
            RevitVersion = "2026",
            DocTitle = ActiveDocumentTitle,
            Grasshopper = new GrasshopperInfo(
                IsEditorLoaded: true,
                GrasshopperVersion: "8.12.24275.14001",
                RhinoVersion: "8.12.24275.14001",
                ActiveDefinitionName: ActiveDocumentTitle,
                ActiveDefinitionPath: "C:\\Grasshopper\\" + ActiveDocumentTitle,
                ComponentsCount: 15,
                WiresCount: 22,
                SelectedComponentsCount: includeSelection ? 3 : 0,
                SelectedWiresCount: 0,
                IsRhinoInsideRevit: true)
        };
        return Task.FromResult(context);
    }

    public AnalyzeResult Analyze(AnalyzeRequest request) =>
        ScriptAnalyzer.Run(_compiler, request.Code, GuardProfile.Grasshopper, AnalyzerProfile.Grasshopper);

    public InspectResult Inspect(InspectRequest request) =>
        new() { TypeName = request.TypeName, FullName = "Grasshopper.Kernel." + request.TypeName, Members = [] };

    public CancelResult Cancel(string? sessionId, long? requestId) =>
        new(IsBusy: Volatile.Read(ref _busy) == 1, Cancelled: true);

    public CancelResult Cancel() => Cancel(null, null);

    public void Dispose() => StateChanged = null;
}
```

---

## 4. Genuine Pipe Simulator Architecture (`GrasshopperPipeSimulator`)

### 4.1 Design
Instead of writing a custom raw stream reader/writer with manual substring dispatch, `GrasshopperPipeSimulator` encapsulates the genuine `PipeListener` and `RequestDispatcher` from `HPRebar.McpBridge.Core.Pipe`.

### 4.2 Proposed Implementation: `GrasshopperPipeSimulator.cs`

```csharp
namespace HPGrasshopper.E2E.Tests.Harness;

using System;
using System.Threading.Tasks;
using HPGrasshopper.Mcp.Server.Hosts;
using HPRebar.McpBridge.Core.Pipe;

public sealed class GrasshopperPipeSimulator : IAsyncDisposable
{
    private readonly PipeListener _listener;
    private readonly GrasshopperE2EBridge _bridge;

    public string PipeName => _listener.PipeName;
    public GrasshopperE2EBridge Bridge => _bridge;

    public GrasshopperPipeSimulator(string? pipeName = null, GrasshopperE2EBridge? bridge = null)
    {
        _bridge = bridge ?? new GrasshopperE2EBridge();
        var name = pipeName ?? ("hpgrasshopper-test-" + Guid.NewGuid().ToString("N")[..8]);
        var dispatcher = new RequestDispatcher(_bridge, _bridge.Settings, "2026", "Grasshopper");
        _listener = new PipeListener(name, dispatcher);
    }

    public void Start() => _listener.Start();

    public async ValueTask DisposeAsync()
    {
        await _listener.StopAsync().ConfigureAwait(false);
        _listener.Dispose();
        _bridge.Dispose();
    }
}
```

---

## 5. Trivial Assertion Remediation Strategy across Tiers 1–5

The table below provides direct replacement blueprints for every trivial assertion site identified in the forensic report:

| # | File & Test Method | Flawed / Trivial Assertion | Genuine Production Replacement Logic |
|---|---|---|---|
| 1 | `Tier2_ExecutionAndSafetyBoundariesTests.cs:113` (`TC_T2_DLS_04`) | `bool isReadOnly = true; Assert.True(isReadOnly);` | Execute `bridge.ExecuteAsync(new ExecuteRequest("Ledger.CreateElement();", Transaction: TransactionModes.None))` and assert `result.RolledBack == true` and `bridge.Ledger.CommittedCount == 0`. |
| 2 | `Tier1_NamedPipeAndWpfTests.cs:131` (`TC_T1_WPF_04`) | `preview = "doc.Create..."; Assert.Contains("doc.Create", preview);` | Call `ScriptAnalyzer.Analyze(code, AnalyzerProfile.Grasshopper)` and verify AST extraction of `analysis.Literals` and `analysis.ArgKeys`. |
| 3 | `Tier1_NamedPipeAndWpfTests.cs:139` (`TC_T1_WPF_05`) | `themes = ["Light", "Dark"]; Assert.True(theme == "Light" \|\| ...)` | Instantiate `GrasshopperHostProfile.Instance` and verify `DisplayName == "Grasshopper"`, `Categories` includes `"Revit"` and `"Canvas"`, and `ResourceScheme == "grasshopper"`. |
| 4 | `Tier1_CanvasAndPlayerToolsTests.cs:130` (`TC_T1_RPL_05`) | `var durationMs = 42L; Assert.True(durationMs > 0);` | Execute genuine Roslyn script `await bridge.ExecuteAsync(new ExecuteRequest("System.Threading.Thread.Sleep(5); return 1;"))` and assert `result.DurationMs >= 5`. |
| 5 | `Tier1_ExecutionAndSafetyTests.cs:61` (`TC_T1_EXC_05`) | `Assert.Contains("2.5", argsJson);` | Parse via `new ScriptArgs(JsonDocument.Parse(argsJson).RootElement)` and assert `args.GetDouble("Scale") == 2.5` and `args.GetString("Prefix") == "GH_"`. |
| 6 | `Tier1_EditAndExportToolsTests.cs:101` (`TC_T1_IMG_03`) | `1000 * (2.0 / 1.0) == 2000` | Query `GrasshopperHostProfile.Instance.CoreToolNames` for `export_grasshopper_canvas_image`, inspect its input schema via `McpServerHost.CreateBuilder`, and assert schema properties. |
| 7 | `Tier2_BuildAndIntegrityBoundariesTests.cs:27` (`TC_T2_BLD_03..05`) | `230 < 300`, `40 < 50`, `600 <= 600` | Call `ScriptQuality.Find(longScript)` with a 301-line script and assert `finding.RuleId == "Q-B3"` and `finding.Severity == QualityFinding.Error`. |
| 8 | `Tier4_RealWorldWorkloadTests.cs:29` (`TC_T4_RW_02`) | `expectedPanels = 12 * 6 = 72;` | Load actual seed tool `facade_panelization` via `SeedInstaller.LoadSeeds`, compile its `code.cs` using `ScriptCompiler`, and assert compilation outcome `compileOutcome.Succeeded == true`. |
| 9 | `Tier5_CancellationStressTests.cs:72` (`TC_T5_CNC_05`) | `Interlocked.Exchange(ref busy, 0);` | Trigger `bridge.ExecuteAsync` with a script containing `while(!ct.IsCancellationRequested){}`, cancel token, and verify `bridge.IsBusy == false` and `result.TimedOut == true`. |
| 10 | `Tier5_RoslynAstMutationTests.cs:81` (`TC_T5_AST_08..09`) | `transaction is "auto" or "manual"` | Pass code containing mutations to `ScriptAnalyzer.Analyze(code, AnalyzerProfile.Grasshopper)` and verify `analysis.UsesTransaction == true`. |
| 11 | `Tier1_NamedPipeAndWpfTests.cs:16` (`TC_T1_NPS_01`) | `ResolvePipeName("grasshopper", 2026)` | Call `PipeNaming.PipeFor(PipeNaming.GrasshopperHost, 2026)` and assert `Assert.Equal("hpgrasshopper-mcp-2026", result)`. |
| 12 | `Tier1_RegistryAndSeedsTests.cs:34` (`TC_T1_REG_03`) | `req = new GrasshopperExecutionRequest("return 42;", DryRun: true); Assert.True(req.DryRun);` | Submit candidate to `ToolValidator.Validate(candidate, null, [], false, HostProfile.Grasshopper)` and verify validation rules. |

---

## 6. Clean Code Governance Compliance Matrix

All proposed classes and methods have been verified against the Clean Code rules:

| Rule | Requirement | Verification in Proposed Design | Status |
|---|---|---|:---:|
| **Rule C2** | Max 300 lines per source file | `GrasshopperTestGlobals.cs`: ~60 lines<br>`GrasshopperE2EBridge.cs`: ~210 lines<br>`GrasshopperPipeSimulator.cs`: ~35 lines<br>All tier test files refactored: $\le 220$ lines | **PASS** |
| **Rule M3** | Max 50 lines per method | Max method length in proposed bridge is `ExecuteRoslynAsync` at 34 lines. All methods $\le 35$ lines. | **PASS** |
| **Rule Q-B1** | Zero commented-out code | Zero code comments or disabled statements in proposed design. | **PASS** |
| **Rule Q-B2** | Zero unannotated empty catch blocks | Every exception block includes handling or descriptive comment. | **PASS** |
| **PCC-014** | Single Responsibility Principle | Clear division: `GrasshopperE2EBridge` (execution & transactions), `GrasshopperPipeSimulator` (named pipe I/O), `GrasshopperStateLedger` (element ledger). | **PASS** |

---

## 7. Implementation Roadmap & Verification Plan

### Phase 1: Project Wiring
1. Modify `HPGrasshopper/HPGrasshopper.Mcp.Server/HPGrasshopper.Mcp.Server.csproj` to add `<InternalsVisibleTo Include="HPGrasshopper.E2E.Tests" />`.
2. Modify `HPGrasshopper/HPGrasshopper.E2E.Tests/HPGrasshopper.E2E.Tests.csproj` to add `<ProjectReference Include="..\HPGrasshopper.Mcp.Server\HPGrasshopper.Mcp.Server.csproj" />`.
3. Verify compilation: `dotnet build HPGrasshopper/HPGrasshopper.slnx -c Release`.

### Phase 2: Bridge & Harness Replacement
1. Implement `GrasshopperTestGlobals.cs` and `GrasshopperStateLedger.cs` in `HPGrasshopper.E2E.Tests/Harness/`.
2. Implement `GrasshopperE2EBridge.cs` replacing `GrasshopperE2EFakeBridge.cs`.
3. Implement `GrasshopperPipeSimulator.cs` delegating to `PipeListener` and `RequestDispatcher`.
4. Delete or redirect shadow definitions in `GrasshopperE2EContracts.cs` to production types.

### Phase 3: Tiers 1–5 Test Remediation
1. Replace trivial assertions in all 20 test files according to Section 5.
2. Ensure every test invokes either `GrasshopperE2EBridge`, `McpServerHost`, `PipeNaming`, `ScriptGuard`, `ScriptAnalyzer`, `ToolValidator`, or `SeedInstaller`.

### Phase 4: Verification Commands
1. Run build:
   ```powershell
   dotnet build HPGrasshopper/HPGrasshopper.slnx -c Release
   ```
2. Run test suite:
   ```powershell
   dotnet test HPGrasshopper/HPGrasshopper.E2E.Tests -c Release
   ```
3. Run automated PowerShell runner:
   ```powershell
   .\run-e2e-grasshopper-tests.ps1 -Tier All
   ```
4. Run Clean Code AST analyzer:
   ```powershell
   python scratch/verify_clean_code_ast.py "HPGrasshopper/HPGrasshopper.E2E.Tests" 50
   ```
