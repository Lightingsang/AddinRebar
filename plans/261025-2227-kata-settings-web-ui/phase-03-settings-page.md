# Phase 03 — Settings page (React + TS + Tailwind + shadcn)

Priority: high · Status: DONE 2026-10-25 (dev page + tests; not yet in Revit) · Type: AFK

## Context
- `HPRebar/HPBimToolkit.Frontend/` — vite multi-page `{index, kata}`; shadcn in `src/components/ui/`; Kata i18n `src/kata/i18n.ts`, theme `hp.kata.theme`.

## Requirements
- `settings.html` + vite input `settings`; `src/kataSettings/` (App, sections, `useSettingsForm` reducer, contracts, mock bridge, i18n keys).
- Layout: header (title, VI/EN, theme), note banner ("chưa áp dụng" + "Đọc thép Excel lại"), Tabs Detail thép / Thông số đặc thù / Thép mặc định,
  grouped Cards with labelled inputs (unit suffix mm / ° / d), Select for listed choices, Switch for bools, badge "chưa áp dụng" on pending fields,
  editable tables (add/remove row) for mass + lap, footer Mặc định / Chấp nhận / Thoát.
- Dirty tracking, Enter = accept, Esc = exit (confirm when dirty), inline error under the field + tab dot; host errors mapped by `field`.
- Add shadcn `label`, `table` (or Tailwind table) — no new npm deps beyond Radix already present; check before adding.
- Responsive 560–1000 px; light/dark; keyboard reachable.

## Steps
1. Contracts (`settingsContracts.ts`) + drift vitest reading `KataSettingsWebContracts.cs` / `KataSettingsForm.cs` / `KataSettingsFormChoices.cs` as text (init properties too) + mock with fixture; 2. form reducer + tests; 3. sections; 4. tables; 5. App tests (load, edit, error, save, defaults).

## Success
- `npm run typecheck && npm run lint && npm test && npm run build`; dev page screenshot both themes.

## Result
- Page `settings.html` (vite input `settings`; `embed-frontend.targets` inputs + incremental output now `dist/settings.html`), embedded in HPRebar R26 (`wwwroot/settings.html` in HPRebar.dll, checked).
- `src/kataSettings/`: `settingsContracts.ts` + drift test (15: every record incl. init-property records, commands, events);
  `settingsMockBridge.ts` (fixture snapshot; empty numbers refused, more via `refuse`); `formPath.ts` (paths = host error fields);
  `useSettingsForm.ts` (load, edit by path, host check 300 ms after the last change with stale answers dropped, defaults, save, close);
  `fields.tsx` (Section/Number/Select/Switch/Hook/LapZone/Text, errors under the field, `aria-invalid`), `DetailTab`, `SpecialTab` + `ShopTable`
  (add/remove rows), `JointsTab`, `SettingsHeader` (VI/EN + theme, same `hp.kata.*` keys as Kata Export), `SettingsApp`
  (tabs with error counts, notice banner, footer Mặc định / Chưa lưu / n ô cần sửa / Thoát / Chấp nhận, Enter accepts, Esc exits,
  shadcn AlertDialog before discarding changes), `i18n.ts` (VI from the WPF dialog, EN), `main.tsx` (`?file=edited|notice&theme=dark`).
- shadcn components added: `label.tsx`, `alert-dialog.tsx` (radix-ui already a dependency — no new npm package).
- Checks: typecheck ✅, lint ✅, vitest 197/197 (settings 29: contracts 15, paths 4, app 10), `npm run build` ✅, `dotnet build -c Debug.R26` ✅.
- Dev page looked at in the browser pane: light + dark, all 3 tabs, VI/EN, field error + tab count + footer state. Fixed while looking:
  long decimals shown to 6 significant digits (1/6 → 0.166667, value kept until typed), unit column aligned, faint off-switch on light cards.

## Open
- Not yet opened in Revit (phase 5). Accept/close order, Enter/Esc inside WebView2 and the modal owner only provable there.
