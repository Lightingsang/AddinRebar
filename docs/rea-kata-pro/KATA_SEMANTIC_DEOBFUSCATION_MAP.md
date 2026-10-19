# BẢN ĐỒ GIẢI MÃ NGỮ NGHĨA ĐỊNH DANH BỊ XÁO TRỘN & THÂN HÀM CIL (`C:\kata_pro`)
## (REA 6.6.0 Semantic Deobfuscation & CIL Member Recovery Map)

- **Công cụ kiểm toán**: **REA 6.6.0** (`rea@6.6.0` / Node.js `v24.18.0`) kết hợp chuẩn hóa luồng siêu dữ liệu ECMA-335 tạm thời và lọc `--type <Module>`
- **Mục tiêu**: Giải mã toàn bộ `Kata_pro64.dll`, 5 DLL CAD lớn (`Kata_pro64_Cad2025.dll`, `Cad2013`, `GstarCad`, `GstarCad2025`, `ZwCad`), ánh xạ các định danh bị làm rối (`Class0..`, `smethod_0..`, tên băm SmartAssembly/Babel) sang vai trò kỹ thuật thực tế, và giải mã cấu trúc PE bản địa của `kata_pro.dll`.
- **Tính toàn vẹn dữ liệu gốc**: **100% READ-ONLY** trên `C:\kata_pro\` (mọi thao tác chuẩn hóa header `BSJB` chỉ thực hiện trên bản sao tạm thời trong `%TEMP%` và đã xóa sạch sau khi thu thập `evidence_id`).

---

## 1. Giải mã hoàn chỉnh `Kata_pro64.dll` qua Chuẩn hóa Header `BSJB` Tạm thời (`num_streams: 9 -> 5`)

### 1.1 Cơ chế bẫy Obfuscator và Phẫu thuật Header ECMA-335
- **Tệp gốc**: `C:\kata_pro\Kata_pro64.dll` (`3,415,040` bytes, SHA-256: `5da9a37be8fea727da725df05cc89108b5fe3e6071958b012e3fcc2441b01b44`)
- **Vị trí Metadata Root (`BSJB`)**: `file_offset = 2555068` (`0x26FDBC`), chuỗi phiên bản CLR `v2.0.50727` (`ver_len = 12`).
- **Phát hiện pháp y**: Tại `file_offset = 2555102` (`BSJB + 34`), trường `NumberOfStreams` bị bộ đóng gói (Dotfuscator/SmartAssembly) ghi giá trị `9` thay vì `5`:
  - **5 luồng ECMA-335 hợp lệ (`Streams 0..4`)**:
    1. `Stream 0`: `#~` (`offset = 108`, `size = 161,412` bytes) — Bảng siêu dữ liệu nén.
    2. `Stream 1`: `#Strings` (`offset = 161,520`, `size = 651,880` bytes) — Heap tên định danh.
    3. `Stream 2`: `#US` (`offset = 813,400`, `size = 4` bytes) — Heap chuỗi người dùng.
    4. `Stream 3`: `#GUID` (`offset = 813,404`, `size = 16` bytes) — GUID hợp lệ của module.
    5. `Stream 4`: `#Blob` (`offset = 813,420`, `size = 44,096` bytes) — Heap chữ ký nhị phân.
  - **4 luồng bẫy giả mạo (`Streams 5..8`)**:
    6. `Stream 5`: `#GUID` (`offset = 857,516`, `size = 16` bytes, header tại `file_offset = 2555180`) — Luồng trùng tên gây lỗi `Duplicate metadata stream #GUID`.
    7. `Stream 6`: `#Strings` (`offset = 857,532`, `size = 12` bytes) — Luồng trùng tên thứ hai.
    8. `Stream 7`: `#Blob` (`offset = 857,544`, `size = 8` bytes) — Luồng trùng tên thứ ba.
    9. `Stream 8`: `#Schema` (`offset = 857,552`, `size = 4` bytes) — Luồng rác kết thúc.
- **Giải pháp chuẩn hóa không xâm phạm**: Tạo bản sao tạm thời trong `%TEMP%`, chỉnh sửa đúng 2 byte `NumberOfStreams` tại `file_offset = 2555102` từ `0x0009` thành `0x0005` (SHA-256 bản sao chuẩn hóa: `7f5ed1a4fd1bc81f9ae14fd33d4ed02dd4f8201d9035146643944f1c8a518107`), sau đó chạy trực tiếp **REA 6.6.0**.

