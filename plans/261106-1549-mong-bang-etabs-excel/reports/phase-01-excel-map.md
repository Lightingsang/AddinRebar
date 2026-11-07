# GĐ1 — Bản đồ sheet `MB1-4COT`

File: `5574-2018-mong-bang-T1.xls`, đọc 2026-11-06 qua `hprebar-excel` (Excel Read-Only, tier R, cổng ghi tắt).
Nguồn số liệu: dump toàn bộ A1:M176 (724 ô có dữ liệu, 387 công thức), cả cột/hàng ẩn.

Nhãn: **[đọc]** thấy trực tiếp trong file. **[suy ra]** kết luận từ công thức. `GIẢ ĐỊNH CHƯA XÁC MINH` chưa có bằng chứng.

## 1. Tổng quan workbook [đọc]

| Mục | Giá trị |
|---|---|
| Định dạng | `.xls` (BIFF8, FileFormat 56), Read-Only |
| VBA | Không |
| Liên kết ngoài | Không |
| Tính toán | Automatic, không iteration, không Precision-as-displayed |
| Tham chiếu sheet khác từ `MB1-4COT` | Không (4 kết quả tìm `!` đều là chuỗi `"!!!"` trong IF) |
| Named range | 16 toàn workbook: `_FilterDatabase`, `Print_Area` của sheet khác, 2 `_xlfn`. Không name nào cho `MB1-4COT` |
| Bảo vệ sheet | Không |
| Ẩn trên `MB1-4COT` | Hàng 11-13, 38, 100-102, 130-133. Cột J |
| Hình | 20 OLE (công thức ảnh, hàng 31-167), 1 ảnh `Picture 1` tại N145, 8 ghi chú |

**Sheet (32):** hiện 14 sheet tính: MB1, MB2, MB5, MB7, MB9, MB10, MB11, MB13 (4 cột), MB3, MB4, MB6 (2 cột), MB8, MB12, MB14 (3 cột). Ẩn 18: `Sheet1`, các bản 2/3/5 cột. Sheet khác chưa đọc nội dung theo phạm vi.

## 2. Quy ước ô [đọc + suy ra]

- **Chữ đỏ (ColorIndex 3) = kỹ sư nhập tay.** Ngoại lệ: bảng đất E5:I10 nhập tay nhưng chữ đen.
- Toàn sheet nền trắng. Không dùng màu nền phân loại.

## 3. Bản đồ theo vùng

| Hàng | Nội dung | Đơn vị | Ô nhập | Ô kết quả chính |
|---|---|---|---|---|
| 1 | Tên sheet tự động | | | D1 |
| 2-13 | Địa chất: lớp, dày, γ, c, φ, E | m, T/m³, T/m², độ, T/m² | E5:E10, F5:F10, G7:G10, H7:H10 (độ + phút), I7:I10 | D6:D13 độ sâu |
| 14-20 | Tải tính toán tại 4 trục A-D: N, Mx, Qx, My, Qy | T, T.m | B17:G20 | |
| 21-29 | Kích thước móng | m | D22 hm, D24 h, D25 b, A29:E29 console + nhịp | D23 H, D26 a = 11.7, F29:I29 x1-x4 |
| 30-41 | Tổng tải, mô men tại tâm móng | T, T.m | | C31 = 403, F31 = 335.83 (÷1.2), F34, C36, D39, C41 |
| 43-52 | Cường độ đất nền | T/m² | | F44 c, F45 φ, F46-48 A, B, D, B52 Rtc = 25.89 |
| 53-64 | Kiểm tra kích thước, lệch tâm | T/m² | | D55, D57, D61 = 18.13, F61 = 31.07, D64 = 15.16 |
| 66-72 | Bề rộng dầm, chiều cao cánh | m | D68 bc, B69 bd, D72 h1 | H67-H69, C71 |
| 73-107 | Lún tổng lớp phân tố | m | F88 = 1, C88 = 0 | E78 σgl, C85, bảng 88-102, D105 S = 0.019 |
| 108-118 | Vật liệu, thép cánh móng | kG/cm², cm² | C110 B20, C111 CB400-V, F116:G116, F118:G118 | E110 Rb, H110 Rbt, E111 Rs, H111 Rsw, C114, F114, C116 = 4.78 |
| 119-132 | Hệ số lò xo | T/m³, T/m | B128 = 0.1, B129 = 0.05, E128 = 1 | C128 K = 665.5, F128 = 159.72, F129 = 79.86 |
| 134-146 | Tính thép dầm băng theo 11 vị trí | T.m, cm | **C136:C146**, D136 a | G, H, I136:I146 |
| 147-160 | Bố trí thép dầm | cm² | C150:F150, E151:F152, E155:F155 | G, H, I150:I160 |
| 161-176 | Cốt đai | T, cm | C170 Ø8, **A172:A173**, I172:I173 n = 4, I175:I176 | B, C, D..H172:173 |

## 4. Ranh giới với phần mềm phân tích

**Ô nhận kết quả phân tích [đọc]:** có ghi chú trực tiếp trong file.

