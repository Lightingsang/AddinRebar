# P6 live verify — KataExport trong Revit 2026.4 (lần 1, 2026-09-27)

## Môi trường
- Revit 2026.4. Model thật `THCPHCS2-HPC-LH_BM_HC-ZZ-M3-ES-0001_C01_S1.rvt`, view "MẶT BẰNG DẦM SÀN TẦNG 1", được người dùng đồng ý test. Chỉ đọc + đổi selection, không sửa, không lưu.
- Excel: **bản sao** `kata-p6-copy.xlsm` của `C:\kata_pro\Kata.xlsm`, nằm trong scratchpad. File gốc không đụng.
- Add-in: `HPRebar.dll` Debug.R26 deploy 27/09 00:50.
- Người dùng chọn dải dầm, bấm **HPRebar ▸ Rebar ▸ Kata Export** → **Xuất Excel**.
- Claude đọc lại Excel bằng COM (read-only, `read-dam.ps1`) và đo độc lập qua `hprebar-revit` `execute_revit_code` (transaction `none`, runId 55–56, `changed` = 0).
- Kata Pro **chưa cài cho Revit 2026** (không có `Revit_Kata_Call.addin` ở `Addins\2026`) → chưa có golden từ lệnh Beam của Kata.

## Dải dầm test
5 × `B_200x350` (T1-DX12), trục D.1a, level "Tầng 1B" (−50), s = 0 → 21950 mm. 6 cột (C_350x500 / C_300x450) chạy liên tục qua tầng.

## Kết quả đối chiếu

| Ô / hàng | KataExport ghi | Đo độc lập (MCP) | KQ |
|---|---|---|---|
| B3 / B4 | `T1-DX12` / `1` | `STR_ElementName` / `STR_ElementCount` | ✅ |
| B5 / B6 | 350 / 200 | `B_200x350` | ✅ |
| B7 | 100 | sàn `San_HanhLang_D100` đỉnh −50, dày 100 | ✅ |
| B8 / B9 | `D.1a` / 0 | lưới D.1a song song, t = 0 | ✅ |
| B10 | số `-0.05` | cần text `-0.050` | ❌ → đã sửa (xem dưới) |
| Hàng 11 | 350·4650·300·2900·300·4100·500·3800·300·4500·500 | cột −150..200, 4850..5150, 8050..8350, 12450..12950, 16750..17050, 21550..22050 | ✅ khớp từng mm |
| Hàng 19 | gối `350;0`·`300;0`·`300;0`·`500;0`·`300;0`·`500;0`; nhịp 0 | cột liên tục lên tầng trên cùng tiết diện; đỉnh dầm = level | ✅ |
| Hàng 21 | −75·−50·−50·−50·+50·+150; nhịp 0 | = lệch lưới (dầm giao nằm trên trục) | ✅ |
| Hàng 22 | 3.1a·5.1a·6.1a·8.1a·10.1a·12.1a | lưới s = −50/4950/8150/12650/16950/21950 | ✅ |
| Hàng 23 | −75·−50·−50·−50·+50·+150 | trục − tâm cột (25/5000/8200/12700/16900/21800) | ✅ |

**Vùng xoá:** cột N trở đi của hàng 11–23 đã sạch (mẫu cũ có `O11=0`). Hàng 12–18 và 20 trong phạm vi 11 cột vẫn giữ dữ liệu cũ của mẫu — giống Dynamo, theo hợp đồng P1.

**Sau khi ghi:** 5 dầm được chọn lại trong Revit ✅.

Lưới không nằm trong gối nào (4.1a, 7.1a, 9.1a, 9*.1a, 11.1a) đều bị bỏ qua đúng. Các mối nối dầm (4950/8150/12650/16950) nằm trong cột nên không sinh gối 0 — đúng.

## Lỗi tìm thấy và đã sửa
- **B10 ghi thành số:** `KataText` `-0.050` khớp regex "số thuần" nên bị đổi thành số.
- Sửa: tách quy tắc sang `HPRebar.Core/KataExport/Calculators/KataExcelCell.cs`, trong đó `KataText` luôn đi dạng text. Có 14 test mới (`KataExcelCellTests`).
- Tổng test 399/399; build R26 + R24 pass.
- **Chưa deploy** vì Revit đang mở → cần test lại B10.

## Chưa làm
- Test lại B10 sau khi deploy lại.
- Các đường lỗi: Excel tắt, workbook không có sheet `Dam`, đổi workbook giữa chừng, Esc khi chọn dầm, dầm cong/dốc, view không có lưới.
- Các ca hình học khác: console, gối là dầm giao, gối là vách, cột trên lệch tâm, Reverse.
- Golden từ Kata: cần Kata Pro trên Revit 2026, hoặc người dùng chạy lệnh Beam của Kata trên Revit 2024 với cùng model.
- Chạy macro vẽ của Kata trên dữ liệu vừa ghi.

---

# P6 live verify — lần 2: giằng móng + B10 (2026-09-27 01:45)

