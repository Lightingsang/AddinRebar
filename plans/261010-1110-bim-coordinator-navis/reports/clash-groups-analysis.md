# Ma trận va chạm HP — phân tích Nhóm 1 / 2 / 3

Nguồn: sheet `RuleClash(HP)` của `HPBIM_MaTranKiemSoatVaCham.xlsx` (sha256 `1274e330763d…`), qua `Rules/hp-clash-matrix.json` mà engine dùng để tạo clash test.

## 1. Nhóm là gì

"Nhóm" là **mức ưu tiên** ghi trong từng ô của ma trận (số 1, 2 hoặc 3 tại giao của hai nhóm cấu kiện), không phải nhóm cấu kiện A/S/M. Ô trống = *tùy dự án hoặc không áp dụng* → không tạo test.

| Nhóm | Mức | Ý nghĩa | Thời hạn xử lý |
|---|---|---|---|
| Nhóm 1 | HIGH | Va chạm lớn ảnh hưởng phương án thiết kế và/hoặc tiến độ dự án. | Phải giải quyết **trước buổi họp phối hợp** (coordination meeting). |
| Nhóm 2 | MEDIUM | Va chạm ảnh hưởng thiết kế và/hoặc thi công nhưng mức độ thấp hơn Nhóm 1. | Phải xử lý **trước khi trình nộp bản vẽ** (drawing submittal). |
| Nhóm 3 | LOW | Các va chạm còn lại. | **Cập nhật định kỳ** theo thiết kế và xử lý ngoài hiện trường. |

Một cặp được kiểm ở một LOD khi **cả hai** nhóm cấu kiện ghi `Y` ở cột LOD đó. Tên test `HP|LOD<lod>|P<nhóm>|<bộ môn>|<trái>-<phải>`: sắp Clash Detective theo tên là đọc theo LOD rồi theo Nhóm (API Navisworks 2026 không tạo được folder test — mọi loại folder bị từ chối "unsupported type of SavedItem").

## 2. LOD và dung sai

| LOD | Giai đoạn | Dung sai | Số cặp | Nhóm 1 | Nhóm 2 | Nhóm 3 |
|---|---|---|---|---|---|---|
| 200 | Thiết kế cơ sở / Basic Design | 50 mm | 27 | 7 | 17 | 3 |
| 300 | Thiết kế kỹ thuật / Technical Design | 30 mm | 154 | 39 | 81 | 34 |
| 350 | Bản vẽ thi công / Construction Drawings | 10 mm | 184 | 40 | 99 | 45 |
| 400 | Bản vẽ thi công / Construction Drawings (cột G dùng chung 350/400) | 10 mm | 184 | 40 | 99 | 45 |

- Dung sai 200/300/350 lấy từ ghi chú của sheet (50/30/10 mm). Sheet **không** ghi LOD400; BIM lead chọn 10 mm (2026-10-11) → test LOD400 trùng cặp và dung sai với LOD350, kết quả sẽ lặp lại.
- LOD300 không thuộc phạm vi tạo test lần này (engine vẫn hỗ trợ).

## 3. Nhóm cấu kiện và LOD được kiểm

| Mã | Cấu kiện | Bộ môn | 200 | 300 | 350/400 | Số cặp tham gia |
|---|---|---|---|---|---|---|
| A1 | Nội thất / Furnitures | ARC | N | N | Y | 16 |
| A2 | Trần giả / Ceilings | ARC | N | Y | Y | 15 |
| A3 | Cột kiến trúc / Architectural Columns | ARC | Y | Y | Y | 19 |
| A4 | Vách kính / Curtain Walls | ARC | N | Y | Y | 20 |
| A5 | Cửa đi / Doors | ARC | Y | Y | Y | 19 |
| A6 | Sàn kiến trúc / Architectural Floors | ARC | N | Y | Y | 17 |
| A7 | Thang bộ & Lan kan / Stairs and Railings | ARC | N | Y | Y | 16 |
| A8 | Tường kiến trúc / Architectural Walls | ARC | N | Y | Y | 21 |
| A9 | Cửa sổ / Windows | ARC | Y | Y | Y | 19 |
| M1 | Miệng gió / Air terminals | MEP | Y | Y | Y | 15 |
| M2 | Thang/Máng cáp / Cable Ladders/Trays | MEP | N | Y | Y | 18 |
| M3 | Ống gió & Phụ kiện ống gió / Ducts and Duct Accessories | MEP | Y | Y | Y | 17 |
| M4 | Ống & Phụ kiện đường ống / Pipes/Pipe Accessories | MEP | N | Y | Y | 18 |
| M5 | Thiết bị phân phối điện & Phụ kiện / EDE & Accessories | MEP | N | Y | Y | 8 |
| M6 | Thiết bị Thông gió Điều hòa / HVAC Equipments | MEP | N | Y | Y | 19 |
| M7 | Thiết bị chiếu sáng & Phụ kiện / Lighting Fixtures & Accessories | MEP | N | N | Y | 15 |
| S1 | Móng / Foundations | STR | N | Y | Y | 11 |
| S2 | Tường/vách chịu lực / Structural Walls | STR | Y | Y | Y | 20 |
| S3 | Cột chịu lực / Structural Columns | STR | Y | Y | Y | 20 |
| S4 | Sàn chịu lực / Structural Floors | STR | Y | Y | Y | 15 |
| S5 | Hệ dầm chịu lực / Structural Framings | STR | N | Y | Y | 21 |

## 4.1 LOD200 @ 50 mm — 27 cặp