| Ô | Ghi chú trong ô | Giá trị hiện tại |
|---|---|---|
| C136:C146 | C135: "lấy từ sap 2000" | 42.8, 47.8, 32, 18, 18, 46.5, 10, 10, 22, 8, 8 T.m |
| A172:A173 | A171: "Lực cắt lấy từ kết quả sap 2000" | 75, 63 T |

Tất cả là số gõ tay, không link.

**Dữ liệu có khả năng đưa vào mô hình** — `GIẢ ĐỊNH CHƯA XÁC MINH`, chờ GĐ2 đối chiếu:
- Tải C17:G20 tại 4 trục.
- Hình học A29:E29.
- Tiết diện dầm B69 × D24 (0.5 × 0.8 m), bề rộng móng D25.
- Lò xo F128 (bước 0.1 m), F129 (đầu mút 0.05 m).

## 5. Chuỗi tính từ nội lực đến thép [đọc]

Dầm băng, ví dụ C137 (gối A):

```
C137 = 47.8 T.m (nhập tay)
→ G137 = M·10⁵ / (Rb·b·h0²) = 0.1697     Rb = E110 = 115, b = 50, h0 = 80-10
→ H137 = (1+√(1-2A))/2 = 0.9064          (ξ)
→ I137 = M·10⁵ / (H·Rs·h0) = 21.52 cm²   Rs = E111 = 3500
→ H151 = I137
→ G151 = 5Ø20 + 5Ø20 = 31.415 cm²        (bố trí nhập tay)
→ I151 = "Đạt" khi H151 < G151
```

Cánh móng: C114 = C31/(b·a) + 2·H = 18.35 → F114 = q·b1²/2 = 8.28 T.m/m → C116 = 4.78 cm² → chọn Ø14a150 = 10.26 cm².
Hàng 118 bố trí Ø12a200, cấu tạo.

Cốt đai: Q (A172) → D172 Utt = Rsw·n·fđ·8·Rbt·b·h0²/Q² = 17.65 cm; E172, F172, G172 giới hạn; H172 = MIN = 17.65. Chọn I175 = I176 = 15 cm tay.

Lò xo: σgl (G88) / S (D105) = K (C128) → ki = K·b·ai·n.

## 6. Hằng số viết cứng trong công thức [đọc]

| Giá trị | Ô | Ý nghĩa |
|---|---|---|
| 1.2 | F31, C36, C41, F61 | Hệ số tải, 1.2Rtc |
| 2 (T/m³) | D61, D64, C114, E78, E80 | γtb đất + móng |
| 75 | C71 | Rk, T/m² |
| 0.9 | C116 | Hệ số tay đòn thép cánh |
| 0.05 | C116 | a của cánh, m |
| 0.8 | I88:I101 | β tính lún |
| 3.14 / 3.1415 / 3.1416 | H116 / G150 / D172 | π lệch nhau theo vùng |
| B15, B20, B22.5, B25 | E110, H110 | Bảng Rb, Rbt theo cấp bền |
| CB240-T, CB300-V, CB400-V | E111, H111 | Bảng Rs, Rsw theo nhóm thép |

Tên file ghi 5574-2018. Chưa đối chiếu công thức và các bảng này với tiêu chuẩn: `GIẢ ĐỊNH CHƯA XÁC MINH`.

## 7. Điểm cần bạn xác nhận (không sửa gì)

| # | Ô | Hiện trạng | Cần hỏi |
|---|---|---|---|
| Q1 | C135, A171 | Ghi chú nói SAP 2000, bạn nói ETABS | Quy trình đã chuyển sang ETABS chưa, hay vẫn còn số SAP? |
| Q2 | D64 | Số hạng `6e/b` nằm ngoài ngoặc, khác D61. Tính lại: 15.16 theo sheet, 13.79 nếu cùng ngoặc | Có chủ ý không? |
| Q3 | C71 | Dùng Rk = 75 T/m² cố định, trong khi B20 có H110 = 9 kG/cm² = 90 T/m². C71 = 0.476 m, nếu 90 là 0.396 m | Giữ 75 hay lấy theo cấp bền? |
| Q4 | C136, C137 | Cùng "Gối A" nhưng 42.8 và 47.8. Các gối khác hai dòng bằng nhau | Hai bên gối khác nhau, hay gối A lấy khác? |
| Q5 | C136:C146 | Không có dấu, không ghi thớ trên/dưới. Bố trí thép cũng không ghi | Quy ước trích M và phân thép trên/dưới thế nào? |
| Q6 | A172, A173 | 75, 63 không nhãn vị trí | Là Q gối, Q nhịp? Lấy max thế nào? |
| Q7 | C114 | Áp lực cánh cộng thêm 2·H = 4 T/m² | Có chủ ý không, hay nên dùng áp lực ròng? |
| Q8 | D22, D23, B52, F44 | B52 dùng hm = 1.5, tra lớp đất dùng H = 2 | Đúng ý đồ? |
| Q9 | D105, C128 | S cộng từ các giá trị ROUND 3 chữ số, K rất nhạy theo S | Giữ cách làm tròn này cho tool? |
| Q10 | I150:I160 | Chỉ so Fa bố trí với Fa tính. Không kiểm αm, μ, khoảng hở | Tool cần thêm kiểm tra không? |
| Q11 | D172 | Công thức Utt theo dạng 8RbtBh0²/Q² | Giữ công thức này trong tool? |
| Q12 | N145 | `Picture 1` ngoài vùng tính, 497×201 px | Đây là biểu đồ gì? Trích được ảnh nhưng công cụ xem bị từ chối, chưa xem được |

