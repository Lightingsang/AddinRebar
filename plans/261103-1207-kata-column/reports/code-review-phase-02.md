# Code review — phase 2 (Kata Column Export: Revit reader, Cot writer, command + web page)

Mode: read-only on source. Reviewer ran `dotnet test HPRebar.Core.Tests` (1719/1719 pass) and `npx vitest run src/kataColumn` (12/12 pass). Build `Debug.R26` was **not** run by the reviewer (it triggers the npm build); live VC / Revit / Excel not run (CHƯA TEST).

## Scope

| Area | Files |
|---|---|
| Core | `HPRebar.Core/KataExport/ColumnStack/*` (5 files, 445 lines) + `HPRebar.Core.Tests/KataExport/ColumnStack/KataColumnSheetBuilderTests.cs` (14 tests) |
| Revit | `KataExport/Service/KataColumnStackReader.cs`, `KataColumnExcelWriter.cs`, `KataWorkbookTarget.cs`, `KataExportCommand`-sibling `KataColumnExportCommand.cs`, `View/Web/ColumnExport/*` |
| Wiring | `Application.cs` (KataColumnExport lines only), `HPRebar.csproj` (`Compile Remove`), `RibbonIcons.cs` |
| Frontend | `column.html`, `src/kataColumn/**`, `vite.config.ts`, `embed-frontend.targets`, `tools/build-frontend-once.mjs` |

Beam writer parity: `git diff KataExcelWriter.cs` = pure extraction (`Target` → `KataWorkbookTarget.Open(TargetSheetName)`, `WithBlock`/`SetRange` → `KataExcelRange`, busy codes/message moved). Same calls, same order, same messages ("Dam" now interpolated from the parameter = same text). **Unchanged behaviour ✅.**

## Checks run (static)

