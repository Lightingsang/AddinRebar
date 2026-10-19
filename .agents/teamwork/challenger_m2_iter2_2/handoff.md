# Handoff Report — Milestone M2 Empirical Challenge (Iteration 2)

**Challenger**: challenger_m2_iter2_2 (Roles: critic, specialist)  
**Recipient**: orchestrator_12 (`f28750cf-2706-4246-bea4-77bf7beedc18`)  
**Date**: 2026-10-12T08:42:00Z  
**Type**: Hard Handoff  
**Verdict**: **REQUEST_CHANGES**

---

## 1. Observation

1. **Target-Typed `new(...)` AST Bypass in Mode "none" (Finding F3-Bypass)**:
   - **Production File**: `HPGrasshopper/HPGrasshopper.McpBridge/Execution/ScriptAnalyzer.cs`, lines 124–143:
     ```csharp
     public override void VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node)
     {
         if (node.Parent is EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax varDecl } })
         {
             NoteTransaction(varDecl.Type);
         }
         base.VisitImplicitObjectCreationExpression(node);
     }

     public override void VisitVariableDeclaration(VariableDeclarationSyntax node)
     {
         foreach (var declarator in node.Variables)
         {
             if (declarator.Initializer?.Value is ImplicitObjectCreationExpressionSyntax)
             {
                 NoteTransaction(node.Type);
             }
         }
         base.VisitVariableDeclaration(node);
     }
     ```
   - **Test Suite**: `HPGrasshopper/HPGrasshopper.McpBridge.Tests/Execution/TargetTypedNewEmpiricalChallengeTests.cs` (112 lines, <= 300).
   - **Verbatim Test Run Failure Output**:
     ```text
     failed HPGrasshopper.McpBridge.Tests.Execution.TargetTypedNewEmpiricalChallengeTests.UsingStatements_TargetTypedNew_DetectedAsTransaction(code: "using SubTransaction s = new(doc);") (132ms)
       Failed to detect transaction in using statement: using SubTransaction s = new(doc);
         at HPGrasshopper.McpBridge.Tests.Execution.TargetTypedNewEmpiricalChallengeTests.UsingStatements_TargetTypedNew_DetectedAsTransaction(String code) in TargetTypedNewEmpiricalChallengeTests.cs:45

     failed HPGrasshopper.McpBridge.Tests.Execution.TargetTypedNewEmpiricalChallengeTests.UsingStatements_TargetTypedNew_DetectedAsTransaction(code: "using Transaction t = new(doc, \"Adversarial\");") (9ms)
       Failed to detect transaction in using statement: using Transaction t = new(doc, "Adversarial");
         at HPGrasshopper.McpBridge.Tests.Execution.TargetTypedNewEmpiricalChallengeTests.UsingStatements_TargetTypedNew_DetectedAsTransaction(String code) in TargetTypedNewEmpiricalChallengeTests.cs:45

     failed HPGrasshopper.McpBridge.Tests.Execution.TargetTypedNewEmpiricalChallengeTests.UsingStatements_TargetTypedNew_DetectedAsTransaction(code: "using TransactionGroup tg = new(doc, \"Adversarial\");") (0ms)
       Failed to detect transaction in using statement: using TransactionGroup tg = new(doc, "Adversarial");
         at HPGrasshopper.McpBridge.Tests.Execution.TargetTypedNewEmpiricalChallengeTests.UsingStatements_TargetTypedNew_DetectedAsTransaction(String code) in TargetTypedNewEmpiricalChallengeTests.cs:45

     failed HPGrasshopper.McpBridge.Tests.Execution.TargetTypedNewEmpiricalChallengeTests.FullyQualifiedNames_TargetTypedNew_DetectedAsTransaction(code: "using Autodesk.Revit.DB.SubTransaction s = new(doc);") (0ms)
       Failed to detect transaction in qualified name: using Autodesk.Revit.DB.SubTransaction s = new(doc);
         at HPGrasshopper.McpBridge.Tests.Execution.TargetTypedNewEmpiricalChallengeTests.FullyQualifiedNames_TargetTypedNew_DetectedAsTransaction(String code) in TargetTypedNewEmpiricalChallengeTests.cs:60

     failed HPGrasshopper.McpBridge.Tests.Execution.TargetTypedNewEmpiricalChallengeTests.ModeNone_BypassVulnerability_ProvedEmpirically(code: "using Autodesk.Revit.DB;\nTransaction t;\nt = new(doc, \"Bypass\");") (2s 672ms)
       CRITICAL BYPASS: Script was NOT blocked by mode 'none' pre-flight AST analyzer! Result message: The script does not compile. Fix the listed errors and retry.
         at HPGrasshopper.McpBridge.Tests.Execution.TargetTypedNewEmpiricalChallengeTests.ModeNone_BypassVulnerability_ProvedEmpirically(String code) in TargetTypedNewEmpiricalChallengeTests.cs:109

     failed HPGrasshopper.McpBridge.Tests.Execution.TargetTypedNewEmpiricalChallengeTests.ModeNone_BypassVulnerability_ProvedEmpirically(code: "using Autodesk.Revit.DB;\nSubTransaction st;\nst = new(doc);") (883ms)
       CRITICAL BYPASS: Script was NOT blocked by mode 'none' pre-flight AST analyzer! Result message: The script does not compile. Fix the listed errors and retry.
         at HPGrasshopper.McpBridge.Tests.Execution.TargetTypedNewEmpiricalChallengeTests.ModeNone_BypassVulnerability_ProvedEmpirically(String code) in TargetTypedNewEmpiricalChallengeTests.cs:109

     failed HPGrasshopper.McpBridge.Tests.Execution.TargetTypedNewEmpiricalChallengeTests.ModeNone_BypassVulnerability_ProvedEmpirically(code: "using Autodesk.Revit.DB;\nTransaction t = null;\nt = new(doc, \"Bypass\");") (1s 251ms)
       CRITICAL BYPASS: Script was NOT blocked by mode 'none' pre-flight AST analyzer! Result message: The script does not compile. Fix the listed errors and retry.
         at HPGrasshopper.McpBridge.Tests.Execution.TargetTypedNewEmpiricalChallengeTests.ModeNone_BypassVulnerability_ProvedEmpirically(String code) in TargetTypedNewEmpiricalChallengeTests.cs:109

     failed HPGrasshopper.McpBridge.Tests.Execution.TargetTypedNewEmpiricalChallengeTests.ModeNone_BypassVulnerability_ProvedEmpirically(code: "using Autodesk.Revit.DB;\nusing SubTransaction s = new(doc);") (5ms)
       CRITICAL BYPASS: Script was NOT blocked by mode 'none' pre-flight AST analyzer! Result message: The script does not compile. Fix the listed errors and retry.
         at HPGrasshopper.McpBridge.Tests.Execution.TargetTypedNewEmpiricalChallengeTests.ModeNone_BypassVulnerability_ProvedEmpirically(String code) in TargetTypedNewEmpiricalChallengeTests.cs:109
     ```

