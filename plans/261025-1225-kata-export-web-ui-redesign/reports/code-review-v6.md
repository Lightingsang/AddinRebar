# Code review — Kata Export web UI v6 (2026-10-25)

Scope: uncommitted frontend diff since 4dd999a (shadcn redesign, VI/EN, page theme, Revit 3D navigation + ViewCube, image export removed) + harnesses. Reviewer: code-reviewer agent, read-only.

| # | Sev | Finding | Outcome |
|---|---|---|---|
| 1 | High | Esc closing the ViewCube menu (portalled) bubbled through React to the 3D box → `kata.run('close')`; menu pointermove → hover under it | Fixed: `onKeyDown` / `onPointerMove` ignore `defaultPrevented` and targets outside the box. vitest `Esc that closes the ViewCube menu keeps the window open` (fails without the guard); live: menu closes, window stays |
| 2 | Med | Pivot ball stayed after a cube drag | Fixed: `ViewCubeHandlers.orbitEnd` on pointerup / pointercancel after a drag |
| 3 | Med | Light `--success` / `--steel` < AA on white | Fixed: #15803d / #c2410c |
| 4 | Med | White text on dark accent and `--action-*` < AA | Fixed: dark `--primary` #1565c0; actions #1b7a42 / #1f66c2 / #b54a14 both themes; hover darkens |
| 5 | Med | Invisible focus on the cog `<select>` | Fixed: `has-[:focus-visible]:ring-2` |
| 6 | Med | Floating 2D/3D tabs covered the tool strip / section panel when narrow | Fixed: tabs moved into the card toolbar |
| 7 | Med | Prefs harness wiped the user's saved choices | Fixed: saves both keys, starts from defaults, restores them (live `[null,null] → [null,null]`) |
| 8 | Low | EN bar length formatted vi-VN | Fixed: `t.numberLocale` |
| 9 | Low | Untranslated page text (`không có mặt cắt này`, find result frozen in old language) | Fixed both; selection hint and section caption are drawing text, stay Vietnamese (ADR-0012) |
| 10 | Low | No pan without a middle button (right button off) | Kept: Revit behaviour, as the user asked |
| 11 | Low | Middle double click by time only | Fixed: ≤ 4 px apart, Shift presses not counted |
| 12 | Low | Two WebGL contexts released only by GC | Fixed: `forceContextLoss()` before `dispose()` (view + cube) |
| 13 | Low | Tabs without `TabsContent` (`aria-controls` dangling), disabled-tab title unseen | Kept: tabs are the 2D/3D switch the tests and harnesses drive by role; noted |

Checks after fixes: typecheck, lint, vitest 166/166; R26 build + deploy; live on a B03 copy: kata-web 16/16, web-rebar 9/9, native 14/14, tools 6/6, 3d 19/19, prefs 7/7, Esc-in-menu manual script OK.
