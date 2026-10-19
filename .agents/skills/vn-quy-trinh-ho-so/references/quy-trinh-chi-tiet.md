# Chi tiết mười một bước

Mỗi bước ghi: đầu vào, việc phải làm, đầu ra, cửa soát. Ví dụ đường dẫn lấy theo hồ sơ mẫu
`examples/nha_pho_5x18m`.

## Bước 1. Tiếp nhận yêu cầu

**Đầu vào:** mô tả của người dùng, ảnh sổ đỏ, bản phác tay nếu có.

**Việc:** lập bảng thông số, hỏi những ô còn trống. Câu hỏi mẫu (luôn kèm phương án):

| Ô | Câu hỏi | Phương án gợi ý |
|---|---|---|
| Lô đất | Kích thước lô và mặt tiền? | 4x16, 5x18, 5x20, khác |
| Số tầng | Mấy tầng, có tum thang, sân thượng? | 2 tầng, 3 tầng + sân thượng, 4 tầng + tum |
| Cao tầng | Chiều cao tầng trệt và tầng lầu? | 3,6 + 3,4; 3,65 đều; 4,0 + 3,4 |
| Công năng | Tầng trệt để làm gì? | gara + khách + bếp; kinh doanh; ở hoàn toàn |
| Phòng | Số phòng ngủ, WC, có phòng thờ? | theo gia đình 3, 4, 5 người |
| Địa chất | Nền đất khu vực? | ven sông yếu (cọc ép), sét pha (móng băng), chưa rõ (khảo sát) |
| Hướng | Mặt tiền hướng nào? | ảnh hưởng bố trí phòng thờ và cửa sổ |

**Đầu ra:** bảng thông số người dùng đã xác nhận. **Cửa:** không còn ô "chưa rõ" ảnh hưởng tới
kết cấu (địa chất, số tầng, nhịp).

## Bước 2. Dữ liệu công trình

**Việc:** sửa `layout.py` theo thông số: `AXIS_X`, `AXIS_Y`, `FLOOR_LEVEL`, `LEVELS`,
`FLOOR_WALLS` (tường kèm lỗ mở), `FLOOR_ROOMS` (tên, tâm nhãn, diện tích, góc xoay nhãn),
`FLOOR_FIXTURES`, `DOORS`, `NOTES`, `TITLE_INFO`.

**Cửa:** các file `draw_*.py` chỉ đọc số từ `layout.py`; diện tích phòng tính khớp kích thước.

## Bước 3. Danh mục tờ

**Việc:** chọn tờ theo [danh-muc-ho-so-ban-ve.md](danh-muc-ho-so-ban-ve.md), ghi số hiệu, tên,
tỷ lệ từng hình. Nhà rộng hơn 8m hoặc sâu hơn 20m thì mỗi mặt bằng một tờ A3 hoặc dùng A2.

**Cửa:** mỗi tờ có một chủ đề; không tờ nào nhồi quá 3 cụm hình.

## Bước 4. Bố cục model space

**Việc:** trong `regions.py` đặt mỗi cụm hình vào một ô lưới 32m x 30m, hàng theo bộ môn
(mặt bằng, mặt đứng và mặt cắt, chi tiết, kết cấu, cơ điện, sơ đồ). Khung ô vẽ trên lớp
`MODEL-GUIDE` không in. Chi tiết: skill vn-bo-cuc-to-giay.

**Cửa:** `check_cell_bleed` sạch; tỷ lệ khung model gần vuông (mẫu: 173m x 150m).

## Bước 5. Vẽ kiến trúc

**Việc:** skill vn-kien-truc. Thứ tự: tường, cột (vẽ sau tường), cửa, thang, thiết bị, kích thước
3 lớp, bong bóng trục, nhãn phòng, tiêu đề hình. Chữ và kích thước vẽ đúng tỷ lệ của cửa sổ sẽ
nhìn hình đó.

**Cửa:** soát không render không có lỗi mới ở các ô kiến trúc.

## Bước 6. Vẽ kết cấu và bảng thép

**Việc:** skill vn-ket-cau. Móng, dầm sàn, chi tiết thép, cầu thang. Bảng thống kê cốt thép sinh
bằng hàm từ kích thước cấu kiện, không gõ số thanh.

**Cửa:** số thanh khớp bước thép ghi trên hình; thép chịu lực đúng lớp; lớp bảo vệ khớp ghi chú;
hàm lượng thép trong khoảng hợp lý (vn-soat-ban-ve).

## Bước 7. Vẽ cơ điện

**Việc:** skill vn-co-dien. Mặt bằng điện, nước từng tầng trên nền tường mờ; sơ đồ tủ điện, sơ đồ
không gian cấp thoát nước; bảng ký hiệu.

**Cửa:** mọi tuyến ống, dây có ghi tiết diện; mọi ký hiệu có trong bảng ký hiệu.

## Bước 8. Dựng tờ giấy

**Việc:** skill vn-bo-cuc-to-giay. Mỗi tờ một Layout A3, cửa sổ nhìn mở vào ô tương ứng với tỷ lệ
riêng; tâm cửa sổ lấy bằng `tools/do-o-bao-cum-hinh.py`, không đoán. Khung tên, ghi chú, bảng
biểu vẽ thẳng trên giấy.

**Cửa:** `check_views` sạch (không chồng cửa sổ, không lấn khung tên, không lọt khung).

## Bước 9. Soát

**Việc:** skill vn-soat-ban-ve, agent kiem-soat-ban-ve. Ba tầng: bốn phép soát tự động; render và
nhìn từng tờ mới hoặc tờ vừa sửa; soát kỹ thuật.

**Cửa:** bộ soát báo SACH; đã nhìn ảnh; không còn lỗi kỹ thuật.

## Bước 10. Dự toán

**Việc:** skill vn-du-toan. Dòng thép lấy từ `rebar_quantities`; dòng nào chưa có căn cứ trên bản
vẽ ghi "SỐ ƯỚC" và nêu giả thiết.

**Cửa:** đọc ngược file Excel thấy đúng số; soát chéo hàm lượng (kg thép trên m² sàn, trên m³ bê tông).

## Bước 11. Xuất và bàn giao

**Việc:** `python -m du_an.<ten>.build_ho_so`; skill vn-xuat-autocad chuyển DXF sang DWG và đếm lại
số tờ; PDF xuất bằng `render.save_pdf`. Viết báo cáo theo [checklist-ban-giao.md](checklist-ban-giao.md).

**Cửa:** AutoCAD trả về đúng số tờ; báo cáo liệt kê giả thiết cần duyệt và việc còn treo.