| Cặp bộ môn | Nhóm 1 | Nhóm 2 | Nhóm 3 | Tổng |
|---|---|---|---|---|
| ARC-ARC | 0 | 4 | 0 | 4 |
| ARC-MEP | 1 | 5 | 0 | 6 |
| ARC-STR | 2 | 3 | 3 | 8 |
| MEP-MEP | 1 | 0 | 0 | 1 |
| MEP-STR | 3 | 2 | 0 | 5 |
| STR-STR | 0 | 3 | 0 | 3 |

**Nhóm 1 – HIGH (7 cặp)**

- `A3-M3` A3 Cột kiến trúc (Architectural Columns) × M3 Ống gió & Phụ kiện ống gió (Ducts and Duct Accessories) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A5-S2` A5 Cửa đi (Doors) × S2 Tường/vách chịu lực (Structural Walls) — ARC-STR
- `A5-S3` A5 Cửa đi (Doors) × S3 Cột chịu lực (Structural Columns) — ARC-STR
- `M3-M3` M3 Ống gió & Phụ kiện ống gió (Ducts and Duct Accessories) × M3 Ống gió & Phụ kiện ống gió (Ducts and Duct Accessories) — MEP-MEP  _(skip trên THCSLT: set rỗng)_
- `M3-S2` M3 Ống gió & Phụ kiện ống gió (Ducts and Duct Accessories) × S2 Tường/vách chịu lực (Structural Walls) — MEP-STR  _(skip trên THCSLT: set rỗng)_
- `M3-S3` M3 Ống gió & Phụ kiện ống gió (Ducts and Duct Accessories) × S3 Cột chịu lực (Structural Columns) — MEP-STR  _(skip trên THCSLT: set rỗng)_
- `M3-S4` M3 Ống gió & Phụ kiện ống gió (Ducts and Duct Accessories) × S4 Sàn chịu lực (Structural Floors) — MEP-STR  _(skip trên THCSLT: set rỗng)_

**Nhóm 2 – MEDIUM (17 cặp)**

- `A3-A5` A3 Cột kiến trúc (Architectural Columns) × A5 Cửa đi (Doors) — ARC-ARC  _(skip trên THCSLT: set rỗng)_
- `A3-A9` A3 Cột kiến trúc (Architectural Columns) × A9 Cửa sổ (Windows) — ARC-ARC  _(skip trên THCSLT: set rỗng)_
- `A3-M1` A3 Cột kiến trúc (Architectural Columns) × M1 Miệng gió (Air terminals) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A5-A9` A5 Cửa đi (Doors) × A9 Cửa sổ (Windows) — ARC-ARC
- `A5-M1` A5 Cửa đi (Doors) × M1 Miệng gió (Air terminals) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A5-M3` A5 Cửa đi (Doors) × M3 Ống gió & Phụ kiện ống gió (Ducts and Duct Accessories) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A5-S4` A5 Cửa đi (Doors) × S4 Sàn chịu lực (Structural Floors) — ARC-STR
- `A9-A9` A9 Cửa sổ (Windows) × A9 Cửa sổ (Windows) — ARC-ARC
- `A9-M1` A9 Cửa sổ (Windows) × M1 Miệng gió (Air terminals) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A9-M3` A9 Cửa sổ (Windows) × M3 Ống gió & Phụ kiện ống gió (Ducts and Duct Accessories) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A9-S2` A9 Cửa sổ (Windows) × S2 Tường/vách chịu lực (Structural Walls) — ARC-STR
- `A9-S3` A9 Cửa sổ (Windows) × S3 Cột chịu lực (Structural Columns) — ARC-STR
- `M1-S2` M1 Miệng gió (Air terminals) × S2 Tường/vách chịu lực (Structural Walls) — MEP-STR  _(skip trên THCSLT: set rỗng)_
- `M1-S3` M1 Miệng gió (Air terminals) × S3 Cột chịu lực (Structural Columns) — MEP-STR  _(skip trên THCSLT: set rỗng)_
- `S2-S3` S2 Tường/vách chịu lực (Structural Walls) × S3 Cột chịu lực (Structural Columns) — STR-STR
- `S2-S4` S2 Tường/vách chịu lực (Structural Walls) × S4 Sàn chịu lực (Structural Floors) — STR-STR
- `S3-S4` S3 Cột chịu lực (Structural Columns) × S4 Sàn chịu lực (Structural Floors) — STR-STR

**Nhóm 3 – LOW (3 cặp)**

- `A3-S2` A3 Cột kiến trúc (Architectural Columns) × S2 Tường/vách chịu lực (Structural Walls) — ARC-STR  _(skip trên THCSLT: set rỗng)_
- `A3-S3` A3 Cột kiến trúc (Architectural Columns) × S3 Cột chịu lực (Structural Columns) — ARC-STR  _(skip trên THCSLT: set rỗng)_
- `A3-S4` A3 Cột kiến trúc (Architectural Columns) × S4 Sàn chịu lực (Structural Floors) — ARC-STR  _(skip trên THCSLT: set rỗng)_

## 4.2 LOD350 @ 10 mm — 184 cặp

| Cặp bộ môn | Nhóm 1 | Nhóm 2 | Nhóm 3 | Tổng |
|---|---|---|---|---|
| ARC-ARC | 1 | 24 | 12 | 37 |
| ARC-MEP | 9 | 31 | 14 | 54 |
| ARC-STR | 5 | 19 | 14 | 38 |
| MEP-MEP | 9 | 6 | 1 | 16 |
| MEP-STR | 16 | 9 | 2 | 27 |
| STR-STR | 0 | 10 | 2 | 12 |

