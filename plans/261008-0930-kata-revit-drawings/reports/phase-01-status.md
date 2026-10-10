# Phase 1 status — 2026-10-08 13:15

Implemented + built (Debug.R26) + Core tests 1627/1627:
- setting `CreateKataDrawings` (KataSettings → rules → checkbox "Tạo bản vẽ Kata (mặt cắt, dim, tag, sheet)", default on)
- Core `KataDimChains` (+ `KataDimChainsTests`: touching segments → one chain; every Kata elevation dim of B01/B02/B03 measured by a chain)
- Revit `KataSectionViews` (shared section-view making, extracted from `KataLongSectionView`), `KataCrossSections` (one view per flag, "<B3> n-n", look +local X, crop = Kata section drawing + 50, far clip 300; break lines kata_dim, layer-2 circles kata_net manh), `KataDrawingDims` (invisible-line ticks + NewDimension per chain; refused chain logged, ticks removed), `KataSheetComposer` ("K-<B3>", first title block by name, B01 layout numbers), `KataLineStyles`, storage kinds Drafting / Sheet / Cross:<i>
- `KataLongSectionDrafter` now drafts marks + drawings in the non-fatal step; long-section crop grows to Kata's elevation drawing

Live: CHƯA TEST. Two test-Revit launches on `b03-marks-test.rvt` ended while opening the model (journals 1621/1622 stop at Jrn.Content.Open, no crash block) before any Kata code ran. At 13:09:40 the user's Revit (journal 1615, central model TruongTHCS) hit a Revit fatal error during an interactive "Create a section view" (keyboard shortcut + two clicks — user input, not the test scripts, which send no keys; Revit DBG "Deleting GRepsCache while it is in use by drawing code"). Live testing paused until the user says the machine is free.

Not done: rebar tags (phase 2), dims bound to column faces (phase 3), "first title block" not reviewed with the user's template.

## Live 2026-10-08 19:00–20:05 (test Revit on b03-marks-test.rvt, scratch workbook kataB03-cover-test.xlsm)
| Check | Result |
|---|---|
| run 1 (before fixes) | drafting threw NRE in `KataLineStyles.Invisible` (Category.GetCategory(OST_InvisibleLines) = null) and, outside the catch filter, rolled back the whole generation → catch widened to any exception (bars kept, error in message) |
| `<Invisible lines>` lookup | not in Lines' subcategories, not in a detail line's GetLineStyleIds → found among `GraphicsStyle` elements by category id |
| run after fixes | `10 cross sections, 35 dimensions (0 refused), sheet K-B03`; re-run reuses the long section (id 9820630) |
| cross section B03 2-2 | real bars cut, dims 500 / 150 + 1050 / 1200 as Kata, slab break lines |
| long section | Kata chains over (2600/5200/2600 …) and under the beam, section marks 1-1 … at the flags |
| title block | first by name = cover sheet "HỒ SƠ THIẾT KẾ CƠ SỞ" → now the one the model's sheets use most: `HP-TitleBlock-A1-Vert: A1` |
| sheet layout | elevation on top (wider than A1: 33.6 m at 1:25 = 1345 mm), 10 sections in a row under it |
| Ctrl+Z ×2 | views + sheet gone, Undo greyed |
Test Revit closed without saving; user's Revit untouched by the scripts (keystrokes now only after a foreground-pid check, `type-into.ps1`).
