# Adversarial Challenge Report — Milestone 1 (McpShared Tekla Integration)

- **Agent**: `teamwork_preview_challenger_m1_1` (EMPIRICAL CHALLENGER)
- **Target**: Milestone 1 changes in `McpShared` (`HPRebar.Mcp.Contracts`, `HPRebar.McpBridge.Core`, `HPRebar.Mcp.Server.Core`)
- **Date**: 2026-09-22
- **Verdict**: **REQUEST_CHANGES**

---

## 1. Challenge Summary

**Overall risk assessment**: **HIGH** (Transaction commit bypass vulnerability in `GuardProfile.Tekla` allows scripts to commit model modifications even during `dryRun = true`).

Milestone 1 implements the additive contracts, pipe naming, and wire messages for Tekla Structures 2025. However, adversarial stress testing revealed an evasion vulnerability in the Roslyn AST security guard:
- `CommitChanges` was placed **only** in `deniedMembersOnIdentifier["model"]`.
- `ScriptGuard` only checks whether the member access expression is an exact `IdentifierNameSyntax` matching `"model"`.
- Any aliased receiver (`var m = model; m.CommitChanges();`), parenthesized expression (`(model).CommitChanges();`), or cast expression (`((Model)model).CommitChanges();`) evaluates to `0` violations and compiles cleanly.
- Because `HPTekla.McpBridge` relies on `dryRun = true` to protect user projects during evaluation passes (Requirement R2), a script invoking `m.CommitChanges()` will commit changes directly to the active Tekla model database without bridge authorization.

All other components (PipeNaming, JsonRpcMethods, ContextResult serialization, null omission, and host neutrality) passed all empirical checks with flying colors.

---

## 2. Detailed Challenges & Empirical Findings

### [Critical] Challenge 1: Transaction Commit Bypass in `GuardProfile.Tekla`

- **Assumption challenged**: That placing `CommitChanges` under `deniedMembersOnIdentifier["model"]` prevents scripts from bypassing bridge transaction ownership and committing changes during `dryRun = true`.
- **Attack scenario**:
  1. An AI agent or script writer assigns `model` to a local variable: `var m = model; m.CommitChanges();`
  2. An AI agent or script wraps `model` in parentheses: `(model).CommitChanges();`
  3. An AI agent or script casts `model`: `((Tekla.Structures.Model.Model)model).CommitChanges();`
- **Root cause analysis**:
  In `McpShared/HPRebar.McpBridge.Core/Scripting/ScriptGuard.cs`:
  ```csharp
  // Members denied only on a named global, e.g. tr.Commit() — the bridge owns that transaction.
  if (node.Expression is IdentifierNameSyntax { Identifier.ValueText: var receiver }
      && profile.DeniedMembersOnIdentifier.TryGetValue(receiver, out var denied)
      && Array.IndexOf(denied, member) >= 0)
      Report(node.Name, $"{receiver}.{member} is not allowed: the bridge owns `{receiver}` and commits or rolls it back for you.");
  ```
  `node.Expression` for `(model)` is `ParenthesizedExpressionSyntax`, and for `m` the receiver is `"m"`. Because `"m"` is not in `DeniedMembersOnIdentifier`, the check evaluates to `false`. Furthermore, `"CommitChanges"` is not in `profile.DeniedMembers`, so line 124 is skipped:
  ```csharp
  else if (profile.DeniedMembers.Contains(member))
      Report(node.Name, $".{member} is not allowed in {_host} scripts...");
  ```
- **Blast radius**:
  Direct bypass of `dryRun = true` enforcement in `HPTekla.McpBridge`. Untrusted scripts or test scripts can permanently alter or corrupt live production Tekla models during test runs.
- **Empirical reproduction**:
  Empirically verified in both `TeklaMilestone1ChallengerTests.cs` (net10) and `TeklaMilestone1Challenger2Net48Tests.cs` (net48):
  - `ScriptGuard.Check("var m = model; m.CommitChanges();", GuardProfile.Tekla)` returns **0 diagnostics**.
  - `ScriptGuard.Check("(model).CommitChanges();", GuardProfile.Tekla)` returns **0 diagnostics**.
  - `TeklaMilestone1Challenger2Net48Tests.GuardProfile_Tekla_parenthesized_receiver_bypass_test` failed with `Exit code: 2`.
  - `TeklaMilestone1Challenger2Net48Tests.GuardProfile_Tekla_aliased_receiver_bypass_test` failed with `Exit code: 2`.
- **Mitigation required**:
  Add `"CommitChanges"` to `GuardProfile.Tekla.DeniedMembers`:
  ```csharp
  public static readonly GuardProfile Tekla = new GuardProfile(
      "Tekla Structures",
      deniedIdentifiers: new[] { "MessageBox", "Picker" },
      deniedMembers: new[]
      {
          // Interactive UI picking methods that block waiting for mouse clicks
          "PickObject", "PickObjects", "PickPoint", "PickPoints", "PickLine", "PickPolygon",
          // Application shutdown
          "Exit", "Quit",
          // CommitChanges: bridge owns transaction commit and dryRun enforcement across all receivers
          "CommitChanges",
      },
      ...
  ```
  Tekla Open API does not possess any other benign member named `CommitChanges`. Adding `"CommitChanges"` to `deniedMembers` blocks any invocation of `.CommitChanges()` across all receiver syntaxes.

