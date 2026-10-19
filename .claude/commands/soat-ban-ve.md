---
description: Soát toàn bộ một bộ hồ sơ bản vẽ (tự động, bằng mắt, kỹ thuật) và báo lỗi kèm cách sửa
argument-hint: <gói dự án, ví dụ du_an.nha_ong_4x16; bỏ trống để soát hồ sơ mẫu>
---

Soát bộ hồ sơ `$ARGUMENTS` (bỏ trống thì dùng hồ sơ mẫu `examples.nha_pho_5x18m`).

Làm theo skill `vn-soat-ban-ve`, đủ ba tầng:

1. Chạy `python ".claude/skills/_shared/vn-autocad/tools/soat-ban-ve-khong-render.py" --du-an <goi>`.
2. Render và nhìn các tờ (150 dpi toàn tờ, 400 dpi cho vùng chi tiết thép).
3. Soát kỹ thuật: thép đúng lớp, số thanh khớp bước, lớp bảo vệ, hàm lượng thép, dự toán khớp bảng.

Có thể giao cho agent `kiem-soat-ban-ve` để soát độc lập. Chỉ báo cáo, không tự sửa bản vẽ trừ khi
người dùng yêu cầu. Báo cáo theo mẫu trong skill.
