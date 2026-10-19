---
name: vn-bo-cuc-to-giay
description: "-"
  Tổ chức một bộ hồ sơ CAD chuyên nghiệp trong MỘT file DXF: xếp model space theo lưới ô theo bộ
  môn, mỗi tờ là một Layout A3 có khung tên, cửa sổ nhìn với tỷ lệ riêng, ghi chú và bảng biểu vẽ
  trên giấy. Dùng khi dựng hoặc sửa tờ giấy, thêm hình vào tờ, đổi tỷ lệ, xử lý hình bị cắt trong
  cửa sổ nhìn, hoặc khi bản vẽ "nhìn lộn xộn, không chuyên nghiệp".
---

<!-- portable-host-contract:start -->
## Portable host contract

This skill is shared by Codex and Google Antigravity.

- Use the host's native task tracker. In Codex, use `update_plan`; in Antigravity, maintain the task-list artifact.
- Use native collaboration and user-input tools exposed by the host. Treat legacy tool names as capability descriptions, never literal calls.
- Resolve bundled resources from the project-local `.agents/skills/` tree. Do not create or write a user-global skill directory.
<!-- portable-host-contract:end -->

# Bố cục model space và tờ giấy

Cách hồ sơ thật tổ chức: **một file DXF chứa cả bộ, mỗi tờ là một Layout**. Model space giữ hình
học ở kích thước thật 1:1; tờ giấy chỉ mở cửa sổ nhìn vào đó. Sửa hình một chỗ, mọi tờ tự cập nhật.

## Model space: lưới ô theo bộ môn

```python
CELL_W, CELL_H = 32000.0, 30000.0
PAD_X = 3600.0                         # cho hang kich thuoc doc ben trai

def o(col, row, dx=0.0, dy=0.0):
    """Goc dat cum hinh trong o (col, row). Hang 0 nam tren cung."""
    return (col * CELL_W + PAD_X + dx, -row * CELL_H + dy)

R = {"tang1": o(0, 0, dy=8000), "mat_cat": o(0, 1, dy=12000), "mong": o(0, 3, dy=18000), ...}
O_LUOI = [(0, 0, "KT-01", "Mat bang tang 1 va tang 2"), ...]   # (col, row, so hieu, ten)
```

- Mỗi hàng một bộ môn: mặt bằng; mặt đứng và mặt cắt; chi tiết kiến trúc; kết cấu; cơ điện; sơ đồ.
- Khung ô và nhãn vẽ trên lớp `MODEL-GUIDE` (không in, tự đóng băng trong mọi cửa sổ nhìn).
- Cụm hình rộng hơn một ô thì thu gọn hình (đoạn đường có nét gãy) chứ đừng để lấn sang ô bên.
- Hàm vẽ dùng cao độ tuyệt đối (mặt cắt, mặt đứng): bọc `with place.Placed(msp, ox, oy):`.

Kết quả ở bộ mẫu: model space từ một dải 644m x 35m thành 173m x 150m, zoom ra là đọc được.

## Tờ giấy A3 (420 x 297mm)

| Vùng | Toạ độ trên giấy (mm) |
|---|---|
| Khung vẽ | x 20 đến 410 (lề gáy 20), y 10 đến 287 |
| **Khung tên** | x 250 đến 410, y 10 đến 72: cửa sổ nhìn không được lấn vào |
| Cột ghi chú bên phải | `note_panel(lay, 248, 280, ...)` rộng 155 |
| Cửa sổ trái cao hết tờ | tâm (131, 148.5), cỡ 218 x 273 |
| Cửa sổ nửa trên | tâm (131, 217.5), cỡ 218 x 135, nửa dưới để bảng |

```python
from vncad import render, sheet as sh, table

lay = sh.new_sheet(doc, "KC-01", info, "A3")                 # khung + khung ten
sh.add_view(lay, (131, 217.5), (218, 135), tam_model, "1/100")
sh.add_view(lay, (72, 84), (98, 112), tam_chi_tiet, "1/20")  # tron ty le tren mot to
sh.view_caption(lay, 72, 21, "Chi tiet dai mong M1", "1/20")  # tieu de cho hinh chi tiet
sh.note_panel(lay, 248, 280, "Ghi chu ket cau", NOTES, width=155)
table.draw(lay, (158, 272), cot, dong, "CHU GIAI CT1", row_h=5.0)
sh.drop_default_layouts(doc)
render.save_sheet_set(doc, "out/HoSo.dxf", "out", ten_cac_to)
```

Khung tên phải ghi **đủ mọi tỷ lệ có trên tờ** ("1/100, 1/20"), không chỉ tỷ lệ chính.

## Chọn kích thước và tâm cửa sổ: đo, không đoán

```bash
python ".agents/skills/_shared/vn-autocad/tools/do-o-bao-cum-hinh.py" draw_stair_railing.draw_railing_detail --du-an du_an.<ten>
```

Công cụ in kích thước hình trên giấy theo từng tỷ lệ và tâm ô bao so với gốc đặt. Kích thước
cửa sổ = kích thước thật / mẫu số tỷ lệ, cộng 3 đến 5mm dư. Tâm điền vào `regions.centers()`.
Bộ mẫu từng để chi tiết rộng 199mm trong cửa sổ 196mm nên mất chữ ở mép.

## Chọn tỷ lệ cho vừa mà vẫn đọc được

| Hình | Tỷ lệ | Khi không vừa |
|---|---|---|
| Mặt bằng, mặt đứng, mặt cắt | 1/100 | nhà lớn: mỗi mặt bằng một tờ hoặc A2 |
| Khai triển WC, cầu thang | 1/50 | |
| Chi tiết dầm theo chiều dài | 1/50 | |
| Mặt cắt cấu kiện, nối thép | 1/25 | |
| Nút cấu tạo, chống thấm | 1/20 | nhiều nút: đánh số lớp + bảng chú giải |
| Hình mảnh (lan can) | 1/10 | chỉ vẽ phần cần đọc |
| Sơ đồ nguyên lý | không tỷ lệ | chọn 1/75 hay tương tự cho vừa giấy |
| Tổng mặt bằng | 1/200 | |

Chữ và kích thước trong model phải vẽ theo tỷ lệ của cửa sổ **sẽ nhìn nó**:
`S.th("note", "1/20")` cho hình xem ở 1/20, để trên giấy chữ luôn cao đúng 2mm.

## Kiểm tra

`sh.check_views(doc, ten_cac_to, "A3")` bắt cửa sổ chồng nhau, lấn khung tên, lọt khung.
`qc.check_cell_bleed(...)` bắt hình tràn ô. Cả hai nằm trong bộ soát của skill `vn-soat-ban-ve`.
Bảng biểu và ghi chú trên giấy **chưa được soát tự động**: render ra và nhìn.
