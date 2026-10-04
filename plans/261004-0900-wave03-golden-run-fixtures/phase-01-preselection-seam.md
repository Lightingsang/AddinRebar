# Phase 1 — Pre-selection seam (product change, needs approval)

## Context
- `ColumnRebarCommand.cs:45` `PickObjects`, `BeamRebarCommand.cs:44` `PickObjects`, `FoundationRebarCommand.cs:42` `PickObject` — interactive only.
- Precedent: `KataExport/KataExportCommand.cs:41-71` (selection filtered by `AllowElement`, pick when empty).
- Rejected alternatives: MCP calling HPRebar (impossible, see plan.md); headless debug command reading a spec JSON (bypasses VM/runner contracts that Wave 4 changes; `#if DEBUG` under `Debug.R26` [UNVERIFIED] and diverges Debug from Release); UIA-driven pick (coordinates + options-bar Finish; ribbon/option bar partly invisible to UIA per `plans/261002-1520-kata-canvas-barmarks-sections/reports/live-verify.md`).

## Overview
Priority P1 · planned · a feature batch (`feat(...)`), NOT part of any refactoring wave (REFACTORING_PLAN §1.3). Committed before the baseline.

## Requirements
- If current selection contains ≥ 1 element passing the feature's `ISelectionFilter.AllowElement`, use those elements (Column/Beam: all passing; Foundation: exactly one passing Floor — more than one → fall back to pick, no guessing).
- Else unchanged `PickObjects`/`PickObject` path incl. Esc handling.
- Validators, ordering (`OrderBy BottomFace Z` for columns), dialogs: untouched.

## Architecture
New `HPRebar/HPRebar/Shared/Revit/PreselectionPicker.cs` (`namespace HPRebar.Shared.Revit;`, static, 3 features use it → ADR-0004 OK):
- `IList<Element> ElementsOrPick(UIDocument, ISelectionFilter, string prompt)` → empty list on Esc.
- `Element? ElementOrPickOne(UIDocument, ISelectionFilter, string prompt)`.
Kata keeps its own copy (frozen files; adopt in Kata wave).

## Related files
Create: `Shared/Revit/PreselectionPicker.cs`. Modify: `ColumnRebar/ColumnRebarCommand.cs`, `BeamRebar/BeamRebarCommand.cs`, `FoundationRebar/FoundationRebarCommand.cs`. (4 files.)

## Steps
1. Write helper (multi-version safe: no ElementId arithmetic needed).
2. Replace the pick block in each command with one helper call; keep the `references.Count == 0` / null early returns.
3. Build `Debug.R23`…`R27` with `-p:DeployAddin=false`; `dotnet test HPRebar.Core.Tests`.
4. code-reviewer agent vs `docs/clean-code/CODE_REVIEW_CHECKLIST.md`.
5. Commit `feat(rebar): use the current selection when it already holds valid elements` (no push).

## Todo
- [ ] user approval
- [ ] helper + 3 commands
- [ ] build matrix R23-R27, Core tests
- [ ] review, commit

## Success criteria
Build 0 errors on 5 configs; Core tests count unchanged and green; live proof comes in phase 3 (select by MCP → ribbon click opens window without prompt).

## Risks
- User-visible change (a stray selection of valid elements is used instead of prompting) — same behaviour users already know from Kata. Low.
- Rollback: revert the single commit.

## Security
None (no new input surface).
