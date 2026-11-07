# GĐ2 — Bản đồ mô hình `MB1.EDB`

Đọc 2026-11-06 qua `hprebar-etabs`, tier R (`transaction: none`). ETABS 22.7.0, OAPI 2.10.
Người dùng xác nhận: `MB1.EDB` là mô hình của `MB1-4COT`, dựng tay trong ETABS.

Nhãn: **[đọc]** thấy trực tiếp từ model. **[suy ra]** kết luận từ số liệu. `GIẢ ĐỊNH CHƯA XÁC MINH` chưa có bằng chứng.
Đổi đơn vị dùng 1 T = 9.80665 kN.

## 1. Tổng quan [đọc]

| Mục | Giá trị |
|---|---|
| Trạng thái | **Khoá**, cả 4 case status 4 (đã chạy xong) |
| Đơn vị | `Ton_m_C` (hiện tại + database) |
| Đối tượng | 118 point, 117 frame, 0 area |
| Tầng | Story1 +3 m, Story2 +6 m, Story3 +9 m. Dầm nằm ở Story1 |
| Lưới | G1 |

## 2. Hình học [đọc]

- Dầm dọc trục X, y = 4 m, z = 3 m, từ x = 0 đến 11.7 m.
- 117 frame `B6..B122`, mỗi đoạn 0.1 m, cùng tiết diện `DMB1`.
- Không release, không modifier, không tải trên frame.

| Trục | Point | x (m) | Excel (cộng dồn A29:E29) |
|---|---|---|---|
| Đầu trái | 1 | 0 | 0 |
| A | 2 | 1.6 | 1.6 ✅ |
| B | 3 | 5.3 | 5.3 ✅ |
| C | 4 | 9.0 | 9.0 ✅ |
| D | 5 | 11.4 | 11.4 ✅ |
| Đầu phải | 6 | 11.7 | 11.7 ✅ |

## 3. Tiết diện, vật liệu

`DMB1` kiểu PrecastI, vật liệu `CONC`, modifier = 1 [đọc]:
- `b = [500, 2400, 500, 0]`, `d = [800, 200, 0, 0, 300, 300]` mm.
- A = 1.255 m², I33 = 0.04648 m⁴, As2 = 1.0524 m².

**[suy ra]** Chữ T ngược: cao 0.8 m, sườn 0.5 m, cánh rộng 2.4 m, bản 0.3 m + vát 0.3 m + đỉnh sườn 0.2 m. Kiểm tra diện tích: 0.5×0.2 + (2.4+0.5)/2×0.3 + 2.4×0.3 = 1.255 m² ✅. Khớp b = 2.4, bd = 0.5, h = 0.8, h1 = 0.6.

`CONC`: E = 26 478 MPa (= 2.7×10⁶ T/m²), ν = 0.2 [đọc]. Excel không có ô E. Nguồn giá trị: chưa rõ.

Tiết diện thừa không dùng: D50.80, D30.150, SteelCol, SteelBm, ConcCol, ConcBm.

## 4. Gối, lò xo [đọc]

Không point nào có restraint. Nền mô tả bằng spring point:

| Spring | Point | U3 (T/m) | U1, U2 (T/m) | Excel |
|---|---|---|---|---|
| `Z2` | 1 (x = 0) | 80 | 10⁶ | F129 = 79.86 → làm tròn |
| `Z1` | 117 point còn lại, **gồm cả point 6 (x = 11.7)** | 160 | 10⁶ | F128 = 159.72 → làm tròn |

Đầu phải dùng 160 thay vì 80, khác đầu trái. Xem Q15.

## 5. Tải [đọc]

Pattern: `DL` (self-weight 1), `P-CHANCOT` (0), `~LLRF`.
Case: `P-CHANCOT`, `DL`, `Modal`, `~LLRF`.
Combo: `TH-MONG` = 1.0 × `P-CHANCOT`. Không có `DL`.

Tải đặt tại 4 point cột. So với Excel:

| Trục | Điểm | C (N) ↔ −F3 | E (Qx) ↔ F1 | G (Qy) ↔ F2 | D (Mx) ↔ M1 | F (My) ↔ M2 |
|---|---|---|---|---|---|---|
| A | 2 | 135 = 135 ✅ | 1 = 1 ✅ | 1.7 = 1.7 ✅ | 1 = 1 ✅ | 2.7 = 2.7 ✅ |
| B | 3 | 116 = 116 ✅ | 1 = 1 ✅ | 1.9 = 1.9 ✅ | 1 = 1 ✅ | 3.4 = 3.4 ✅ |
| C | 4 | **100.5 ≠ 100** | **1.5 ≠ 1** | 2.4 = 2.4 ✅ | **1.1 ≠ 1** | 3.1 = 3.1 ✅ |
| D | 5 | **51.5 ≠ 51.1** | 1 = 1 ✅ | 2.3 = 2.3 ✅ | 1 = 1 ✅ | 2.6 = 2.6 ✅ |

Tổng N: Excel 403, ETABS 402.1. Lệch 4 ô.

## 6. Kết quả combo `TH-MONG` [đọc]

Cân bằng: tổng phản lực lò xo U3 = 402.10 T = tổng tải ✅.
Chuyển vị đứng: −20.14 đến −23.57 mm. Excel D105 S = 19 mm.
Nội lực ngoài mặt phẳng nhỏ: |V3| ≤ 1.06 T, |M2| ≤ 0.29 T.m, |T| ≤ 0.91 T.m, |P| ≤ 0.59 T.

