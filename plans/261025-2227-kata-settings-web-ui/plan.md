# Kata Settings — web UI (WebView2 + React + shadcn), Revit 2026

Status: phases 1–4 DONE 2026-10-25 (Core.Tests 1695/1695, build Debug.R26 + R25, frontend 197/197 + build, WPF dialog deleted, ADR-0013); phase 3b redesign done; phase 5 live 17/17 on the redesign in a fresh Revit 2026 (2026-10-26). Nothing committed. Decision A = delete WPF settings everywhere, button R26 only (user 2026-10-25); B = own modal window. Follows ADR-0010/0011/0012 (Kata web page in HPRebar, R26 only, shadcn).

## Goal
Replace WPF modal `KataSettingsView` (3 tabs: Detail thép / Thông số đặc thù / Thép mặc định) with web page `settings.html`
hosted like `kata.html`. React = form UX; C# = load/validate/save `%AppData%\HPRebar\KataSettings.json`.
Same fields, same ranges, same file format — behaviour identical, only UI changes.

## Key facts (scout)
- Settings touch no Revit API: file I/O only (`KataSettingsStore.LoadFile/SaveFile`) → bridge answers on UI thread, no ExternalEvent.
- Validation + import/export live in 3 WPF VMs (`ViewModel/Tabs/Kata*SettingsTabViewModel.cs`, 211/…/… lines), **no tests**.
- Choices from `KataSettingsChoices` (crank Ø, slope, coupler Ø), hook angles {90,135,180}, hanger angle {45,60}, stirrup spec options.
- Button "Kata Settings" exists on every Revit version (KataRebar R23–R27 reads the same file).

## Design
- Core (pure, testable): `HPRebar.Core/KataExport/Settings/` — `KataSettingsForm` DTO (flat Detail/Special/Joints + table rows),
  `KataSettingsFormMapper` (file ↔ form, out-of-list notice), `KataSettingsRules.Validate` → `[{field, message}]` (rules ported 1:1 from VMs).
- Host: `KataExport/View/Web/Settings/` — `KataSettingsWebWindow` (modal, owner Revit, `kata.hprebar.local`, start page `settings.html`)
  + `KataSettingsWebBridge`: `settings.load` → {form, choices, notice}; `settings.validate`; `settings.save` → validate then write
  (errors per field, or saveFailed); `settings.defaults`; `settings.close`.
- Frontend: `settings.html` + `src/kataSettings/` — shadcn Tabs/Card/Input/Select/Switch + Label, editable tables
  (mass, lap), dirty state, inline field errors, VI/EN + theme shared with Kata (`hp.kata.*` localStorage), mock bridge for `npm run dev`.
- Contracts: TS generated/checked by drift test + fixture JSON written by Core.Tests (same pattern as `kata-scene-b03.json`).

## Phases
| # | Phase | Type | Gate |
|---|---|---|---|
| 1 | [Core form + rules + tests](phase-01-core-form-and-rules.md) ✅ | AFK | Core.Tests green, rules = VM rules |
| 2 | [Host window + bridge](phase-02-host-window-and-bridge.md) ✅ | AFK | build Debug.R26 |
| 3 | [Settings page (React + shadcn)](phase-03-settings-page.md) ✅ | AFK | typecheck, lint, vitest, build |
| 4 | [Cut-over + docs](phase-04-cut-over-and-docs.md) ✅ | AFK | build R26 (+ other configs per decision A) |
| 3b | [Settings page redesign](phase-03b-settings-page-redesign.md) ✅ | AFK | typecheck, lint, vitest, build, screenshots |
| 5 | [Live verify](phase-05-live-verify.md) ✅ 17/17 ([report](reports/phase-05-live-verify.md)) | HITL light | harness PASS on 2nd Revit, settings file restored |

## Decisions (confirmed 2026-10-25)
- A. Web on R26; WPF settings dialog deleted in every version; Kata Settings button R26-only.
- B. Own modal window from the ribbon; "Thiết lập" button in Kata Export header = follow-up.

## Risks
- Harness writes the user's real `KataSettings.json` → back up before, restore in `finally`, keep timestamped copy.
- Modal `ShowDialog` + WebView2: init async inside modal loop — proven pattern in BIM Toolkit? not yet → phase 2 spike check first.
- Rule drift VM → Core: port with tests per rule before deleting VMs.