### 1.2 Bằng chứng và Kết quả Giải mã `Kata_pro64.dll` từ REA 6.6.0
- `inspect-managed-artifact`: `ev_acde17b59c751adb77d47c9b713981365c4cbac98bf6fc4612f5a3015c5faa58` (`status: "managed"`, `.NET v2.0.50727`, `x86`)
- `inspect-managed-members`: `ev_ccc2e9e991f329df73ff4acd3bb0a520f54169cec0c0bf6a63bf89dbe5f75c37`
- `inspect-managed-native-boundaries`: `ev_606175309220230a9c487b1120b7924ea84d5e5cb2225c69ac868e0d8e607bb7`
- `inspect-pe-resources`: `ev_391ba6f0a7edb4da66c0ae0f87937163643f2d874e77a7d95bacd00bd41ff1d5`

| Hạng mục Siêu dữ liệu (`Kata_pro64.dll`) | Số lượng đã giải mã | Chi tiết kỹ thuật |
|---|---:|---|
| Tổng số kiểu (`TypeDef`) | **166** | Bao gồm các lớp nghiệp vụ (`Class1`..`Class98`, `Rebar`, `Info_rebar`, `Design_Beam`, `Kata_form`...) |
| Tổng số phương thức (`MethodDef`) | **4,720** | Toàn bộ chữ ký hàm và thân hàm quản lý |
| Tổng số trường (`FieldDef`) | **1,613** | Biến trạng thái, cấu hình, tham chiếu UI Controls |
| Tham chiếu thành viên (`MemberRef`) | **1,212** | Gọi hàm sang AutoCAD COM (`AutoCAD.Interop`), Excel (`Microsoft.Office.Interop.Excel`), WinForms |
| Tham chiếu kiểu ngoài (`TypeRef`) | **258** | Kiểu dữ liệu từ `mscorlib`, `System.Windows.Forms`, `System.Drawing`, `Microsoft.VisualBasic` |
| Tham số phương thức (`Param`) | **2,756** | Đầy đủ tên và kiểu tham số |
| Thuộc tính tùy biến (`CustomAttribute`) | **2,931** | Thuộc tính trình biên dịch VB.NET / C# và COM Interop |
| Tài nguyên nhúng (`ManifestResource`) | **30** | Biểu mẫu `.resources` và khối dữ liệu mã hóa nội bộ |
| Phụ thuộc Assembly (`AssemblyRef`) | **13** | `mscorlib`, `Microsoft.VisualBasic`, `System.Data`, `System.Windows.Forms`, `Autodesk.AutoCAD.Interop`... |
| Cạnh đồ thị gọi hàm (`CallEdges`) | **55** | Các luồng gọi tĩnh trực tiếp ngoài wrapper giải mã runtime |
| Truy cập trường (`FieldAccesses`) | **60** | Các lệnh đọc/ghi trường tĩnh cấp module |

