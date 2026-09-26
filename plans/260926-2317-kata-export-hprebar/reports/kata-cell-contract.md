# Kata cell contract + golden cases (P1)

Nguồn:
- [analysis report](../../reports/analysis-260926-drawingkata-dynamo-export-excel.md) mục 4.
- **File Kata mẫu `C:\kata_pro\Kata.xlsm`** (Kata Pro), sheet `Dam`: nhãn cột A, hàng 10, comment ô, data validation. Đọc trên bản sao (openpyxl) ngày 2026-09-26, file gốc không mở.

Code tương ứng: [KataRowBuilder.cs](../../../HPRebar/HPRebar.Core/KataExport/Calculators/KataRowBuilder.cs), [KataSegmenter.cs](../../../HPRebar/HPRebar.Core/KataExport/Calculators/KataSegmenter.cs).

## Bố cục sheet `Dam` theo Kata (verified by Kata.xlsm)

- Hàng 10 từ C: `Cột | Nhịp | Cột | Nhịp | …` **xen kẽ cố định**. Vì vậy mọi điểm nối hoặc đầu console phải là một cột "Cột" rộng `0`. File mẫu có `I11=0` (gối rộng 0 giữa dải) và `O11=0` (đệm cuối dải).
- Hàng 11–23: mỗi cột có **nghĩa khác nhau tuỳ là Cột hay Nhịp**. Nhãn cột A mô tả cột "Cột"; comment ở F/H mô tả cột "Nhịp".

| Ô / hàng | Kata: cột "Cột" (gối) | Kata: cột "Nhịp" | Tool ghi (gối / nhịp) | Kết luận |
|---|---|---|---|---|
| B3 | Tên dầm | | tham số Name | ✅ khớp |
| B4 | Số cấu kiện | | tham số Count | ✅ khớp |
| B5 / B6 | h / b dầm (mm) | | h, b | ✅ khớp |
| **B7** | **h sàn (mm)** (mẫu 150) | | `0` (Dynamo) | ⚠️ Dynamo luôn ghi 0 |
| **B8** | **Tên trục dầm** | | `""` (Dynamo) | ⚠️ Dynamo để trống |
| **B9** | **Độ lệch của trục** (mẫu −100) | | `−b/2` (Dynamo) | ⚠️ hằng số, không đo |
| B10 | Cao trình dầm (mẫu text `'+3.300'`) | | số m: `3.3` | 🟡 khác kiểu; Dynamo ghi số và đã dùng được |
| 11 | bề rộng cột | chiều dài nhịp | bề rộng / `"bxh"` dầm gối; chiều dài | ✅ |
| 12–18 | thép chịu lực / gia cường (người dùng nhập) | idem | không ghi; xoá phần **ngoài** n cột | ✅ vùng xoá chỉ dọn cột thừa của dầm trước |
| 19 | (không có comment) | "giật mép trên;thép" (`100;5f25`, dương = lên) | `"rộngCộtTrên;lệch"` / `z Offset Value` | 🟡 nhịp ✅; gối chưa xác minh |
| 20 | Bề rộng dầm giao tại cột (mm) | cốt giá (`2f12`, `0`) | không ghi | — (Dynamo cũng không ghi) |
| 21 | **Độ lệch dầm giao so với tâm cột** ("Ko nhập > cột/2") | "giật mép dưới;thép" (`-100;5f20`) | lệch trục (lấy từ hàng 23) / `−(h_i − h_1) + zOffset_i` | 🟡 nhịp ✅; gối dùng lệch lưới **xấp xỉ** lệch dầm giao |
| 22 | Tên các trục cột | (đai chịu lực `a100/200/50`) | tên lưới / `""` | ✅ |
| 23 | Độ lệch của trục so với tâm cột ("Ko nhập > cột/2") | (đai gia cường) | `s_trục − s_tâm` / `""` | ✅; chỉ lấy lưới nằm trong gối nên luôn ≤ cột/2 |
| 24–44 | đai theo gối (người dùng) | | không đụng | ✅ ngoài vùng xoá |

## 5 điểm mở — trạng thái

