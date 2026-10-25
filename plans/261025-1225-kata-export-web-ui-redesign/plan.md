# Kata Export — giao diện web app hiện đại

Status: **Done (uncommitted)** — live 16 + 9 + 14 + 15 + 6 on a B03 copy; screenshots in `reports/`. — user 2026-10-25: "giao diện quá xấu, không giống web html", chose **Web app hiện đại** (AskUserQuestion). Frontend only (`HPBimToolkit.Frontend/src/kata/` + new primitives in `src/ui/`); no C#, no wire contract, no new dependency.

## Problems seen on the live page (reports of phase 6)
- The drawing sits in a thin strip of a big card; the left column of parameters is mostly empty; the Kata table is cut off below the fold.
- The toolbar is a row of plain checkboxes and grey buttons; the header is a bare title; the status bar is a wall of buttons.
- Bars are drawn at Kata's fixed 3 px at every zoom → on the whole run (0.024 px/mm) layers 25 mm apart merge into red bands.

## Target layout (1280 × 860 default, 720 min)
```
header   ◆ Kata Export · B03 (4 dầm · 33 600 mm)        [▤ KataB03.xlsm · Dam] [↻] [Chọn lại dầm]
drawing  card ~62 %: [2D|3D] [‹ Trước][Tiếp ›][⤢ Toàn dải] chips(Thép · Số hiệu · Mặt cắt) switch(Đảo chiều) … [↶ Hoàn tác]
         selection pill bottom-left; tools strip + minimap stay inside
panel    tabs: [Bảng ô Kata] [Thông số] [Cảnh báo n]  — table full width, sticky first column, scrolls
footer   ● status text                              [Đóng] [Đọc thép Excel] [Tạo thép Revit] [Xuất Excel ▶]
```

## v2 (proposal, waiting for approval)
User 2026-10-25: v1 "vẫn chưa đúng ý" → wants a more modern design shown first, implemented only after approval.
Mockup `mockup/kata-export-mockup-v2.html` (static, illustrative data) + `mockup/kata-export-mockup-v2-dark.png` / `-light.png`:
- Workflow rail on the left (Chọn dải dầm → Lập bảng → Thép từ Excel → Tạo thép Revit → Xuất Excel), with each step's state.
- The drawing gets a dotted background and floating toolbars (view, layers, measure, find, export / span navigation, zoom / minimap).
- Inspector on the right: KPI tiles, the selected span's properties, its bar groups, its section.
- Kata cells in a collapsible drawer under the drawing.
- Action bar at the bottom with a gradient primary button.

## v6 — built into Revit 2026 and live-verified (user 2026-10-25)
User: no "HPRebar Kata Pro" badge, no Đóng button (Esc / window X still close), footer left → right Xuất Excel (green) · Đọc thép Excel (blue) · Tạo thép Revit (orange) with clear colours, a VI/EN switch and a light/dark icon; in 3D no navigation hint line and no "571/571 thanh".
- `src/kata/i18n.ts` (page words VI/EN, `useT`), choices kept in `localStorage` `hp.kata.lang` / `hp.kata.theme` (theme follows Revit until picked). Host text (status, warnings, selection chip) and Kata's drawing text stay Vietnamese.
- Review [reports/code-review-v6.md](reports/code-review-v6.md): 1 High (Esc in the ViewCube menu closed the window) + 6 Medium fixed; 2D/3D tabs moved into the card toolbar.
- Checks: typecheck, lint, vitest 166 ✅; R26 build + deploy ✅; live on a B03 copy (Dam kata test): kata-web 16/16, web-rebar 9/9 (read only), native 14/14, tools 6/6, 3d 19/19, new `live-verify-kata-prefs.mjs` 7/7 (removes the choices from the shared WebView2 profile). Screenshots `mockup/live-v6-*.png`, browser `mockup/kata-v6-*.png`.

