# 0013 — Kata Settings: a web page on Revit 2026, the WPF dialog removed

- **Status:** Accepted (2026-10-25, by user — plan `plans/261025-2227-kata-settings-web-ui/`)
- **Tags:** [REVIT] [PROJECT]

## Context
Kata Export became a web window on Revit 2026 only (ADR-0011, ADR-0012). Its settings dialog, HPRebar ▸ Rebar ▸ Kata Settings, was still WPF. That dialog had three tabs: Detail thép / Thông số đặc thù / Thép mặc định.
- It wrote `%AppData%\HPRebar\KataSettings.json`, which Kata Export and Kata Rebar read.
- Its checks lived in three view models with no tests, and each check gave one message per tab.

The user asked for the same dialog as WebView2 + React + shadcn/ui, with C# keeping the work on the settings. On 2026-10-25 the user also decided two things:
- **Revit 2026 only:** the WPF dialog is deleted in every version.
- **Own window:** the settings keep their own modal window opened from the ribbon.

## Decision
1. **Rules in Core:** `HPRebar.Core/KataExport/Settings/` holds what the page edits.
   - `KataSettingsForm`: one record per tab; numbers are nullable so an empty input comes back as a field error.
   - `KataSettingsFormMapper`: `Snapshot(file)` builds the form; `ToFile(form)` refuses an invalid form.
   - `KataSettingsRules`: one `{field, message}` per wrong value, with the ranges of the old dialog.
   - Every value from a list must come from that list. A saved value outside its list opens as the default, with a notice.
2. **Host:** `KataExport/View/Web/Settings/KataSettingsWebWindow` is modal over Revit.
   - It uses the same virtual host, WebView2 profile and debug port as Kata Export, with start page `settings.html`.
   - Its commands are `settings.load`, `validate`, `save`, `defaults` and `close`, plus the `settings.theme` event. They read and write the file on the UI thread; no Revit API, no Revit queue.
   - Save never closes the window; the page asks for that.
3. **Page:** `HPBimToolkit.Frontend/settings.html` + `src/kataSettings/` uses the shadcn/ui kit and tokens of ADR-0012 (`kata.css`).
   - Two components were added to `src/components/ui/`: `label` and `alert-dialog`, both from the radix-ui package already approved.
   - The page shares the `hp.kata.lang` / `hp.kata.theme` choices with Kata Export.
   - The host checks the form 300 ms after the last change, and the page shows the host's errors only.
4. **Multi-version:** `KataSettingsCommand` and the Kata Settings button exist only when `RevitVersion` is 2026 (csproj `Compile Remove` + `#if REVIT2026`).
   - The WPF `KataSettingsView`, its three tab views, `KataSettingsViewModel` and the tab view models are deleted.
   - On the other versions, Kata Rebar keeps reading the settings file, or Kata's defaults when there is none.

## Consequences
- \+ The checks have tests (`KataSettingsRulesTests`, field by field, each edge on both sides). The page marks the exact wrong input.
- \+ There is one UI technology and one look for the Kata windows on Revit 2026.
- − Revit 2023–2025 and 2027 can no longer edit the settings. Their Kata Rebar runs on a file written by Revit 2026, or on the defaults.
- − `embed-frontend.targets` (shared with HP BIM Toolkit) now watches `settings.html` as its last page.

## Alternatives
- **Keep WPF on the other versions.** Rejected by the user: a second UI for versions nobody develops against.
- **A settings drawer in the Kata Export page.** Not chosen. The button needs no selection or open Kata window today. A "Thiết lập" button in the Kata Export header that opens this window is a possible follow-up.
- **Validate in TypeScript.** Rejected: two copies of the rules would drift. The C# rules are the only rules, and the page asks.

## Rules
ADR-0008 P5 exception as extended by ADR-0010, ADR-0011 and ADR-0012. DEPENDENCY_RULES D6: no new package.