| # | Điểm | Kết luận | Bằng chứng |
|---|---|---|---|
| 1 | Vùng xoá C11+n → BZ23 | **Giữ.** Hàng 12–18, 20 là thép/cốt giá người dùng nhập theo từng cột; xoá phần ngoài n cột chỉ bỏ dữ liệu thừa của dầm trước | Nhãn A12–A18, comment F20/H20 |
| 2 | "Gối 0" tại điểm nối / đệm console | **Giữ.** Kata bắt buộc Cột/Nhịp xen kẽ; file mẫu có gối rộng 0 | Hàng 10; `I11=0`, `O11=0` |
| 3 | Hàng 21 nhịp = −(h_i − h_1) + zOffset_i | **Đúng.** Mép dưới nhịp i so với mép dưới danh nghĩa (B10 − B5): (zOffset_i − h_i) − (−h_1) | Comment F21/H21 "mép dưới giật" |
| 4 | Header khi Reverse | **Không ảnh hưởng.** B5 = h_1 cũng là mốc của hàng 21, nên nhất quán ở cả 2 chiều | Công thức mục 3 |
| 5 | B7, B8, B9 | **Đã rõ nghĩa:** h sàn / tên trục dầm / độ lệch của trục. Dynamo ghi hằng số `0` / `""` / `−b/2` → cần người dùng chọn giữ hay tính từ model | Nhãn A7–A9 |

## Còn mở

- Hàng 19 tại cột gối: Kata không có comment. Dynamo ghi `"bề rộng cột trên;độ lệch"`.
- Hàng 21 tại cột gối: Kata là **lệch dầm giao so với tâm cột**, Dynamo ghi lệch **trục lưới** (đúng khi dầm giao nằm trên trục).
- B10: text `'+3.300'` (mẫu) hay số `3.3` (Dynamo).

## Golden (tuỳ chọn)

Bố cục và 4/5 điểm mở đã chốt nhờ file mẫu, nên golden chỉ còn để kiểm số liệu hình học. Nếu chạy được tool cũ: case a–f như cũ (console 2 đầu; gối là dầm; cột trên lệch; lưới lệch tâm; 1 dầm vắt nhiều gối; 1 case Reverse), Save As mỗi lần → Claude đọc `B3:B10`, `C11:BZ23`.

## Giằng móng (bổ sung 2026-09-27, người dùng xác nhận hợp đồng qua /grill-me)

| Tình huống | Gối (hàng 11) | Cột trên (hàng 19) | Ghi chú |
|---|---|---|---|
| Giằng chạm/cắt đài hoặc móng dựng bằng **Structural Foundation: Slab** dày > 100 mm, dài ≤ 6000 mm dọc trục | bề rộng móng dọc trục | cột đứng trên móng: `"rộng;lệch so với tâm móng"` | cột đứng trên móng không bị cảnh báo |
| Cột xuyên qua giằng và đứng trên móng | hợp móng + cột (= bề rộng móng) | cột | |
| Slab foundation dày ≤ 100 mm (**bê tông lót**) | không phải gối, bỏ im lặng | — | chiều dày: tham số type → instance → compound → chiều cao khối |
| Slab foundation dày > 100 mm nhưng dài > 6000 mm; móng băng dưới tường (WallFoundation) | không phải gối | — | cảnh báo "strip/raft" |
| Móng family (móng đơn, đài) | bề rộng dọc trục nếu ≤ 6000 mm | như trên | ngưỡng cũ 3000 mm → 6000 mm |
| Giằng cao hơn mặt móng (có cổ cột, không chạm móng) | cổ cột | — | như dầm sàn |
| Dầm giao / giằng vuông góc tại móng | — | — | hàng 21 = lệch tim dầm giao so với tâm móng |

Model kiểm: `Template_Structural_HoangPhuc_RV2026_V3.rvt` (MẶT BẰNG MÓNG): đài `DaiCoc_D800` (dày 800), lót `BeTongLot_D100` (tham số dày 60 → bị lọc), giằng GMX3 `B_300x500` đỉnh −2000 lọt trong đài −2800..−2000, cột đứng trên đài (đáy −2000).

