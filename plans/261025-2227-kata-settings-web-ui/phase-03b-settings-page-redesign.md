# Phase 03b — Settings page redesign (user: "xấu quá, đẹp hơn, hiện đại hơn")

Priority: high · Status: DONE 2026-10-25 (dev page; not yet opened in Revit) · Type: AFK (frontend only, no contract change)

## Design
- One scrolling page instead of three tabs; left nav (md+) lists the three groups and their sections with icons, error
  counts per group and scroll-spy highlight; click scrolls to the section.
- Header: icon tile, title in sentence case + subtitle, search box (filters rows by label, accent-insensitive; sections
  and groups with no match hide; empty state), VI/EN segmented, theme button.
- Sections: card with icon chip, title, "chưa áp dụng" amber badge; pending sections muted (dashed border).
- Rows: label left, control right; unit as suffix inside the input; short lists as segmented controls (hook angle,
  crank slope, lap origin, hanger angle); a dot marks values that differ from the saved file.
- Tables: spreadsheet look (borderless cells, header band, row hover, delete on hover).
- Footer: glass bar; Mặc định left; "n mục đã sửa" + "n ô cần sửa" chips; Thoát + Chấp nhận (Enter kbd hint).
- A refused save scrolls to and focuses the first wrong field.

## Checks
typecheck, lint, vitest (App tests rewritten for the single page), build, screenshots light/dark/EN/error/search.

## Result
- New: `navigation.ts` (outline), `SettingsNav.tsx`, `settings.css` (search hides empty cards/groups, empty state, thin scrollbar).
- Rewritten: `fields.tsx` (Section with icon, Row with changed dot + error icon, unit-inside boxes, `SegmentedInput`, table cells,
  `SearchableBlock`, accent-insensitive `fold`), `SettingsApp.tsx` (one page, scroll-spy, search, refused save focuses the field,
  footer chips, Enter kbd), `SettingsHeader.tsx` (icon tile, subtitle, search), `DetailTab`/`SpecialTab`/`JointsTab`/`ShopTable`;
  `useSettingsForm` exposes `savedForm`; i18n keys for subtitle/search/outline/changed/group descriptions.
- Tests: `SettingsApp.test.tsx` rewritten (12): outline + groups, segmented hook angle / lap origin, search (no marks, empty state),
  refused save focuses `field-joints-beam-stirrups`, changed count. vitest 199/199, typecheck, lint, build, `dotnet build -c Debug.R26`.
- Looked at in the browser pane: light, dark, VI/EN, search "noi thep", error + chips, tables. Fixed while looking: dark-mode
  accent text (`text-accent-strong` instead of `text-primary`), doubled focus outline in boxes, stronger selected segment.
