# Code review — Kata Export rebar round trip (uncommitted, 2026-09-29)

Scope: `git diff HEAD` + untracked under HPRebar.Core/KataRebar, HPRebar.Core.Tests/KataRebar, HPRebar/KataRebar, HPRebar/KataExport, Application.cs, HPRebar.csproj, install/Installer.cs, tools/build-kata-installer.ps1. User decisions in plan.md respected (none below asks to reverse them).

Checks run (isolated `--artifacts-path`, no deploy):
| Check | Result |
|---|---|
| `dotnet test HPRebar.Core.Tests` | 763/766 — the 3 failures are `ThemeTokenCoverageTests` (walk up to `HPRebar.slnx`, known artifacts-path artefact), not Kata |
| `dotnet build HPRebar.csproj -c Debug.R26` / `Debug.R24` | pass, 0 warnings in Kata files |
| `-c Debug.R26 -p:KataOnly=true` | pass |
| `install/Installer.csproj -c Release` | pass |

Verified OK: every Revit call in Preview/Generate runs inside `Execute` (API thread); doc-equality guard; `KataRebarTypeResolver` materialises types on the API thread; hook "Right" rule `facing × normal` matches Revit's definition (mapper frame is right-handed incl. reversed placement); `SetLayoutAsNumberWithSpacing(n, s, barsOnNormalSide:true, …)` with normal = +local X lays copies along +X (and `CheckLayout` verifies); `Diameters(plan)` includes BarSets so `barTypes[set.Diameter]` cannot KeyNotFound; station map direction `plan.Reversed == IsReverse` is correct because matcher and Kata Export share `KataRunReader`/segmenter order and the elevation mirrors stations under IsReverse.

## H — must fix

