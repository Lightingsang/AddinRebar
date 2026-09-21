# Remediation Report: Milestone 1 (McpShared Tekla Integration - Vulnerability Fix)

- **Agent**: `teamwork_preview_worker_m1_gen2`
- **Role**: implementer, qa, specialist
- **Date**: 2026-09-22
- **Target Files**:
  - `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs`
  - `McpShared/HPRebar.McpBridge.Core.Net48Tests/TeklaMilestone1Challenger2Net48Tests.cs`
  - `McpShared/HPRebar.Mcp.Server.Core.Tests/TeklaMilestone1ChallengerTests.cs`
  - `McpShared/HPRebar.Mcp.Server.Core.Tests/TeklaProfileTests.cs`

---

## 1. Summary of Vulnerabilities Identified by Challengers

### Vulnerability 1: Direct Commit Bypass via Aliasing and Parentheses
- **Identified by**: Challenger 1 (`teamwork_preview_challenger_m1_1`) & Challenger 2 (`teamwork_preview_challenger_m1_2`)
- **Root Cause**: `CommitChanges` was previously defined only in `deniedMembersOnIdentifier["model"]`. In `ScriptGuard.cs`, `deniedMembersOnIdentifier` is checked only when `node.Expression is IdentifierNameSyntax { Identifier.ValueText: var receiver }`.
  - When written as `(model).CommitChanges()`, the syntax is `ParenthesizedExpressionSyntax`, bypassing the identifier check.
  - When written as `var m = model; m.CommitChanges()`, the receiver is `"m"` rather than `"model"`, bypassing the dictionary check.
  - In Tekla Structures Open API, `CommitChanges()` writes changes directly to the model database. Allowing scripts to invoke `CommitChanges()` bypasses the bridge's transaction management and `dryRun = true` guarantees.

### Vulnerability 2: Interactive Main-Thread Freezing via `PickFace`
- **Identified by**: Challenger 2 (`teamwork_preview_challenger_m1_2`)
- **Root Cause**: `PickFace` is a synchronous interactive viewport-picking method in `Tekla.Structures.Model.UI.Picker` (present in `Tekla.Structures.Model.dll`). While `PickObject`, `PickObjects`, `PickPoint`, `PickPoints`, `PickLine`, and `PickPolygon` were denied in `GuardProfile.Tekla.deniedMembers`, `PickFace` had been omitted.
  - Calling `picker.PickFace()` synchronously blocks the Tekla main thread waiting for user mouse clicks, which halts named pipe communication and causes client timeouts.

---

## 2. Remediation Details

### 2.1 GuardProfile.Tekla Update
In `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs`:
Added `"PickFace"` and `"CommitChanges"` to `deniedMembers`:

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
            // If bridge owns transaction commit and dryRun enforcement:
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

Because `CommitChanges` is now in `deniedMembers`, `ScriptGuard` checks `profile.DeniedMembers.Contains(member)` regardless of whether the receiver is named `model`, aliased to `m`, wrapped in parentheses `(model)`, or casted.

### 2.2 Test Suite Updates
1. `McpShared/HPRebar.McpBridge.Core.Net48Tests/TeklaMilestone1Challenger2Net48Tests.cs`:
   - Updated `GuardProfile_Tekla_vulnerability_parenthesized_receiver_bypasses_guard` to verify `(model).CommitChanges()` is blocked with `"CommitChanges"` diagnostic.
   - Updated `GuardProfile_Tekla_vulnerability_aliased_receiver_bypasses_guard` to verify `var m = model; m.CommitChanges()` is blocked with `"CommitChanges"` diagnostic.
   - Updated `GuardProfile_Tekla_vulnerability_PickFace_is_missing_from_denied_members` to verify `picker.PickFace()` is blocked with `"PickFace"` diagnostic.
   - Added `[InlineData("picker.PickFace();", "PickFace")]` and `[InlineData("picker?.PickFace();", "PickFace")]` to `GuardProfile_Tekla_blocks_forbidden_syntax_under_desktop_clr`.
   - Added alias methods `GuardProfile_Tekla_parenthesized_receiver_bypass_test` and `GuardProfile_Tekla_aliased_receiver_bypass_test` to satisfy both challenger test naming conventions.

2. `McpShared/HPRebar.Mcp.Server.Core.Tests/TeklaMilestone1ChallengerTests.cs`:
   - Updated `Guard_limitation_CommitChanges_evasion_via_aliased_or_parenthesized_receiver` to verify `ScriptGuard` blocks aliased and parenthesized commit calls (`Assert.NotEmpty(diagnostics)`).
   - Added `[InlineData("p.PickFace(); return 1;", "PickFace")]` and `[InlineData("p?.PickFace(); return 1;", "PickFace")]` to `GuardProfile_Tekla_denies_forbidden_operations_across_syntax_variations`.
   - Ensured `[Theory]` attribute is intact.

3. `McpShared/HPRebar.Mcp.Server.Core.Tests/TeklaProfileTests.cs`:
   - Added `[InlineData("p.PickFace(); return 1;", "PickFace")]`, `[InlineData("m.CommitChanges(); return 1;", "CommitChanges")]`, and `[InlineData("(model).CommitChanges(); return 1;", "CommitChanges")]`.

---

## 3. Verification & Test Execution Results

1. **`HPRebar.McpBridge.Core.Net48Tests`** (.NET Framework 4.8 / Desktop CLR v4.0.30319):
   - Command: `dotnet test HPRebar.McpBridge.Core.Net48Tests` (run in `McpShared/`)
   - Result: **Passed! Total: 113, Failed: 0, Succeeded: 113, Skipped: 0** (Duration: 2.48s)

2. **`HPRebar.Mcp.Server.Core.Tests`** (.NET 10.0):
   - Command: `dotnet test HPRebar.Mcp.Server.Core.Tests` (run in `McpShared/`)
   - Result: **Passed! Total: 742, Failed: 0, Succeeded: 742, Skipped: 0** (Duration: 3.14s)

3. **Regression Check on Existing Hosts (`HPRebar.Mcp.Server.Tests`)**:
   - Command: `dotnet test HPRebar.Mcp.Server.Tests` (run in `HPRebar/`)
   - Result: **Passed! Total: 109, Failed: 0, Succeeded: 109, Skipped: 0** (Duration: 7.57s)

All vulnerabilities are completely remediated with 100% test pass rate and zero regressions.