#### Danh sách các lớp nghiệp vụ công khai & tài nguyên biểu mẫu (`.resources`) trong `Kata_pro64.dll`:
- Lớp nghiệp vụ / Form: **`Kata_pro64.My.MySettings`**
- Lớp nghiệp vụ / Form: **`Kata_pro64.Cat_thep_thay_doi`**
- Lớp nghiệp vụ / Form: **`Kata_pro64.Check_time_run`**
- Lớp nghiệp vụ / Form: **`Kata_pro64.Customer`**
- Lớp nghiệp vụ / Form: **`Kata_pro64.Danh_ten`**
- Lớp nghiệp vụ / Form: **`Kata_pro64.Edit_nhiu_thep`**
- Lớp nghiệp vụ / Form: **`Kata_pro64.Edit_thep`**
- Lớp nghiệp vụ / Form: **`Kata_pro64.Edit_tk_thep`**
- Lớp nghiệp vụ / Form: **`Kata_pro64.Info_rebar`**
- Lớp nghiệp vụ / Form: **`Kata_pro64.Input1`**
- Lớp nghiệp vụ / Form: **`Kata_pro64.Input_select`**
- Lớp nghiệp vụ / Form: **`Kata_pro64.Input_slab`**
- Lớp nghiệp vụ / Form: **`Kata_pro64.In_off`**
- Lớp nghiệp vụ / Form: **`Kata_pro64.Lanh_to`**
- Lớp nghiệp vụ / Form: **`Kata_pro64.QS_form`**
- Lớp nghiệp vụ / Form: **`Kata_pro64.Round_thep_chay`**
- Lớp nghiệp vụ / Form: **`Kata_pro64.setting_tk_thep_bvbs`**
- Lớp nghiệp vụ / Form: **`Kata_pro64.Tim_thep`**
- Lớp nghiệp vụ / Form: **`info_sh_vitri_thep`**
- Lớp nghiệp vụ / Form: **`Kata_pro64.TK_kieu_xuat`**
- Lớp nghiệp vụ / Form: **`Kata_pro64.TK_san`**
- Lớp nghiệp vụ / Form: **`Kata_pro64.TK_ten`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.setting_tk_thep_bvbs.resources`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.Edit_nhiu_thep.resources`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.Input_slab.resources`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.Cat_thep_thay_doi.resources`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.Info_rebar.resources`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.Info_slab.resources`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.Setting_atlas.resources`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.In_off.resources`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.Edit_tk_thep.resources`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.TK_kieu_xuat.resources`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.User_kho_thep.resources`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.Check_time_run.resources`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.TK_san.resources`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.view_san.resources`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.view_mong_don.resources`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.Input1.resources`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.Tim_thep.resources`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.Edit_thep.resources`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.TK_ten.resources`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.Customer.resources`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.Main_rebar_Column.resources`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.QS_form.resources`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.Setting.resources`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.Input_select.resources`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.Danh_ten.resources`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.Lanh_to.resources`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.view_mat_cat_dam.resources`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.view_thang.resources`**
- Tài nguyên nhúng ManifestResource: **`Kata_pro64.Round_thep_chay.resources`**
- Tài nguyên nhúng ManifestResource: **`j=;c(U/c?7>|\[Y %so'UU\,\]f.resources`**

---

## 2. Giải mã Bảng Siêu dữ liệu & Tham chiếu Thành viên của 5 DLL CAD Lớn qua REA 6.6.0 (`--type <Module>`)

### 2.1 Nguyên nhân Gốc rễ của Lỗi `67108864 byte representation budget` và Cách Vượt qua Chính thức bằng REA 6.6.0
- Trong mã nguồn của REA 6.6.0 (`ManagedSnapshot-CPE9C3OD.js`), hằng số `MAX_METADATA_REPRESENTATION_BYTES = 64 * 1024 * 1024` (`67,108,864` bytes) kiểm soát tổng dung lượng biểu diễn JSON khi giải mã đồng thời toàn bộ **33,792 thân hàm CIL** và chữ ký `#Blob` (`budgetedSignature` báo cáo `layout.blob.offset` như `14897632` ở `Cad2025`).
- Khi truyền tham số lọc `--type "<Module>"` (hoặc từng tên lớp cụ thể `--type <ClassName>`) vào lệnh chính thức `rea inspect-managed-members`, REA 6.6.0 **không phải mở rộng đồng thời 33,792 thân hàm vào một chuỗi JSON duy nhất**, nhờ đó trả về thành công `coverage.state = "complete"` kèm toàn bộ thống kê `table_row_counts`, `member_refs`, `type_refs` và đồ thị lệnh!

### 2.2 Bảng Tổng hợp Số lượng Thành viên (`TableRowCounts`) của 5 DLL CAD từ REA 6.6.0

