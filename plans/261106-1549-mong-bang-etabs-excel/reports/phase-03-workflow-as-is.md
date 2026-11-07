# GĐ3 — Quy trình hiện tại (bản nháp, đang phỏng vấn)

Trạng thái: **Hết phỏng vấn, còn 1 mục (Q12)**. Chờ người dùng xác nhận toàn bộ mô tả.

## 1. Luồng dữ liệu [đọc + suy ra]

```text
Excel: địa chất + tải + kích thước
  ↓ D61, D64 → C76 → E78 σgl → G88
  ↓ bảng lún → D105 S
  ↓ C128 K = G88/D105 → F128, F129 ki
Nhập tay vào ETABS (làm tròn 160 / 80)
  ↓ chạy phân tích
ETABS: combo TH-MONG → M3, V2
  ↓ đọc và gõ tay
Excel: C136:C146, A172:A173
  ↓ A, ξ, Fa → bố trí thép → kiểm tra
```

Một chiều, không vòng lặp: K không phụ thuộc kết quả ETABS.
Hệ quả với Q2: D64 đổi thì σgl và S đổi gần cùng tỷ lệ, nên K = σgl/S gần như giữ nguyên, chỉ lệch do ROUND của cột I (Q9). `GIẢ ĐỊNH CHƯA XÁC MINH`: chưa tính lại cả bảng lún.

## 2. Ánh xạ Excel ↔ ETABS

| Excel | ETABS | Biến đổi | Trạng thái |
|---|---|---|---|
| A29:E29 | Vị trí point cột, chia 0.1 m | Cộng dồn | ✅ Khớp |
| B69, D24, D25, D72 | Tiết diện `DMB1` | Chữ T ngược | ✅ Khớp |
| C17:C20 | Point F3 | Đổi dấu | ⚠️ Lệch trục C, D |
| E17:E20 | Point F1 | Giữ nguyên | ⚠️ Lệch trục C |
| G17:G20 | Point F2 | Giữ nguyên | ✅ Khớp |
| D17:D20 | Point M1 | Giữ nguyên | ⚠️ Lệch trục C, ngược quy ước Q14 |
| F17:F20 | Point M2 | Giữ nguyên | ⚠️ Ngược quy ước Q14 |
| F128 | Spring `Z1` U3 | Làm tròn | ✅ Khớp |
| F129 | Spring `Z2` U3 | Làm tròn | ⚠️ Chỉ đầu trái, Q15 |
| C136:C146 | M3, combo `TH-MONG` | abs, T.m | ⚠️ Chưa rõ quy tắc |
| A172:A173 | V2, combo `TH-MONG` | abs, làm tròn | ⚠️ Chưa rõ quy tắc |

Đơn vị đồng nhất: model `Ton_m_C`, Excel T, T.m, m. Không có bước đổi đơn vị nếu đọc ở `Ton_m_C`.

## 3. Lần theo một tiết diện: nhịp B-C

Số dưới đây theo bản 15:22 (a = 10 cm). Xem mục cập nhật cuối file.

| Bước | Vị trí | Giá trị |
|---|---|---|
| Tải | C18 = 116 T, C19 = 100.5 T (model 100) | N trục B, C |
| Mô hình | Point 3, 4; spring 160 T/m; `DMB1` | |
| Nội lực | M3 tại x = 7.3 m, combo `TH-MONG` | −46.49 T.m |
| Ô Excel | C141 (gõ tay) | 46.5 T.m |
| Hệ số | G141 = A; H141 = ξ | 0.16504; 0.90924 |
| Thép tính | I141 → H155 | 20.87 cm² |
| Thép chọn | G155: 5Ø20 + 5Ø20 | 31.415 cm² |
| Kết luận | I155 | Đạt |

Tính lại độc lập bằng Python: A, ξ, Fa khớp sheet.

Ghi chú: b = 50 cm cho cả gối và nhịp. Tại nhịp, vùng nén nằm ở cánh rộng 2.4 m, nên dùng b = 50 là thiên an toàn. `GIẢ ĐỊNH CHƯA XÁC MINH` cho tới khi xác nhận thớ căng.

Ảnh hưởng số lệch ở gối A: 47.8 T.m cho Fa = 21.52 cm²; 50.53 T.m cho Fa = 22.91 cm². Bố trí 31.415 cm² vẫn đạt.

