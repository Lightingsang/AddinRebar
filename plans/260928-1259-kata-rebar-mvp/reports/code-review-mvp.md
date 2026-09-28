# Code review — Kata Rebar MVP (uncommitted, 2026-09-28)

## Xử lý sau review (2026-09-28)

| # | Kết quả |
|---|---|
| H1 | ✅ `KataPolylineSegments` (Core, có test) trả cạnh đóng của vòng kín; `KataRebarCurveFactory.Curves` dùng nó |
| M1 | ✅ G6 ≤ 0 → không vẽ đai + cảnh báo trong rules |
| M2 | ✅ Handler giữ `Document` của cửa sổ, từ chối khi model đang mở khác |
| M3 | ✅ Thông báo đổi: chọn kiểu khác hoặc đóng/mở lại cửa sổ |
| M4 | ✅ Đọc Excel lỗi → `ForgetSheet` (spec null, CanGenerate false) |
| M5 | ✅ `CheckFirstStirrup` so đai đầu (hộp out-to-out, góc, trạm) ±3 mm; lệch → fallback đai lẻ; kiểm lại hướng sau khi đảo |
| M6 | ⏭ Không xử lý: bản ac6c2d4 chưa từng chạy trong Revit; thanh cũ chỉ có Partition = tên dầm → không phân biệt được với thanh vẽ tay |
| L1 | ✅ Planner trừ khối lượng cốt giá tự động đã bỏ |
| L2 | ✅ Reader đổi ô lỗi (Int32 của Value2) thành ô trống |
| L3 | ✅ Excel bận ở `Worksheets.Item` báo "Excel đang bận" |
| L5 | ✅ Matcher bắt cả `Autodesk.Revit.Exceptions.ApplicationException` |
| L8 | ✅ Đọc lại Excel giữ kiểu thép đã chọn theo đường kính |
| L4, L6, L7 | ⏭ Chấp nhận (nhịp ≤ 100 mm; nhánh chết; margin cùng kiểu file XAML hiện có) |

Kiểm lại: build Debug.R26/R25/R24 0 lỗi; `dotnet test HPRebar/HPRebar.Core.Tests` 703/703.

Scope: `HPRebar.Core/KataRebar/**`, `HPRebar/KataRebar/**`, `HPRebar.Core.Tests/KataRebar/**` (excluded: Application.cs, HPRebar.csproj, install/, .gitignore, .claude/, build-kata-installer.ps1). Rule table = contract, not re-litigated. Read-only.

## Gates (run by reviewer, isolated `--artifacts-path`, `-p:DeployAddin=false`)
| Check | Result |
|---|---|
| build Debug.R26 / R25 / R24 | pass, 0 errors, 0 KataRebar warnings |
| `dotnet test HPRebar.Core.Tests` | 697 + 3 `ThemeTokenCoverageTests` (fail only off-repo, pass 3/3 via junction layout) = 700/700 |
| Golden (rule table) re-derived by hand | matches: top x 43→6757 leg 443; bottom inset 45 → x 88, leg 288; stirrups 15@100 from 450 / 15@200 from 2000 / 15@100 from 4950; J9 40 → b 22, y ±110 |
| Reversal math (`KataBeamPlacement`) | correct: (−Axis, −Transverse, Z) right-handed, `station = origin − x`; stirrup normal = across × up = local +X both ways |
| Feature folder / file size / plan refs in comments | OK (root = 4 files; subfolders Model/Service/View/ViewModel; max 276 lines; no phase/finding refs) |

## High

### H1 — Fallback single stirrups are drawn open (3 sides)
- `KataStirrupCurveFactory.cs:292-299` builds 5 points (last = first) with `isClosed: true` and calls `Simplify(1.0)`.
- `Polyline3.Simplify` (`HPRebar.Core/BeamRebar/Models/Polyline3.cs:63-66`) removes the closing vertex of a closed polyline → 4 points.
- `KataRebarCurveFactory.Curves` (`HPRebar/KataRebar/Service/KataRebarCurveFactory.cs:31-47`) ignores `IsClosed` → 3 lines. Sibling `BeamMainBarCreator.BuildCurves` (`BeamRebar/Service/BeamMainBarCreator.cs:106-111`) adds the closing segment; the Kata copy does not.
- Scenario: project without shape 41/M_41/M_T1/T1/… (handler logs and passes `shape = null`, `KataRebarExternalEventHandler.cs:129-131`) or any zone whose set fails → every stirrup is a U open at one side, created without error, counted as "đai lẻ".
- Fix: in `Curves`, after the loop `if (polyline.IsClosed && points.Count > 2) curves.Add(Line.CreateBound(last, first))` (points = simplified). Add a Core test that the closed hoop keeps 4 corners + `IsClosed`, and consider a Revit-side assert (4 curves for `StirrupClosed`).

