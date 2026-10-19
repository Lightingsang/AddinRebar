# DISPATCH — worker_m3_iter2: Milestone M3 Remediation (Iteration 2)

## Mission
Remediate the code review findings from `reviewer_m3_1` to bring Milestone M3 into 100% compliance with Clean Code Governance (Rule M3 method length limits, port index parsing hardening) with zero regressions across all 658+ tests.

## Working Directory
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_iter2\`

## Reference Artifacts
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m3_1\handoff.md`
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m3_2\handoff.md`

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Tasks

### 1. Fix Rule M3 in `HPGrasshopper/HPGrasshopper.McpBridge/Tools/CanvasEditor.cs`
- `ExecuteBatch` (currently lines 25–106, 82 lines span) exceeds the 50-line limit by 32 lines.
- Refactor `ExecuteBatch` into clean, focused private helper methods such that **every method is strictly $\le 50$ lines total span**.
  - Example split:
    - `ExecuteBatch`: parameter checking, preflight validation call, undo scope creation, mutation delegation, result building ($\le 40$ lines).
    - `CreatePreflightFailureResult(CanvasPreflightResult preflight)` ($\le 20$ lines).
    - `ExecuteMutations(...)` or `ApplyBatchOperations(...)` ($\le 35$ lines).
- Ensure `CanvasEditor.cs` total file length remains strictly $\le 300$ lines (Rule C2). Currently 294 lines, so concise, well-structured helper methods are essential.

### 2. Fix Rule M3 in `HPGrasshopper/HPGrasshopper.McpBridge/Tools/ImageExporter.cs`
- `Export` (currently lines 19–80, 62 lines span) exceeds the 50-line limit by 12 lines.
- Refactor `Export` into focused private helper methods such that **every method is strictly $\le 50$ lines total span**.
  - Example split:
    - Extract viewport saving/restoring: `TryDuplicateViewport(GH_Canvas canvas)` and `TryRestoreViewport(GH_Canvas canvas, GH_Viewport? savedViewport)`.
    - Or extract rendering/saving: `RenderAndEncode(GH_Canvas canvas, Rectangle bounds, double zoom, string? outputPath, bool returnBase64)`.
- Ensure `ImageExporter.cs` total file length remains strictly $\le 300$ lines (Rule C2).

### 3. Harden Port Index Extraction in `CanvasBatchValidator.cs` & `CanvasEditor.cs`
- In `CanvasBatchValidator.cs` (lines 146, 150, 183, 187) and `CanvasEditor.cs` (lines 222, 223, 252, 253):
  - Replace direct calls `item.TryGetProperty("sourceOutput", out var so) ? so.GetInt32() : 0` with safe parsing:
    `int sourcePort = (so.ValueKind == JsonValueKind.Number && so.TryGetInt32(out var p)) ? p : 0;` (or validate `ValueKind == JsonValueKind.Number` in validator).
  - Handle null or non-numeric JSON elements gracefully without throwing unhandled `InvalidOperationException`.

### 4. Verification & Clean Code Attestation
- Verify AST spans using Python script: confirm 0 methods $> 50$ lines in all touched files.
- Verify 0 lines of commented-out code (Rule Q-B1).
- Verify 0 unannotated empty catch blocks (Rule Q-B2).
- Verify file lengths $\le 300$ lines (Rule C2).
- Run full build and all test suites:
  - `dotnet build HPGrasshopper/HPGrasshopper.slnx -c Release`
  - `dotnet test HPGrasshopper/HPGrasshopper.Mcp.Server.Tests -c Release` (77 tests)
  - `dotnet test HPGrasshopper/HPGrasshopper.E2E.Tests -c Release` (181 tests)
  - McpBridge test runner: 400 tests
  - Solution total: 658+ tests passing cleanly.
- Verify `McpShared/` has exactly 0 modified files (`git status -s McpShared/`).

## Output Deliverables
Write `handoff.md` in `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_iter2\` with:
1. Exact file diff summaries and method line spans.
2. Complete test command outputs and pass counts.
3. Attestation of Clean Code rules (C2, M3, Q-B1, Q-B2) and McpShared isolation.
Notify `orchestrator_12` via `send_message` upon completion.


## 2026-10-12T10:39:08Z
Received dispatch message from parent f28750cf-2706-4246-bea4-77bf7beedc18:
Remediate the Milestone M3 code review findings:
1. Decompose CanvasEditor.ExecuteBatch to <= 50 lines total span (Rule M3) while keeping CanvasEditor.cs <= 300 lines (Rule C2).
2. Decompose ImageExporter.Export to <= 50 lines total span (Rule M3) while keeping ImageExporter.cs <= 300 lines (Rule C2).
3. Harden port index extraction in CanvasBatchValidator.cs and CanvasEditor.cs against non-numeric JSON values using TryGetInt32 and ValueKind checks.
4. Verify all Clean Code metrics (C2 <= 300, M3 <= 50, Q-B1, Q-B2, McpShared untouched).
5. Ensure 100% build pass and all 658+ tests pass cleanly.
Write handoff.md in your working directory and notify orchestrator_12 via send_message when complete.
