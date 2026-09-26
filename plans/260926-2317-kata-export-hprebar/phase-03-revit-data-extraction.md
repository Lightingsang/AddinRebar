---
phase: 3
title: "Đọc dữ liệu Revit"
status: completed
priority: P1
effort: "1.5d"
dependencies: [2]
---

# Phase 3: Đọc dữ liệu Revit → `KataRunInput`

## Overview
Các service trong `HPRebar/HPRebar/KataExport/Service/` biến dầm được chọn + môi trường quanh nó thành `KataRunInput` (mm, trạm `s`). Không tham chiếu feature khác; chép pattern.

## Requirements
- Functional: dải dầm thẳng liên tục (Line, cùng phương ≤ 1°, lệch ngang ≤ 10 mm; cho phép khác cao độ/tiết diện); gối = cột, móng, dầm giao trong active view; cột tầng trên; lưới trong view; tham số header.
- Non-functional: mọi đơn vị qua `UnitUtils` (không phụ thuộc đơn vị project); lọc `BuiltInCategory`/`BuiltInParameter` (không so tên tiếng Anh); không dùng `Curve.Intersect`; không dùng API mới hơn R23; mỗi file < 300 dòng.

## Architecture
- `KataRunReader`: trục = phương dầm đầu, hướng sao cho thành phần trội dương (Normal của Dynamo); chiếu pieces như `BeamStackReader.cs:21-46`; b/h từ type param (mặc định `b`/`h`), thiếu → đo solid (`BeamSolidFaceReader.cs:170-184`); `Z_OFFSET_VALUE`, `INSTANCE_REFERENCE_LEVEL_PARAM`; chụp dictionary tên → giá trị hiển thị mọi tham số instance của dầm đầu dải (combo Name/Count không cần API).
- `KataSolidReader`: solid qua `GeometryInstance.GetInstanceGeometry()` + solid cấp 1, bỏ qua null (sửa R5).
- `KataSupportCollector`: `FilteredElementCollector(doc, view.Id)` + `BoundingBoxIntersectsFilter` quanh dải (mở rộng theo h) cho Column/Foundation/Framing (bỏ dầm đã chọn); footprint = `BooleanOperationsUtils` Intersect(solid gối, prism dầm) → chiếu lên trục; lỗi boolean → chiếu bbox (`BeamSupportFinder.cs:311-409`) + warning; dầm giao: góc 45–135°, **truyền tất cả** (kể cả dầm trùng cột — core tự gộp thành `CrossingBeamStationMm` cho hàng 21 tại gối), `SectionText` = `"{b}x{h}"`.
- **Vách làm gối** (người dùng chọn, theo Kata `ColumnWall` — [kata-pro-revit-dll-reference.md](reports/kata-pro-revit-dll-reference.md)): `OST_Walls` có `WALL_STRUCTURAL_SIGNIFICANT` = true giao dầm → `KataSupportKind.Column` (core không đổi); footprint như cột.
- Giới hạn ghi rõ: chỉ lưới thẳng (Kata có xử lý lưới cong — ngoài phạm vi); 1 dải dầm mỗi lần.
- `KataHeaderReader` (bổ sung sau Kata.xlsm; B7 = sàn **dày hơn** trong hai bên dầm, hai bên khác nhau → cảnh báo): **B7** chiều dày sàn chạm mặt trên dầm (Floor, `FLOOR_ATTR_THICKNESS_PARAM`); **B8** tên lưới song song dọc dầm (góc ≤ 1°, gần tim dầm nhất trong ±b); **B9** khoảng cách có dấu tim dầm → lưới đó (dương bên trái hướng chạy; quy ước dấu kiểm ở P6). Không tìm thấy → null (core ghi giá trị Dynamo + cảnh báo).
- `KataUpperColumnFinder`: cột có đáy ∈ [đáy dầm, đỉnh dầm + 500 mm] và footprint chồng khoảng gối → `Upper` (ý tưởng `ColumnNeighbourFinder.cs:14-28`) — thay đầu dò +450 mm (sửa R9).
- `KataGridReader`: `Grid` dạng Line trong view; giao 2D closed-form với trục → `S` (sửa R13).
- `RevitUnits`: bản sao per-feature theo `BeamRebar/Service/RevitUnits.cs`.

## Related Code Files
- Create: `HPRebar/HPRebar/KataExport/Service/{KataRunReader,KataSolidReader,KataSupportCollector,KataUpperColumnFinder,KataGridReader,RevitUnits}.cs`
- Create: `HPRebar/HPRebar/KataExport/Model/KataExportSession.cs`

## Implementation Steps
1. `RevitUnits`, `KataSolidReader`.
2. `KataRunReader` + validation (lỗi rõ: dầm cong, không thẳng hàng, lẫn category).
3. `KataSupportCollector` (cột, móng, dầm giao) + fallback bbox.
4. `KataUpperColumnFinder`, `KataGridReader`.
5. `KataExportSession` gom `KataRunInput` + dictionary tham số + ElementIds.
6. Build `-c Debug.R26`; compile `-c Debug.R24` (kiểm không dùng API mới).

## Success Criteria
- [ ] `dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` pass
- [ ] `dotnet build HPRebar/HPRebar/HPRebar.csproj -c Debug.R24 -p:DeployAddin=false` pass

## Tiến độ (2026-09-27, phiên addinrebar-47)

