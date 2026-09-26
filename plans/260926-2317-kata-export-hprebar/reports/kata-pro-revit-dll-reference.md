# Tham khảo DLL Revit của Kata Pro (2026-09-26)

Nguồn: `F:\3-SOFT\OneDrive\PROGRAM\KATA\Update2025\` (Kata Pro, bản cập nhật 2025).

Cách đọc: chỉ đọc chuỗi ASCII/UTF-16 của file, **không load, không chạy, không decompile, không chép code**. Các DLL đều bị obfuscate: `System.Reflection.Metadata` báo "Illegal tables in compressed metadata stream", user string bị mã hoá. Vì vậy mọi kết luận dưới đây chỉ **suy từ tên hàm/lệnh** — [chưa xác minh hành vi].

## Các file

| File | Vai trò (suy từ tên) |
|---|---|
| `Addins/<2020..2024>/Revit_Kata_Call.dll` + `.addin` | Ribbon "Kata Tools" trong Revit. Nạp DLL theo phiên bản. Lệnh: `Beam_Command` → `.Export_Kata_beam`, `Column_Command` → `.Export_Rebar_ColumnWall`, `Floor_Command` → `.Export_Rebar_Slab`, móng băng/cọc/đơn, thang, `Build_model_from_cad`, `Export_structure_plan_to_cad`, `Load_family` |
| `Revit_Kata2025.dll` (nội bộ `Revit_Kata2023`) | **Dầm Revit → Kata:** `Export_Kata_beam`, `xuat_excel_dam`, `info_dam_revit`, `xuly_nhip_cot_dam`, `xuly_dam_giao_dam`, `xuly_loi_model_nhip_dam`, `cong_xon`, `Get_dam_giao_columnwall`, `GetManyBeamkataByRectangle`, `gan_ten_dam`, `chon_1_Column_wall`, `joint_walls`, `info_revit_slab`, `info_revit_grid(_Arc)`, `Xuyen_dam`; kèm form `Setting`, `Level_ColumnWall`, `Frm_Setup_Tag` |
| `Revit_Thong2025.dll` | Vẽ thép/mặt cắt/dim/tag **vào** Revit: `create_rebar*`, `ve_thep_mong_bang`, `ve_mong_coc`, `ve_thep_cot`, `tao_section`, `tao_dim_tu_tap_XYZ*`, form thép dầm (`tbThepChuTren/Duoi`, `tbDamXienDau/Cuoi`, `tb_day_san_trai/phai`) |

## Điểm đáng dùng cho KataExport (hành vi, không phải code)

| Quan sát (tên trong DLL) | Ý nghĩa với plan | Đề xuất |
|---|---|---|
| `chon_1_Column_wall`, `Get_dam_giao_columnwall`, `joint_walls`, `Level_ColumnWall` | Kata coi **vách (wall) là gối** giống cột ("ColumnWall") | Thêm `OST_Walls` kết cấu làm gối kind Column (P3). Dynamo không làm |
| `dgiao_daucongxong_la_dchinh`, `dgiao_giuanhip_la_dchinh` | Dầm giao ở đầu console / giữa nhịp được coi là dầm chính, tức là **gối** | Khớp thiết kế hiện tại (dầm giao = gối kind Beam) |
| `xuly_loi_model_nhip_dam` | Có bước xử lý lỗi model (nhịp hở, chồng) | Khớp `Warnings` của core |
| `tb_day_san_trai`, `tb_day_san_phai` | Chiều dày sàn **hai bên** dầm | B7 là một ô → lấy sàn dày hơn trong hai bên, ghi cảnh báo khi hai bên khác nhau (P3) |
| `info_revit_grid_Arc` | Có xử lý lưới cong | Plan hiện chỉ xét lưới thẳng → ghi rõ giới hạn |
| `GetManyBeamkataByRectangle` | Xuất nhiều dải dầm một lần (quét chọn) | Ngoài phạm vi hiện tại (1 dải/lần, như Dynamo) |

## Hệ quả

- **Kata Pro đã có sẵn lệnh Revit → sheet `Dam`** (`Export_Kata_beam`). Nếu lệnh này dùng được trên máy người dùng, nó là **nguồn golden chuẩn nhất** cho P6, vì nó do chính tác giả Kata viết. Nó cũng đặt câu hỏi có nên tiếp tục tự xây tool riêng hay không.
- ⚠️ **Bảo mật (của nhà cung cấp, không phải repo này):** `Revit_Kata_Call.dll` chứa chuỗi kết nối SQL Server có tài khoản và mật khẩu ghi thẳng trong file (dùng cho kiểm tra cập nhật/bản quyền). Không chép giá trị đó vào repo, report hay commit nào.
