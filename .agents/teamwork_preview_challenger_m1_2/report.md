# Empirical Challenge Report — Challenger 2 (Milestone M1: Net48 Runtime, Roslyn & Host Neutrality)

- **Author**: Challenger 2 (`teamwork_preview_challenger_m1_2`) — Empirical Challenger & Adversarial Reviewer
- **Target**: Milestone 1 (`McpShared` additive integration for Tekla Structures 2025)
- **Repo Root**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar`
- **Execution Runtime**: Windows Desktop CLR `v4.0.30319` (.NET Framework 4.8.9181.0, x64)
- **Date**: 2026-09-22
- **Verdict**: **REQUEST_CHANGES** (Action required on `GuardProfile.Tekla`)

---

## 1. Challenge Summary

**Overall risk assessment**: **CRITICAL**

While the additive integration into `McpShared` successfully maintains build integrity, passes baseline test suites, executes cleanly on desktop CLR v4.0.30319, and achieves 100% host neutrality across both net10 and net48 assets, an empirical stress-test of `GuardProfile.Tekla` on the desktop CLR has exposed **three confirmed vulnerabilities**:
1. **Parenthesized Receiver Bypass**: `(model).CommitChanges()` slips past `deniedMembersOnIdentifier` because the receiver is parsed as `ParenthesizedExpressionSyntax`, which fails the type check `node.Expression is IdentifierNameSyntax`.
2. **Aliased Receiver / Instance Bypass**: `var m = model; m.CommitChanges()` or `new Model().CommitChanges()` slips past the guard because `CommitChanges` was restricted solely to receiver `"model"` instead of being placed in `DeniedMembers`.
3. **Missing Interactive Picking Method**: `picker.PickFace()` on `Tekla.Structures.Model.UI.Picker` is omitted from `DeniedMembers` (which included `PickPolygon`, a non-existent method in Tekla 2025, instead of `PickFace`). Calling `PickFace()` hangs the Tekla main thread waiting indefinitely for user interaction.

---

## 2. Detailed Challenges & Confirmed Vulnerabilities

### [Critical] Challenge 1: `(model).CommitChanges()` Parenthesized Syntax Bypass

- **Assumption challenged**: The worker assumed that restricting `CommitChanges` via `deniedMembersOnIdentifier: new Dictionary<string, string[]> { ["model"] = new[] { "CommitChanges" } }` would prevent scripts from committing changes to the Tekla database directly.
- **Attack scenario**:
  An AI agent or user script executes:
  ```csharp
  (model).CommitChanges();
  return 1;
  ```
  In `McpShared/HPRebar.McpBridge.Core/Scripting/ScriptGuard.cs` (lines 127–130):
  ```csharp
  // Members denied only on a named global, e.g. tr.Commit() — the bridge owns that transaction.
  if (node.Expression is IdentifierNameSyntax { Identifier.ValueText: var receiver }
      && profile.DeniedMembersOnIdentifier.TryGetValue(receiver, out var denied)
      && Array.IndexOf(denied, member) >= 0)
      Report(node.Name, $"{receiver}.{member} is not allowed: the bridge owns `{receiver}` and commits or rolls it back for you.");
  ```
  `node.Expression` is `(model)`, which is a `ParenthesizedExpressionSyntax`. The pattern `node.Expression is IdentifierNameSyntax` evaluates to `false`. Furthermore, `node.Name` is `CommitChanges`, which is NOT in `profile.DeniedMembers`. As a result, `ScriptGuard.Check` returns **0 violations**.
- **Blast radius**: **CRITICAL**. The script bypasses bridge transaction ownership and executes a database commit during `dryRun = true` operations, corrupting model state and invalidating automatic snapshot restoration.
- **Mitigation**:
  Add `"CommitChanges"` to `GuardProfile.Tekla.DeniedMembers`. In Tekla Structures, no script running through an MCP agent should ever call `.CommitChanges()` directly on any receiver, because the bridge is the sole owner of transaction commit, rollback, and dryRun safety.

---

### [Critical] Challenge 2: Aliased Receiver Bypass (`m.CommitChanges()`)

- **Assumption challenged**: The worker assumed scripts would only invoke `CommitChanges` directly on the global identifier named `"model"`.
- **Attack scenario**:
  An AI agent or user script writes:
  ```csharp
  var m = model;
  m.CommitChanges();
  return 1;
  ```
  or creates an instance:
  ```csharp
  var myModel = new Tekla.Structures.Model.Model();
  myModel.CommitChanges();
  return 1;
  ```
  Here, `receiver` is `"m"` or `"myModel"`. `profile.DeniedMembersOnIdentifier` only checks key `"model"`. Because `CommitChanges` was omitted from `profile.DeniedMembers`, `ScriptGuard.Check` returns **0 violations**.
- **Blast radius**: **CRITICAL**. Tekla Structures database commits are process-wide per model. Committing via an alias commits all uncommitted changes created by the script, bypassing the bridge's transaction management and `dryRun = true` simulation mode.
- **Mitigation**:
  Place `"CommitChanges"` into `GuardProfile.Tekla.DeniedMembers`.

---

### [High] Challenge 3: Omission of `PickFace` from `GuardProfile.Tekla.DeniedMembers`

- **Assumption challenged**: The worker assumed the list `new[] { "PickObject", "PickObjects", "PickPoint", "PickPoints", "PickLine", "PickPolygon" }` covered all interactive picking methods in Tekla Open API.
- **Attack scenario**:
  An AI agent or user script invokes:
  ```csharp
  picker.PickFace();
  ```
  Inspection of `Tekla.Structures.Model.UI.Picker` in `Tekla.Structures.Model.dll` reveals its exact declared interactive picking methods:
  - `PickPoint`
  - `PickPoints`
  - `PickLine`
  - `PickFace` *(Omitted by worker)*
  - `PickObject`
  - `PickObjects`
  Notice that `PickPolygon` does not exist on `Picker` in Tekla 2025.0 (likely mistakenly copied from AutoCAD's prompt list), whereas `PickFace` is a core picking method.
  When `picker.PickFace()` is called, `ScriptGuard.Check` returns **0 violations**.
- **Blast radius**: **HIGH**. Calling `PickFace()` prompts the user to select a face in the 3D viewport. The call blocks the Tekla UI/main thread synchronously, freezing pipe communication, triggering named pipe client timeouts, and leaving the bridge in an unresponsive state.
- **Mitigation**:
  Add `"PickFace"` to `GuardProfile.Tekla.DeniedMembers`. (Optionally retain `"PickPolygon"` for defense-in-depth or drawing API variants).

---

## 3. Empirical Stress Test Results on Desktop CLR (.NET Framework 4.8)

A dedicated empirical stress suite `TeklaMilestone1Challenger2Net48Tests.cs` (260 lines) was placed in `McpShared/HPRebar.McpBridge.Core.Net48Tests` and executed on desktop CLR:

```powershell
dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests
```

### 3.1 Test Execution Output (Desktop CLR v4.0.30319)

```
Running tests from G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.McpBridge.Core.Net48Tests\bin\Debug\net48\HPRebar.McpBridge.Core.Net48Tests.exe (net48|x64)
Standard output: xUnit.net v3 Microsoft.Testing.Platform Runner v3.1.0+03a071627b (64-bit .NET Framework 4.8.9181.0)
G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.McpBridge.Core.Net48Tests\bin\Debug\net48\HPRebar.McpBridge.Core.Net48Tests.exe (net48|x64) passed (2s 289ms)