## Medium

### M1 — G6 = 0 (or error cell) makes Generate fail with an unfixable message
- Parser keeps 0 (`KataDamSheetParser.cs:57`); rules default to Ø10 (`KataDetailingRuleBuilder.cs:29`); zones are laid out regardless of diameter (`KataStirrupZoneLayout.Build`); handler requires a type for `plan.Rules.StirrupDiameter` (`KataRebarExternalEventHandler.cs:136-141`); VM mapping skips diameter ≤ 0 (`KataRebarTypeResolver.cs:67,80`).
- Result: VM check passes, handler returns "Thiếu RebarBarType cho Ø10 … chọn kiểu khác trong bảng" but the table has no Ø10 row. Scope message "G6 không có đường kính đai" implies 0 = no stirrups, layout disagrees.
- Fix: one source of truth — build the mapping from the plan (`Layout.MainTop/Bottom` diameters + `Rules.StirrupDiameter` when zones exist, same as `Diameters(plan)`), or treat G6 ≤ 0 as "no stirrups" (no zones + `Skipped` line). Decide which matches the rule table (G6 "vẽ").

### M2 — Active-document switch applies stale ElementIds to another model
- VM/type list come from the document at command time (`KataRebarCommand.cs`, `KataRebarViewModel.cs:58`); handler always uses `app.ActiveUIDocument` (`KataRebarExternalEventHandler.cs:49,99,120`).
- Scenario: window open on model A, user activates model B, clicks Generate → ids of A measured/drawn in B (ids can resolve to unrelated framing / bar types in B), cleanup deletes by host in B.
- Fix: carry the originating `Document` in the request (or its `PathName`+`Title`) and refuse when `uidoc.Document` differs ("Cửa sổ Kata Rebar thuộc mô hình khác — đóng và mở lại").

### M3 — Bar-type list frozen at window open; UI advice cannot work
- `KataRebarTypeResolver` ctor snapshots `RebarBarType`s once (`KataRebarTypeResolver.cs:21-36`); `LoadSheet` reuses it. VM (`KataRebarViewModel.cs:93`) and XAML tell the user "Tải kiểu thép vào dự án rồi đọc lại Excel" — reading Excel again never sees the new type.
- Fix: refresh the list on the API thread (add a runner call, e.g. `ListBarTypesAsync`, used by Refresh), or change the text to "đóng và mở lại cửa sổ".

### M4 — Failed Excel refresh keeps the previous sheet generatable
- `LoadSheet` returns early on read/parse failure without clearing `_spec` or `CanGenerate` (`KataRebarViewModel.cs:109-131`).
- Scenario: user switches workbook / Excel busy / parse error → red status, but Generate stays enabled and draws the previous sheet.
- Fix: on failure `_spec = null; Replan();` (CanGenerate → false) or keep the spec but set `CanGenerate = false` until a successful read.

### M5 — `ScaleToBox` compromise is not verified
- API doc (RevitAPI.xml 2026.4, `ScaleToBox`): if the shape is over-constrained "a compromise is attempted … scaling the whole shape until either the width or the height is correct" — no exception. `KataStirrupSetCreator.cs:67-80` only checks direction and count.
- Scenario: office shape "41" with a fixed/hook-driven parameter → stirrups of wrong size, silently accepted as a set.
- Fix: after `Layout`, read bar 0 centreline (`GetCenterlineCurves`), map to local via `PointMapper.ToLocal`, compare the Y/Z extents to `OutToOutWidth/Height − ds` and X to `StartStationX` (±2 mm); mismatch → throw `InvalidOperationException` → sub-rollback → singles (after H1 is fixed). Also re-check `GrowsAlong` after the flip (line 74-75) and throw if still wrong.

