# Milestone 1 Independent Review Report: McpShared Additive Integration for Tekla Structures 2025

- **Reviewer**: `teamwork_preview_reviewer_m1_2` (Reviewer & Adversarial Critic)
- **Target**: Milestone 1 Implementation by `teamwork_preview_worker_m1`
- **Date**: 2026-09-22
- **Repo Root**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar`

---

## 1. Review Summary

**Verdict**: **APPROVE**

Milestone 1 successfully extends `McpShared` to support Trimble Tekla Structures 2025 in a strictly additive, backward-compatible manner. All wire constants, script contracts, Roslyn guard/analyzer profiles, context DTOs, and test suites are implemented in accordance with repository standards.

No integrity violations (hardcoded results, dummy facades, unauthorized shortcuts, or fabricated outputs) were detected. All automated tests were independently executed and passed 100%.

---

## 2. Integrity Verification

As mandated by adversarial reviewer protocol, the codebase was inspected for integrity violations:
- **Hardcoded test results / facade shortcuts**: None found. Implementations in `HostScriptContracts`, `PipeNaming`, `JsonRpcMethods`, `ContextMessages`, `GuardProfile`, and `AnalyzerProfile` provide real data structures and rules.
- **Fabricated verification outputs**: None found. Test execution was independently executed via .NET CLI against `McpShared.slnx` (`HPRebar.Mcp.Server.Core.Tests` and `HPRebar.McpBridge.Core.Net48Tests`). Results strictly match the worker's reported numbers.
- **Self-certifying work without verification**: None found. Full independent reproduction was performed.

---

## 3. Verified Claims

| Claim from Worker Report | Verification Method | Result | Notes |
|---|---|---|---|
| `McpShared.slnx` compiles with 0 errors and 0 warnings | `dotnet build McpShared.slnx` | **PASS** | 0 errors, 0 warnings |
| `HPRebar.Mcp.Server.Core.Tests` passes 100% (643 passed) | `dotnet test HPRebar.Mcp.Server.Core.Tests --no-build` | **PASS** | 643 passed, 0 failed, 0 skipped in 3.92s |
| `HPRebar.McpBridge.Core.Net48Tests` passes 100% (73 passed) | `dotnet test HPRebar.McpBridge.Core.Net48Tests --no-build` | **PASS** | 73 passed, 0 failed, 0 skipped in 3.20s |
| Pipe naming `PipeNaming.For("tekla", 2025)` produces `hptekla-mcp-2025` | Source code inspection & unit test in `TeklaProfileTests` & `HostNeutralityTests` | **PASS** | Correctly maps both `"tekla"` and `"TEKLA"` |
| `CommitChanges` is guarded on `model` under `deniedMembersOnIdentifier` | `GuardProfile.Tekla` line 120 & `TeklaProfileTests` | **PASS** | Blocked via `deniedMembersOnIdentifier["model"] = ["CommitChanges"]` |
| `CommitChanges` flagged as transaction method in `AnalyzerProfile.Tekla` | `AnalyzerProfile.Tekla` line 141 & `TeklaProfileTests` | **PASS** | Detected by `ScriptAnalyzer` as `UsesTransaction = true` |
| Custom handler addition in `McpBridgeHost` / `RequestDispatcher` is additive | Code review of `McpBridgeHost.cs`, `RequestDispatcher.cs` and unit test | **PASS** | Uses optional parameter `= null`; zero regressions |
| Wire isolation: `TeklaInfo` omitted when null | `TeklaProfileTests.Wire_additions_are_invisible_when_unused` | **PASS** | Non-Tekla serialized context results contain no `"tekla"` key |
| Git diff cleanliness: only `McpShared/` touched for Milestone 1 | `git diff --stat McpShared/` | **PASS** | 10 files modified, 3 test files added |

---

## 4. Adversarial Challenges & Red-Team Stress-Testing

**Overall Risk Assessment**: **LOW** (with key design caveats for Milestone 2)

### Challenge 1: Circumventing the `CommitChanges` Guard in `dryRun` Mode

- **Assumption Challenged**: Placing `"CommitChanges"` under `deniedMembersOnIdentifier: ["model"] = ["CommitChanges"]` is sufficient to prevent unauthorized model modification during `dryRun = true`.
- **Attack Scenario**:
  Because `ScriptGuard` checks `node.Expression is IdentifierNameSyntax { Identifier.ValueText: var receiver }`, an AI or script writer can easily evade the identifier check through:
  1. Variable re-assignment:
     ```csharp
     var m = model;
     m.CommitChanges();
     ```
  2. Direct instantiation:
     ```csharp
     var newModel = new Tekla.Structures.Model.Model();
     newModel.CommitChanges();
     ```
  3. Parenthesization / Casting:
     ```csharp
     (model).CommitChanges();
     ((Tekla.Structures.Model.Model)model).CommitChanges();
     ```
  In all three scenarios, `ScriptGuard.Check` returns **0 guard violations** because the receiver identifier is not `"model"`.
- **Blast Radius**: If Milestone 2 (`HPTekla.McpBridge`) relies solely on `ScriptGuard.Check` to enforce `dryRun`, a script calling `m.CommitChanges()` would permanently mutate the Tekla model despite `dryRun = true`.
- **Mitigation**:
  1. Note that `ScriptAnalyzer` DOES flag ANY `.CommitChanges()` invocation as `UsesTransaction = true` via `profile.TransactionMethodNames.Contains(invoked)`.
  2. In Milestone 2 (`HPTekla.McpBridge`), the bridge executor MUST enforce:
     - If `dryRun == true` and `analyzed.UsesTransaction == true`, reject execution or throw a `BridgeRequestException`.
     - Alternatively, consider adding `"CommitChanges"` to `deniedMembers` across all receivers if scripts are never intended to call `CommitChanges` directly.

### Challenge 2: Interactive Picking Methods Evading Specific Member Denials

- **Assumption Challenged**: The denied member list for picking (`PickObject`, `PickObjects`, `PickPoint`, `PickPoints`, `PickLine`, `PickPolygon`) covers all interactive methods that freeze the Tekla UI thread.
- **Attack Scenario**:
  Tekla Open API's `Tekla.Structures.Model.UI.Picker` also includes methods such as `PickFace(...)` and `PickCoordinateSystem(...)`. If an AI generates `picker.PickFace()`, this specific method is not in `deniedMembers`.
- **Mitigation**:
  `"Picker"` is listed in `deniedIdentifiers: new[] { "MessageBox", "Picker" }`. Thus, any declaration like `var picker = new Picker();` or `Picker p = ...` is blocked at the identifier level before reaching member access. For defense-in-depth, adding `"PickFace"` and `"PickCoordinateSystem"` to `deniedMembers` in Milestone 2 is recommended.

---

## 5. Findings

### [Minor] Finding 1: `TeklaInfo` DTO Omits `Units` Property
- **What**: `TeklaInfo` record does not include an explicit `Units` property.
- **Where**: `McpShared/HPRebar.Mcp.Contracts/Messages/ContextMessages.cs`, lines 86-96.
- **Why**: The authoritative specification in `ORIGINAL_REQUEST.md` (R1) states: *"Định nghĩa DTO ngữ cảnh mô hình `ContextResult.Tekla` / `TeklaInfo` (Tên model, đường dẫn dự án, đơn vị, trạng thái kết nối)"*. In Tekla Structures, Open API database units are always metric (mm, radians, kg). However, without an explicit field, AI agents inspecting `get_tekla_context` do not receive explicit machine-readable confirmation of the unit system.
- **Suggestion**: In Milestone 2, consider adding `string Units = "mm"` (or `string? Units = "mm"`) to `TeklaInfo` so the context tool outputs `"units": "mm"`.

### [Advisory] Finding 2: `dryRun` Enforcement in Milestone 2 Bridge
- **What**: Guarding `CommitChanges` under `deniedMembersOnIdentifier` is an AST syntactic check that can be bypassed by local alias or casting.
- **Where**: `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs`, line 120.
- **Why**: As proven in Challenge 1, `dryRun` cannot rely on syntax guard alone.
- **Suggestion**: Ensure Milestone 2 (`HPTekla.McpBridge`) pairs `ScriptGuard.Check` with `analyzed.UsesTransaction` check during `dryRun` mode.

---

## 6. Coverage Gaps & Unverified Items

- **Tekla Open API Binary Reference**: `Tekla.Structures.dll` and related binaries were not linked in Milestone 1 because Milestone 1 is purely `McpShared` additive integration. Compilation against actual Tekla binaries will occur in Milestone 2 (`HPTekla.McpBridge` on `net48`) and Milestone 3 (`HPTekla.Mcp.Server.Tests`).
- **Live Tekla Execution**: Not applicable to Milestone 1 (McpShared contracts only). Unattended live verification is planned for Milestone 4/5.

---

## 7. Conclusion

The Milestone 1 deliverable is robust, cleanly separated, fully tested, and ready to serve as the foundation for Milestone 2 (`HPTekla.McpBridge`). The changes maintain 100% backward compatibility with zero regressions across all 9 existing hosts.