Test run summary: Passed!
  total: 109
  failed: 0
  succeeded: 109
  skipped: 0
  duration: 2s 703ms
```

### 3.2 Detailed Stress Scenarios Matrix

| # | Scenario / Input | Expected Secure Behavior | Actual Behavior in Current Code | Result | Finding Reference |
|---|---|---|---|---|---|
| 1 | Runtime Environment Verification | Execute on Desktop CLR v4.0.30319 | Framework: `.NET Framework 4.8.9181.0`, CLR: `v4.0.30319` | **PASS** | CLR Verified |
| 2 | `model.CommitChanges();` | Blocked with GUARD violation | Blocked (`model.CommitChanges is not allowed`) | **PASS** | Baseline rule |
| 3 | `(model).CommitChanges();` | Blocked with GUARD violation | **0 violations (Bypassed guard)** | **CONFIRMED VULNERABILITY** | Challenge 1 |
| 4 | `var m = model; m.CommitChanges();` | Blocked with GUARD violation | **0 violations (Bypassed guard)** | **CONFIRMED VULNERABILITY** | Challenge 2 |
| 5 | `picker.PickFace();` | Blocked with GUARD violation | **0 violations (Bypassed guard)** | **CONFIRMED VULNERABILITY** | Challenge 3 |
| 6 | `picker.PickObject();` | Blocked with GUARD violation | Blocked (`.PickObject is not allowed`) | **PASS** | Baseline rule |
| 7 | `picker.PickPoints();` | Blocked with GUARD violation | Blocked (`.PickPoints is not allowed`) | **PASS** | Baseline rule |
| 8 | `picker.PickLine();` | Blocked with GUARD violation | Blocked (`.PickLine is not allowed`) | **PASS** | Baseline rule |
| 9 | `picker.PickPolygon();` | Blocked with GUARD violation | Blocked (`.PickPolygon is not allowed`) | **PASS** | Baseline rule |
| 10 | `MessageBox.Show("modal");` | Blocked with GUARD violation | Blocked (`MessageBox is not allowed`) | **PASS** | Baseline rule |
| 11 | `System.Windows.Forms.MessageBox.Show();` | Blocked with GUARD violation | Blocked (`System.Windows.Forms is not allowed`) | **PASS** | Baseline rule |
| 12 | `using Tekla.Structures.Dialog;` | Blocked with GUARD violation | Blocked (`using Tekla.Structures.Dialog is not allowed`) | **PASS** | Namespace rule |
| 13 | `using Tekla.Structures.Drawing.UI;` | Blocked with GUARD violation | Blocked (`using Tekla.Structures.Drawing.UI is not allowed`) | **PASS** | Namespace rule |
| 14 | `using HPTekla.McpBridge;` | Blocked with GUARD violation | Blocked (`using HPTekla.McpBridge is not allowed`) | **PASS** | Namespace rule |
| 15 | `global::HPTekla.McpBridge.Something.Do();` | Blocked with GUARD violation | Blocked (`HPTekla.McpBridge is not allowed`) | **PASS** | Global alias defense |
| 16 | `global::Tekla.Structures.Dialog.UIFilterForm f = null;` | Blocked with GUARD violation | Blocked (`Tekla.Structures.Dialog is not allowed`) | **PASS** | Global alias defense |
| 17 | `global::HPRebar.McpBridge.Core.Host.McpBridgeHost.Current.Stop();` | Blocked with GUARD violation | Blocked (`HPRebar.McpBridge.Core.Host is not allowed`) | **PASS** | Internal host defense |
| 18 | `System.Diagnostics.Process.Start("cmd.exe");` | Blocked with GUARD violation | Blocked (`System.Diagnostics.Process is not allowed`) | **PASS** | Base list rule |
| 19 | `#r "Tekla.Structures.Model.dll"` | Blocked with GUARD violation | Blocked (`#r is not allowed`) | **PASS** | Directive defense |
| 20 | `#load "script.csx"` | Blocked with GUARD violation | Blocked (`#load is not allowed`) | **PASS** | Directive defense |
| 21 | `dynamic d = 1;` | Blocked with GUARD violation | Blocked (`dynamic is not allowed`) | **PASS** | Base list rule |
| 22 | `await Task.Delay(10);` | Blocked with GUARD violation | Blocked (`await is not allowed`) | **PASS** | Base list rule |
| 23 | `unsafe { int* p = null; }` | Blocked with GUARD violation | Blocked (`unsafe code is not allowed`) | **PASS** | Base list rule |
| 24 | Roslyn compile & run with Tekla globals on net48 | Compiles and runs returning value and logs | Returns `30`, logs `["Tekla script started", "Sum: 30"]` | **PASS** | Runtime execution |
| 25 | Roslyn compile with real Tekla 2025.0 Open API reference assemblies on net48 | Successfully compiles clean Tekla geometry script | Compiles `Tekla.Structures.Geometry3d.Point` code cleanly | **PASS** | Tekla 2025 integration |
| 26 | Host Neutrality: Shared net48 assemblies reference zero host APIs | `HPRebar.Mcp.Contracts` & `HPRebar.McpBridge.Core` have 0 host refs | 0 host references found | **PASS** | Host neutrality |