### M6 — Bars of the previous KataRebar build are never cleaned up
- Old tag `HPRebar_Kata_{beamName}` (HEAD `KataRebarCleanupService`), new tag `HPRebar_Kata:{hostUniqueId}` (`KataRebarTag.cs`, cleanup `KataRebarCleanupService.cs:140-151`).
- Scenario: a model already drawn with ac6c2d4 (an installer script exists, commit 2cf8bc6) → re-run adds a second full set on the same beam.
- Fix (host-scoped, so still safe): also delete rebars hosted on the picked hosts whose comment starts with the legacy `HPRebar_Kata_` prefix; log the count separately. Drop to Low if ac6c2d4 never left the dev machine.

## Low
| # | File:line | Issue | Fix |
|---|---|---|---|
| L1 | `KataRebarPlanner.cs:44-48`, `KataRebarCalculator.cs:84-86` | Weight includes auto side bars that the planner strips (h ≥ 700) → preview kg overstated | recompute `TotalSteelWeightKg` after stripping |
| L2 | `KataCellTable.cs:124,144` | COM `Value2` returns error cells (#REF!, #N/A) as `Int32` codes → parsed as −2.1e9 (G6 error → M1) | treat `int` from COM as error → null (numbers arrive as `double`) |
| L3 | `KataDamComReader.cs:42-49` | `Worksheets.Item` busy COMException reported as "không có sheet Dam" | `catch (COMException ex) when (!IsBusy(ex))` |
| L4 | `KataStirrupZoneLayout.cs:168-176` | span ≤ ~100 mm → left/right first stirrups coincide or cross | skip right zone when `firstRight <= lastLeft` |
| L5 | `KataBeamMatcher.cs:57` | filter misses `Autodesk.Revit.Exceptions.ApplicationException` (support scan / solids) → surfaces as generic "Lỗi: …" not a failed match | add it to the filter |
| L6 | `KataRebarOrchestrator.cs:41-43` | `TryGetValue` false → silent 0 stirrups with success text (dead today, handler guarantees the type) | throw or add a result warning |
| L7 | `KataRebarView.xaml:142,145` | new raw `Margin` values beside `Spacing` tokens (pre-existing pattern) | use `Spacing.*` tokens when touching the file |
| L8 | `KataRebarViewModel.cs:109-124` | Refresh rebuilds `BarTypeMappings` → user's manual type choices lost | keep selection by diameter when rebuilding |

## Checked, no finding
- Transactions: step exception → rollback + rethrow; `Commit` ≠ Committed → throw; group `Assimilate` status checked; escaping non-filtered exceptions still roll back (using-disposed group/sub-transaction) and are caught by the handler catch-all → VM `Busy` catch.
- `SubTransaction` per zone with rollback on failure; singles only for the failed zone's stations (no duplicates).
- Multi-version: `#if REVIT2026_OR_GREATER` `BarTerminationsData(doc)` overload exists in 2026.4 XML; pre-2026 overload with null hooks OK; builds R24/R25/R26 clean.
- COM: every object released in `finally`; `TargetInvocationException` unwrapped by `ComLateBinding`.
- Handler re-measures + re-plans before drawing and refuses blocked plans; VM `CanGenerate` requires a measured, unblocked plan.
- Cleanup deletes only tagged bars whose host is in the picked run.
- Anchorage / cover / stirrup zones match rule table C1–C4, A1–A5, S1–S4, G1.

## Recommended order
1. H1 (one line + test). 2. M1, M4 (VM/plan consistency). 3. M2, M3. 4. M5 before live verify (phase 03 risk list already names it). 5. M6 if the old build was distributed. Low as convenient.

**Status:** DONE_WITH_CONCERNS
**Summary:** Builds R24/R25/R26 clean, 700/700 tests; core math and reversal verified against the golden. One High (fallback stirrups open), six Medium (G6=0 unfixable failure, doc switch, frozen type list, stale sheet after failed refresh, unverified ScaleToBox, legacy tag cleanup).
**Concerns/Blockers:** H1 must be fixed before phase-03 live verify if the test model lacks a closed stirrup shape.