---

### [Pass] Challenge 1B: Other `GuardProfile.Tekla` Directives, Namespaces, and Identifiers

The following attack vectors were tested and confirmed firmly denied by `ScriptGuard.Check`:
- Direct `model.CommitChanges();` and `model?.CommitChanges();` -> **DENIED**
- `MessageBox.Show(...)` (both WinForms and bare identifier) -> **DENIED**
- `var p = new Picker();` -> **DENIED**
- `p.PickObject()`, `p.PickObjects()`, `p.PickPoint()`, `p.PickPoints()`, `p.PickLine()`, `p.PickPolygon()` across all receivers -> **DENIED**
- `Process.Start(...)`, `System.Diagnostics.Process` -> **DENIED**
- Directives `#r` and `#load` (including whitespace and tab formatting variations) -> **DENIED**
- Denied namespaces (`System.Windows.Forms`, `Tekla.Structures.Dialog`, `Tekla.Structures.Drawing.UI`, `HPTekla.McpBridge`, `HPRebar.McpBridge.Core.Host`), including attempts to evade via `global::` prefix -> **DENIED**
- Positive controls: legitimate Tekla Open API scripts creating Beams, Columns, ContourPlates, RebarGroups, querying ModelInfo, and using LINQ -> **PASS with 0 diagnostics**.

---

### [Pass] Challenge 2: PipeNaming Robustness & Pairwise Distinctness

- **Canonical verification**:
  `PipeNaming.For("tekla", 2025)` produces exactly `"hptekla-mcp-2025"`.
  `PipeNaming.For(PipeNaming.TeklaHost, 2025)` produces exactly `"hptekla-mcp-2025"`.
- **Case invariance & whitespace normalization**:
  - `"TEKLA"`, `"Tekla"`, `"tEkLa"` -> all produce `"hptekla-mcp-2025"`.
  - `"  tekla  "`, `"\ttekla\r\n"` -> all produce `"hptekla-mcp-2025"`.
- **Version handling**:
  - `PipeNaming.For("tekla", 2024)` -> `"hptekla-mcp-2024"`.
  - `PipeNaming.For("tekla", 2026)` -> `"hptekla-mcp-2026"`.
  - Version validation is correctly enforced by `BridgeOptionsValidator` via `HostProfile.ValidVersions = [2025]`; setting `Bridge:HostVersion = "1997"` throws `OptionsValidationException`.
- **Input boundary checks**:
  - `PipeNaming.For(null, 2025)` throws `ArgumentException`.
  - `PipeNaming.For("", 2025)` throws `ArgumentException`.
  - `PipeNaming.For("   ", 2025)` throws `ArgumentException`.
- **10-Host Pairwise Distinctness**:
  All 10 host constants (`revit`, `autocad`, `navis`, `etabs`, `civil3d`, `sap2000`, `powerbi`, `excel`, `robot`, `tekla`) and their generated pipe names are 100% pairwise distinct.

---

### [Pass] Challenge 3: JsonRpcMethods Bijection & Notification Dispatching

- **Prefix**: `JsonRpcMethods.TeklaPrefix` is `"tekla."` (ends with a dot).
- **Suffix extraction**:
  `JsonRpcMethods.Suffix("tekla.execute")` produces `"execute"`.
  `JsonRpcMethods.Suffix("tekla.progress")` produces `"progress"`.
  `JsonRpcMethods.Suffix("tekla.context")` produces `"context"`.
- **Progress dispatching**:
  Empirically verified via reflection on `RequestDispatcher.ProgressMethodFor`:
  - `ProgressMethodFor("tekla.execute")` produces `"tekla.progress"`.
  - `ProgressMethodFor("execute")` falls back to `JsonRpcMethods.ProgressNotification` (`"revit.progress"`).
- **Notification detectors**:
  - `JsonRpcMethods.IsProgress("tekla.progress")` == `true`
  - `JsonRpcMethods.IsLog("tekla.log")` == `true`
  - `JsonRpcMethods.IsStatus("tekla.status")` == `true`
  - Non-notifications (`tekla.execute`, `tekla.context`) return `false`.

---

### [Pass] Challenge 4: ContextResult Serialization & Wire Isolation

- **Null suppression & sibling host isolation**:
  When serializing `ContextResult` for any of the other 9 hosts (`revit`, `autocad`, `navis`, `etabs`, `civil3d`, `sap2000`, `powerbi`, `excel`, `robot`), the `"tekla"` property is completely omitted from the JSON string.
  When `context.Tekla == null`, `"tekla"` is not serialized.
- **Tekla wire payload fidelity**:
  When `TeklaInfo` is populated:
  - Formatted in strict `camelCase`: `isConnected`, `modelName`, `modelPath`, `projectName`, `teklaVersion`, `heavyOperationsEnabled`, `partCount`, `rebarCount`, `drawingCount`.
  - Deserialization round-trip preserves all values with zero data loss.
  - Sibling host payloads (`autocad`, `navis`, `etabs`, etc.) are 100% omitted from the Tekla payload.