## v5.1 — no image export; Revit navigation and ViewCube in 3D (user 2026-10-25)
"Bỏ chức năng xuất ảnh, view 3d thép phải zoom, pan, xoay, viewcube giống revit hoàn toàn" (+ a picture of Revit's ViewCube).
- Removed: the drawing's "Xuất hình" menu (SVG / PNG ×2 / ×4 / print), `elevation/exportDrawing.ts`, its test and the harness step. The host's generic download-to-Save-dialog path in HPWebHost stays (unused by Kata now).
- 3D (`model3d/`): `revitNavigation.ts` (26 ViewCube regions + names, fit for both cameras, orbit about a pivot — pure, 9 tests), `cameraRig.ts` (orthographic by default like Revit's {3D}, perspective from the menu; wheel = zoom to the cursor, middle = pan via OrbitControls; Shift + middle = orbit about the selection or the model with Revit's green pivot ball; middle double click = fit; animated 350 ms turns), `viewCube.ts` (cube of 54 tiles → 26 regions lit on hover, click turns + fits, drag orbits; compass ring N/E/S/W — letters turn to that side, ring drag turns about the vertical; `data-faces` for harnesses), `Rebar3DView.tsx` (Home button, ▾ menu: Về Home / Vừa khung / Trực giao·Phối cảnh / Đặt view hiện tại làm Home; old preset buttons removed; `data-view` = region name). 2D/3D switch moved to the drawing's top centre.
- Checks: typecheck, lint, vitest 162 ✅; headless Chrome (SwiftShader) drive of the dev page: Home = TOP FRONT RIGHT, cube FRONT → FRONT, TOP → TOP, Shift+middle orbit (pivot ball shown), middle pan, wheel zoom, middle double click fit, Home ✅ (`mockup/3d-*.png`). `live-verify-kata-3d.mjs` ported (real mouse on the Radix tabs, Shift + middle orbit, cube faces, menu projections) — not run in Revit yet.

## v5 (built against the sample data, waiting for approval before Revit) — the window layout of the user's screenshot
User 2026-10-25: "Thiết kế lại giống hình ảnh gửi trên, chỉ xóa thông tin khung màu đỏ" (screenshot of the earlier Kata Export window; red box = "Các ô ghi sang Kata Excel" + the B3–B10 line). Code now follows that window with shadcn/ui pieces: title bar + "HPRebar Kata Pro" badge; command bar (Chọn lại dầm, Kiểm tra Excel | workbook + status); left card THÔNG SỐ KATA (B3:B10, cog pickers for B3/B4 over native selects); right card MẶT ĐỨNG DẢI DẦM (Trước/Tiếp, Đảo chiều, Toàn dải, Hiện thép/số hiệu/mặt cắt, undo icon, selection chip; 2D/3D in the drawing's top-right); warnings strip; footer (Số phần tử, Tổng chiều dài | Đọc thép Excel, Tạo thép Revit, Đóng, Xuất Excel). Removed: the Kata cells table, the workflow rail, the inspector, the drawer (`KataSteps`, `KataInspector`, `KataDetailsPanel`, `KataColumnsTable`, `inspectorData`). Dark palette = neutral greys of the window. Checks: typecheck, lint, vitest 157 ✅; screenshots `mockup/kata-v5-dark.png` / `-light.png` (dev page `?theme=…&column=3&rebar=1&warn=1`, sample warnings marked "[Ví dụ]"). Not built into the add-in yet.

## v4 (proposal, waiting for approval) — Kata cells table removed
User 2026-10-25 (screenshot of the window, red box around "Các ô ghi sang Kata Excel" + the B3–B10 line): drop it, the Kata workbook already shows it.
`mockup/kata-export-mockup-v4.html` = v3 without the "Bảng Kata (sheet Dam)" screen (cells C→K rows 10–27 and the B3:B10 / E1:J9 grid); the overview keeps the spans table and the steel settings card, with a "Mở sheet Dam" button instead. Implementing it also drops the drawer's "Bảng ô" tab of the shadcn v2 code (span selection stays on the drawing). Screenshots `mockup/kata-v4-*.png`.

## v3 (proposal, waiting for approval) — connected to the Kata workbook, more information
User 2026-10-25: v2 still not right; connect the Kata Excel file, modern UI with fuller information.
Read a **copy** of `KataB03.xlsm` (stdlib XML; the original was open in Excel and never touched): sheet Dam carries B3:B12, settings E1:J9 (anchorage 40d/30d, side bars Ø12×2, stirrup Ø10 a150/a200, cantilever a150, cover 30/25, cut-offs 0.25L/0.2L), rows 10–27 per column (geometry, top extra layers 13–16, bottom extra 17–18, stirrup types 25–27); `Thong_ke` / `Thong_ke_QS` hold only headers (Kata never filled them), so the schedule comes from the plan.
Mockup `mockup/kata-export-mockup-v3.html` + `kata-v3-data.js` (real B03 data) + screenshots `kata-v3-*.png`: shadcn dashboard layout (sidebar with screens + Excel connection card; breadcrumb top bar with the four commands), screens **Tổng quan** (KPIs: steel kg, concrete m³, kg/m³, main bars; elevation preview; kg per Ø; spans table; settings), **Mặt đứng & 3D** (the real drawing), **Bảng Kata** (all sheet Dam cells grouped), **Thống kê thép** (Kata Thong_ke layout: mark, shape sketch, Ø, segments, L, count, m, kg + totals per Ø), **Cảnh báo**.
Implementation would need new host data (C#): the sheet inputs (`KataBeamRebarSpec`) and a schedule built from the plan's bars with their own weights (the 3D fixture misses hooks: 2 888 vs 2 931 kg).

## v2 with shadcn/ui (built against the sample data, waiting for approval before Revit)
User 2026-10-25: React + shadcn/ui; approved the new npm packages, Kata page only, show before going into Revit.
- Packages (exact): radix-ui 1.7.0, class-variance-authority 0.7.1, clsx 2.1.1, tailwind-merge 3.7.0, lucide-react 1.55.0, tw-animate-css 1.4.0 (dev); components copied by shadcn CLI 4.21.4 into `src/components/ui/` (+ README: `bg-accent`/`bg-muted` → `bg-secondary`, since HP `accent` = brand blue and `muted` = grey text). The CLI wrote into a literal `@/` folder and installed a stray `cn` package — both removed.
- `src/kata/kata.css` (kata.html only): v2 palette over the HP tokens + shadcn tokens; `@` alias = `/src`.
- Views: `KataSteps` (workflow from `inspectorData.workflowSteps`), `ElevationCard` (Tabs 2D/3D, ToggleGroup layers, Switch, floating span navigation), `KataInspector` (KPIs, picked span, bar groups per span from Kata's tags, section thumbnail via `paintSection`), `KataDetailsPanel` (collapsible drawer, Tabs), `KataHeader`, `KataFooter`.
- Checks: typecheck, lint ✅, vitest 162 ✅ (4 new for `inspectorData`). Screenshots `mockup/kata-shadcn-v2-dark.png` / `-light.png` (dev page `?theme=…&column=3&rebar=1`). Not built into the add-in yet.

## Result
- New: `src/ui/SegmentedControl|ToggleChip|Switch|Tabs.tsx`, icons; `src/kata/KataHeader|ElevationCard|KataDetailsPanel|KataFooter.tsx`; `KataParameters` as tiles, `KataColumnsTable` with sticky header column, zebra rows and the Excel row per line; warnings as a tab that opens itself on a `[Chặn]` item.
- Drawing: bar / stirrup pens `barPx(scale)` = min(3, max(1, round(25 mm × scale × DPR))); the selection pen follows.
- The tool strip over the drawing uses the page tokens (icons, floating bar, menus).
- Checks: vitest 158, typecheck, lint ✅; R26 build ✅; live harnesses unchanged and green; Revit screenshots `reports/kata-ui-*-dark.png`. The light theme was seen only in the browser preview (mock).

## Steps
1. `src/ui/`: `SegmentedControl`, `ToggleChip`, `Switch`, `Tabs`, `IconButton` + icons (chevrons, undo, expand, excel, cube); font stack `Segoe UI Variable Text` first.
2. `KataApp` shell: header, drawing card, tab panel, footer; `KataParameters` as a grid of tiles; `KataColumnsTable` restyled (sticky header column, zebra rows, selected column band); warnings as a tab with a count badge (danger when a `[Chặn]` item exists).
3. Bar lineweight: `min(Kata px, max(1, bar Ø × scale))` for bars and stirrups (Ø 25 nominal); 3 px from ~0.12 px/mm.
4. Accessible names kept (`2D`/`3D` tabs, button labels, table / combobox names, test ids) so vitest and the live harnesses still drive the page; update tests where the parameters move into a tab.
5. Checks: typecheck, lint, vitest; `npm run dev` preview screenshots dark + light (mock B03); R26 build + live harness set on a B03 copy; screenshots in Revit.

## Risks
- The UI kit is shared with the dashboard: new primitives only, existing ones untouched except the font stack.
- Hidden tab content must not break tests that look for the table or the parameter boxes: the table is the default tab, tests open the other tabs explicitly.
