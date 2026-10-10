# Handoff — grill-me contract for "mặt cắt dọc/ngang, dim, tag thép dầm trong Revit" (2026-10-08, session 3fe400eb)

For the session running `plans/261008-0930-kata-revit-drawings/` (phase 1 done, phase 2 tags next). This session stops here
and touches no file of that plan. User decisions taken in this session's interview, to apply there:

## Decisions (user, 2026-10-08 20:00–20:30)
| Topic | Decision | Delta vs plan 261008-0930 |
|---|---|---|
| Order | 1 long section full (dims, tags, break lines) → 2 cross sections → 3 sheet | plan already did 2 + 3 in phase 1; keep |
| Object kind | real Revit objects (Dimension, Rebar Tag, real section views); no CAD-exact drafting | same (hybrid) |
| Families | from the HP template only; tool never embeds .rfa (csproj untouched) | same |
| Tag text | family's standard labels (Rebar Number, Quantity, Bar Diameter, Spacing); tool only places tags where Kata puts them | same |
| Long-section content | span/support chain (top row), additional-bar length + step dims, stirrup-zone chain (row 600) + stirrup tags (row 525) attached to the zone's set, longitudinal bar tags (one per Kata number, attached to the set/bar), **break lines at every column stub end** | tags = phase 2; stub break lines were drawn as detail lines in phase 1 → **change to the template's Break Line detail item** (user chose the family, 2026-10-08 20:30) |
| Break line | detail item "Break Line" family of the template, one per column stub end at the crop edge, width = column; family/type name fixed, missing → warn + skip | new |
| Type names | **fixed names the user gives** (user declined a per-model picker tab) — ask the user for: rebar tag type (longitudinal), rebar tag type (stirrups), dimension type, break line family/type; missing in a model → warning naming the type, that group skipped, bars and the rest still made | plan: "rebar tag already in the HP template" — same direction |
| Dims anchoring | real column faces / beam where Revit gives references; bar-related dims to the bar when the API yields a reference, else to the bar-end cut mark detail lines (already drawn by `KataBarEndMarkCreator`) — log the fallback | plan phase 3 (column faces); invisible-line ticks of phase 1 are the fallback today |
| Re-run | delete every tool-tagged annotation and place again (Kata regenerates everything); user-added annotation kept; one Ctrl+Z per run | same |
| Tag position | head at Kata's coordinates (Core `KataBarTagBuilder`: X, RowZ, InsertX; Kata draws bar layers 15–25 mm off the real centreline, tag leader attaches to the real set so it still points right) | — |

## Acceptance (this session's contract, step 1)
1. Span/support chain reads Kata's numbers (B03: 400/10400/400/13400/400/6200/400/2000), anchored to column faces + beam.
2. Additional-bar length and step dims from column face to bar end, ±2 mm of Kata.
3. Stirrup chain at row 600, one segment per zone; stirrup tag at row 525 attached to that zone's set.
4. One longitudinal tag per Kata number, attached to its set, head within ±100 mm of Kata.
5. Break line at each column stub end, column-wide.
6. Re-run replaces tool annotation only; Ctrl+Z ×1 undoes a run.
7. Missing type → warning with the name, group skipped, generation succeeds.
8. Build R26, Core tests green, live on B03 and B01 copies.

## Open for the user (phase 2 cannot start without)
- Names in the template: rebar tag type for longitudinal bars, rebar tag type for stirrups (Spacing label), dimension type, break line family + type.

## State of the working tree this session leaves
Uncommitted, built (Debug.R26) and live-verified by this session: ribbon "Kata Settings" button (plan 261006-1600 report
`live-ribbon-settings-button.md`), settings combos + `ShowBarEndMarks` + bar-end cut marks (`plans/261008-0541-kata-bar-end-marks/`,
review fixes applied, 1622 Core tests at 07:20). The other session has since edited `KataLongSectionDrafter`, `KataRebarOrchestrator`,
`KataSettings`, the Detail tab (its `CreateKataDrawings`) on top of it — commit as one series from that session; this session
makes no further edits.
