# Code review — settings combos + bar-end cut marks (2026-10-08)

Reviewer: code-reviewer agent, score 7/10 ("ready once H1, M1, M3 fixed"). Core + dialog clean; risk in the Revit drafting step.

| # | Finding | Disposition |
|---|---|---|
| H1 | Section type may hand a template → Scale/DetailLevel throw, run rolled back | ✅ fixed: `ViewTemplateId = Invalid` right after `CreateSection`; type without default template first, stable order by id. Live: View Template `<None>` |
| M1 | B3 name with `\:{}[]|;<>?`~` makes `view.Name` throw | ✅ fixed: Core `KataViewName.Clean` (→ "-", blank → "Kata <id>") + theory test |
| M2 | Orientation change deleted the user's view | ✅ fixed: old view untagged (`KataDraftingStorage.Forget`) and kept, new one made, message names it |
| M3 | No longitudinal bars → throw → run rolled back | ✅ fixed: no marks → no section, returns before touching views |
| M4 | Any drafting failure discards correct rebar | ✅ fixed: orchestrator `DraftLongSection` catches the same exception filter, step rolled back alone, warning in message |
| M5 | Cut at centre: bars at y < 0 cut away; marks could float | ✅ fixed: cut plane in front of front face (w/2 + 300), depth through back face (far clip 1100 for b 500), marks drawn on that plane. Live re-checked |
| M6 | Ribbon settings change → stale removed keys | no change: already refused by `KataRebarWorkflow.cs:56` (removed keys carry the planned layout fingerprint; a changed layout is refused) |
| L1 | Storage lookup scans every curve | ✅ `ExtensibleStorageFilter` + fields looked up once |
| L2 | Run key = exact host set | open: re-picking a run with a different piece set leaves old marks + a second view; rare, noted |
| L3 | Recrop kept old far clip | ✅ depth re-cropped too |
| L4 | Case-sensitive unique name | ✅ `OrdinalIgnoreCase` |
| L5 | Duplicated views share the tag | ✅ lowest id used, warning logged |
| L6 | `LineStyle` creates a subcategory | ✅ renamed `GetOrCreateLineStyle` |
| L7 | Setting named "hooks" | ✅ renamed `ShowBarEndMarks` before first commit |
| Info | plan said side bars toward mid-depth | ✅ plan text fixed (shared `KataBarDrafting.Inward`: up) |

After fixes: build Debug.R26 OK, Core 1622/1622, live run 1 created / run 2 reused (same id 9820630), 46 marks, Ctrl+Z ×2 clean.
