---
name: vn-xuat-autocad
description: >-
  Đưa bộ hồ sơ DXF vào AutoCAD qua AutoCAD MCP: chuyển sang DWG (ghi đè được cả khi file đang
  mở), đếm lại số tờ, dọn rác bản vẽ; xuất PDF bằng Python. Dùng khi người dùng cần file DWG,
  cần mở bản vẽ trong AutoCAD, gặp lỗi "Error saving the document", AutoCAD treo ở hộp thoại lưu,
  hoặc cần PDF để in.
---

# Xuất sang AutoCAD

## Cần gì

- **AutoCAD bản đầy đủ trên Windows.** Các hàm `vla-*` là ActiveX, không có trên AutoCAD LT
  và bản Mac.
- **Một AutoCAD MCP server** có thao tác chạy AutoLISP (`execute_lisp`), ví dụ backend File IPC.
- Không có AutoCAD thì file DXF vẫn mở được trong mọi phần mềm CAD; chỉ bước chuyển DWG là cần.

## Chuyển DXF sang DWG

```python
from vncad import acad
lisp = acad.lisp_open_and_convert("out/HoSo.dxf", "out/dwg/HoSo.dwg")
```

Đưa chuỗi `lisp` cho tool của AutoCAD MCP (thao tác `execute_lisp`). Đoạn LISP tự:

1. Duyệt các bản vẽ đang mở, **đóng bản vẽ nào trùng tên file đích** (không lưu). AutoCAD trả
   đường dẫn gạch ngược, thư viện dùng gạch xuôi: đã đổi về cùng kiểu trước khi so.
2. Nếu chính bản vẽ đang chạy LISP lại là file đích (không đóng được), cho nó "lưu thành" file giữ
   chỗ `C:/temp/_giu_cho_ten_file.dwg` để nhả tên.
3. Mở DXF, zoom vừa khung, lưu DWG (định dạng 2018).

Sau đó **luôn đếm lại số tờ** để xác nhận, không tin vào chữ "CONVERTED":

```lisp
(setq n 0)
(vlax-for x (vla-get-Layouts d) (if (/= (strcase (vla-get-Name x)) "MODEL") (setq n (1+ n))))
(princ (strcat "SO TO: " (itoa n)))
```

Nhiều file một lần: `acad.lisp_convert_batch(ten_cac_file, thu_muc_dxf, thu_muc_dwg)`.
Dọn rác và sửa lỗi trước khi giao: `acad.lisp_purge_and_audit()`.

## PDF

**Không in PDF qua MCP được.** Lệnh in tác động lên bản vẽ đang hoạt động, mà LISP gọi qua MCP luôn
chạy trong ngữ cảnh một bản vẽ cố định; `vla-Activate` báo "Drawing is busy". Xuất PDF bằng Python:

```python
from vncad import render
render.save_pdf(doc, "out/KT-01.pdf", sheet="A3")
```

## Sự cố thường gặp

| Hiện tượng | Nguyên nhân | Cách xử lý |
|---|---|---|
| AutoCAD treo, không trả lời | hộp thoại "Save changes?" do lệnh CLOSE bản vẽ chưa lưu | người dùng bấm No; từ nay chỉ đóng bằng `vla-Close d :vlax-false` |
| "Error saving the document" | file đích đang mở | dùng `lisp_open_and_convert` bản hiện tại (tự nhả khoá) |
| Trả về `BUSY` ở bản cũ | chính bản vẽ đang chạy LISP là file đích | bản hiện tại tự lưu sang file giữ chỗ |
| Chữ tiếng Việt vỡ khi đọc DWG cũ | file gốc mã hoá VNI-Windows | đọc cp1252 rồi ánh xạ VNI sang Unicode |

## Không có MCP

- `acad.send_command(...)`: gửi lệnh qua thư mục IPC `C:/temp`, cần AutoCAD đã nạp `mcp_dispatch.lsp`.
- `acad.write_script(scr, dxf, dwg)`: tạo file `.scr` chạy ngầm bằng `accoreconsole.exe /i <dxf> /s <scr>`.