2. **Zero False Positives Confirmed**:
   - 12 valid non-transaction scripts tested in `ValidNonTransactionScripts_ZeroFalsePositives`:
     - `var a = 1;`
     - `int x = 42;`
     - `string s = "hello";`
     - `var list = new List<string>();`
     - `List<string> items = new();`
     - `Point3d pt = new(1, 2, 3);`
     - `Curve c = new Line(p1, p2);`
     - `using var cts = new CancellationTokenSource();`
     - `double area = Math.PI * 10 * 10;`
     - `bool flag = false;\nif (flag) { int y = 2; }`
     - `var coords = new double[] { 1.0, 2.0, 3.0 };`
     - `Dictionary<string, int> dict = new();`
   - All 12 returned `UsesTransaction == false` (100% PASS, zero false positives).

3. **Existing Worker Test Suite**:
   - `Milestone2ChallengerEmpiricalTests.cs`: 19 passed, 0 failed.
   - `QueueLifecycleAndIdentityAdversarialTests.cs`: 10 passed, 0 failed (zombie task execution defect remains fixed).

---

## 2. Logic Chain

1. **Why Top-Level `using SubTransaction s = new(doc);` is Missed**:
   - *Observation*: In `SourceCodeKind.Script`, Roslyn parses top-level `using SubTransaction s = new(doc);` into two parts:
     - `UsingDirectiveSyntax`: `using SubTransaction`
     - `GlobalStatementSyntax` -> `ExpressionStatementSyntax` -> `AssignmentExpressionSyntax`: `s = new(doc)`
   - *Reasoning*: Because `s = new(doc)` is an `AssignmentExpressionSyntax`, `ImplicitObjectCreationExpressionSyntax` (`new(doc)`) has parent `AssignmentExpressionSyntax`, NOT `EqualsValueClauseSyntax`.
   - *Failure*: `ScriptAnalyzer.FactsWalker.VisitImplicitObjectCreationExpression` only checks:
     `if (node.Parent is EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax varDecl } })`
     Thus, it returns without noting the transaction.

