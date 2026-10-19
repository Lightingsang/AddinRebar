# Quy chuẩn đồ hoạ bản vẽ xây dựng Việt Nam

Số liệu dưới đây trích từ 34 bộ bản vẽ thi công thật, không phải ước lượng.
Nguồn gốc và cách bóc tách xem `docs/phan-tich-34-ban-ve-thuc-te.md`.

## 1. Hệ layer kiến trúc (21 layer)

Định nghĩa bằng mã trong `vncad/standards.py:ARCH_LAYERS`. Gọi `document.new_drawing("arch")`
là nạp đủ, không cần khai báo lại.

| Layer | Màu ACI | Kiểu nét | Nét in (mm) | Dùng cho |
|---|---|---|---|---|
| `A-AXIS` | 1 | CENTER | 0.13 | tim trục định vị |
| `A-COLS` | 2 | Continuous | 0.50 | cột BTCT, nét cắt đậm nhất |
| `A-COLS-HATCH` | 8 | Continuous | 0.09 | tô đặc SOLID trong cột |
| `A-WALL` | 7 | Continuous | 0.40 | nét cắt tường 220 và 110 |
| `A-WALL-HATCH` | 9 | Continuous | 0.09 | hatch gạch ANSI31 nghiêng 45 độ |
| `A-BEAM` | 4 | DASHED | 0.30 | dầm khuất trên trần |
| `A-DOOR` | 4 | Continuous | 0.25 | khuôn, cánh, cung mở cửa |
| `A-WIND` | 3 | Continuous | 0.25 | cửa sổ, vách kính |
| `A-STAIR` | 6 | Continuous | 0.25 | bậc thang, mũi bậc |
| `A-RAILING` | 6 | Continuous | 0.25 | lan can, tay vịn |
| `A-FURN` | 8 | Continuous | 0.15 | nội thất |
| `A-SAN` | 5 | Continuous | 0.18 | thiết bị vệ sinh |
| `A-FLOR` | 9 | Continuous | 0.09 | hoa văn gạch lát |
| `A-ROOF` | 1 | Continuous | 0.30 | mái, máng xối |
| `A-EQUIP` | 3 | Continuous | 0.18 | bếp, tủ lạnh, máy lạnh |
| `DIM` | 4 | Continuous | 0.13 | đường gióng và con số |
| `TEXT` | 2 | Continuous | 0.20 | tên phòng, ghi chú |
| `A-GRID-BUBBLE` | 2 | Continuous | 0.30 | bong bóng trục |
| `A-SYMBOL` | 6 | Continuous | 0.25 | cao độ, mặt cắt, chi tiết |
| `KHUNG-TEN` | 7 | Continuous | 0.60 | khung bản vẽ và khung tên |
| `DEFPOINTS` | 7 | Continuous | không in | phụ trợ |

Nguyên tắc bề dày: cột chịu lực đậm nhất, rồi tới tường cắt, rồi nét thấy, mỏng nhất là hatch.
Nhìn bản vẽ in ra phải phân biệt được ba cấp độ đậm nhạt này ngay.

## 2. Chiều cao chữ theo tỷ lệ

Trong bản vẽ thật, chiều cao chữ hay gặp nhất là 200 đơn vị model ở tỷ lệ 1/100,
tức 2.0mm trên giấy. Bảng phân cấp trong `standards.TEXT_MM`:

| Vai trò | mm trên giấy | model @1/100 | model @1/50 |
|---|---|---|---|
| con số kích thước | 2.0 | 200 | 100 |
| ghi chú, ký hiệu thép | 2.0 | 200 | 100 |
| tên phòng | 2.5 | 250 | 125 |
| diện tích phòng | 2.0 | 200 | 100 |
| nhãn cấu kiện D1, C1, M1 | 2.6 | 260 | 130 |
| tiêu đề hình chiếu | 3.5 | 350 | 175 |
| tiêu đề lớn | 5.0 | 500 | 250 |

Gọi `standards.th("room", "1/50")` để lấy đúng số, đừng gõ số cứng.

## 3. Tỷ lệ dùng trong hồ sơ thật

`1/100` mặt bằng và mặt đứng tổng thể; `1/75` bản vẽ trung gian; `1/50` mặt cắt, khai triển WC,
khai triển cầu thang, mặt đứng thi công; `1/25` chi tiết dầm cột; `1/20` chi tiết móng, đài cọc,
lan can; `1/15` và `1/10` chi tiết nút liên kết và chống thấm.

## 4. Kiểu kích thước (DimStyle)

- Mũi tên: gạch chéo kiến trúc `ARCHTICK`, cỡ 2.0mm.
- Đường gióng nhô ra 1.25mm, chân cách cấu kiện 1.0mm, khe hở chữ 0.8mm.
- Chữ nằm trên đường kích thước, canh giữa, làm tròn số nguyên milimet.
- Mỗi tỷ lệ một dimstyle riêng: `document.dimstyle_for(doc, "1/50")`.

## 5. Khung tên hành chính

Bố trí trong `vncad/titleblock.py`, rộng 160mm cao 62mm ở góc dưới phải.
Lề trái bản vẽ 20mm để đóng gáy, ba lề còn lại 10mm.

Cột trái: tên đơn vị thiết kế, địa chỉ, liên hệ; lưới chữ ký 4 vai trò
(Giám đốc, Chủ nhiệm đồ án, Chủ trì, Thiết kế); chủ đầu tư; ngày hoàn thành.

Cột phải: tên công trình; vị trí; hạng mục; tên bản vẽ; hàng cuối chia đôi gồm
giai đoạn cùng tỷ lệ bên trái và số hiệu bản vẽ khổ chữ lớn bên phải.

Số hiệu theo bộ môn: `KT-xx` kiến trúc, `KC-xx` kết cấu, `ME-xx` cơ điện, `DT-xx` dự toán.

## 6. Ký hiệu thường dùng

- Cao độ: hình thoi tô nửa trên mặt bằng, tam giác trên mặt cắt, trị số 3 chữ số thập phân.
- Bong bóng trục: tròn bán kính 4mm, chữ cao 2.4mm, trục ngang đánh số 1 2 3, trục dọc đánh chữ A B C.
- Ghi thép: `Ø8a150` nghĩa là thép đường kính 8 khoảng cách 150; `2Ø16` là 2 thanh đường kính 16.
  Trong file DWG gốc ký tự Ø viết là `%%C`.