## 4. Nhật ký phỏng vấn

| # | Câu hỏi | Trả lời | Ngày |
|---|---|---|---|
| Q1 | Quy trình đã chuyển sang ETABS? | Có. `MB1.EDB` đúng móng MB1-4COT, dựng tay trong ETABS | 2026-11-06 |
| Q13 | Tải lệch: bên nào chuẩn? | Excel đúng. C17:G20 là nguồn chuẩn; model MB1 hiện chưa cập nhật. Tool đọc tải từ Excel | 2026-11-06 |
| Q14 | Quy ước Mx/My | Theo model ETABS: cột F (My) → M2, uốn phương dài; cột D (Mx) → M1, xoắn. Ghi nhận: F34 và D39 trong Excel đang cộng đảo, chưa sửa, cần quyết định riêng | 2026-11-06 |
| Q16, Q17, Q18 | Cách lấy mô men | Gối: M tại tim cột, lấy max hai bên. Nhịp: cực trị trong nhịp. 18, 10, 8 và 42.8 là số cũ hoặc tự nâng, không phải quy tắc | 2026-11-06 |
| Q6 | Cách lấy lực cắt | A172 = V tuyệt đối lớn nhất toàn dầm, A173 = V tuyệt đối lớn nhì tại cột khác, làm tròn lên số nguyên | 2026-11-06 |
| Q5 | Thớ thép | Gối = thép dưới, nhịp = thép trên. 5Ø20 chạy suốt, thêm 5Ø20 gia cường | 2026-11-06 |
| Q15, Q9 | Lò xo | Hai đầu mút đều dùng k/2. Đầu phải 160 trong model hiện tại là nhập sót. Làm tròn k như hiện tại: 159.72 → 160, 79.86 → 80 (người dùng chọn, khác khuyến nghị) | 2026-11-06 |
| Q19 | Combo | Có chủ ý: `TH-MONG` = 1.0 × `P-CHANCOT`, không có DL. Tool giữ nguyên | 2026-11-06 |
| Q20 | E bê tông | Tra bảng sheet `Data` (ẩn) của `Y:\01-PHONG THIET KE\02-KET CAU\01-FILE TÍNH TOÁN KẾT CẤU 2025\09.FILE TINH VBA\CreateSectionEtabs2022v2.xlsm` theo cấp bền. B20: Eb = 27 500 MPa; model hiện 26 478 MPa. Bảng có B15..B60, CB240-T..CB500-V, các Rb, Rbt, Rs, Rsw khớp Excel | 2026-11-06 |
| Q2 | Ngoặc D64 | Giữ như sheet (15.16 T/m²), người dùng chọn, khác khuyến nghị. Tool tái tạo đúng công thức sheet | 2026-11-06 |
| Q3 | Rk tại C71 | Giữ 75 T/m² như sheet, không theo cấp bền | 2026-11-06 |
| Q7, Q8, Q11 | C114, B52 + F44, D172 | Giữ cả ba như sheet | 2026-11-06 |
| Q10 | Kiểm tra thêm | Thêm αm ≤ αR, hàm lượng μ, khoảng hở thanh. Chỉ cảnh báo, không đổi kết quả Excel | 2026-11-06 |
| Mục tiêu 5 | Bố trí thép | Tool đề xuất, kỹ sư duyệt rồi mới ghi vào Excel | 2026-11-06 |
| Phạm vi | Đợt đầu | MB1-4COT trước, rồi 7 sheet 4 cột còn lại, sau đó 2 và 3 cột | 2026-11-06 |
| Dựng model | Cách dựng | Tạo mới đầy đủ EDB cho mỗi móng từ số liệu Excel, không phụ thuộc file mẫu, không đụng model gốc | 2026-11-06 |
| Q14 tiếp | F34, D39 | Tính theo quy ước ETABS (F34 = 50.55, D39 = 10.74 T.m), in số cũ (42.85, 18.44) bên cạnh để đối chiếu | 2026-11-06 |
| Q12 | Ảnh N145 | Có dữ liệu cần dùng. **Chưa nhận được nội dung** | 2026-11-06 |

## 5. Việc còn mở

