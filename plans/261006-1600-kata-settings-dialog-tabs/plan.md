# Kata settings dialog in three tabs (Detail thép · Thông số đặc thù · Thép mặc định)

Contract: grill-me 2026-10-06 (user: approach B; joint defaults `5f8a50`, hanger on, `2f16`, 150, 45).
Reference: Kata "Cài đặt thông số Kata" dialog (3 screenshots from the user).

## Goal

Kata Export ▸ Thiết lập (one page, 14 boxes) becomes a 3-tab dialog. One JSON file, three groups:

| Group | Record | Used by the drawing code |
|---|---|---|
| Drawing | `KataSettings` (existing 14 + `CrankSlope` + `JointBeam` / `JointColumn`) | yes |
| Pending beam options | `KataBeamOptions` (5 Kata options with no rule yet) | no — stored, labelled "chưa áp dụng" |
| Shop | `KataShopSettings` (laps, coupler, max length, tables) | no — stored for the later shop tool |

File stays `%AppData%\HPRebar\KataSettings.json`, flat JSON, prefixes `JointBeam.` `JointColumn.` `Pending.` `Shop.`; tables as strings. Version 3; v1/v2 files load unchanged.

## Phases

| # | Phase | Status |
|---|---|---|
| 1 | Core settings records + JSON sections + sanitize + migration | done |
| 2 | Core rules: joint stirrups / hanger bars per load kind from settings, joint stirrup Ø of its own, crank slope | done |
| 3 | Revit: bar type per stirrup zone, type mapping row | done (live generation CHƯA TEST) |
| 4 | UI: window shell + 3 tab UserControls + tab view models | done, live |
| 5 | Tests, rules doc §16, build R26, live on a model copy | done — [live](reports/live-settings-dialog.md) |

Detail: [phase-01-05-settings-dialog.md](phase-01-05-settings-dialog.md)

## Consequence of the chosen default (user decision)

Joint stirrups used the span's stirrup Ø (G6). Default `5f8a50` gives them Ø8 of their own: on B01 (G6 = Ø10) they change Ø10 → Ø8 and get their own bar number. DWG parity tests pin the settings Kata had when the DWG was drawn (`5f10a50`).

## Out of scope

CAD-only presentation options; rules for the 5 pending options; the shop tool; Column / Foundation settings; other Kata tabs.

## Constraints

Build Debug.R26 only · no commit without approval · no change to HPRebar.csproj / Application.cs / Installer.cs · MaterialDesign + DynamicResource · XAML < 500 lines, view model < 250 lines.