## Môi trường
- Add-in deploy lúc 01:44: gồm sửa B10 (`KataExcelCell`) và quy tắc giằng móng (slab foundation > 100 mm là gối, lót ≤ 100 mm bị bỏ, ngưỡng dài 6000, cột trên móng).
- Model `Template_Structural_HoangPhuc_RV2026_V3.rvt`, view MẶT BẰNG MÓNG. Người dùng chọn 8 giằng **GMX3** `B_300x500` tại y = 24900 (id 9491230…9491301), rồi Kata Export → Xuất Excel vào bản sao Kata.
- Số dự kiến được tính **trước khi chạy**, từ khảo sát MCP (runId 57, read-only): đài `DaiCoc_D800` dày 800, cao độ −2800..−2000; lót `BeTongLot_D100` có tham số dày 60; giằng có đỉnh −2000; cột đáy −2000.

## Kết quả

| Ô / hàng | Dự kiến (từ khảo sát) | KataExport ghi | KQ |
|---|---|---|---|
| Hàng 11 | 1700·2650·1700·2550·1700·2650·1700·2550·1700·2550·1700·3000·1700·2550·1700·1720·1830 | giống hệt, 17 cột | ✅ |
| Hàng 19 | 8 đài có cột ở tâm → `350;0`; đài cuối có cột C_250x450 lệch +790 → `250;790`; nhịp 0 | giống hệt | ✅ |
| Hàng 22 | trục qua tâm đài | 14A·16A·18A·20A·23A·25A·27A·29A·31A | ✅ |
| Hàng 23 / 21 (gối) | tâm đài vs trục: 0, −50, 0, −50, 0, +50, −50, 0, +815 | giống hệt; hàng 21 = hàng 23 vì giằng GMY nằm đúng trên trục | ✅ |
| Bê tông lót | không phải gối | không xuất hiện gối nào ≤ 100 mm | ✅ |
| B3–B9 | GMX3, 2, 500, 300, 0 (không có sàn), XA, 0 | giống hệt | ✅ |
| B10 | text `-2.000` | `'-2.000`, NumberFormat `@`, kiểu String | ✅ lỗi lần 1 đã sửa |

## Còn lại
- Hồi quy dải dầm sàn T1-DX12 (THCPHCS2) với bản mới: chưa chạy lại. Code dầm sàn chỉ đổi phần cảnh báo "đứng trên dầm" và B10.
- Đường lỗi (Excel tắt / thiếu sheet `Dam` / đổi workbook / Esc) — CHƯA TEST.
- Ca gối là vách, giằng có cổ cột (giằng không chạm móng), Reverse — CHƯA TEST.
- Golden từ Kata Tools ▸ Beam: Kata Pro chưa cài cho Revit 2026.
- Chạy macro vẽ Kata trên dữ liệu vừa ghi: 👤.

## Lần 3 — 2026-09-27 02:5x, bản có sửa review giằng móng + view mặt đứng (deploy 02:48)
- Model Template V3, view MẶT BẰNG MÓNG; Claude chọn 8 giằng GMX3 y = 24900 qua MCP (`transaction: none`, chỉ đổi selection), bấm HPRebar ▸ Kata Export bằng UIA, Xuất Excel bằng UIA vào bản sao `kata-p6-copy.xlsm`.
- Sheet Dam đọc lại (`read-dam.ps1`) **giống hệt** lần 2 (`diff` rỗng): hàng 11 `1700·2650·…·1720·1830`, hàng 19 `350;0` ×8 + `250;790`, hàng 22 14A…31A, hàng 23 0/−50/…/815 → sửa review giằng móng (H1/H2/M1/M2/M4) không đổi kết quả dải này. ✅
- Cửa sổ mới (view mặt đứng) chạy trong Revit: xem [phase-07](../phase-07-elevation-view.md). 
- Còn lại: T1-DX12 (THCPHCS2) hồi quy dầm sàn trên bản mới — CHƯA TEST.

## Lần 4 — 2026-09-27 03:0x, THCPHCS2 T1-DX12, bản deploy 02:59 (sửa bubble)
- Claude chọn 5 dầm như lần 1 (id 9801057…9801069, s = 0→21950; dầm 9801051 ở đầu x = 0…4150 không thuộc lần 1), mở Kata Export + Xuất Excel bằng UIA vào bản sao.
- Hàng 11/19/21/22/23 + B3–B9: **giống hệt lần 1** (`diff` với `dam-after.txt`). Khác duy nhất có chủ đích: B10 `'-0.050` (String, NumberFormat `@`) thay số `-0.05` — sửa B10 đã xác minh. ✅
- Không còn cảnh báo "cột đứng trên dầm" (khung cảnh báo ẩn) → sửa H2 xác minh trên dầm sàn. ✅
- Hàng 14–17 (thép lớp 2/3 người dùng nhập trong mẫu Kata) của bản sao đã trống từ lần 2 (giữa 01:08 và 01:49), trước mọi thay đổi hôm nay. Writer chỉ xoá C+n..BZ hàng 11–23 (`KataExcelWriter.cs:70-71`; lần 2 n = 17 → từ cột T) nên không chạm C14..S17. Nguyên nhân (macro Kata khi đổi B3?) = GIẢ ĐỊNH CHƯA XÁC MINH; chỉ ảnh hưởng bản sao nháp.
