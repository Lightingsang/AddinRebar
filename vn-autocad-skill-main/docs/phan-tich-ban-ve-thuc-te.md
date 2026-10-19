# Phân tích 34 bộ bản vẽ thi công thật

Tài liệu ghi lại cách bóc tách dữ liệu từ bản vẽ thật và những gì rút ra được. Mọi con số trong
`vncad/standards.py` và trong các skill đều truy ngược về đây.

## Nguồn dữ liệu

34 file DWG định dạng AC1018 (AutoCAD 2004) của 4 công trình ở Nam Bộ. Tên chủ đầu tư và địa
điểm cụ thể được lược bỏ.

| Công trình | Nội dung có trong hồ sơ |
|---|---|
| Biệt thự phố | kiến trúc, kết cấu, điện nước, khai triển WC, cầu thang, thống kê cửa |
| Nhà phố | mặt bằng, kết cấu, nội thất, phối cảnh |
| Công trình tôn giáo kèm nhà ở | kết cấu, thuyết minh tính toán |
| Hội trường khẩu độ lớn | vì kèo thép, dầm sàn, móng |

## Cách bóc tách

DWG là định dạng nhị phân nén, đọc trực tiếp không ra gì. Quy trình đã dùng:

1. Chạy LISP trong AutoCAD duyệt từng file, xuất ra text: danh sách lớp kèm màu và kiểu nét,
   danh sách layout, danh sách block, mọi đối tượng chữ kèm lớp và chiều cao.
2. Chạy script Python tổng hợp tần suất lớp, block, chiều cao chữ, phân loại nội dung chữ.
3. Chữ trong file gốc mã hoá VNI-Windows (phông VNI-Times, VNI-Helve): đọc bằng cp1252 rồi ánh xạ
   sang Unicode mới ra tiếng Việt có dấu. Đọc bằng utf-8 sẽ làm hỏng byte.

Thu được **74.257 đối tượng chữ**, 6.895 chuỗi khác nhau sau khi lọc trùng.

## Kết quả chính

### 1. Chiều cao chữ

| Chiều cao (model) | Số lần | Ý nghĩa ở tỷ lệ 1/100 |
|---|---|---|
| 200 | 14.633 | 2,0mm, cỡ chữ chủ đạo |
| 297 | 8.894 | 3,0mm |
| 260 | 8.709 | 2,6mm, nhãn cấu kiện |
| 250 | 5.687 | 2,5mm, tên phòng |
| 350 | 3.585 | 3,5mm, tiêu đề hình |
| 500 | 1.122 | 5,0mm, tiêu đề lớn |

Đây là cơ sở của bảng `standards.TEXT_MM`.

### 2. Tỷ lệ dùng thật

`1/50` xuất hiện 146 lần, `1/100` 123 lần, `1/20` 35 lần, `1/25` 26 lần, `1/10` 14 lần,
`1/15` 7 lần, `1/75` 6 lần. Một bộ hồ sơ dùng nhiều tỷ lệ song song, không chỉ một.

### 3. Block hay dùng

Dấu gạch chéo kích thước, ký hiệu cao độ, cửa đi các cỡ, phễu thu sàn, chậu rửa, bếp, bàn ăn,
bồn cầu, tivi, ký hiệu đường kính thép, cây xanh, logo đơn vị.

Bản vẽ thật **không** vẽ lại từng cái bồn cầu bằng đường thẳng mà dùng block. Đây là lý do lớn
nhất khiến bản vẽ thật trông dày dặn còn bản vẽ tự động trông rỗng.

### 4. Cách ghi thép

`%%C8a150` 294 lần, `%%C10a150` 167 lần, `%%C8a120` 127 lần, `2%%C16` 107 lần, `D6a150` 104 lần,
`d14a150` 84 lần, `3(1x4)%%C20` 18 lần, `%%C6a100-200`. `%%C` là mã AutoCAD của ký tự Ø.
Dạng chuẩn: `Ø<đường kính>a<bước>` hoặc `<số lượng>Ø<đường kính>`.

### 5. Mã hiệu cấu kiện

Đài móng `M1` đến `M4`, ghép với cụm cọc thành `M1-CC1-C1`. Cột `C1` đến `C4`. Giằng móng
`GM1` đến `GM18`. Đà kiềng `DK1`. Ô sàn `S1` đến `S6`.

### 6. Cao độ

Ghi 3 chữ số thập phân: `+5.485`, `+8.950`, `-1.280`. Không làm tròn. 488 giá trị cao độ khác nhau.

### 7. Trường khung tên

Lặp lại ở mọi tờ: `ĐƠN VỊ THIẾT KẾ`, `CHỦ ĐẦU TƯ`, `TÊN CÔNG TRÌNH`, `VỊ TRÍ`, `HẠNG MỤC`,
`TÊN BẢN VẼ`, `TỶ LỆ`, `SỐ HIỆU BẢN VẼ`, `NGÀY HOÀN THÀNH`, `LẦN SỬA`, `NỘI DUNG SỬA`,
`GIÁM ĐỐC`, `CHỦ NHIỆM ĐỒ ÁN`, `CHỦ TRÌ KIẾN TRÚC`, `CHỦ TRÌ KẾT CẤU`, `THIẾT KẾ`.

### 8. Ghi chú kỹ thuật thật

Vài câu nguyên văn đã giải mã, dùng làm mẫu cho ghi chú:

- "Thép gối tăng cường của dầm và sàn kéo dài tối thiểu 1/4 nhịp"
- "Tất cả các dầm giao nhau tăng cường thép đai: 5D6a50 mỗi bên"
- "Tất cả các vị trí có lỗ kỹ thuật trên sàn phải tăng cường 2D14a50"
- "Tại vị trí có ô vệ sinh: phải uốn thép đà và sàn xuống 5cm"
- "Tải thử tĩnh: P.max = 60 tấn; P.min = 50 tấn"
- "Giằng móng nối thép ở 1/3 khoảng cách giữa hai móng"

## Mười khoảng cách giữa bản vẽ tự động thông thường và bản vẽ thật

| # | Bản vẽ thật | Bản vẽ tự động thông thường | Xử lý bằng |
|---|---|---|---|
| 1 | bộ nhiều tờ | một tờ gộp hết | danh mục hồ sơ, `sheet` nhiều Layout |
| 2 | dùng block | nét rời rạc | `blocks`, `fixtures` |
| 3 | nhiều tỷ lệ | chỉ 1/100 | `standards.SCALES`, cửa sổ nhìn tỷ lệ riêng |
| 4 | cao độ 3 số lẻ | làm tròn thô | `annotate.level_mark` |
| 5 | ghi thép Ø.a | không có | `struct.rebar_line`, `rebar` |
| 6 | mã hiệu cấu kiện | không có | `struct` |
| 7 | ghi chú thi công thật | câu chung chung | `layout.NOTES`, `chi-tiet-cau-tao-thuc-te.md` |
| 8 | khung tên 16 trường | khung tên rút gọn | `titleblock` |
| 9 | tách lớp hatch | gộp chung | `standards.ARCH_LAYERS` |
| 10 | chữ phân cấp 6 mức | một cỡ chữ | `standards.TEXT_MM` |
