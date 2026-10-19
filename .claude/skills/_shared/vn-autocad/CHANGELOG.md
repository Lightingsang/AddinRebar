# Nhật ký thay đổi

## 1.0.0 (11/09/2026)

Bản đầu tiên.

- **Plugin Claude Code:** 8 skill theo công đoạn (quy trình, kiến trúc, kết cấu, cơ điện, bố cục tờ
  giấy, soát bản vẽ, dự toán, xuất AutoCAD), 5 agent theo vai, 3 lệnh (`/ho-so-moi`,
  `/soat-ban-ve`, `/xuat-dwg`).
- **Thư viện `vncad`:** quy chuẩn đồ hoạ rút từ 34 bộ bản vẽ thi công thật; kiến trúc, kết cấu, cơ
  điện, ghi chú, tờ giấy nhiều Layout, render DXF, PNG, PDF, dự toán Excel, kết nối AutoCAD.
- **Hồ sơ mẫu 26 tờ** nhà phố 5x18m, 3 tầng, trong một file DXF; model space xếp theo lưới ô
  theo bộ môn.
- **Bộ soát bốn phép:** cửa sổ nhìn, chữ đè chữ, chữ bị nét vẽ cắt, hình tràn ô lưới. Bề rộng chữ
  đo trên phông thật, có tính góc xoay.
- **Bảng thống kê cốt thép sinh từ kích thước** cấu kiện (sàn, móng, cầu thang); dự toán lấy khối
  lượng thép từ đó.
- **Chuyển DWG** ghi đè được cả khi file đích đang mở trong AutoCAD.
- **Công cụ** soát không render, chỉ ra nét cắt chữ, đề xuất chỗ đặt chữ, đo ô bao; dùng cho mọi
  dự án qua `--du-an`.
- **22 bài kiểm thử**, CI Linux và Windows.
