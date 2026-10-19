# DISPATCH — worker_m2_iter4

## 2026-10-12T09:04:00Z

You are `worker_m2_iter4`.
Your working directory is:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_iter4\`

### Mission
Remediate the 8 failing tests in `AdversarialEdgeCaseEmpiricalTests.cs` by extending `ScriptAnalyzer.cs`:

- **Target File**: `HPGrasshopper/HPGrasshopper.McpBridge/Execution/ScriptAnalyzer.cs`
- **Failing Test Suite**: `HPGrasshopper/HPGrasshopper.McpBridge.Tests/Execution/AdversarialEdgeCaseEmpiricalTests.cs` (8/45 tests currently fail)

### Required Enhancements in `ScriptAnalyzer.cs`:
1. **Unwrap Array and Generic Collection Types in `IsTransactionType(TypeSyntax type)`**:
   ```csharp
   if (type is ArrayTypeSyntax array)
       return IsTransactionType(array.ElementType);

   if (type is GenericNameSyntax generic)
       return generic.TypeArgumentList.Arguments.Any(IsTransactionType);
   ```
   This immediately enables `VisitVariableDeclaration` to recognize:
   - `Transaction[] arr = [new(doc, "1")];` -> `IsTransactionType` is `true`.
   - `List<Transaction> list = [new(doc, "1")];` -> `IsTransactionType` is `true`.
   - `Func<Transaction> f = () => new(doc, "lambda");` -> `IsTransactionType` is `true`.
   - `_transactionVars` will correctly record `"arr"`, `"list"`, `"f"`.

2. **Traverse Collection and Initializer Ancestors in `VisitImplicitObjectCreationExpression`**:
   In the ancestor loop:
   ```csharp
   SyntaxNode? current = node.Parent;
   while (current is ParenthesizedExpressionSyntax or CastExpressionSyntax or ConditionalExpressionSyntax or CollectionExpressionSyntax or InitializerExpressionSyntax or LambdaExpressionSyntax or ReturnStatementSyntax)
       current = current.Parent;
   ```
   So target-typed `new` inside collections `[new(...)]` or `{ new(...) }` or lambda expressions traverses up to the enclosing `EqualsValueClauseSyntax` (inspecting `varDecl.Type`) or `AssignmentExpressionSyntax` (inspecting `assign.Left`).

### Constraints & Verification:
- Rule C2: `ScriptAnalyzer.cs` must remain $\le 300$ lines (currently 238 lines, budget headroom = 62 lines).
- Rule M3: methods must remain $\le 50$ lines.
- Rule Q-B1: zero commented-out dead code.
- Rule Q-B2: zero unannotated empty catches.
- `McpShared/` must remain 100% UNTOUCHED.
- Run test commands:
  ```powershell
  & '.\HPGrasshopper\HPGrasshopper.McpBridge.Tests\bin\Debug\net10.0-windows7.0\HPGrasshopper.McpBridge.Tests.exe' --filter-class HPGrasshopper.McpBridge.Tests.Execution.AdversarialEdgeCaseEmpiricalTests
  ```
  Verify all 45/45 PASS!
  ```powershell
  & '.\HPGrasshopper\HPGrasshopper.McpBridge.Tests\bin\Debug\net10.0-windows7.0\HPGrasshopper.McpBridge.Tests.exe' --filter-class HPGrasshopper.McpBridge.Tests.Execution.TargetTypedNewEmpiricalChallengeTests
  ```
  Verify all 37/37 PASS!
  ```powershell
  & '.\HPGrasshopper\HPGrasshopper.McpBridge.Tests\bin\Debug\net10.0-windows7.0\HPGrasshopper.McpBridge.Tests.exe' --no-progress
  ```
  Verify all 278 tests in McpBridge PASS!

Write detailed report to `handoff.md` and notify `orchestrator_12` via `send_message`.
