# Handoff Report — Challenger 2 (Milestone M1: Net48 Runtime, Roslyn & Host Neutrality)

- **Author**: Challenger 2 (`teamwork_preview_challenger_m1_2`)
- **Date**: 2026-09-22
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_challenger_m1_2`
- **Target Components**:
  - `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs` (`GuardProfile.Tekla`)
  - `McpShared/HPRebar.McpBridge.Core/Scripting/AnalyzerProfile.cs` (`AnalyzerProfile.Tekla`)
  - `McpShared/HPRebar.McpBridge.Core.Net48Tests` (Desktop CLR `v4.0.30319`)
  - `McpShared/HPRebar.Mcp.Server.Core.Tests/HostNeutralityTests.cs`
- **Empirical Test Suite**: `McpShared/HPRebar.McpBridge.Core.Net48Tests/TeklaMilestone1Challenger2Net48Tests.cs`
- **Verdict**: **REQUEST_CHANGES** (Action required on `GuardProfile.Tekla` by Worker M1)

---

## 1. Observation

### 1.1 Desktop CLR Runtime Execution
Execution of `HPRebar.McpBridge.Core.Net48Tests` was performed directly using:
```powershell
dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests
```
The test harness output confirmed execution on the 64-bit desktop CLR:
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
The runtime environment verified:
- `RuntimeInformation.FrameworkDescription`: `.NET Framework 4.8.9181.0`
- `Environment.Version.Major`: `4`
- `RuntimeEnvironment.GetSystemVersion()`: `v4.0.30319`
- `RuntimeEnvironment.GetRuntimeDirectory()`: `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\`

### 1.2 Observation 1: Parenthesized Receiver Bypass on `(model).CommitChanges()`
- Verbatim definition in `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs` (lines 210–214):
  ```csharp
  deniedMembersOnIdentifier: new Dictionary<string, string[]>(StringComparer.Ordinal)
  {
      // If bridge owns transaction commit and dryRun enforcement:
      ["model"] = new[] { "CommitChanges" },
  },
  ```
- Verbatim logic in `McpShared/HPRebar.McpBridge.Core/Scripting/ScriptGuard.cs` (lines 127–130):
  ```csharp
  // Members denied only on a named global, e.g. tr.Commit() — the bridge owns that transaction.
  if (node.Expression is IdentifierNameSyntax { Identifier.ValueText: var receiver }
      && profile.DeniedMembersOnIdentifier.TryGetValue(receiver, out var denied)
      && Array.IndexOf(denied, member) >= 0)
      Report(node.Name, $"{receiver}.{member} is not allowed: the bridge owns `{receiver}` and commits or rolls it back for you.");
  ```
- Directly observed result when executing:
  ```csharp
  ScriptGuard.Check("(model).CommitChanges(); return 1;", GuardProfile.Tekla);
  ```
  Returns `0` diagnostics (collection is empty). The script passes the guard completely without error.

### 1.3 Observation 2: Aliased Receiver Bypass on `m.CommitChanges()`
- Directly observed result when executing:
  ```csharp
  ScriptGuard.Check("var m = model; m.CommitChanges(); return 1;", GuardProfile.Tekla);
  ```
  Returns `0` diagnostics. Because receiver is `"m"` and `"CommitChanges"` is not in `profile.DeniedMembers`, the call passes without restriction.

### 1.4 Observation 3: Missing `PickFace` in `DeniedMembers`
- Verbatim code in `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs` (lines 203–209):
  ```csharp
  deniedMembers: new[]
  {
      // Interactive UI picking methods that block waiting for mouse clicks
      "PickObject", "PickObjects", "PickPoint", "PickPoints", "PickLine", "PickPolygon",
      // Application shutdown
      "Exit", "Quit",
  },
  ```
- Direct reflection on `Tekla.Structures.Model.UI.Picker` from installed `C:\Program Files\Tekla Structures\2025.0\bin\Tekla.Structures.Model.dll`:
  Declared interactive picking methods:
  - `PickPoint`
  - `PickPoints`
  - `PickLine`
  - `PickFace` *(Present in assembly, omitted from DeniedMembers)*
  - `PickObject`
  - `PickObjects`
  Notice `PickPolygon` does not exist on `Picker`.
- Directly observed result when executing:
  ```csharp
  ScriptGuard.Check("picker.PickFace();", GuardProfile.Tekla);
  ```
  Returns `0` diagnostics.

### 1.5 Observation 4: Roslyn Compilation with Tekla 2025 Reference Assemblies
- In test `Roslyn_compilation_with_real_Tekla_assemblies_if_installed_on_net48`:
  Loaded `Tekla.Structures.dll`, `Tekla.Structures.Model.dll`, and `Tekla.Structures.Catalogs.dll` from `C:\Program Files\Tekla Structures\2025.0\bin`.
  Compiled script:
  ```csharp
  var p1 = new Tekla.Structures.Geometry3d.Point(0, 0, 0);
  var p2 = new Tekla.Structures.Geometry3d.Point(1000, 0, 0);
  return p1.X + p2.X;
  ```
  `outcome.Succeeded` is `true`, `ScriptAnalyzer.Run` compiles and detects 0 violations.

### 1.6 Observation 5: Host Neutrality on .NET Framework 4.8
- In test `McpShared_net48_assemblies_reference_zero_host_apis`:
  Inspected assembly references of `HPRebar.Mcp.Contracts` and `HPRebar.McpBridge.Core` compiled for `net48`.
  Verified zero references to `Tekla.*`, `RevitAPI*`, `AcDbMgd*`, `Autodesk.Navisworks*`, `ETABSv1*`, `SAP2000v1*`, `RobotOM*`, or `Microsoft.Office.Interop.Excel*`.

---

## 2. Logic Chain

1. **Vulnerability of `(model).CommitChanges()`**:
   - In Roslyn, parenthesized expressions wrap the target in a `ParenthesizedExpressionSyntax` node.
   - `node.Expression is IdentifierNameSyntax` strictly requires an unparenthesized identifier.
   - Therefore, `(model).CommitChanges()` will never enter the `DeniedMembersOnIdentifier` check.
   - Because `CommitChanges` was only listed in `deniedMembersOnIdentifier` and NOT in `deniedMembers`, it is ignored by general member checks.
   - Consequently, any parenthesized commit call slips past static analysis.

2. **Vulnerability of Aliased Model Commit**:
   - In C#, `var m = model;` assigns the active model reference to local variable `m`.
   - Calling `m.CommitChanges()` invokes member `CommitChanges` with receiver `m`.
   - `DeniedMembersOnIdentifier` only maps string key `"model"`.
   - Because `CommitChanges` is absent from `DeniedMembers`, the call is permitted.
   - In Tekla Structures, calling `CommitChanges()` writes changes directly to the model database.
   - This breaks the fundamental bridge guarantee: the bridge must control transaction commit and enforce `dryRun = true` without external commit leakage.

3. **Vulnerability of Missing `PickFace`**:
   - Interactive picking methods in Tekla halt execution and await user mouse clicks on the viewport.
   - While `PickObject`, `PickObjects`, `PickPoint`, `PickPoints`, and `PickLine` were denied, `PickFace` was omitted.
   - A script invoking `picker.PickFace()` hangs the Tekla main thread synchronously, preventing the pipe listener from processing requests and causing agent timeouts.

4. **Host Neutrality**:
   - Verified that neither net10 nor net48 shared assemblies reference any Tekla assemblies.
   - Therefore, adding Tekla support is completely additive and host-neutral.

---

## 3. Caveats

- Live in-process execution inside a running `TeklaStructures.exe` instance is deferred to Milestone 2 (plugin bridge build).
- The syntax-level bypass of `CommitChanges` is a static guard limitation; if `ScriptAnalyzer.Analyze` is called, `UsesTransaction` is correctly detected as `true` because `AnalyzerProfile.Tekla` checks member name `CommitChanges` regardless of receiver. However, `ScriptAnalyzer.Run` only blocks if `GuardViolations.Count > 0`. Without a guard violation, the script compiles and is considered valid for execution.

---

## 4. Conclusion & Actionable Next Steps

**Verdict**: **REQUEST_CHANGES**

Milestone 1 is well-structured and 100% additive, but Worker M1 must fix `GuardProfile.Tekla` before Milestone 1 can be approved.

### Action Items for Worker M1:
In `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs` (lines 200–223):
1. Add `"CommitChanges"` to `deniedMembers`.
2. Add `"PickFace"` to `deniedMembers`.

Updated configuration:
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

To independently reproduce the findings and verify the fixes:

1. **Run Net48 Test Suite on Desktop CLR**:
   ```powershell
   dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests
   ```
   Inspect tests in `McpShared/HPRebar.McpBridge.Core.Net48Tests/TeklaMilestone1Challenger2Net48Tests.cs`:
   - `GuardProfile_Tekla_vulnerability_parenthesized_receiver_bypasses_guard`
   - `GuardProfile_Tekla_vulnerability_aliased_receiver_bypasses_guard`
   - `GuardProfile_Tekla_vulnerability_PickFace_is_missing_from_denied_members`

2. **Run Server Core Test Suite on .NET 10.0**:
   ```powershell
   dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests
   ```
   Verify 688/688 tests pass.