Lỗi nhỏ, không ảnh hưởng số hiện tại:
- G173 tham chiếu F137, dòng khác F138 (cùng giá trị).
- D168, D169 cùng ghi "h>450"; `G171` trùng tên cột `Uct3` với F171.
- F127 ghi đơn vị `ki(T)`, thực tế T/m.
- A103 dùng `<5`, D105 dùng `<=5`.

## 8. Lưu ý cho tool sau này

- 14 sheet hiện có bố cục khác nhau theo số cột (used range 157, 176, 202 hàng; MB7 đến cột V, MB8 đến cột AC). Không được ghi cứng địa chỉ một sheet cho tất cả.
- F116, G116 là chuỗi (`f14`, `a150`), công thức đọc bằng `RIGHT`. Sai định dạng là hỏng kết quả.
- Không có named range: mọi địa chỉ phải tự định nghĩa.
- Chưa có kiểm tra phạm vi (Q10).

## 9. Hạn chế công cụ đã gặp

| Vấn đề | Bằng chứng | Xử lý |
|---|---|---|
| Tier W báo động giả: mọi `List.Add` bị xếp Write, seed `read_range`, `read_worksheet_info` bị từ chối khi cổng ghi tắt | [ExcelTierTable.cs:101](../../../HPExcel/HPExcel.McpBridge/Safety/ExcelTierTable.cs#L101) | Viết script chỉ đọc bằng LINQ, không `.Add`. Không bật quyền ghi |
| **Lỗ an toàn chiều ngược:** member ghi không có trong bảng được xếp ReadOnly, dù comment nói fail-closed | [ExcelTierTable.cs:120](../../../HPExcel/HPExcel.McpBridge/Safety/ExcelTierTable.cs#L120) | Chỉ ghi nhận, chưa sửa |
| Ép kiểu `(Workbook)`, `(Worksheet)` lỗi `TYPE_E_ELEMENTNOTFOUND` | runId 2 | Dùng dynamic qua `var` |
| `TopLeftCell` lỗi trên OLE, comment | runId 9 | Tính vị trí từ `Top`, `Left` |
| Không đọc được nội dung 20 công thức ảnh | công cụ xem ảnh từ chối | Công thức thật đã có trong ô tính, ảnh chỉ minh hoạ |

## 10. Toàn vẹn file gốc

| File | Trước | Sau GĐ1 |
|---|---|---|
| `5574-2018-mong-bang-T1.xls` | 1.617.920 byte, 15:22:23.70 | Giống hệt |
| `01_Mo_Hinh/MB1.EDB` | 76.309 byte, 15:31:23.57 | Giống hệt |

## Cập nhật 2026-11-06 16:50 — file gốc đã đổi trong lúc làm

`5574-2018-mong-bang-T1.xls` trên đĩa đổi từ 1.617.920 byte (15:22:23) thành 1.662.976 byte (16:40:35).

- Không do lệnh của tôi: cổng ghi Excel tắt suốt phiên, bridge chỉ ghi nhận lệnh bị từ chối và lỗi script, không có `.hpexcel_snapshots`, phiên Excel attach ở chế độ Read-Only.
- Chưa xác định ai lưu. `H:` là ổ mạng `\192.168.20.20\ProjectHP`; lúc 16:46 có `MB2.*`, `MB3-2c.*` mới ghi trong `01_Mo_Hinh`, tức có người đang làm cùng thư mục.
- So 55 ô chính của `MB1-4COT` trên đĩa (đọc bằng xlrd): 53 giống.
- Đã xác định khác: **D136 = 10 → 5 cm**. D137:D146 link theo, nên h0 = 75 cm, kéo theo G, H, I136:I146 và B172:D173, E172:E173.
- Ví dụ: I136 19.05 → 17.56, I137 21.52 → 19.80, I141 20.87 → 19.21 cm². D172 17.65 → 20.27, H172 17.65 → 20.27, D173 25.02 → 28.72, H173 25.02 → 26.67 cm. Kết luận thép vẫn Đạt.
- So toàn bộ 32 sheet bằng tổng kiểm (số ô, số ô số, tổng giá trị) giữa Excel đang giữ và file trên đĩa: 30 sheet giống hệt. Khác: `MB1-4COT` (số ô giữ nguyên, tổng đổi vì D136) và `MB2-4COT` (thêm 4 số, khớp việc MB2 đang làm). Tổng kiểm không chứng minh tuyệt đối từng ô.
- Excel đang mở (Read-Only) vẫn giữ bản cũ trong bộ nhớ. `MB1.EDB` không đổi.
