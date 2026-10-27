# Phase 05 — live verify (Revit 2026.5, harness Revit pid 23300 on a copy of DynamoSampleModel.rvt)

Date 2026-10-25. Harness `HPRebar/HPBimToolkit/tools/harness/live-verify-kata-settings.mjs <pid> 9334 <shots>`; Revit started by
`start-revit-with-toolkit.ps1` with `HPREBAR_KATA_DEBUG_PORT=9334`. Build = phase 1–4 page (before the 3b redesign).
No `KataSettings.json` existed on the machine: the harness removes it at the end (also on an abnormal exit).

## Result: 9/10 PASS
| Check | Result |
|---|---|
| ribbon Kata Settings opens `settings.html` in the modal window (WebView2 inside `ShowDialog` works) | PASS |
| no settings file → Kata's defaults (11700) | PASS |
| end zone 0.7 → host rule message under the field "Tỉ lệ vùng đai dày 0 … 0.5 (× Ln)." | PASS |
| footer counts one field to fix | PASS |
| Accept with a wrong value keeps the window and writes nothing | PASS |
| lap row added and filled | PASS |
| edited form has no error | PASS |
| Enter accepts, window closes | PASS |
| file holds 11000, 0.3, `16:480,640,400,500` | PASS |
| reopen through the ribbon | FAIL — UIA did not reopen within 240 s (harness timing, see below) |

Not run live (after the failed reopen): reopened values, Defaults, Exit-with-changes dialog, Escape. Covered by vitest only.

## Harness lessons (kept in the script)
- CDP clicks need the element in the viewport: `scrollIntoView` first (rows below the fold were missed, value stayed default).
- `Emulation.setFocusEmulationEnabled` so keys/text reach a page whose window is not in front (`cdp-session.mjs emulateFocus`).
- Never click the HPRebar ribbon tab when it is already selected: Revit minimises the ribbon.
- One UIA walk of Revit's ribbon: 10 s – 2 min; a hung `page.screenshot()` after the window closed skipped `finally`
  once → leftover settings file removed by hand; now `process.on('exit')` restores and screenshots time out.
- Bridge timings (log): `settings.load` 0–23 ms, `validate`/`save` < 31 ms.

## Rerun on the 3b redesign (2026-10-26, fresh harness Revit, final build): 17/17 PASS
Screenshots `reports/redesign/settings-0{1,2,3}-*.png` (Revit dark theme). All checks above plus: reopen shows the saved values,
Defaults shows 11700 and writes nothing, Exit after a change asks, Discard closes and keeps the file, Escape with nothing
changed closes, Revit responding. No settings file left behind; harness Revit closed.

Found on the way (fixed, each rerun):
- the Defaults toast sat over the footer's Exit / Accept (shared `ToastProvider` gets an additive `bottom` prop; settings uses `bottom-16`);
- doubled focus ring inside the boxes in WebView2: index.css's unlayered `:focus-visible` outline beats Tailwind's layered
  utility → `input:focus-visible { outline: none }` in `settings.css`;
- harness: UIA child must be a plain (not detached) process; wait 1.5 s after a window opens before the first click;
  "clean window" detection reads the new footer chip ("mục đã sửa").
- A Revit that already ran the harness keeps its in-memory settings after the harness deletes the file
  (`KataSettingsStore` caches, as before) — the "no file → defaults" check is only meaningful on a fresh Revit.