| Tên tệp DLL CAD | Evidence ID (`inspect-managed-members --type <Module>`) | `TypeDef` | `MethodDef` | `Field` | `MemberRef` | `TypeRef` | `Param` | `Property` | `CustomAttribute` | `ImplMap` (P/Invoke) | `AssemblyRef` | `ManifestResource` | `NestedClass` |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `Kata_pro64_Cad2025.dll` | `ev_0f7f84c05803f0e0c6302790caa8f7011f76a5bed5c8b4a7dddbd28f999b8608` | **4,276** | **33,792** | **14,763** | **8,606** | **1,004** | **18,682** | **3,648** | **15,463** | **22** | **66** | **6** | **1,005** |
| `Kata_pro64_Cad2013.dll` | `ev_79f60895cb5e479070932a8bda9cc8b8ba57ae184d31ce056719546f3963295f` | **4,274** | **33,785** | **14,734** | **8,601** | **1,012** | **18,529** | **3,653** | **15,478** | **10** | **28** | **6** | **998** |
| `Kata_pro64_GstarCad.dll` | `ev_68ffec45f8fbed88b352390cff0fa9255155014181c4693ae0f6d1bb2c13df30` | **3,744** | **28,797** | **12,868** | **7,628** | **956** | **14,990** | **3,308** | **13,605** | **4** | **29** | **6** | **810** |
| `Kata_pro64_GstarCad2025.dll` | `ev_422537c9568f3fdf6fc56ddc25672e3f160126c40e8b5882f0ca313244bc5008` | **4,285** | **33,849** | **14,762** | **8,616** | **1,009** | **18,706** | **3,648** | **15,464** | **22** | **66** | **6** | **1,005** |
| `Kata_pro64_ZwCad.dll` | `ev_ff89d9237cd26eaed118c7a3e8b924452517ce16ca6f333a4731a632bc8dc265` | **1,170** | **18,467** | **10,314** | **7,633** | **953** | **14,981** | **3,296** | **17,002** | **4** | **26** | **5** | **812** |

---

## 3. Bản đồ Ánh xạ Ngữ nghĩa Các Lớp & Phương thức Bị Xáo trộn (`Semantic Deobfuscation Map`)

Dựa trên đồ thị gọi hàm (`CallEdges`), chữ ký trường (`Field Signatures`), tham chiếu API ngoài (`Autodesk.AutoCAD.DatabaseServices`, `Autodesk.Revit.DB.Structure`, `Microsoft.Office.Interop.Excel`, `CSiAPIv1`), các thuộc tính `[CommandMethod]` và 21 module VBA đã mở khóa trong `Kata.xlsm`, bảng dưới đây ánh xạ các lớp và phương thức nội bộ sang chức năng kỹ thuật thực sự:

### 3.1 Ánh xạ Ngữ nghĩa trong `Kata_Class_Lib.dll` & `Kata_Class_Lib_Revit.dll`