### H1 Inner U / □ / C hooks: a point is used as a direction
- `KataInnerStirrupLayout.cs:151,156,163,168` sets `HookToward` = the **point** the hooks turn to (centre of the hoop / bar a) — pinned by `KataInnerStirrupTests.cs:55` (`HookToward.Y == 107`, the bar's Y).
- `KataBarSetCreator.cs:52` turns it into a model **direction** (`AxisY*Y + AxisZ*Z`), `KataRebarCurveFactory.cs:37-38,67` dots the end's "right" with that direction. `KataBarSet.cs:40` documents it as a direction; `KataSideBarLayout.cs:109` (C tie, `(0,0,1)`) uses direction semantics — the two producers disagree.
- Failure: U open at top — both ends test `right·toward` with right = +Y at both ends, so the two hooks always turn the same Y way: one hook turns outward (into cover/outer hoop) for every U. □ hoop centred or left of centre (e.g. "Đai □ 2-3" of 4 bars): end hook at the top-left corner turns −Y (outward). "Đai C 2" on the middle bar of an odd count (`ya = 0`): dot = 0 at both ends → both "Left" → Z-shaped tie. Only □/U right of centre and C off-centre come out right, by accident.
- Fix: treat `HookToward` as a local point everywhere: in `CreateHooked` pass `mapper.ToXyz(HookToward with X = shape X)` and use `towardPoint − curves[0].GetEndPoint(0)` for the start, `towardPoint − curves[^1].GetEndPoint(1)` for the end. Change the side-tie producer to a point above the layer (e.g. `(0, 0, 0)` = beam top, above `zt`), fix the `KataBarSet.HookToward` doc. Add a Core test computing both end orientations for U/□/C/tie with the same rule.

### H2 Symmetric run + sheet exported reversed → bars mirrored in the model
- `KataSheetGeometryCheck.cs:58`: `reversed = backwardScore + 2 < forwardScore`; a run symmetric within 2 mm (equal spans, equal column widths — very common) is always read forward.
- Failure: user exported with IsReverse = true (sheet column C = Revit far end), fills asymmetric steel (e.g. support C 3Ø20, support G 2Ø20). Planner places column C's bars at Revit's near end; `KataBeamPlacement` builds it that way; canvas (`KataExportViewModel.Rebar.cs:148`, sameOrder = false) faithfully shows the same mirror, so the preview does not reveal it except by column labels. Wrong steel at the wrong support, silently.
- Fix: break the tie with evidence the sheet carries: row 22 grid names on support columns (Kata Export writes them) vs grid crossings measured in Revit, else the export window's `IsReverse` as a preferred direction (`Plan(spec, measured, settings, preferReversed)`); when still ambiguous and the steel is asymmetric, add a warning naming the assumed direction.

## M — should fix

### M1 Repick keeps the old rebar plan and `CanGenerateRebar`
- `KataExportViewModel.cs:247-248` reloads the session but never calls `ClearRebar()`. The old plan is painted over the new elevation with a new station map, and "Tạo thép" stays enabled; Generate re-plans the old sheet on the new beams (usually blocked by row 11, but a geometrically equal other beam line gets another beam's steel with only a preview-time B3 warning that is never recomputed).
- Fix: `ClearRebar()` (and `RebarSpec = null` or auto re-preview) after a successful repick.

### M2 Side-bar C ties share the outer hoop's plane
- Ties at `SpanStart + FirstStirrupOffset + i·400` (`KataSideBarLayout.cs:91`); left-zone hoops at `SpanStart + FirstStirrupOffset + i·sDense` (`KataStirrupZoneLayout.cs:111`). With sDense 100/200 every tie in the end zones sits in a hoop's plane; its 180° hook arcs (bend radius from the bar type) reach the hoop leg → hard clash in Revit / fabrication.
- Fix: offset ties by `±ds` like the inner stirrups (`KataInnerStirrupLayout.cs:118`), or `−ds` so they never meet inner stirrups at `+ds`.

### M3 Side bars of adjacent spans overlap collinearly in narrow interior supports
- `KataSideBarLayout.cs:39-40,76-77`: each span reaches `min(10d, width − a)` into the shared support at the same (y, z) when layer counts match. Width < 20d (Ø14 in a 220/250 column) → both bars occupy the same line for `20d − width` mm → duplicate/clashing bars.
- Fix: at interior supports cap each reach at `width/2` (or stop the second bar short by the overlap), or run one continuous bar when layers/diameter match.

### M4 Strong-side leg at an interior support drops through the other levels
- `KataSupportTopBarLayout.cs:174-179` + `Anchor` `legRoom = level.Z − bottomBarZ` (`:204`): at an end support the bottom bars bend up and lower levels stop inboard, but at an interior support the bottom main bars and the lower rows 14-16 (straight weak side or symmetric) run through the far face. The row-13 leg (Y between main bars) goes down to exactly the bottom-bar centre level and crosses any lower-level bar or bottom bar sharing its Y.
- Fix: at interior supports clamp the leg to above the next level below (`nextLevel.Z + (d1+d2)/2 + gap`) and to `bottom + clear gap`, or pick Y slots for lower levels that avoid the strong slots; report the shortfall like end supports do.

### M5 Settings dialog: unowned modal + Enter loses the typed value
- `KataExportViewModel.Rebar.cs:207-211`: `Owner = Application.Current?.Windows…` — `Application.Current` is null inside Revit, so the modal has no owner; `ShowDialog` disables Revit + Kata window and the dialog can fall behind → apparent freeze. Rule: modal owner = `UiApplication.MainWindowHandle` / the Kata window.
- `KataSettingsView.xaml:66-110` TextBox bindings update on LostFocus; Accept is `IsDefault` (`:120`) → typing a value then Enter saves the previous value.
- Fix: owner via `WindowInteropHelper(window).Owner = Process.GetCurrentProcess().MainWindowHandle` or pass the Kata window; `UpdateSourceTrigger=PropertyChanged` on the TextBoxes.

## L — nice to fix
- L1 `KataExportExternalEventHandler.cs:55,72`: `Raise()` result ignored — `Denied`/`TimedOut` leaves the TCS pending, `IsBusy` stuck true, all buttons disabled until the window is closed. Fail the request when not Accepted/Pending.
- L2 `OpenSettingsCommand` (`KataExportViewModel.Rebar.cs:196`) not gated on `IsBusy`: Accept during a preview starts a second preview; the first's `finally` clears `IsBusy` early.
- L3 `KataSettingsViewModel.cs:59-61`: "không ghi được file" message set then the window closes immediately — never seen. Show it in the Kata window status instead.
- L4 `KataSettingsJson.cs:126,165` + `KataSettingsStore.Load`: file values are not validated like the dialog (`Accept`). A hand-edited `1e400` parses as ∞ on net8 → `RoundUp` returns NaN → NaN station → Revit exception; negative hook factors pass. Clamp non-finite / ≤ 0 to defaults on Read.
- L5 `KataSideBarLayout.cs:64`: row 20 "2f12;0" → `Any(Count<=0)` → no side bars at all; intent is probably "0 alone = none". Test only the single "0" item.
- L6 `KataRebarTypeResolver.cs:87-89`: BarSets rows are added before "Cốt đai" and dedup by diameter, so the stirrup type row is labelled "Móc C giữ cốt giá nhịp 1" — confusing when picking the stirrup type.
- L7 `Installer.cs:19` GUID shared by HPRebar and HPRebar-KataExport with `MajorUpgrade.Default`; the script defaults `-Version 1.0.0` (`build-kata-installer.ps1:24`) → on a machine with the full HPRebar (GitVersion 2026.x) the Kata MSI is refused ("newer version installed"); installing HPRebar later silently removes the Kata product. Sharing the upgrade code is right (same AddInId/files), but derive the Kata version from the same versioning or document the exclusivity.
- L8 Canvas: `KataElevationRebarPainter.Paint` (`:37-44`) never draws BarSets (ties / inner stirrups) on the elevation; section card Y is not mirrored when the map direction is −1 (section viewed from the other side).
- L9 Preview/Generate measure supports with the *current* `ActiveView` (`KataRebarWorkflow.cs:234`), not the view the session was built on; switching to a sheet/3D view with a section box gives a different support count → confusing "khác số gối" block.

## Plan follow-ups
- Phases 1–6 code present; phase 7 (live verify 2 spans) not run by this review — H1/M2/M4 are exactly what a live 2-span check with inner U/□ would show.
- Next: fix H1 + H2 + M1 before live verification; M2–M4 before any release build.

Status: DONE_WITH_CONCERNS

## Xử lý (2026-09-29)
| # | Trạng thái | Cách sửa |
|---|---|---|
| H1 | sửa | `HookToward` = điểm ở cả hai phía; `CreateHooked` lấy (điểm − đầu thanh) từng đầu; móc C cốt giá hướng về tâm cốt giá (trên thanh C); test |
| H2 | sửa | `KataSheetGeometryCheck.Compare(…, preferReversed)`: chênh ≤ 2 mm → theo chiều cửa sổ (IsReverse), rõ hơn → fit tốt hơn; Kata Export truyền IsReverse; test 4 ca |
| M1 | sửa | chọn lại dầm → `ClearRebar()` |
| M2 | sửa | móc C cốt giá lệch +2·d_đai so với đai ngoài (đai trong +d_đai) |
| M3 | sửa | gối giữa: cốt giá mỗi bên ≤ nửa gối − d/2; test |
| M4 | sửa | gối giữa: chân bẻ vế lớn dừng cách thép dưới 1 khe lớp; test |
| M5 | sửa | owner = cửa sổ có DataContext này (không cần Application.Current); bỏ IsDefault ở Chấp nhận (bấm nút làm mất focus → binding cập nhật) |
| L1 | sửa | `Raise()` ≠ Accepted/Pending → request fail ngay |
| L2 | sửa | Cài đặt khoá khi IsBusy |
| L3 | sửa | lỗi ghi file → báo ở status cửa sổ Kata |
| L4 | sửa | JSON: giá trị ngoài miền → mặc định Kata; test |
| L5 | sửa | hàng 20: bỏ phần "0", chỉ "0" một mình = không cốt giá; test |
| L6 | sửa | dòng "Cốt đai" trước "Móc C / đai trong" |
| L7 | sửa | script: version mặc định = ngày (yyyy.M.d); ghi rõ dùng chung upgrade code với HPRebar (loại trừ nhau) |
| L8 | một phần | mặt cắt lật khi map ngược; chưa vẽ bộ móc/đai trong trên mặt đứng (chỉ mặt cắt) |
| L9 | sửa | đo theo view lúc mở phiên / chọn lại dầm, view mất → view đang mở |
Core 774/774; build R26/R25/R24 0 lỗi.