- Q12: cần ảnh hoặc mô tả `Picture 1` tại N145.
- File `.xls` đã đổi: cần xác nhận D136 = 5 cm và bản nào là chuẩn.

## 6. Quy trình chuẩn tool phải theo

1. Đọc Excel: địa chất, tải C17:G20, kích thước, vật liệu.
2. Tính như sheet, trừ F34 và D39 theo quy ước ETABS. Giữ nguyên D64, C71, C114, B52, F44, D172.
3. Lấy ki từ F128, F129, làm tròn số nguyên. Cả hai đầu mút dùng k/2.
4. Tạo EDB mới: lưới 0.1 m, chữ T ngược, Eb theo cấp bền (bảng `Data`), spring U3, U1 = U2 = 10⁶ T/m.
5. Đặt tải tại cột: C → −F3, E → F1, G → F2, D → M1, F → M2. Combo = 1.0 × tải chân cột.
6. Chạy, đọc M3, V2 ở `Ton_m_C`.
7. Gối: M tại tim cột, max hai bên. Nhịp: cực trị. Q: hai giá trị lớn nhất tại hai cột khác nhau, làm tròn lên.
8. Tính A, ξ, Fa; đề xuất thép (gối = dưới, nhịp = trên); cảnh báo αm, μ, khoảng hở; kỹ sư duyệt; ghi Excel.

## 7. Hệ quả cho việc đối chiếu

Số Excel hiện tại không dùng trực tiếp làm mốc chuẩn được. Theo các quyết định trên, kết quả tool sẽ khác sheet vì:
- Tải trục C, D đổi theo Excel (model cũ).
- Lò xo đầu phải đổi 160 → 80.
- Eb đổi 26 478 → 27 500 MPa.
- F34, D39 đổi quy ước.
- Hàng C136:C146 thay bằng quy tắc mới, ví dụ gối A 47.8 → khoảng 50.5 T.m.

Đề xuất mốc chuẩn cho GĐ4: chạy lại `MB1` trên bản sao với thông số đã chốt, kỹ sư duyệt số đó, rồi dùng làm mốc. Sheet hiện tại chỉ để so chênh lệch có giải thích.

Khung sheet có 2 hàng cho mỗi gối (C136/C137...). Với quy tắc "max hai bên", hai hàng sẽ bằng nhau. Xử lý chi tiết để GĐ4.

## Cập nhật 2026-11-06 16:50 — file gốc đã đổi trong lúc làm

`5574-2018-mong-bang-T1.xls` trên đĩa đổi từ 1.617.920 byte (15:22:23) thành 1.662.976 byte (16:40:35).

- Không do lệnh của tôi: cổng ghi Excel tắt suốt phiên, bridge chỉ ghi nhận lệnh bị từ chối và lỗi script, không có `.hpexcel_snapshots`, phiên Excel attach ở chế độ Read-Only.
- Chưa xác định ai lưu. `H:` là ổ mạng `\192.168.20.20\ProjectHP`; lúc 16:46 có `MB2.*`, `MB3-2c.*` mới ghi trong `01_Mo_Hinh`, tức có người đang làm cùng thư mục.
- So 55 ô chính của `MB1-4COT` trên đĩa (đọc bằng xlrd): 53 giống.
- Đã xác định khác: **D136 = 10 → 5 cm**. D137:D146 link theo, nên h0 = 75 cm, kéo theo G, H, I136:I146 và B172:D173, E172:E173.
- Ví dụ: I136 19.05 → 17.56, I137 21.52 → 19.80, I141 20.87 → 19.21 cm². D172 17.65 → 20.27, H172 17.65 → 20.27, D173 25.02 → 28.72, H173 25.02 → 26.67 cm. Kết luận thép vẫn Đạt.
- So toàn bộ 32 sheet bằng tổng kiểm (số ô, số ô số, tổng giá trị) giữa Excel đang giữ và file trên đĩa: 30 sheet giống hệt. Khác: `MB1-4COT` (số ô giữ nguyên, tổng đổi vì D136) và `MB2-4COT` (thêm 4 số, khớp việc MB2 đang làm). Tổng kiểm không chứng minh tuyệt đối từng ô.
- Excel đang mở (Read-Only) vẫn giữ bản cũ trong bộ nhớ. `MB1.EDB` không đổi.