**Đã làm (khác thiết kế ban đầu, có lý do):**
- Đo gối bằng **đầu dò**: đường thẳng dọc tim dầm ở 4 cao độ (đỉnh −20, giữa, đáy +20, đáy −20 mm), vươn ±1500 mm qua đầu dầm, cắt với solid gối bằng `Solid.IntersectWithCurve`. Thay cho ý định `BooleanOperationsUtils` + fallback bbox. Cách này đúng như Dynamo đo (trục ∩ khối gối) và giữ nguyên bề rộng gối ở đầu dầm.
- Không có file `KataUpperColumnFinder` riêng: cột trên = cột cắt đầu dò ở (đỉnh dầm cao nhất + 500 mm), gom trong `KataSupportCollector`.
- Ứng viên lấy **toàn document** (không chỉ view) qua `KataCandidateCollector`, có lọc phase của view và bỏ design option phụ.
- B7 = sàn **dày nhất** có đỉnh trùng đỉnh dầm (±50 mm); các sàn khác độ dày → cảnh báo.
- Hàng 19 nhịp: bước đỉnh đo từ **hình học gốc** (`GetOriginalGeometry`) trừ cao độ level (lấy từ location line − Start Level Offset), thay cho chỉ đọc `z Offset Value`. Nhờ vậy tính đúng cả Start/End Level Offset và z justification.
- Files: `Service/{RevitUnits, KataAxisFrame, KataSolidReader, KataCandidateCollector, KataRunReader, KataSupportCollector, KataGridReader, KataHeaderReader, KataSessionReader}`, `Model/{KataBeamGeometry, KataExportSession}`.

**Review** [code-review-phase-03-revit-readers.md](reports/code-review-phase-03-revit-readers.md) 6.5/10 → đã sửa:
- **H1:** bắt `ApplicationException` quanh từng solid, đếm số solid bị bỏ và ghi cảnh báo.
- **H2:** dầm giao dùng hình học gốc (chưa bị cắt tại mặt cột).
- **H3:** bỏ móng băng / móng bè / móng dài > 3 m.
- **H4:** đỉnh và đáy dầm lấy từ hình học gốc.
- **M1:** cột/vách đứng trên dầm không tính là gối.
- **M2:** đầu dò cột trên đặt ở đỉnh dầm cao nhất + 500 mm.
- **M3:** lọc phase và design option.
- **M4:** chặn dầm dốc (> 10 mm) và dầm khác level.
- **M5:** đo bước đỉnh bằng hình học (xem trên).
- **M7:** B9 đổi dấu khi Reverse (core, có test).
- **M8:** cảnh báo khi view không có trục cắt dầm.
- **M9:** dựng đầu dò một lần; phân loại phần tử trước khi đọc hình học; sàn đọc bằng bbox.

**Giằng móng (2026-09-27):**
- Slab foundation dày > 100 mm là gối; bê tông lót ≤ 100 mm bị bỏ im lặng.
- Ngưỡng móng dài dọc trục: 3000 → 6000 mm.
- Cột đứng trên gối móng không bị cảnh báo. Cảnh báo "đứng trên dầm" chỉ tính phần tử nằm trên tim dầm và trong phạm vi dải.
- Test lõi `KataTieBeamTests` (3). Chi tiết: [kata-cell-contract.md](reports/kata-cell-contract.md) ▸ Giằng móng.
- Review [code-review-tie-beam-foundations.md](reports/code-review-tie-beam-foundations.md) 6.5/10 → đã sửa:
  - **H1:** móng đo bằng đầu dò riêng vươn 6000 mm → đài rộng hơn 3 m ở đầu dải được đo đủ; bè dưới dải ngắn vẫn dài > 6000 nên bị bỏ.
  - **H2:** cột/vách đứng trên **bất kỳ** gối cứng (cột, vách, móng) không bị cảnh báo — trường hợp cột theo từng tầng của dầm sàn.
  - **M1:** móng băng chỉ được đếm khi đầu dò thật sự chạm nó trong phạm vi dải.
  - **M2:** móng chỉ là gối khi đầu dò ngay dưới đáy dầm chạm vào, tránh sàn trệt dựng bằng foundation slab.
  - **M4:** cảnh báo khi một gối có nhiều cột trên (móng kép).
  - **M5:** các quy tắc chuyển sang `Core/KataSupportRules` + `KataSupportRulesTests`.
  - **L2:** đặt tên sai số 0.5 mm.
  - **L4:** regex số chỉ nhận chữ số ASCII; `01` giữ dạng text.
  - **L5:** sửa comment test.
  - **M3** (lọc bê tông lót cho móng family) KHÔNG làm: nằm ngoài phạm vi hợp đồng người dùng đã xác nhận.
  - Kết quả: 415/415 test.

**Còn lại:**
- M6 (`ProjectElevation` với Elevation Base khác) — kiểm ở P6 bằng model có project base point Z ≠ 0.
- Các mục Low L1–L12.

**Kiểm tra:**
- ✅ Build Debug.R26 pass.
- ✅ Debug.R24 compile: code P3 sạch. Lỗi duy nhất `KataExportExternalEventHandler.cs:60` thuộc phiên song song làm P5, đã báo qua SendMessage.
- ✅ Core 385/385.
- ❌ Chạy trong Revit: CHƯA TEST.

## Risk Assessment
Boolean Revit ném lỗi trên hình học phức tạp → fallback bbox + warning hiển thị ở preview. Dầm không có param `b`/`h` → đo solid. Kiểm chứng thật ở P6 (live).
