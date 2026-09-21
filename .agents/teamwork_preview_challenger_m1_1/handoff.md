# Handoff Report — Milestone 1 (McpShared Tekla Integration)

- **Agent**: `teamwork_preview_challenger_m1_1` (EMPIRICAL CHALLENGER)
- **Role**: critic, specialist
- **Date**: 2026-09-22
- **Verdict**: **REQUEST_CHANGES**

---

## 1. Observation

### Observation 1.1: `GuardProfile.Tekla` configuration
In `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.McpBridge.Core\Scripting\GuardProfile.cs`, lines 199–222:
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

### Observation 1.2: `ScriptGuard.cs` AST visitor logic for denied members
In `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.McpBridge.Core\Scripting\ScriptGuard.cs`, lines 122–130:
```csharp
            var member = node.Name.Identifier.ValueText;
            if (DeniedMembers.Contains(member)) Report(node.Name, $".{member} is not allowed: reflection and process control are blocked in {_host} scripts.");
            else if (profile.DeniedMembers.Contains(member)) Report(node.Name, $".{member} is not allowed in {_host} scripts: it prompts the user, leaves or replaces the bridge's transaction, or opens modal UI.");

            // Members denied only on a named global, e.g. tr.Commit() — the bridge owns that transaction.
            if (node.Expression is IdentifierNameSyntax { Identifier.ValueText: var receiver }
                && profile.DeniedMembersOnIdentifier.TryGetValue(receiver, out var denied)
                && Array.IndexOf(denied, member) >= 0)
                Report(node.Name, $"{receiver}.{member} is not allowed: the bridge owns `{receiver}` and commits or rolls it back for you.");
```

### Observation 1.3: Empirical test failures on aliasing and parenthesized expressions
Running `dotnet test HPRebar.McpBridge.Core.Net48Tests` produced:
```
failed HPRebar.Mcp.Server.Tests.TeklaMilestone1Challenger2Net48Tests.GuardProfile_Tekla_aliased_receiver_bypass_test (2ms)
  Aliased receiver `m.CommitChanges()` should be blocked by guard!
    at HPRebar.Mcp.Server.Tests.TeklaMilestone1Challenger2Net48Tests.GuardProfile_Tekla_aliased_receiver_bypass_test() in G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.McpBridge.Core.Net48Tests\TeklaMilestone1Challenger2Net48Tests.cs:140
failed HPRebar.Mcp.Server.Tests.TeklaMilestone1Challenger2Net48Tests.GuardProfile_Tekla_parenthesized_receiver_bypass_test (1ms)
  Parenthesized receiver `(model).CommitChanges()` should be blocked by guard!
    at HPRebar.Mcp.Server.Tests.TeklaMilestone1Challenger2Net48Tests.GuardProfile_Tekla_parenthesized_receiver_bypass_test() in G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.McpBridge.Core.Net48Tests\TeklaMilestone1Challenger2Net48Tests.cs:129

Test run summary: Failed!
  total: 108
  failed: 2
  succeeded: 106
  skipped: 0
  duration: 2s 723ms
```
And running `ScriptGuard.Check("var m = model; m.CommitChanges();", GuardProfile.Tekla)` returns **0 diagnostics**.

### Observation 1.4: Empirical verification of other contracts
Running `dotnet test HPRebar.Mcp.Server.Core.Tests` with our challenger test suite (`TeklaMilestone1ChallengerTests.cs`) passed:
```
Test run summary: Passed!
  total: 688
  failed: 0
  succeeded: 688
  skipped: 0
  duration: 3s 443ms
```
- `PipeNaming.For("tekla", 2025)` == `"hptekla-mcp-2025"`. Casing variations (`"TEKLA"`, `"Tekla"`) and whitespace trimming verified.
- `JsonRpcMethods.Suffix("tekla.execute")` == `"execute"`.
- `RequestDispatcher.ProgressMethodFor("tekla.execute")` == `"tekla.progress"`.
- `ContextResult` serialized for all 9 sibling hosts contains zero `"tekla"` property.
- `ContextResult` for Tekla formats camelCase and omits null properties.
- In-memory named pipe round-trip with fake executor verified ping, context, execute with progress, cancel, and analyze.

---

## 2. Logic Chain

1. **Premise**: In Requirement R2 of the Tekla specification, the bridge must manage transaction commits and support `dryRun = true` testing without calling `model.CommitChanges()`.
2. **From Observation 1.1**: Worker 1 placed `"CommitChanges"` inside `deniedMembersOnIdentifier["model"]`, but did not place `"CommitChanges"` in `deniedMembers`.
3. **From Observation 1.2**: In `ScriptGuard.cs`, `deniedMembersOnIdentifier` is checked only when `node.Expression is IdentifierNameSyntax { Identifier.ValueText: var receiver }`. If the expression is an alias `m`, `receiver` is `"m"` (not in dictionary). If the expression is parenthesized `(model)`, `node.Expression` is `ParenthesizedExpressionSyntax` (not an `IdentifierNameSyntax`).
4. **From Observation 1.3**: Empirical tests confirm that `(model).CommitChanges()` and `var m = model; m.CommitChanges()` completely evade `ScriptGuard.Check` with zero diagnostics, leading to test failures in `TeklaMilestone1Challenger2Net48Tests`.
5. **Deduction**: Because Tekla Open API has no other member named `CommitChanges`, placing `"CommitChanges"` in `deniedMembers` blocks all forms of `.CommitChanges()` without false positives.

---

## 3. Caveats

- Tekla Structures 2025 desktop application is not installed on this dev environment (standard CI environment), so live process testing against `TeklaStructures.exe` was conducted using the verified in-memory fake executor and named pipe listener. Full live testing will run under Milestone 5.
- No other caveats.

---

## 4. Conclusion

**Verdict: REQUEST_CHANGES**

Milestone 1 is well-architected and 95% complete, but requires one single fix before being approved:
In `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs`, add `"CommitChanges"` into `GuardProfile.Tekla.DeniedMembers`:
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

## 5. Verification Method

To independently verify after the worker applies the fix:
1. Run `dotnet test HPRebar.McpBridge.Core.Net48Tests` from `McpShared/`:
   `GuardProfile_Tekla_aliased_receiver_bypass_test` and `GuardProfile_Tekla_parenthesized_receiver_bypass_test` must now **PASS**.
2. Run `dotnet test HPRebar.Mcp.Server.Core.Tests` from `McpShared/`:
   All 688+ tests must pass with 0 failures, 0 errors, 0 skipped.
