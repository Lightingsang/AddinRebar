# 25 bài học rút ra khi dựng bộ hồ sơ mẫu

Mỗi dòng là một lỗi thật đã gặp, kèm nguyên nhân và cách làm đúng. Đọc trước khi bắt đầu một
dự án mới để khỏi vấp lại.

## Bố cục

1. **Model space xếp tuỳ tiện thành một dải 644m x 35m**, zoom ra không đọc được gì.
   Xếp theo lưới ô 32m x 30m, hàng theo bộ môn: còn 173m x 150m, nhìn là thấy bố cục.
2. **Hàm vẽ mặt cắt dùng cao độ tuyệt đối**, bỏ qua gốc đặt theo trục đứng, nên mặt cắt thang vẽ
   đè lên mặt bằng thang. Bọc bằng `place.Placed(msp, 0, oy)` để dời cả cụm.
3. **Tâm cửa sổ nhìn đoán bằng mắt** làm chi tiết nút khung (rộng 199mm) bị cắt trong cửa sổ 196mm.
   Luôn đo bằng `tools/do-o-bao-cum-hinh.py` rồi mới đặt kích thước và tâm.
4. **Ba chi tiết chống thấm ở 1/10 cần 450mm** mà khổ A3 chỉ còn 273mm. Chuyển 1/20, đánh số
   lớp trên hình, diễn giải trong bảng chú giải riêng như hồ sơ thật.
5. **Sơ đồ tủ điện ở 1/50 cao gần 16m**, tràn khổ. Sơ đồ nguyên lý không theo tỷ lệ hình học,
   chọn 1/75 cho vừa giấy mà chữ vẫn 2mm.
6. **Hình mảnh và cao** (lan can kính 12mm) ở 1/20 chỉ còn 0,6mm trên giấy. Vẽ 1/10 và chỉ vẽ
   phần cần đọc (một bậc).
7. **Mặt cắt sàn vẽ đủ L/4 dài gần 4m mà dày 100** thành một vạch mờ. Cắt ngắn, đánh ký hiệu
   cắt ở hai đầu, ghi chiều dài thật trong chú giải.

## Chữ

8. **Ước bề rộng chữ bằng hệ số 0,60** thiếu một phần ba ("MAT BANG TANG 1" cao 2,5mm ước 22,5mm,
   thật 33,5mm) nên bỏ sót chỗ chồng. Đo bằng `standards.rong_chu` trên chính file phông.
9. **Ô bao chữ không tính góc xoay** làm nhãn dầm đứng bị tính nằm ngang: 11 báo động giả.
10. **Nhãn dầm đặt ngay trên trục** nên nét biên dầm cắt qua chữ; nhãn dầm chạy suốt đặt giữa
    chiều dài thì rơi đúng vào trục ngang. Đẩy nhãn vuông góc với dầm, đặt vào giữa một nhịp.
11. **Mã hiệu đài móng đặt thẳng trên đỉnh đài** nên giằng móng chạy qua chữ. Đặt ở góc đài.
12. **Nhãn phòng hẹp** ("BAN CONG" rộng 1,9m trong ban công 1,5m) tràn sang hai tường. Xoay 90 độ.
    Nhãn WC bị cung quét cánh cửa cắt: dời lên góc xa cửa.
13. **Mạch gạch chạy xuyên qua mã vật liệu**, và phòng lớn trải gạch đè cả phạm vi phòng nhỏ lọt
    trong nó. Ngắt mạch quanh MỌI nhãn của cả tầng (`tiles.floor_tiles(chua=[...])`).
14. **Nét chỉ vòng tròn đánh số xuất phát từ tâm** nên gạch ngang qua chính con số. Xuất phát từ viền
    (`annotate.so_lop`).
15. **`rebar_line` tuyến 2 điểm đặt ghi chú ở `pts[1]`**, tức đầu cuối, đè lên dầm. Lấy trung điểm thật.
16. **Bộ soát có điểm mù:** chữ nằm trong block kích thước, hai chữ chạm mép dưới ngưỡng 18%.
    Hai lỗi loại này chỉ tìm được khi render và nhìn.

## Kỹ thuật

17. **Thép bản thang vẽ ngược lớp**: hàm tính khoảng cách từ mặt trên bị đảo, thép chịu lực chạy suốt
    nằm lớp trên còn thép mũ nằm lớp dưới. Bộ soát không bắt được; phải soát lớp thép bằng mắt.
18. **Bảng thép sàn chỉ ghi một phương**, không có thép mũ, thiếu khoảng hai phần ba khối lượng sàn.
    Sinh bảng từ kích thước ô sàn theo đúng chi tiết đã vẽ.
19. **Số thanh không khớp bước ghi trên hình**: hình ghi Ø8a200 mà bảng tính 22 thanh (tức a150).
20. **Hàm vẽ mặt cắt cố định lớp bảo vệ 25mm** mâu thuẫn ghi chú móng 50mm. Thêm tham số `cover`.
21. **Dầm phụ chạy xuyên giữa một ô sàn**: bộ soát báo "D3 đè S1", thực chất là lỗi thiết kế. Tách ô.

## Dự toán

22. **Khối lượng gõ tay lệch bản vẽ rất xa**: thép khung 13.700 kg so với 7.217,6 kg tính từ bản vẽ;
    thép móng 3.200 so với 748,9 kg; bê tông móng 38 m³ so với khoảng 12 m³. Luôn sinh từ bảng
    thống kê và soát chéo hàm lượng.

## AutoCAD

23. **Lệnh CLOSE một bản vẽ chưa lưu từ LISP** bật hộp thoại "Save changes?" và treo AutoCAD.
    Chỉ đóng bằng `vla-Close d :vlax-false`, không bao giờ gọi CLOSE.
24. **Không in PDF qua MCP được**: LISP gọi qua MCP luôn chạy trong ngữ cảnh một tài liệu cố định,
    không đổi được tài liệu hoạt động. Xuất PDF bằng `render.save_pdf` bên Python.
25. **Ghi đè DWG đang mở báo lỗi**: phải đóng tài liệu trùng tên trước; AutoCAD trả đường dẫn gạch
    ngược nên phải đổi về cùng kiểu mới so được; nếu chính tài liệu đang chạy LISP là file đích thì
    cho nó "lưu thành" file giữ chỗ để nhả tên. Đã gói trong `acad.lisp_open_and_convert`.
