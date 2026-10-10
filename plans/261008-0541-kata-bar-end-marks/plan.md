# Kata settings combos + bar-end cut marks on a long section

Status: in progress (2026-10-08). Contract: grill-me 2026-10-07/08 (user confirmed "implement").

## Step 1 — dialog (done, built + tested)
- Combos (fixed lists, `KataSettingsChoices`): crank Ø 10…40 (16), crank slope 1/4·1/6·1/10·1/12 (1/6), coupler Ø 16…50 (30).
- Saved value outside list → default shown + notice; rules keep file value until Accept.
- Checkbox "Thể hiện móc cắt kết thúc thép" → `KataSettings.ShowBarEndMarks` (Drawing group, default on).

## Step 2 — bar-end marks in Revit
| Item | Rule |
|---|---|
| Ends | every polyline end of main / additional / side / hanger bars (bent: leg tip); none for stirrups, ties; no laps exist yet |
| Shape | detail line from end back along bar, 30° off bar, 3.2 mm paper (80 mm at 1:25) — user's numbers, DWG has 75×25 (18.4°) |
| Turn | shared with the canvas (`KataBarDrafting.Inward`): straight end of a top bar down, any other bar up; leg tip toward the bar body |
| Dedupe | ends equal in elevation (bars side by side) → one mark |
| View | one section per run, name = B3 (else "Kata <id>"), 1:25, no template, cut plane on beam centre, far clip past the back face, crop = bars ± 400 mm; rebars unobscured |
| Re-run | section found by storage key (host unique ids), crop re-set, old marks (storage-marked detail curves) deleted, new drawn; user annotations kept |
| Switch off | old marks deleted, no section created |
| Line style | Lines subcategory `kata_thep chu`, red, weight 3, created when missing |
| Undo | own transaction inside the run's TransactionGroup → one Ctrl+Z |

Files: Core `Calculators/KataBarEndMarkLayout.cs` + `Models/KataBarEndMark.cs`; Revit `KataRebar/Service/KataLongSectionService.cs`, `KataBarEndMarkCreator.cs`, `KataDraftingStorage.cs`; orchestrator hook.

## Verify
- xUnit: B01 fixture ends ↔ DWG tick anchors (±1 mm), turn side vs DWG, dedupe, switch.
- Live on model copy: section "B01" created, marks at ends, re-run no duplicates, one Ctrl+Z.

Reports: `reports/`.
