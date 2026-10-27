# Phase 05 — Live verify (Revit 2026, second instance)

Priority: medium · Status: pending · Type: HITL light (unsigned add-in prompt)

## Rules
- Second Revit via `HPBimToolkit/tools/harness/start-revit-with-toolkit.ps1` on a model copy; never the user's Revit.
- Back up `%AppData%\HPRebar\KataSettings.json` (timestamped copy) before, restore in `finally`.
- No keys into file dialogs; theme of Revit untouched.

## Checks (`HPBimToolkit/tools/harness/live-verify-kata-settings.mjs`)
1. Ribbon HPRebar ▸ Kata Settings (UIA) → window, page `settings.html`, values = file.
2. Bad value (EndZoneFraction 0.7) → inline error, nothing written.
3. Edit 3 fields + 1 lap row → Chấp nhận → file holds them, window closed.
4. Reopen → values kept; Mặc định → defaults shown, Thoát → file unchanged.
5. Kata Export "Đọc thép Excel" uses the new value (skip if no scratch workbook).
6. Revit responding; log clean. Report `reports/phase-05-live-verify.md`.
