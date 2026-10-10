# Live — ribbon button Kata Settings (2026-10-07)

Change: dialog opens from HPRebar ▸ Rebar ▸ Kata Settings (first button of panel, slider glyph `RibbonIcons.KataSettings`); "Cài đặt" button + `OpenSettingsCommand` removed from Kata Export. Command `KataExport/KataSettingsCommand.cs` (modal, owner = Revit main window, nested `Availability` always true). Application.cs: +1 line (full build only; KATA_ONLY untouched by user decision).

Deviation: feature root KataExport now holds 5 files (convention says 4) — accepted in contract, no new feature folder for one button.

| Check | Result |
|---|---|
| build Debug.R26 | pass |
| Core tests | 1610/1610 |
| test Revit pid 38812 on scratch copy of spike-project.rte (DLL deployed by renaming the locked copy) | ribbon shows Kata Settings first in Rebar panel |
| UIA invoke button | dialog "Thiết lập thép Kata" opens, dark theme, reminder line shown |
| Accept | `%AppData%\HPRebar\KataSettings.json` written, SettingsVersion 3; test file removed after (none existed before), copy in scratchpad |
| zero-document state | CHƯA TEST (Revit 2026 home page has no ribbon to click) |

Test Revit closed without saving; user's two Revits untouched.
