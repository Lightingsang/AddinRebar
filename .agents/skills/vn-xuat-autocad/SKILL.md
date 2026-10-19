---
name: vn-xuat-autocad
description: "-"
  Đưa bộ hồ sơ DXF vào AutoCAD qua AutoCAD MCP: chuyển sang DWG (ghi đè được cả khi file đang
  mở), đếm lại số tờ, dọn rác bản vẽ; xuất PDF bằng Python. Dùng khi người dùng cần file DWG,
  cần mở bản vẽ trong AutoCAD, gặp lỗi "Error saving the document", AutoCAD treo ở hộp thoại lưu,
  hoặc cần PDF để in.
---

<!-- portable-host-contract:start -->
## Portable host contract

This skill is shared by Codex and Google Antigravity.

- Use the host's native task tracker. In Codex, use `update_plan`; in Antigravity, maintain the task-list artifact.
- Use native collaboration and user-input tools exposed by the host. Treat legacy tool names as capability descriptions, never literal calls.
- Resolve bundled resources from the project-local `.agents/skills/` tree. Do not create or write a user-global skill directory.
<!-- portable-host-contract:end -->

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
   chỗ `_giu_cho_ten_file.dwg` trong `acad.IPC_DIR` để nhả tên.
3. Mở DXF, zoom vừa khung, lưu DWG (định dạng 2018).

Sau đó **luôn đếm lại số tờ** để xác nhận, không tin vào chữ "CONVERTED":

```lisp
(setq n 0)
(vlax-for x (vla-get-Layouts d) (if (/= (strcase (vla-get-Name x)) "MODEL") (setq n (1+ n))))
(princ (strcat "SO TO: " (itoa n)))
```

Nhiều file một lần: `acad.lisp_convert_batch(ten_cac_file, thu_muc_dxf, thu_muc_dwg)`.
Dọn rác và sửa lỗi trước khi giao: `acad.lisp_purge_and_audit()`.

## Qua HPAutoCad MCP của repo này (không có `execute_lisp`)

Server `hprebar-autocad` chạy C# chứ không chạy AutoLISP. Script dưới đây đọc DXF vào một
`Database` phụ, đếm tờ, rồi lưu thành DWG. Nó không mở bản vẽ mới và không đụng bản vẽ đang mở.
Đã chạy trong AutoCAD 2026 ngày 2026-10-20 với DXF bộ mẫu: ra 26 tờ, 0,25 s. Đọc lại DWG thấy
định dạng AC1032, đủ 26 Layout từ KC-01 đến ME-06, model space có 3 548 đối tượng.

1. `get_autocad_context`: bridge phải kết nối và `executionEnabled` phải là true. Nếu chưa,
   người dùng mở `HPMCPBRIDGE` và tick *Allow AI code execution*.
2. Gọi `execute_autocad_code` với `transaction: "none"`, `args` = `{"dxf": "<đường dẫn .dxf tuyệt đối>",
   "dwg": "<đường dẫn .dwg tuyệt đối>"}`. File đích không được đang mở trong AutoCAD.

```csharp
var dxf = args.Require("dxf");
var dwg = args.Require("dwg");
var side = new Database(false, true);
try
{
    side.DxfIn(dxf, null);
    int sheets = 0;
    using (var dict = (DBDictionary)side.LayoutDictionaryId.Open(OpenMode.ForRead))
        foreach (DBDictionaryEntry e in dict)
            if (!string.Equals(e.Key, "Model", StringComparison.OrdinalIgnoreCase)) sheets++;
    side.SaveAs(dwg, DwgVersion.Current);
    return new { dwg, sheets };
}
finally { side.Dispose(); }
```

Không dùng `StartTransaction` (guard chặn). `Open` cũ đủ để đọc từ điển Layout của database phụ.
So `sheets` với số tờ trong danh mục.

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

- `acad.send_command(...)`: gửi lệnh qua thư mục IPC `acad.IPC_DIR`, cần AutoCAD đã nạp `mcp_dispatch.lsp`.
- `acad.write_script(scr, dxf, dwg)`: tạo file `.scr` chạy ngầm bằng `accoreconsole.exe /i <dxf> /s <scr>`.
