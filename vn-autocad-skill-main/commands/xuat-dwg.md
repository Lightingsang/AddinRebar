---
description: Chuyển bộ hồ sơ DXF sang DWG qua AutoCAD MCP và đếm lại số tờ
argument-hint: <đường dẫn file .dxf> [đường dẫn file .dwg đích]
---

Chuyển bộ hồ sơ sang DWG: $ARGUMENTS

Làm theo skill `vn-xuat-autocad`:

1. Kiểm tra có AutoCAD MCP đang chạy (thao tác health). Không có thì nói rõ, đề nghị dùng file DXF
   hoặc cài AutoCAD MCP; không tự xoay cách khác.
2. Sinh LISP bằng `vncad.acad.lisp_open_and_convert(dxf, dwg)`; nếu không có đích thì đặt DWG
   cùng tên trong thư mục `dwg` cạnh file DXF.
3. Chạy qua `execute_lisp`, rồi đếm lại số Layout trong DWG và so với số tờ trong DXF.
4. Báo kết quả kèm đường dẫn tuyệt đối. PDF thì xuất bằng `vncad.render.save_pdf`, không in qua MCP.