**Nhóm 1 – HIGH (40 cặp)**

- `A1-A8` A1 Nội thất (Furnitures) × A8 Tường kiến trúc (Architectural Walls) — ARC-ARC  _(skip trên THCSLT: set rỗng)_
- `A2-M2` A2 Trần giả (Ceilings) × M2 Thang/Máng cáp (Cable Ladders/Trays) — ARC-MEP
- `A2-M3` A2 Trần giả (Ceilings) × M3 Ống gió & Phụ kiện ống gió (Ducts and Duct Accessories) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A2-M4` A2 Trần giả (Ceilings) × M4 Ống & Phụ kiện đường ống (Pipes/Pipe Accessories) — ARC-MEP
- `A2-M6` A2 Trần giả (Ceilings) × M6 Thiết bị Thông gió Điều hòa (HVAC Equipments) — ARC-MEP
- `A2-S5` A2 Trần giả (Ceilings) × S5 Hệ dầm chịu lực (Structural Framings) — ARC-STR
- `A3-M2` A3 Cột kiến trúc (Architectural Columns) × M2 Thang/Máng cáp (Cable Ladders/Trays) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A3-M3` A3 Cột kiến trúc (Architectural Columns) × M3 Ống gió & Phụ kiện ống gió (Ducts and Duct Accessories) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A3-M4` A3 Cột kiến trúc (Architectural Columns) × M4 Ống & Phụ kiện đường ống (Pipes/Pipe Accessories) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A3-M6` A3 Cột kiến trúc (Architectural Columns) × M6 Thiết bị Thông gió Điều hòa (HVAC Equipments) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A4-S5` A4 Vách kính (Curtain Walls) × S5 Hệ dầm chịu lực (Structural Framings) — ARC-STR
- `A5-S2` A5 Cửa đi (Doors) × S2 Tường/vách chịu lực (Structural Walls) — ARC-STR
- `A5-S3` A5 Cửa đi (Doors) × S3 Cột chịu lực (Structural Columns) — ARC-STR
- `A8-M6` A8 Tường kiến trúc (Architectural Walls) × M6 Thiết bị Thông gió Điều hòa (HVAC Equipments) — ARC-MEP
- `A9-S5` A9 Cửa sổ (Windows) × S5 Hệ dầm chịu lực (Structural Framings) — ARC-STR
- `M2-M2` M2 Thang/Máng cáp (Cable Ladders/Trays) × M2 Thang/Máng cáp (Cable Ladders/Trays) — MEP-MEP
- `M2-M3` M2 Thang/Máng cáp (Cable Ladders/Trays) × M3 Ống gió & Phụ kiện ống gió (Ducts and Duct Accessories) — MEP-MEP  _(skip trên THCSLT: set rỗng)_
- `M2-M4` M2 Thang/Máng cáp (Cable Ladders/Trays) × M4 Ống & Phụ kiện đường ống (Pipes/Pipe Accessories) — MEP-MEP
- `M2-M6` M2 Thang/Máng cáp (Cable Ladders/Trays) × M6 Thiết bị Thông gió Điều hòa (HVAC Equipments) — MEP-MEP
- `M2-S2` M2 Thang/Máng cáp (Cable Ladders/Trays) × S2 Tường/vách chịu lực (Structural Walls) — MEP-STR
- `M2-S3` M2 Thang/Máng cáp (Cable Ladders/Trays) × S3 Cột chịu lực (Structural Columns) — MEP-STR
- `M2-S5` M2 Thang/Máng cáp (Cable Ladders/Trays) × S5 Hệ dầm chịu lực (Structural Framings) — MEP-STR
- `M3-M3` M3 Ống gió & Phụ kiện ống gió (Ducts and Duct Accessories) × M3 Ống gió & Phụ kiện ống gió (Ducts and Duct Accessories) — MEP-MEP  _(skip trên THCSLT: set rỗng)_
- `M3-M4` M3 Ống gió & Phụ kiện ống gió (Ducts and Duct Accessories) × M4 Ống & Phụ kiện đường ống (Pipes/Pipe Accessories) — MEP-MEP  _(skip trên THCSLT: set rỗng)_
- `M3-M6` M3 Ống gió & Phụ kiện ống gió (Ducts and Duct Accessories) × M6 Thiết bị Thông gió Điều hòa (HVAC Equipments) — MEP-MEP  _(skip trên THCSLT: set rỗng)_
- `M3-S2` M3 Ống gió & Phụ kiện ống gió (Ducts and Duct Accessories) × S2 Tường/vách chịu lực (Structural Walls) — MEP-STR  _(skip trên THCSLT: set rỗng)_
- `M3-S3` M3 Ống gió & Phụ kiện ống gió (Ducts and Duct Accessories) × S3 Cột chịu lực (Structural Columns) — MEP-STR  _(skip trên THCSLT: set rỗng)_
- `M3-S4` M3 Ống gió & Phụ kiện ống gió (Ducts and Duct Accessories) × S4 Sàn chịu lực (Structural Floors) — MEP-STR  _(skip trên THCSLT: set rỗng)_
- `M3-S5` M3 Ống gió & Phụ kiện ống gió (Ducts and Duct Accessories) × S5 Hệ dầm chịu lực (Structural Framings) — MEP-STR  _(skip trên THCSLT: set rỗng)_
- `M4-M4` M4 Ống & Phụ kiện đường ống (Pipes/Pipe Accessories) × M4 Ống & Phụ kiện đường ống (Pipes/Pipe Accessories) — MEP-MEP
- `M4-M6` M4 Ống & Phụ kiện đường ống (Pipes/Pipe Accessories) × M6 Thiết bị Thông gió Điều hòa (HVAC Equipments) — MEP-MEP
- `M4-S2` M4 Ống & Phụ kiện đường ống (Pipes/Pipe Accessories) × S2 Tường/vách chịu lực (Structural Walls) — MEP-STR
- `M4-S3` M4 Ống & Phụ kiện đường ống (Pipes/Pipe Accessories) × S3 Cột chịu lực (Structural Columns) — MEP-STR
- `M4-S4` M4 Ống & Phụ kiện đường ống (Pipes/Pipe Accessories) × S4 Sàn chịu lực (Structural Floors) — MEP-STR
- `M4-S5` M4 Ống & Phụ kiện đường ống (Pipes/Pipe Accessories) × S5 Hệ dầm chịu lực (Structural Framings) — MEP-STR
- `M5-S2` M5 Thiết bị phân phối điện & Phụ kiện (EDE & Accessories) × S2 Tường/vách chịu lực (Structural Walls) — MEP-STR
- `M5-S3` M5 Thiết bị phân phối điện & Phụ kiện (EDE & Accessories) × S3 Cột chịu lực (Structural Columns) — MEP-STR
- `M6-S2` M6 Thiết bị Thông gió Điều hòa (HVAC Equipments) × S2 Tường/vách chịu lực (Structural Walls) — MEP-STR
- `M6-S3` M6 Thiết bị Thông gió Điều hòa (HVAC Equipments) × S3 Cột chịu lực (Structural Columns) — MEP-STR
- `M6-S5` M6 Thiết bị Thông gió Điều hòa (HVAC Equipments) × S5 Hệ dầm chịu lực (Structural Framings) — MEP-STR

**Nhóm 2 – MEDIUM (99 cặp)**

- `A1-A1` A1 Nội thất (Furnitures) × A1 Nội thất (Furnitures) — ARC-ARC  _(skip trên THCSLT: set rỗng)_
- `A1-A3` A1 Nội thất (Furnitures) × A3 Cột kiến trúc (Architectural Columns) — ARC-ARC  _(skip trên THCSLT: set rỗng)_
- `A1-A4` A1 Nội thất (Furnitures) × A4 Vách kính (Curtain Walls) — ARC-ARC  _(skip trên THCSLT: set rỗng)_
- `A1-A5` A1 Nội thất (Furnitures) × A5 Cửa đi (Doors) — ARC-ARC  _(skip trên THCSLT: set rỗng)_
- `A1-A9` A1 Nội thất (Furnitures) × A9 Cửa sổ (Windows) — ARC-ARC  _(skip trên THCSLT: set rỗng)_
- `A1-S2` A1 Nội thất (Furnitures) × S2 Tường/vách chịu lực (Structural Walls) — ARC-STR  _(skip trên THCSLT: set rỗng)_
- `A1-S3` A1 Nội thất (Furnitures) × S3 Cột chịu lực (Structural Columns) — ARC-STR  _(skip trên THCSLT: set rỗng)_
- `A1-S4` A1 Nội thất (Furnitures) × S4 Sàn chịu lực (Structural Floors) — ARC-STR  _(skip trên THCSLT: set rỗng)_
- `A1-S5` A1 Nội thất (Furnitures) × S5 Hệ dầm chịu lực (Structural Framings) — ARC-STR  _(skip trên THCSLT: set rỗng)_
- `A2-A4` A2 Trần giả (Ceilings) × A4 Vách kính (Curtain Walls) — ARC-ARC
- `A2-A5` A2 Trần giả (Ceilings) × A5 Cửa đi (Doors) — ARC-ARC
- `A2-A9` A2 Trần giả (Ceilings) × A9 Cửa sổ (Windows) — ARC-ARC
- `A2-M1` A2 Trần giả (Ceilings) × M1 Miệng gió (Air terminals) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A2-S2` A2 Trần giả (Ceilings) × S2 Tường/vách chịu lực (Structural Walls) — ARC-STR
- `A2-S3` A2 Trần giả (Ceilings) × S3 Cột chịu lực (Structural Columns) — ARC-STR
- `A3-A4` A3 Cột kiến trúc (Architectural Columns) × A4 Vách kính (Curtain Walls) — ARC-ARC  _(skip trên THCSLT: set rỗng)_
- `A3-A5` A3 Cột kiến trúc (Architectural Columns) × A5 Cửa đi (Doors) — ARC-ARC  _(skip trên THCSLT: set rỗng)_
- `A3-A7` A3 Cột kiến trúc (Architectural Columns) × A7 Thang bộ & Lan kan (Stairs and Railings) — ARC-ARC  _(skip trên THCSLT: set rỗng)_
- `A3-A9` A3 Cột kiến trúc (Architectural Columns) × A9 Cửa sổ (Windows) — ARC-ARC  _(skip trên THCSLT: set rỗng)_
- `A3-M1` A3 Cột kiến trúc (Architectural Columns) × M1 Miệng gió (Air terminals) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A3-S5` A3 Cột kiến trúc (Architectural Columns) × S5 Hệ dầm chịu lực (Structural Framings) — ARC-STR  _(skip trên THCSLT: set rỗng)_
- `A4-A4` A4 Vách kính (Curtain Walls) × A4 Vách kính (Curtain Walls) — ARC-ARC
- `A4-A5` A4 Vách kính (Curtain Walls) × A5 Cửa đi (Doors) — ARC-ARC
- `A4-A6` A4 Vách kính (Curtain Walls) × A6 Sàn kiến trúc (Architectural Floors) — ARC-ARC
- `A4-A7` A4 Vách kính (Curtain Walls) × A7 Thang bộ & Lan kan (Stairs and Railings) — ARC-ARC
- `A4-A8` A4 Vách kính (Curtain Walls) × A8 Tường kiến trúc (Architectural Walls) — ARC-ARC
- `A4-A9` A4 Vách kính (Curtain Walls) × A9 Cửa sổ (Windows) — ARC-ARC
- `A4-M1` A4 Vách kính (Curtain Walls) × M1 Miệng gió (Air terminals) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A4-M2` A4 Vách kính (Curtain Walls) × M2 Thang/Máng cáp (Cable Ladders/Trays) — ARC-MEP
- `A4-M3` A4 Vách kính (Curtain Walls) × M3 Ống gió & Phụ kiện ống gió (Ducts and Duct Accessories) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A4-M4` A4 Vách kính (Curtain Walls) × M4 Ống & Phụ kiện đường ống (Pipes/Pipe Accessories) — ARC-MEP
- `A4-M5` A4 Vách kính (Curtain Walls) × M5 Thiết bị phân phối điện & Phụ kiện (EDE & Accessories) — ARC-MEP
- `A4-M6` A4 Vách kính (Curtain Walls) × M6 Thiết bị Thông gió Điều hòa (HVAC Equipments) — ARC-MEP
- `A4-M7` A4 Vách kính (Curtain Walls) × M7 Thiết bị chiếu sáng & Phụ kiện (Lighting Fixtures & Accessories) — ARC-MEP
- `A4-S2` A4 Vách kính (Curtain Walls) × S2 Tường/vách chịu lực (Structural Walls) — ARC-STR
- `A4-S3` A4 Vách kính (Curtain Walls) × S3 Cột chịu lực (Structural Columns) — ARC-STR
- `A4-S4` A4 Vách kính (Curtain Walls) × S4 Sàn chịu lực (Structural Floors) — ARC-STR
- `A5-A8` A5 Cửa đi (Doors) × A8 Tường kiến trúc (Architectural Walls) — ARC-ARC
- `A5-A9` A5 Cửa đi (Doors) × A9 Cửa sổ (Windows) — ARC-ARC
- `A5-M1` A5 Cửa đi (Doors) × M1 Miệng gió (Air terminals) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A5-M2` A5 Cửa đi (Doors) × M2 Thang/Máng cáp (Cable Ladders/Trays) — ARC-MEP
- `A5-M3` A5 Cửa đi (Doors) × M3 Ống gió & Phụ kiện ống gió (Ducts and Duct Accessories) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A5-M4` A5 Cửa đi (Doors) × M4 Ống & Phụ kiện đường ống (Pipes/Pipe Accessories) — ARC-MEP
- `A5-M5` A5 Cửa đi (Doors) × M5 Thiết bị phân phối điện & Phụ kiện (EDE & Accessories) — ARC-MEP
- `A5-M6` A5 Cửa đi (Doors) × M6 Thiết bị Thông gió Điều hòa (HVAC Equipments) — ARC-MEP
- `A5-M7` A5 Cửa đi (Doors) × M7 Thiết bị chiếu sáng & Phụ kiện (Lighting Fixtures & Accessories) — ARC-MEP
- `A5-S4` A5 Cửa đi (Doors) × S4 Sàn chịu lực (Structural Floors) — ARC-STR
- `A5-S5` A5 Cửa đi (Doors) × S5 Hệ dầm chịu lực (Structural Framings) — ARC-STR
- `A6-M1` A6 Sàn kiến trúc (Architectural Floors) × M1 Miệng gió (Air terminals) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A6-M4` A6 Sàn kiến trúc (Architectural Floors) × M4 Ống & Phụ kiện đường ống (Pipes/Pipe Accessories) — ARC-MEP
- `A6-M6` A6 Sàn kiến trúc (Architectural Floors) × M6 Thiết bị Thông gió Điều hòa (HVAC Equipments) — ARC-MEP
- `A7-A9` A7 Thang bộ & Lan kan (Stairs and Railings) × A9 Cửa sổ (Windows) — ARC-ARC
- `A7-M1` A7 Thang bộ & Lan kan (Stairs and Railings) × M1 Miệng gió (Air terminals) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A7-M2` A7 Thang bộ & Lan kan (Stairs and Railings) × M2 Thang/Máng cáp (Cable Ladders/Trays) — ARC-MEP
- `A7-M3` A7 Thang bộ & Lan kan (Stairs and Railings) × M3 Ống gió & Phụ kiện ống gió (Ducts and Duct Accessories) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A7-M4` A7 Thang bộ & Lan kan (Stairs and Railings) × M4 Ống & Phụ kiện đường ống (Pipes/Pipe Accessories) — ARC-MEP
- `A7-S1` A7 Thang bộ & Lan kan (Stairs and Railings) × S1 Móng (Foundations) — ARC-STR
- `A7-S2` A7 Thang bộ & Lan kan (Stairs and Railings) × S2 Tường/vách chịu lực (Structural Walls) — ARC-STR
- `A7-S3` A7 Thang bộ & Lan kan (Stairs and Railings) × S3 Cột chịu lực (Structural Columns) — ARC-STR
- `A7-S4` A7 Thang bộ & Lan kan (Stairs and Railings) × S4 Sàn chịu lực (Structural Floors) — ARC-STR
- `A7-S5` A7 Thang bộ & Lan kan (Stairs and Railings) × S5 Hệ dầm chịu lực (Structural Framings) — ARC-STR
- `A8-A8` A8 Tường kiến trúc (Architectural Walls) × A8 Tường kiến trúc (Architectural Walls) — ARC-ARC
- `A8-A9` A8 Tường kiến trúc (Architectural Walls) × A9 Cửa sổ (Windows) — ARC-ARC
- `A8-M3` A8 Tường kiến trúc (Architectural Walls) × M3 Ống gió & Phụ kiện ống gió (Ducts and Duct Accessories) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A9-A9` A9 Cửa sổ (Windows) × A9 Cửa sổ (Windows) — ARC-ARC
- `A9-M1` A9 Cửa sổ (Windows) × M1 Miệng gió (Air terminals) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A9-M2` A9 Cửa sổ (Windows) × M2 Thang/Máng cáp (Cable Ladders/Trays) — ARC-MEP
- `A9-M3` A9 Cửa sổ (Windows) × M3 Ống gió & Phụ kiện ống gió (Ducts and Duct Accessories) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A9-M4` A9 Cửa sổ (Windows) × M4 Ống & Phụ kiện đường ống (Pipes/Pipe Accessories) — ARC-MEP
- `A9-M5` A9 Cửa sổ (Windows) × M5 Thiết bị phân phối điện & Phụ kiện (EDE & Accessories) — ARC-MEP
- `A9-M6` A9 Cửa sổ (Windows) × M6 Thiết bị Thông gió Điều hòa (HVAC Equipments) — ARC-MEP
- `A9-M7` A9 Cửa sổ (Windows) × M7 Thiết bị chiếu sáng & Phụ kiện (Lighting Fixtures & Accessories) — ARC-MEP
- `A9-S2` A9 Cửa sổ (Windows) × S2 Tường/vách chịu lực (Structural Walls) — ARC-STR
- `A9-S3` A9 Cửa sổ (Windows) × S3 Cột chịu lực (Structural Columns) — ARC-STR
- `M1-M2` M1 Miệng gió (Air terminals) × M2 Thang/Máng cáp (Cable Ladders/Trays) — MEP-MEP  _(skip trên THCSLT: set rỗng)_
- `M1-M6` M1 Miệng gió (Air terminals) × M6 Thiết bị Thông gió Điều hòa (HVAC Equipments) — MEP-MEP  _(skip trên THCSLT: set rỗng)_
- `M1-M7` M1 Miệng gió (Air terminals) × M7 Thiết bị chiếu sáng & Phụ kiện (Lighting Fixtures & Accessories) — MEP-MEP  _(skip trên THCSLT: set rỗng)_
- `M1-S2` M1 Miệng gió (Air terminals) × S2 Tường/vách chịu lực (Structural Walls) — MEP-STR  _(skip trên THCSLT: set rỗng)_
- `M1-S3` M1 Miệng gió (Air terminals) × S3 Cột chịu lực (Structural Columns) — MEP-STR  _(skip trên THCSLT: set rỗng)_
- `M2-M7` M2 Thang/Máng cáp (Cable Ladders/Trays) × M7 Thiết bị chiếu sáng & Phụ kiện (Lighting Fixtures & Accessories) — MEP-MEP
- `M2-S4` M2 Thang/Máng cáp (Cable Ladders/Trays) × S4 Sàn chịu lực (Structural Floors) — MEP-STR
- `M3-M7` M3 Ống gió & Phụ kiện ống gió (Ducts and Duct Accessories) × M7 Thiết bị chiếu sáng & Phụ kiện (Lighting Fixtures & Accessories) — MEP-MEP  _(skip trên THCSLT: set rỗng)_
- `M4-S1` M4 Ống & Phụ kiện đường ống (Pipes/Pipe Accessories) × S1 Móng (Foundations) — MEP-STR
- `M5-S5` M5 Thiết bị phân phối điện & Phụ kiện (EDE & Accessories) × S5 Hệ dầm chịu lực (Structural Framings) — MEP-STR
- `M6-M7` M6 Thiết bị Thông gió Điều hòa (HVAC Equipments) × M7 Thiết bị chiếu sáng & Phụ kiện (Lighting Fixtures & Accessories) — MEP-MEP
- `M6-S1` M6 Thiết bị Thông gió Điều hòa (HVAC Equipments) × S1 Móng (Foundations) — MEP-STR
- `M6-S4` M6 Thiết bị Thông gió Điều hòa (HVAC Equipments) × S4 Sàn chịu lực (Structural Floors) — MEP-STR
- `M7-S3` M7 Thiết bị chiếu sáng & Phụ kiện (Lighting Fixtures & Accessories) × S3 Cột chịu lực (Structural Columns) — MEP-STR
- `M7-S5` M7 Thiết bị chiếu sáng & Phụ kiện (Lighting Fixtures & Accessories) × S5 Hệ dầm chịu lực (Structural Framings) — MEP-STR
- `S1-S2` S1 Móng (Foundations) × S2 Tường/vách chịu lực (Structural Walls) — STR-STR
- `S1-S3` S1 Móng (Foundations) × S3 Cột chịu lực (Structural Columns) — STR-STR
- `S1-S4` S1 Móng (Foundations) × S4 Sàn chịu lực (Structural Floors) — STR-STR
- `S1-S5` S1 Móng (Foundations) × S5 Hệ dầm chịu lực (Structural Framings) — STR-STR
- `S2-S3` S2 Tường/vách chịu lực (Structural Walls) × S3 Cột chịu lực (Structural Columns) — STR-STR
- `S2-S4` S2 Tường/vách chịu lực (Structural Walls) × S4 Sàn chịu lực (Structural Floors) — STR-STR
- `S2-S5` S2 Tường/vách chịu lực (Structural Walls) × S5 Hệ dầm chịu lực (Structural Framings) — STR-STR
- `S3-S4` S3 Cột chịu lực (Structural Columns) × S4 Sàn chịu lực (Structural Floors) — STR-STR
- `S3-S5` S3 Cột chịu lực (Structural Columns) × S5 Hệ dầm chịu lực (Structural Framings) — STR-STR
- `S4-S5` S4 Sàn chịu lực (Structural Floors) × S5 Hệ dầm chịu lực (Structural Framings) — STR-STR

**Nhóm 3 – LOW (45 cặp)**

- `A1-A2` A1 Nội thất (Furnitures) × A2 Trần giả (Ceilings) — ARC-ARC  _(skip trên THCSLT: set rỗng)_
- `A1-A6` A1 Nội thất (Furnitures) × A6 Sàn kiến trúc (Architectural Floors) — ARC-ARC  _(skip trên THCSLT: set rỗng)_
- `A1-M1` A1 Nội thất (Furnitures) × M1 Miệng gió (Air terminals) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A1-M5` A1 Nội thất (Furnitures) × M5 Thiết bị phân phối điện & Phụ kiện (EDE & Accessories) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A1-M6` A1 Nội thất (Furnitures) × M6 Thiết bị Thông gió Điều hòa (HVAC Equipments) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A1-M7` A1 Nội thất (Furnitures) × M7 Thiết bị chiếu sáng & Phụ kiện (Lighting Fixtures & Accessories) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A2-A3` A2 Trần giả (Ceilings) × A3 Cột kiến trúc (Architectural Columns) — ARC-ARC  _(skip trên THCSLT: set rỗng)_
- `A2-A8` A2 Trần giả (Ceilings) × A8 Tường kiến trúc (Architectural Walls) — ARC-ARC
- `A2-M7` A2 Trần giả (Ceilings) × M7 Thiết bị chiếu sáng & Phụ kiện (Lighting Fixtures & Accessories) — ARC-MEP
- `A3-A6` A3 Cột kiến trúc (Architectural Columns) × A6 Sàn kiến trúc (Architectural Floors) — ARC-ARC  _(skip trên THCSLT: set rỗng)_
- `A3-A8` A3 Cột kiến trúc (Architectural Columns) × A8 Tường kiến trúc (Architectural Walls) — ARC-ARC  _(skip trên THCSLT: set rỗng)_
- `A3-M7` A3 Cột kiến trúc (Architectural Columns) × M7 Thiết bị chiếu sáng & Phụ kiện (Lighting Fixtures & Accessories) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A3-S1` A3 Cột kiến trúc (Architectural Columns) × S1 Móng (Foundations) — ARC-STR  _(skip trên THCSLT: set rỗng)_
- `A3-S2` A3 Cột kiến trúc (Architectural Columns) × S2 Tường/vách chịu lực (Structural Walls) — ARC-STR  _(skip trên THCSLT: set rỗng)_
- `A3-S3` A3 Cột kiến trúc (Architectural Columns) × S3 Cột chịu lực (Structural Columns) — ARC-STR  _(skip trên THCSLT: set rỗng)_
- `A3-S4` A3 Cột kiến trúc (Architectural Columns) × S4 Sàn chịu lực (Structural Floors) — ARC-STR  _(skip trên THCSLT: set rỗng)_
- `A5-A6` A5 Cửa đi (Doors) × A6 Sàn kiến trúc (Architectural Floors) — ARC-ARC
- `A5-A7` A5 Cửa đi (Doors) × A7 Thang bộ & Lan kan (Stairs and Railings) — ARC-ARC
- `A6-A7` A6 Sàn kiến trúc (Architectural Floors) × A7 Thang bộ & Lan kan (Stairs and Railings) — ARC-ARC
- `A6-A8` A6 Sàn kiến trúc (Architectural Floors) × A8 Tường kiến trúc (Architectural Walls) — ARC-ARC
- `A6-A9` A6 Sàn kiến trúc (Architectural Floors) × A9 Cửa sổ (Windows) — ARC-ARC
- `A6-M2` A6 Sàn kiến trúc (Architectural Floors) × M2 Thang/Máng cáp (Cable Ladders/Trays) — ARC-MEP
- `A6-M3` A6 Sàn kiến trúc (Architectural Floors) × M3 Ống gió & Phụ kiện ống gió (Ducts and Duct Accessories) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A6-S1` A6 Sàn kiến trúc (Architectural Floors) × S1 Móng (Foundations) — ARC-STR
- `A6-S2` A6 Sàn kiến trúc (Architectural Floors) × S2 Tường/vách chịu lực (Structural Walls) — ARC-STR
- `A6-S3` A6 Sàn kiến trúc (Architectural Floors) × S3 Cột chịu lực (Structural Columns) — ARC-STR
- `A6-S4` A6 Sàn kiến trúc (Architectural Floors) × S4 Sàn chịu lực (Structural Floors) — ARC-STR
- `A6-S5` A6 Sàn kiến trúc (Architectural Floors) × S5 Hệ dầm chịu lực (Structural Framings) — ARC-STR
- `A7-A8` A7 Thang bộ & Lan kan (Stairs and Railings) × A8 Tường kiến trúc (Architectural Walls) — ARC-ARC
- `A7-M6` A7 Thang bộ & Lan kan (Stairs and Railings) × M6 Thiết bị Thông gió Điều hòa (HVAC Equipments) — ARC-MEP
- `A8-M1` A8 Tường kiến trúc (Architectural Walls) × M1 Miệng gió (Air terminals) — ARC-MEP  _(skip trên THCSLT: set rỗng)_
- `A8-M2` A8 Tường kiến trúc (Architectural Walls) × M2 Thang/Máng cáp (Cable Ladders/Trays) — ARC-MEP
- `A8-M4` A8 Tường kiến trúc (Architectural Walls) × M4 Ống & Phụ kiện đường ống (Pipes/Pipe Accessories) — ARC-MEP
- `A8-M5` A8 Tường kiến trúc (Architectural Walls) × M5 Thiết bị phân phối điện & Phụ kiện (EDE & Accessories) — ARC-MEP
- `A8-M7` A8 Tường kiến trúc (Architectural Walls) × M7 Thiết bị chiếu sáng & Phụ kiện (Lighting Fixtures & Accessories) — ARC-MEP
- `A8-S1` A8 Tường kiến trúc (Architectural Walls) × S1 Móng (Foundations) — ARC-STR
- `A8-S2` A8 Tường kiến trúc (Architectural Walls) × S2 Tường/vách chịu lực (Structural Walls) — ARC-STR
- `A8-S3` A8 Tường kiến trúc (Architectural Walls) × S3 Cột chịu lực (Structural Columns) — ARC-STR
- `A8-S4` A8 Tường kiến trúc (Architectural Walls) × S4 Sàn chịu lực (Structural Floors) — ARC-STR
- `A8-S5` A8 Tường kiến trúc (Architectural Walls) × S5 Hệ dầm chịu lực (Structural Framings) — ARC-STR
- `M1-S5` M1 Miệng gió (Air terminals) × S5 Hệ dầm chịu lực (Structural Framings) — MEP-STR  _(skip trên THCSLT: set rỗng)_
- `M4-M7` M4 Ống & Phụ kiện đường ống (Pipes/Pipe Accessories) × M7 Thiết bị chiếu sáng & Phụ kiện (Lighting Fixtures & Accessories) — MEP-MEP
- `M7-S2` M7 Thiết bị chiếu sáng & Phụ kiện (Lighting Fixtures & Accessories) × S2 Tường/vách chịu lực (Structural Walls) — MEP-STR
- `S1-S1` S1 Móng (Foundations) × S1 Móng (Foundations) — STR-STR
- `S5-S5` S5 Hệ dầm chịu lực (Structural Framings) × S5 Hệ dầm chịu lực (Structural Framings) — STR-STR

## 4.3 LOD400 @ 10 mm — 184 cặp

| Cặp bộ môn | Nhóm 1 | Nhóm 2 | Nhóm 3 | Tổng |
|---|---|---|---|---|
| ARC-ARC | 1 | 24 | 12 | 37 |
| ARC-MEP | 9 | 31 | 14 | 54 |
| ARC-STR | 5 | 19 | 14 | 38 |
| MEP-MEP | 9 | 6 | 1 | 16 |
| MEP-STR | 16 | 9 | 2 | 27 |
| STR-STR | 0 | 10 | 2 | 12 |

Danh sách cặp giống hệt LOD350 (mục 4.2).

## 5. Áp lên THCSLT

Set rỗng trên model: A1, A3, M1, M3 → cặp có một trong các nhóm này bị **skip** (không tạo test, không mở rộng sang cả model).

| LOD | Cặp | Tạo | Skip |
|---|---|---|---|
| 200 | 27 | 10 | 17 |
| 350 | 184 | 121 | 63 |
| 400 | 184 | 121 | 63 |

## 6. Nhận xét về sheet

- 13/21 nhóm ghi `N` ở LOD200 (A1, A2, A4, A6, A7, A8, M2, M4, M5, M6, M7, S1, S5) → LOD200 chỉ còn 27 cặp, chỉ giữa các nhóm A3 Cột kiến trúc (Architectural Columns), A5 Cửa đi (Doors), A9 Cửa sổ (Windows), M1 Miệng gió (Air terminals), M3 Ống gió & Phụ kiện ống gió (Ducts and Duct Accessories), S2 Tường/vách chịu lực (Structural Walls), S3 Cột chịu lực (Structural Columns), S4 Sàn chịu lực (Structural Floors).
- Nhóm chỉ kiểm từ LOD350: A1, M7 (nội thất, chiếu sáng).
- 9 cặp tự va chạm cùng nhóm (A1-A1, A4-A4, A8-A8, A9-A9, M2-M2, M3-M3, M4-M4, S1-S1, S5-S5).
- Nhóm tham gia nhiều cặp nhất: A8 (21), S5 (21), A4 (20), S2 (20); ít nhất: M5 (8), S1 (11), A2 (15).
- Ghi chú phương pháp 3 của sheet: không kiểm ống gió mềm, ống mềm và ống D < 32 mm. Test dùng set nền M4 (đã loại ống mềm theo category) nhưng **vẫn gồm ống D < 32 mm**; set `M4.9 Clash scope - pipes D >= 32 mm` đã có — đổi selection các cặp M4 sang M4.9 là quyết định riêng, chưa làm.
- Ghi chú 1 (lỗ mở sàn KT/KC qua góc nhìn đỉnh/đáy) và 2 (kiểm mọi phần tử trong phòng máy) không phải cặp ma trận → không sinh test.
- LOD400 không có dung sai trong sheet; 10 mm là lựa chọn của BIM lead, nên ghi bổ sung vào sheet để nguồn và engine khớp.
