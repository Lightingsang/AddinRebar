# DISPATCH — worker_m2_iter3

## 2026-10-12T08:44:00Z

You are `worker_m2_iter3`.
Your working directory is:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_iter3\`

### Mission
Remediate the 8 failing tests in `TargetTypedNewEmpiricalChallengeTests.cs` by completing AST traversal in `ScriptAnalyzer.cs`:

- **Target File**: `HPGrasshopper/HPGrasshopper.McpBridge/Execution/ScriptAnalyzer.cs`
- **Failing Test Suite**: `HPGrasshopper/HPGrasshopper.McpBridge.Tests/Execution/TargetTypedNewEmpiricalChallengeTests.cs` (8/37 tests currently fail)

### The 3 Evasion Vectors to Fix in `ScriptAnalyzer.FactsWalker`:
1. **Late Assignment / Reassignment (`Transaction t; t = new(doc, ...);` or `Transaction t = null; t = new(doc, ...);`)**:
   - Currently, `VisitVariableDeclaration` only calls `NoteTransaction` if `declarator.Initializer?.Value is ImplicitObjectCreationExpressionSyntax`.
   - Fix: In `VisitVariableDeclaration(VariableDeclarationSyntax node)`, inspect `node.Type` directly via `NoteTransaction(node.Type)` for all variable declarations. If a script declares a variable of type `Transaction`, `TransactionGroup`, or `SubTransaction` (or fully-qualified `Autodesk.Revit.DB.Transaction`), it is intending to use transactions, which is strictly prohibited under read-only mode `"none"`.
2. **Top-Level `using` Declarations / Using Statements (`using SubTransaction s = new(doc);`)**:
   - In Roslyn Script mode or local declarations, inspect `LocalDeclarationStatementSyntax` with `UsingKeyword` or `UsingDirectiveSyntax` / `UsingStatementSyntax`.
   - When `node` is `LocalDeclarationStatementSyntax`, `node.Declaration` contains the type — ensure `VisitLocalDeclarationStatement` or `VisitVariableDeclaration` visits it.
3. **Conditional Expressions (Ternary Operator: `cond ? new(...) : new(...)`) & Parenthesized Expressions**:
   - In `VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node)`:
     Walk up parent nodes while `current is ParenthesizedExpressionSyntax or CastExpressionSyntax or ConditionalExpressionSyntax`. If the ancestor is `EqualsValueClauseSyntax` with `VariableDeclaratorSyntax` and `VariableDeclarationSyntax`, extract `varDecl.Type` and call `NoteTransaction(varDecl.Type)`.
     Also if ancestor is `AssignmentExpressionSyntax assign`, resolve or check if it assigns to a transaction variable or mark transaction usage.

### Constraints & Verification:
- Rule C2: `ScriptAnalyzer.cs` must remain $\le 300$ lines.
- Rule M3: methods must remain $\le 50$ lines.
- Rule Q-B1: zero dead/commented-out code.
- Rule Q-B2: zero unannotated empty catches.
- `McpShared/` must remain 100% UNTOUCHED.
- Run test command:
  ```powershell
  & '.\HPGrasshopper\HPGrasshopper.McpBridge.Tests\bin\Debug\net10.0-windows7.0\HPGrasshopper.McpBridge.Tests.exe' --filter-class HPGrasshopper.McpBridge.Tests.Execution.TargetTypedNewEmpiricalChallengeTests
  ```
  Verify all 37 tests PASS (0 failed).
- Run full suite:
  ```powershell
  & '.\HPGrasshopper\HPGrasshopper.McpBridge.Tests\bin\Debug\net10.0-windows7.0\HPGrasshopper.McpBridge.Tests.exe' --no-progress
  ```
  Verify all 229 tests in McpBridge pass.

Write detailed handoff to `handoff.md` and notify `orchestrator_12` via `send_message`.
