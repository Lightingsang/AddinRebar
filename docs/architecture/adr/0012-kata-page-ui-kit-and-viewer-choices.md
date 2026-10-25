# 0012 — Kata Export page: shadcn/ui kit, VI/EN and light/dark chosen in the page, no drawing export

- **Status:** Accepted (2026-10-25, by user — plan `plans/261025-1225-kata-export-web-ui-redesign/`)
- **Tags:** [REVIT] [PROJECT]

## Context
The first web Kata Export page (ADR-0011) used the hand-made primitives of `HPBimToolkit.Frontend/src/ui/`. The user rejected two redesigns built that way and asked for React + shadcn/ui. After that the user asked for three more changes:
- a language switch (Vietnamese / English) and a light/dark switch in the window's header;
- the three footer commands in workflow order, in clear colours, with no Close button;
- the drawing's image export (SVG / PNG / print) removed.

The page runs in Revit 2026's own WebView2. Every Revit instance of the user shares that WebView2 profile, and so its `localStorage`.

## Decision
1. **UI kit:** the Kata page (`kata.html`) uses shadcn/ui components copied into `src/components/ui/`. The npm packages are pinned exactly and were approved by the user on 2026-10-25:
   - radix-ui 1.7.0
   - class-variance-authority 0.7.1
   - clsx 2.1.1
   - tailwind-merge 3.7.0
   - lucide-react 1.55.0
   - tw-animate-css 1.4.0 (dev)

   The tokens live in `src/kata/kata.css`, which only `kata.html` loads. The dashboard (`index.html`) keeps `index.css` and `src/ui/` unchanged.
2. **Language:** the page's own words come from `src/kata/i18n.ts` (`useT`, VI and EN). These stay in Vietnamese because they come from elsewhere:
   - text the host sends (status, warnings, selection line, workbook name);
   - the text of Kata's drawing.
3. **Theme:** the page follows Revit's theme until the user picks one with the header switch. That choice overrides the host theme for the page and its drawings (2D, sections, 3D).
4. **Persistence:** both choices are kept in `localStorage` (`hp.kata.lang`, `hp.kata.theme`). Reads and writes are wrapped, so a refused storage just forgets the choice. Harnesses that change a choice must remove it before they end, because the profile is the user's (`live-verify-kata-prefs.mjs`).
5. **No drawing export:** the export menu and `exportDrawing.ts` are deleted. The host's Save-dialog download path in `HPWebHost` stays, because it is shared code.
6. **Footer:** left to right, Xuất Excel (Excel green), Đọc thép Excel (blue), Tạo thép Revit (orange), each as a filled button with white text (`--action-*` tokens). There is no Close button; Esc and the window's own close button still close it.

## Consequences
- \+ The Kata page gets accessible components (Radix) and a consistent look. Its labels can be read in English.
- \+ The light/dark choice no longer depends on Revit's per-user setting, which harnesses must not change.
- − The page carries 6 more npm packages. Updates go through `npm ci` with exact versions.
- − An English page still shows Vietnamese host messages until the view model is translated in C#.

## Alternatives
- **Keep `src/ui/` primitives.** The user rejected two designs built with them.
- **A language setting on the host (C#).** That would translate host messages too, but it needs view model changes for a page-only request. It is left for when English host text is asked for.
- **Session-only choices.** Rejected: the user would pick the language again on every opening.

## Rules
DEPENDENCY_RULES D6 (new packages approved by the user). ADR-0008 P5 exception as extended by ADR-0010 and ADR-0011.