---

## 4. Host Neutrality Verification

Verification of host neutrality was conducted independently across both runtimes:
1. **.NET 10.0 Suite** (`McpShared/HPRebar.Mcp.Server.Core.Tests/HostNeutralityTests.cs`):
   - `Shared_assemblies_reference_no_host_api` verifies that `HPRebar.Mcp.Contracts`, `HPRebar.McpBridge.Core`, and `HPRebar.Mcp.Server.Core` reference zero assemblies starting with `Tekla.Structures`, `RevitAPI`, `AcDbMgd`, etc.
   - Verified 688/688 tests passing in `HPRebar.Mcp.Server.Core.Tests`.
2. **.NET Framework 4.8 Suite** (`TeklaMilestone1Challenger2Net48Tests.McpShared_net48_assemblies_reference_zero_host_apis`):
   - Inspects `PipeNaming.Assembly` (`HPRebar.Mcp.Contracts` net48) and `PipeListener.Assembly` (`HPRebar.McpBridge.Core` net48).
   - Confirmed zero references to `Tekla`, `RevitAPI`, `AcDbMgd`, `Autodesk.Navisworks`, `ETABSv1`, `SAP2000v1`, `RobotOM`, `Microsoft.Office.Interop.Excel`, or `Microsoft.AnalysisServices`.
   - Host neutrality is completely intact.

