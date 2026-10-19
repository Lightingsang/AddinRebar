# Danh mục bộ hồ sơ bản vẽ thi công nhà ở

Trích từ cấu trúc thư mục và tiêu đề bản vẽ trong 34 bộ hồ sơ thật. Đây là lý do chính
khiến bản vẽ AI trông thiếu: hồ sơ thật là một BỘ nhiều tờ, không phải một tờ gộp.

## Phần kiến trúc (KT)

| Số hiệu | Tên bản vẽ | Tỷ lệ |
|---|---|---|
| KT-01 | Mặt bằng bố trí kiến trúc tầng trệt, lầu 1 | 1/100 |
| KT-02 | Mặt bằng bố trí kiến trúc lầu 2, mặt bằng mái | 1/100 |
| KT-03 | Mặt đứng chính, mặt đứng bên | 1/100 hoặc 1/50 |
| KT-04 | Mặt cắt A-A, mặt cắt B-B | 1/50 |
| KT-05 | Khai triển cầu thang: mặt bằng, mặt cắt, chi tiết bậc | 1/50 và 1/20 |
| KT-06 | Khai triển vệ sinh WC1 đến WC4: mặt bằng, 4 mặt tường ốp | 1/50 |
| KT-07 | Mặt bằng lát gạch từng tầng, chỉ định vật liệu | 1/100 |
| KT-08 | Mặt bằng trần, mặt cắt trần giật cấp | 1/100 và 1/25 |
| KT-09 | Bảng thống kê cửa: ký hiệu, kích thước, số lượng, vật liệu | 1/50 |
| KT-10 | Chi tiết lan can, ban công, hàng rào, cổng | 1/20 |

## Phần kết cấu (KC)

| Số hiệu | Tên bản vẽ | Tỷ lệ |
|---|---|---|
| KC-01 | Mặt bằng định vị cọc ép và đài móng | 1/100 |
| KC-02 | Mặt bằng đà kiềng, giằng móng; chi tiết đài móng M1 đến M4 | 1/100 và 1/20 |
| KC-03 | Mặt bằng định vị cột, chi tiết bố trí thép cột C1 đến C4 | 1/100 và 1/25 |
| KC-04 | Mặt bằng kết cấu dầm sàn tầng 2 | 1/100 |
| KC-05 | Mặt bằng kết cấu dầm sàn tầng 3 và sàn mái | 1/100 |
| KC-06 | Chi tiết bố trí thép dầm D1, D2; mặt cắt dầm | 1/25 |
| KC-07 | Chi tiết cầu thang: bản thang, dầm chiếu nghỉ | 1/25 |
| KC-08 | Bảng thống kê cốt thép, bảng tổng hợp theo nhóm đường kính | không tỷ lệ |

## Phần cơ điện (ME)

| Số hiệu | Tên bản vẽ | Tỷ lệ |
|---|---|---|
| ME-01 | Mặt bằng cấp điện chiếu sáng từng tầng | 1/100 |
| ME-02 | Mặt bằng ổ cắm, điều hoà, sơ đồ tủ điện | 1/100 |
| ME-03 | Mặt bằng cấp nước, thoát nước từng tầng | 1/100 |
| ME-04 | Sơ đồ không gian cấp thoát nước, chi tiết bể tự hoại | 1/50 |

## Quy tắc ghép tờ

- Một tờ A3 tỷ lệ 1/100 chứa tối đa 2 mặt bằng nhà phố ngang 5m sâu 18m.
  Muốn xếp 3 mặt bằng thì dùng khổ A2.
- Nhà rộng hơn 8m hoặc sâu hơn 20m: mỗi mặt bằng một tờ A3, hoặc dùng A2.
- Bảng thống kê cốt thép và thống kê cửa luôn tách thành tờ riêng khi số dòng vượt 15.
- Ghi chú kỹ thuật đặt thành cột dọc bên phải bản vẽ, phía trên khung tên.

## Thứ tự tên tiêu đề hình chiếu (đúng cách viết trong hồ sơ thật)

`MẶT BẰNG BỐ TRÍ KIẾN TRÚC TẦNG 1 (TRỆT)`, `MẶT BẰNG LẦU 1`, `MẶT BẰNG ÁP MÁI`,
`MẶT ĐỨNG CHÍNH`, `MẶT ĐỨNG TRỤC 1-5`, `MẶT CẮT A-A`, `MẶT CẮT 1-1`,
`CHI TIẾT ĐÀ KIỀNG`, `CHI TIẾT CẦU THANG`, `KHAI TRIỂN BAN CÔNG`,
`BẢNG THỐNG KÊ CỐT THÉP`, `BẢNG THỐNG KÊ CỬA`, `MẶT BẰNG ĐỊNH VỊ`.

## Bộ mẫu 26 tờ trong repo (`examples/nha_pho_5x18m`)

Số hiệu trong bộ mẫu không trùng hẳn bảng trên: bảng trên là cách đánh số phổ biến rút từ hồ sơ
thật, bộ mẫu gộp tờ theo khổ A3 cho nhà 5x18m. Khi làm dự án thật, theo quy ước của đơn vị thiết kế.

| Số hiệu | Tên tờ | Số hiệu | Tên tờ |
|---|---|---|---|
| KT-01 | Mặt bằng kiến trúc tầng 1, tầng 2 | KT-14 | Chi tiết cấu tạo chống thấm |
| KT-02 | Mặt bằng tầng 3 và mặt bằng mái | KT-15 | Tổng mặt bằng định vị |
| KT-03 | Mặt cắt A-A và mặt đứng chính | KC-01 | Mặt bằng móng cọc, đà kiềng, thống kê thép móng |
| KT-04 | Khai triển phòng vệ sinh WC 2 | KC-02 | Kết cấu dầm sàn tầng 2 và thống kê thép |
| KT-05 | Khai triển cầu thang bộ và chi tiết lan can | KC-03 | Chi tiết thép dầm D1, cột C1, nối thép cột |
| KT-06 | Mặt đứng bên trục 1-5 | KC-04 | Chi tiết nút khung và thép mũ gối sàn |
| KT-07 | Mặt bằng lát gạch tầng 1, tầng 2 | KC-05 | Kết cấu cầu thang bộ |
| KT-08 | Mặt bằng trần tầng 1, tầng 2 | ME-01 | Mặt bằng cấp điện chiếu sáng tầng 1 |
| KT-09 | Bảng thống kê cửa đi và cửa sổ | ME-02 | Mặt bằng cấp thoát nước tầng 1 |
| KT-10 | Mặt cắt B-B | ME-03 | Mặt bằng cấp điện tầng 2, tầng 3 |
| KT-11 | Khai triển phòng vệ sinh WC 1 | ME-04 | Mặt bằng cấp thoát nước tầng 2, tầng 3 |
| KT-12 | Khai triển phòng vệ sinh WC 3 | ME-05 | Sơ đồ nguyên lý tủ điện tổng |
| KT-13 | Mặt bằng lát gạch và trần tầng 3 | ME-06 | Sơ đồ không gian cấp thoát nước |