| Check | Result |
|---|---|
| Revit API from the window | ✅ none — stack measured in `Execute`, window gets plain records ([KataColumnExportCommand.cs:43](../../../HPRebar/HPRebar/KataExport/KataColumnExportCommand.cs#L43)) |
| Web bridge trust | ✅ shared `WebMessageTransport` origin check; dev URL localhost-only (`PageOrigins.Resolve`); write request validated (name, count ≥ 1) |
| Excel formula injection | ✅ strings go through `KataExcelCell.ToCellValue` → `'` prefix (C3 name, A level names) |
| COM release | ✅ `KataWorkbookTarget.Dispose` + `KataExcelRange` release every intermediate; no leaked `Range` |
| Wrong workbook | ✅ write refused when `ActiveWorkbook.FullName` ≠ last probe |
| R26-only | ✅ csproj `Compile Remove` + `#if REVIT2026` both panels |
| Template rows 16..216 A..V | ✅ empty in `Kata.xlsm` (cell dump) — clear hits no template formula |

## Findings

| # | Sev | File:line | Problem | Fix |
|---|---|---|---|---|
| 1 | Medium | [KataColumnStackReader.cs:71-72](../../../HPRebar/HPRebar/KataExport/Service/KataColumnStackReader.cs#L71) | Circle = "any vertical cylindrical face". A rectangular column with rounded corners, a cast-in sleeve/void or a round capital is written as **round** (D = 0, C = b) with no warning — silent wrong section. Diameter is taken from tessellated vertices (chord sagitta → may round down 1–3 mm when the seam is not on the frame X). | Circle only when the plan extents are square (±1 mm) **and** a full-height `CylindricalFace` radius × 2 ≈ width; take the diameter from `cylinder.Radius` (ft→mm). Otherwise rectangle, plus a warning when the section's area (volume / height) differs from b×h by > 2 % (also catches L/T sections boxed silently). |
| 2 | Medium | [KataColumnStackReader.cs:86-91](../../../HPRebar/HPRebar/KataExport/Service/KataColumnStackReader.cs#L86), [:102-104](../../../HPRebar/HPRebar/KataExport/Service/KataColumnStackReader.cs#L102) | K = **bbox height** of the deepest beam, but Kata draws the soffit K below the level. A beam with a top offset (drop at wet areas, −50…−150 mm) gives a wrong K, and one whose top is > 50 mm below the head is not found at all (outline is head ± 50) → K = 0. Bbox may also reflect cut geometry while `BeamWidthMm` uses original solids (K1: two measuring rules for one beam). | Collect beams with box `Max.Z > head − 300 mm` and `Min.Z < head − 50 mm`; K = `head − min(soffit)` from `KataSolidReader.GetOriginalSolids` (same source as the width). Add a warning when a beam top is not at the head. |
| 3 | Medium | [KataColumnStackReader.cs:64-65](../../../HPRebar/HPRebar/KataExport/Service/KataColumnStackReader.cs#L64) | Storey base/top from **cut** geometry (`GetSolids`). When a floor or beam cuts the column (join order switched, common on older models) the top drops to the soffit → validator "starts 200 mm from the top of…" error on a valid stack, or a short first storey. | Use `GetOriginalSolids(column)` for the vertical range (the class already exists for framing), or the instance's base/top level + offset parameters; keep cut geometry only for the plan section if needed. |
| 4 | Medium | [KataColumnExcelWriter.cs:107-117](../../../HPRebar/HPRebar/KataExport/Service/KataColumnExcelWriter.cs#L107) | Rows below the stack are cleared A..V — the bar/hoop/tie entries of a previously exported taller column are destroyed with no undo (COM writes clear Excel's undo stack) and the page never says so. Conversely, rows of the new stack beyond the old count have empty E..I → Kata refuses ("Lỗi nhập … ô E15", variant v16) and rows that existed keep **another column's** bars silently. | Before writing, read `A10:I216` once (`Value2` block): report in the page "N storey rows below will be cleared (A–V)" and "rows X–Y have no bars (E–I)"; return both in `KataColumnWriteResponse` as warnings. Consider clearing only A (enough to stop Kata) and leaving E..V, decided with the user. |
| 5 | Medium | [KataColumnStepRule.cs:48-68](../../../HPRebar/HPRebar.Core/KataExport/ColumnStack/KataColumnStepRule.cs#L48) | Phase-1 spec ([phase-01-core-export.md:30](../phase-01-core-export.md)) requires a **warning when the upper storey is larger** (Kata semantics for growth unverified — contract variants only shrink). Code writes −size/+size silently; no test covers growth (T4). Same for a step in h: "keeps the bottom face" = MinY of the Revit frame is an assumption (contract v04 says only "one-sided") — GIẢ ĐỊNH CHƯA XÁC MINH. | Add the growth warning in `PlaceAxis` (size of upper > lower + tol) + test; pin the h side with the existing dump `reports/variants/v04-h-neg.json` (which face VC kept) and name it in the contract; keep the warning until a VC variant proves growth. |
| 6 | Low-Med | [KataColumnStackReader.cs:39-43](../../../HPRebar/HPRebar/KataExport/Service/KataColumnStackReader.cs#L39) | The frame is the lowest column's transform even when that column is **round** — its rotation is arbitrary, so rectangles above are refused as "xoay khác hướng" or measured in a meaningless b direction. | `frame = measured.FirstOrDefault(m => !m.IsCircle)?.Transform ?? Transform.Identity` (keep origin of the lowest column). |
| 7 | Low-Med | [KataColumnStackReader.cs:106-116](../../../HPRebar/HPRebar/KataExport/Service/KataColumnStackReader.cs#L106) | Pile cap found only when its bottom is > 50 mm below the column base; a column modelled from the cap bottom (common) gets "no pile cap", B9 = 0 and the cap thickness inside storey 1. B9 = 0 itself was never drawn by VC (CHƯA TEST). | Also accept a foundation whose box contains the column base (`Min.Z ≤ base ≤ Max.Z`): C5 = cap bottom, B9 = cap height, first storey base = cap top (warn). Run one VC variant with B9 = 0 before shipping the no-cap path. |
| 8 | Low | [KataColumnInputValidator.cs:57-61](../../../HPRebar/HPRebar.Core/KataExport/ColumnStack/KataColumnInputValidator.cs#L57) | Duplicate level names (two columns on the same base level with offsets) are accepted; Kata's section picker VLOOKUPs column A (`AK2 = VLOOKUP($AA$22,$A$10:$J$216,…)`), the page keys rows by `level` (React duplicate keys in `ColumnApp.tsx:210`, `ColumnSketch.tsx:25`). | Reject or suffix (`FLOOR 1 (2)`) in the builder with a warning; key rows by index in the page. |
| 9 | Low | [KataColumnSheetBuilder.cs:73](../../../HPRebar/HPRebar.Core/KataExport/ColumnStack/KataColumnSheetBuilder.cs#L73) | Each height rounded on its own (`Round(top − base)`): Kata sums B from C5, so levels drift by up to 0.5 mm per storey on non-whole-mm models. | `Metres(Round(top) − Round(base))` so the sum telescopes to `Round(top of stack)`. |
| 10 | Low | [KataColumnStackReader.cs:31-48](../../../HPRebar/HPRebar/KataExport/Service/KataColumnStackReader.cs#L31) + Core warnings | User-facing text mixed: reader errors Vietnamese, validator/step warnings English, shown in one dialog ("Chồng cột không ghi được sang Kata: FLOOR 2 starts…") and in a page with a VI/EN switch (N7). | Return warning codes + values from Core and render them in `i18n.ts` (VI/EN), or at least Vietnamese host-side like the beam export. |
| 11 | Low | [KataColumnStackReader.cs:23-60](../../../HPRebar/HPRebar/KataExport/Service/KataColumnStackReader.cs#L23) | Decisions (cap choice, beam filter, circle test, axis tolerance, plan overlap) live in the Revit adapter, untestable without Revit (M7, T7); findings 1–3, 6, 7 would have been caught by unit tests. | Read raw facts (boxes, faces, transforms) into records in the adapter; move the choices into `HPRebar.Core/KataExport/ColumnStack/KataColumnStackRules` with xUnit cases. |
| 12 | Low | [KataColumnStackReader.cs:52-54](../../../HPRebar/HPRebar/KataExport/Service/KataColumnStackReader.cs#L52), [KataColumnModels.cs:55-56](../../../HPRebar/HPRebar.Core/KataExport/ColumnStack/KataColumnModels.cs#L55) | C4 always starts at 1 and overwrites the count the user had in the workbook; `Name`/`Count` typed `object?` though only string/int ever flow. | Read C3/C4 in `Probe` and pre-fill the page when the sheet's C3 equals the measured name; type them `string` / `int`. |
| 13 | Info | [KataColumnStackReader.cs:180](../../../HPRebar/HPRebar/KataExport/Service/KataColumnStackReader.cs#L180) | Slanted columns: `IsVertical` relies on `BasisZ`; for Revit slanted columns the instance transform may stay vertical (GIẢ ĐỊNH CHƯA XÁC MINH). | Also check `SLANTED_COLUMN_TYPE_PARAM` = vertical. |

No Critical / High: nothing writes the wrong workbook, no Revit API off-thread, no injection path, COM objects released.

## Design / standards verdicts

- Thread model ✅ (modal window, COM on Revit's UI thread like Kata Settings' file I/O; a hung Excel would freeze Revit — same as the beam export, accepted).
- K1 ⚠️ beam size measured two ways (finding 2). M7/T7 ⚠️ (finding 11). N7 ⚠️ (finding 10). CM6 ✅ no plan codes in code. FM4 ok (two internal types in `KataWorkbookTarget.cs`, related). P5 exception per ADR-0014 ✅.
- Frontend: contracts drift-guarded against the C# text (`columnContracts.test.ts`), fixtures regenerated by the Core test ✅; `build-frontend-once.mjs` now also watches `settings.html` (was missing) ✅.

## Plan follow-ups (report only)

- Phase 2 row in `plan.md` still ❌: live gate (Revit pick → Cot → VC draws) not run — CHƯA TEST.
- Phase 4 (rebar) must re-derive the same frame (lowest non-round column, handedness of mirrored instances) or the b/h sign meaning flips; record it in the contract.
- Contract "Still open" gains: growth semantics, h-side of a negative D, B9 = 0.

**Score: 7 / 10** — Core rules are small, pure and tested against Kata's own sample; the COM extraction is a clean, behaviour-identical refactor; the web bridge is safe. Points lost on the Revit measurement adapter: three silent-or-spurious geometry paths (circle detection, beam depth, cut-geometry storey range), an unannounced destructive clear of user rows, and a phase-1 warning (growth) that never landed.

**What is good:** workbook identity re-checked at write time, apostrophe-safe text cells, Revit fully decoupled from the window, contract-driven tests with page fixtures generated from the mapper.

**Status:** DONE_WITH_CONCERNS
**Summary:** No blocking defect; 5 Medium findings in geometry measurement and the row clear should be fixed before the live VC gate.
**Majors:** #1 circle misdetection, #2 beam depth K, #3 cut-geometry storey range, #4 silent A..V clear / stale bars, #5 growth warning + h-side assumption.

## Fix round (2026-11-03)

| # | Outcome |
|---|---|
| 1 | ✅ round only when plan extents square and a vertical cylinder spans them; Ø = 2 × `CylindricalFace` radius; area ≠ b×h (> 2 %) → warning |
| 2 | ✅ beams with top within 300 mm under the head; K = head − lowest soffit (original solids) |
| 3 | ✅ vertical range + section from `GetOriginalSolids` |
| 4 | ✅ below the stack only A–D and K cleared (user's E–J, L–V kept); write answer lists storeys with empty E, page shows them |
| 5 | ✅ warning only for a wider storey kept on its left face (centred growth drawn by VC in v01); test added. h side of negative D stays open (contract) |
| 6 | ✅ frame from first rectangular column |
| 7 | ✅ foundation containing the column base accepted; storey 1 counted from cap top + warning. B9 = 0 still undrawn by VC (contract open) |
| 8 | ✅ duplicate level → "(2)" suffix + warning; React keys by index |
| 9 | ✅ height = Round(top) − Round(base); test added |
| 10 | ⏭ mixed EN warnings / VI errors kept (beam export does the same); follow-up |
| 11 | ⏭ reader rules stay in the adapter for now; live check next |
| 12 | ⏭ C4 defaults to 1, editable on the page |
| 13 | ✅ `SLANTED_COLUMN_TYPE_PARAM` checked too |

Checks: build Debug.R26 ✅, Core 1722/1722 ✅, vitest kataColumn 13/13 ✅, typecheck + eslint ✅.