---

## 5. Action Items for Worker M1

To resolve the identified security and reliability flaws, Worker M1 must make the following changes to `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs`:

```csharp
public static readonly GuardProfile Tekla = new GuardProfile(
    "Tekla Structures",
    deniedIdentifiers: new[] { "MessageBox", "Picker" },
    deniedMembers: new[]
    {
        // Interactive UI picking methods that block waiting for mouse clicks
        "PickObject", "PickObjects", "PickPoint", "PickPoints", "PickLine", "PickPolygon", "PickFace",
        // Direct commit bypass on any receiver (bridge owns transaction commit and dryRun enforcement)
        "CommitChanges",
        // Application shutdown
        "Exit", "Quit",
    },
    deniedMembersOnIdentifier: new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        // Kept for backward compatibility and explicit error messages:
        ["model"] = new[] { "CommitChanges" },
    },
    deniedNamespaces: new[]
    {
        "System.Windows.Forms",
        "Tekla.Structures.Dialog",
        "Tekla.Structures.Drawing.UI",
        "HPTekla.McpBridge",
        "HPRebar.McpBridge.Core.Host",
    });
```

---

## 6. Unchallenged Areas

- In-process execution within `TeklaStructures.exe` (Milestone 2 scope; requires `HPTekla.McpBridge` plugin deployment).
- Stdio MCP server protocol communications (Milestone 3 scope; requires `HPTekla.Mcp.Server`).