Nội lực tại vị trí tính thép, so với ô nhập tay Excel:

| Ô Excel | Vị trí | Excel | ETABS | Chênh |
|---|---|---|---|---|
| C136 | Console A, Gối A | 42.8 | 47.83 (trái A); 42.07 tại x = 1.5; 43.17 tại x = 1.7 | **Không khớp vị trí nào** |
| C137 | Gối A | 47.8 | 47.83 trái, **50.53 phải** | Khớp phía trái. Nhỏ hơn max 2.73 |
| C138 | Nhịp A-B | 32 | −29.16 tại x = 3.7 | Excel cao hơn 2.84 |
| C139, C140 | Gối B | 18, 18 | 11.94 trái, 15.34 phải | Excel cao hơn |
| C141 | Nhịp B-C | 46.5 | −46.49 tại x = 7.3 | Khớp |
| C142, C143 | Gối C | 10, 10 | 2.85 trái, 5.95 phải | Excel cao hơn |
| C144 | Nhịp C-D | 22 | −21.90 tại x = 10.3 | Khớp, làm tròn |
| C145, C146 | Gối D | 8, 8 | −0.47 trái, 2.13 phải | Excel cao hơn |
| A172 | Q (không nhãn) | 75 | 73.67 phải A = |V2| max | Làm tròn lên |
| A173 | Q (không nhãn) | 63 | 61.94 phải B | Làm tròn lên |

**[suy ra]** Dấu theo ETABS: M3 dương tại gối, âm giữa nhịp. Dầm móng chịu phản lực đất hướng lên, nên gối căng thớ dưới, nhịp căng thớ trên. `GIẢ ĐỊNH CHƯA XÁC MINH` cho tới khi bạn xác nhận (Q5).

Ngoại suy từ số liệu: bước nhảy M tại cột bằng My (A: 50.53 − 47.83 = 2.7). Cả 4 gối đều thế.

## 7. Trả lời được từ GĐ1

| Câu | Kết quả |
|---|---|
| Q1 | ETABS là nguồn hiện tại. Người dùng xác nhận. Ghi chú "sap 2000" trong file đã cũ |
| Q4 | C136, C137 không phải hai bên gối theo model hiện tại (47.83 / 50.53). Vẫn cần hỏi |
| Q6 | A172 ứng với |Q| lớn nhất toàn dầm (phải A), A173 ứng với |Q| lớn nhì (phải B). Chưa rõ cách chọn |
| Q9 | Lò xo được làm tròn 159.72 → 160, 79.86 → 80 trước khi nhập model |

## 8. Câu hỏi mới

| # | Nội dung |
|---|---|
| Q13 | Tải trục C, D ở Excel và ETABS khác nhau. Bên nào là số đúng? |
| Q14 | Excel cộng D (Mx) + E (Qx)·h vào mô men **phương dài** (F34), và F (My) + G (Qy)·h vào **phương ngắn** (D39). Trong model, F (My) đặt thành M2, tức uốn **phương dài**, còn D (Mx) thành M1, là xoắn. Hai bên đang ngược nhau. Quy ước đúng là gì? |
| Q15 | Đầu phải (point 6) dùng spring 160 thay vì 80. Có chủ ý không? |
| Q16 | C136 = 42.8 lấy từ đâu? |
| Q17 | Gối B, C, D Excel cao hơn ETABS rõ rệt (18/15.3, 10/5.95, 8/2.13). Có phải số từ lần chạy cũ, hay bạn chủ động lấy min cấu tạo? |
| Q18 | Lấy mô men tại tim cột hay mép cột? |
| Q19 | `TH-MONG` chỉ có `P-CHANCOT`, không có `DL`. Có chủ ý không (tải ròng không tính trọng lượng bản thân)? |
| Q20 | E = 26 478 MPa của `CONC`: giữ cố định, hay theo cấp bền? |

## 9. Hạn chế, ảnh hưởng phụ

- Bridge đang chạy là bản publish 2026-09-19, cũ hơn source (commit 2026-09-22 có auto-start). Không ảnh hưởng đọc.
- `Results.Setup` đổi lựa chọn case xuất kết quả về `TH-MONG` trong phiên ETABS. Không đổi model. Bạn có thể phải chọn lại trong Display/Show Tables nếu đang dùng case khác.
- Chỉ chạy một script mỗi lần: gọi song song trả `Another script is still running`.
- `GetPrecastI` trả `d` không kèm tên tham số. Ý nghĩa `d` là suy ra từ diện tích.

## 10. Toàn vẹn file gốc

| File | Trước | Sau GĐ2 |
|---|---|---|
| `5574-2018-mong-bang-T1.xls` | 1.617.920 byte, 15:22:23.70 | Giống hệt |
| `MB1.EDB` | 76.309 byte, 15:31:23.57 | Giống hệt |

- Không có snapshot ETABS nào cho `MB1`, nghĩa là bridge không chạy lệnh ghi.
- Model vẫn khoá, đơn vị vẫn `Ton_m_C`, kết quả phân tích còn nguyên.
- `MB1.ebk` đổi từ 74.972 byte (14:44) thành 76.309 byte (15:31:23). Thời gian trùng khớp EDB, xảy ra trước khi bridge Attach. Không do lệnh của tôi. `GIẢ ĐỊNH CHƯA XÁC MINH`: ETABS tự tạo backup khi mở file.
