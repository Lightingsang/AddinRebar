# Phase 2 — bridge quality walker, contract fields, validator, review section

## Context
[map-01 §1–§4, §7](reports/map-01-revit-mcpshared.md); [map-03 §1](reports/map-03-com-standalone.md) (ETABS/SAP early return, `GuardViolations` → errors); [map-04 §1–§2](reports/map-04-net48-inprocess.md); [evaluation §3, §5](reports/evaluation.md).

## Overview
Priority P1 · Planned · McpShared only (Contracts, McpBridge.Core, Mcp.Server.Core + their tests). No host folder edited: every bridge reaches the walker through `ScriptAnalyzer.Run → Analyze` and gets the fields on its next redeploy.

## Architecture
```
propose_tool → ToolLifecycleService.ProposeAsync → bridge <host>.analyze
   → ScriptAnalyzer.Run → Analyze(code) ─┬─ FactsWalker (existing)
                                          └─ ScriptQualityWalker (new) → QualityFindings, QualityAnalysed=true
   ← AnalyzeResult (+2 additive fields, STJ reflection, camelCase, unknown fields skipped)
ToolValidator: error → errors (blocks draft) · warning → warnings · analysed=false → warning "quality not analysed"
ProposeAsync: InsertEvent(name, "quality_checked", "code <sha12>\n" + summary)   ← review data (stale when the code changed)
Publish: manual → WriteReview prints "## Code quality" (latest event or "quality not analysed")
         auto   → same line appended to the outcome message (D3)
```

## Requirements
- `QualityFinding(string RuleId, string Severity, int Line, int Column, string Message)` record in `AnalyzeMessages.cs`; `AnalyzeResult.QualityAnalysed` (bool, default false), `QualityFindings` (`IReadOnlyList<QualityFinding>`, default empty). Severity `"error"`/`"warning"` strings (no enum converter on the pipe).
- `ScriptQualityWalker` (new file, ≤ 200 lines; responsibility: list the Q-rule findings of one parsed script) — rules and detection exactly as [evaluation §3](reports/evaluation.md) (D4 interpretations **pending user decision**). Comment rules read `DescendantTrivia()`; local functions **and** script-level methods handled.
- `ScriptQuality.Find(string code)` public static entry for seed tests (same parse options as `Analyze`).
- `ScriptAnalyzer.Analyze` calls the walker on the tree it already parsed; nothing in `Run`.
- Rule ids never `PREVIEW`, `PATH`, `DESTRUCTIVE`, `HEAVY`; findings never added to `GuardViolations`.
- `ToolValidator` new branch only when `analysis != null`; the `analysis == null` offline warning text unchanged (19 test files rely on it).
- `WriteReview` moved to `ToolLifecycleService.Review.cs` (partial) to keep the main file < 300 lines (K10).
- Acceptance 3 under every policy: offline/old bridge → draft saved, publish allowed; manual → review line; auto → message line (D3 **pending**).

## Related files
Create: `McpShared/HPRebar.McpBridge.Core/Scripting/ScriptQualityWalker.cs`, `McpShared/HPRebar.Mcp.Server.Core/Registry/ToolLifecycleService.Review.cs`, `McpShared/HPRebar.Mcp.Server.Core.Tests/ScriptQualityWalkerTests.cs`.
Modify: `McpShared/HPRebar.Mcp.Contracts/Messages/AnalyzeMessages.cs`, `.../Scripting/ScriptAnalyzer.cs`, `.../Registry/ToolValidator.cs`, `.../Registry/ToolLifecycleService.cs`, `.../HPRebar.Mcp.Server.Core.Tests/ToolLifecycleTests.cs`, `McpShared/HPRebar.McpBridge.Core.Net48Tests/ScriptCompilerNet48Tests.cs` (walker on net48), `.../PipeListenerNet48Tests.cs` (AnalyzeResult round-trip with the new fields).

## Tests
- Walker: positive + negative case per rule (evaluation §3 column), incl. the 4 Navis prose headers, 2 ETABS notes, `// v2`, AutoCAD `get_entities:38` reason comment, Civil `Fail(ex)` catch.
- Validator: error blocks, warning passes, `QualityAnalysed=false` → one warning, `null` → old warning text.
- Lifecycle: `quality_checked` event written; review file section (manual); auto message line.
- net48: walker compiles + runs; old-shape JSON (no fields) → `QualityAnalysed=false`.

## Build / test commands
`cd McpShared && dotnet test HPRebar.Mcp.Server.Core.Tests` · `dotnet test HPRebar.McpBridge.Core.Net48Tests` · each `cd <Host> && dotnet test <Host>.Mcp.Server.Tests` (10) · `cd HPCivil3d && dotnet test HPCivil3d.McpBridge.Tests` · `python plans/261004-1005-hp-clean-code-ai-tools/reports/snapshot-tools-list-baseline.py` vs `reports/tools-list-baseline-debug/` (byte compare ×10).

## Live verification (acceptance 7)
Revit 2026 closed → `dotnet build HPRebar/HPRebar.McpBridge -c Debug.R26` (deploy) → start Revit on a scratch copy, Load Once, bridge opt-in via UIA → `propose_tool` with commented-out code → rejected (error lists Q-B1); clean tool → `test_tool` → `publish_tool` (manual) → review file has "## Code quality". Other 9 bridges: CHƯA TEST (not redeployed) — reported as "quality not analysed" path only. **Needs a Revit-closed window** (golden-run Revit and Kata session).

## Success criteria
Acceptance 1, 2, 3, 5, 6, 7 of contract §2.

## Commit split (P6)
1. `feat(mcp): report script-quality findings from the bridge analyzer` (Contracts + walker + Analyze + tests).
2. `feat(mcp): block or warn on script-quality findings when a tool is proposed` (validator, lifecycle, review, tests).

## Risks
False positives of Q-B1 (K1) — negative test corpus mandatory before commit 1; net48 (K3); `tools/list` drift — gate per commit.

## Rollback
Revert commit 2 restores old validator behaviour; reverting commit 1 removes fields (deployed bridges ignore/miss them harmlessly — additive contract).