| Định danh trong DLL (Công khai / Obfuscated) | Thư viện chứa | Các Phương thức / Trường Đặc trưng | Tham chiếu API & Dữ liệu Liên kết | Vai trò Kỹ thuật & Nghiệp vụ Thực tế (Semantic Role) |
|---|---|---|---|---|
| `Kata_Class_Lib.Class1`..`Class12` / Các lớp Obfuscated mang P/Invoke | `Kata_Class_Lib.dll`, `Kata_Class_Lib_Revit.dll` | `GetActiveObject`, `SetForegroundWindow`, `FindWindow`, `ShowWindow`, `IsIconic` | `oleaut32.dll`, `user32.dll`, `Microsoft.Office.Interop.Excel` | **Trình Quản lý Kết nối COM Excel & Cửa sổ Host (`ExcelComSessionBridge`)**: Bắt phiên `Excel.Application` đang mở để đọc/ghi trực tiếp các sheet `Dam`, `Cot`, `Vach`, `Thong_ke` và chuyển đổi tiêu điểm giữa CAD/Revit và Excel. |
| `Kata_Class_Lib.XYZ`, `CircleXYZ`, `CurveXYZ` | `Kata_Class_Lib.dll` | `X`, `Y`, `Z`, `Add`, `Subtract`, `CrossProduct`, `DotProduct`, `DistanceTo` | Thuần C# Toán học Vector 3D | **Hạt nhân Hình học Không gian Độc lập Host (`Pure3dGeometryKernel`)**: Biểu diễn tọa độ điểm, đoạn thẳng, cung tròn 3D dùng chung giữa AutoCAD (`Point3d`) và Revit (`Autodesk.Revit.DB.XYZ`). |
| `Kata_Class_Lib.Hinh_hoc_phang`, `Hinh_hoc_khong_gian` | `Kata_Class_Lib.dll` | Các hàm `smethod_*` nhận `XYZ`, `CurveXYZ`, `List<XYZ>` | Toán học giao cắt đoạn thẳng, chiếu vuông góc, xét điểm trong đa giác | **Bộ Giải Hình học Phẳng & Giao cắt Cấu kiện (`IntersectionAndProjectionSolver`)**: Tính giao điểm trục dầm - cột/vách, tìm miền rải thép sàn đa giác và khoảng lùi bê tông bảo vệ (`cover`). |
| `info_Block_Beam_Rebar`, `info_BlockBeam_AllData1nhip`, `info_BlockBeam_DataEtabs` | `Kata_Class_Lib.dll` | `U_ngoai`, `thep_t`, `thep_d`, `thep_gc`, `cd`, `a_dai`, `dai_gc_input` | Tương ứng trực tiếp với `Type data_dam` và `Sub Read_data_beam` trong module VBA `Ve_dam` (`Kata.xlsm`) | **Mô hình Dữ liệu Bố trí Thép Nhịp Dầm & Nội lực ETABS (`BeamSpanRebarAndEtabsModel`)**: Lưu trữ cấu hình thép chủ trên/dưới, thép gia cường gối/nhịp, bước đai 3 vùng và diện tích thép yêu cầu $A_s$ từ ETABS. |
| `info_revit_thep`, `info_revit_thep_ngang`, `info_revit_dai`, `info_revit_moc_thep` | `Kata_Class_Lib.dll`, `Kata_Class_Lib_Revit.dll` | `Origin`, `Direction_L`, `Direction_H`, `Barshape`, `Mark`, `Sch_Mark`, `Stt_cad`, `Dia`, `Spacing` | `Autodesk.Revit.DB.Structure.Rebar` | **Hợp đồng Chuyển giao Cốt thép 3D sang Revit (`RevitRebarSpecificationDto`)**: Chuẩn hóa tọa độ gốc, vector phương rải, mã hình dạng `dang00..67`, đường kính và bước rải trước khi gọi Revit API. |
| `Kata_Class_Lib_Revit.Revit_rebar` + các hàm helper `smethod_*` | `Kata_Class_Lib_Revit.dll` | `create_rebar`, `create_rebar_with_so_thanh`, `create_rebar_with_hooks`, `Bend_diameter`, `lay_tap_curve_rebar` | `Rebar.CreateFromCurves`, `RebarShapeDrivenAccessor.SetLayoutAsFixedNumber`, `SetLayoutAsMaximumSpacing` | **Động cơ Khởi tạo Cốt thép 3D Revit (`RevitRebarCreationEngine`)**: Chuyển mảng `CurveXYZ` thành `IList<Curve>`, tra cứu đường kính uốn tiêu chuẩn và sinh phần tử `Rebar` trong `Document` Revit. |
| `Kata_Class_Lib_Revit.Get_element` + các hàm nội bộ `smethod_*` | `Kata_Class_Lib_Revit.dll` | `Get_Delete_rebar_in_host`, `Get_Delete_view_section`, `Get_columnwall_giao_Beam`, `get_barshape`, `get_barType`, `get_hookType` | `FilteredElementCollector`, `BoundingBoxIntersectsFilter`, `ElementIntersectsSolidFilter` | **Bộ Truy vấn Phần tử & Đảm bảo Lặp lại An toàn (`RevitHostQueryAndIdempotencyService`)**: Xóa cốt thép/mặt cắt Kata cũ trong host trước khi vẽ lại và tìm các cột/vách giao với dầm để xác định nhịp thông thủy. |
| `Kata_Class_Lib_Revit.Perform_2D` + các hàm nội bộ `smethod_*` | `Kata_Class_Lib_Revit.dll` | `tao_section_dung`, `tao_section_nam`, `dim_ro_revit`, `dim_al_revit`, `Create_rebar_mul_Tag`, `Create_Break_Line` | `ViewSection.CreateSection`, `Document.Create.NewDimension`, `IndependentTag.Create` | **Động cơ Triển khai Bản vẽ Chi tiết 2D Revit (`RevitShopDrawing2dAnnotator`)**: Tự động cắt mặt cắt dọc/ngang dầm-cột, ghi kích thước tự động, gắn tag cốt thép và đặt ký hiệu nét cắt `Break Line`. |

### 3.2 Ánh xạ Ngữ nghĩa Các Nhóm Lớp Lệnh trong `Kata_pro64_Cad2025.dll` & `Kata_pro64.dll`

