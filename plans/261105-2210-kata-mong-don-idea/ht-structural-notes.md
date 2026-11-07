# Ghi chú tham khảo HT-Structural (Tới Hoàng)

Đọc 2026-11-05 từ kênh công khai https://www.youtube.com/@ht_structural (18 video): tiêu đề + mô tả video.

**Không có trong nguồn này:** code, công thức, luật bố trí thép. Không xem nội dung video: phụ đề bị YouTube chặn với client đọc tự động, blog `ht-structural.blogspot.com` chuyển sang đăng nhập Blogger, Facebook cần đăng nhập. Chỉ học tính năng và quy trình, không sao chép.

## Chưa thấy trong nguồn
- Tool riêng cho **móng đơn**: tìm "móng đơn" trong kênh ra 0 video. Chỉ thấy **Móng Cọc** và **cọc khoan nhồi**.

## Tính năng ghi nhận (từ nhật ký cập nhật)
| Nhóm | Tính năng | Video |
|---|---|---|
| Chuỗi làm việc | Dựng khung → sơ đồ tính → tải → FEM 3D → tính thép → bố trí thép tự động vào Revit | OQk8jYGph6E |
| Dữ liệu | Tool tính thép đẩy dữ liệu sang tool vẽ; tool vẽ đọc thông tin thiết kế; xuất nhập Excel | PG6HpWzw9QI, 9uFDXD1ZL8s |
| Móng | Tool vẽ thép Móng Cọc nhận móng là family lồng nhau | S-_Xo9rxVRg |
| Ghi chú | Tool ghi chú mặt cắt ngang + mặt bằng Móng Cọc tách khỏi tool vẽ thép; có dim, đặt 2D cọc | S-_Xo9rxVRg |
| Sheet | Tool chèn View vào Sheets nhanh | ZQ5B5MGB-AQ |
| View | Auto mặt cắt dọc + ngang Dầm, chọn tiêu đề view | s1-4kBIVzqg |
| Thống kê | Bảng thống kê thép, tạo ảnh hình dạng thép, đếm số cây cho nhập tay | HSNliNOxg1k, s1-4kBIVzqg |
| Giao diện | Mặt bố trí xem trước, check box hiện thép chủ, cấu hình lưu theo tầng | HSNliNOxg1k, PG6HpWzw9QI, s1-4kBIVzqg |

## Gợi ý cho tool móng đơn (suy luận của Claude, không phải điều nguồn nói)
- Tách 3 bước chạy độc lập: vẽ thép → ghi chú view (dim, tag) → chèn view vào sheet.
- Đọc móng là family lồng nhau ngay từ đầu; đây là lỗi nguồn phải sửa sau khi phát hành.
- Có mặt bố trí xem trước trước khi tạo thép.
- Lưu cấu hình dùng lại cho nhiều móng; nhận dữ liệu thiết kế qua Excel.
- Dim dễ mất khi tạo lại view (nguồn có sửa lỗi mất dim): cần test chạy lại.