- **ContextService shaping**:
  Calling `ContextService.ReadAsync` for Tekla strips legacy Revit-specific fields (`revitVersion`, `isFamily`), preserves `host = "tekla"`, and emits the clean `tekla` block.

---

### [Pass] Challenge 5: Named Pipe Live Round-Trip

A real in-memory Named Pipe test was executed with `PipeListener`, `RequestDispatcher`, `FakeRevitExecutor`, and `RevitBridgeClient` using `TeklaTestProfile`:
- `tekla.ping` -> responded with Pong = true, ExecutionEnabled = true.
- `tekla.context` -> returned shaped Tekla context with model name and object counts.
- `tekla.execute` with streaming `tekla.progress` -> verified 3 consecutive progress events received in order.
- `tekla.cancel` -> processed cancellation without error.
- `tekla.analyze` -> analyzed script compilation without error.

---

## 3. Stress Test Results

| Scenario | Input / Expression | Expected Behavior | Actual Behavior | Result |
|---|---|---|---|---|
| Direct Commit | `model.CommitChanges();` | Denied | Blocked by `ScriptGuard` | **PASS** |
| Aliased Commit | `var m = model; m.CommitChanges();` | Denied | **0 violations (Bypass)** | **FAIL** |
| Parenthesized Commit | `(model).CommitChanges();` | Denied | **0 violations (Bypass)** | **FAIL** |
| Casted Commit | `((Model)model).CommitChanges();` | Denied | **0 violations (Bypass)** | **FAIL** |
| Modal UI | `MessageBox.Show("text");` | Denied | Blocked by `ScriptGuard` | **PASS** |
| WinForms UI | `System.Windows.Forms.Form f;` | Denied | Blocked by `ScriptGuard` | **PASS** |
| Tekla Picker | `var p = new Picker();` | Denied | Blocked by `ScriptGuard` | **PASS** |
| Pick Method | `p.PickObject();` | Denied | Blocked by `ScriptGuard` | **PASS** |
| Process Start | `Process.Start("calc");` | Denied | Blocked by `ScriptGuard` | **PASS** |
| Script Directives | `#r "foo.dll"` / `#load "bar.csx"` | Denied | Blocked by `ScriptGuard` | **PASS** |
| Pipe Naming Canonical | `PipeNaming.For("tekla", 2025)` | `"hptekla-mcp-2025"` | `"hptekla-mcp-2025"` | **PASS** |
| Pipe Naming Casing | `PipeNaming.For("TEKLA", 2025)` | `"hptekla-mcp-2025"` | `"hptekla-mcp-2025"` | **PASS** |
| Pipe Naming Whitespace | `PipeNaming.For("  tekla  ", 2025)` | `"hptekla-mcp-2025"` | `"hptekla-mcp-2025"` | **PASS** |
| Pipe Naming Empty Host | `PipeNaming.For("", 2025)` | Throws ArgumentException | Throws ArgumentException | **PASS** |
| JsonRpc Suffix | `JsonRpcMethods.Suffix("tekla.execute")`| `"execute"` | `"execute"` | **PASS** |
| Progress Method | `ProgressMethodFor("tekla.execute")` | `"tekla.progress"` | `"tekla.progress"` | **PASS** |
| Wire Isolation Sibling | `ContextResult` for Revit | No `"tekla"` property | Verified omitted | **PASS** |
| Wire Isolation Tekla | `ContextResult` for Tekla | Full camelCase `TeklaInfo` | Verified exact camelCase | **PASS** |
| Host Neutrality | Referenced assemblies in McpShared | 0 `Tekla.*` references | Verified 0 references | **PASS** |

---

## 4. Unchallenged Areas

None. All 4 prompt-specified challenge areas and related runtime contracts were directly evaluated and tested under both .NET 10.0 and .NET Framework 4.8.

---

## 5. Conclusion & Actionable Next Steps

Milestone 1 is nearly ready, but has a concrete security flaw that must be fixed before proceeding to Milestone 2:

**Action for Worker**:
In `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs`, update `GuardProfile.Tekla` to include `"CommitChanges"` in `deniedMembers`:
```csharp
    public static readonly GuardProfile Tekla = new GuardProfile(
        "Tekla Structures",
        deniedIdentifiers: new[] { "MessageBox", "Picker" },
        deniedMembers: new[]
        {
            // Interactive UI picking methods that block waiting for mouse clicks
            "PickObject", "PickObjects", "PickPoint", "PickPoints", "PickLine", "PickPolygon",
            // Application shutdown
            "Exit", "Quit",
            // Direct commit bypass
            "CommitChanges",
        },
        deniedMembersOnIdentifier: new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["model"] = new[] { "CommitChanges" },
        },
        ...
```
Once this single additive line is applied, all aliased and parenthesized calls to `CommitChanges` will be firmly blocked, making `GuardProfile.Tekla` watertight.