| Nhóm Lớp & Phương thức Nội bộ (`Class*` / `smethod_*`) | Các Lệnh `[CommandMethod]` Liên kết | Tương tác DWG / XData / Excel / VBA | Chức năng Kỹ thuật Giải mã được |
|---|---|---|---|
| **Khối Khởi tạo Ribbon & Cấu hình Hệ thống** (`KATA_RIBBON`, `setupthep`, `setupkata`, `setupkata1`) | `KATA_RIBBON`, `setupthep`, `setupkata`, `setupkata1`, `kata_abouts`, `pukata` | Đọc/ghi 86 dòng cấu hình cuối tệp `Goi lenh.lsp`, nạp `menukata.cuix` và 107 icon trong `Icon\Ribbon\*.png`, nhập 119 block từ `Iso_kata.dwg` | Khởi tạo giao diện Ribbon AutoCAD, đồng bộ 86 tham số quy chuẩn (lớp bê tông bảo vệ, chiều dài nối chồng $L_{\text{lap}}$, chiều dài neo $L_{\text{anchor}}$, tỷ lệ mặt cắt, tên layer, màu sắc) và dọn dẹp tài nguyên Kata. |
| **Khối Vẽ & Chỉnh sửa Thanh Thép Đơn & XData** (`Rebar`, `Info_rebar`, các `smethod_*` xử lý `ResultBuffer`) | `ve_thep`, `taothep`, `sua_thep`, `sua_moc_dai`, `dai90`, `dai135`, `dai180`, `dai90135`, `nhan_thep`, `neo_thep`, `update_thep`, `cat_thep`, `cat_tuy_y`, `noi_thep` | Ghi/đọc `RegAppName = "KATA"` với chuỗi mã DXF XData `1000` (Dạng thép `00..67`, tên cấu kiện, khoảng cách), `1040` (Chiều dài các đoạn `L1..L6`), `1070` (Đường kính $\varnothing$, số thứ tự `STT`, số lượng cấu kiện) | Tạo và biến đổi thực thể `Polyline`/`Line` biểu diễn cốt thép kèm siêu dữ liệu mở rộng XData, tự động cắt thanh 11.7m, tạo mối nối chồng hoặc coupler và cập nhật nhãn ghi chú liên kết. |
| **Khối Triển khai Bản vẽ Chi tiết Dầm (Beam Shop Drawing)** (`Design_Beam`, `info_Block_Beam_Rebar`, liên kết module VBA `Ve_dam` & `Tinh_dam`) | `ve_dam`, `ve_dam_shop`, `ve_dam_mong`, `ve_giang_mong`, `ve_giang_shop`, `ve_dam_mc`, `dam_giao`, `ve_dam_xien`, `ve_dam_cong`, `raidam`, `loadbeam` | Đọc sheet `Dam` trong `Kata.xlsm` (tọa độ `A3:B12`, dòng `10..20`) hoặc chạy trực tiếp cùng macro `Sub Ve_dam_cad()` trong module `Ve_dam` (439 dòng VBA đã giải mã) | Dựng mặt cắt dọc dầm liên tục nhiều nhịp, bố trí thép chủ chạy suốt + thép gia cường gối/nhịp, cắt thanh thương phẩm 11.7m, rải cốt đai 3 vùng (2 đầu dầy, giữa thưa), gia cường đai vai bò tại vị trí dầm phụ giao dầm chính và vẽ các mặt cắt ngang `1-1`, `2-2`. |
| **Khối Triển khai Chi tiết Cột & Vách Cứng (Column & Shear Wall)** (`Info_ColumnWall3D`, `Info_dai_cot`, `Update_Block_ColumnWall`) | `ve_cot`, `ve_cot_shop`, `raicot`, `dimcot`, `loadcolumn`, `ve_vach`, `ve_vach_shop`, `ve_loi_vach`, `loadwall` | Đọc sheet `Cot` và `Vach` trong `Kata.xlsm`, sử dụng 13 tệp WMF trong `Cot\` (`Crank Lap Bar.wmf`, `Starter L Bar.wmf`...) | Triển khai cốt thép cột/vách qua nhiều tầng, tự động uốn cổ chai (crank) khi thay đổi tiết diện tầng trên, bố trí thép chờ móng, đai lồng/đai C kháng chấn vùng tới hạn $l_0$ và vẽ mặt cắt ngang cột/vách. |
| **Khối Triển khai Móng, Sàn, Cầu thang & Lanh tô** (`Info_Slab3D`, `info_revit_slab`) | `ve_mong_don`, `ve_mong_coc`, `udmong`, `phamong`, `ve_san`, `vesanpl`, `vesan4d`, `thong_ke_block_san`, `ve_thang`, `velanhto` | Đọc biên dạng `Polyline` trong CAD, chèn block ký hiệu rải thép từ `Iso_kata.dwg` | Vẽ mặt bằng & mặt cắt móng đơn/đài cọc, rải lưới thép sàn 2 phương lớp dưới + thép mũ gối lớp trên ($L/4$) + thép chân chó (`spacer chair`), và triển khai bản thang bộ / lanh tô cửa. |
| **Khối Đánh số, Thống kê Thép & Xuất Excel** (liên kết module VBA `save_info`) | `stt0`, `stt1`, `thong_ke_thep`, `sua_thong_ke`, `sstk`, `tong_hop_thep`, `thong_ke_coupler`, `toexcel`, `toexcelqh`, `in_tem_thep` | Quét toàn bộ thực thể mang XData `KATA`, chèn block hình dạng `dang00..dang67` từ `Iso_kata.dwg`, xuất sang sheet `Thong_ke` (`Kata.xlsm`) và `Iso_QHPlus.xls` | Gom nhóm thanh thép trùng hình dạng/đường kính/kích thước, đánh số hiệu `STT`, tính tổng chiều dài và khối lượng theo trọng lượng riêng TCVN, thống kê ống nối ren Coupler và xuất bảng thống kê chuẩn CAD/Excel/QH Plus. |
| **Khối Tối ưu hóa Cắt Thép 1D (`Cutting Stock 11.7m`)** (liên kết module VBA `to_hop`) | `to_hop_thep` | Đồng bộ thuật toán với module VBA `to_hop` (424 dòng VBA đã giải mã: `Sub To_hop_thep()`, `Sort_to_hop`, `Ghep_thanh`, `Xuat_ket_qua`) | Giải bài toán cắt thép thanh thương phẩm $L = 11,700\text{ mm}$ bằng quy hoạch tham lam kết hợp ghép tổ hợp đa đoạn (First-Fit Decreasing & Multi-Segment Combination) nhằm cực tiểu hóa phế liệu (decop) và tận dụng đoạn thép dư. |
| **Khối Tích hợp CSI ETABS & Bóc tách Tiên lượng QS** | `Etabs`, `QS`, `QS_mbkc`, `QS_tru`, `xuatQS`, `timqs`, `danhmuc`, `sap_ban_ve`, `tao_khung`, `taomv` | Tham chiếu `CSiAPIv1.dll` (`cOAPI`, `cSapModel`), mô hình mẫu `ISO_KATA.EDB`, xuất báo cáo khối lượng bê tông - ván khuôn - cốt thép | Đọc trực tiếp nội lực và diện tích cốt thép tính toán từ mô hình ETABS đang mở, bóc tách khối lượng bê tông/ván khuôn trừ giao cắt cấu kiện và tự động dàn trang khung tên bản vẽ. |

---

## 4. Phân tích Pháp y Tĩnh Toàn diện Tệp Nhị phân Bản địa `kata_pro.dll` (Native Win32 PE32 / VB6 COM Server)

- **Tệp tin**: `C:\kata_pro\kata_pro.dll` (`1,011,712` bytes, SHA-256: `2c6ab3af0338c7a33345d3a1a156d0aa13265cd81101f4d574a0ac1f50fb6c88`)
- **Bằng chứng REA 6.6.0**:
  - `inspect-managed-artifact`: `ev_797ce6746a3314d441f7d2a0f41c45f1aa50e0b32200bcf4e409271a66b820b9` (`status: "not-managed"`, `cli-directory-absent`)
  - `inspect-pe-resources`: `ev_a2878acabb45e0f38d9583b03d0494d0c061195ad66a2d5741df63e3cd7d7c3f`
  - `inspect-managed-native-boundaries`: `ev_02033629ddbb53b5f614f88cec59fc242be648788483c329156ae4e4e573197c`

### 4.1 Cấu trúc PE Header & COFF Sections của `kata_pro.dll`
- **Kiến trúc máy (`Machine`)**: `0x014C (IMAGE_FILE_MACHINE_I386 / x86 32-bit)`
- **Định dạng PE (`Magic`)**: `0x010B (PE32 32-bit)`
- **Trình liên kết (`Linker Version`)**: `6.0` (Microsoft Visual Basic 6.0 / VC++ 6.0 Native Compiler)
- **Địa chỉ nạp cơ sở (`ImageBase`)**: `0x11000000` | **Điểm vào (`EntryPoint RVA`)**: `0x00003C2C`
- **Tên nội bộ trong Export Directory**: `kata_pro.dll`

| Tên Section | Địa chỉ Ảo (`VirtualAddress`) | Kích thước Ảo (`VirtualSize`) | Vị trí File (`RawPtr`) | Kích thước Thô (`RawSize`) | Đặc tính (`Characteristics`) | Entropy (Bits/Byte) | Mã băm SHA-256 của Section |
|---|---:|---:|---:|---:|---|---:|---|
| `.text` | `0x00001000` | `920,357` | `0x00001000` | `921,600` | `0x60000020` | **5.7869** | `0a8f08913ec1c43bc7c0a0ac...` |
| `.data` | `0x000E2000` | `65,364` | `0x000E2000` | `4,096` | `0xC0000040` | **-0.0** | `ad7facb2586fc6e966c004d7...` |
| `.rsrc` | `0x000F2000` | `4,628` | `0x000E3000` | `8,192` | `0x40000040` | **2.5284** | `b7955e4be4c1343cf906f4b7...` |
| `.reloc` | `0x000F4000` | `71,048` | `0x000E5000` | `73,728` | `0x42000040` | **6.6402** | `9f167b72c8ee52090db211f0...` |

### 4.2 Bảng Hàm Xuất khẩu (`Export Directory`) và Nhập khẩu (`Import Directory`)
- **4 hàm xuất khẩu chuẩn COM ActiveX In-Process Server**:
  - Ordinal `1`: **`DllCanUnloadNow`** — RVA `0x00017A9A` (`file_offset = 96922`)
  - Ordinal `2`: **`DllGetClassObject`** — RVA `0x00017A6E` (`file_offset = 96878`)
  - Ordinal `3`: **`DllRegisterServer`** — RVA `0x00017A84` (`file_offset = 96900`)
  - Ordinal `4`: **`DllUnregisterServer`** — RVA `0x00017A58` (`file_offset = 96856`)
- **Thư viện nhập khẩu (`Import Directory`)**:
  - **`MSVBVM60.DLL`** (**191 hàm**): `__vbaVarSub (hint=0), __vbaVarTstGt (hint=0), __vbaStrI2 (hint=0), _CIcos (hint=0), _adj_fptan (hint=0), Ordinal#585, __vbaVarMove (hint=0), __vbaStrI4 (hint=0), __vbaVarVargNofree (hint=0), __vbaFreeVar (hint=0), __vbaAptOffset (hint=0), __vbaStrVarMove (hint=0), __vbaLenBstr (hint=0), __vbaLineInputStr (hint=0), Ordinal#696` ...
