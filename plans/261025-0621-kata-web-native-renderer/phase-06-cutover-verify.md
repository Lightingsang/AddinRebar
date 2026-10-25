# Phase 6 — Cut-over, live parity, review, docs

## Steps
1. Native renderer becomes the only path on R26: remove `kata.drawing`, hidden canvas creation + `Rendered` subscription in `KataWebWindow`/`KataWebBridge`, `?renderer` flag; delete `HPWebHost/Rendering/DrawingSvgWriter.cs` + tests (no other user).
2. User decision: delete the WPF Kata Export window in every version — remove `KataExportView.xaml(.cs)`, `View/Controls/*` (painters, canvas, section view), `HPREBAR_KATA_UI`; `KataExportCommand` + ribbon buttons + `View/Web` compiled for R26 only (`#if REVIT2026`, `// Multi-version: Kata Export web only`); `KataSettings` untouched. Check R23/R27 still compile once (cheap insurance), even though they are out of focus.
3. Live on model copies (second Revit, debug port 9334): harness 19 + rebar 9 checks updated; perf: scene build ms, first paint ms, zoom frame time on the largest available run.
4. Screenshots dark + light; side-by-side with WPF window on the same beam.
5. `code-reviewer` + CODE_REVIEW_CHECKLIST; fix High/Medium.
6. ADR-0011 (supersedes ADR-0010 decision 3), CLAUDE.md + AGENTS.md hand-inserted, install guide, skill `revit-webview-addin` reference.

## Success
Build R26 ✅, Core/WebHost/Toolkit/vitest ✅, live checks ✅, no High open, docs updated. Xuất Excel still needs a user-opened scratch workbook.
