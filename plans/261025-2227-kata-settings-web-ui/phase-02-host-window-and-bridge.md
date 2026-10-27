# Phase 02 — Host window + bridge (R26)

Priority: high · Status: DONE 2026-10-25 (build only) · Type: AFK

## Context
- Pattern: `HPRebar/HPRebar/KataExport/View/Web/KataWebWindow.cs` (71 lines), `KataWebBridge.cs`, `HPWebHost` (`WebViewHost`, `CommandDispatcher`, `WebMessageTransport`).
- Store: `HPRebar/HPRebar/KataRebar/Service/KataSettingsStore.cs`.

## Requirements
- `KataExport/View/Web/Settings/KataSettingsWebWindow.cs`: same virtual host `kata.hprebar.local`, start page `settings.html`,
  dev URL `HPREBAR_KATA_DEV_URL` (+ `/settings.html`), debug port `HPREBAR_KATA_DEBUG_PORT`, `MaterialThemeBridge.Attach`, ShowDialog with Revit owner.
- `KataSettingsWebBridge.cs` commands (no Revit API, answered on the UI thread):
  `settings.load`, `settings.validate`, `settings.save`, `settings.defaults`, `settings.close`. Save = validate → `ToFile` → `SaveFile`; `saveFailed` returned, command shows the same error dialog as today.
- `KataSettingsCommand` (R26): opens the web window; other versions per decision A.
- Contracts file `KataSettingsWebContracts.cs` + TS drift test beside the Kata one.

## Steps
1. Spike: WebView2 init inside `ShowDialog` (no remote harness yet) — open, load, close ×3, no hang.
2. Window + bridge + contracts; csproj `Compile Remove` for non-2026 already covers `View/Web/**`.
3. Build `Debug.R26 -p:DeployAddin=false`.

## Success
- Build green; drift test green; window opens page in dev URL mode.

## Security
- Same origin check + CSP as Kata page; bridge never takes a path from the page.

## Result
- `KataExport/View/Web/Settings/KataSettingsWebContracts.cs` — commands `settings.load|validate|save|defaults|close`, event `settings.theme`;
  `KataSettingsPageState(theme, snapshot)`, `KataSettingsValidation(errors)`, `KataSettingsSaveResult(saved, fileFailed, errors)`.
- `KataSettingsWebBridge.cs` — UI thread, no Revit queue; save = rules → `ToFile` → `KataSettingsStore.SaveFile`, window stays open
  (page sends close); missing tab in payload = BAD_REQUEST; theme event marshalled to the window dispatcher.
- `KataSettingsWebWindow.cs` — modal, centre on Revit, same host/profile/debug port as Kata Export; dev page = `settings.html`
  beside `HPREBAR_KATA_DEV_URL`; close runs at Background priority after the answer.
- `KataSettingsCommand` — `#if REVIT2026` web window, `#else` WPF dialog (removed in phase 4); same error dialog when the file fails.
- Core.Tests +2: form JSON from the page reads back to the same file; emptied inputs arrive null and are refused by field.
- Checks: `dotnet build HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false` ✅ (no new warnings), `-c Debug.R25` ✅ (else branch), Core.Tests 1695/1695.

## Moved / open
- TS drift test → phase 3 (it is a vitest that reads the C# as text, needs the TS contracts).
- WebView2 inside `ShowDialog` spike: not run (Revit 2026 of the user is open; page not built yet) → first check of phase 5.