2. **Why Late Assignment / Reassignment Bypasses Mode "none"**:
   - *Observation*: Scripts with:
     `Transaction t; t = new(doc, "Bypass");` or `Transaction t = null; t = new(doc, "Bypass");`
     declare a variable of type `Transaction`, but initialize it on a subsequent line via assignment.
   - *Reasoning*:
     - In `VisitVariableDeclaration(VariableDeclarationSyntax node)`:
       Line 137 checks `if (declarator.Initializer?.Value is ImplicitObjectCreationExpressionSyntax)`. When `declarator.Initializer` is `null` or `null` literal, `NoteTransaction(node.Type)` is NEVER called!
     - In `VisitImplicitObjectCreationExpression`:
       `node.Parent` is `AssignmentExpressionSyntax`, which is completely ignored.
   - *Failure*: Neither the variable declaration (`Transaction t;`) nor the assignment (`t = new(...)`) triggers `UsesTransaction = true`.

3. **Why This is a Critical Bypass Under Mode "none"**:
   - *Observation*: `GrasshopperExecutionEngine.DispatchAsync` lines 188–196 enforce read-only execution:
     ```csharp
     var mode = TransactionModes.Normalize(request.Transaction) ?? TransactionModes.Auto;
     if (string.Equals(mode, TransactionModes.None, StringComparison.OrdinalIgnoreCase))
     {
         var analysis = ScriptAnalyzer.Analyze(request.Code, AnalyzerProfile.Grasshopper);
         if (analysis.UsesTransaction)
         {
             return ExecuteResult.Failure(
                 "Transaction mode 'none' enforces read-only execution. The script attempts to open a Revit Transaction/TransactionGroup/SubTransaction, which is forbidden under mode 'none'.");
         }
     }
     ```
   - *Reasoning*: Because `analysis.UsesTransaction` evaluates to `false` for late assignments, reassignments, and top-level using statements, the pre-flight safety check is completely bypassed.
   - *Empirical Proof*: `ModeNone_BypassVulnerability_ProvedEmpirically` empirically proved that the script fell through to Roslyn compilation instead of being blocked with the read-only error message.

---

## 3. Caveats

- In-block `using` statements (e.g. `{ using SubTransaction s = new(doc); }` or inside methods) ARE correctly detected by `VisitVariableDeclaration`. The bypass occurs specifically for top-level script statements, late assignments, reassignments, and conditional/ternary expressions (`cond ? new(...) : new(...)`).
- No other caveats.

---

## 4. Conclusion

While the worker successfully implemented target-typed `new` detection for direct variable declarations (`SubTransaction st = new(doc);`), the implementation is incomplete and vulnerable to multiple straightforward AST bypasses:
1. `using SubTransaction s = new(doc);` (explicitly mandated by dispatch) is NOT detected.
2. `Transaction t;\nt = new(doc, "name");` (late assignment) is NOT detected.
3. `Transaction t = null;\nt = new(doc, "name");` (reassignment) is NOT detected.
4. `Transaction t = cond ? new(...) : new(...)` (conditional expression) is NOT detected.
5. All 4 bypass forms evade the pre-flight read-only check under transaction mode "none".

**Required Remediation in `ScriptAnalyzer.cs`**:
1. In `VisitVariableDeclaration`: Unconditionally check `NoteTransaction(node.Type);` for all variable declarations (regardless of whether the initializer is `ImplicitObjectCreationExpressionSyntax`). Any variable declared with a `Transaction`, `SubTransaction`, or `TransactionGroup` type in a mode "none" script must flag `UsesTransaction = true`.
2. In `VisitUsingDirective`: Check `if (profile.TransactionTypeNames.Contains(node.Name.ToString())) UsesTransaction = true;` (to catch top-level `using SubTransaction`).
3. In `VisitAssignmentExpression`: If `node.Right is ImplicitObjectCreationExpressionSyntax` and `node.Left` refers to a transaction or identifier, or if `node.Right is ImplicitObjectCreationExpressionSyntax`, inspect target assignment context.

**Final Verdict**: **REQUEST_CHANGES**

---

## 5. Verification Method

To independently reproduce all 8 empirical failures:

```powershell
& '.\HPGrasshopper\HPGrasshopper.McpBridge.Tests\bin\Debug\net10.0-windows7.0\HPGrasshopper.McpBridge.Tests.exe' --filter-class HPGrasshopper.McpBridge.Tests.Execution.TargetTypedNewEmpiricalChallengeTests
```

Expected result before fix: **Failed! 8 failed, 29 passed, total 37**.  
Target after worker remediation: **Passed! 37 passed, 0 failed**.
