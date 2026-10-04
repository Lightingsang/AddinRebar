# Phase 01 — Column: bar types by id, session without the Revit stack

## Context
- Audit AUD-012 (`docs/clean-code/CLEAN_CODE_AUDIT.md`), DEPENDENCY_RULES L2–L4, ADR-0004 (shared kernel).
- `ColumnRebar/Model/RebarTypeInfo.cs` holds `RebarBarType BarType`; only `ColumnRebar/Service/RebarCreationService.cs` reads `.BarType`.
- `ColumnRebarSession.Stack` (`ColumnStack`: `Element`, `PlanarFace`) — 3 uses in View/ViewModel.

## Steps
1. `HPRebar/Shared/Revit/ElementIds.cs` (if no helper exists): `long ToLong(ElementId)` / `ElementId FromLong(long)` gated `#if REVIT2024_OR_GREATER` (`// Multi-version: ElementId`).
2. `RebarTypeInfo`: replace `RebarBarType BarType` with `long BarTypeId`; the catalog that builds the list fills the id.
3. `RebarCreationService`: resolve `doc.GetElement(ElementIds.FromLong(info.BarTypeId)) as RebarBarType` once per run; a missing type → the existing "bar type not found" path (no silent skip).
4. Session: replace `ColumnStack Stack` with what the VM reads (Core `ColumnSection` list + names); the handler/orchestrator keeps the `ColumnStack` it was built from.
5. Build R23/R26/R27, Core tests, review, log.

## Success
- `grep -rn "Autodesk\|RebarBarType\|ColumnStack" HPRebar/ColumnRebar/ViewModel` → nothing.
- Golden run (Column fixture) identical — or waived by the user and logged CHƯA TEST.

## Risks
- Id round-trip across R23 (`int`) / R24+ (`long`): unit-check the helper in the add-in build only (no Core test possible).
