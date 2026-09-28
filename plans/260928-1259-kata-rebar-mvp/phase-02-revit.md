# Phase 02 — Revit (HPRebar/KataRebar)

Status: done (build Debug.R26 / R25 / R24 `-p:DeployAddin=false`, 0 lỗi, 0 cảnh báo KataRebar). Chạy trong Revit: CHƯA TEST (phase 03).

## Thay đổi
- `KataDamComReader` (Service): A1:BZ44 một lần, release mọi COM object, báo Excel bận. Xoá `Excel/` (ComKataDamReader + ClosedXmlKataDamReader).
- `KataBeamMatcher.Measure`: dùng `KataRunReader` + `KataSupportCollector` + `KataSegmenter` của KataExport → `KataMeasuredBeam` + extents; b/h từ Revit.
- `KataBeamPlacement`: gốc = mép ngoài gối đầu theo thứ tự sheet, y = tâm tiết diện (`CenterOffsetMm`), z = đỉnh dầm (`TopFt`); chiều ngược đảo X và Y. Host = dầm chứa trạm.
- `KataRebarCurveFactory`: `// Multi-version: rebar terminations` — R26+ `BarTerminationsData`, R23–R25 overload `RebarHookOrientation`.
- `KataStirrupSetCreator`: 1 bộ / vùng (`CreateFromRebarShape` → `ScaleToBox` out-to-out → `SetLayoutAsNumberWithSpacing`), tự kiểm hướng rải, `SubTransaction` mỗi vùng, lỗi → đai lẻ từ curves (không trùng).
- `KataTransactionRunner`: warning log + xoá, error → rollback, `Commit` ≠ Committed → throw → rollback cả group. `KataRebarOrchestrator`: group "Kata Rebar - {tên}", 3 bước, `Assimilate` kiểm status.
- Tag: Comments `HPRebar_Kata:{hostUniqueId}`; cleanup chỉ xoá đúng tag của host.
- Type resolver: ±0.5 mm, không lấy đường kính gần nhất; thiếu → báo tên Ø. Shape resolver: chỉ nhận StirrupTie ≥ 4 đoạn.
- Handler: Pick / Measure / Generate (đo lại + plan lại trước khi vẽ). VM 192 dòng + `KataRebarPreviewBuilder`; XAML bỏ Browse, AutomationId, tab "Cảnh báo & Bỏ qua" với [Chặn]/[Cảnh báo]/[Bỏ qua].

## Không sửa (thay đổi treo phiên khác)
`HPRebar.csproj` (PackageReference ClosedXML còn, không còn code dùng), `Application.cs`, `install/Installer.cs`.
