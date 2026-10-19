---
description: Dựng một bộ hồ sơ bản vẽ thi công nhà ở mới, từ mô tả công trình tới DXF, DWG và dự toán
argument-hint: <mô tả nhà, ví dụ "nhà phố 4x16m, 3 tầng, 3 phòng ngủ, có gara">
---

Dựng bộ hồ sơ bản vẽ thiết kế thi công cho công trình sau: $ARGUMENTS

Làm theo đúng skill `vn-quy-trinh-ho-so`:

1. Bắt đầu bằng bước 1: lập bảng thông số từ mô tả trên, hỏi những ô còn thiếu (đặc biệt địa chất,
   chiều cao tầng, công năng từng tầng), mỗi câu hỏi kèm 2 đến 4 phương án. Chờ người dùng xác nhận.
2. Nói rõ ngay từ đầu: bộ hồ sơ là bản triển khai theo cấu tạo, cần người có chứng chỉ hành nghề
   kiểm tra và ký trước khi xin phép hay thi công.
3. Tạo dự án mới bằng cách chép hồ sơ mẫu `${CLAUDE_PLUGIN_ROOT}/examples/nha_pho_5x18m`
   vào `du_an/<ten_du_an>`, rồi sửa dữ liệu theo thông số đã chốt.
4. Đi lần lượt các bước 2 đến 11, qua cửa soát của từng bước. Giao việc song song cho các agent
   `kien-truc-su`, `ky-su-ket-cau`, `ky-su-co-dien` khi phù hợp, và `kiem-soat-ban-ve` ở bước 9.
5. Bàn giao theo checklist của skill, kèm đường dẫn tuyệt đối mọi file và danh sách giả thiết cần duyệt.