- **Kết luận kiến trúc về `kata_pro.dll`**:
  1. Phụ thuộc duy nhất của `kata_pro.dll` là **`MSVBVM60.DLL` (175 hàm nhập khẩu)** — chứng minh đây là **ActiveX COM DLL thế hệ cũ được biên dịch bằng Microsoft Visual Basic 6.0** (`kata_pro.dll` chứa 3 COM Interface: `_THEP`, `_TINH_CAU_KIEN`, `_VE_CAU_KIEN` trong tài nguyên `TYPELIB`).
  2. Đối chiếu với toàn bộ bảng `ImplMap` (P/Invoke) và `AssemblyRef` của cả 8 DLL .NET 64-bit (`Kata_Class_Lib.dll`, `Kata_Class_Lib_Revit.dll`, `Kata_pro64.dll`, `Kata_pro64_Cad2025.dll`...): **Không có bất kỳ DLL .NET 64-bit nào gọi hay nạp `kata_pro.dll`** (vì tiến trình AutoCAD 2025 / Revit 2026 64-bit không thể nạp trực tiếp DLL COM 32-bit `MSVBVM60.DLL`). Toàn bộ logic của `kata_pro.dll` đã được tác giả di trú sang các lớp .NET trong `Kata_Class_Lib.dll` và `Kata_pro64_Cad*.dll`.
