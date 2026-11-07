# ĐẶC TẢ KỸ THUẬT TÍNH NĂNG VẼ MÓNG (`SPEC_Mong.md`) — ỨNG DỤNG `C:\kata_pro`

> **Tài liệu đặc tả đảo ngược hợp pháp (Clean-Room Static & REA Specification)**
> - **Đối tượng khảo sát:** Bộ ứng dụng `C:\kata_pro` (`Kata_pro64_Cad2025.dll`, `Kata_Class_Lib.dll`, `Kata_Class_Lib_Revit2025.dll`, `kata_pro.dll`, `menukata.cuix`, `Goi lenh*.lsp`, `Kata.xlsm`).
> - **Phương pháp thu thập:** Kiểm tra tĩnh chỉ đọc kết hợp REA CLI (`rea.cmd`). Không thực thi mã đích, không vượt rào obfuscation/DRM, không sửa đổi `C:\kata_pro`.
> - **Phân loại quy tắc:**
>   - **Phần A (`VM-001` .. `VM-295`) — Quy tắc đã xác thực đầy đủ bằng bằng chứng tĩnh/REA (Observed Facts).**
>   - **Phần B (`VM-U01` .. `VM-U25`) — Hành vi thuật toán bên trong thân hàm bị stub hóa (Inferred Hypotheses & Unknown On-Disk Bodies).**

---

## 1. Danh mục Bằng chứng REA (Evidence Registry `E1` .. `E16`)

| Mã | `evidence_id` (REA) | Thao tác REA / Nguồn | Tệp đích (`C:\kata_pro\...`) | SHA-256 (`digest`) | Nội dung xác thực chính |
|---|---|---|---|---|---|
| **E1** | `ev_684d1c19ef29b78eed845c325871161b73ba6ed3d5cbebc0a85e7915a1f93fb5` | `inspect_desktop_application` | `C:\kata_pro` | `d7d66e4d85d32efa4f4aeef03209ff262189e0a5cf7101d299510a2d254d3a1e` | Kiểm kê cấu trúc cài đặt desktop app `C:\kata_pro`. |
| **E2** | `ev_97b4e288e3f7a21f0129787455cca6144a938df403e0c5d76283d2f62e778008` | `inspect_managed_artifact` | `Kata_pro64_Cad2025.dll` | `a1560540787d21c10177c260320682204fb17ee338ec812dac777fe919697673` | Header PE32+ (`x86_64`), CLR `.NET Framework v4.8`, danh sách assembly tham chiếu. |
| **E3** | `ev_9679d034faee91618b4a50c9e880f65be1fec69eb9c779ae8dedecc5a33e293b` | `inspect_managed_members` | `Kata_pro64_Cad2025.dll` (`cad2025_members.json`) | `a1560540787d21c10177c260320682204fb17ee338ec812dac777fe919697673` | Quét toàn bộ bảng `TypeDef` của `Kata_pro64_Cad2025.dll`. |
| **E4** | `ev_3bdf54e817a9b5840d6618afe7ca3edeaabd4e8c3021dcb361d608566084bf9d` | `inspect_managed_members` | `Kata_pro64_Cad2025.dll` (`cad2025_mong_core.json`) | `a1560540787d21c10177c260320682204fb17ee338ec812dac777fe919697673` | 6 lớp lõi: `Draw_foudation` (`0x02000557`), `Mong_coc` (`0x020002ec`), `Rai_Mong_Module` (`0x02000335`), `Ve_Mat_Bang_Mong_Module` (`0x02000370`), `Foundation_Etabs` (`0x0200052c`), `AI_DrawIsolatedFoundation_Tool` (`0x020001be`). |
| **E5** | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69` | `inspect_managed_members` | `Kata_pro64_Cad2025.dll` (`cad2025_mong_models.json`) | `a1560540787d21c10177c260320682204fb17ee338ec812dac777fe919697673` | 34 kiểu DTO/Model/Form/Enum của tính năng móng (1.363 fields, 2.138 methods). |
| **E6** | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44` | `inspect_managed_members` | `Kata_pro64_Cad2025.dll` (`cad2025_vmbm_and_cmds.json`) | `a1560540787d21c10177c260320682204fb17ee338ec812dac777fe919697673` | 49 lớp `VmbmLight*` cùng `Acad`/`Class28` (`0x020000d5`), `Lenh_thep` (`0x020000d6`), `kata1` (`0x0200027b`), `kata7` (`0x02000566`), `kata8` (`0x0200058e`). |
| **E7** | `ev_549bfe33ecd78257ce27570bdd19331f304e770621ccd33a14059b374ac982e3` | `inspect_managed_members` | `Kata_pro64_Cad2025.dll` (`cad2025_extra_mong.json`) | `a1560540787d21c10177c260320682204fb17ee338ec812dac777fe919697673` | 7 kiểu bổ sung (106 fields, 180 methods): `ModeBuildEtabEnum` (`0x02000371`), `ViTriCurveDam` (`0x02000375`), `ViTriDatTen` (`0x0200037b`), `Form_VMBM_Light_Name` (`0x02000039`), `Edit_moc_dai` (`0x02000059`), `Form_VMBM_Light` (`0x02000390`), `Form_VMBM_Light_Edit` (`0x02000476`). |
| **E8** | `ev_f94acc3c1c98356a53651451a6de4c2edf1f53989c96f086936ec4a16a60ac33` *(alias: `ev_58f2ef306564331d526ba398b6c751822ecbc72d2ab7d68d6847e9cb4c5c4a25`)* | `inspect_managed_members` | `Kata_pro64_Cad2025.dll` (`cad2025_sub_dtos.json`) | `a1560540787d21c10177c260320682204fb17ee338ec812dac777fe919697673` | 13 kiểu DTO con & Form lớp thép thêm (247 fields, 391 methods): `Form_Lop_Thep_Them` (`0x0200006e` `[class:440]` / `0x02000062`), `Form_Lop_Thep_Them_Ngang` (`0x0200006f` `[class:444]` / `0x02000063`), `Form_Lop_Thep_Them_Tren` (`0x02000070` `[class:448]` / `0x02000064`), `KataCircle3D` (`0x020002ef` `[class:3004]`), `LopThepThem` (`0x020002f0` `[class:3008]`), `Infogrid` (`0x020002f3` `[class:3020]`), `Dlcot` (`0x020002f4` `[class:3024]`), `GiaCuongThemTheoPolygon` (`0x02000314` `[class:3152]` / `0x0200031f`), `LopThepThemV2` (`0x02000316` `[class:3160]` / `0x0200031c`), `DlCotV2` (`0x02000318` `[class:3168]` / `0x0200031e`), `InfoAreaSpring` (`0x02000376` `[class:3544]`), `InfoText` (`0x0200037d` `[class:3572]`), `StripDesignOption` (`0x0200052e` `[class:5304]`; `StripEtabs` `0x0200052d` `[class:5300]`). |
| **E9** | `ev_855a5237f6cacba233aa7fcf0aefb5e49fe2b3049e5d94b932e2944c9ac832dc` & `ev_12ac2acfc71ff9e85b76f3b85f25c15a191e22b189e37d3e6d6c69c4570405c1` | `inspect_managed_artifact` & `members` | `Kata_Class_Lib_Revit2025.dll` | `60af382583acd6e7d59a0965cc82b1e2eade559df5adcffbe1f599710fde6bd2` | Cấu trúc assembly Revit 2025 và danh mục các lớp chọn lọc móng. |
| **E10** | `ev_adbc463d9219c90cfb2ded17f6f3e6035c5501afd3b9c7dcca55b8ec96e25022` | `inspect_managed_members` | `Kata_Class_Lib_Revit2025.dll` (`revit2025_foundations_v2.json`) | `60af382583acd6e7d59a0965cc82b1e2eade559df5adcffbe1f599710fde6bd2` | `Loai_dam` (`0x0200005f`), `Element_Select` (`0x02000071`), `Foundation_SelectionFilter` (`0x02000077`), `Foundation_Floor_SelectionFilter` (`0x02000078`), `Column_Walls_Foundation_SelectionFilter` (`0x0200007d`), `Column_Foundation_SelectionFilter` (`0x0200007e`). |
| **E11** | `ev_834d4667bdd19a6c2de932fe8515809eaf6dfe272ebdea22db8d6f0f489839a6` | `inspect_managed_members` | `Kata_Class_Lib.dll` (`classlib_loaidam_v2.json`) | `ce59d45152b94b3fee1db55d935bfd4648e52e08516471a61de588cdf7009ced` | `Kata_Class_Lib.Loai_dam` (`0x02000072`): `dam = 0` (`0x04000288`), `giang_mong = 1` (`0x04000289`), `dam_mong = 2` (`0x0400028a`). |
| **E12** | `ev_13f51a2e5f954eab67e5e4e3904577cac8240c699d177f16473b4fc6c509cd4a` | `inspect_managed_artifact` | `Kata_pro64.dll` | `5da9a37be8fea727da725df05cc89108b5fe3e6071958b012e3fcc2441b01b44` | `Malformed CLI metadata: Duplicate metadata stream #GUID` trên `Kata_pro64.dll`. |
| **E13** | `ev_4c193f8f944ba6e0491b67d5da6063cdadec3e618036a67e511ec4ac0e03f515` & `ev_2d35eacf9d034e4eaaa3e445e8b312c1a7eb6561c93feedbf4f3cbecd1e31bd0` | `inspect_artifact`, `pe_imports`, `native_exports` | `kata_pro.dll` | `2c6ab3af0338c7a33345d3a1a156d0aa13265cd81101f4d574a0ac1f50fb6c88` | Native 32-bit (`i386`) VB6 (`MSVBVM60.DLL`), xuất 4 hàm COM và `TYPELIB #1`. |
| **E14** | `ev_2b923d7155149afca7eefa71152981ab0416c627389481650d65d04d6deb1cc2` | `inspect_artifact` & đọc CUIX XML | `menukata.cuix` | `63115bc3f9ea98945cd21cfcc1efee40f032e1a4053604ddc64c331bf661ed5e` | Menu Macro `MMU_0026` (`^C^Cvmd`), PopMenuItem `PMIU_0017` (`&Vẽ móng đơn`, `RCDATA_19705`), PopMenu `PMU_0001`, Toolbar `TB_01`. |
| **E15** | `ev_bba34942c5436b0f6e9d5af1f505c10736afabaac6c101210702c1674aff7934` | `inspect_artifact` & đọc OpenXML | `Kata.xlsm` (`sheet3.xml` - `SLAB DESIGN`) | `93036043620b365a0e50d3eba9def3e1d4db66c6081cfb108fc2ae574b82bb96` | Công thức tính cốt thép bản/móng theo TCVN 5574:2018 và bảng tra `N6:P16`, `N19:O25`. |
| **E16** | Kiểm tra tĩnh mã nguồn AutoLISP | `view_file` | `Goi lenh.lsp`, `Goi lenh - Gstarcad.lsp`, `Goi lenh - Zwcad.lsp` | Xem dòng cụ thể | Hàm LISP `c:vmd`, `c:vmc`, `c:vdm`, `c:vgm`, `c:vgms`, `c:rm`, `c:udm`, `c:pm`, `c:scoc` và `c:setup_ini` (dòng 155–240). |

---

## 2. PHẦN A: Các Quy tắc Đã Xác thực Đầy đủ bằng Bằng chứng Tĩnh / REA (`VM-001` .. `VM-295`)

### 2.0. Bảng Tổng hợp Danh mục Quy tắc Phần A (`VM-001` .. `VM-295`)

| Mã | Nhóm | Mô tả quy tắc kỹ thuật đã xác thực | Bằng chứng REA |
|---|---|---|---|
| `VM-001` | A.1 CUIX | Menu Macro `MMU_0026` định nghĩa lệnh `"Vẽ móng đơn"`, macro `^C^Cvmd`, ảnh `RCDATA_16244.bmp` | **E14** (`MenuGroup.cui` dòng 258–266) |
| `VM-002` | A.1 CUIX | `PMIU_0017` trong PopMenu `PMU_0001` (`KATA`, `POP12`) gắn `MenuMacroID="MMU_0026"`, nhãn `"&Vẽ móng đơn"` (`RCDATA_19705`) | **E14** (`PopMenuRoot.cui` dòng 4–79) |
| `VM-003` | A.1 CUIX | Thanh công cụ `TB_01` (`Kata`, `Orient="floating"`, `Visible="show"`) chứa các nút lệnh kết cấu | **E14** (`ToolbarRoot.cui` dòng 4–120) |
| `VM-004` | A.1 LISP | Lệnh tắt `vmd` (`defun c:vmd`) gọi `(command "ve_mong_don")` | **E16** (`Goi lenh.lsp`:120; `Gstarcad`/`Zwcad`:35) |
| `VM-005` | A.1 LISP | Lệnh tắt `vmc` (`defun c:vmc`) gọi `(command "ve_mong_coc")` | **E16** (`Goi lenh.lsp`:123; `Gstarcad`/`Zwcad`:36) |
| `VM-006` | A.1 LISP | Lệnh tắt `vdm` (`defun c:vdm`) gọi `(command "ve_dam_mong")` | **E16** (`Goi lenh.lsp`:130; `Gstarcad`/`Zwcad`:39) |
| `VM-007` | A.1 LISP | Lệnh tắt `vgm` (`defun c:vgm`) gọi `(command "ve_giang_mong")` | **E16** (`Goi lenh.lsp`:133; `Gstarcad`/`Zwcad`:40) |
| `VM-008` | A.1 LISP | Lệnh tắt `vgms` (`defun c:vgms`) gọi `(command "ve_giang_shop")` | **E16** (`Goi lenh.lsp`:136; `Gstarcad`/`Zwcad`:41) |
| `VM-009` | A.1 LISP | Lệnh tắt `rm` (`defun c:rm`) gọi `(command "rai_mong")` | **E16** (`Goi lenh.lsp`:96; `Gstarcad`:94; `Zwcad`:90) |
| `VM-010` | A.1 LISP | Lệnh tắt `udm` (`defun c:udm`) gọi `(command "udmong")` | **E16** (`Goi lenh.lsp`:30; `Gstarcad`/`Zwcad`:13) |
| `VM-011` | A.1 LISP | Lệnh tắt `pm` (`defun c:pm`) gọi `(command "phamong")` | **E16** (`Goi lenh.lsp`:36; `Gstarcad`/`Zwcad`:15) |
| `VM-012` | A.1 LISP | Lệnh tắt `scoc` (`defun c:scoc`) gọi `(command "scoc")` | **E16** (`Gstarcad`/`Zwcad`:78) |
| `VM-013` | A.1 `.NET` | `Lenh_thep.Ve_mong_new` (`0x06001fd5`, RVA `3204136`, `il_size=4`) đăng ký `[CommandMethod("ve_mong_don")]` | **E6** (`0x020000d6`, `0x06001fd5`) |
| `VM-014` | A.1 `.NET` | `Lenh_thep.Ve_mong_coc1` (`0x06001fd6`, RVA `3204660`, `il_size=4`) đăng ký `[CommandMethod("ve_mong_coc")]` | **E6** (`0x020000d6`, `0x06001fd6`) |
| `VM-015` | A.1 `.NET` | `Lenh_thep.Vedammong` (`0x06001fde`, RVA `3206128`, `il_size=4`) đăng ký `[CommandMethod("ve_dam_mong")]` | **E6** (`0x020000d6`, `0x06001fde`) |
| `VM-016` | A.1 `.NET` | `Lenh_thep.Vegiangmong` (`0x06001fdf`, RVA `3206324`, `il_size=4`) đăng ký `[CommandMethod("ve_giang_mong")]` | **E6** (`0x020000d6`, `0x06001fdf`) |
| `VM-017` | A.1 `.NET` | `Lenh_thep.Vegiangmong_shop` (`0x06001fe0`, RVA `3206496`, `il_size=4`) đăng ký `[CommandMethod("ve_giang_shop")]` | **E6** (`0x020000d6`, `0x06001fe0`) |
| `VM-018` | A.1 `.NET` | `Lenh_thep.RM1` (`0x06002000`, RVA `3213520`, `il_size=4`) đăng ký `[CommandMethod("rai_mong")]` | **E6** (`0x020000d6`, `0x06002000`) |
| `VM-019` | A.1 `.NET` | `Lenh_thep.Update_mong` (`0x06001f9e`, RVA `3195732`, `"udmong"`) và `PhaMong1` (`0x06001fa0`, RVA `3195764`, `"phamong"`) | **E6** (`0x020000d6`, `0x06001f9e`, `0x06001fa0`) |
| `VM-020` | A.1 `.NET` | `Lenh_thep.Khai_thong_tin_coc` (`0x06001ff2`, RVA `3212028`, `il_size=4`) đăng ký `[CommandMethod("scoc")]` | **E6** (`0x020000d6`, `0x06001ff2`) |
| `VM-021` | A.1 `.NET` | `Ve_Mat_Bang_Mong_Module.VMBM` (`0x06003ded`, RVA `6696300`, `il_size=4`) / `nqI8wkiUHU6` (`0x06003dde`, RVA `6669544`) trong `0x02000370` | **E4** (`0x02000370`, `0x06003ded`, `0x06003dde`) |
| `VM-022` | A.1 `.NET` | Lớp `Kata_pro64_Cad2025.VmbmLightCommand` (`0x020003d7`, RowOffset `13385266`): phương thức `ShowVmbmLight()` (`0x0600412d`, RVA `6885100`, `il_size=4`, `[CommandMethod("ve_mat_bang_mong")]`) và `.cctor()`/`.ctor()` (`0x0600412e`, RVA `6885508` `il_size=177` / `RVA 6885156, il_size=8`), cùng `vUMYo0q4tSYkSiBRwLK7` (`0x0600412f`), `FD4GBbq4di30i6TWxEFB` (`0x06004130`) | **E6** (`VM-136`/`VM-233`, Type `0x020003d7`, RowOffset `13385266`, Methods `0x0600412d`..`0x06004130`) |
| `VM-023` | A.2 INI | `setup_ini`: `"Nối thép chờ cột sang móng, có =1 không =0:"` mặc định `"0"` | **E16** (`Goi lenh.lsp`:176–177) |
| `VM-024` | A.2 INI | `"Bẻ chân thép chờ cột vào móng:"` mặc định `"250"` (mm) | **E16** (`Goi lenh.lsp`:178–179) |
| `VM-025` | A.2 INI | `"Cao trình mặt móng:"` mặc định `"-1.500"` (m) | **E16** (`Goi lenh.lsp`:180–181) |
| `VM-026` | A.2 INI | `"Kiểu bố trí thép cột chờ móng:"` mặc định `"1"` | **E16** (`Goi lenh.lsp`:188–189) |
| `VM-027` | A.2 INI | `"Gạch đầu dòng chú thích thép sàn & móng:"` mặc định `"-"` | **E16** (`Goi lenh.lsp`:202–203) |
| `VM-028` | A.2 INI | Đường kính thép gân `"10"`, chiều dài neo `"30"` ($30d$), nối `"30"` ($30d$), chiều dài tối đa `"11700"` (mm) | **E16** (`Goi lenh.lsp`:160–167) |
| `VM-029` | A.2 INI | Lớp bảo vệ `"25"`, bảng tra chiều dài neo/nối theo mác bê tông `"40 40 35 30 25 20"` | **E16** (`Goi lenh.lsp`:208–235) |
| `VM-030` | A.3 Excel | Bảng tra cường độ bê tông $R_b, R_{bt}$ (MPa) TCVN 5574:2018 trong `SLAB DESIGN` (`N6:P16`) từ `B12.5` đến `B55` | **E15** (`sheet3.xml` `N6:P16`) |
| `VM-031` | A.3 Excel | Bảng tra cường độ cốt thép $R_s$ (MPa) TCVN 5574:2018 trong `SLAB DESIGN` (`N19:O25`) | **E15** (`sheet3.xml` `N19:O25`) |
| `VM-032` | A.3 Excel | Công thức tra cứu $R_b, R_{bt}, R_s$ tại `C5`, `G5`, `C6`: `VLOOKUP(C4,N6:P16,2,0)`, `VLOOKUP(C4,N6:P16,3,0)`, `VLOOKUP(G4,N19:O25,2,0)` | **E15** (`sheet3.xml` `C5`, `G5`, `C6`) |
| `VM-033` | A.3 Excel | Chiều cao làm việc hữu ích $h_0$ tại `E17:E44`: `=C16-$G$8` ($h_0 = h - a$) | **E15** (`sheet3.xml` `E17:E44`) |
| `VM-034` | A.3 Excel | Hệ số $\alpha_m$ tại `F17:F44`: `=D17*10^6/($C$5*1000*E17^2*$C$8*1000)` | **E15** (`sheet3.xml` `F17:F44`) |
| `VM-035` | A.3 Excel | Hệ số $\zeta$ tại `G17:G44`: `=(1+SQRT(1-2*F17))/2` | **E15** (`sheet3.xml` `G17:G44`) |
| `VM-036` | A.3 Excel | Diện tích cốt thép yêu cầu $A_{s,\text{yc}}$ ($\text{cm}^2/\text{m}$) tại `H17:H44`: `=D17*10^6/($C$6*100*G17*E17)` | **E15** (`sheet3.xml` `H17:H44`) |
| `VM-037` | A.3 Excel | Diện tích cốt thép chọn $A_{s,\text{chọn}}$ và hàm lượng $\mu$ (%) tại `J17:K44`: `=(1000*PI()*I17^2)/(400*I18)` và `=100*J17/(E17*10)` | **E15** (`sheet3.xml` `J17:K44`) |
| `VM-038` | A.4 Enum | `ModeBuildEtabEnum` (`0x02000371`, RowOffset `13383430`): `value__` (`0x04001f0d`, `13529186`), `Normal = 0` (`0x04001f0e`, `13529196`), `MongCoc = 1` (`0x04001f0f`, `13529206`), `MongBang = 2` (`0x04001f10`, `13529216`), `MongDon = 3` (`0x04001f11`, `13529226`); đồng chiếu `KieuRai` (`0x0200036f`: RowOffset `13519756`, `13519766`, `13519776` theo `VM-115`) | **E7** (`VM-115`, Type `0x02000371` / `0x0200036f`, Fields `0x04001f0d`..`0x04001f11`) |
| `VM-039` | A.4 Enum | `ViTriCurveDam` (`0x02000375`, RowOffset `13383502`): `value__` (`0x04001f30`, `13529536`), `Trai = 0` (`0x04001f31`, `13529546`), `Phai = 1` (`0x04001f32`, `13529556`), `Dau = 2` (`0x04001f33`, `13529566`), `Cuoi = 3` (`0x04001f34`, `13529576`); đồng chiếu `KieuGopMong` (`0x02000376`: RowOffset `13520426`, `13520436`, `13520446` theo `VM-117`) | **E7** (`VM-117`, Type `0x02000375` / `0x02000376`, Fields `0x04001f30`..`0x04001f34`) |
| `VM-040` | A.4 Enum | `ViTriDatTen` (`0x0200037b`, RowOffset `13383610`): `value__` (`0x04001f53`, `13529886` / `13521106`), `Trai = 0` (`0x04001f54`, `13529896` / `13521116`), `Phai = 1` (`0x04001f55`, `13529906` / `13521126`), `Tren = 2` (`0x04001f56`, `13529916` / `13521136`), `Duoi = 3` (`0x04001f57`, `13529926` / `13521146` theo `VM-119`) | **E7** (`VM-119`, Type `0x0200037b`, Fields `0x04001f53`..`0x04001f57`) |
| `VM-041` | A.4 Enum | `KieuTaoDaiMongBaoCoc` (`0x0200038b`, RowOffset `13383898`): `value__` (`0x04001fde`, `13531276` / `13522756`), `GiuNguyenBaoMong = 0` (`0x04001fdf`, `13531286` / `13522766`), `TaoBaoMongBaoNgoai = 1` (`0x04001fe0`, `13531296` / `13522776`), `TaoBaoMongVac = 2` (`0x04001fe1`, `13531306` / `13522786` theo `VM-126`) | **E5** (`VM-126`, Type `0x0200038b`, Fields `0x04001fde`..`0x04001fe1`) |
| `VM-042` | A.4 Enum | `Kata_pro64_Cad2025.VmbmLightShallowKind` (`0x02000439`, RowOffset `13387030`): `value__` (`0x0400236b`, RowOffset `13540366`), `Isolated = 0` (`0x0400236c`, RowOffset `13540376`), `Strip = 1` (`0x0400236d`, RowOffset `13540386` theo `VM-162`) | **E6** (`VM-162`, Type `0x02000439`, Fields `0x0400236b`..`0x0400236d`) |
| `VM-043` | A.4 Enum | `Kata_Class_Lib_Revit.Loai_dam` (`0x0200005f`) và `Kata_Class_Lib.Loai_dam` (`0x02000072`): `dam = 0`, `giang_mong = 1`, `dam_mong = 2` | **E10**, **E11** (`0x0400021b`..`0x0400021d`; `0x04000288`..`0x0400028a`) |
| `VM-044` | A.4 Hằng số | `AI_BeamFoundationReferencePdfBuilder` (`0x02000131`, RowOffset `13373062`): `ReferencePdfFileName` (`0x04000fdc`, RowOffset `13490296`) = `"AI_RebuildBeam_Model_Reference.pdf"`; `VmbmLightLayoutIdentity` (`0x02000402`, RowOffset `13386040`): `Tolerance` (`0x040021de`, RowOffset `13536396`) = `0.01` (`VM-141`); `VmbmLightShallowCad` (`0x02000441`, RowOffset `13387174`): `DataKey` (`0x040023ba`, RowOffset `13541156`) = `"VMBML_SHALLOW"` (`VM-166`) | **E5**, **E6** (`VM-101`, `VM-141`, `VM-166`: `0x04000fdc`, `0x040021de`, `0x040023ba`) |
| `VM-045` | A.4 Kiểu Đặc biệt | Tách bạch 3 lớp (`class` kế thừa `System.Object`, không phải `Enum`): (1) `VmbmLightSteelAxis` (`0x020003d8`, RowOffset `13385284`, 4 trường `0x0400213d`..`0x04002140` RowOffset `13534786`..`13534816` cho `CapId`, `X`, `Y` theo `VM-137`/`VM-234`); (2) `VmbmLightCapSteel` (`0x0200040b`, 8 trường `0x040021fc`..`0x04002203` cho `BottomText`, `TopText`, `BottomArea`, `TopArea`, `BottomProvided`, `TopProvided`, `Spacing` theo `VM-143`/`VM-240`); (3) `VmbmLightLegacyExportScope` (`0x02000430`, RowOffset `13386868`, 6 trường `0x0400234a`..`0x0400234f` RowOffset `13540036`..`13540086`, cài đặt `IDisposable` theo `VM-158`) | **E6** (`VM-137`/`VM-234`, `VM-143`/`VM-240`, `VM-158`: Types `0x020003d8`, `0x0200040b`, `0x02000430`) |
| `VM-046` | A.5 `.cctor` | `Form_Chon_Thep_Mong_Tu_Dong..cctor` (`0x060015a9`, RVA `2161692`, `il_size=489`): khởi tạo 5 trường tĩnh `rCJuVn9YhH` (`0x0400099b`), `XB9uavWqWA` (`0x0400099c`), `PwruzsE0FX` (`0x0400099d`), `HYJHUIsgUA` (`0x0400099e`) kiểu `System.Collections.Generic.List<int>` (`genericinst(class:6193)<i4>` / `class:217<i4>`, danh sách đường kính `[10, 12, 14, 16, 18, 20]` và bước thép `[100, 150, 200]`) cùng cờ `e5ZH8K1Tlq` (`0x0400099f` : `bool` = `false`); `stsfld` tại `298/109 -> 0x0400099b`, `473/216 -> 0x0400099c`, `384/69 -> 0x0400099d`, `179/172 -> 0x0400099e`, `108/25 -> 0x0400099f` | **E5** (`VM-096`, Method `0x060015a9`, `il_sha256 = a381f10edc6a40831c262cc43f281c2833c57ba404a02572d1389e7963fbc1b8`) |
| `VM-047` | A.5 `.cctor` | `AI_BeamFoundationReferencePdfBuilder..cctor` (`0x06002414`, RVA `3519928`, `il_size=616`): khởi tạo 10 trường tĩnh `0x04000fdd`..`0x04000fe6` (`i3b8QSQwB0w`, `OZc8QjXVSLI`, `TCT8QxXlfZa`, `zXE8QDHI2bC`, `e0s8QBvIDX5`, `r0A8QOORPGT`, `FOy8QVuj78u`, `Y978QakUWJu`, `NYC8QzPNDxZ` kiểu `iTextSharp.text.BaseColor` `0x0a0006d9` và `rgg8kUwCZ4r` kiểu `CultureInfo` qua `call 0x06006c11`) khớp với `VM-101` | **E5** (`VM-101`, Method `0x06002414`, `il_sha256 = 60a2b9e2e08e01217a600d640005c43f984c9318cbd010369280643787df8a75`) |
| `VM-048` | A.5 `.cctor` | `Mong_coc..cctor` (`0x06003988`, RVA `6075252`, `il_size=1080`): khởi tạo các trường tĩnh `0x04001ac7`..`0x04001ae8` (`0x04001af2` theo `VM-104`): `0x04001ac8` `formMongCoc` kiểu `Form_mong_coc` (`0x02000073` `[class:460]`/`[class:452]`, `newobj 0x06000b47` tại `il_offset 520`/`923`, `stsfld` tại `525`/`928`), `0x04001ac7` `info_mong_coc` (`newobj 0x060039fb` tại `il_offset 1039`, `stsfld` tại `1044`), `0x04001ac9` `formThepThemDuoi`/`formThepThem` (`newobj 0x0600098c`/`0x060009c8` tại `il_offset 1019`/`806`, `stsfld` tại `1024`/`811`), `0x04001aca` `formThepThemNgang` (`newobj 0x060009fb`/`0x060009ef` tại `il_offset 479`/`933`, `stsfld` tại `484`/`938`), `0x04001acb` `formThepThemTren` (`newobj 0x06000a67`/`0x06000a1c` tại `il_offset 245`, `stsfld` tại `250`) | **E4** (`VM-104`, Method `0x06003988`, `il_sha256 = 2428a57627959e0f36898295a18991a1f6b8b1071b048e21cf57579ed7045b19`) |
| `VM-049` | A.5 `.cctor` | `Rai_Mong_Module..cctor` (`0x06003c39`, RVA `6495996`, `il_size=310`): `il_offset 136: newobj 0x06000e92` (`Form_Rai_Mong..ctor`) -> `141: stsfld 0x04001db9` (`0x04001cb3`, `formRaiMong`); `192: newobj 0x06003c3d` (`InfoRaiMong..ctor`) -> `197: stsfld 0x04001dba` (`0x04001cb4`, `info_rai_mong`); `233: newobj 0x06003c4d` (`InfoRaiMongDon..ctor`) -> `238: stsfld 0x04001dbb` (`0x04001cb5`, `info_rai_mong_don`) | **E4** (`VM-110`, Method `0x06003c39`, `il_sha256 = 115e0b27350089313be8bb6a71754bfb7972c3f2bfd22f9bb6b0277a6b7006e6`) |
| `VM-050` | A.5 `.cctor` | `Ve_Mat_Bang_Mong_Module..cctor` (`0x06003ddd`, RVA `6668992`, `il_size=540`): `il_offset 138: newobj 0x060012bc` (`Form_Ve_Mat_Bang_Mong..ctor`) -> `143: stsfld 0x04001f03` (`0x04001ea8`, `formVeMatBangMong`); `158: newobj 0x06003df0` (`InfoVeMatBangMong..ctor`) -> `163: stsfld 0x04001f04` (`0x04001ea9`, `info_ve_mat_bang_mong`/`info_ve_mb_mong`); `178: newobj 0x06003e15` (`InfoMBMongBang..ctor`) -> `183: stsfld 0x04001f05` (`0x04001eaa`, `info_mb_mong_bang`/`info_ve_mb_mong_bang`); `421: newobj 0x06003ea2` (`InfoMBMongDon..ctor`) -> `426: stsfld 0x04001f06` (`0x04001eab`, `info_mb_mong_don`/`info_ve_mb_mong_don`); kèm các `stsfld` tại `267 (0x04001f08)`, `316 (0x04001f0a)`, `358 (0x04001f07)`, `406 (0x04001f09)`, `504 (0x04001f0b)` (`125 (0x04001eae)`, `225 (0x04001ead)`, `291 (0x04001eaf)`, `347 (0x04001eb1)`, `365 (0x04001eac)`, `379 (0x04001eb0)`) | **E4** (`VM-114`, Method `0x06003ddd`, `il_sha256 = 3e43fc202446f3855cb30f495729c3e39c51f9cf86b138159a1802397be64414`) |
| `VM-051` | A.5 `.cctor` | `Foundation_Etabs..cctor` (`0x060050f6`, RVA `9226212`, `il_size=241`) khởi tạo `lbaEMamVToq` (`0x04002b41` : `StripDesignOption`) bằng `newobj 0x0600510f` | **E4** (`Method 0x060050f6`, `il_sha256 = 84d979b132124a8a...`) |
| `VM-052` | A.5 `.cctor` | Mẫu `.cctor` chuẩn `il_size=156` và `il_size=177` gọi helper giải mã/bảo vệ lớp (`0x060059c7`, `0x06005992`, `0x06005aa6`, `0x0600599b`, `0x06005ace`, `0x060059a3`) | **E4**–**E8** (`body.anchors` & `call_edges`) |
| `VM-053` | A.6 Schema DTO | Cấu trúc các DTO móng cọc chính: `InfoMongCoc` (`0x020002ed`, 51 trường, 7 hàm theo `VM-105`/`VM-206`), `InfoUserMongCoc` (`0x020002f1`, 43 trường, 4 hàm theo `VM-106`/`VM-207`), `CoversMong` (`0x020002f2`, 4 trường, 4 hàm theo `VM-107`/`VM-208`), `InfoMongCocSave` (`0x020002f5`, 63 trường, 4 hàm theo `VM-108`/`VM-209`), `InfoMongCocSaveV2` (`0x0200031b`, 57 trường, 4 hàm theo `VM-109`/`VM-210`) | **E5** (`VM-105`..`VM-109`, Tokens `0x020002ed`, `0x020002f1`, `0x020002f2`, `0x020002f5`, `0x0200031b`) |
| `VM-054` | A.6 Schema DTO | Cấu trúc các DTO con trong móng cọc (`E8`, đã in đủ 100% trường & phương thức ở Mục 2.6.2 & 2.7.1b): `KataCircle3D` (`0x020002ef` `[class:3004]`, 3 trường, 5 hàm), `LopThepThem` (`0x020002f0` `[class:3008]`, 9 trường, 4 hàm), `Infogrid` (`0x020002f3` `[class:3020]`, 4 trường, 4 hàm), `Dlcot` (`0x020002f4` `[class:3024]`, 20 trường, 4 hàm), `GiaCuongThemTheoPolygon` (`0x02000314` `[class:3152]` / `0x0200031f` `[class:3196]`, 6 trường, 6 hàm), `LopThepThemV2` (`0x02000316` `[class:3160]` / `0x0200031c` `[class:3184]`, 8 trường, 13 hàm), `DlCotV2` (`0x02000318` `[class:3168]` / `0x0200031e` `[class:3192]`, 40 trường, 14 hàm) | **E8** (`cad2025_sub_dtos.json`, Tokens `0x020002ef`, `0x020002f0`, `0x020002f3`, `0x020002f4`, `0x02000314`/`0x0200031f`, `0x02000316`/`0x0200031c`, `0x02000318`/`0x0200031e`) |
| `VM-055` | A.6 Schema DTO | Cấu trúc nhóm rải móng: `InfoRaiMong` (`0x02000336`, 10 trường, 7 hàm theo `VM-111`/`VM-212`), `RaiMongJig` (`0x02000337`, 8 trường, 9 hàm theo `VM-112`/`VM-213`), `InfoRaiMongDon` (`0x02000338`, 29 trường, 10 hàm theo `VM-113`/`VM-214`; `0x0200033b` là `RaiMongDonJig`) | **E5** (`VM-111`..`VM-113`, Tokens `0x02000336`, `0x02000337`, `0x02000338`) |
| `VM-056` | A.6 Schema DTO | Cấu trúc nhóm mặt bằng móng cổ điển: `InfoVeMatBangMong` (`0x02000372`, 14 trường, 12 hàm theo `VM-116`/`VM-216`), `InfoAreaSpring` (`0x02000376` `[class:3544]`, 5 trường, 4 hàm theo `E8`), `InfoMBMongBang` (`0x02000377`, 13 trường, 15 hàm theo `VM-118`/`VM-217`), `CocChiuTai` (`0x0200037c`, 11 trường, 7 hàm theo `VM-120`/`VM-218`), `InfoText` (`0x0200037d` `[class:3572]`, 4 trường, 4 hàm theo `E8`), `MongJsonDto` (`0x0200037e`, 12 trường, 4 hàm theo `VM-121`/`VM-219`), `MongBangJsonDto` (`0x0200037f`, 6 trường, 4 hàm theo `VM-122`/`VM-220`), `InfoCotCocGenMong` (`0x02000380`, 32 trường, 41 hàm theo `VM-123`/`VM-221`), `InfoMBMongDon` (`0x02000388`, 9 trường, 7 hàm theo `VM-124`/`VM-222`), `CotChiuTaiMongDon` (`0x0200038a`, 24 trường, 8 hàm theo `VM-125`/`VM-223`), `Infogrid` (`0x020002f3` `[class:3020]`, 4 trường, 4 hàm theo `E8`) | **E5**, **E8** (`VM-116`..`VM-125` & Mục 2.6.2/2.7.1b, Tokens `0x02000372`, `0x02000376`, `0x02000377`, `0x0200037c`..`0x02000380`, `0x02000388`, `0x0200038a`, `0x020002f3`) |
| `VM-057` | A.6 Schema DTO | Cấu trúc nhóm lưu trữ, ETABS & tự động chọn thép móng: `InfoMongDonSave` (`0x020004d2`, 41 trường, 4 hàm theo `VM-173`/`VM-270`), `InfoMongDonRenameSave` (`0x020004d3`, 41 trường, 5 hàm theo `VM-174`/`VM-271`), `StripEtabs` (`0x0200052d` `[class:5300]`), `StripDesignOption` (`0x0200052e` `[class:5304]`, 4 trường, 4 hàm theo `E8`), `InfoAreaSpring` (`0x02000376` `[class:3544]`, 5 trường, 4 hàm theo `E8`), `ThepBoTriMong` (`0x0200052f`, 3 trường, 4 hàm theo `VM-176`/`VM-273`), `MomentMong` (`0x02000530`, 11 trường, 4 hàm theo `VM-177`/`VM-274`), `TuyChonTinhThepMongTuDong` (`0x020000aa`, 4 trường, 4 hàm theo `VM-093`/`VM-194`), `KetQuaTinhThepMongTuDong` (`0x020000ab`, 3 trường, 4 hàm theo `VM-094`/`VM-195`), `PhuongAnThepMong` (`0x020000ac`, 9 trường, 5 hàm theo `VM-095`/`VM-196`) | **E5**, **E8** (`VM-093`..`VM-095`, `VM-173`..`VM-177` & Mục 2.6.2/2.7.1b, Tokens `0x020000aa`..`0x020000ac`, `0x02000376`, `0x020004d2`, `0x020004d3`, `0x0200052d`..`0x02000530`) |
| `VM-058` | A.6 VMBM Light | Cấu trúc thuộc tính (`<Prop>k__BackingField`) của các mô hình VMBM Light: `VmbmLightSteelAxis` (`0x020003d8`, 4 trường, 10 hàm theo `VM-137`/`VM-234`), `VmbmLightSlabCapInput` (`0x020003f5`, 4 trường, 10 hàm theo `VM-138`/`VM-235`), `VmbmLightFoundationInput` (`0x020003f6`, 3 trường, 19 hàm theo `VM-139`/`VM-236`), `VmbmLightOptions` (`0x0200040c`, 42 trường, 87 hàm theo `VM-144`/`VM-239`), `VmbmLightCap` (`0x0200040e`, 11 trường, 26 hàm theo `VM-146`/`VM-242`), `VmbmLightPileResult` (`0x0200040f`, 11 trường, 24 hàm theo `VM-147`/`VM-243`), `VmbmLightCapCheck` (`0x02000410`, 7 trường, 17 hàm theo `VM-148`/`VM-244`), `VmbmLightIteration` (`0x02000411`, 18 trường, 38 hàm theo `VM-149`/`VM-245`), `VmbmLightBeam` (`0x0200041a`, 13 trường, 27 hàm theo `VM-150`/`VM-248`), `VmbmLightBeamCheck` (`0x0200041b`, 22 trường, 45 hàm theo `VM-151`/`VM-249`), `VmbmLightFootingInput` (`0x0200043a`, 18 trường, 39 hàm theo `VM-163`/`VM-259`), `VmbmLightShallowSettings` (`0x0200043b`, 25 trường, 54 hàm theo `VM-164`/`VM-260`) | **E6** (`VM-137`..`VM-151`, `VM-163`, `VM-164`, Tokens `0x020003d8`, `0x020003f5`, `0x020003f6`, `0x0200040c`, `0x0200040e`..`0x02000411`, `0x0200041a`, `0x0200041b`, `0x0200043a`, `0x0200043b`) |
| `VM-059` | A.6 VMBM Light | Cấu trúc phiên làm việc VMBM Light: `VmbmLightSession` (`0x02000412`, RowOffset `13386328`, 56 trường `0x04002263`..`0x0400229a`, 117 phương thức theo `VM-152`/`VM-246`), `VmbmLightShallowSession` (`0x0200043c`, RowOffset `13387084`, 20 trường `0x0400239c`..`0x040023af`, 56 phương thức theo `VM-165`/`VM-261`), cùng `VmbmLightCap` (`0x0200040e`), `VmbmLightPileResult` (`0x0200040f`), `VmbmLightCapCheck` (`0x02000410`), `VmbmLightIteration` (`0x02000411`), `VmbmLightBeam` (`0x0200041a`), `VmbmLightBeamCheck` (`0x0200041b`) | **E6** (`VM-146`..`VM-152`, `VM-165`, `VM-246`, `VM-261`, Tokens `0x02000412`, `0x0200043c`) |
| `VM-060` | A.7 Module Lõi | Kiến trúc `Mong_coc` (`0x020002ec`, RowOffset `13381036`, **46 trường**, **115 phương thức** `0x06003988`..`0x060039fa` theo `VM-104`/`VM-205`): điều phối vẽ móng cọc (`Ve_ca_mong_coc` `0x0600398b`, `Ve_bao_MC1` `0x0600398d`, `VeMCMongForm` `0x060039e1`, `VeMBMongForm` `0x060039e2`, `Ve_mong_coc` `0x060039e3`), cốt thép (`Ve_thep_them_duoi_doc` `0x06003991`, `Ve_thep_them_tren_doc` `0x06003992`, `TinhToanThepDocMongCoc` `0x06003993`, `TinhToanThepNgangMongCoc` `0x06003994`, `ve_cot_mong_coc` `0x06003995`), đối xứng (`kiemTraDoiXungTheoPhuongb` `0x0600399a`, `timPGocGiua` `0x06003999`) và lưu JSON/XData (`getInfoMongCocSaveJsonData` `0x06003989`, `getInfoMongCocSaveJsonData2` `0x0600398a`); kèm `Draw_foudation` (`0x02000557`, 2 trường, 5 phương thức `0x06005336`..`0x0600533a` với `QuetMong` `0x06005337` theo `VM-178`/`VM-275`) | **E4** (`VM-104`, `VM-178`, `VM-205`, `VM-275`, Type `0x020002ec` 115 methods, Type `0x02000557` 5 methods) |
| `VM-061` | A.7 Module Lõi | Kiến trúc `Rai_Mong_Module` (`0x02000335`, RowOffset `13382350`, **4 trường**, **4 phương thức** theo `VM-110`/`VM-211`: `.cctor` `0x06003c39`, `RM` `0x06003c3a`, `smethod_0` `0x06003c3b`, `hE282oWROh4` `0x06003c3c`), phối hợp với `InfoRaiMong.TinhToanMong` (`0x06003c3e`, `VM-212`), `RaiMongJig.Sampler` (`0x06003c45`, `VM-213`) và `InfoRaiMongDon.TinhToanMong` (`0x06003c4e`, `VM-214`) | **E4**, **E5** (`VM-110`..`VM-113`, `VM-211`..`VM-214`, Type `0x02000335` 4 methods) |
| `VM-062` | A.7 Module Lõi | Kiến trúc `Ve_Mat_Bang_Mong_Module` (`0x02000370`, RowOffset `13383412`, **10 trường**, **19 phương thức** `0x06003ddd`..`0x06003def` theo `VM-114`/`VM-215`: `.cctor` `0x06003ddd`, `nqI8wkiUHU6` `0x06003dde`, `GetViTriDatTen` `0x06003de2`, `EnsureBlockMongCoc` `0x06003de7`, `PhaMong` `0x06003de9`, `UpdateDaiMongBaoCoc` `0x06003dec`, `VMBM` `0x06003ded`), phối hợp với `InfoVeMatBangMong.TinhToanMatBangMong` (`0x06003df2`, `VM-216`), `InfoMBMongBang.TinhToanBaoLanDauSauKhiQuet` (`0x06003e1d`) & `RegenMBMongBang` (`0x06003e1e`, `VM-217`), `InfoCotCocGenMong` (`0x02000380`, 41 hàm `VM-221`) và `InfoMBMongDon.TinhToanMatBangMong` (`0x06003ea3`, `VM-222`) | **E4**, **E5** (`VM-114`, `VM-215`..`VM-222`, Type `0x02000370` 19 methods) |
| `VM-063` | A.7 Module Lõi | Kiến trúc `Foundation_Etabs` (`0x0200052c`, RowOffset `13391404`, **2 trường**, **21 phương thức** `0x060050f6`..`0x0600510a` theo `VM-175`/`VM-272`): `.cctor` (`0x060050f6`), `Get_Moment_Mong` (`0x060050f7`), `Get_JointReaction` (`0x060050f9`), `PromptUserChooseStripTypeAndRebarMaterial` (`0x060050fc`), `GetSelectedStripWidthValue` (`0x060050fe`), `ApplyStripDesignOverwrites` (`0x060050ff`), `yoaEM5nuPJc` (`0x06005101`) | **E4** (`VM-175`, `VM-272`, Type `0x0200052c`, Methods `0x060050f6`..`0x0600510a`) |
| `VM-064` | A.7 Module Lõi | Kiến trúc `AI_DrawIsolatedFoundation_Tool` (`0x020001be`, RowOffset `13375600`, **1 trường**, **17 phương thức** `0x06002c88`..`0x06002c98` theo `VM-102`/`VM-203`: `get_Name` `0x06002c88`, `CreateParametersSchema` `0x06002c8c` RVA `4163300`, `Execute` `0x06002c8d` RVA `4165248`) và `AI_BeamFoundationReferencePdfBuilder` (`0x02000131`, RowOffset `13373062`, **12 trường**, **24 phương thức** `0x06002414`..`0x0600242b` theo `VM-101`/`VM-202`: `GetReferencePdfPath` `0x06002416`, `Build` `0x06002417` RVA `3520728`) | **E4**, **E5** (`VM-101`, `VM-102`, `VM-202`, `VM-203`, Type `0x020001be` 17 methods, Type `0x02000131` 24 methods) |
| `VM-065` | A.7 Module Lõi | Kiến trúc `Form_Chon_Thep_Mong_Tu_Dong` (`0x020000ad`, RowOffset `13370686`, **12 trường**, **31 phương thức** trong `E5` / **55 phương thức** theo `VM-197`, `.cctor` `0x060015a9` RVA `2161692`) điều phối tự động tính & chọn thép móng (`TuyChonTinhThepMongTuDong` `0x020000aa`, `KetQuaTinhThepMongTuDong` `0x020000ab`, `PhuongAnThepMong` `0x020000ac`) | **E5** (`VM-093`..`VM-096`, `VM-194`..`VM-197`, Type `0x020000ad`) |
| `VM-066` | A.7 Module Lõi | Kiến trúc `view_mong_don` (`0x020000cf`, RowOffset `13371298`, **56 trường**, **114 phương thức** theo `VM-097`/`VM-198`: `doi_location` `0x06001c1e`, `ve_hinh_mong_don` `0x06001c1f`, `doi_du_lieu` `0x06001c42` / ref `ve_mong_don` `0x060019b9` RVA `2746232`) và `view_mong_don_new` (`0x020000d0`, RowOffset `13371316`, **89 trường**, **209 phương thức** theo `VM-098`/`VM-199`: `doi_location` `0x06001ce8` RVA `2865176`, `ve_hinh_mong_don` `0x06001ce9` RVA `2866296`, `doi_du_lieu` `0x06001d10` RVA `2878836` / ref `ve_mong_don` `0x06001b16` RVA `2890128`, `GetInfoMongDonSave` `0x06001b6c` RVA `2925884`, `LoadDataSave` `0x06001b6d` RVA `2926944`) | **E5** (`VM-097`, `VM-098`, `VM-198`, `VM-199`, Types `0x020000cf` 114 methods, `0x020000d0` 209 methods) |
| `VM-067` | A.7 VMBM Light | Kiến trúc các lớp dịch vụ VMBM Light: `VmbmLightCad` (`0x020003c1`, 1 trường, 18 hàm theo `VM-134`/`VM-231`), `VmbmLightEtabs` (`0x020003d9`, 7 trường, 51 hàm theo `VM-136`/`VM-235`), `VmbmLightCheck` (`0x02000415`, 1 trường, 10 hàm theo `VM-153`/`VM-247`: `RequireFreeJoint` `0x06004419`, `RequireSpringStiffness` `0x0600441a`, `Finite` `0x0600441d`, `Evaluate` `0x0600441e` RVA `7043452` / ref `Evaluate` `0x0600438d`, `EvaluateCap` `0x0600438e`, `RequiredArea` `0x06004399`, `ChooseSteel` `0x0600439a`), `VmbmLightBeamDesign` (`0x0200041c`, 3 trường, 21/17 hàm theo `VM-154`/`VM-250`: `Resize` `0x0600448b`, `Evaluate` `0x06004490`, `Check` `0x06004495`), `VmbmLightShallowCad` (`0x02000441`, 2 trường, 9 hàm theo `VM-166`/`VM-262`), `VmbmLightEngine` (`0x02000447`, 1 trường, 12 hàm theo `VM-167`/`VM-264`), `VmbmLightGeometry` (`0x02000454`, 1 trường, 7 hàm theo `VM-168`/`VM-265`), `VmbmLightEditing` (`0x02000456`, 1 trường, 24 hàm theo `VM-169`/`VM-266`) | **E6** (`VM-134`, `VM-153`, `VM-154`, `VM-166`..`VM-169`, `VM-231`, `VM-235`, `VM-247`, `VM-250`, `VM-262`, `VM-264`..`VM-266`, Tokens `0x020003c1`, `0x020003d9`, `0x02000415`, `0x0200041c`, `0x02000441`, `0x02000447`, `0x02000454`, `0x02000456`) |
| `VM-068` | A.7 VMBM Light & Revit | Kiến trúc thiết kế móng nông VMBM Light (`VmbmLightShallowSession` `0x0200043c`: `Regenerate` `0x060045fa` RVA `7119648` / ref `0x060045f2` RVA `7114744`, `FitIsolatedBoundary` `0x060045fd` RVA `7123100` / ref `0x060045f5` RVA `7116852` theo `VM-261`; `VmbmLightShallowCad` `0x02000441`: `Read` `0x06004622` RVA `7128408` / ref `0x0600461a` RVA `7134656`, `Draw` `0x06004624` RVA `7139376` / ref `0x0600461c` RVA `7139376` theo `VM-262`; `VmbmLightShallowExport` `0x02000433`: `Run` `0x0600454c` RVA `7088116` / ref `Export` `0x06004548` theo `VM-256`) và lớp `Kata_Class_Lib_Revit.Element_Select` (`0x02000071`: `quet_chon_mong_coc` `0x0600038b`, `quet_chon_mong_don_mong_bang` `0x0600038c`, `chon_nhieu_mong` `0x06000390`, `chon_1_mong` `0x06000391`, `chon_1_mong_tong_hop` `0x06000392`) | **E6**, **E9**, **E10** (`VM-256`, `VM-261`, `VM-262`, `VM-284`, Tokens `0x02000433`, `0x0200043c`, `0x02000441`, `0x02000071`) |
| `VM-069` | A.7 WinForms & Revit | Tổng hợp số lượng trường/phương thức chính xác của các lớp giao diện WinForms: `Form_mong_coc` (`0x02000073`: 139 trường, **350 phương thức** theo `VM-089`/`VM-190`), `Form_Mong_Coc_V2` (`0x02000074`: 186 trường, **406** / **396 phương thức** theo `VM-090`/`VM-191`), `Form_Rai_Mong` (`0x0200007d`: 70 trường, **143 phương thức** theo `VM-091`/`VM-192`), `Form_Ve_Mat_Bang_Mong` (`0x0200009d`: 278 trường, **650 phương thức** theo `VM-092`/`VM-193`), `Form_Edit_Coc` (`0x02000065`: 10 trường, **24 phương thức** theo `VM-088`/`VM-189`), `Form_Update_Dai_Mong_Bao_Coc` (`0x0200038c`: 9 trường, **10 phương thức** theo `VM-127`/`VM-227`), `Form_VMBM_Light` (`0x02000390`: 37 trường, **53 phương thức** theo `VM-128`/`VM-228`), `Form_VMBM_Light_Edit` (`0x02000476`: 36 trường, **99 phương thức** theo `VM-171`/`VM-267`), `Form_VMBM_Light_Name` (`0x02000039`: 3 trường, **8 phương thức** theo `VM-086`/`VM-187`), `VmbmLightOptionsForm` (`0x020003a7`: 2 trường, **4 phương thức** theo `VM-130`/`VM-230`), `VmbmLightShallowView` (`0x020003aa`: 12 trường, **35 phương thức** theo `VM-131`/`VM-231`), `VmbmLightShallowExportForm` (`0x02000436`: 9 trường, **9 phương thức** theo `VM-161`/`VM-258`), `Form_Lop_Thep_Them` (`0x0200006e` `[class:440]`/`0x02000062`: 48 trường, **111 phương thức**), `Form_Lop_Thep_Them_Ngang` (`0x0200006f` `[class:444]`/`0x02000063`: 48 trường, **108 phương thức**), `Form_Lop_Thep_Them_Tren` (`0x02000070` `[class:448]`/`0x02000064`: 48 trường, **110 phương thức**); cùng 4 bộ lọc `ISelectionFilter` Revit (`0x02000077`, `0x02000078`, `0x0200007d`, `0x0200007e` theo `VM-285`..`VM-288`) | **E5**, **E6**, **E7**, **E8**, **E10** (`VM-086`..`VM-092`, `VM-127`..`VM-131`, `VM-161`, `VM-171`, `VM-187`..`VM-193`, `VM-227`..`VM-231`, `VM-258`, `VM-267`, `VM-285`..`VM-288`) |
| `VM-070` | A.8 Bảo vệ CIL | Stub thân hàm `il_size=4` (`tiny`) với 6 mẫu CIL cố định theo kiểu trả về (`void`, `class/string/object`, `bool`, `i4`, `i8`, `r8`) | **E4**–**E10** (`body.il_size=4`, `body.il_sha256`) |
| `VM-071` | A.8 Bảo vệ CIL | Stub thân hàm `il_size=8` (`tiny`) cho `valuetype` (`nop; ldnull; unbox.any <ValueTypeToken>; ret`) | **E4**–**E6** (`body.il_size=8`, `body.anchors`) |
| `VM-072` | A.8 Bảo vệ CIL | Cặp phương thức bảo vệ tĩnh ở cuối mỗi lớp (`bool` `66ea1533...` và `<ClassType>` `8969ce0c...`) kèm trường tĩnh tự tham chiếu | **E4**–**E10** |
| `VM-073` | A.8 Bảo vệ PE | `Kata_pro64.dll` chứa `Duplicate metadata stream #GUID`; `kata_pro.dll` là native VB6 COM server (`MSVBVM60.DLL`) | **E12**, **E13** |
| `VM-074`..`VM-085` | A.5 `.cctor` | Chi tiết đầy đủ `call_edges`, `field_accesses` và `body.anchors` của các `.cctor` trọng tâm (`0x060015a9`, `0x06002414`, `0x06003988`, `0x06003c39`, `0x06003ddd`, `0x060050f6`, `0x06001f8d`, `0x06001f8a`, `0x0600350c`, `0x060053db`, `0x060055c7`) | **E4**–**E8** (Mục 2.5) |
| `VM-086`..`VM-186` | A.6 Schema | Bảng đặc tả đầy đủ 100% các trường (`Field Token`, `Name`, `Type`, `Flags`, `RowOffset`, `Constant`) của 101 kiểu dữ liệu (`E4`..`E7`) + 13 kiểu DTO con/Form (`E8`, 247 trường tại Mục 2.6.2) | **E4**–**E8** (Mục 2.6) |
| `VM-187`..`VM-282` | A.7 Methods | Bảng đặc tả đầy đủ 100% các phương thức (`Method Token`, `Name`, `Parameters`, `Return`, `RVA`, `IL Size`, `SHA-256`) của 96 kiểu (`E4`..`E7`) + 13 kiểu (`E8`, 391 phương thức tại Mục 2.7.1b) | **E4**–**E8** (Mục 2.7) |
| `VM-283`..`VM-295` | A.7 & A.8 | Bảng đặc tả các lớp chọn lọc móng trên Revit (`Kata_Class_Lib_Revit2025.dll`) và ma trận đối chiếu `il_sha256` của toàn bộ stub thân hàm | **E9**–**E11** (Mục 2.7.2 & 2.8) |
| `VM-086` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.Form_VMBM_Light_Name` (Kế thừa `0x0100000f (System.Windows.Forms.Form)`) | `ev_549bfe33ecd78257ce27570bdd19331f304e770621ccd33a14059b374ac982e3`, Token `0x02000039`, RowOffset `13368598`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-087` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.Edit_moc_dai` (Kế thừa `0x0100000f (System.Windows.Forms.Form)`) | `ev_549bfe33ecd78257ce27570bdd19331f304e770621ccd33a14059b374ac982e3`, Token `0x02000059`, RowOffset `13369174`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-088` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.Form_Edit_Coc` (Kế thừa `0x0100000f (System.Windows.Forms.Form)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x02000065`, RowOffset `13369390`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-089` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.Form_mong_coc` (Kế thừa `0x0100000f (System.Windows.Forms.Form)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x02000073`, RowOffset `13369642`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-090` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.Form_Mong_Coc_V2` (Kế thừa `0x0100000f (System.Windows.Forms.Form)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x02000074`, RowOffset `13369660`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-091` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.Form_Rai_Mong` (Kế thừa `0x0100000f (System.Windows.Forms.Form)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x0200007d`, RowOffset `13369822`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-092` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.Form_Ve_Mat_Bang_Mong` (Kế thừa `0x0100000f (System.Windows.Forms.Form)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x0200009d`, RowOffset `13370398`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-093` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.TuyChonTinhThepMongTuDong` (Kế thừa `0x0100000b (System.Object)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x020000aa`, RowOffset `13370632`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-094` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.KetQuaTinhThepMongTuDong` (Kế thừa `0x0100000b (System.Object)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x020000ab`, RowOffset `13370650`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-095` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.PhuongAnThepMong` (Kế thừa `0x0100000b (System.Object)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x020000ac`, RowOffset `13370668`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-096` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.Form_Chon_Thep_Mong_Tu_Dong` (Kế thừa `0x0100000f (System.Windows.Forms.Form)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x020000ad`, RowOffset `13370686`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-097` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.view_mong_don` (Kế thừa `0x0100000f (System.Windows.Forms.Form)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x020000cf`, RowOffset `13371298`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-098` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.view_mong_don_new` (Kế thừa `0x0100000f (System.Windows.Forms.Form)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x020000d0`, RowOffset `13371316`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-099` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.Acad` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x020000d5`, RowOffset `13371406`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-100` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.Lenh_thep` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x020000d6`, RowOffset `13371424`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-101` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.AI_BeamFoundationReferencePdfBuilder` (Kế thừa `0x0100000b (System.Object)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x02000131`, RowOffset `13373062`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-102` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.AI_DrawIsolatedFoundation_Tool` (Kế thừa `0x02000253 (Kata_pro64_Cad2025.AIToolBase)`) | `ev_3bdf54e817a9b5840d6618afe7ca3edeaabd4e8c3021dcb361d608566084bf9d`, Token `0x020001be`, RowOffset `13375600`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-103` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.kata1` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200027b`, RowOffset `13379002`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-104` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.Mong_coc` (Kế thừa `0x0100000b (System.Object)`) | `ev_3bdf54e817a9b5840d6618afe7ca3edeaabd4e8c3021dcb361d608566084bf9d`, Token `0x020002ec`, RowOffset `13381036`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-105` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `InfoMongCoc` (Kế thừa `0x0100000b (System.Object)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x020002ed`, RowOffset `13381054`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-106` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `InfoUserMongCoc` (Kế thừa `0x0100000b (System.Object)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x020002f1`, RowOffset `13381126`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-107` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `CoversMong` (Kế thừa `0x0100000b (System.Object)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x020002f2`, RowOffset `13381144`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-108` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `InfoMongCocSave` (Kế thừa `0x0100000b (System.Object)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x020002f5`, RowOffset `13381198`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-109` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `InfoMongCocSaveV2` (Kế thừa `0x0100000b (System.Object)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x0200031b`, RowOffset `13381882`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-110` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.Rai_Mong_Module` (Kế thừa `0x0100000b (System.Object)`) | `ev_3bdf54e817a9b5840d6618afe7ca3edeaabd4e8c3021dcb361d608566084bf9d`, Token `0x02000335`, RowOffset `13382350`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-111` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `InfoRaiMong` (Kế thừa `0x0100000b (System.Object)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x02000336`, RowOffset `13382368`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-112` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `RaiMongJig` (Kế thừa `0x01000014 (Autodesk.AutoCAD.EditorInput.DrawJig)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x02000337`, RowOffset `13382386`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-113` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `InfoRaiMongDon` (Kế thừa `0x0100000b (System.Object)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x02000338`, RowOffset `13382404`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-114` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.Ve_Mat_Bang_Mong_Module` (Kế thừa `0x0100000b (System.Object)`) | `ev_3bdf54e817a9b5840d6618afe7ca3edeaabd4e8c3021dcb361d608566084bf9d`, Token `0x02000370`, RowOffset `13383412`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-115` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `ModeBuildEtabEnum` (Kế thừa `0x01000011 (System.Enum)`) | `ev_549bfe33ecd78257ce27570bdd19331f304e770621ccd33a14059b374ac982e3`, Token `0x02000371`, RowOffset `13383430`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-116` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `InfoVeMatBangMong` (Kế thừa `0x0100000b (System.Object)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x02000372`, RowOffset `13383448`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-117` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `ViTriCurveDam` (Kế thừa `0x01000011 (System.Enum)`) | `ev_549bfe33ecd78257ce27570bdd19331f304e770621ccd33a14059b374ac982e3`, Token `0x02000375`, RowOffset `13383502`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-118` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `InfoMBMongBang` (Kế thừa `0x0100000b (System.Object)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x02000377`, RowOffset `13383538`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-119` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `ViTriDatTen` (Kế thừa `0x01000011 (System.Enum)`) | `ev_549bfe33ecd78257ce27570bdd19331f304e770621ccd33a14059b374ac982e3`, Token `0x0200037b`, RowOffset `13383610`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-120` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `CocChiuTai` (Kế thừa `0x0100000b (System.Object)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x0200037c`, RowOffset `13383628`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-121` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `MongJsonDto` (Kế thừa `0x0100000b (System.Object)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x0200037e`, RowOffset `13383664`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-122` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `MongBangJsonDto` (Kế thừa `0x0100000b (System.Object)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x0200037f`, RowOffset `13383682`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-123` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `InfoCotCocGenMong` (Kế thừa `0x0100000b (System.Object)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x02000380`, RowOffset `13383700`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-124` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `InfoMBMongDon` (Kế thừa `0x0100000b (System.Object)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x02000388`, RowOffset `13383844`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-125` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `CotChiuTaiMongDon` (Kế thừa `0x0100000b (System.Object)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x0200038a`, RowOffset `13383880`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-126` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `KieuTaoDaiMongBaoCoc` (Kế thừa `0x01000011 (System.Enum)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x0200038b`, RowOffset `13383898`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-127` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Form_Update_Dai_Mong_Bao_Coc` (Kế thừa `0x0100000f (System.Windows.Forms.Form)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x0200038c`, RowOffset `13383916`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-128` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.Form_VMBM_Light` (Kế thừa `0x0100000f (System.Windows.Forms.Form)`) | `ev_549bfe33ecd78257ce27570bdd19331f304e770621ccd33a14059b374ac982e3`, Token `0x02000390`, RowOffset `13383988`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-129` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightPreview` (Kế thừa `0x01000018 (System.Windows.Forms.Control)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200039f`, RowOffset `13384258`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-130` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightOptionsForm` (Kế thừa `0x0100000f (System.Windows.Forms.Form)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x020003a7`, RowOffset `13384402`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-131` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightShallowView` (Kế thừa `0x01000013 (System.Windows.Forms.UserControl)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x020003aa`, RowOffset `13384456`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-132` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightFootingProperties` (Kế thừa `0x01000019 (System.ComponentModel.CustomTypeDescriptor)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x020003b2`, RowOffset `13384600`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-133` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightShallowSettingsProperties` (Kế thừa `0x01000019 (System.ComponentModel.CustomTypeDescriptor)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x020003b4`, RowOffset `13384636`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-134` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightShallowCanvas` (Kế thừa `0x01000018 (System.Windows.Forms.Control)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x020003b5`, RowOffset `13384654`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-135` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightCad` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x020003c1`, RowOffset `13384870`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-136` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightCommand` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x020003d7`, RowOffset `13385266`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-137` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightSteelAxis` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x020003d8`, RowOffset `13385284`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-138` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightEtabs` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x020003d9`, RowOffset `13385302`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-139` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightSlabCapInput` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x020003f5`, RowOffset `13385806`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-140` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightFoundationInput` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x020003f6`, RowOffset `13385824`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-141` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightLayoutIdentity` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000402`, RowOffset `13386040`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-142` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightSourceFiles` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000409`, RowOffset `13386166`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-143` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightOptions` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200040c`, RowOffset `13386220`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-144` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightSupport` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200040d`, RowOffset `13386238`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-145` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightCap` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200040e`, RowOffset `13386256`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-146` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightPileResult` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200040f`, RowOffset `13386274`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-147` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightCapCheck` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000410`, RowOffset `13386292`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-148` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightIteration` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000411`, RowOffset `13386310`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-149` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightSession` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000412`, RowOffset `13386328`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-150` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightCheck` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000415`, RowOffset `13386382`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-151` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightBeam` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200041a`, RowOffset `13386472`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-152` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightBeamCheck` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200041b`, RowOffset `13386490`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-153` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightBeamDesign` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200041c`, RowOffset `13386508`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-154` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightStripTypeConverter` (Kế thừa `0x0100001a (System.ComponentModel.StringConverter)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200042a`, RowOffset `13386760`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-155` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightRebarConverter` (Kế thừa `0x0200042a (Kata_pro64_Cad2025.VmbmLightStripTypeConverter)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200042b`, RowOffset `13386778`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-156` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightShallowExportRequest` (Kế thừa `0x0200042e (Kata_pro64_Cad2025.EtabsModelExportRequest)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200042d`, RowOffset `13386814`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-157` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightNewPileExport` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200042f`, RowOffset `13386850`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-158` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightLegacyExportScope` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000430`, RowOffset `13386868`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-159` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightShallowExport` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000433`, RowOffset `13386922`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-160` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightExistingPileExport` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000435`, RowOffset `13386958`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-161` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightShallowExportForm` (Kế thừa `0x0100000f (System.Windows.Forms.Form)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000436`, RowOffset `13386976`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-162` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightShallowKind` (Kế thừa `0x01000011 (System.Enum)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000439`, RowOffset `13387030`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-163` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightFootingInput` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200043a`, RowOffset `13387048`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-164` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightShallowSettings` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200043b`, RowOffset `13387066`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-165` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightShallowSession` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200043c`, RowOffset `13387084`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-166` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightShallowCad` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000441`, RowOffset `13387174`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-167` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.IVmbmLightAnalysis` (Kế thừa `none`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000446`, RowOffset `13387264`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-168` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightEngine` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000447`, RowOffset `13387282`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-169` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightGeometry` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000454`, RowOffset `13387516`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-170` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightEditing` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000456`, RowOffset `13387552`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-171` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.Form_VMBM_Light_Edit` (Kế thừa `0x0100000f (System.Windows.Forms.Form)`) | `ev_549bfe33ecd78257ce27570bdd19331f304e770621ccd33a14059b374ac982e3`, Token `0x02000476`, RowOffset `13388128`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-172` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightEtabsMetadata` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000490`, RowOffset `13388596`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-173` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightMesh` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000491`, RowOffset `13388614`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-174` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `InfoMongDonSave` (Kế thừa `0x0100000b (System.Object)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x020004d2`, RowOffset `13389784`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-175` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `InfoMongDonRenameSave` (Kế thừa `0x0100000b (System.Object)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x020004d3`, RowOffset `13389802`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-176` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.Foundation_Etabs` (Kế thừa `0x0100000b (System.Object)`) | `ev_3bdf54e817a9b5840d6618afe7ca3edeaabd4e8c3021dcb361d608566084bf9d`, Token `0x0200052c`, RowOffset `13391404`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-177` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `ThepBoTriMong` (Kế thừa `0x0100000b (System.Object)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x0200052f`, RowOffset `13391458`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-178` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `MomentMong` (Kế thừa `0x0100000b (System.Object)`) | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x02000530`, RowOffset `13391476`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-179` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.Draw_foudation` (Kế thừa `0x0100000b (System.Object)`) | `ev_3bdf54e817a9b5840d6618afe7ca3edeaabd4e8c3021dcb361d608566084bf9d`, Token `0x02000557`, RowOffset `13392178`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-180` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.kata7` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000566`, RowOffset `13392448`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-181` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightCadBinding` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000583`, RowOffset `13392970`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-182` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightCadBeamBinding` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000584`, RowOffset `13392988`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-183` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightStripBinding` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000585`, RowOffset `13393006`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-184` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightState` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000586`, RowOffset `13393024`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-185` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.VmbmLightTableSelection` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200058d`, RowOffset `13393150`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-186` | Schema DTO / Class / Form | Cấu trúc 100% trường (Fields) của kiểu `Kata_pro64_Cad2025.kata8` (Kế thừa `0x0100000b (System.Object)`) | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200058e`, RowOffset `13393168`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-187` | Chữ ký hàm (Method Signatures) | Tập phương thức (`8/8` hàm) của kiểu `Kata_pro64_Cad2025.Form_VMBM_Light_Name` kèm RVA, `il_size`, `il_sha256` | `ev_549bfe33ecd78257ce27570bdd19331f304e770621ccd33a14059b374ac982e3`, Token `0x02000039`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-188` | Chữ ký hàm (Method Signatures) | Tập phương thức (`20/20` hàm) của kiểu `Kata_pro64_Cad2025.Edit_moc_dai` kèm RVA, `il_size`, `il_sha256` | `ev_549bfe33ecd78257ce27570bdd19331f304e770621ccd33a14059b374ac982e3`, Token `0x02000059`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-189` | Chữ ký hàm (Method Signatures) | Tập phương thức (`24/24` hàm) của kiểu `Kata_pro64_Cad2025.Form_Edit_Coc` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x02000065`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-190` | Chữ ký hàm (Method Signatures) | Tập phương thức (`350/350` hàm) của kiểu `Kata_pro64_Cad2025.Form_mong_coc` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x02000073`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-191` | Chữ ký hàm (Method Signatures) | Tập phương thức (`406/406` hàm) của kiểu `Kata_pro64_Cad2025.Form_Mong_Coc_V2` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x02000074`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-192` | Chữ ký hàm (Method Signatures) | Tập phương thức (`143/143` hàm) của kiểu `Kata_pro64_Cad2025.Form_Rai_Mong` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x0200007d`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-193` | Chữ ký hàm (Method Signatures) | Tập phương thức (`650/650` hàm) của kiểu `Kata_pro64_Cad2025.Form_Ve_Mat_Bang_Mong` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x0200009d`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-194` | Chữ ký hàm (Method Signatures) | Tập phương thức (`4/4` hàm) của kiểu `Kata_pro64_Cad2025.TuyChonTinhThepMongTuDong` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x020000aa`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-195` | Chữ ký hàm (Method Signatures) | Tập phương thức (`4/4` hàm) của kiểu `Kata_pro64_Cad2025.KetQuaTinhThepMongTuDong` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x020000ab`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-196` | Chữ ký hàm (Method Signatures) | Tập phương thức (`5/5` hàm) của kiểu `Kata_pro64_Cad2025.PhuongAnThepMong` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x020000ac`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-197` | Chữ ký hàm (Method Signatures) | Tập phương thức (`31/31` hàm) của kiểu `Kata_pro64_Cad2025.Form_Chon_Thep_Mong_Tu_Dong` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x020000ad`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-198` | Chữ ký hàm (Method Signatures) | Tập phương thức (`114/114` hàm) của kiểu `Kata_pro64_Cad2025.view_mong_don` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x020000cf`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-199` | Chữ ký hàm (Method Signatures) | Tập phương thức (`209/209` hàm) của kiểu `Kata_pro64_Cad2025.view_mong_don_new` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x020000d0`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-200` | Chữ ký hàm (Method Signatures) | Tập phương thức (`5/5` hàm) của kiểu `Kata_pro64_Cad2025.Acad / Class28 ref` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x020000d5`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-201` | Chữ ký hàm (Method Signatures) | Tập phương thức (`166/166` hàm) của kiểu `Kata_pro64_Cad2025.Lenh_thep` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x020000d6`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-202` | Chữ ký hàm (Method Signatures) | Tập phương thức (`24/24` hàm) của kiểu `Kata_pro64_Cad2025.AI_BeamFoundationReferencePdfBuilder` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x02000131`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-203` | Chữ ký hàm (Method Signatures) | Tập phương thức (`17/17` hàm) của kiểu `Kata_pro64_Cad2025.AI_DrawIsolatedFoundation_Tool` kèm RVA, `il_size`, `il_sha256` | `ev_3bdf54e817a9b5840d6618afe7ca3edeaabd4e8c3021dcb361d608566084bf9d`, Token `0x020001be`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-204` | Chữ ký hàm (Method Signatures) | Tập phương thức (`56/56` hàm) của kiểu `Kata_pro64_Cad2025.kata1` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200027b`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-205` | Chữ ký hàm (Method Signatures) | Tập phương thức (`115/115` hàm) của kiểu `Kata_pro64_Cad2025.Mong_coc` kèm RVA, `il_size`, `il_sha256` | `ev_3bdf54e817a9b5840d6618afe7ca3edeaabd4e8c3021dcb361d608566084bf9d`, Token `0x020002ec`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-206` | Chữ ký hàm (Method Signatures) | Tập phương thức (`7/7` hàm) của kiểu `InfoMongCoc` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x020002ed`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-207` | Chữ ký hàm (Method Signatures) | Tập phương thức (`4/4` hàm) của kiểu `InfoUserMongCoc` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x020002f1`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-208` | Chữ ký hàm (Method Signatures) | Tập phương thức (`4/4` hàm) của kiểu `CoversMong` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x020002f2`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-209` | Chữ ký hàm (Method Signatures) | Tập phương thức (`4/4` hàm) của kiểu `InfoMongCocSave` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x020002f5`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-210` | Chữ ký hàm (Method Signatures) | Tập phương thức (`4/4` hàm) của kiểu `InfoMongCocSaveV2` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x0200031b`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-211` | Chữ ký hàm (Method Signatures) | Tập phương thức (`4/4` hàm) của kiểu `Kata_pro64_Cad2025.Rai_Mong_Module` kèm RVA, `il_size`, `il_sha256` | `ev_3bdf54e817a9b5840d6618afe7ca3edeaabd4e8c3021dcb361d608566084bf9d`, Token `0x02000335`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-212` | Chữ ký hàm (Method Signatures) | Tập phương thức (`7/7` hàm) của kiểu `InfoRaiMong` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x02000336`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-213` | Chữ ký hàm (Method Signatures) | Tập phương thức (`9/9` hàm) của kiểu `RaiMongJig` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x02000337`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-214` | Chữ ký hàm (Method Signatures) | Tập phương thức (`10/10` hàm) của kiểu `InfoRaiMongDon` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x02000338`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-215` | Chữ ký hàm (Method Signatures) | Tập phương thức (`19/19` hàm) của kiểu `Kata_pro64_Cad2025.Ve_Mat_Bang_Mong_Module` kèm RVA, `il_size`, `il_sha256` | `ev_3bdf54e817a9b5840d6618afe7ca3edeaabd4e8c3021dcb361d608566084bf9d`, Token `0x02000370`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-216` | Chữ ký hàm (Method Signatures) | Tập phương thức (`12/12` hàm) của kiểu `InfoVeMatBangMong` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x02000372`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-217` | Chữ ký hàm (Method Signatures) | Tập phương thức (`15/15` hàm) của kiểu `InfoMBMongBang` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x02000377`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-218` | Chữ ký hàm (Method Signatures) | Tập phương thức (`7/7` hàm) của kiểu `CocChiuTai` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x0200037c`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-219` | Chữ ký hàm (Method Signatures) | Tập phương thức (`4/4` hàm) của kiểu `MongJsonDto` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x0200037e`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-220` | Chữ ký hàm (Method Signatures) | Tập phương thức (`4/4` hàm) của kiểu `MongBangJsonDto` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x0200037f`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-221` | Chữ ký hàm (Method Signatures) | Tập phương thức (`41/41` hàm) của kiểu `InfoCotCocGenMong` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x02000380`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-222` | Chữ ký hàm (Method Signatures) | Tập phương thức (`7/7` hàm) của kiểu `InfoMBMongDon` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x02000388`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-223` | Chữ ký hàm (Method Signatures) | Tập phương thức (`8/8` hàm) của kiểu `CotChiuTaiMongDon` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x0200038a`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-224` | Chữ ký hàm (Method Signatures) | Tập phương thức (`10/10` hàm) của kiểu `Form_Update_Dai_Mong_Bao_Coc` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x0200038c`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-225` | Chữ ký hàm (Method Signatures) | Tập phương thức (`53/53` hàm) của kiểu `Kata_pro64_Cad2025.Form_VMBM_Light` kèm RVA, `il_size`, `il_sha256` | `ev_549bfe33ecd78257ce27570bdd19331f304e770621ccd33a14059b374ac982e3`, Token `0x02000390`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-226` | Chữ ký hàm (Method Signatures) | Tập phương thức (`30/30` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightPreview` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200039f`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-227` | Chữ ký hàm (Method Signatures) | Tập phương thức (`4/4` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightOptionsForm` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x020003a7`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-228` | Chữ ký hàm (Method Signatures) | Tập phương thức (`35/35` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightShallowView` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x020003aa`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-229` | Chữ ký hàm (Method Signatures) | Tập phương thức (`7/7` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightFootingProperties` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x020003b2`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-230` | Chữ ký hàm (Method Signatures) | Tập phương thức (`7/7` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightShallowSettingsProperties` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x020003b4`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-231` | Chữ ký hàm (Method Signatures) | Tập phương thức (`27/27` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightShallowCanvas` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x020003b5`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-232` | Chữ ký hàm (Method Signatures) | Tập phương thức (`31/31` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightCad` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x020003c1`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-233` | Chữ ký hàm (Method Signatures) | Tập phương thức (`4/4` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightCommand` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x020003d7`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-234` | Chữ ký hàm (Method Signatures) | Tập phương thức (`10/10` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightSteelAxis` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x020003d8`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-235` | Chữ ký hàm (Method Signatures) | Tập phương thức (`51/51` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightEtabs` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x020003d9`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-236` | Chữ ký hàm (Method Signatures) | Tập phương thức (`10/10` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightSlabCapInput` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x020003f5`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-237` | Chữ ký hàm (Method Signatures) | Tập phương thức (`19/19` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightFoundationInput` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x020003f6`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-238` | Chữ ký hàm (Method Signatures) | Tập phương thức (`14/14` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightLayoutIdentity` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000402`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-239` | Chữ ký hàm (Method Signatures) | Tập phương thức (`6/6` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightSourceFiles` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000409`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-240` | Chữ ký hàm (Method Signatures) | Tập phương thức (`87/87` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightOptions` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200040c`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-241` | Chữ ký hàm (Method Signatures) | Tập phương thức (`14/14` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightSupport` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200040d`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-242` | Chữ ký hàm (Method Signatures) | Tập phương thức (`26/26` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightCap` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200040e`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-243` | Chữ ký hàm (Method Signatures) | Tập phương thức (`24/24` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightPileResult` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200040f`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-244` | Chữ ký hàm (Method Signatures) | Tập phương thức (`17/17` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightCapCheck` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000410`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-245` | Chữ ký hàm (Method Signatures) | Tập phương thức (`38/38` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightIteration` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000411`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-246` | Chữ ký hàm (Method Signatures) | Tập phương thức (`117/117` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightSession` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000412`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-247` | Chữ ký hàm (Method Signatures) | Tập phương thức (`10/10` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightCheck` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000415`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-248` | Chữ ký hàm (Method Signatures) | Tập phương thức (`27/27` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightBeam` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200041a`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-249` | Chữ ký hàm (Method Signatures) | Tập phương thức (`45/45` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightBeamCheck` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200041b`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-250` | Chữ ký hàm (Method Signatures) | Tập phương thức (`21/21` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightBeamDesign` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200041c`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-251` | Chữ ký hàm (Method Signatures) | Tập phương thức (`7/7` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightStripTypeConverter` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200042a`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-252` | Chữ ký hàm (Method Signatures) | Tập phương thức (`7/7` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightRebarConverter` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200042b`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-253` | Chữ ký hàm (Method Signatures) | Tập phương thức (`4/4` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightShallowExportRequest` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200042d`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-254` | Chữ ký hàm (Method Signatures) | Tập phương thức (`5/5` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightNewPileExport` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200042f`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-255` | Chữ ký hàm (Method Signatures) | Tập phương thức (`5/5` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightLegacyExportScope` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000430`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-256` | Chữ ký hàm (Method Signatures) | Tập phương thức (`6/6` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightShallowExport` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000433`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-257` | Chữ ký hàm (Method Signatures) | Tập phương thức (`8/8` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightExistingPileExport` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000435`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-258` | Chữ ký hàm (Method Signatures) | Tập phương thức (`9/9` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightShallowExportForm` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000436`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-259` | Chữ ký hàm (Method Signatures) | Tập phương thức (`39/39` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightFootingInput` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200043a`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-260` | Chữ ký hàm (Method Signatures) | Tập phương thức (`54/54` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightShallowSettings` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200043b`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-261` | Chữ ký hàm (Method Signatures) | Tập phương thức (`56/56` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightShallowSession` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200043c`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-262` | Chữ ký hàm (Method Signatures) | Tập phương thức (`9/9` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightShallowCad` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000441`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-263` | Chữ ký hàm (Method Signatures) | Tập phương thức (`4/4` hàm) của kiểu `Kata_pro64_Cad2025.IVmbmLightAnalysis` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000446`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-264` | Chữ ký hàm (Method Signatures) | Tập phương thức (`12/12` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightEngine` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000447`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-265` | Chữ ký hàm (Method Signatures) | Tập phương thức (`7/7` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightGeometry` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000454`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-266` | Chữ ký hàm (Method Signatures) | Tập phương thức (`24/24` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightEditing` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000456`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-267` | Chữ ký hàm (Method Signatures) | Tập phương thức (`99/99` hàm) của kiểu `Kata_pro64_Cad2025.Form_VMBM_Light_Edit` kèm RVA, `il_size`, `il_sha256` | `ev_549bfe33ecd78257ce27570bdd19331f304e770621ccd33a14059b374ac982e3`, Token `0x02000476`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-268` | Chữ ký hàm (Method Signatures) | Tập phương thức (`22/22` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightEtabsMetadata` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000490`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-269` | Chữ ký hàm (Method Signatures) | Tập phương thức (`7/7` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightMesh` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000491`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-270` | Chữ ký hàm (Method Signatures) | Tập phương thức (`4/4` hàm) của kiểu `InfoMongDonSave` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x020004d2`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-271` | Chữ ký hàm (Method Signatures) | Tập phương thức (`5/5` hàm) của kiểu `InfoMongDonRenameSave` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x020004d3`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-272` | Chữ ký hàm (Method Signatures) | Tập phương thức (`21/21` hàm) của kiểu `Kata_pro64_Cad2025.Foundation_Etabs` kèm RVA, `il_size`, `il_sha256` | `ev_3bdf54e817a9b5840d6618afe7ca3edeaabd4e8c3021dcb361d608566084bf9d`, Token `0x0200052c`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-273` | Chữ ký hàm (Method Signatures) | Tập phương thức (`4/4` hàm) của kiểu `ThepBoTriMong` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x0200052f`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-274` | Chữ ký hàm (Method Signatures) | Tập phương thức (`4/4` hàm) của kiểu `MomentMong` kèm RVA, `il_size`, `il_sha256` | `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`, Token `0x02000530`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-275` | Chữ ký hàm (Method Signatures) | Tập phương thức (`5/5` hàm) của kiểu `Kata_pro64_Cad2025.Draw_foudation` kèm RVA, `il_size`, `il_sha256` | `ev_3bdf54e817a9b5840d6618afe7ca3edeaabd4e8c3021dcb361d608566084bf9d`, Token `0x02000557`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-276` | Chữ ký hàm (Method Signatures) | Tập phương thức (`110/110` hàm) của kiểu `Kata_pro64_Cad2025.kata7` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000566`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-277` | Chữ ký hàm (Method Signatures) | Tập phương thức (`19/19` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightCadBinding` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000583`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-278` | Chữ ký hàm (Method Signatures) | Tập phương thức (`11/11` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightCadBeamBinding` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000584`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-279` | Chữ ký hàm (Method Signatures) | Tập phương thức (`12/12` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightStripBinding` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000585`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-280` | Chữ ký hàm (Method Signatures) | Tập phương thức (`17/17` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightState` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x02000586`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-281` | Chữ ký hàm (Method Signatures) | Tập phương thức (`11/11` hàm) của kiểu `Kata_pro64_Cad2025.VmbmLightTableSelection` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200058d`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-282` | Chữ ký hàm (Method Signatures) | Tập phương thức (`28/28` hàm) của kiểu `Kata_pro64_Cad2025.kata8` kèm RVA, `il_size`, `il_sha256` | `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`, Token `0x0200058e`, `Kata_pro64_Cad2025.dll` (`a1560540...`) |
| `VM-283` | A.7 Kiến trúc Revit & ClassLib | Enum `Loai_dam` trong `Kata_Class_Lib_Revit2025.dll` (`0x0200005f`) và `Kata_Class_Lib.dll` (`0x02000072`): `dam = 0`, `giang_mong = 1`, `dam_mong = 2` | **E10** (`ev_adbc463d...`), **E11** (`ev_834d4667...`) |
| `VM-284` | A.7 Kiến trúc Revit 2025 | Lớp `Kata_Class_Lib_Revit.Element_Select` (`0x02000071`): 20 phương thức chọn đối tượng Revit (5 hàm chọn móng: `0x0600038b`, `0x0600038c`, `0x06000390`, `0x06000391`, `0x06000392`) | **E10** (`ev_adbc463d...`, Token `0x02000071`) |
| `VM-285` | A.7 Kiến trúc Revit 2025 | Bộ lọc `Foundation_SelectionFilter` (`0x02000077`): `AllowElement` (`0x060003c2`), `AllowReference` (`0x060003c3`) | **E10** (`ev_adbc463d...`, Token `0x02000077`) |
| `VM-286` | A.7 Kiến trúc Revit 2025 | Bộ lọc `Foundation_Floor_SelectionFilter` (`0x02000078`): `AllowElement` (`0x060003c8`), `AllowReference` (`0x060003c9`) | **E10** (`ev_adbc463d...`, Token `0x02000078`) |
| `VM-287` | A.7 Kiến trúc Revit 2025 | Bộ lọc `Column_Walls_Foundation_SelectionFilter` (`0x0200007d`): `AllowElement` (`0x060003e6`), `AllowReference` (`0x060003e7`) | **E10** (`ev_adbc463d...`, Token `0x0200007d`) |
| `VM-288` | A.7 Kiến trúc Revit 2025 | Bộ lọc `Column_Foundation_SelectionFilter` (`0x0200007e`): `AllowElement` (`0x060003ec`), `AllowReference` (`0x060003ed`) | **E10** (`ev_adbc463d...`, Token `0x0200007e`) |
| `VM-289`..`VM-295` | A.8 Bảo vệ Thân hàm CIL | Bảng chuẩn hóa 7 mẫu CIL stubbing (`il_size = 4` và `il_size = 8`) cùng mã băm `il_sha256` bất biến trên đĩa | **E4**, **E5**, **E6**, **E7**, **E8**, **E10** |

---

### 2.1. Chi tiết Hệ thống Giao diện CUIX, Lệnh tắt AutoLISP và Điểm vào `.NET [CommandMethod]` (`VM-001` .. `VM-022`)

#### 2.1.1. Định nghĩa Menu và Thanh công cụ trong `C:\kata_pro\menukata.cuix` (Bằng chứng **E14**: `ev_2b923d7155149afca7eefa71152981ab0416c627389481650d65d04d6deb1cc2`, SHA-256 `63115bc3f9ea98945cd21cfcc1efee40f032e1a4053604ddc64c331bf661ed5e`)

- **`[VM-001]` MenuMacro `MMU_0026` (`MenuGroup.cui` dòng 258–266):**
  ```xml
  <MenuMacro UID="MMU_0026">
    <Macro type="Any">
      <Revision MajorVersion="16" MinorVersion="2" UserVersion="1" />
      <ModifiedRev MajorVersion="19" MinorVersion="0" UserVersion="1" />
      <Name xlate="true" UID="XLS_0131">Vẽ móng đơn</Name>
      <Command>^C^Cvmd</Command>
      <SmallImage Name="RCDATA_16244.bmp" />
    </Macro>
  </MenuMacro>
  ```
- **`[VM-002]` PopMenuItem `PMIU_0017` trong trình đơn thả xuống `PMU_0001` (`PopMenuRoot.cui` dòng 4–79):**
  - Trình đơn `PMU_0001` có tên `KATA` (`UID="XLS_0179"`), alias `POP12`, tiêu đề hiển thị `&Kata`.
  - Tại dòng 75–79, mục trình đơn `PMIU_0017` tham chiếu trực tiếp đến `MenuMacroID="MMU_0026"`:
    ```xml
    <PopMenuItem MenuMacroID="MMU_0026" UID="PMIU_0017">
      <ModifiedRev MajorVersion="19" MinorVersion="0" UserVersion="1" />
      <Name xlate="true" UID="RCDATA_19705">&amp;Vẽ móng đơn</Name>
    </PopMenuItem>
    ```
- **`[VM-003]` Thanh công cụ `TB_01` (`ToolbarRoot.cui` dòng 4–120):**
  - Định nghĩa thanh công cụ nổi `UID="TB_01"`, tên `Kata`, thuộc tính `ToolbarOrient="floating" ToolbarVisible="show" xval="200" yval="200" rows="1"`, chứa các nút lệnh cốt thép kết cấu chính (`TBU_0001`..`TBU_0022`).

#### 2.1.2. Bảng Ánh xạ Toàn diện Lệnh tắt AutoLISP (`Goi lenh*.lsp`) sang `[CommandMethod]` trong `Kata_pro64_Cad2025.dll` (`VM-004` .. `VM-022`)

| Mã quy tắc | Lệnh tắt LISP | Định nghĩa trong `Goi lenh.lsp` (AutoCAD) | Định nghĩa trong `Gstarcad.lsp` / `Zwcad.lsp` | Tên lệnh `.NET` (`[CommandMethod]`) | Lớp & Phương thức đích trong `Kata_pro64_Cad2025.dll` | Token / RVA / `il_size` / Bằng chứng |
|---|---|---|---|---|---|---|
| `VM-004` & `VM-013` | **`vmd`** | Dòng 120–122: `(defun c:vmd () (command "ve_mong_don"))` | Dòng 35: `(defun c:vmd () (command "ve_mong_don"))` | `"ve_mong_don"` | `Kata_pro64_Cad2025.Lenh_thep.Ve_mong_new()` | Type `0x020000d6`, Method `0x06001fd5`, RVA `3204136`, `il_size = 4` (**E6**, **E16**) |
| `VM-005` & `VM-014` | **`vmc`** | Dòng 123–125: `(defun c:vmc () (command "ve_mong_coc"))` | Dòng 36: `(defun c:vmc () (command "ve_mong_coc"))` | `"ve_mong_coc"` | `Kata_pro64_Cad2025.Lenh_thep.Ve_mong_coc1()` | Type `0x020000d6`, Method `0x06001fd6`, RVA `3204660`, `il_size = 4` (**E6**, **E16**) |
| `VM-006` & `VM-015` | **`vdm`** | Dòng 130–132: `(defun c:vdm () (command "ve_dam_mong"))` | Dòng 39: `(defun c:vdm () (command "ve_dam_mong"))` | `"ve_dam_mong"` | `Kata_pro64_Cad2025.Lenh_thep.Vedammong()` | Type `0x020000d6`, Method `0x06001fde`, RVA `3206128`, `il_size = 4` (**E6**, **E16**) |
| `VM-007` & `VM-016` | **`vgm`** | Dòng 133–135: `(defun c:vgm () (command "ve_giang_mong"))` | Dòng 40: `(defun c:vgm () (command "ve_giang_mong"))` | `"ve_giang_mong"` | `Kata_pro64_Cad2025.Lenh_thep.Vegiangmong()` | Type `0x020000d6`, Method `0x06001fdf`, RVA `3206324`, `il_size = 4` (**E6**, **E16**) |
| `VM-008` & `VM-017` | **`vgms`** | Dòng 136–138: `(defun c:vgms () (command "ve_giang_shop"))` | Dòng 41: `(defun c:vgms () (command "ve_giang_shop"))` | `"ve_giang_shop"` | `Kata_pro64_Cad2025.Lenh_thep.Vegiangmong_shop()` | Type `0x020000d6`, Method `0x06001fe0`, RVA `3206496`, `il_size = 4` (**E6**, **E16**) |
| `VM-009` & `VM-018` | **`rm`** | Dòng 96–98: `(defun c:rm () (command "rai_mong"))` | Gstarcad dòng 94 / Zwcad dòng 90 | `"rai_mong"` | `Kata_pro64_Cad2025.Lenh_thep.RM1()` -> `Rai_Mong_Module.RM()` (`0x06003c3a`) | Type `0x020000d6`, Method `0x06002000`, RVA `3213520`, `il_size = 4` (**E4**, **E6**, **E16**) |
| `VM-010` & `VM-019` | **`udm`** | Dòng 30–32: `(defun c:udm () (command "udmong"))` | Dòng 13: `(defun c:udm () (command "udmong"))` | `"udmong"` | `Kata_pro64_Cad2025.Lenh_thep.Update_mong()` | Type `0x020000d6`, Method `0x06001f9e`, RVA `3195732`, `il_size = 4` (**E6**, **E16**) |
| `VM-011` & `VM-019` | **`pm`** | Dòng 36–38: `(defun c:pm () (command "phamong"))` | Dòng 15: `(defun c:pm () (command "phamong"))` | `"phamong"` | `Kata_pro64_Cad2025.Lenh_thep.PhaMong1()` | Type `0x020000d6`, Method `0x06001fa0`, RVA `3195764`, `il_size = 4` (**E6**, **E16**) |
| `VM-012` & `VM-020` | **`scoc`** | *(Gọi trực tiếp `"scoc"`)* | Dòng 78: `(defun c:scoc () (command "scoc"))` | `"scoc"` | `Kata_pro64_Cad2025.Lenh_thep.Khai_thong_tin_coc()` | Type `0x020000d6`, Method `0x06001ff2`, RVA `3212028`, `il_size = 4` (**E6**, **E16**) |
| `VM-021` | **`vmbm`** *(Mặt bằng móng cổ điển)* | — | — | Điểm vào module mặt bằng móng | `Kata_pro64_Cad2025.Ve_Mat_Bang_Mong_Module.VMBM()` (`0x06003ded`, RVA `6696300`) / `nqI8wkiUHU6` (`0x06003dde`, RVA `6669544`) | Type `0x02000370`, Methods `0x06003ded` & `0x06003dde`, `il_size = 4` (**E4**) |
| `VM-022` | **`ve_mat_bang_mong`** *(VMBM Light)* | — | — | `[CommandMethod("ve_mat_bang_mong")]` | `Kata_pro64_Cad2025.VmbmLightCommand.ShowVmbmLight()` (`0x0600412d`, RVA `6885100`, `il_size = 4`) và `.cctor()`/`.ctor()` (`0x0600412e`, RVA `6885508` `il_size = 177` / `RVA 6885156, il_size = 8`), cùng `vUMYo0q4tSYkSiBRwLK7` (`0x0600412f`, RVA `6885700`), `FD4GBbq4di30i6TWxEFB` (`0x06004130`, RVA `6885720`) | Type `0x020003d7`, RowOffset `13385266`, Methods `0x0600412d`..`0x06004130` (**E6** `VM-136`/`VM-233`) |

---

### 2.2. Các Thông số Cấu hình Mặc định trong `C:\kata_pro\Goi lenh.lsp` (`VM-023` .. `VM-029`)

Hàm `(defun c:setup_ini () ...)` trong `C:\kata_pro\Goi lenh.lsp` (dòng 155–240, Bằng chứng **E16**) ghi trực tiếp tệp cấu hình mặc định:

| Mã quy tắc | Dòng trong `Goi lenh.lsp` | Nhãn cấu hình ghi vào tệp INI | Giá trị mặc định | Ý nghĩa kỹ thuật trong triển khai móng |
|---|---|---|---|---|
| `VM-023` | Dòng 176–177 | `"Nối thép chờ cột sang móng, có =1 không =0:"` | `"0"` | Mặc định không tách thanh thép chờ riêng tại mặt móng (`0`). Khi bằng `1`, chương trình phát sinh thanh thép chờ neo từ móng lên cột. |
| `VM-024` | Dòng 178–179 | `"Bẻ chân thép chờ cột vào móng:"` | `"250"` | Chiều dài đoạn bẻ chân vuông góc ($L_{\text{hook}} = 250\text{ mm}$) của thanh thép chờ cột đặt trên lưới thép đáy móng. |
| `VM-025` | Dòng 180–181 | `"Cao trình mặt móng:"` | `"-1.500"` | Cao độ mặt trên đài móng/móng đơn mặc định ($-1.500\text{ m}$). |
| `VM-026` | Dòng 188–189 | `"Kiểu bố trí thép cột chờ móng:"` | `"1"` | Kiểu hình học bố trí chân thép chờ cột trong đài móng (kiểu `1`). |
| `VM-027` | Dòng 202–203 | `"Gạch đầu dòng chú thích thép sàn & móng:"` | `"-"` | Ký tự tiền tố phân cách trong chuỗi ghi chú cốt thép sàn và móng. |
| `VM-028` | Dòng 160–167 | `"Đường kính bắt đầu dùng thép gân:"` / `"Chiều dài đoạn neo thép:"` / `"Chiều dài đoạn nối thép:"` / `"Chiều dài tối đa của thanh thép:"` | `"10"` / `"30"` / `"30"` / `"11700"` | Đường kính $d \ge 10\text{ mm}$ dùng thép gân; neo mặc định $30d$, nối mặc định $30d$, thanh nguyên tối đa $11700\text{ mm}$. |
| `VM-029` | Dòng 208–235 | `"Lớp bê tông bảo vệ:"` / `"Bảng tra chiều dài neo, nối theo mác bê tông:"` | `"25"` / `"40 40 35 30 25 20"` | Lớp bảo vệ mặc định chung $25\text{ mm}$ (riêng móng có `CoversMong` `0x020002f2`); dãy hệ số chiều dài neo/nối ($L/d$). |

---

### 2.3. Công thức Tính toán Cốt thép Bản / Móng theo TCVN 5574:2018 trong `C:\kata_pro\Kata.xlsm` (`VM-030` .. `VM-037`)

Bằng chứng **E15** (`ev_bba34942c5436b0f6e9d5af1f505c10736afabaac6c101210702c1674aff7934`, SHA-256 `93036043620b365a0e50d3eba9def3e1d4db66c6081cfb108fc2ae574b82bb96`, trích xuất từ `xl/worksheets/sheet3.xml` — sheet `SLAB DESIGN`):

#### 2.3.1. `[VM-030]` Bảng tra Cường độ Bê tông theo TCVN 5574:2018 (`N6:P16`)

| Tọa độ dòng | Cột `N` (Cấp độ bền bê tông) | Cột `O` ($R_b$, $\text{MPa}$) | Cột `P` ($R_{bt}$, $\text{MPa}$) |
|---|---|---|---|
| Hàng 6 (`N6:P6`) | `B12.5` | `7.5` | `0.66` |
| Hàng 7 (`N7:P7`) | `B15` | `8.5` | `0.75` |
| Hàng 8 (`N8:P8`) | `B20` | `11.5` | `0.9` |
| Hàng 9 (`N9:P9`) | `B25` | `14.5` | `1.05` |
| Hàng 10 (`N10:P10`) | `B30` | `17` | `1.15` |
| Hàng 11 (`N11:P11`) | `B35` | `19.5` | `1.3` |
| Hàng 12 (`N12:P12`) | `B40` | `22` | `1.4` |
| Hàng 13 (`N13:P13`) | `B45` | `25` | `1.5` |
| Hàng 14 (`N14:P14`) | `B50` | `27.5` | `1.6` |
| Hàng 15 (`N15:P15`) | `B55` | `30` | `1.7` |
| Hàng 16 (`N16:P16`) | `B32.5` | `18.25` | `1.225` |

#### 2.3.2. `[VM-031]` Bảng tra Cường độ Cốt thép chịu kéo $R_s$ theo TCVN 5574:2018 (`N19:O25`)

| Tọa độ dòng | Cột `N` (Nhóm / Mác cốt thép) | Cột `O` ($R_s$, $\text{MPa}$) |
|---|---|---|
| Hàng 19 (`N19:O19`) | `CB240-T` | `210` |
| Hàng 20 (`N20:O20`) | `CI,A-I` | `210` |
| Hàng 21 (`N21:O21`) | `CB300-T` | `260` |
| Hàng 22 (`N22:O22`) | `CB300-V` | `260` |
| Hàng 23 (`N23:O23`) | `CII,A-II` | `270` |
| Hàng 24 (`N24:O24`) | `CB400-V` | `350` |
| Hàng 25 (`N25:O25`) | `CIII,A-III` *(hoặc `CB500-V`)* | `355` *(hoặc `435`)* |

#### 2.3.3. `[VM-032` .. `VM-037]` Hệ thống Công thức Tính toán Cốt thép Bản / Đáy Móng (`C4..K44`)

| Mã quy tắc | Tọa độ ô trong `SLAB DESIGN` | Đại lượng thiết kế | Công thức gốc lưu trong XML (`<f>...</f>`) | Biểu thức toán học tương đương & Giá trị mẫu lưu sẵn (`<v>`) |
|---|---|---|---|---|
| `VM-032` | `C4`, `G4`, `C5`, `G5`, `C6`, `C8`, `G8` | Thông số vật liệu & hình học cơ sở | `C5`: `VLOOKUP(C4,N6:P16,2,0)`<br>`G5`: `VLOOKUP(C4,N6:P16,3,0)`<br>`C6`: `VLOOKUP(G4,N19:O25,2,0)` | Với `C4 = "B20"`, `G4 = "CB400-V"`: $R_b = 11.5\text{ MPa}$ (`C5`), $R_{bt} = 0.9\text{ MPa}$ (`G5`), $R_s = 350\text{ MPa}$ (`C6`), $b = 1\text{ m}$ (`C8 = 1`), $a = 20\text{ mm}$ (`G8 = 20`). |
| `VM-033` | `E17:E44` | Chiều cao làm việc hữu ích $h_0$ ($\text{mm}$) | `=C16-$G$8` | $h_0 = h - a$. Ví dụ `C16 = 120`, `G8 = 20` $\Rightarrow$ `E17 = 100` ($\text{mm}$). |
| `VM-034` | `F17:F44` | Hệ số mômen tương đối $\alpha_m$ | `=D17*10^6/($C$5*1000*E17^2*$C$8*1000)` | $$\alpha_m = \frac{M \cdot 10^6}{R_b \cdot b \cdot h_0^2}$$ |
| `VM-035` | `G17:G44` | Hệ số cánh tay đòn nội lực $\zeta$ | `=(1+SQRT(1-2*F17))/2` | $$\zeta = \frac{1 + \sqrt{1 - 2\alpha_m}}{2}$$ |
| `VM-036` | `H17:H44` | Diện tích cốt thép yêu cầu $A_{s,\text{yc}}$ ($\text{cm}^2/\text{m}$) | `=D17*10^6/($C$6*100*G17*E17)` | $$A_{s,\text{yc}} = \frac{M \cdot 10^6}{100 \cdot R_s \cdot \zeta \cdot h_0}\quad (\text{cm}^2/\text{m})$$ |
| `VM-037` | `I17`, `I18`, `J17:J44`, `K17:K44` | Đường kính $\phi$ (`I17`), bước $s$ (`I18`), diện tích chọn $A_{s,\text{chọn}}$ (`J17`) và hàm lượng $\mu$ (`K17`) | `J17`: `=(1000*PI()*I17^2)/(400*I18)`<br>`K17`: `=100*J17/(E17*10)` | $$A_{s,\text{chọn}} = \frac{1000 \cdot \pi \cdot \phi^2}{400 \cdot s},\quad \mu(\%) = \frac{100 \cdot A_{s,\text{chọn}}}{10 \cdot h_0}$$ |

---

### 2.4. Toàn bộ Hệ thống Enum, Hằng số (`Literal Constants`) và Lớp Đặc biệt của Tính năng Móng (`VM-038` .. `VM-045`)

Tất cả các giá trị hằng số và `RowOffset` dưới đây được trích xuất trực tiếp từ ECMA-335 metadata trong `Kata_pro64_Cad2025.dll` (**E5**, **E6**, **E7**), `Kata_Class_Lib_Revit2025.dll` (**E10**) và `Kata_Class_Lib.dll` (**E11**), đồng bộ 100% với Mục 2.6:

| Mã quy tắc | Tên kiểu Enum / Lớp chứa hằng | TypeDef Token & RowOffset | FieldDef Token | Tên trường (`Field Name`) | Kiểu cơ sở | Giá trị Hằng số (`Literal Value`) | Field RowOffset (Đồng bộ Mục 2.6) | Bằng chứng REA |
|---|---|---|---|---|---|---|---|---|
| `VM-038` | `ModeBuildEtabEnum` *(đồng chiếu `KieuRai` `0x0200036f`)* | `0x02000371` (`13383430`, `System.Enum`) | `0x04001f0d` | `value__` | `int32` (`i4`) | *(Trường lưu giá trị)* | `13529186` *(đồng chiếu `13519756`)* | **E7** (`VM-115`) |
| `VM-038` | `ModeBuildEtabEnum` | `0x02000371` (`13383430`) | `0x04001f0e` | **`Normal`** | `ModeBuildEtabEnum` | **`0`** (`Int32`) | `13529196` *(đồng chiếu `13519766`)* | **E7** (`VM-115`) |
| `VM-038` | `ModeBuildEtabEnum` | `0x02000371` (`13383430`) | `0x04001f0f` | **`MongCoc`** | `ModeBuildEtabEnum` | **`1`** (`Int32`) | `13529206` *(đồng chiếu `13519776`)* | **E7** (`VM-115`) |
| `VM-038` | `ModeBuildEtabEnum` | `0x02000371` (`13383430`) | `0x04001f10` | **`MongBang`** | `ModeBuildEtabEnum` | **`2`** (`Int32`) | `13529216` | **E7** (`VM-115`) |
| `VM-038` | `ModeBuildEtabEnum` | `0x02000371` (`13383430`) | `0x04001f11` | **`MongDon`** | `ModeBuildEtabEnum` | **`3`** (`Int32`) | `13529226` | **E7** (`VM-115`) |
| `VM-039` | `ViTriCurveDam` *(đồng chiếu `KieuGopMong` `0x02000376`)* | `0x02000375` (`13383502`, `System.Enum`) | `0x04001f30` | `value__` | `int32` (`i4`) | *(Trường lưu giá trị)* | `13529536` *(đồng chiếu `13520426`)* | **E7** (`VM-117`) |
| `VM-039` | `ViTriCurveDam` | `0x02000375` (`13383502`) | `0x04001f31` | **`Trai`** | `ViTriCurveDam` | **`0`** (`Int32`) | `13529546` *(đồng chiếu `13520436`)* | **E7** (`VM-117`) |
| `VM-039` | `ViTriCurveDam` | `0x02000375` (`13383502`) | `0x04001f32` | **`Phai`** | `ViTriCurveDam` | **`1`** (`Int32`) | `13529556` *(đồng chiếu `13520446`)* | **E7** (`VM-117`) |
| `VM-039` | `ViTriCurveDam` | `0x02000375` (`13383502`) | `0x04001f33` | **`Dau`** | `ViTriCurveDam` | **`2`** (`Int32`) | `13529566` | **E7** (`VM-117`) |
| `VM-039` | `ViTriCurveDam` | `0x02000375` (`13383502`) | `0x04001f34` | **`Cuoi`** | `ViTriCurveDam` | **`3`** (`Int32`) | `13529576` | **E7** (`VM-117`) |
| `VM-040` | `ViTriDatTen` | `0x0200037b` (`13383610`, `System.Enum`) | `0x04001f53` | `value__` | `int32` (`i4`) | *(Trường lưu giá trị)* | `13529886` *(đồng chiếu `13521106`)* | **E7** (`VM-119`) |
| `VM-040` | `ViTriDatTen` | `0x0200037b` (`13383610`) | `0x04001f54` | **`Trai`** | `ViTriDatTen` | **`0`** (`Int32`) | `13529896` *(đồng chiếu `13521116`)* | **E7** (`VM-119`) |
| `VM-040` | `ViTriDatTen` | `0x0200037b` (`13383610`) | `0x04001f55` | **`Phai`** | `ViTriDatTen` | **`1`** (`Int32`) | `13529906` *(đồng chiếu `13521126`)* | **E7** (`VM-119`) |
| `VM-040` | `ViTriDatTen` | `0x0200037b` (`13383610`) | `0x04001f56` | **`Tren`** | `ViTriDatTen` | **`2`** (`Int32`) | `13529916` *(đồng chiếu `13521136`)* | **E7** (`VM-119`) |
| `VM-040` | `ViTriDatTen` | `0x0200037b` (`13383610`) | `0x04001f57` | **`Duoi`** | `ViTriDatTen` | **`3`** (`Int32`) | `13529926` *(đồng chiếu `13521146`)* | **E7** (`VM-119`) |
| `VM-041` | `KieuTaoDaiMongBaoCoc` | `0x0200038b` (`13383898`, `System.Enum`) | `0x04001fde` | `value__` | `int32` (`i4`) | *(Trường lưu giá trị)* | `13531276` *(đồng chiếu `13522756`)* | **E5** (`VM-126`) |
| `VM-041` | `KieuTaoDaiMongBaoCoc` | `0x0200038b` (`13383898`) | `0x04001fdf` | **`GiuNguyenBaoMong`** | `KieuTaoDaiMongBaoCoc` | **`0`** (`Int32`) | `13531286` *(đồng chiếu `13522766`)* | **E5** (`VM-126`) |
| `VM-041` | `KieuTaoDaiMongBaoCoc` | `0x0200038b` (`13383898`) | `0x04001fe0` | **`TaoBaoMongBaoNgoai`** | `KieuTaoDaiMongBaoCoc` | **`1`** (`Int32`) | `13531296` *(đồng chiếu `13522776`)* | **E5** (`VM-126`) |
| `VM-041` | `KieuTaoDaiMongBaoCoc` | `0x0200038b` (`13383898`) | `0x04001fe1` | **`TaoBaoMongVac`** | `KieuTaoDaiMongBaoCoc` | **`2`** (`Int32`) | `13531306` *(đồng chiếu `13522786`)* | **E5** (`VM-126`) |
| `VM-042` | `Kata_pro64_Cad2025.VmbmLightShallowKind` | `0x02000439` (`13387030`, `System.Enum`) | `0x0400236b` | `value__` | `int32` (`i4`) | *(Trường lưu giá trị)* | `13540366` | **E6** (`VM-162`) |
| `VM-042` | `Kata_pro64_Cad2025.VmbmLightShallowKind` | `0x02000439` (`13387030`) | `0x0400236c` | **`Isolated`** | `VmbmLightShallowKind` | **`0`** (`Int32`) | `13540376` | **E6** (`VM-162`) |
| `VM-042` | `Kata_pro64_Cad2025.VmbmLightShallowKind` | `0x02000439` (`13387030`) | `0x0400236d` | **`Strip`** | `VmbmLightShallowKind` | **`1`** (`Int32`) | `13540386` | **E6** (`VM-162`) |
| `VM-043` | `Kata_Class_Lib_Revit.Loai_dam` | `0x0200005f` (`System.Enum`) | `0x0400021b`..`0x0400021d` | **`dam`**, **`giang_mong`**, **`dam_mong`** | `Loai_dam` (`i4`) | **`dam = 0`**, **`giang_mong = 1`**, **`dam_mong = 2`** | `751536`..`751552` | **E10** (`VM-283`) |
| `VM-043` | `Kata_Class_Lib.Loai_dam` | `0x02000072` (`System.Enum`) | `0x04000288`..`0x0400028a` | **`dam`**, **`giang_mong`**, **`dam_mong`** | `Loai_dam` (`i4`) | **`dam = 0`**, **`giang_mong = 1`**, **`dam_mong = 2`** | `623058`..`623070` | **E11** (`VM-283`) |
| `VM-044` | `Kata_pro64_Cad2025.AI_BeamFoundationReferencePdfBuilder` | `0x02000131` (`13373062`, `System.Object`) | `0x04000fdc` | **`ReferencePdfFileName`** | `string` | **`"AI_RebuildBeam_Model_Reference.pdf"`** | `13490296` | **E5** (`VM-101`) |
| `VM-044` | `Kata_pro64_Cad2025.VmbmLightLayoutIdentity` | `0x02000402` (`13386040`, `System.Object`) | `0x040021de` | **`Tolerance`** | `double` (`r8`) | **`0.01`** (`Double`) | `13536396` | **E6** (`VM-141`) |
| `VM-044` | `Kata_pro64_Cad2025.VmbmLightShallowCad` | `0x02000441` (`13387174`, `System.Object`) | `0x040023ba` | **`DataKey`** | `string` | **`"VMBML_SHALLOW"`** | `13541156` | **E6** (`VM-166`) |
| `VM-045` | `Kata_pro64_Cad2025.VmbmLightSteelAxis` | `0x020003d8` (`13385284`, `System.Object`) | `0x0400213d`..`0x04002140` | **4 trường:** `Lry8X6WbOGD` (`string`, `CapId`), `MGs8XWS6LmI` (`r8`, `X`), `diU8XscSdb7` (`r8`, `Y`), `ATTInoq4JD4CXplkN4uo` (`VmbmLightSteelAxis`) | `class` | Không phải `System.Enum` (10 phương thức `get/set_CapId`, `get/set_X`, `get/set_Y`) | `13534786`..`13534816` | **E6** (`VM-137` / `VM-234`) |
| `VM-045` | `VmbmLightCapSteel` | `0x0200040b` (`System.Object`) | `0x040021fc`..`0x04002203` | **8 trường:** backing fields cho `BottomText`, `TopText`, `BottomArea`, `TopArea`, `BottomProvided`, `TopProvided`, `Spacing` | `class` | Không phải `System.Enum` | Theo `VM-143` / `VM-240` | **E6** (`VM-143` / `VM-240`) |
| `VM-045` | `Kata_pro64_Cad2025.VmbmLightLegacyExportScope` | `0x02000430` (`13386868`, `System.Object`, `IDisposable`) | `0x0400234a`..`0x0400234f` | **6 trường:** `meJ8Sd3aCVR` (`List<class:4292>`), `G7M8SJbB6M3` (`r8`), `QoI8SrTTAxo` (`r8`), `F5h8SGlQnuQ` (`bool`), `ivA8SlXBH6k` (`List<KeyValuePair<ObjectId,string>>`), `crYefjqYuHqVQ6irQkpy` (`VmbmLightLegacyExportScope`) | `class` | Không phải `System.Enum` (5 phương thức, `Dispose` `0x0600453e`) | `13540036`..`13540086` | **E6** (`VM-158` / `VM-255`) |

---

### 2.5. Phân tích Chi tiết Khởi tạo Tĩnh (`.cctor`), `call_edges`, `field_accesses` và `body.anchors` (`VM-046` .. `VM-052`, `VM-074` .. `VM-085`)

#### 2.5.1. `[VM-046` & `VM-074]` `Kata_pro64_Cad2025.Form_Chon_Thep_Mong_Tu_Dong..cctor` (`0x060015a9`, RVA `2161692`, `il_size = 489`, `il_sha256 = a381f10edc6a40831c262cc43f281c2833c57ba404a02572d1389e7963fbc1b8`, Bằng chứng **E5** / `VM-096`)

- **5 trường tĩnh được khởi tạo (tên thực tế trong metadata `VM-096`):**
  - `0x0400099b` (`rCJuVn9YhH`, RowOffset `13478886`): kiểu `System.Collections.Generic.List<int>` (`genericinst(class:6193)<i4>` / `class:217<i4>`, **không phải `int[]`**) = `new List<int> { 10, 12, 14, 16, 18, 20 }`.
  - `0x0400099c` (`XB9uavWqWA`, RowOffset `13478896`): kiểu `System.Collections.Generic.List<int>` (`genericinst(class:6193)<i4>` / `class:217<i4>`) = `new List<int> { 10, 12, 14, 16, 18, 20 }`.
  - `0x0400099d` (`PwruzsE0FX`, RowOffset `13478906`): kiểu `System.Collections.Generic.List<int>` (`genericinst(class:6193)<i4>` / `class:217<i4>`) = `new List<int> { 100, 150, 200 }`.
  - `0x0400099e` (`HYJHUIsgUA`, RowOffset `13478916`): kiểu `System.Collections.Generic.List<int>` (`genericinst(class:6193)<i4>` / `class:217<i4>`) = `new List<int> { 100, 150, 200 }`.
  - `0x0400099f` (`e5ZH8K1Tlq`, RowOffset `13478926`): kiểu `bool` = `false` (`ldc.i4.0`).
- **Chi tiết `call_edges` và `field_accesses` của `0x060015a9`:**
  - `call_edges` (27 cạnh trong `cad2025_mong_models.json` & đồng chiếu control-flow):
    - Các lệnh gọi helper bảo vệ: `il_offset 0 -> call 0x060059c7`, `92 -> call 0x06005aa6`, `194 -> call 0x0600599b`, `230 -> call 0x06005992`, `334 -> call 0x06005ace` *(đồng chiếu dãy gọi helper: `il_offset 5 -> 0x060059c7`, `15 -> 0x06005992`, `20 -> 0x06005aa6`, `64 -> 0x0600599b`, `102 -> 0x0600599b`, `167 -> 0x0600599b`, `211 -> 0x0600599b`, `235 -> 0x06005ace`, `241 -> 0x06005992`, `246 -> 0x060059a3`)*.
    - Khởi tạo `List<int>` (`newobj 0x0a00018e` `List`1..ctor`) và thêm phần tử (`callvirt 0x0a000194` `List`1.Add`):
      - `il_offset 144: newobj 0x0a00018e` -> `152, 163, 174: callvirt 0x0a000194` (`100, 150, 200`) -> `stsfld 0x0400099e` (`HYJHUIsgUA`).
      - `il_offset 245: newobj 0x0a00018e` -> `253, 261, 269, 277, 285, 293: callvirt 0x0a000194` (`10, 12, 14, 16, 18, 20`) -> `stsfld 0x0400099b` (`rCJuVn9YhH`).
      - `il_offset 349: newobj 0x0a00018e` -> `357, 368, 379: callvirt 0x0a000194` (`100, 150, 200`) -> `stsfld 0x0400099d` (`PwruzsE0FX`).
      - `il_offset 420: newobj 0x0a00018e` -> `428, 436, 444, 452, 460, 468: callvirt 0x0a000194` (`10, 12, 14, 16, 18, 20`) -> `stsfld 0x0400099c` (`XB9uavWqWA`).
  - `field_accesses` (`stsfld`):
    - Trong `cad2025_mong_models.json`: `il_offset 108 -> stsfld 0x0400099f` (`e5ZH8K1Tlq`), `179 -> stsfld 0x0400099e` (`HYJHUIsgUA`), `298 -> stsfld 0x0400099b` (`rCJuVn9YhH`), `384 -> stsfld 0x0400099d` (`PwruzsE0FX`), `473 -> stsfld 0x0400099c` (`XB9uavWqWA`), cùng các lệnh đọc cờ trạng thái module `ldsfld 0x04003184` / `ldfld 0x04003146, 0x0400318b, 0x0400314b, 0x0400317c` *(đồng chiếu tọa độ rút gọn: `stsfld` tại `109 -> 0x0400099b`, `216 -> 0x0400099c`, `69 -> 0x0400099d`, `172 -> 0x0400099e`, `25 -> 0x0400099f`)*.

#### 2.5.2. `[VM-047` & `VM-075]` `Kata_pro64_Cad2025.AI_BeamFoundationReferencePdfBuilder..cctor` (`0x06002414`, RVA `3519928`, `il_size = 616`, `il_sha256 = 60a2b9e2e08e01217a600d640005c43f984c9318cbd010369280643787df8a75`, Bằng chứng **E5** / `VM-101`)

- **Danh sách `call_edges` (15 cạnh trong `cad2025_mong_models.json`):**
  - `il_offset 0: call -> 0x060059c7` (`aYj0MQpRG4E`), `112: call -> 0x0600599b` (`eNG01e7lqd6`), `181: call -> 0x06005992` (`OWY01KALxyA`), `246: call -> 0x06005aa6` (`MOhA7Yn0P1y`), `261: call -> 0x06005ace` (`B9NAYsc0lGT`).
  - `il_offset 331: call -> 0x06006c11` (`EinG9HPDVFWmX89tk6df.l15Pg59AcxX`, lấy `CultureInfo.InvariantCulture`).
  - 9 lệnh `newobj -> 0x0a0006d9` (`iTextSharp.text.BaseColor..ctor(int, int, int)`) tại các `il_offset`: `160`, `226`, `285`, `381`, `428`, `480`, `512`, `543`, `575`.
- **Bảng ánh xạ đầy đủ 10 trường tĩnh `0x04000fdd`..`0x04000fe6` (khớp 100% với `VM-101`) và `field_accesses` (`stsfld`):**

| Field Token | Tên trường thực tế (`VM-101`) | Kiểu dữ liệu (`VM-101`) | RowOffset | `il_offset` (`newobj`/`call` -> `stsfld`) | Giá trị khởi tạo từ `body.anchors` |
|---|---|---|---|---|---|
| `0x04000fdd` | **`i3b8QSQwB0w`** | `iTextSharp.text.BaseColor [class:1493]` | `13490306` | `480: newobj 0x0a0006d9` -> `485: stsfld 0x04000fdd` | `new BaseColor(0, 168, 210)` |
| `0x04000fde` | **`OZc8QjXVSLI`** | `iTextSharp.text.BaseColor [class:1493]` | `13490316` | `381: newobj 0x0a0006d9` -> `386: stsfld 0x04000fde` | `new BaseColor(20, 95, 225)` |
| `0x04000fdf` | **`TCT8QxXlfZa`** | `iTextSharp.text.BaseColor [class:1493]` | `13490326` | `512: newobj 0x0a0006d9` -> `517: stsfld 0x04000fdf` | `new BaseColor(242, 137, 28)` |
| `0x04000fe0` | **`zXE8QDHI2bC`** | `iTextSharp.text.BaseColor [class:1493]` | `13490336` | `160: newobj 0x0a0006d9` -> `165: stsfld 0x04000fe0` | `new BaseColor(155, 74, 210)` |
| `0x04000fe1` | **`e0s8QBvIDX5`** | `iTextSharp.text.BaseColor [class:1493]` | `13490346` | `226: newobj 0x0a0006d9` -> `231: stsfld 0x04000fe1` | `new BaseColor(214, 45, 120)` |
| `0x04000fe2` | **`r0A8QOORPGT`** | `iTextSharp.text.BaseColor [class:1493]` | `13490356` | `575: newobj 0x0a0006d9` -> `580: stsfld 0x04000fe2` | `new BaseColor(130, 90, 180)` |
| `0x04000fe3` | **`FOy8QVuj78u`** | `iTextSharp.text.BaseColor [class:1493]` | `13490366` | `285: newobj 0x0a0006d9` -> `290: stsfld 0x04000fe3` | `new BaseColor(205, 55, 45)` |
| `0x04000fe4` | **`Y978QakUWJu`** | `iTextSharp.text.BaseColor [class:1493]` | `13490376` | `428: newobj 0x0a0006d9` -> `433: stsfld 0x04000fe4` | `new BaseColor(20, 20, 20)` |
| `0x04000fe5` | **`NYC8QzPNDxZ`** | `iTextSharp.text.BaseColor [class:1493]` | `13490386` | `543: newobj 0x0a0006d9` -> `548: stsfld 0x04000fe5` | `new BaseColor(0, 150, 185)` |
| `0x04000fe6` | **`rgg8kUwCZ4r`** | `System.Globalization.CultureInfo [class:1497]` | `13490396` | `326: ldsfld 0x0400351f` -> `331: call 0x06006c11` -> `336: stsfld 0x04000fe6` | `CultureInfo.InvariantCulture` |

*(Các truy cập `ldsfld`/`ldfld` kiểm tra trạng thái nhánh trong `0x06002414`: `122/127 -> 0x04003128`, `191/196 -> 0x04003190`, `300/305 -> 0x0400316e`, `346/351 -> 0x04003179`, `396/401 -> 0x04003171`, `443/448 -> 0x04003142`, `590/595 -> 0x04003141`).*

#### 2.5.3. `[VM-048` & `VM-076]` `Kata_pro64_Cad2025.Mong_coc..cctor` (`0x06003988`, RVA `6075252`, `il_size = 1080`, `il_sha256 = 2428a57627959e0f36898295a18991a1f6b8b1071b048e21cf57579ed7045b19`, Bằng chứng **E4** / `VM-104`)

- **Dải token trường tĩnh thực tế trong `Mong_coc` (`0x020002ec`, `VM-104`):** `0x04001ac7`..`0x04001ae8` (và `0x04001ae9`..`0x04001af4`).
- **5 đối tượng Form & DTO trung tâm khởi tạo trong `0x06003988`:**
  1. `0x04001ac8` **`formMongCoc`** kiểu `Kata_pro64_Cad2025.Form_mong_coc` (`0x02000073` `[class:460]`/`[class:452]`): khởi tạo qua `newobj 0x06000b47` (`Form_mong_coc..ctor`) tại `il_offset 520` (`stsfld` tại `525`; đồng chiếu `newobj 0x06000b47` tại `il_offset 923`, `stsfld` tại `928`).
  2. `0x04001ac7` **`info_mong_coc`** kiểu `InfoMongCoc` (`0x020002ed` `[class:2996]`): khởi tạo qua `newobj 0x060039fb` (`InfoMongCoc..ctor`) tại `il_offset 1039` (`stsfld` tại `1044`).
  3. `0x04001ac9` **`formThepThemDuoi`** *(đồng chiếu `formThepThem`)* kiểu `Kata_pro64_Cad2025.Form_Lop_Thep_Them` (`0x0200006e` `[class:440]` / `0x02000062`): khởi tạo qua `newobj 0x0600098c` tại `il_offset 1019` (`stsfld` tại `1024`; đồng chiếu `newobj 0x060009c8` tại `il_offset 806`, `stsfld` tại `811`).
  4. `0x04001aca` **`formThepThemNgang`** kiểu `Kata_pro64_Cad2025.Form_Lop_Thep_Them_Ngang` (`0x0200006f` `[class:444]` / `0x02000063`): khởi tạo qua `newobj 0x060009fb` tại `il_offset 479` (`stsfld` tại `484`; đồng chiếu `newobj 0x060009ef` tại `il_offset 933`, `stsfld` tại `938`).
  5. `0x04001acb` **`formThepThemTren`** kiểu `Kata_pro64_Cad2025.Form_Lop_Thep_Them_Tren` (`0x02000070` `[class:448]` / `0x02000064`): khởi tạo qua `newobj 0x06000a67` / `newobj 0x06000a1c` tại `il_offset 245` (`stsfld` tại `250`).
- **Danh sách đầy đủ 31 `call_edges` trong `cad2025_mong_core.json` của `0x06003988`:**
  - Các lệnh `call` helper: `0 -> 0x060059c7`, `230 -> 0x0600599b`, `383 -> 0x06005ace`, `663 -> 0x06005aa6`, `700 -> 0x060059a3`, `923 -> 0x06005992`.
  - Các lệnh `newobj`: `245 -> 0x06000a67` (`Form_Lop_Thep_Them_Tren..ctor`), `287 -> 0x0a00022d` (`diem..ctor`), `323 -> 0x0a000573` (`List<List<diem>>..ctor`), `343 -> 0x0a000224` (`List<diem>..ctor`), `363 -> 0x0a0003fa` (`List<Entity>..ctor`), `398 -> 0x0a000224`, `418 -> 0x0a000224`, `459 -> 0x0a00022d`, `479 -> 0x060009fb` (`Form_Lop_Thep_Them_Ngang..ctor`), `520 -> 0x06000b47` (`Form_mong_coc..ctor`), `561 -> 0x0a000d21` (`List<List<double>>..ctor`), `581 -> 0x0a00022d`, `622 -> 0x0a0003fa`, `720 -> 0x0a00022d`, `740 -> 0x0a00022d`, `781 -> 0x0a00022d`, `822 -> 0x0a00022d`, `842 -> 0x0a00022d`, `862 -> 0x0a00022d`, `882 -> 0x0a000c7f` (`List<List<Entity>>..ctor`), `959 -> 0x0a00022d`, `979 -> 0x0a0003fa`, `999 -> 0x0a0001a1` (`List<object>..ctor`), `1019 -> 0x0600098c` (`Form_Lop_Thep_Them..ctor`), `1039 -> 0x060039fb` (`InfoMongCoc..ctor`).
- **Danh sách đầy đủ các lệnh `stsfld` trong `0x06003988`:**
  - Theo `cad2025_mong_core.json`: `194 (0x04001ada QGS8lBI6wrt)`, `250 (0x04001acb formThepThemTren)`, `292 (0x04001ae3 cY98uPaGj7u)`, `308 (0x04001ad4 phuongB)`, `328 (0x04001af0 GqC8uvvT445)`, `348 (0x04001ad9 listLocation)`, `368 (0x04001aeb KAh8u4G0NNx)`, `403 (0x04001af2 TNe8uM1iM2G)`, `423 (0x04001af1 T4L8u1bcu2F)`, `464 (0x04001add zKU8laCANAn)`, `484 (0x04001aca formThepThemNgang)`, `525 (0x04001ac8 formMongCoc)`, `546 (0x04001adc aDU8lV0hgnR)`, `566 (0x04001acc listlistL)`, `586 (0x04001ae0 g8p8u8gcvOq)`, `627 (0x04001ae9 q4h8uQpQT7s)`, `705 (0x04001ad3 LOAIDATAMONGCOC)`, `725 (0x04001ae4 RnR8u38WecU)`, `745 (0x04001adf JXu8uUNLeK7)`, `786 (0x04001adb uml8lOyw60m)`, `827 (0x04001ae1 JPy8uEHlhpr)`, `847 (0x04001ade QNF8lzTnKfn)`, `867 (0x04001aef pIA8uFDuXjF)`, `887 (0x04001aec znx8uYoCgew)`, `964 (0x04001ae2 TWa8u0bdxcT)`, `984 (0x04001aea Pyv8ukisZq0)`, `1004 (0x04001aed h6X8uov7C4g)`, `1024 (0x04001ac9 formThepThemDuoi)`, `1044 (0x04001ac7 info_mong_coc)`.
  - Đồng chiếu 25 tọa độ `stsfld` đối xứng (`0x04001ac7`..`0x04001ae8`): `194 (0x04001ada)`, `250 (0x04001acb)`, `292 (0x04001ae3)`, `308 (0x04001ad4)`, `449 (0x04001ae0)`, `589 (0x04001ad0)`, `605 (0x04001ae1)`, `619 (0x04001ad7)`, `635 (0x04001ad1)`, `651 (0x04001ad8)`, `742 (0x04001ae2)`, `752 (0x04001adc)`, `762 (0x04001adf)`, `792 (0x04001adb)`, `811 (0x04001ac9)`, `821 (0x04001add)`, `827 (0x04001ad2)`, `896 (0x04001ad5)`, `913 (0x04001ad9)`, `928 (0x04001ac8)`, `938 (0x04001aca)`, `957 (0x04001ae8)`, `1017 (0x04001ad6)`, `1034 (0x04001ad3)`, `1044 (0x04001ac7)`.

#### 2.5.4. `[VM-049` & `VM-077]` `Kata_pro64_Cad2025.Rai_Mong_Module..cctor` (`0x06003c39`, RVA `6495996`, `il_size = 310`, `il_sha256 = 115e0b27350089313be8bb6a71754bfb7972c3f2bfd22f9bb6b0277a6b7006e6`, Bằng chứng **E4** / `VM-110`)

- **Các cạnh gọi hàm (`call_edges`) và gán trường tĩnh (`field_accesses`) trong `cad2025_mong_core.json`:**
  - `il_offset 0: call -> 0x060059c7`, `84: call -> 0x06005992`, `121: call -> 0x06005aa6`, `156: call -> 0x06005ace`, `274: call -> 0x0600599b`.
  - `il_offset 136: newobj 0x06000e92` (`Kata_pro64_Cad2025.Form_Rai_Mong..ctor`) $\rightarrow$ `il_offset 141: stsfld 0x04001db9` *(đồng chiếu `0x04001cb3`, `formRaiMong` : `Form_Rai_Mong [class:500]`)*.
  - `il_offset 192: newobj 0x06003c3d` (`InfoRaiMong..ctor`) $\rightarrow$ `il_offset 197: stsfld 0x04001dba` *(đồng chiếu `0x04001cb4`, `info_rai_mong` : `InfoRaiMong [class:3288]`)*.
  - `il_offset 233: newobj 0x06003c4d` (`InfoRaiMongDon..ctor`) $\rightarrow$ `il_offset 238: stsfld 0x04001dbb` *(đồng chiếu `0x04001cb5`, `info_rai_mong_don` : `InfoRaiMongDon [class:3296]`)*.

#### 2.5.5. `[VM-050` & `VM-078]` `Kata_pro64_Cad2025.Ve_Mat_Bang_Mong_Module..cctor` (`0x06003ddd`, RVA `6668992`, `il_size = 540`, `il_sha256 = 3e43fc202446f3855cb30f495729c3e39c51f9cf86b138159a1802397be64414`, Bằng chứng **E4** / `VM-114`)

- **Các cạnh khởi tạo đối tượng (`newobj` -> `stsfld`) trong `cad2025_mong_core.json`:**
  - `il_offset 138: newobj 0x060012bc` (`Kata_pro64_Cad2025.Form_Ve_Mat_Bang_Mong..ctor`) $\rightarrow$ `143: stsfld 0x04001f03` *(đồng chiếu `0x04001ea8`, `formVeMatBangMong` : `Form_Ve_Mat_Bang_Mong [class:628]`)*.
  - `il_offset 158: newobj 0x06003df0` (`InfoVeMatBangMong..ctor`) $\rightarrow$ `163: stsfld 0x04001f04` *(đồng chiếu `0x04001ea9`, `info_ve_mat_bang_mong` / `info_ve_mb_mong` : `InfoVeMatBangMong [class:3528]`)*.
  - `il_offset 178: newobj 0x06003e15` (`InfoMBMongBang..ctor`) $\rightarrow$ `183: stsfld 0x04001f05` *(đồng chiếu `0x04001eaa`, `info_mb_mong_bang` / `info_ve_mb_mong_bang` : `InfoMBMongBang [class:3548]`)*.
  - `il_offset 421: newobj 0x06003ea2` (`InfoMBMongDon..ctor`) $\rightarrow$ `426: stsfld 0x04001f06` *(đồng chiếu `0x04001eab`, `info_mb_mong_don` / `info_ve_mb_mong_don` : `InfoMBMongDon [class:3616]`)*.
- **Các lệnh khởi tạo khóa XData / trường tĩnh còn lại (`call` -> `stsfld`):**
  - Trong `cad2025_mong_core.json`: `262: call 0x060059a3` -> `267: stsfld 0x04001f08` (`LOAIDATAJOINTREACTION`), `311: call 0x060059a3` -> `316: stsfld 0x04001f0a` (`LOAIDATAMBMONGBANG`), `353: call 0x060059a3` -> `358: stsfld 0x04001f07` (`LOAIDATACOC`), `401: call 0x060059a3` -> `406: stsfld 0x04001f09` (`LOAIDATAMBMONGCOC`), `499: call 0x060059a3` -> `504: stsfld 0x04001f0b` (`LOAIDATALINECHEO`), cùng các helper `0 -> 0x060059c7`, `108 -> 0x06005ace`, `123 -> 0x06005aa6`, `198 -> 0x06005992`, `462 -> 0x0600599b`.
  - Đồng chiếu tọa độ `stsfld` tương ứng: `125 (0x04001eae KEY_XDATA_MONG_BANG)`, `225 (0x04001ead KEY_XDATA_MONG)`, `291 (0x04001eaf KEY_XDATA_MONG_DON)`, `347 (0x04001eb1 BLOCK_NAME_MONG_COC)`, `365 (0x04001eac listPointCoc_MB)`, `379 (0x04001eb0 listMong_MB)`.

#### 2.5.6. `[VM-051` & `VM-079`..`VM-085]` Các `.cctor` Khác trong Nhóm Móng (`Foundation_Etabs`, `Lenh_thep`, `Acad`, `kata1`, `kata7`, `kata8`, `VmbmLight*`)

| Mã quy tắc | Lớp & Phương thức `.cctor` | Token & RVA | `il_size` | `il_sha256` | Hành vi khởi tạo tĩnh đã xác thực |
|---|---|---|---|---|---|
| `VM-079` | `Foundation_Etabs..cctor` (`0x0200052c`) | `0x060050f6`, RVA `9226212` | `241` | `84d979b132124a8a...` | Gọi `newobj 0x0600510f` (`StripDesignOption..ctor`) và gán vào trường tĩnh `0x04002b41` (`lbaEMamVToq` : `StripDesignOption` `0x0200052e`, **E4**). |
| `VM-080` | `Lenh_thep..cctor` (`0x020000d6`) | `0x06001f8d`, RVA `3190556` | `803` | `fd89cfadd9594a85...` | Khởi tạo các biến trạng thái toàn cục của trình điều phối lệnh thép & móng (`0x020000d6`, **E6**). |
| `VM-081` | `Acad..cctor` (`Class28` `0x020000d5`) | `0x06001f8a`, RVA `3190328` | `177` | `427b2971881e1663...` | Khởi tạo lớp giao tiếp AutoCAD cơ sở (`0x020000d5`, **E6**). |
| `VM-082` | `kata1..cctor` (`0x0200027b`, `VM-103`/`VM-204`) | **`0x0600350c`**, RVA `5279112` | `195` | `61a0f88e3f7b107a...` | Khởi tạo lớp tiện ích `Kata_pro64_Cad2025.kata1` (TypeDef token **`0x0200027b`**, RowOffset `13379002`, 5 trường `0x0400178a`..`0x0400178e`, 56 phương thức `0x0600350c`..`0x06003543` theo `VM-103` & `VM-204`, **E6**; lưu ý `0x0200037c` là `CocChiuTai`). |
| `VM-083` | `kata7..cctor` (`0x02000566`) | `0x060053db`, RVA `10614024` | `991` | `4b177f2fc3cdeca1...` | Khởi tạo bộ đồng bộ ngôn ngữ đa biểu mẫu (`StartFormLanguageSynchronization`, `ApplyFormLanguage`, `0x02000566`, **E6**). |
| `VM-084` | `kata8..cctor` (`0x0200058e`) | `0x060055c7`, RVA `11077288` | `241` | `bfa43553b3a825fa...` | Khởi tạo lớp tiện ích `kata8` (`0x0200058e`, **E6**). |
| `VM-085` | Các lớp DTO/Form/Engine `VmbmLight*` | Xem từng bảng ở Mục 2.7 | `156` / `177` | Xem bảng Mục 2.7 | Mọi lớp có `.cctor` `156` hoặc `177` bytes đều thực thi mẫu khởi tạo bảo vệ lớp (`0x060059c7`, `0x06005992`, `0x06005aa6`, `0x0600599b`, `0x06005ace`, `0x060059a3`, **E4**–**E8**). |

---

### 2.6. Đặc tả Toàn diện Schema Trường dữ liệu (Fields) của 101 Kiểu trong E4..E7 (VM-086 .. VM-186) và 13 Kiểu trong E8 (Mục 2.6.2)

#### 2.6.1. Bảng Chi tiết 100% Trường dữ liệu của 101 Kiểu trong E4..E7 (VM-086 .. VM-186)

### [VM-086] Type `Kata_pro64_Cad2025.Form_VMBM_Light_Name` (Token `0x02000039`, RowOffset `13368598`, Extends `0x0100000f (System.Windows.Forms.Form)`, Evidence `ev_549bfe33ecd78257ce27570bdd19331f304e770621ccd33a14059b374ac982e3`)
- Fields count: 3 | Methods count: 8
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x0400012a`|`NHg8IAIoB4`|`System.Windows.Forms.TextBox [class:249]`|`33`|`13452676`|`-`|
|`0x0400012b`|`QqI8ygKsk1`|`System.Windows.Forms.NumericUpDown [class:325]`|`33`|`13452686`|`-`|
|`0x0400012c`|`ONZYDBq8EvolsLARQkRw`|`Kata_pro64_Cad2025.Form_VMBM_Light_Name [class:228]`|`17`|`13452696`|`-`|

### [VM-087] Type `Kata_pro64_Cad2025.Edit_moc_dai` (Token `0x02000059`, RowOffset `13369174`, Extends `0x0100000f (System.Windows.Forms.Form)`, Evidence `ev_549bfe33ecd78257ce27570bdd19331f304e770621ccd33a14059b374ac982e3`)
- Fields count: 15 | Methods count: 20
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04000230`|`tKLAIeRXeP`|`System.ComponentModel.IContainer [class:157]`|`1`|`13455296`|`-`|
|`0x04000231`|`a2BAyaNUoe`|`System.Windows.Forms.Button [class:161]`|`1`|`13455306`|`-`|
|`0x04000232`|`cNMARygDFR`|`System.Windows.Forms.Label [class:245]`|`1`|`13455316`|`-`|
|`0x04000233`|`q6xAgdCOil`|`System.Windows.Forms.PictureBox [class:645]`|`1`|`13455326`|`-`|
|`0x04000234`|`gWSAhYCGyy`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13455336`|`-`|
|`0x04000235`|`JgFAZ6Jidw`|`System.Windows.Forms.Label [class:245]`|`1`|`13455346`|`-`|
|`0x04000236`|`tap_dang`|`System.Collections.Generic.List`1 [class:217]<string>`|`6`|`13455356`|`-`|
|`0x04000237`|`MrWAemhOjn`|`System.Windows.Forms.PictureBox [class:645][]`|`1`|`13455366`|`-`|
|`0x04000238`|`UdTALYLJkW`|`System.Windows.Forms.Label [class:245][]`|`1`|`13455376`|`-`|
|`0x04000239`|`TextBox_moc`|`System.Windows.Forms.TextBox [class:249][]`|`6`|`13455386`|`-`|
|`0x0400023a`|`tQTATLfK8i`|`System.Windows.Forms.Label [class:245][]`|`1`|`13455396`|`-`|
|`0x0400023b`|`EOlAf4TVJW`|`i4`|`1`|`13455406`|`-`|
|`0x0400023c`|`ljbAcDFgCV`|`r8`|`1`|`13455416`|`-`|
|`0x0400023d`|`XJkA6Sr6OH`|`r8`|`1`|`13455426`|`-`|
|`0x0400023e`|`XB4Gsuq8iVM456xZcPqo`|`Kata_pro64_Cad2025.Edit_moc_dai [class:356]`|`19`|`13455436`|`-`|

### [VM-088] Type `Kata_pro64_Cad2025.Form_Edit_Coc` (Token `0x02000065`, RowOffset `13369390`, Extends `0x0100000f (System.Windows.Forms.Form)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 10 | Methods count: 24
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04000360`|`UhRYXCeEqU`|`System.ComponentModel.IContainer [class:157]`|`1`|`13458336`|`-`|
|`0x04000361`|`C7kYiJmTrr`|`System.Windows.Forms.DataGridView [class:117]`|`1`|`13458346`|`-`|
|`0x04000362`|`zw2YnqUJB0`|`System.Windows.Forms.Button [class:161]`|`1`|`13458356`|`-`|
|`0x04000363`|`WBMY5k7Jh4`|`System.Windows.Forms.Button [class:161]`|`1`|`13458366`|`-`|
|`0x04000364`|`zwZYSSx2mS`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13458376`|`-`|
|`0x04000365`|`ehZYj47PuV`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13458386`|`-`|
|`0x04000366`|`LheYx3DqUE`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13458396`|`-`|
|`0x04000367`|`oPkYD3YX3o`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13458406`|`-`|
|`0x04000368`|`status`|`string`|`6`|`13458416`|`-`|
|`0x04000369`|`TG33D4qEKnn9idxbDnOq`|`Kata_pro64_Cad2025.Form_Edit_Coc [class:404]`|`19`|`13458426`|`-`|

### [VM-089] Type `Kata_pro64_Cad2025.Form_mong_coc` (Token `0x02000073`, RowOffset `13369642`, Extends `0x0100000f (System.Windows.Forms.Form)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 139 | Methods count: 350
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x040004d0`|`SvwNWvHX0x`|`System.ComponentModel.IContainer [class:157]`|`1`|`13462016`|`-`|
|`0x040004d1`|`Y7oNsa7bSi`|`System.Windows.Forms.Label [class:245]`|`1`|`13462026`|`-`|
|`0x040004d2`|`cdnNtYJUJ9`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13462036`|`-`|
|`0x040004d3`|`y3JNdnRTMg`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462046`|`-`|
|`0x040004d4`|`elsNJMLley`|`System.Windows.Forms.Label [class:245]`|`1`|`13462056`|`-`|
|`0x040004d5`|`YuTNrYcfVG`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462066`|`-`|
|`0x040004d6`|`w1mNGPJgxt`|`System.Windows.Forms.Label [class:245]`|`1`|`13462076`|`-`|
|`0x040004d7`|`xPGNl48ymy`|`System.Windows.Forms.Label [class:245]`|`1`|`13462086`|`-`|
|`0x040004d8`|`JeJNuNPttx`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462096`|`-`|
|`0x040004d9`|`OUYNHKLst6`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462106`|`-`|
|`0x040004da`|`q1oNpOW3sB`|`System.Windows.Forms.Label [class:245]`|`1`|`13462116`|`-`|
|`0x040004db`|`rsyNwo9qRr`|`System.Windows.Forms.Label [class:245]`|`1`|`13462126`|`-`|
|`0x040004dc`|`rEbN2C3ZEN`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462136`|`-`|
|`0x040004dd`|`zfcNXwtJAR`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462146`|`-`|
|`0x040004de`|`NSHNixu2fE`|`System.Windows.Forms.Label [class:245]`|`1`|`13462156`|`-`|
|`0x040004df`|`o0oNnZahJK`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13462166`|`-`|
|`0x040004e0`|`LqHN5JxjkS`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462176`|`-`|
|`0x040004e1`|`OsrNSiRDpm`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462186`|`-`|
|`0x040004e2`|`fNkNjV7y3w`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13462196`|`-`|
|`0x040004e3`|`TBONxESLiV`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462206`|`-`|
|`0x040004e4`|`kJHNDktjdF`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462216`|`-`|
|`0x040004e5`|`lbuNBpFDNq`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462226`|`-`|
|`0x040004e6`|`PGUNOnkldo`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462236`|`-`|
|`0x040004e7`|`FSuNVPiu6A`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462246`|`-`|
|`0x040004e8`|`w0DNaFlpU5`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462256`|`-`|
|`0x040004e9`|`hQaNzCHXnn`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462266`|`-`|
|`0x040004ea`|`VQhbUHT9Wo`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462276`|`-`|
|`0x040004eb`|`ldjb8S8wMQ`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462286`|`-`|
|`0x040004ec`|`zZobEDo1kr`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462296`|`-`|
|`0x040004ed`|`ioxb0ZIchw`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462306`|`-`|
|`0x040004ee`|`v6CbPSAcrJ`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462316`|`-`|
|`0x040004ef`|`oQ3b3v8vpc`|`System.Windows.Forms.Button [class:161]`|`1`|`13462326`|`-`|
|`0x040004f0`|`gq3bqvrRHs`|`System.Windows.Forms.Button [class:161]`|`1`|`13462336`|`-`|
|`0x040004f1`|`x1sbAlt5C3`|`System.Windows.Forms.BindingSource [class:237]`|`1`|`13462346`|`-`|
|`0x040004f2`|`uCfbChgRXu`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13462356`|`-`|
|`0x040004f3`|`F5abmLJrkv`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462366`|`-`|
|`0x040004f4`|`EXYbQNFtlE`|`System.Windows.Forms.CheckBox [class:257]`|`1`|`13462376`|`-`|
|`0x040004f5`|`it1bkSTPJl`|`System.Windows.Forms.CheckBox [class:257]`|`1`|`13462386`|`-`|
|`0x040004f6`|`N03b4gdDxm`|`System.Windows.Forms.Label [class:245]`|`1`|`13462396`|`-`|
|`0x040004f7`|`MGIbYyqxNs`|`System.Windows.Forms.Label [class:245]`|`1`|`13462406`|`-`|
|`0x040004f8`|`ofAbokVGRQ`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462416`|`-`|
|`0x040004f9`|`jPmb7BAemG`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462426`|`-`|
|`0x040004fa`|`sjjbFjkid3`|`System.Windows.Forms.Label [class:245]`|`1`|`13462436`|`-`|
|`0x040004fb`|`fGcbvufTx9`|`System.Windows.Forms.Label [class:245]`|`1`|`13462446`|`-`|
|`0x040004fc`|`qmMb1HEsFA`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13462456`|`-`|
|`0x040004fd`|`GAybMmaih9`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462466`|`-`|
|`0x040004fe`|`WITb9AOaFC`|`System.Windows.Forms.Label [class:245]`|`1`|`13462476`|`-`|
|`0x040004ff`|`IIvbKXqTmH`|`System.Windows.Forms.Label [class:245]`|`1`|`13462486`|`-`|
|`0x04000500`|`FcpbNXimre`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462496`|`-`|
|`0x04000501`|`ittbbV4Dox`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462506`|`-`|
|`0x04000502`|`MJCbIFwBem`|`System.Windows.Forms.Label [class:245]`|`1`|`13462516`|`-`|
|`0x04000503`|`nQqbyVT6j0`|`System.Windows.Forms.Label [class:245]`|`1`|`13462526`|`-`|
|`0x04000504`|`EXjbRLEqi5`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462536`|`-`|
|`0x04000505`|`xMGbgDsMdJ`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462546`|`-`|
|`0x04000506`|`YfLbhnGmcn`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462556`|`-`|
|`0x04000507`|`LkRbZ3WAeO`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462566`|`-`|
|`0x04000508`|`HQMbeecZJd`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462576`|`-`|
|`0x04000509`|`g5TbLCouyx`|`System.Windows.Forms.Label [class:245]`|`1`|`13462586`|`-`|
|`0x0400050a`|`bIbbT6wPYe`|`System.Windows.Forms.RadioButton [class:253]`|`1`|`13462596`|`-`|
|`0x0400050b`|`j17bfgBl0w`|`System.Windows.Forms.RadioButton [class:253]`|`1`|`13462606`|`-`|
|`0x0400050c`|`gD9bcY8d8v`|`System.Windows.Forms.Label [class:245]`|`1`|`13462616`|`-`|
|`0x0400050d`|`ajXb6tqBPU`|`System.Windows.Forms.RadioButton [class:253]`|`1`|`13462626`|`-`|
|`0x0400050e`|`nribWFUbbF`|`System.Windows.Forms.RadioButton [class:253]`|`1`|`13462636`|`-`|
|`0x0400050f`|`lOAbsBZGu2`|`System.Windows.Forms.Label [class:245]`|`1`|`13462646`|`-`|
|`0x04000510`|`FDnbtGTfRl`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13462656`|`-`|
|`0x04000511`|`FZgbdTgFNj`|`System.Windows.Forms.Label [class:245]`|`1`|`13462666`|`-`|
|`0x04000512`|`Un3bJoZTc7`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462676`|`-`|
|`0x04000513`|`DM9brHXskl`|`System.Windows.Forms.Label [class:245]`|`1`|`13462686`|`-`|
|`0x04000514`|`rvPbGDevCx`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462696`|`-`|
|`0x04000515`|`OPlbl3427p`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462706`|`-`|
|`0x04000516`|`FK0bufpWdi`|`System.Windows.Forms.Label [class:245]`|`1`|`13462716`|`-`|
|`0x04000517`|`Y6qbHwQvyH`|`System.Windows.Forms.RadioButton [class:253]`|`1`|`13462726`|`-`|
|`0x04000518`|`dEmbp2Y3Ti`|`System.Windows.Forms.RadioButton [class:253]`|`1`|`13462736`|`-`|
|`0x04000519`|`BiZbwOrfMI`|`System.Windows.Forms.Label [class:245]`|`1`|`13462746`|`-`|
|`0x0400051a`|`YIsb2RtNaM`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13462756`|`-`|
|`0x0400051b`|`tk2bXLm9se`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462766`|`-`|
|`0x0400051c`|`baxbiRXPma`|`System.Windows.Forms.Button [class:161]`|`1`|`13462776`|`-`|
|`0x0400051d`|`DdmbnxHcQ4`|`System.Windows.Forms.Button [class:161]`|`1`|`13462786`|`-`|
|`0x0400051e`|`BKkb5tuM8O`|`System.Windows.Forms.Button [class:161]`|`1`|`13462796`|`-`|
|`0x0400051f`|`Sb4bS1MHou`|`System.Windows.Forms.RadioButton [class:253]`|`1`|`13462806`|`-`|
|`0x04000520`|`CHTbj1f9qY`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13462816`|`-`|
|`0x04000521`|`M8hbxwjA1L`|`System.Windows.Forms.Label [class:245]`|`1`|`13462826`|`-`|
|`0x04000522`|`zsgbDNBOqN`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462836`|`-`|
|`0x04000523`|`ALHbB8RneS`|`System.Windows.Forms.Panel [class:373]`|`1`|`13462846`|`-`|
|`0x04000524`|`XOBbOfcaQK`|`System.Windows.Forms.Button [class:161]`|`1`|`13462856`|`-`|
|`0x04000525`|`rivbVW5F6j`|`System.Windows.Forms.Label [class:245]`|`1`|`13462866`|`-`|
|`0x04000526`|`rlfba0DbyO`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462876`|`-`|
|`0x04000527`|`r6YbzWbX5F`|`System.Windows.Forms.Label [class:245]`|`1`|`13462886`|`-`|
|`0x04000528`|`SQ7IU6wkiO`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462896`|`-`|
|`0x04000529`|`Q5aI8HnuYr`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462906`|`-`|
|`0x0400052a`|`FUOIEOH29F`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462916`|`-`|
|`0x0400052b`|`HQvI0ilD9e`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13462926`|`-`|
|`0x0400052c`|`DHyIPS1QsU`|`System.Windows.Forms.CheckBox [class:257]`|`1`|`13462936`|`-`|
|`0x0400052d`|`jh3I3VBCdB`|`System.Windows.Forms.ComboBox [class:377]`|`1`|`13462946`|`-`|
|`0x0400052e`|`nOQIqp6JVv`|`System.Windows.Forms.Label [class:245]`|`1`|`13462956`|`-`|
|`0x0400052f`|`S57IAyV08w`|`System.Windows.Forms.Button [class:161]`|`1`|`13462966`|`-`|
|`0x04000530`|`K7wICj0diq`|`System.Windows.Forms.TabControl [class:365]`|`1`|`13462976`|`-`|
|`0x04000531`|`ebsImUx1J1`|`System.Windows.Forms.TabPage [class:369]`|`1`|`13462986`|`-`|
|`0x04000532`|`ImQIQZ0PxE`|`System.Windows.Forms.Panel [class:373]`|`1`|`13462996`|`-`|
|`0x04000533`|`lFNIkPFcTC`|`System.Windows.Forms.Label [class:245]`|`1`|`13463006`|`-`|
|`0x04000534`|`mSGI4GCcdC`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463016`|`-`|
|`0x04000535`|`DqQIY49QGi`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463026`|`-`|
|`0x04000536`|`cMKIosEjxR`|`System.Windows.Forms.Label [class:245]`|`1`|`13463036`|`-`|
|`0x04000537`|`aSXI79CPjU`|`System.Windows.Forms.Label [class:245]`|`1`|`13463046`|`-`|
|`0x04000538`|`SxZIFAbd8W`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463056`|`-`|
|`0x04000539`|`YGTIv9CQHy`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463066`|`-`|
|`0x0400053a`|`qJRI1MqJff`|`System.Windows.Forms.Label [class:245]`|`1`|`13463076`|`-`|
|`0x0400053b`|`YW5IMOSnWl`|`System.Windows.Forms.Label [class:245]`|`1`|`13463086`|`-`|
|`0x0400053c`|`ay9I9Yipxc`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463096`|`-`|
|`0x0400053d`|`RuDIKWXAum`|`System.Windows.Forms.Label [class:245]`|`1`|`13463106`|`-`|
|`0x0400053e`|`A6SINApfEi`|`System.Windows.Forms.Label [class:245]`|`1`|`13463116`|`-`|
|`0x0400053f`|`bqPIbUSVnF`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463126`|`-`|
|`0x04000540`|`OptIIGob8R`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463136`|`-`|
|`0x04000541`|`tUnIyhHVNU`|`System.Windows.Forms.CheckBox [class:257]`|`1`|`13463146`|`-`|
|`0x04000542`|`NCGIRiTpSX`|`System.Windows.Forms.Label [class:245]`|`1`|`13463156`|`-`|
|`0x04000543`|`dEyIgBHOvK`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463166`|`-`|
|`0x04000544`|`pqQIhuLBaL`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463176`|`-`|
|`0x04000545`|`KmCIZLpdoX`|`System.Windows.Forms.Label [class:245]`|`1`|`13463186`|`-`|
|`0x04000546`|`moyIeyo9C1`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463196`|`-`|
|`0x04000547`|`j0TILMYPwZ`|`System.Windows.Forms.Label [class:245]`|`1`|`13463206`|`-`|
|`0x04000548`|`bC1ITLhoJa`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463216`|`-`|
|`0x04000549`|`PpCIfjeMlZ`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463226`|`-`|
|`0x0400054a`|`CyYIcuOBY9`|`System.Windows.Forms.Label [class:245]`|`1`|`13463236`|`-`|
|`0x0400054b`|`E2EI61xGH9`|`System.Windows.Forms.Label [class:245]`|`1`|`13463246`|`-`|
|`0x0400054c`|`d4KIWwKOa2`|`System.Windows.Forms.Label [class:245]`|`1`|`13463256`|`-`|
|`0x0400054d`|`NvdIsRT8Lf`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463266`|`-`|
|`0x0400054e`|`OvdItwQaCd`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463276`|`-`|
|`0x0400054f`|`JH6IdbZDqW`|`System.Windows.Forms.Label [class:245]`|`1`|`13463286`|`-`|
|`0x04000550`|`akVIJc8LIs`|`System.Windows.Forms.RadioButton [class:253]`|`1`|`13463296`|`-`|
|`0x04000551`|`yFaIrKINcb`|`System.Windows.Forms.CheckBox [class:257]`|`1`|`13463306`|`-`|
|`0x04000552`|`HwiIGAJRM8`|`System.Windows.Forms.CheckBox [class:257]`|`1`|`13463316`|`-`|
|`0x04000553`|`Qa8IlYYZlL`|`Kata_Class_Lib_Revit.PreviewCanvas [class:261]`|`1`|`13463326`|`-`|
|`0x04000554`|`da_hien_form`|`bool`|`6`|`13463336`|`-`|
|`0x04000555`|`C4dIu7y28i`|`bool`|`1`|`13463346`|`-`|
|`0x04000556`|`GeOIHC4mT7`|`bool`|`1`|`13463356`|`-`|
|`0x04000557`|`aIKIpyy04m`|`System.Drawing.Point [valuetype:173]`|`1`|`13463366`|`-`|
|`0x04000558`|`NkHIwHNQE9`|`System.Drawing.Point [valuetype:173]`|`1`|`13463376`|`-`|
|`0x04000559`|`LcyI2isTt9`|`i4`|`1`|`13463386`|`-`|
|`0x0400055a`|`km3HRYqEG23ijcsB73Kk`|`Kata_pro64_Cad2025.Form_mong_coc [class:460]`|`17`|`13463396`|`-`|

### [VM-090] Type `Kata_pro64_Cad2025.Form_Mong_Coc_V2` (Token `0x02000074`, RowOffset `13369660`, Extends `0x0100000f (System.Windows.Forms.Form)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 186 | Methods count: 406
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x0400055b`|`GFAygWtt1m`|`System.ComponentModel.IContainer [class:157]`|`1`|`13463406`|`-`|
|`0x0400055c`|`S5nyhrxDSg`|`System.Windows.Forms.Label [class:245]`|`1`|`13463416`|`-`|
|`0x0400055d`|`TowyZnuLjS`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13463426`|`-`|
|`0x0400055e`|`HUeye4yqIY`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463436`|`-`|
|`0x0400055f`|`eAXyLBplMp`|`System.Windows.Forms.Label [class:245]`|`1`|`13463446`|`-`|
|`0x04000560`|`wZAyTpePoT`|`System.Windows.Forms.Label [class:245]`|`1`|`13463456`|`-`|
|`0x04000561`|`MfVyfLe0jE`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463466`|`-`|
|`0x04000562`|`KhJycXxh8N`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463476`|`-`|
|`0x04000563`|`k5iy6YaMBU`|`System.Windows.Forms.Label [class:245]`|`1`|`13463486`|`-`|
|`0x04000564`|`KmByWAoUen`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13463496`|`-`|
|`0x04000565`|`pFQysnVhO9`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463506`|`-`|
|`0x04000566`|`MHGytK0W4G`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13463516`|`-`|
|`0x04000567`|`hZIyd07wdB`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463526`|`-`|
|`0x04000568`|`DwLyJih3tZ`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463536`|`-`|
|`0x04000569`|`OX8yr7ckT1`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463546`|`-`|
|`0x0400056a`|`JNmyGTOpv9`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463556`|`-`|
|`0x0400056b`|`WOWyleQUMN`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463566`|`-`|
|`0x0400056c`|`FdlyunMFBV`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463576`|`-`|
|`0x0400056d`|`sJ3yHlci73`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463586`|`-`|
|`0x0400056e`|`jNAypHi8Qg`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463596`|`-`|
|`0x0400056f`|`Gq7ywrGLje`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463606`|`-`|
|`0x04000570`|`XQay2yyv0e`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463616`|`-`|
|`0x04000571`|`KL1yXVZLAH`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463626`|`-`|
|`0x04000572`|`Ej0yirGdVy`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463636`|`-`|
|`0x04000573`|`LUjynpxO1K`|`System.Windows.Forms.Button [class:161]`|`1`|`13463646`|`-`|
|`0x04000574`|`ckdy5ZFo02`|`System.Windows.Forms.Button [class:161]`|`1`|`13463656`|`-`|
|`0x04000575`|`py2yS2IwXt`|`System.Windows.Forms.BindingSource [class:237]`|`1`|`13463666`|`-`|
|`0x04000576`|`wbEyjCw82F`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13463676`|`-`|
|`0x04000577`|`U9ryx9IcKi`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463686`|`-`|
|`0x04000578`|`HnIyD3esx6`|`System.Windows.Forms.CheckBox [class:257]`|`1`|`13463696`|`-`|
|`0x04000579`|`KdmyBuWIh9`|`System.Windows.Forms.CheckBox [class:257]`|`1`|`13463706`|`-`|
|`0x0400057a`|`CxxyOUx1uL`|`System.Windows.Forms.Label [class:245]`|`1`|`13463716`|`-`|
|`0x0400057b`|`pEwyV9rwjN`|`System.Windows.Forms.Label [class:245]`|`1`|`13463726`|`-`|
|`0x0400057c`|`WqCyaVkvsD`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463736`|`-`|
|`0x0400057d`|`yN4yzuZBwm`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463746`|`-`|
|`0x0400057e`|`TjpRU5Jj3s`|`System.Windows.Forms.Label [class:245]`|`1`|`13463756`|`-`|
|`0x0400057f`|`THsR81p6A0`|`System.Windows.Forms.Label [class:245]`|`1`|`13463766`|`-`|
|`0x04000580`|`mbwREmjSQN`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13463776`|`-`|
|`0x04000581`|`strR0972Sx`|`System.Windows.Forms.Label [class:245]`|`1`|`13463786`|`-`|
|`0x04000582`|`a8RRPMqaqh`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463796`|`-`|
|`0x04000583`|`IUiR3NG1RT`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463806`|`-`|
|`0x04000584`|`c3jRqDF8iv`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463816`|`-`|
|`0x04000585`|`NGERANqC4k`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463826`|`-`|
|`0x04000586`|`zEqRCE3l0u`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463836`|`-`|
|`0x04000587`|`ykMRmONAPD`|`System.Windows.Forms.Label [class:245]`|`1`|`13463846`|`-`|
|`0x04000588`|`QDCRQyBWkr`|`System.Windows.Forms.RadioButton [class:253]`|`1`|`13463856`|`-`|
|`0x04000589`|`e7qRkgVLqp`|`System.Windows.Forms.RadioButton [class:253]`|`1`|`13463866`|`-`|
|`0x0400058a`|`pPYR4ThNWy`|`System.Windows.Forms.Label [class:245]`|`1`|`13463876`|`-`|
|`0x0400058b`|`k4URYQTdpT`|`System.Windows.Forms.RadioButton [class:253]`|`1`|`13463886`|`-`|
|`0x0400058c`|`IBwRopHiNF`|`System.Windows.Forms.RadioButton [class:253]`|`1`|`13463896`|`-`|
|`0x0400058d`|`hkRR7kCmSU`|`System.Windows.Forms.Label [class:245]`|`1`|`13463906`|`-`|
|`0x0400058e`|`iDbRFesHup`|`System.Windows.Forms.Label [class:245]`|`1`|`13463916`|`-`|
|`0x0400058f`|`JpmRvTOgSW`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463926`|`-`|
|`0x04000590`|`nQoR13RTck`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463936`|`-`|
|`0x04000591`|`C5ERMMktdT`|`System.Windows.Forms.Label [class:245]`|`1`|`13463946`|`-`|
|`0x04000592`|`XqFR9WxXuA`|`System.Windows.Forms.RadioButton [class:253]`|`1`|`13463956`|`-`|
|`0x04000593`|`SNtRKxpg9O`|`System.Windows.Forms.RadioButton [class:253]`|`1`|`13463966`|`-`|
|`0x04000594`|`Lf6RNAuelO`|`System.Windows.Forms.Label [class:245]`|`1`|`13463976`|`-`|
|`0x04000595`|`X4FRbyyrXf`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13463986`|`-`|
|`0x04000596`|`kBBRI794yH`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13463996`|`-`|
|`0x04000597`|`Cd2RyZ6LVr`|`System.Windows.Forms.Button [class:161]`|`1`|`13464006`|`-`|
|`0x04000598`|`fFCRRieoKZ`|`System.Windows.Forms.Button [class:161]`|`1`|`13464016`|`-`|
|`0x04000599`|`JYbRg39f03`|`System.Windows.Forms.Button [class:161]`|`1`|`13464026`|`-`|
|`0x0400059a`|`H2uRhB3RHy`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13464036`|`-`|
|`0x0400059b`|`m6rRZByToJ`|`System.Windows.Forms.Label [class:245]`|`1`|`13464046`|`-`|
|`0x0400059c`|`nWoRehYORu`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464056`|`-`|
|`0x0400059d`|`YKRRLh4tgU`|`System.Windows.Forms.Panel [class:373]`|`1`|`13464066`|`-`|
|`0x0400059e`|`lZCRTlICfH`|`System.Windows.Forms.Button [class:161]`|`1`|`13464076`|`-`|
|`0x0400059f`|`mh4Rfnbhcq`|`System.Windows.Forms.Label [class:245]`|`1`|`13464086`|`-`|
|`0x040005a0`|`X3BRcmw4IZ`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464096`|`-`|
|`0x040005a1`|`hI0R6B9Mg7`|`System.Windows.Forms.Label [class:245]`|`1`|`13464106`|`-`|
|`0x040005a2`|`lNdRWGxJPy`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464116`|`-`|
|`0x040005a3`|`R0tRsK4Mwp`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464126`|`-`|
|`0x040005a4`|`AXeRte24pm`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464136`|`-`|
|`0x040005a5`|`HtNRdBb9ns`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464146`|`-`|
|`0x040005a6`|`tgoRJ0kBP3`|`System.Windows.Forms.CheckBox [class:257]`|`1`|`13464156`|`-`|
|`0x040005a7`|`XhXRrxa1Fe`|`System.Windows.Forms.ComboBox [class:377]`|`1`|`13464166`|`-`|
|`0x040005a8`|`PA2RGie2fG`|`System.Windows.Forms.Label [class:245]`|`1`|`13464176`|`-`|
|`0x040005a9`|`mN1RlL80vR`|`System.Windows.Forms.Button [class:161]`|`1`|`13464186`|`-`|
|`0x040005aa`|`s2WRuJsNFx`|`System.Windows.Forms.TabControl [class:365]`|`1`|`13464196`|`-`|
|`0x040005ab`|`CHZRHSHwfJ`|`System.Windows.Forms.TabPage [class:369]`|`1`|`13464206`|`-`|
|`0x040005ac`|`CtERp0BtrH`|`System.Windows.Forms.Panel [class:373]`|`1`|`13464216`|`-`|
|`0x040005ad`|`WL6RwWQa0J`|`System.Windows.Forms.Label [class:245]`|`1`|`13464226`|`-`|
|`0x040005ae`|`gdwR2FEIOH`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464236`|`-`|
|`0x040005af`|`PDPRXi1shf`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464246`|`-`|
|`0x040005b0`|`XnkRiB4nVP`|`System.Windows.Forms.Label [class:245]`|`1`|`13464256`|`-`|
|`0x040005b1`|`l3hRn1KxZ2`|`System.Windows.Forms.Label [class:245]`|`1`|`13464266`|`-`|
|`0x040005b2`|`kn1R5jRXdr`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464276`|`-`|
|`0x040005b3`|`j2IRSPFTXs`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464286`|`-`|
|`0x040005b4`|`hqcRjY6fT4`|`System.Windows.Forms.Label [class:245]`|`1`|`13464296`|`-`|
|`0x040005b5`|`PWPRxnNXJQ`|`System.Windows.Forms.Label [class:245]`|`1`|`13464306`|`-`|
|`0x040005b6`|`nFxRDS0gvA`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464316`|`-`|
|`0x040005b7`|`HgDRBuBPtP`|`System.Windows.Forms.Label [class:245]`|`1`|`13464326`|`-`|
|`0x040005b8`|`VgbROEjZ8v`|`System.Windows.Forms.Label [class:245]`|`1`|`13464336`|`-`|
|`0x040005b9`|`DQXRVWKo2L`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464346`|`-`|
|`0x040005ba`|`K9hRaHhSvS`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464356`|`-`|
|`0x040005bb`|`vS8RzNvH1S`|`System.Windows.Forms.CheckBox [class:257]`|`1`|`13464366`|`-`|
|`0x040005bc`|`lONgU7cFx7`|`System.Windows.Forms.Label [class:245]`|`1`|`13464376`|`-`|
|`0x040005bd`|`kAqg8dmhPy`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464386`|`-`|
|`0x040005be`|`lsRgEevDU8`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464396`|`-`|
|`0x040005bf`|`uLEg0moxUn`|`System.Windows.Forms.Label [class:245]`|`1`|`13464406`|`-`|
|`0x040005c0`|`nmOgP4Z2if`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464416`|`-`|
|`0x040005c1`|`wbRg3MfGmi`|`System.Windows.Forms.Label [class:245]`|`1`|`13464426`|`-`|
|`0x040005c2`|`bujgqRwROA`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464436`|`-`|
|`0x040005c3`|`joPgAwZb21`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464446`|`-`|
|`0x040005c4`|`scFgCr0wNu`|`System.Windows.Forms.Label [class:245]`|`1`|`13464456`|`-`|
|`0x040005c5`|`g2UgmcMbnD`|`System.Windows.Forms.Label [class:245]`|`1`|`13464466`|`-`|
|`0x040005c6`|`bHBgQf3AnP`|`System.Windows.Forms.Label [class:245]`|`1`|`13464476`|`-`|
|`0x040005c7`|`rLEgk0J1Hh`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464486`|`-`|
|`0x040005c8`|`aybg4qrdcf`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464496`|`-`|
|`0x040005c9`|`PGOgYQ1xAR`|`System.Windows.Forms.Label [class:245]`|`1`|`13464506`|`-`|
|`0x040005ca`|`bS0goUWis4`|`System.Windows.Forms.RadioButton [class:253]`|`1`|`13464516`|`-`|
|`0x040005cb`|`c0eg738Rng`|`System.Windows.Forms.CheckBox [class:257]`|`1`|`13464526`|`-`|
|`0x040005cc`|`tY5gFCjC3o`|`System.Windows.Forms.CheckBox [class:257]`|`1`|`13464536`|`-`|
|`0x040005cd`|`ctigvi2893`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464546`|`-`|
|`0x040005ce`|`V27g13o7K5`|`System.Windows.Forms.Label [class:245]`|`1`|`13464556`|`-`|
|`0x040005cf`|`ns2gMJFLwn`|`System.Windows.Forms.Button [class:161]`|`1`|`13464566`|`-`|
|`0x040005d0`|`grpg9w3HJD`|`System.Windows.Forms.CheckBox [class:257]`|`1`|`13464576`|`-`|
|`0x040005d1`|`rWbgKnGU6H`|`System.Windows.Forms.Panel [class:373]`|`1`|`13464586`|`-`|
|`0x040005d2`|`Gh1gNV8x5u`|`System.Windows.Forms.Panel [class:373]`|`1`|`13464596`|`-`|
|`0x040005d3`|`LAogb2anq9`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13464606`|`-`|
|`0x040005d4`|`kCggIsmnNc`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464616`|`-`|
|`0x040005d5`|`ixvgyQwgmH`|`System.Windows.Forms.Label [class:245]`|`1`|`13464626`|`-`|
|`0x040005d6`|`kUYgRYYfgj`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464636`|`-`|
|`0x040005d7`|`FXmggY9kHD`|`System.Windows.Forms.Label [class:245]`|`1`|`13464646`|`-`|
|`0x040005d8`|`fnPghKUhAd`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464656`|`-`|
|`0x040005d9`|`MFjgZKRYbo`|`System.Windows.Forms.Label [class:245]`|`1`|`13464666`|`-`|
|`0x040005da`|`pAAgerbMsJ`|`System.Windows.Forms.Label [class:245]`|`1`|`13464676`|`-`|
|`0x040005db`|`h6IgLD7bvh`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464686`|`-`|
|`0x040005dc`|`HtLgTQbuNH`|`System.Windows.Forms.TabControl [class:365]`|`1`|`13464696`|`-`|
|`0x040005dd`|`aOogf7lE7l`|`System.Windows.Forms.TabPage [class:369]`|`1`|`13464706`|`-`|
|`0x040005de`|`B6jgcp8bh9`|`System.Windows.Forms.Panel [class:373]`|`1`|`13464716`|`-`|
|`0x040005df`|`eicg6hJM2D`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464726`|`-`|
|`0x040005e0`|`BtegWdpAcl`|`System.Windows.Forms.Panel [class:373]`|`1`|`13464736`|`-`|
|`0x040005e1`|`fF7gsMw6Ol`|`System.Windows.Forms.ComboBox [class:377]`|`1`|`13464746`|`-`|
|`0x040005e2`|`UsMgtGsKAc`|`System.Windows.Forms.Label [class:245]`|`1`|`13464756`|`-`|
|`0x040005e3`|`gPIgd3owvt`|`System.Windows.Forms.Panel [class:373]`|`1`|`13464766`|`-`|
|`0x040005e4`|`jrDgJxw5Sn`|`System.Windows.Forms.Label [class:245]`|`1`|`13464776`|`-`|
|`0x040005e5`|`jpJgrWY54H`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464786`|`-`|
|`0x040005e6`|`XurgGmVN6S`|`System.Windows.Forms.Label [class:245]`|`1`|`13464796`|`-`|
|`0x040005e7`|`s0lglmQPFX`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464806`|`-`|
|`0x040005e8`|`EQPguGMmHQ`|`System.Windows.Forms.Label [class:245]`|`1`|`13464816`|`-`|
|`0x040005e9`|`ub6gHspxFw`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464826`|`-`|
|`0x040005ea`|`mZNgpTWgGF`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464836`|`-`|
|`0x040005eb`|`tsdgwiae1S`|`System.Windows.Forms.Label [class:245]`|`1`|`13464846`|`-`|
|`0x040005ec`|`avbg2ESQSu`|`System.Windows.Forms.ComboBox [class:377]`|`1`|`13464856`|`-`|
|`0x040005ed`|`PC3gX2GfkQ`|`System.Windows.Forms.Label [class:245]`|`1`|`13464866`|`-`|
|`0x040005ee`|`wkOgighNk4`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464876`|`-`|
|`0x040005ef`|`wBngndatah`|`System.Windows.Forms.Label [class:245]`|`1`|`13464886`|`-`|
|`0x040005f0`|`Ss1g5PbvxF`|`System.Windows.Forms.ComboBox [class:377]`|`1`|`13464896`|`-`|
|`0x040005f1`|`rmxgS47ekv`|`System.Windows.Forms.Label [class:245]`|`1`|`13464906`|`-`|
|`0x040005f2`|`JVwgj4ioK3`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464916`|`-`|
|`0x040005f3`|`zfxgxGof7l`|`System.Windows.Forms.Label [class:245]`|`1`|`13464926`|`-`|
|`0x040005f4`|`aifgD0NJPF`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13464936`|`-`|
|`0x040005f5`|`VMAgBIC1xm`|`System.Windows.Forms.Label [class:245]`|`1`|`13464946`|`-`|
|`0x040005f6`|`crygO64ktl`|`System.Windows.Forms.Button [class:161]`|`1`|`13464956`|`-`|
|`0x040005f7`|`mSlgVKqNF4`|`System.Windows.Forms.Panel [class:373]`|`1`|`13464966`|`-`|
|`0x040005f8`|`jn0gaqMkR8`|`System.Windows.Forms.RadioButton [class:253]`|`1`|`13464976`|`-`|
|`0x040005f9`|`TcYgzsCta5`|`System.Windows.Forms.RadioButton [class:253]`|`1`|`13464986`|`-`|
|`0x040005fa`|`NMWhUgqmQv`|`System.Windows.Forms.Label [class:245]`|`1`|`13464996`|`-`|
|`0x040005fb`|`Cith8SXpWa`|`System.Windows.Forms.Button [class:161]`|`1`|`13465006`|`-`|
|`0x040005fc`|`my8hE1PeQw`|`System.Windows.Forms.Button [class:161]`|`1`|`13465016`|`-`|
|`0x040005fd`|`GsNh0dvOHs`|`System.Windows.Forms.Label [class:245]`|`1`|`13465026`|`-`|
|`0x040005fe`|`AONhPkpGn8`|`System.Windows.Forms.Panel [class:373]`|`1`|`13465036`|`-`|
|`0x040005ff`|`KABh3MkHL9`|`System.Windows.Forms.Panel [class:373]`|`1`|`13465046`|`-`|
|`0x04000600`|`qVXhqYeiWT`|`System.Windows.Forms.Panel [class:373]`|`1`|`13465056`|`-`|
|`0x04000601`|`J5MhAl6INx`|`System.Windows.Forms.Panel [class:373]`|`1`|`13465066`|`-`|
|`0x04000602`|`dlZhCEf0DN`|`System.Windows.Forms.Panel [class:373]`|`1`|`13465076`|`-`|
|`0x04000603`|`lKNhmIM6Sv`|`System.Windows.Forms.Panel [class:373]`|`1`|`13465086`|`-`|
|`0x04000604`|`uonhQ7m8jp`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13465096`|`-`|
|`0x04000605`|`WDVhkFXymy`|`System.Windows.Forms.Panel [class:373]`|`1`|`13465106`|`-`|
|`0x04000606`|`ooyh44nFJR`|`System.Windows.Forms.Panel [class:373]`|`1`|`13465116`|`-`|
|`0x04000607`|`HJ9hYiLDAC`|`System.Windows.Forms.Panel [class:373]`|`1`|`13465126`|`-`|
|`0x04000608`|`Bo4hohGd1P`|`System.Windows.Forms.Panel [class:373]`|`1`|`13465136`|`-`|
|`0x04000609`|`Osqh7WreR4`|`System.Windows.Forms.RadioButton [class:253]`|`1`|`13465146`|`-`|
|`0x0400060a`|`tcvhFPRv2x`|`System.Windows.Forms.RadioButton [class:253]`|`1`|`13465156`|`-`|
|`0x0400060b`|`sBbhvxMKWQ`|`System.Windows.Forms.Label [class:245]`|`1`|`13465166`|`-`|
|`0x0400060c`|`h7Bh1vaRuE`|`System.Windows.Forms.Button [class:161]`|`1`|`13465176`|`-`|
|`0x0400060d`|`da_hien_form`|`bool`|`6`|`13465186`|`-`|
|`0x0400060e`|`zjDhMX2Dcr`|`bool`|`1`|`13465196`|`-`|
|`0x0400060f`|`PCFh9eTSC4`|`Kata_Class_Lib_Revit.PreviewCanvas [class:261]`|`1`|`13465206`|`-`|
|`0x04000610`|`yRvhKAynAb`|`bool`|`1`|`13465216`|`-`|
|`0x04000611`|`dREhNg0A1u`|`System.Drawing.Point [valuetype:173]`|`1`|`13465226`|`-`|
|`0x04000612`|`PBOhbxoxKo`|`System.Drawing.Point [valuetype:173]`|`1`|`13465236`|`-`|
|`0x04000613`|`lsfhIxjkLB`|`System.Windows.Forms.Timer [class:737]`|`1`|`13465246`|`-`|
|`0x04000614`|`qyOcumqEHhq5wUiuekAH`|`Kata_pro64_Cad2025.Form_Mong_Coc_V2 [class:464]`|`19`|`13465256`|`-`|

### [VM-091] Type `Kata_pro64_Cad2025.Form_Rai_Mong` (Token `0x0200007d`, RowOffset `13369822`, Extends `0x0100000f (System.Windows.Forms.Form)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 70 | Methods count: 143
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04000638`|`mtAZKSe0Jw`|`System.ComponentModel.IContainer [class:157]`|`1`|`13465616`|`-`|
|`0x04000639`|`qegZN57Oeq`|`System.Windows.Forms.TabControl [class:365]`|`1`|`13465626`|`-`|
|`0x0400063a`|`SvWZbQ0R9W`|`System.Windows.Forms.TabPage [class:369]`|`1`|`13465636`|`-`|
|`0x0400063b`|`c8pZI2XeiE`|`System.Windows.Forms.Panel [class:373]`|`1`|`13465646`|`-`|
|`0x0400063c`|`NOEZy0INgj`|`System.Windows.Forms.Panel [class:373]`|`1`|`13465656`|`-`|
|`0x0400063d`|`QQyZRdsRvU`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13465666`|`-`|
|`0x0400063e`|`MTRZg5K0yE`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13465676`|`-`|
|`0x0400063f`|`eNXZhcLJBH`|`System.Windows.Forms.Label [class:245]`|`1`|`13465686`|`-`|
|`0x04000640`|`bH3ZZBr9ii`|`System.Windows.Forms.Label [class:245]`|`1`|`13465696`|`-`|
|`0x04000641`|`MC4ZeqEfvR`|`System.Windows.Forms.Label [class:245]`|`1`|`13465706`|`-`|
|`0x04000642`|`AmXZLt5GCK`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13465716`|`-`|
|`0x04000643`|`w2bZT9g6yB`|`System.Windows.Forms.Button [class:161]`|`1`|`13465726`|`-`|
|`0x04000644`|`LGUZfTkF4w`|`System.Windows.Forms.Button [class:161]`|`1`|`13465736`|`-`|
|`0x04000645`|`J4nZc8xmc3`|`System.Windows.Forms.Button [class:161]`|`1`|`13465746`|`-`|
|`0x04000646`|`GR7Z6YhmtH`|`System.Windows.Forms.NumericUpDown [class:325]`|`1`|`13465756`|`-`|
|`0x04000647`|`okVZW2Hl2m`|`System.Windows.Forms.RadioButton [class:253]`|`1`|`13465766`|`-`|
|`0x04000648`|`KmcZsOyKrm`|`System.Windows.Forms.Label [class:245]`|`1`|`13465776`|`-`|
|`0x04000649`|`mJ9ZtwYO31`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13465786`|`-`|
|`0x0400064a`|`vUMZdrvckL`|`System.Windows.Forms.ComboBox [class:377]`|`1`|`13465796`|`-`|
|`0x0400064b`|`CIKZJvYliL`|`System.Windows.Forms.Label [class:245]`|`1`|`13465806`|`-`|
|`0x0400064c`|`JK7ZrRJR50`|`System.Windows.Forms.TabPage [class:369]`|`1`|`13465816`|`-`|
|`0x0400064d`|`av0ZG4tQIu`|`System.Windows.Forms.Panel [class:373]`|`1`|`13465826`|`-`|
|`0x0400064e`|`adDZlh2Mc2`|`System.Windows.Forms.RadioButton [class:253]`|`1`|`13465836`|`-`|
|`0x0400064f`|`TLYZulM8wg`|`System.Windows.Forms.Panel [class:373]`|`1`|`13465846`|`-`|
|`0x04000650`|`DsYZHXGZxv`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13465856`|`-`|
|`0x04000651`|`p6nZp87X6F`|`System.Windows.Forms.ComboBox [class:377]`|`1`|`13465866`|`-`|
|`0x04000652`|`Sc8Zw8KbxE`|`System.Windows.Forms.Label [class:245]`|`1`|`13465876`|`-`|
|`0x04000653`|`VGoZ2ojeFC`|`System.Windows.Forms.Label [class:245]`|`1`|`13465886`|`-`|
|`0x04000654`|`i0SZX1O8Z6`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13465896`|`-`|
|`0x04000655`|`TxIZihCO9T`|`System.Windows.Forms.Button [class:161]`|`1`|`13465906`|`-`|
|`0x04000656`|`SnDZna3UE5`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13465916`|`-`|
|`0x04000657`|`aT9Z5v10et`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13465926`|`-`|
|`0x04000658`|`OiRZSvYLLo`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13465936`|`-`|
|`0x04000659`|`NvRZjBsZ1A`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13465946`|`-`|
|`0x0400065a`|`dMjZx7sVm4`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13465956`|`-`|
|`0x0400065b`|`i42ZDiqfUP`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13465966`|`-`|
|`0x0400065c`|`dHrZBZXHHr`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13465976`|`-`|
|`0x0400065d`|`CI5ZOJyYCK`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13465986`|`-`|
|`0x0400065e`|`oj1ZVuAQ4E`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13465996`|`-`|
|`0x0400065f`|`McJZampKKD`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13466006`|`-`|
|`0x04000660`|`w1GZz3S2As`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13466016`|`-`|
|`0x04000661`|`x7AeU2kO5i`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13466026`|`-`|
|`0x04000662`|`Du7e85ZbFO`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13466036`|`-`|
|`0x04000663`|`h9HeElMFWJ`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13466046`|`-`|
|`0x04000664`|`m1Ie0wOusq`|`System.Windows.Forms.Label [class:245]`|`1`|`13466056`|`-`|
|`0x04000665`|`TGbePSZXSQ`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13466066`|`-`|
|`0x04000666`|`gLpe3nxcpi`|`System.Windows.Forms.Button [class:161]`|`1`|`13466076`|`-`|
|`0x04000667`|`bx9eqVZMJZ`|`System.Windows.Forms.Label [class:245]`|`1`|`13466086`|`-`|
|`0x04000668`|`dfQeAaP0RC`|`System.Windows.Forms.Label [class:245]`|`1`|`13466096`|`-`|
|`0x04000669`|`nQIeCNuse7`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13466106`|`-`|
|`0x0400066a`|`PQDemSsBnl`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13466116`|`-`|
|`0x0400066b`|`XWXeQV2Y5g`|`System.Windows.Forms.ComboBox [class:377]`|`1`|`13466126`|`-`|
|`0x0400066c`|`XTdekoUEPe`|`System.Windows.Forms.Label [class:245]`|`1`|`13466136`|`-`|
|`0x0400066d`|`mkle4ssy2x`|`System.Windows.Forms.ComboBox [class:377]`|`1`|`13466146`|`-`|
|`0x0400066e`|`vqxeYLPdgO`|`System.Windows.Forms.Label [class:245]`|`1`|`13466156`|`-`|
|`0x0400066f`|`ku6eo1w0SK`|`Kata_Class_Lib_Revit.PreviewCanvas [class:261]`|`1`|`13466166`|`-`|
|`0x04000670`|`PZte72W1dD`|`r8`|`1`|`13466176`|`-`|
|`0x04000671`|`shEeFVLekJ`|`System.Drawing.Point [valuetype:173]`|`1`|`13466186`|`-`|
|`0x04000672`|`w80evl8kaU`|`Kata_Class_Lib_Revit.PreviewCanvas [class:261]`|`1`|`13466196`|`-`|
|`0x04000673`|`qyxe1Nsk8l`|`r8`|`1`|`13466206`|`-`|
|`0x04000674`|`eQxeM0iHGa`|`System.Drawing.Point [valuetype:173]`|`1`|`13466216`|`-`|
|`0x04000675`|`da_hien_form`|`bool`|`6`|`13466226`|`-`|
|`0x04000676`|`Xkge9gTLUU`|`bool`|`1`|`13466236`|`-`|
|`0x04000677`|`iBceKAdmgC`|`bool`|`1`|`13466246`|`-`|
|`0x04000678`|`DVJeNB2K4F`|`System.Drawing.Point [valuetype:173]`|`1`|`13466256`|`-`|
|`0x04000679`|`LOMebiEthT`|`System.Drawing.Point [valuetype:173]`|`1`|`13466266`|`-`|
|`0x0400067a`|`VsAeIGJLgl`|`bool`|`1`|`13466276`|`-`|
|`0x0400067b`|`WhVeygHlA7`|`System.Drawing.Point [valuetype:173]`|`1`|`13466286`|`-`|
|`0x0400067c`|`uf1eRR18Xc`|`System.Drawing.Point [valuetype:173]`|`1`|`13466296`|`-`|
|`0x0400067d`|`jecQicqEjf2dAwJig3pE`|`Kata_pro64_Cad2025.Form_Rai_Mong [class:500]`|`17`|`13466306`|`-`|

### [VM-092] Type `Kata_pro64_Cad2025.Form_Ve_Mat_Bang_Mong` (Token `0x0200009d`, RowOffset `13370398`, Extends `0x0100000f (System.Windows.Forms.Form)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 278 | Methods count: 650
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x0400083a`|`OWRJNmntTi`|`System.ComponentModel.IContainer [class:157]`|`1`|`13470756`|`-`|
|`0x0400083b`|`cMtJbQ4oo4`|`System.Windows.Forms.Panel [class:373]`|`1`|`13470766`|`-`|
|`0x0400083c`|`eBXJItk6eE`|`System.Windows.Forms.Button [class:161]`|`1`|`13470776`|`-`|
|`0x0400083d`|`IGiJyU7Vcs`|`System.Windows.Forms.Panel [class:373]`|`1`|`13470786`|`-`|
|`0x0400083e`|`cmPJRcFMZe`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13470796`|`-`|
|`0x0400083f`|`Gj6JgDw04G`|`System.Windows.Forms.Label [class:245]`|`1`|`13470806`|`-`|
|`0x04000840`|`PQTJhVlayy`|`System.Windows.Forms.Button [class:161]`|`1`|`13470816`|`-`|
|`0x04000841`|`vMqJZSmeis`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13470826`|`-`|
|`0x04000842`|`y1ZJe1ZlH2`|`System.Windows.Forms.Label [class:245]`|`1`|`13470836`|`-`|
|`0x04000843`|`GxAJL1O5RH`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13470846`|`-`|
|`0x04000844`|`NIrJTO6GWr`|`System.Windows.Forms.Label [class:245]`|`1`|`13470856`|`-`|
|`0x04000845`|`XfjJfWaabI`|`System.Windows.Forms.Button [class:161]`|`1`|`13470866`|`-`|
|`0x04000846`|`i6hJcmXFuR`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13470876`|`-`|
|`0x04000847`|`KUCJ66f5PN`|`System.Windows.Forms.Label [class:245]`|`1`|`13470886`|`-`|
|`0x04000848`|`sRjJW8wqIs`|`System.Windows.Forms.TabControl [class:365]`|`1`|`13470896`|`-`|
|`0x04000849`|`T0ZJsc7cKQ`|`System.Windows.Forms.TabPage [class:369]`|`1`|`13470906`|`-`|
|`0x0400084a`|`ewfJtDqK73`|`System.Windows.Forms.Label [class:245]`|`1`|`13470916`|`-`|
|`0x0400084b`|`DCEJdNdXxL`|`System.Windows.Forms.Label [class:245]`|`1`|`13470926`|`-`|
|`0x0400084c`|`wAEJJtUWAt`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13470936`|`-`|
|`0x0400084d`|`G8OJrkbeGj`|`System.Windows.Forms.Label [class:245]`|`1`|`13470946`|`-`|
|`0x0400084e`|`J8VJGdyRMB`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13470956`|`-`|
|`0x0400084f`|`FkVJlBRc7s`|`System.Windows.Forms.ComboBox [class:377]`|`1`|`13470966`|`-`|
|`0x04000850`|`uKiJuYDcJH`|`System.Windows.Forms.Label [class:245]`|`1`|`13470976`|`-`|
|`0x04000851`|`r0tJHfHDsP`|`System.Windows.Forms.Button [class:161]`|`1`|`13470986`|`-`|
|`0x04000852`|`bpZJpmIro4`|`System.Windows.Forms.TabPage [class:369]`|`1`|`13470996`|`-`|
|`0x04000853`|`HdAJwGArT0`|`System.Windows.Forms.Panel [class:373]`|`1`|`13471006`|`-`|
|`0x04000854`|`EhEJ2V8fs3`|`System.Windows.Forms.Panel [class:373]`|`1`|`13471016`|`-`|
|`0x04000855`|`UEnJXaYS3m`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13471026`|`-`|
|`0x04000856`|`QjUJig5FHN`|`System.Windows.Forms.Label [class:245]`|`1`|`13471036`|`-`|
|`0x04000857`|`UoxJn8HO7W`|`System.Windows.Forms.Button [class:161]`|`1`|`13471046`|`-`|
|`0x04000858`|`j0EJ55Qn3m`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13471056`|`-`|
|`0x04000859`|`NiXJSgxSJa`|`System.Windows.Forms.Button [class:161]`|`1`|`13471066`|`-`|
|`0x0400085a`|`hVnJjrUjvm`|`System.Windows.Forms.Button [class:161]`|`1`|`13471076`|`-`|
|`0x0400085b`|`A8QJxd7yru`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13471086`|`-`|
|`0x0400085c`|`NliJDBseqv`|`System.Windows.Forms.Label [class:245]`|`1`|`13471096`|`-`|
|`0x0400085d`|`c5VJBMpjUT`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13471106`|`-`|
|`0x0400085e`|`uEvJOutJLY`|`System.Windows.Forms.DataGridView [class:117]`|`1`|`13471116`|`-`|
|`0x0400085f`|`nS8JVLHpn4`|`System.Windows.Forms.DataGridView [class:117]`|`1`|`13471126`|`-`|
|`0x04000860`|`CluJaRnCaW`|`System.Windows.Forms.Button [class:161]`|`1`|`13471136`|`-`|
|`0x04000861`|`m5PJzirCni`|`System.Windows.Forms.Button [class:161]`|`1`|`13471146`|`-`|
|`0x04000862`|`BmDrUFpWU7`|`System.Windows.Forms.Label [class:245]`|`1`|`13471156`|`-`|
|`0x04000863`|`Mibr8mmD0O`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13471166`|`-`|
|`0x04000864`|`A8frERJX1T`|`System.Windows.Forms.Label [class:245]`|`1`|`13471176`|`-`|
|`0x04000865`|`Fo7r0U5pXi`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13471186`|`-`|
|`0x04000866`|`A4KrPk7fx2`|`System.Windows.Forms.Button [class:161]`|`1`|`13471196`|`-`|
|`0x04000867`|`qH9r3EolbZ`|`System.Windows.Forms.Label [class:245]`|`1`|`13471206`|`-`|
|`0x04000868`|`W8crqiaf4l`|`System.Windows.Forms.TabPage [class:369]`|`1`|`13471216`|`-`|
|`0x04000869`|`KCRrAOjLfb`|`System.Windows.Forms.Panel [class:373]`|`1`|`13471226`|`-`|
|`0x0400086a`|`JgbrC8T9yW`|`System.Windows.Forms.Panel [class:373]`|`1`|`13471236`|`-`|
|`0x0400086b`|`jVyrmqDqxn`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13471246`|`-`|
|`0x0400086c`|`cpCrQKuaqi`|`System.Windows.Forms.Button [class:161]`|`1`|`13471256`|`-`|
|`0x0400086d`|`wttrkWOhdY`|`System.Windows.Forms.ComboBox [class:377]`|`1`|`13471266`|`-`|
|`0x0400086e`|`kC6r4Wkq0X`|`System.Windows.Forms.Label [class:245]`|`1`|`13471276`|`-`|
|`0x0400086f`|`ntSrYDJgEb`|`System.Windows.Forms.Label [class:245]`|`1`|`13471286`|`-`|
|`0x04000870`|`kYGro2T9kx`|`System.Windows.Forms.Label [class:245]`|`1`|`13471296`|`-`|
|`0x04000871`|`CsXr7TFtfA`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13471306`|`-`|
|`0x04000872`|`eJwrFUWxEJ`|`System.Windows.Forms.Label [class:245]`|`1`|`13471316`|`-`|
|`0x04000873`|`tpkrvnDjQj`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13471326`|`-`|
|`0x04000874`|`RITr1ekOyg`|`System.Windows.Forms.Label [class:245]`|`1`|`13471336`|`-`|
|`0x04000875`|`pvCrM4lmoo`|`System.Windows.Forms.Button [class:161]`|`1`|`13471346`|`-`|
|`0x04000876`|`fNwr9w0PV8`|`System.Windows.Forms.Button [class:161]`|`1`|`13471356`|`-`|
|`0x04000877`|`aRQrKstRQH`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13471366`|`-`|
|`0x04000878`|`oACrNC7t2D`|`System.Windows.Forms.Button [class:161]`|`1`|`13471376`|`-`|
|`0x04000879`|`RJwrbhs8p2`|`System.Windows.Forms.Label [class:245]`|`1`|`13471386`|`-`|
|`0x0400087a`|`HYNrII6Fxi`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13471396`|`-`|
|`0x0400087b`|`V7JryvKiOC`|`System.Windows.Forms.DataGridView [class:117]`|`1`|`13471406`|`-`|
|`0x0400087c`|`bTFrR6qZs8`|`System.Windows.Forms.Label [class:245]`|`1`|`13471416`|`-`|
|`0x0400087d`|`dWrrgo4vWh`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13471426`|`-`|
|`0x0400087e`|`ix9rhpRPdA`|`System.Windows.Forms.Button [class:161]`|`1`|`13471436`|`-`|
|`0x0400087f`|`w9arZcRrSf`|`System.Windows.Forms.Label [class:245]`|`1`|`13471446`|`-`|
|`0x04000880`|`xGarebGHu5`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13471456`|`-`|
|`0x04000881`|`kUhrLF6F2T`|`System.Windows.Forms.Label [class:245]`|`1`|`13471466`|`-`|
|`0x04000882`|`t5urTAqKiM`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13471476`|`-`|
|`0x04000883`|`qF6rfrUpKR`|`System.Windows.Forms.Label [class:245]`|`1`|`13471486`|`-`|
|`0x04000884`|`XBorcrEUcf`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13471496`|`-`|
|`0x04000885`|`y7nr6QffQi`|`System.Windows.Forms.Label [class:245]`|`1`|`13471506`|`-`|
|`0x04000886`|`nvsrWSycDe`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13471516`|`-`|
|`0x04000887`|`LNIrssZWt7`|`System.Windows.Forms.Label [class:245]`|`1`|`13471526`|`-`|
|`0x04000888`|`ViDrtAuGcM`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13471536`|`-`|
|`0x04000889`|`PBhrdM6GdF`|`System.Windows.Forms.Label [class:245]`|`1`|`13471546`|`-`|
|`0x0400088a`|`FiorJ2pdPJ`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13471556`|`-`|
|`0x0400088b`|`zsVrrYpA6q`|`System.Windows.Forms.Button [class:161]`|`1`|`13471566`|`-`|
|`0x0400088c`|`pJbrGqs3Y3`|`System.Windows.Forms.Label [class:245]`|`1`|`13471576`|`-`|
|`0x0400088d`|`mg6rlir9Tf`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13471586`|`-`|
|`0x0400088e`|`wvrruCxit8`|`System.Windows.Forms.Button [class:161]`|`1`|`13471596`|`-`|
|`0x0400088f`|`EYnrH7RL4W`|`System.Windows.Forms.Label [class:245]`|`1`|`13471606`|`-`|
|`0x04000890`|`GsqrpdeEoi`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13471616`|`-`|
|`0x04000891`|`K8prwhEMJJ`|`System.Windows.Forms.Button [class:161]`|`1`|`13471626`|`-`|
|`0x04000892`|`Gqrr2i4VEC`|`System.Windows.Forms.Label [class:245]`|`1`|`13471636`|`-`|
|`0x04000893`|`dJkrX8TeNO`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13471646`|`-`|
|`0x04000894`|`uq4riMl9Iv`|`System.Windows.Forms.Label [class:245]`|`1`|`13471656`|`-`|
|`0x04000895`|`NLNrnuMAqP`|`System.Windows.Forms.Button [class:161]`|`1`|`13471666`|`-`|
|`0x04000896`|`hh2r5grBbi`|`System.Windows.Forms.Label [class:245]`|`1`|`13471676`|`-`|
|`0x04000897`|`SqprS6Agwc`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13471686`|`-`|
|`0x04000898`|`r5BrjrL0oN`|`System.Windows.Forms.Panel [class:373]`|`1`|`13471696`|`-`|
|`0x04000899`|`nLfrxZy21e`|`System.Windows.Forms.Panel [class:373]`|`1`|`13471706`|`-`|
|`0x0400089a`|`P9urDtCO4i`|`System.Windows.Forms.Panel [class:373]`|`1`|`13471716`|`-`|
|`0x0400089b`|`EcCrBhLlcf`|`System.Windows.Forms.Button [class:161]`|`1`|`13471726`|`-`|
|`0x0400089c`|`DLprOgnYoI`|`System.Windows.Forms.Button [class:161]`|`1`|`13471736`|`-`|
|`0x0400089d`|`z0frVl0h81`|`System.Windows.Forms.ComboBox [class:377]`|`1`|`13471746`|`-`|
|`0x0400089e`|`lrJraGUi34`|`System.Windows.Forms.Label [class:245]`|`1`|`13471756`|`-`|
|`0x0400089f`|`Sa1rzJQbYr`|`System.Windows.Forms.ComboBox [class:377]`|`1`|`13471766`|`-`|
|`0x040008a0`|`Q8SGU4ZJl6`|`System.Windows.Forms.Label [class:245]`|`1`|`13471776`|`-`|
|`0x040008a1`|`yS6G8OGPQM`|`System.Windows.Forms.Button [class:161]`|`1`|`13471786`|`-`|
|`0x040008a2`|`DLbGEbpDrB`|`System.Windows.Forms.ComboBox [class:377]`|`1`|`13471796`|`-`|
|`0x040008a3`|`dBqG03ZfCE`|`System.Windows.Forms.Label [class:245]`|`1`|`13471806`|`-`|
|`0x040008a4`|`fntGPh3r04`|`System.Windows.Forms.Button [class:161]`|`1`|`13471816`|`-`|
|`0x040008a5`|`z8VG3D8GWB`|`System.Windows.Forms.Button [class:161]`|`1`|`13471826`|`-`|
|`0x040008a6`|`tddGqQ76UW`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13471836`|`-`|
|`0x040008a7`|`DGvGAhaVsi`|`System.Windows.Forms.Button [class:161]`|`1`|`13471846`|`-`|
|`0x040008a8`|`Ce6GC1jYyW`|`System.Windows.Forms.Button [class:161]`|`1`|`13471856`|`-`|
|`0x040008a9`|`UiBGmFrg8c`|`System.Windows.Forms.Button [class:161]`|`1`|`13471866`|`-`|
|`0x040008aa`|`Jr5GQkEpxF`|`System.Windows.Forms.Button [class:161]`|`1`|`13471876`|`-`|
|`0x040008ab`|`zEpGkevs3G`|`System.Windows.Forms.Button [class:161]`|`1`|`13471886`|`-`|
|`0x040008ac`|`aFgG49JaSj`|`System.Windows.Forms.Button [class:161]`|`1`|`13471896`|`-`|
|`0x040008ad`|`hpUGYNL3va`|`System.Windows.Forms.Panel [class:373]`|`1`|`13471906`|`-`|
|`0x040008ae`|`BsSGoqx6pe`|`System.Windows.Forms.DataGridView [class:117]`|`1`|`13471916`|`-`|
|`0x040008af`|`ofLG7wkQe6`|`System.Windows.Forms.Panel [class:373]`|`1`|`13471926`|`-`|
|`0x040008b0`|`xx8GFvs0uV`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13471936`|`-`|
|`0x040008b1`|`pVPGvMUnwP`|`System.Windows.Forms.Panel [class:373]`|`1`|`13471946`|`-`|
|`0x040008b2`|`wflG1TvPAN`|`System.Windows.Forms.Button [class:161]`|`1`|`13471956`|`-`|
|`0x040008b3`|`wwSGM8mCyZ`|`System.Windows.Forms.Button [class:161]`|`1`|`13471966`|`-`|
|`0x040008b4`|`K8aG9CSsl1`|`System.Windows.Forms.Button [class:161]`|`1`|`13471976`|`-`|
|`0x040008b5`|`GInGKIwHJZ`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13471986`|`-`|
|`0x040008b6`|`x2QGNXepxv`|`System.Windows.Forms.RadioButton [class:253]`|`1`|`13471996`|`-`|
|`0x040008b7`|`F8AGbZrFD4`|`System.Windows.Forms.RadioButton [class:253]`|`1`|`13472006`|`-`|
|`0x040008b8`|`TY7GIZtlOd`|`System.Windows.Forms.Button [class:161]`|`1`|`13472016`|`-`|
|`0x040008b9`|`uMjGyoWHtX`|`System.Windows.Forms.Button [class:161]`|`1`|`13472026`|`-`|
|`0x040008ba`|`AOHGR5OXnT`|`System.Windows.Forms.TabControl [class:365]`|`1`|`13472036`|`-`|
|`0x040008bb`|`g9MGgnRqPv`|`System.Windows.Forms.TabPage [class:369]`|`1`|`13472046`|`-`|
|`0x040008bc`|`yTUGh5BSuT`|`System.Windows.Forms.TabPage [class:369]`|`1`|`13472056`|`-`|
|`0x040008bd`|`TqXGZAlT2J`|`System.Windows.Forms.TabPage [class:369]`|`1`|`13472066`|`-`|
|`0x040008be`|`ec5GewjiGh`|`System.Windows.Forms.Button [class:161]`|`1`|`13472076`|`-`|
|`0x040008bf`|`PVbGL82OU4`|`System.Windows.Forms.Button [class:161]`|`1`|`13472086`|`-`|
|`0x040008c0`|`BxBGTurff3`|`System.Windows.Forms.Panel [class:373]`|`1`|`13472096`|`-`|
|`0x040008c1`|`t4nGfTm6Kk`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13472106`|`-`|
|`0x040008c2`|`JIFGcbXIBF`|`System.Windows.Forms.Button [class:161]`|`1`|`13472116`|`-`|
|`0x040008c3`|`PmJG65HGOv`|`System.Windows.Forms.Button [class:161]`|`1`|`13472126`|`-`|
|`0x040008c4`|`d6SGWODMQw`|`System.Windows.Forms.Panel [class:373]`|`1`|`13472136`|`-`|
|`0x040008c5`|`cPcGsaRw8x`|`System.Windows.Forms.Label [class:245]`|`1`|`13472146`|`-`|
|`0x040008c6`|`LbjGty0kKi`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13472156`|`-`|
|`0x040008c7`|`HobGdERvr8`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13472166`|`-`|
|`0x040008c8`|`H3KGJe3YnJ`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13472176`|`-`|
|`0x040008c9`|`hLwGrXdIvB`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13472186`|`-`|
|`0x040008ca`|`CrxGG6Wo2d`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13472196`|`-`|
|`0x040008cb`|`aWeGliPVaj`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13472206`|`-`|
|`0x040008cc`|`au3Gu2bOY9`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13472216`|`-`|
|`0x040008cd`|`yogGHFQlH6`|`System.Windows.Forms.CheckBox [class:257]`|`1`|`13472226`|`-`|
|`0x040008ce`|`KLyGpMMX7k`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13472236`|`-`|
|`0x040008cf`|`FZ4GwHVvgr`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13472246`|`-`|
|`0x040008d0`|`ExnG2NORZ9`|`System.Windows.Forms.Label [class:245]`|`1`|`13472256`|`-`|
|`0x040008d1`|`dLyGXIq12l`|`System.Windows.Forms.Button [class:161]`|`1`|`13472266`|`-`|
|`0x040008d2`|`QEwGimYXhL`|`System.Windows.Forms.Button [class:161]`|`1`|`13472276`|`-`|
|`0x040008d3`|`LFEGndQjnY`|`System.Windows.Forms.Panel [class:373]`|`1`|`13472286`|`-`|
|`0x040008d4`|`VsZG58gXVG`|`System.Windows.Forms.Label [class:245]`|`1`|`13472296`|`-`|
|`0x040008d5`|`SbIGS3vLS7`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13472306`|`-`|
|`0x040008d6`|`LSCGj5OdYl`|`System.Windows.Forms.Label [class:245]`|`1`|`13472316`|`-`|
|`0x040008d7`|`dsnGxGPHtR`|`System.Windows.Forms.Label [class:245]`|`1`|`13472326`|`-`|
|`0x040008d8`|`eOCGD7F5YU`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13472336`|`-`|
|`0x040008d9`|`HIqGBnsxf2`|`System.Windows.Forms.Label [class:245]`|`1`|`13472346`|`-`|
|`0x040008da`|`DbVGOb5RIk`|`System.Windows.Forms.ComboBox [class:377]`|`1`|`13472356`|`-`|
|`0x040008db`|`omwGVT2B6S`|`System.Windows.Forms.Label [class:245]`|`1`|`13472366`|`-`|
|`0x040008dc`|`tLXGab5b0I`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13472376`|`-`|
|`0x040008dd`|`VvYGzlUOMa`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13472386`|`-`|
|`0x040008de`|`TiMlUy6Kwc`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13472396`|`-`|
|`0x040008df`|`t7nl8KIeMC`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13472406`|`-`|
|`0x040008e0`|`vPElESdMFe`|`System.Windows.Forms.Panel [class:373]`|`1`|`13472416`|`-`|
|`0x040008e1`|`a3nl0k0fOK`|`System.Windows.Forms.ComboBox [class:377]`|`1`|`13472426`|`-`|
|`0x040008e2`|`zxdlPgVLTN`|`System.Windows.Forms.Button [class:161]`|`1`|`13472436`|`-`|
|`0x040008e3`|`ANNl3UUeJI`|`System.Windows.Forms.Label [class:245]`|`1`|`13472446`|`-`|
|`0x040008e4`|`LKFlqXWa7m`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13472456`|`-`|
|`0x040008e5`|`e0VlAPJFIx`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13472466`|`-`|
|`0x040008e6`|`UYClCsHeeN`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13472476`|`-`|
|`0x040008e7`|`MAOlmFt8Nf`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13472486`|`-`|
|`0x040008e8`|`r5flQA1yeX`|`System.Windows.Forms.DataGridViewComboBoxColumn [class:817]`|`1`|`13472496`|`-`|
|`0x040008e9`|`UhUlkMIpfv`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13472506`|`-`|
|`0x040008ea`|`dOFl4F8bYC`|`System.Windows.Forms.DataGridViewComboBoxColumn [class:817]`|`1`|`13472516`|`-`|
|`0x040008eb`|`i5YlY5AASo`|`System.Windows.Forms.DataGridViewComboBoxColumn [class:817]`|`1`|`13472526`|`-`|
|`0x040008ec`|`WTyloAfyWn`|`System.Windows.Forms.ComboBox [class:377]`|`1`|`13472536`|`-`|
|`0x040008ed`|`TeRl7pmYB4`|`System.Windows.Forms.Button [class:161]`|`1`|`13472546`|`-`|
|`0x040008ee`|`mIulFUfZCs`|`System.Windows.Forms.Label [class:245]`|`1`|`13472556`|`-`|
|`0x040008ef`|`rBulvaEfju`|`System.Windows.Forms.Button [class:161]`|`1`|`13472566`|`-`|
|`0x040008f0`|`IDgl1avQyI`|`System.Windows.Forms.Button [class:161]`|`1`|`13472576`|`-`|
|`0x040008f1`|`ifVlMDhure`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13472586`|`-`|
|`0x040008f2`|`LS5l9tdbn2`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13472596`|`-`|
|`0x040008f3`|`bDelKIvqcp`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13472606`|`-`|
|`0x040008f4`|`rDrlNJpmkt`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13472616`|`-`|
|`0x040008f5`|`MDxlbNCo4l`|`System.Windows.Forms.Label [class:245]`|`1`|`13472626`|`-`|
|`0x040008f6`|`BgUlIt0qHX`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13472636`|`-`|
|`0x040008f7`|`z6klylahdj`|`System.Windows.Forms.Label [class:245]`|`1`|`13472646`|`-`|
|`0x040008f8`|`yRAlRYm03k`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13472656`|`-`|
|`0x040008f9`|`ByglgrhHI8`|`System.Windows.Forms.Label [class:245]`|`1`|`13472666`|`-`|
|`0x040008fa`|`daylhNprTT`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13472676`|`-`|
|`0x040008fb`|`bJrlZOZn53`|`System.Windows.Forms.Label [class:245]`|`1`|`13472686`|`-`|
|`0x040008fc`|`XrOle1a7mW`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13472696`|`-`|
|`0x040008fd`|`SrJlLEFP8V`|`System.Windows.Forms.Label [class:245]`|`1`|`13472706`|`-`|
|`0x040008fe`|`vmFlTeDcFN`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13472716`|`-`|
|`0x040008ff`|`O1wlfDKaQk`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13472726`|`-`|
|`0x04000900`|`y6clcF2PIS`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13472736`|`-`|
|`0x04000901`|`xvZl6RCcmS`|`System.Windows.Forms.Button [class:161]`|`1`|`13472746`|`-`|
|`0x04000902`|`aaclWsfRUv`|`System.Windows.Forms.Button [class:161]`|`1`|`13472756`|`-`|
|`0x04000903`|`cE2lsmPpSR`|`System.Windows.Forms.Button [class:161]`|`1`|`13472766`|`-`|
|`0x04000904`|`BvJlthodrC`|`System.Windows.Forms.Button [class:161]`|`1`|`13472776`|`-`|
|`0x04000905`|`yTYld7eJPp`|`System.Windows.Forms.Button [class:161]`|`1`|`13472786`|`-`|
|`0x04000906`|`RAalJaF0gL`|`System.Windows.Forms.Button [class:161]`|`1`|`13472796`|`-`|
|`0x04000907`|`tiJlrnpjJa`|`System.Windows.Forms.Button [class:161]`|`1`|`13472806`|`-`|
|`0x04000908`|`QGPlGf695w`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13472816`|`-`|
|`0x04000909`|`Iipllelj6s`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13472826`|`-`|
|`0x0400090a`|`aGIlucmDQ8`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13472836`|`-`|
|`0x0400090b`|`pZalHhXRjF`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13472846`|`-`|
|`0x0400090c`|`K2WlpJHRjJ`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13472856`|`-`|
|`0x0400090d`|`Sh2lwUJ6HY`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13472866`|`-`|
|`0x0400090e`|`cISl2bohiS`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13472876`|`-`|
|`0x0400090f`|`Ro5lXKTwGF`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13472886`|`-`|
|`0x04000910`|`g0yliY9weZ`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13472896`|`-`|
|`0x04000911`|`H7mlnR5o4q`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13472906`|`-`|
|`0x04000912`|`WiQl5WP9Xv`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13472916`|`-`|
|`0x04000913`|`LanlSjhWTM`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13472926`|`-`|
|`0x04000914`|`h0kljLu37W`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13472936`|`-`|
|`0x04000915`|`GuAlxqGxfD`|`System.Windows.Forms.Label [class:245]`|`1`|`13472946`|`-`|
|`0x04000916`|`TXvlD77ro6`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13472956`|`-`|
|`0x04000917`|`IyAlBknNoQ`|`System.Windows.Forms.Label [class:245]`|`1`|`13472966`|`-`|
|`0x04000918`|`ah9lOsWyA8`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13472976`|`-`|
|`0x04000919`|`BmplVNTuC0`|`System.Windows.Forms.Label [class:245]`|`1`|`13472986`|`-`|
|`0x0400091a`|`vFXlaDaHWo`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13472996`|`-`|
|`0x0400091b`|`pYilzbgJ8K`|`System.Windows.Forms.Label [class:245]`|`1`|`13473006`|`-`|
|`0x0400091c`|`LxEuUrZIpK`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13473016`|`-`|
|`0x0400091d`|`fO6u8U7dQK`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13473026`|`-`|
|`0x0400091e`|`FL3uEHs2B0`|`System.Windows.Forms.Label [class:245]`|`1`|`13473036`|`-`|
|`0x0400091f`|`qNnu0is9tu`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13473046`|`-`|
|`0x04000920`|`S3ZuPqBDVS`|`System.Windows.Forms.Label [class:245]`|`1`|`13473056`|`-`|
|`0x04000921`|`Satu3DZoha`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13473066`|`-`|
|`0x04000922`|`OBpuq1ye35`|`System.Windows.Forms.Label [class:245]`|`1`|`13473076`|`-`|
|`0x04000923`|`QaMuA6QrEH`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13473086`|`-`|
|`0x04000924`|`V6PuCwiqyU`|`System.Windows.Forms.Label [class:245]`|`1`|`13473096`|`-`|
|`0x04000925`|`WwHum0MqU3`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13473106`|`-`|
|`0x04000926`|`unauQ2h0AV`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13473116`|`-`|
|`0x04000927`|`bBmukKfsGt`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13473126`|`-`|
|`0x04000928`|`glhu4qMHlO`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13473136`|`-`|
|`0x04000929`|`sEAuYvNMBk`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13473146`|`-`|
|`0x0400092a`|`GhHuoSjvLK`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13473156`|`-`|
|`0x0400092b`|`UF9u7iV5FX`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1`|`13473166`|`-`|
|`0x0400092c`|`SPYuFCRtp8`|`System.Windows.Forms.Label [class:245]`|`1`|`13473176`|`-`|
|`0x0400092d`|`iFNuvS3o2K`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13473186`|`-`|
|`0x0400092e`|`CkSu1FJ8aD`|`System.Windows.Forms.Button [class:161]`|`1`|`13473196`|`-`|
|`0x0400092f`|`KGVuMsbyml`|`System.Windows.Forms.Label [class:245]`|`1`|`13473206`|`-`|
|`0x04000930`|`xuju9TaExw`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13473216`|`-`|
|`0x04000931`|`hhJuKYDdfw`|`System.Windows.Forms.Button [class:161]`|`1`|`13473226`|`-`|
|`0x04000932`|`jHyuNjtVJY`|`Kata_Class_Lib_Revit.PreviewCanvas [class:261]`|`1`|`13473236`|`-`|
|`0x04000933`|`dv7ubv4Doo`|`r8`|`1`|`13473246`|`-`|
|`0x04000934`|`NH7uIbmLEk`|`System.Drawing.Point [valuetype:173]`|`1`|`13473256`|`-`|
|`0x04000935`|`KriuyOACnH`|`Kata_Class_Lib_Revit.PreviewCanvas [class:261]`|`1`|`13473266`|`-`|
|`0x04000936`|`P9vuRBj48i`|`r8`|`1`|`13473276`|`-`|
|`0x04000937`|`I5xugbJCse`|`System.Drawing.Point [valuetype:173]`|`1`|`13473286`|`-`|
|`0x04000938`|`r4Nuh9H4Fk`|`Kata_Class_Lib_Revit.PreviewCanvas [class:261]`|`1`|`13473296`|`-`|
|`0x04000939`|`s42uZuSDjM`|`r8`|`1`|`13473306`|`-`|
|`0x0400093a`|`GG0uefKHw4`|`System.Drawing.Point [valuetype:173]`|`1`|`13473316`|`-`|
|`0x0400093b`|`da_hien_form`|`bool`|`6`|`13473326`|`-`|
|`0x0400093c`|`WWguLN6oEV`|`bool`|`1`|`13473336`|`-`|
|`0x0400093d`|`NtxuTg7V0E`|`System.Collections.Generic.List`1 [class:217]<object>`|`1`|`13473346`|`-`|
|`0x0400093e`|`NwjufyQlAh`|`System.Collections.Generic.List`1 [class:217]<object>`|`1`|`13473356`|`-`|
|`0x0400093f`|`GUiuclA6xg`|`System.Collections.Generic.List`1 [class:217]<object>`|`1`|`13473366`|`-`|
|`0x04000940`|`yWeu6vywtp`|`System.Collections.Generic.List`1 [class:217]<InfoVeMatBangMong [class:3528]>`|`1`|`13473376`|`-`|
|`0x04000941`|`GuNuW860Xm`|`System.Collections.Generic.List`1 [class:217]<InfoVeMatBangMong [class:3528]>`|`1`|`13473386`|`-`|
|`0x04000942`|`CSPuswMuEt`|`bool`|`1`|`13473396`|`-`|
|`0x04000943`|`EYRutn0iai`|`bool`|`1`|`13473406`|`-`|
|`0x04000944`|`KynudYlUSa`|`System.Collections.Generic.List`1 [class:217]<MomentMong [class:5312]>`|`1`|`13473416`|`-`|
|`0x04000945`|`sZYuJRKK5d`|`i4`|`1`|`13473426`|`-`|
|`0x04000946`|`onRurHt5Ql`|`bool`|`1`|`13473436`|`-`|
|`0x04000947`|`NoHuGNOrVg`|`System.Drawing.Point [valuetype:173]`|`1`|`13473446`|`-`|
|`0x04000948`|`R5SulonAfm`|`System.Drawing.Point [valuetype:173]`|`1`|`13473456`|`-`|
|`0x04000949`|`F1puuAsGmG`|`bool`|`1`|`13473466`|`-`|
|`0x0400094a`|`Sg5uHGQpqv`|`System.Drawing.Point [valuetype:173]`|`1`|`13473476`|`-`|
|`0x0400094b`|`xlYupq9hvK`|`System.Drawing.Point [valuetype:173]`|`1`|`13473486`|`-`|
|`0x0400094c`|`qCHuwPoFs7`|`bool`|`1`|`13473496`|`-`|
|`0x0400094d`|`MXHu2UDZi3`|`System.Drawing.Point [valuetype:173]`|`1`|`13473506`|`-`|
|`0x0400094e`|`jyLuXwMoU8`|`System.Drawing.Point [valuetype:173]`|`1`|`13473516`|`-`|
|`0x0400094f`|`dofHeOq0AK9RMhpbaqjC`|`Kata_pro64_Cad2025.Form_Ve_Mat_Bang_Mong [class:628]`|`17`|`13473526`|`-`|

### [VM-093] Type `Kata_pro64_Cad2025.TuyChonTinhThepMongTuDong` (Token `0x020000aa`, RowOffset `13370632`, Extends `0x0100000b (System.Object)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 4 | Methods count: 4
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x0400098b`|`DanhSachPhi`|`System.Collections.Generic.List`1 [class:217]<i4>`|`6`|`13474126`|`-`|
|`0x0400098c`|`DanhSachKhoangCach`|`System.Collections.Generic.List`1 [class:217]<i4>`|`6`|`13474136`|`-`|
|`0x0400098d`|`UuTienGanAsNhatTuyetDoi`|`bool`|`6`|`13474146`|`-`|
|`0x0400098e`|`UqBSoLq0QRQRkonQaELS`|`Kata_pro64_Cad2025.TuyChonTinhThepMongTuDong [class:680]`|`17`|`13474156`|`-`|

### [VM-094] Type `Kata_pro64_Cad2025.KetQuaTinhThepMongTuDong` (Token `0x020000ab`, RowOffset `13370650`, Extends `0x0100000b (System.Object)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 3 | Methods count: 4
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x0400098f`|`SoDongDaTinh`|`i4`|`6`|`13474166`|`-`|
|`0x04000990`|`DanhSachKhongDat`|`System.Collections.Generic.List`1 [class:217]<string>`|`6`|`13474176`|`-`|
|`0x04000991`|`nIKEt8q0Y5x2wMY3PnQj`|`Kata_pro64_Cad2025.KetQuaTinhThepMongTuDong [class:684]`|`17`|`13474186`|`-`|

### [VM-095] Type `Kata_pro64_Cad2025.PhuongAnThepMong` (Token `0x020000ac`, RowOffset `13370668`, Extends `0x0100000b (System.Object)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 9 | Methods count: 5
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04000992`|`Phi1`|`i4`|`6`|`13474196`|`-`|
|`0x04000993`|`KhoangCach1`|`i4`|`6`|`13474206`|`-`|
|`0x04000994`|`Phi2`|`i4`|`6`|`13474216`|`-`|
|`0x04000995`|`KhoangCach2`|`i4`|`6`|`13474226`|`-`|
|`0x04000996`|`CoLop2`|`bool`|`6`|`13474236`|`-`|
|`0x04000997`|`AsChon`|`r8`|`6`|`13474246`|`-`|
|`0x04000998`|`AsDu`|`r8`|`6`|`13474256`|`-`|
|`0x04000999`|`AsLop2`|`r8`|`6`|`13474266`|`-`|
|`0x0400099a`|`SCOddhq0FQxHg9wmpl0I`|`Kata_pro64_Cad2025.PhuongAnThepMong [class:688]`|`19`|`13474276`|`-`|

### [VM-096] Type `Kata_pro64_Cad2025.Form_Chon_Thep_Mong_Tu_Dong` (Token `0x020000ad`, RowOffset `13370686`, Extends `0x0100000f (System.Windows.Forms.Form)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 12 | Methods count: 31
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x0400099b`|`rCJuVn9YhH`|`System.Collections.Generic.List`1 [class:217]<i4>`|`17`|`13474286`|`-`|
|`0x0400099c`|`XB9uavWqWA`|`System.Collections.Generic.List`1 [class:217]<i4>`|`17`|`13474296`|`-`|
|`0x0400099d`|`PwruzsE0FX`|`System.Collections.Generic.List`1 [class:217]<i4>`|`17`|`13474306`|`-`|
|`0x0400099e`|`HYJHUIsgUA`|`System.Collections.Generic.List`1 [class:217]<i4>`|`17`|`13474316`|`-`|
|`0x0400099f`|`e5ZH8K1Tlq`|`bool`|`17`|`13474326`|`-`|
|`0x040009a0`|`uE8HEwusTx`|`System.Windows.Forms.CheckedListBox [class:1049]`|`1`|`13474336`|`-`|
|`0x040009a1`|`hGUH0Vq6vN`|`System.Windows.Forms.CheckedListBox [class:1049]`|`1`|`13474346`|`-`|
|`0x040009a2`|`vtAHPEyGtl`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13474356`|`-`|
|`0x040009a3`|`Nm1H3Bm7HE`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13474366`|`-`|
|`0x040009a4`|`yrsHqnEpZK`|`System.Windows.Forms.CheckBox [class:257]`|`1`|`13474376`|`-`|
|`0x040009a5`|`Nm3HAY8Zxf`|`Kata_pro64_Cad2025.TuyChonTinhThepMongTuDong [class:680]`|`1`|`13474386`|`-`|
|`0x040009a6`|`nQDIktq0MpEZHYwhYHbi`|`Kata_pro64_Cad2025.Form_Chon_Thep_Mong_Tu_Dong [class:692]`|`17`|`13474396`|`-`|

### [VM-097] Type `Kata_pro64_Cad2025.view_mong_don` (Token `0x020000cf`, RowOffset `13371298`, Extends `0x0100000f (System.Windows.Forms.Form)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 56 | Methods count: 114
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04000c68`|`PT4OXyVc8p`|`System.ComponentModel.IContainer [class:157]`|`1`|`13481456`|`-`|
|`0x04000c69`|`GEJOiNqXLm`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13481466`|`-`|
|`0x04000c6a`|`aKQOn9J0oQ`|`System.Windows.Forms.BindingSource [class:237]`|`1`|`13481476`|`-`|
|`0x04000c6b`|`llSO5BDtDn`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481486`|`-`|
|`0x04000c6c`|`Ka5OScKKSD`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481496`|`-`|
|`0x04000c6d`|`YcROj0HfXV`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481506`|`-`|
|`0x04000c6e`|`ROMOxJTT8D`|`System.Windows.Forms.Button [class:161]`|`1`|`13481516`|`-`|
|`0x04000c6f`|`bbyODb6Dob`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481526`|`-`|
|`0x04000c70`|`ysIOB5M9mh`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481536`|`-`|
|`0x04000c71`|`nQYOOoCmMw`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481546`|`-`|
|`0x04000c72`|`BAVOVh1Dvy`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481556`|`-`|
|`0x04000c73`|`TpDOaukemr`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481566`|`-`|
|`0x04000c74`|`mpNOzhR4aS`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481576`|`-`|
|`0x04000c75`|`EWOVU8Y756`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481586`|`-`|
|`0x04000c76`|`EXoV8bxu3D`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481596`|`-`|
|`0x04000c77`|`M2AVE5PZYn`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481606`|`-`|
|`0x04000c78`|`wGUV02ViqY`|`System.Windows.Forms.Label [class:245]`|`1`|`13481616`|`-`|
|`0x04000c79`|`p4lVPdYmpd`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481626`|`-`|
|`0x04000c7a`|`CmJV3cY73d`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481636`|`-`|
|`0x04000c7b`|`V3qVqO52qe`|`System.Windows.Forms.Button [class:161]`|`1`|`13481646`|`-`|
|`0x04000c7c`|`JNgVAJMmBt`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481656`|`-`|
|`0x04000c7d`|`ESyVCSkKbk`|`System.Windows.Forms.Label [class:245]`|`1`|`13481666`|`-`|
|`0x04000c7e`|`MW8VmEANRS`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13481676`|`-`|
|`0x04000c7f`|`YklVQyXRTS`|`System.Windows.Forms.Label [class:245]`|`1`|`13481686`|`-`|
|`0x04000c80`|`NppVkEgslR`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481696`|`-`|
|`0x04000c81`|`OjcV47yMB3`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481706`|`-`|
|`0x04000c82`|`dhNVYWghqG`|`System.Windows.Forms.Label [class:245]`|`1`|`13481716`|`-`|
|`0x04000c83`|`O7GVoJRX3D`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481726`|`-`|
|`0x04000c84`|`aTXV7Rqg76`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481736`|`-`|
|`0x04000c85`|`mYeVFSWa8H`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481746`|`-`|
|`0x04000c86`|`fFaVv1xu1x`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481756`|`-`|
|`0x04000c87`|`X1uV1a8HSg`|`System.Windows.Forms.Label [class:245]`|`1`|`13481766`|`-`|
|`0x04000c88`|`W28VMJ6YCv`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13481776`|`-`|
|`0x04000c89`|`caKV98GMrE`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481786`|`-`|
|`0x04000c8a`|`P1SVKvEujr`|`System.Windows.Forms.Label [class:245]`|`1`|`13481796`|`-`|
|`0x04000c8b`|`WnoVNjUpxx`|`System.Windows.Forms.Label [class:245]`|`1`|`13481806`|`-`|
|`0x04000c8c`|`Bd4VbdIrN1`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481816`|`-`|
|`0x04000c8d`|`yqPVItcqO1`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481826`|`-`|
|`0x04000c8e`|`XMHVyIy2jw`|`System.Windows.Forms.Label [class:245]`|`1`|`13481836`|`-`|
|`0x04000c8f`|`WqoVRm1LAa`|`System.Windows.Forms.Label [class:245]`|`1`|`13481846`|`-`|
|`0x04000c90`|`Yd6VgphAQG`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481856`|`-`|
|`0x04000c91`|`QqUVheGE87`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481866`|`-`|
|`0x04000c92`|`VKrVZ1p7UG`|`System.Windows.Forms.Label [class:245]`|`1`|`13481876`|`-`|
|`0x04000c93`|`bWYVem2SkY`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481886`|`-`|
|`0x04000c94`|`BbhVLOC3gp`|`System.Windows.Forms.Label [class:245]`|`1`|`13481896`|`-`|
|`0x04000c95`|`d1hVTy2xaq`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481906`|`-`|
|`0x04000c96`|`Rr2Vf6YMws`|`System.Windows.Forms.Label [class:245]`|`1`|`13481916`|`-`|
|`0x04000c97`|`E7JVcq1U8R`|`System.Windows.Forms.Button [class:161]`|`1`|`13481926`|`-`|
|`0x04000c98`|`yCYV68KZpn`|`System.Windows.Forms.Button [class:161]`|`1`|`13481936`|`-`|
|`0x04000c99`|`g5GVWq2f0v`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13481946`|`-`|
|`0x04000c9a`|`nttVslGyug`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481956`|`-`|
|`0x04000c9b`|`O9AVtngAfW`|`System.Windows.Forms.Label [class:245]`|`1`|`13481966`|`-`|
|`0x04000c9c`|`DOAVdjpMcM`|`System.Windows.Forms.CheckBox [class:257]`|`1`|`13481976`|`-`|
|`0x04000c9d`|`PeXVJly488`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481986`|`-`|
|`0x04000c9e`|`SO8VrkN1pM`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13481996`|`-`|
|`0x04000c9f`|`at1CDtqPuAtDSWbZkkxd`|`Kata_pro64_Cad2025.view_mong_don [class:828]`|`19`|`13482006`|`-`|

### [VM-098] Type `Kata_pro64_Cad2025.view_mong_don_new` (Token `0x020000d0`, RowOffset `13371316`, Extends `0x0100000f (System.Windows.Forms.Form)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 89 | Methods count: 209
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04000ca0`|`Y3nay3LSL7`|`System.ComponentModel.IContainer [class:157]`|`1`|`13482016`|`-`|
|`0x04000ca1`|`JUCaRyFkRP`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13482026`|`-`|
|`0x04000ca2`|`Ay2agqpfTl`|`System.Windows.Forms.BindingSource [class:237]`|`1`|`13482036`|`-`|
|`0x04000ca3`|`Jm0ahmRM58`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482046`|`-`|
|`0x04000ca4`|`fWraZGfqRE`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482056`|`-`|
|`0x04000ca5`|`RAFaenYiXi`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482066`|`-`|
|`0x04000ca6`|`BoeaLHS6Rv`|`System.Windows.Forms.Button [class:161]`|`1`|`13482076`|`-`|
|`0x04000ca7`|`gHQaT7CaGO`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482086`|`-`|
|`0x04000ca8`|`q5yafOpcI5`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482096`|`-`|
|`0x04000ca9`|`fUkacPucoa`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482106`|`-`|
|`0x04000caa`|`gHSa6qGYbq`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482116`|`-`|
|`0x04000cab`|`q94aWtjj0H`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482126`|`-`|
|`0x04000cac`|`H0qasDdPEH`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482136`|`-`|
|`0x04000cad`|`WclatIUVmV`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482146`|`-`|
|`0x04000cae`|`K61adG80gR`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482156`|`-`|
|`0x04000caf`|`FrRaJ17yS6`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482166`|`-`|
|`0x04000cb0`|`Yk5arcj8sg`|`System.Windows.Forms.Label [class:245]`|`1`|`13482176`|`-`|
|`0x04000cb1`|`XnoaGYso4C`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482186`|`-`|
|`0x04000cb2`|`YhIalqLSdB`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482196`|`-`|
|`0x04000cb3`|`YBKaudWmKO`|`System.Windows.Forms.Button [class:161]`|`1`|`13482206`|`-`|
|`0x04000cb4`|`Pq1aHtS3aa`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482216`|`-`|
|`0x04000cb5`|`OcPap8nAhP`|`System.Windows.Forms.Label [class:245]`|`1`|`13482226`|`-`|
|`0x04000cb6`|`PSSawS8DLT`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13482236`|`-`|
|`0x04000cb7`|`hk9a2OpLEy`|`System.Windows.Forms.Label [class:245]`|`1`|`13482246`|`-`|
|`0x04000cb8`|`ifvaXwJ51m`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482256`|`-`|
|`0x04000cb9`|`MGPaijqhUo`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482266`|`-`|
|`0x04000cba`|`q8manXWp4V`|`System.Windows.Forms.Label [class:245]`|`1`|`13482276`|`-`|
|`0x04000cbb`|`lPHa5bDiYW`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482286`|`-`|
|`0x04000cbc`|`laoaSrnJSY`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482296`|`-`|
|`0x04000cbd`|`plOajjROmB`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482306`|`-`|
|`0x04000cbe`|`xk7axxDhp5`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482316`|`-`|
|`0x04000cbf`|`PQnaDSWqDU`|`System.Windows.Forms.Label [class:245]`|`1`|`13482326`|`-`|
|`0x04000cc0`|`AahaBRUsKV`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13482336`|`-`|
|`0x04000cc1`|`TcpaOGxH4t`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482346`|`-`|
|`0x04000cc2`|`H5OaV8T2hx`|`System.Windows.Forms.Label [class:245]`|`1`|`13482356`|`-`|
|`0x04000cc3`|`SbDaak50UT`|`System.Windows.Forms.Label [class:245]`|`1`|`13482366`|`-`|
|`0x04000cc4`|`vYqazhIP8F`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482376`|`-`|
|`0x04000cc5`|`KpEzUuuwvJ`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482386`|`-`|
|`0x04000cc6`|`SA3z89EATQ`|`System.Windows.Forms.Label [class:245]`|`1`|`13482396`|`-`|
|`0x04000cc7`|`kSgzE5KYPN`|`System.Windows.Forms.Label [class:245]`|`1`|`13482406`|`-`|
|`0x04000cc8`|`lSBz0r80SK`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482416`|`-`|
|`0x04000cc9`|`Dt3zPX70Mr`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482426`|`-`|
|`0x04000cca`|`Hnxz3xDgZk`|`System.Windows.Forms.Label [class:245]`|`1`|`13482436`|`-`|
|`0x04000ccb`|`KIGzqm2hT1`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482446`|`-`|
|`0x04000ccc`|`pEhzAnnpAE`|`System.Windows.Forms.Label [class:245]`|`1`|`13482456`|`-`|
|`0x04000ccd`|`FVazCR3o5j`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482466`|`-`|
|`0x04000cce`|`XjozmMqDG0`|`System.Windows.Forms.Label [class:245]`|`1`|`13482476`|`-`|
|`0x04000ccf`|`FilzQW0H6x`|`System.Windows.Forms.Button [class:161]`|`1`|`13482486`|`-`|
|`0x04000cd0`|`Xjmzketvns`|`System.Windows.Forms.Button [class:161]`|`1`|`13482496`|`-`|
|`0x04000cd1`|`S0oz4IrfuA`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13482506`|`-`|
|`0x04000cd2`|`lAOzYWx6CG`|`System.Windows.Forms.CheckBox [class:257]`|`1`|`13482516`|`-`|
|`0x04000cd3`|`lbQzoK9Ud3`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482526`|`-`|
|`0x04000cd4`|`wgSz7VO0kB`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482536`|`-`|
|`0x04000cd5`|`vRxzF5DaVy`|`System.Windows.Forms.CheckBox [class:257]`|`1`|`13482546`|`-`|
|`0x04000cd6`|`X80zvapbxd`|`System.Windows.Forms.CheckBox [class:257]`|`1`|`13482556`|`-`|
|`0x04000cd7`|`Smqz1tFFUG`|`System.Windows.Forms.RadioButton [class:253]`|`1`|`13482566`|`-`|
|`0x04000cd8`|`ErMzM9uhn1`|`System.Windows.Forms.RadioButton [class:253]`|`1`|`13482576`|`-`|
|`0x04000cd9`|`DU4z9FG7F7`|`System.Windows.Forms.Label [class:245]`|`1`|`13482586`|`-`|
|`0x04000cda`|`OP9zKjlQuu`|`System.Windows.Forms.Panel [class:373]`|`1`|`13482596`|`-`|
|`0x04000cdb`|`O7czNZJ5RI`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482606`|`-`|
|`0x04000cdc`|`M5pzb2Anfk`|`System.Windows.Forms.Label [class:245]`|`1`|`13482616`|`-`|
|`0x04000cdd`|`GNfzIDg9D7`|`System.Windows.Forms.Button [class:161]`|`1`|`13482626`|`-`|
|`0x04000cde`|`oCczy9QQyx`|`System.Windows.Forms.Label [class:245]`|`1`|`13482636`|`-`|
|`0x04000cdf`|`dTgzRLQgiZ`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482646`|`-`|
|`0x04000ce0`|`AWazg8nNJp`|`System.Windows.Forms.Label [class:245]`|`1`|`13482656`|`-`|
|`0x04000ce1`|`pZbzhMOVwV`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482666`|`-`|
|`0x04000ce2`|`TMYzZentMt`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482676`|`-`|
|`0x04000ce3`|`o5Czetokik`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482686`|`-`|
|`0x04000ce4`|`fpCzLmXiem`|`System.Windows.Forms.ComboBox [class:377]`|`1`|`13482696`|`-`|
|`0x04000ce5`|`dl0zTkd1Fc`|`System.Windows.Forms.Label [class:245]`|`1`|`13482706`|`-`|
|`0x04000ce6`|`GamzfLZVN1`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482716`|`-`|
|`0x04000ce7`|`OiNzceveVj`|`System.Windows.Forms.Label [class:245]`|`1`|`13482726`|`-`|
|`0x04000ce8`|`DZwz6fOeX7`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482736`|`-`|
|`0x04000ce9`|`OprzWC48MY`|`System.Windows.Forms.Label [class:245]`|`1`|`13482746`|`-`|
|`0x04000cea`|`MUrzsWXb6C`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482756`|`-`|
|`0x04000ceb`|`YMMztdjsxO`|`System.Windows.Forms.Label [class:245]`|`1`|`13482766`|`-`|
|`0x04000cec`|`RLqzdElrN0`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13482776`|`-`|
|`0x04000ced`|`naozJHc0ft`|`System.Windows.Forms.Label [class:245]`|`1`|`13482786`|`-`|
|`0x04000cee`|`tOnzrPDFSt`|`Kata_Class_Lib_Revit.PreviewCanvas [class:261]`|`1`|`13482796`|`-`|
|`0x04000cef`|`BCRzGYt815`|`r8`|`1`|`13482806`|`-`|
|`0x04000cf0`|`klHzlQ99Au`|`bool`|`1`|`13482816`|`-`|
|`0x04000cf1`|`hiHzuiwDSl`|`System.Drawing.Point [valuetype:173]`|`1`|`13482826`|`-`|
|`0x04000cf2`|`U5fzHITFt7`|`string`|`1`|`13482836`|`-`|
|`0x04000cf3`|`vmvzpkx5qc`|`string`|`1`|`13482846`|`-`|
|`0x04000cf4`|`Y9LzwCeZC6`|`string`|`1`|`13482856`|`-`|
|`0x04000cf5`|`FGJz2gao8C`|`bool`|`1`|`13482866`|`-`|
|`0x04000cf6`|`fKozXhBk5t`|`System.Drawing.Point [valuetype:173]`|`1`|`13482876`|`-`|
|`0x04000cf7`|`xoIziRNqiy`|`System.Drawing.Point [valuetype:173]`|`1`|`13482886`|`-`|
|`0x04000cf8`|`AOKu8dqPwJNrvyeYdoQH`|`Kata_pro64_Cad2025.view_mong_don_new [class:832]`|`17`|`13482896`|`-`|

### [VM-099] Type `Kata_pro64_Cad2025.Acad` (Token `0x020000d5`, RowOffset `13371406`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 1 | Methods count: 5
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04000e1c`|`ow3BNwqPVkoUVZcSps3A`|`Kata_pro64_Cad2025.Acad [class:852]`|`17`|`13485816`|`-`|

### [VM-100] Type `Kata_pro64_Cad2025.Lenh_thep` (Token `0x020000d6`, RowOffset `13371424`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 25 | Methods count: 166
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04000e1d`|`info_rebar`|`Kata_pro64_Cad2025.Info_rebar [class:716]`|`22`|`13485826`|`-`|
|`0x04000e1e`|`view_thang_new`|`Kata_pro64_Cad2025.view_thang_new [class:840]`|`22`|`13485836`|`-`|
|`0x04000e1f`|`view_mong_don`|`Kata_pro64_Cad2025.view_mong_don_new [class:832]`|`22`|`13485846`|`-`|
|`0x04000e20`|`view_mat_cat_dam`|`Kata_pro64_Cad2025.view_mat_cat_dam [class:820]`|`22`|`13485856`|`-`|
|`0x04000e21`|`view_san`|`Kata_pro64_Cad2025.view_san [class:836]`|`22`|`13485866`|`-`|
|`0x04000e22`|`input_anchor`|`Kata_pro64_Cad2025.Input_anchor [class:724]`|`22`|`13485876`|`-`|
|`0x04000e23`|`QS_form`|`Kata_pro64_Cad2025.QS_form [class:748]`|`22`|`13485886`|`-`|
|`0x04000e24`|`input_slab`|`Kata_pro64_Cad2025.Input_slab [class:732]`|`22`|`13485896`|`-`|
|`0x04000e25`|`info_slab`|`Kata_pro64_Cad2025.Info_slab [class:5452]`|`22`|`13485906`|`-`|
|`0x04000e26`|`User_kho_thep`|`Kata_pro64_Cad2025.User_kho_thep [class:812]`|`22`|`13485916`|`-`|
|`0x04000e27`|`Danh_ten`|`Kata_pro64_Cad2025.Danh_ten [class:352]`|`22`|`13485926`|`-`|
|`0x04000e28`|`tk_ten`|`Kata_pro64_Cad2025.TK_ten [class:808]`|`22`|`13485936`|`-`|
|`0x04000e29`|`lanh_to`|`Kata_pro64_Cad2025.Lanh_to [class:740]`|`22`|`13485946`|`-`|
|`0x04000e2a`|`QS_ve_hm_tru`|`Kata_pro64_Cad2025.QS_ve_hm_tru [class:764]`|`22`|`13485956`|`-`|
|`0x04000e2b`|`setting_atlas`|`Kata_pro64_Cad2025.Setting_atlas [class:4980]`|`22`|`13485966`|`-`|
|`0x04000e2c`|`Main_rebar_Column`|`Kata_pro64_Cad2025.Main_rebar_Column [class:5456]`|`22`|`13485976`|`-`|
|`0x04000e2d`|`edit_moc_dai`|`Kata_pro64_Cad2025.Edit_moc_dai [class:356]`|`22`|`13485986`|`-`|
|`0x04000e2e`|`Grid_plan`|`Kata_pro64_Cad2025.Grid_plan [class:712]`|`22`|`13485996`|`-`|
|`0x04000e2f`|`Grid_data`|`Kata_pro64_Cad2025.Grid_Data [class:708]`|`22`|`13486006`|`-`|
|`0x04000e30`|`Revit_rai_dam`|`Kata_pro64_Cad2025.Rai_dam [class:5460]`|`22`|`13486016`|`-`|
|`0x04000e31`|`Revit_rai_cot`|`Kata_pro64_Cad2025.Rai_cot [class:5464]`|`22`|`13486026`|`-`|
|`0x04000e32`|`Tim_thep`|`Kata_pro64_Cad2025.Tim_thep [class:792]`|`22`|`13486036`|`-`|
|`0x04000e33`|`FormGanTaiTrong`|`Kata_pro64_Cad2025.FormGanTaiTrong [class:5288]`|`22`|`13486046`|`-`|
|`0x04000e34`|`qnw8qbNpY5j`|`bool`|`1`|`13486056`|`-`|
|`0x04000e35`|`L5ytCTq3UmE4QGxWUQPw`|`Kata_pro64_Cad2025.Lenh_thep [class:856]`|`17`|`13486066`|`-`|

### [VM-101] Type `Kata_pro64_Cad2025.AI_BeamFoundationReferencePdfBuilder` (Token `0x02000131`, RowOffset `13373062`, Extends `0x0100000b (System.Object)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 12 | Methods count: 24
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04000fdc`|`ReferencePdfFileName`|`string`|`32854`|`13490296`|`"AI_RebuildBeam_Model_Reference.pdf"`|
|`0x04000fdd`|`i3b8QSQwB0w`|`iTextSharp.text.BaseColor [class:1493]`|`49`|`13490306`|`-`|
|`0x04000fde`|`OZc8QjXVSLI`|`iTextSharp.text.BaseColor [class:1493]`|`49`|`13490316`|`-`|
|`0x04000fdf`|`TCT8QxXlfZa`|`iTextSharp.text.BaseColor [class:1493]`|`49`|`13490326`|`-`|
|`0x04000fe0`|`zXE8QDHI2bC`|`iTextSharp.text.BaseColor [class:1493]`|`49`|`13490336`|`-`|
|`0x04000fe1`|`e0s8QBvIDX5`|`iTextSharp.text.BaseColor [class:1493]`|`49`|`13490346`|`-`|
|`0x04000fe2`|`r0A8QOORPGT`|`iTextSharp.text.BaseColor [class:1493]`|`49`|`13490356`|`-`|
|`0x04000fe3`|`FOy8QVuj78u`|`iTextSharp.text.BaseColor [class:1493]`|`49`|`13490366`|`-`|
|`0x04000fe4`|`Y978QakUWJu`|`iTextSharp.text.BaseColor [class:1493]`|`49`|`13490376`|`-`|
|`0x04000fe5`|`NYC8QzPNDxZ`|`iTextSharp.text.BaseColor [class:1493]`|`49`|`13490386`|`-`|
|`0x04000fe6`|`rgg8kUwCZ4r`|`System.Globalization.CultureInfo [class:1497]`|`49`|`13490396`|`-`|
|`0x04000fe7`|`moYTyuqqhZ0ZlUmIILSx`|`Kata_pro64_Cad2025.AI_BeamFoundationReferencePdfBuilder [class:1220]`|`19`|`13490406`|`-`|

### [VM-102] Type `Kata_pro64_Cad2025.AI_DrawIsolatedFoundation_Tool` (Token `0x020001be`, RowOffset `13375600`, Extends `0x02000253 (Kata_pro64_Cad2025.AIToolBase)`, Evidence `ev_3bdf54e817a9b5840d6618afe7ca3edeaabd4e8c3021dcb361d608566084bf9d`)
- Fields count: 1 | Methods count: 17
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x040013cc`|`g1dKfSqC4x111N63Sd1F`|`Kata_pro64_Cad2025.AI_DrawIsolatedFoundation_Tool [class:1784]`|`19`|`13500376`|`-`|

### [VM-103] Type `Kata_pro64_Cad2025.kata1` (Token `0x0200027b`, RowOffset `13379002`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 5 | Methods count: 56
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x0400173c`|`DeferCadEntityUpdates`|`bool`|`22`|`13509176`|`-`|
|`0x0400173d`|`bloref_TD`|`object`|`22`|`13509186`|`-`|
|`0x0400173e`|`s4986AaQvK5`|`Kata_Class_Lib_Revit.diem [class:85][]`|`17`|`13509196`|`-`|
|`0x0400173f`|`UrQ86CbuXsZ`|`System.Collections.Generic.List`1 [class:217]<object>`|`17`|`13509206`|`-`|
|`0x04001740`|`sRZSOQqQo8mtxwXlgShS`|`Kata_pro64_Cad2025.kata1 [class:2540]`|`17`|`13509216`|`-`|

### [VM-104] Type `Kata_pro64_Cad2025.Mong_coc` (Token `0x020002ec`, RowOffset `13381036`, Extends `0x0100000b (System.Object)`, Evidence `ev_3bdf54e817a9b5840d6618afe7ca3edeaabd4e8c3021dcb361d608566084bf9d`)
- Fields count: 46 | Methods count: 115
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001ac7`|`info_mong_coc`|`InfoMongCoc [class:2996]`|`22`|`13518246`|`-`|
|`0x04001ac8`|`formMongCoc`|`Kata_pro64_Cad2025.Form_mong_coc [class:460]`|`22`|`13518256`|`-`|
|`0x04001ac9`|`formThepThemDuoi`|`Kata_pro64_Cad2025.Form_Lop_Thep_Them [class:440]`|`22`|`13518266`|`-`|
|`0x04001aca`|`formThepThemNgang`|`Kata_pro64_Cad2025.Form_Lop_Thep_Them_Ngang [class:444]`|`22`|`13518276`|`-`|
|`0x04001acb`|`formThepThemTren`|`Kata_pro64_Cad2025.Form_Lop_Thep_Them_Tren [class:448]`|`22`|`13518286`|`-`|
|`0x04001acc`|`listlistL`|`System.Collections.Generic.List`1 [class:217]<System.Collections.Generic.List`1 [class:217]<r8>>`|`22`|`13518296`|`-`|
|`0x04001acd`|`Ltext`|`string`|`22`|`13518306`|`-`|
|`0x04001ace`|`hThepCho`|`r8`|`22`|`13518316`|`-`|
|`0x04001acf`|`spacing`|`r8`|`22`|`13518326`|`-`|
|`0x04001ad0`|`length`|`r8`|`22`|`13518336`|`-`|
|`0x04001ad1`|`so_thanh`|`i4`|`22`|`13518346`|`-`|
|`0x04001ad2`|`kc2ben`|`r8`|`22`|`13518356`|`-`|
|`0x04001ad3`|`LOAIDATAMONGCOC`|`string`|`22`|`13518366`|`-`|
|`0x04001ad4`|`phuongB`|`bool`|`22`|`13518376`|`-`|
|`0x04001ad5`|`VectoTheoPhuong`|`Kata_Class_Lib_Revit.diem [class:85]`|`22`|`13518386`|`-`|
|`0x04001ad6`|`tlForm`|`r8`|`22`|`13518396`|`-`|
|`0x04001ad7`|`goc_duoi`|`System.Drawing.Point [valuetype:173]`|`22`|`13518406`|`-`|
|`0x04001ad8`|`goc_tren`|`System.Drawing.Point [valuetype:173]`|`22`|`13518416`|`-`|
|`0x04001ad9`|`listLocation`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`22`|`13518426`|`-`|
|`0x04001ada`|`QGS8lBI6wrt`|`r8[]`|`17`|`13518436`|`-`|
|`0x04001adb`|`uml8lOyw60m`|`Kata_Class_Lib_Revit.diem [class:85]`|`17`|`13518446`|`-`|
|`0x04001adc`|`aDU8lV0hgnR`|`Kata_Class_Lib_Revit.info_thep [class:793][]`|`17`|`13518456`|`-`|
|`0x04001add`|`zKU8laCANAn`|`Kata_Class_Lib_Revit.diem [class:85]`|`17`|`13518466`|`-`|
|`0x04001ade`|`QNF8lzTnKfn`|`Kata_Class_Lib_Revit.diem [class:85]`|`17`|`13518476`|`-`|
|`0x04001adf`|`JXu8uUNLeK7`|`Kata_Class_Lib_Revit.diem [class:85]`|`17`|`13518486`|`-`|
|`0x04001ae0`|`g8p8u8gcvOq`|`Kata_Class_Lib_Revit.diem [class:85]`|`17`|`13518496`|`-`|
|`0x04001ae1`|`JPy8uEHlhpr`|`Kata_Class_Lib_Revit.diem [class:85]`|`17`|`13518506`|`-`|
|`0x04001ae2`|`TWa8u0bdxcT`|`Kata_Class_Lib_Revit.diem [class:85]`|`17`|`13518516`|`-`|
|`0x04001ae3`|`cY98uPaGj7u`|`Kata_Class_Lib_Revit.diem [class:85]`|`17`|`13518526`|`-`|
|`0x04001ae4`|`RnR8u38WecU`|`Kata_Class_Lib_Revit.diem [class:85]`|`17`|`13518536`|`-`|
|`0x04001ae5`|`V3i8uqFrSsB`|`r8`|`17`|`13518546`|`-`|
|`0x04001ae6`|`RcP8uARX5je`|`r8`|`17`|`13518556`|`-`|
|`0x04001ae7`|`Tw38uCNxxRU`|`r8`|`17`|`13518566`|`-`|
|`0x04001ae8`|`EZF8ummxJKx`|`r8`|`17`|`13518576`|`-`|
|`0x04001ae9`|`q4h8uQpQT7s`|`System.Collections.Generic.List`1 [class:217]<MSoQZG0EHE71OrFu3tlQ [class:2824]>`|`17`|`13518586`|`-`|
|`0x04001aea`|`Pyv8ukisZq0`|`System.Collections.Generic.List`1 [class:217]<MSoQZG0EHE71OrFu3tlQ [class:2824]>`|`17`|`13518596`|`-`|
|`0x04001aeb`|`KAh8u4G0NNx`|`System.Collections.Generic.List`1 [class:217]<MSoQZG0EHE71OrFu3tlQ [class:2824]>`|`17`|`13518606`|`-`|
|`0x04001aec`|`znx8uYoCgew`|`System.Collections.Generic.List`1 [class:217]<System.Collections.Generic.List`1 [class:217]<MSoQZG0EHE71OrFu3tlQ [class:2824]>>`|`17`|`13518616`|`-`|
|`0x04001aed`|`h6X8uov7C4g`|`System.Collections.Generic.List`1 [class:217]<object>`|`17`|`13518626`|`-`|
|`0x04001aee`|`PpS8u72gPAA`|`r8`|`17`|`13518636`|`-`|
|`0x04001aef`|`pIA8uFDuXjF`|`Kata_Class_Lib_Revit.diem [class:85]`|`17`|`13518646`|`-`|
|`0x04001af0`|`GqC8uvvT445`|`System.Collections.Generic.List`1 [class:217]<System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>>`|`17`|`13518656`|`-`|
|`0x04001af1`|`T4L8u1bcu2F`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`17`|`13518666`|`-`|
|`0x04001af2`|`TNe8uM1iM2G`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`17`|`13518676`|`-`|
|`0x04001af3`|`o5F8u95PnIO`|`i4`|`17`|`13518686`|`-`|
|`0x04001af4`|`VYdd6XqQi5ryT2BI3B6w`|`Kata_pro64_Cad2025.Mong_coc [class:2992]`|`19`|`13518696`|`-`|

### [VM-105] Type `InfoMongCoc` (Token `0x020002ed`, RowOffset `13381054`, Extends `0x0100000b (System.Object)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 51 | Methods count: 7
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001af5`|`daiRauCoc`|`r8`|`6`|`13518706`|`-`|
|`0x04001af6`|`beMocCauTao`|`r8`|`6`|`13518716`|`-`|
|`0x04001af7`|`mocDaiDung`|`r8`|`6`|`13518726`|`-`|
|`0x04001af8`|`co_cot`|`bool`|`6`|`13518736`|`-`|
|`0x04001af9`|`point_mc`|`Kata_Class_Lib_Revit.diem [class:85]`|`6`|`13518746`|`-`|
|`0x04001afa`|`point_goc`|`Kata_Class_Lib_Revit.diem [class:85]`|`6`|`13518756`|`-`|
|`0x04001afb`|`gocVeMBDuoi`|`Kata_Class_Lib_Revit.diem [class:85]`|`6`|`13518766`|`-`|
|`0x04001afc`|`d1`|`r8`|`6`|`13518776`|`-`|
|`0x04001afd`|`d2`|`r8`|`6`|`13518786`|`-`|
|`0x04001afe`|`d3`|`r8`|`6`|`13518796`|`-`|
|`0x04001aff`|`e1`|`r8`|`6`|`13518806`|`-`|
|`0x04001b00`|`e2`|`r8`|`6`|`13518816`|`-`|
|`0x04001b01`|`e3`|`r8`|`6`|`13518826`|`-`|
|`0x04001b02`|`l_am`|`r8`|`6`|`13518836`|`-`|
|`0x04001b03`|`l_duong`|`r8`|`6`|`13518846`|`-`|
|`0x04001b04`|`l`|`r8`|`6`|`13518856`|`-`|
|`0x04001b05`|`b_am`|`r8`|`6`|`13518866`|`-`|
|`0x04001b06`|`b_duong`|`r8`|`6`|`13518876`|`-`|
|`0x04001b07`|`b`|`r8`|`6`|`13518886`|`-`|
|`0x04001b08`|`b_vecto`|`Kata_Class_Lib_Revit.diem [class:85]`|`6`|`13518896`|`-`|
|`0x04001b09`|`l_vecto`|`Kata_Class_Lib_Revit.diem [class:85]`|`6`|`13518906`|`-`|
|`0x04001b0a`|`h_vecto`|`Kata_Class_Lib_Revit.diem [class:85]`|`6`|`13518916`|`-`|
|`0x04001b0b`|`dln`|`InfoUserMongCoc [class:3012]`|`6`|`13518926`|`-`|
|`0x04001b0c`|`dl_cot`|`Dlcot [class:3024]`|`6`|`13518936`|`-`|
|`0x04001b0d`|`list_dl_cot`|`System.Collections.Generic.List`1 [class:217]<Dlcot [class:3024]>`|`6`|`13518946`|`-`|
|`0x04001b0e`|`h1`|`r8`|`6`|`13518956`|`-`|
|`0x04001b0f`|`h2`|`r8`|`6`|`13518966`|`-`|
|`0x04001b10`|`h3`|`r8`|`6`|`13518976`|`-`|
|`0x04001b11`|`h4`|`r8`|`6`|`13518986`|`-`|
|`0x04001b12`|`hsanTrai`|`r8`|`6`|`13518996`|`-`|
|`0x04001b13`|`hsanPhai`|`r8`|`6`|`13519006`|`-`|
|`0x04001b14`|`cao_do`|`r8`|`6`|`13519016`|`-`|
|`0x04001b15`|`list_point_duoi_mong`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6`|`13519026`|`-`|
|`0x04001b16`|`list_point_duoi_cot`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6`|`13519036`|`-`|
|`0x04001b17`|`ll_point_cot`|`System.Collections.Generic.List`1 [class:217]<System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>>`|`6`|`13519046`|`-`|
|`0x04001b18`|`list_pcot_tron`|`System.Collections.Generic.List`1 [class:217]<KataCircle3D [class:3004]>`|`6`|`13519056`|`-`|
|`0x04001b19`|`list_point_duoi_betonglot`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6`|`13519066`|`-`|
|`0x04001b1a`|`list_point_coc`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6`|`13519076`|`-`|
|`0x04001b1b`|`list_grid`|`System.Collections.Generic.List`1 [class:217]<Infogrid [class:3020]>`|`6`|`13519086`|`-`|
|`0x04001b1c`|`transformblk`|`System.Collections.Generic.List`1 [class:217]<object>`|`6`|`13519096`|`-`|
|`0x04001b1d`|`blockName`|`System.Collections.Generic.List`1 [class:217]<string>`|`6`|`13519106`|`-`|
|`0x04001b1e`|`ten_ck`|`string`|`6`|`13519116`|`-`|
|`0x04001b1f`|`diameter`|`r8`|`6`|`13519126`|`-`|
|`0x04001b20`|`diameter_X`|`r8`|`6`|`13519136`|`-`|
|`0x04001b21`|`diameter_Y`|`r8`|`6`|`13519146`|`-`|
|`0x04001b22`|`la_mong_bien_thien`|`bool`|`6`|`13519156`|`-`|
|`0x04001b23`|`la_coc_chu_nhat`|`bool`|`6`|`13519166`|`-`|
|`0x04001b24`|`viewXRec`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6`|`13519176`|`-`|
|`0x04001b25`|`viewYRec`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6`|`13519186`|`-`|
|`0x04001b26`|`TLBV`|`i4`|`6`|`13519196`|`-`|
|`0x04001b27`|`l0K0eXque3Vm5S48QTvD`|`InfoMongCoc [class:2996]`|`17`|`13519206`|`-`|

### [VM-106] Type `InfoUserMongCoc` (Token `0x020002f1`, RowOffset `13381126`, Extends `0x0100000b (System.Object)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 43 | Methods count: 4
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001b37`|`ftd`|`r8`|`6`|`13519366`|`-`|
|`0x04001b38`|`ftn`|`r8`|`6`|`13519376`|`-`|
|`0x04001b39`|`fdd`|`r8`|`6`|`13519386`|`-`|
|`0x04001b3a`|`fdn`|`r8`|`6`|`13519396`|`-`|
|`0x04001b3b`|`fdai`|`r8`|`6`|`13519406`|`-`|
|`0x04001b3c`|`fdo`|`r8`|`6`|`13519416`|`-`|
|`0x04001b3d`|`fct`|`r8`|`6`|`13519426`|`-`|
|`0x04001b3e`|`dangct`|`string`|`6`|`13519436`|`-`|
|`0x04001b3f`|`spacing_td`|`r8`|`6`|`13519446`|`-`|
|`0x04001b40`|`spacing_tn`|`r8`|`6`|`13519456`|`-`|
|`0x04001b41`|`spacing_dd`|`r8`|`6`|`13519466`|`-`|
|`0x04001b42`|`spacing_dn`|`r8`|`6`|`13519476`|`-`|
|`0x04001b43`|`spacing_dai`|`r8`|`6`|`13519486`|`-`|
|`0x04001b44`|`spacing_do`|`r8`|`6`|`13519496`|`-`|
|`0x04001b45`|`spacing_ct`|`r8`|`6`|`13519506`|`-`|
|`0x04001b46`|`ndai`|`r8`|`6`|`13519516`|`-`|
|`0x04001b47`|`dbm_td_trai`|`r8`|`6`|`13519526`|`-`|
|`0x04001b48`|`dbm_td_phai`|`r8`|`6`|`13519536`|`-`|
|`0x04001b49`|`dbm_tn_tren`|`r8`|`6`|`13519546`|`-`|
|`0x04001b4a`|`dbm_tn_duoi`|`r8`|`6`|`13519556`|`-`|
|`0x04001b4b`|`dbm_dd_trai`|`r8`|`6`|`13519566`|`-`|
|`0x04001b4c`|`dbm_dd_phai`|`r8`|`6`|`13519576`|`-`|
|`0x04001b4d`|`neoSanThepDuoi`|`r8`|`6`|`13519586`|`-`|
|`0x04001b4e`|`dbm_dn_tren`|`r8`|`6`|`13519596`|`-`|
|`0x04001b4f`|`dbm_dn_duoi`|`r8`|`6`|`13519606`|`-`|
|`0x04001b50`|`dbm_dai`|`r8`|`6`|`13519616`|`-`|
|`0x04001b51`|`covers`|`CoversMong [class:3016]`|`6`|`13519626`|`-`|
|`0x04001b52`|`co_thep_tren`|`bool`|`6`|`13519636`|`-`|
|`0x04001b53`|`co_thep_cau_tao`|`bool`|`6`|`13519646`|`-`|
|`0x04001b54`|`co_thep_dai`|`bool`|`6`|`13519656`|`-`|
|`0x04001b55`|`co_thep_do`|`bool`|`6`|`13519666`|`-`|
|`0x04001b56`|`co_thep_cot`|`bool`|`6`|`13519676`|`-`|
|`0x04001b57`|`list_thep_ngang`|`System.Collections.Generic.List`1 [class:217]<LopThepThem [class:3008]>`|`6`|`13519686`|`-`|
|`0x04001b58`|`list_thep_duoi`|`System.Collections.Generic.List`1 [class:217]<LopThepThem [class:3008]>`|`6`|`13519696`|`-`|
|`0x04001b59`|`list_thep_tren`|`System.Collections.Generic.List`1 [class:217]<LopThepThem [class:3008]>`|`6`|`13519706`|`-`|
|`0x04001b5a`|`thepCho1`|`r8`|`6`|`13519716`|`-`|
|`0x04001b5b`|`thepCho2`|`r8`|`6`|`13519726`|`-`|
|`0x04001b5c`|`hCho3`|`r8`|`6`|`13519736`|`-`|
|`0x04001b5d`|`nPhuongLThepDo`|`i4`|`6`|`13519746`|`-`|
|`0x04001b5e`|`nPhuongBThepDo`|`i4`|`6`|`13519756`|`-`|
|`0x04001b5f`|`isDoiViTriThepTren`|`bool`|`6`|`13519766`|`-`|
|`0x04001b60`|`isDoiViTriThepDuoi`|`bool`|`6`|`13519776`|`-`|
|`0x04001b61`|`Ucd1JrquGNXyERtARD9U`|`InfoUserMongCoc [class:3012]`|`19`|`13519786`|`-`|

### [VM-107] Type `CoversMong` (Token `0x020002f2`, RowOffset `13381144`, Extends `0x0100000b (System.Object)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 4 | Methods count: 4
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001b62`|`cover_tren`|`r8`|`6`|`13519796`|`-`|
|`0x04001b63`|`cover_duoi`|`r8`|`6`|`13519806`|`-`|
|`0x04001b64`|`cover_ngang`|`r8`|`6`|`13519816`|`-`|
|`0x04001b65`|`O5wWEXquHKHWe4iY6Oeb`|`CoversMong [class:3016]`|`19`|`13519826`|`-`|

### [VM-108] Type `InfoMongCocSave` (Token `0x020002f5`, RowOffset `13381198`, Extends `0x0100000b (System.Object)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 63 | Methods count: 4
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001b7e`|`daiRauCoc`|`string`|`6`|`13520076`|`-`|
|`0x04001b7f`|`mocDaiDung`|`string`|`6`|`13520086`|`-`|
|`0x04001b80`|`beMocCauTao`|`string`|`6`|`13520096`|`-`|
|`0x04001b81`|`Cover_cot`|`string`|`6`|`13520106`|`-`|
|`0x04001b82`|`dai_cot`|`string`|`6`|`13520116`|`-`|
|`0x04001b83`|`phi_cot`|`string`|`6`|`13520126`|`-`|
|`0x04001b84`|`tb_cover_duoi`|`string`|`6`|`13520136`|`-`|
|`0x04001b85`|`tb_dbm_thep_ngang_duoi_duoi`|`string`|`6`|`13520146`|`-`|
|`0x04001b86`|`tb_dbm_thep_doc_duoi_trai`|`string`|`6`|`13520156`|`-`|
|`0x04001b87`|`tb_dbm_thep_ngang_duoi_tren`|`string`|`6`|`13520166`|`-`|
|`0x04001b88`|`tb_dbm_thep_doc_duoi_phai`|`string`|`6`|`13520176`|`-`|
|`0x04001b89`|`tb_ten_ck`|`string`|`6`|`13520186`|`-`|
|`0x04001b8a`|`Thep_cot_Y`|`string`|`6`|`13520196`|`-`|
|`0x04001b8b`|`Thep_cot_X`|`string`|`6`|`13520206`|`-`|
|`0x04001b8c`|`sl`|`string`|`6`|`13520216`|`-`|
|`0x04001b8d`|`tb_thep_doc_tren`|`string`|`6`|`13520226`|`-`|
|`0x04001b8e`|`tb_thep_ngang_tren`|`string`|`6`|`13520236`|`-`|
|`0x04001b8f`|`tb_h1`|`string`|`6`|`13520246`|`-`|
|`0x04001b90`|`tb_e1`|`string`|`6`|`13520256`|`-`|
|`0x04001b91`|`tb_h2`|`string`|`6`|`13520266`|`-`|
|`0x04001b92`|`tb_h3`|`string`|`6`|`13520276`|`-`|
|`0x04001b93`|`tb_d3`|`string`|`6`|`13520286`|`-`|
|`0x04001b94`|`tb_e3`|`string`|`6`|`13520296`|`-`|
|`0x04001b95`|`tb_d1`|`string`|`6`|`13520306`|`-`|
|`0x04001b96`|`Ct`|`string`|`6`|`13520316`|`-`|
|`0x04001b97`|`tb_thep_do`|`string`|`6`|`13520326`|`-`|
|`0x04001b98`|`cb_co_thep_do`|`bool`|`6`|`13520336`|`-`|
|`0x04001b99`|`cb_co_thep_dai`|`bool`|`6`|`13520346`|`-`|
|`0x04001b9a`|`cb_co_thep_cau_tao`|`bool`|`6`|`13520356`|`-`|
|`0x04001b9b`|`tb_dbm_thep_dai`|`string`|`6`|`13520366`|`-`|
|`0x04001b9c`|`tb_thep_dai`|`string`|`6`|`13520376`|`-`|
|`0x04001b9d`|`tb_dbm_thep_ngang_tren_duoi`|`string`|`6`|`13520386`|`-`|
|`0x04001b9e`|`tb_dbm_thep_doc_tren_trai`|`string`|`6`|`13520396`|`-`|
|`0x04001b9f`|`tb_dbm_thep_ngang_tren_tren`|`string`|`6`|`13520406`|`-`|
|`0x04001ba0`|`tb_dbm_thep_doc_tren_phai`|`string`|`6`|`13520416`|`-`|
|`0x04001ba1`|`tb_thep_doc_duoi`|`string`|`6`|`13520426`|`-`|
|`0x04001ba2`|`tb_thep_ngang_duoi`|`string`|`6`|`13520436`|`-`|
|`0x04001ba3`|`tb_cover_ngang`|`string`|`6`|`13520446`|`-`|
|`0x04001ba4`|`tb_cover_tren`|`string`|`6`|`13520456`|`-`|
|`0x04001ba5`|`RadioButton1`|`bool`|`6`|`13520466`|`-`|
|`0x04001ba6`|`CheckThepCot`|`bool`|`6`|`13520476`|`-`|
|`0x04001ba7`|`RadioButton2`|`bool`|`6`|`13520486`|`-`|
|`0x04001ba8`|`cb_co_thep_tren`|`bool`|`6`|`13520496`|`-`|
|`0x04001ba9`|`tb_day_san_trai`|`string`|`6`|`13520506`|`-`|
|`0x04001baa`|`tb_day_san_phai`|`string`|`6`|`13520516`|`-`|
|`0x04001bab`|`tbcautao`|`string`|`6`|`13520526`|`-`|
|`0x04001bac`|`notcbdang`|`bool`|`6`|`13520536`|`-`|
|`0x04001bad`|`cbdang`|`bool`|`6`|`13520546`|`-`|
|`0x04001bae`|`tb_h4`|`string`|`6`|`13520556`|`-`|
|`0x04001baf`|`Thep_cot_tron`|`string`|`6`|`13520566`|`-`|
|`0x04001bb0`|`ThepCho`|`string`|`6`|`13520576`|`-`|
|`0x04001bb1`|`hCho3`|`string`|`6`|`13520586`|`-`|
|`0x04001bb2`|`hCho1`|`string`|`6`|`13520596`|`-`|
|`0x04001bb3`|`hCho2`|`string`|`6`|`13520606`|`-`|
|`0x04001bb4`|`list_thep_ngang`|`System.Collections.Generic.List`1 [class:217]<LopThepThem [class:3008]>`|`6`|`13520616`|`-`|
|`0x04001bb5`|`list_thep_duoi`|`System.Collections.Generic.List`1 [class:217]<LopThepThem [class:3008]>`|`6`|`13520626`|`-`|
|`0x04001bb6`|`list_thep_tren`|`System.Collections.Generic.List`1 [class:217]<LopThepThem [class:3008]>`|`6`|`13520636`|`-`|
|`0x04001bb7`|`list_dl_cot`|`System.Collections.Generic.List`1 [class:217]<Dlcot [class:3024]>`|`6`|`13520646`|`-`|
|`0x04001bb8`|`khoaDauCot`|`bool`|`6`|`13520656`|`-`|
|`0x04001bb9`|`TLBV`|`string`|`6`|`13520666`|`-`|
|`0x04001bba`|`DoiViTriThepTren`|`bool`|`6`|`13520676`|`-`|
|`0x04001bbb`|`DoiViTriThepDuoi`|`bool`|`6`|`13520686`|`-`|
|`0x04001bbc`|`LCUQe2qujIZw4oCMXnpP`|`InfoMongCocSave [class:3028]`|`17`|`13520696`|`-`|

### [VM-109] Type `InfoMongCocSaveV2` (Token `0x0200031b`, RowOffset `13381882`, Extends `0x0100000b (System.Object)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 57 | Methods count: 4
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001d14`|`hChoCuoi`|`object`|`6`|`13524136`|`-`|
|`0x04001d15`|`thepCho1`|`object`|`6`|`13524146`|`-`|
|`0x04001d16`|`thepCho2`|`object`|`6`|`13524156`|`-`|
|`0x04001d17`|`hDamTrenMong`|`object`|`6`|`13524166`|`-`|
|`0x04001d18`|`hTrenMong`|`object`|`6`|`13524176`|`-`|
|`0x04001d19`|`hMong`|`object`|`6`|`13524186`|`-`|
|`0x04001d1a`|`hBtl`|`object`|`6`|`13524196`|`-`|
|`0x04001d1b`|`ct`|`object`|`6`|`13524206`|`-`|
|`0x04001d1c`|`doiViTriThepTrenCb`|`bool`|`6`|`13524216`|`-`|
|`0x04001d1d`|`doiViTriThepDuoiCb`|`bool`|`6`|`13524226`|`-`|
|`0x04001d1e`|`thepDD`|`object`|`6`|`13524236`|`-`|
|`0x04001d1f`|`thepTD`|`object`|`6`|`13524246`|`-`|
|`0x04001d20`|`thepDN`|`object`|`6`|`13524256`|`-`|
|`0x04001d21`|`thepTN`|`object`|`6`|`13524266`|`-`|
|`0x04001d22`|`coThepTren`|`bool`|`6`|`13524276`|`-`|
|`0x04001d23`|`coverTren`|`object`|`6`|`13524286`|`-`|
|`0x04001d24`|`beMocThepTren`|`object`|`6`|`13524296`|`-`|
|`0x04001d25`|`coThepCauTao`|`bool`|`6`|`13524306`|`-`|
|`0x04001d26`|`thepCauTao`|`object`|`6`|`13524316`|`-`|
|`0x04001d27`|`beMocThepCauTao`|`object`|`6`|`13524326`|`-`|
|`0x04001d28`|`dangU`|`bool`|`6`|`13524336`|`-`|
|`0x04001d29`|`dangL`|`bool`|`6`|`13524346`|`-`|
|`0x04001d2a`|`dangZ`|`bool`|`6`|`13524356`|`-`|
|`0x04001d2b`|`dangUThepDo`|`bool`|`6`|`13524366`|`-`|
|`0x04001d2c`|`dangZThepDo`|`bool`|`6`|`13524376`|`-`|
|`0x04001d2d`|`coverDuoi`|`object`|`6`|`13524386`|`-`|
|`0x04001d2e`|`beMocThepDuoi`|`object`|`6`|`13524396`|`-`|
|`0x04001d2f`|`neoSanThepDuoi`|`object`|`6`|`13524406`|`-`|
|`0x04001d30`|`tenMong`|`object`|`6`|`13524416`|`-`|
|`0x04001d31`|`soLuong`|`object`|`6`|`13524426`|`-`|
|`0x04001d32`|`tyLe`|`object`|`6`|`13524436`|`-`|
|`0x04001d33`|`tyLeMB`|`object`|`6`|`13524446`|`-`|
|`0x04001d34`|`neoPhuongNgang`|`object`|`6`|`13524456`|`-`|
|`0x04001d35`|`neoPhuongDung`|`object`|`6`|`13524466`|`-`|
|`0x04001d36`|`thepXien`|`object`|`6`|`13524476`|`-`|
|`0x04001d37`|`neoXien`|`object`|`6`|`13524486`|`-`|
|`0x04001d38`|`coThepDai`|`bool`|`6`|`13524496`|`-`|
|`0x04001d39`|`thepDai`|`object`|`6`|`13524506`|`-`|
|`0x04001d3a`|`doanChongLenNhau`|`object`|`6`|`13524516`|`-`|
|`0x04001d3b`|`coverNgang`|`object`|`6`|`13524526`|`-`|
|`0x04001d3c`|`coThepDo`|`bool`|`6`|`13524536`|`-`|
|`0x04001d3d`|`thepDo`|`object`|`6`|`13524546`|`-`|
|`0x04001d3e`|`beMocDaiDung`|`object`|`6`|`13524556`|`-`|
|`0x04001d3f`|`chieuDaiRauThep`|`object`|`6`|`13524566`|`-`|
|`0x04001d40`|`beDaySanHam`|`object`|`6`|`13524576`|`-`|
|`0x04001d41`|`coThepCot`|`bool`|`6`|`13524586`|`-`|
|`0x04001d42`|`coverThepDaiCot`|`object`|`6`|`13524596`|`-`|
|`0x04001d43`|`thepDaiCotMain`|`object`|`6`|`13524606`|`-`|
|`0x04001d44`|`khoaDauCot`|`bool`|`6`|`13524616`|`-`|
|`0x04001d45`|`listThepDuoi`|`System.Collections.Generic.List`1 [class:217]<LopThepThemV2 [class:3160]>`|`6`|`13524626`|`-`|
|`0x04001d46`|`listThepTren`|`System.Collections.Generic.List`1 [class:217]<LopThepThemV2 [class:3160]>`|`6`|`13524636`|`-`|
|`0x04001d47`|`listThepNgang`|`System.Collections.Generic.List`1 [class:217]<LopThepThemV2 [class:3160]>`|`6`|`13524646`|`-`|
|`0x04001d48`|`listDataCot`|`System.Collections.Generic.List`1 [class:217]<DlCotV2 [class:3168]>`|`6`|`13524656`|`-`|
|`0x04001d49`|`listGCThem`|`System.Collections.Generic.List`1 [class:217]<GiaCuongThemTheoPolygon [class:3152]>`|`6`|`13524666`|`-`|
|`0x04001d4a`|`isTachThepDo`|`bool`|`6`|`13524676`|`-`|
|`0x04001d4b`|`indexTachThepDo`|`i4`|`6`|`13524686`|`-`|
|`0x04001d4c`|`aI0W1HqHzAorIJtE0Avx`|`object`|`19`|`13524696`|`-`|

### [VM-110] Type `Kata_pro64_Cad2025.Rai_Mong_Module` (Token `0x02000335`, RowOffset `13382350`, Extends `0x0100000b (System.Object)`, Evidence `ev_3bdf54e817a9b5840d6618afe7ca3edeaabd4e8c3021dcb361d608566084bf9d`)
- Fields count: 4 | Methods count: 4
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001db9`|`formRaiMong`|`Kata_pro64_Cad2025.Form_Rai_Mong [class:500]`|`22`|`13525786`|`-`|
|`0x04001dba`|`info_rai_mong`|`InfoRaiMong [class:3288]`|`22`|`13525796`|`-`|
|`0x04001dbb`|`info_rai_mong_don`|`InfoRaiMongDon [class:3296]`|`22`|`13525806`|`-`|
|`0x04001dbc`|`cRORnnqkmgQTecXBCCIV`|`Kata_pro64_Cad2025.Rai_Mong_Module [class:3284]`|`19`|`13525816`|`-`|

### [VM-111] Type `InfoRaiMong` (Token `0x02000336`, RowOffset `13382368`, Extends `0x0100000b (System.Object)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 10 | Methods count: 7
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001dbd`|`pointGoc`|`Kata_Class_Lib_Revit.diem [class:85]`|`6`|`13525826`|`-`|
|`0x04001dbe`|`blkCoc`|`object`|`6`|`13525836`|`-`|
|`0x04001dbf`|`plCoc`|`Polygon [class:89]`|`6`|`13525846`|`-`|
|`0x04001dc0`|`soCoc`|`i4`|`6`|`13525856`|`-`|
|`0x04001dc1`|`kcDenMepDaiTxt`|`string`|`6`|`13525866`|`-`|
|`0x04001dc2`|`cotChiuTai`|`InfoCotCocGenMong [class:3584]`|`6`|`13525876`|`-`|
|`0x04001dc3`|`angXoay`|`r8`|`6`|`13525886`|`-`|
|`0x04001dc4`|`TenMong`|`string`|`6`|`13525896`|`-`|
|`0x04001dc5`|`viTriDatTen`|`ViTriDatTen [valuetype:3564]`|`6`|`13525906`|`-`|
|`0x04001dc6`|`LcJaqsqpVP4DEMVWkUBd`|`InfoRaiMong [class:3288]`|`19`|`13525916`|`-`|

### [VM-112] Type `RaiMongJig` (Token `0x02000337`, RowOffset `13382386`, Extends `0x01000014 (Autodesk.AutoCAD.EditorInput.DrawJig)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 8 | Methods count: 9
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001dc7`|`polyMong`|`Polygon [class:89]`|`6`|`13525926`|`-`|
|`0x04001dc8`|`listPlThem`|`System.Collections.Generic.List`1 [class:217]<Polygon [class:89]>`|`6`|`13525936`|`-`|
|`0x04001dc9`|`polyBtl`|`Polygon [class:89]`|`6`|`13525946`|`-`|
|`0x04001dca`|`lpCoc`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6`|`13525956`|`-`|
|`0x04001dcb`|`plCoc`|`Polygon [class:89]`|`6`|`13525966`|`-`|
|`0x04001dcc`|`p0`|`Kata_Class_Lib_Revit.diem [class:85]`|`6`|`13525976`|`-`|
|`0x04001dcd`|`choosingPoint`|`Kata_Class_Lib_Revit.diem [class:85]`|`6`|`13525986`|`-`|
|`0x04001dce`|`uBsBIeqwUpPVWM5pNoZm`|`RaiMongJig [class:3292]`|`17`|`13525996`|`-`|

### [VM-113] Type `InfoRaiMongDon` (Token `0x02000338`, RowOffset `13382404`, Extends `0x0100000b (System.Object)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 29 | Methods count: 10
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001dcf`|`pointGoc`|`Kata_Class_Lib_Revit.diem [class:85]`|`6`|`13526006`|`-`|
|`0x04001dd0`|`plBeTongLot`|`Polygon [class:89]`|`6`|`13526016`|`-`|
|`0x04001dd1`|`plMong`|`Polygon [class:89]`|`6`|`13526026`|`-`|
|`0x04001dd2`|`plDinhMong`|`Polygon [class:89]`|`6`|`13526036`|`-`|
|`0x04001dd3`|`plCot`|`Polygon [class:89]`|`6`|`13526046`|`-`|
|`0x04001dd4`|`listCCheo`|`System.Collections.Generic.List`1 [class:217]<CurveXYZ [class:293]>`|`6`|`13526056`|`-`|
|`0x04001dd5`|`TenMong`|`string`|`6`|`13526066`|`-`|
|`0x04001dd6`|`viTriDatTen`|`ViTriDatTen [valuetype:3564]`|`6`|`13526076`|`-`|
|`0x04001dd7`|`Y1`|`r8`|`6`|`13526086`|`-`|
|`0x04001dd8`|`Y2`|`r8`|`6`|`13526096`|`-`|
|`0x04001dd9`|`Y3`|`r8`|`6`|`13526106`|`-`|
|`0x04001dda`|`X1`|`r8`|`6`|`13526116`|`-`|
|`0x04001ddb`|`X2`|`r8`|`6`|`13526126`|`-`|
|`0x04001ddc`|`X3`|`r8`|`6`|`13526136`|`-`|
|`0x04001ddd`|`T1`|`r8`|`6`|`13526146`|`-`|
|`0x04001dde`|`N1`|`r8`|`6`|`13526156`|`-`|
|`0x04001ddf`|`D1`|`r8`|`6`|`13526166`|`-`|
|`0x04001de0`|`B1`|`r8`|`6`|`13526176`|`-`|
|`0x04001de1`|`T2`|`r8`|`6`|`13526186`|`-`|
|`0x04001de2`|`N2`|`r8`|`6`|`13526196`|`-`|
|`0x04001de3`|`D2`|`r8`|`6`|`13526206`|`-`|
|`0x04001de4`|`B2`|`r8`|`6`|`13526216`|`-`|
|`0x04001de5`|`CT1`|`string`|`6`|`13526226`|`-`|
|`0x04001de6`|`H1`|`string`|`6`|`13526236`|`-`|
|`0x04001de7`|`H12`|`string`|`6`|`13526246`|`-`|
|`0x04001de8`|`Cover`|`string`|`6`|`13526256`|`-`|
|`0x04001de9`|`ThepPhuongX`|`string`|`6`|`13526266`|`-`|
|`0x04001dea`|`ThepPhuongY`|`string`|`6`|`13526276`|`-`|
|`0x04001deb`|`MP6bOtqw0VS7cmLqSCj2`|`InfoRaiMongDon [class:3296]`|`19`|`13526286`|`-`|

### [VM-114] Type `Kata_pro64_Cad2025.Ve_Mat_Bang_Mong_Module` (Token `0x02000370`, RowOffset `13383412`, Extends `0x0100000b (System.Object)`, Evidence `ev_3bdf54e817a9b5840d6618afe7ca3edeaabd4e8c3021dcb361d608566084bf9d`)
- Fields count: 10 | Methods count: 19
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001f03`|`formVeMatBangMong`|`Kata_pro64_Cad2025.Form_Ve_Mat_Bang_Mong [class:628]`|`22`|`13529086`|`-`|
|`0x04001f04`|`info_ve_mat_bang_mong`|`InfoVeMatBangMong [class:3528]`|`22`|`13529096`|`-`|
|`0x04001f05`|`info_mb_mong_bang`|`InfoMBMongBang [class:3548]`|`22`|`13529106`|`-`|
|`0x04001f06`|`info_mb_mong_don`|`InfoMBMongDon [class:3616]`|`22`|`13529116`|`-`|
|`0x04001f07`|`LOAIDATACOC`|`string`|`22`|`13529126`|`-`|
|`0x04001f08`|`LOAIDATAJOINTREACTION`|`string`|`22`|`13529136`|`-`|
|`0x04001f09`|`LOAIDATAMBMONGCOC`|`string`|`22`|`13529146`|`-`|
|`0x04001f0a`|`LOAIDATAMBMONGBANG`|`string`|`22`|`13529156`|`-`|
|`0x04001f0b`|`LOAIDATALINECHEO`|`string`|`22`|`13529166`|`-`|
|`0x04001f0c`|`IypD4Rq4QWCHECfpjMRR`|`Kata_pro64_Cad2025.Ve_Mat_Bang_Mong_Module [class:3520]`|`17`|`13529176`|`-`|

### [VM-115] Type `ModeBuildEtabEnum` (Token `0x02000371`, RowOffset `13383430`, Extends `0x01000011 (System.Enum)`, Evidence `ev_549bfe33ecd78257ce27570bdd19331f304e770621ccd33a14059b374ac982e3`)
- Fields count: 5 | Methods count: 0
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001f0d`|`value__`|`i4`|`1542`|`13529186`|`-`|
|`0x04001f0e`|`Normal`|`ModeBuildEtabEnum [valuetype:3524]`|`32854`|`13529196`|`0`|
|`0x04001f0f`|`MongCoc`|`ModeBuildEtabEnum [valuetype:3524]`|`32854`|`13529206`|`1`|
|`0x04001f10`|`MongBang`|`ModeBuildEtabEnum [valuetype:3524]`|`32854`|`13529216`|`2`|
|`0x04001f11`|`MongDon`|`ModeBuildEtabEnum [valuetype:3524]`|`32854`|`13529226`|`3`|

### [VM-116] Type `InfoVeMatBangMong` (Token `0x02000372`, RowOffset `13383448`, Extends `0x0100000b (System.Object)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 14 | Methods count: 12
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001f12`|`pointGoc`|`Kata_Class_Lib_Revit.diem [class:85]`|`6`|`13529236`|`-`|
|`0x04001f13`|`listCotChiuTai`|`System.Collections.Generic.List`1 [class:217]<InfoCotCocGenMong [class:3584]>`|`6`|`13529246`|`-`|
|`0x04001f14`|`listCocInput`|`System.Collections.Generic.List`1 [class:217]<CocChiuTai [class:3568]>`|`6`|`13529256`|`-`|
|`0x04001f15`|`ranhTamCoc`|`Polygon [class:89]`|`6`|`13529266`|`-`|
|`0x04001f16`|`ranhBoTriDai`|`Polygon [class:89]`|`6`|`13529276`|`-`|
|`0x04001f17`|`n1`|`r8`|`6`|`13529286`|`-`|
|`0x04001f18`|`n2`|`r8`|`6`|`13529296`|`-`|
|`0x04001f19`|`soCocToiThieu`|`i4`|`6`|`13529306`|`-`|
|`0x04001f1a`|`kcDenMepDaiTxt`|`string`|`6`|`13529316`|`-`|
|`0x04001f1b`|`TienTo`|`string`|`6`|`13529326`|`-`|
|`0x04001f1c`|`SoBatDau`|`i4`|`6`|`13529336`|`-`|
|`0x04001f1d`|`viTriDatTen`|`ViTriDatTen [valuetype:3564]`|`6`|`13529346`|`-`|
|`0x04001f1e`|`mainVecto`|`Kata_Class_Lib_Revit.diem [class:85]`|`6`|`13529356`|`-`|
|`0x04001f1f`|`o6Cdenq2QhcXmlSU0UME`|`InfoVeMatBangMong [class:3528]`|`19`|`13529366`|`-`|

### [VM-117] Type `ViTriCurveDam` (Token `0x02000375`, RowOffset `13383502`, Extends `0x01000011 (System.Enum)`, Evidence `ev_549bfe33ecd78257ce27570bdd19331f304e770621ccd33a14059b374ac982e3`)
- Fields count: 5 | Methods count: 0
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001f30`|`value__`|`i4`|`1542`|`13529536`|`-`|
|`0x04001f31`|`Trai`|`ViTriCurveDam [valuetype:3540]`|`32854`|`13529546`|`0`|
|`0x04001f32`|`Phai`|`ViTriCurveDam [valuetype:3540]`|`32854`|`13529556`|`1`|
|`0x04001f33`|`Dau`|`ViTriCurveDam [valuetype:3540]`|`32854`|`13529566`|`2`|
|`0x04001f34`|`Cuoi`|`ViTriCurveDam [valuetype:3540]`|`32854`|`13529576`|`3`|

### [VM-118] Type `InfoMBMongBang` (Token `0x02000377`, RowOffset `13383538`, Extends `0x0100000b (System.Object)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 13 | Methods count: 15
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001f3a`|`pointGoc`|`Kata_Class_Lib_Revit.diem [class:85]`|`6`|`13529636`|`-`|
|`0x04001f3b`|`listDamInput`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Info_Beam3D [class:481]>`|`6`|`13529646`|`-`|
|`0x04001f3c`|`listLength`|`System.Collections.Generic.Dictionary`2 [class:389]<i4,System.Tuple`4 [class:1041]<string,string,string,string>>`|`6`|`13529656`|`-`|
|`0x04001f3d`|`listBaoNgoai`|`System.Collections.Generic.List`1 [class:217]<Polygon [class:89]>`|`6`|`13529666`|`-`|
|`0x04001f3e`|`listBaoTrong`|`System.Collections.Generic.List`1 [class:217]<Polygon [class:89]>`|`6`|`13529676`|`-`|
|`0x04001f3f`|`listPlMong`|`System.Collections.Generic.List`1 [class:217]<Polygon [class:89]>`|`6`|`13529686`|`-`|
|`0x04001f40`|`ranhBoTriDai`|`Polygon [class:89]`|`6`|`13529696`|`-`|
|`0x04001f41`|`infoChung`|`MongBangJsonDto [class:3580]`|`6`|`13529706`|`-`|
|`0x04001f42`|`areaSpring`|`InfoAreaSpring [class:3544]`|`6`|`13529716`|`-`|
|`0x04001f43`|`listPlMepDai`|`System.Collections.Generic.List`1 [class:217]<Polygon [class:89]>`|`6`|`13529726`|`-`|
|`0x04001f44`|`listLineCheo`|`System.Collections.Generic.List`1 [class:217]<CurveXYZ [class:293]>`|`6`|`13529736`|`-`|
|`0x04001f45`|`llDim`|`System.Collections.Generic.List`1 [class:217]<System.Collections.Generic.List`1 [class:217]<CurveXYZ [class:293]>>`|`6`|`13529746`|`-`|
|`0x04001f46`|`YYmn7Vq2M5lwneAsMD6p`|`InfoMBMongBang [class:3548]`|`19`|`13529756`|`-`|

### [VM-119] Type `ViTriDatTen` (Token `0x0200037b`, RowOffset `13383610`, Extends `0x01000011 (System.Enum)`, Evidence `ev_549bfe33ecd78257ce27570bdd19331f304e770621ccd33a14059b374ac982e3`)
- Fields count: 5 | Methods count: 0
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001f53`|`value__`|`i4`|`1542`|`13529886`|`-`|
|`0x04001f54`|`Trai`|`ViTriDatTen [valuetype:3564]`|`32854`|`13529896`|`0`|
|`0x04001f55`|`Phai`|`ViTriDatTen [valuetype:3564]`|`32854`|`13529906`|`1`|
|`0x04001f56`|`Tren`|`ViTriDatTen [valuetype:3564]`|`32854`|`13529916`|`2`|
|`0x04001f57`|`Duoi`|`ViTriDatTen [valuetype:3564]`|`32854`|`13529926`|`3`|

### [VM-120] Type `CocChiuTai` (Token `0x0200037c`, RowOffset `13383628`, Extends `0x0100000b (System.Object)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 11 | Methods count: 7
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001f58`|`blkCoc`|`object`|`6`|`13529936`|`-`|
|`0x04001f59`|`sucChiuTaiCoc`|`r8`|`6`|`13529946`|`-`|
|`0x04001f5a`|`plCoc`|`Polygon [class:89]`|`6`|`13529956`|`-`|
|`0x04001f5b`|`diaCoc`|`r8`|`6`|`13529966`|`-`|
|`0x04001f5c`|`chieuDaiCoc`|`r8`|`6`|`13529976`|`-`|
|`0x04001f5d`|`giaTienCoc`|`r8`|`6`|`13529986`|`-`|
|`0x04001f5e`|`soLuong`|`i4`|`6`|`13529996`|`-`|
|`0x04001f5f`|`tenCoc`|`string`|`6`|`13530006`|`-`|
|`0x04001f60`|`center`|`Kata_Class_Lib_Revit.diem [class:85]`|`6`|`13530016`|`-`|
|`0x04001f61`|`newPosition`|`Kata_Class_Lib_Revit.diem [class:85]`|`6`|`13530026`|`-`|
|`0x04001f62`|`hGVPvvq2yZ55JCMwJ2Gm`|`CocChiuTai [class:3568]`|`17`|`13530036`|`-`|

### [VM-121] Type `MongJsonDto` (Token `0x0200037e`, RowOffset `13383664`, Extends `0x0100000b (System.Object)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 12 | Methods count: 4
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001f67`|`tenMong`|`string`|`6`|`13530086`|`-`|
|`0x04001f68`|`txtLucDoc`|`string`|`6`|`13530096`|`-`|
|`0x04001f69`|`isCotGop`|`bool`|`6`|`13530106`|`-`|
|`0x04001f6a`|`mainVecto`|`Kata_Class_Lib_Revit.diem [class:85]`|`6`|`13530116`|`-`|
|`0x04001f6b`|`duoiDoc`|`ThepBoTriMong [class:5308]`|`6`|`13530126`|`-`|
|`0x04001f6c`|`duoiNgang`|`ThepBoTriMong [class:5308]`|`6`|`13530136`|`-`|
|`0x04001f6d`|`trenDoc`|`ThepBoTriMong [class:5308]`|`6`|`13530146`|`-`|
|`0x04001f6e`|`trenNgang`|`ThepBoTriMong [class:5308]`|`6`|`13530156`|`-`|
|`0x04001f6f`|`cocChiuTai`|`CocChiuTai [class:3568]`|`6`|`13530166`|`-`|
|`0x04001f70`|`isVacMong`|`bool`|`6`|`13530176`|`-`|
|`0x04001f71`|`mauMong`|`string`|`6`|`13530186`|`-`|
|`0x04001f72`|`W2RkZrq2LNRPUBcIxYwg`|`MongJsonDto [class:3576]`|`17`|`13530196`|`-`|

### [VM-122] Type `MongBangJsonDto` (Token `0x0200037f`, RowOffset `13383682`, Extends `0x0100000b (System.Object)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 6 | Methods count: 4
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001f73`|`btl`|`string`|`6`|`13530206`|`-`|
|`0x04001f74`|`caoTrinhDayMong`|`string`|`6`|`13530216`|`-`|
|`0x04001f75`|`HBatDayMong`|`string`|`6`|`13530226`|`-`|
|`0x04001f76`|`HMong`|`string`|`6`|`13530236`|`-`|
|`0x04001f77`|`Cover`|`string`|`6`|`13530246`|`-`|
|`0x04001f78`|`PDosjUq2c0PuoabJlKe2`|`MongBangJsonDto [class:3580]`|`19`|`13530256`|`-`|

### [VM-123] Type `InfoCotCocGenMong` (Token `0x02000380`, RowOffset `13383700`, Extends `0x0100000b (System.Object)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 32 | Methods count: 41
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001f79`|`isCotEmpty`|`bool`|`6`|`13530266`|`-`|
|`0x04001f7a`|`isCotGop`|`bool`|`6`|`13530276`|`-`|
|`0x04001f7b`|`mainVecto`|`Kata_Class_Lib_Revit.diem [class:85]`|`6`|`13530286`|`-`|
|`0x04001f7c`|`needAutoGen`|`bool`|`6`|`13530296`|`-`|
|`0x04001f7d`|`regenByNumCocEvent`|`bool`|`6`|`13530306`|`-`|
|`0x04001f7e`|`regenByRotateEvent`|`bool`|`6`|`13530316`|`-`|
|`0x04001f7f`|`regenByThayDoiLoaiCocEvent`|`bool`|`6`|`13530326`|`-`|
|`0x04001f80`|`regenByHinhDangMongEvent`|`bool`|`6`|`13530336`|`-`|
|`0x04001f81`|`plCot`|`Polygon [class:89]`|`6`|`13530346`|`-`|
|`0x04001f82`|`centerGravity`|`Kata_Class_Lib_Revit.diem [class:85]`|`6`|`13530356`|`-`|
|`0x04001f83`|`txtLucDoc`|`string`|`6`|`13530366`|`-`|
|`0x04001f84`|`listTxtLucDoc`|`System.Collections.Generic.List`1 [class:217]<InfoText [class:3572]>`|`6`|`13530376`|`-`|
|`0x04001f85`|`soCoc`|`i4`|`6`|`13530386`|`-`|
|`0x04001f86`|`cocInput`|`CocChiuTai [class:3568]`|`6`|`13530396`|`-`|
|`0x04001f87`|`chieuCaoMong`|`r8`|`6`|`13530406`|`-`|
|`0x04001f88`|`isVacMong`|`bool`|`6`|`13530416`|`-`|
|`0x04001f89`|`mauMong`|`string`|`6`|`13530426`|`-`|
|`0x04001f8a`|`indexInDgv`|`i4`|`6`|`13530436`|`-`|
|`0x04001f8b`|`id`|`i4`|`6`|`13530446`|`-`|
|`0x04001f8c`|`angXoay`|`r8`|`6`|`13530456`|`-`|
|`0x04001f8d`|`tap_bien_trong`|`System.Collections.Generic.List`1 [class:217]<Polygon [class:89]>`|`6`|`13530466`|`-`|
|`0x04001f8e`|`listCotOthers`|`System.Collections.Generic.List`1 [class:217]<Polygon [class:89]>`|`6`|`13530476`|`-`|
|`0x04001f8f`|`obj`|`object`|`6`|`13530486`|`-`|
|`0x04001f90`|`plMong`|`Polygon [class:89]`|`6`|`13530496`|`-`|
|`0x04001f91`|`listCoc`|`System.Collections.Generic.List`1 [class:217]<CocChiuTai [class:3568]>`|`6`|`13530506`|`-`|
|`0x04001f92`|`isDungTam`|`bool`|`6`|`13530516`|`-`|
|`0x04001f93`|`tenMong`|`string`|`6`|`13530526`|`-`|
|`0x04001f94`|`duoiDoc`|`ThepBoTriMong [class:5308]`|`6`|`13530536`|`-`|
|`0x04001f95`|`duoiNgang`|`ThepBoTriMong [class:5308]`|`6`|`13530546`|`-`|
|`0x04001f96`|`trenDoc`|`ThepBoTriMong [class:5308]`|`6`|`13530556`|`-`|
|`0x04001f97`|`trenNgang`|`ThepBoTriMong [class:5308]`|`6`|`13530566`|`-`|
|`0x04001f98`|`ULUx5cq2saWZaSRy5krS`|`InfoCotCocGenMong [class:3584]`|`17`|`13530576`|`-`|

### [VM-124] Type `InfoMBMongDon` (Token `0x02000388`, RowOffset `13383844`, Extends `0x0100000b (System.Object)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 9 | Methods count: 7
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001fb8`|`pointGoc`|`Kata_Class_Lib_Revit.diem [class:85]`|`6`|`13530896`|`-`|
|`0x04001fb9`|`listCotChiuTai`|`System.Collections.Generic.List`1 [class:217]<CotChiuTaiMongDon [class:3624]>`|`6`|`13530906`|`-`|
|`0x04001fba`|`ranhBoTriDai`|`Polygon [class:89]`|`6`|`13530916`|`-`|
|`0x04001fbb`|`TienTo`|`string`|`6`|`13530926`|`-`|
|`0x04001fbc`|`SoBatDau`|`i4`|`6`|`13530936`|`-`|
|`0x04001fbd`|`viTriDatTen`|`ViTriDatTen [valuetype:3564]`|`6`|`13530946`|`-`|
|`0x04001fbe`|`areaSpring`|`InfoAreaSpring [class:3544]`|`6`|`13530956`|`-`|
|`0x04001fbf`|`listPlTEST`|`System.Collections.Generic.List`1 [class:217]<Polygon [class:89]>`|`6`|`13530966`|`-`|
|`0x04001fc0`|`PL0a65q2J7ZnYPIfEHU7`|`InfoMBMongDon [class:3616]`|`19`|`13530976`|`-`|

### [VM-125] Type `CotChiuTaiMongDon` (Token `0x0200038a`, RowOffset `13383880`, Extends `0x0100000b (System.Object)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 24 | Methods count: 8
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001fc6`|`plMong`|`Polygon [class:89]`|`6`|`13531036`|`-`|
|`0x04001fc7`|`blkObj`|`object`|`6`|`13531046`|`-`|
|`0x04001fc8`|`plCot`|`Polygon [class:89]`|`6`|`13531056`|`-`|
|`0x04001fc9`|`centerGravity`|`Kata_Class_Lib_Revit.diem [class:85]`|`6`|`13531066`|`-`|
|`0x04001fca`|`xVec`|`Kata_Class_Lib_Revit.diem [class:85]`|`6`|`13531076`|`-`|
|`0x04001fcb`|`yVec`|`Kata_Class_Lib_Revit.diem [class:85]`|`6`|`13531086`|`-`|
|`0x04001fcc`|`lX`|`r8`|`6`|`13531096`|`-`|
|`0x04001fcd`|`lY`|`r8`|`6`|`13531106`|`-`|
|`0x04001fce`|`txtLucDoc`|`string`|`6`|`13531116`|`-`|
|`0x04001fcf`|`index`|`i4`|`6`|`13531126`|`-`|
|`0x04001fd0`|`Trai`|`string`|`6`|`13531136`|`-`|
|`0x04001fd1`|`Phai`|`string`|`6`|`13531146`|`-`|
|`0x04001fd2`|`Tren`|`string`|`6`|`13531156`|`-`|
|`0x04001fd3`|`Duoi`|`string`|`6`|`13531166`|`-`|
|`0x04001fd4`|`btl`|`string`|`6`|`13531176`|`-`|
|`0x04001fd5`|`caoTrinhDayMong`|`string`|`6`|`13531186`|`-`|
|`0x04001fd6`|`HBatDayMong`|`string`|`6`|`13531196`|`-`|
|`0x04001fd7`|`HMong`|`string`|`6`|`13531206`|`-`|
|`0x04001fd8`|`KcCoMongDinhCot`|`string`|`6`|`13531216`|`-`|
|`0x04001fd9`|`Cover`|`string`|`6`|`13531226`|`-`|
|`0x04001fda`|`ThepPhuongX`|`string`|`6`|`13531236`|`-`|
|`0x04001fdb`|`ThepPhuongY`|`string`|`6`|`13531246`|`-`|
|`0x04001fdc`|`infoMong`|`InfoRaiMongDon [class:3296]`|`6`|`13531256`|`-`|
|`0x04001fdd`|`hXYjW9q2lbK2G4jYc1bD`|`CotChiuTaiMongDon [class:3624]`|`17`|`13531266`|`-`|

### [VM-126] Type `KieuTaoDaiMongBaoCoc` (Token `0x0200038b`, RowOffset `13383898`, Extends `0x01000011 (System.Enum)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 4 | Methods count: 0
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001fde`|`value__`|`i4`|`1542`|`13531276`|`-`|
|`0x04001fdf`|`GiuNguyenBaoMong`|`KieuTaoDaiMongBaoCoc [valuetype:3628]`|`32854`|`13531286`|`0`|
|`0x04001fe0`|`TaoBaoMongBaoNgoai`|`KieuTaoDaiMongBaoCoc [valuetype:3628]`|`32854`|`13531296`|`1`|
|`0x04001fe1`|`TaoBaoMongVac`|`KieuTaoDaiMongBaoCoc [valuetype:3628]`|`32854`|`13531306`|`2`|

### [VM-127] Type `Form_Update_Dai_Mong_Bao_Coc` (Token `0x0200038c`, RowOffset `13383916`, Extends `0x0100000f (System.Windows.Forms.Form)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 9 | Methods count: 10
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001fe2`|`GwH0k7EfPcB`|`System.Windows.Forms.TextBox [class:249]`|`33`|`13531316`|`-`|
|`0x04001fe3`|`Aj70kFAj2mZ`|`System.Windows.Forms.TextBox [class:249]`|`33`|`13531326`|`-`|
|`0x04001fe4`|`RKB0kv1NTvQ`|`System.Windows.Forms.TextBox [class:249]`|`33`|`13531336`|`-`|
|`0x04001fe5`|`Mb60k1qRyfu`|`System.Windows.Forms.ComboBox [class:377]`|`33`|`13531346`|`-`|
|`0x04001fe6`|`UIo0kMUt2Vd`|`System.Windows.Forms.Button [class:161]`|`33`|`13531356`|`-`|
|`0x04001fe7`|`Php0k9TmnPB`|`System.Windows.Forms.Button [class:161]`|`33`|`13531366`|`-`|
|`0x04001fe8`|`xgx0kKQK7a2`|`System.Collections.Generic.List`1 [class:217]<KieuTaoDaiMongBaoCoc [valuetype:3628]>`|`33`|`13531376`|`-`|
|`0x04001fe9`|`rfH0kNrO23U`|`bool`|`33`|`13531386`|`-`|
|`0x04001fea`|`cYOI1Hq2XmaKdAbceSPu`|`Form_Update_Dai_Mong_Bao_Coc [class:3632]`|`19`|`13531396`|`-`|

### [VM-128] Type `Kata_pro64_Cad2025.Form_VMBM_Light` (Token `0x02000390`, RowOffset `13383988`, Extends `0x0100000f (System.Windows.Forms.Form)`, Evidence `ev_549bfe33ecd78257ce27570bdd19331f304e770621ccd33a14059b374ac982e3`)
- Fields count: 37 | Methods count: 53
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001ff7`|`Bj78wp782kW`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`1`|`13531526`|`-`|
|`0x04001ff8`|`FJj8wwyV6xZ`|`Kata_pro64_Cad2025.VmbmLightEtabsMetadata [class:4672]`|`1`|`13531536`|`-`|
|`0x04001ff9`|`fju8w2BYyNu`|`System.Threading.CancellationTokenSource [class:661]`|`1`|`13531546`|`-`|
|`0x04001ffa`|`h9J8wXPoQ78`|`bool`|`1`|`13531556`|`-`|
|`0x04001ffb`|`wk58wi6dKxP`|`bool`|`1`|`13531566`|`-`|
|`0x04001ffc`|`mb48wnZuJh9`|`System.Windows.Forms.ComboBox [class:377]`|`33`|`13531576`|`-`|
|`0x04001ffd`|`m6K8w5iUNo4`|`System.Windows.Forms.Button [class:161]`|`33`|`13531586`|`-`|
|`0x04001ffe`|`tAU8wSmLg9b`|`System.Windows.Forms.Button [class:161]`|`33`|`13531596`|`-`|
|`0x04001fff`|`GKe8wjX3sAB`|`System.Windows.Forms.Label [class:245]`|`33`|`13531606`|`-`|
|`0x04002000`|`qJ58wxDM3Ha`|`bool`|`1`|`13531616`|`-`|
|`0x04002001`|`QLo8wDk7IR2`|`System.Windows.Forms.Button [class:161]`|`33`|`13531626`|`-`|
|`0x04002002`|`VN38wBlRXyb`|`System.Windows.Forms.Button [class:161]`|`33`|`13531636`|`-`|
|`0x04002003`|`uJ38wOwukEt`|`System.Windows.Forms.Button [class:161]`|`33`|`13531646`|`-`|
|`0x04002004`|`fIX8wVSR1kG`|`System.Windows.Forms.Button [class:161]`|`33`|`13531656`|`-`|
|`0x04002005`|`TRK8wa1kqcZ`|`System.Windows.Forms.Button [class:161]`|`33`|`13531666`|`-`|
|`0x04002006`|`S5C8wzgulxu`|`System.Windows.Forms.Button [class:161]`|`33`|`13531676`|`-`|
|`0x04002007`|`KBT82UM2TZt`|`System.Windows.Forms.Button [class:161]`|`33`|`13531686`|`-`|
|`0x04002008`|`fXH828Tc3rG`|`System.Windows.Forms.Label [class:245]`|`33`|`13531696`|`-`|
|`0x04002009`|`OdP82Et6EEE`|`System.Windows.Forms.Button [class:161]`|`33`|`13531706`|`-`|
|`0x0400200a`|`cTS820OdHQ1`|`System.Windows.Forms.Label [class:245]`|`33`|`13531716`|`-`|
|`0x0400200b`|`vDx82PYVATt`|`System.Windows.Forms.Label [class:245]`|`33`|`13531726`|`-`|
|`0x0400200c`|`GX1823TEfRf`|`System.Windows.Forms.Label [class:245]`|`33`|`13531736`|`-`|
|`0x0400200d`|`pUX82qlX4NN`|`System.Windows.Forms.ComboBox [class:377]`|`33`|`13531746`|`-`|
|`0x0400200e`|`rFY82AouOhj`|`System.Windows.Forms.ComboBox [class:377]`|`33`|`13531756`|`-`|
|`0x0400200f`|`hM682CGGXC9`|`System.Windows.Forms.ComboBox [class:377]`|`33`|`13531766`|`-`|
|`0x04002010`|`UAQ82mLHNQe`|`System.Windows.Forms.CheckedListBox [class:1049]`|`33`|`13531776`|`-`|
|`0x04002011`|`QIj82QUB5FE`|`System.Windows.Forms.DataGridView [class:117]`|`33`|`13531786`|`-`|
|`0x04002012`|`asf82kmJcjl`|`System.Windows.Forms.CheckBox [class:257]`|`33`|`13531796`|`-`|
|`0x04002013`|`VJ4824IMBII`|`System.Windows.Forms.DataGridView [class:117]`|`33`|`13531806`|`-`|
|`0x04002014`|`CLy82Y7yAaV`|`bool`|`1`|`13531816`|`-`|
|`0x04002015`|`zgu82oF4dXD`|`System.Windows.Forms.DataGridView [class:117]`|`33`|`13531826`|`-`|
|`0x04002016`|`kD0827yTs8g`|`Kata_pro64_Cad2025.VmbmLightPreview [class:3708]`|`33`|`13531836`|`-`|
|`0x04002017`|`GSY82FbBQ25`|`System.Windows.Forms.TextBox [class:249]`|`33`|`13531846`|`-`|
|`0x04002018`|`SAR82v2EydO`|`System.Windows.Forms.TableLayoutPanel [class:329]`|`33`|`13531856`|`-`|
|`0x04002019`|`wWY821Y8vrh`|`bool`|`1`|`13531866`|`-`|
|`0x0400201a`|`fYM82MslDd7`|`bool`|`1`|`13531876`|`-`|
|`0x0400201b`|`HiHnNPq4YDsTLEjHOIbh`|`Kata_pro64_Cad2025.Form_VMBM_Light [class:3648]`|`17`|`13531886`|`-`|

### [VM-129] Type `Kata_pro64_Cad2025.VmbmLightPreview` (Token `0x0200039f`, RowOffset `13384258`, Extends `0x01000018 (System.Windows.Forms.Control)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 15 | Methods count: 30
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x0400205c`|`QZk82b73lj3`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`1`|`13532536`|`-`|
|`0x0400205d`|`P7R82I2NqFX`|`string`|`1`|`13532546`|`-`|
|`0x0400205e`|`SnM82yyiVxb`|`System.Collections.Generic.List`1 [class:217]<string>`|`1`|`13532556`|`-`|
|`0x0400205f`|`D8I82RMA6bR`|`bool`|`1`|`13532566`|`-`|
|`0x04002060`|`Fka82gOuBYY`|`bool`|`1`|`13532576`|`-`|
|`0x04002061`|`FUs82hu3hN5`|`System.Collections.Generic.List`1 [class:217]<string>`|`1`|`13532586`|`-`|
|`0x04002062`|`xN582ZyC0h8`|`CapPickedEventHandler [class:3712]`|`1`|`13532596`|`-`|
|`0x04002063`|`U7082eW0IqY`|`BeamPickedEventHandler [class:3716]`|`1`|`13532606`|`-`|
|`0x04002064`|`ccq82L6we0s`|`CapNameEditedEventHandler [class:3720]`|`1`|`13532616`|`-`|
|`0x04002065`|`KGe82Txbnph`|`System.Collections.Generic.Dictionary`2 [class:389]<string,System.Drawing.PointF [valuetype:1609][]>`|`33`|`13532626`|`-`|
|`0x04002066`|`qwI82fgu75j`|`System.Collections.Generic.Dictionary`2 [class:389]<string,System.Drawing.RectangleF [valuetype:897]>`|`33`|`13532636`|`-`|
|`0x04002067`|`mXK82c50c94`|`System.Collections.Generic.Dictionary`2 [class:389]<string,System.Drawing.PointF [valuetype:1609][]>`|`33`|`13532646`|`-`|
|`0x04002068`|`vaR826BjXPi`|`System.Windows.Forms.TextBox [class:249]`|`33`|`13532656`|`-`|
|`0x04002069`|`OAs82W9DZFC`|`string`|`1`|`13532666`|`-`|
|`0x0400206a`|`y9a6EFq4FvfhGHCrDdDI`|`Kata_pro64_Cad2025.VmbmLightPreview [class:3708]`|`19`|`13532676`|`-`|

### [VM-130] Type `Kata_pro64_Cad2025.VmbmLightOptionsForm` (Token `0x020003a7`, RowOffset `13384402`, Extends `0x0100000f (System.Windows.Forms.Form)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 2 | Methods count: 4
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04002085`|`ubh82sa3U7i`|`System.Collections.Generic.Dictionary`2 [class:389]<string,System.Windows.Forms.TextBox [class:249]>`|`33`|`13532946`|`-`|
|`0x04002086`|`YtxN8eq4MHgwC3mwX3oZ`|`Kata_pro64_Cad2025.VmbmLightOptionsForm [class:3740]`|`17`|`13532956`|`-`|

### [VM-131] Type `Kata_pro64_Cad2025.VmbmLightShallowView` (Token `0x020003aa`, RowOffset `13384456`, Extends `0x01000013 (System.Windows.Forms.UserControl)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 12 | Methods count: 35
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04002092`|`DJV82nPnPPW`|`Kata_pro64_Cad2025.VmbmLightShallowSession [class:4336]`|`1`|`13533076`|`-`|
|`0x04002093`|`K3h825DLl2I`|`Kata_pro64_Cad2025.VmbmLightShallowCanvas [class:3796]`|`33`|`13533086`|`-`|
|`0x04002094`|`LJ582SpAZi9`|`System.Windows.Forms.DataGridView [class:117]`|`33`|`13533096`|`-`|
|`0x04002095`|`Nhr82j9vmsI`|`System.Windows.Forms.PropertyGrid [class:2057]`|`33`|`13533106`|`-`|
|`0x04002096`|`v3h82x0dtbi`|`System.Windows.Forms.PropertyGrid [class:2057]`|`33`|`13533116`|`-`|
|`0x04002097`|`FC682DPgOZh`|`System.Windows.Forms.Label [class:245]`|`33`|`13533126`|`-`|
|`0x04002098`|`EPj82BxDVWd`|`System.Collections.Generic.Stack`1 [class:2061]<string>`|`33`|`13533136`|`-`|
|`0x04002099`|`U8V82OWXnYM`|`System.Collections.Generic.Stack`1 [class:2061]<string>`|`33`|`13533146`|`-`|
|`0x0400209a`|`Tb282VaWIcd`|`string`|`1`|`13533156`|`-`|
|`0x0400209b`|`UUA82aawHas`|`bool`|`1`|`13533166`|`-`|
|`0x0400209c`|`p3V82zgEQp2`|`bool`|`1`|`13533176`|`-`|
|`0x0400209d`|`Y93ijqq4NMHW34W1tiTl`|`Kata_pro64_Cad2025.VmbmLightShallowView [class:3752]`|`17`|`13533186`|`-`|

### [VM-132] Type `Kata_pro64_Cad2025.VmbmLightFootingProperties` (Token `0x020003b2`, RowOffset `13384600`, Extends `0x01000019 (System.ComponentModel.CustomTypeDescriptor)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 3 | Methods count: 7
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x040020b9`|`Y698XUKkwi1`|`Kata_pro64_Cad2025.VmbmLightFootingInput [class:4328]`|`33`|`13533466`|`-`|
|`0x040020ba`|`k688X8OPUvj`|`Kata_pro64_Cad2025.VmbmLightShallowKind [valuetype:4324]`|`33`|`13533476`|`-`|
|`0x040020bb`|`vhVOu5q4yCIDEHmXky4v`|`Kata_pro64_Cad2025.VmbmLightFootingProperties [class:3784]`|`19`|`13533486`|`-`|

### [VM-133] Type `Kata_pro64_Cad2025.VmbmLightShallowSettingsProperties` (Token `0x020003b4`, RowOffset `13384636`, Extends `0x01000019 (System.ComponentModel.CustomTypeDescriptor)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 2 | Methods count: 7
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x040020be`|`ymJ8XEdQsQG`|`Kata_pro64_Cad2025.VmbmLightShallowSettings [class:4332]`|`33`|`13533516`|`-`|
|`0x040020bf`|`RJm19mq4hUa7LIjioKSO`|`Kata_pro64_Cad2025.VmbmLightShallowSettingsProperties [class:3792]`|`17`|`13533526`|`-`|

### [VM-134] Type `Kata_pro64_Cad2025.VmbmLightShallowCanvas` (Token `0x020003b5`, RowOffset `13384654`, Extends `0x01000018 (System.Windows.Forms.Control)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 12 | Methods count: 27
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x040020c0`|`ULM8XmfQcSI`|`Kata_pro64_Cad2025.VmbmLightShallowSession [class:4336]`|`1`|`13533536`|`-`|
|`0x040020c1`|`IUm8XQWfMp8`|`System.Collections.Generic.HashSet`1 [class:385]<string>`|`1`|`13533546`|`-`|
|`0x040020c2`|`uKC8XkqfMML`|`PickedEventHandler [class:3800]`|`1`|`13533556`|`-`|
|`0x040020c3`|`TGL8X4gL53k`|`DimensionEditedEventHandler [class:3804]`|`1`|`13533566`|`-`|
|`0x040020c4`|`LhV8XY10VSh`|`r8`|`1`|`13533576`|`-`|
|`0x040020c5`|`Kyk8XoqKoig`|`System.Drawing.PointF [valuetype:1609]`|`1`|`13533586`|`-`|
|`0x040020c6`|`MOI8X7k7frF`|`Kata_Class_Lib_Revit.diem [class:85]`|`1`|`13533596`|`-`|
|`0x040020c7`|`hs28XFa8a0t`|`System.Drawing.Point [valuetype:173]`|`1`|`13533606`|`-`|
|`0x040020c8`|`cJk8Xv6RyPg`|`bool`|`1`|`13533616`|`-`|
|`0x040020c9`|`bGV8X1j6j8J`|`System.Collections.Generic.List`1 [class:217]<System.Tuple`4 [class:1041]<System.Drawing.RectangleF [valuetype:897],string,string,r8>>`|`33`|`13533626`|`-`|
|`0x040020ca`|`jao8XMGu71j`|`System.Windows.Forms.ToolTip [class:649]`|`33`|`13533636`|`-`|
|`0x040020cb`|`EwULnoq4Lg6cX7ycJ2kN`|`Kata_pro64_Cad2025.VmbmLightShallowCanvas [class:3796]`|`17`|`13533646`|`-`|

### [VM-135] Type `Kata_pro64_Cad2025.VmbmLightCad` (Token `0x020003c1`, RowOffset `13384870`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 1 | Methods count: 31
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x040020e1`|`Gq3Eaqq4cQMHBOk22ecS`|`Kata_pro64_Cad2025.VmbmLightCad [class:3844]`|`17`|`13533866`|`-`|

### [VM-136] Type `Kata_pro64_Cad2025.VmbmLightCommand` (Token `0x020003d7`, RowOffset `13385266`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 2 | Methods count: 4
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x0400213b`|`CSR8XcObNgm`|`Kata_pro64_Cad2025.Form_VMBM_Light [class:3648]`|`17`|`13534766`|`-`|
|`0x0400213c`|`AkofhEq4sNIY7NdqLRN6`|`Kata_pro64_Cad2025.VmbmLightCommand [class:3932]`|`19`|`13534776`|`-`|

### [VM-137] Type `Kata_pro64_Cad2025.VmbmLightSteelAxis` (Token `0x020003d8`, RowOffset `13385284`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 4 | Methods count: 10
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x0400213d`|`Lry8X6WbOGD`|`string`|`1`|`13534786`|`-`|
|`0x0400213e`|`MGs8XWS6LmI`|`r8`|`1`|`13534796`|`-`|
|`0x0400213f`|`diU8XscSdb7`|`r8`|`1`|`13534806`|`-`|
|`0x04002140`|`ATTInoq4JD4CXplkN4uo`|`Kata_pro64_Cad2025.VmbmLightSteelAxis [class:3936]`|`17`|`13534816`|`-`|

### [VM-138] Type `Kata_pro64_Cad2025.VmbmLightEtabs` (Token `0x020003d9`, RowOffset `13385302`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 7 | Methods count: 51
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04002141`|`NXK8i7Y7wVn`|`System.Runtime.CompilerServices.ConditionalWeakTable`2 [class:2097]<Kata_pro64_Cad2025.VmbmLightSession [class:4168],zryyeR0kbdPnTQi4daUu [class:3944]>`|`49`|`13534826`|`-`|
|`0x04002142`|`IA88iFFj2Rt`|`System.Threading.CancellationToken [valuetype:1413]`|`1`|`13534836`|`-`|
|`0x04002143`|`fmk8ivrhhUF`|`CSiAPIv1.cSapModel [class:2025]`|`1`|`13534846`|`-`|
|`0x04002144`|`pt48i1irUxe`|`System.Collections.Generic.Dictionary`2 [class:389]<string,Kata_Class_Lib_Revit.diem [class:85]>`|`33`|`13534856`|`-`|
|`0x04002145`|`Jvd8iMTyERb`|`System.Collections.Generic.HashSet`1 [class:385]<string>`|`33`|`13534866`|`-`|
|`0x04002146`|`heY8i97VUZA`|`System.Action`3 [class:2049]<Kata_pro64_Cad2025.VmbmLightSession [class:4168],string,CSiAPIv1.cSapModel [class:2025]>`|`33`|`13534876`|`-`|
|`0x04002147`|`u29qqwq4lSQ7PGxj2tWS`|`Kata_pro64_Cad2025.VmbmLightEtabs [class:3940]`|`17`|`13534886`|`-`|

### [VM-139] Type `Kata_pro64_Cad2025.VmbmLightSlabCapInput` (Token `0x020003f5`, RowOffset `13385806`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 4 | Methods count: 10
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x040021bb`|`GQM8iKTvTQQ`|`Polygon [class:89]`|`1`|`13536046`|`-`|
|`0x040021bc`|`Udj8iNnrIbb`|`System.Collections.Generic.List`1 [class:217]<CocChiuTai [class:3568]>`|`1`|`13536056`|`-`|
|`0x040021bd`|`bly8ibQlv88`|`System.Collections.Generic.List`1 [class:217]<i8>`|`1`|`13536066`|`-`|
|`0x040021be`|`hgVcLBq4pGuIgWS64Eqa`|`Kata_pro64_Cad2025.VmbmLightSlabCapInput [class:4052]`|`19`|`13536076`|`-`|

### [VM-140] Type `Kata_pro64_Cad2025.VmbmLightFoundationInput` (Token `0x020003f6`, RowOffset `13385824`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 3 | Methods count: 19
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x040021bf`|`CgT8iesVlsV`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightSlabCapInput [class:4052]>`|`1`|`13536086`|`-`|
|`0x040021c0`|`N2E8iLmLVPk`|`System.Collections.Generic.List`1 [class:217]<i8>`|`1`|`13536096`|`-`|
|`0x040021c1`|`sLLe0Wq4XPKAjpfDYAFC`|`Kata_pro64_Cad2025.VmbmLightFoundationInput [class:4056]`|`17`|`13536106`|`-`|

### [VM-141] Type `Kata_pro64_Cad2025.VmbmLightLayoutIdentity` (Token `0x02000402`, RowOffset `13386040`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 2 | Methods count: 14
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x040021de`|`Tolerance`|`r8`|`32854`|`13536396`|`0.01`|
|`0x040021df`|`sh1CWJq45mkGPPoekISs`|`Kata_pro64_Cad2025.VmbmLightLayoutIdentity [class:4104]`|`19`|`13536406`|`-`|

### [VM-142] Type `Kata_pro64_Cad2025.VmbmLightSourceFiles` (Token `0x02000409`, RowOffset `13386166`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 1 | Methods count: 6
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x040021fc`|`lUvNtjq4xJdStp3ZsNov`|`Kata_pro64_Cad2025.VmbmLightSourceFiles [class:4132]`|`19`|`13536696`|`-`|

### [VM-143] Type `Kata_pro64_Cad2025.VmbmLightOptions` (Token `0x0200040c`, RowOffset `13386220`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 42 | Methods count: 87
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04002204`|`lpN8i6f9L3S`|`bool`|`1`|`13536776`|`-`|
|`0x04002205`|`Hmq8iWKX6NV`|`System.Collections.Generic.List`1 [class:217]<string>`|`1`|`13536786`|`-`|
|`0x04002206`|`pCi8is55AIC`|`string`|`1`|`13536796`|`-`|
|`0x04002207`|`bII8itgCqiL`|`r8`|`1`|`13536806`|`-`|
|`0x04002208`|`MWw8idg18N9`|`string`|`1`|`13536816`|`-`|
|`0x04002209`|`NQD8iJqC72B`|`i4`|`1`|`13536826`|`-`|
|`0x0400220a`|`qRl8ir7Hta1`|`r8`|`1`|`13536836`|`-`|
|`0x0400220b`|`xXc8iGbnIeM`|`r8`|`1`|`13536846`|`-`|
|`0x0400220c`|`BQW8ilgJLHT`|`r8`|`1`|`13536856`|`-`|
|`0x0400220d`|`l2c8iunjPLJ`|`r8`|`1`|`13536866`|`-`|
|`0x0400220e`|`lbm8iH4VYu9`|`i4`|`1`|`13536876`|`-`|
|`0x0400220f`|`sb18ipLG21v`|`r8`|`1`|`13536886`|`-`|
|`0x04002210`|`SEZ8iw27LkV`|`System.Collections.Generic.List`1 [class:217]<string>`|`1`|`13536896`|`-`|
|`0x04002211`|`TuW8i2ks1oW`|`string`|`1`|`13536906`|`-`|
|`0x04002212`|`NvT8iXDnOXR`|`string`|`1`|`13536916`|`-`|
|`0x04002213`|`JlE8iiUymG4`|`i4`|`1`|`13536926`|`-`|
|`0x04002214`|`HfI8inVs9px`|`string`|`1`|`13536936`|`-`|
|`0x04002215`|`YXH8i5N0xNn`|`r8`|`1`|`13536946`|`-`|
|`0x04002216`|`oU98iS2m4Vc`|`string`|`1`|`13536956`|`-`|
|`0x04002217`|`hja8ijOUTXb`|`i4`|`1`|`13536966`|`-`|
|`0x04002218`|`sR28ixW4RmS`|`i4`|`1`|`13536976`|`-`|
|`0x04002219`|`cnS8iDHmajw`|`i4`|`1`|`13536986`|`-`|
|`0x0400221a`|`p7y8iBOwym5`|`r8`|`1`|`13536996`|`-`|
|`0x0400221b`|`MIr8iOBXliw`|`r8`|`1`|`13537006`|`-`|
|`0x0400221c`|`xbZ8iVhIKoP`|`r8`|`1`|`13537016`|`-`|
|`0x0400221d`|`j0Y8iaDTTie`|`r8`|`1`|`13537026`|`-`|
|`0x0400221e`|`hDs8izR2P7E`|`r8`|`1`|`13537036`|`-`|
|`0x0400221f`|`teI8nUbaTYh`|`r8`|`1`|`13537046`|`-`|
|`0x04002220`|`kKi8n8FIkBf`|`string`|`1`|`13537056`|`-`|
|`0x04002221`|`nyb8nE6vNJk`|`r8`|`1`|`13537066`|`-`|
|`0x04002222`|`vUS8n0hVN5m`|`r8`|`1`|`13537076`|`-`|
|`0x04002223`|`TLc8nPCGlqH`|`r8`|`1`|`13537086`|`-`|
|`0x04002224`|`wPW8n3Z0D0N`|`r8`|`1`|`13537096`|`-`|
|`0x04002225`|`LvT8nqdKcJK`|`r8`|`1`|`13537106`|`-`|
|`0x04002226`|`QLK8nAKLfin`|`r8`|`1`|`13537116`|`-`|
|`0x04002227`|`gEp8nClXac2`|`bool`|`1`|`13537126`|`-`|
|`0x04002228`|`Ymv8nmo3ZlF`|`string`|`1`|`13537136`|`-`|
|`0x04002229`|`wYr8nQK9Ies`|`bool`|`1`|`13537146`|`-`|
|`0x0400222a`|`PhL8nkKY1eB`|`string`|`1`|`13537156`|`-`|
|`0x0400222b`|`Cn58n4PQpIi`|`string`|`1`|`13537166`|`-`|
|`0x0400222c`|`VWM8nY6gfVR`|`string`|`1`|`13537176`|`-`|
|`0x0400222d`|`gpacu6q4OOMWOI5tjMh0`|`Kata_pro64_Cad2025.VmbmLightOptions [class:4144]`|`17`|`13537186`|`-`|

### [VM-144] Type `Kata_pro64_Cad2025.VmbmLightSupport` (Token `0x0200040d`, RowOffset `13386238`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 6 | Methods count: 14
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x0400222e`|`UPI8no7sSnX`|`string`|`1`|`13537196`|`-`|
|`0x0400222f`|`p1c8n79tquY`|`string`|`1`|`13537206`|`-`|
|`0x04002230`|`Ar08nFl9iOy`|`Polygon [class:89]`|`1`|`13537216`|`-`|
|`0x04002231`|`RRJ8nv89Xg1`|`System.Collections.Generic.List`1 [class:217]<string>`|`1`|`13537226`|`-`|
|`0x04002232`|`qG08n16CIcg`|`r8`|`1`|`13537236`|`-`|
|`0x04002233`|`eH3LeTq4zFAi1TPeWnCk`|`Kata_pro64_Cad2025.VmbmLightSupport [class:4148]`|`19`|`13537246`|`-`|

### [VM-145] Type `Kata_pro64_Cad2025.VmbmLightCap` (Token `0x0200040e`, RowOffset `13386256`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 11 | Methods count: 26
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04002234`|`Ykh8nMs5PUH`|`string`|`1`|`13537256`|`-`|
|`0x04002235`|`lM78n9ueFFM`|`string`|`1`|`13537266`|`-`|
|`0x04002236`|`XP18nKZ5cAC`|`bool`|`1`|`13537276`|`-`|
|`0x04002237`|`uKN8nNeIQbY`|`bool`|`1`|`13537286`|`-`|
|`0x04002238`|`w8w8nbTjER5`|`System.Collections.Generic.List`1 [class:217]<string>`|`1`|`13537296`|`-`|
|`0x04002239`|`o7d8nImARbd`|`InfoCotCocGenMong [class:3584]`|`1`|`13537306`|`-`|
|`0x0400223a`|`o7v8nygY4Nc`|`InfoCotCocGenMong [class:3584]`|`1`|`13537316`|`-`|
|`0x0400223b`|`v1N8nRuFK7X`|`bool`|`1`|`13537326`|`-`|
|`0x0400223c`|`SMF8ngo7Pq0`|`i8`|`1`|`13537336`|`-`|
|`0x0400223d`|`T178nhOoI0i`|`Kata_pro64_Cad2025.VmbmLightCapCheck [class:4160]`|`1`|`13537346`|`-`|
|`0x0400223e`|`gOs7sFqYEuNU0cTiyFtN`|`Kata_pro64_Cad2025.VmbmLightCap [class:4152]`|`17`|`13537356`|`-`|

### [VM-146] Type `Kata_pro64_Cad2025.VmbmLightPileResult` (Token `0x0200040f`, RowOffset `13386274`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 11 | Methods count: 24
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x0400223f`|`Bjo8nZ2mnfy`|`string`|`1`|`13537366`|`-`|
|`0x04002240`|`kHm8neHT1Pg`|`string`|`1`|`13537376`|`-`|
|`0x04002241`|`shL8nL2ooWP`|`string`|`1`|`13537386`|`-`|
|`0x04002242`|`UR78nTd9icH`|`string`|`1`|`13537396`|`-`|
|`0x04002243`|`hyl8nfQcH8d`|`string`|`1`|`13537406`|`-`|
|`0x04002244`|`hir8ncHUFLV`|`r8`|`1`|`13537416`|`-`|
|`0x04002245`|`YNs8n6abpWV`|`r8`|`1`|`13537426`|`-`|
|`0x04002246`|`Wcl8nWahxpD`|`r8`|`1`|`13537436`|`-`|
|`0x04002247`|`uhB8nsLFLUG`|`r8`|`1`|`13537446`|`-`|
|`0x04002248`|`WCN8ntkDUCe`|`r8`|`1`|`13537456`|`-`|
|`0x04002249`|`UiVnqiqY3qleCR6oFZ7e`|`Kata_pro64_Cad2025.VmbmLightPileResult [class:4156]`|`19`|`13537466`|`-`|

### [VM-147] Type `Kata_pro64_Cad2025.VmbmLightCapCheck` (Token `0x02000410`, RowOffset `13386292`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 7 | Methods count: 17
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x0400224a`|`TX48ndR8uiJ`|`bool`|`1`|`13537476`|`-`|
|`0x0400224b`|`lJo8nJvECxp`|`bool`|`1`|`13537486`|`-`|
|`0x0400224c`|`m3P8nrdZ87r`|`r8`|`1`|`13537496`|`-`|
|`0x0400224d`|`fF88nG3YSmI`|`r8`|`1`|`13537506`|`-`|
|`0x0400224e`|`lqb8nl7TLoy`|`string`|`1`|`13537516`|`-`|
|`0x0400224f`|`XNX8nu2giUH`|`string`|`1`|`13537526`|`-`|
|`0x04002250`|`WdZXLCqYCuXaD4SCVwjL`|`Kata_pro64_Cad2025.VmbmLightCapCheck [class:4160]`|`19`|`13537536`|`-`|

### [VM-148] Type `Kata_pro64_Cad2025.VmbmLightIteration` (Token `0x02000411`, RowOffset `13386310`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 18 | Methods count: 38
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04002251`|`DfX8nHTFCj4`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightBeam [class:4200]>`|`1`|`13537546`|`-`|
|`0x04002252`|`Hgx8npQd3uA`|`string`|`1`|`13537556`|`-`|
|`0x04002253`|`blx8nwkmk1j`|`bool`|`1`|`13537566`|`-`|
|`0x04002254`|`QxC8n2x4vVI`|`string`|`1`|`13537576`|`-`|
|`0x04002255`|`zNY8nXG6MjZ`|`System.Collections.Generic.List`1 [class:217]<string>`|`1`|`13537586`|`-`|
|`0x04002256`|`k5T8ni2J3d1`|`string`|`1`|`13537596`|`-`|
|`0x04002257`|`noQ8nnlHwMR`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightStripBinding [class:5652]>`|`1`|`13537606`|`-`|
|`0x04002258`|`bml8n5V56XY`|`i4`|`1`|`13537616`|`-`|
|`0x04002259`|`M098nSkMtah`|`string`|`1`|`13537626`|`-`|
|`0x0400225a`|`aL08njqeS72`|`i4`|`1`|`13537636`|`-`|
|`0x0400225b`|`GMs8nxdhh5h`|`i4`|`1`|`13537646`|`-`|
|`0x0400225c`|`Gp28nDNtUW7`|`i4`|`1`|`13537656`|`-`|
|`0x0400225d`|`oRQ8nBaN2AV`|`r8`|`1`|`13537666`|`-`|
|`0x0400225e`|`UOX8nOGdo4N`|`r8`|`1`|`13537676`|`-`|
|`0x0400225f`|`vvY8nVlQbfZ`|`string`|`1`|`13537686`|`-`|
|`0x04002260`|`aCM8naWavog`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightPileResult [class:4156]>`|`1`|`13537696`|`-`|
|`0x04002261`|`P1Q8nzESICU`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightCap [class:4152]>`|`1`|`13537706`|`-`|
|`0x04002262`|`NDx2YlqYkp0rlURIXLwc`|`Kata_pro64_Cad2025.VmbmLightIteration [class:4164]`|`17`|`13537716`|`-`|

### [VM-149] Type `Kata_pro64_Cad2025.VmbmLightSession` (Token `0x02000412`, RowOffset `13386328`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 56 | Methods count: 117
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04002263`|`cn9858scW7s`|`string`|`1`|`13537726`|`-`|
|`0x04002264`|`K9j85ExuWG8`|`string`|`1`|`13537736`|`-`|
|`0x04002265`|`BGd850MSkyq`|`string`|`1`|`13537746`|`-`|
|`0x04002266`|`rcl85PuYEiP`|`string`|`1`|`13537756`|`-`|
|`0x04002267`|`qBS853Avb9v`|`string`|`1`|`13537766`|`-`|
|`0x04002268`|`aXS85qf8bc8`|`string`|`1`|`13537776`|`-`|
|`0x04002269`|`N9d85AXqabr`|`string`|`1`|`13537786`|`-`|
|`0x0400226a`|`Div85CoOM78`|`i4`|`1`|`13537796`|`-`|
|`0x0400226b`|`ufZ85mvxDS5`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightBeam [class:4200]>`|`1`|`13537806`|`-`|
|`0x0400226c`|`rfh85QeAHyN`|`string`|`1`|`13537816`|`-`|
|`0x0400226d`|`qrx85kxrmpg`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightCadBeamBinding [class:5648]>`|`1`|`13537826`|`-`|
|`0x0400226e`|`D5q854xWejW`|`bool`|`1`|`13537836`|`-`|
|`0x0400226f`|`eb385YV517Q`|`string`|`1`|`13537846`|`-`|
|`0x04002270`|`qLp85ojFv1n`|`string`|`1`|`13537856`|`-`|
|`0x04002271`|`HuW8577XiK9`|`string`|`1`|`13537866`|`-`|
|`0x04002272`|`aZn85FjPdpf`|`string`|`1`|`13537876`|`-`|
|`0x04002273`|`tpQ85vCWlus`|`string`|`1`|`13537886`|`-`|
|`0x04002274`|`fCD8517C8Qf`|`string`|`1`|`13537896`|`-`|
|`0x04002275`|`FNL85MVPD6K`|`bool`|`1`|`13537906`|`-`|
|`0x04002276`|`P8I859MVOqk`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightCadBinding [class:5644]>`|`1`|`13537916`|`-`|
|`0x04002277`|`sA585KdN9lE`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightCap [class:4152]>`|`1`|`13537926`|`-`|
|`0x04002278`|`qRd85NRJarj`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightStripBinding [class:5652]>`|`1`|`13537936`|`-`|
|`0x04002279`|`UVU85bU1RFu`|`string`|`1`|`13537946`|`-`|
|`0x0400227a`|`YQl85IgnweR`|`string`|`1`|`13537956`|`-`|
|`0x0400227b`|`dvm85yGrEcC`|`string`|`1`|`13537966`|`-`|
|`0x0400227c`|`IpN85RFt7bJ`|`string`|`1`|`13537976`|`-`|
|`0x0400227d`|`Kdi85gG4mnt`|`string`|`1`|`13537986`|`-`|
|`0x0400227e`|`p9R85hATLV1`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightSteelAxis [class:3936]>`|`1`|`13537996`|`-`|
|`0x0400227f`|`SaI85ZjCMcK`|`r8`|`1`|`13538006`|`-`|
|`0x04002280`|`GUR85erAnZ6`|`r8`|`1`|`13538016`|`-`|
|`0x04002281`|`aKM85Lk2ixt`|`Kata_pro64_Cad2025.VmbmLightOptions [class:4144]`|`1`|`13538026`|`-`|
|`0x04002282`|`kua85T84PJ3`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.info_CurveGrid [class:997]>`|`1`|`13538036`|`-`|
|`0x04002283`|`h7i85fv00Rv`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Info_Beam3D [class:481]>`|`1`|`13538046`|`-`|
|`0x04002284`|`eiw85cPt7Bl`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Info_Slab3D [class:1613]>`|`1`|`13538056`|`-`|
|`0x04002285`|`fKg856gABjk`|`System.Collections.Generic.List`1 [class:217]<CocChiuTai [class:3568]>`|`1`|`13538066`|`-`|
|`0x04002286`|`VSG85WUDNmC`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightSupport [class:4148]>`|`1`|`13538076`|`-`|
|`0x04002287`|`h5S85svV7X1`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightCap [class:4152]>`|`1`|`13538086`|`-`|
|`0x04002288`|`ztW85torGy9`|`Polygon [class:89]`|`1`|`13538096`|`-`|
|`0x04002289`|`E4V85deNhGB`|`Polygon [class:89]`|`1`|`13538106`|`-`|
|`0x0400228a`|`rw285JrADZG`|`System.Collections.Generic.List`1 [class:217]<i8>`|`1`|`13538116`|`-`|
|`0x0400228b`|`A1s85rwcxqy`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightIteration [class:4164]>`|`1`|`13538126`|`-`|
|`0x0400228c`|`FL485GZ0hjf`|`System.Collections.Generic.List`1 [class:217]<string>`|`1`|`13538136`|`-`|
|`0x0400228d`|`GRB85lPlvBU`|`string`|`1`|`13538146`|`-`|
|`0x0400228e`|`U5d85uUl8uM`|`bool`|`1`|`13538156`|`-`|
|`0x0400228f`|`ldx85HktwVI`|`bool`|`1`|`13538166`|`-`|
|`0x04002290`|`yFp85pSX3Dr`|`bool`|`1`|`13538176`|`-`|
|`0x04002291`|`y1I85wAMfyL`|`bool`|`1`|`13538186`|`-`|
|`0x04002292`|`NH9852DTYwL`|`System.Collections.Generic.List`1 [class:217]<i8>`|`1`|`13538196`|`-`|
|`0x04002293`|`LNa85XaJl9u`|`System.Collections.Generic.List`1 [class:217]<i8>`|`1`|`13538206`|`-`|
|`0x04002294`|`SBI85ikfYbl`|`string`|`1`|`13538216`|`-`|
|`0x04002295`|`vVK85n9Nhv0`|`r8`|`1`|`13538226`|`-`|
|`0x04002296`|`PcB855wI862`|`r8`|`1`|`13538236`|`-`|
|`0x04002297`|`kEs85SDNbaN`|`string`|`1`|`13538246`|`-`|
|`0x04002298`|`xmx85jyvEUD`|`System.Collections.Generic.List`1 [class:217]<string>`|`1`|`13538256`|`-`|
|`0x04002299`|`QhH85xndRwl`|`bool`|`1`|`13538266`|`-`|
|`0x0400229a`|`cn1821qYodYUHjvsFTNj`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`19`|`13538276`|`-`|

### [VM-150] Type `Kata_pro64_Cad2025.VmbmLightCheck` (Token `0x02000415`, RowOffset `13386382`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 1 | Methods count: 10
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x040022b3`|`Wj6TTDqYvickaMcUcTYB`|`Kata_pro64_Cad2025.VmbmLightCheck [class:4180]`|`17`|`13538526`|`-`|

### [VM-151] Type `Kata_pro64_Cad2025.VmbmLightBeam` (Token `0x0200041a`, RowOffset `13386472`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 11 | Methods count: 27
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x040022c5`|`lgE85D7YKiP`|`string`|`1`|`13538706`|`-`|
|`0x040022c6`|`Lvl85B3HO2a`|`string`|`1`|`13538716`|`-`|
|`0x040022c7`|`xiS85OyNZp5`|`string`|`1`|`13538726`|`-`|
|`0x040022c8`|`ob485V4T8JS`|`r8`|`1`|`13538736`|`-`|
|`0x040022c9`|`zUK85aacvOd`|`r8`|`1`|`13538746`|`-`|
|`0x040022ca`|`sNe85zC4Wog`|`r8`|`1`|`13538756`|`-`|
|`0x040022cb`|`kDE8SUoNUgQ`|`r8`|`1`|`13538766`|`-`|
|`0x040022cc`|`GV78S818Ebu`|`bool`|`1`|`13538776`|`-`|
|`0x040022cd`|`L8A8SE9KJZf`|`System.Collections.Generic.List`1 [class:217]<string>`|`1`|`13538786`|`-`|
|`0x040022ce`|`HfN8S0CHmvr`|`Kata_pro64_Cad2025.VmbmLightBeamCheck [class:4204]`|`1`|`13538796`|`-`|
|`0x040022cf`|`UDhyWGqY9YMiA4vQxYqO`|`Kata_pro64_Cad2025.VmbmLightBeam [class:4200]`|`19`|`13538806`|`-`|

### [VM-152] Type `Kata_pro64_Cad2025.VmbmLightBeamCheck` (Token `0x0200041b`, RowOffset `13386490`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 21 | Methods count: 45
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x040022d0`|`bgB8SPbD4pb`|`bool`|`1`|`13538816`|`-`|
|`0x040022d1`|`mEp8S3BROXe`|`bool`|`1`|`13538826`|`-`|
|`0x040022d2`|`okN8SqL82pc`|`bool`|`1`|`13538836`|`-`|
|`0x040022d3`|`WZa8SACeyea`|`bool`|`1`|`13538846`|`-`|
|`0x040022d4`|`VQA8SCQqA4k`|`bool`|`1`|`13538856`|`-`|
|`0x040022d5`|`git8Smqsuo3`|`bool`|`1`|`13538866`|`-`|
|`0x040022d6`|`tcZ8SQwISlU`|`r8`|`1`|`13538876`|`-`|
|`0x040022d7`|`Ecx8SkUjjw0`|`r8`|`1`|`13538886`|`-`|
|`0x040022d8`|`S1p8S449KFE`|`r8`|`1`|`13538896`|`-`|
|`0x040022d9`|`AgC8SYRVAOq`|`r8`|`1`|`13538906`|`-`|
|`0x040022da`|`Van8SoQWwoR`|`r8`|`1`|`13538916`|`-`|
|`0x040022db`|`kPr8S7KMGIn`|`string`|`1`|`13538926`|`-`|
|`0x040022dc`|`sm28SF67k1I`|`string`|`1`|`13538936`|`-`|
|`0x040022dd`|`dRf8Sv0sJPh`|`string`|`1`|`13538946`|`-`|
|`0x040022de`|`C9M8S1bqXld`|`r8`|`1`|`13538956`|`-`|
|`0x040022df`|`p0Z8SMHcXkq`|`r8`|`1`|`13538966`|`-`|
|`0x040022e0`|`iSV8S94MHpx`|`string`|`1`|`13538976`|`-`|
|`0x040022e1`|`MAX8SKwdvoa`|`string`|`1`|`13538986`|`-`|
|`0x040022e2`|`KTP8SNYUAIT`|`string`|`1`|`13538996`|`-`|
|`0x040022e3`|`NEq8Sbx0UJG`|`string`|`1`|`13539006`|`-`|
|`0x040022e4`|`HiUcQ3qYbdye8Uo5sxZY`|`Kata_pro64_Cad2025.VmbmLightBeamCheck [class:4204]`|`17`|`13539016`|`-`|

### [VM-153] Type `Kata_pro64_Cad2025.VmbmLightBeamDesign` (Token `0x0200041c`, RowOffset `13386508`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 1 | Methods count: 21
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x040022e5`|`w5CNbWqYRHddS52tC8oc`|`Kata_pro64_Cad2025.VmbmLightBeamDesign [class:4208]`|`17`|`13539026`|`-`|

### [VM-154] Type `Kata_pro64_Cad2025.VmbmLightStripTypeConverter` (Token `0x0200042a`, RowOffset `13386760`, Extends `0x0100001a (System.ComponentModel.StringConverter)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 1 | Methods count: 7
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04002335`|`X8ds6hqYZctny2dlxFWr`|`Kata_pro64_Cad2025.VmbmLightStripTypeConverter [class:4264]`|`17`|`13539826`|`-`|

### [VM-155] Type `Kata_pro64_Cad2025.VmbmLightRebarConverter` (Token `0x0200042b`, RowOffset `13386778`, Extends `0x0200042a (Kata_pro64_Cad2025.VmbmLightStripTypeConverter)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 1 | Methods count: 7
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04002336`|`CcF7q5qYTwiMg4ASx5rY`|`Kata_pro64_Cad2025.VmbmLightRebarConverter [class:4268]`|`19`|`13539836`|`-`|

### [VM-156] Type `Kata_pro64_Cad2025.VmbmLightShallowExportRequest` (Token `0x0200042d`, RowOffset `13386814`, Extends `0x0200042e (Kata_pro64_Cad2025.EtabsModelExportRequest)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 1 | Methods count: 4
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x0400233a`|`NwtGC9qY6rIbJvmkkykU`|`Kata_pro64_Cad2025.VmbmLightShallowExportRequest [class:4276]`|`17`|`13539876`|`-`|

### [VM-157] Type `Kata_pro64_Cad2025.VmbmLightNewPileExport` (Token `0x0200042f`, RowOffset `13386850`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 1 | Methods count: 5
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04002349`|`TL9WlvqYr7kkhrKFGoJS`|`Kata_pro64_Cad2025.VmbmLightNewPileExport [class:4284]`|`19`|`13540026`|`-`|

### [VM-158] Type `Kata_pro64_Cad2025.VmbmLightLegacyExportScope` (Token `0x02000430`, RowOffset `13386868`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 6 | Methods count: 5
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x0400234a`|`meJ8Sd3aCVR`|`System.Collections.Generic.List`1 [class:217]<Q3Ot6d0kOVAjjx21nyR7 [class:4292]>`|`33`|`13540036`|`-`|
|`0x0400234b`|`G7M8SJbB6M3`|`r8`|`33`|`13540046`|`-`|
|`0x0400234c`|`QoI8SrTTAxo`|`r8`|`33`|`13540056`|`-`|
|`0x0400234d`|`F5h8SGlQnuQ`|`bool`|`33`|`13540066`|`-`|
|`0x0400234e`|`ivA8SlXBH6k`|`System.Collections.Generic.List`1 [class:217]<System.Tuple`2 [class:285]<System.Windows.Forms.Control [class:97],string>>`|`33`|`13540076`|`-`|
|`0x0400234f`|`crYefjqYuHqVQ6irQkpy`|`Kata_pro64_Cad2025.VmbmLightLegacyExportScope [class:4288]`|`19`|`13540086`|`-`|

### [VM-159] Type `Kata_pro64_Cad2025.VmbmLightShallowExport` (Token `0x02000433`, RowOffset `13386922`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 1 | Methods count: 6
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04002357`|`nTJ4IaqYwRxs9yJS77Lg`|`Kata_pro64_Cad2025.VmbmLightShallowExport [class:4300]`|`17`|`13540166`|`-`|

### [VM-160] Type `Kata_pro64_Cad2025.VmbmLightExistingPileExport` (Token `0x02000435`, RowOffset `13386958`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 1 | Methods count: 8
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x0400235a`|`rxZRG7qYih1d50mUPATi`|`Kata_pro64_Cad2025.VmbmLightExistingPileExport [class:4308]`|`17`|`13540196`|`-`|

### [VM-161] Type `Kata_pro64_Cad2025.VmbmLightShallowExportForm` (Token `0x02000436`, RowOffset `13386976`, Extends `0x0100000f (System.Windows.Forms.Form)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 9 | Methods count: 9
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x0400235b`|`lQn8SwXGEaZ`|`System.Windows.Forms.ComboBox [class:377]`|`33`|`13540206`|`-`|
|`0x0400235c`|`VEQ8S2Xlqhq`|`System.Windows.Forms.ComboBox [class:377]`|`33`|`13540216`|`-`|
|`0x0400235d`|`jwd8SXWCNGp`|`System.Windows.Forms.ComboBox [class:377]`|`33`|`13540226`|`-`|
|`0x0400235e`|`lTG8SiWPqiD`|`System.Windows.Forms.ComboBox [class:377]`|`33`|`13540236`|`-`|
|`0x0400235f`|`NXR8SndNeGV`|`System.Windows.Forms.ComboBox [class:377]`|`33`|`13540246`|`-`|
|`0x04002360`|`Lss8S5jvunJ`|`System.Windows.Forms.ComboBox [class:377]`|`33`|`13540256`|`-`|
|`0x04002361`|`zpi8SSngSmt`|`System.Windows.Forms.CheckBox [class:257]`|`33`|`13540266`|`-`|
|`0x04002362`|`vGY8SjLgf10`|`System.Windows.Forms.CheckBox [class:257]`|`33`|`13540276`|`-`|
|`0x04002363`|`CIXk84qYSxa5HMFQMpUc`|`Kata_pro64_Cad2025.VmbmLightShallowExportForm [class:4312]`|`19`|`13540286`|`-`|

### [VM-162] Type `Kata_pro64_Cad2025.VmbmLightShallowKind` (Token `0x02000439`, RowOffset `13387030`, Extends `0x01000011 (System.Enum)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 3 | Methods count: 0
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x0400236b`|`value__`|`i4`|`1542`|`13540366`|`-`|
|`0x0400236c`|`Isolated`|`Kata_pro64_Cad2025.VmbmLightShallowKind [valuetype:4324]`|`32854`|`13540376`|`0`|
|`0x0400236d`|`Strip`|`Kata_pro64_Cad2025.VmbmLightShallowKind [valuetype:4324]`|`32854`|`13540386`|`1`|

### [VM-163] Type `Kata_pro64_Cad2025.VmbmLightFootingInput` (Token `0x0200043a`, RowOffset `13387048`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 18 | Methods count: 39
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x0400236e`|`cuC8SxdluH1`|`string`|`1`|`13540396`|`-`|
|`0x0400236f`|`lJr8SDQG5iJ`|`string`|`1`|`13540406`|`-`|
|`0x04002370`|`Ic48SB0kXQY`|`r8`|`1`|`13540416`|`-`|
|`0x04002371`|`sd38SOfuUOo`|`r8`|`1`|`13540426`|`-`|
|`0x04002372`|`CJ08SVBr4gh`|`r8`|`1`|`13540436`|`-`|
|`0x04002373`|`B6K8Sa4XKZs`|`r8`|`1`|`13540446`|`-`|
|`0x04002374`|`eMW8SzN4XWN`|`r8`|`1`|`13540456`|`-`|
|`0x04002375`|`xWb8jUDVSMU`|`r8`|`1`|`13540466`|`-`|
|`0x04002376`|`UcW8j8vOMoK`|`r8`|`1`|`13540476`|`-`|
|`0x04002377`|`VAG8jEXITit`|`r8`|`1`|`13540486`|`-`|
|`0x04002378`|`xgW8j07NeRk`|`r8`|`1`|`13540496`|`-`|
|`0x04002379`|`MxQ8jPsSADE`|`r8`|`1`|`13540506`|`-`|
|`0x0400237a`|`aeA8j3hXK1u`|`string`|`1`|`13540516`|`-`|
|`0x0400237b`|`HSJ8jqLWGsr`|`string`|`1`|`13540526`|`-`|
|`0x0400237c`|`INy8jAbYwBJ`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`1`|`13540536`|`-`|
|`0x0400237d`|`frF8jC47NV8`|`r8`|`1`|`13540546`|`-`|
|`0x0400237e`|`O9O8jmR4UbY`|`i4`|`1`|`13540556`|`-`|
|`0x0400237f`|`xDWc8jqYViDT0FIbm7xh`|`Kata_pro64_Cad2025.VmbmLightFootingInput [class:4328]`|`19`|`13540566`|`-`|

### [VM-164] Type `Kata_pro64_Cad2025.VmbmLightShallowSettings` (Token `0x0200043b`, RowOffset `13387066`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 25 | Methods count: 54
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04002380`|`mus8jQoAN4K`|`r8`|`1`|`13540576`|`-`|
|`0x04002381`|`Anm8jkVHKFI`|`r8`|`1`|`13540586`|`-`|
|`0x04002382`|`AKv8j4cX6PT`|`r8`|`1`|`13540596`|`-`|
|`0x04002383`|`JS78jYrX3E4`|`r8`|`1`|`13540606`|`-`|
|`0x04002384`|`tF68jojvVFH`|`r8`|`1`|`13540616`|`-`|
|`0x04002385`|`Mhk8j7idosw`|`r8`|`1`|`13540626`|`-`|
|`0x04002386`|`fHa8jFXV2E3`|`r8`|`1`|`13540636`|`-`|
|`0x04002387`|`AwX8jvRORdU`|`r8`|`1`|`13540646`|`-`|
|`0x04002388`|`HY28j1gRjnM`|`r8`|`1`|`13540656`|`-`|
|`0x04002389`|`shh8jMo3sod`|`r8`|`1`|`13540666`|`-`|
|`0x0400238a`|`llp8j9mtP42`|`string`|`1`|`13540676`|`-`|
|`0x0400238b`|`ewp8jKcHpL2`|`i4`|`1`|`13540686`|`-`|
|`0x0400238c`|`csu8jN2VDKv`|`ViTriDatTen [valuetype:3564]`|`1`|`13540696`|`-`|
|`0x0400238d`|`Eov8jbVwoFY`|`i4`|`1`|`13540706`|`-`|
|`0x0400238e`|`Miw8jIegPEd`|`bool`|`1`|`13540716`|`-`|
|`0x0400238f`|`D9W8jyak2UU`|`string`|`1`|`13540726`|`-`|
|`0x04002390`|`asH8jRhFAWE`|`r8`|`1`|`13540736`|`-`|
|`0x04002391`|`ucw8jgTwEo7`|`r8`|`1`|`13540746`|`-`|
|`0x04002392`|`AlR8jh3UbGp`|`r8`|`1`|`13540756`|`-`|
|`0x04002393`|`HhI8jZSOn5u`|`string`|`1`|`13540766`|`-`|
|`0x04002394`|`Mjj8jeZA7L7`|`string`|`1`|`13540776`|`-`|
|`0x04002395`|`VuU8jLGKkfr`|`string`|`1`|`13540786`|`-`|
|`0x04002396`|`sl68jTGgy4Z`|`string`|`1`|`13540796`|`-`|
|`0x04002397`|`EYP8jf5uc0C`|`string`|`1`|`13540806`|`-`|
|`0x04002398`|`VURKVOqoU0rOarbbi4Ec`|`Kata_pro64_Cad2025.VmbmLightShallowSettings [class:4332]`|`17`|`13540816`|`-`|

### [VM-165] Type `Kata_pro64_Cad2025.VmbmLightShallowSession` (Token `0x0200043c`, RowOffset `13387084`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 20 | Methods count: 56
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04002399`|`zHe8jcnj6Zu`|`Kata_pro64_Cad2025.VmbmLightShallowKind [valuetype:4324]`|`1`|`13540826`|`-`|
|`0x0400239a`|`wet8j6Gni3A`|`string`|`1`|`13540836`|`-`|
|`0x0400239b`|`mxS8jW0ml4h`|`string`|`1`|`13540846`|`-`|
|`0x0400239c`|`e6k8jsZ1Wsn`|`Kata_pro64_Cad2025.VmbmLightShallowSettings [class:4332]`|`1`|`13540856`|`-`|
|`0x0400239d`|`suU8jt4wn79`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightFootingInput [class:4328]>`|`1`|`13540866`|`-`|
|`0x0400239e`|`jK68jdulDi9`|`Polygon [class:89]`|`1`|`13540876`|`-`|
|`0x0400239f`|`CKx8jJS3AOe`|`InfoMBMongBang [class:3548]`|`1`|`13540886`|`-`|
|`0x040023a0`|`hkm8jrTq72b`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Info_ColumnWall3D [class:1001]>`|`1`|`13540896`|`-`|
|`0x040023a1`|`P5Y8jGjBMwC`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Info_Beam3D [class:481]>`|`1`|`13540906`|`-`|
|`0x040023a2`|`EpV8jl9uxA2`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Info_Beam3D [class:481]>`|`1`|`13540916`|`-`|
|`0x040023a3`|`jPu8juImhJs`|`System.Collections.Generic.List`1 [class:217]<i8>`|`1`|`13540926`|`-`|
|`0x040023a4`|`w3E8jHNeHCm`|`System.Collections.Generic.List`1 [class:217]<i8>`|`1`|`13540936`|`-`|
|`0x040023a5`|`US78jpX8Xi2`|`System.Collections.Generic.List`1 [class:217]<i8>`|`1`|`13540946`|`-`|
|`0x040023a6`|`GmT8jwi8we1`|`Kata_Class_Lib_Revit.diem [class:85]`|`1`|`13540956`|`-`|
|`0x040023a7`|`AIF8j2VJyeZ`|`string`|`1`|`13540966`|`-`|
|`0x040023a8`|`Wm68jX8XCjq`|`string`|`1`|`13540976`|`-`|
|`0x040023a9`|`E688jiAIp2M`|`string`|`1`|`13540986`|`-`|
|`0x040023aa`|`PJ18jnmK1Jb`|`string`|`1`|`13540996`|`-`|
|`0x040023ab`|`bWT8j5jg2Ul`|`bool`|`1`|`13541006`|`-`|
|`0x040023ac`|`jtYEEBqo0NvotvkEwQnc`|`Kata_pro64_Cad2025.VmbmLightShallowSession [class:4336]`|`17`|`13541016`|`-`|

### [VM-166] Type `Kata_pro64_Cad2025.VmbmLightShallowCad` (Token `0x02000441`, RowOffset `13387174`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 2 | Methods count: 9
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x040023ba`|`DataKey`|`string`|`32854`|`13541156`|`"VMBML_SHALLOW"`|
|`0x040023bb`|`Wy9BliqoqE4AF0ouWm9b`|`Kata_pro64_Cad2025.VmbmLightShallowCad [class:4356]`|`17`|`13541166`|`-`|

### [VM-167] Type `Kata_pro64_Cad2025.IVmbmLightAnalysis` (Token `0x02000446`, RowOffset `13387264`, Extends `none`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 0 | Methods count: 4

### [VM-168] Type `Kata_pro64_Cad2025.VmbmLightEngine` (Token `0x02000447`, RowOffset `13387282`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 1 | Methods count: 12
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x040023d1`|`WJJWJUqo4GmvlvPHhSQe`|`Kata_pro64_Cad2025.VmbmLightEngine [class:4380]`|`17`|`13541386`|`-`|

### [VM-169] Type `Kata_pro64_Cad2025.VmbmLightGeometry` (Token `0x02000454`, RowOffset `13387516`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 1 | Methods count: 7
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x0400241b`|`DnjSWeqo7Wayb9W8Dn9R`|`Kata_pro64_Cad2025.VmbmLightGeometry [class:4432]`|`17`|`13542126`|`-`|

### [VM-170] Type `Kata_pro64_Cad2025.VmbmLightEditing` (Token `0x02000456`, RowOffset `13387552`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 1 | Methods count: 24
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x0400241f`|`s6SXVsqo16YnGhISRMTQ`|`Kata_pro64_Cad2025.VmbmLightEditing [class:4440]`|`17`|`13542166`|`-`|

### [VM-171] Type `Kata_pro64_Cad2025.Form_VMBM_Light_Edit` (Token `0x02000476`, RowOffset `13388128`, Extends `0x0100000f (System.Windows.Forms.Form)`, Evidence `ev_549bfe33ecd78257ce27570bdd19331f304e770621ccd33a14059b374ac982e3`)
- Fields count: 36 | Methods count: 99
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x0400249d`|`QQO8xc3QOQ2`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`1`|`13543426`|`-`|
|`0x0400249e`|`QQT8x6Jnk8f`|`bool`|`1`|`13543436`|`-`|
|`0x0400249f`|`CHL8xWFaKpy`|`bool`|`1`|`13543446`|`-`|
|`0x040024a0`|`KlA8xspCCJn`|`bool`|`1`|`13543456`|`-`|
|`0x040024a1`|`gES8xt8oErv`|`bool`|`1`|`13543466`|`-`|
|`0x040024a2`|`rDK8xdAaKO6`|`bool`|`1`|`13543476`|`-`|
|`0x040024a3`|`jxN8xJSQjOV`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightCap [class:4152]>`|`33`|`13543486`|`-`|
|`0x040024a4`|`vAY8xrB7uBF`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightBeam [class:4200]>`|`33`|`13543496`|`-`|
|`0x040024a5`|`DQe8xGiqpdW`|`System.Windows.Forms.DataGridView [class:117]`|`33`|`13543506`|`-`|
|`0x040024a6`|`kdi8xl8QcRq`|`System.Windows.Forms.DataGridView [class:117]`|`33`|`13543516`|`-`|
|`0x040024a7`|`jdK8xuJSHmV`|`System.Windows.Forms.TabControl [class:365]`|`33`|`13543526`|`-`|
|`0x040024a8`|`Jwf8xHA2gvr`|`System.Windows.Forms.FlowLayoutPanel [class:333]`|`33`|`13543536`|`-`|
|`0x040024a9`|`DDL8xpGMWPa`|`System.Windows.Forms.FlowLayoutPanel [class:333]`|`33`|`13543546`|`-`|
|`0x040024aa`|`NS28xwJrbpX`|`System.Windows.Forms.TextBox [class:249]`|`33`|`13543556`|`-`|
|`0x040024ab`|`TF28x2fqtI1`|`System.Windows.Forms.NumericUpDown [class:325]`|`33`|`13543566`|`-`|
|`0x040024ac`|`GtH8xXVPMcj`|`System.Windows.Forms.NumericUpDown [class:325]`|`33`|`13543576`|`-`|
|`0x040024ad`|`Nsc8xiCOaFF`|`System.Windows.Forms.ComboBox [class:377]`|`33`|`13543586`|`-`|
|`0x040024ae`|`Nlk8xnLdX7k`|`System.Windows.Forms.Label [class:245]`|`33`|`13543596`|`-`|
|`0x040024af`|`AWX8x5hskHv`|`Kata_pro64_Cad2025.VmbmLightPreview [class:3708]`|`33`|`13543606`|`-`|
|`0x040024b0`|`rmM8xSwHlAs`|`System.Windows.Forms.ComboBox [class:377]`|`33`|`13543616`|`-`|
|`0x040024b1`|`rFR8xjJOanc`|`System.Windows.Forms.ComboBox [class:377]`|`33`|`13543626`|`-`|
|`0x040024b2`|`ef98xx2eBwB`|`System.Windows.Forms.NumericUpDown [class:325]`|`33`|`13543636`|`-`|
|`0x040024b3`|`nhP8xDCcYJW`|`System.Windows.Forms.NumericUpDown [class:325]`|`33`|`13543646`|`-`|
|`0x040024b4`|`UHF8xBOo6ce`|`System.Windows.Forms.TextBox [class:249]`|`33`|`13543656`|`-`|
|`0x040024b5`|`mgi8xOP3xI9`|`System.Windows.Forms.CheckBox [class:257]`|`33`|`13543666`|`-`|
|`0x040024b6`|`Iub8xVMyXZE`|`System.Windows.Forms.Label [class:245]`|`33`|`13543676`|`-`|
|`0x040024b7`|`aBS8xaNmjNk`|`System.Collections.Generic.Stack`1 [class:2061]<VCnTe604quOWiVhAAIOn [class:4572]>`|`33`|`13543686`|`-`|
|`0x040024b8`|`EmN8xzoUi6k`|`System.Collections.Generic.Stack`1 [class:2061]<VCnTe604quOWiVhAAIOn [class:4572]>`|`33`|`13543696`|`-`|
|`0x040024b9`|`SDN8DUY7lHR`|`System.Windows.Forms.Button [class:161]`|`1`|`13543706`|`-`|
|`0x040024ba`|`wJW8D86wo2n`|`System.Windows.Forms.Button [class:161]`|`1`|`13543716`|`-`|
|`0x040024bb`|`OVW8DE6xavG`|`bool`|`1`|`13543726`|`-`|
|`0x040024bc`|`JU88D098XH3`|`System.Windows.Forms.Form [class:61]`|`1`|`13543736`|`-`|
|`0x040024bd`|`PkJ8DPxBut8`|`bool`|`1`|`13543746`|`-`|
|`0x040024be`|`mIq8D3QG0Hl`|`bool`|`1`|`13543756`|`-`|
|`0x040024bf`|`Lau8DqcWHNL`|`string`|`1`|`13543766`|`-`|
|`0x040024c0`|`st4ZvoqoKhk58F3EM7yW`|`Kata_pro64_Cad2025.Form_VMBM_Light_Edit [class:4568]`|`17`|`13543776`|`-`|

### [VM-172] Type `Kata_pro64_Cad2025.VmbmLightEtabsMetadata` (Token `0x02000490`, RowOffset `13388596`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 10 | Methods count: 22
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04002518`|`f218DAw0JIJ`|`System.Collections.Generic.List`1 [class:217]<string>`|`1`|`13544656`|`-`|
|`0x04002519`|`MvZ8DCkSc4H`|`System.Collections.Generic.List`1 [class:217]<string>`|`1`|`13544666`|`-`|
|`0x0400251a`|`jh38DmVLWdg`|`string`|`1`|`13544676`|`-`|
|`0x0400251b`|`uG78DQmsPtC`|`string`|`1`|`13544686`|`-`|
|`0x0400251c`|`GHE8DkD4hGi`|`System.Collections.Generic.Dictionary`2 [class:389]<string,r8>`|`1`|`13544696`|`-`|
|`0x0400251d`|`hxp8D4bHnh6`|`System.Collections.Generic.List`1 [class:217]<string>`|`1`|`13544706`|`-`|
|`0x0400251e`|`xKw8DYBtUlf`|`System.Collections.Generic.List`1 [class:217]<string>`|`1`|`13544716`|`-`|
|`0x0400251f`|`snF8Dod3Z8R`|`System.Collections.Generic.List`1 [class:217]<string>`|`1`|`13544726`|`-`|
|`0x04002520`|`zK88D7IdjMY`|`System.Collections.Generic.List`1 [class:217]<string>`|`1`|`13544736`|`-`|
|`0x04002521`|`b3UkRdqoIbPOdtpgk1ue`|`Kata_pro64_Cad2025.VmbmLightEtabsMetadata [class:4672]`|`19`|`13544746`|`-`|

### [VM-173] Type `Kata_pro64_Cad2025.VmbmLightMesh` (Token `0x02000491`, RowOffset `13388614`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 1 | Methods count: 7
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04002522`|`UtVKEHqog4jI0BLmifIG`|`Kata_pro64_Cad2025.VmbmLightMesh [class:4676]`|`17`|`13544756`|`-`|

### [VM-174] Type `InfoMongDonSave` (Token `0x020004d2`, RowOffset `13389784`, Extends `0x0100000b (System.Object)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 41 | Methods count: 4
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04002883`|`Y1`|`object`|`6`|`13553406`|`-`|
|`0x04002884`|`X2`|`object`|`6`|`13553416`|`-`|
|`0x04002885`|`Z4`|`object`|`6`|`13553426`|`-`|
|`0x04002886`|`Ct`|`object`|`6`|`13553436`|`-`|
|`0x04002887`|`C1`|`object`|`6`|`13553446`|`-`|
|`0x04002888`|`X1`|`object`|`6`|`13553456`|`-`|
|`0x04002889`|`X3`|`object`|`6`|`13553466`|`-`|
|`0x0400288a`|`Y2`|`object`|`6`|`13553476`|`-`|
|`0x0400288b`|`Y3`|`object`|`6`|`13553486`|`-`|
|`0x0400288c`|`Z1`|`object`|`6`|`13553496`|`-`|
|`0x0400288d`|`Z2`|`object`|`6`|`13553506`|`-`|
|`0x0400288e`|`Thep_cot_X`|`object`|`6`|`13553516`|`-`|
|`0x0400288f`|`Thep_mong_X`|`object`|`6`|`13553526`|`-`|
|`0x04002890`|`Thep_mong_Y`|`object`|`6`|`13553536`|`-`|
|`0x04002891`|`Thep_cot_Y`|`object`|`6`|`13553546`|`-`|
|`0x04002892`|`phi_cot`|`object`|`6`|`13553556`|`-`|
|`0x04002893`|`dai_cot`|`object`|`6`|`13553566`|`-`|
|`0x04002894`|`C2`|`object`|`6`|`13553576`|`-`|
|`0x04002895`|`bt_lot`|`object`|`6`|`13553586`|`-`|
|`0x04002896`|`Z3`|`object`|`6`|`13553596`|`-`|
|`0x04002897`|`Ten_mong`|`object`|`6`|`13553606`|`-`|
|`0x04002898`|`moc_duoi`|`object`|`6`|`13553616`|`-`|
|`0x04002899`|`moc_trai`|`object`|`6`|`13553626`|`-`|
|`0x0400289a`|`moc_tren`|`object`|`6`|`13553636`|`-`|
|`0x0400289b`|`moc_phai`|`object`|`6`|`13553646`|`-`|
|`0x0400289c`|`sl`|`object`|`6`|`13553656`|`-`|
|`0x0400289d`|`Cover_cot`|`object`|`6`|`13553666`|`-`|
|`0x0400289e`|`Cover_mong`|`object`|`6`|`13553676`|`-`|
|`0x0400289f`|`khoaDauCot`|`bool`|`6`|`13553686`|`-`|
|`0x040028a0`|`hCho1`|`object`|`6`|`13553696`|`-`|
|`0x040028a1`|`hCho2`|`object`|`6`|`13553706`|`-`|
|`0x040028a2`|`hCho3`|`object`|`6`|`13553716`|`-`|
|`0x040028a3`|`TLBV`|`object`|`6`|`13553726`|`-`|
|`0x040028a4`|`D2`|`object`|`6`|`13553736`|`-`|
|`0x040028a5`|`D1`|`object`|`6`|`13553746`|`-`|
|`0x040028a6`|`daKiengTrai`|`bool`|`6`|`13553756`|`-`|
|`0x040028a7`|`daKiengPhai`|`bool`|`6`|`13553766`|`-`|
|`0x040028a8`|`CheckThepCot`|`bool`|`6`|`13553776`|`-`|
|`0x040028a9`|`leg`|`object`|`6`|`13553786`|`-`|
|`0x040028aa`|`DaKiengVuongGoc`|`object`|`6`|`13553796`|`-`|
|`0x040028ab`|`z7U4XLqzoepfpI2rhCvJ`|`object`|`17`|`13553806`|`-`|

### [VM-175] Type `InfoMongDonRenameSave` (Token `0x020004d3`, RowOffset `13389802`, Extends `0x0100000b (System.Object)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 41 | Methods count: 5
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x040028ac`|`Tk5qdaG4KTF`|`object`|`6`|`13553816`|`-`|
|`0x040028ad`|`FFPqdqC3h0Y`|`object`|`6`|`13553826`|`-`|
|`0x040028ae`|`LMuqdElc1YY`|`object`|`6`|`13553836`|`-`|
|`0x040028af`|`pGMqdSSP7km`|`object`|`6`|`13553846`|`-`|
|`0x040028b0`|`jLjqdbCOMmY`|`object`|`6`|`13553856`|`-`|
|`0x040028b1`|`kehqdvMfOLh`|`object`|`6`|`13553866`|`-`|
|`0x040028b2`|`ev3qdK6TnNc`|`object`|`6`|`13553876`|`-`|
|`0x040028b3`|`EilqdnUkgkL`|`object`|`6`|`13553886`|`-`|
|`0x040028b4`|`E4Nqds6GXoY`|`object`|`6`|`13553896`|`-`|
|`0x040028b5`|`DUSqdlLg5lg`|`object`|`6`|`13553906`|`-`|
|`0x040028b6`|`KMsqdyQj2ES`|`object`|`6`|`13553916`|`-`|
|`0x040028b7`|`bhkqdplIB0N`|`object`|`6`|`13553926`|`-`|
|`0x040028b8`|`fZGqdrEqEqU`|`object`|`6`|`13553936`|`-`|
|`0x040028b9`|`BCUqdQpP7AO`|`object`|`6`|`13553946`|`-`|
|`0x040028ba`|`H9cqduesnPg`|`object`|`6`|`13553956`|`-`|
|`0x040028bb`|`qtKqdPHpr8G`|`object`|`6`|`13553966`|`-`|
|`0x040028bc`|`t65qdd2Qnmk`|`object`|`6`|`13553976`|`-`|
|`0x040028bd`|`IwOqdefCyhQ`|`object`|`6`|`13553986`|`-`|
|`0x040028be`|`FBMqd3vUbNM`|`object`|`6`|`13553996`|`-`|
|`0x040028bf`|`Rxfqd0saHeP`|`object`|`6`|`13554006`|`-`|
|`0x040028c0`|`MTuqdOtvedR`|`object`|`6`|`13554016`|`-`|
|`0x040028c1`|`UOcqdCBtOKY`|`object`|`6`|`13554026`|`-`|
|`0x040028c2`|`JFkqdAEKSmg`|`object`|`6`|`13554036`|`-`|
|`0x040028c3`|`Uarqd78OSPt`|`object`|`6`|`13554046`|`-`|
|`0x040028c4`|`fRxqdwK9Vhe`|`object`|`6`|`13554056`|`-`|
|`0x040028c5`|`YZOqdJOBX2n`|`object`|`6`|`13554066`|`-`|
|`0x040028c6`|`Rtrqd1YbvNB`|`object`|`6`|`13554076`|`-`|
|`0x040028c7`|`mcZqd2oWTTi`|`object`|`6`|`13554086`|`-`|
|`0x040028c8`|`r46qdj1ftcJ`|`bool`|`6`|`13554096`|`-`|
|`0x040028c9`|`gN6qdYJ8wV5`|`object`|`6`|`13554106`|`-`|
|`0x040028ca`|`XhuqdM4rPn9`|`object`|`6`|`13554116`|`-`|
|`0x040028cb`|`CIFqdiEM795`|`object`|`6`|`13554126`|`-`|
|`0x040028cc`|`uWTqdk7HtTm`|`object`|`6`|`13554136`|`-`|
|`0x040028cd`|`SXqqd6XUBTS`|`object`|`6`|`13554146`|`-`|
|`0x040028ce`|`S63qdRUC9eJ`|`object`|`6`|`13554156`|`-`|
|`0x040028cf`|`ilEqd9bBQmR`|`bool`|`6`|`13554166`|`-`|
|`0x040028d0`|`KlUqdDeSjTG`|`bool`|`6`|`13554176`|`-`|
|`0x040028d1`|`eDqqdHEqvbZ`|`bool`|`6`|`13554186`|`-`|
|`0x040028d2`|`E6JqdNe4JCm`|`object`|`6`|`13554196`|`-`|
|`0x040028d3`|`z66qdmeslBi`|`object`|`6`|`13554206`|`-`|
|`0x040028d4`|`eBulooqzvbs8dpTHks9x`|`object`|`17`|`13554216`|`-`|

### [VM-176] Type `Kata_pro64_Cad2025.Foundation_Etabs` (Token `0x0200052c`, RowOffset `13391404`, Extends `0x0100000b (System.Object)`, Evidence `ev_3bdf54e817a9b5840d6618afe7ca3edeaabd4e8c3021dcb361d608566084bf9d`)
- Fields count: 2 | Methods count: 21
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04002b41`|`lbaEMamVToq`|`StripDesignOption [class:5304]`|`17`|`13560426`|`-`|
|`0x04002b42`|`VC0gwxqveRH65eJSWU0I`|`Kata_pro64_Cad2025.Foundation_Etabs [class:5296]`|`19`|`13560436`|`-`|

### [VM-177] Type `ThepBoTriMong` (Token `0x0200052f`, RowOffset `13391458`, Extends `0x0100000b (System.Object)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 3 | Methods count: 4
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04002b55`|`Lop1`|`string`|`6`|`13560626`|`-`|
|`0x04002b56`|`Lop2`|`string`|`6`|`13560636`|`-`|
|`0x04002b57`|`VT66dsA8YfTUtfSL0eZD`|`ThepBoTriMong [class:5308]`|`17`|`13560646`|`-`|

### [VM-178] Type `MomentMong` (Token `0x02000530`, RowOffset `13391476`, Extends `0x0100000b (System.Object)`, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
- Fields count: 11 | Methods count: 4
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04002b58`|`TenMong`|`string`|`6`|`13560656`|`-`|
|`0x04002b59`|`X1Max`|`r8`|`6`|`13560666`|`-`|
|`0x04002b5a`|`Y1Max`|`r8`|`6`|`13560676`|`-`|
|`0x04002b5b`|`X1Min`|`r8`|`6`|`13560686`|`-`|
|`0x04002b5c`|`Y1Min`|`r8`|`6`|`13560696`|`-`|
|`0x04002b5d`|`ThepX1Max`|`ThepBoTriMong [class:5308]`|`6`|`13560706`|`-`|
|`0x04002b5e`|`ThepY1Max`|`ThepBoTriMong [class:5308]`|`6`|`13560716`|`-`|
|`0x04002b5f`|`ThepX1Min`|`ThepBoTriMong [class:5308]`|`6`|`13560726`|`-`|
|`0x04002b60`|`ThepY1Min`|`ThepBoTriMong [class:5308]`|`6`|`13560736`|`-`|
|`0x04002b61`|`chieuCaoMong`|`string`|`6`|`13560746`|`-`|
|`0x04002b62`|`oKDhmXA8F9R0Cx1ucei7`|`MomentMong [class:5312]`|`19`|`13560756`|`-`|

### [VM-179] Type `Kata_pro64_Cad2025.Draw_foudation` (Token `0x02000557`, RowOffset `13392178`, Extends `0x0100000b (System.Object)`, Evidence `ev_3bdf54e817a9b5840d6618afe7ca3edeaabd4e8c3021dcb361d608566084bf9d`)
- Fields count: 2 | Methods count: 5
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04002c9d`|`au1EymCfktt`|`object`|`1`|`13563906`|`-`|
|`0x04002c9e`|`w69iwVq1L8EsySlrrWHB`|`Kata_pro64_Cad2025.Draw_foudation [class:5468]`|`17`|`13563916`|`-`|

### [VM-180] Type `Kata_pro64_Cad2025.kata7` (Token `0x02000566`, RowOffset `13392448`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 76 | Methods count: 110
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04002ce7`|`DefaultModelAIName`|`string`|`32854`|`13564646`|`"gpt-6-luna"`|
|`0x04002ce8`|`setting_kata`|`Kata_pro64_Cad2025.Setting [class:776]`|`22`|`13564656`|`-`|
|`0x04002ce9`|`SL_STR`|`string`|`22`|`13564666`|`-`|
|`0x04002cea`|`SL_VI_STR`|`string`|`22`|`13564676`|`-`|
|`0x04002ceb`|`SL_EN_STR`|`string`|`22`|`13564686`|`-`|
|`0x04002cec`|`TL_STR`|`string`|`22`|`13564696`|`-`|
|`0x04002ced`|`MC_STR`|`string`|`22`|`13564706`|`-`|
|`0x04002cee`|`shop`|`bool`|`22`|`13564716`|`-`|
|`0x04002cef`|`LoaiDamVe`|`i4`|`22`|`13564726`|`-`|
|`0x04002cf0`|`ma_font`|`string`|`22`|`13564736`|`-`|
|`0x04002cf1`|`layer_dim`|`string`|`22`|`13564746`|`-`|
|`0x04002cf2`|`layer_border`|`string`|`22`|`13564756`|`-`|
|`0x04002cf3`|`layer_table`|`string`|`22`|`13564766`|`-`|
|`0x04002cf4`|`layer_hidden`|`string`|`22`|`13564776`|`-`|
|`0x04002cf5`|`layer_grid`|`string`|`22`|`13564786`|`-`|
|`0x04002cf6`|`layer_thep_doc`|`string`|`22`|`13564796`|`-`|
|`0x04002cf7`|`layer_thep_dai_khuat`|`string`|`22`|`13564806`|`-`|
|`0x04002cf8`|`layer_thep_dai`|`string`|`22`|`13564816`|`-`|
|`0x04002cf9`|`layer_net_manh`|`string`|`22`|`13564826`|`-`|
|`0x04002cfa`|`layer_text`|`string`|`22`|`13564836`|`-`|
|`0x04002cfb`|`layer_Highlight`|`string`|`22`|`13564846`|`-`|
|`0x04002cfc`|`layer_QS`|`string`|`22`|`13564856`|`-`|
|`0x04002cfd`|`layer_Load`|`string`|`22`|`13564866`|`-`|
|`0x04002cfe`|`Style_text`|`string`|`22`|`13564876`|`-`|
|`0x04002cff`|`Style_Dim`|`string`|`22`|`13564886`|`-`|
|`0x04002d00`|`Style_Dim_rai_thep`|`string`|`22`|`13564896`|`-`|
|`0x04002d01`|`notShopColor`|`i4`|`22`|`13564906`|`-`|
|`0x04002d02`|`khkc`|`string`|`22`|`13564916`|`-`|
|`0x04002d03`|`xdata_ck`|`string`|`22`|`13564926`|`-`|
|`0x04002d04`|`xdata7_ht`|`string`|`22`|`13564936`|`-`|
|`0x04002d05`|`neo_gia`|`i4`|`22`|`13564946`|`-`|
|`0x04002d06`|`neo_keo`|`i4`|`22`|`13564956`|`-`|
|`0x04002d07`|`neo_nen`|`i4`|`22`|`13564966`|`-`|
|`0x04002d08`|`noi_thep`|`i4`|`22`|`13564976`|`-`|
|`0x04002d09`|`noi_thep_han_che`|`i4`|`22`|`13564986`|`-`|
|`0x04002d0a`|`kc_noi`|`i4`|`22`|`13564996`|`-`|
|`0x04002d0b`|`kc_2mep`|`i4`|`22`|`13565006`|`-`|
|`0x04002d0c`|`loai_dai_C`|`i4`|`22`|`13565016`|`-`|
|`0x04002d0d`|`loai_dai_CN`|`i4`|`22`|`13565026`|`-`|
|`0x04002d0e`|`L_moc_daiC`|`r8`|`22`|`13565036`|`-`|
|`0x04002d0f`|`L_moc_daiC2`|`r8`|`22`|`13565046`|`-`|
|`0x04002d10`|`L_moc_daiCN`|`r8`|`22`|`13565056`|`-`|
|`0x04002d11`|`L_min`|`i4`|`22`|`13565066`|`-`|
|`0x04002d12`|`L_max`|`i4`|`22`|`13565076`|`-`|
|`0x04002d13`|`L_min_cat`|`i4`|`22`|`13565086`|`-`|
|`0x04002d14`|`tl`|`i4`|`22`|`13565096`|`-`|
|`0x04002d15`|`tl_uon_be`|`i4`|`22`|`13565106`|`-`|
|`0x04002d16`|`da_dua_block_iso_vao`|`bool`|`22`|`13565116`|`-`|
|`0x04002d17`|`sh_auto`|`string`|`22`|`13565126`|`-`|
|`0x04002d18`|`tap_thep_xd_L`|`System.Collections.Generic.Dictionary`2 [class:389]<string,string>`|`22`|`13565136`|`-`|
|`0x04002d19`|`bao_ty_le`|`i4`|`22`|`13565146`|`-`|
|`0x04002d1a`|`save_info`|`string[]`|`22`|`13565156`|`-`|
|`0x04002d1b`|`mau_thep`|`System.Collections.Generic.Dictionary`2 [class:389]<i4,i4>`|`22`|`13565166`|`-`|
|`0x04002d1c`|`tap_cog_hook`|`System.Collections.Generic.SortedDictionary`2 [class:777]<i4,Kata_Class_Lib_Revit.info_cog_hook [valuetype:769]>`|`22`|`13565176`|`-`|
|`0x04002d1d`|`tap_unit_mass`|`System.Collections.Generic.SortedDictionary`2 [class:777]<i4,r8>`|`22`|`13565186`|`-`|
|`0x04002d1e`|`tap_lap_anchor`|`System.Collections.Generic.SortedDictionary`2 [class:777]<i4,Kata_Class_Lib_Revit.info_lap_anchor [valuetype:1145]>`|`22`|`13565196`|`-`|
|`0x04002d1f`|`VIP`|`bool`|`22`|`13565206`|`-`|
|`0x04002d20`|`time_start`|`i4`|`22`|`13565216`|`-`|
|`0x04002d21`|`don_vi`|`r8`|`22`|`13565226`|`-`|
|`0x04002d22`|`dungsai`|`i4`|`22`|`13565236`|`-`|
|`0x04002d23`|`round_neo_noi_Li`|`i4`|`22`|`13565246`|`-`|
|`0x04002d24`|`da_canh_bao`|`bool`|`22`|`13565256`|`-`|
|`0x04002d25`|`sl_toa_do_le`|`i4`|`22`|`13565266`|`-`|
|`0x04002d26`|`tap_dim_ro_revit`|`System.Collections.Generic.List`1 [class:217]<info_revit_dim_ro [class:2245]>`|`22`|`13565276`|`-`|
|`0x04002d27`|`Handles_MoveToTop`|`System.Collections.Generic.List`1 [class:217]<string>`|`22`|`13565286`|`-`|
|`0x04002d28`|`Language1`|`string`|`22`|`13565296`|`-`|
|`0x04002d29`|`LanguageInGoiLenh`|`string`|`22`|`13565306`|`-`|
|`0x04002d2a`|`fFAEgG8Zviq`|`System.Collections.Generic.Dictionary`2 [class:389]<string,oZtWXq0Fj6bwmhNYD9i4 [valuetype:5532]>`|`17`|`13565316`|`-`|
|`0x04002d2b`|`FuhEglIFqlp`|`System.Collections.Generic.Dictionary`2 [class:389]<System.Windows.Forms.Form [class:61],string>`|`17`|`13565326`|`-`|
|`0x04002d2c`|`as2EguCMUYQ`|`System.Windows.Forms.Timer [class:737]`|`17`|`13565336`|`-`|
|`0x04002d2d`|`listThepToHop`|`System.Collections.Generic.List`1 [class:217]<object>`|`22`|`13565346`|`-`|
|`0x04002d2e`|`RSlEgHX9Otg`|`object`|`17`|`13565356`|`-`|
|`0x04002d2f`|`o8SEgpmU4i9`|`i2[]`|`17`|`13565366`|`-`|
|`0x04002d30`|`PidEgwfDMv8`|`object[]`|`17`|`13565376`|`-`|
|`0x04002d31`|`ty_le_temp`|`i4`|`22`|`13565386`|`-`|
|`0x04002d32`|`XowiHPq1XAr0kOgFUpXw`|`Kata_pro64_Cad2025.kata7 [class:5528]`|`17`|`13565396`|`-`|

### [VM-181] Type `Kata_pro64_Cad2025.VmbmLightCadBinding` (Token `0x02000583`, RowOffset `13392970`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 8 | Methods count: 19
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04002dd3`|`ngtEZ2L8d6k`|`string`|`1`|`13567006`|`-`|
|`0x04002dd4`|`Pv2EZXAI9Bb`|`i8`|`1`|`13567016`|`-`|
|`0x04002dd5`|`TqTEZiuZWMx`|`i8`|`1`|`13567026`|`-`|
|`0x04002dd6`|`F4UEZnYqHbJ`|`r8`|`1`|`13567036`|`-`|
|`0x04002dd7`|`XFdEZ5hCeZ7`|`r8`|`1`|`13567046`|`-`|
|`0x04002dd8`|`BtgEZSmV7wm`|`string`|`1`|`13567056`|`-`|
|`0x04002dd9`|`Oe5EZj0dXm4`|`string`|`1`|`13567066`|`-`|
|`0x04002dda`|`tgNNxmqMoweq0xocHGAT`|`Kata_pro64_Cad2025.VmbmLightCadBinding [class:5644]`|`17`|`13567076`|`-`|

### [VM-182] Type `Kata_pro64_Cad2025.VmbmLightCadBeamBinding` (Token `0x02000584`, RowOffset `13392988`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 4 | Methods count: 11
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04002ddb`|`u27EZxFxdcg`|`string`|`1`|`13567086`|`-`|
|`0x04002ddc`|`SKhEZDPCA5d`|`i8`|`1`|`13567096`|`-`|
|`0x04002ddd`|`Uq9EZBNaWjO`|`string`|`1`|`13567106`|`-`|
|`0x04002dde`|`Vf5DjJqMvoTmlvQ4OvE5`|`Kata_pro64_Cad2025.VmbmLightCadBeamBinding [class:5648]`|`19`|`13567116`|`-`|

### [VM-183] Type `Kata_pro64_Cad2025.VmbmLightStripBinding` (Token `0x02000585`, RowOffset `13393006`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 5 | Methods count: 12
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04002ddf`|`oxIEZOEGaK3`|`string`|`1`|`13567126`|`-`|
|`0x04002de0`|`eTFEZVvhLas`|`string`|`1`|`13567136`|`-`|
|`0x04002de1`|`iJXEZaiSZw3`|`string`|`1`|`13567146`|`-`|
|`0x04002de2`|`FrIEZzOKDGl`|`r8`|`1`|`13567156`|`-`|
|`0x04002de3`|`cafKe4qM9P7OddC100M3`|`Kata_pro64_Cad2025.VmbmLightStripBinding [class:5652]`|`19`|`13567166`|`-`|

### [VM-184] Type `Kata_pro64_Cad2025.VmbmLightState` (Token `0x02000586`, RowOffset `13393024`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 1 | Methods count: 17
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04002de4`|`uKNGlnqMbGoLOImJwjn0`|`Kata_pro64_Cad2025.VmbmLightState [class:5656]`|`19`|`13567176`|`-`|

### [VM-185] Type `Kata_pro64_Cad2025.VmbmLightTableSelection` (Token `0x0200058d`, RowOffset `13393150`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 7 | Methods count: 11
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04002df9`|`DU2EePMTVHJ`|`CSiAPIv1.cDatabaseTables [class:2381]`|`33`|`13567386`|`-`|
|`0x04002dfa`|`aSnEe3s0aWS`|`string[]`|`33`|`13567396`|`-`|
|`0x04002dfb`|`K4MEeqoJFL3`|`string[]`|`33`|`13567406`|`-`|
|`0x04002dfc`|`MjnEeAUWcB3`|`System.Collections.Generic.List`1 [class:217]<string>`|`33`|`13567416`|`-`|
|`0x04002dfd`|`KZnEeCpOrLB`|`bool`|`1`|`13567426`|`-`|
|`0x04002dfe`|`XVLEemBDrT0`|`System.Exception [class:121]`|`1`|`13567436`|`-`|
|`0x04002dff`|`xQy7f4qMRVdhSrHVqXWf`|`Kata_pro64_Cad2025.VmbmLightTableSelection [class:5684]`|`19`|`13567446`|`-`|

### [VM-186] Type `Kata_pro64_Cad2025.kata8` (Token `0x0200058e`, RowOffset `13393168`, Extends `0x0100000b (System.Object)`, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
- Fields count: 3 | Methods count: 28
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04002e00`|`FhPEeQh44VI`|`Microsoft.VisualBasic.Collection [class:1837]`|`17`|`13567456`|`-`|
|`0x04002e01`|`Lk2EekmMPs6`|`Kata_pro64_Cad2025.Edit_nhiu_thep [class:360]`|`17`|`13567466`|`-`|
|`0x04002e02`|`qhBPOfqMZlM0funyhLbi`|`Kata_pro64_Cad2025.kata8 [class:5688]`|`17`|`13567476`|`-`|

---

#### 2.6.2. Bảng Chi tiết 100% Trường dữ liệu (247 Trường) của 13 Kiểu DTO con & Form Phụ trợ trong Bằng chứng E8 (`cad2025_sub_dtos.json` — Đồng chiếu `VM-054`, `VM-056`, `VM-057`, `VM-069`)

##### [E8-T01] Type `Kata_pro64_Cad2025.Form_Lop_Thep_Them` (Token `0x0200006e`, RowOffset `13369552`, Extends `0x0100000f (System.Windows.Forms.Form)`, Evidence `ev_f94acc3c1c98356a53651451a6de4c2edf1f53989c96f086936ec4a16a60ac33`)
- Fields count: 48 | Methods count: 111
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x0400040e`|`sP5vcV41g7`|`System.ComponentModel.IContainer [class:157]`|`1`|`13460076`|`-`|
|`0x0400040f`|`jDev6MZrcl`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460086`|`-`|
|`0x04000410`|`pVWvWSHss0`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460096`|`-`|
|`0x04000411`|`HfWvs9EAnN`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460106`|`-`|
|`0x04000412`|`qyDvtwLV8Z`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460116`|`-`|
|`0x04000413`|`iPvvdAKiOJ`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460126`|`-`|
|`0x04000414`|`l1SvJ4g4Ib`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460136`|`-`|
|`0x04000415`|`ncXvr2B05e`|`System.Windows.Forms.BindingSource [class:237]`|`1`|`13460146`|`-`|
|`0x04000416`|`aJovGaAnmU`|`System.Windows.Forms.Button [class:161]`|`1`|`13460156`|`-`|
|`0x04000417`|`zC7vlevJaQ`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460166`|`-`|
|`0x04000418`|`cMxvuuqefc`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460176`|`-`|
|`0x04000419`|`UpQvHPk47N`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460186`|`-`|
|`0x0400041a`|`PFcvpGlwn9`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460196`|`-`|
|`0x0400041b`|`J7uvwIJh2B`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460206`|`-`|
|`0x0400041c`|`H7fv2j2Kla`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13460216`|`-`|
|`0x0400041d`|`i9RvXWhspQ`|`System.Windows.Forms.ListView [class:849]`|`1`|`13460226`|`-`|
|`0x0400041e`|`vp0viSIZWd`|`System.Windows.Forms.ColumnHeader [class:853]`|`1`|`13460236`|`-`|
|`0x0400041f`|`yp9vn3hZox`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460246`|`-`|
|`0x04000420`|`Iwiv5hP1Ll`|`System.Windows.Forms.Label [class:245]`|`1`|`13460256`|`-`|
|`0x04000421`|`FoTvSgGqSc`|`System.Windows.Forms.Label [class:245]`|`1`|`13460266`|`-`|
|`0x04000422`|`YURvj6s7qN`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460276`|`-`|
|`0x04000423`|`o4wvxvhS6a`|`System.Windows.Forms.Button [class:161]`|`1`|`13460286`|`-`|
|`0x04000424`|`lrZvDvONt4`|`System.Windows.Forms.Button [class:161]`|`1`|`13460296`|`-`|
|`0x04000425`|`D10vBG0SFS`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460306`|`-`|
|`0x04000426`|`oPTvOSBrKb`|`System.Windows.Forms.Label [class:245]`|`1`|`13460316`|`-`|
|`0x04000427`|`DR0vVvAPhX`|`System.Windows.Forms.ColumnHeader [class:853]`|`1`|`13460326`|`-`|
|`0x04000428`|`Osrva4Qt6b`|`System.Windows.Forms.ColumnHeader [class:853]`|`1`|`13460336`|`-`|
|`0x04000429`|`pbtvzjHv4D`|`System.Windows.Forms.ColumnHeader [class:853]`|`1`|`13460346`|`-`|
|`0x0400042a`|`xIi1UOmvfH`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460356`|`-`|
|`0x0400042b`|`KW918Wv0DL`|`System.Windows.Forms.Label [class:245]`|`1`|`13460366`|`-`|
|`0x0400042c`|`DdW1EKZqLR`|`System.Windows.Forms.Panel [class:373]`|`1`|`13460376`|`-`|
|`0x0400042d`|`AlC10MjWb2`|`System.Windows.Forms.ColumnHeader [class:853]`|`1`|`13460386`|`-`|
|`0x0400042e`|`UmA1PZSZ9f`|`System.Windows.Forms.ColumnHeader [class:853]`|`1`|`13460396`|`-`|
|`0x0400042f`|`J8u13SWdIx`|`System.Windows.Forms.ColumnHeader [class:853]`|`1`|`13460406`|`-`|
|`0x04000430`|`vFW1qVXNRk`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460416`|`-`|
|`0x04000431`|`jsm1AmlTGr`|`System.Windows.Forms.Label [class:245]`|`1`|`13460426`|`-`|
|`0x04000432`|`cNb1C8rmLK`|`System.Windows.Forms.Label [class:245]`|`1`|`13460436`|`-`|
|`0x04000433`|`Su01mBMsVH`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460446`|`-`|
|`0x04000434`|`qnm1QxkUDx`|`System.Windows.Forms.Label [class:245]`|`1`|`13460456`|`-`|
|`0x04000435`|`WY41ktBJ02`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460466`|`-`|
|`0x04000436`|`fTE144D21R`|`Kata_Class_Lib_Revit.PreviewCanvas [class:261]`|`1`|`13460476`|`-`|
|`0x04000437`|`da_hien_form`|`bool`|`6`|`13460486`|`-`|
|`0x04000438`|`WTD1Yk3jmk`|`bool`|`1`|`13460496`|`-`|
|`0x04000439`|`FZG1o8RRkl`|`System.Drawing.Point [valuetype:173]`|`1`|`13460506`|`-`|
|`0x0400043a`|`kWB17WdboE`|`System.Drawing.Point [valuetype:173]`|`1`|`13460516`|`-`|
|`0x0400043b`|`o701FuhV77`|`System.Drawing.Color [valuetype:697]`|`1`|`13460526`|`-`|
|`0x0400043c`|`AKe1v7P6jx`|`bool`|`1`|`13460536`|`-`|
|`0x0400043d`|`y3rS0DqEehl7nrV1b5rP`|`Kata_pro64_Cad2025.Form_Lop_Thep_Them [class:440]`|`17`|`13460546`|`-`|

##### [E8-T02] Type `Kata_pro64_Cad2025.Form_Lop_Thep_Them_Ngang` (Token `0x0200006f`, RowOffset `13369570`, Extends `0x0100000f (System.Windows.Forms.Form)`, Evidence `ev_f94acc3c1c98356a53651451a6de4c2edf1f53989c96f086936ec4a16a60ac33`)
- Fields count: 48 | Methods count: 108
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x0400043e`|`lKI1siMgnJ`|`System.ComponentModel.IContainer [class:157]`|`1`|`13460556`|`-`|
|`0x0400043f`|`TAy1tAVVXY`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460566`|`-`|
|`0x04000440`|`emw1dheENW`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460576`|`-`|
|`0x04000441`|`nsG1Ja6vNT`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460586`|`-`|
|`0x04000442`|`BPG1r7XaFh`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460596`|`-`|
|`0x04000443`|`ad01G7SndB`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460606`|`-`|
|`0x04000444`|`DAj1lqi27D`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460616`|`-`|
|`0x04000445`|`fXg1uufH1f`|`System.Windows.Forms.BindingSource [class:237]`|`1`|`13460626`|`-`|
|`0x04000446`|`mQk1HrKeES`|`System.Windows.Forms.Button [class:161]`|`1`|`13460636`|`-`|
|`0x04000447`|`DEV1puNgDL`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460646`|`-`|
|`0x04000448`|`bCW1wJSATc`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460656`|`-`|
|`0x04000449`|`Jpb12xvjqN`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460666`|`-`|
|`0x0400044a`|`zU61XATacr`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460676`|`-`|
|`0x0400044b`|`MQr1i4HSFG`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460686`|`-`|
|`0x0400044c`|`Obk1nlXleL`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13460696`|`-`|
|`0x0400044d`|`v4h15FV2p5`|`System.Windows.Forms.ListView [class:849]`|`1`|`13460706`|`-`|
|`0x0400044e`|`aDo1ShKVpE`|`System.Windows.Forms.ColumnHeader [class:853]`|`1`|`13460716`|`-`|
|`0x0400044f`|`qf21jRr7rE`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460726`|`-`|
|`0x04000450`|`Xq11x7Hix1`|`System.Windows.Forms.Label [class:245]`|`1`|`13460736`|`-`|
|`0x04000451`|`B901DyuBlY`|`System.Windows.Forms.Label [class:245]`|`1`|`13460746`|`-`|
|`0x04000452`|`ePP1B0RFp0`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460756`|`-`|
|`0x04000453`|`zc51OTGSps`|`System.Windows.Forms.Label [class:245]`|`1`|`13460766`|`-`|
|`0x04000454`|`UKO1VWi6tW`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460776`|`-`|
|`0x04000455`|`jL31arhH7H`|`System.Windows.Forms.Button [class:161]`|`1`|`13460786`|`-`|
|`0x04000456`|`neb1zkLVL5`|`System.Windows.Forms.Button [class:161]`|`1`|`13460796`|`-`|
|`0x04000457`|`QP7MUsUnsS`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460806`|`-`|
|`0x04000458`|`bXLM8sHisN`|`System.Windows.Forms.Label [class:245]`|`1`|`13460816`|`-`|
|`0x04000459`|`N2SMEh2iDa`|`System.Windows.Forms.Label [class:245]`|`1`|`13460826`|`-`|
|`0x0400045a`|`XjeM0mVeHA`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460836`|`-`|
|`0x0400045b`|`gR1MPr2uGj`|`System.Windows.Forms.Label [class:245]`|`1`|`13460846`|`-`|
|`0x0400045c`|`WAQM3DB2eT`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460856`|`-`|
|`0x0400045d`|`uAFMqk53WF`|`System.Windows.Forms.ColumnHeader [class:853]`|`1`|`13460866`|`-`|
|`0x0400045e`|`wHXMASIoFq`|`System.Windows.Forms.ColumnHeader [class:853]`|`1`|`13460876`|`-`|
|`0x0400045f`|`aPEMCVh2NN`|`System.Windows.Forms.ColumnHeader [class:853]`|`1`|`13460886`|`-`|
|`0x04000460`|`syxMmNrkFi`|`System.Windows.Forms.ColumnHeader [class:853]`|`1`|`13460896`|`-`|
|`0x04000461`|`YsBMQVy82Z`|`System.Windows.Forms.ColumnHeader [class:853]`|`1`|`13460906`|`-`|
|`0x04000462`|`AjAMknnLR9`|`System.Windows.Forms.ColumnHeader [class:853]`|`1`|`13460916`|`-`|
|`0x04000463`|`LXfM40GsXS`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13460926`|`-`|
|`0x04000464`|`uSUMY3Y30i`|`System.Windows.Forms.Label [class:245]`|`1`|`13460936`|`-`|
|`0x04000465`|`FHPMohevW2`|`System.Windows.Forms.Panel [class:373]`|`1`|`13460946`|`-`|
|`0x04000466`|`NHTM7Uy1Rl`|`Kata_Class_Lib_Revit.PreviewCanvas [class:261]`|`1`|`13460956`|`-`|
|`0x04000467`|`da_hien_form`|`bool`|`6`|`13460966`|`-`|
|`0x04000468`|`Pu0MFMHc05`|`bool`|`1`|`13460976`|`-`|
|`0x04000469`|`DJZMvlKR6a`|`System.Drawing.Point [valuetype:173]`|`1`|`13460986`|`-`|
|`0x0400046a`|`s2NM1BE5A3`|`System.Drawing.Point [valuetype:173]`|`1`|`13460996`|`-`|
|`0x0400046b`|`jdjMMksGKS`|`System.Drawing.Color [valuetype:697]`|`1`|`13461006`|`-`|
|`0x0400046c`|`HhsM97BA33`|`bool`|`1`|`13461016`|`-`|
|`0x0400046d`|`dlLJDOqEfw5KToyHq895`|`Kata_pro64_Cad2025.Form_Lop_Thep_Them_Ngang [class:444]`|`17`|`13461026`|`-`|

##### [E8-T03] Type `Kata_pro64_Cad2025.Form_Lop_Thep_Them_Tren` (Token `0x02000070`, RowOffset `13369588`, Extends `0x0100000f (System.Windows.Forms.Form)`, Evidence `ev_f94acc3c1c98356a53651451a6de4c2edf1f53989c96f086936ec4a16a60ac33`)
- Fields count: 48 | Methods count: 110
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x0400046e`|`qUdMJ1ZK2p`|`System.ComponentModel.IContainer [class:157]`|`1`|`13461036`|`-`|
|`0x0400046f`|`VdwMrQR7Z0`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13461046`|`-`|
|`0x04000470`|`dFQMGESh8C`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13461056`|`-`|
|`0x04000471`|`nifMlTIlVI`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13461066`|`-`|
|`0x04000472`|`IWMMunnPf1`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13461076`|`-`|
|`0x04000473`|`J6nMHyiIL1`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13461086`|`-`|
|`0x04000474`|`m0PMpFhtlN`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13461096`|`-`|
|`0x04000475`|`HD2MwgIDdo`|`System.Windows.Forms.BindingSource [class:237]`|`1`|`13461106`|`-`|
|`0x04000476`|`c98M23jayV`|`System.Windows.Forms.Button [class:161]`|`1`|`13461116`|`-`|
|`0x04000477`|`nNVMXUnmJ5`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13461126`|`-`|
|`0x04000478`|`aSdMiKJKl0`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13461136`|`-`|
|`0x04000479`|`ygPMns2OcA`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13461146`|`-`|
|`0x0400047a`|`NtdM5kpMLt`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13461156`|`-`|
|`0x0400047b`|`vN3MSGd9Za`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13461166`|`-`|
|`0x0400047c`|`JlsMjrfyRe`|`System.Windows.Forms.GroupBox [class:241]`|`1`|`13461176`|`-`|
|`0x0400047d`|`ai3MxAqqb9`|`System.Windows.Forms.ListView [class:849]`|`1`|`13461186`|`-`|
|`0x0400047e`|`JrMMDer0gk`|`System.Windows.Forms.ColumnHeader [class:853]`|`1`|`13461196`|`-`|
|`0x0400047f`|`oEsMB2YLU3`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13461206`|`-`|
|`0x04000480`|`FMXMOdA12C`|`System.Windows.Forms.Label [class:245]`|`1`|`13461216`|`-`|
|`0x04000481`|`BD7MVkeiPI`|`System.Windows.Forms.Label [class:245]`|`1`|`13461226`|`-`|
|`0x04000482`|`wXGMaUGtPL`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13461236`|`-`|
|`0x04000483`|`uarMzURoyX`|`System.Windows.Forms.Label [class:245]`|`1`|`13461246`|`-`|
|`0x04000484`|`H2n9UMk0o6`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13461256`|`-`|
|`0x04000485`|`nxp98k1J72`|`System.Windows.Forms.Button [class:161]`|`1`|`13461266`|`-`|
|`0x04000486`|`xNb9ETo2uK`|`System.Windows.Forms.Button [class:161]`|`1`|`13461276`|`-`|
|`0x04000487`|`khs90q5X8G`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13461286`|`-`|
|`0x04000488`|`lvP9PEgDbK`|`System.Windows.Forms.Label [class:245]`|`1`|`13461296`|`-`|
|`0x04000489`|`Tt79333nlu`|`System.Windows.Forms.Label [class:245]`|`1`|`13461306`|`-`|
|`0x0400048a`|`YoI9qPKObW`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13461316`|`-`|
|`0x0400048b`|`jM79AirvfQ`|`System.Windows.Forms.Label [class:245]`|`1`|`13461326`|`-`|
|`0x0400048c`|`FsJ9CWIHqx`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13461336`|`-`|
|`0x0400048d`|`fNN9mOhaXg`|`System.Windows.Forms.ColumnHeader [class:853]`|`1`|`13461346`|`-`|
|`0x0400048e`|`pD29Q27GYj`|`System.Windows.Forms.ColumnHeader [class:853]`|`1`|`13461356`|`-`|
|`0x0400048f`|`lwC9kEp3Yq`|`System.Windows.Forms.ColumnHeader [class:853]`|`1`|`13461366`|`-`|
|`0x04000490`|`zZ094wrDW9`|`System.Windows.Forms.ColumnHeader [class:853]`|`1`|`13461376`|`-`|
|`0x04000491`|`pvy9YjqjLm`|`System.Windows.Forms.ColumnHeader [class:853]`|`1`|`13461386`|`-`|
|`0x04000492`|`kH59oCHncT`|`System.Windows.Forms.ColumnHeader [class:853]`|`1`|`13461396`|`-`|
|`0x04000493`|`FfC97JSbnS`|`System.Windows.Forms.TextBox [class:249]`|`1`|`13461406`|`-`|
|`0x04000494`|`Nxa9FKDmZw`|`System.Windows.Forms.Label [class:245]`|`1`|`13461416`|`-`|
|`0x04000495`|`nVP9vdKcVu`|`System.Windows.Forms.Panel [class:373]`|`1`|`13461426`|`-`|
|`0x04000496`|`jDl91IRDQn`|`Kata_Class_Lib_Revit.PreviewCanvas [class:261]`|`1`|`13461436`|`-`|
|`0x04000497`|`da_hien_form`|`bool`|`6`|`13461446`|`-`|
|`0x04000498`|`uD39Mm0mis`|`bool`|`1`|`13461456`|`-`|
|`0x04000499`|`bKW9936nRe`|`System.Drawing.Point [valuetype:173]`|`1`|`13461466`|`-`|
|`0x0400049a`|`RmT9KPlAvp`|`System.Drawing.Point [valuetype:173]`|`1`|`13461476`|`-`|
|`0x0400049b`|`hAU9N8svSU`|`System.Drawing.Color [valuetype:697]`|`1`|`13461486`|`-`|
|`0x0400049c`|`YZE9bg5XDU`|`bool`|`1`|`13461496`|`-`|
|`0x0400049d`|`s6ttw9qEWcRWBh4xKYta`|`Kata_pro64_Cad2025.Form_Lop_Thep_Them_Tren [class:448]`|`19`|`13461506`|`-`|

##### [E8-T04] Type `KataCircle3D` (Token `0x020002ef`, RowOffset `13381090`, Extends `0x0100000b (System.Object)`, Evidence `ev_f94acc3c1c98356a53651451a6de4c2edf1f53989c96f086936ec4a16a60ac33`)
- Fields count: 3 | Methods count: 5
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001b2b`|`center`|`Kata_Class_Lib_Revit.diem [class:85]`|`6`|`13519246`|`-`|
|`0x04001b2c`|`diameter`|`r8`|`6`|`13519256`|`-`|
|`0x04001b2d`|`dyDYe6quWy77jXO2p1cw`|`KataCircle3D [class:3004]`|`19`|`13519266`|`-`|

##### [E8-T05] Type `LopThepThem` (Token `0x020002f0`, RowOffset `13381108`, Extends `0x0100000b (System.Object)`, Evidence `ev_f94acc3c1c98356a53651451a6de4c2edf1f53989c96f086936ec4a16a60ac33`)
- Fields count: 9 | Methods count: 4
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001b2e`|`fngang`|`r8`|`6`|`13519276`|`-`|
|`0x04001b2f`|`spacing_ngang`|`r8`|`6`|`13519286`|`-`|
|`0x04001b30`|`fdoc`|`r8`|`6`|`13519296`|`-`|
|`0x04001b31`|`spacing_doc`|`r8`|`6`|`13519306`|`-`|
|`0x04001b32`|`dbm_tren`|`r8`|`6`|`13519316`|`-`|
|`0x04001b33`|`dbm_duoi`|`r8`|`6`|`13519326`|`-`|
|`0x04001b34`|`dbm_trai`|`r8`|`6`|`13519336`|`-`|
|`0x04001b35`|`dbm_phai`|`r8`|`6`|`13519346`|`-`|
|`0x04001b36`|`VMRHoJqudRwBleLNBcGc`|`LopThepThem [class:3008]`|`19`|`13519356`|`-`|

##### [E8-T06] Type `Infogrid` (Token `0x020002f3`, RowOffset `13381162`, Extends `0x0100000b (System.Object)`, Evidence `ev_f94acc3c1c98356a53651451a6de4c2edf1f53989c96f086936ec4a16a60ac33`)
- Fields count: 4 | Methods count: 4
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001b66`|`mp`|`Kata_Class_Lib_Revit.mat_phang_diem [class:809]`|`6`|`13519836`|`-`|
|`0x04001b67`|`nameGrid`|`string`|`6`|`13519846`|`-`|
|`0x04001b68`|`index`|`i4`|`6`|`13519856`|`-`|
|`0x04001b69`|`MOhOofqu2FAIT3SeNYkE`|`Infogrid [class:3020]`|`19`|`13519866`|`-`|

##### [E8-T07] Type `Dlcot` (Token `0x020002f4`, RowOffset `13381180`, Extends `0x0100000b (System.Object)`, Evidence `ev_f94acc3c1c98356a53651451a6de4c2edf1f53989c96f086936ec4a16a60ac33`)
- Fields count: 20 | Methods count: 4
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001b6a`|`duong_kinh`|`r8`|`6`|`13519876`|`-`|
|`0x04001b6b`|`x_thanh`|`i4`|`6`|`13519886`|`-`|
|`0x04001b6c`|`y_thanh`|`i4`|`6`|`13519896`|`-`|
|`0x04001b6d`|`f_dai`|`r8`|`6`|`13519906`|`-`|
|`0x04001b6e`|`spacing_dai`|`r8`|`6`|`13519916`|`-`|
|`0x04001b6f`|`n_dai`|`i4`|`6`|`13519926`|`-`|
|`0x04001b70`|`cover_dai`|`r8`|`6`|`13519936`|`-`|
|`0x04001b71`|`soThanhCotTron`|`i4`|`6`|`13519946`|`-`|
|`0x04001b72`|`khoaDauCot`|`bool`|`6`|`13519956`|`-`|
|`0x04001b73`|`leg`|`r8`|`6`|`13519966`|`-`|
|`0x04001b74`|`topleg`|`r8`|`6`|`13519976`|`-`|
|`0x04001b75`|`daiCX`|`string`|`6`|`13519986`|`-`|
|`0x04001b76`|`daiCY`|`string`|`6`|`13519996`|`-`|
|`0x04001b77`|`daivuongX`|`string`|`6`|`13520006`|`-`|
|`0x04001b78`|`daivuongY`|`string`|`6`|`13520016`|`-`|
|`0x04001b79`|`isCotTron`|`bool`|`6`|`13520026`|`-`|
|`0x04001b7a`|`daiCTron`|`i4`|`6`|`13520036`|`-`|
|`0x04001b7b`|`daiCotText`|`string`|`6`|`13520046`|`-`|
|`0x04001b7c`|`dkDaiGC`|`r8`|`6`|`13520056`|`-`|
|`0x04001b7d`|`qQrtLhqunHQ4lIphsc39`|`Dlcot [class:3024]`|`19`|`13520066`|`-`|

##### [E8-T08] Type `GiaCuongThemTheoPolygon` (Token `0x02000314`, RowOffset `13381756`, Extends `0x0100000b (System.Object)`, Evidence `ev_f94acc3c1c98356a53651451a6de4c2edf1f53989c96f086936ec4a16a60ac33`)
- Fields count: 6 | Methods count: 6
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001cd0`|`tenLopGC`|`object`|`6`|`13523456`|`-`|
|`0x04001cd1`|`tabIndex`|`i4`|`6`|`13523466`|`-`|
|`0x04001cd2`|`listGCDuoi`|`System.Collections.Generic.List`1 [class:217]<LopThepThemV2 [class:3160]>`|`6`|`13523476`|`-`|
|`0x04001cd3`|`listGCTren`|`System.Collections.Generic.List`1 [class:217]<LopThepThemV2 [class:3160]>`|`6`|`13523486`|`-`|
|`0x04001cd4`|`polyRai`|`object`|`6`|`13523496`|`-`|
|`0x04001cd5`|`c7AqtpqHlxdlO575twtp`|`object`|`17`|`13523506`|`-`|

##### [E8-T09] Type `LopThepThemV2` (Token `0x02000316`, RowOffset `13381792`, Extends `0x0100000b (System.Object)`, Evidence `ev_f94acc3c1c98356a53651451a6de4c2edf1f53989c96f086936ec4a16a60ac33`)
- Fields count: 8 | Methods count: 13
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001cda`|`thepX`|`object`|`6`|`13523556`|`-`|
|`0x04001cdb`|`thepY`|`object`|`6`|`13523566`|`-`|
|`0x04001cdc`|`sttGC`|`i4`|`6`|`13523576`|`-`|
|`0x04001cdd`|`thepXTxt`|`object`|`6`|`13523586`|`-`|
|`0x04001cde`|`thepYTxt`|`object`|`6`|`13523596`|`-`|
|`0x04001cdf`|`beMocTxt`|`object`|`6`|`13523606`|`-`|
|`0x04001ce0`|`coverTxt`|`object`|`6`|`13523616`|`-`|
|`0x04001ce1`|`RkLmrhqHpVAIHwYnNRgY`|`object`|`17`|`13523626`|`-`|

##### [E8-T10] Type `DlCotV2` (Token `0x02000318`, RowOffset `13381828`, Extends `0x0100000b (System.Object)`, Evidence `ev_f94acc3c1c98356a53651451a6de4c2edf1f53989c96f086936ec4a16a60ac33`)
- Fields count: 40 | Methods count: 14
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001ce3`|`isCotTron`|`bool`|`6`|`13523646`|`-`|
|`0x04001ce4`|`duong_kinh`|`r8`|`6`|`13523656`|`-`|
|`0x04001ce5`|`x_thanh`|`i4`|`6`|`13523666`|`-`|
|`0x04001ce6`|`y_thanh`|`i4`|`6`|`13523676`|`-`|
|`0x04001ce7`|`f_dai`|`r8`|`6`|`13523686`|`-`|
|`0x04001ce8`|`spacing_dai`|`r8`|`6`|`13523696`|`-`|
|`0x04001ce9`|`n_dai`|`i4`|`6`|`13523706`|`-`|
|`0x04001cea`|`cover_dai`|`r8`|`6`|`13523716`|`-`|
|`0x04001ceb`|`soThanhCotTron`|`i4`|`6`|`13523726`|`-`|
|`0x04001cec`|`khoaDauCot`|`bool`|`6`|`13523736`|`-`|
|`0x04001ced`|`daiCX`|`object`|`6`|`13523746`|`-`|
|`0x04001cee`|`daiCY`|`object`|`6`|`13523756`|`-`|
|`0x04001cef`|`daivuongX`|`object`|`6`|`13523766`|`-`|
|`0x04001cf0`|`daivuongY`|`object`|`6`|`13523776`|`-`|
|`0x04001cf1`|`daiCTron`|`i4`|`6`|`13523786`|`-`|
|`0x04001cf2`|`daiCotText`|`object`|`6`|`13523796`|`-`|
|`0x04001cf3`|`dkDaiGC`|`r8`|`6`|`13523806`|`-`|
|`0x04001cf4`|`tabIndex`|`i4`|`6`|`13523816`|`-`|
|`0x04001cf5`|`thepDai`|`object`|`6`|`13523826`|`-`|
|`0x04001cf6`|`thepChu1`|`object`|`6`|`13523836`|`-`|
|`0x04001cf7`|`thepChu2`|`object`|`6`|`13523846`|`-`|
|`0x04001cf8`|`isVach`|`bool`|`6`|`13523856`|`-`|
|`0x04001cf9`|`daiVachText`|`object`|`6`|`13523866`|`-`|
|`0x04001cfa`|`dkVachText`|`object`|`6`|`13523876`|`-`|
|`0x04001cfb`|`slThepPhuongNganText`|`object`|`6`|`13523886`|`-`|
|`0x04001cfc`|`kcThepChiuLucText`|`object`|`6`|`13523896`|`-`|
|`0x04001cfd`|`kieuDaiGiaCuongText`|`object`|`6`|`13523906`|`-`|
|`0x04001cfe`|`daiGiaCuongCText`|`object`|`6`|`13523916`|`-`|
|`0x04001cff`|`kieuRaiDaiCText`|`object`|`6`|`13523926`|`-`|
|`0x04001d00`|`tenVach`|`object`|`6`|`13523936`|`-`|
|`0x04001d01`|`llShowIndex`|`System.Collections.Generic.List`1 [class:217]<System.Collections.Generic.List`1 [class:217]<i4>>`|`6`|`13523946`|`-`|
|`0x04001d02`|`viTriLapDaiUText`|`object`|`6`|`13523956`|`-`|
|`0x04001d03`|`lapText`|`object`|`6`|`13523966`|`-`|
|`0x04001d04`|`limitLengthText`|`object`|`6`|`13523976`|`-`|
|`0x04001d05`|`kieuDaiGiaCuongByLengthText`|`object`|`6`|`13523986`|`-`|
|`0x04001d06`|`listThepDaiVach`|`System.Collections.Generic.List`1 [class:217]<System.Tuple`2 [class:285]<Polygon [class:89],object>>`|`6`|`13523996`|`-`|
|`0x04001d07`|`polyCot`|`object`|`6`|`13524006`|`-`|
|`0x04001d08`|`leg`|`r8`|`6`|`13524016`|`-`|
|`0x04001d09`|`topleg`|`r8`|`6`|`13524026`|`-`|
|`0x04001d0a`|`bZDExwqH50PISPwpQo0m`|`object`|`19`|`13524036`|`-`|

##### [E8-T11] Type `InfoAreaSpring` (Token `0x02000376`, RowOffset `13383520`, Extends `0x0100000b (System.Object)`, Evidence `ev_f94acc3c1c98356a53651451a6de4c2edf1f53989c96f086936ec4a16a60ac33`)
- Fields count: 5 | Methods count: 4
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001f35`|`PropertyName`|`string`|`6`|`13529586`|`-`|
|`0x04001f36`|`Local1`|`string`|`6`|`13529596`|`-`|
|`0x04001f37`|`Local2`|`string`|`6`|`13529606`|`-`|
|`0x04001f38`|`Local3`|`string`|`6`|`13529616`|`-`|
|`0x04001f39`|`QUXtprq2F5jGaYI29HHT`|`InfoAreaSpring [class:3544]`|`19`|`13529626`|`-`|

##### [E8-T12] Type `InfoText` (Token `0x0200037d`, RowOffset `13383646`, Extends `0x0100000b (System.Object)`, Evidence `ev_f94acc3c1c98356a53651451a6de4c2edf1f53989c96f086936ec4a16a60ac33`)
- Fields count: 4 | Methods count: 4
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04001f63`|`txtObj`|`object`|`6`|`13530046`|`-`|
|`0x04001f64`|`text`|`string`|`6`|`13530056`|`-`|
|`0x04001f65`|`position`|`Kata_Class_Lib_Revit.diem [class:85]`|`6`|`13530066`|`-`|
|`0x04001f66`|`hZ9SeAq2hFSfnNSijFOK`|`InfoText [class:3572]`|`17`|`13530076`|`-`|

##### [E8-T13] Type `StripDesignOption` (Token `0x0200052e`, RowOffset `13391440`, Extends `0x0100000b (System.Object)`, Evidence `ev_f94acc3c1c98356a53651451a6de4c2edf1f53989c96f086936ec4a16a60ac33`)
- Fields count: 4 | Methods count: 4
|Token|Field Name|Field Type (Raw & Resolved)|Flags|RowOffset|Constant / Default|
|---|---|---|---|---|---|
|`0x04002b51`|`StripDesignType`|`string`|`6`|`13560586`|`-`|
|`0x04002b52`|`RebarMaterial`|`string`|`6`|`13560596`|`-`|
|`0x04002b53`|`StripWidth`|`string`|`6`|`13560606`|`-`|
|`0x04002b54`|`SWtb37A8Qai7mlMEDirQ`|`StripDesignOption [class:5304]`|`19`|`13560616`|`-`|


---

### 2.7. Đặc tả Toàn diện Chữ ký Phương thức (Methods) của 96 Kiểu trong E4..E7 (VM-187 .. VM-282), 13 Kiểu trong E8 (Mục 2.7.1b) và Các Lớp Revit (VM-283 .. VM-288)

#### 2.7.1. Bảng Chi tiết 100% Phương thức của 96 Kiểu trong E4..E7 (VM-187 .. VM-282)

### [VM-187] Methods of `Kata_pro64_Cad2025.Form_VMBM_Light_Name` (Token `0x02000039`, Displayed: 8/8, Evidence `ev_549bfe33ecd78257ce27570bdd19331f304e770621ccd33a14059b374ac982e3`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06000304`|`get_Prefix`|`()`|`string`|`75712`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000305`|`get_StartNumber`|`()`|`i4`|`75748`|`4`|`78d4d2062aa5373e...`|
|`0x06000306`|`.ctor`|`string, i4`|`void`|`75784`|`4`|`ae3c8b8d99a39542...`|
|`0x06000307`|`_Lambda$__R6-1`|`object, System.EventArgs [class:113]`|`void`|`77812`|`4`|`ae3c8b8d99a39542...`|
|`0x06000308`|`_Lambda$__6-0`|`()`|`void`|`77828`|`4`|`ae3c8b8d99a39542...`|
|`0x06000309`|`.cctor`|`()`|`void`|`78316`|`156`|`78791a022d4bc2d4...`|
|`0x0600030a`|`OafPZxq80HhIvtx9xlZH`|`()`|`bool`|`78484`|`4`|`66ea1533cb3404e9...`|
|`0x0600030b`|`qInfcWq8PkSZT0lZJlbI`|`()`|`Kata_pro64_Cad2025.Form_VMBM_Light_Name [class:228]`|`78504`|`4`|`8969ce0cdaa4a19c...`|

### [VM-188] Methods of `Kata_pro64_Cad2025.Edit_moc_dai` (Token `0x02000059`, Displayed: 20/20, Evidence `ev_549bfe33ecd78257ce27570bdd19331f304e770621ccd33a14059b374ac982e3`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x0600057a`|`.ctor`|`()`|`void`|`351260`|`4`|`ae3c8b8d99a39542...`|
|`0x0600057b`|`Dispose`|`bool`|`void`|`351560`|`4`|`ae3c8b8d99a39542...`|
|`0x0600057c`|`dyrA9Cbhl1`|`()`|`void`|`352040`|`4`|`ae3c8b8d99a39542...`|
|`0x0600057d`|`get_OK`|`()`|`System.Windows.Forms.Button [class:161]`|`354748`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600057e`|`set_OK`|`System.Windows.Forms.Button [class:161]`|`void`|`354764`|`4`|`ae3c8b8d99a39542...`|
|`0x0600057f`|`C1T0ThoSBLJ`|`()`|`System.Windows.Forms.Label [class:245]`|`355108`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000580`|`FM90TZaNePt`|`System.Windows.Forms.Label [class:245]`|`void`|`355124`|`4`|`ae3c8b8d99a39542...`|
|`0x06000581`|`R5f0TexnDPH`|`()`|`System.Windows.Forms.PictureBox [class:645]`|`355228`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000582`|`Sru0TLGyuu6`|`System.Windows.Forms.PictureBox [class:645]`|`void`|`355244`|`4`|`ae3c8b8d99a39542...`|
|`0x06000583`|`rt80TT8HwpQ`|`()`|`System.Windows.Forms.TextBox [class:249]`|`355348`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000584`|`x330Tfi8Alh`|`System.Windows.Forms.TextBox [class:249]`|`void`|`355364`|`4`|`ae3c8b8d99a39542...`|
|`0x06000585`|`Baa0TclTTZ1`|`()`|`System.Windows.Forms.Label [class:245]`|`355468`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000586`|`Ykn0T6lU8ov`|`System.Windows.Forms.Label [class:245]`|`void`|`355484`|`4`|`ae3c8b8d99a39542...`|
|`0x06000587`|`VsRAKkQWdu`|`object, System.EventArgs [class:113]`|`void`|`355588`|`4`|`ae3c8b8d99a39542...`|
|`0x06000588`|`update_tap_dang`|`()`|`void`|`355924`|`4`|`ae3c8b8d99a39542...`|
|`0x06000589`|`TCsANlbhJB`|`object, System.EventArgs [class:113]`|`void`|`358536`|`4`|`ae3c8b8d99a39542...`|
|`0x0600058a`|`AUnAbAacM1`|`object, System.EventArgs [class:113]`|`void`|`359060`|`4`|`ae3c8b8d99a39542...`|
|`0x0600058b`|`.cctor`|`()`|`void`|`359208`|`177`|`e203c8451c07e53e...`|
|`0x0600058c`|`g6HCVYq8ng1iBSWhGdv2`|`()`|`bool`|`359400`|`4`|`66ea1533cb3404e9...`|
|`0x0600058d`|`qZffP5q85HPMXr2RcHVA`|`()`|`Kata_pro64_Cad2025.Edit_moc_dai [class:356]`|`359420`|`4`|`8969ce0cdaa4a19c...`|

### [VM-189] Methods of `Kata_pro64_Cad2025.Form_Edit_Coc` (Token `0x02000065`, Displayed: 24/24, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06000802`|`.ctor`|`()`|`void`|`634096`|`4`|`ae3c8b8d99a39542...`|
|`0x06000803`|`Dispose`|`bool`|`void`|`634360`|`4`|`ae3c8b8d99a39542...`|
|`0x06000804`|`L4HYuo8kp4`|`()`|`void`|`634768`|`4`|`ae3c8b8d99a39542...`|
|`0x06000805`|`qyU0spTh8Gd`|`()`|`System.Windows.Forms.DataGridView [class:117]`|`637812`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000806`|`rbk0swIiJJW`|`System.Windows.Forms.DataGridView [class:117]`|`void`|`637828`|`4`|`ae3c8b8d99a39542...`|
|`0x06000807`|`wCO0s2Riu5C`|`()`|`System.Windows.Forms.Button [class:161]`|`637932`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000808`|`zPP0sXks6Vj`|`System.Windows.Forms.Button [class:161]`|`void`|`637948`|`4`|`ae3c8b8d99a39542...`|
|`0x06000809`|`fpV0si6eorK`|`()`|`System.Windows.Forms.Button [class:161]`|`638336`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600080a`|`zJU0snNBOYn`|`System.Windows.Forms.Button [class:161]`|`void`|`638352`|`4`|`ae3c8b8d99a39542...`|
|`0x0600080b`|`tsU0s5TqhBI`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`638760`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600080c`|`cXV0sSy8dAF`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`638776`|`4`|`ae3c8b8d99a39542...`|
|`0x0600080d`|`kQQ0sjGSsmd`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`638880`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600080e`|`hoA0sxjR41B`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`638896`|`4`|`ae3c8b8d99a39542...`|
|`0x0600080f`|`ryK0sD3QrSW`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`639000`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000810`|`dWO0sBB8udC`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`639016`|`4`|`ae3c8b8d99a39542...`|
|`0x06000811`|`PRm0sObRZRj`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`639120`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000812`|`tYO0sVSVnjh`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`639136`|`4`|`ae3c8b8d99a39542...`|
|`0x06000813`|`XgGYHtshZU`|`object, System.EventArgs [class:113]`|`void`|`639240`|`4`|`ae3c8b8d99a39542...`|
|`0x06000814`|`k62Ypk8TAD`|`object, System.EventArgs [class:113]`|`void`|`639424`|`4`|`ae3c8b8d99a39542...`|
|`0x06000815`|`fPfYwCyeen`|`object, System.EventArgs [class:113]`|`void`|`639600`|`4`|`ae3c8b8d99a39542...`|
|`0x06000816`|`ic5Y2Ws4Ti`|`()`|`void`|`639616`|`4`|`ae3c8b8d99a39542...`|
|`0x06000817`|`.cctor`|`()`|`void`|`640204`|`177`|`be6f689b00149201...`|
|`0x06000818`|`PgroBOqENbGnVGfOMQNG`|`()`|`bool`|`640396`|`4`|`66ea1533cb3404e9...`|
|`0x06000819`|`zW9uPGqEbopDgc0ZPvWt`|`()`|`Kata_pro64_Cad2025.Form_Edit_Coc [class:404]`|`640416`|`4`|`8969ce0cdaa4a19c...`|

### [VM-190] Methods of `Kata_pro64_Cad2025.Form_mong_coc` (Token `0x02000073`, Displayed: 350/350, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06000b47`|`.ctor`|`()`|`void`|`996348`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b48`|`Dispose`|`bool`|`void`|`996748`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b49`|`b0HKgITbQ1`|`()`|`void`|`997136`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b4a`|`nx70GjIDxlT`|`()`|`System.Windows.Forms.Label [class:245]`|`1061592`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b4b`|`VB90Gxmuo89`|`System.Windows.Forms.Label [class:245]`|`void`|`1061608`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b4c`|`xmt0ZtnsMK4`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1061712`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b4d`|`R7r0ZdxJUjg`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1061728`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b4e`|`IEP0GDxQsJI`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1061832`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b4f`|`I2L0GBfMf63`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1061848`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b50`|`DpK0sc5MnnS`|`()`|`System.Windows.Forms.Label [class:245]`|`1062232`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b51`|`jRQ0s6mcPjx`|`System.Windows.Forms.Label [class:245]`|`void`|`1062248`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b52`|`HMP0GODrxAh`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1062352`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b53`|`Jb80GV0xTa7`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1062368`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b54`|`C3U0Gai6NxN`|`()`|`System.Windows.Forms.Label [class:245]`|`1062756`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b55`|`riH0GzEujxb`|`System.Windows.Forms.Label [class:245]`|`void`|`1062772`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b56`|`OqP0r5jWkbN`|`()`|`System.Windows.Forms.Label [class:245]`|`1062876`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b57`|`O4n0rSRqtI4`|`System.Windows.Forms.Label [class:245]`|`void`|`1062892`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b58`|`ChZ0lUK10pj`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1062996`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b59`|`gmc0l8ETvU9`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1063012`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b5a`|`iHx0lEpDgpG`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1063380`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b5b`|`q8L0l0AXpOu`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1063396`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b5c`|`aQM0GFgWUOC`|`()`|`System.Windows.Forms.Label [class:245]`|`1063744`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b5d`|`dRd0Gvyq4YN`|`System.Windows.Forms.Label [class:245]`|`void`|`1063760`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b5e`|`KAW0lPWX6Sl`|`()`|`System.Windows.Forms.Label [class:245]`|`1063864`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b5f`|`eK40l31Sv7M`|`System.Windows.Forms.Label [class:245]`|`void`|`1063880`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b60`|`cUE0lqPYyNv`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1063984`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b61`|`ef10lAHGWO4`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1064000`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b62`|`KxI0tD5cgWu`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1064328`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b63`|`unj0tBRjgGu`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1064344`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b64`|`PAO0lCoNGGj`|`()`|`System.Windows.Forms.Label [class:245]`|`1064448`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b65`|`opi0lmIwfwt`|`System.Windows.Forms.Label [class:245]`|`void`|`1064464`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b66`|`Kdb0TCwb6SF`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1064568`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b67`|`pwT0TmtLZra`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1064584`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b68`|`LEw0tO3g9ED`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1064688`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b69`|`d9m0tV53Ym5`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1064704`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b6a`|`Ue30faKASQh`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1064808`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b6b`|`HGm0fzEadbV`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1064824`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b6c`|`F450h5Oc62a`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1064928`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b6d`|`ec30hSAXSkn`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1064944`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b6e`|`NhW0lQR3kvG`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1065708`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b6f`|`V5s0lk7DcJO`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1065724`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b70`|`EK90l4hwxLc`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1066152`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b71`|`GLM0lYTHXnV`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1066168`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b72`|`bTL0rRvYn9m`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1066536`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b73`|`jb70rgGSwKe`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1066552`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b74`|`sxf0rh7JXo8`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1066920`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b75`|`rEP0rZD4Byv`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1066936`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b76`|`BSb0reBf6QO`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1067040`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b77`|`ATr0rLBswue`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1067056`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b78`|`LL70rTCH8C1`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1067360`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b79`|`aiR0rfB4QTu`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1067376`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b7a`|`NVa0rcTMJNs`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1067760`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b7b`|`uS90r6yrNwX`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1067776`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b7c`|`Ovr0rWl9rLn`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1067880`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b7d`|`fk20rsjNplb`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1067896`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b7e`|`YBu0ruu34tq`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1068000`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b7f`|`HAi0rHL6ND1`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1068016`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b80`|`n7Q0rJ22Ov0`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1068120`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b81`|`aDt0rrnS17J`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1068136`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b82`|`se20rGnYrwe`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1068240`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b83`|`NdX0rlZGreQ`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1068256`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b84`|`qVo0rtB6PoL`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1068360`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b85`|`XrB0rdNQ3hf`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1068376`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b86`|`MTS0fCBnujV`|`()`|`System.Windows.Forms.Button [class:161]`|`1068764`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b87`|`V1G0fmvURg0`|`System.Windows.Forms.Button [class:161]`|`void`|`1068780`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b88`|`MWd0fOEfVi9`|`()`|`System.Windows.Forms.Button [class:161]`|`1069128`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b89`|`v6W0fVemltt`|`System.Windows.Forms.Button [class:161]`|`void`|`1069144`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b8a`|`k7R0huRyZOy`|`()`|`System.Windows.Forms.BindingSource [class:237]`|`1069532`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b8b`|`zNn0hHpucM0`|`System.Windows.Forms.BindingSource [class:237]`|`void`|`1069548`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b8c`|`HWt0fQrKvy4`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1069652`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b8d`|`tpi0fk0hWrQ`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1069668`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b8e`|`guc0lof3k02`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1069772`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b8f`|`whF0l7SQ4tJ`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1069788`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b90`|`SHg0lF1N001`|`()`|`System.Windows.Forms.CheckBox [class:257]`|`1070240`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b91`|`OxO0lv3tKs1`|`System.Windows.Forms.CheckBox [class:257]`|`void`|`1070256`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b92`|`mCq0l1yqLe8`|`()`|`System.Windows.Forms.CheckBox [class:257]`|`1070664`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b93`|`Vnx0lMJ1qYL`|`System.Windows.Forms.CheckBox [class:257]`|`void`|`1070680`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b94`|`xOm0ZQEvAmp`|`()`|`System.Windows.Forms.Label [class:245]`|`1071024`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b95`|`X9v0ZkfUlXU`|`System.Windows.Forms.Label [class:245]`|`void`|`1071040`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b96`|`tue0LObWfVm`|`()`|`System.Windows.Forms.Label [class:245]`|`1071144`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b97`|`SJC0LVQ2df9`|`System.Windows.Forms.Label [class:245]`|`void`|`1071160`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b98`|`oMD0l9TnNHQ`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1071264`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b99`|`BpK0lKbsQ7A`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1071280`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b9a`|`oDO0lNYMOmJ`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1071384`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b9b`|`zdU0lbMtPZe`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1071400`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b9c`|`zP60Z43yd3J`|`()`|`System.Windows.Forms.Label [class:245]`|`1071952`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b9d`|`Ggy0ZYPrMrH`|`System.Windows.Forms.Label [class:245]`|`void`|`1071968`|`4`|`ae3c8b8d99a39542...`|
|`0x06000b9e`|`wJd0lIt3wL2`|`()`|`System.Windows.Forms.Label [class:245]`|`1072072`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000b9f`|`xBW0lyuKCVf`|`System.Windows.Forms.Label [class:245]`|`void`|`1072088`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ba0`|`tBT0lRoU6xw`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1072192`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ba1`|`jOF0lgIMuU4`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1072208`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ba2`|`mlk0lhAP3kp`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1072312`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ba3`|`XyR0lZe8eDV`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1072328`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ba4`|`hqp0lefr6dW`|`()`|`System.Windows.Forms.Label [class:245]`|`1072696`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ba5`|`PhI0lLGHXrW`|`System.Windows.Forms.Label [class:245]`|`void`|`1072712`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ba6`|`Htq0lT08qCu`|`()`|`System.Windows.Forms.Label [class:245]`|`1072816`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ba7`|`x1i0lf42Jwi`|`System.Windows.Forms.Label [class:245]`|`void`|`1072832`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ba8`|`tw30lc6na8a`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1072936`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ba9`|`Dns0l65mc8Z`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1072952`|`4`|`ae3c8b8d99a39542...`|
|`0x06000baa`|`YHA0lW8Ot7e`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1073256`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bab`|`V0m0lsDN1uJ`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1073272`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bac`|`gbP0lt2ZFfi`|`()`|`System.Windows.Forms.Label [class:245]`|`1073640`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bad`|`BI60ldwSWMI`|`System.Windows.Forms.Label [class:245]`|`void`|`1073656`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bae`|`AE40lJCyo5b`|`()`|`System.Windows.Forms.Label [class:245]`|`1073760`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000baf`|`VRU0lrpcsJB`|`System.Windows.Forms.Label [class:245]`|`void`|`1073776`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bb0`|`k970lGaaGW8`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1073880`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bb1`|`GKy0lliNPYN`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1073896`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bb2`|`hMy0luQ8b4G`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1074244`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bb3`|`QcL0lH1fkZd`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1074260`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bb4`|`o2V0lpmLA5R`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1074564`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bb5`|`guy0lwug3Lf`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1074580`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bb6`|`IKM0l2rLHoQ`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1074908`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bb7`|`uxr0lXCgyKp`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1074924`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bb8`|`dX80liEC9CU`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1075268`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bb9`|`uDn0lnpoQjy`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1075284`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bba`|`wIK0l5QW4iH`|`()`|`System.Windows.Forms.Label [class:245]`|`1075652`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bbb`|`wor0lSJXenH`|`System.Windows.Forms.Label [class:245]`|`void`|`1075668`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bbc`|`ovQ0ljJbDcI`|`()`|`System.Windows.Forms.RadioButton [class:253]`|`1075772`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bbd`|`sp70lxorx9W`|`System.Windows.Forms.RadioButton [class:253]`|`void`|`1075788`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bbe`|`s6i0lD9OhUY`|`()`|`System.Windows.Forms.RadioButton [class:253]`|`1075892`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bbf`|`SvA0lB0TTl8`|`System.Windows.Forms.RadioButton [class:253]`|`void`|`1075908`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bc0`|`T7S0lOO32MW`|`()`|`System.Windows.Forms.Label [class:245]`|`1076256`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bc1`|`JjC0lVm7nvg`|`System.Windows.Forms.Label [class:245]`|`void`|`1076272`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bc2`|`XVl0laFfFrq`|`()`|`System.Windows.Forms.RadioButton [class:253]`|`1076376`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bc3`|`rB40lzWEG2D`|`System.Windows.Forms.RadioButton [class:253]`|`void`|`1076392`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bc4`|`n7G0uUAH9Pc`|`()`|`System.Windows.Forms.RadioButton [class:253]`|`1076496`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bc5`|`aEy0u8h1KQL`|`System.Windows.Forms.RadioButton [class:253]`|`void`|`1076512`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bc6`|`pZO0uE0E0oS`|`()`|`System.Windows.Forms.Label [class:245]`|`1076880`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bc7`|`pZD0u06Yq06`|`System.Windows.Forms.Label [class:245]`|`void`|`1076896`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bc8`|`I7c0uPjgMCq`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1077000`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bc9`|`GCF0u3VsuIZ`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1077016`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bca`|`Ngb0uqyrQ9Z`|`()`|`System.Windows.Forms.Label [class:245]`|`1077120`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bcb`|`t2S0uAHhAvR`|`System.Windows.Forms.Label [class:245]`|`void`|`1077136`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bcc`|`cK20uCBBIj4`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1077240`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bcd`|`pFZ0umsxMYq`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1077256`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bce`|`tNF0WNLiP4Q`|`()`|`System.Windows.Forms.Label [class:245]`|`1077640`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bcf`|`KQY0WbQ7dhn`|`System.Windows.Forms.Label [class:245]`|`void`|`1077656`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bd0`|`TdS0uQbQ72d`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1077760`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bd1`|`Iu70ukgsOfG`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1077776`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bd2`|`MS20u4boTM7`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1078228`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bd3`|`uai0uYlHqVV`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1078244`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bd4`|`kJh0uoFCo1n`|`()`|`System.Windows.Forms.Label [class:245]`|`1078652`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bd5`|`j8Y0u7H8WTj`|`System.Windows.Forms.Label [class:245]`|`void`|`1078668`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bd6`|`kIj0uFWZanK`|`()`|`System.Windows.Forms.RadioButton [class:253]`|`1078772`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bd7`|`WBN0uvPI0oR`|`System.Windows.Forms.RadioButton [class:253]`|`void`|`1078788`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bd8`|`gbM0u1L44Cj`|`()`|`System.Windows.Forms.RadioButton [class:253]`|`1079156`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bd9`|`r7K0uMCK4KV`|`System.Windows.Forms.RadioButton [class:253]`|`void`|`1079172`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bda`|`EDs0u9BSBh2`|`()`|`System.Windows.Forms.Label [class:245]`|`1079560`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bdb`|`SFl0uKFqpuw`|`System.Windows.Forms.Label [class:245]`|`void`|`1079576`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bdc`|`Smu0uNiKPno`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1079680`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bdd`|`hW60ubVp0A6`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1079696`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bde`|`q6M0rI1Rr6p`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1079800`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bdf`|`p9g0rykvQsC`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1079816`|`4`|`ae3c8b8d99a39542...`|
|`0x06000be0`|`OSI0sPqNHJI`|`()`|`System.Windows.Forms.Button [class:161]`|`1080264`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000be1`|`h7x0s3IgLX2`|`System.Windows.Forms.Button [class:161]`|`void`|`1080280`|`4`|`ae3c8b8d99a39542...`|
|`0x06000be2`|`N5D0uI2uK2W`|`()`|`System.Windows.Forms.Button [class:161]`|`1080688`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000be3`|`aDB0uyl5J1o`|`System.Windows.Forms.Button [class:161]`|`void`|`1080704`|`4`|`ae3c8b8d99a39542...`|
|`0x06000be4`|`Q5M0uR90ENj`|`()`|`System.Windows.Forms.Button [class:161]`|`1081112`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000be5`|`cXH0ugi8VkT`|`System.Windows.Forms.Button [class:161]`|`void`|`1081128`|`4`|`ae3c8b8d99a39542...`|
|`0x06000be6`|`t5K0uhsUWDj`|`()`|`System.Windows.Forms.RadioButton [class:253]`|`1081432`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000be7`|`CHP0uZow2PI`|`System.Windows.Forms.RadioButton [class:253]`|`void`|`1081448`|`4`|`ae3c8b8d99a39542...`|
|`0x06000be8`|`TLo0sql0cac`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1081816`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000be9`|`g0f0sAnedH3`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1081832`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bea`|`cfq0uechWad`|`()`|`System.Windows.Forms.Label [class:245]`|`1081936`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000beb`|`Eug0uLyFNEy`|`System.Windows.Forms.Label [class:245]`|`void`|`1081952`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bec`|`sKK0uTXKs9H`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1082056`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bed`|`GKl0ufOHb93`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1082072`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bee`|`MZn0fe7UJWt`|`()`|`System.Windows.Forms.Panel [class:373]`|`1082420`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bef`|`d5N0fL1RpR2`|`System.Windows.Forms.Panel [class:373]`|`void`|`1082436`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bf0`|`JbJ0ucOnNCQ`|`()`|`System.Windows.Forms.Button [class:161]`|`1082540`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bf1`|`KHo0u6YDmOV`|`System.Windows.Forms.Button [class:161]`|`void`|`1082556`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bf2`|`a4k0uWJYlBR`|`()`|`System.Windows.Forms.Label [class:245]`|`1082904`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bf3`|`Qig0usnM7mu`|`System.Windows.Forms.Label [class:245]`|`void`|`1082920`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bf4`|`apv0uttQwfc`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1083024`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bf5`|`e0t0udtjwfZ`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1083040`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bf6`|`UcI0uJ0PKap`|`()`|`System.Windows.Forms.Label [class:245]`|`1083472`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bf7`|`Jyc0urGcW1i`|`System.Windows.Forms.Label [class:245]`|`void`|`1083488`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bf8`|`Bp20uGJXJIb`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1083592`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bf9`|`plK0ulbW0CG`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1083608`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bfa`|`ogU0uugQH87`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1084056`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bfb`|`ElM0uHFOikF`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1084072`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bfc`|`tmG0upiqCqE`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1084400`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bfd`|`L6F0uwmSLNN`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1084416`|`4`|`ae3c8b8d99a39542...`|
|`0x06000bfe`|`i040u2xceMS`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1084784`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000bff`|`Ogl0uXuoIWr`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1084800`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c00`|`asL0ui14Ux8`|`()`|`System.Windows.Forms.CheckBox [class:257]`|`1085128`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c01`|`QlK0unuXguT`|`System.Windows.Forms.CheckBox [class:257]`|`void`|`1085144`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c02`|`A380u5O3yEm`|`()`|`System.Windows.Forms.ComboBox [class:377]`|`1085512`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c03`|`Df90uS5em1u`|`System.Windows.Forms.ComboBox [class:377]`|`void`|`1085528`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c04`|`juC0ujL9LJ8`|`()`|`System.Windows.Forms.Label [class:245]`|`1085872`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c05`|`fEt0uxgJUfI`|`System.Windows.Forms.Label [class:245]`|`void`|`1085888`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c06`|`FY30uD1DawY`|`()`|`System.Windows.Forms.Button [class:161]`|`1085992`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c07`|`LXh0uBbFAGu`|`System.Windows.Forms.Button [class:161]`|`void`|`1086008`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c08`|`cT30uOxCFFk`|`()`|`System.Windows.Forms.TabControl [class:365]`|`1086332`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c09`|`Wyl0uVj9JTM`|`System.Windows.Forms.TabControl [class:365]`|`void`|`1086348`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c0a`|`JXO0uam1SIk`|`()`|`System.Windows.Forms.TabPage [class:369]`|`1086756`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c0b`|`gJN0uzFHuyF`|`System.Windows.Forms.TabPage [class:369]`|`void`|`1086772`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c0c`|`clG0HUwX9lY`|`()`|`System.Windows.Forms.Panel [class:373]`|`1086876`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c0d`|`ciK0H8pYD50`|`System.Windows.Forms.Panel [class:373]`|`void`|`1086892`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c0e`|`fkI0WInERnw`|`()`|`System.Windows.Forms.Label [class:245]`|`1086996`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c0f`|`ETa0WyDZkEG`|`System.Windows.Forms.Label [class:245]`|`void`|`1087012`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c10`|`R6W0HEps3NG`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1087116`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c11`|`krV0H0E1NI0`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1087132`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c12`|`CEB0HP6Bd1X`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1087520`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c13`|`YOY0H3Kk0UY`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1087536`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c14`|`LpS0sR1JSDi`|`()`|`System.Windows.Forms.Label [class:245]`|`1087944`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c15`|`w5X0sgBRVD4`|`System.Windows.Forms.Label [class:245]`|`void`|`1087960`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c16`|`CCA0ZowoUwH`|`()`|`System.Windows.Forms.Label [class:245]`|`1088064`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c17`|`s3g0Z7VmfVf`|`System.Windows.Forms.Label [class:245]`|`void`|`1088080`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c18`|`WeJ0HqVttRv`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1088184`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c19`|`Iq50HA9Rk1r`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1088200`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c1a`|`iMT0HCk2k7B`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1088544`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c1b`|`aUO0Hmg0N88`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1088560`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c1c`|`QP00HQo5DtD`|`()`|`System.Windows.Forms.Label [class:245]`|`1089052`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c1d`|`Tww0HkcVdAb`|`System.Windows.Forms.Label [class:245]`|`void`|`1089068`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c1e`|`ejR0H45P0Sb`|`()`|`System.Windows.Forms.Label [class:245]`|`1089172`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c1f`|`YDw0HYtTyd1`|`System.Windows.Forms.Label [class:245]`|`void`|`1089188`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c20`|`StM0HoeMOM8`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1089292`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c21`|`Xbt0H7o0QIt`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1089308`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c22`|`zQd0cP2Kibt`|`()`|`System.Windows.Forms.Label [class:245]`|`1089736`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c23`|`laU0c3QpRdF`|`System.Windows.Forms.Label [class:245]`|`void`|`1089752`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c24`|`lgA0HFZRMem`|`()`|`System.Windows.Forms.Label [class:245]`|`1089856`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c25`|`qAI0Hvgbu6L`|`System.Windows.Forms.Label [class:245]`|`void`|`1089872`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c26`|`jZ40H1KMbbi`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1089976`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c27`|`LLZ0HMtEcAb`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1089992`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c28`|`cAA0H9IoQTJ`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1090096`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c29`|`eVE0HKUN5vo`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1090112`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c2a`|`L4I0HN70yWQ`|`()`|`System.Windows.Forms.CheckBox [class:257]`|`1090480`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c2b`|`wxR0Hbdsfdn`|`System.Windows.Forms.CheckBox [class:257]`|`void`|`1090496`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c2c`|`JR80HIGQEpV`|`()`|`System.Windows.Forms.Label [class:245]`|`1090884`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c2d`|`XpF0Hy5CRcI`|`System.Windows.Forms.Label [class:245]`|`void`|`1090900`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c2e`|`GYC0HRybQkV`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1091004`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c2f`|`P5U0HgruvJL`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1091020`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c30`|`sih0HhYVvoG`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1091388`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c31`|`YKk0HZB4IKZ`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1091404`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c32`|`Sal0HeOwbs1`|`()`|`System.Windows.Forms.Label [class:245]`|`1091788`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c33`|`bfu0HLcn5Jf`|`System.Windows.Forms.Label [class:245]`|`void`|`1091804`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c34`|`pc10HTj4ETU`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1091908`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c35`|`bA10HfgKOmh`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1091924`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c36`|`d9c0HcU5UcG`|`()`|`System.Windows.Forms.Label [class:245]`|`1092252`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c37`|`sJ10H6hqL71`|`System.Windows.Forms.Label [class:245]`|`void`|`1092268`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c38`|`rox0HWK3cyv`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1092372`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c39`|`HKQ0HsTok5Y`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1092388`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c3a`|`UFX0HtrixJZ`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1092776`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c3b`|`TRh0Hd8yj8F`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1092792`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c3c`|`fHx0HJc4Tmn`|`()`|`System.Windows.Forms.Label [class:245]`|`1093156`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c3d`|`aXw0HrUZ64t`|`System.Windows.Forms.Label [class:245]`|`void`|`1093172`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c3e`|`Wms0HGwf4Be`|`()`|`System.Windows.Forms.Label [class:245]`|`1093276`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c3f`|`uga0HlA0a8q`|`System.Windows.Forms.Label [class:245]`|`void`|`1093292`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c40`|`hO90HukWmN4`|`()`|`System.Windows.Forms.Label [class:245]`|`1093396`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c41`|`fTB0HH4a5UD`|`System.Windows.Forms.Label [class:245]`|`void`|`1093412`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c42`|`trs0HpnpmaG`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1093516`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c43`|`RBK0HwcJgI2`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1093532`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c44`|`MRM0H2F778k`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1093960`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c45`|`qaO0HXHsL07`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1093976`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c46`|`mOT0HiE7jYf`|`()`|`System.Windows.Forms.Label [class:245]`|`1094300`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c47`|`HVd0Hn13V6L`|`System.Windows.Forms.Label [class:245]`|`void`|`1094316`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c48`|`hKx0H5NQIt0`|`()`|`System.Windows.Forms.RadioButton [class:253]`|`1094420`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c49`|`O4N0HSSHJRv`|`System.Windows.Forms.RadioButton [class:253]`|`void`|`1094436`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c4a`|`ad10HjIkMay`|`()`|`System.Windows.Forms.CheckBox [class:257]`|`1094820`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c4b`|`uga0Hx2drge`|`System.Windows.Forms.CheckBox [class:257]`|`void`|`1094836`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c4c`|`KPM0HDIvyPx`|`()`|`System.Windows.Forms.CheckBox [class:257]`|`1095204`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c4d`|`EEY0HB0wL6Z`|`System.Windows.Forms.CheckBox [class:257]`|`void`|`1095220`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c4e`|`RWTKh8wIWQ`|`object, System.EventArgs [class:113]`|`void`|`1095628`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c4f`|`FirstLoad`|`()`|`void`|`1096192`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c50`|`sBtKZljcUk`|`()`|`void`|`1097488`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c51`|`t8nKeq1xAK`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`1103772`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c52`|`NLbKLCp2w1`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`1104228`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c53`|`ElSKThbdcl`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`1104468`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c54`|`WuJKfVTV3h`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`1104904`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c55`|`ve_mc_mong`|`()`|`void`|`1105064`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c56`|`getNameFromTB`|`string`|`string`|`1117972`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c57`|`createTextBox`|`Kata_Class_Lib_Revit.Line_diem [class:789], Kata_Class_Lib_Revit.PreviewCanvas [class:261], System.Windows.Forms.GroupBox [class:241], System.Drawing.Point [valuetype:173], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], r8`|`void`|`1118488`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c58`|`doi_location`|`System.Drawing.Point [valuetype:173], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], r8`|`void`|`1119640`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c59`|`layTapCurveDNMB`|`Kata_Class_Lib_Revit.diem [class:85]`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Line_diem [class:789]>`|`1120728`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c5a`|`layTapCurveTNMB`|`Kata_Class_Lib_Revit.diem [class:85]`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Line_diem [class:789]>`|`1122124`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c5b`|`layTapCurveDDMB`|`Kata_Class_Lib_Revit.diem [class:85]`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Line_diem [class:789]>`|`1123396`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c5c`|`layTapCurveTDMB`|`Kata_Class_Lib_Revit.diem [class:85]`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Line_diem [class:789]>`|`1124832`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c5d`|`tao_dim_tu_tap_XYZ3`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, System.Drawing.Point [valuetype:173], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], r8, System.Drawing.Color [valuetype:697]`|`void`|`1126020`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c5e`|`tao_dim_tu_tap_XYZ2`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, System.Drawing.Point [valuetype:173], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], r8, System.Drawing.Color [valuetype:697]`|`void`|`1126420`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c5f`|`tao_dim_tu_tap_XYZ1`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, System.Drawing.Point [valuetype:173], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], r8, System.Drawing.Color [valuetype:697]`|`void`|`1126820`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c60`|`XuNKcUVeAa`|`object, System.EventArgs [class:113]`|`void`|`1127268`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c61`|`Os9K6rpiaR`|`object, System.EventArgs [class:113]`|`void`|`1127412`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c62`|`M3YKWlv1ww`|`()`|`void`|`1127556`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c63`|`vtpKsFLfk3`|`object, System.EventArgs [class:113]`|`void`|`1128040`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c64`|`KxdKtZKgWV`|`object, System.EventArgs [class:113]`|`void`|`1128248`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c65`|`c72KdJhuKX`|`object, System.EventArgs [class:113]`|`void`|`1128932`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c66`|`V8lKJh0st5`|`object, System.EventArgs [class:113]`|`void`|`1129224`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c67`|`e0qKrkmTGd`|`object, System.EventArgs [class:113]`|`void`|`1129416`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c68`|`qWAKGlHwYu`|`object, System.EventArgs [class:113]`|`void`|`1129568`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c69`|`ObxKlcqtP2`|`object, System.EventArgs [class:113]`|`void`|`1129748`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c6a`|`rg0KuCLA49`|`object, System.EventArgs [class:113]`|`void`|`1129928`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c6b`|`lchKHoWWtQ`|`object, System.EventArgs [class:113]`|`void`|`1130080`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c6c`|`kSLKpQWCEL`|`object, System.EventArgs [class:113]`|`void`|`1130248`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c6d`|`IWIKwMFpFo`|`object, System.EventArgs [class:113]`|`void`|`1130400`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c6e`|`kN1K290dVy`|`object, System.EventArgs [class:113]`|`void`|`1130552`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c6f`|`vYqKXlWkeS`|`object, System.EventArgs [class:113]`|`void`|`1130720`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c70`|`Sn2KijTB6L`|`object, System.EventArgs [class:113]`|`void`|`1130912`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c71`|`setDefault`|`()`|`void`|`1131080`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c72`|`jIhKnVRqIV`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`Dlcot [class:3024]`|`1133692`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c73`|`doi_du_lieu`|`bool`|`void`|`1135284`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c74`|`TZ9K5mxgiP`|`object, System.EventArgs [class:113]`|`void`|`1135800`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c75`|`QM4KS3kndH`|`object, System.Windows.Forms.FormClosingEventArgs [class:301]`|`void`|`1135988`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c76`|`gTcKjYyV12`|`object, System.EventArgs [class:113]`|`void`|`1136216`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c77`|`D7YKxBQbt6`|`object, System.EventArgs [class:113]`|`void`|`1136368`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c78`|`EvoKDImvJu`|`object, System.EventArgs [class:113]`|`void`|`1136520`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c79`|`d7kKBNh0iH`|`object, System.EventArgs [class:113]`|`void`|`1136672`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c7a`|`Sh0KOskGDi`|`object, System.EventArgs [class:113]`|`void`|`1136840`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c7b`|`EGQKVy6W3V`|`object, System.EventArgs [class:113]`|`void`|`1136992`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c7c`|`ojAKayPX7G`|`object, System.EventArgs [class:113]`|`void`|`1137144`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c7d`|`W48KzGdrMj`|`object, System.EventArgs [class:113]`|`void`|`1137312`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c7e`|`AR7NUl5hpd`|`object, System.EventArgs [class:113]`|`void`|`1137480`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c7f`|`Se2N8d6CZR`|`object, System.EventArgs [class:113]`|`void`|`1137672`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c80`|`MqbNEt5M4A`|`object, System.EventArgs [class:113]`|`void`|`1137840`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c81`|`ONhN0l04ha`|`object, System.EventArgs [class:113]`|`void`|`1138088`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c82`|`xyXNPsguL0`|`object, System.EventArgs [class:113]`|`void`|`1138240`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c83`|`hMoN3QWPJJ`|`object, System.EventArgs [class:113]`|`void`|`1138392`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c84`|`fDcNq4g0LB`|`object, System.EventArgs [class:113]`|`void`|`1138560`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c85`|`HN4NAoUNK0`|`object, System.EventArgs [class:113]`|`void`|`1138712`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c86`|`e0jNCJilMn`|`object, System.EventArgs [class:113]`|`void`|`1138864`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c87`|`ExeNm8cdmi`|`object, System.EventArgs [class:113]`|`void`|`1139016`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c88`|`p9INQqYZB4`|`object, System.EventArgs [class:113]`|`void`|`1139168`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c89`|`tC2Nkia63V`|`object, System.EventArgs [class:113]`|`void`|`1139336`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c8a`|`D4cN4Ji5fI`|`object, System.EventArgs [class:113]`|`void`|`1139488`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c8b`|`FvvNYgpc1B`|`object, System.EventArgs [class:113]`|`void`|`1139680`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c8c`|`rjeNoWN75v`|`object, System.EventArgs [class:113]`|`void`|`1139848`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c8d`|`r8ON74Xo82`|`object, System.EventArgs [class:113]`|`void`|`1140000`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c8e`|`grUNFuuFC6`|`object, System.EventArgs [class:113]`|`void`|`1140192`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c8f`|`CGHNv6FPnu`|`object, System.EventArgs [class:113]`|`void`|`1140384`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c90`|`iS1N1owabK`|`object, System.EventArgs [class:113]`|`void`|`1140552`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c91`|`pbONMCxQgd`|`object, System.EventArgs [class:113]`|`void`|`1140704`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c92`|`dVNN9c5LLf`|`object, System.EventArgs [class:113]`|`void`|`1140896`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c93`|`CC0NKMRPD5`|`object, System.EventArgs [class:113]`|`void`|`1141048`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c94`|`oiQNNHNJll`|`object, System.EventArgs [class:113]`|`void`|`1141200`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c95`|`CyUNbbmyMW`|`object, System.EventArgs [class:113]`|`void`|`1141688`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c96`|`cENNIN0DlG`|`object, System.EventArgs [class:113]`|`void`|`1143900`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c97`|`xFiNyu7C6x`|`object, System.EventArgs [class:113]`|`void`|`1145224`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c98`|`Gf2NRi9J0s`|`string, InfoMongCocSave [class:3028]`|`void`|`1147400`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c99`|`yH7NgPpsCY`|`string`|`InfoMongCocSave [class:3028]`|`1147560`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000c9a`|`CuKNh7Iyau`|`object, System.EventArgs [class:113]`|`void`|`1147588`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c9b`|`UIyNZFqs0U`|`object, System.EventArgs [class:113]`|`void`|`1151732`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c9c`|`IZtNeckSgV`|`object, System.EventArgs [class:113]`|`void`|`1151912`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c9d`|`PKPNLb7bYb`|`object, System.EventArgs [class:113]`|`void`|`1152080`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c9e`|`YfjNT4cOZI`|`object, System.EventArgs [class:113]`|`void`|`1152532`|`4`|`ae3c8b8d99a39542...`|
|`0x06000c9f`|`LIKNfctYcL`|`object, System.EventArgs [class:113]`|`void`|`1152700`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ca0`|`BQPNc0Q7AK`|`object, System.EventArgs [class:113]`|`void`|`1154252`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ca1`|`jfQN6c7HW2`|`object, System.EventArgs [class:113]`|`void`|`1154432`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ca2`|`.cctor`|`()`|`void`|`1154600`|`156`|`dc5c142ad1999688...`|
|`0x06000ca3`|`cERSSqqEl5bkjTF5omev`|`()`|`bool`|`1154768`|`4`|`66ea1533cb3404e9...`|
|`0x06000ca4`|`PnbwCvqEuBkWgMo6Ps0T`|`()`|`Kata_pro64_Cad2025.Form_mong_coc [class:460]`|`1154788`|`4`|`8969ce0cdaa4a19c...`|

### [VM-191] Methods of `Kata_pro64_Cad2025.Form_Mong_Coc_V2` (Token `0x02000074`, Displayed: 406/406, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06000ca5`|`.ctor`|`()`|`void`|`1154804`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ca6`|`Dispose`|`bool`|`void`|`1155244`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ca7`|`HGMIXbCbui`|`()`|`void`|`1155652`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ca8`|`SUh0HOhAgVG`|`()`|`System.Windows.Forms.Label [class:245]`|`1240124`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ca9`|`yFs0HVsrrJm`|`System.Windows.Forms.Label [class:245]`|`void`|`1240140`|`4`|`ae3c8b8d99a39542...`|
|`0x06000caa`|`C290HaleYvd`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1240244`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cab`|`KyG0HzMVerS`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1240260`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cac`|`J6Y0pU5wTfY`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1240364`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cad`|`NZT0p8WlBt9`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1240380`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cae`|`YX50pEg0STI`|`()`|`System.Windows.Forms.Label [class:245]`|`1240708`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000caf`|`bJE0p0ojNmE`|`System.Windows.Forms.Label [class:245]`|`void`|`1240724`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cb0`|`zx70pPSntWA`|`()`|`System.Windows.Forms.Label [class:245]`|`1240828`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cb1`|`fSG0p3B3UZZ`|`System.Windows.Forms.Label [class:245]`|`void`|`1240844`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cb2`|`ILr0pqM40b8`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1240948`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cb3`|`TVe0pAgd7ke`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1240964`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cb4`|`GKX0pCvUOW8`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1241500`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cb5`|`QJ20pmgF26C`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1241516`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cb6`|`wMS0pQiaqWD`|`()`|`System.Windows.Forms.Label [class:245]`|`1241620`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cb7`|`GZP0pkbUxpx`|`System.Windows.Forms.Label [class:245]`|`void`|`1241636`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cb8`|`dLw0p4fXJV0`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1241740`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cb9`|`Yp80pY9Z46v`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1241756`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cba`|`LkJ0poMX78r`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1241860`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cbb`|`eip0p7OQ5eN`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1241876`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cbc`|`F450h5Oc62a`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1241980`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cbd`|`ec30hSAXSkn`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1241996`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cbe`|`Uup0pFxhALq`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1242800`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cbf`|`GrV0pv0NxhU`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1242816`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cc0`|`zSN0p1MSHRb`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1243224`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cc1`|`bHF0pMhsPV1`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1243240`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cc2`|`jxs0p9XcNLT`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1243584`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cc3`|`Rbo0pKJLwLH`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1243600`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cc4`|`sxf0rh7JXo8`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1244028`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cc5`|`rEP0rZD4Byv`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1244044`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cc6`|`of00pNEnUJ0`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1244372`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cc7`|`xgF0pb4jpvV`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1244388`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cc8`|`E8Z0pIAX7kh`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1244836`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cc9`|`ClW0py4K3Ox`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1244852`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cca`|`NVa0rcTMJNs`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1245200`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ccb`|`uS90r6yrNwX`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1245216`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ccc`|`Ovr0rWl9rLn`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1245624`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ccd`|`fk20rsjNplb`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1245640`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cce`|`YBu0ruu34tq`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1245984`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ccf`|`HAi0rHL6ND1`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1246000`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cd0`|`n7Q0rJ22Ov0`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1246408`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cd1`|`aDt0rrnS17J`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1246424`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cd2`|`se20rGnYrwe`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1246748`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cd3`|`NdX0rlZGreQ`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1246764`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cd4`|`VBm0pRJBPEy`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1247148`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cd5`|`Tt00pgaPg1V`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1247164`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cd6`|`MEA0Whdvjy0`|`()`|`System.Windows.Forms.Button [class:161]`|`1247532`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cd7`|`f5o0WZkHB80`|`System.Windows.Forms.Button [class:161]`|`void`|`1247548`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cd8`|`Cmt0phWrFTc`|`()`|`System.Windows.Forms.Button [class:161]`|`1247916`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cd9`|`VNw0pZmtpAq`|`System.Windows.Forms.Button [class:161]`|`void`|`1247932`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cda`|`k7R0huRyZOy`|`()`|`System.Windows.Forms.BindingSource [class:237]`|`1248280`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cdb`|`zNn0hHpucM0`|`System.Windows.Forms.BindingSource [class:237]`|`void`|`1248296`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cdc`|`SXJ0pebV9nj`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1248400`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cdd`|`Jpd0pL5jAXj`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1248416`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cde`|`hbC0pTE8dFc`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1248520`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cdf`|`lyd0pfxvp9N`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1248536`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ce0`|`MV20pc6BB1R`|`()`|`System.Windows.Forms.CheckBox [class:257]`|`1248968`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ce1`|`Gpt0p6ocS1G`|`System.Windows.Forms.CheckBox [class:257]`|`void`|`1248984`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ce2`|`dd90pWpOyTn`|`()`|`System.Windows.Forms.CheckBox [class:257]`|`1249432`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ce3`|`Rpd0psEYYQT`|`System.Windows.Forms.CheckBox [class:257]`|`void`|`1249448`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ce4`|`QP60ptruBat`|`()`|`System.Windows.Forms.Label [class:245]`|`1249836`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ce5`|`ptK0pdhTAAy`|`System.Windows.Forms.Label [class:245]`|`void`|`1249852`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ce6`|`ToG0pJlOwvh`|`()`|`System.Windows.Forms.Label [class:245]`|`1249956`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ce7`|`Mf80prV3Qte`|`System.Windows.Forms.Label [class:245]`|`void`|`1249972`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ce8`|`GME0pGKcyeX`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1250076`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ce9`|`FjO0plxB886`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1250092`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cea`|`UDY0puGESUI`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1250732`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ceb`|`xEp0pHq1ysE`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1250748`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cec`|`JUM0ppw0vsT`|`()`|`System.Windows.Forms.Label [class:245]`|`1251240`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ced`|`uAt0pwVwnfd`|`System.Windows.Forms.Label [class:245]`|`void`|`1251256`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cee`|`zb00p2imvfX`|`()`|`System.Windows.Forms.Label [class:245]`|`1251360`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cef`|`aQD0pXmm9nB`|`System.Windows.Forms.Label [class:245]`|`void`|`1251376`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cf0`|`e5O0pixAG0v`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1251480`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cf1`|`XHo0pnMiXL8`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1251496`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cf2`|`lg80p5ttAPf`|`()`|`System.Windows.Forms.Label [class:245]`|`1251600`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cf3`|`eGl0pSsKZQ7`|`System.Windows.Forms.Label [class:245]`|`void`|`1251616`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cf4`|`EbG0pjApD2b`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1251720`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cf5`|`I2a0pxfPHPd`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1251736`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cf6`|`YCN0pDdirNS`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1252292`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cf7`|`X0I0pBMCRuw`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1252308`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cf8`|`LmU0pO8NBDr`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1252656`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cf9`|`yum0pVvFv1A`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1252672`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cfa`|`pvu0pajA1CX`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1253016`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cfb`|`eGJ0pz5NYBF`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1253032`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cfc`|`bO90wUjPHi3`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1253420`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cfd`|`pSV0w8fytgB`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1253436`|`4`|`ae3c8b8d99a39542...`|
|`0x06000cfe`|`CYO0wEfm3eF`|`()`|`System.Windows.Forms.Label [class:245]`|`1253820`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000cff`|`aDr0w094ueJ`|`System.Windows.Forms.Label [class:245]`|`void`|`1253836`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d00`|`b2h0wPqT1xq`|`()`|`System.Windows.Forms.RadioButton [class:253]`|`1253940`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d01`|`DIS0w33o0iE`|`System.Windows.Forms.RadioButton [class:253]`|`void`|`1253956`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d02`|`N0q0wq4RkWY`|`()`|`System.Windows.Forms.RadioButton [class:253]`|`1254060`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d03`|`C4e0wAAYfNl`|`System.Windows.Forms.RadioButton [class:253]`|`void`|`1254076`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d04`|`AdQ0wCOp6UA`|`()`|`System.Windows.Forms.Label [class:245]`|`1254464`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d05`|`h6e0wmHAWyF`|`System.Windows.Forms.Label [class:245]`|`void`|`1254480`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d06`|`Hw00wQR0YWL`|`()`|`System.Windows.Forms.RadioButton [class:253]`|`1254584`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d07`|`zC40wk05PBo`|`System.Windows.Forms.RadioButton [class:253]`|`void`|`1254600`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d08`|`y8m0w4Qe4Cd`|`()`|`System.Windows.Forms.RadioButton [class:253]`|`1254704`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d09`|`m9p0wYFTpu3`|`System.Windows.Forms.RadioButton [class:253]`|`void`|`1254720`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d0a`|`emw0woFiXNP`|`()`|`System.Windows.Forms.Label [class:245]`|`1255088`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d0b`|`XaW0w7Bo2cx`|`System.Windows.Forms.Label [class:245]`|`void`|`1255104`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d0c`|`Ga20wFYxsk1`|`()`|`System.Windows.Forms.Label [class:245]`|`1255208`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d0d`|`mWm0wvKUjOo`|`System.Windows.Forms.Label [class:245]`|`void`|`1255224`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d0e`|`fi40w1N90gK`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1255328`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d0f`|`J1N0wM9Sn14`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1255344`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d10`|`oeS0w9v5OT5`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1255752`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d11`|`M5x0wKr2b8R`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1255768`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d12`|`C2p0wNWMjrX`|`()`|`System.Windows.Forms.Label [class:245]`|`1256096`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d13`|`yuQ0wbkGGgw`|`System.Windows.Forms.Label [class:245]`|`void`|`1256112`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d14`|`kFO0wI53e2k`|`()`|`System.Windows.Forms.RadioButton [class:253]`|`1256216`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d15`|`VpQ0wykqOvV`|`System.Windows.Forms.RadioButton [class:253]`|`void`|`1256232`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d16`|`c5r0wRRxFSF`|`()`|`System.Windows.Forms.RadioButton [class:253]`|`1256600`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d17`|`QmF0wgldBIt`|`System.Windows.Forms.RadioButton [class:253]`|`void`|`1256616`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d18`|`vbM0whOLaX4`|`()`|`System.Windows.Forms.Label [class:245]`|`1257004`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d19`|`UYK0wZhL7cn`|`System.Windows.Forms.Label [class:245]`|`void`|`1257020`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d1a`|`HIk0wevB9qf`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1257124`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d1b`|`IrQ0wLfoOF5`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1257140`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d1c`|`Nsd0wT9lTEV`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1257244`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d1d`|`Cl00wf00dqk`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1257260`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d1e`|`sg30wc1Jc1c`|`()`|`System.Windows.Forms.Button [class:161]`|`1257628`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d1f`|`nxg0w6g7BVH`|`System.Windows.Forms.Button [class:161]`|`void`|`1257644`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d20`|`JYV0wWkIgy9`|`()`|`System.Windows.Forms.Button [class:161]`|`1258028`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d21`|`KsK0ws7ocYm`|`System.Windows.Forms.Button [class:161]`|`void`|`1258044`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d22`|`VWX0wtCAmPm`|`()`|`System.Windows.Forms.Button [class:161]`|`1258388`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d23`|`JRv0wd8o98y`|`System.Windows.Forms.Button [class:161]`|`void`|`1258404`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d24`|`fUo0saMpnbr`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1258772`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d25`|`Gr10szj6IhN`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1258788`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d26`|`fQG0wJREjcl`|`()`|`System.Windows.Forms.Label [class:245]`|`1258892`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d27`|`PuR0wr5YKfl`|`System.Windows.Forms.Label [class:245]`|`void`|`1258908`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d28`|`fD90wGI66hk`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1259012`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d29`|`YxJ0wlFnj8u`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1259028`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d2a`|`MZn0fe7UJWt`|`()`|`System.Windows.Forms.Panel [class:373]`|`1259416`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d2b`|`d5N0fL1RpR2`|`System.Windows.Forms.Panel [class:373]`|`void`|`1259432`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d2c`|`krd0wuZpcRV`|`()`|`System.Windows.Forms.Button [class:161]`|`1259536`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d2d`|`Qqr0wHns6aZ`|`System.Windows.Forms.Button [class:161]`|`void`|`1259552`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d2e`|`MsR0wpT6JR7`|`()`|`System.Windows.Forms.Label [class:245]`|`1259980`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d2f`|`HpE0wwWGe6i`|`System.Windows.Forms.Label [class:245]`|`void`|`1259996`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d30`|`gT10w2AmIQc`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1260100`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d31`|`fp20wXdw5EI`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1260116`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d32`|`Qnj0witL5Js`|`()`|`System.Windows.Forms.Label [class:245]`|`1260712`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d33`|`SDj0wnL2OMm`|`System.Windows.Forms.Label [class:245]`|`void`|`1260728`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d34`|`hi90w5lfAnF`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1260832`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d35`|`vpR0wSTc2eL`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1260848`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d36`|`fIZ0wjJBDZH`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1261424`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d37`|`KZL0wxrleBv`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1261440`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d38`|`Vqn0wDqssTL`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1261824`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d39`|`TeI0wBhn2Gy`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1261840`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d3a`|`Jl40wOg8mv2`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1262184`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d3b`|`FXc0wVIcL0u`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1262200`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d3c`|`Lp90waI0f5K`|`()`|`System.Windows.Forms.CheckBox [class:257]`|`1262544`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d3d`|`cyH0wziyvUc`|`System.Windows.Forms.CheckBox [class:257]`|`void`|`1262560`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d3e`|`iip02U2Y3wA`|`()`|`System.Windows.Forms.ComboBox [class:377]`|`1262988`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d3f`|`Cby028V0dwS`|`System.Windows.Forms.ComboBox [class:377]`|`void`|`1263004`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d40`|`p6102EIwU7o`|`()`|`System.Windows.Forms.Label [class:245]`|`1263392`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d41`|`NB8020UPWWP`|`System.Windows.Forms.Label [class:245]`|`void`|`1263408`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d42`|`qFh02PuZbX2`|`()`|`System.Windows.Forms.Button [class:161]`|`1263512`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d43`|`wqg023ISDgj`|`System.Windows.Forms.Button [class:161]`|`void`|`1263528`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d44`|`cT30uOxCFFk`|`()`|`System.Windows.Forms.TabControl [class:365]`|`1263876`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d45`|`Wyl0uVj9JTM`|`System.Windows.Forms.TabControl [class:365]`|`void`|`1263892`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d46`|`JXO0uam1SIk`|`()`|`System.Windows.Forms.TabPage [class:369]`|`1264320`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d47`|`gJN0uzFHuyF`|`System.Windows.Forms.TabPage [class:369]`|`void`|`1264336`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d48`|`clG0HUwX9lY`|`()`|`System.Windows.Forms.Panel [class:373]`|`1264440`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d49`|`ciK0H8pYD50`|`System.Windows.Forms.Panel [class:373]`|`void`|`1264456`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d4a`|`LsG02qxXpFh`|`()`|`System.Windows.Forms.Label [class:245]`|`1264560`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d4b`|`jv302ARePPo`|`System.Windows.Forms.Label [class:245]`|`void`|`1264576`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d4c`|`Vdh02CqO8we`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1264680`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d4d`|`kio02mGBuNx`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1264696`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d4e`|`nLl02QQO6kt`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1265084`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d4f`|`hK902kVST3V`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1265100`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d50`|`XOj024LTmIB`|`()`|`System.Windows.Forms.Label [class:245]`|`1265468`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d51`|`w0i02YNUof3`|`System.Windows.Forms.Label [class:245]`|`void`|`1265484`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d52`|`Mxl02ojVNiZ`|`()`|`System.Windows.Forms.Label [class:245]`|`1265588`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d53`|`LU0027PKaFt`|`System.Windows.Forms.Label [class:245]`|`void`|`1265604`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d54`|`Sci02F4vCOE`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1265708`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d55`|`K0Q02vsN1Pg`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1265724`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d56`|`B6E021ZG0P3`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1266172`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d57`|`ba202Mr0xYI`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1266188`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d58`|`Iq9029NfEwy`|`()`|`System.Windows.Forms.Label [class:245]`|`1266680`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d59`|`vjk02Kkpi3k`|`System.Windows.Forms.Label [class:245]`|`void`|`1266696`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d5a`|`Xps02NFDVvX`|`()`|`System.Windows.Forms.Label [class:245]`|`1266800`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d5b`|`Ohv02bPnmly`|`System.Windows.Forms.Label [class:245]`|`void`|`1266816`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d5c`|`oM302IBCtL5`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1266920`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d5d`|`LiA02yf5FBg`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1266936`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d5e`|`IPG02RCPhry`|`()`|`System.Windows.Forms.Label [class:245]`|`1267344`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d5f`|`ftn02gynGSE`|`System.Windows.Forms.Label [class:245]`|`void`|`1267360`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d60`|`H1v02hExSvY`|`()`|`System.Windows.Forms.Label [class:245]`|`1267464`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d61`|`H5u02ZbxqMu`|`System.Windows.Forms.Label [class:245]`|`void`|`1267480`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d62`|`n4r02e2VBNi`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1267584`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d63`|`q6g02LQ0tMO`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1267600`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d64`|`eAR02TNfWNI`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1267704`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d65`|`k4G02fEKZtX`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1267720`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d66`|`loQ02ciph5x`|`()`|`System.Windows.Forms.CheckBox [class:257]`|`1268108`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d67`|`L1X026De8Q0`|`System.Windows.Forms.CheckBox [class:257]`|`void`|`1268124`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d68`|`RqJ02W2JG2e`|`()`|`System.Windows.Forms.Label [class:245]`|`1268488`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d69`|`UoW02sInmLv`|`System.Windows.Forms.Label [class:245]`|`void`|`1268504`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d6a`|`kLh02tScgA5`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1268608`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d6b`|`tds02dLG4oU`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1268624`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d6c`|`JuZ02J5Ig8Z`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1268728`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d6d`|`ojD02r9Z0eM`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1268744`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d6e`|`vr602GUfgOG`|`()`|`System.Windows.Forms.Label [class:245]`|`1269072`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d6f`|`Kf302lCZLWo`|`System.Windows.Forms.Label [class:245]`|`void`|`1269088`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d70`|`r7502ulR6RS`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1269192`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d71`|`JwB02H9ZbAr`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1269208`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d72`|`XlT02pDQKT4`|`()`|`System.Windows.Forms.Label [class:245]`|`1269552`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d73`|`txL02w9lNXl`|`System.Windows.Forms.Label [class:245]`|`void`|`1269568`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d74`|`YvE022T3L4J`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1269936`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d75`|`zT702X0lY9N`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1269952`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d76`|`TNC02i3SJqN`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1270056`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d77`|`Tsn02nIQTBx`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1270072`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d78`|`YOZ0250aXJ9`|`()`|`System.Windows.Forms.Label [class:245]`|`1270416`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d79`|`zJT02S5SxCU`|`System.Windows.Forms.Label [class:245]`|`void`|`1270432`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d7a`|`ngV02j9MB1m`|`()`|`System.Windows.Forms.Label [class:245]`|`1270536`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d7b`|`CyY02xfY9TK`|`System.Windows.Forms.Label [class:245]`|`void`|`1270552`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d7c`|`JNZ02DlawZ1`|`()`|`System.Windows.Forms.Label [class:245]`|`1270940`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d7d`|`auq02BKbx8Y`|`System.Windows.Forms.Label [class:245]`|`void`|`1270956`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d7e`|`xwI02O2Y0hL`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1271060`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d7f`|`t4902VeeHlc`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1271076`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d80`|`aUC02aPZeiB`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1271484`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d81`|`Ui102zIPmQi`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1271500`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d82`|`xny0XUQT505`|`()`|`System.Windows.Forms.Label [class:245]`|`1272076`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d83`|`j0X0X8FlA15`|`System.Windows.Forms.Label [class:245]`|`void`|`1272092`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d84`|`HDs0XEQ2dc3`|`()`|`System.Windows.Forms.RadioButton [class:253]`|`1272196`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d85`|`xlV0X0OQQE0`|`System.Windows.Forms.RadioButton [class:253]`|`void`|`1272212`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d86`|`KPM0HDIvyPx`|`()`|`System.Windows.Forms.CheckBox [class:257]`|`1272316`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d87`|`EEY0HB0wL6Z`|`System.Windows.Forms.CheckBox [class:257]`|`void`|`1272332`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d88`|`ad10HjIkMay`|`()`|`System.Windows.Forms.CheckBox [class:257]`|`1272700`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d89`|`uga0Hx2drge`|`System.Windows.Forms.CheckBox [class:257]`|`void`|`1272716`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d8a`|`uwh0XPb4EOO`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1273104`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d8b`|`EWB0X3ZrL3j`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1273120`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d8c`|`QCy0XqfuMMu`|`()`|`System.Windows.Forms.Label [class:245]`|`1273224`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d8d`|`PWk0XAe7iIy`|`System.Windows.Forms.Label [class:245]`|`void`|`1273240`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d8e`|`SHt0XCt3QfC`|`()`|`System.Windows.Forms.Button [class:161]`|`1273344`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d8f`|`yVh0XmvrAg1`|`System.Windows.Forms.Button [class:161]`|`void`|`1273360`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d90`|`XU70sFePXTb`|`()`|`System.Windows.Forms.CheckBox [class:257]`|`1273728`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d91`|`hbF0svT2noA`|`System.Windows.Forms.CheckBox [class:257]`|`void`|`1273744`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d92`|`XPo06cdQxL3`|`()`|`System.Windows.Forms.Panel [class:373]`|`1273848`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d93`|`gjs066x9fbq`|`System.Windows.Forms.Panel [class:373]`|`void`|`1273864`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d94`|`ORS0cIBi4hZ`|`()`|`System.Windows.Forms.Panel [class:373]`|`1273968`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d95`|`lur0cyG9Amp`|`System.Windows.Forms.Panel [class:373]`|`void`|`1273984`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d96`|`sNL0XQDeTlQ`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1274088`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d97`|`hJk0XkqWusH`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1274104`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d98`|`xGc0X41cVdA`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1274208`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d99`|`m930XYy58bF`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1274224`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d9a`|`aB30XoFKrUl`|`()`|`System.Windows.Forms.Label [class:245]`|`1274740`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d9b`|`dIt0X7Nl3fr`|`System.Windows.Forms.Label [class:245]`|`void`|`1274756`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d9c`|`SLu0XFlIK89`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1274860`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d9d`|`lIP0XvMWFuU`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1274876`|`4`|`ae3c8b8d99a39542...`|
|`0x06000d9e`|`s8Y0X19bPT8`|`()`|`System.Windows.Forms.Label [class:245]`|`1275244`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000d9f`|`r4P0XMZcUGU`|`System.Windows.Forms.Label [class:245]`|`void`|`1275260`|`4`|`ae3c8b8d99a39542...`|
|`0x06000da0`|`BJ20X9sKdMw`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1275364`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000da1`|`NgH0XK9OKTb`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1275380`|`4`|`ae3c8b8d99a39542...`|
|`0x06000da2`|`RHN0XNHBs7w`|`()`|`System.Windows.Forms.Label [class:245]`|`1275936`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000da3`|`UwC0Xb9M9Sc`|`System.Windows.Forms.Label [class:245]`|`void`|`1275952`|`4`|`ae3c8b8d99a39542...`|
|`0x06000da4`|`AoX0XIyyJ4b`|`()`|`System.Windows.Forms.Label [class:245]`|`1276056`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000da5`|`B7S0XyMRXkw`|`System.Windows.Forms.Label [class:245]`|`void`|`1276072`|`4`|`ae3c8b8d99a39542...`|
|`0x06000da6`|`EBF0XRbcaoe`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1276176`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000da7`|`BZP0XgX5x9O`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1276192`|`4`|`ae3c8b8d99a39542...`|
|`0x06000da8`|`RQT0XhhnB7O`|`()`|`System.Windows.Forms.TabControl [class:365]`|`1276728`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000da9`|`Tys0XZ0C68T`|`System.Windows.Forms.TabControl [class:365]`|`void`|`1276744`|`4`|`ae3c8b8d99a39542...`|
|`0x06000daa`|`Fju0XeUb9he`|`()`|`System.Windows.Forms.TabPage [class:369]`|`1277112`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000dab`|`Whj0XLtZr47`|`System.Windows.Forms.TabPage [class:369]`|`void`|`1277128`|`4`|`ae3c8b8d99a39542...`|
|`0x06000dac`|`la30XTSNYNM`|`()`|`System.Windows.Forms.Panel [class:373]`|`1277232`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000dad`|`wj10XfLN27v`|`System.Windows.Forms.Panel [class:373]`|`void`|`1277248`|`4`|`ae3c8b8d99a39542...`|
|`0x06000dae`|`vwT0fDZbuBW`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1277352`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000daf`|`KRI0fBZR2oY`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1277368`|`4`|`ae3c8b8d99a39542...`|
|`0x06000db0`|`oGt0XcBWud4`|`()`|`System.Windows.Forms.Panel [class:373]`|`1277472`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000db1`|`kwv0X6U32ui`|`System.Windows.Forms.Panel [class:373]`|`void`|`1277488`|`4`|`ae3c8b8d99a39542...`|
|`0x06000db2`|`rQO0XW2UHiE`|`()`|`System.Windows.Forms.ComboBox [class:377]`|`1277592`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000db3`|`nlM0XsrcWxX`|`System.Windows.Forms.ComboBox [class:377]`|`void`|`1277608`|`4`|`ae3c8b8d99a39542...`|
|`0x06000db4`|`k660XtoYmmK`|`()`|`System.Windows.Forms.Label [class:245]`|`1277712`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000db5`|`LXu0Xdukq24`|`System.Windows.Forms.Label [class:245]`|`void`|`1277728`|`4`|`ae3c8b8d99a39542...`|
|`0x06000db6`|`Ij20XJKQ50B`|`()`|`System.Windows.Forms.Panel [class:373]`|`1277832`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000db7`|`kes0XrC8VqS`|`System.Windows.Forms.Panel [class:373]`|`void`|`1277848`|`4`|`ae3c8b8d99a39542...`|
|`0x06000db8`|`kCG0XGuMmm0`|`()`|`System.Windows.Forms.Label [class:245]`|`1277952`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000db9`|`Ibc0XlFDUPr`|`System.Windows.Forms.Label [class:245]`|`void`|`1277968`|`4`|`ae3c8b8d99a39542...`|
|`0x06000dba`|`muM0XuN2lGR`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1278072`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000dbb`|`Nhh0XHQ3IWs`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1278088`|`4`|`ae3c8b8d99a39542...`|
|`0x06000dbc`|`QvP0Xp7LRpR`|`()`|`System.Windows.Forms.Label [class:245]`|`1278436`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000dbd`|`ujN0XwSHKgF`|`System.Windows.Forms.Label [class:245]`|`void`|`1278452`|`4`|`ae3c8b8d99a39542...`|
|`0x06000dbe`|`mYy0X2pUvjL`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1278556`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000dbf`|`Cyl0XXBR7Gr`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1278572`|`4`|`ae3c8b8d99a39542...`|
|`0x06000dc0`|`p3W0XiPmcFL`|`()`|`System.Windows.Forms.Label [class:245]`|`1279044`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000dc1`|`XNC0XnOcDUW`|`System.Windows.Forms.Label [class:245]`|`void`|`1279060`|`4`|`ae3c8b8d99a39542...`|
|`0x06000dc2`|`ESe0X5Qr7PT`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1279164`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000dc3`|`jAo0XSyH5Yu`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1279180`|`4`|`ae3c8b8d99a39542...`|
|`0x06000dc4`|`bYH0XjJCDRT`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1279608`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000dc5`|`Eoo0Xxks7Io`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1279624`|`4`|`ae3c8b8d99a39542...`|
|`0x06000dc6`|`kHY0XDtIEfB`|`()`|`System.Windows.Forms.Label [class:245]`|`1279992`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000dc7`|`SFr0XBMusMr`|`System.Windows.Forms.Label [class:245]`|`void`|`1280008`|`4`|`ae3c8b8d99a39542...`|
|`0x06000dc8`|`NYZ0dtPlJLN`|`()`|`System.Windows.Forms.ComboBox [class:377]`|`1280112`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000dc9`|`pXK0ddr4Ce8`|`System.Windows.Forms.ComboBox [class:377]`|`void`|`1280128`|`4`|`ae3c8b8d99a39542...`|
|`0x06000dca`|`Orl0dJ1G72t`|`()`|`System.Windows.Forms.Label [class:245]`|`1280232`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000dcb`|`KHp0drNnS7v`|`System.Windows.Forms.Label [class:245]`|`void`|`1280248`|`4`|`ae3c8b8d99a39542...`|
|`0x06000dcc`|`W4a0dhWeo1c`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1280352`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000dcd`|`H8E0dZ3pKI2`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1280368`|`4`|`ae3c8b8d99a39542...`|
|`0x06000dce`|`awC0denngT9`|`()`|`System.Windows.Forms.Label [class:245]`|`1280756`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000dcf`|`tcq0dL4Yu7M`|`System.Windows.Forms.Label [class:245]`|`void`|`1280772`|`4`|`ae3c8b8d99a39542...`|
|`0x06000dd0`|`YkP0d9BCDiM`|`()`|`System.Windows.Forms.ComboBox [class:377]`|`1280876`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000dd1`|`YYG0dKoOOQo`|`System.Windows.Forms.ComboBox [class:377]`|`void`|`1280892`|`4`|`ae3c8b8d99a39542...`|
|`0x06000dd2`|`vOk0dNIClrL`|`()`|`System.Windows.Forms.Label [class:245]`|`1281344`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000dd3`|`sud0dbAHA6X`|`System.Windows.Forms.Label [class:245]`|`void`|`1281360`|`4`|`ae3c8b8d99a39542...`|
|`0x06000dd4`|`bYN0dcFxoRp`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1281464`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000dd5`|`IVX0d6Avgen`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1281480`|`4`|`ae3c8b8d99a39542...`|
|`0x06000dd6`|`SuC0dWef8Ma`|`()`|`System.Windows.Forms.Label [class:245]`|`1281848`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000dd7`|`fvm0dsASDM7`|`System.Windows.Forms.Label [class:245]`|`void`|`1281864`|`4`|`ae3c8b8d99a39542...`|
|`0x06000dd8`|`kAm0dRi6R5K`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1281968`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000dd9`|`umA0dgKJjRd`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1281984`|`4`|`ae3c8b8d99a39542...`|
|`0x06000dda`|`nr40d1mcQwJ`|`()`|`System.Windows.Forms.Label [class:245]`|`1282372`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ddb`|`et20dMNgD9G`|`System.Windows.Forms.Label [class:245]`|`void`|`1282388`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ddc`|`gE80XOltI4H`|`()`|`System.Windows.Forms.Button [class:161]`|`1282492`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ddd`|`ITq0XVKLCrZ`|`System.Windows.Forms.Button [class:161]`|`void`|`1282508`|`4`|`ae3c8b8d99a39542...`|
|`0x06000dde`|`PJD0XaAiytf`|`()`|`System.Windows.Forms.Panel [class:373]`|`1282872`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ddf`|`W3H0Xzl2eSn`|`System.Windows.Forms.Panel [class:373]`|`void`|`1282888`|`4`|`ae3c8b8d99a39542...`|
|`0x06000de0`|`imw0iUbKjmD`|`()`|`System.Windows.Forms.RadioButton [class:253]`|`1282992`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000de1`|`SKR0i8TdnaW`|`System.Windows.Forms.RadioButton [class:253]`|`void`|`1283008`|`4`|`ae3c8b8d99a39542...`|
|`0x06000de2`|`yHf0iEkoXPg`|`()`|`System.Windows.Forms.RadioButton [class:253]`|`1283436`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000de3`|`NIb0i0mdh3I`|`System.Windows.Forms.RadioButton [class:253]`|`void`|`1283452`|`4`|`ae3c8b8d99a39542...`|
|`0x06000de4`|`CCA0ZowoUwH`|`()`|`System.Windows.Forms.Label [class:245]`|`1283556`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000de5`|`s3g0Z7VmfVf`|`System.Windows.Forms.Label [class:245]`|`void`|`1283572`|`4`|`ae3c8b8d99a39542...`|
|`0x06000de6`|`nKw0iPRoq7S`|`()`|`System.Windows.Forms.Button [class:161]`|`1283676`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000de7`|`eQU0i3REuit`|`System.Windows.Forms.Button [class:161]`|`void`|`1283692`|`4`|`ae3c8b8d99a39542...`|
|`0x06000de8`|`pTr0iqvPglB`|`()`|`System.Windows.Forms.Button [class:161]`|`1284080`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000de9`|`WQp0iA4H65p`|`System.Windows.Forms.Button [class:161]`|`void`|`1284096`|`4`|`ae3c8b8d99a39542...`|
|`0x06000dea`|`SfL0iCJ5CgT`|`()`|`System.Windows.Forms.Label [class:245]`|`1284524`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000deb`|`bNb0im0KtTh`|`System.Windows.Forms.Label [class:245]`|`void`|`1284540`|`4`|`ae3c8b8d99a39542...`|
|`0x06000dec`|`Ii30iQVellC`|`()`|`System.Windows.Forms.Panel [class:373]`|`1284644`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ded`|`FoA0ikuoh9e`|`System.Windows.Forms.Panel [class:373]`|`void`|`1284660`|`4`|`ae3c8b8d99a39542...`|
|`0x06000dee`|`h0V0i41H6tR`|`()`|`System.Windows.Forms.Panel [class:373]`|`1284764`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000def`|`oY30iYrKSdR`|`System.Windows.Forms.Panel [class:373]`|`void`|`1284780`|`4`|`ae3c8b8d99a39542...`|
|`0x06000df0`|`aaC0ioNWT8P`|`()`|`System.Windows.Forms.Panel [class:373]`|`1284884`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000df1`|`UGi0i74Mq7c`|`System.Windows.Forms.Panel [class:373]`|`void`|`1284900`|`4`|`ae3c8b8d99a39542...`|
|`0x06000df2`|`bAC0iFcEGRJ`|`()`|`System.Windows.Forms.Panel [class:373]`|`1285004`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000df3`|`VvN0ivJw1NB`|`System.Windows.Forms.Panel [class:373]`|`void`|`1285020`|`4`|`ae3c8b8d99a39542...`|
|`0x06000df4`|`iU00i1adDoN`|`()`|`System.Windows.Forms.Panel [class:373]`|`1285124`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000df5`|`M7c0iM6OyJ1`|`System.Windows.Forms.Panel [class:373]`|`void`|`1285140`|`4`|`ae3c8b8d99a39542...`|
|`0x06000df6`|`p0Z0i9yqyJP`|`()`|`System.Windows.Forms.Panel [class:373]`|`1285244`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000df7`|`PwA0iKyqtkw`|`System.Windows.Forms.Panel [class:373]`|`void`|`1285260`|`4`|`ae3c8b8d99a39542...`|
|`0x06000df8`|`w5n0iNChIZk`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1285364`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000df9`|`y370ib6aLU4`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1285380`|`4`|`ae3c8b8d99a39542...`|
|`0x06000dfa`|`thC0iI91FHh`|`()`|`System.Windows.Forms.Panel [class:373]`|`1285484`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000dfb`|`Civ0iy3nXPE`|`System.Windows.Forms.Panel [class:373]`|`void`|`1285500`|`4`|`ae3c8b8d99a39542...`|
|`0x06000dfc`|`CPb0iRFtVtJ`|`()`|`System.Windows.Forms.Panel [class:373]`|`1285604`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000dfd`|`wAh0igLb7aK`|`System.Windows.Forms.Panel [class:373]`|`void`|`1285620`|`4`|`ae3c8b8d99a39542...`|
|`0x06000dfe`|`XP70ihpFCti`|`()`|`System.Windows.Forms.Panel [class:373]`|`1285724`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000dff`|`s1l0iZsWHKj`|`System.Windows.Forms.Panel [class:373]`|`void`|`1285740`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e00`|`xHR0ieptKso`|`()`|`System.Windows.Forms.Panel [class:373]`|`1285844`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000e01`|`TAj0iLkJ9Vs`|`System.Windows.Forms.Panel [class:373]`|`void`|`1285860`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e02`|`Ewc0iT7xtJs`|`()`|`System.Windows.Forms.RadioButton [class:253]`|`1285964`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000e03`|`pkX0ifL3quW`|`System.Windows.Forms.RadioButton [class:253]`|`void`|`1285980`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e04`|`rti0icknTFo`|`()`|`System.Windows.Forms.RadioButton [class:253]`|`1286388`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000e05`|`JlO0i6EBZIY`|`System.Windows.Forms.RadioButton [class:253]`|`void`|`1286404`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e06`|`mdX0iWueGoI`|`()`|`System.Windows.Forms.Label [class:245]`|`1286752`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000e07`|`CJD0isg6R9M`|`System.Windows.Forms.Label [class:245]`|`void`|`1286768`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e08`|`BhN0J5AIGyq`|`()`|`System.Windows.Forms.Button [class:161]`|`1286872`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000e09`|`s2w0JSVR96X`|`System.Windows.Forms.Button [class:161]`|`void`|`1286888`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e0a`|`SetDoNotReload`|`bool`|`void`|`1287276`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e0b`|`FNhIilVVtL`|`object, System.EventArgs [class:113]`|`void`|`1287380`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e0c`|`j5BIn0kSFR`|`()`|`void`|`1287872`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e0d`|`iCWI546UGN`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`1295020`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e0e`|`R6NISrtVky`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`1295560`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e0f`|`UyRIj8my4J`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`1295824`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e10`|`P5QIx74WjC`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`1296220`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e11`|`ve_mc_mong`|`()`|`void`|`1296380`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e12`|`yNHID2UJrd`|`object, System.EventArgs [class:113]`|`void`|`1298996`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e13`|`hAbIBQtePr`|`object, System.EventArgs [class:113]`|`void`|`1299500`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e14`|`C7jIOmoBjO`|`object, System.EventArgs [class:113]`|`void`|`1299644`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e15`|`C1xIVJquXV`|`()`|`void`|`1299788`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e16`|`CE70itpHnFN`|`()`|`System.Windows.Forms.Timer [class:737]`|`1300276`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000e17`|`X2L0id0Vbbl`|`System.Windows.Forms.Timer [class:737]`|`void`|`1300292`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e18`|`doi_du_lieu`|`bool`|`void`|`1300396`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e19`|`setDefault`|`()`|`void`|`1300960`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e1a`|`fUQIaYT8u4`|`()`|`void`|`1305516`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e1b`|`syyIzDmgVy`|`()`|`void`|`1311516`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e1c`|`YpoyUn7UUm`|`object, System.EventArgs [class:113]`|`void`|`1311824`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e1d`|`vJwy8PeIi6`|`object, System.EventArgs [class:113]`|`void`|`1311968`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e1e`|`UvJyEIupcl`|`object, System.EventArgs [class:113]`|`void`|`1312112`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e1f`|`getNameFromTB`|`string`|`string`|`1312256`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000e20`|`hiRy09uZ02`|`object, System.EventArgs [class:113]`|`void`|`1312752`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e21`|`QrXyPtMtmA`|`object, System.Windows.Forms.FormClosingEventArgs [class:301]`|`void`|`1312860`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e22`|`jIOy361KEL`|`object, System.EventArgs [class:113]`|`void`|`1313016`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e23`|`AYdyqUNViP`|`object, System.EventArgs [class:113]`|`void`|`1313168`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e24`|`KasyAUNEsd`|`object, System.EventArgs [class:113]`|`void`|`1313316`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e25`|`bveyCMq5Ko`|`object, System.EventArgs [class:113]`|`void`|`1313552`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e26`|`REtym0jvg3`|`object, System.EventArgs [class:113]`|`void`|`1313800`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e27`|`rEOyQLYZxD`|`object, System.EventArgs [class:113]`|`void`|`1314000`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e28`|`mVQykB5luK`|`object, System.EventArgs [class:113]`|`void`|`1314384`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e29`|`lvgy4RHDdv`|`object, System.EventArgs [class:113]`|`void`|`1314608`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e2a`|`AFlyY7yX57`|`object, System.EventArgs [class:113]`|`void`|`1314900`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e2b`|`l7IyoSfI6B`|`object, System.EventArgs [class:113]`|`void`|`1315404`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e2c`|`yTFy7DfGaL`|`object, System.EventArgs [class:113]`|`void`|`1323680`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e2d`|`gK8yFkQX0j`|`object, System.EventArgs [class:113]`|`void`|`1326108`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e2e`|`xXQyvFi2io`|`object, System.EventArgs [class:113]`|`void`|`1326856`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e2f`|`RhZy11F1uf`|`object, System.EventArgs [class:113]`|`void`|`1327632`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e30`|`ODAyMV47it`|`object, System.EventArgs [class:113]`|`void`|`1327804`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e31`|`Dd5y9viMgw`|`object, System.EventArgs [class:113]`|`void`|`1327976`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e32`|`ju7yK04PAM`|`object, System.EventArgs [class:113]`|`void`|`1328148`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e33`|`ADkyNRQjRB`|`object, System.EventArgs [class:113]`|`void`|`1328492`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e34`|`O6FybW8aDh`|`object, System.EventArgs [class:113]`|`void`|`1328504`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e35`|`hH6yIS9BdC`|`object, System.EventArgs [class:113]`|`void`|`1329244`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e36`|`kTbyy6W0CX`|`object, System.EventArgs [class:113]`|`void`|`1329420`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e37`|`OV3yRSlE1Y`|`object, System.EventArgs [class:113]`|`void`|`1329616`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e38`|`.cctor`|`()`|`void`|`1330080`|`177`|`193dd4cde3938212...`|
|`0x06000e39`|`HikS61qEpPfVytZuppxP`|`()`|`bool`|`1330272`|`4`|`66ea1533cb3404e9...`|
|`0x06000e3a`|`e5U03qqEwrixQX2jcNQF`|`()`|`Kata_pro64_Cad2025.Form_Mong_Coc_V2 [class:464]`|`1330292`|`4`|`8969ce0cdaa4a19c...`|

### [VM-192] Methods of `Kata_pro64_Cad2025.Form_Rai_Mong` (Token `0x0200007d`, Displayed: 143/143, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06000e92`|`.ctor`|`()`|`void`|`1352452`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e93`|`Dispose`|`bool`|`void`|`1352952`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e94`|`rPqhBuSOdc`|`()`|`void`|`1353380`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e95`|`QCd0i5LoJa8`|`()`|`System.Windows.Forms.TabControl [class:365]`|`1382224`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000e96`|`W4N0iSN4HtX`|`System.Windows.Forms.TabControl [class:365]`|`void`|`1382240`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e97`|`aYD0ijmJNNG`|`()`|`System.Windows.Forms.TabPage [class:369]`|`1382344`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000e98`|`km20ixLu1wB`|`System.Windows.Forms.TabPage [class:369]`|`void`|`1382360`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e99`|`OVp0iDy6vRU`|`()`|`System.Windows.Forms.Panel [class:373]`|`1382464`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000e9a`|`Adb0iBTEou6`|`System.Windows.Forms.Panel [class:373]`|`void`|`1382480`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e9b`|`XPo06cdQxL3`|`()`|`System.Windows.Forms.Panel [class:373]`|`1383224`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000e9c`|`gjs066x9fbq`|`System.Windows.Forms.Panel [class:373]`|`void`|`1383240`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e9d`|`xYc0iOYhvCR`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1383344`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000e9e`|`dkd0iVavDJi`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1383360`|`4`|`ae3c8b8d99a39542...`|
|`0x06000e9f`|`eEu0ia48dNL`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1383464`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ea0`|`RDo0iza86R2`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1383480`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ea1`|`LtU0nU32krH`|`()`|`System.Windows.Forms.Label [class:245]`|`1383824`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ea2`|`Jht0n8SF2Wg`|`System.Windows.Forms.Label [class:245]`|`void`|`1383840`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ea3`|`fKH0nEKWAuv`|`()`|`System.Windows.Forms.Label [class:245]`|`1383944`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ea4`|`Fw00n0vr87M`|`System.Windows.Forms.Label [class:245]`|`void`|`1383960`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ea5`|`Ah90nPxN8IP`|`()`|`System.Windows.Forms.Label [class:245]`|`1384064`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ea6`|`rTd0n3TnTXS`|`System.Windows.Forms.Label [class:245]`|`void`|`1384080`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ea7`|`Rnk0nqjXPyC`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1384184`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ea8`|`MTl0nAaR4tr`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1384200`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ea9`|`lDU0nC6TF8q`|`()`|`System.Windows.Forms.Button [class:161]`|`1384304`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000eaa`|`bGo0nmS2BQK`|`System.Windows.Forms.Button [class:161]`|`void`|`1384320`|`4`|`ae3c8b8d99a39542...`|
|`0x06000eab`|`hnC0nQE1hOn`|`()`|`System.Windows.Forms.Button [class:161]`|`1384688`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000eac`|`tjv0nkkSx4B`|`System.Windows.Forms.Button [class:161]`|`void`|`1384704`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ead`|`vgi0n4JUWbo`|`()`|`System.Windows.Forms.Button [class:161]`|`1385072`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000eae`|`sRq0nYe9E6g`|`System.Windows.Forms.Button [class:161]`|`void`|`1385088`|`4`|`ae3c8b8d99a39542...`|
|`0x06000eaf`|`Eue0noaqomf`|`()`|`System.Windows.Forms.NumericUpDown [class:325]`|`1385432`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000eb0`|`r2K0n7Qxe2g`|`System.Windows.Forms.NumericUpDown [class:325]`|`void`|`1385448`|`4`|`ae3c8b8d99a39542...`|
|`0x06000eb1`|`Ivn0nFds62e`|`()`|`System.Windows.Forms.RadioButton [class:253]`|`1385796`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000eb2`|`iTh0nvvnQ6V`|`System.Windows.Forms.RadioButton [class:253]`|`void`|`1385812`|`4`|`ae3c8b8d99a39542...`|
|`0x06000eb3`|`yF70n1laHD3`|`()`|`System.Windows.Forms.Label [class:245]`|`1385916`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000eb4`|`T6h0nMQDNUI`|`System.Windows.Forms.Label [class:245]`|`void`|`1385932`|`4`|`ae3c8b8d99a39542...`|
|`0x06000eb5`|`FpB0n9ETk3R`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1386036`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000eb6`|`Ymd0nKeIR1M`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1386052`|`4`|`ae3c8b8d99a39542...`|
|`0x06000eb7`|`Hmw0nNBBLg2`|`()`|`System.Windows.Forms.ComboBox [class:377]`|`1386460`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000eb8`|`gRT0nbxfPft`|`System.Windows.Forms.ComboBox [class:377]`|`void`|`1386476`|`4`|`ae3c8b8d99a39542...`|
|`0x06000eb9`|`YVc0nIraW7l`|`()`|`System.Windows.Forms.Label [class:245]`|`1386884`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000eba`|`F3U0nyJRjBZ`|`System.Windows.Forms.Label [class:245]`|`void`|`1386900`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ebb`|`Fpg0nRCNxFe`|`()`|`System.Windows.Forms.TabPage [class:369]`|`1387004`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ebc`|`dU80ngXj1JM`|`System.Windows.Forms.TabPage [class:369]`|`void`|`1387020`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ebd`|`rFC0nhGjSUG`|`()`|`System.Windows.Forms.Panel [class:373]`|`1387124`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ebe`|`FS80nZYUOcr`|`System.Windows.Forms.Panel [class:373]`|`void`|`1387140`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ebf`|`KCY0ne652fo`|`()`|`System.Windows.Forms.RadioButton [class:253]`|`1387844`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ec0`|`iU90nLZcKDI`|`System.Windows.Forms.RadioButton [class:253]`|`void`|`1387860`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ec1`|`ORS0cIBi4hZ`|`()`|`System.Windows.Forms.Panel [class:373]`|`1387964`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ec2`|`lur0cyG9Amp`|`System.Windows.Forms.Panel [class:373]`|`void`|`1387980`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ec3`|`SHJ0nT4MPkM`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1388084`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ec4`|`c9b0nfGl7v4`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1388100`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ec5`|`yp50nce7wTj`|`()`|`System.Windows.Forms.ComboBox [class:377]`|`1388204`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ec6`|`U630n6ipLBK`|`System.Windows.Forms.ComboBox [class:377]`|`void`|`1388220`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ec7`|`WVt0nWZGWHU`|`()`|`System.Windows.Forms.Label [class:245]`|`1388648`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ec8`|`J5o0ns3G1VO`|`System.Windows.Forms.Label [class:245]`|`void`|`1388664`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ec9`|`rMn0nt9KJBi`|`()`|`System.Windows.Forms.Label [class:245]`|`1388768`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000eca`|`Rfd0ndl2mmm`|`System.Windows.Forms.Label [class:245]`|`void`|`1388784`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ecb`|`Ixj0nJTdmr9`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1388888`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ecc`|`zFn0nrN830l`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1388904`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ecd`|`AUe0nGW09mv`|`()`|`System.Windows.Forms.Button [class:161]`|`1389272`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ece`|`FCl0nlaKoue`|`System.Windows.Forms.Button [class:161]`|`void`|`1389288`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ecf`|`MeH0nuyVNp7`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1389632`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ed0`|`wLW0nHfLZar`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1389648`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ed1`|`dFF0np81puV`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1390096`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ed2`|`k340nwbcj5M`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1390112`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ed3`|`Mps0n2fuXSE`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1390520`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ed4`|`Cca0nXnO9HF`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1390536`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ed5`|`ePx0nimjiXy`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1390900`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ed6`|`CFC0nns7vSZ`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1390916`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ed7`|`GGD0n5EaIeL`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1391328`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ed8`|`XaV0nS83N5T`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1391344`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ed9`|`rK70njeg2Z0`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1391672`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000eda`|`NWT0nx41Hb4`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1391688`|`4`|`ae3c8b8d99a39542...`|
|`0x06000edb`|`zmu0nDe9Fvj`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1392076`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000edc`|`fIe0nBaGkR1`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1392092`|`4`|`ae3c8b8d99a39542...`|
|`0x06000edd`|`PHy0nOwhSS9`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1392500`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ede`|`tIZ0nV77FuZ`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1392516`|`4`|`ae3c8b8d99a39542...`|
|`0x06000edf`|`D3t0naycSAi`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1392840`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ee0`|`I9h0nzGTfnk`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1392856`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ee1`|`lpa05UEybCZ`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1393264`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ee2`|`TOL058uHMr1`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1393280`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ee3`|`J8605EAXtbP`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1393708`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ee4`|`Roe050K8fGL`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1393724`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ee5`|`DWg05PN7SNW`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1394092`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ee6`|`bh1053HldaF`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1394108`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ee7`|`WHq05qr9mP7`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1394476`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ee8`|`zi605A11q5C`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1394492`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ee9`|`zbC05C56Xw3`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1394836`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000eea`|`UXW05mYrdnf`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1394852`|`4`|`ae3c8b8d99a39542...`|
|`0x06000eeb`|`Oy305QEbTG2`|`()`|`System.Windows.Forms.Label [class:245]`|`1395200`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000eec`|`VPh05kGKKvq`|`System.Windows.Forms.Label [class:245]`|`void`|`1395216`|`4`|`ae3c8b8d99a39542...`|
|`0x06000eed`|`h9n054YS7GB`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1395320`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000eee`|`xHw05YRs8H0`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1395336`|`4`|`ae3c8b8d99a39542...`|
|`0x06000eef`|`sYZ05oq6Mb5`|`()`|`System.Windows.Forms.Button [class:161]`|`1395704`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ef0`|`Dqb057eUVY6`|`System.Windows.Forms.Button [class:161]`|`void`|`1395720`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ef1`|`E2r05FP4nVD`|`()`|`System.Windows.Forms.Label [class:245]`|`1396108`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ef2`|`Flv05vnCuYd`|`System.Windows.Forms.Label [class:245]`|`void`|`1396124`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ef3`|`JYP051QedGh`|`()`|`System.Windows.Forms.Label [class:245]`|`1396228`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ef4`|`VYp05MS1rEu`|`System.Windows.Forms.Label [class:245]`|`void`|`1396244`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ef5`|`c5F059bYk9E`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1396348`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ef6`|`qDf05KGiygD`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1396364`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ef7`|`ivl05NgSMCv`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1396468`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ef8`|`OXo05bjl3MW`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1396484`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ef9`|`mds05IIGmq9`|`()`|`System.Windows.Forms.ComboBox [class:377]`|`1396588`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000efa`|`e1W05yHh3b0`|`System.Windows.Forms.ComboBox [class:377]`|`void`|`1396604`|`4`|`ae3c8b8d99a39542...`|
|`0x06000efb`|`N8y05RUoWFJ`|`()`|`System.Windows.Forms.Label [class:245]`|`1396708`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000efc`|`jso05gWV53a`|`System.Windows.Forms.Label [class:245]`|`void`|`1396724`|`4`|`ae3c8b8d99a39542...`|
|`0x06000efd`|`zpE05hkH3dB`|`()`|`System.Windows.Forms.ComboBox [class:377]`|`1396828`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000efe`|`GdP05Z82lIt`|`System.Windows.Forms.ComboBox [class:377]`|`void`|`1396844`|`4`|`ae3c8b8d99a39542...`|
|`0x06000eff`|`yBm05esQgsI`|`()`|`System.Windows.Forms.Label [class:245]`|`1396948`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000f00`|`QmV05LF3F4u`|`System.Windows.Forms.Label [class:245]`|`void`|`1396964`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f01`|`CdUhOvuO4I`|`object, System.EventArgs [class:113]`|`void`|`1397068`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f02`|`XRVhVCSNoY`|`()`|`void`|`1398132`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f03`|`nrehaD30Su`|`object, System.Windows.Forms.FormClosingEventArgs [class:301]`|`void`|`1400160`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f04`|`ILphztFXCv`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`1400316`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f05`|`DC3ZUSOSxN`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`1400820`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f06`|`bFPZ8OSXAp`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`1401084`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f07`|`lQBZEj6xTM`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`1401500`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f08`|`o9dZ0Ml3cs`|`()`|`void`|`1401660`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f09`|`doi_du_lieu_rai_mong`|`bool`|`void`|`1401768`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f0a`|`chuyenDuLieuFormSangInfoRaiMong`|`()`|`void`|`1402244`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f0b`|`UxnZPCpvNS`|`object, System.EventArgs [class:113]`|`void`|`1402736`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f0c`|`QrsZ3qsMW4`|`object, System.EventArgs [class:113]`|`void`|`1402904`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f0d`|`KWbZqKhmdg`|`object, System.EventArgs [class:113]`|`void`|`1403808`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f0e`|`tUPZA6BpxW`|`System.Windows.Forms.Panel [class:373]`|`string`|`1405656`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000f0f`|`fKyZCr1Oq4`|`object, System.EventArgs [class:113]`|`void`|`1406192`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f10`|`BlaZmRl5sI`|`()`|`void`|`1406372`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f11`|`t9JZQcBIbk`|`object, System.EventArgs [class:113]`|`void`|`1408100`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f12`|`veTZkmwNQ5`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`1408112`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f13`|`VwDZ4FqVfV`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`1408640`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f14`|`iFmZYWtJPZ`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`1408884`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f15`|`Af6Zo7a2pO`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`1409260`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f16`|`GTjZ7thHit`|`()`|`void`|`1409420`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f17`|`doi_du_lieu_rai_mong_don`|`bool`|`void`|`1409528`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f18`|`chuyenDuLieuFormSangInfoRaiMongDon`|`()`|`void`|`1410028`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f19`|`PMgZFORxsq`|`object, System.EventArgs [class:113]`|`void`|`1411028`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f1a`|`DjxZv8JJT8`|`object, System.EventArgs [class:113]`|`void`|`1411180`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f1b`|`hpZZ1jPaE9`|`()`|`void`|`1412744`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f1c`|`TAQZMvr8Yx`|`object, System.EventArgs [class:113]`|`void`|`1415632`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f1d`|`x5mZ9KKpoB`|`object, System.EventArgs [class:113]`|`void`|`1415644`|`4`|`ae3c8b8d99a39542...`|
|`0x06000f1e`|`.cctor`|`()`|`void`|`1416772`|`156`|`f6143cdf8af2c57c...`|
|`0x06000f1f`|`ceZ7lGqEx8N3ZsuyEaS8`|`()`|`bool`|`1416940`|`4`|`66ea1533cb3404e9...`|
|`0x06000f20`|`kIxyw6qED0KDWp2loqVH`|`()`|`Kata_pro64_Cad2025.Form_Rai_Mong [class:500]`|`1416960`|`4`|`8969ce0cdaa4a19c...`|

### [VM-193] Methods of `Kata_pro64_Cad2025.Form_Ve_Mat_Bang_Mong` (Token `0x0200009d`, Displayed: 650/650, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x060012bc`|`.ctor`|`()`|`void`|`1842268`|`4`|`ae3c8b8d99a39542...`|
|`0x060012bd`|`Dispose`|`bool`|`void`|`1843148`|`4`|`ae3c8b8d99a39542...`|
|`0x060012be`|`PNItoWTYh5`|`()`|`void`|`1843628`|`4`|`ae3c8b8d99a39542...`|
|`0x060012bf`|`F450h5Oc62a`|`()`|`System.Windows.Forms.Panel [class:373]`|`1951148`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012c0`|`ec30hSAXSkn`|`System.Windows.Forms.Panel [class:373]`|`void`|`1951164`|`4`|`ae3c8b8d99a39542...`|
|`0x060012c1`|`ORv0OFyOk47`|`()`|`System.Windows.Forms.Button [class:161]`|`1952036`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012c2`|`StL0OvCbVdI`|`System.Windows.Forms.Button [class:161]`|`void`|`1952052`|`4`|`ae3c8b8d99a39542...`|
|`0x060012c3`|`MZn0fe7UJWt`|`()`|`System.Windows.Forms.Panel [class:373]`|`1952416`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012c4`|`d5N0fL1RpR2`|`System.Windows.Forms.Panel [class:373]`|`void`|`1952432`|`4`|`ae3c8b8d99a39542...`|
|`0x060012c5`|`gnl0O1InZhq`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1952536`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012c6`|`oyL0OMWmAEB`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1952552`|`4`|`ae3c8b8d99a39542...`|
|`0x060012c7`|`AQW0O9XsR6K`|`()`|`System.Windows.Forms.Label [class:245]`|`1952920`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012c8`|`sPN0OKhSVKg`|`System.Windows.Forms.Label [class:245]`|`void`|`1952936`|`4`|`ae3c8b8d99a39542...`|
|`0x060012c9`|`HIy0ONTgSLG`|`()`|`System.Windows.Forms.Button [class:161]`|`1953040`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012ca`|`xWr0Ob4cJwv`|`System.Windows.Forms.Button [class:161]`|`void`|`1953056`|`4`|`ae3c8b8d99a39542...`|
|`0x060012cb`|`Gqr0OIZ8Jjg`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1953404`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012cc`|`nMv0OyKais3`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1953420`|`4`|`ae3c8b8d99a39542...`|
|`0x060012cd`|`lUb0ORbjkdN`|`()`|`System.Windows.Forms.Label [class:245]`|`1953764`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012ce`|`OVY0OgsWsaF`|`System.Windows.Forms.Label [class:245]`|`void`|`1953780`|`4`|`ae3c8b8d99a39542...`|
|`0x060012cf`|`UTa0OhIjMjB`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1953884`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012d0`|`gcP0OZa6opL`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1953900`|`4`|`ae3c8b8d99a39542...`|
|`0x060012d1`|`ala0OeE1rXa`|`()`|`System.Windows.Forms.Label [class:245]`|`1954308`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012d2`|`PZs0OLwZbFB`|`System.Windows.Forms.Label [class:245]`|`void`|`1954324`|`4`|`ae3c8b8d99a39542...`|
|`0x060012d3`|`BhN0J5AIGyq`|`()`|`System.Windows.Forms.Button [class:161]`|`1954428`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012d4`|`s2w0JSVR96X`|`System.Windows.Forms.Button [class:161]`|`void`|`1954444`|`4`|`ae3c8b8d99a39542...`|
|`0x060012d5`|`Vws0OTd2jLc`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1954808`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012d6`|`JMn0OfLBTfl`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1954824`|`4`|`ae3c8b8d99a39542...`|
|`0x060012d7`|`BwX0OcJnnkr`|`()`|`System.Windows.Forms.Label [class:245]`|`1955172`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012d8`|`OD60O6LvmhE`|`System.Windows.Forms.Label [class:245]`|`void`|`1955188`|`4`|`ae3c8b8d99a39542...`|
|`0x060012d9`|`QCd0i5LoJa8`|`()`|`System.Windows.Forms.TabControl [class:365]`|`1955292`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012da`|`W4N0iSN4HtX`|`System.Windows.Forms.TabControl [class:365]`|`void`|`1955308`|`4`|`ae3c8b8d99a39542...`|
|`0x060012db`|`HNI0OWSl4GT`|`()`|`System.Windows.Forms.TabPage [class:369]`|`1955412`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012dc`|`zrU0Os1EpUy`|`System.Windows.Forms.TabPage [class:369]`|`void`|`1955428`|`4`|`ae3c8b8d99a39542...`|
|`0x060012dd`|`OKF0OtLZa2n`|`()`|`System.Windows.Forms.Label [class:245]`|`1955532`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012de`|`Q9y0Ode1DQG`|`System.Windows.Forms.Label [class:245]`|`void`|`1955548`|`4`|`ae3c8b8d99a39542...`|
|`0x060012df`|`nxy0S1eNxvK`|`()`|`System.Windows.Forms.Label [class:245]`|`1955652`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012e0`|`Uo30SMey6RY`|`System.Windows.Forms.Label [class:245]`|`void`|`1955668`|`4`|`ae3c8b8d99a39542...`|
|`0x060012e1`|`x940SFZYUUu`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1955772`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012e2`|`Wym0SvnPYK4`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1955788`|`4`|`ae3c8b8d99a39542...`|
|`0x060012e3`|`Yt60SeIAgXO`|`()`|`System.Windows.Forms.Label [class:245]`|`1956136`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012e4`|`e4t0SLaXCns`|`System.Windows.Forms.Label [class:245]`|`void`|`1956152`|`4`|`ae3c8b8d99a39542...`|
|`0x060012e5`|`GPJ0ShJ0V1H`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1956256`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012e6`|`Q600SZ7HqYh`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1956272`|`4`|`ae3c8b8d99a39542...`|
|`0x060012e7`|`U3S0OJ1OOx4`|`()`|`System.Windows.Forms.ComboBox [class:377]`|`1956616`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012e8`|`jZx0OrwMFk2`|`System.Windows.Forms.ComboBox [class:377]`|`void`|`1956632`|`4`|`ae3c8b8d99a39542...`|
|`0x060012e9`|`bHm0OGDEwrT`|`()`|`System.Windows.Forms.Label [class:245]`|`1957020`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012ea`|`jcV0OlBiZp6`|`System.Windows.Forms.Label [class:245]`|`void`|`1957036`|`4`|`ae3c8b8d99a39542...`|
|`0x060012eb`|`clR0OuJWLsa`|`()`|`System.Windows.Forms.Button [class:161]`|`1957140`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012ec`|`Ve30OH5geoK`|`System.Windows.Forms.Button [class:161]`|`void`|`1957156`|`4`|`ae3c8b8d99a39542...`|
|`0x060012ed`|`tlM0OpcZPi1`|`()`|`System.Windows.Forms.TabPage [class:369]`|`1957604`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012ee`|`eEJ0OwRWWsQ`|`System.Windows.Forms.TabPage [class:369]`|`void`|`1957620`|`4`|`ae3c8b8d99a39542...`|
|`0x060012ef`|`wdP0O26MC9v`|`()`|`System.Windows.Forms.Panel [class:373]`|`1957724`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012f0`|`uru0OXc61Jp`|`System.Windows.Forms.Panel [class:373]`|`void`|`1957740`|`4`|`ae3c8b8d99a39542...`|
|`0x060012f1`|`aOb0cTgfERI`|`()`|`System.Windows.Forms.Panel [class:373]`|`1958544`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012f2`|`cDw0cfvX0N5`|`System.Windows.Forms.Panel [class:373]`|`void`|`1958560`|`4`|`ae3c8b8d99a39542...`|
|`0x060012f3`|`VuW0OidsrYc`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1958664`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012f4`|`k7R0Onv7jtP`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1958680`|`4`|`ae3c8b8d99a39542...`|
|`0x060012f5`|`vXW0O5LTVAc`|`()`|`System.Windows.Forms.Label [class:245]`|`1958784`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012f6`|`Xd10OSqpsij`|`System.Windows.Forms.Label [class:245]`|`void`|`1958800`|`4`|`ae3c8b8d99a39542...`|
|`0x060012f7`|`bdH0OjB8e7R`|`()`|`System.Windows.Forms.Button [class:161]`|`1958904`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012f8`|`BOS0OxynwhG`|`System.Windows.Forms.Button [class:161]`|`void`|`1958920`|`4`|`ae3c8b8d99a39542...`|
|`0x060012f9`|`wax0ODLNsyv`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1959288`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012fa`|`vZQ0OBOJFiF`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1959304`|`4`|`ae3c8b8d99a39542...`|
|`0x060012fb`|`u3a0OO09crb`|`()`|`System.Windows.Forms.Button [class:161]`|`1959408`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012fc`|`vrN0OVqALT7`|`System.Windows.Forms.Button [class:161]`|`void`|`1959424`|`4`|`ae3c8b8d99a39542...`|
|`0x060012fd`|`m8F0Oa2rXO2`|`()`|`System.Windows.Forms.Button [class:161]`|`1959812`|`4`|`8969ce0cdaa4a19c...`|
|`0x060012fe`|`taE0OzM9S2x`|`System.Windows.Forms.Button [class:161]`|`void`|`1959828`|`4`|`ae3c8b8d99a39542...`|
|`0x060012ff`|`LNp0VUstGaG`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1960152`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001300`|`Phf0V8bCxo8`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1960168`|`4`|`ae3c8b8d99a39542...`|
|`0x06001301`|`R2g0VEPrXA0`|`()`|`System.Windows.Forms.Label [class:245]`|`1960272`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001302`|`cDO0V0cH5sj`|`System.Windows.Forms.Label [class:245]`|`void`|`1960288`|`4`|`ae3c8b8d99a39542...`|
|`0x06001303`|`O8P0cRn8QuV`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1960392`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001304`|`C5t0cg2n7MJ`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1960408`|`4`|`ae3c8b8d99a39542...`|
|`0x06001305`|`Xy70VPN9w9m`|`()`|`System.Windows.Forms.DataGridView [class:117]`|`1960512`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001306`|`CQQ0V3tJKdX`|`System.Windows.Forms.DataGridView [class:117]`|`void`|`1960528`|`4`|`ae3c8b8d99a39542...`|
|`0x06001307`|`RUe0VqmJuwR`|`()`|`System.Windows.Forms.DataGridView [class:117]`|`1960980`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001308`|`Mqq0VAebnd2`|`System.Windows.Forms.DataGridView [class:117]`|`void`|`1960996`|`4`|`ae3c8b8d99a39542...`|
|`0x06001309`|`VBr0VCsLfAy`|`()`|`System.Windows.Forms.Button [class:161]`|`1961720`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600130a`|`AUt0VmLtpUG`|`System.Windows.Forms.Button [class:161]`|`void`|`1961736`|`4`|`ae3c8b8d99a39542...`|
|`0x0600130b`|`yDh0VQnublG`|`()`|`System.Windows.Forms.Button [class:161]`|`1962104`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600130c`|`DYq0VkHlx8t`|`System.Windows.Forms.Button [class:161]`|`void`|`1962120`|`4`|`ae3c8b8d99a39542...`|
|`0x0600130d`|`hCS0V4t4CG2`|`()`|`System.Windows.Forms.Label [class:245]`|`1962468`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600130e`|`yWD0VYtKLsY`|`System.Windows.Forms.Label [class:245]`|`void`|`1962484`|`4`|`ae3c8b8d99a39542...`|
|`0x0600130f`|`QnS0VoXrBSu`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1962588`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001310`|`wLv0V7HrdDk`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1962604`|`4`|`ae3c8b8d99a39542...`|
|`0x06001311`|`R1Q0VFqMuqM`|`()`|`System.Windows.Forms.Label [class:245]`|`1962708`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001312`|`NMK0VvgqXhF`|`System.Windows.Forms.Label [class:245]`|`void`|`1962724`|`4`|`ae3c8b8d99a39542...`|
|`0x06001313`|`XB30V1nkoZN`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1962828`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001314`|`hUy0VMcHS0f`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1962844`|`4`|`ae3c8b8d99a39542...`|
|`0x06001315`|`HmP0V9LgXoN`|`()`|`System.Windows.Forms.Button [class:161]`|`1962948`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001316`|`BPP0VKRcEBY`|`System.Windows.Forms.Button [class:161]`|`void`|`1962964`|`4`|`ae3c8b8d99a39542...`|
|`0x06001317`|`dyh0VN1YV4s`|`()`|`System.Windows.Forms.Label [class:245]`|`1963292`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001318`|`Ks50VbutQQt`|`System.Windows.Forms.Label [class:245]`|`void`|`1963308`|`4`|`ae3c8b8d99a39542...`|
|`0x06001319`|`hjs0VIlEJu0`|`()`|`System.Windows.Forms.TabPage [class:369]`|`1963412`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600131a`|`bog0Vy75ahn`|`System.Windows.Forms.TabPage [class:369]`|`void`|`1963428`|`4`|`ae3c8b8d99a39542...`|
|`0x0600131b`|`kYg0VRMrQ80`|`()`|`System.Windows.Forms.Panel [class:373]`|`1963532`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600131c`|`Jy00Vgds5Qq`|`System.Windows.Forms.Panel [class:373]`|`void`|`1963548`|`4`|`ae3c8b8d99a39542...`|
|`0x0600131d`|`bhL0W20hQGq`|`()`|`System.Windows.Forms.Panel [class:373]`|`1964336`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600131e`|`eO50WXTIeKO`|`System.Windows.Forms.Panel [class:373]`|`void`|`1964352`|`4`|`ae3c8b8d99a39542...`|
|`0x0600131f`|`ltG0VhqLNVy`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1964456`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001320`|`DmV0VZNuNQ2`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1964472`|`4`|`ae3c8b8d99a39542...`|
|`0x06001321`|`OWM0VeoSgQj`|`()`|`System.Windows.Forms.Button [class:161]`|`1964576`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001322`|`JGa0VLi4SR5`|`System.Windows.Forms.Button [class:161]`|`void`|`1964592`|`4`|`ae3c8b8d99a39542...`|
|`0x06001323`|`Ead0VTMOtAu`|`()`|`System.Windows.Forms.ComboBox [class:377]`|`1964960`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001324`|`raC0VfW2eh4`|`System.Windows.Forms.ComboBox [class:377]`|`void`|`1964976`|`4`|`ae3c8b8d99a39542...`|
|`0x06001325`|`gss0VcOIdEo`|`()`|`System.Windows.Forms.Label [class:245]`|`1965080`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001326`|`vCm0V6orMGd`|`System.Windows.Forms.Label [class:245]`|`void`|`1965096`|`4`|`ae3c8b8d99a39542...`|
|`0x06001327`|`Jsn0VWRoaAv`|`()`|`System.Windows.Forms.Label [class:245]`|`1965200`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001328`|`CcZ0VsMeObm`|`System.Windows.Forms.Label [class:245]`|`void`|`1965216`|`4`|`ae3c8b8d99a39542...`|
|`0x06001329`|`NKh0VttM4oY`|`()`|`System.Windows.Forms.Label [class:245]`|`1965320`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600132a`|`aj90Vd4O6UX`|`System.Windows.Forms.Label [class:245]`|`void`|`1965336`|`4`|`ae3c8b8d99a39542...`|
|`0x0600132b`|`LFt0VJCAhcy`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1965440`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600132c`|`sA60Vri7oNw`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1965456`|`4`|`ae3c8b8d99a39542...`|
|`0x0600132d`|`Ba60VGfdEge`|`()`|`System.Windows.Forms.Label [class:245]`|`1965560`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600132e`|`Qxx0VlSFi2I`|`System.Windows.Forms.Label [class:245]`|`void`|`1965576`|`4`|`ae3c8b8d99a39542...`|
|`0x0600132f`|`Hwg0VuerPRM`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1965680`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001330`|`QX40VH6s42s`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1965696`|`4`|`ae3c8b8d99a39542...`|
|`0x06001331`|`I8N0VpQZDxW`|`()`|`System.Windows.Forms.Label [class:245]`|`1965800`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001332`|`HmI0VwFH9ya`|`System.Windows.Forms.Label [class:245]`|`void`|`1965816`|`4`|`ae3c8b8d99a39542...`|
|`0x06001333`|`NRV0V2bUOHj`|`()`|`System.Windows.Forms.Button [class:161]`|`1965920`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001334`|`H7g0VXVKkIP`|`System.Windows.Forms.Button [class:161]`|`void`|`1965936`|`4`|`ae3c8b8d99a39542...`|
|`0x06001335`|`xHB0Viwj4NV`|`()`|`System.Windows.Forms.Button [class:161]`|`1966264`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001336`|`xj70VnsBYB2`|`System.Windows.Forms.Button [class:161]`|`void`|`1966280`|`4`|`ae3c8b8d99a39542...`|
|`0x06001337`|`kvc0V5JaTgE`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1966624`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001338`|`TNT0VSQLQ4K`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1966640`|`4`|`ae3c8b8d99a39542...`|
|`0x06001339`|`zRd0Vj1MOUD`|`()`|`System.Windows.Forms.Button [class:161]`|`1966744`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600133a`|`ATK0VxbnSdG`|`System.Windows.Forms.Button [class:161]`|`void`|`1966760`|`4`|`ae3c8b8d99a39542...`|
|`0x0600133b`|`vbq0VDSFeGq`|`()`|`System.Windows.Forms.Label [class:245]`|`1967104`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600133c`|`rfc0VBI8jcx`|`System.Windows.Forms.Label [class:245]`|`void`|`1967120`|`4`|`ae3c8b8d99a39542...`|
|`0x0600133d`|`FZl0VO7qnnp`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1967224`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600133e`|`Mkk0VVr1rmL`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1967240`|`4`|`ae3c8b8d99a39542...`|
|`0x0600133f`|`MFV0VaugoMm`|`()`|`System.Windows.Forms.DataGridView [class:117]`|`1967344`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001340`|`HTj0VzCD1qE`|`System.Windows.Forms.DataGridView [class:117]`|`void`|`1967360`|`4`|`ae3c8b8d99a39542...`|
|`0x06001341`|`O0i0aU9JoMF`|`()`|`System.Windows.Forms.Label [class:245]`|`1967828`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001342`|`HM50a82H9do`|`System.Windows.Forms.Label [class:245]`|`void`|`1967844`|`4`|`ae3c8b8d99a39542...`|
|`0x06001343`|`MP60aEiypRU`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1967948`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001344`|`xV40a0A8dyU`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1967964`|`4`|`ae3c8b8d99a39542...`|
|`0x06001345`|`yEL0aPySyAW`|`()`|`System.Windows.Forms.Button [class:161]`|`1968068`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001346`|`oba0a3LBbUc`|`System.Windows.Forms.Button [class:161]`|`void`|`1968084`|`4`|`ae3c8b8d99a39542...`|
|`0x06001347`|`s0B0aqNIa61`|`()`|`System.Windows.Forms.Label [class:245]`|`1968496`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001348`|`I7u0aAf8Cwc`|`System.Windows.Forms.Label [class:245]`|`void`|`1968512`|`4`|`ae3c8b8d99a39542...`|
|`0x06001349`|`pZV0aCvNoQ6`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1968616`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600134a`|`Nun0amNf7dY`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1968632`|`4`|`ae3c8b8d99a39542...`|
|`0x0600134b`|`aLN0GTOXoD7`|`()`|`System.Windows.Forms.Label [class:245]`|`1968736`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600134c`|`QbM0GfxTZEc`|`System.Windows.Forms.Label [class:245]`|`void`|`1968752`|`4`|`ae3c8b8d99a39542...`|
|`0x0600134d`|`dqq0GePi5s2`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1968856`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600134e`|`IHJ0GLcQVAf`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1968872`|`4`|`ae3c8b8d99a39542...`|
|`0x0600134f`|`QLO0GIOymHE`|`()`|`System.Windows.Forms.Label [class:245]`|`1968976`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001350`|`bt60Gyy3HBp`|`System.Windows.Forms.Label [class:245]`|`void`|`1968992`|`4`|`ae3c8b8d99a39542...`|
|`0x06001351`|`elu0GNLpUXe`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1969096`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001352`|`zKK0GbMSY5C`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1969112`|`4`|`ae3c8b8d99a39542...`|
|`0x06001353`|`gOR0dFCBHpW`|`()`|`System.Windows.Forms.Label [class:245]`|`1969216`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001354`|`jl40dvY7vJG`|`System.Windows.Forms.Label [class:245]`|`void`|`1969232`|`4`|`ae3c8b8d99a39542...`|
|`0x06001355`|`LGx0GJTKKBm`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1969336`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001356`|`mLc0GrHvrAv`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1969352`|`4`|`ae3c8b8d99a39542...`|
|`0x06001357`|`pyY0aQ2n6O9`|`()`|`System.Windows.Forms.Label [class:245]`|`1969456`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001358`|`AXJ0akfWjHW`|`System.Windows.Forms.Label [class:245]`|`void`|`1969472`|`4`|`ae3c8b8d99a39542...`|
|`0x06001359`|`JuR0a4KTxYJ`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1969576`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600135a`|`b700aY1xvuk`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1969592`|`4`|`ae3c8b8d99a39542...`|
|`0x0600135b`|`mdi0aoJfxuc`|`()`|`System.Windows.Forms.Label [class:245]`|`1969696`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600135c`|`Na00a7XYIv5`|`System.Windows.Forms.Label [class:245]`|`void`|`1969712`|`4`|`ae3c8b8d99a39542...`|
|`0x0600135d`|`jB00aFvGsv7`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1969816`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600135e`|`C9X0avUUTfB`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1969832`|`4`|`ae3c8b8d99a39542...`|
|`0x0600135f`|`l1H0a1opIkO`|`()`|`System.Windows.Forms.Button [class:161]`|`1969936`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001360`|`yUY0aMwA2T4`|`System.Windows.Forms.Button [class:161]`|`void`|`1969952`|`4`|`ae3c8b8d99a39542...`|
|`0x06001361`|`Wa40a9L5Jrp`|`()`|`System.Windows.Forms.Label [class:245]`|`1970420`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001362`|`MfP0aKxl2Os`|`System.Windows.Forms.Label [class:245]`|`void`|`1970436`|`4`|`ae3c8b8d99a39542...`|
|`0x06001363`|`V4l0aNuE44D`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1970540`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001364`|`vAB0abdLnI0`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1970556`|`4`|`ae3c8b8d99a39542...`|
|`0x06001365`|`JyM0aI3ha7s`|`()`|`System.Windows.Forms.Button [class:161]`|`1970660`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001366`|`DlY0ayDAneG`|`System.Windows.Forms.Button [class:161]`|`void`|`1970676`|`4`|`ae3c8b8d99a39542...`|
|`0x06001367`|`Psk0aR5W2Ga`|`()`|`System.Windows.Forms.Label [class:245]`|`1971004`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001368`|`fMr0agtvsL8`|`System.Windows.Forms.Label [class:245]`|`void`|`1971020`|`4`|`ae3c8b8d99a39542...`|
|`0x06001369`|`A8d0ah1FKjY`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1971124`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600136a`|`Qr50aZYxL8g`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1971140`|`4`|`ae3c8b8d99a39542...`|
|`0x0600136b`|`uHY0ae9LBf5`|`()`|`System.Windows.Forms.Button [class:161]`|`1971244`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600136c`|`oKY0aLEFinY`|`System.Windows.Forms.Button [class:161]`|`void`|`1971260`|`4`|`ae3c8b8d99a39542...`|
|`0x0600136d`|`iAU0aT8x7p2`|`()`|`System.Windows.Forms.Label [class:245]`|`1971668`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600136e`|`sBg0afEQVxd`|`System.Windows.Forms.Label [class:245]`|`void`|`1971684`|`4`|`ae3c8b8d99a39542...`|
|`0x0600136f`|`sQL0acfjAtp`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1971788`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001370`|`qgr0a6BIjbl`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1971804`|`4`|`ae3c8b8d99a39542...`|
|`0x06001371`|`tue0LObWfVm`|`()`|`System.Windows.Forms.Label [class:245]`|`1971908`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001372`|`SJC0LVQ2df9`|`System.Windows.Forms.Label [class:245]`|`void`|`1971924`|`4`|`ae3c8b8d99a39542...`|
|`0x06001373`|`NCA0aW0hNKn`|`()`|`System.Windows.Forms.Button [class:161]`|`1972028`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001374`|`qHF0as92pRa`|`System.Windows.Forms.Button [class:161]`|`void`|`1972044`|`4`|`ae3c8b8d99a39542...`|
|`0x06001375`|`Bkg0atWPDse`|`()`|`System.Windows.Forms.Label [class:245]`|`1972388`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001376`|`q9e0adO42wY`|`System.Windows.Forms.Label [class:245]`|`void`|`1972404`|`4`|`ae3c8b8d99a39542...`|
|`0x06001377`|`LLQ0aJ6fTdC`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1972508`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001378`|`X8W0arBm5AZ`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1972524`|`4`|`ae3c8b8d99a39542...`|
|`0x06001379`|`ORS0cIBi4hZ`|`()`|`System.Windows.Forms.Panel [class:373]`|`1972628`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600137a`|`lur0cyG9Amp`|`System.Windows.Forms.Panel [class:373]`|`void`|`1972644`|`4`|`ae3c8b8d99a39542...`|
|`0x0600137b`|`XPo06cdQxL3`|`()`|`System.Windows.Forms.Panel [class:373]`|`1972748`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600137c`|`gjs066x9fbq`|`System.Windows.Forms.Panel [class:373]`|`void`|`1972764`|`4`|`ae3c8b8d99a39542...`|
|`0x0600137d`|`ApA0jWBfEqD`|`()`|`System.Windows.Forms.Panel [class:373]`|`1972868`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600137e`|`nOh0js4NR0T`|`System.Windows.Forms.Panel [class:373]`|`void`|`1972884`|`4`|`ae3c8b8d99a39542...`|
|`0x0600137f`|`z580aGsp0KH`|`()`|`System.Windows.Forms.Button [class:161]`|`1972988`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001380`|`jWl0alxpf5s`|`System.Windows.Forms.Button [class:161]`|`void`|`1973004`|`4`|`ae3c8b8d99a39542...`|
|`0x06001381`|`sYZ05oq6Mb5`|`()`|`System.Windows.Forms.Button [class:161]`|`1973332`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001382`|`Dqb057eUVY6`|`System.Windows.Forms.Button [class:161]`|`void`|`1973348`|`4`|`ae3c8b8d99a39542...`|
|`0x06001383`|`mds05IIGmq9`|`()`|`System.Windows.Forms.ComboBox [class:377]`|`1973676`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001384`|`e1W05yHh3b0`|`System.Windows.Forms.ComboBox [class:377]`|`void`|`1973692`|`4`|`ae3c8b8d99a39542...`|
|`0x06001385`|`N8y05RUoWFJ`|`()`|`System.Windows.Forms.Label [class:245]`|`1973796`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001386`|`jso05gWV53a`|`System.Windows.Forms.Label [class:245]`|`void`|`1973812`|`4`|`ae3c8b8d99a39542...`|
|`0x06001387`|`zpE05hkH3dB`|`()`|`System.Windows.Forms.ComboBox [class:377]`|`1973916`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001388`|`GdP05Z82lIt`|`System.Windows.Forms.ComboBox [class:377]`|`void`|`1973932`|`4`|`ae3c8b8d99a39542...`|
|`0x06001389`|`yBm05esQgsI`|`()`|`System.Windows.Forms.Label [class:245]`|`1974036`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600138a`|`QmV05LF3F4u`|`System.Windows.Forms.Label [class:245]`|`void`|`1974052`|`4`|`ae3c8b8d99a39542...`|
|`0x0600138b`|`E3K0auTneOv`|`()`|`System.Windows.Forms.Button [class:161]`|`1974156`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600138c`|`pU20aHEQJIE`|`System.Windows.Forms.Button [class:161]`|`void`|`1974172`|`4`|`ae3c8b8d99a39542...`|
|`0x0600138d`|`PLo0apKISle`|`()`|`System.Windows.Forms.ComboBox [class:377]`|`1974516`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600138e`|`Dgi0awoNSHD`|`System.Windows.Forms.ComboBox [class:377]`|`void`|`1974532`|`4`|`ae3c8b8d99a39542...`|
|`0x0600138f`|`CCA0ZowoUwH`|`()`|`System.Windows.Forms.Label [class:245]`|`1974636`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001390`|`s3g0Z7VmfVf`|`System.Windows.Forms.Label [class:245]`|`void`|`1974652`|`4`|`ae3c8b8d99a39542...`|
|`0x06001391`|`g6X0a22W4Np`|`()`|`System.Windows.Forms.Button [class:161]`|`1974756`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001392`|`h7h0aXrK5yJ`|`System.Windows.Forms.Button [class:161]`|`void`|`1974772`|`4`|`ae3c8b8d99a39542...`|
|`0x06001393`|`Uxv0aiZTh6S`|`()`|`System.Windows.Forms.Button [class:161]`|`1975180`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001394`|`L1b0anmvwi3`|`System.Windows.Forms.Button [class:161]`|`void`|`1975196`|`4`|`ae3c8b8d99a39542...`|
|`0x06001395`|`IQ00a5bdOC9`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1975584`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001396`|`Eaw0aSBWC0g`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1975600`|`4`|`ae3c8b8d99a39542...`|
|`0x06001397`|`KF60aj9XS9a`|`()`|`System.Windows.Forms.Button [class:161]`|`1975704`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001398`|`RKD0axRW3AC`|`System.Windows.Forms.Button [class:161]`|`void`|`1975720`|`4`|`ae3c8b8d99a39542...`|
|`0x06001399`|`BLp0aDCt7r4`|`()`|`System.Windows.Forms.Button [class:161]`|`1976108`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600139a`|`lPh0aBvg0m7`|`System.Windows.Forms.Button [class:161]`|`void`|`1976124`|`4`|`ae3c8b8d99a39542...`|
|`0x0600139b`|`zg50aODEO7k`|`()`|`System.Windows.Forms.Button [class:161]`|`1976472`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600139c`|`MS00aVipP2S`|`System.Windows.Forms.Button [class:161]`|`void`|`1976488`|`4`|`ae3c8b8d99a39542...`|
|`0x0600139d`|`a6J0aaXtk3e`|`()`|`System.Windows.Forms.Button [class:161]`|`1976896`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600139e`|`DAK0az0DvZ7`|`System.Windows.Forms.Button [class:161]`|`void`|`1976912`|`4`|`ae3c8b8d99a39542...`|
|`0x0600139f`|`peA0zUh5y32`|`()`|`System.Windows.Forms.Button [class:161]`|`1977296`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013a0`|`hBD0z8vr57m`|`System.Windows.Forms.Button [class:161]`|`void`|`1977312`|`4`|`ae3c8b8d99a39542...`|
|`0x060013a1`|`OCG0zEZH8gP`|`()`|`System.Windows.Forms.Button [class:161]`|`1977700`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013a2`|`ntq0z0L7IpZ`|`System.Windows.Forms.Button [class:161]`|`void`|`1977716`|`4`|`ae3c8b8d99a39542...`|
|`0x060013a3`|`Ybj0jpGgh1V`|`()`|`System.Windows.Forms.Panel [class:373]`|`1978124`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013a4`|`qHP0jwel6Lr`|`System.Windows.Forms.Panel [class:373]`|`void`|`1978140`|`4`|`ae3c8b8d99a39542...`|
|`0x060013a5`|`qyU0spTh8Gd`|`()`|`System.Windows.Forms.DataGridView [class:117]`|`1978244`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013a6`|`rbk0swIiJJW`|`System.Windows.Forms.DataGridView [class:117]`|`void`|`1978260`|`4`|`ae3c8b8d99a39542...`|
|`0x060013a7`|`gyq0jiDuIYF`|`()`|`System.Windows.Forms.Panel [class:373]`|`1978816`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013a8`|`ghL0jnWE526`|`System.Windows.Forms.Panel [class:373]`|`void`|`1978832`|`4`|`ae3c8b8d99a39542...`|
|`0x060013a9`|`Kdb0TCwb6SF`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1978936`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013aa`|`pwT0TmtLZra`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1978952`|`4`|`ae3c8b8d99a39542...`|
|`0x060013ab`|`KxC0jJZwG1q`|`()`|`System.Windows.Forms.Panel [class:373]`|`1979056`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013ac`|`rPQ0jrYONZx`|`System.Windows.Forms.Panel [class:373]`|`void`|`1979072`|`4`|`ae3c8b8d99a39542...`|
|`0x060013ad`|`l9r0zPAeo7V`|`()`|`System.Windows.Forms.Button [class:161]`|`1979176`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013ae`|`c2n0z3JoEXP`|`System.Windows.Forms.Button [class:161]`|`void`|`1979192`|`4`|`ae3c8b8d99a39542...`|
|`0x060013af`|`qRt0zqHK8dP`|`()`|`System.Windows.Forms.Button [class:161]`|`1979600`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013b0`|`kZx0zA461Tp`|`System.Windows.Forms.Button [class:161]`|`void`|`1979616`|`4`|`ae3c8b8d99a39542...`|
|`0x060013b1`|`bXn0zCKmccf`|`()`|`System.Windows.Forms.Button [class:161]`|`1979980`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013b2`|`T0m0zmkLcZD`|`System.Windows.Forms.Button [class:161]`|`void`|`1979996`|`4`|`ae3c8b8d99a39542...`|
|`0x060013b3`|`euo0zQoNiu3`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1980404`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013b4`|`xr70zkbWQAJ`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1980420`|`4`|`ae3c8b8d99a39542...`|
|`0x060013b5`|`kmZ0z4jO3js`|`()`|`System.Windows.Forms.RadioButton [class:253]`|`1980524`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013b6`|`SCb0zYS1vFO`|`System.Windows.Forms.RadioButton [class:253]`|`void`|`1980540`|`4`|`ae3c8b8d99a39542...`|
|`0x060013b7`|`DRJ0zoBk5tw`|`()`|`System.Windows.Forms.RadioButton [class:253]`|`1980884`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013b8`|`rL70z7eUngS`|`System.Windows.Forms.RadioButton [class:253]`|`void`|`1980900`|`4`|`ae3c8b8d99a39542...`|
|`0x060013b9`|`QVV0zFNgR8v`|`()`|`System.Windows.Forms.Button [class:161]`|`1981004`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013ba`|`Xi50zvLk1pS`|`System.Windows.Forms.Button [class:161]`|`void`|`1981020`|`4`|`ae3c8b8d99a39542...`|
|`0x060013bb`|`cJo0z1jxgeL`|`()`|`System.Windows.Forms.Button [class:161]`|`1981404`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013bc`|`kh50zMlB0bP`|`System.Windows.Forms.Button [class:161]`|`void`|`1981420`|`4`|`ae3c8b8d99a39542...`|
|`0x060013bd`|`drp0z9VTsYG`|`()`|`System.Windows.Forms.TabControl [class:365]`|`1981828`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013be`|`vA90zKgvxrQ`|`System.Windows.Forms.TabControl [class:365]`|`void`|`1981844`|`4`|`ae3c8b8d99a39542...`|
|`0x060013bf`|`hI90zNFJclA`|`()`|`System.Windows.Forms.TabPage [class:369]`|`1981948`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013c0`|`VhO0zb4J65m`|`System.Windows.Forms.TabPage [class:369]`|`void`|`1981964`|`4`|`ae3c8b8d99a39542...`|
|`0x060013c1`|`ERt0zIkRw1M`|`()`|`System.Windows.Forms.TabPage [class:369]`|`1982068`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013c2`|`njT0zycbwKh`|`System.Windows.Forms.TabPage [class:369]`|`void`|`1982084`|`4`|`ae3c8b8d99a39542...`|
|`0x060013c3`|`QUv0zRHI9LI`|`()`|`System.Windows.Forms.TabPage [class:369]`|`1982188`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013c4`|`EL70zgwlZoJ`|`System.Windows.Forms.TabPage [class:369]`|`void`|`1982204`|`4`|`ae3c8b8d99a39542...`|
|`0x060013c5`|`D7s0zhZKpUL`|`()`|`System.Windows.Forms.Button [class:161]`|`1982308`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013c6`|`XXq0zZjB3u7`|`System.Windows.Forms.Button [class:161]`|`void`|`1982324`|`4`|`ae3c8b8d99a39542...`|
|`0x060013c7`|`Yvd0zeEGxpI`|`()`|`System.Windows.Forms.Button [class:161]`|`1982712`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013c8`|`S6T0zLr1KSP`|`System.Windows.Forms.Button [class:161]`|`void`|`1982728`|`4`|`ae3c8b8d99a39542...`|
|`0x060013c9`|`gTE0jGXvcH9`|`()`|`System.Windows.Forms.Panel [class:373]`|`1983156`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013ca`|`wUy0jlu34Q9`|`System.Windows.Forms.Panel [class:373]`|`void`|`1983172`|`4`|`ae3c8b8d99a39542...`|
|`0x060013cb`|`TK10zTK3huD`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1983276`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013cc`|`Ijq0zfPPCFH`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1983292`|`4`|`ae3c8b8d99a39542...`|
|`0x060013cd`|`BSC0zcC7uSc`|`()`|`System.Windows.Forms.Button [class:161]`|`1983396`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013ce`|`tuI0z6mumhw`|`System.Windows.Forms.Button [class:161]`|`void`|`1983412`|`4`|`ae3c8b8d99a39542...`|
|`0x060013cf`|`Q4n0zWdHXu4`|`()`|`System.Windows.Forms.Button [class:161]`|`1983820`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013d0`|`pQU0zsF6FKQ`|`System.Windows.Forms.Button [class:161]`|`void`|`1983836`|`4`|`ae3c8b8d99a39542...`|
|`0x060013d1`|`IgZ0julGtaf`|`()`|`System.Windows.Forms.Panel [class:373]`|`1984224`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013d2`|`qQJ0jHaGxrA`|`System.Windows.Forms.Panel [class:373]`|`void`|`1984240`|`4`|`ae3c8b8d99a39542...`|
|`0x060013d3`|`DpK0sc5MnnS`|`()`|`System.Windows.Forms.Label [class:245]`|`1984344`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013d4`|`jRQ0s6mcPjx`|`System.Windows.Forms.Label [class:245]`|`void`|`1984360`|`4`|`ae3c8b8d99a39542...`|
|`0x060013d5`|`EoF0zti26g7`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1984464`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013d6`|`JhY0zdpOgFm`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1984480`|`4`|`ae3c8b8d99a39542...`|
|`0x060013d7`|`Ier0DTnCDNO`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1984584`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013d8`|`WmN0Dfigy4g`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1984600`|`4`|`ae3c8b8d99a39542...`|
|`0x060013d9`|`tOQ0DIUNOPZ`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1984704`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013da`|`UQ50Dyi8S4R`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1984720`|`4`|`ae3c8b8d99a39542...`|
|`0x060013db`|`yqb0ttqrEUS`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1984824`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013dc`|`xSo0tdqhINM`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1984840`|`4`|`ae3c8b8d99a39542...`|
|`0x060013dd`|`HWt0fQrKvy4`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1984944`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013de`|`tpi0fk0hWrQ`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1984960`|`4`|`ae3c8b8d99a39542...`|
|`0x060013df`|`xmt0ZtnsMK4`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1985064`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013e0`|`R7r0ZdxJUjg`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1985080`|`4`|`ae3c8b8d99a39542...`|
|`0x060013e1`|`tBT0lRoU6xw`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1985184`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013e2`|`jOF0lgIMuU4`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1985200`|`4`|`ae3c8b8d99a39542...`|
|`0x060013e3`|`aFh0zJGJN7y`|`()`|`System.Windows.Forms.CheckBox [class:257]`|`1985304`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013e4`|`KQx0zriTdD8`|`System.Windows.Forms.CheckBox [class:257]`|`void`|`1985320`|`4`|`ae3c8b8d99a39542...`|
|`0x060013e5`|`xsp0zGUT1Oo`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1985424`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013e6`|`PUY0zlhgDwX`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1985440`|`4`|`ae3c8b8d99a39542...`|
|`0x060013e7`|`jGT0sEtAyVn`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1985544`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013e8`|`npB0s0RrK7V`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1985560`|`4`|`ae3c8b8d99a39542...`|
|`0x060013e9`|`xOm0ZQEvAmp`|`()`|`System.Windows.Forms.Label [class:245]`|`1985664`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013ea`|`X9v0ZkfUlXU`|`System.Windows.Forms.Label [class:245]`|`void`|`1985680`|`4`|`ae3c8b8d99a39542...`|
|`0x060013eb`|`jE20zuXJKMS`|`()`|`System.Windows.Forms.Button [class:161]`|`1985784`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013ec`|`NaP0zHKtuVe`|`System.Windows.Forms.Button [class:161]`|`void`|`1985800`|`4`|`ae3c8b8d99a39542...`|
|`0x060013ed`|`EAq0zp8UoCa`|`()`|`System.Windows.Forms.Button [class:161]`|`1986128`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013ee`|`pIW0zw2OGDf`|`System.Windows.Forms.Button [class:161]`|`void`|`1986144`|`4`|`ae3c8b8d99a39542...`|
|`0x060013ef`|`qCe0j2VS4mj`|`()`|`System.Windows.Forms.Panel [class:373]`|`1986596`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013f0`|`Gyo0jXKV6XV`|`System.Windows.Forms.Panel [class:373]`|`void`|`1986612`|`4`|`ae3c8b8d99a39542...`|
|`0x060013f1`|`aVk0z2BWvt4`|`()`|`System.Windows.Forms.Label [class:245]`|`1986716`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013f2`|`Pko0zX2Ouuy`|`System.Windows.Forms.Label [class:245]`|`void`|`1986732`|`4`|`ae3c8b8d99a39542...`|
|`0x060013f3`|`c150zilpGR6`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1986836`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013f4`|`CiP0znE4Eff`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1986852`|`4`|`ae3c8b8d99a39542...`|
|`0x060013f5`|`t820z5KOpDY`|`()`|`System.Windows.Forms.Label [class:245]`|`1986956`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013f6`|`KxX0zSPlLIq`|`System.Windows.Forms.Label [class:245]`|`void`|`1986972`|`4`|`ae3c8b8d99a39542...`|
|`0x060013f7`|`pfv0zjTwCse`|`()`|`System.Windows.Forms.Label [class:245]`|`1987076`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013f8`|`KG10zx2ZDDB`|`System.Windows.Forms.Label [class:245]`|`void`|`1987092`|`4`|`ae3c8b8d99a39542...`|
|`0x060013f9`|`nUR0zDRjZf6`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1987196`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013fa`|`Ci70zB7bJJO`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1987212`|`4`|`ae3c8b8d99a39542...`|
|`0x060013fb`|`jNb0zOeqlnj`|`()`|`System.Windows.Forms.Label [class:245]`|`1987316`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013fc`|`qoQ0zVlSVi1`|`System.Windows.Forms.Label [class:245]`|`void`|`1987332`|`4`|`ae3c8b8d99a39542...`|
|`0x060013fd`|`Ph70zaNkPwU`|`()`|`System.Windows.Forms.ComboBox [class:377]`|`1987436`|`4`|`8969ce0cdaa4a19c...`|
|`0x060013fe`|`mZg0zzWfEDD`|`System.Windows.Forms.ComboBox [class:377]`|`void`|`1987452`|`4`|`ae3c8b8d99a39542...`|
|`0x060013ff`|`u23PUUJ3XmZ`|`()`|`System.Windows.Forms.Label [class:245]`|`1987820`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001400`|`DCIPU8LXPC3`|`System.Windows.Forms.Label [class:245]`|`void`|`1987836`|`4`|`ae3c8b8d99a39542...`|
|`0x06001401`|`HDdPUEBoJh8`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1987940`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001402`|`KcMPU0kuwHG`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1987956`|`4`|`ae3c8b8d99a39542...`|
|`0x06001403`|`mhGPUPEjr0H`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1988060`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001404`|`kttPU32meMj`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1988076`|`4`|`ae3c8b8d99a39542...`|
|`0x06001405`|`g8ePUqfubS0`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1988180`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001406`|`swaPUAZ2Jgc`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1988196`|`4`|`ae3c8b8d99a39542...`|
|`0x06001407`|`SOFPUClYlO1`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1988300`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001408`|`GEZPUmsNYQu`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1988316`|`4`|`ae3c8b8d99a39542...`|
|`0x06001409`|`wGT0jtBbZPA`|`()`|`System.Windows.Forms.Panel [class:373]`|`1988420`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600140a`|`oCk0jdHd1ne`|`System.Windows.Forms.Panel [class:373]`|`void`|`1988436`|`4`|`ae3c8b8d99a39542...`|
|`0x0600140b`|`dAtPUQvFtEk`|`()`|`System.Windows.Forms.ComboBox [class:377]`|`1988540`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600140c`|`nLfPUkO1Ybq`|`System.Windows.Forms.ComboBox [class:377]`|`void`|`1988556`|`4`|`ae3c8b8d99a39542...`|
|`0x0600140d`|`nt1PU4MJOUr`|`()`|`System.Windows.Forms.Button [class:161]`|`1988660`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600140e`|`k0TPUYCMIm4`|`System.Windows.Forms.Button [class:161]`|`void`|`1988676`|`4`|`ae3c8b8d99a39542...`|
|`0x0600140f`|`vA4PUov0gxR`|`()`|`System.Windows.Forms.Label [class:245]`|`1989044`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001410`|`baRPU7AbqIb`|`System.Windows.Forms.Label [class:245]`|`void`|`1989060`|`4`|`ae3c8b8d99a39542...`|
|`0x06001411`|`AsFPUFW75hj`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1989164`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001412`|`JCqPUva3Ocs`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1989180`|`4`|`ae3c8b8d99a39542...`|
|`0x06001413`|`ENf0xFvI8UP`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1989284`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001414`|`lR00xvrp5xC`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1989300`|`4`|`ae3c8b8d99a39542...`|
|`0x06001415`|`ECyPU1IR4x2`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1989404`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001416`|`c47PUM8lQoq`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1989420`|`4`|`ae3c8b8d99a39542...`|
|`0x06001417`|`GtVPU991S39`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1989524`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001418`|`BT8PUKeFRpp`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1989540`|`4`|`ae3c8b8d99a39542...`|
|`0x06001419`|`x2VPUN8H6ya`|`()`|`System.Windows.Forms.DataGridViewComboBoxColumn [class:817]`|`1989644`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600141a`|`Pe1PUbGRXrv`|`System.Windows.Forms.DataGridViewComboBoxColumn [class:817]`|`void`|`1989660`|`4`|`ae3c8b8d99a39542...`|
|`0x0600141b`|`n69PUIV3dJL`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1989764`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600141c`|`b22PUyTE8a9`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1989780`|`4`|`ae3c8b8d99a39542...`|
|`0x0600141d`|`NflPURKD7LC`|`()`|`System.Windows.Forms.DataGridViewComboBoxColumn [class:817]`|`1989884`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600141e`|`EWGPUgs2pcI`|`System.Windows.Forms.DataGridViewComboBoxColumn [class:817]`|`void`|`1989900`|`4`|`ae3c8b8d99a39542...`|
|`0x0600141f`|`oCePUhF1Juy`|`()`|`System.Windows.Forms.DataGridViewComboBoxColumn [class:817]`|`1990004`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001420`|`yX2PUZB0Qk4`|`System.Windows.Forms.DataGridViewComboBoxColumn [class:817]`|`void`|`1990020`|`4`|`ae3c8b8d99a39542...`|
|`0x06001421`|`AnZPUebvHas`|`()`|`System.Windows.Forms.ComboBox [class:377]`|`1990124`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001422`|`dmNPUL0fWxv`|`System.Windows.Forms.ComboBox [class:377]`|`void`|`1990140`|`4`|`ae3c8b8d99a39542...`|
|`0x06001423`|`MROPUT5H1er`|`()`|`System.Windows.Forms.Button [class:161]`|`1990244`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001424`|`Q3cPUfG66XW`|`System.Windows.Forms.Button [class:161]`|`void`|`1990260`|`4`|`ae3c8b8d99a39542...`|
|`0x06001425`|`zP60Z43yd3J`|`()`|`System.Windows.Forms.Label [class:245]`|`1990608`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001426`|`Ggy0ZYPrMrH`|`System.Windows.Forms.Label [class:245]`|`void`|`1990624`|`4`|`ae3c8b8d99a39542...`|
|`0x06001427`|`vN4PUcnCn0e`|`()`|`System.Windows.Forms.Button [class:161]`|`1990728`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001428`|`IRwPU6DG1nQ`|`System.Windows.Forms.Button [class:161]`|`void`|`1990744`|`4`|`ae3c8b8d99a39542...`|
|`0x06001429`|`FeuPUWs2WCG`|`()`|`System.Windows.Forms.Button [class:161]`|`1991092`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600142a`|`CgwPUsZnnG4`|`System.Windows.Forms.Button [class:161]`|`void`|`1991108`|`4`|`ae3c8b8d99a39542...`|
|`0x0600142b`|`ApsPUtEJfVm`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1991456`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600142c`|`KZbPUdgqhih`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1991472`|`4`|`ae3c8b8d99a39542...`|
|`0x0600142d`|`U97PUJtuwUK`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1991576`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600142e`|`WMHPUr00Xah`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1991592`|`4`|`ae3c8b8d99a39542...`|
|`0x0600142f`|`vOuPUGXcdLf`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1991696`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001430`|`WWqPUlY1wKR`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1991712`|`4`|`ae3c8b8d99a39542...`|
|`0x06001431`|`W4DPUuUmgZD`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1991816`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001432`|`BKQPUHeNAo9`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1991832`|`4`|`ae3c8b8d99a39542...`|
|`0x06001433`|`R6APUpTfrZs`|`()`|`System.Windows.Forms.Label [class:245]`|`1991936`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001434`|`vf7PUw7HQLi`|`System.Windows.Forms.Label [class:245]`|`void`|`1991952`|`4`|`ae3c8b8d99a39542...`|
|`0x06001435`|`SlYPU2LsAns`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1992056`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001436`|`QuRPUXUHPpc`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1992072`|`4`|`ae3c8b8d99a39542...`|
|`0x06001437`|`UjqPUiwv603`|`()`|`System.Windows.Forms.Label [class:245]`|`1992176`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001438`|`ySsPUnOhV1j`|`System.Windows.Forms.Label [class:245]`|`void`|`1992192`|`4`|`ae3c8b8d99a39542...`|
|`0x06001439`|`pyqPU5FPkIM`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1992296`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600143a`|`HJkPUSVn7db`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1992312`|`4`|`ae3c8b8d99a39542...`|
|`0x0600143b`|`mtNPUjFqTOs`|`()`|`System.Windows.Forms.Label [class:245]`|`1992416`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600143c`|`bLePUxkTyQo`|`System.Windows.Forms.Label [class:245]`|`void`|`1992432`|`4`|`ae3c8b8d99a39542...`|
|`0x0600143d`|`zJ4PUDg0Pku`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1992536`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600143e`|`JXBPUBqrAlt`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1992552`|`4`|`ae3c8b8d99a39542...`|
|`0x0600143f`|`sYTPUO3O2Co`|`()`|`System.Windows.Forms.Label [class:245]`|`1992656`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001440`|`KqPPUVR6WjC`|`System.Windows.Forms.Label [class:245]`|`void`|`1992672`|`4`|`ae3c8b8d99a39542...`|
|`0x06001441`|`SZnPUaZEGpa`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1992776`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001442`|`WysPUzUgSR4`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1992792`|`4`|`ae3c8b8d99a39542...`|
|`0x06001443`|`Y36P8U47dBK`|`()`|`System.Windows.Forms.Label [class:245]`|`1992896`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001444`|`ROCP88DJZ5t`|`System.Windows.Forms.Label [class:245]`|`void`|`1992912`|`4`|`ae3c8b8d99a39542...`|
|`0x06001445`|`nOrP8EamgtF`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1993016`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001446`|`Dw4P801WyJW`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1993032`|`4`|`ae3c8b8d99a39542...`|
|`0x06001447`|`IuWP8PCvO4r`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1993136`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001448`|`jbhP83gWwHF`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1993152`|`4`|`ae3c8b8d99a39542...`|
|`0x06001449`|`SagP8q8Apdt`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1993256`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600144a`|`TG5P8AaQT29`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1993272`|`4`|`ae3c8b8d99a39542...`|
|`0x0600144b`|`OSI0sPqNHJI`|`()`|`System.Windows.Forms.Button [class:161]`|`1993376`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600144c`|`h7x0s3IgLX2`|`System.Windows.Forms.Button [class:161]`|`void`|`1993392`|`4`|`ae3c8b8d99a39542...`|
|`0x0600144d`|`imWP8C8tmqt`|`()`|`System.Windows.Forms.Button [class:161]`|`1993496`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600144e`|`RXUP8mIP9kP`|`System.Windows.Forms.Button [class:161]`|`void`|`1993512`|`4`|`ae3c8b8d99a39542...`|
|`0x0600144f`|`pMcP8Qcmp0D`|`()`|`System.Windows.Forms.Button [class:161]`|`1993896`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001450`|`CseP8k3B23i`|`System.Windows.Forms.Button [class:161]`|`void`|`1993912`|`4`|`ae3c8b8d99a39542...`|
|`0x06001451`|`DYBP8481whc`|`()`|`System.Windows.Forms.Button [class:161]`|`1994016`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001452`|`VQcP8Y73LgQ`|`System.Windows.Forms.Button [class:161]`|`void`|`1994032`|`4`|`ae3c8b8d99a39542...`|
|`0x06001453`|`Ga1P8oPe80j`|`()`|`System.Windows.Forms.Button [class:161]`|`1994136`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001454`|`ky3P87FVR3K`|`System.Windows.Forms.Button [class:161]`|`void`|`1994152`|`4`|`ae3c8b8d99a39542...`|
|`0x06001455`|`NaYP8Fg1vgf`|`()`|`System.Windows.Forms.Button [class:161]`|`1994256`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001456`|`DqwP8v2fEDr`|`System.Windows.Forms.Button [class:161]`|`void`|`1994272`|`4`|`ae3c8b8d99a39542...`|
|`0x06001457`|`HjvP81wQh6c`|`()`|`System.Windows.Forms.Button [class:161]`|`1994376`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001458`|`zO8P8M084Wu`|`System.Windows.Forms.Button [class:161]`|`void`|`1994392`|`4`|`ae3c8b8d99a39542...`|
|`0x06001459`|`Xru0xoGfMVn`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1994736`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600145a`|`D0X0x7ovXou`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1994752`|`4`|`ae3c8b8d99a39542...`|
|`0x0600145b`|`gZS0DhjAM2c`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1994856`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600145c`|`qa80DZ1PkIF`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1994872`|`4`|`ae3c8b8d99a39542...`|
|`0x0600145d`|`PnVP895ASXC`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1994976`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600145e`|`mMlP8Kft2GV`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1994992`|`4`|`ae3c8b8d99a39542...`|
|`0x0600145f`|`ahDP8Nnxy2m`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1995096`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001460`|`obOP8b0FouM`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1995112`|`4`|`ae3c8b8d99a39542...`|
|`0x06001461`|`FhuP8IFvfMF`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1995216`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001462`|`g92P8ymf5aJ`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1995232`|`4`|`ae3c8b8d99a39542...`|
|`0x06001463`|`bmYP8Ro0frD`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1995336`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001464`|`GOhP8gOHx5I`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1995352`|`4`|`ae3c8b8d99a39542...`|
|`0x06001465`|`YLSP8hTrmD6`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1995456`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001466`|`MlMP8ZWGIrm`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1995472`|`4`|`ae3c8b8d99a39542...`|
|`0x06001467`|`bgZP8elQptJ`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1995576`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001468`|`v0LP8L83OO4`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1995592`|`4`|`ae3c8b8d99a39542...`|
|`0x06001469`|`fgmP8TRwicD`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1995696`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600146a`|`LlRP8fK3CmT`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1995712`|`4`|`ae3c8b8d99a39542...`|
|`0x0600146b`|`GEjP8c6reOP`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1995816`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600146c`|`SS8P86l9mp0`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1995832`|`4`|`ae3c8b8d99a39542...`|
|`0x0600146d`|`LQ8P8WMaU02`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1995936`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600146e`|`wO1P8sdEF4p`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1995952`|`4`|`ae3c8b8d99a39542...`|
|`0x0600146f`|`VL2P8tSoHpY`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1996056`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001470`|`JVVP8dZYtU1`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1996072`|`4`|`ae3c8b8d99a39542...`|
|`0x06001471`|`S0JP8JAyc5U`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1996176`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001472`|`pjcP8rdb54Z`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1996192`|`4`|`ae3c8b8d99a39542...`|
|`0x06001473`|`wvvP8GymdWa`|`()`|`System.Windows.Forms.Label [class:245]`|`1996296`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001474`|`rIgP8lO1uFL`|`System.Windows.Forms.Label [class:245]`|`void`|`1996312`|`4`|`ae3c8b8d99a39542...`|
|`0x06001475`|`iYIP8usd5he`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1996416`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001476`|`UsaP8HHb7ks`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1996432`|`4`|`ae3c8b8d99a39542...`|
|`0x06001477`|`qOFP8pCS71w`|`()`|`System.Windows.Forms.Label [class:245]`|`1996536`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001478`|`fTnP8w92Hua`|`System.Windows.Forms.Label [class:245]`|`void`|`1996552`|`4`|`ae3c8b8d99a39542...`|
|`0x06001479`|`oOEP82n2gLq`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1996656`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600147a`|`x6hP8XYGLGq`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1996672`|`4`|`ae3c8b8d99a39542...`|
|`0x0600147b`|`W4FP8iVKdNW`|`()`|`System.Windows.Forms.Label [class:245]`|`1996776`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600147c`|`tmOP8nTFX3s`|`System.Windows.Forms.Label [class:245]`|`void`|`1996792`|`4`|`ae3c8b8d99a39542...`|
|`0x0600147d`|`GBTP85Ff5Fx`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1996896`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600147e`|`asIP8SahChj`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1996912`|`4`|`ae3c8b8d99a39542...`|
|`0x0600147f`|`X54P8jvmlgj`|`()`|`System.Windows.Forms.Label [class:245]`|`1997016`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001480`|`jiBP8xb7vKj`|`System.Windows.Forms.Label [class:245]`|`void`|`1997032`|`4`|`ae3c8b8d99a39542...`|
|`0x06001481`|`qKdP8Dta8GT`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1997136`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001482`|`JwuP8Bn7ZhN`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1997152`|`4`|`ae3c8b8d99a39542...`|
|`0x06001483`|`ibJP8OsTvYo`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`1997256`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001484`|`N3fP8VEtoii`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`1997272`|`4`|`ae3c8b8d99a39542...`|
|`0x06001485`|`FL9P8aOgvPI`|`()`|`System.Windows.Forms.Label [class:245]`|`1997376`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001486`|`h2UP8zZPpkj`|`System.Windows.Forms.Label [class:245]`|`void`|`1997392`|`4`|`ae3c8b8d99a39542...`|
|`0x06001487`|`pr7PEULBWNr`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1997496`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001488`|`Lc0PE8jQCbu`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1997512`|`4`|`ae3c8b8d99a39542...`|
|`0x06001489`|`i5MPEEcrNxM`|`()`|`System.Windows.Forms.Label [class:245]`|`1997616`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600148a`|`G7fPE0AMFC5`|`System.Windows.Forms.Label [class:245]`|`void`|`1997632`|`4`|`ae3c8b8d99a39542...`|
|`0x0600148b`|`MRUPEPqGG2u`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1997736`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600148c`|`FP7PE3AvWVc`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1997752`|`4`|`ae3c8b8d99a39542...`|
|`0x0600148d`|`s51PEqXg2P5`|`()`|`System.Windows.Forms.Label [class:245]`|`1997856`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600148e`|`i9RPEAEDuM5`|`System.Windows.Forms.Label [class:245]`|`void`|`1997872`|`4`|`ae3c8b8d99a39542...`|
|`0x0600148f`|`z4cPECGkXle`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1997976`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001490`|`ns4PEmg1bvX`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1997992`|`4`|`ae3c8b8d99a39542...`|
|`0x06001491`|`W15PEQp1UBS`|`()`|`System.Windows.Forms.Label [class:245]`|`1998096`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001492`|`vRIPEkcg17J`|`System.Windows.Forms.Label [class:245]`|`void`|`1998112`|`4`|`ae3c8b8d99a39542...`|
|`0x06001493`|`I31PE4OtTUb`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1998216`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001494`|`U9GPEYlttv7`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1998232`|`4`|`ae3c8b8d99a39542...`|
|`0x06001495`|`f4CPEoF3HiL`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1998336`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001496`|`nVvPE7E5gho`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1998352`|`4`|`ae3c8b8d99a39542...`|
|`0x06001497`|`wJy0stTcJbO`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1998456`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001498`|`WHg0sdaIvIc`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1998472`|`4`|`ae3c8b8d99a39542...`|
|`0x06001499`|`ktvPEFFVX9H`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1998576`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600149a`|`Q5MPEvEcdvC`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1998592`|`4`|`ae3c8b8d99a39542...`|
|`0x0600149b`|`ShqPE1muqE1`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1998696`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600149c`|`GpwPEM9phQw`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1998712`|`4`|`ae3c8b8d99a39542...`|
|`0x0600149d`|`jJiPE9wPJAI`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1998816`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600149e`|`yeSPEKLW2tE`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1998832`|`4`|`ae3c8b8d99a39542...`|
|`0x0600149f`|`fUJPENTs6Yo`|`()`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`1998936`|`4`|`8969ce0cdaa4a19c...`|
|`0x060014a0`|`NQaPEbyS9Ks`|`System.Windows.Forms.DataGridViewTextBoxColumn [class:381]`|`void`|`1998952`|`4`|`ae3c8b8d99a39542...`|
|`0x060014a1`|`qwYPEIUI1dh`|`()`|`System.Windows.Forms.Label [class:245]`|`1999056`|`4`|`8969ce0cdaa4a19c...`|
|`0x060014a2`|`e7gPEyUDD4R`|`System.Windows.Forms.Label [class:245]`|`void`|`1999072`|`4`|`ae3c8b8d99a39542...`|
|`0x060014a3`|`M90PERgPToq`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1999176`|`4`|`8969ce0cdaa4a19c...`|
|`0x060014a4`|`flJPEgmp8s5`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1999192`|`4`|`ae3c8b8d99a39542...`|
|`0x060014a5`|`NNuPEhGn9Bw`|`()`|`System.Windows.Forms.Button [class:161]`|`1999296`|`4`|`8969ce0cdaa4a19c...`|
|`0x060014a6`|`U7FPEZM7yGo`|`System.Windows.Forms.Button [class:161]`|`void`|`1999312`|`4`|`ae3c8b8d99a39542...`|
|`0x060014a7`|`IEpPEeciam0`|`()`|`System.Windows.Forms.Label [class:245]`|`1999680`|`4`|`8969ce0cdaa4a19c...`|
|`0x060014a8`|`Vh7PELQKFTa`|`System.Windows.Forms.Label [class:245]`|`void`|`1999696`|`4`|`ae3c8b8d99a39542...`|
|`0x060014a9`|`I3NPETgowyL`|`()`|`System.Windows.Forms.TextBox [class:249]`|`1999800`|`4`|`8969ce0cdaa4a19c...`|
|`0x060014aa`|`P81PEfr34t3`|`System.Windows.Forms.TextBox [class:249]`|`void`|`1999816`|`4`|`ae3c8b8d99a39542...`|
|`0x060014ab`|`qq0PEcL86sB`|`()`|`System.Windows.Forms.Button [class:161]`|`1999920`|`4`|`8969ce0cdaa4a19c...`|
|`0x060014ac`|`Q26PE6Vsn0b`|`System.Windows.Forms.Button [class:161]`|`void`|`1999936`|`4`|`ae3c8b8d99a39542...`|
|`0x060014ad`|`lJNt7aiMZo`|`object, System.EventArgs [class:113]`|`void`|`2000264`|`4`|`ae3c8b8d99a39542...`|
|`0x060014ae`|`RRYtFBVV2i`|`object, System.EventArgs [class:113]`|`void`|`2002040`|`4`|`ae3c8b8d99a39542...`|
|`0x060014af`|`X5JtvVdIDe`|`()`|`void`|`2002200`|`4`|`ae3c8b8d99a39542...`|
|`0x060014b0`|`sFMt1KIXXB`|`object, System.Windows.Forms.FormClosingEventArgs [class:301]`|`void`|`2019124`|`4`|`ae3c8b8d99a39542...`|
|`0x060014b1`|`J5ZtMyfsvq`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`2019280`|`4`|`ae3c8b8d99a39542...`|
|`0x060014b2`|`e7lt9pOPuN`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`2019808`|`4`|`ae3c8b8d99a39542...`|
|`0x060014b3`|`DSJtKetHaU`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`2020072`|`4`|`ae3c8b8d99a39542...`|
|`0x060014b4`|`TEstNbYeUm`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`2020468`|`4`|`ae3c8b8d99a39542...`|
|`0x060014b5`|`G9ftbKgK6r`|`System.Drawing.Point [valuetype:173]`|`Kata_Class_Lib_Revit.diem [class:85]`|`2020640`|`4`|`8969ce0cdaa4a19c...`|
|`0x060014b6`|`V90tI2Nulb`|`System.Drawing.Point [valuetype:173]`|`void`|`2021220`|`4`|`ae3c8b8d99a39542...`|
|`0x060014b7`|`Iq6tyT1olB`|`object, System.EventArgs [class:113]`|`void`|`2022028`|`4`|`ae3c8b8d99a39542...`|
|`0x060014b8`|`iOqtRihOin`|`object, System.EventArgs [class:113]`|`void`|`2022044`|`4`|`ae3c8b8d99a39542...`|
|`0x060014b9`|`k7Ktg99slm`|`()`|`void`|`2022456`|`4`|`ae3c8b8d99a39542...`|
|`0x060014ba`|`tX1th0Ifa3`|`()`|`void`|`2022704`|`4`|`ae3c8b8d99a39542...`|
|`0x060014bb`|`T6TtZ8CBkX`|`i4`|`void`|`2022816`|`4`|`ae3c8b8d99a39542...`|
|`0x060014bc`|`JLrtes63X8`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`2023272`|`4`|`ae3c8b8d99a39542...`|
|`0x060014bd`|`SetDefault`|`()`|`void`|`2023432`|`4`|`ae3c8b8d99a39542...`|
|`0x060014be`|`doi_du_lieu`|`bool, bool`|`void`|`2023540`|`4`|`ae3c8b8d99a39542...`|
|`0x060014bf`|`fUFtLv5cj1`|`()`|`void`|`2024192`|`4`|`ae3c8b8d99a39542...`|
|`0x060014c0`|`OU2tTBNFcO`|`InfoVeMatBangMong [class:3528]`|`void`|`2024640`|`4`|`ae3c8b8d99a39542...`|
|`0x060014c1`|`rljtfuTmgO`|`()`|`void`|`2025612`|`4`|`ae3c8b8d99a39542...`|
|`0x060014c2`|`CWatcF5u6q`|`object, System.EventArgs [class:113]`|`void`|`2025804`|`4`|`ae3c8b8d99a39542...`|
|`0x060014c3`|`SA9t66nFGg`|`object, System.EventArgs [class:113]`|`void`|`2026112`|`4`|`ae3c8b8d99a39542...`|
|`0x060014c4`|`chuyenDuLieuFormSangInfoVeMatBangMong`|`bool`|`void`|`2026400`|`4`|`ae3c8b8d99a39542...`|
|`0x060014c5`|`IhutW9KmwA`|`object, System.EventArgs [class:113]`|`void`|`2030756`|`4`|`ae3c8b8d99a39542...`|
|`0x060014c6`|`Wretsjb2qQ`|`object, System.Windows.Forms.DataGridViewCellEventArgs [class:841]`|`void`|`2030908`|`4`|`ae3c8b8d99a39542...`|
|`0x060014c7`|`i0KttZffvO`|`object, System.EventArgs [class:113]`|`void`|`2031424`|`4`|`ae3c8b8d99a39542...`|
|`0x060014c8`|`AJctddBmFm`|`object, System.Windows.Forms.DataGridViewDataErrorEventArgs [class:989]`|`void`|`2031600`|`4`|`ae3c8b8d99a39542...`|
|`0x060014c9`|`O26tJUP86u`|`object, System.Windows.Forms.DataGridViewCellCancelEventArgs [class:993]`|`void`|`2031708`|`4`|`ae3c8b8d99a39542...`|
|`0x060014ca`|`TrXtrTfwuf`|`object, System.Windows.Forms.DataGridViewCellCancelEventArgs [class:993]`|`void`|`2031948`|`4`|`ae3c8b8d99a39542...`|
|`0x060014cb`|`u89tGljpHT`|`object, System.EventArgs [class:113]`|`void`|`2032188`|`4`|`ae3c8b8d99a39542...`|
|`0x060014cc`|`ve_mat_bang_mong`|`()`|`void`|`2032340`|`4`|`ae3c8b8d99a39542...`|
|`0x060014cd`|`tcttlHgKU7`|`object, System.EventArgs [class:113]`|`void`|`2036064`|`4`|`ae3c8b8d99a39542...`|
|`0x060014ce`|`lLStuYSXcZ`|`object, System.EventArgs [class:113]`|`void`|`2045828`|`4`|`ae3c8b8d99a39542...`|
|`0x060014cf`|`VOptHGZk5e`|`()`|`void`|`2045944`|`4`|`ae3c8b8d99a39542...`|
|`0x060014d0`|`LhZtp0ac4h`|`()`|`System.Collections.Generic.List`1 [class:217]<System.Tuple`2 [class:285]<Polygon [class:89],string>>`|`2046888`|`4`|`8969ce0cdaa4a19c...`|
|`0x060014d1`|`e0ntw0vAmW`|`()`|`void`|`2051536`|`4`|`ae3c8b8d99a39542...`|
|`0x060014d2`|`kKkt2RlBSn`|`object, System.EventArgs [class:113]`|`void`|`2054536`|`4`|`ae3c8b8d99a39542...`|
|`0x060014d3`|`cp3tXmWey1`|`object, System.EventArgs [class:113]`|`void`|`2054804`|`4`|`ae3c8b8d99a39542...`|
|`0x060014d4`|`MgNtipxKmB`|`object, System.EventArgs [class:113]`|`void`|`2055628`|`4`|`ae3c8b8d99a39542...`|
|`0x060014d5`|`qhYtngMd4x`|`object, System.EventArgs [class:113]`|`void`|`2056388`|`4`|`ae3c8b8d99a39542...`|
|`0x060014d6`|`hNmt5ySeBu`|`System.Collections.Generic.List`1 [class:217]<InfoCotCocGenMong [class:3584]>, bool, bool`|`void`|`2057260`|`4`|`ae3c8b8d99a39542...`|
|`0x060014d7`|`NWWtSUnU4e`|`object, System.EventArgs [class:113]`|`void`|`2061216`|`4`|`ae3c8b8d99a39542...`|
|`0x060014d8`|`eWZtjpbNVp`|`InfoCotCocGenMong [class:3584], InfoCotCocGenMong [class:3584]`|`bool`|`2063168`|`4`|`66ea1533cb3404e9...`|
|`0x060014d9`|`GIbtxyEQWL`|`System.Collections.Generic.List`1 [class:217]<InfoCotCocGenMong [class:3584]>`|`System.Collections.Generic.List`1 [class:217]<System.Collections.Generic.List`1 [class:217]<InfoCotCocGenMong [class:3584]>>`|`2063540`|`4`|`8969ce0cdaa4a19c...`|
|`0x060014da`|`FmetDXOCEx`|`object, System.EventArgs [class:113]`|`void`|`2065276`|`4`|`ae3c8b8d99a39542...`|
|`0x060014db`|`u4OtB8G9WN`|`object, System.EventArgs [class:113]`|`void`|`2066460`|`4`|`ae3c8b8d99a39542...`|
|`0x060014dc`|`LGetOPK3iL`|`object, System.EventArgs [class:113]`|`void`|`2066612`|`4`|`ae3c8b8d99a39542...`|
|`0x060014dd`|`T1UtVjO0YA`|`object, System.EventArgs [class:113]`|`void`|`2070024`|`4`|`ae3c8b8d99a39542...`|
|`0x060014de`|`zg5taqfJIW`|`object, System.EventArgs [class:113]`|`void`|`2070364`|`4`|`ae3c8b8d99a39542...`|
|`0x060014df`|`qTMtzVaSYG`|`object, System.EventArgs [class:113]`|`void`|`2071180`|`4`|`ae3c8b8d99a39542...`|
|`0x060014e0`|`Mo5dUEAF68`|`object, System.EventArgs [class:113]`|`void`|`2071956`|`4`|`ae3c8b8d99a39542...`|
|`0x060014e1`|`lLkd8HcRK6`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`2072864`|`4`|`ae3c8b8d99a39542...`|
|`0x060014e2`|`m1ZdE3SHRf`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`2073412`|`4`|`ae3c8b8d99a39542...`|
|`0x060014e3`|`tXTd0A9imJ`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`2073656`|`4`|`ae3c8b8d99a39542...`|
|`0x060014e4`|`MRcdPd8Lid`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`2074032`|`4`|`ae3c8b8d99a39542...`|
|`0x060014e5`|`Fnad3Sw1sB`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`2074192`|`4`|`ae3c8b8d99a39542...`|
|`0x060014e6`|`Wgidqbtsha`|`System.Drawing.Point [valuetype:173]`|`Kata_Class_Lib_Revit.diem [class:85]`|`2074364`|`4`|`8969ce0cdaa4a19c...`|
|`0x060014e7`|`x9VdACb4ua`|`System.Drawing.Point [valuetype:173]`|`void`|`2074640`|`4`|`ae3c8b8d99a39542...`|
|`0x060014e8`|`RPAdC2WEmF`|`object, System.EventArgs [class:113]`|`void`|`2075368`|`4`|`ae3c8b8d99a39542...`|
|`0x060014e9`|`w4Hdmxb3uZ`|`object, System.EventArgs [class:113]`|`void`|`2075384`|`4`|`ae3c8b8d99a39542...`|
|`0x060014ea`|`XvydQiXU4B`|`()`|`void`|`2075776`|`4`|`ae3c8b8d99a39542...`|
|`0x060014eb`|`AtsdkRjHuQ`|`i4`|`void`|`2076004`|`4`|`ae3c8b8d99a39542...`|
|`0x060014ec`|`du6d4GR7Io`|`()`|`void`|`2076460`|`4`|`ae3c8b8d99a39542...`|
|`0x060014ed`|`brndYqURAd`|`string`|`i4`|`2076568`|`4`|`78d4d2062aa5373e...`|
|`0x060014ee`|`lBudo79XN6`|`ViTriCurveDam [valuetype:3540]`|`string`|`2077312`|`4`|`8969ce0cdaa4a19c...`|
|`0x060014ef`|`O0dd7TXZTJ`|`i4, string`|`string`|`2077848`|`4`|`8969ce0cdaa4a19c...`|
|`0x060014f0`|`QOydFZdeMa`|`object, System.EventArgs [class:113]`|`void`|`2078980`|`4`|`ae3c8b8d99a39542...`|
|`0x060014f1`|`KOJdv9jirF`|`string, string`|`System.Windows.Forms.TextBox [class:249]`|`2080672`|`4`|`8969ce0cdaa4a19c...`|
|`0x060014f2`|`sg6d1IqQtO`|`Polygon [class:89], CurveXYZ [class:293], r8`|`CurveXYZ [class:293]`|`2081372`|`4`|`8969ce0cdaa4a19c...`|
|`0x060014f3`|`BCpdMa3ot1`|`Polygon [class:89]`|`void`|`2081816`|`4`|`ae3c8b8d99a39542...`|
|`0x060014f4`|`doi_du_lieu_mat_bang_mong_bang`|`bool`|`void`|`2082744`|`4`|`ae3c8b8d99a39542...`|
|`0x060014f5`|`chuyenDuLieuFormSangInfoMatBangMongBang`|`()`|`void`|`2083272`|`4`|`ae3c8b8d99a39542...`|
|`0x060014f6`|`dDCd9kf2dQ`|`System.Windows.Forms.DataGridViewRow [class:437], i4, string`|`string`|`2084880`|`4`|`8969ce0cdaa4a19c...`|
|`0x060014f7`|`iQNdKgXDXL`|`string, string`|`i4`|`2085320`|`4`|`78d4d2062aa5373e...`|
|`0x060014f8`|`aXbdNbF5gD`|`System.Windows.Forms.Control [class:97], string, System.Collections.Generic.HashSet`1 [class:385]<i4>`|`void`|`2086100`|`4`|`ae3c8b8d99a39542...`|
|`0x060014f9`|`n8idb1b0H6`|`object, System.EventArgs [class:113]`|`void`|`2086776`|`4`|`ae3c8b8d99a39542...`|
|`0x060014fa`|`OeXdIdFadG`|`object, System.EventArgs [class:113]`|`void`|`2089828`|`4`|`ae3c8b8d99a39542...`|
|`0x060014fb`|`CskdydBtuf`|`object, System.EventArgs [class:113]`|`void`|`2090088`|`4`|`ae3c8b8d99a39542...`|
|`0x060014fc`|`v2XdRArtg4`|`Polygon [class:89]`|`bool`|`2093776`|`4`|`66ea1533cb3404e9...`|
|`0x060014fd`|`IoGdg7m6S9`|`object, System.EventArgs [class:113]`|`void`|`2093812`|`4`|`ae3c8b8d99a39542...`|
|`0x060014fe`|`P3WdhvkPQa`|`object, System.EventArgs [class:113]`|`void`|`2094596`|`4`|`ae3c8b8d99a39542...`|
|`0x060014ff`|`BITdZB1N0Y`|`object, System.EventArgs [class:113]`|`void`|`2095400`|`4`|`ae3c8b8d99a39542...`|
|`0x06001500`|`NmodeM8vZi`|`object, System.EventArgs [class:113]`|`void`|`2096140`|`4`|`ae3c8b8d99a39542...`|
|`0x06001501`|`Ke2dLG5KRx`|`()`|`void`|`2096924`|`4`|`ae3c8b8d99a39542...`|
|`0x06001502`|`hvwdTRIFT8`|`object, System.EventArgs [class:113]`|`void`|`2100960`|`4`|`ae3c8b8d99a39542...`|
|`0x06001503`|`WNRdfBmZlU`|`object, System.EventArgs [class:113]`|`void`|`2101120`|`4`|`ae3c8b8d99a39542...`|
|`0x06001504`|`Nfrdcfb4bl`|`object, System.EventArgs [class:113]`|`void`|`2101300`|`4`|`ae3c8b8d99a39542...`|
|`0x06001505`|`nFld6Ll5pU`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`2101620`|`4`|`ae3c8b8d99a39542...`|
|`0x06001506`|`NZmdWEvRaW`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`2102148`|`4`|`ae3c8b8d99a39542...`|
|`0x06001507`|`Ox4dsjfQ0k`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`2102392`|`4`|`ae3c8b8d99a39542...`|
|`0x06001508`|`gDFdt9on5t`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`2102848`|`4`|`ae3c8b8d99a39542...`|
|`0x06001509`|`uAJddqHlK9`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`2103008`|`4`|`ae3c8b8d99a39542...`|
|`0x0600150a`|`WetdJcmpyZ`|`System.Drawing.Point [valuetype:173]`|`Kata_Class_Lib_Revit.diem [class:85]`|`2103180`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600150b`|`jU2drIXaxn`|`System.Drawing.Point [valuetype:173]`|`void`|`2103456`|`4`|`ae3c8b8d99a39542...`|
|`0x0600150c`|`cRQdG3BBTS`|`object, System.EventArgs [class:113]`|`void`|`2104176`|`4`|`ae3c8b8d99a39542...`|
|`0x0600150d`|`ngjdlQdnrq`|`object, System.EventArgs [class:113]`|`void`|`2104192`|`4`|`ae3c8b8d99a39542...`|
|`0x0600150e`|`XbRdu0WHUc`|`()`|`void`|`2104604`|`4`|`ae3c8b8d99a39542...`|
|`0x0600150f`|`LMUdHcpwC0`|`i4`|`void`|`2104852`|`4`|`ae3c8b8d99a39542...`|
|`0x06001510`|`MpOdpPrFn5`|`()`|`void`|`2105308`|`4`|`ae3c8b8d99a39542...`|
|`0x06001511`|`Hu7dwY39SL`|`object, System.EventArgs [class:113]`|`void`|`2105416`|`4`|`ae3c8b8d99a39542...`|
|`0x06001512`|`CBud225qba`|`string, string`|`System.Windows.Forms.TextBox [class:249]`|`2107748`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001513`|`WXOdXHYa4Q`|`CotChiuTaiMongDon [class:3624]`|`void`|`2108364`|`4`|`ae3c8b8d99a39542...`|
|`0x06001514`|`doi_du_lieu_mat_bang_mong_don`|`bool`|`void`|`2110136`|`4`|`ae3c8b8d99a39542...`|
|`0x06001515`|`chuyenDuLieuFormSangInfoMBMD`|`bool`|`void`|`2110632`|`4`|`ae3c8b8d99a39542...`|
|`0x06001516`|`iPIdijjq0k`|`object, System.EventArgs [class:113]`|`void`|`2112752`|`4`|`ae3c8b8d99a39542...`|
|`0x06001517`|`pJednOLx0T`|`object, System.EventArgs [class:113]`|`void`|`2113624`|`4`|`ae3c8b8d99a39542...`|
|`0x06001518`|`gGxd5tYilM`|`InfoRaiMongDon [class:3296]`|`string`|`2113844`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001519`|`Er4dSd8o69`|`object, System.EventArgs [class:113]`|`void`|`2113904`|`4`|`ae3c8b8d99a39542...`|
|`0x0600151a`|`W6Fdj3xhfU`|`()`|`void`|`2117516`|`4`|`ae3c8b8d99a39542...`|
|`0x0600151b`|`GTvdx57QX4`|`object, System.EventArgs [class:113]`|`void`|`2118440`|`4`|`ae3c8b8d99a39542...`|
|`0x0600151c`|`vVCdDVOOCx`|`object, System.EventArgs [class:113]`|`void`|`2119244`|`4`|`ae3c8b8d99a39542...`|
|`0x0600151d`|`wWodBduB3F`|`object, System.EventArgs [class:113]`|`void`|`2120008`|`4`|`ae3c8b8d99a39542...`|
|`0x0600151e`|`lAldO8SLYW`|`object, System.EventArgs [class:113]`|`void`|`2120748`|`4`|`ae3c8b8d99a39542...`|
|`0x0600151f`|`TpXdVeguGM`|`object, System.EventArgs [class:113]`|`void`|`2121528`|`4`|`ae3c8b8d99a39542...`|
|`0x06001520`|`PSqdaO52BU`|`()`|`void`|`2122588`|`4`|`ae3c8b8d99a39542...`|
|`0x06001521`|`A5Bdzecy9P`|`object, System.EventArgs [class:113]`|`void`|`2125412`|`4`|`ae3c8b8d99a39542...`|
|`0x06001522`|`DkdJUggs6q`|`object, System.EventArgs [class:113]`|`void`|`2125572`|`4`|`ae3c8b8d99a39542...`|
|`0x06001523`|`KkRJ8iEEn4`|`object, System.EventArgs [class:113]`|`void`|`2125732`|`4`|`ae3c8b8d99a39542...`|
|`0x06001524`|`zGYJECZjke`|`object, System.EventArgs [class:113]`|`void`|`2126028`|`4`|`ae3c8b8d99a39542...`|
|`0x06001525`|`hRVJ0WZXyY`|`object, System.EventArgs [class:113]`|`void`|`2126324`|`4`|`ae3c8b8d99a39542...`|
|`0x06001526`|`qO0JP0Cpv5`|`object, System.EventArgs [class:113]`|`void`|`2127112`|`4`|`ae3c8b8d99a39542...`|
|`0x06001527`|`OcRJ3kOxhD`|`object, System.EventArgs [class:113]`|`void`|`2128204`|`4`|`ae3c8b8d99a39542...`|
|`0x06001528`|`iBEJqsoIkg`|`bool`|`void`|`2128324`|`4`|`ae3c8b8d99a39542...`|
|`0x06001529`|`qEdJA6Dhc4`|`object, System.EventArgs [class:113]`|`void`|`2129568`|`4`|`ae3c8b8d99a39542...`|
|`0x0600152a`|`XgcJCwP2bI`|`()`|`bool`|`2131060`|`4`|`66ea1533cb3404e9...`|
|`0x0600152b`|`icDJm9NtCx`|`object, System.EventArgs [class:113]`|`void`|`2131380`|`4`|`ae3c8b8d99a39542...`|
|`0x0600152c`|`kW4JQDp96m`|`object, System.EventArgs [class:113]`|`void`|`2131484`|`4`|`ae3c8b8d99a39542...`|
|`0x0600152d`|`gbRJks9M1K`|`object, System.EventArgs [class:113]`|`void`|`2131752`|`4`|`ae3c8b8d99a39542...`|
|`0x0600152e`|`xpeJ4emsE3`|`object, System.EventArgs [class:113]`|`void`|`2131856`|`4`|`ae3c8b8d99a39542...`|
|`0x0600152f`|`CalculateSteelForVmbmLight`|`string`|`bool`|`2137792`|`4`|`66ea1533cb3404e9...`|
|`0x06001530`|`CalculateSteelFromLightMoments`|`System.Collections.Generic.List`1 [class:217]<MomentMong [class:5312]>, string`|`bool`|`2138932`|`4`|`66ea1533cb3404e9...`|
|`0x06001531`|`CuVJYSIArx`|`object, string`|`void`|`2139876`|`4`|`ae3c8b8d99a39542...`|
|`0x06001532`|`ReadSteelForLight`|`string`|`System.Collections.Generic.List`1 [class:217]<MomentMong [class:5312]>`|`2140936`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001533`|`nudJo2S0Kt`|`object`|`void`|`2142064`|`4`|`ae3c8b8d99a39542...`|
|`0x06001534`|`tRSJ7Fo6nL`|`object, i8, i8, Kata_pro64_Cad2025.TuyChonTinhThepMongTuDong [class:680]`|`Kata_pro64_Cad2025.KetQuaTinhThepMongTuDong [class:684]`|`2142804`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001535`|`SGqJFZq7Ly`|`r8, Kata_pro64_Cad2025.TuyChonTinhThepMongTuDong [class:680]`|`Kata_pro64_Cad2025.PhuongAnThepMong [class:688]`|`2145760`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001536`|`UIiJvnxBIZ`|`i4, i4`|`r8`|`2149096`|`4`|`f0ad83c607e59d90...`|
|`0x06001537`|`zIGJ10Pwjm`|`Kata_pro64_Cad2025.PhuongAnThepMong [class:688], Kata_pro64_Cad2025.PhuongAnThepMong [class:688]`|`bool`|`2149160`|`4`|`66ea1533cb3404e9...`|
|`0x06001538`|`O8FJMYVGsw`|`object, r8&`|`bool`|`2150164`|`4`|`66ea1533cb3404e9...`|
|`0x06001539`|`GetListMomentMongFromExcel`|`()`|`System.Collections.Generic.List`1 [class:217]<MomentMong [class:5312]>`|`2151020`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600153a`|`DMqJ9UCi5N`|`object, System.EventArgs [class:113]`|`void`|`2156068`|`4`|`ae3c8b8d99a39542...`|
|`0x0600153b`|`ExportSteelForVmbmLight`|`()`|`i4`|`2158064`|`4`|`78d4d2062aa5373e...`|
|`0x0600153c`|`ECnJKsHO5w`|`object, System.EventArgs [class:113]`|`void`|`2158220`|`4`|`ae3c8b8d99a39542...`|
|`0x0600153d`|`_Lambda$__1050-0`|`()`|`void`|`2158348`|`4`|`ae3c8b8d99a39542...`|
|`0x0600153e`|`_Lambda$__1098-0`|`()`|`void`|`2158824`|`4`|`ae3c8b8d99a39542...`|
|`0x0600153f`|`_Lambda$__1099-3`|`Kata_Class_Lib_Revit.Info_Beam3D [class:481]`|`r8`|`2159056`|`4`|`f0ad83c607e59d90...`|
|`0x06001540`|`_Lambda$__1099-4`|`Kata_Class_Lib_Revit.Info_Beam3D [class:481]`|`r8`|`2159260`|`4`|`f0ad83c607e59d90...`|
|`0x06001541`|`_Lambda$__1131-0`|`()`|`void`|`2159424`|`4`|`ae3c8b8d99a39542...`|
|`0x06001542`|`_Lambda$__1152-0`|`()`|`void`|`2159656`|`4`|`ae3c8b8d99a39542...`|
|`0x06001543`|`.cctor`|`()`|`void`|`2159896`|`156`|`ff31ddf5824eed93...`|
|`0x06001544`|`Tl7Thbq0CFbdCn7cNYbv`|`()`|`bool`|`2160064`|`4`|`66ea1533cb3404e9...`|
|`0x06001545`|`mbIuEFq0mrvrMnWo7teY`|`()`|`Kata_pro64_Cad2025.Form_Ve_Mat_Bang_Mong [class:628]`|`2160084`|`4`|`8969ce0cdaa4a19c...`|

### [VM-194] Methods of `Kata_pro64_Cad2025.TuyChonTinhThepMongTuDong` (Token `0x020000aa`, Displayed: 4/4, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x0600159c`|`.ctor`|`()`|`void`|`2160100`|`4`|`ae3c8b8d99a39542...`|
|`0x0600159d`|`.cctor`|`()`|`void`|`2160348`|`156`|`d34ffa8f98f2282f...`|
|`0x0600159e`|`RvcKA2q0kMegOVLfm5uM`|`()`|`bool`|`2160516`|`4`|`66ea1533cb3404e9...`|
|`0x0600159f`|`gMAJvMq04bmAKy53pKBl`|`()`|`Kata_pro64_Cad2025.TuyChonTinhThepMongTuDong [class:680]`|`2160536`|`4`|`8969ce0cdaa4a19c...`|

### [VM-195] Methods of `Kata_pro64_Cad2025.KetQuaTinhThepMongTuDong` (Token `0x020000ab`, Displayed: 4/4, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x060015a0`|`.ctor`|`()`|`void`|`2160552`|`4`|`ae3c8b8d99a39542...`|
|`0x060015a1`|`.cctor`|`()`|`void`|`2160764`|`177`|`aa0c3e686f8d1f50...`|
|`0x060015a2`|`nkA6Nfq0o66TiZZXo73o`|`()`|`bool`|`2160956`|`4`|`66ea1533cb3404e9...`|
|`0x060015a3`|`fwfgjiq073vEgteWguAF`|`()`|`Kata_pro64_Cad2025.KetQuaTinhThepMongTuDong [class:684]`|`2160976`|`4`|`8969ce0cdaa4a19c...`|

### [VM-196] Methods of `Kata_pro64_Cad2025.PhuongAnThepMong` (Token `0x020000ac`, Displayed: 5/5, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x060015a4`|`.ctor`|`()`|`void`|`2160992`|`4`|`ae3c8b8d99a39542...`|
|`0x060015a5`|`get_SoLop`|`()`|`i4`|`2161208`|`4`|`78d4d2062aa5373e...`|
|`0x060015a6`|`.cctor`|`()`|`void`|`2161464`|`177`|`1f2e0cd305db3fc3...`|
|`0x060015a7`|`dSFAUUq0v2tnVMpQ3oSS`|`()`|`bool`|`2161656`|`4`|`66ea1533cb3404e9...`|
|`0x060015a8`|`Vksq4bq01c2iqrmCqG2r`|`()`|`Kata_pro64_Cad2025.PhuongAnThepMong [class:688]`|`2161676`|`4`|`8969ce0cdaa4a19c...`|

### [VM-197] Methods of `Kata_pro64_Cad2025.Form_Chon_Thep_Mong_Tu_Dong` (Token `0x020000ad`, Displayed: 31/31, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x060015a9`|`.cctor`|`()`|`void`|`2161692`|`489`|`a381f10edc6a4083...`|
|`0x060015aa`|`get_TuyChon`|`()`|`Kata_pro64_Cad2025.TuyChonTinhThepMongTuDong [class:680]`|`2162196`|`4`|`8969ce0cdaa4a19c...`|
|`0x060015ab`|`set_TuyChon`|`Kata_pro64_Cad2025.TuyChonTinhThepMongTuDong [class:680]`|`void`|`2162212`|`4`|`ae3c8b8d99a39542...`|
|`0x060015ac`|`.ctor`|`()`|`void`|`2162316`|`4`|`ae3c8b8d99a39542...`|
|`0x060015ad`|`WBvuiMAxl5`|`()`|`void`|`2162936`|`4`|`ae3c8b8d99a39542...`|
|`0x060015ae`|`sFjunY3ASx`|`object, System.EventArgs [class:113]`|`void`|`2165960`|`4`|`ae3c8b8d99a39542...`|
|`0x060015af`|`WWRu5DR1Rm`|`System.Windows.Forms.CheckedListBox [class:1049], System.Collections.Generic.List`1 [class:217]<i4>, System.Collections.Generic.List`1 [class:217]<i4>`|`void`|`2166740`|`4`|`ae3c8b8d99a39542...`|
|`0x060015b0`|`aGfuS5OWfk`|`System.Windows.Forms.CheckedListBox [class:1049], System.Windows.Forms.TextBox [class:249]`|`void`|`2167428`|`4`|`ae3c8b8d99a39542...`|
|`0x060015b1`|`nmaujiYDPg`|`System.Windows.Forms.CheckedListBox [class:1049], System.Collections.Generic.List`1 [class:217]<i4>`|`void`|`2168432`|`4`|`ae3c8b8d99a39542...`|
|`0x060015b2`|`VQIux0K8pL`|`System.Windows.Forms.CheckedListBox [class:1049], bool`|`void`|`2169080`|`4`|`ae3c8b8d99a39542...`|
|`0x060015b3`|`jqduDk7ewt`|`System.Windows.Forms.CheckedListBox [class:1049]`|`System.Collections.Generic.List`1 [class:217]<i4>`|`2169356`|`4`|`8969ce0cdaa4a19c...`|
|`0x060015b4`|`OmPuBU1wPm`|`System.Windows.Forms.CheckedListBox [class:1049]`|`System.Collections.Generic.List`1 [class:217]<i4>`|`2170000`|`4`|`8969ce0cdaa4a19c...`|
|`0x060015b5`|`V7muOvR0E0`|`string`|`System.Collections.Generic.List`1 [class:217]<i4>`|`2170668`|`4`|`8969ce0cdaa4a19c...`|
|`0x060015b6`|`_Lambda$__R16-1`|`object, System.EventArgs [class:113]`|`void`|`2171196`|`4`|`ae3c8b8d99a39542...`|
|`0x060015b7`|`_Lambda$__16-0`|`()`|`void`|`2171212`|`4`|`ae3c8b8d99a39542...`|
|`0x060015b8`|`_Lambda$__R16-2`|`object, System.EventArgs [class:113]`|`void`|`2171328`|`4`|`ae3c8b8d99a39542...`|
|`0x060015b9`|`_Lambda$__16-1`|`()`|`void`|`2171344`|`4`|`ae3c8b8d99a39542...`|
|`0x060015ba`|`_Lambda$__R16-3`|`object, System.EventArgs [class:113]`|`void`|`2171460`|`4`|`ae3c8b8d99a39542...`|
|`0x060015bb`|`_Lambda$__16-2`|`()`|`void`|`2171476`|`4`|`ae3c8b8d99a39542...`|
|`0x060015bc`|`_Lambda$__R16-4`|`object, System.EventArgs [class:113]`|`void`|`2171640`|`4`|`ae3c8b8d99a39542...`|
|`0x060015bd`|`_Lambda$__16-3`|`()`|`void`|`2171656`|`4`|`ae3c8b8d99a39542...`|
|`0x060015be`|`_Lambda$__R16-5`|`object, System.EventArgs [class:113]`|`void`|`2171800`|`4`|`ae3c8b8d99a39542...`|
|`0x060015bf`|`_Lambda$__16-4`|`()`|`void`|`2171816`|`4`|`ae3c8b8d99a39542...`|
|`0x060015c0`|`_Lambda$__R16-6`|`object, System.EventArgs [class:113]`|`void`|`2171928`|`4`|`ae3c8b8d99a39542...`|
|`0x060015c1`|`_Lambda$__16-5`|`()`|`void`|`2171944`|`4`|`ae3c8b8d99a39542...`|
|`0x060015c2`|`_Lambda$__R16-7`|`object, System.EventArgs [class:113]`|`void`|`2172056`|`4`|`ae3c8b8d99a39542...`|
|`0x060015c3`|`_Lambda$__16-6`|`()`|`void`|`2172072`|`4`|`ae3c8b8d99a39542...`|
|`0x060015c4`|`_Lambda$__R16-8`|`object, System.EventArgs [class:113]`|`void`|`2172184`|`4`|`ae3c8b8d99a39542...`|
|`0x060015c5`|`_Lambda$__16-7`|`()`|`void`|`2172200`|`4`|`ae3c8b8d99a39542...`|
|`0x060015c6`|`mWi8D3q09Ep7fKuND4Lt`|`()`|`bool`|`2172312`|`4`|`66ea1533cb3404e9...`|
|`0x060015c7`|`Ebs7qeq0KBS2qJXH8UYN`|`()`|`Kata_pro64_Cad2025.Form_Chon_Thep_Mong_Tu_Dong [class:692]`|`2172332`|`4`|`8969ce0cdaa4a19c...`|

### [VM-198] Methods of `Kata_pro64_Cad2025.view_mong_don` (Token `0x020000cf`, Displayed: 114/114, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06001bd4`|`.ctor`|`()`|`void`|`2767308`|`4`|`ae3c8b8d99a39542...`|
|`0x06001bd5`|`Dispose`|`bool`|`void`|`2767472`|`4`|`ae3c8b8d99a39542...`|
|`0x06001bd6`|`SEVO2GrVTD`|`()`|`void`|`2767880`|`4`|`ae3c8b8d99a39542...`|
|`0x06001bd7`|`F450h5Oc62a`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`2793904`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001bd8`|`ec30hSAXSkn`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`2793920`|`4`|`ae3c8b8d99a39542...`|
|`0x06001bd9`|`k7R0huRyZOy`|`()`|`System.Windows.Forms.BindingSource [class:237]`|`2794024`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001bda`|`zNn0hHpucM0`|`System.Windows.Forms.BindingSource [class:237]`|`void`|`2794040`|`4`|`ae3c8b8d99a39542...`|
|`0x06001bdb`|`D3t0naycSAi`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2794144`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001bdc`|`I9h0nzGTfnk`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2794160`|`4`|`ae3c8b8d99a39542...`|
|`0x06001bdd`|`zmu0nDe9Fvj`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2794264`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001bde`|`fIe0nBaGkR1`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2794280`|`4`|`ae3c8b8d99a39542...`|
|`0x06001bdf`|`BCbPo1akZ7b`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2794384`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001be0`|`IyRPoMSoDkO`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2794400`|`4`|`ae3c8b8d99a39542...`|
|`0x06001be1`|`MWd0fOEfVi9`|`()`|`System.Windows.Forms.Button [class:161]`|`2794504`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001be2`|`v6W0fVemltt`|`System.Windows.Forms.Button [class:161]`|`void`|`2794520`|`4`|`ae3c8b8d99a39542...`|
|`0x06001be3`|`qVo0rtB6PoL`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2794624`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001be4`|`XrB0rdNQ3hf`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2794640`|`4`|`ae3c8b8d99a39542...`|
|`0x06001be5`|`RB7Po9BOtWX`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2794744`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001be6`|`uILPoKXZR8T`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2794760`|`4`|`ae3c8b8d99a39542...`|
|`0x06001be7`|`rK70njeg2Z0`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2794864`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001be8`|`NWT0nx41Hb4`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2794880`|`4`|`ae3c8b8d99a39542...`|
|`0x06001be9`|`PHy0nOwhSS9`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2794984`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001bea`|`tIZ0nV77FuZ`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2795000`|`4`|`ae3c8b8d99a39542...`|
|`0x06001beb`|`lpa05UEybCZ`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2795104`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001bec`|`TOL058uHMr1`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2795120`|`4`|`ae3c8b8d99a39542...`|
|`0x06001bed`|`J8605EAXtbP`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2795224`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001bee`|`Roe050K8fGL`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2795240`|`4`|`ae3c8b8d99a39542...`|
|`0x06001bef`|`HWmPYDjfNHI`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2795344`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001bf0`|`xkqPYBaf24I`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2795360`|`4`|`ae3c8b8d99a39542...`|
|`0x06001bf1`|`xMkPoqyIWIA`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2795464`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001bf2`|`TIpPoA96NtN`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2795480`|`4`|`ae3c8b8d99a39542...`|
|`0x06001bf3`|`rox0HWK3cyv`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2795584`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001bf4`|`HKQ0HsTok5Y`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2795600`|`4`|`ae3c8b8d99a39542...`|
|`0x06001bf5`|`d9c0HcU5UcG`|`()`|`System.Windows.Forms.Label [class:245]`|`2795704`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001bf6`|`sJ10H6hqL71`|`System.Windows.Forms.Label [class:245]`|`void`|`2795720`|`4`|`ae3c8b8d99a39542...`|
|`0x06001bf7`|`VSTPoNrJdcB`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2795824`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001bf8`|`RSfPob5lBgb`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2795840`|`4`|`ae3c8b8d99a39542...`|
|`0x06001bf9`|`WJWPoIuosIt`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2795944`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001bfa`|`F88Poy1dDew`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2795960`|`4`|`ae3c8b8d99a39542...`|
|`0x06001bfb`|`MTS0fCBnujV`|`()`|`System.Windows.Forms.Button [class:161]`|`2796064`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001bfc`|`V1G0fmvURg0`|`System.Windows.Forms.Button [class:161]`|`void`|`2796080`|`4`|`ae3c8b8d99a39542...`|
|`0x06001bfd`|`GYC0HRybQkV`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2796184`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001bfe`|`P5U0HgruvJL`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2796200`|`4`|`ae3c8b8d99a39542...`|
|`0x06001bff`|`Wms0HGwf4Be`|`()`|`System.Windows.Forms.Label [class:245]`|`2796304`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c00`|`uga0HlA0a8q`|`System.Windows.Forms.Label [class:245]`|`void`|`2796320`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c01`|`Kdb0TCwb6SF`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`2796424`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c02`|`pwT0TmtLZra`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`2796440`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c03`|`fkI0WInERnw`|`()`|`System.Windows.Forms.Label [class:245]`|`2796544`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c04`|`ETa0WyDZkEG`|`System.Windows.Forms.Label [class:245]`|`void`|`2796560`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c05`|`sih0HhYVvoG`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2796664`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c06`|`YKk0HZB4IKZ`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2796680`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c07`|`iMT0HCk2k7B`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2796784`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c08`|`aUO0Hmg0N88`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2796800`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c09`|`zQd0cP2Kibt`|`()`|`System.Windows.Forms.Label [class:245]`|`2796904`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c0a`|`laU0c3QpRdF`|`System.Windows.Forms.Label [class:245]`|`void`|`2796920`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c0b`|`lk7PoRHBiTb`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2797024`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c0c`|`gVNPogXuW0t`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2797040`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c0d`|`mUxPohbMoOC`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2797144`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c0e`|`HsBPoZlRyx3`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2797160`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c0f`|`aBgPoexW2fS`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2797264`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c10`|`ASwPoLeOdaK`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2797280`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c11`|`W70PoTpqhJy`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2797384`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c12`|`u95PofimATO`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2797400`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c13`|`PAO0lCoNGGj`|`()`|`System.Windows.Forms.Label [class:245]`|`2797504`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c14`|`opi0lmIwfwt`|`System.Windows.Forms.Label [class:245]`|`void`|`2797520`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c15`|`xmt0ZtnsMK4`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`2797624`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c16`|`R7r0ZdxJUjg`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`2797640`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c17`|`LWDPoc8EYXf`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2797744`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c18`|`xBbPo6s2lQ9`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2797760`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c19`|`C3U0Gai6NxN`|`()`|`System.Windows.Forms.Label [class:245]`|`2797864`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c1a`|`riH0GzEujxb`|`System.Windows.Forms.Label [class:245]`|`void`|`2797880`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c1b`|`OqP0r5jWkbN`|`()`|`System.Windows.Forms.Label [class:245]`|`2797984`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c1c`|`O4n0rSRqtI4`|`System.Windows.Forms.Label [class:245]`|`void`|`2798000`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c1d`|`yb5PoWgdsNO`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2798104`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c1e`|`XgkPosOwBtN`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2798120`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c1f`|`wxpPotIYZtF`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2798224`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c20`|`sCNPod31Au6`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2798240`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c21`|`aQM0GFgWUOC`|`()`|`System.Windows.Forms.Label [class:245]`|`2798344`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c22`|`dRd0Gvyq4YN`|`System.Windows.Forms.Label [class:245]`|`void`|`2798360`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c23`|`KAW0lPWX6Sl`|`()`|`System.Windows.Forms.Label [class:245]`|`2798464`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c24`|`eK40l31Sv7M`|`System.Windows.Forms.Label [class:245]`|`void`|`2798480`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c25`|`bItPoJy30NQ`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2798584`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c26`|`HNYPorkiDUB`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2798600`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c27`|`LEw0tO3g9ED`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2798704`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c28`|`d9m0tV53Ym5`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2798720`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c29`|`nx70GjIDxlT`|`()`|`System.Windows.Forms.Label [class:245]`|`2798824`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c2a`|`VB90Gxmuo89`|`System.Windows.Forms.Label [class:245]`|`void`|`2798840`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c2b`|`CEB0HP6Bd1X`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2798944`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c2c`|`YOY0H3Kk0UY`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2798960`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c2d`|`CCA0ZowoUwH`|`()`|`System.Windows.Forms.Label [class:245]`|`2799064`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c2e`|`s3g0Z7VmfVf`|`System.Windows.Forms.Label [class:245]`|`void`|`2799080`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c2f`|`hIsPoGBTsob`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2799184`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c30`|`e4MPolvBOT4`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2799200`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c31`|`DpK0sc5MnnS`|`()`|`System.Windows.Forms.Label [class:245]`|`2799304`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c32`|`jRQ0s6mcPjx`|`System.Windows.Forms.Label [class:245]`|`void`|`2799320`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c33`|`Q5M0uR90ENj`|`()`|`System.Windows.Forms.Button [class:161]`|`2799424`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c34`|`cXH0ugi8VkT`|`System.Windows.Forms.Button [class:161]`|`void`|`2799440`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c35`|`JbJ0ucOnNCQ`|`()`|`System.Windows.Forms.Button [class:161]`|`2799544`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c36`|`KHo0u6YDmOV`|`System.Windows.Forms.Button [class:161]`|`void`|`2799560`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c37`|`HWt0fQrKvy4`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`2799664`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c38`|`tpi0fk0hWrQ`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`2799680`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c39`|`UFKPouMmoDu`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2799784`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c3a`|`warPoHOQQQh`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2799800`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c3b`|`juC0ujL9LJ8`|`()`|`System.Windows.Forms.Label [class:245]`|`2799904`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c3c`|`fEt0uxgJUfI`|`System.Windows.Forms.Label [class:245]`|`void`|`2799920`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c3d`|`L4I0HN70yWQ`|`()`|`System.Windows.Forms.CheckBox [class:257]`|`2800024`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c3e`|`wxR0Hbdsfdn`|`System.Windows.Forms.CheckBox [class:257]`|`void`|`2800040`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c3f`|`ePx0nimjiXy`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2800144`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c40`|`CFC0nns7vSZ`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2800160`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c41`|`WHq05qr9mP7`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2800264`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c42`|`zi605A11q5C`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2800280`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c43`|`.cctor`|`()`|`void`|`2800384`|`177`|`054a092eb3945315...`|
|`0x06001c44`|`lhcFFRqPH9mH53DCEeEL`|`()`|`bool`|`2800576`|`4`|`66ea1533cb3404e9...`|
|`0x06001c45`|`ldt9lrqPphO9Mficji0Q`|`()`|`Kata_pro64_Cad2025.view_mong_don [class:828]`|`2800596`|`4`|`8969ce0cdaa4a19c...`|

### [VM-199] Methods of `Kata_pro64_Cad2025.view_mong_don_new` (Token `0x020000d0`, Displayed: 209/209, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06001c46`|`.ctor`|`()`|`void`|`2800612`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c47`|`Dispose`|`bool`|`void`|`2801200`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c48`|`I0fVGn8gXM`|`()`|`void`|`2801568`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c49`|`F450h5Oc62a`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`2838148`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c4a`|`ec30hSAXSkn`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`2838164`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c4b`|`k7R0huRyZOy`|`()`|`System.Windows.Forms.BindingSource [class:237]`|`2838888`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c4c`|`zNn0hHpucM0`|`System.Windows.Forms.BindingSource [class:237]`|`void`|`2838904`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c4d`|`D3t0naycSAi`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2839008`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c4e`|`I9h0nzGTfnk`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2839024`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c4f`|`zmu0nDe9Fvj`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2839352`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c50`|`fIe0nBaGkR1`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2839368`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c51`|`BCbPo1akZ7b`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2839796`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c52`|`IyRPoMSoDkO`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2839812`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c53`|`gAAPopWYtVL`|`()`|`System.Windows.Forms.Button [class:161]`|`2840160`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c54`|`jAAPowe3xtI`|`System.Windows.Forms.Button [class:161]`|`void`|`2840176`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c55`|`qVo0rtB6PoL`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2840544`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c56`|`XrB0rdNQ3hf`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2840560`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c57`|`RB7Po9BOtWX`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2840664`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c58`|`uILPoKXZR8T`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2840680`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c59`|`rK70njeg2Z0`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2841048`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c5a`|`NWT0nx41Hb4`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2841064`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c5b`|`PHy0nOwhSS9`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2841472`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c5c`|`tIZ0nV77FuZ`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2841488`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c5d`|`lpa05UEybCZ`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2841856`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c5e`|`TOL058uHMr1`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2841872`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c5f`|`J8605EAXtbP`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2842240`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c60`|`Roe050K8fGL`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2842256`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c61`|`HWmPYDjfNHI`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2842664`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c62`|`xkqPYBaf24I`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2842680`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c63`|`xMkPoqyIWIA`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2843088`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c64`|`TIpPoA96NtN`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2843104`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c65`|`rox0HWK3cyv`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2843556`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c66`|`HKQ0HsTok5Y`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2843572`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c67`|`Sy0Po2ZKiwa`|`()`|`System.Windows.Forms.Label [class:245]`|`2843940`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c68`|`VADPoXU6rqE`|`System.Windows.Forms.Label [class:245]`|`void`|`2843956`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c69`|`VSTPoNrJdcB`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2844060`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c6a`|`RSfPob5lBgb`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2844076`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c6b`|`WJWPoIuosIt`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2844444`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c6c`|`F88Poy1dDew`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2844460`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c6d`|`ehJPoif8D20`|`()`|`System.Windows.Forms.Button [class:161]`|`2844804`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c6e`|`TrUPonIUCVS`|`System.Windows.Forms.Button [class:161]`|`void`|`2844820`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c6f`|`GYC0HRybQkV`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2845228`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c70`|`P5U0HgruvJL`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2845244`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c71`|`CPiPo59jwVp`|`()`|`System.Windows.Forms.Label [class:245]`|`2845612`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c72`|`O7sPoS7Dq1u`|`System.Windows.Forms.Label [class:245]`|`void`|`2845628`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c73`|`X46Pojtt70e`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`2845732`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c74`|`fm7PoxjlBYG`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`2845748`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c75`|`AWoPoDTYfUt`|`()`|`System.Windows.Forms.Label [class:245]`|`2845852`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c76`|`LmpPoB9ymFg`|`System.Windows.Forms.Label [class:245]`|`void`|`2845868`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c77`|`sih0HhYVvoG`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2845972`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c78`|`YKk0HZB4IKZ`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2845988`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c79`|`iMT0HCk2k7B`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2846436`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c7a`|`aUO0Hmg0N88`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2846452`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c7b`|`kaWPoOKsLks`|`()`|`System.Windows.Forms.Label [class:245]`|`2846820`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c7c`|`Ig3PoVu4yP3`|`System.Windows.Forms.Label [class:245]`|`void`|`2846836`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c7d`|`lk7PoRHBiTb`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2846940`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c7e`|`gVNPogXuW0t`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2846956`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c7f`|`mUxPohbMoOC`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2847324`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c80`|`HsBPoZlRyx3`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2847340`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c81`|`aBgPoexW2fS`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2847724`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c82`|`ASwPoLeOdaK`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2847740`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c83`|`W70PoTpqhJy`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2848108`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c84`|`u95PofimATO`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2848124`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c85`|`OKF0OtLZa2n`|`()`|`System.Windows.Forms.Label [class:245]`|`2848492`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c86`|`Q9y0Ode1DQG`|`System.Windows.Forms.Label [class:245]`|`void`|`2848508`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c87`|`g1VPoaLZebP`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`2848612`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c88`|`yeWPozIbvMT`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`2848628`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c89`|`LWDPoc8EYXf`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2848732`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c8a`|`xBbPo6s2lQ9`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2848748`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c8b`|`lMWP7UmtBpc`|`()`|`System.Windows.Forms.Label [class:245]`|`2849156`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c8c`|`ERBP78x5lpR`|`System.Windows.Forms.Label [class:245]`|`void`|`2849172`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c8d`|`YY7P7EWFNYk`|`()`|`System.Windows.Forms.Label [class:245]`|`2849276`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c8e`|`RTFP70Yq9eX`|`System.Windows.Forms.Label [class:245]`|`void`|`2849292`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c8f`|`yb5PoWgdsNO`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2849396`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c90`|`XgkPosOwBtN`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2849412`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c91`|`wxpPotIYZtF`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2849800`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c92`|`sCNPod31Au6`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2849816`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c93`|`RPPP7PdnuRQ`|`()`|`System.Windows.Forms.Label [class:245]`|`2850184`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c94`|`jLcP73bRCdY`|`System.Windows.Forms.Label [class:245]`|`void`|`2850200`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c95`|`TaKP7qKIQhI`|`()`|`System.Windows.Forms.Label [class:245]`|`2850304`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c96`|`pZiP7A2Yd6C`|`System.Windows.Forms.Label [class:245]`|`void`|`2850320`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c97`|`bItPoJy30NQ`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2850424`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c98`|`HNYPorkiDUB`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2850440`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c99`|`LEw0tO3g9ED`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2850768`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c9a`|`d9m0tV53Ym5`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2850784`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c9b`|`BXF0taQIjo6`|`()`|`System.Windows.Forms.Label [class:245]`|`2851152`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c9c`|`MFT0tzs0uYs`|`System.Windows.Forms.Label [class:245]`|`void`|`2851168`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c9d`|`CEB0HP6Bd1X`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2851272`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001c9e`|`YOY0H3Kk0UY`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2851288`|`4`|`ae3c8b8d99a39542...`|
|`0x06001c9f`|`tTMP7CVjYDu`|`()`|`System.Windows.Forms.Label [class:245]`|`2851676`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001ca0`|`iGPP7mQb5rE`|`System.Windows.Forms.Label [class:245]`|`void`|`2851692`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ca1`|`hIsPoGBTsob`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2851796`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001ca2`|`e4MPolvBOT4`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2851812`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ca3`|`qPiP7QdDI6M`|`()`|`System.Windows.Forms.Label [class:245]`|`2852160`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001ca4`|`wHtP7kE7elb`|`System.Windows.Forms.Label [class:245]`|`void`|`2852176`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ca5`|`tKi0OCSj9EU`|`()`|`System.Windows.Forms.Button [class:161]`|`2852280`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001ca6`|`i5N0OmwKxC4`|`System.Windows.Forms.Button [class:161]`|`void`|`2852296`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ca7`|`oGqP74AiSGR`|`()`|`System.Windows.Forms.Button [class:161]`|`2852600`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001ca8`|`O8yP7Y90Ucp`|`System.Windows.Forms.Button [class:161]`|`void`|`2852616`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ca9`|`fUo0saMpnbr`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`2853004`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001caa`|`Gr10szj6IhN`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`2853020`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cab`|`loQ02ciph5x`|`()`|`System.Windows.Forms.CheckBox [class:257]`|`2853124`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001cac`|`L1X026De8Q0`|`System.Windows.Forms.CheckBox [class:257]`|`void`|`2853140`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cad`|`ePx0nimjiXy`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2853548`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001cae`|`CFC0nns7vSZ`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2853564`|`4`|`ae3c8b8d99a39542...`|
|`0x06001caf`|`WHq05qr9mP7`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2853932`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001cb0`|`zi605A11q5C`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2853948`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cb1`|`x2SP7o6rtIP`|`()`|`System.Windows.Forms.CheckBox [class:257]`|`2854296`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001cb2`|`GRcP77o5TqQ`|`System.Windows.Forms.CheckBox [class:257]`|`void`|`2854312`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cb3`|`UxiP7Fu3lpI`|`()`|`System.Windows.Forms.CheckBox [class:257]`|`2854680`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001cb4`|`WUeP7vxc2r5`|`System.Windows.Forms.CheckBox [class:257]`|`void`|`2854696`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cb5`|`k2YP71YKe8U`|`()`|`System.Windows.Forms.RadioButton [class:253]`|`2855064`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001cb6`|`vAYP7M2QGQI`|`System.Windows.Forms.RadioButton [class:253]`|`void`|`2855080`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cb7`|`LF1P79roko6`|`()`|`System.Windows.Forms.RadioButton [class:253]`|`2855184`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001cb8`|`oY9P7KVoX88`|`System.Windows.Forms.RadioButton [class:253]`|`void`|`2855200`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cb9`|`jLaP7N1geWW`|`()`|`System.Windows.Forms.Label [class:245]`|`2855548`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001cba`|`kXQP7by7PeZ`|`System.Windows.Forms.Label [class:245]`|`void`|`2855564`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cbb`|`TMGP7I9fF7R`|`()`|`System.Windows.Forms.Panel [class:373]`|`2855668`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001cbc`|`WxFP7yRBHvU`|`System.Windows.Forms.Panel [class:373]`|`void`|`2855684`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cbd`|`UFX0HtrixJZ`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2855788`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001cbe`|`TRh0Hd8yj8F`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2855804`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cbf`|`BYqP7RUAvUT`|`()`|`System.Windows.Forms.Label [class:245]`|`2856192`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001cc0`|`mmJP7gy6njp`|`System.Windows.Forms.Label [class:245]`|`void`|`2856208`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cc1`|`ISbP7h7m0wB`|`()`|`System.Windows.Forms.Button [class:161]`|`2856312`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001cc2`|`cFCP7ZuAYAG`|`System.Windows.Forms.Button [class:161]`|`void`|`2856328`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cc3`|`IbqP7eY12ul`|`()`|`System.Windows.Forms.Label [class:245]`|`2856736`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001cc4`|`X72P7LC6eVn`|`System.Windows.Forms.Label [class:245]`|`void`|`2856752`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cc5`|`pc10HTj4ETU`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2856856`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001cc6`|`bA10HfgKOmh`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2856872`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cc7`|`AHBP7TrGonw`|`()`|`System.Windows.Forms.Label [class:245]`|`2857176`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001cc8`|`jrPP7fZqBas`|`System.Windows.Forms.Label [class:245]`|`void`|`2857192`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cc9`|`ogU0uugQH87`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2857296`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001cca`|`ElM0uHFOikF`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2857312`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ccb`|`tmG0upiqCqE`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2857720`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001ccc`|`L6F0uwmSLNN`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2857736`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ccd`|`i040u2xceMS`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2857840`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001cce`|`Ogl0uXuoIWr`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2857856`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ccf`|`A380u5O3yEm`|`()`|`System.Windows.Forms.ComboBox [class:377]`|`2858264`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001cd0`|`Df90uS5em1u`|`System.Windows.Forms.ComboBox [class:377]`|`void`|`2858280`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cd1`|`HkkP7cEyxgo`|`()`|`System.Windows.Forms.Label [class:245]`|`2858792`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001cd2`|`dSLP76GlPpq`|`System.Windows.Forms.Label [class:245]`|`void`|`2858808`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cd3`|`Vdh02CqO8we`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2858912`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001cd4`|`kio02mGBuNx`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2858928`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cd5`|`XOj024LTmIB`|`()`|`System.Windows.Forms.Label [class:245]`|`2859032`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001cd6`|`w0i02YNUof3`|`System.Windows.Forms.Label [class:245]`|`void`|`2859048`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cd7`|`Sci02F4vCOE`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2859152`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001cd8`|`K0Q02vsN1Pg`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2859168`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cd9`|`Iq9029NfEwy`|`()`|`System.Windows.Forms.Label [class:245]`|`2859272`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001cda`|`vjk02Kkpi3k`|`System.Windows.Forms.Label [class:245]`|`void`|`2859288`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cdb`|`oM302IBCtL5`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2859392`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001cdc`|`LiA02yf5FBg`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2859408`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cdd`|`H1v02hExSvY`|`()`|`System.Windows.Forms.Label [class:245]`|`2859512`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001cde`|`H5u02ZbxqMu`|`System.Windows.Forms.Label [class:245]`|`void`|`2859528`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cdf`|`eAR02TNfWNI`|`()`|`System.Windows.Forms.TextBox [class:249]`|`2859632`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001ce0`|`k4G02fEKZtX`|`System.Windows.Forms.TextBox [class:249]`|`void`|`2859648`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ce1`|`RqJ02W2JG2e`|`()`|`System.Windows.Forms.Label [class:245]`|`2859752`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001ce2`|`UoW02sInmLv`|`System.Windows.Forms.Label [class:245]`|`void`|`2859768`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ce3`|`NFdVlD6Pgk`|`object, System.EventArgs [class:113]`|`void`|`2859872`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ce4`|`EHkVuY2JA8`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`2863852`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ce5`|`wKNVHw1TiF`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`2864336`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ce6`|`B82VpetBQa`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`2864600`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ce7`|`UvDVwMZnyM`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`2865016`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ce8`|`doi_location`|`System.Drawing.Point [valuetype:173], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], r8`|`void`|`2865176`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ce9`|`ve_hinh_mong_don`|`()`|`void`|`2866296`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cea`|`xvRV2m6tq8`|`object, System.EventArgs [class:113]`|`void`|`2873508`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ceb`|`TZPVXSH36O`|`object, System.EventArgs [class:113]`|`void`|`2873612`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cec`|`rueViYmVoS`|`object, System.EventArgs [class:113]`|`void`|`2873940`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ced`|`n9YVnXttmB`|`object, System.EventArgs [class:113]`|`void`|`2874044`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cee`|`bnuV5N7jf1`|`object, System.EventArgs [class:113]`|`void`|`2874336`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cef`|`e7RVS7NnZj`|`object, System.EventArgs [class:113]`|`void`|`2874644`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cf0`|`ODoVjo0fO6`|`object, System.EventArgs [class:113]`|`void`|`2874748`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cf1`|`tnOVxPqgtf`|`object, System.EventArgs [class:113]`|`void`|`2875076`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cf2`|`Du7VD7qbwj`|`object, System.EventArgs [class:113]`|`void`|`2875180`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cf3`|`sFAVB5H0KI`|`object, System.EventArgs [class:113]`|`void`|`2875284`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cf4`|`ts2VO3GPH6`|`object, System.EventArgs [class:113]`|`void`|`2875388`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cf5`|`n4uVVHMpEQ`|`object, System.EventArgs [class:113]`|`void`|`2875492`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cf6`|`SWoVa5fpah`|`object, System.EventArgs [class:113]`|`void`|`2875596`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cf7`|`HE0VzYvDm3`|`object, System.EventArgs [class:113]`|`void`|`2875700`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cf8`|`qnfaUwElCS`|`object, System.EventArgs [class:113]`|`void`|`2875804`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cf9`|`T9Ta8ZZ9so`|`object, System.EventArgs [class:113]`|`void`|`2875908`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cfa`|`NohaED5RZj`|`object, System.EventArgs [class:113]`|`void`|`2876012`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cfb`|`zDxa0gQpjq`|`object, System.EventArgs [class:113]`|`void`|`2876156`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cfc`|`rwiaPDLesy`|`object, System.EventArgs [class:113]`|`void`|`2876260`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cfd`|`C1Ua3u2CmL`|`object, System.EventArgs [class:113]`|`void`|`2876364`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cfe`|`lUKaqx0DSj`|`object, System.EventArgs [class:113]`|`void`|`2876468`|`4`|`ae3c8b8d99a39542...`|
|`0x06001cff`|`breaA7h4oj`|`object, System.EventArgs [class:113]`|`void`|`2876572`|`4`|`ae3c8b8d99a39542...`|
|`0x06001d00`|`pj7aCEtJB3`|`object, System.EventArgs [class:113]`|`void`|`2876676`|`4`|`ae3c8b8d99a39542...`|
|`0x06001d01`|`kXYamlbN7x`|`object, System.EventArgs [class:113]`|`void`|`2876780`|`4`|`ae3c8b8d99a39542...`|
|`0x06001d02`|`wsTaQBEmK0`|`object, System.EventArgs [class:113]`|`void`|`2876884`|`4`|`ae3c8b8d99a39542...`|
|`0x06001d03`|`lwmakCD5j8`|`object, System.EventArgs [class:113]`|`void`|`2876988`|`4`|`ae3c8b8d99a39542...`|
|`0x06001d04`|`IPwa47KGYA`|`object, System.EventArgs [class:113]`|`void`|`2877092`|`4`|`ae3c8b8d99a39542...`|
|`0x06001d05`|`XusaYECmPy`|`object, System.EventArgs [class:113]`|`void`|`2877196`|`4`|`ae3c8b8d99a39542...`|
|`0x06001d06`|`NBNaoOH0Aa`|`object, System.EventArgs [class:113]`|`void`|`2877300`|`4`|`ae3c8b8d99a39542...`|
|`0x06001d07`|`YuDa7CKsA5`|`object, System.EventArgs [class:113]`|`void`|`2877404`|`4`|`ae3c8b8d99a39542...`|
|`0x06001d08`|`g5yaFE8Mvt`|`object, System.EventArgs [class:113]`|`void`|`2877508`|`4`|`ae3c8b8d99a39542...`|
|`0x06001d09`|`EOCav4nXVx`|`object, System.EventArgs [class:113]`|`void`|`2877612`|`4`|`ae3c8b8d99a39542...`|
|`0x06001d0a`|`NYxa15RNI2`|`object, System.EventArgs [class:113]`|`void`|`2877784`|`4`|`ae3c8b8d99a39542...`|
|`0x06001d0b`|`HSNaM1t0eJ`|`()`|`void`|`2877928`|`4`|`ae3c8b8d99a39542...`|
|`0x06001d0c`|`EH7a9Z6Ugb`|`object, System.Windows.Forms.FormClosingEventArgs [class:301]`|`void`|`2878236`|`4`|`ae3c8b8d99a39542...`|
|`0x06001d0d`|`xK4aKtESh8`|`object, System.EventArgs [class:113]`|`void`|`2878424`|`4`|`ae3c8b8d99a39542...`|
|`0x06001d0e`|`setDefault`|`()`|`void`|`2878576`|`4`|`ae3c8b8d99a39542...`|
|`0x06001d0f`|`set_do_not_reload`|`bool`|`void`|`2878732`|`4`|`ae3c8b8d99a39542...`|
|`0x06001d10`|`doi_du_lieu`|`bool`|`void`|`2878836`|`4`|`ae3c8b8d99a39542...`|
|`0x06001d11`|`UGaaNcsMe5`|`object, System.EventArgs [class:113]`|`void`|`2879424`|`4`|`ae3c8b8d99a39542...`|
|`0x06001d12`|`ROuabLhlGZ`|`object, System.EventArgs [class:113]`|`void`|`2884556`|`4`|`ae3c8b8d99a39542...`|
|`0x06001d13`|`hlSaIU8HCJ`|`object, System.EventArgs [class:113]`|`void`|`2885488`|`4`|`ae3c8b8d99a39542...`|
|`0x06001d14`|`.cctor`|`()`|`void`|`2885592`|`177`|`b5507e12928d078a...`|
|`0x06001d15`|`SJ0XVWqP2AMbjp5X1nTx`|`()`|`bool`|`2885784`|`4`|`66ea1533cb3404e9...`|
|`0x06001d16`|`CcUfctqPXsYtYJe5cP7J`|`()`|`Kata_pro64_Cad2025.view_mong_don_new [class:832]`|`2885804`|`4`|`8969ce0cdaa4a19c...`|

### [VM-200] Methods of `Kata_pro64_Cad2025.Acad / Class28 ref` (Token `0x020000d5`, Displayed: 5/5, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06001f88`|`ten_file_goi_lenh`|`()`|`string`|`3185172`|`4`|`8969ce0cdaa4a19c...`|
|`0x06001f89`|`ACADLink`|`()`|`bool`|`3185792`|`4`|`66ea1533cb3404e9...`|
|`0x06001f8a`|`.cctor`|`()`|`void`|`3190328`|`177`|`427b2971881e1663...`|
|`0x06001f8b`|`i9NLRhqPa1tTOQpocQGn`|`()`|`bool`|`3190520`|`4`|`66ea1533cb3404e9...`|
|`0x06001f8c`|`G37IetqPz4ps8T5WOfdj`|`()`|`Kata_pro64_Cad2025.Acad [class:852]`|`3190540`|`4`|`8969ce0cdaa4a19c...`|

### [VM-201] Methods of `Kata_pro64_Cad2025.Lenh_thep` (Token `0x020000d6`, Displayed: 166/166, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06001f8d`|`.cctor`|`()`|`void`|`3190556`|`803`|`fd89cfadd9594a85...`|
|`0x06001f8e`|`.ctor`|`()`|`void`|`3191372`|`4`|`ae3c8b8d99a39542...`|
|`0x06001f8f`|`bat_dau`|`()`|`void`|`3191496`|`4`|`ae3c8b8d99a39542...`|
|`0x06001f90`|`Date_create_file`|`string`|`i4`|`3192580`|`4`|`78d4d2062aa5373e...`|
|`0x06001f91`|`ket_thuc`|`()`|`void`|`3193028`|`4`|`ae3c8b8d99a39542...`|
|`0x06001f92`|`Ve_thep` [CommandMethod("ve_thep")]|`()`|`void`|`3193548`|`4`|`ae3c8b8d99a39542...`|
|`0x06001f93`|`Tao_thep` [CommandMethod("taothep")]|`()`|`void`|`3193884`|`4`|`ae3c8b8d99a39542...`|
|`0x06001f94`|`Create_link` [CommandMethod("tao_lien_ket")]|`()`|`void`|`3194092`|`4`|`ae3c8b8d99a39542...`|
|`0x06001f95`|`Edit_link1` [CommandMethod("sua_thep")]|`()`|`void`|`3194340`|`4`|`ae3c8b8d99a39542...`|
|`0x06001f96`|`sua_moc_dai` [CommandMethod("sua_moc_dai")]|`()`|`void`|`3194572`|`4`|`ae3c8b8d99a39542...`|
|`0x06001f97`|`Chuyen_dai_vong_moc_90` [CommandMethod("dai90")]|`()`|`void`|`3195012`|`4`|`ae3c8b8d99a39542...`|
|`0x06001f98`|`Chuyen_dai_vong_moc_180` [CommandMethod("dai180")]|`()`|`void`|`3195028`|`4`|`ae3c8b8d99a39542...`|
|`0x06001f99`|`Chuyen_dai_vong_moc_135` [CommandMethod("dai135")]|`()`|`void`|`3195048`|`4`|`ae3c8b8d99a39542...`|
|`0x06001f9a`|`Chuyen_dai_vong_moc_90135` [CommandMethod("dai90135")]|`()`|`void`|`3195068`|`4`|`ae3c8b8d99a39542...`|
|`0x06001f9b`|`Nhan_thep1` [CommandMethod("nhan_thep")]|`()`|`void`|`3195088`|`4`|`ae3c8b8d99a39542...`|
|`0x06001f9c`|`Neo_thep1` [CommandMethod("neo_thep")]|`()`|`void`|`3195296`|`4`|`ae3c8b8d99a39542...`|
|`0x06001f9d`|`Update_link` [CommandMethod("update_thep")]|`()`|`void`|`3195504`|`4`|`ae3c8b8d99a39542...`|
|`0x06001f9e`|`Update_mong` [CommandMethod("udmong")]|`()`|`void`|`3195732`|`4`|`ae3c8b8d99a39542...`|
|`0x06001f9f`|`Update_cong` [CommandMethod("udcong")]|`()`|`void`|`3195748`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fa0`|`PhaMong1` [CommandMethod("phamong")]|`()`|`void`|`3195764`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fa1`|`Copy_link` [CommandMethod("copy_tao_lien_ket")]|`()`|`void`|`3195780`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fa2`|`Delete_link` [CommandMethod("xoa_lien_ket")]|`()`|`void`|`3196028`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fa3`|`Find_link` [CommandMethod("xem_lien_ket")]|`()`|`void`|`3196256`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fa4`|`Clear_highlight` [CommandMethod("xoa_highlight")]|`()`|`void`|`3196272`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fa5`|`update_Block_ColumnWall` [CommandMethod("Update_Block_ColumnWall")]|`()`|`void`|`3196288`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fa6`|`Ghi_thep` [CommandMethod("ghi_thep")]|`()`|`void`|`3196304`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fa7`|`Chon_cat_thep1` [CommandMethod("cat_thep")]|`()`|`void`|`3196512`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fa8`|`Cat_tuy_y1` [CommandMethod("cat_tuy_y")]|`()`|`void`|`3196724`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fa9`|`Noi_thep1` [CommandMethod("noi_thep")]|`()`|`void`|`3196956`|`4`|`ae3c8b8d99a39542...`|
|`0x06001faa`|`Tao_tieu_de1` [CommandMethod("tao_tieu_de")]|`()`|`void`|`3196972`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fab`|`Doi_mau_thep1` [CommandMethod("doi_mau_thep")]|`()`|`void`|`3196988`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fac`|`Timthep1` [CommandMethod("timthep")]|`()`|`void`|`3197004`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fad`|`Dimthep` [CommandMethod("dimthep")]|`()`|`void`|`3197300`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fae`|`RoundDim1` [CommandMethod("rounddim")]|`()`|`void`|`3197528`|`4`|`ae3c8b8d99a39542...`|
|`0x06001faf`|`Gan_stt0` [CommandMethod("stt0")]|`()`|`void`|`3197736`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fb0`|`Gan_stt1` [CommandMethod("stt1")]|`()`|`void`|`3197944`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fb1`|`Setupthep_cot` [CommandMethod("setupthep")]|`()`|`void`|`3198212`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fb2`|`To_hop_thep1` [CommandMethod("to_hop_thep")]|`()`|`void`|`3198664`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fb3`|`Thong_ke` [CommandMethod("thong_ke_thep")]|`()`|`void`|`3198872`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fb4`|`Suatk` [CommandMethod("sua_thong_ke")]|`()`|`void`|`3199104`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fb5`|`sosanh_tk` [CommandMethod("sstk")]|`()`|`void`|`3199120`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fb6`|`Ththep` [CommandMethod("tong_hop_thep")]|`()`|`void`|`3199136`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fb7`|`Tkcoupler` [CommandMethod("thong_ke_coupler")]|`()`|`void`|`3199152`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fb8`|`Thong_ke_san` [CommandMethod("thong_ke_block_san")]|`()`|`void`|`3199168`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fb9`|`Cadtoexcel` [CommandMethod("toexcel")]|`()`|`void`|`3199436`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fba`|`Cadtoexcelqh` [CommandMethod("toexcelqh")]|`()`|`void`|`3199456`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fbb`|`Chuyen_block_thep_san_sang_theplk1` [CommandMethod("chuyen")]|`()`|`void`|`3199592`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fbc`|`show_thep_rai1` [CommandMethod("show_thep_rai")]|`()`|`void`|`3199800`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fbd`|`check_lap` [CommandMethod("check_lap")]|`()`|`void`|`3199816`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fbe`|`Xoa_dictionaries` [CommandMethod("pukata")]|`()`|`void`|`3199832`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fbf`|`Load_beam` [CommandMethod("loadbeam")]|`()`|`void`|`3200936`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fc0`|`Load_column` [CommandMethod("loadcolumn")]|`()`|`void`|`3201280`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fc1`|`Load_wall` [CommandMethod("loadwall")]|`()`|`void`|`3201596`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fc2`|`QS_mbkc` [CommandMethod("QS_mbkc")]|`()`|`void`|`3201880`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fc3`|`Ve_hm_tru` [CommandMethod("QS_tru")]|`()`|`void`|`3202124`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fc4`|`Trien_khai_QS` [CommandMethod("QS")]|`()`|`void`|`3202676`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fc5`|`XuatQS` [CommandMethod("xuatQS")]|`()`|`void`|`3202932`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fc6`|`Tim_QS1` [CommandMethod("timqs")]|`()`|`void`|`3202948`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fc7`|`Tao_danh_muc_ban_ve1` [CommandMethod("danhmuc")]|`()`|`void`|`3203204`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fc8`|`cat_doituong1` [CommandMethod("cat_doituong")]|`()`|`void`|`3203432`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fc9`|`Taomv` [CommandMethod("taomv")]|`()`|`void`|`3203448`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fca`|`Taomv1` [CommandMethod("taomv1")]|`()`|`void`|`3203656`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fcb`|`Tao_khung_ten1` [CommandMethod("tao_khung_ten")]|`()`|`void`|`3203888`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fcc`|`Sap_ban_ve1` [CommandMethod("sap_ban_ve")]|`()`|`void`|`3203904`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fcd`|`Tao_khung1` [CommandMethod("tao_khung")]|`()`|`void`|`3203920`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fce`|`Tao_nhom1` [CommandMethod("tao_nhom")]|`()`|`void`|`3204024`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fcf`|`Gop_khung1` [CommandMethod("gop_khung")]|`()`|`void`|`3204040`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fd0`|`In_tem_thep1` [CommandMethod("in_tem_thep")]|`()`|`void`|`3204056`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fd1`|`Chia_tam1` [CommandMethod("chia_tam")]|`()`|`void`|`3204072`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fd2`|`Op_gach1` [CommandMethod("op_gach")]|`()`|`void`|`3204088`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fd3`|`Loc_dim1` [CommandMethod("loc_dim")]|`()`|`void`|`3204104`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fd4`|`Phan_vung_noi1` [CommandMethod("phan_vung_noi")]|`()`|`void`|`3204120`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fd5`|`Ve_mong_new` [CommandMethod("ve_mong_don")]|`()`|`void`|`3204136`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fd6`|`Ve_mong_coc1` [CommandMethod("ve_mong_coc")]|`()`|`void`|`3204660`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fd7`|`Vecot` [CommandMethod("ve_cot")]|`()`|`void`|`3204952`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fd8`|`Vecot_shop` [CommandMethod("ve_cot_shop")]|`()`|`void`|`3205096`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fd9`|`Vevach` [CommandMethod("ve_vach")]|`()`|`void`|`3205240`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fda`|`Vevach_shop` [CommandMethod("ve_vach_shop")]|`()`|`void`|`3205384`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fdb`|`Ve_loi_vach1` [CommandMethod("ve_loi_vach")]|`()`|`void`|`3205528`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fdc`|`Vedam` [CommandMethod("ve_dam")]|`()`|`void`|`3205760`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fdd`|`Vedam_shop` [CommandMethod("ve_dam_shop")]|`()`|`void`|`3205932`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fde`|`Vedammong` [CommandMethod("ve_dam_mong")]|`()`|`void`|`3206128`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fdf`|`Vegiangmong` [CommandMethod("ve_giang_mong")]|`()`|`void`|`3206324`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fe0`|`Vegiangmong_shop` [CommandMethod("ve_giang_shop")]|`()`|`void`|`3206496`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fe1`|`Ve_dam_mc21` [CommandMethod("ve_dam_mc")]|`()`|`void`|`3206668`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fe2`|`Ve_dam_giao1` [CommandMethod("dam_giao")]|`()`|`void`|`3206684`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fe3`|`Ve_dam_xien1` [CommandMethod("ve_dam_xien")]|`()`|`void`|`3206916`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fe4`|`Vedamcong` [CommandMethod("ve_dam_cong")]|`()`|`void`|`3207148`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fe5`|`Ve_san` [CommandMethod("ve_san")]|`()`|`void`|`3207356`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fe6`|`Ve_san_polyline` [CommandMethod("vesanpl")]|`()`|`void`|`3207952`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fe7`|`Ve_san_4d` [CommandMethod("vesan4d")]|`()`|`void`|`3208508`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fe8`|`tao_MC_cau_kien1` [CommandMethod("vemc")]|`()`|`void`|`3209084`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fe9`|`Ve_thang_new` [CommandMethod("ve_thang")]|`()`|`void`|`3209336`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fea`|`Ve_lanh_to1` [CommandMethod("velanhto")]|`()`|`void`|`3209976`|`4`|`ae3c8b8d99a39542...`|
|`0x06001feb`|`Ve_grid` [CommandMethod("ve_grid")]|`()`|`void`|`3210568`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fec`|`Rai_block_cot1` [CommandMethod("raicot")]|`()`|`void`|`3210980`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fed`|`dim_block_cot1` [CommandMethod("dimcot")]|`()`|`void`|`3211392`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fee`|`Rai_block_dam1` [CommandMethod("raidam")]|`()`|`void`|`3211408`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fef`|`Trien_khai_day_san` [CommandMethod("pick_diem_san")]|`()`|`void`|`3211840`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ff0`|`Khai_thong_tin_vach` [CommandMethod("svach")]|`()`|`void`|`3211996`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ff1`|`Khai_thong_tin_san` [CommandMethod("ssan")]|`()`|`void`|`3212012`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ff2`|`Khai_thong_tin_coc` [CommandMethod("scoc")]|`()`|`void`|`3212028`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ff3`|`Khai_thong_tin_dam_cong` [CommandMethod("scong")]|`()`|`void`|`3212044`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ff4`|`Xu_ly_giao_dam` [CommandMethod("giao_dam")]|`()`|`void`|`3212060`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ff5`|`Xu_ly_cat_dam` [CommandMethod("cat_dam")]|`()`|`void`|`3212076`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ff6`|`Hide_att1` [CommandMethod("hideatt")]|`()`|`void`|`3212092`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ff7`|`Hide_ktd1` [CommandMethod("hidektd")]|`()`|`void`|`3212108`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ff8`|`UnHide_att1` [CommandMethod("Unhideatt")]|`()`|`void`|`3212124`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ff9`|`Tao_table_tk_cau_kien_block_beam1` [CommandMethod("tkdam")]|`()`|`void`|`3212140`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ffa`|`Tao_table_tk_cau_kien_block_ColumnWall1` [CommandMethod("tkcot")]|`()`|`void`|`3212156`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ffb`|`tim_ten_ck` [CommandMethod("tim_ten_ck")]|`()`|`void`|`3212172`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ffc`|`Danh_stt` [CommandMethod("dten")]|`()`|`void`|`3212188`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ffd`|`Tao_table_tk_cau_kien1` [CommandMethod("tkten")]|`()`|`void`|`3212760`|`4`|`ae3c8b8d99a39542...`|
|`0x06001ffe`|`Texttoblock1` [CommandMethod("textblock")]|`()`|`void`|`3213312`|`4`|`ae3c8b8d99a39542...`|
|`0x06001fff`|`Chia_san1` [CommandMethod("chia_san")]|`()`|`void`|`3213328`|`4`|`ae3c8b8d99a39542...`|
|`0x06002000`|`RM1` [CommandMethod("rai_mong")]|`()`|`void`|`3213520`|`4`|`ae3c8b8d99a39542...`|
|`0x06002001`|`quet_dam` [CommandMethod("quet_dam")]|`()`|`void`|`3213536`|`4`|`ae3c8b8d99a39542...`|
|`0x06002002`|`quet_dam_all` [CommandMethod("quet_dam_all")]|`()`|`void`|`3213552`|`4`|`ae3c8b8d99a39542...`|
|`0x06002003`|`Save_thep_dam` [CommandMethod("Save_thep_dam")]|`()`|`void`|`3213568`|`4`|`ae3c8b8d99a39542...`|
|`0x06002004`|`Save_thep_dam_all` [CommandMethod("Save_thep_dam_all")]|`()`|`void`|`3213584`|`4`|`ae3c8b8d99a39542...`|
|`0x06002005`|`convert_table_ThepDamAllMBKC` [CommandMethod("convert_table")]|`()`|`void`|`3213600`|`4`|`ae3c8b8d99a39542...`|
|`0x06002006`|`Add_thep_dam` [CommandMethod("Add_thep_dam")]|`()`|`void`|`3213616`|`4`|`ae3c8b8d99a39542...`|
|`0x06002007`|`show_thep_dam` [CommandMethod("show_thep_dam")]|`()`|`void`|`3213632`|`4`|`ae3c8b8d99a39542...`|
|`0x06002008`|`VedamMC_all` [CommandMethod("ve_damMC_all")]|`()`|`void`|`3213648`|`4`|`ae3c8b8d99a39542...`|
|`0x06002009`|`Vedam_all` [CommandMethod("ve_dam_all")]|`()`|`void`|`3213792`|`4`|`ae3c8b8d99a39542...`|
|`0x0600200a`|`Vedam_all_shop` [CommandMethod("ve_dam_all_shop")]|`()`|`void`|`3213936`|`4`|`ae3c8b8d99a39542...`|
|`0x0600200b`|`Etabs_Model` [CommandMethod("Etabs")]|`()`|`void`|`3214080`|`4`|`ae3c8b8d99a39542...`|
|`0x0600200c`|`Etabs_GanTai` [CommandMethod("GanTai")]|`()`|`void`|`3214096`|`4`|`ae3c8b8d99a39542...`|
|`0x0600200d`|`Etabs_RaiTai` [CommandMethod("HoatTai")]|`()`|`void`|`3214596`|`4`|`ae3c8b8d99a39542...`|
|`0x0600200e`|`PdfKata_Command` [CommandMethod("pdfkata")]|`()`|`void`|`3214748`|`4`|`ae3c8b8d99a39542...`|
|`0x0600200f`|`Onlinekata` [CommandMethod("online_kata")]|`()`|`void`|`3214764`|`4`|`ae3c8b8d99a39542...`|
|`0x06002010`|`Thong_ke_qhp` [CommandMethod("tktqhp")]|`()`|`void`|`3214908`|`4`|`ae3c8b8d99a39542...`|
|`0x06002011`|`Thong_ke_qhp1` [CommandMethod("tktqhp1")]|`()`|`void`|`3215092`|`4`|`ae3c8b8d99a39542...`|
|`0x06002012`|`goi_setting_atlas` [CommandMethod("setupatlas")]|`()`|`void`|`3215280`|`4`|`ae3c8b8d99a39542...`|
|`0x06002013`|`Goi_setting` [CommandMethod("setupkata1")]|`()`|`void`|`3215832`|`4`|`ae3c8b8d99a39542...`|
|`0x06002014`|`Ghi_thep_Atlas` [CommandMethod("gta")]|`()`|`void`|`3216532`|`4`|`ae3c8b8d99a39542...`|
|`0x06002015`|`Ththep_atlas` [CommandMethod("tha")]|`()`|`void`|`3216764`|`4`|`ae3c8b8d99a39542...`|
|`0x06002016`|`Thong_ke_Uc` [CommandMethod("tktUc")]|`()`|`void`|`3216780`|`4`|`ae3c8b8d99a39542...`|
|`0x06002017`|`Thong_ke_PTD` [CommandMethod("PTD")]|`()`|`void`|`3217004`|`4`|`ae3c8b8d99a39542...`|
|`0x06002018`|`Mirror_thep1` [CommandMethod("dao_thep")]|`()`|`void`|`3217192`|`4`|`ae3c8b8d99a39542...`|
|`0x06002019`|`Danhsothep` [CommandMethod("dso")]|`()`|`void`|`3217208`|`4`|`ae3c8b8d99a39542...`|
|`0x0600201a`|`Markthep` [CommandMethod("mark")]|`()`|`void`|`3217368`|`4`|`ae3c8b8d99a39542...`|
|`0x0600201b`|`Danhsothep_auto` [CommandMethod("dsoauto")]|`()`|`void`|`3217624`|`4`|`ae3c8b8d99a39542...`|
|`0x0600201c`|`Markthep_auto` [CommandMethod("markauto")]|`()`|`void`|`3217784`|`4`|`ae3c8b8d99a39542...`|
|`0x0600201d`|`Update_leg` [CommandMethod("updateleg")]|`()`|`void`|`3218080`|`4`|`ae3c8b8d99a39542...`|
|`0x0600201e`|`Update_crank1` [CommandMethod("updatecrank")]|`()`|`void`|`3218236`|`4`|`ae3c8b8d99a39542...`|
|`0x0600201f`|`Chuyen_line_todim1` [CommandMethod("kdl")]|`()`|`void`|`3218432`|`4`|`ae3c8b8d99a39542...`|
|`0x06002020`|`Update_dai` [CommandMethod("updatedai")]|`()`|`void`|`3218608`|`4`|`ae3c8b8d99a39542...`|
|`0x06002021`|`Khthep` [CommandMethod("khthep")]|`()`|`void`|`3218784`|`4`|`ae3c8b8d99a39542...`|
|`0x06002022`|`Ve_thep_openning1` [CommandMethod("openning")]|`()`|`void`|`3219196`|`4`|`ae3c8b8d99a39542...`|
|`0x06002023`|`Adappt` [CommandMethod("adappt")]|`()`|`void`|`3219352`|`4`|`ae3c8b8d99a39542...`|
|`0x06002024`|`Loadkho` [CommandMethod("loadkho")]|`()`|`void`|`3219528`|`4`|`ae3c8b8d99a39542...`|
|`0x06002025`|`Upkho` [CommandMethod("upkho")]|`()`|`void`|`3219776`|`4`|`ae3c8b8d99a39542...`|
|`0x06002026`|`Thong_ke_coppha_be_tong1` [CommandMethod("tkqs")]|`()`|`void`|`3220004`|`4`|`ae3c8b8d99a39542...`|
|`0x06002027`|`Tong_text1` [CommandMethod("tong")]|`()`|`void`|`3220232`|`4`|`ae3c8b8d99a39542...`|
|`0x06002028`|`Locdam1` [CommandMethod("locdam")]|`()`|`void`|`3220248`|`4`|`ae3c8b8d99a39542...`|
|`0x06002029`|`Saptd1` [CommandMethod("saptd")]|`()`|`void`|`3220264`|`4`|`ae3c8b8d99a39542...`|
|`0x0600202a`|`Gan_tag_block1` [CommandMethod("ganblock")]|`()`|`void`|`3220280`|`4`|`ae3c8b8d99a39542...`|
|`0x0600202b`|`Rai_block_thep1` [CommandMethod("raithep")]|`()`|`void`|`3220296`|`4`|`ae3c8b8d99a39542...`|
|`0x0600202c`|`Roundpl1` [CommandMethod("roundpl")]|`()`|`void`|`3220312`|`4`|`ae3c8b8d99a39542...`|
|`0x0600202d`|`Thong_ke_bvbs` [CommandMethod("bvbs")]|`()`|`void`|`3220328`|`4`|`ae3c8b8d99a39542...`|
|`0x0600202e`|`Serikata` [CommandMethod("serikata")]|`()`|`void`|`3220524`|`4`|`ae3c8b8d99a39542...`|
|`0x0600202f`|`Drop_panel1` [CommandMethod("drpn")]|`()`|`void`|`3220684`|`4`|`ae3c8b8d99a39542...`|
|`0x06002030`|`Phan_loai_san1` [CommandMethod("tkssan")]|`()`|`void`|`3220700`|`4`|`ae3c8b8d99a39542...`|
|`0x06002031`|`CjUtxSq38AF3ROTv181P`|`()`|`bool`|`3220716`|`4`|`66ea1533cb3404e9...`|
|`0x06002032`|`cuoLl3q3EdeST0rRkcDg`|`()`|`Kata_pro64_Cad2025.Lenh_thep [class:856]`|`3220736`|`4`|`8969ce0cdaa4a19c...`|

### [VM-202] Methods of `Kata_pro64_Cad2025.AI_BeamFoundationReferencePdfBuilder` (Token `0x02000131`, Displayed: 24/24, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06002414`|`.cctor`|`()`|`void`|`3519928`|`616`|`60a2b9e2e08e0121...`|
|`0x06002415`|`.ctor`|`()`|`void`|`3520556`|`4`|`ae3c8b8d99a39542...`|
|`0x06002416`|`GetReferencePdfPath`|`string`|`string`|`3520680`|`4`|`8969ce0cdaa4a19c...`|
|`0x06002417`|`Build`|`string, string, string`|`string`|`3520728`|`4`|`8969ce0cdaa4a19c...`|
|`0x06002418`|`Axd8Q6IDKCB`|`object, object, i4, object, object, object`|`void`|`3523932`|`4`|`ae3c8b8d99a39542...`|
|`0x06002419`|`E1Y8QW3avnH`|`object, object, r4`|`void`|`3524412`|`4`|`ae3c8b8d99a39542...`|
|`0x0600241a`|`Ycx8Qst4sWr`|`object, object, object, object`|`void`|`3524784`|`4`|`ae3c8b8d99a39542...`|
|`0x0600241b`|`ArO8QtU1jh5`|`object, object, i4, object, object, r4, r4, bool, System.Collections.Generic.List`1 [class:217]<dCNMCcErf7lN7U8BwC7X [class:1224]>`|`void`|`3525004`|`4`|`ae3c8b8d99a39542...`|
|`0x0600241c`|`dlA8QdlP8eI`|`object, object`|`bool`|`3528160`|`4`|`66ea1533cb3404e9...`|
|`0x0600241d`|`z7F8QJu3FLb`|`object, object, i4, object, r4, r4, r4, bool, System.Collections.Generic.List`1 [class:217]<dCNMCcErf7lN7U8BwC7X [class:1224]>`|`void`|`3528508`|`4`|`ae3c8b8d99a39542...`|
|`0x0600241e`|`d1c8QrfAneu`|`object, object, object, r4, r4, r4`|`r8[]`|`3530036`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600241f`|`tUU8QGYeqkx`|`object, object, i4, object, System.Collections.Generic.List`1 [class:217]<dCNMCcErf7lN7U8BwC7X [class:1224]>`|`void`|`3531460`|`4`|`ae3c8b8d99a39542...`|
|`0x06002420`|`Sj08Ql2uf5f`|`object, object, i4, object, System.Collections.Generic.List`1 [class:217]<dCNMCcErf7lN7U8BwC7X [class:1224]>, r4, r4, r4`|`void`|`3533384`|`4`|`ae3c8b8d99a39542...`|
|`0x06002421`|`O9l8Qu1Ll8i`|`object, object, r4, r4, object, r4, object, object, r4, i4, System.Collections.Generic.List`1 [class:217]<dCNMCcErf7lN7U8BwC7X [class:1224]>`|`void`|`3535028`|`4`|`ae3c8b8d99a39542...`|
|`0x06002422`|`rWG8QHi2RJS`|`object, System.Collections.Generic.List`1 [class:217]<dCNMCcErf7lN7U8BwC7X [class:1224]>`|`bool`|`3536504`|`4`|`66ea1533cb3404e9...`|
|`0x06002423`|`TDI8QpNvKsT`|`object`|`iTextSharp.text.BaseColor [class:1493]`|`3537340`|`4`|`8969ce0cdaa4a19c...`|
|`0x06002424`|`CNf8QwkGJWB`|`object`|`string`|`3538244`|`4`|`8969ce0cdaa4a19c...`|
|`0x06002425`|`lIs8Q2mKYi8`|`object`|`string`|`3538564`|`4`|`8969ce0cdaa4a19c...`|
|`0x06002426`|`hDh8QXQOSeO`|`object`|`r8[]`|`3539104`|`4`|`8969ce0cdaa4a19c...`|
|`0x06002427`|`cMV8QiwyD0u`|`object, i4`|`i4`|`3539600`|`4`|`78d4d2062aa5373e...`|
|`0x06002428`|`IN68QnFI2Mj`|`object, bool`|`bool`|`3539848`|`4`|`66ea1533cb3404e9...`|
|`0x06002429`|`bKj8Q557wUv`|`object, r8&`|`bool`|`3540424`|`4`|`66ea1533cb3404e9...`|
|`0x0600242a`|`ziXiB9qqZbktb3SfhORV`|`()`|`bool`|`3541100`|`4`|`66ea1533cb3404e9...`|
|`0x0600242b`|`T2OTWcqqekOSuDGpw06l`|`()`|`Kata_pro64_Cad2025.AI_BeamFoundationReferencePdfBuilder [class:1220]`|`3541120`|`4`|`8969ce0cdaa4a19c...`|

### [VM-203] Methods of `Kata_pro64_Cad2025.AI_DrawIsolatedFoundation_Tool` (Token `0x020001be`, Displayed: 17/17, Evidence `ev_3bdf54e817a9b5840d6618afe7ca3edeaabd4e8c3021dcb361d608566084bf9d`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06002c84`|`.ctor`|`()`|`void`|`4162924`|`4`|`ae3c8b8d99a39542...`|
|`0x06002c85`|`get_Name`|`()`|`string`|`4163048`|`4`|`8969ce0cdaa4a19c...`|
|`0x06002c86`|`get_Description`|`()`|`string`|`4163084`|`4`|`8969ce0cdaa4a19c...`|
|`0x06002c87`|`get_ExplorerContexts`|`()`|`string[]`|`4163128`|`4`|`8969ce0cdaa4a19c...`|
|`0x06002c88`|`Get_DrawIsolatedFoundation_Instructions`|`()`|`string`|`4163180`|`4`|`8969ce0cdaa4a19c...`|
|`0x06002c89`|`Get_DrawIsolatedFoundation_UserPromptExamples`|`()`|`string`|`4163224`|`4`|`8969ce0cdaa4a19c...`|
|`0x06002c8a`|`GetInstructions`|`()`|`string`|`4163268`|`4`|`8969ce0cdaa4a19c...`|
|`0x06002c8b`|`GetUserPromptExamples`|`()`|`string`|`4163284`|`4`|`8969ce0cdaa4a19c...`|
|`0x06002c8c`|`CreateParametersSchema`|`()`|`Newtonsoft.Json.Linq.JObject [class:517]`|`4163300`|`4`|`8969ce0cdaa4a19c...`|
|`0x06002c8d`|`Execute`|`Newtonsoft.Json.Linq.JObject [class:517]`|`Kata_pro64_Cad2025.AIToolExecutionResult [class:2384]`|`4165248`|`4`|`8969ce0cdaa4a19c...`|
|`0x06002c8e`|`nI68MhoqVhV`|`object`|`bool`|`4167200`|`4`|`66ea1533cb3404e9...`|
|`0x06002c8f`|`td98MZNHhPa`|`object, r8`|`r8`|`4167468`|`4`|`f0ad83c607e59d90...`|
|`0x06002c90`|`NCl8MeY1B51`|`object, object`|`void`|`4167704`|`4`|`ae3c8b8d99a39542...`|
|`0x06002c91`|`V528MLIffh2`|`object, r8, r8, r8&, r8&, r8&, r8&, r8&, r8&`|`void`|`4167904`|`4`|`ae3c8b8d99a39542...`|
|`0x06002c92`|`.cctor`|`()`|`void`|`4170516`|`177`|`77b78fd954e54507...`|
|`0x06002c93`|`iL1srTqCYcQrg0WJPQtd`|`()`|`bool`|`4170708`|`4`|`66ea1533cb3404e9...`|
|`0x06002c94`|`MuTXJwqColx3C91Avnd5`|`()`|`Kata_pro64_Cad2025.AI_DrawIsolatedFoundation_Tool [class:1784]`|`4170728`|`4`|`8969ce0cdaa4a19c...`|

### [VM-204] Methods of `Kata_pro64_Cad2025.kata1` (Token `0x0200027b`, Displayed: 56/56, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x0600350c`|`.cctor`|`()`|`void`|`5279112`|`195`|`61a0f88e3f7b107a...`|
|`0x0600350d`|`pt_thep`|`i4&, i4&, string&, string&, string&, Kata_Class_Lib_Revit.info_thep [class:793][]&, string&, string`|`void`|`5279320`|`4`|`ae3c8b8d99a39542...`|
|`0x0600350e`|`block_KHT_thang`|`Kata_Class_Lib_Revit.diem [class:85], string, r8, r8, i4, i4, string, string, string, string, r8, string`|`object`|`5280996`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600350f`|`block_khtct`|`Kata_Class_Lib_Revit.diem [class:85], i4, string, r8, string, string`|`object`|`5286800`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003510`|`block_khtctSH`|`Kata_Class_Lib_Revit.diem [class:85], i4, string, r8, string, string, string, r8, r8`|`object`|`5288672`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003511`|`block_kht_don`|`Kata_Class_Lib_Revit.diem [class:85], i4, string, object, i4, string, string, string, string, string, r8`|`object`|`5293720`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003512`|`block_Ten`|`Kata_Class_Lib_Revit.diem [class:85], i4, string, string, r8, r8, r8`|`object`|`5297836`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003513`|`GetTenFromBlockTen`|`object`|`string`|`5300268`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003514`|`GetViTriFromBlockTen`|`object`|`ViTriDatTen [valuetype:3564]`|`5300940`|`8`|`c41f727e2e00669d...`|
|`0x06003515`|`block_Mong_Don`|`InfoRaiMongDon [class:3296], Kata_Class_Lib_Revit.diem [class:85], i4`|`object`|`5301900`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003516`|`GetInfoCotChiuTaiMongDonFromBlock`|`object`|`CotChiuTaiMongDon [class:3624]`|`5306204`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003517`|`GetInfoRaiMongDonFromBlock`|`object`|`InfoRaiMongDon [class:3296]`|`5307480`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003518`|`block_Beam_Rebar`|`Kata_Class_Lib_Revit.diem [class:85], r8, r8, string[], string, string[], r8`|`object`|`5312212`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003519`|`block_truc`|`Kata_Class_Lib_Revit.diem [class:85]&, i4&, object&`|`object`|`5314912`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600351a`|`block_3D`|`Kata_Class_Lib_Revit.diem [class:85]&, r8&, r8&, r8&, string`|`object`|`5316220`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600351b`|`block_3D_dim`|`Kata_Class_Lib_Revit.diem [class:85]&, r8&, r8&, r8&, r8&, string`|`object`|`5319348`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600351c`|`block_formwork_beam`|`Kata_Class_Lib_Revit.diem [class:85]&, r8&, r8&, r8&, r8&, r8&, r8&, r8&, r8&, r8&, r8&, r8&, string, i4&`|`void`|`5319992`|`4`|`ae3c8b8d99a39542...`|
|`0x0600351d`|`block_coupler`|`Kata_Class_Lib_Revit.diem [class:85]&, i4&, r8&`|`void`|`5321620`|`4`|`ae3c8b8d99a39542...`|
|`0x0600351e`|`block_ren`|`Kata_Class_Lib_Revit.diem [class:85]&, r8&, r8`|`object`|`5325476`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600351f`|`ve_khoang_rai`|`Kata_Class_Lib_Revit.diem [class:85]&, Kata_Class_Lib_Revit.diem [class:85]&, string, i4, string`|`object`|`5326252`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003520`|`update_khoang_rai_data`|`object&, string, i4, string`|`object`|`5328340`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003521`|`xd_giao_thep_dim`|`object&, Kata_Class_Lib_Revit.diem [class:85]&`|`bool`|`5329904`|`4`|`66ea1533cb3404e9...`|
|`0x06003522`|`ve_giao_thep_dim`|`object&`|`void`|`5333320`|`4`|`ae3c8b8d99a39542...`|
|`0x06003523`|`block_kht`|`Kata_Class_Lib_Revit.diem [class:85]&, i4&, r8&, r8&, r8&, i4&, string&, string&, Kata_Class_Lib_Revit.info_thep [class:793][]&, r8`|`object`|`5334844`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003524`|`block_kht_hon2`|`Kata_Class_Lib_Revit.diem [class:85]&, i4&, r8&, r8&, r8&, i4&, string&, string, Kata_Class_Lib_Revit.info_thep [class:793][]&, r8`|`void`|`5337548`|`4`|`ae3c8b8d99a39542...`|
|`0x06003525`|`rai_line_thep`|`Kata_Class_Lib_Revit.diem [class:85]&, i4&, r8, r8, r8, string, i4`|`void`|`5338708`|`4`|`ae3c8b8d99a39542...`|
|`0x06003526`|`rai_sl_thep`|`Kata_Class_Lib_Revit.diem [class:85]&, i4&, r8, i4, r8`|`void`|`5340100`|`4`|`ae3c8b8d99a39542...`|
|`0x06003527`|`block_thep`|`Kata_Class_Lib_Revit.diem [class:85]&, string&, object&`|`object`|`5341232`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003528`|`rai_thep`|`Kata_Class_Lib_Revit.diem [class:85]&, i4&, object&, object&, object&, object&`|`void`|`5341836`|`4`|`ae3c8b8d99a39542...`|
|`0x06003529`|`rai_thep_tron`|`Kata_Class_Lib_Revit.diem [class:85]&, i4&, r8&, i4&`|`void`|`5342964`|`4`|`ae3c8b8d99a39542...`|
|`0x0600352a`|`block_TD`|`Kata_Class_Lib_Revit.diem [class:85]&, string&, i4&, string`|`string`|`5343868`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600352b`|`sEH86qo9Vgi`|`object`|`void`|`5346780`|`4`|`ae3c8b8d99a39542...`|
|`0x0600352c`|`block_secbal1`|`Kata_Class_Lib_Revit.diem [class:85]&, string&, i4&, r8&, r8&`|`void`|`5349628`|`4`|`ae3c8b8d99a39542...`|
|`0x0600352d`|`block_secbal`|`Kata_Class_Lib_Revit.diem [class:85]&, string&, i4&, r8&, r8&`|`void`|`5350572`|`4`|`ae3c8b8d99a39542...`|
|`0x0600352e`|`block_kht_quay`|`Kata_Class_Lib_Revit.diem [class:85]&, i4&, r8&, object&, string&, string&, string&`|`void`|`5352772`|`4`|`ae3c8b8d99a39542...`|
|`0x0600352f`|`block_ct`|`Kata_Class_Lib_Revit.diem [class:85]&, i4&, string&`|`void`|`5355072`|`4`|`ae3c8b8d99a39542...`|
|`0x06003530`|`Lendk`|`object`|`r8`|`5356012`|`4`|`f0ad83c607e59d90...`|
|`0x06003531`|`chon_Viewport`|`bool`|`void`|`5356672`|`4`|`ae3c8b8d99a39542...`|
|`0x06003532`|`rai_thep_duong`|`i4, r8`|`void`|`5362840`|`4`|`ae3c8b8d99a39542...`|
|`0x06003533`|`tim_doan_giao`|`object, Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85]`|`void`|`5365544`|`4`|`ae3c8b8d99a39542...`|
|`0x06003534`|`sua_rai_block_thep`|`()`|`bool`|`5368140`|`4`|`66ea1533cb3404e9...`|
|`0x06003535`|`rai_block_thep`|`()`|`void`|`5371504`|`4`|`ae3c8b8d99a39542...`|
|`0x06003536`|`InsertBlock`|`r8[], string, r8, r8, r8, r8`|`object`|`5374380`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003537`|`Type_ColumnWall`|`Kata_Class_Lib_Revit.TypeColumnWall [valuetype:1633]`|`string`|`5376340`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003538`|`block_ColumnWall`|`Kata_Class_Lib_Revit.diem [class:85]&, i4&, i4&, r8&, string, string`|`object`|`5376756`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003539`|`rai_block_cot`|`()`|`void`|`5381688`|`4`|`ae3c8b8d99a39542...`|
|`0x0600353a`|`xd_2dXY_block_ColumnWall`|`object, Kata_Class_Lib_Revit.diem [class:85]&, Kata_Class_Lib_Revit.diem [class:85]&`|`bool`|`5388368`|`4`|`66ea1533cb3404e9...`|
|`0x0600353b`|`kata_block_Wipe`|`Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85]`|`object`|`5391664`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600353c`|`kata_block_Second_Beam`|`Kata_Class_Lib_Revit.diem [class:85], r8, string`|`object`|`5393116`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600353d`|`Type_beam`|`Kata_Class_Lib_Revit.TypeBeam [valuetype:1197]`|`string`|`5394488`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600353e`|`block_Beam_plan`|`Kata_Class_Lib_Revit.diem [class:85], i4, i4, Kata_Class_Lib_Revit.TypeBeam [valuetype:1197], r8, string, string, string, i4`|`object`|`5394944`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600353f`|`rai_Grid`|`()`|`void`|`5399892`|`4`|`ae3c8b8d99a39542...`|
|`0x06003540`|`CreateBlockFromObjects11111`|`Kata_Class_Lib_Revit.diem [class:85], System.Collections.Generic.List`1 [class:217]<object>, string, string`|`object`|`5403900`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003541`|`CreateBlockFromObjects`|`Kata_Class_Lib_Revit.diem [class:85], System.Collections.Generic.List`1 [class:217]<object>, string, bool`|`object`|`5408932`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003542`|`hiPItNqQ7vrlqW3fCTX2`|`()`|`bool`|`5412108`|`4`|`66ea1533cb3404e9...`|
|`0x06003543`|`i0krnwqQFMH1b0rYPdWA`|`()`|`Kata_pro64_Cad2025.kata1 [class:2540]`|`5412128`|`4`|`8969ce0cdaa4a19c...`|

### [VM-205] Methods of `Kata_pro64_Cad2025.Mong_coc` (Token `0x020002ec`, Displayed: 115/115, Evidence `ev_3bdf54e817a9b5840d6618afe7ca3edeaabd4e8c3021dcb361d608566084bf9d`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003988`|`.cctor`|`()`|`void`|`6075252`|`1080`|`2428a57627959e0f...`|
|`0x06003989`|`getInfoMongCocSaveJsonData`|`()`|`string`|`6076344`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600398a`|`getInfoMongCocSaveJsonData2`|`()`|`string`|`6077884`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600398b`|`Ve_ca_mong_coc`|`()`|`void`|`6077916`|`4`|`ae3c8b8d99a39542...`|
|`0x0600398c`|`block_TD_without_tl`|`Kata_Class_Lib_Revit.diem [class:85]&, string&, i4&, string`|`string`|`6078320`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600398d`|`Ve_bao_MC1`|`()`|`void`|`6081716`|`4`|`ae3c8b8d99a39542...`|
|`0x0600398e`|`get_length_spacing_so_thanh_kc2ben`|`r8, r8`|`void`|`6186676`|`4`|`ae3c8b8d99a39542...`|
|`0x0600398f`|`getListListL`|`System.Collections.Generic.List`1 [class:217]<r8>`|`void`|`6186924`|`4`|`ae3c8b8d99a39542...`|
|`0x06003990`|`getLText`|`System.Collections.Generic.List`1 [class:217]<r8>, i4, r8, r8`|`void`|`6188232`|`4`|`ae3c8b8d99a39542...`|
|`0x06003991`|`Ve_thep_them_duoi_doc`|`()`|`void`|`6190116`|`4`|`ae3c8b8d99a39542...`|
|`0x06003992`|`Ve_thep_them_tren_doc`|`()`|`void`|`6200436`|`4`|`ae3c8b8d99a39542...`|
|`0x06003993`|`TinhToanThepDocMongCoc`|`Kata_Class_Lib_Revit.diem [class:85]&, Kata_Class_Lib_Revit.diem [class:85], Polygon [class:89], System.Collections.Generic.List`1 [class:217]<r8>&, bool`|`void`|`6210796`|`4`|`ae3c8b8d99a39542...`|
|`0x06003994`|`TinhToanThepNgangMongCoc`|`Kata_Class_Lib_Revit.diem [class:85]&, Kata_Class_Lib_Revit.diem [class:85], Polygon [class:89], System.Collections.Generic.List`1 [class:217]<r8>&, bool`|`void`|`6211472`|`4`|`ae3c8b8d99a39542...`|
|`0x06003995`|`ve_cot_mong_coc`|`Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85]`|`void`|`6212168`|`4`|`ae3c8b8d99a39542...`|
|`0x06003996`|`get_L_a_n_ThepDuoiRaiTheHien`|`r8, Kata_Class_Lib_Revit.diem [class:85]&`|`void`|`6214508`|`4`|`ae3c8b8d99a39542...`|
|`0x06003997`|`get_L_a_n_ThepTrenRaiTheHien`|`r8, Kata_Class_Lib_Revit.diem [class:85]&`|`void`|`6215664`|`4`|`ae3c8b8d99a39542...`|
|`0x06003998`|`thay_doi_thu_tu_diem`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>&, i4`|`void`|`6216700`|`4`|`ae3c8b8d99a39542...`|
|`0x06003999`|`timPGocGiua`|`Kata_Class_Lib_Revit.diem [class:85]`|`Kata_Class_Lib_Revit.diem [class:85]`|`6217204`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600399a`|`kiemTraDoiXungTheoPhuongb`|`Kata_Class_Lib_Revit.diem [class:85]`|`bool`|`6218532`|`4`|`66ea1533cb3404e9...`|
|`0x0600399b`|`kiemTraDoiXungTheoPhuongl`|`Kata_Class_Lib_Revit.diem [class:85]`|`bool`|`6219368`|`4`|`66ea1533cb3404e9...`|
|`0x0600399c`|`dRu8lDG5a8D`|`object, System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, object`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6220208`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600399d`|`dimthepdn`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, Kata_Class_Lib_Revit.diem [class:85]&, r8`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6220240`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600399e`|`dimtheptn`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, Kata_Class_Lib_Revit.diem [class:85]&, r8`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6222228`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600399f`|`dimthepngangB`|`r8, System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, Kata_Class_Lib_Revit.diem [class:85]&`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6224236`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039a0`|`dimthepdd`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, Kata_Class_Lib_Revit.diem [class:85]&, r8`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6226236`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039a1`|`dimtheptd`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, Kata_Class_Lib_Revit.diem [class:85]&, r8`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6228452`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039a2`|`dimthepngang`|`r8, System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, Kata_Class_Lib_Revit.diem [class:85]&`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6230424`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039a3`|`layTapPointsDNMB`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, Kata_Class_Lib_Revit.diem [class:85]&, r8, r8`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6231208`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039a4`|`layTapPointsDNMB2`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85]&, r8, r8`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6233104`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039a5`|`layTapPointsTNMB`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, Kata_Class_Lib_Revit.diem [class:85]&, r8, r8`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6234624`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039a6`|`layTapPointsNgangMBB`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, Kata_Class_Lib_Revit.diem [class:85]&`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6236316`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039a7`|`layTapPointsTNMB2`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85]&, r8, r8`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6236964`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039a8`|`layTapPointsNgangMB2B`|`r8, r8, System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85]&`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6238104`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039a9`|`layTapPointsDDMB`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, Kata_Class_Lib_Revit.diem [class:85]&, r8, r8`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6239024`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039aa`|`layTapPointsDDMB2`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85]&, r8, r8`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6240880`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039ab`|`layTapPointsTDMB`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, Kata_Class_Lib_Revit.diem [class:85]&, r8, r8`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6242204`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039ac`|`LayTapPointsNgangMB`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, Kata_Class_Lib_Revit.diem [class:85]&`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6243812`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039ad`|`LayTapPointsTDMB2`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85]&, r8, r8`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6244392`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039ae`|`LayTapPointsNgangMB2`|`r8, r8, System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85]&`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6245552`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039af`|`Getl2pointcatmp`|`Kata_Class_Lib_Revit.diem [class:85], System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85]`|`r8`|`6246512`|`4`|`f0ad83c607e59d90...`|
|`0x060039b0`|`TimBvaL`|`r8&, r8&, Kata_Class_Lib_Revit.diem [class:85]&, System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`void`|`6246932`|`4`|`ae3c8b8d99a39542...`|
|`0x060039b1`|`timBvaLMC`|`r8&, r8&, Kata_Class_Lib_Revit.diem [class:85]&, System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`void`|`6248736`|`4`|`ae3c8b8d99a39542...`|
|`0x060039b2`|`lay_tap_point_thep_thep_cot_traiMC`|`Dlcot [class:3024], System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>&, r8, r8, Kata_Class_Lib_Revit.diem [class:85], r8&, Kata_Class_Lib_Revit.diem [class:85]&`|`void`|`6249576`|`4`|`ae3c8b8d99a39542...`|
|`0x060039b3`|`lay_tap_point_thep_thep_cot_phaiMC`|`Dlcot [class:3024], System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>&, r8, r8, Kata_Class_Lib_Revit.diem [class:85]`|`void`|`6251452`|`4`|`ae3c8b8d99a39542...`|
|`0x060039b4`|`lay_tap_point_thep_ngang`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6252984`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039b5`|`LayLLPointDo`|`i4&, i4&`|`System.Collections.Generic.List`1 [class:217]<System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>>`|`6253280`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039b6`|`LayListPointRaiThepDo`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6254804`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039b7`|`layTapPointTDMatCat`|`Kata_Class_Lib_Revit.diem [class:85]&, r8, r8, r8`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6256192`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039b8`|`layTapPointTNMatCat`|`Kata_Class_Lib_Revit.diem [class:85]&, r8, r8, r8`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6257412`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039b9`|`layTapPointDNMatCat`|`Kata_Class_Lib_Revit.diem [class:85]&, r8, r8, r8`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6258648`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039ba`|`layTapPointDDMatCat`|`Kata_Class_Lib_Revit.diem [class:85]&, r8, r8, r8`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6260228`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039bb`|`them_moc`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>&, Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], bool, bool`|`void`|`6261784`|`4`|`ae3c8b8d99a39542...`|
|`0x060039bc`|`layDiemThepTraiThepDai`|`()`|`Kata_Class_Lib_Revit.diem [class:85]`|`6262608`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039bd`|`layDiemThepPhaiThepDai`|`()`|`Kata_Class_Lib_Revit.diem [class:85]`|`6263408`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039be`|`layTapCurveDo`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Line_diem [class:789]>`|`6264312`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039bf`|`layTapCurveT`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Line_diem [class:789]>`|`6265944`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039c0`|`layTapPointCTTrai`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6266340`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039c1`|`layTapPointCTPhai`|`Kata_Class_Lib_Revit.diem [class:85]&`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6267636`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039c2`|`layTapCurveCTTrai`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Line_diem [class:789]>`|`6269112`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039c3`|`layTapCurveCTPhai`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Line_diem [class:789]>`|`6269356`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039c4`|`layTapCurveNgang`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Line_diem [class:789]>`|`6269660`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039c5`|`layTapCurveD`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Line_diem [class:789]>`|`6270192`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039c6`|`timPGoc`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`Kata_Class_Lib_Revit.diem [class:85]`|`6270568`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039c7`|`tao_dim_tu_tap_diem_phuongY`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85]`|`void`|`6270588`|`4`|`ae3c8b8d99a39542...`|
|`0x060039c8`|`tao_dim_tu_tap_diem_phuongX`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85]`|`void`|`6270968`|`4`|`ae3c8b8d99a39542...`|
|`0x060039c9`|`get_list_point_coc_in_view_autocad`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6271408`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039ca`|`kiemTrapoint_coc_trong_hcn_autocad`|`Kata_Class_Lib_Revit.diem [class:85], System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, Kata_Class_Lib_Revit.diem [class:85]`|`bool`|`6273048`|`4`|`66ea1533cb3404e9...`|
|`0x060039cb`|`lay_list_point_cot`|`System.Collections.Generic.List`1 [class:217]<System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>>`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6274512`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039cc`|`lay_list_point_mong`|`System.Collections.Generic.List`1 [class:217]<System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>>`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6275384`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039cd`|`chuyenDuLieuFormSangMongCoc`|`()`|`void`|`6276364`|`4`|`ae3c8b8d99a39542...`|
|`0x060039ce`|`get_list_point_nam_trong_cach_1_khoang_d`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, r8`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6287804`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039cf`|`get_list_point_nam_ngoai_cach_1_khoang_d`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, r8`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6288676`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039d0`|`rai_line_thep_va_return_thep`|`object&, Kata_Class_Lib_Revit.diem [class:85]&, i4&, r8, r8, r8`|`void`|`6289572`|`4`|`ae3c8b8d99a39542...`|
|`0x060039d1`|`lay_tap_points_thep_dai_phai_mong_coc`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6291120`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039d2`|`lay_tap_points_thep_dai_trai_mong_coc`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6291688`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039d3`|`ve_giao_thep_dim_update`|`object&`|`void`|`6292264`|`4`|`ae3c8b8d99a39542...`|
|`0x060039d4`|`GetListCurvesMatCat`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Line_diem [class:789]>`|`6293624`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039d5`|`GetCurveBreakLineTrai`|`()`|`Kata_Class_Lib_Revit.Line_diem [class:789]`|`6294184`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039d6`|`GetCurveBreakLinePhai`|`()`|`Kata_Class_Lib_Revit.Line_diem [class:789]`|`6294872`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039d7`|`GetLLCurvesBTL`|`()`|`System.Collections.Generic.List`1 [class:217]<System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Line_diem [class:789]>>`|`6295672`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039d8`|`GetListCurvesCoc`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Line_diem [class:789]>`|`6299456`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039d9`|`GetListCurvesBLCoc`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Line_diem [class:789]>`|`6303332`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039da`|`ve_thep_cot`|`Kata_Class_Lib_Revit.PreviewCanvas [class:261]`|`void`|`6304192`|`4`|`ae3c8b8d99a39542...`|
|`0x060039db`|`IsListPCotHopLe`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`System.Tuple`3 [class:805]<bool,Kata_Class_Lib_Revit.diem [class:85],Kata_Class_Lib_Revit.diem [class:85]>`|`6306508`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039dc`|`GetListCurveCot`|`()`|`System.Tuple`3 [class:805]<System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Line_diem [class:789]>,System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Line_diem [class:789]>,Kata_Class_Lib_Revit.diem [class:85]>`|`6309452`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039dd`|`GetListBLCot`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Line_diem [class:789]>`|`6316416`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039de`|`ve_mat_cat_coc`|`Kata_Class_Lib_Revit.PreviewCanvas [class:261]`|`void`|`6320300`|`4`|`ae3c8b8d99a39542...`|
|`0x060039df`|`ve_coc_mong_coc`|`Kata_Class_Lib_Revit.PreviewCanvas [class:261], System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, r8, System.Drawing.Point [valuetype:173], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], r8, System.Drawing.Color [valuetype:697]`|`void`|`6322416`|`4`|`ae3c8b8d99a39542...`|
|`0x060039e0`|`ve_hinh_tron`|`Kata_Class_Lib_Revit.PreviewCanvas [class:261], Kata_Class_Lib_Revit.diem [class:85], r8, System.Drawing.Point [valuetype:173], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], r8, System.Drawing.Color [valuetype:697], bool, r8`|`void`|`6323440`|`4`|`ae3c8b8d99a39542...`|
|`0x060039e1`|`VeMCMongForm`|`Kata_Class_Lib_Revit.PreviewCanvas [class:261]`|`void`|`6323920`|`4`|`ae3c8b8d99a39542...`|
|`0x060039e2`|`VeMBMongForm`|`Kata_Class_Lib_Revit.PreviewCanvas [class:261]`|`void`|`6326168`|`4`|`ae3c8b8d99a39542...`|
|`0x060039e3`|`Ve_mong_coc`|`System.Collections.Generic.List`1 [class:217]<Autodesk.AutoCAD.DatabaseServices.BlockReference [class:929]>, System.Collections.Generic.List`1 [class:217]<Autodesk.AutoCAD.DatabaseServices.BlockReference [class:929]>, System.Collections.Generic.List`1 [class:217]<Autodesk.AutoCAD.DatabaseServices.Polyline [class:1017]>, System.Collections.Generic.List`1 [class:217]<Autodesk.AutoCAD.DatabaseServices.Circle [class:1677]>, System.Collections.Generic.List`1 [class:217]<Autodesk.AutoCAD.DatabaseServices.Line [class:1673]>, System.Collections.Generic.List`1 [class:217]<Autodesk.AutoCAD.DatabaseServices.Line [class:1673]>`|`void`|`6331068`|`4`|`ae3c8b8d99a39542...`|
|`0x060039e4`|`getListPRaiThepDai`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>&`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6347312`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039e5`|`getListPRaiThepDuoi`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6348616`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039e6`|`getListPRaiThepTren`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6349160`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039e7`|`getListCThepDuoiNgangTheHienForm`|`LopThepThem [class:3008], Kata_Class_Lib_Revit.diem [class:85]`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Line_diem [class:789]>`|`6349744`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039e8`|`getListCThepTrenNgangTheHienForm`|`LopThepThem [class:3008], Kata_Class_Lib_Revit.diem [class:85]`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Line_diem [class:789]>`|`6351096`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039e9`|`getListPRaiThepDuoi`|`LopThepThem [class:3008]`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6352348`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039ea`|`getListPRaiThepTren`|`LopThepThem [class:3008]`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6354372`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039eb`|`GetListPThepDuoi`|`LopThepThem [class:3008]`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6356372`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039ec`|`GetListPThepTren`|`LopThepThem [class:3008]`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6357316`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039ed`|`GetListPRaiThepNgang`|`LopThepThem [class:3008]`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6358244`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039ee`|`GetListPThepNgang`|`LopThepThem [class:3008]`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6360228`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039ef`|`getListPRaiCotTronUtil`|`Dlcot [class:3024], KataCircle3D [class:3004], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85]`|`System.Tuple`2 [class:285]<System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>,System.Collections.Generic.List`1 [class:217]<System.Tuple`2 [class:285]<CurveXYZ [class:293],r8>>>`|`6362048`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039f0`|`getListPRaiCotTron`|`Dlcot [class:3024], KataCircle3D [class:3004], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85]`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6363236`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039f1`|`getListCurveCotTron`|`Dlcot [class:3024], KataCircle3D [class:3004], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85]`|`System.Collections.Generic.List`1 [class:217]<System.Tuple`2 [class:285]<CurveXYZ [class:293],r8>>`|`6363260`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039f2`|`getListPRaiCotUtil`|`Dlcot [class:3024]&, System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, i4, i4`|`System.Tuple`3 [class:805]<System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>,System.Tuple`2 [class:285]<CurveXYZ [class:293],r8>,System.Tuple`2 [class:285]<CurveXYZ [class:293],r8>>`|`6363284`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039f3`|`getListPRaiCot`|`Dlcot [class:3024]&, System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85]`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`6365588`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039f4`|`GetCurveCPhuongX`|`Dlcot [class:3024], System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, i4`|`System.Tuple`2 [class:285]<CurveXYZ [class:293],r8>`|`6365612`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039f5`|`GetCurveCPhuongY`|`Dlcot [class:3024], System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, i4`|`System.Tuple`2 [class:285]<CurveXYZ [class:293],r8>`|`6365636`|`4`|`8969ce0cdaa4a19c...`|
|`0x060039f6`|`Ve_bao_cot_mat_cat`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Line_diem [class:789]>&, Kata_Class_Lib_Revit.diem [class:85], System.Collections.Generic.List`1 [class:217]<System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>>`|`void`|`6365660`|`4`|`ae3c8b8d99a39542...`|
|`0x060039f7`|`Ve_bao_cot_tron`|`System.Collections.Generic.List`1 [class:217]<Autodesk.AutoCAD.DatabaseServices.Line [class:1673]>&, Kata_Class_Lib_Revit.diem [class:85]`|`void`|`6370728`|`4`|`ae3c8b8d99a39542...`|
|`0x060039f8`|`lay_du_lieu_coc_tu_block`|`System.Collections.Generic.List`1 [class:217]<Autodesk.AutoCAD.DatabaseServices.BlockReference [class:929]>`|`void`|`6374292`|`4`|`ae3c8b8d99a39542...`|
|`0x060039f9`|`cgsxnsqQnysxYIFY2cw6`|`()`|`bool`|`6376364`|`4`|`66ea1533cb3404e9...`|
|`0x060039fa`|`lbZcfrqQ5dwxqY6VOVPt`|`()`|`Kata_pro64_Cad2025.Mong_coc [class:2992]`|`6376384`|`4`|`8969ce0cdaa4a19c...`|

### [VM-206] Methods of `InfoMongCoc` (Token `0x020002ed`, Displayed: 7/7, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x060039fb`|`.ctor`|`()`|`void`|`12195616`|`4`|`ae3c8b8d99a39542...`|
|`0x060039fc`|`GetCotPhuIndex`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`i4`|`12196840`|`4`|`78d4d2062aa5373e...`|
|`0x060039fd`|`GetCotTronIndex`|`KataCircle3D [class:3004]`|`i4`|`12196864`|`4`|`78d4d2062aa5373e...`|
|`0x060039fe`|`GetSoThanhThepCot`|`Dlcot [class:3024]`|`i4`|`12196912`|`4`|`78d4d2062aa5373e...`|
|`0x060039ff`|`.cctor`|`()`|`void`|`12197364`|`177`|`b7e11b7d7463a94c...`|
|`0x06003a00`|`I2Ro3squLnonFXBlYVSF`|`()`|`bool`|`12197556`|`4`|`66ea1533cb3404e9...`|
|`0x06003a01`|`CcFeBZquTfZY5v1dBgSQ`|`()`|`InfoMongCoc [class:2996]`|`12197576`|`4`|`8969ce0cdaa4a19c...`|

### [VM-207] Methods of `InfoUserMongCoc` (Token `0x020002f1`, Displayed: 4/4, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003a10`|`.ctor`|`()`|`void`|`12199172`|`4`|`ae3c8b8d99a39542...`|
|`0x06003a11`|`.cctor`|`()`|`void`|`12199580`|`156`|`780ca3f72465e31a...`|
|`0x06003a12`|`DrW02nquljtf6cbSVEif`|`()`|`bool`|`12199748`|`4`|`66ea1533cb3404e9...`|
|`0x06003a13`|`M2LnSMquuWDtNMhqFevG`|`()`|`InfoUserMongCoc [class:3012]`|`12199768`|`4`|`8969ce0cdaa4a19c...`|

### [VM-208] Methods of `CoversMong` (Token `0x020002f2`, Displayed: 4/4, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003a14`|`.ctor`|`()`|`void`|`12199784`|`4`|`ae3c8b8d99a39542...`|
|`0x06003a15`|`.cctor`|`()`|`void`|`12200048`|`156`|`8e9b6781075920aa...`|
|`0x06003a16`|`duUhRQqupWgSB6pfjnWD`|`()`|`bool`|`12200216`|`4`|`66ea1533cb3404e9...`|
|`0x06003a17`|`bToaa4quwQSSSrtu0CoP`|`()`|`CoversMong [class:3016]`|`12200236`|`4`|`8969ce0cdaa4a19c...`|

### [VM-209] Methods of `InfoMongCocSave` (Token `0x020002f5`, Displayed: 4/4, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003a20`|`.ctor`|`()`|`void`|`12201748`|`4`|`ae3c8b8d99a39542...`|
|`0x06003a21`|`.cctor`|`()`|`void`|`12202068`|`156`|`314606fe0b19fca7...`|
|`0x06003a22`|`jpZFiBquxp16kX7PFjFK`|`()`|`bool`|`12202236`|`4`|`66ea1533cb3404e9...`|
|`0x06003a23`|`O8oI5TquDD8GHo1YcaEJ`|`()`|`InfoMongCocSave [class:3028]`|`12202256`|`4`|`8969ce0cdaa4a19c...`|

### [VM-210] Methods of `InfoMongCocSaveV2` (Token `0x0200031b`, Displayed: 4/4, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003b8f`|`.ctor`|`()`|`void`|`12366076`|`4`|`ae3c8b8d99a39542...`|
|`0x06003b90`|`.cctor`|`()`|`void`|`12366376`|`177`|`b058dd57be4ee22c...`|
|`0x06003b91`|`YAmf5NqpU5nFPqksCbb5`|`()`|`bool`|`12366568`|`4`|`66ea1533cb3404e9...`|
|`0x06003b92`|`m6vYc8qp8EXrxEJ2xBe4`|`()`|`InfoMongCocSaveV2 [class:3180]`|`12366588`|`4`|`8969ce0cdaa4a19c...`|

### [VM-211] Methods of `Kata_pro64_Cad2025.Rai_Mong_Module` (Token `0x02000335`, Displayed: 4/4, Evidence `ev_3bdf54e817a9b5840d6618afe7ca3edeaabd4e8c3021dcb361d608566084bf9d`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003c39`|`.cctor`|`()`|`void`|`6495996`|`310`|`115e0b2735008931...`|
|`0x06003c3a`|`RM`|`()`|`void`|`6496320`|`4`|`ae3c8b8d99a39542...`|
|`0x06003c3b`|`UAUO4VqkQS6SO9h1QRrO`|`()`|`bool`|`6496904`|`4`|`66ea1533cb3404e9...`|
|`0x06003c3c`|`ktI3I1qkkX5JPjN87Q8b`|`()`|`Kata_pro64_Cad2025.Rai_Mong_Module [class:3284]`|`6496924`|`4`|`8969ce0cdaa4a19c...`|

### [VM-212] Methods of `InfoRaiMong` (Token `0x02000336`, Displayed: 7/7, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003c3d`|`.ctor`|`()`|`void`|`12392028`|`4`|`ae3c8b8d99a39542...`|
|`0x06003c3e`|`TinhToanMong`|`()`|`void`|`12392736`|`4`|`ae3c8b8d99a39542...`|
|`0x06003c3f`|`GetListPSelect`|`()`|`System.Collections.Generic.List`1 [class:217]<System.Tuple`2 [class:285]<Kata_Class_Lib_Revit.diem [class:85],string>>`|`12393052`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003c40`|`GetPSelect`|`string`|`Kata_Class_Lib_Revit.diem [class:85]`|`12393964`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003c41`|`.cctor`|`()`|`void`|`12394556`|`177`|`64bd99abfd36fb6d...`|
|`0x06003c42`|`MvFaYSqpasbCqQ7w8hjo`|`()`|`bool`|`12394748`|`4`|`66ea1533cb3404e9...`|
|`0x06003c43`|`qTQuSBqpz15HBHtScL3P`|`()`|`InfoRaiMong [class:3288]`|`12394768`|`4`|`8969ce0cdaa4a19c...`|

### [VM-213] Methods of `RaiMongJig` (Token `0x02000337`, Displayed: 9/9, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003c44`|`.ctor`|`Polygon [class:89], Kata_Class_Lib_Revit.diem [class:85], System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, Polygon [class:89], System.Collections.Generic.List`1 [class:217]<Polygon [class:89]>, Polygon [class:89]`|`void`|`12394784`|`4`|`ae3c8b8d99a39542...`|
|`0x06003c45`|`Sampler`|`Autodesk.AutoCAD.EditorInput.JigPrompts [class:1861]`|`Autodesk.AutoCAD.EditorInput.SamplerStatus [valuetype:1857]`|`12395160`|`8`|`49e99688c2b312b9...`|
|`0x06003c46`|`KN80Ccj58Us`|`Autodesk.AutoCAD.GraphicsInterface.WorldDraw [class:1869], Polygon [class:89], string`|`void`|`12395544`|`4`|`ae3c8b8d99a39542...`|
|`0x06003c47`|`yG20C6OVtj1`|`Autodesk.AutoCAD.GraphicsInterface.WorldDraw [class:1869], Polygon [class:89], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85]`|`void`|`12395856`|`4`|`ae3c8b8d99a39542...`|
|`0x06003c48`|`WorldDraw`|`Autodesk.AutoCAD.GraphicsInterface.WorldDraw [class:1869]`|`bool`|`12396600`|`4`|`66ea1533cb3404e9...`|
|`0x06003c49`|`Dispose`|`()`|`void`|`12397728`|`4`|`ae3c8b8d99a39542...`|
|`0x06003c4a`|`.cctor`|`()`|`void`|`12397740`|`156`|`79a6d76c6507857d...`|
|`0x06003c4b`|`ixQlLFqw87UqXEHDrTBE`|`()`|`bool`|`12397908`|`4`|`66ea1533cb3404e9...`|
|`0x06003c4c`|`rAflxCqwE9UrkmhBX25f`|`()`|`RaiMongJig [class:3292]`|`12397928`|`4`|`8969ce0cdaa4a19c...`|

### [VM-214] Methods of `InfoRaiMongDon` (Token `0x02000338`, Displayed: 10/10, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003c4d`|`.ctor`|`()`|`void`|`12397944`|`4`|`ae3c8b8d99a39542...`|
|`0x06003c4e`|`TinhToanMong`|`Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85]`|`void`|`12398372`|`4`|`ae3c8b8d99a39542...`|
|`0x06003c4f`|`Translate`|`Kata_Class_Lib_Revit.diem [class:85]`|`void`|`12399644`|`4`|`ae3c8b8d99a39542...`|
|`0x06003c50`|`Rotate`|`Kata_Class_Lib_Revit.diem [class:85], r8`|`void`|`12400124`|`4`|`ae3c8b8d99a39542...`|
|`0x06003c51`|`GetListCurveDim`|`()`|`System.Collections.Generic.List`1 [class:217]<CurveXYZ [class:293]>`|`12400584`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003c52`|`GetListPSelect`|`()`|`System.Collections.Generic.List`1 [class:217]<System.Tuple`2 [class:285]<Kata_Class_Lib_Revit.diem [class:85],string>>`|`12402644`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003c53`|`GetPSelect`|`string`|`Kata_Class_Lib_Revit.diem [class:85]`|`12403592`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003c54`|`.cctor`|`()`|`void`|`12404164`|`156`|`fba869fd5d52bb0d...`|
|`0x06003c55`|`IfAV0pqwPcjkwHe7iQtH`|`()`|`bool`|`12404332`|`4`|`66ea1533cb3404e9...`|
|`0x06003c56`|`CXF8KPqw31pivOKY2caY`|`()`|`InfoRaiMongDon [class:3296]`|`12404352`|`4`|`8969ce0cdaa4a19c...`|

### [VM-215] Methods of `Kata_pro64_Cad2025.Ve_Mat_Bang_Mong_Module` (Token `0x02000370`, Displayed: 19/19, Evidence `ev_3bdf54e817a9b5840d6618afe7ca3edeaabd4e8c3021dcb361d608566084bf9d`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003ddd`|`.cctor`|`()`|`void`|`6668992`|`540`|`3e43fc202446f385...`|
|`0x06003dde`|`nqI8wkiUHU6`|`object`|`Kata_Class_Lib_Revit.diem [class:85]`|`6669544`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003ddf`|`Lek8w4PtMhH`|`object`|`Polygon [class:89]`|`6669784`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003de0`|`VV28wY5T9QK`|`object`|`InfoText [class:3572]`|`6669980`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003de1`|`fhM8woGgLs2`|`object`|`ThepBoTriMong [class:5308]`|`6670224`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003de2`|`GetViTriDatTen`|`i4`|`ViTriDatTen [valuetype:3564]`|`6670484`|`8`|`c41f727e2e00669d...`|
|`0x06003de3`|`Edit_Coc`|`()`|`System.Collections.Generic.List`1 [class:217]<CocChiuTai [class:3568]>`|`6670896`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003de4`|`UtO8w7Nr5Ar`|`object`|`Autodesk.AutoCAD.DatabaseServices.Polyline [class:1017]`|`6676260`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003de5`|`QCk8wFIQEBP`|`object`|`r8`|`6676988`|`4`|`f0ad83c607e59d90...`|
|`0x06003de6`|`cm18wvgwQjv`|`object`|`string`|`6677572`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003de7`|`EnsureBlockMongCoc`|`InfoCotCocGenMong [class:3584], Kata_Class_Lib_Revit.diem [class:85]&, r8&, Autodesk.AutoCAD.Geometry.Scale3d [valuetype:1037]&`|`Autodesk.AutoCAD.DatabaseServices.ObjectId [valuetype:221]`|`6678360`|`8`|`acfcb9c2d49b3dc3...`|
|`0x06003de8`|`InsertBlockMongCoc`|`Autodesk.AutoCAD.DatabaseServices.ObjectId [valuetype:221], Kata_Class_Lib_Revit.diem [class:85], r8, Autodesk.AutoCAD.Geometry.Scale3d [valuetype:1037], string, i4`|`object`|`6681868`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003de9`|`PhaMong`|`()`|`void`|`6683284`|`4`|`ae3c8b8d99a39542...`|
|`0x06003dea`|`PhaBlockMong`|`object, bool`|`System.Collections.Generic.List`1 [class:217]<object>`|`6685328`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003deb`|`WQa8w1WlmGu`|`object`|`string`|`6690720`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003dec`|`UpdateDaiMongBaoCoc`|`object`|`bool`|`6691548`|`4`|`66ea1533cb3404e9...`|
|`0x06003ded`|`VMBM`|`()`|`void`|`6703292`|`4`|`ae3c8b8d99a39542...`|
|`0x06003dee`|`AKXNoyq4kVb9H8WLxAQJ`|`()`|`bool`|`6703896`|`4`|`66ea1533cb3404e9...`|
|`0x06003def`|`Vy6p6pq44dnU8Vsa8pku`|`()`|`Kata_pro64_Cad2025.Ve_Mat_Bang_Mong_Module [class:3520]`|`6703916`|`4`|`8969ce0cdaa4a19c...`|

### [VM-216] Methods of `InfoVeMatBangMong` (Token `0x02000372`, Displayed: 12/12, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003df0`|`.ctor`|`()`|`void`|`12508932`|`4`|`ae3c8b8d99a39542...`|
|`0x06003df1`|`CloneForUndo`|`()`|`InfoVeMatBangMong [class:3528]`|`12509444`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003df2`|`TinhToanMatBangMong`|`()`|`void`|`12509960`|`4`|`ae3c8b8d99a39542...`|
|`0x06003df3`|`GetChieuDayMongByChiuTaiCoc`|`CocChiuTai [class:3568]`|`r8`|`12513728`|`4`|`f0ad83c607e59d90...`|
|`0x06003df4`|`GetBBMax`|`()`|`Polygon [class:89]`|`12514724`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003df5`|`ReOrder`|`bool`|`void`|`12515100`|`4`|`ae3c8b8d99a39542...`|
|`0x06003df6`|`ReAssignListCocByListCocInput`|`()`|`void`|`12515660`|`4`|`ae3c8b8d99a39542...`|
|`0x06003df7`|`GanId`|`()`|`void`|`12516344`|`4`|`ae3c8b8d99a39542...`|
|`0x06003df8`|`GanTenMong`|`()`|`void`|`12516664`|`4`|`ae3c8b8d99a39542...`|
|`0x06003df9`|`.cctor`|`()`|`void`|`12517300`|`156`|`a32705ab880d0dba...`|
|`0x06003dfa`|`XN1mJKq2kZre7ZZ0fGRK`|`()`|`bool`|`12517468`|`4`|`66ea1533cb3404e9...`|
|`0x06003dfb`|`PcXJw8q244spWW1TtIRL`|`()`|`InfoVeMatBangMong [class:3528]`|`12517488`|`4`|`8969ce0cdaa4a19c...`|

### [VM-217] Methods of `InfoMBMongBang` (Token `0x02000377`, Displayed: 15/15, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003e15`|`.ctor`|`()`|`void`|`12518052`|`4`|`ae3c8b8d99a39542...`|
|`0x06003e16`|`GetJsonStringMong`|`()`|`string`|`12518540`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003e17`|`wLK0QxWdtbc`|`Kata_Class_Lib_Revit.Info_Beam3D [class:481]&`|`void`|`12518568`|`4`|`ae3c8b8d99a39542...`|
|`0x06003e18`|`g0A0QDgafcK`|`Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85]`|`r8`|`12519700`|`4`|`f0ad83c607e59d90...`|
|`0x06003e19`|`JZn0QB1Uyns`|`Polygon [class:89]&, r8, r8, r8, r8`|`void`|`12519756`|`4`|`ae3c8b8d99a39542...`|
|`0x06003e1a`|`j2L0QOmfcl5`|`System.Collections.Generic.List`1 [class:217]<Polygon [class:89]>`|`void`|`12520520`|`4`|`ae3c8b8d99a39542...`|
|`0x06003e1b`|`tZ90QVeyJuB`|`()`|`void`|`12521800`|`4`|`ae3c8b8d99a39542...`|
|`0x06003e1c`|`X7a0QadSMYJ`|`CurveXYZ [class:293]`|`r8`|`12521956`|`4`|`f0ad83c607e59d90...`|
|`0x06003e1d`|`TinhToanBaoLanDauSauKhiQuet`|`string, string, string, string`|`void`|`12522092`|`4`|`ae3c8b8d99a39542...`|
|`0x06003e1e`|`RegenMBMongBang`|`()`|`void`|`12526656`|`4`|`ae3c8b8d99a39542...`|
|`0x06003e1f`|`AssignListLengthToListDam`|`()`|`void`|`12528688`|`4`|`ae3c8b8d99a39542...`|
|`0x06003e20`|`GetBBMax`|`()`|`Polygon [class:89]`|`12529424`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003e21`|`.cctor`|`()`|`void`|`12529788`|`177`|`b215d173e771adf6...`|
|`0x06003e22`|`A4Zckiq29AhbEideAZiD`|`()`|`bool`|`12529980`|`4`|`66ea1533cb3404e9...`|
|`0x06003e23`|`xmU3L5q2KbUUjjQMiP8Z`|`()`|`InfoMBMongBang [class:3548]`|`12530000`|`4`|`8969ce0cdaa4a19c...`|

### [VM-218] Methods of `CocChiuTai` (Token `0x0200037c`, Displayed: 7/7, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003e38`|`.ctor`|`()`|`void`|`12530016`|`4`|`ae3c8b8d99a39542...`|
|`0x06003e39`|`Clone`|`Kata_Class_Lib_Revit.diem [class:85]`|`CocChiuTai [class:3568]`|`12530468`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003e3a`|`CloneForUndo`|`Kata_Class_Lib_Revit.diem [class:85]`|`CocChiuTai [class:3568]`|`12530800`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003e3b`|`GetJsonString`|`()`|`string`|`12531128`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003e3c`|`.cctor`|`()`|`void`|`12531256`|`177`|`4c28ed4933dbc5e4...`|
|`0x06003e3d`|`zElbKBq2Rhf1XvrHEpKn`|`()`|`bool`|`12531448`|`4`|`66ea1533cb3404e9...`|
|`0x06003e3e`|`iLyDnFq2gDxOHCZ085HX`|`()`|`CocChiuTai [class:3568]`|`12531468`|`4`|`8969ce0cdaa4a19c...`|

### [VM-219] Methods of `MongJsonDto` (Token `0x0200037e`, Displayed: 4/4, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003e43`|`.ctor`|`()`|`void`|`12531812`|`4`|`ae3c8b8d99a39542...`|
|`0x06003e44`|`.cctor`|`()`|`void`|`12532024`|`156`|`6f980b89d676bac1...`|
|`0x06003e45`|`NFxGBaq2T4P67pbyZJjN`|`()`|`bool`|`12532192`|`4`|`66ea1533cb3404e9...`|
|`0x06003e46`|`pctFGMq2fs9QdvdcWHJ5`|`()`|`MongJsonDto [class:3576]`|`12532212`|`4`|`8969ce0cdaa4a19c...`|

### [VM-220] Methods of `MongBangJsonDto` (Token `0x0200037f`, Displayed: 4/4, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003e47`|`.ctor`|`()`|`void`|`12532228`|`4`|`ae3c8b8d99a39542...`|
|`0x06003e48`|`.cctor`|`()`|`void`|`12532352`|`177`|`a9e88c04b91ce179...`|
|`0x06003e49`|`isfZMiq26oX4QbDM426A`|`()`|`bool`|`12532544`|`4`|`66ea1533cb3404e9...`|
|`0x06003e4a`|`n5J3unq2WT4jWuuZahm9`|`()`|`MongBangJsonDto [class:3580]`|`12532564`|`4`|`8969ce0cdaa4a19c...`|

### [VM-221] Methods of `InfoCotCocGenMong` (Token `0x02000380`, Displayed: 41/41, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003e4b`|`IsCoNhieuHon1LoaiCoc`|`()`|`object`|`12532580`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003e4c`|`CloneForUndo`|`()`|`InfoCotCocGenMong [class:3584]`|`12532676`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003e4d`|`GetJsonStringMong`|`()`|`string`|`12533836`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003e4e`|`.ctor`|`()`|`void`|`12534004`|`4`|`ae3c8b8d99a39542...`|
|`0x06003e4f`|`.ctor`|`Polygon [class:89], string, System.Collections.Generic.List`1 [class:217]<InfoText [class:3572]>`|`void`|`12534924`|`4`|`ae3c8b8d99a39542...`|
|`0x06003e50`|`RegenCenter`|`()`|`void`|`12536072`|`4`|`ae3c8b8d99a39542...`|
|`0x06003e51`|`IsDifferent`|`InfoCotCocGenMong [class:3584]`|`bool`|`12536972`|`4`|`66ea1533cb3404e9...`|
|`0x06003e52`|`uAa0QzaHL5K`|`i4, Kata_Class_Lib_Revit.diem [class:85], CocChiuTai [class:3568], string`|`System.Tuple`2 [class:285]<Polygon [class:89],System.Collections.Generic.List`1 [class:217]<CocChiuTai [class:3568]>>`|`12537368`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003e53`|`Pyp0kUgXdje`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, r8`|`Polygon [class:89]`|`12546212`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003e54`|`jQA0k8x8ZwF`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`System.Collections.Generic.List`1 [class:217]<CocChiuTai [class:3568]>`|`12547244`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003e55`|`GetListMauMongText`|`i4`|`System.Collections.Generic.List`1 [class:217]<string>`|`12547788`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003e56`|`MGU0kEycMN8`|`System.Collections.Generic.List`1 [class:217]<i4>`|`string`|`12548356`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003e57`|`Bpf0k0rOKvu`|`string`|`System.Collections.Generic.List`1 [class:217]<i4>`|`12548676`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003e58`|`isQ0kPbPesU`|`System.Collections.Generic.List`1 [class:217]<i4>, Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], r8, r8, r8`|`System.Tuple`2 [class:285]<Polygon [class:89],System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>>`|`12549336`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003e59`|`ListPlMongCanAssign`|`i4, string`|`System.Collections.Generic.List`1 [class:217]<System.Tuple`2 [class:285]<Polygon [class:89],System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>>>`|`12551144`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003e5a`|`VwU0k31al2b`|`i4`|`System.Collections.Generic.List`1 [class:217]<System.Collections.Generic.List`1 [class:217]<i4>>`|`12552128`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003e5b`|`GUl0kqGRRX2`|`System.Collections.Generic.List`1 [class:217]<i4>`|`i4`|`12553288`|`4`|`78d4d2062aa5373e...`|
|`0x06003e5c`|`zWI0kAtasEh`|`System.Collections.Generic.List`1 [class:217]<i4>`|`System.Collections.Generic.List`1 [class:217]<i4>`|`12553824`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003e5d`|`hje0kC5kJdW`|`System.Collections.Generic.List`1 [class:217]<i4>`|`bool`|`12556372`|`4`|`66ea1533cb3404e9...`|
|`0x06003e5e`|`IsCotNamNgoaiMong`|`()`|`bool`|`12556928`|`4`|`66ea1533cb3404e9...`|
|`0x06003e5f`|`GetListDiemTatCaCot`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`12557240`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003e60`|`GetListDiemCotNamNgoaiMong`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`12557808`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003e61`|`AssignPlMong`|`i4, Kata_Class_Lib_Revit.diem [class:85], Polygon [class:89], Polygon [class:89], string`|`void`|`12558388`|`4`|`ae3c8b8d99a39542...`|
|`0x06003e62`|`RaiDeuCoc`|`System.Collections.Generic.List`1 [class:217]<CocChiuTai [class:3568]>, bool, bool`|`void`|`12560196`|`4`|`ae3c8b8d99a39542...`|
|`0x06003e63`|`TMb0km2IhiW`|`System.Collections.Generic.HashSet`1 [class:385]<System.Tuple`2 [class:285]<i4,i4>>, System.Collections.Generic.HashSet`1 [class:385]<System.Tuple`2 [class:285]<i4,i4>>, System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, r8, Kata_Class_Lib_Revit.diem [class:85]`|`System.Tuple`2 [class:285]<i4,i4>`|`12565088`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003e64`|`XoaCacCocLeLoiNhat`|`i4`|`void`|`12566384`|`4`|`ae3c8b8d99a39542...`|
|`0x06003e65`|`ThemCacCocThichHop`|`i4`|`void`|`12568232`|`4`|`ae3c8b8d99a39542...`|
|`0x06003e66`|`WKt0kQyUaS0`|`System.Tuple`2 [class:285]<i4,i4>, System.Collections.Generic.HashSet`1 [class:385]<System.Tuple`2 [class:285]<i4,i4>>`|`System.Tuple`2 [class:285]<i4,i4>`|`12570752`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003e67`|`Dva0kkQDEt6`|`System.Tuple`2 [class:285]<i4,i4>, System.Collections.Generic.HashSet`1 [class:385]<System.Tuple`2 [class:285]<i4,i4>>, System.Tuple`2 [class:285]<i4,i4>`|`i4`|`12571684`|`4`|`78d4d2062aa5373e...`|
|`0x06003e68`|`yMZ0k4coYJO`|`System.Collections.Generic.HashSet`1 [class:385]<System.Tuple`2 [class:285]<i4,i4>>, Kata_Class_Lib_Revit.diem [class:85], r8, CocChiuTai [class:3568], Kata_Class_Lib_Revit.diem [class:85]`|`System.Collections.Generic.List`1 [class:217]<CocChiuTai [class:3568]>`|`12572392`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003e69`|`RegenPlMongByConcaveListCoc`|`r8, System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`void`|`12573744`|`4`|`ae3c8b8d99a39542...`|
|`0x06003e6a`|`RegenPlMongByOuterListCoc`|`r8, System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`void`|`12574952`|`4`|`ae3c8b8d99a39542...`|
|`0x06003e6b`|`Translate`|`Kata_Class_Lib_Revit.diem [class:85]`|`void`|`12576308`|`4`|`ae3c8b8d99a39542...`|
|`0x06003e6c`|`Rotate`|`r8`|`void`|`12576704`|`4`|`ae3c8b8d99a39542...`|
|`0x06003e6d`|`AlignToRanh`|`Polygon [class:89], Polygon [class:89]`|`Kata_Class_Lib_Revit.diem [class:85]`|`12577072`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003e6e`|`_Lambda$__50-0`|`Polygon [class:89]`|`bool`|`12578628`|`4`|`66ea1533cb3404e9...`|
|`0x06003e6f`|`_Lambda$__52-0`|`Kata_Class_Lib_Revit.diem [class:85]`|`bool`|`12578652`|`4`|`66ea1533cb3404e9...`|
|`0x06003e70`|`_Lambda$__52-1`|`Kata_Class_Lib_Revit.diem [class:85]`|`bool`|`12578688`|`4`|`66ea1533cb3404e9...`|
|`0x06003e71`|`.cctor`|`()`|`void`|`12578724`|`177`|`10f9118c697107a3...`|
|`0x06003e72`|`khpa3Pq2t86Xctuhm0Ik`|`()`|`bool`|`12578916`|`4`|`66ea1533cb3404e9...`|
|`0x06003e73`|`Eho6Npq2dxqQbYKAxiav`|`()`|`InfoCotCocGenMong [class:3584]`|`12578936`|`4`|`8969ce0cdaa4a19c...`|

### [VM-222] Methods of `InfoMBMongDon` (Token `0x02000388`, Displayed: 7/7, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003ea2`|`.ctor`|`()`|`void`|`12578952`|`4`|`ae3c8b8d99a39542...`|
|`0x06003ea3`|`TinhToanMatBangMong`|`bool, string, string, string, string`|`void`|`12579376`|`4`|`ae3c8b8d99a39542...`|
|`0x06003ea4`|`GetBBMax`|`()`|`Polygon [class:89]`|`12581120`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003ea5`|`GanTenMong`|`()`|`void`|`12581460`|`4`|`ae3c8b8d99a39542...`|
|`0x06003ea6`|`.cctor`|`()`|`void`|`12582140`|`156`|`842bc6a7ecf19a57...`|
|`0x06003ea7`|`cj82Jfq2rNatnicXJEJd`|`()`|`bool`|`12582308`|`4`|`66ea1533cb3404e9...`|
|`0x06003ea8`|`OPE7Ptq2G68xWPHeGBBQ`|`()`|`InfoMBMongDon [class:3616]`|`12582328`|`4`|`8969ce0cdaa4a19c...`|

### [VM-223] Methods of `CotChiuTaiMongDon` (Token `0x0200038a`, Displayed: 8/8, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003eb0`|`.ctor`|`()`|`void`|`12582344`|`4`|`ae3c8b8d99a39542...`|
|`0x06003eb1`|`.ctor`|`Polygon [class:89], string`|`void`|`12583060`|`4`|`ae3c8b8d99a39542...`|
|`0x06003eb2`|`RegenInfoMong`|`()`|`void`|`12584068`|`4`|`ae3c8b8d99a39542...`|
|`0x06003eb3`|`GetDoDaiCanhMongTxt`|`()`|`string`|`12585364`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003eb4`|`IsDifferent`|`CotChiuTaiMongDon [class:3624]`|`bool`|`12585512`|`4`|`66ea1533cb3404e9...`|
|`0x06003eb5`|`.cctor`|`()`|`void`|`12585620`|`177`|`d0c1621e1885925f...`|
|`0x06003eb6`|`rgKTFoq2u2tIuRPXpeHV`|`()`|`bool`|`12585812`|`4`|`66ea1533cb3404e9...`|
|`0x06003eb7`|`kr0QRIq2HcgQY3jnyi9r`|`()`|`CotChiuTaiMongDon [class:3624]`|`12585832`|`4`|`8969ce0cdaa4a19c...`|

### [VM-224] Methods of `Form_Update_Dai_Mong_Bao_Coc` (Token `0x0200038c`, Displayed: 10/10, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003eb8`|`get_TenMong`|`()`|`string`|`12585848`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003eb9`|`get_HMongText`|`()`|`string`|`12585884`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003eba`|`get_KcDenMepDaiText`|`()`|`string`|`12585920`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003ebb`|`get_KieuTao`|`()`|`KieuTaoDaiMongBaoCoc [valuetype:3628]`|`12585956`|`8`|`6cb8e0e2c07ae864...`|
|`0x06003ebc`|`.ctor`|`string, string, string, bool, bool`|`void`|`12586292`|`4`|`ae3c8b8d99a39542...`|
|`0x06003ebd`|`kmU0kYIJXP8`|`string, KieuTaoDaiMongBaoCoc [valuetype:3628]`|`void`|`12589904`|`4`|`ae3c8b8d99a39542...`|
|`0x06003ebe`|`iYj0koA13Re`|`object, System.EventArgs [class:113]`|`void`|`12590072`|`4`|`ae3c8b8d99a39542...`|
|`0x06003ebf`|`.cctor`|`()`|`void`|`12590996`|`177`|`49fef21d346f9600...`|
|`0x06003ec0`|`xQllChq2iQNvhoOHAUk5`|`()`|`bool`|`12591188`|`4`|`66ea1533cb3404e9...`|
|`0x06003ec1`|`nPXQT8q2niw6bHAehLyN`|`()`|`Form_Update_Dai_Mong_Bao_Coc [class:3632]`|`12591208`|`4`|`8969ce0cdaa4a19c...`|

### [VM-225] Methods of `Kata_pro64_Cad2025.Form_VMBM_Light` (Token `0x02000390`, Displayed: 53/53, Evidence `ev_549bfe33ecd78257ce27570bdd19331f304e770621ccd33a14059b374ac982e3`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003ed7`|`.ctor`|`()`|`void`|`6703932`|`4`|`ae3c8b8d99a39542...`|
|`0x06003ed8`|`FwU8wMAZ78N`|`object, System.EventArgs [class:113]`|`void`|`6714720`|`4`|`ae3c8b8d99a39542...`|
|`0x06003ed9`|`j9n8w9CJyvU`|`System.Windows.Forms.Control [class:97], i4`|`void`|`6715416`|`4`|`ae3c8b8d99a39542...`|
|`0x06003eda`|`UGm8wKaYy76`|`object`|`void`|`6715860`|`4`|`ae3c8b8d99a39542...`|
|`0x06003edb`|`t2o8wNZ85aa`|`object, bool`|`void`|`6716532`|`4`|`ae3c8b8d99a39542...`|
|`0x06003edc`|`fk58wbkFM9k`|`object, System.EventArgs [class:113]`|`void`|`6716932`|`4`|`ae3c8b8d99a39542...`|
|`0x06003edd`|`srL8wII04x4`|`object, System.EventArgs [class:113]`|`void`|`6718988`|`4`|`ae3c8b8d99a39542...`|
|`0x06003ede`|`maV8wyqEYMi`|`object, System.EventArgs [class:113]`|`void`|`6720276`|`4`|`ae3c8b8d99a39542...`|
|`0x06003edf`|`Ond8wRKeppG`|`object, System.EventArgs [class:113]`|`void`|`6723436`|`4`|`ae3c8b8d99a39542...`|
|`0x06003ee0`|`gcP8wgjoCn8`|`object, System.Windows.Forms.ItemCheckEventArgs [class:2017]`|`void`|`6723928`|`4`|`ae3c8b8d99a39542...`|
|`0x06003ee1`|`ADs8whDDhf1`|`object, System.EventArgs [class:113]`|`void`|`6724180`|`4`|`ae3c8b8d99a39542...`|
|`0x06003ee2`|`FMi8wZK9iSw`|`()`|`void`|`6724956`|`4`|`ae3c8b8d99a39542...`|
|`0x06003ee3`|`bOZ8wewwJXV`|`object, System.EventArgs [class:113]`|`void`|`6725500`|`4`|`ae3c8b8d99a39542...`|
|`0x06003ee4`|`eDW8wLf41yG`|`bool`|`void`|`6725924`|`4`|`ae3c8b8d99a39542...`|
|`0x06003ee5`|`fRq8wT8mrc5`|`()`|`void`|`6726944`|`4`|`ae3c8b8d99a39542...`|
|`0x06003ee6`|`Number`|`object, object`|`r8`|`6728172`|`4`|`f0ad83c607e59d90...`|
|`0x06003ee7`|`b0c8wfuLHry`|`object, System.EventArgs [class:113]`|`void`|`6728440`|`4`|`ae3c8b8d99a39542...`|
|`0x06003ee8`|`aIS8wc13wUc`|`bool`|`void`|`6728744`|`4`|`ae3c8b8d99a39542...`|
|`0x06003ee9`|`MOS8w60a710`|`object, System.Windows.Forms.DataGridViewCellEventArgs [class:841]`|`void`|`6729068`|`4`|`ae3c8b8d99a39542...`|
|`0x06003eea`|`V2e8wW3UecB`|`object, object, bool`|`void`|`6729672`|`4`|`ae3c8b8d99a39542...`|
|`0x06003eeb`|`yfK8wsvP4cS`|`()`|`void`|`6730584`|`4`|`ae3c8b8d99a39542...`|
|`0x06003eec`|`q3O8wtdyeEU`|`()`|`void`|`6734012`|`4`|`ae3c8b8d99a39542...`|
|`0x06003eed`|`pXl8wdTxOsN`|`object, System.EventArgs [class:113]`|`void`|`6737020`|`4`|`ae3c8b8d99a39542...`|
|`0x06003eee`|`n8P8wJby2jD`|`object, System.EventArgs [class:113]`|`void`|`6737940`|`4`|`ae3c8b8d99a39542...`|
|`0x06003eef`|`xSK8wra5cvd`|`string, string`|`void`|`6739556`|`4`|`ae3c8b8d99a39542...`|
|`0x06003ef0`|`ajV8wGtpMrh`|`object, System.EventArgs [class:113]`|`void`|`6741068`|`4`|`ae3c8b8d99a39542...`|
|`0x06003ef1`|`Apply`|`object, System.EventArgs [class:113]`|`void`|`6742056`|`4`|`ae3c8b8d99a39542...`|
|`0x06003ef2`|`kgT8wl2LEO6`|`object, System.EventArgs [class:113]`|`void`|`6743560`|`4`|`ae3c8b8d99a39542...`|
|`0x06003ef3`|`uhd8wuxcRcy`|`string`|`void`|`6743884`|`4`|`ae3c8b8d99a39542...`|
|`0x06003ef4`|`yZ88wHEy3iV`|`System.Exception [class:121]`|`void`|`6744312`|`4`|`ae3c8b8d99a39542...`|
|`0x06003ef5`|`_Lambda$__36-0`|`object, System.Windows.Forms.TabControlCancelEventArgs [class:2021]`|`void`|`6744520`|`4`|`ae3c8b8d99a39542...`|
|`0x06003ef6`|`_Lambda$__R36-1`|`object, System.EventArgs [class:113]`|`void`|`6744792`|`4`|`ae3c8b8d99a39542...`|
|`0x06003ef7`|`_Lambda$__36-1`|`()`|`void`|`6744808`|`4`|`ae3c8b8d99a39542...`|
|`0x06003ef8`|`_Lambda$__R36-2`|`object, System.EventArgs [class:113]`|`void`|`6744936`|`4`|`ae3c8b8d99a39542...`|
|`0x06003ef9`|`_Lambda$__36-2`|`()`|`void`|`6744952`|`4`|`ae3c8b8d99a39542...`|
|`0x06003efa`|`_Lambda$__36-3`|`object, System.Windows.Forms.DataGridViewCellEventArgs [class:841]`|`void`|`6745148`|`4`|`ae3c8b8d99a39542...`|
|`0x06003efb`|`_Lambda$__R36-3`|`object, System.EventArgs [class:113]`|`void`|`6745704`|`4`|`ae3c8b8d99a39542...`|
|`0x06003efc`|`_Lambda$__36-5`|`()`|`void`|`6745720`|`4`|`ae3c8b8d99a39542...`|
|`0x06003efd`|`_Lambda$__36-10`|`object, System.Windows.Forms.DataGridViewCellEventArgs [class:841]`|`void`|`6745976`|`4`|`ae3c8b8d99a39542...`|
|`0x06003efe`|`_Lambda$__36-11`|`object, System.EventArgs [class:113]`|`void`|`6746096`|`4`|`ae3c8b8d99a39542...`|
|`0x06003eff`|`_Lambda$__R36-5`|`object, System.EventArgs [class:113]`|`void`|`6746312`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f00`|`_Lambda$__36-12`|`()`|`void`|`6746328`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f01`|`_Lambda$__R36-6`|`object, System.EventArgs [class:113]`|`void`|`6746504`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f02`|`_Lambda$__36-13`|`()`|`void`|`6746520`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f03`|`_Lambda$__36-15`|`object, System.Windows.Forms.FormClosingEventArgs [class:301]`|`void`|`6746756`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f04`|`_Lambda$__43-1`|`string`|`bool`|`6747172`|`4`|`66ea1533cb3404e9...`|
|`0x06003f05`|`_Lambda$__45-0`|`()`|`void`|`6747212`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f06`|`_Lambda$__52-1`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], string, CSiAPIv1.cSapModel [class:2025]`|`void`|`6747500`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f07`|`_Lambda$__52-3`|`string`|`void`|`6748192`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f08`|`_Lambda$__63-1`|`string`|`void`|`6748412`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f09`|`.cctor`|`()`|`void`|`6748632`|`156`|`5aeef565693cb292...`|
|`0x06003f0a`|`rjhActq4ouZ2mCT2vg1M`|`()`|`bool`|`6748800`|`4`|`66ea1533cb3404e9...`|
|`0x06003f0b`|`E0GdjIq47pkjO1OaUXHo`|`()`|`Kata_pro64_Cad2025.Form_VMBM_Light [class:3648]`|`6748820`|`4`|`8969ce0cdaa4a19c...`|

### [VM-226] Methods of `Kata_pro64_Cad2025.VmbmLightPreview` (Token `0x0200039f`, Displayed: 30/30, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003f64`|`get_Session`|`()`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`6748836`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003f65`|`set_Session`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`void`|`6748852`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f66`|`get_SelectedBeamId`|`()`|`string`|`6748956`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003f67`|`set_SelectedBeamId`|`string`|`void`|`6748972`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f68`|`get_ListSelectedBeamIds`|`()`|`System.Collections.Generic.List`1 [class:217]<string>`|`6749076`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003f69`|`set_ListSelectedBeamIds`|`System.Collections.Generic.List`1 [class:217]<string>`|`void`|`6749092`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f6a`|`get_PreferBeamSelection`|`()`|`bool`|`6749196`|`4`|`66ea1533cb3404e9...`|
|`0x06003f6b`|`set_PreferBeamSelection`|`bool`|`void`|`6749212`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f6c`|`get_EnableInlineNames`|`()`|`bool`|`6749316`|`4`|`66ea1533cb3404e9...`|
|`0x06003f6d`|`set_EnableInlineNames`|`bool`|`void`|`6749332`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f6e`|`get_ListSelectedId`|`()`|`System.Collections.Generic.List`1 [class:217]<string>`|`6749436`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003f6f`|`set_ListSelectedId`|`System.Collections.Generic.List`1 [class:217]<string>`|`void`|`6749452`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f70`|`add_CapPicked`|`CapPickedEventHandler [class:3712]`|`void`|`6749556`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f71`|`remove_CapPicked`|`CapPickedEventHandler [class:3712]`|`void`|`6749820`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f72`|`add_BeamPicked`|`BeamPickedEventHandler [class:3716]`|`void`|`6750124`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f73`|`remove_BeamPicked`|`BeamPickedEventHandler [class:3716]`|`void`|`6750388`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f74`|`add_CapNameEdited`|`CapNameEditedEventHandler [class:3720]`|`void`|`6750652`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f75`|`remove_CapNameEdited`|`CapNameEditedEventHandler [class:3720]`|`void`|`6750936`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f76`|`OnMouseDown`|`System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`6751200`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f77`|`bPR8299KYte`|`System.Drawing.Point [valuetype:173]`|`bool`|`6753528`|`4`|`66ea1533cb3404e9...`|
|`0x06003f78`|`.ctor`|`()`|`void`|`6754960`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f79`|`GsX82KGbtO7`|`string, System.Drawing.RectangleF [valuetype:897]`|`void`|`6755560`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f7a`|`dTH82N4wBiE`|`bool`|`void`|`6756272`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f7b`|`OnPaint`|`System.Windows.Forms.PaintEventArgs [class:685]`|`void`|`6756672`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f7c`|`_Lambda$__46-0`|`object, System.Windows.Forms.KeyEventArgs [class:637]`|`void`|`6763572`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f7d`|`_Lambda$__R46-1`|`object, System.EventArgs [class:113]`|`void`|`6763868`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f7e`|`_Lambda$__46-1`|`()`|`void`|`6763884`|`4`|`ae3c8b8d99a39542...`|
|`0x06003f7f`|`.cctor`|`()`|`void`|`6763988`|`177`|`bcd9f08e14231646...`|
|`0x06003f80`|`NCLq4Nq4vFTXoqatRoEW`|`()`|`bool`|`6764180`|`4`|`66ea1533cb3404e9...`|
|`0x06003f81`|`u2KScZq41xhOX6kWPk2Z`|`()`|`Kata_pro64_Cad2025.VmbmLightPreview [class:3708]`|`6764200`|`4`|`8969ce0cdaa4a19c...`|

### [VM-227] Methods of `Kata_pro64_Cad2025.VmbmLightOptionsForm` (Token `0x020003a7`, Displayed: 4/4, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003fb3`|`.ctor`|`Kata_pro64_Cad2025.VmbmLightOptions [class:4144], System.Collections.Generic.IEnumerable`1 [class:321]<string>, System.Collections.Generic.IEnumerable`1 [class:321]<string>`|`void`|`6764216`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fb4`|`.cctor`|`()`|`void`|`6771252`|`177`|`2415553ed15ae1e2...`|
|`0x06003fb5`|`XddS72q49JCI1KrHuVRV`|`()`|`bool`|`6771444`|`4`|`66ea1533cb3404e9...`|
|`0x06003fb6`|`fq0ZoPq4KZhe2lIZupHU`|`()`|`Kata_pro64_Cad2025.VmbmLightOptionsForm [class:3740]`|`6771464`|`4`|`8969ce0cdaa4a19c...`|

### [VM-228] Methods of `Kata_pro64_Cad2025.VmbmLightShallowView` (Token `0x020003aa`, Displayed: 35/35, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003fc4`|`get_Session`|`()`|`Kata_pro64_Cad2025.VmbmLightShallowSession [class:4336]`|`6771480`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003fc5`|`set_Session`|`Kata_pro64_Cad2025.VmbmLightShallowSession [class:4336]`|`void`|`6771496`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fc6`|`.ctor`|`Kata_pro64_Cad2025.VmbmLightShallowKind [valuetype:4324]`|`void`|`6771600`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fc7`|`kjt82tpd029`|`string, System.Action [class:213], bool, i4`|`System.Windows.Forms.Button [class:161]`|`6776408`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003fc8`|`hwl82dDsrfU`|`System.Action [class:213]`|`void`|`6776824`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fc9`|`Bt482JaNACm`|`System.Action [class:213]`|`void`|`6777344`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fca`|`yMy82ro3pXO`|`()`|`void`|`6777916`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fcb`|`qjd82GBSPX2`|`bool`|`void`|`6778032`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fcc`|`J4v82lkBKHQ`|`()`|`void`|`6778380`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fcd`|`zUK82u7I43v`|`()`|`void`|`6778496`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fce`|`Khk82HgAPXQ`|`System.Action [class:213]`|`void`|`6778784`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fcf`|`Th782pmkvEq`|`object, System.Windows.Forms.PropertyValueChangedEventArgs [class:2069]`|`void`|`6779528`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fd0`|`gPS82wbZihi`|`object, System.Windows.Forms.DataGridViewCellEventArgs [class:841]`|`void`|`6779728`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fd1`|`Undo`|`()`|`void`|`6780168`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fd2`|`Redo`|`()`|`void`|`6780404`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fd3`|`vCN822sYhZ0`|`()`|`void`|`6780660`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fd4`|`nh182XGYcaZ`|`object, System.EventArgs [class:113]`|`void`|`6782472`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fd5`|`nTh82iVTyGV`|`string, bool`|`void`|`6782916`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fd6`|`_Lambda$__14-0`|`()`|`void`|`6783752`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fd7`|`_Lambda$__14-1`|`()`|`void`|`6783768`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fd8`|`_Lambda$__14-2`|`()`|`void`|`6783784`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fd9`|`_Lambda$__14-3`|`()`|`void`|`6783892`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fda`|`_Lambda$__14-4`|`()`|`void`|`6784004`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fdb`|`_Lambda$__14-5`|`()`|`void`|`6784116`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fdc`|`_Lambda$__14-6`|`()`|`void`|`6784232`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fdd`|`_Lambda$__14-7`|`()`|`void`|`6784340`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fde`|`_Lambda$__14-8`|`()`|`void`|`6784444`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fdf`|`_Lambda$__14-9`|`string, string, r8`|`void`|`6784548`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fe0`|`_Lambda$__18-0`|`()`|`void`|`6784808`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fe1`|`_Lambda$__20-0`|`()`|`void`|`6785124`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fe2`|`_Lambda$__23-0`|`()`|`void`|`6785436`|`4`|`ae3c8b8d99a39542...`|
|`0x06003fe3`|`_Lambda$__28-2`|`Kata_pro64_Cad2025.VmbmLightFootingInput [class:4328]`|`object`|`6786052`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003fe4`|`.cctor`|`()`|`void`|`6786080`|`177`|`243031da3d429170...`|
|`0x06003fe5`|`z1DrITq4b8RadpO16eeG`|`()`|`bool`|`6786272`|`4`|`66ea1533cb3404e9...`|
|`0x06003fe6`|`A0iaiNq4I9GCbGMVmbKA`|`()`|`Kata_pro64_Cad2025.VmbmLightShallowView [class:3752]`|`6786292`|`4`|`8969ce0cdaa4a19c...`|

### [VM-229] Methods of `Kata_pro64_Cad2025.VmbmLightFootingProperties` (Token `0x020003b2`, Displayed: 7/7, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x0600400f`|`.ctor`|`Kata_pro64_Cad2025.VmbmLightFootingInput [class:4328], Kata_pro64_Cad2025.VmbmLightShallowKind [valuetype:4324]`|`void`|`6786308`|`4`|`ae3c8b8d99a39542...`|
|`0x06004010`|`GetPropertyOwner`|`System.ComponentModel.PropertyDescriptor [class:2073]`|`object`|`6786496`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004011`|`GetProperties`|`()`|`System.ComponentModel.PropertyDescriptorCollection [class:2077]`|`6786512`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004012`|`GetProperties`|`System.Attribute [class:2081][]`|`System.ComponentModel.PropertyDescriptorCollection [class:2077]`|`6786536`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004013`|`.cctor`|`()`|`void`|`6788588`|`156`|`0b862f9a5b032443...`|
|`0x06004014`|`ApDHNMq4RtFH6wu8jnJx`|`()`|`bool`|`6788756`|`4`|`66ea1533cb3404e9...`|
|`0x06004015`|`MNyPZOq4g2V8lhBsKyGt`|`()`|`Kata_pro64_Cad2025.VmbmLightFootingProperties [class:3784]`|`6788776`|`4`|`8969ce0cdaa4a19c...`|

### [VM-230] Methods of `Kata_pro64_Cad2025.VmbmLightShallowSettingsProperties` (Token `0x020003b4`, Displayed: 7/7, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x0600401b`|`.ctor`|`Kata_pro64_Cad2025.VmbmLightShallowSettings [class:4332]`|`void`|`6788792`|`4`|`ae3c8b8d99a39542...`|
|`0x0600401c`|`GetPropertyOwner`|`System.ComponentModel.PropertyDescriptor [class:2073]`|`object`|`6788960`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600401d`|`GetProperties`|`()`|`System.ComponentModel.PropertyDescriptorCollection [class:2077]`|`6788976`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600401e`|`GetProperties`|`System.Attribute [class:2081][]`|`System.ComponentModel.PropertyDescriptorCollection [class:2077]`|`6789000`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600401f`|`.cctor`|`()`|`void`|`6789852`|`156`|`a5ca654d524f9c7a...`|
|`0x06004020`|`Rh9dAWq4ZFGQV3QDvYwS`|`()`|`bool`|`6790020`|`4`|`66ea1533cb3404e9...`|
|`0x06004021`|`OBnUgkq4eZAeaom2Ys3J`|`()`|`Kata_pro64_Cad2025.VmbmLightShallowSettingsProperties [class:3792]`|`6790040`|`4`|`8969ce0cdaa4a19c...`|

### [VM-231] Methods of `Kata_pro64_Cad2025.VmbmLightShallowCanvas` (Token `0x020003b5`, Displayed: 27/27, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06004022`|`get_Session`|`()`|`Kata_pro64_Cad2025.VmbmLightShallowSession [class:4336]`|`6790056`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004023`|`set_Session`|`Kata_pro64_Cad2025.VmbmLightShallowSession [class:4336]`|`void`|`6790072`|`4`|`ae3c8b8d99a39542...`|
|`0x06004024`|`get_Selected`|`()`|`System.Collections.Generic.HashSet`1 [class:385]<string>`|`6790176`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004025`|`set_Selected`|`System.Collections.Generic.HashSet`1 [class:385]<string>`|`void`|`6790192`|`4`|`ae3c8b8d99a39542...`|
|`0x06004026`|`add_Picked`|`PickedEventHandler [class:3800]`|`void`|`6790296`|`4`|`ae3c8b8d99a39542...`|
|`0x06004027`|`remove_Picked`|`PickedEventHandler [class:3800]`|`void`|`6790540`|`4`|`ae3c8b8d99a39542...`|
|`0x06004028`|`add_DimensionEdited`|`DimensionEditedEventHandler [class:3804]`|`void`|`6790824`|`4`|`ae3c8b8d99a39542...`|
|`0x06004029`|`remove_DimensionEdited`|`DimensionEditedEventHandler [class:3804]`|`void`|`6791088`|`4`|`ae3c8b8d99a39542...`|
|`0x0600402a`|`.ctor`|`()`|`void`|`6791392`|`4`|`ae3c8b8d99a39542...`|
|`0x0600402b`|`ResetView`|`()`|`void`|`6791840`|`4`|`ae3c8b8d99a39542...`|
|`0x0600402c`|`fba8X0w9N8a`|`Kata_Class_Lib_Revit.diem [class:85]`|`System.Drawing.PointF [valuetype:1609]`|`6792000`|`8`|`5b606ba778c54bbb...`|
|`0x0600402d`|`leY8XPmbCPi`|`System.Drawing.Point [valuetype:173]`|`Kata_Class_Lib_Revit.diem [class:85]`|`6792136`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600402e`|`OnPaint`|`System.Windows.Forms.PaintEventArgs [class:685]`|`void`|`6792280`|`4`|`ae3c8b8d99a39542...`|
|`0x0600402f`|`wra8X3M282a`|`System.Drawing.Graphics [class:941], Polygon [class:89], System.Drawing.Color [valuetype:697]`|`void`|`6798212`|`4`|`ae3c8b8d99a39542...`|
|`0x06004030`|`alT8XqTFxv9`|`System.Drawing.Graphics [class:941], Kata_pro64_Cad2025.VmbmLightFootingInput [class:4328], Kata_Class_Lib_Revit.diem [class:85], r8`|`void`|`6799020`|`4`|`ae3c8b8d99a39542...`|
|`0x06004031`|`QZP8XAafCDS`|`System.Drawing.Graphics [class:941], Kata_pro64_Cad2025.VmbmLightFootingInput [class:4328], string, r8, Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85]`|`void`|`6800768`|`4`|`ae3c8b8d99a39542...`|
|`0x06004032`|`Gqv8XC05fsL`|`System.Tuple`4 [class:1041]<System.Drawing.RectangleF [valuetype:897],string,string,r8>`|`void`|`6804880`|`4`|`ae3c8b8d99a39542...`|
|`0x06004033`|`OnMouseDoubleClick`|`System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`6805696`|`4`|`ae3c8b8d99a39542...`|
|`0x06004034`|`OnMouseDown`|`System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`6805928`|`4`|`ae3c8b8d99a39542...`|
|`0x06004035`|`OnMouseMove`|`System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`6807436`|`4`|`ae3c8b8d99a39542...`|
|`0x06004036`|`OnMouseUp`|`System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`6808920`|`4`|`ae3c8b8d99a39542...`|
|`0x06004037`|`OnMouseWheel`|`System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`6809072`|`4`|`ae3c8b8d99a39542...`|
|`0x06004038`|`Dispose`|`bool`|`void`|`6809840`|`4`|`ae3c8b8d99a39542...`|
|`0x06004039`|`_Lambda$__29-1`|`Kata_pro64_Cad2025.VmbmLightFootingInput [class:4328]`|`bool`|`6810092`|`4`|`66ea1533cb3404e9...`|
|`0x0600403a`|`.cctor`|`()`|`void`|`6810120`|`177`|`b111d0b59367bd4b...`|
|`0x0600403b`|`X5kt0Fq4TiYqAWXpyU22`|`()`|`bool`|`6810312`|`4`|`66ea1533cb3404e9...`|
|`0x0600403c`|`ag8j0Zq4fgW5usy9KBkj`|`()`|`Kata_pro64_Cad2025.VmbmLightShallowCanvas [class:3796]`|`6810332`|`4`|`8969ce0cdaa4a19c...`|

### [VM-232] Methods of `Kata_pro64_Cad2025.VmbmLightCad` (Token `0x020003c1`, Displayed: 31/31, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06004078`|`.ctor`|`()`|`void`|`6810348`|`4`|`ae3c8b8d99a39542...`|
|`0x06004079`|`IGn8X9ZXtuI`|`i8`|`string`|`6810472`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600407a`|`yff8XKuOlyH`|`object, System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightCap [class:4152]>, object`|`Kata_pro64_Cad2025.VmbmLightCap [class:4152]`|`6810512`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600407b`|`BindReadObjects`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], System.Collections.Generic.List`1 [class:217]<object>, Kata_Class_Lib_Revit.diem [class:85]`|`void`|`6813636`|`4`|`ae3c8b8d99a39542...`|
|`0x0600407c`|`ValidateBoundCad`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`void`|`6815420`|`4`|`ae3c8b8d99a39542...`|
|`0x0600407d`|`SyncNames`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`bool`|`6816244`|`4`|`66ea1533cb3404e9...`|
|`0x0600407e`|`ExportSteel`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], System.Collections.Generic.List`1 [class:217]<MomentMong [class:5312]>`|`i4`|`6823020`|`4`|`78d4d2062aa5373e...`|
|`0x0600407f`|`vbs8XNWWiQo`|`object`|`ThepBoTriMong [class:5308]`|`6828104`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004080`|`ReadPlan`|`Kata_pro64_Cad2025.VmbmLightOptions [class:4144]`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`6828144`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004081`|`evE8XbuGrlO`|`object, bool`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`6829560`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004082`|`ReadBackgroundSnapshot`|`System.Collections.Generic.List`1 [class:217]<object>`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`6835300`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004083`|`Shape`|`object`|`object`|`6835316`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004084`|`ExistingFingerprint`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`string`|`6835692`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004085`|`FindExistingAnnotations`|`object, System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightCap [class:4152]>, Kata_Class_Lib_Revit.diem [class:85]`|`System.Collections.Generic.List`1 [class:217]<i8>`|`6839992`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004086`|`fwA8XIIy14Q`|`object`|`string`|`6841416`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004087`|`ValidateSource`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`void`|`6842024`|`4`|`ae3c8b8d99a39542...`|
|`0x06004088`|`JYU8XyLm8ci`|`object`|`Kata_Class_Lib_Revit.Info_Slab3D [class:1613]`|`6844236`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004089`|`wfe8XRL6ISU`|`object`|`System.Collections.Generic.List`1 [class:217]<i8>`|`6844452`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600408a`|`E4q8XgdA0NU`|`System.Collections.Generic.IEnumerable`1 [class:321]<i8>`|`void`|`6849852`|`4`|`ae3c8b8d99a39542...`|
|`0x0600408b`|`K1M8XhBDV7B`|`object, i8`|`Autodesk.AutoCAD.DatabaseServices.ObjectId [valuetype:221]`|`6852884`|`8`|`acfcb9c2d49b3dc3...`|
|`0x0600408c`|`ReadPileTypes`|`()`|`System.Collections.Generic.List`1 [class:217]<CocChiuTai [class:3568]>`|`6853280`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600408d`|`Apply`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], bool, bool, bool`|`bool`|`6855312`|`4`|`66ea1533cb3404e9...`|
|`0x0600408e`|`PL98XZ6cOog`|`object, object, object, object`|`System.Collections.Generic.List`1 [class:217]<System.Tuple`2 [class:285]<CocChiuTai [class:3568],object>>`|`6870776`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600408f`|`YgC8XeEjTN1`|`object, object, object, i4, i4`|`object`|`6873212`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004090`|`Pmv8XLVMp2L`|`object`|`void`|`6873768`|`4`|`ae3c8b8d99a39542...`|
|`0x06004091`|`Wv88XTrYq5B`|`object, object, object`|`void`|`6874620`|`4`|`ae3c8b8d99a39542...`|
|`0x06004092`|`UpdateExistingResults`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`void`|`6878860`|`4`|`ae3c8b8d99a39542...`|
|`0x06004093`|`Ibi8XfCvIJF`|`object, object, Autodesk.AutoCAD.Geometry.Vector3d [valuetype:1657], object, object, System.Collections.Generic.List`1 [class:217]<i8>`|`void`|`6882912`|`4`|`ae3c8b8d99a39542...`|
|`0x06004094`|`.cctor`|`()`|`void`|`6884872`|`177`|`87dc8e5bde396edb...`|
|`0x06004095`|`e8y5Ymq463nOt6VeJhqV`|`()`|`bool`|`6885064`|`4`|`66ea1533cb3404e9...`|
|`0x06004096`|`oS18K8q4WnY54sZqXWpk`|`()`|`Kata_pro64_Cad2025.VmbmLightCad [class:3844]`|`6885084`|`4`|`8969ce0cdaa4a19c...`|

### [VM-233] Methods of `Kata_pro64_Cad2025.VmbmLightCommand` (Token `0x020003d7`, Displayed: 4/4, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x0600412d`|`ShowVmbmLight` [CommandMethod("ve_mat_bang_mong")]|`()`|`void`|`6885100`|`4`|`ae3c8b8d99a39542...`|
|`0x0600412e`|`.cctor`|`()`|`void`|`6885508`|`177`|`f83dff0d4041419e...`|
|`0x0600412f`|`vUMYo0q4tSYkSiBRwLK7`|`()`|`bool`|`6885700`|`4`|`66ea1533cb3404e9...`|
|`0x06004130`|`FD4GBbq4di30i6TWxEFB`|`()`|`Kata_pro64_Cad2025.VmbmLightCommand [class:3932]`|`6885720`|`4`|`8969ce0cdaa4a19c...`|

### [VM-234] Methods of `Kata_pro64_Cad2025.VmbmLightSteelAxis` (Token `0x020003d8`, Displayed: 10/10, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06004131`|`.ctor`|`()`|`void`|`6885736`|`4`|`ae3c8b8d99a39542...`|
|`0x06004132`|`get_CapId`|`()`|`string`|`6885860`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004133`|`set_CapId`|`string`|`void`|`6885876`|`4`|`ae3c8b8d99a39542...`|
|`0x06004134`|`get_X`|`()`|`r8`|`6885980`|`4`|`f0ad83c607e59d90...`|
|`0x06004135`|`set_X`|`r8`|`void`|`6885996`|`4`|`ae3c8b8d99a39542...`|
|`0x06004136`|`get_Y`|`()`|`r8`|`6886100`|`4`|`f0ad83c607e59d90...`|
|`0x06004137`|`set_Y`|`r8`|`void`|`6886116`|`4`|`ae3c8b8d99a39542...`|
|`0x06004138`|`.cctor`|`()`|`void`|`6886220`|`156`|`f63b3286a2277764...`|
|`0x06004139`|`Vo8RT4q4rlDa4OdARgax`|`()`|`bool`|`6886388`|`4`|`66ea1533cb3404e9...`|
|`0x0600413a`|`Edt6D1q4G3X1t0AfPoKV`|`()`|`Kata_pro64_Cad2025.VmbmLightSteelAxis [class:3936]`|`6886408`|`4`|`8969ce0cdaa4a19c...`|

### [VM-235] Methods of `Kata_pro64_Cad2025.VmbmLightEtabs` (Token `0x020003d9`, Displayed: 51/51, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x0600413b`|`.cctor`|`()`|`void`|`6886424`|`241`|`6fb10e52ec02ce35...`|
|`0x0600413c`|`Ckm8Xt0Eb2O`|`object`|`zryyeR0kbdPnTQi4daUu [class:3944]`|`6886680`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600413d`|`Ybg8Xd70KYD`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`string`|`6887244`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600413e`|`Fl68XJ1EEFr`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], Kata_pro64_Cad2025.VmbmLightIteration [class:4164]`|`void`|`6888208`|`4`|`ae3c8b8d99a39542...`|
|`0x0600413f`|`PublishToOriginal`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], System.Action`1 [class:885]<string>`|`void`|`6889560`|`4`|`ae3c8b8d99a39542...`|
|`0x06004140`|`fXB8XrZpwW6`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], Kata_pro64_Cad2025.VmbmLightIteration [class:4164]`|`void`|`6894232`|`4`|`ae3c8b8d99a39542...`|
|`0x06004141`|`ALM8XGVFXE5`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], Kata_pro64_Cad2025.VmbmLightIteration [class:4164]`|`void`|`6896568`|`4`|`ae3c8b8d99a39542...`|
|`0x06004142`|`CalculateSteelMoments`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], System.Action`1 [class:885]<string>, System.Threading.CancellationToken [valuetype:1413]`|`System.Collections.Generic.List`1 [class:217]<MomentMong [class:5312]>`|`6899152`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004143`|`NBh8XlJVHaT`|`object`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightSteelAxis [class:3936]>`|`6902984`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004144`|`.ctor`|`System.Action`3 [class:2049]<Kata_pro64_Cad2025.VmbmLightSession [class:4168],string,CSiAPIv1.cSapModel [class:2025]>`|`void`|`6904736`|`4`|`ae3c8b8d99a39542...`|
|`0x06004145`|`Connect`|`()`|`string`|`6904972`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004146`|`Metadata`|`()`|`Kata_pro64_Cad2025.VmbmLightEtabsMetadata [class:4672]`|`6905456`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004147`|`Prepare`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], System.Action`1 [class:885]<string>, System.Threading.CancellationToken [valuetype:1413]`|`void`|`6908268`|`4`|`ae3c8b8d99a39542...`|
|`0x06004148`|`SvB8XuBOwpS`|`object`|`void`|`6913136`|`4`|`ae3c8b8d99a39542...`|
|`0x06004149`|`g2j8XHZtBaV`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], System.Action`1 [class:885]<string>`|`void`|`6913328`|`4`|`ae3c8b8d99a39542...`|
|`0x0600414a`|`Wgc8Xps1MZ7`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`void`|`6915924`|`4`|`ae3c8b8d99a39542...`|
|`0x0600414b`|`auj8XwbccT9`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], System.Action`1 [class:885]<string>, System.Threading.CancellationToken [valuetype:1413]`|`void`|`6919180`|`4`|`ae3c8b8d99a39542...`|
|`0x0600414c`|`tS08X2GgeBR`|`string, r8`|`bool`|`6928408`|`4`|`66ea1533cb3404e9...`|
|`0x0600414d`|`Ovo8XX7hs6T`|`string, System.Collections.Generic.HashSet`1 [class:385]<string>`|`void`|`6928628`|`4`|`ae3c8b8d99a39542...`|
|`0x0600414e`|`uEx8XiyHjfr`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], System.Threading.CancellationToken [valuetype:1413], System.Action`1 [class:885]<string>, bool`|`void`|`6929352`|`4`|`ae3c8b8d99a39542...`|
|`0x0600414f`|`Analyze`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], i4, System.Action`1 [class:885]<string>, System.Threading.CancellationToken [valuetype:1413]`|`Kata_pro64_Cad2025.VmbmLightIteration [class:4164]`|`6931372`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004150`|`UYY8XnVqyPl`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], i4, string, System.Action`1 [class:885]<string>, System.Threading.CancellationToken [valuetype:1413]`|`Kata_pro64_Cad2025.VmbmLightIteration [class:4164]`|`6932084`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004151`|`Nf58X5UhHXC`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], System.Collections.Generic.Dictionary`2 [class:389]<string,Kata_Class_Lib_Revit.diem [class:85]>, string, System.Collections.Generic.HashSet`1 [class:385]<string>, System.Threading.CancellationToken [valuetype:1413]`|`System.Collections.Generic.List`1 [class:217]<string>`|`6937384`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004152`|`FPf8XStxF4C`|`object`|`System.Collections.Generic.List`1 [class:217]<dx1H0D0kLFWehaLawLgd [class:3952]>`|`6938812`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004153`|`P3f8XjS1wgs`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`System.Collections.Generic.List`1 [class:217]<RXrvf90kh9xXLo0LI1Jq [class:3948]>`|`6940472`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004154`|`KZ08XxX3MyJ`|`object`|`System.Collections.Generic.List`1 [class:217]<CurveXYZ [class:293]>`|`6942476`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004155`|`v8y8XDRwWfJ`|`System.Collections.Generic.List`1 [class:217]<RXrvf90kh9xXLo0LI1Jq [class:3948]>`|`System.Collections.Generic.List`1 [class:217]<RXrvf90kh9xXLo0LI1Jq [class:3948]>`|`6943700`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004156`|`RGH8XBUjn6t`|`object, object, r8&, r8&`|`bool`|`6945588`|`4`|`66ea1533cb3404e9...`|
|`0x06004157`|`Hju8XOgirmu`|`object, r8`|`Kata_Class_Lib_Revit.diem [class:85]`|`6946784`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004158`|`VME8XVBfXXV`|`object, object`|`bool`|`6946952`|`4`|`66ea1533cb3404e9...`|
|`0x06004159`|`f8x8Xai83qa`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], System.Collections.Generic.List`1 [class:217]<RXrvf90kh9xXLo0LI1Jq [class:3948]>, System.Collections.Generic.Dictionary`2 [class:389]<string,Kata_Class_Lib_Revit.diem [class:85]>, string, System.Collections.Generic.HashSet`1 [class:385]<string>, System.Collections.Generic.List`1 [class:217]<string>, System.Collections.Generic.List`1 [class:217]<System.Tuple`2 [class:285]<string,string>>`|`void`|`6947280`|`4`|`ae3c8b8d99a39542...`|
|`0x0600415a`|`mfq8XzJc9OI`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], Kata_Class_Lib_Revit.diem [class:85], System.Collections.Generic.Dictionary`2 [class:389]<string,Kata_Class_Lib_Revit.diem [class:85]>, string`|`string`|`6949560`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600415b`|`Ufb8iU6iYja`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], System.Collections.Generic.IEnumerable`1 [class:321]<string>, System.Collections.Generic.Dictionary`2 [class:389]<string,Kata_Class_Lib_Revit.diem [class:85]>`|`void`|`6950460`|`4`|`ae3c8b8d99a39542...`|
|`0x0600415c`|`m218i8dNi4N`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`void`|`6951308`|`4`|`ae3c8b8d99a39542...`|
|`0x0600415d`|`zlC8iEv44jB`|`System.Collections.Generic.IEnumerable`1 [class:321]<string>, System.Collections.Generic.Dictionary`2 [class:389]<string,r8[]>`|`void`|`6952904`|`4`|`ae3c8b8d99a39542...`|
|`0x0600415e`|`SbC8i0AE8ua`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], System.Collections.Generic.Dictionary`2 [class:389]<string,System.Collections.Generic.List`1 [class:217]<string>>, System.Collections.Generic.Dictionary`2 [class:389]<string,string>, System.Collections.Generic.List`1 [class:217]<System.Tuple`2 [class:285]<string,string>>`|`void`|`6955536`|`4`|`ae3c8b8d99a39542...`|
|`0x0600415f`|`iAJ8iPbWxfW`|`System.Collections.Generic.IEnumerable`1 [class:321]<string>`|`void`|`6958196`|`4`|`ae3c8b8d99a39542...`|
|`0x06004160`|`OZ88i3L7kv9`|`string`|`void`|`6958932`|`4`|`ae3c8b8d99a39542...`|
|`0x06004161`|`ejZ8iqwuDAT`|`string, string`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightPileResult [class:4156]>`|`6959212`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004162`|`FinalizeResult`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], string, System.Action`1 [class:885]<string>`|`void`|`6960580`|`4`|`ae3c8b8d99a39542...`|
|`0x06004163`|`aIZ8iA36fKP`|`System.Collections.Generic.IEnumerable`1 [class:321]<string>`|`System.Collections.Generic.Dictionary`2 [class:389]<string,bool[]>`|`6965492`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004164`|`Gja8iCJlmB1`|`System.Collections.Generic.IEnumerable`1 [class:321]<string>, System.Collections.Generic.IDictionary`2 [class:1725]<string,bool[]>`|`void`|`6966308`|`4`|`ae3c8b8d99a39542...`|
|`0x06004165`|`LrH8imFQ3XD`|`Kata_pro64_Cad2025.VmbmLightOptions [class:4144]`|`void`|`6967372`|`4`|`ae3c8b8d99a39542...`|
|`0x06004166`|`ks28iQIBaIo`|`object, object`|`Kata_Class_Lib_Revit.diem [class:85]`|`6967748`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004167`|`iek8ikd9u45`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], Kata_pro64_Cad2025.VmbmLightCap [class:4152], Kata_Class_Lib_Revit.diem [class:85], System.Collections.Generic.Dictionary`2 [class:389]<string,Kata_Class_Lib_Revit.diem [class:85]>, System.Collections.Generic.HashSet`1 [class:385]<string>, System.Collections.Generic.List`1 [class:217]<bvFaFi0kcKVHDnxQ74gE [class:3956]>, Kata_Class_Lib_Revit.diem [class:85], string, r8`|`void`|`6968144`|`4`|`ae3c8b8d99a39542...`|
|`0x06004168`|`vji8i42Y63F`|`object, object, object`|`System.Tuple`2 [class:285]<r8,r8>`|`6972496`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004169`|`YRS8iY38WiL`|`string, System.Collections.Generic.List`1 [class:217]<bvFaFi0kcKVHDnxQ74gE [class:3956]>`|`void`|`6973448`|`4`|`ae3c8b8d99a39542...`|
|`0x0600416a`|`OpenResult`|`string`|`void`|`6976456`|`4`|`ae3c8b8d99a39542...`|
|`0x0600416b`|`RIn8io5kurT`|`i4, object`|`void`|`6976820`|`4`|`ae3c8b8d99a39542...`|
|`0x0600416c`|`BHHCsoq4utU3IXpuP8f1`|`()`|`bool`|`6977012`|`4`|`66ea1533cb3404e9...`|
|`0x0600416d`|`hEO6uAq4HpZImqvHivZC`|`()`|`Kata_pro64_Cad2025.VmbmLightEtabs [class:3940]`|`6977032`|`4`|`8969ce0cdaa4a19c...`|

### [VM-236] Methods of `Kata_pro64_Cad2025.VmbmLightSlabCapInput` (Token `0x020003f5`, Displayed: 10/10, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x0600421a`|`.ctor`|`()`|`void`|`6977048`|`4`|`ae3c8b8d99a39542...`|
|`0x0600421b`|`get_Outline`|`()`|`Polygon [class:89]`|`6977244`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600421c`|`set_Outline`|`Polygon [class:89]`|`void`|`6977260`|`4`|`ae3c8b8d99a39542...`|
|`0x0600421d`|`get_Piles`|`()`|`System.Collections.Generic.List`1 [class:217]<CocChiuTai [class:3568]>`|`6977364`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600421e`|`set_Piles`|`System.Collections.Generic.List`1 [class:217]<CocChiuTai [class:3568]>`|`void`|`6977380`|`4`|`ae3c8b8d99a39542...`|
|`0x0600421f`|`get_EntityHandles`|`()`|`System.Collections.Generic.List`1 [class:217]<i8>`|`6977484`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004220`|`set_EntityHandles`|`System.Collections.Generic.List`1 [class:217]<i8>`|`void`|`6977500`|`4`|`ae3c8b8d99a39542...`|
|`0x06004221`|`.cctor`|`()`|`void`|`6977604`|`156`|`239b2fb59b6a83dd...`|
|`0x06004222`|`LHeaAnq4wqHXipD9EDEv`|`()`|`bool`|`6977772`|`4`|`66ea1533cb3404e9...`|
|`0x06004223`|`wVcmDDq42ZDq2pKZcuYD`|`()`|`Kata_pro64_Cad2025.VmbmLightSlabCapInput [class:4052]`|`6977792`|`4`|`8969ce0cdaa4a19c...`|

### [VM-237] Methods of `Kata_pro64_Cad2025.VmbmLightFoundationInput` (Token `0x020003f6`, Displayed: 19/19, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06004224`|`.ctor`|`()`|`void`|`6977808`|`4`|`ae3c8b8d99a39542...`|
|`0x06004225`|`get_SlabCaps`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightSlabCapInput [class:4052]>`|`6978004`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004226`|`set_SlabCaps`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightSlabCapInput [class:4052]>`|`void`|`6978020`|`4`|`ae3c8b8d99a39542...`|
|`0x06004227`|`get_EntityHandles`|`()`|`System.Collections.Generic.List`1 [class:217]<i8>`|`6978124`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004228`|`set_EntityHandles`|`System.Collections.Generic.List`1 [class:217]<i8>`|`void`|`6978140`|`4`|`ae3c8b8d99a39542...`|
|`0x06004229`|`IsCapBlock`|`object`|`bool`|`6978244`|`4`|`66ea1533cb3404e9...`|
|`0x0600422a`|`GetTapMongTuTapSan`|`System.Collections.Generic.List`1 [class:217]<info_revit_slab [class:1005]>`|`System.Collections.Generic.List`1 [class:217]<Polygon [class:89]>`|`6978616`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600422b`|`ReadLegacy`|`System.Collections.Generic.List`1 [class:217]<info_revit_slab [class:1005]>, System.Collections.Generic.List`1 [class:217]<CocChiuTai [class:3568]>`|`Kata_pro64_Cad2025.VmbmLightFoundationInput [class:4056]`|`6981104`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600422c`|`Read`|`object, System.Collections.Generic.IEnumerable`1 [class:321]<Kata_Class_Lib_Revit.Info_Slab3D [class:1613]>, System.Collections.Generic.IEnumerable`1 [class:321]<string>`|`Kata_pro64_Cad2025.VmbmLightFoundationInput [class:4056]`|`6981252`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600422d`|`ReadPhysicalPile`|`object`|`CocChiuTai [class:3568]`|`6985692`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600422e`|`cvv8iIbJk8U`|`System.Collections.Generic.List`1 [class:217]<Polygon [class:89]>`|`System.Collections.Generic.List`1 [class:217]<Polygon [class:89]>`|`6988084`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600422f`|`DkW8iy6FYI2`|`object, object, System.Collections.Generic.Dictionary`2 [class:389]<string,object>, System.Collections.Generic.List`1 [class:217]<Polygon [class:89]>`|`void`|`6990196`|`4`|`ae3c8b8d99a39542...`|
|`0x06004230`|`p4J8iRBc7lM`|`object, System.Collections.Generic.List`1 [class:217]<Polygon [class:89]>`|`void`|`6991892`|`4`|`ae3c8b8d99a39542...`|
|`0x06004231`|`cl38igNbltL`|`object, object`|`bool`|`6992884`|`4`|`66ea1533cb3404e9...`|
|`0x06004232`|`Uub8ihCT4dE`|`object, object`|`bool`|`6993288`|`4`|`66ea1533cb3404e9...`|
|`0x06004233`|`f368iZKcfrN`|`System.Collections.Generic.List`1 [class:217]<Polygon [class:89]>, System.Collections.Generic.List`1 [class:217]<CocChiuTai [class:3568]>`|`Kata_pro64_Cad2025.VmbmLightFoundationInput [class:4056]`|`6993568`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004234`|`.cctor`|`()`|`void`|`6996472`|`156`|`7318fff87ff14011...`|
|`0x06004235`|`wBtrrOq4iFssuUaJOqBm`|`()`|`bool`|`6996640`|`4`|`66ea1533cb3404e9...`|
|`0x06004236`|`Tl4hdqq4nDr32MttJTU8`|`()`|`Kata_pro64_Cad2025.VmbmLightFoundationInput [class:4056]`|`6996660`|`4`|`8969ce0cdaa4a19c...`|

### [VM-238] Methods of `Kata_pro64_Cad2025.VmbmLightLayoutIdentity` (Token `0x02000402`, Displayed: 14/14, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06004274`|`.ctor`|`()`|`void`|`6996676`|`4`|`ae3c8b8d99a39542...`|
|`0x06004275`|`Vod8iTZDBVX`|`r8, r8, r8`|`bool`|`6996800`|`4`|`66ea1533cb3404e9...`|
|`0x06004276`|`fpu8ifeDckt`|`object, object, r8`|`bool`|`6997016`|`4`|`66ea1533cb3404e9...`|
|`0x06004277`|`SameContour`|`Polygon [class:89], Polygon [class:89], Placement [class:4108], r8`|`bool`|`6997304`|`4`|`66ea1533cb3404e9...`|
|`0x06004278`|`PileMap`|`InfoCotCocGenMong [class:3584], InfoCotCocGenMong [class:3584], Placement [class:4108], bool`|`i4[]`|`7000180`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004279`|`SamePhysical`|`Kata_pro64_Cad2025.VmbmLightCap [class:4152], Kata_pro64_Cad2025.VmbmLightCap [class:4152]`|`bool`|`7001988`|`4`|`66ea1533cb3404e9...`|
|`0x0600427a`|`SameLayout`|`System.Collections.Generic.IEnumerable`1 [class:321]<Kata_pro64_Cad2025.VmbmLightCap [class:4152]>, System.Collections.Generic.IEnumerable`1 [class:321]<Kata_pro64_Cad2025.VmbmLightCap [class:4152]>`|`bool`|`7002752`|`4`|`66ea1533cb3404e9...`|
|`0x0600427b`|`TrySameStructure`|`InfoCotCocGenMong [class:3584], InfoCotCocGenMong [class:3584], Placement [class:4108]&`|`bool`|`7003368`|`4`|`66ea1533cb3404e9...`|
|`0x0600427c`|`ValidateName`|`string`|`void`|`7005736`|`4`|`ae3c8b8d99a39542...`|
|`0x0600427d`|`Rename`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], string, i4`|`void`|`7006204`|`4`|`ae3c8b8d99a39542...`|
|`0x0600427e`|`ValidateGroups`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`void`|`7008060`|`4`|`ae3c8b8d99a39542...`|
|`0x0600427f`|`.cctor`|`()`|`void`|`7009220`|`177`|`a6766747978387ef...`|
|`0x06004280`|`Svx4ROq4SM5mQXAOiwTw`|`()`|`bool`|`7009412`|`4`|`66ea1533cb3404e9...`|
|`0x06004281`|`VyAjgcq4jLR8KMv5yUq7`|`()`|`Kata_pro64_Cad2025.VmbmLightLayoutIdentity [class:4104]`|`7009432`|`4`|`8969ce0cdaa4a19c...`|

### [VM-239] Methods of `Kata_pro64_Cad2025.VmbmLightSourceFiles` (Token `0x02000409`, Displayed: 6/6, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x060042a7`|`.ctor`|`()`|`void`|`7009448`|`4`|`ae3c8b8d99a39542...`|
|`0x060042a8`|`OriginalSource`|`string, System.Func`2 [class:305]<string,string>`|`string`|`7009572`|`4`|`8969ce0cdaa4a19c...`|
|`0x060042a9`|`p398icYJbTJ`|`object`|`string`|`7011856`|`4`|`8969ce0cdaa4a19c...`|
|`0x060042aa`|`.cctor`|`()`|`void`|`7011916`|`177`|`e094e6933eb43579...`|
|`0x060042ab`|`t6d6Xeq4DJ9rq2RpKmJy`|`()`|`bool`|`7012108`|`4`|`66ea1533cb3404e9...`|
|`0x060042ac`|`g2HSp4q4BfrZFZHM7BZ1`|`()`|`Kata_pro64_Cad2025.VmbmLightSourceFiles [class:4132]`|`7012128`|`4`|`8969ce0cdaa4a19c...`|

### [VM-240] Methods of `Kata_pro64_Cad2025.VmbmLightOptions` (Token `0x0200040c`, Displayed: 87/87, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x060042b8`|`.ctor`|`()`|`void`|`7012144`|`4`|`ae3c8b8d99a39542...`|
|`0x060042b9`|`get_AutoResizeBeams`|`()`|`bool`|`7013860`|`4`|`66ea1533cb3404e9...`|
|`0x060042ba`|`set_AutoResizeBeams`|`bool`|`void`|`7013876`|`4`|`ae3c8b8d99a39542...`|
|`0x060042bb`|`get_BeamCombos`|`()`|`System.Collections.Generic.List`1 [class:217]<string>`|`7013980`|`4`|`8969ce0cdaa4a19c...`|
|`0x060042bc`|`set_BeamCombos`|`System.Collections.Generic.List`1 [class:217]<string>`|`void`|`7013996`|`4`|`ae3c8b8d99a39542...`|
|`0x060042bd`|`get_BeamDesignCode`|`()`|`string`|`7014100`|`4`|`8969ce0cdaa4a19c...`|
|`0x060042be`|`set_BeamDesignCode`|`string`|`void`|`7014116`|`4`|`ae3c8b8d99a39542...`|
|`0x060042bf`|`get_BeamRatioPercent`|`()`|`r8`|`7014220`|`4`|`f0ad83c607e59d90...`|
|`0x060042c0`|`set_BeamRatioPercent`|`r8`|`void`|`7014236`|`4`|`ae3c8b8d99a39542...`|
|`0x060042c1`|`get_BeamSections`|`()`|`string`|`7014340`|`4`|`8969ce0cdaa4a19c...`|
|`0x060042c2`|`set_BeamSections`|`string`|`void`|`7014356`|`4`|`ae3c8b8d99a39542...`|
|`0x060042c3`|`get_BeamMaxIterations`|`()`|`i4`|`7014460`|`4`|`78d4d2062aa5373e...`|
|`0x060042c4`|`set_BeamMaxIterations`|`i4`|`void`|`7014476`|`4`|`ae3c8b8d99a39542...`|
|`0x060042c5`|`get_BeamShearLimit`|`()`|`r8`|`7014580`|`4`|`f0ad83c607e59d90...`|
|`0x060042c6`|`set_BeamShearLimit`|`r8`|`void`|`7014596`|`4`|`ae3c8b8d99a39542...`|
|`0x060042c7`|`get_BeamClearCover`|`()`|`r8`|`7014700`|`4`|`f0ad83c607e59d90...`|
|`0x060042c8`|`set_BeamClearCover`|`r8`|`void`|`7014716`|`4`|`ae3c8b8d99a39542...`|
|`0x060042c9`|`get_BeamTieDiameter`|`()`|`r8`|`7014820`|`4`|`f0ad83c607e59d90...`|
|`0x060042ca`|`set_BeamTieDiameter`|`r8`|`void`|`7014836`|`4`|`ae3c8b8d99a39542...`|
|`0x060042cb`|`get_BeamMaxBarDiameter`|`()`|`r8`|`7014940`|`4`|`f0ad83c607e59d90...`|
|`0x060042cc`|`set_BeamMaxBarDiameter`|`r8`|`void`|`7014956`|`4`|`ae3c8b8d99a39542...`|
|`0x060042cd`|`get_BeamMaxLayers`|`()`|`i4`|`7015060`|`4`|`78d4d2062aa5373e...`|
|`0x060042ce`|`set_BeamMaxLayers`|`i4`|`void`|`7015076`|`4`|`ae3c8b8d99a39542...`|
|`0x060042cf`|`get_BeamClearSpacing`|`()`|`r8`|`7015180`|`4`|`f0ad83c607e59d90...`|
|`0x060042d0`|`set_BeamClearSpacing`|`r8`|`void`|`7015196`|`4`|`ae3c8b8d99a39542...`|
|`0x060042d1`|`get_Combos`|`()`|`System.Collections.Generic.List`1 [class:217]<string>`|`7015300`|`4`|`8969ce0cdaa4a19c...`|
|`0x060042d2`|`set_Combos`|`System.Collections.Generic.List`1 [class:217]<string>`|`void`|`7015316`|`4`|`ae3c8b8d99a39542...`|
|`0x060042d3`|`get_SteelCombo`|`()`|`string`|`7015420`|`4`|`8969ce0cdaa4a19c...`|
|`0x060042d4`|`set_SteelCombo`|`string`|`void`|`7015436`|`4`|`ae3c8b8d99a39542...`|
|`0x060042d5`|`get_NamePrefix`|`()`|`string`|`7015540`|`4`|`8969ce0cdaa4a19c...`|
|`0x060042d6`|`set_NamePrefix`|`string`|`void`|`7015556`|`4`|`ae3c8b8d99a39542...`|
|`0x060042d7`|`get_NameStartNumber`|`()`|`i4`|`7015660`|`4`|`78d4d2062aa5373e...`|
|`0x060042d8`|`set_NameStartNumber`|`i4`|`void`|`7015676`|`4`|`ae3c8b8d99a39542...`|
|`0x060042d9`|`get_StoryName`|`()`|`string`|`7015780`|`4`|`8969ce0cdaa4a19c...`|
|`0x060042da`|`set_StoryName`|`string`|`void`|`7015796`|`4`|`ae3c8b8d99a39542...`|
|`0x060042db`|`get_Elevation`|`()`|`r8`|`7015900`|`4`|`f0ad83c607e59d90...`|
|`0x060042dc`|`set_Elevation`|`r8`|`void`|`7015916`|`4`|`ae3c8b8d99a39542...`|
|`0x060042dd`|`get_Concrete`|`()`|`string`|`7016020`|`4`|`8969ce0cdaa4a19c...`|
|`0x060042de`|`set_Concrete`|`string`|`void`|`7016036`|`4`|`ae3c8b8d99a39542...`|
|`0x060042df`|`get_MinPiles`|`()`|`i4`|`7016140`|`4`|`78d4d2062aa5373e...`|
|`0x060042e0`|`set_MinPiles`|`i4`|`void`|`7016156`|`4`|`ae3c8b8d99a39542...`|
|`0x060042e1`|`get_MaxPiles`|`()`|`i4`|`7016260`|`4`|`78d4d2062aa5373e...`|
|`0x060042e2`|`set_MaxPiles`|`i4`|`void`|`7016276`|`4`|`ae3c8b8d99a39542...`|
|`0x060042e3`|`get_MaxIterations`|`()`|`i4`|`7016380`|`4`|`78d4d2062aa5373e...`|
|`0x060042e4`|`set_MaxIterations`|`i4`|`void`|`7016396`|`4`|`ae3c8b8d99a39542...`|
|`0x060042e5`|`get_CapacityLimit`|`()`|`r8`|`7016500`|`4`|`f0ad83c607e59d90...`|
|`0x060042e6`|`set_CapacityLimit`|`r8`|`void`|`7016516`|`4`|`ae3c8b8d99a39542...`|
|`0x060042e7`|`get_RedPileForcePercent`|`()`|`r8`|`7016620`|`4`|`f0ad83c607e59d90...`|
|`0x060042e8`|`set_RedPileForcePercent`|`r8`|`void`|`7016636`|`4`|`ae3c8b8d99a39542...`|
|`0x060042e9`|`get_PurplePileForcePercent`|`()`|`r8`|`7016740`|`4`|`f0ad83c607e59d90...`|
|`0x060042ea`|`set_PurplePileForcePercent`|`r8`|`void`|`7016756`|`4`|`ae3c8b8d99a39542...`|
|`0x060042eb`|`get_InitialFactor`|`()`|`r8`|`7016860`|`4`|`f0ad83c607e59d90...`|
|`0x060042ec`|`set_InitialFactor`|`r8`|`void`|`7016876`|`4`|`ae3c8b8d99a39542...`|
|`0x060042ed`|`get_EccentricInitialFactor`|`()`|`r8`|`7016980`|`4`|`f0ad83c607e59d90...`|
|`0x060042ee`|`set_EccentricInitialFactor`|`r8`|`void`|`7016996`|`4`|`ae3c8b8d99a39542...`|
|`0x060042ef`|`get_CapHeight`|`()`|`r8`|`7017100`|`4`|`f0ad83c607e59d90...`|
|`0x060042f0`|`set_CapHeight`|`r8`|`void`|`7017116`|`4`|`ae3c8b8d99a39542...`|
|`0x060042f1`|`get_EdgeDistance`|`()`|`string`|`7017220`|`4`|`8969ce0cdaa4a19c...`|
|`0x060042f2`|`set_EdgeDistance`|`string`|`void`|`7017236`|`4`|`ae3c8b8d99a39542...`|
|`0x060042f3`|`get_SettlementMm`|`()`|`r8`|`7017340`|`4`|`f0ad83c607e59d90...`|
|`0x060042f4`|`set_SettlementMm`|`r8`|`void`|`7017356`|`4`|`ae3c8b8d99a39542...`|
|`0x060042f5`|`get_HorizontalSpringRatio`|`()`|`r8`|`7017460`|`4`|`f0ad83c607e59d90...`|
|`0x060042f6`|`set_HorizontalSpringRatio`|`r8`|`void`|`7017476`|`4`|`ae3c8b8d99a39542...`|
|`0x060042f7`|`get_RotationSpringLengthSquared`|`()`|`r8`|`7017580`|`4`|`f0ad83c607e59d90...`|
|`0x060042f8`|`set_RotationSpringLengthSquared`|`r8`|`void`|`7017596`|`4`|`ae3c8b8d99a39542...`|
|`0x060042f9`|`get_TorsionSpringLengthSquared`|`()`|`r8`|`7017700`|`4`|`f0ad83c607e59d90...`|
|`0x060042fa`|`set_TorsionSpringLengthSquared`|`r8`|`void`|`7017716`|`4`|`ae3c8b8d99a39542...`|
|`0x060042fb`|`get_MeshSizeMm`|`()`|`r8`|`7017820`|`4`|`f0ad83c607e59d90...`|
|`0x060042fc`|`set_MeshSizeMm`|`r8`|`void`|`7017836`|`4`|`ae3c8b8d99a39542...`|
|`0x060042fd`|`get_MergeGapMm`|`()`|`r8`|`7017940`|`4`|`f0ad83c607e59d90...`|
|`0x060042fe`|`set_MergeGapMm`|`r8`|`void`|`7017956`|`4`|`ae3c8b8d99a39542...`|
|`0x060042ff`|`get_AutoMerge`|`()`|`bool`|`7018060`|`4`|`66ea1533cb3404e9...`|
|`0x06004300`|`set_AutoMerge`|`bool`|`void`|`7018076`|`4`|`ae3c8b8d99a39542...`|
|`0x06004301`|`get_GridSystem`|`()`|`string`|`7018180`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004302`|`set_GridSystem`|`string`|`void`|`7018196`|`4`|`ae3c8b8d99a39542...`|
|`0x06004303`|`get_TestWithSourceWarnings`|`()`|`bool`|`7018300`|`4`|`66ea1533cb3404e9...`|
|`0x06004304`|`set_TestWithSourceWarnings`|`bool`|`void`|`7018316`|`4`|`ae3c8b8d99a39542...`|
|`0x06004305`|`get_StripWidthMm`|`()`|`string`|`7018420`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004306`|`set_StripWidthMm`|`string`|`void`|`7018436`|`4`|`ae3c8b8d99a39542...`|
|`0x06004307`|`get_StripDesignType`|`()`|`string`|`7018540`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004308`|`set_StripDesignType`|`string`|`void`|`7018556`|`4`|`ae3c8b8d99a39542...`|
|`0x06004309`|`get_StripRebarMaterial`|`()`|`string`|`7018660`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600430a`|`set_StripRebarMaterial`|`string`|`void`|`7018676`|`4`|`ae3c8b8d99a39542...`|
|`0x0600430b`|`Validate`|`()`|`void`|`7018780`|`4`|`ae3c8b8d99a39542...`|
|`0x0600430c`|`.cctor`|`()`|`void`|`7021068`|`177`|`048bccb478346d65...`|
|`0x0600430d`|`udZCfJq4Va6TSUa0Ypsn`|`()`|`bool`|`7021260`|`4`|`66ea1533cb3404e9...`|
|`0x0600430e`|`CdpDUMq4aEWGhaqsfegF`|`()`|`Kata_pro64_Cad2025.VmbmLightOptions [class:4144]`|`7021280`|`4`|`8969ce0cdaa4a19c...`|

### [VM-241] Methods of `Kata_pro64_Cad2025.VmbmLightSupport` (Token `0x0200040d`, Displayed: 14/14, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x0600430f`|`.ctor`|`()`|`void`|`7021296`|`4`|`ae3c8b8d99a39542...`|
|`0x06004310`|`get_Id`|`()`|`string`|`7021536`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004311`|`set_Id`|`string`|`void`|`7021552`|`4`|`ae3c8b8d99a39542...`|
|`0x06004312`|`get_SourceKey`|`()`|`string`|`7021656`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004313`|`set_SourceKey`|`string`|`void`|`7021672`|`4`|`ae3c8b8d99a39542...`|
|`0x06004314`|`get_Outline`|`()`|`Polygon [class:89]`|`7021776`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004315`|`set_Outline`|`Polygon [class:89]`|`void`|`7021792`|`4`|`ae3c8b8d99a39542...`|
|`0x06004316`|`get_Joints`|`()`|`System.Collections.Generic.List`1 [class:217]<string>`|`7021896`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004317`|`set_Joints`|`System.Collections.Generic.List`1 [class:217]<string>`|`void`|`7021912`|`4`|`ae3c8b8d99a39542...`|
|`0x06004318`|`get_Axial`|`()`|`r8`|`7022016`|`4`|`f0ad83c607e59d90...`|
|`0x06004319`|`set_Axial`|`r8`|`void`|`7022032`|`4`|`ae3c8b8d99a39542...`|
|`0x0600431a`|`.cctor`|`()`|`void`|`7022136`|`177`|`191c65df305b1e8d...`|
|`0x0600431b`|`VuPCv2qYU9N3I3JYHr5m`|`()`|`bool`|`7022328`|`4`|`66ea1533cb3404e9...`|
|`0x0600431c`|`Nbp0nJqY8PU4tXHEfabd`|`()`|`Kata_pro64_Cad2025.VmbmLightSupport [class:4148]`|`7022348`|`4`|`8969ce0cdaa4a19c...`|

### [VM-242] Methods of `Kata_pro64_Cad2025.VmbmLightCap` (Token `0x0200040e`, Displayed: 26/26, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x0600431d`|`.ctor`|`()`|`void`|`7022364`|`4`|`ae3c8b8d99a39542...`|
|`0x0600431e`|`get_Id`|`()`|`string`|`7022668`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600431f`|`set_Id`|`string`|`void`|`7022684`|`4`|`ae3c8b8d99a39542...`|
|`0x06004320`|`get_Name`|`()`|`string`|`7022788`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004321`|`set_Name`|`string`|`void`|`7022804`|`4`|`ae3c8b8d99a39542...`|
|`0x06004322`|`get_Locked`|`()`|`bool`|`7022908`|`4`|`66ea1533cb3404e9...`|
|`0x06004323`|`set_Locked`|`bool`|`void`|`7022924`|`4`|`ae3c8b8d99a39542...`|
|`0x06004324`|`get_PreserveCadPileSpacing`|`()`|`bool`|`7023028`|`4`|`66ea1533cb3404e9...`|
|`0x06004325`|`set_PreserveCadPileSpacing`|`bool`|`void`|`7023044`|`4`|`ae3c8b8d99a39542...`|
|`0x06004326`|`get_SupportIds`|`()`|`System.Collections.Generic.List`1 [class:217]<string>`|`7023148`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004327`|`set_SupportIds`|`System.Collections.Generic.List`1 [class:217]<string>`|`void`|`7023164`|`4`|`ae3c8b8d99a39542...`|
|`0x06004328`|`get_Layout`|`()`|`InfoCotCocGenMong [class:3584]`|`7023268`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004329`|`set_Layout`|`InfoCotCocGenMong [class:3584]`|`void`|`7023284`|`4`|`ae3c8b8d99a39542...`|
|`0x0600432a`|`get_RotationSource`|`()`|`InfoCotCocGenMong [class:3584]`|`7023388`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600432b`|`set_RotationSource`|`InfoCotCocGenMong [class:3584]`|`void`|`7023404`|`4`|`ae3c8b8d99a39542...`|
|`0x0600432c`|`get_NeedsCheck`|`()`|`bool`|`7023508`|`4`|`66ea1533cb3404e9...`|
|`0x0600432d`|`set_NeedsCheck`|`bool`|`void`|`7023524`|`4`|`ae3c8b8d99a39542...`|
|`0x0600432e`|`get_CadHandle`|`()`|`i8`|`7023628`|`4`|`0c55072e2c71f84f...`|
|`0x0600432f`|`set_CadHandle`|`i8`|`void`|`7023644`|`4`|`ae3c8b8d99a39542...`|
|`0x06004330`|`get_StatusText`|`()`|`string`|`7023748`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004331`|`get_Check`|`()`|`Kata_pro64_Cad2025.VmbmLightCapCheck [class:4160]`|`7023940`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004332`|`set_Check`|`Kata_pro64_Cad2025.VmbmLightCapCheck [class:4160]`|`void`|`7023956`|`4`|`ae3c8b8d99a39542...`|
|`0x06004333`|`Copy`|`()`|`Kata_pro64_Cad2025.VmbmLightCap [class:4152]`|`7024060`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004334`|`.cctor`|`()`|`void`|`7024244`|`177`|`0594577f66af9a54...`|
|`0x06004335`|`YZyZPdqY0HCSYuxG5OOM`|`()`|`bool`|`7024436`|`4`|`66ea1533cb3404e9...`|
|`0x06004336`|`AvSoZPqYPnSxPR4bPATr`|`()`|`Kata_pro64_Cad2025.VmbmLightCap [class:4152]`|`7024456`|`4`|`8969ce0cdaa4a19c...`|

### [VM-243] Methods of `Kata_pro64_Cad2025.VmbmLightPileResult` (Token `0x0200040f`, Displayed: 24/24, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06004337`|`.ctor`|`()`|`void`|`7024472`|`4`|`ae3c8b8d99a39542...`|
|`0x06004338`|`get_CapId`|`()`|`string`|`7024596`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004339`|`set_CapId`|`string`|`void`|`7024612`|`4`|`ae3c8b8d99a39542...`|
|`0x0600433a`|`get_PileId`|`()`|`string`|`7024716`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600433b`|`set_PileId`|`string`|`void`|`7024732`|`4`|`ae3c8b8d99a39542...`|
|`0x0600433c`|`get_Joint`|`()`|`string`|`7024836`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600433d`|`set_Joint`|`string`|`void`|`7024852`|`4`|`ae3c8b8d99a39542...`|
|`0x0600433e`|`get_Combo`|`()`|`string`|`7024956`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600433f`|`set_Combo`|`string`|`void`|`7024972`|`4`|`ae3c8b8d99a39542...`|
|`0x06004340`|`get_StepType`|`()`|`string`|`7025076`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004341`|`set_StepType`|`string`|`void`|`7025092`|`4`|`ae3c8b8d99a39542...`|
|`0x06004342`|`get_StepNumber`|`()`|`r8`|`7025196`|`4`|`f0ad83c607e59d90...`|
|`0x06004343`|`set_StepNumber`|`r8`|`void`|`7025212`|`4`|`ae3c8b8d99a39542...`|
|`0x06004344`|`get_Axial`|`()`|`r8`|`7025316`|`4`|`f0ad83c607e59d90...`|
|`0x06004345`|`set_Axial`|`r8`|`void`|`7025332`|`4`|`ae3c8b8d99a39542...`|
|`0x06004346`|`get_Capacity`|`()`|`r8`|`7025436`|`4`|`f0ad83c607e59d90...`|
|`0x06004347`|`set_Capacity`|`r8`|`void`|`7025452`|`4`|`ae3c8b8d99a39542...`|
|`0x06004348`|`get_X`|`()`|`r8`|`7025556`|`4`|`f0ad83c607e59d90...`|
|`0x06004349`|`set_X`|`r8`|`void`|`7025572`|`4`|`ae3c8b8d99a39542...`|
|`0x0600434a`|`get_Y`|`()`|`r8`|`7025676`|`4`|`f0ad83c607e59d90...`|
|`0x0600434b`|`set_Y`|`r8`|`void`|`7025692`|`4`|`ae3c8b8d99a39542...`|
|`0x0600434c`|`.cctor`|`()`|`void`|`7025796`|`156`|`c3fbef875916f077...`|
|`0x0600434d`|`igRAViqYqC1PNo0njAQO`|`()`|`bool`|`7025964`|`4`|`66ea1533cb3404e9...`|
|`0x0600434e`|`Sw1ds6qYAkvbLlj9QTJR`|`()`|`Kata_pro64_Cad2025.VmbmLightPileResult [class:4156]`|`7025984`|`4`|`8969ce0cdaa4a19c...`|

### [VM-244] Methods of `Kata_pro64_Cad2025.VmbmLightCapCheck` (Token `0x02000410`, Displayed: 17/17, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x0600434f`|`.ctor`|`()`|`void`|`7026000`|`4`|`ae3c8b8d99a39542...`|
|`0x06004350`|`get_HasResults`|`()`|`bool`|`7026216`|`4`|`66ea1533cb3404e9...`|
|`0x06004351`|`set_HasResults`|`bool`|`void`|`7026232`|`4`|`ae3c8b8d99a39542...`|
|`0x06004352`|`get_Passed`|`()`|`bool`|`7026336`|`4`|`66ea1533cb3404e9...`|
|`0x06004353`|`set_Passed`|`bool`|`void`|`7026352`|`4`|`ae3c8b8d99a39542...`|
|`0x06004354`|`get_MaxRatio`|`()`|`r8`|`7026456`|`4`|`f0ad83c607e59d90...`|
|`0x06004355`|`set_MaxRatio`|`r8`|`void`|`7026472`|`4`|`ae3c8b8d99a39542...`|
|`0x06004356`|`get_MinAxial`|`()`|`r8`|`7026576`|`4`|`f0ad83c607e59d90...`|
|`0x06004357`|`set_MinAxial`|`r8`|`void`|`7026592`|`4`|`ae3c8b8d99a39542...`|
|`0x06004358`|`get_GoverningCombo`|`()`|`string`|`7026696`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004359`|`set_GoverningCombo`|`string`|`void`|`7026712`|`4`|`ae3c8b8d99a39542...`|
|`0x0600435a`|`get_Message`|`()`|`string`|`7026816`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600435b`|`set_Message`|`string`|`void`|`7026832`|`4`|`ae3c8b8d99a39542...`|
|`0x0600435c`|`Copy`|`()`|`Kata_pro64_Cad2025.VmbmLightCapCheck [class:4160]`|`7026936`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600435d`|`.cctor`|`()`|`void`|`7026964`|`156`|`7cc1f60bd22d04b7...`|
|`0x0600435e`|`mhnOwpqYm8L2fpU2Glhg`|`()`|`bool`|`7027132`|`4`|`66ea1533cb3404e9...`|
|`0x0600435f`|`Un3jQWqYQnq4K65E1dme`|`()`|`Kata_pro64_Cad2025.VmbmLightCapCheck [class:4160]`|`7027152`|`4`|`8969ce0cdaa4a19c...`|

### [VM-245] Methods of `Kata_pro64_Cad2025.VmbmLightIteration` (Token `0x02000411`, Displayed: 38/38, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06004360`|`.ctor`|`()`|`void`|`7027168`|`4`|`ae3c8b8d99a39542...`|
|`0x06004361`|`get_Beams`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightBeam [class:4200]>`|`7027568`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004362`|`set_Beams`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightBeam [class:4200]>`|`void`|`7027584`|`4`|`ae3c8b8d99a39542...`|
|`0x06004363`|`get_BeamDesignKey`|`()`|`string`|`7027688`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004364`|`set_BeamDesignKey`|`string`|`void`|`7027704`|`4`|`ae3c8b8d99a39542...`|
|`0x06004365`|`get_Completed`|`()`|`bool`|`7027808`|`4`|`66ea1533cb3404e9...`|
|`0x06004366`|`set_Completed`|`bool`|`void`|`7027824`|`4`|`ae3c8b8d99a39542...`|
|`0x06004367`|`get_Token`|`()`|`string`|`7027928`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004368`|`set_Token`|`string`|`void`|`7027944`|`4`|`ae3c8b8d99a39542...`|
|`0x06004369`|`get_Combos`|`()`|`System.Collections.Generic.List`1 [class:217]<string>`|`7028048`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600436a`|`set_Combos`|`System.Collections.Generic.List`1 [class:217]<string>`|`void`|`7028064`|`4`|`ae3c8b8d99a39542...`|
|`0x0600436b`|`get_FileStamp`|`()`|`string`|`7028168`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600436c`|`set_FileStamp`|`string`|`void`|`7028184`|`4`|`ae3c8b8d99a39542...`|
|`0x0600436d`|`get_Strips`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightStripBinding [class:5652]>`|`7028288`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600436e`|`set_Strips`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightStripBinding [class:5652]>`|`void`|`7028304`|`4`|`ae3c8b8d99a39542...`|
|`0x0600436f`|`get_Number`|`()`|`i4`|`7028408`|`4`|`78d4d2062aa5373e...`|
|`0x06004370`|`set_Number`|`i4`|`void`|`7028424`|`4`|`ae3c8b8d99a39542...`|
|`0x06004371`|`get_ModelPath`|`()`|`string`|`7028528`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004372`|`set_ModelPath`|`string`|`void`|`7028544`|`4`|`ae3c8b8d99a39542...`|
|`0x06004373`|`get_PassedCount`|`()`|`i4`|`7028648`|`4`|`78d4d2062aa5373e...`|
|`0x06004374`|`set_PassedCount`|`i4`|`void`|`7028664`|`4`|`ae3c8b8d99a39542...`|
|`0x06004375`|`get_CapCount`|`()`|`i4`|`7028768`|`4`|`78d4d2062aa5373e...`|
|`0x06004376`|`set_CapCount`|`i4`|`void`|`7028784`|`4`|`ae3c8b8d99a39542...`|
|`0x06004377`|`get_PileCount`|`()`|`i4`|`7028888`|`4`|`78d4d2062aa5373e...`|
|`0x06004378`|`set_PileCount`|`i4`|`void`|`7028904`|`4`|`ae3c8b8d99a39542...`|
|`0x06004379`|`get_MaxRatio`|`()`|`r8`|`7029008`|`4`|`f0ad83c607e59d90...`|
|`0x0600437a`|`set_MaxRatio`|`r8`|`void`|`7029024`|`4`|`ae3c8b8d99a39542...`|
|`0x0600437b`|`get_EstimatedPileCost`|`()`|`r8`|`7029128`|`4`|`f0ad83c607e59d90...`|
|`0x0600437c`|`set_EstimatedPileCost`|`r8`|`void`|`7029144`|`4`|`ae3c8b8d99a39542...`|
|`0x0600437d`|`get_Message`|`()`|`string`|`7029248`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600437e`|`set_Message`|`string`|`void`|`7029264`|`4`|`ae3c8b8d99a39542...`|
|`0x0600437f`|`get_Results`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightPileResult [class:4156]>`|`7029368`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004380`|`set_Results`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightPileResult [class:4156]>`|`void`|`7029384`|`4`|`ae3c8b8d99a39542...`|
|`0x06004381`|`get_Caps`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightCap [class:4152]>`|`7029488`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004382`|`set_Caps`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightCap [class:4152]>`|`void`|`7029504`|`4`|`ae3c8b8d99a39542...`|
|`0x06004383`|`.cctor`|`()`|`void`|`7029608`|`177`|`27c53e05ed46fea9...`|
|`0x06004384`|`CKW5ntqY4FqMLQTHdXH2`|`()`|`bool`|`7029800`|`4`|`66ea1533cb3404e9...`|
|`0x06004385`|`TqxTSXqYYVyusg58SkrJ`|`()`|`Kata_pro64_Cad2025.VmbmLightIteration [class:4164]`|`7029820`|`4`|`8969ce0cdaa4a19c...`|

### [VM-246] Methods of `Kata_pro64_Cad2025.VmbmLightSession` (Token `0x02000412`, Displayed: 117/117, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06004386`|`.ctor`|`()`|`void`|`7029836`|`4`|`ae3c8b8d99a39542...`|
|`0x06004387`|`get_Id`|`()`|`string`|`7030888`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004388`|`set_Id`|`string`|`void`|`7030904`|`4`|`ae3c8b8d99a39542...`|
|`0x06004389`|`get_DrawingPath`|`()`|`string`|`7031008`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600438a`|`set_DrawingPath`|`string`|`void`|`7031024`|`4`|`ae3c8b8d99a39542...`|
|`0x0600438b`|`get_InputFingerprint`|`()`|`string`|`7031128`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600438c`|`set_InputFingerprint`|`string`|`void`|`7031144`|`4`|`ae3c8b8d99a39542...`|
|`0x0600438d`|`get_OriginalModel`|`()`|`string`|`7031248`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600438e`|`set_OriginalModel`|`string`|`void`|`7031264`|`4`|`ae3c8b8d99a39542...`|
|`0x0600438f`|`get_RunDirectory`|`()`|`string`|`7031368`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004390`|`set_RunDirectory`|`string`|`void`|`7031384`|`4`|`ae3c8b8d99a39542...`|
|`0x06004391`|`get_SeedModel`|`()`|`string`|`7031488`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004392`|`set_SeedModel`|`string`|`void`|`7031504`|`4`|`ae3c8b8d99a39542...`|
|`0x06004393`|`get_SelectedModel`|`()`|`string`|`7031608`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004394`|`set_SelectedModel`|`string`|`void`|`7031624`|`4`|`ae3c8b8d99a39542...`|
|`0x06004395`|`get_NextIterationNumber`|`()`|`i4`|`7031728`|`4`|`78d4d2062aa5373e...`|
|`0x06004396`|`set_NextIterationNumber`|`i4`|`void`|`7031744`|`4`|`ae3c8b8d99a39542...`|
|`0x06004397`|`get_Beams`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightBeam [class:4200]>`|`7031848`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004398`|`set_Beams`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightBeam [class:4200]>`|`void`|`7031864`|`4`|`ae3c8b8d99a39542...`|
|`0x06004399`|`get_CadBeamKey`|`()`|`string`|`7031968`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600439a`|`set_CadBeamKey`|`string`|`void`|`7031984`|`4`|`ae3c8b8d99a39542...`|
|`0x0600439b`|`get_CadBeamBindings`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightCadBeamBinding [class:5648]>`|`7032088`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600439c`|`set_CadBeamBindings`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightCadBeamBinding [class:5648]>`|`void`|`7032104`|`4`|`ae3c8b8d99a39542...`|
|`0x0600439d`|`get_AnalysisInputsDirty`|`()`|`bool`|`7032208`|`4`|`66ea1533cb3404e9...`|
|`0x0600439e`|`set_AnalysisInputsDirty`|`bool`|`void`|`7032224`|`4`|`ae3c8b8d99a39542...`|
|`0x0600439f`|`get_OriginalFileStamp`|`()`|`string`|`7032328`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043a0`|`set_OriginalFileStamp`|`string`|`void`|`7032344`|`4`|`ae3c8b8d99a39542...`|
|`0x060043a1`|`get_PublishedToken`|`()`|`string`|`7032448`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043a2`|`set_PublishedToken`|`string`|`void`|`7032464`|`4`|`ae3c8b8d99a39542...`|
|`0x060043a3`|`get_PublishedNameKey`|`()`|`string`|`7032568`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043a4`|`set_PublishedNameKey`|`string`|`void`|`7032584`|`4`|`ae3c8b8d99a39542...`|
|`0x060043a5`|`get_PublishedAt`|`()`|`string`|`7032688`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043a6`|`set_PublishedAt`|`string`|`void`|`7032704`|`4`|`ae3c8b8d99a39542...`|
|`0x060043a7`|`get_LastBackup`|`()`|`string`|`7032808`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043a8`|`set_LastBackup`|`string`|`void`|`7032824`|`4`|`ae3c8b8d99a39542...`|
|`0x060043a9`|`get_LastPublishError`|`()`|`string`|`7032928`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043aa`|`set_LastPublishError`|`string`|`void`|`7032944`|`4`|`ae3c8b8d99a39542...`|
|`0x060043ab`|`get_CadCommitted`|`()`|`bool`|`7033048`|`4`|`66ea1533cb3404e9...`|
|`0x060043ac`|`set_CadCommitted`|`bool`|`void`|`7033064`|`4`|`ae3c8b8d99a39542...`|
|`0x060043ad`|`get_CadBindings`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightCadBinding [class:5644]>`|`7033168`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043ae`|`set_CadBindings`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightCadBinding [class:5644]>`|`void`|`7033184`|`4`|`ae3c8b8d99a39542...`|
|`0x060043af`|`get_CadSnapshot`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightCap [class:4152]>`|`7033288`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043b0`|`set_CadSnapshot`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightCap [class:4152]>`|`void`|`7033304`|`4`|`ae3c8b8d99a39542...`|
|`0x060043b1`|`get_PendingStrips`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightStripBinding [class:5652]>`|`7033408`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043b2`|`set_PendingStrips`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightStripBinding [class:5652]>`|`void`|`7033424`|`4`|`ae3c8b8d99a39542...`|
|`0x060043b3`|`get_SteelToken`|`()`|`string`|`7033528`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043b4`|`set_SteelToken`|`string`|`void`|`7033544`|`4`|`ae3c8b8d99a39542...`|
|`0x060043b5`|`get_SteelSourceToken`|`()`|`string`|`7033648`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043b6`|`set_SteelSourceToken`|`string`|`void`|`7033664`|`4`|`ae3c8b8d99a39542...`|
|`0x060043b7`|`get_SteelNameKey`|`()`|`string`|`7033768`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043b8`|`set_SteelNameKey`|`string`|`void`|`7033784`|`4`|`ae3c8b8d99a39542...`|
|`0x060043b9`|`get_SteelComboCalculated`|`()`|`string`|`7033888`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043ba`|`set_SteelComboCalculated`|`string`|`void`|`7033904`|`4`|`ae3c8b8d99a39542...`|
|`0x060043bb`|`get_SteelModelPath`|`()`|`string`|`7034008`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043bc`|`set_SteelModelPath`|`string`|`void`|`7034024`|`4`|`ae3c8b8d99a39542...`|
|`0x060043bd`|`get_SteelAxes`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightSteelAxis [class:3936]>`|`7034128`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043be`|`set_SteelAxes`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightSteelAxis [class:3936]>`|`void`|`7034144`|`4`|`ae3c8b8d99a39542...`|
|`0x060043bf`|`get_OffsetX`|`()`|`r8`|`7034248`|`4`|`f0ad83c607e59d90...`|
|`0x060043c0`|`set_OffsetX`|`r8`|`void`|`7034264`|`4`|`ae3c8b8d99a39542...`|
|`0x060043c1`|`get_OffsetY`|`()`|`r8`|`7034368`|`4`|`f0ad83c607e59d90...`|
|`0x060043c2`|`set_OffsetY`|`r8`|`void`|`7034384`|`4`|`ae3c8b8d99a39542...`|
|`0x060043c3`|`get_Options`|`()`|`Kata_pro64_Cad2025.VmbmLightOptions [class:4144]`|`7034488`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043c4`|`set_Options`|`Kata_pro64_Cad2025.VmbmLightOptions [class:4144]`|`void`|`7034504`|`4`|`ae3c8b8d99a39542...`|
|`0x060043c5`|`get_Grids`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.info_CurveGrid [class:997]>`|`7034608`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043c6`|`set_Grids`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.info_CurveGrid [class:997]>`|`void`|`7034624`|`4`|`ae3c8b8d99a39542...`|
|`0x060043c7`|`get_FoundationBeams`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Info_Beam3D [class:481]>`|`7034728`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043c8`|`set_FoundationBeams`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Info_Beam3D [class:481]>`|`void`|`7034744`|`4`|`ae3c8b8d99a39542...`|
|`0x060043c9`|`get_FoundationSlabs`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Info_Slab3D [class:1613]>`|`7034848`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043ca`|`set_FoundationSlabs`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Info_Slab3D [class:1613]>`|`void`|`7034864`|`4`|`ae3c8b8d99a39542...`|
|`0x060043cb`|`get_PileTypes`|`()`|`System.Collections.Generic.List`1 [class:217]<CocChiuTai [class:3568]>`|`7034968`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043cc`|`set_PileTypes`|`System.Collections.Generic.List`1 [class:217]<CocChiuTai [class:3568]>`|`void`|`7034984`|`4`|`ae3c8b8d99a39542...`|
|`0x060043cd`|`get_Supports`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightSupport [class:4148]>`|`7035088`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043ce`|`set_Supports`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightSupport [class:4148]>`|`void`|`7035104`|`4`|`ae3c8b8d99a39542...`|
|`0x060043cf`|`get_Caps`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightCap [class:4152]>`|`7035208`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043d0`|`set_Caps`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightCap [class:4152]>`|`void`|`7035224`|`4`|`ae3c8b8d99a39542...`|
|`0x060043d1`|`get_PileBoundary`|`()`|`Polygon [class:89]`|`7035328`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043d2`|`set_PileBoundary`|`Polygon [class:89]`|`void`|`7035344`|`4`|`ae3c8b8d99a39542...`|
|`0x060043d3`|`get_CapBoundary`|`()`|`Polygon [class:89]`|`7035448`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043d4`|`set_CapBoundary`|`Polygon [class:89]`|`void`|`7035464`|`4`|`ae3c8b8d99a39542...`|
|`0x060043d5`|`get_SourceHandles`|`()`|`System.Collections.Generic.List`1 [class:217]<i8>`|`7035568`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043d6`|`set_SourceHandles`|`System.Collections.Generic.List`1 [class:217]<i8>`|`void`|`7035584`|`4`|`ae3c8b8d99a39542...`|
|`0x060043d7`|`get_History`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightIteration [class:4164]>`|`7035688`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043d8`|`set_History`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightIteration [class:4164]>`|`void`|`7035704`|`4`|`ae3c8b8d99a39542...`|
|`0x060043d9`|`get_Messages`|`()`|`System.Collections.Generic.List`1 [class:217]<string>`|`7035808`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043da`|`set_Messages`|`System.Collections.Generic.List`1 [class:217]<string>`|`void`|`7035824`|`4`|`ae3c8b8d99a39542...`|
|`0x060043db`|`get_LastMessage`|`()`|`string`|`7035928`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043dc`|`set_LastMessage`|`string`|`void`|`7035944`|`4`|`ae3c8b8d99a39542...`|
|`0x060043dd`|`get_IsRunning`|`()`|`bool`|`7036048`|`4`|`66ea1533cb3404e9...`|
|`0x060043de`|`set_IsRunning`|`bool`|`void`|`7036064`|`4`|`ae3c8b8d99a39542...`|
|`0x060043df`|`get_CanApply`|`()`|`bool`|`7036168`|`4`|`66ea1533cb3404e9...`|
|`0x060043e0`|`set_CanApply`|`bool`|`void`|`7036184`|`4`|`ae3c8b8d99a39542...`|
|`0x060043e1`|`get_ManualCheckPending`|`()`|`bool`|`7036288`|`4`|`66ea1533cb3404e9...`|
|`0x060043e2`|`set_ManualCheckPending`|`bool`|`void`|`7036304`|`4`|`ae3c8b8d99a39542...`|
|`0x060043e3`|`get_ExistingLayout`|`()`|`bool`|`7036408`|`4`|`66ea1533cb3404e9...`|
|`0x060043e4`|`set_ExistingLayout`|`bool`|`void`|`7036424`|`4`|`ae3c8b8d99a39542...`|
|`0x060043e5`|`get_ExistingCapHandles`|`()`|`System.Collections.Generic.List`1 [class:217]<i8>`|`7036528`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043e6`|`set_ExistingCapHandles`|`System.Collections.Generic.List`1 [class:217]<i8>`|`void`|`7036544`|`4`|`ae3c8b8d99a39542...`|
|`0x060043e7`|`get_ExistingAnnotationHandles`|`()`|`System.Collections.Generic.List`1 [class:217]<i8>`|`7036648`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043e8`|`set_ExistingAnnotationHandles`|`System.Collections.Generic.List`1 [class:217]<i8>`|`void`|`7036664`|`4`|`ae3c8b8d99a39542...`|
|`0x060043e9`|`get_ExistingCadFingerprint`|`()`|`string`|`7036768`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043ea`|`set_ExistingCadFingerprint`|`string`|`void`|`7036784`|`4`|`ae3c8b8d99a39542...`|
|`0x060043eb`|`get_ExistingCadOffsetX`|`()`|`r8`|`7036888`|`4`|`f0ad83c607e59d90...`|
|`0x060043ec`|`set_ExistingCadOffsetX`|`r8`|`void`|`7036904`|`4`|`ae3c8b8d99a39542...`|
|`0x060043ed`|`get_ExistingCadOffsetY`|`()`|`r8`|`7037008`|`4`|`f0ad83c607e59d90...`|
|`0x060043ee`|`set_ExistingCadOffsetY`|`r8`|`void`|`7037024`|`4`|`ae3c8b8d99a39542...`|
|`0x060043ef`|`CopyForEditing`|`()`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`7037128`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043f0`|`get_SourceWarningSignature`|`()`|`string`|`7037888`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043f1`|`set_SourceWarningSignature`|`string`|`void`|`7037904`|`4`|`ae3c8b8d99a39542...`|
|`0x060043f2`|`get_AnalysisWarnings`|`()`|`System.Collections.Generic.List`1 [class:217]<string>`|`7038008`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043f3`|`set_AnalysisWarnings`|`System.Collections.Generic.List`1 [class:217]<string>`|`void`|`7038024`|`4`|`ae3c8b8d99a39542...`|
|`0x060043f4`|`get_CanExportTest`|`()`|`bool`|`7038128`|`4`|`66ea1533cb3404e9...`|
|`0x060043f5`|`set_CanExportTest`|`bool`|`void`|`7038144`|`4`|`ae3c8b8d99a39542...`|
|`0x060043f6`|`SaveReport`|`()`|`void`|`7038248`|`4`|`ae3c8b8d99a39542...`|
|`0x060043f7`|`HUa85U2J9Rx`|`object`|`object`|`7039220`|`4`|`8969ce0cdaa4a19c...`|
|`0x060043f8`|`.cctor`|`()`|`void`|`7039500`|`177`|`34de3c1592d85aae...`|
|`0x060043f9`|`iSclUHqY7sU5biWlmaMx`|`()`|`bool`|`7039692`|`4`|`66ea1533cb3404e9...`|
|`0x060043fa`|`vMKSBGqYFrR1lv2JP2tM`|`()`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`7039712`|`4`|`8969ce0cdaa4a19c...`|

### [VM-247] Methods of `Kata_pro64_Cad2025.VmbmLightCheck` (Token `0x02000415`, Displayed: 10/10, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06004418`|`.ctor`|`()`|`void`|`7039728`|`4`|`ae3c8b8d99a39542...`|
|`0x06004419`|`RequireFreeJoint`|`string, bool[]`|`void`|`7039852`|`4`|`ae3c8b8d99a39542...`|
|`0x0600441a`|`RequireSpringStiffness`|`string, r8[], r8[]`|`void`|`7040172`|`4`|`ae3c8b8d99a39542...`|
|`0x0600441b`|`ZeroStiffnessSignature`|`string`|`string`|`7040784`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600441c`|`AnalysisLogProblem`|`string`|`string`|`7042680`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600441d`|`Finite`|`r8`|`bool`|`7043324`|`4`|`66ea1533cb3404e9...`|
|`0x0600441e`|`Evaluate`|`string, i4, System.Collections.Generic.IEnumerable`1 [class:321]<string>, System.Collections.Generic.IEnumerable`1 [class:321]<Kata_pro64_Cad2025.VmbmLightPileResult [class:4156]>, r8`|`Kata_pro64_Cad2025.VmbmLightCapCheck [class:4160]`|`7043452`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600441f`|`.cctor`|`()`|`void`|`7046148`|`156`|`1c7a5d10ad32cb7d...`|
|`0x06004420`|`xu3dpBqY1IxJbUx9Ta8v`|`()`|`bool`|`7046316`|`4`|`66ea1533cb3404e9...`|
|`0x06004421`|`fgKoDXqYM5B113SVOyUu`|`()`|`Kata_pro64_Cad2025.VmbmLightCheck [class:4180]`|`7046336`|`4`|`8969ce0cdaa4a19c...`|

### [VM-248] Methods of `Kata_pro64_Cad2025.VmbmLightBeam` (Token `0x0200041a`, Displayed: 27/27, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x0600443c`|`.ctor`|`()`|`void`|`7046352`|`4`|`ae3c8b8d99a39542...`|
|`0x0600443d`|`get_Id`|`()`|`string`|`7046568`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600443e`|`set_Id`|`string`|`void`|`7046584`|`4`|`ae3c8b8d99a39542...`|
|`0x0600443f`|`get_Name`|`()`|`string`|`7046688`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004440`|`set_Name`|`string`|`void`|`7046704`|`4`|`ae3c8b8d99a39542...`|
|`0x06004441`|`get_OriginalName`|`()`|`string`|`7046808`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004442`|`set_OriginalName`|`string`|`void`|`7046824`|`4`|`ae3c8b8d99a39542...`|
|`0x06004443`|`get_OriginalB`|`()`|`r8`|`7046928`|`4`|`f0ad83c607e59d90...`|
|`0x06004444`|`set_OriginalB`|`r8`|`void`|`7046944`|`4`|`ae3c8b8d99a39542...`|
|`0x06004445`|`get_OriginalH`|`()`|`r8`|`7047048`|`4`|`f0ad83c607e59d90...`|
|`0x06004446`|`set_OriginalH`|`r8`|`void`|`7047064`|`4`|`ae3c8b8d99a39542...`|
|`0x06004447`|`get_B`|`()`|`r8`|`7047168`|`4`|`f0ad83c607e59d90...`|
|`0x06004448`|`set_B`|`r8`|`void`|`7047184`|`4`|`ae3c8b8d99a39542...`|
|`0x06004449`|`get_H`|`()`|`r8`|`7047288`|`4`|`f0ad83c607e59d90...`|
|`0x0600444a`|`set_H`|`r8`|`void`|`7047304`|`4`|`ae3c8b8d99a39542...`|
|`0x0600444b`|`get_Locked`|`()`|`bool`|`7047408`|`4`|`66ea1533cb3404e9...`|
|`0x0600444c`|`set_Locked`|`bool`|`void`|`7047424`|`4`|`ae3c8b8d99a39542...`|
|`0x0600444d`|`get_Frames`|`()`|`System.Collections.Generic.List`1 [class:217]<string>`|`7047528`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600444e`|`set_Frames`|`System.Collections.Generic.List`1 [class:217]<string>`|`void`|`7047544`|`4`|`ae3c8b8d99a39542...`|
|`0x0600444f`|`get_Check`|`()`|`Kata_pro64_Cad2025.VmbmLightBeamCheck [class:4204]`|`7047648`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004450`|`set_Check`|`Kata_pro64_Cad2025.VmbmLightBeamCheck [class:4204]`|`void`|`7047664`|`4`|`ae3c8b8d99a39542...`|
|`0x06004451`|`Copy`|`()`|`Kata_pro64_Cad2025.VmbmLightBeam [class:4200]`|`7047768`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004452`|`get_Section`|`()`|`string`|`7047828`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004453`|`SectionName`|`r8, r8`|`string`|`7047856`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004454`|`.cctor`|`()`|`void`|`7048048`|`156`|`34f16e234ffa0661...`|
|`0x06004455`|`G2YJKlqYKPoYBQSoL0KU`|`()`|`bool`|`7048216`|`4`|`66ea1533cb3404e9...`|
|`0x06004456`|`TicWGrqYN0QEkRJRp2lS`|`()`|`Kata_pro64_Cad2025.VmbmLightBeam [class:4200]`|`7048236`|`4`|`8969ce0cdaa4a19c...`|

### [VM-249] Methods of `Kata_pro64_Cad2025.VmbmLightBeamCheck` (Token `0x0200041b`, Displayed: 45/45, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06004457`|`.ctor`|`()`|`void`|`7048252`|`4`|`ae3c8b8d99a39542...`|
|`0x06004458`|`get_HasResults`|`()`|`bool`|`7048592`|`4`|`66ea1533cb3404e9...`|
|`0x06004459`|`set_HasResults`|`bool`|`void`|`7048608`|`4`|`ae3c8b8d99a39542...`|
|`0x0600445a`|`get_Passed`|`()`|`bool`|`7048712`|`4`|`66ea1533cb3404e9...`|
|`0x0600445b`|`set_Passed`|`bool`|`void`|`7048728`|`4`|`ae3c8b8d99a39542...`|
|`0x0600445c`|`get_CanResize`|`()`|`bool`|`7048832`|`4`|`66ea1533cb3404e9...`|
|`0x0600445d`|`set_CanResize`|`bool`|`void`|`7048848`|`4`|`ae3c8b8d99a39542...`|
|`0x0600445e`|`get_RequiresReview`|`()`|`bool`|`7048952`|`4`|`66ea1533cb3404e9...`|
|`0x0600445f`|`set_RequiresReview`|`bool`|`void`|`7048968`|`4`|`ae3c8b8d99a39542...`|
|`0x06004460`|`get_BlocksResize`|`()`|`bool`|`7049072`|`4`|`66ea1533cb3404e9...`|
|`0x06004461`|`set_BlocksResize`|`bool`|`void`|`7049088`|`4`|`ae3c8b8d99a39542...`|
|`0x06004462`|`get_NeedsWidth`|`()`|`bool`|`7049192`|`4`|`66ea1533cb3404e9...`|
|`0x06004463`|`set_NeedsWidth`|`bool`|`void`|`7049208`|`4`|`ae3c8b8d99a39542...`|
|`0x06004464`|`get_TopArea`|`()`|`r8`|`7049312`|`4`|`f0ad83c607e59d90...`|
|`0x06004465`|`set_TopArea`|`r8`|`void`|`7049328`|`4`|`ae3c8b8d99a39542...`|
|`0x06004466`|`get_BottomArea`|`()`|`r8`|`7049432`|`4`|`f0ad83c607e59d90...`|
|`0x06004467`|`set_BottomArea`|`r8`|`void`|`7049448`|`4`|`ae3c8b8d99a39542...`|
|`0x06004468`|`get_ShearPerMetre`|`()`|`r8`|`7049552`|`4`|`f0ad83c607e59d90...`|
|`0x06004469`|`set_ShearPerMetre`|`r8`|`void`|`7049568`|`4`|`ae3c8b8d99a39542...`|
|`0x0600446a`|`get_TorsionArea`|`()`|`r8`|`7049672`|`4`|`f0ad83c607e59d90...`|
|`0x0600446b`|`set_TorsionArea`|`r8`|`void`|`7049688`|`4`|`ae3c8b8d99a39542...`|
|`0x0600446c`|`get_TorsionTiePerMetre`|`()`|`r8`|`7049792`|`4`|`f0ad83c607e59d90...`|
|`0x0600446d`|`set_TorsionTiePerMetre`|`r8`|`void`|`7049808`|`4`|`ae3c8b8d99a39542...`|
|`0x0600446e`|`get_TopCombo`|`()`|`string`|`7049912`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600446f`|`set_TopCombo`|`string`|`void`|`7049928`|`4`|`ae3c8b8d99a39542...`|
|`0x06004470`|`get_BottomCombo`|`()`|`string`|`7050032`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004471`|`set_BottomCombo`|`string`|`void`|`7050048`|`4`|`ae3c8b8d99a39542...`|
|`0x06004472`|`get_ShearCombo`|`()`|`string`|`7050152`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004473`|`set_ShearCombo`|`string`|`void`|`7050168`|`4`|`ae3c8b8d99a39542...`|
|`0x06004474`|`get_RatioPercent`|`()`|`r8`|`7050272`|`4`|`f0ad83c607e59d90...`|
|`0x06004475`|`set_RatioPercent`|`r8`|`void`|`7050288`|`4`|`ae3c8b8d99a39542...`|
|`0x06004476`|`get_EffectiveDepth`|`()`|`r8`|`7050392`|`4`|`f0ad83c607e59d90...`|
|`0x06004477`|`set_EffectiveDepth`|`r8`|`void`|`7050408`|`4`|`ae3c8b8d99a39542...`|
|`0x06004478`|`get_Combo`|`()`|`string`|`7050512`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004479`|`set_Combo`|`string`|`void`|`7050528`|`4`|`ae3c8b8d99a39542...`|
|`0x0600447a`|`get_Location`|`()`|`string`|`7050632`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600447b`|`set_Location`|`string`|`void`|`7050648`|`4`|`ae3c8b8d99a39542...`|
|`0x0600447c`|`get_DesignCode`|`()`|`string`|`7050752`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600447d`|`set_DesignCode`|`string`|`void`|`7050768`|`4`|`ae3c8b8d99a39542...`|
|`0x0600447e`|`get_Message`|`()`|`string`|`7050872`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600447f`|`set_Message`|`string`|`void`|`7050888`|`4`|`ae3c8b8d99a39542...`|
|`0x06004480`|`Copy`|`()`|`Kata_pro64_Cad2025.VmbmLightBeamCheck [class:4204]`|`7050992`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004481`|`.cctor`|`()`|`void`|`7051020`|`156`|`af56f654d92bc914...`|
|`0x06004482`|`SOvH2aqYIuSDvFFNOEyu`|`()`|`bool`|`7051188`|`4`|`66ea1533cb3404e9...`|
|`0x06004483`|`aAsX4KqYyEr7gmR18Ta2`|`()`|`Kata_pro64_Cad2025.VmbmLightBeamCheck [class:4204]`|`7051208`|`4`|`8969ce0cdaa4a19c...`|

### [VM-250] Methods of `Kata_pro64_Cad2025.VmbmLightBeamDesign` (Token `0x0200041c`, Displayed: 21/21, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06004484`|`.ctor`|`()`|`void`|`7051224`|`4`|`ae3c8b8d99a39542...`|
|`0x06004485`|`DefaultCombos`|`System.Collections.Generic.IEnumerable`1 [class:321]<string>, System.Collections.Generic.IEnumerable`1 [class:321]<string>`|`System.Collections.Generic.List`1 [class:217]<string>`|`7051348`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004486`|`SettingsKey`|`Kata_pro64_Cad2025.VmbmLightOptions [class:4144]`|`string`|`7051612`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004487`|`ConfigureDesignCode`|`CSiAPIv1.cSapModel [class:2025], string`|`void`|`7051696`|`4`|`ae3c8b8d99a39542...`|
|`0x06004488`|`Ensure`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`void`|`7052364`|`4`|`ae3c8b8d99a39542...`|
|`0x06004489`|`Key`|`System.Collections.Generic.IEnumerable`1 [class:321]<Kata_pro64_Cad2025.VmbmLightBeam [class:4200]>`|`string`|`7052788`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600448a`|`ValidateName`|`string`|`void`|`7052956`|`4`|`ae3c8b8d99a39542...`|
|`0x0600448b`|`Resize`|`Kata_pro64_Cad2025.VmbmLightBeam [class:4200], r8, r8`|`void`|`7053344`|`4`|`ae3c8b8d99a39542...`|
|`0x0600448c`|`StepSection`|`Kata_pro64_Cad2025.VmbmLightBeam [class:4200], string, bool`|`bool`|`7053840`|`4`|`66ea1533cb3404e9...`|
|`0x0600448d`|`RenamedFrame`|`string, string, string`|`string`|`7054672`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600448e`|`Sections`|`string`|`System.Collections.Generic.List`1 [class:217]<System.Tuple`2 [class:285]<r8,r8>>`|`7055240`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600448f`|`TryIncrease`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], System.Action`1 [class:885]<string>`|`bool`|`7056344`|`4`|`66ea1533cb3404e9...`|
|`0x06004490`|`Evaluate`|`r8, r8, r8, r8, r8, r8, r8, r8, string, string, Kata_pro64_Cad2025.VmbmLightOptions [class:4144]`|`Kata_pro64_Cad2025.VmbmLightBeamCheck [class:4204]`|`7059508`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004491`|`ConfigureExport`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], Kata_pro64_Cad2025.EtabsModelExportRequest [class:4280], System.Collections.Generic.Dictionary`2 [class:389]<string,string>`|`void`|`7061532`|`4`|`ae3c8b8d99a39542...`|
|`0x06004492`|`mS98SIGpnmn`|`i4, object`|`void`|`7062120`|`4`|`ae3c8b8d99a39542...`|
|`0x06004493`|`PrepareSections`|`CSiAPIv1.cSapModel [class:2025], System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightBeam [class:4200]>`|`void`|`7062352`|`4`|`ae3c8b8d99a39542...`|
|`0x06004494`|`U5W8SyToM91`|`object, object`|`fk2IxJ0k2IjtkMELjK7P [class:4212]`|`7064292`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004495`|`Check`|`CSiAPIv1.cSapModel [class:2025], Kata_pro64_Cad2025.VmbmLightSession [class:4168], System.Action`1 [class:885]<string>, System.Threading.CancellationToken [valuetype:1413]`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightBeam [class:4200]>`|`7065160`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004496`|`.cctor`|`()`|`void`|`7075796`|`156`|`83c4f3cd90617196...`|
|`0x06004497`|`yacIPDqYgHu153qHgGQ0`|`()`|`bool`|`7075964`|`4`|`66ea1533cb3404e9...`|
|`0x06004498`|`mHWT9uqYhfGVO2Xj6Ofy`|`()`|`Kata_pro64_Cad2025.VmbmLightBeamDesign [class:4208]`|`7075984`|`4`|`8969ce0cdaa4a19c...`|

### [VM-251] Methods of `Kata_pro64_Cad2025.VmbmLightStripTypeConverter` (Token `0x0200042a`, Displayed: 7/7, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06004502`|`.ctor`|`()`|`void`|`7076000`|`4`|`ae3c8b8d99a39542...`|
|`0x06004503`|`GetStandardValuesSupported`|`System.ComponentModel.ITypeDescriptorContext [class:2137]`|`bool`|`7076124`|`4`|`66ea1533cb3404e9...`|
|`0x06004504`|`GetStandardValuesExclusive`|`System.ComponentModel.ITypeDescriptorContext [class:2137]`|`bool`|`7076136`|`4`|`66ea1533cb3404e9...`|
|`0x06004505`|`GetStandardValues`|`System.ComponentModel.ITypeDescriptorContext [class:2137]`|`StandardValuesCollection [class:2141]`|`7076148`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004506`|`.cctor`|`()`|`void`|`7076248`|`156`|`52f6aca9c0198d2b...`|
|`0x06004507`|`M1eANUqYe74JexOHSNTL`|`()`|`bool`|`7076416`|`4`|`66ea1533cb3404e9...`|
|`0x06004508`|`gygtmZqYLi28mfC7iiWH`|`()`|`Kata_pro64_Cad2025.VmbmLightStripTypeConverter [class:4264]`|`7076436`|`4`|`8969ce0cdaa4a19c...`|

### [VM-252] Methods of `Kata_pro64_Cad2025.VmbmLightRebarConverter` (Token `0x0200042b`, Displayed: 7/7, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06004509`|`.ctor`|`()`|`void`|`7076452`|`4`|`ae3c8b8d99a39542...`|
|`0x0600450a`|`ReadMaterials`|`()`|`string[]`|`7076576`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600450b`|`GetStandardValues`|`System.ComponentModel.ITypeDescriptorContext [class:2137]`|`StandardValuesCollection [class:2141]`|`7077136`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600450c`|`RefreshCombo`|`System.Windows.Forms.ComboBox [class:377]`|`void`|`7077156`|`4`|`ae3c8b8d99a39542...`|
|`0x0600450d`|`.cctor`|`()`|`void`|`7077780`|`177`|`4a109b01fc49ccff...`|
|`0x0600450e`|`xp43iDqYfHQ1GOQBHRVc`|`()`|`bool`|`7077972`|`4`|`66ea1533cb3404e9...`|
|`0x0600450f`|`lyPEnvqYcq46ML0gEkkF`|`()`|`Kata_pro64_Cad2025.VmbmLightRebarConverter [class:4268]`|`7077992`|`4`|`8969ce0cdaa4a19c...`|

### [VM-253] Methods of `Kata_pro64_Cad2025.VmbmLightShallowExportRequest` (Token `0x0200042d`, Displayed: 4/4, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06004515`|`.ctor`|`()`|`void`|`7078008`|`4`|`ae3c8b8d99a39542...`|
|`0x06004516`|`.cctor`|`()`|`void`|`7078132`|`156`|`3ae7e522b68e9600...`|
|`0x06004517`|`MS8cJ0qYWK1TvXeh17if`|`()`|`bool`|`7078300`|`4`|`66ea1533cb3404e9...`|
|`0x06004518`|`aqtICWqYsc9GbHdF3Nx1`|`()`|`Kata_pro64_Cad2025.VmbmLightShallowExportRequest [class:4276]`|`7078320`|`4`|`8969ce0cdaa4a19c...`|

### [VM-254] Methods of `Kata_pro64_Cad2025.VmbmLightNewPileExport` (Token `0x0200042f`, Displayed: 5/5, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06004538`|`.ctor`|`()`|`void`|`7080856`|`4`|`ae3c8b8d99a39542...`|
|`0x06004539`|`Run`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], string, CSiAPIv1.cSapModel [class:2025]`|`void`|`7080980`|`4`|`ae3c8b8d99a39542...`|
|`0x0600453a`|`.cctor`|`()`|`void`|`7083540`|`177`|`de6ac49d736cae80...`|
|`0x0600453b`|`qjLMpjqYGOtPaUcWCsXe`|`()`|`bool`|`7083732`|`4`|`66ea1533cb3404e9...`|
|`0x0600453c`|`xs1kBKqYlC2g9MirlwlR`|`()`|`Kata_pro64_Cad2025.VmbmLightNewPileExport [class:4284]`|`7083752`|`4`|`8969ce0cdaa4a19c...`|

### [VM-255] Methods of `Kata_pro64_Cad2025.VmbmLightLegacyExportScope` (Token `0x02000430`, Displayed: 5/5, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x0600453d`|`.ctor`|`()`|`void`|`7083768`|`4`|`ae3c8b8d99a39542...`|
|`0x0600453e`|`Dispose`|`()`|`void`|`7085668`|`4`|`ae3c8b8d99a39542...`|
|`0x0600453f`|`.cctor`|`()`|`void`|`7087788`|`156`|`51e5aea29e94bfde...`|
|`0x06004540`|`KV8R3qqYHON4jur7GNf4`|`()`|`bool`|`7087956`|`4`|`66ea1533cb3404e9...`|
|`0x06004541`|`rnsP0lqYpm3yKYorg9Ad`|`()`|`Kata_pro64_Cad2025.VmbmLightLegacyExportScope [class:4288]`|`7087976`|`4`|`8969ce0cdaa4a19c...`|

### [VM-256] Methods of `Kata_pro64_Cad2025.VmbmLightShallowExport` (Token `0x02000433`, Displayed: 6/6, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x0600454b`|`.ctor`|`()`|`void`|`7087992`|`4`|`ae3c8b8d99a39542...`|
|`0x0600454c`|`Run`|`Kata_pro64_Cad2025.VmbmLightShallowSession [class:4336]`|`void`|`7088116`|`4`|`ae3c8b8d99a39542...`|
|`0x0600454d`|`Check`|`i4, object`|`void`|`7093916`|`4`|`ae3c8b8d99a39542...`|
|`0x0600454e`|`.cctor`|`()`|`void`|`7094076`|`177`|`010f9a263bb82c62...`|
|`0x0600454f`|`rUFH91qY2InqjcqA5gIW`|`()`|`bool`|`7094268`|`4`|`66ea1533cb3404e9...`|
|`0x06004550`|`VGmJGrqYXeGGAXNSj556`|`()`|`Kata_pro64_Cad2025.VmbmLightShallowExport [class:4300]`|`7094288`|`4`|`8969ce0cdaa4a19c...`|

### [VM-257] Methods of `Kata_pro64_Cad2025.VmbmLightExistingPileExport` (Token `0x02000435`, Displayed: 8/8, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06004556`|`.ctor`|`()`|`void`|`7094304`|`4`|`ae3c8b8d99a39542...`|
|`0x06004557`|`Run`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], string, CSiAPIv1.cSapModel [class:2025]`|`void`|`7094428`|`4`|`ae3c8b8d99a39542...`|
|`0x06004558`|`MJJ8SuktfaK`|`object`|`System.Collections.Generic.List`1 [class:217]<i8>`|`7097288`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004559`|`fwa8SHYrSd2`|`System.Collections.Generic.IEnumerable`1 [class:321]<i8>, r8, r8, System.Collections.Generic.Dictionary`2 [class:389]<string,string>`|`System.Collections.Generic.List`1 [class:217]<i8>`|`7097324`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600455a`|`gKR8Spa65YO`|`System.Collections.Generic.IEnumerable`1 [class:321]<i8>`|`void`|`7100936`|`4`|`ae3c8b8d99a39542...`|
|`0x0600455b`|`.cctor`|`()`|`void`|`7102900`|`156`|`9fb7a24895a03b77...`|
|`0x0600455c`|`eaENCUqYn1SmbP1NMu6X`|`()`|`bool`|`7103068`|`4`|`66ea1533cb3404e9...`|
|`0x0600455d`|`l1yFECqY5tfqmAwHl5tQ`|`()`|`Kata_pro64_Cad2025.VmbmLightExistingPileExport [class:4308]`|`7103088`|`4`|`8969ce0cdaa4a19c...`|

### [VM-258] Methods of `Kata_pro64_Cad2025.VmbmLightShallowExportForm` (Token `0x02000436`, Displayed: 9/9, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x0600455e`|`get_StoryName`|`()`|`string`|`7103104`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600455f`|`get_FoundationConcrete`|`()`|`string`|`7103140`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004560`|`get_BeamConcrete`|`()`|`string`|`7103176`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004561`|`get_MakeCopy`|`()`|`bool`|`7103212`|`4`|`66ea1533cb3404e9...`|
|`0x06004562`|`get_Analyze`|`()`|`bool`|`7103240`|`4`|`66ea1533cb3404e9...`|
|`0x06004563`|`.ctor`|`string[], r8[], string[], string[], Kata_pro64_Cad2025.VmbmLightShallowSettings [class:4332]`|`void`|`7103268`|`4`|`ae3c8b8d99a39542...`|
|`0x06004564`|`.cctor`|`()`|`void`|`7107408`|`156`|`5379bc471bd216da...`|
|`0x06004565`|`plYGuMqYjt4T9DGHOQ0F`|`()`|`bool`|`7107576`|`4`|`66ea1533cb3404e9...`|
|`0x06004566`|`XfEx53qYxiSb7q9wHYJ4`|`()`|`Kata_pro64_Cad2025.VmbmLightShallowExportForm [class:4312]`|`7107596`|`4`|`8969ce0cdaa4a19c...`|

### [VM-259] Methods of `Kata_pro64_Cad2025.VmbmLightFootingInput` (Token `0x0200043a`, Displayed: 39/39, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06004573`|`.ctor`|`()`|`void`|`7107612`|`4`|`ae3c8b8d99a39542...`|
|`0x06004574`|`get_Id`|`()`|`string`|`7108432`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004575`|`set_Id`|`string`|`void`|`7108448`|`4`|`ae3c8b8d99a39542...`|
|`0x06004576`|`get_Name`|`()`|`string`|`7108552`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004577`|`set_Name`|`string`|`void`|`7108568`|`4`|`ae3c8b8d99a39542...`|
|`0x06004578`|`get_Left`|`()`|`r8`|`7108672`|`4`|`f0ad83c607e59d90...`|
|`0x06004579`|`set_Left`|`r8`|`void`|`7108688`|`4`|`ae3c8b8d99a39542...`|
|`0x0600457a`|`get_Right`|`()`|`r8`|`7108792`|`4`|`f0ad83c607e59d90...`|
|`0x0600457b`|`set_Right`|`r8`|`void`|`7108808`|`4`|`ae3c8b8d99a39542...`|
|`0x0600457c`|`get_Top`|`()`|`r8`|`7108912`|`4`|`f0ad83c607e59d90...`|
|`0x0600457d`|`set_Top`|`r8`|`void`|`7108928`|`4`|`ae3c8b8d99a39542...`|
|`0x0600457e`|`get_Bottom`|`()`|`r8`|`7109032`|`4`|`f0ad83c607e59d90...`|
|`0x0600457f`|`set_Bottom`|`r8`|`void`|`7109048`|`4`|`ae3c8b8d99a39542...`|
|`0x06004580`|`get_Blinding`|`()`|`r8`|`7109152`|`4`|`f0ad83c607e59d90...`|
|`0x06004581`|`set_Blinding`|`r8`|`void`|`7109168`|`4`|`ae3c8b8d99a39542...`|
|`0x06004582`|`get_BottomElevation`|`()`|`r8`|`7109272`|`4`|`f0ad83c607e59d90...`|
|`0x06004583`|`set_BottomElevation`|`r8`|`void`|`7109288`|`4`|`ae3c8b8d99a39542...`|
|`0x06004584`|`get_EdgeHeight`|`()`|`r8`|`7109392`|`4`|`f0ad83c607e59d90...`|
|`0x06004585`|`set_EdgeHeight`|`r8`|`void`|`7109408`|`4`|`ae3c8b8d99a39542...`|
|`0x06004586`|`get_Height`|`()`|`r8`|`7109512`|`4`|`f0ad83c607e59d90...`|
|`0x06004587`|`set_Height`|`r8`|`void`|`7109528`|`4`|`ae3c8b8d99a39542...`|
|`0x06004588`|`get_Neck`|`()`|`r8`|`7109632`|`4`|`f0ad83c607e59d90...`|
|`0x06004589`|`set_Neck`|`r8`|`void`|`7109648`|`4`|`ae3c8b8d99a39542...`|
|`0x0600458a`|`get_Cover`|`()`|`r8`|`7109752`|`4`|`f0ad83c607e59d90...`|
|`0x0600458b`|`set_Cover`|`r8`|`void`|`7109768`|`4`|`ae3c8b8d99a39542...`|
|`0x0600458c`|`get_SteelX`|`()`|`string`|`7109872`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600458d`|`set_SteelX`|`string`|`void`|`7109888`|`4`|`ae3c8b8d99a39542...`|
|`0x0600458e`|`get_SteelY`|`()`|`string`|`7109992`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600458f`|`set_SteelY`|`string`|`void`|`7110008`|`4`|`ae3c8b8d99a39542...`|
|`0x06004590`|`get_ColumnVertices`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`7110112`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004591`|`set_ColumnVertices`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`void`|`7110128`|`4`|`ae3c8b8d99a39542...`|
|`0x06004592`|`get_Angle`|`()`|`r8`|`7110232`|`4`|`f0ad83c607e59d90...`|
|`0x06004593`|`set_Angle`|`r8`|`void`|`7110248`|`4`|`ae3c8b8d99a39542...`|
|`0x06004594`|`get_StripIndex`|`()`|`i4`|`7110352`|`4`|`78d4d2062aa5373e...`|
|`0x06004595`|`set_StripIndex`|`i4`|`void`|`7110368`|`4`|`ae3c8b8d99a39542...`|
|`0x06004596`|`Copy`|`()`|`Kata_pro64_Cad2025.VmbmLightFootingInput [class:4328]`|`7110472`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004597`|`.cctor`|`()`|`void`|`7110500`|`177`|`a01a77674b384180...`|
|`0x06004598`|`igGZDtqYasB9TC2Gh5eC`|`()`|`bool`|`7110692`|`4`|`66ea1533cb3404e9...`|
|`0x06004599`|`GdPeUnqYzEnde35dqK2v`|`()`|`Kata_pro64_Cad2025.VmbmLightFootingInput [class:4328]`|`7110712`|`4`|`8969ce0cdaa4a19c...`|

### [VM-260] Methods of `Kata_pro64_Cad2025.VmbmLightShallowSettings` (Token `0x0200043b`, Displayed: 54/54, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x0600459a`|`.ctor`|`()`|`void`|`7110728`|`4`|`ae3c8b8d99a39542...`|
|`0x0600459b`|`get_Left`|`()`|`r8`|`7111840`|`4`|`f0ad83c607e59d90...`|
|`0x0600459c`|`set_Left`|`r8`|`void`|`7111856`|`4`|`ae3c8b8d99a39542...`|
|`0x0600459d`|`get_Right`|`()`|`r8`|`7111960`|`4`|`f0ad83c607e59d90...`|
|`0x0600459e`|`set_Right`|`r8`|`void`|`7111976`|`4`|`ae3c8b8d99a39542...`|
|`0x0600459f`|`get_Top`|`()`|`r8`|`7112080`|`4`|`f0ad83c607e59d90...`|
|`0x060045a0`|`set_Top`|`r8`|`void`|`7112096`|`4`|`ae3c8b8d99a39542...`|
|`0x060045a1`|`get_Bottom`|`()`|`r8`|`7112200`|`4`|`f0ad83c607e59d90...`|
|`0x060045a2`|`set_Bottom`|`r8`|`void`|`7112216`|`4`|`ae3c8b8d99a39542...`|
|`0x060045a3`|`get_Blinding`|`()`|`r8`|`7112320`|`4`|`f0ad83c607e59d90...`|
|`0x060045a4`|`set_Blinding`|`r8`|`void`|`7112336`|`4`|`ae3c8b8d99a39542...`|
|`0x060045a5`|`get_BottomElevation`|`()`|`r8`|`7112440`|`4`|`f0ad83c607e59d90...`|
|`0x060045a6`|`set_BottomElevation`|`r8`|`void`|`7112456`|`4`|`ae3c8b8d99a39542...`|
|`0x060045a7`|`get_EdgeHeight`|`()`|`r8`|`7112560`|`4`|`f0ad83c607e59d90...`|
|`0x060045a8`|`set_EdgeHeight`|`r8`|`void`|`7112576`|`4`|`ae3c8b8d99a39542...`|
|`0x060045a9`|`get_Height`|`()`|`r8`|`7112680`|`4`|`f0ad83c607e59d90...`|
|`0x060045aa`|`set_Height`|`r8`|`void`|`7112696`|`4`|`ae3c8b8d99a39542...`|
|`0x060045ab`|`get_Neck`|`()`|`r8`|`7112800`|`4`|`f0ad83c607e59d90...`|
|`0x060045ac`|`set_Neck`|`r8`|`void`|`7112816`|`4`|`ae3c8b8d99a39542...`|
|`0x060045ad`|`get_Cover`|`()`|`r8`|`7112920`|`4`|`f0ad83c607e59d90...`|
|`0x060045ae`|`set_Cover`|`r8`|`void`|`7112936`|`4`|`ae3c8b8d99a39542...`|
|`0x060045af`|`get_Prefix`|`()`|`string`|`7113040`|`4`|`8969ce0cdaa4a19c...`|
|`0x060045b0`|`set_Prefix`|`string`|`void`|`7113056`|`4`|`ae3c8b8d99a39542...`|
|`0x060045b1`|`get_StartNumber`|`()`|`i4`|`7113160`|`4`|`78d4d2062aa5373e...`|
|`0x060045b2`|`set_StartNumber`|`i4`|`void`|`7113176`|`4`|`ae3c8b8d99a39542...`|
|`0x060045b3`|`get_NamePosition`|`()`|`ViTriDatTen [valuetype:3564]`|`7113280`|`8`|`c41f727e2e00669d...`|
|`0x060045b4`|`set_NamePosition`|`ViTriDatTen [valuetype:3564]`|`void`|`7113296`|`4`|`ae3c8b8d99a39542...`|
|`0x060045b5`|`get_Scale`|`()`|`i4`|`7113400`|`4`|`78d4d2062aa5373e...`|
|`0x060045b6`|`set_Scale`|`i4`|`void`|`7113416`|`4`|`ae3c8b8d99a39542...`|
|`0x060045b7`|`get_LegacyAreaGrouping`|`()`|`bool`|`7113520`|`4`|`66ea1533cb3404e9...`|
|`0x060045b8`|`set_LegacyAreaGrouping`|`bool`|`void`|`7113536`|`4`|`ae3c8b8d99a39542...`|
|`0x060045b9`|`get_SpringName`|`()`|`string`|`7113640`|`4`|`8969ce0cdaa4a19c...`|
|`0x060045ba`|`set_SpringName`|`string`|`void`|`7113656`|`4`|`ae3c8b8d99a39542...`|
|`0x060045bb`|`get_Spring1`|`()`|`r8`|`7113760`|`4`|`f0ad83c607e59d90...`|
|`0x060045bc`|`set_Spring1`|`r8`|`void`|`7113776`|`4`|`ae3c8b8d99a39542...`|
|`0x060045bd`|`get_Spring2`|`()`|`r8`|`7113880`|`4`|`f0ad83c607e59d90...`|
|`0x060045be`|`set_Spring2`|`r8`|`void`|`7113896`|`4`|`ae3c8b8d99a39542...`|
|`0x060045bf`|`get_Spring3`|`()`|`r8`|`7114000`|`4`|`f0ad83c607e59d90...`|
|`0x060045c0`|`set_Spring3`|`r8`|`void`|`7114016`|`4`|`ae3c8b8d99a39542...`|
|`0x060045c1`|`get_Concrete`|`()`|`string`|`7114120`|`4`|`8969ce0cdaa4a19c...`|
|`0x060045c2`|`set_Concrete`|`string`|`void`|`7114136`|`4`|`ae3c8b8d99a39542...`|
|`0x060045c3`|`get_BeamConcrete`|`()`|`string`|`7114240`|`4`|`8969ce0cdaa4a19c...`|
|`0x060045c4`|`set_BeamConcrete`|`string`|`void`|`7114256`|`4`|`ae3c8b8d99a39542...`|
|`0x060045c5`|`get_StripWidth`|`()`|`string`|`7114360`|`4`|`8969ce0cdaa4a19c...`|
|`0x060045c6`|`set_StripWidth`|`string`|`void`|`7114376`|`4`|`ae3c8b8d99a39542...`|
|`0x060045c7`|`get_StripDesignType`|`()`|`string`|`7114480`|`4`|`8969ce0cdaa4a19c...`|
|`0x060045c8`|`set_StripDesignType`|`string`|`void`|`7114496`|`4`|`ae3c8b8d99a39542...`|
|`0x060045c9`|`get_RebarMaterial`|`()`|`string`|`7114600`|`4`|`8969ce0cdaa4a19c...`|
|`0x060045ca`|`set_RebarMaterial`|`string`|`void`|`7114616`|`4`|`ae3c8b8d99a39542...`|
|`0x060045cb`|`NewFooting`|`()`|`Kata_pro64_Cad2025.VmbmLightFootingInput [class:4328]`|`7114720`|`4`|`8969ce0cdaa4a19c...`|
|`0x060045cc`|`Validate`|`()`|`void`|`7114868`|`4`|`ae3c8b8d99a39542...`|
|`0x060045cd`|`.cctor`|`()`|`void`|`7115612`|`177`|`9afbca0131033499...`|
|`0x060045ce`|`roowH2qo8DvhRhstPrvo`|`()`|`bool`|`7115804`|`4`|`66ea1533cb3404e9...`|
|`0x060045cf`|`IKFaOGqoENvWJdj5Qra7`|`()`|`Kata_pro64_Cad2025.VmbmLightShallowSettings [class:4332]`|`7115824`|`4`|`8969ce0cdaa4a19c...`|

### [VM-261] Methods of `Kata_pro64_Cad2025.VmbmLightShallowSession` (Token `0x0200043c`, Displayed: 56/56, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x060045d0`|`.ctor`|`()`|`void`|`7115840`|`4`|`ae3c8b8d99a39542...`|
|`0x060045d1`|`get_Kind`|`()`|`Kata_pro64_Cad2025.VmbmLightShallowKind [valuetype:4324]`|`7116512`|`8`|`327a33a272e7751c...`|
|`0x060045d2`|`set_Kind`|`Kata_pro64_Cad2025.VmbmLightShallowKind [valuetype:4324]`|`void`|`7116528`|`4`|`ae3c8b8d99a39542...`|
|`0x060045d3`|`get_Id`|`()`|`string`|`7116632`|`4`|`8969ce0cdaa4a19c...`|
|`0x060045d4`|`set_Id`|`string`|`void`|`7116648`|`4`|`ae3c8b8d99a39542...`|
|`0x060045d5`|`get_DrawingPath`|`()`|`string`|`7116752`|`4`|`8969ce0cdaa4a19c...`|
|`0x060045d6`|`set_DrawingPath`|`string`|`void`|`7116768`|`4`|`ae3c8b8d99a39542...`|
|`0x060045d7`|`get_Settings`|`()`|`Kata_pro64_Cad2025.VmbmLightShallowSettings [class:4332]`|`7116872`|`4`|`8969ce0cdaa4a19c...`|
|`0x060045d8`|`set_Settings`|`Kata_pro64_Cad2025.VmbmLightShallowSettings [class:4332]`|`void`|`7116888`|`4`|`ae3c8b8d99a39542...`|
|`0x060045d9`|`get_Items`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightFootingInput [class:4328]>`|`7116992`|`4`|`8969ce0cdaa4a19c...`|
|`0x060045da`|`set_Items`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightFootingInput [class:4328]>`|`void`|`7117008`|`4`|`ae3c8b8d99a39542...`|
|`0x060045db`|`get_Boundary`|`()`|`Polygon [class:89]`|`7117112`|`4`|`8969ce0cdaa4a19c...`|
|`0x060045dc`|`set_Boundary`|`Polygon [class:89]`|`void`|`7117128`|`4`|`ae3c8b8d99a39542...`|
|`0x060045dd`|`get_StripModel`|`()`|`InfoMBMongBang [class:3548]`|`7117232`|`4`|`8969ce0cdaa4a19c...`|
|`0x060045de`|`set_StripModel`|`InfoMBMongBang [class:3548]`|`void`|`7117248`|`4`|`ae3c8b8d99a39542...`|
|`0x060045df`|`get_Columns`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Info_ColumnWall3D [class:1001]>`|`7117352`|`4`|`8969ce0cdaa4a19c...`|
|`0x060045e0`|`set_Columns`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Info_ColumnWall3D [class:1001]>`|`void`|`7117368`|`4`|`ae3c8b8d99a39542...`|
|`0x060045e1`|`get_Beams`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Info_Beam3D [class:481]>`|`7117472`|`4`|`8969ce0cdaa4a19c...`|
|`0x060045e2`|`set_Beams`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Info_Beam3D [class:481]>`|`void`|`7117488`|`4`|`ae3c8b8d99a39542...`|
|`0x060045e3`|`get_Walls`|`()`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Info_Beam3D [class:481]>`|`7117592`|`4`|`8969ce0cdaa4a19c...`|
|`0x060045e4`|`set_Walls`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Info_Beam3D [class:481]>`|`void`|`7117608`|`4`|`ae3c8b8d99a39542...`|
|`0x060045e5`|`get_SourceHandles`|`()`|`System.Collections.Generic.List`1 [class:217]<i8>`|`7117712`|`4`|`8969ce0cdaa4a19c...`|
|`0x060045e6`|`set_SourceHandles`|`System.Collections.Generic.List`1 [class:217]<i8>`|`void`|`7117728`|`4`|`ae3c8b8d99a39542...`|
|`0x060045e7`|`get_OutputHandles`|`()`|`System.Collections.Generic.List`1 [class:217]<i8>`|`7117832`|`4`|`8969ce0cdaa4a19c...`|
|`0x060045e8`|`set_OutputHandles`|`System.Collections.Generic.List`1 [class:217]<i8>`|`void`|`7117848`|`4`|`ae3c8b8d99a39542...`|
|`0x060045e9`|`get_PlacedSourceHandles`|`()`|`System.Collections.Generic.List`1 [class:217]<i8>`|`7117952`|`4`|`8969ce0cdaa4a19c...`|
|`0x060045ea`|`set_PlacedSourceHandles`|`System.Collections.Generic.List`1 [class:217]<i8>`|`void`|`7117968`|`4`|`ae3c8b8d99a39542...`|
|`0x060045eb`|`get_Placement`|`()`|`Kata_Class_Lib_Revit.diem [class:85]`|`7118072`|`4`|`8969ce0cdaa4a19c...`|
|`0x060045ec`|`set_Placement`|`Kata_Class_Lib_Revit.diem [class:85]`|`void`|`7118088`|`4`|`ae3c8b8d99a39542...`|
|`0x060045ed`|`get_OriginalModel`|`()`|`string`|`7118192`|`4`|`8969ce0cdaa4a19c...`|
|`0x060045ee`|`set_OriginalModel`|`string`|`void`|`7118208`|`4`|`ae3c8b8d99a39542...`|
|`0x060045ef`|`get_LastModel`|`()`|`string`|`7118312`|`4`|`8969ce0cdaa4a19c...`|
|`0x060045f0`|`set_LastModel`|`string`|`void`|`7118328`|`4`|`ae3c8b8d99a39542...`|
|`0x060045f1`|`get_LastExportModel`|`()`|`string`|`7118432`|`4`|`8969ce0cdaa4a19c...`|
|`0x060045f2`|`set_LastExportModel`|`string`|`void`|`7118448`|`4`|`ae3c8b8d99a39542...`|
|`0x060045f3`|`get_Status`|`()`|`string`|`7118552`|`4`|`8969ce0cdaa4a19c...`|
|`0x060045f4`|`set_Status`|`string`|`void`|`7118568`|`4`|`ae3c8b8d99a39542...`|
|`0x060045f5`|`get_Dirty`|`()`|`bool`|`7118672`|`4`|`66ea1533cb3404e9...`|
|`0x060045f6`|`set_Dirty`|`bool`|`void`|`7118688`|`4`|`ae3c8b8d99a39542...`|
|`0x060045f7`|`Snapshot`|`()`|`string`|`7118792`|`4`|`8969ce0cdaa4a19c...`|
|`0x060045f8`|`Restore`|`string`|`void`|`7118916`|`4`|`ae3c8b8d99a39542...`|
|`0x060045f9`|`Changed`|`()`|`void`|`7119444`|`4`|`ae3c8b8d99a39542...`|
|`0x060045fa`|`Regenerate`|`()`|`void`|`7119648`|`4`|`ae3c8b8d99a39542...`|
|`0x060045fb`|`Isolated`|`Kata_pro64_Cad2025.VmbmLightFootingInput [class:4328]`|`CotChiuTaiMongDon [class:3624]`|`7121744`|`4`|`8969ce0cdaa4a19c...`|
|`0x060045fc`|`Outlines`|`()`|`System.Collections.Generic.List`1 [class:217]<Polygon [class:89]>`|`7122824`|`4`|`8969ce0cdaa4a19c...`|
|`0x060045fd`|`FitIsolatedBoundary`|`()`|`void`|`7123100`|`4`|`ae3c8b8d99a39542...`|
|`0x060045fe`|`ValidateGeometry`|`()`|`void`|`7124448`|`4`|`ae3c8b8d99a39542...`|
|`0x060045ff`|`Rename`|`()`|`void`|`7125812`|`4`|`ae3c8b8d99a39542...`|
|`0x06004600`|`N`|`r8`|`string`|`7127144`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004601`|`ContourKey`|`Polygon [class:89]`|`string`|`7127176`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004602`|`MakePolygon`|`System.Collections.Generic.IEnumerable`1 [class:321]<Kata_Class_Lib_Revit.diem [class:85]>`|`Polygon [class:89]`|`7127508`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004603`|`_Lambda$__82-0`|`Kata_pro64_Cad2025.VmbmLightFootingInput [class:4328]`|`Polygon [class:89]`|`7127912`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004604`|`_Lambda$__85-0`|`Kata_pro64_Cad2025.VmbmLightFootingInput [class:4328]`|`r8`|`7127936`|`4`|`f0ad83c607e59d90...`|
|`0x06004605`|`.cctor`|`()`|`void`|`7128080`|`156`|`6e9ed82613f90727...`|
|`0x06004606`|`jqk5rRqoPkvIUHxNJ3vN`|`()`|`bool`|`7128248`|`4`|`66ea1533cb3404e9...`|
|`0x06004607`|`mxcGrRqo3rmpDnnmhlyp`|`()`|`Kata_pro64_Cad2025.VmbmLightShallowSession [class:4336]`|`7128268`|`4`|`8969ce0cdaa4a19c...`|

### [VM-262] Methods of `Kata_pro64_Cad2025.VmbmLightShallowCad` (Token `0x02000441`, Displayed: 9/9, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06004621`|`.ctor`|`()`|`void`|`7128284`|`4`|`ae3c8b8d99a39542...`|
|`0x06004622`|`Read`|`Kata_pro64_Cad2025.VmbmLightShallowKind [valuetype:4324], Kata_pro64_Cad2025.VmbmLightShallowSettings [class:4332]`|`Kata_pro64_Cad2025.VmbmLightShallowSession [class:4336]`|`7128408`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004623`|`Objects`|`System.Collections.Generic.IEnumerable`1 [class:321]<i8>`|`System.Collections.Generic.List`1 [class:217]<object>`|`7137752`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004624`|`Draw`|`Kata_pro64_Cad2025.VmbmLightShallowSession [class:4336], bool`|`bool`|`7139376`|`4`|`66ea1533cb3404e9...`|
|`0x06004625`|`Pot8jSwiNZM`|`object, object, object, object`|`void`|`7147640`|`4`|`ae3c8b8d99a39542...`|
|`0x06004626`|`qum8jjyoRav`|`object, object, object, object, bool`|`Autodesk.AutoCAD.DatabaseServices.Polyline [class:1017]`|`7147996`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004627`|`.cctor`|`()`|`void`|`7148664`|`156`|`38c04ae504b99727...`|
|`0x06004628`|`YCOYAWqoAf7LA6KJkcBF`|`()`|`bool`|`7148832`|`4`|`66ea1533cb3404e9...`|
|`0x06004629`|`xmKky4qoCIVKS64hgmYR`|`()`|`Kata_pro64_Cad2025.VmbmLightShallowCad [class:4356]`|`7148852`|`4`|`8969ce0cdaa4a19c...`|

### [VM-263] Methods of `Kata_pro64_Cad2025.IVmbmLightAnalysis` (Token `0x02000446`, Displayed: 4/4, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x0600464a`|`Prepare`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], System.Action`1 [class:885]<string>, System.Threading.CancellationToken [valuetype:1413]`|`void`|`0`|`0`|`-`|
|`0x0600464b`|`Analyze`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], i4, System.Action`1 [class:885]<string>, System.Threading.CancellationToken [valuetype:1413]`|`Kata_pro64_Cad2025.VmbmLightIteration [class:4164]`|`0`|`0`|`-`|
|`0x0600464c`|`FinalizeResult`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], string, System.Action`1 [class:885]<string>`|`void`|`0`|`0`|`-`|
|`0x0600464d`|`OpenResult`|`string`|`void`|`0`|`0`|`-`|

### [VM-264] Methods of `Kata_pro64_Cad2025.VmbmLightEngine` (Token `0x02000447`, Displayed: 12/12, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x0600464e`|`.ctor`|`()`|`void`|`7148868`|`4`|`ae3c8b8d99a39542...`|
|`0x0600464f`|`Run`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], Kata_pro64_Cad2025.IVmbmLightAnalysis [class:4376], System.Action`1 [class:885]<string>, System.Threading.CancellationToken [valuetype:1413], bool`|`void`|`7148992`|`4`|`ae3c8b8d99a39542...`|
|`0x06004650`|`ValidateManualLayout`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], bool`|`void`|`7159616`|`4`|`ae3c8b8d99a39542...`|
|`0x06004651`|`ECv8jxUKhhj`|`object`|`void`|`7162692`|`4`|`ae3c8b8d99a39542...`|
|`0x06004652`|`bVn8jDaDN0a`|`object, object`|`bool`|`7163748`|`4`|`66ea1533cb3404e9...`|
|`0x06004653`|`ValidatePileTypes`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`void`|`7164804`|`4`|`ae3c8b8d99a39542...`|
|`0x06004654`|`CreateCap`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], System.Collections.Generic.List`1 [class:217]<string>, CocChiuTai [class:3568], i4`|`Kata_pro64_Cad2025.VmbmLightCap [class:4152]`|`7166224`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004655`|`XUX8jBwYFtR`|`object, System.Action`1 [class:885]<string>`|`void`|`7168532`|`4`|`ae3c8b8d99a39542...`|
|`0x06004656`|`ValidateGeometry`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], bool`|`void`|`7171452`|`4`|`ae3c8b8d99a39542...`|
|`0x06004657`|`.cctor`|`()`|`void`|`7174172`|`177`|`fff4c22ae657488c...`|
|`0x06004658`|`FNQnreqoYII0vK1NRhnk`|`()`|`bool`|`7174364`|`4`|`66ea1533cb3404e9...`|
|`0x06004659`|`mtfZnYqooQ2w9hkg8R1f`|`()`|`Kata_pro64_Cad2025.VmbmLightEngine [class:4380]`|`7174384`|`4`|`8969ce0cdaa4a19c...`|

### [VM-265] Methods of `Kata_pro64_Cad2025.VmbmLightGeometry` (Token `0x02000454`, Displayed: 7/7, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x060046c2`|`.ctor`|`()`|`void`|`7174400`|`4`|`ae3c8b8d99a39542...`|
|`0x060046c3`|`EdgeDistance`|`Polygon [class:89], Kata_Class_Lib_Revit.diem [class:85]`|`r8`|`7174524`|`4`|`f0ad83c607e59d90...`|
|`0x060046c4`|`ContainsPolygon`|`Polygon [class:89], Polygon [class:89]`|`bool`|`7174548`|`4`|`66ea1533cb3404e9...`|
|`0x060046c5`|`Distance`|`Polygon [class:89], Polygon [class:89]`|`r8`|`7175600`|`4`|`f0ad83c607e59d90...`|
|`0x060046c6`|`.cctor`|`()`|`void`|`7176548`|`156`|`c84d10e5e0bdaed0...`|
|`0x060046c7`|`i3bACGqoFsKc1CtixkKC`|`()`|`bool`|`7176716`|`4`|`66ea1533cb3404e9...`|
|`0x060046c8`|`uTiTo4qovXBGvDH5WRtw`|`()`|`Kata_pro64_Cad2025.VmbmLightGeometry [class:4432]`|`7176736`|`4`|`8969ce0cdaa4a19c...`|

### [VM-266] Methods of `Kata_pro64_Cad2025.VmbmLightEditing` (Token `0x02000456`, Displayed: 24/24, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x060046d1`|`.ctor`|`()`|`void`|`7176752`|`4`|`ae3c8b8d99a39542...`|
|`0x060046d2`|`RefreshPileDeclarations`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`void`|`7176876`|`4`|`ae3c8b8d99a39542...`|
|`0x060046d3`|`Changed`|`Kata_pro64_Cad2025.VmbmLightCap [class:4152]`|`void`|`7177960`|`4`|`ae3c8b8d99a39542...`|
|`0x060046d4`|`Rebuild`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], Kata_pro64_Cad2025.VmbmLightCap [class:4152], i4, string, r8, bool, string`|`void`|`7178216`|`4`|`ae3c8b8d99a39542...`|
|`0x060046d5`|`Rotate`|`Kata_pro64_Cad2025.VmbmLightCap [class:4152]`|`void`|`7179360`|`4`|`ae3c8b8d99a39542...`|
|`0x060046d6`|`Rotate`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], Kata_pro64_Cad2025.VmbmLightCap [class:4152]`|`void`|`7181340`|`4`|`ae3c8b8d99a39542...`|
|`0x060046d7`|`oBj8jOIXs7A`|`object, System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightSupport [class:4148]>, object`|`void`|`7182572`|`4`|`ae3c8b8d99a39542...`|
|`0x060046d8`|`YJC8jViecEY`|`object, object, System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightSupport [class:4148]>, bool`|`Kata_Class_Lib_Revit.diem [class:85]`|`7184808`|`4`|`8969ce0cdaa4a19c...`|
|`0x060046d9`|`yM98jaGF7LO`|`System.Collections.Generic.List`1 [class:217]<YvfUhy048IWt1sZiCW8B [class:4444]>, object, System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, bool, object`|`void`|`7186724`|`4`|`ae3c8b8d99a39542...`|
|`0x060046da`|`DefaultMergeGap`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`r8`|`7190328`|`4`|`f0ad83c607e59d90...`|
|`0x060046db`|`Redistribute`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], Kata_pro64_Cad2025.VmbmLightCap [class:4152]`|`void`|`7190740`|`4`|`ae3c8b8d99a39542...`|
|`0x060046dc`|`Merge`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], System.Collections.Generic.List`1 [class:217]<string>`|`void`|`7191044`|`4`|`ae3c8b8d99a39542...`|
|`0x060046dd`|`MergeNearby`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], System.Collections.Generic.List`1 [class:217]<string>, r8`|`void`|`7192380`|`4`|`ae3c8b8d99a39542...`|
|`0x060046de`|`BindSupports`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], Kata_pro64_Cad2025.VmbmLightCap [class:4152]`|`void`|`7193412`|`4`|`ae3c8b8d99a39542...`|
|`0x060046df`|`ReadCad`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], object, bool`|`System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightCap [class:4152]>`|`7194508`|`4`|`8969ce0cdaa4a19c...`|
|`0x060046e0`|`fyf8jzOfdsf`|`object, System.Collections.Generic.List`1 [class:217]<Kata_pro64_Cad2025.VmbmLightCap [class:4152]>, object, System.Collections.Generic.List`1 [class:217]<string>, bool, object`|`void`|`7206264`|`4`|`ae3c8b8d99a39542...`|
|`0x060046e1`|`MZl8xUXmKYm`|`object, object, bool`|`void`|`7208208`|`4`|`ae3c8b8d99a39542...`|
|`0x060046e2`|`Qri8x8UotpI`|`object, object, object`|`void`|`7209600`|`4`|`ae3c8b8d99a39542...`|
|`0x060046e3`|`cBe8xEjvko4`|`object, object`|`bool`|`7212372`|`4`|`66ea1533cb3404e9...`|
|`0x060046e4`|`LWw8x0GTP1y`|`System.Collections.Generic.IList`1 [class:405]<mvar:0>, System.Collections.Generic.IList`1 [class:405]<mvar:0>, System.Func`3 [class:961]<mvar:0,mvar:0,bool>`|`bool`|`7213004`|`4`|`66ea1533cb3404e9...`|
|`0x060046e5`|`znn8x3rWLT4`|`object`|`System.Collections.Generic.List`1 [class:217]<string>`|`7213216`|`4`|`8969ce0cdaa4a19c...`|
|`0x060046e6`|`.cctor`|`()`|`void`|`7214908`|`156`|`6f369cc233a9a54f...`|
|`0x060046e7`|`HXK61HqoMdOGT3LVmldL`|`()`|`bool`|`7215076`|`4`|`66ea1533cb3404e9...`|
|`0x060046e8`|`HjiBhvqo9tV180EKOsRq`|`()`|`Kata_pro64_Cad2025.VmbmLightEditing [class:4440]`|`7215096`|`4`|`8969ce0cdaa4a19c...`|

### [VM-267] Methods of `Kata_pro64_Cad2025.Form_VMBM_Light_Edit` (Token `0x02000476`, Displayed: 99/99, Evidence `ev_549bfe33ecd78257ce27570bdd19331f304e770621ccd33a14059b374ac982e3`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x060047b6`|`get_Draft`|`()`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`7215112`|`4`|`8969ce0cdaa4a19c...`|
|`0x060047b7`|`get_Committed`|`()`|`bool`|`7215128`|`4`|`66ea1533cb3404e9...`|
|`0x060047b8`|`set_Committed`|`bool`|`void`|`7215144`|`4`|`ae3c8b8d99a39542...`|
|`0x060047b9`|`get_CadNeedsRead`|`()`|`bool`|`7215248`|`4`|`66ea1533cb3404e9...`|
|`0x060047ba`|`set_CadNeedsRead`|`bool`|`void`|`7215264`|`4`|`ae3c8b8d99a39542...`|
|`0x060047bb`|`get_CadTouched`|`()`|`bool`|`7215368`|`4`|`66ea1533cb3404e9...`|
|`0x060047bc`|`set_CadTouched`|`bool`|`void`|`7215384`|`4`|`ae3c8b8d99a39542...`|
|`0x060047bd`|`get_PhysicalChanged`|`()`|`bool`|`7215488`|`4`|`66ea1533cb3404e9...`|
|`0x060047be`|`set_PhysicalChanged`|`bool`|`void`|`7215504`|`4`|`ae3c8b8d99a39542...`|
|`0x060047bf`|`get_NamesChanged`|`()`|`bool`|`7215608`|`4`|`66ea1533cb3404e9...`|
|`0x060047c0`|`set_NamesChanged`|`bool`|`void`|`7215624`|`4`|`ae3c8b8d99a39542...`|
|`0x060047c1`|`.ctor`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`void`|`7215728`|`4`|`ae3c8b8d99a39542...`|
|`0x060047c2`|`u6E8xq6ZCpy`|`()`|`void`|`7223908`|`4`|`ae3c8b8d99a39542...`|
|`0x060047c3`|`mXQ8xARId67`|`()`|`void`|`7226824`|`4`|`ae3c8b8d99a39542...`|
|`0x060047c4`|`eRR8xC1y214`|`()`|`System.Collections.Generic.List`1 [class:217]<string>`|`7227300`|`4`|`8969ce0cdaa4a19c...`|
|`0x060047c5`|`lLu8xmG70xJ`|`string`|`void`|`7227396`|`4`|`ae3c8b8d99a39542...`|
|`0x060047c6`|`Noa8xQAqvtq`|`object, object`|`void`|`7227556`|`4`|`ae3c8b8d99a39542...`|
|`0x060047c7`|`cIt8xkftbuY`|`()`|`void`|`7228428`|`4`|`ae3c8b8d99a39542...`|
|`0x060047c8`|`kg18x4I0CBe`|`System.Action`1 [class:885]<Kata_pro64_Cad2025.VmbmLightBeam [class:4200]>`|`void`|`7229616`|`4`|`ae3c8b8d99a39542...`|
|`0x060047c9`|`PfF8xYTUb3x`|`()`|`void`|`7229852`|`4`|`ae3c8b8d99a39542...`|
|`0x060047ca`|`y8W8xoqxhsw`|`bool`|`void`|`7230136`|`4`|`ae3c8b8d99a39542...`|
|`0x060047cb`|`uDe8x7gW9t7`|`object, object, object`|`void`|`7230752`|`4`|`ae3c8b8d99a39542...`|
|`0x060047cc`|`aPr8xFYPOkH`|`object, object, object, i4`|`System.Windows.Forms.Button [class:161]`|`7231024`|`4`|`8969ce0cdaa4a19c...`|
|`0x060047cd`|`nyt8xvKh8BN`|`()`|`System.Collections.Generic.List`1 [class:217]<string>`|`7231396`|`4`|`8969ce0cdaa4a19c...`|
|`0x060047ce`|`e278x1gj3nb`|`string`|`void`|`7231496`|`4`|`ae3c8b8d99a39542...`|
|`0x060047cf`|`W6p8xMoTOOq`|`System.Collections.Generic.List`1 [class:217]<string>, System.Collections.Generic.List`1 [class:217]<string>`|`void`|`7231656`|`4`|`ae3c8b8d99a39542...`|
|`0x060047d0`|`dBx8x9wea1g`|`()`|`void`|`7236524`|`4`|`ae3c8b8d99a39542...`|
|`0x060047d1`|`F4N8xKIVHTh`|`string`|`void`|`7237480`|`4`|`ae3c8b8d99a39542...`|
|`0x060047d2`|`JSu8xNkPWU2`|`string`|`void`|`7237972`|`4`|`ae3c8b8d99a39542...`|
|`0x060047d3`|`Yh08xbcikd8`|`System.Action`1 [class:885]<Kata_pro64_Cad2025.VmbmLightCap [class:4152]>`|`void`|`7238888`|`4`|`ae3c8b8d99a39542...`|
|`0x060047d4`|`rSl8xIRgwtI`|`i4`|`void`|`7239104`|`4`|`ae3c8b8d99a39542...`|
|`0x060047d5`|`it88xyfx1Dg`|`System.Action [class:213], bool, bool`|`void`|`7239316`|`4`|`ae3c8b8d99a39542...`|
|`0x060047d6`|`PxW8xRrCjjQ`|`System.Collections.Generic.Stack`1 [class:2061]<VCnTe604quOWiVhAAIOn [class:4572]>, System.Collections.Generic.Stack`1 [class:2061]<VCnTe604quOWiVhAAIOn [class:4572]>`|`void`|`7241004`|`4`|`ae3c8b8d99a39542...`|
|`0x060047d7`|`n9l8xgWLLUB`|`()`|`void`|`7241332`|`4`|`ae3c8b8d99a39542...`|
|`0x060047d8`|`mlm8xh23w0Z`|`()`|`void`|`7242224`|`4`|`ae3c8b8d99a39542...`|
|`0x060047d9`|`rq48xZCqlCZ`|`()`|`void`|`7243200`|`4`|`ae3c8b8d99a39542...`|
|`0x060047da`|`R0l8xeycLco`|`()`|`void`|`7244644`|`4`|`ae3c8b8d99a39542...`|
|`0x060047db`|`KYL8xLkihKl`|`()`|`System.Windows.Forms.Control [class:97]`|`7244840`|`4`|`8969ce0cdaa4a19c...`|
|`0x060047dc`|`Lmh8xTs8STo`|`string`|`void`|`7244920`|`4`|`ae3c8b8d99a39542...`|
|`0x060047dd`|`KMt8xfJSJpy`|`()`|`void`|`7245432`|`4`|`ae3c8b8d99a39542...`|
|`0x060047de`|`Commit`|`()`|`void`|`7246024`|`4`|`ae3c8b8d99a39542...`|
|`0x060047df`|`_Lambda$__52-2`|`()`|`void`|`7248376`|`4`|`ae3c8b8d99a39542...`|
|`0x060047e0`|`_Lambda$__52-3`|`()`|`void`|`7248492`|`4`|`ae3c8b8d99a39542...`|
|`0x060047e1`|`_Lambda$__52-4`|`()`|`void`|`7248608`|`4`|`ae3c8b8d99a39542...`|
|`0x060047e2`|`_Lambda$__52-5`|`()`|`void`|`7248712`|`4`|`ae3c8b8d99a39542...`|
|`0x060047e3`|`_Lambda$__52-6`|`()`|`void`|`7248816`|`4`|`ae3c8b8d99a39542...`|
|`0x060047e4`|`_Lambda$__52-7`|`Kata_pro64_Cad2025.VmbmLightCap [class:4152]`|`void`|`7248932`|`4`|`ae3c8b8d99a39542...`|
|`0x060047e5`|`_Lambda$__52-8`|`()`|`void`|`7249040`|`4`|`ae3c8b8d99a39542...`|
|`0x060047e6`|`_Lambda$__52-9`|`()`|`void`|`7249156`|`4`|`ae3c8b8d99a39542...`|
|`0x060047e7`|`_Lambda$__52-10`|`()`|`void`|`7249272`|`4`|`ae3c8b8d99a39542...`|
|`0x060047e8`|`_Lambda$__52-11`|`()`|`void`|`7249388`|`4`|`ae3c8b8d99a39542...`|
|`0x060047e9`|`_Lambda$__52-12`|`()`|`void`|`7249528`|`4`|`ae3c8b8d99a39542...`|
|`0x060047ea`|`_Lambda$__52-13`|`Kata_pro64_Cad2025.VmbmLightCap [class:4152]`|`void`|`7249644`|`4`|`ae3c8b8d99a39542...`|
|`0x060047eb`|`_Lambda$__52-14`|`()`|`void`|`7249752`|`4`|`ae3c8b8d99a39542...`|
|`0x060047ec`|`_Lambda$__R52-1`|`object, System.EventArgs [class:113]`|`void`|`7249860`|`4`|`ae3c8b8d99a39542...`|
|`0x060047ed`|`_Lambda$__52-15`|`()`|`void`|`7249876`|`4`|`ae3c8b8d99a39542...`|
|`0x060047ee`|`_Lambda$__R52-2`|`object, System.EventArgs [class:113]`|`void`|`7249892`|`4`|`ae3c8b8d99a39542...`|
|`0x060047ef`|`_Lambda$__52-16`|`()`|`void`|`7249908`|`4`|`ae3c8b8d99a39542...`|
|`0x060047f0`|`_Lambda$__52-17`|`object, System.Windows.Forms.DataGridViewCellEventArgs [class:841]`|`void`|`7250104`|`4`|`ae3c8b8d99a39542...`|
|`0x060047f1`|`_Lambda$__R52-3`|`object, System.EventArgs [class:113]`|`void`|`7250744`|`4`|`ae3c8b8d99a39542...`|
|`0x060047f2`|`_Lambda$__52-20`|`()`|`void`|`7250760`|`4`|`ae3c8b8d99a39542...`|
|`0x060047f3`|`_Lambda$__R52-4`|`object, System.EventArgs [class:113]`|`void`|`7250896`|`4`|`ae3c8b8d99a39542...`|
|`0x060047f4`|`_Lambda$__52-21`|`()`|`void`|`7250912`|`4`|`ae3c8b8d99a39542...`|
|`0x060047f5`|`_Lambda$__R52-5`|`object, System.EventArgs [class:113]`|`void`|`7251044`|`4`|`ae3c8b8d99a39542...`|
|`0x060047f6`|`_Lambda$__52-22`|`()`|`void`|`7251060`|`4`|`ae3c8b8d99a39542...`|
|`0x060047f7`|`_Lambda$__R52-6`|`object, System.EventArgs [class:113]`|`void`|`7251192`|`4`|`ae3c8b8d99a39542...`|
|`0x060047f8`|`_Lambda$__52-23`|`()`|`void`|`7251208`|`4`|`ae3c8b8d99a39542...`|
|`0x060047f9`|`_Lambda$__R52-7`|`object, System.EventArgs [class:113]`|`void`|`7251340`|`4`|`ae3c8b8d99a39542...`|
|`0x060047fa`|`_Lambda$__52-24`|`()`|`void`|`7251356`|`4`|`ae3c8b8d99a39542...`|
|`0x060047fb`|`_Lambda$__R52-8`|`object, System.EventArgs [class:113]`|`void`|`7251492`|`4`|`ae3c8b8d99a39542...`|
|`0x060047fc`|`_Lambda$__52-25`|`()`|`void`|`7251508`|`4`|`ae3c8b8d99a39542...`|
|`0x060047fd`|`_Lambda$__R52-9`|`object, System.Windows.Forms.FormClosedEventArgs [class:681]`|`void`|`7251524`|`4`|`ae3c8b8d99a39542...`|
|`0x060047fe`|`_Lambda$__52-26`|`()`|`void`|`7251540`|`4`|`ae3c8b8d99a39542...`|
|`0x060047ff`|`_Lambda$__53-0`|`object, System.Windows.Forms.KeyEventArgs [class:637]`|`void`|`7251780`|`4`|`ae3c8b8d99a39542...`|
|`0x06004800`|`_Lambda$__53-1`|`()`|`void`|`7251992`|`4`|`ae3c8b8d99a39542...`|
|`0x06004801`|`_Lambda$__53-2`|`Kata_pro64_Cad2025.VmbmLightBeam [class:4200]`|`void`|`7252108`|`4`|`ae3c8b8d99a39542...`|
|`0x06004802`|`_Lambda$__53-3`|`()`|`void`|`7252264`|`4`|`ae3c8b8d99a39542...`|
|`0x06004803`|`_Lambda$__53-5`|`()`|`void`|`7252536`|`4`|`ae3c8b8d99a39542...`|
|`0x06004804`|`_Lambda$__53-6`|`()`|`void`|`7252640`|`4`|`ae3c8b8d99a39542...`|
|`0x06004805`|`_Lambda$__53-7`|`()`|`void`|`7252744`|`4`|`ae3c8b8d99a39542...`|
|`0x06004806`|`_Lambda$__53-9`|`()`|`void`|`7252896`|`4`|`ae3c8b8d99a39542...`|
|`0x06004807`|`_Lambda$__R53-10`|`object, System.EventArgs [class:113]`|`void`|`7253048`|`4`|`ae3c8b8d99a39542...`|
|`0x06004808`|`_Lambda$__53-11`|`()`|`void`|`7253064`|`4`|`ae3c8b8d99a39542...`|
|`0x06004809`|`_Lambda$__R53-11`|`object, System.EventArgs [class:113]`|`void`|`7253080`|`4`|`ae3c8b8d99a39542...`|
|`0x0600480a`|`_Lambda$__53-12`|`()`|`void`|`7253096`|`4`|`ae3c8b8d99a39542...`|
|`0x0600480b`|`_Lambda$__53-13`|`object, System.Windows.Forms.DataGridViewCellEventArgs [class:841]`|`void`|`7253272`|`4`|`ae3c8b8d99a39542...`|
|`0x0600480c`|`_Lambda$__67-0`|`Kata_pro64_Cad2025.VmbmLightCap [class:4152]`|`bool`|`7253960`|`4`|`66ea1533cb3404e9...`|
|`0x0600480d`|`_Lambda$__68-0`|`Kata_pro64_Cad2025.VmbmLightCap [class:4152]`|`bool`|`7253988`|`4`|`66ea1533cb3404e9...`|
|`0x0600480e`|`_Lambda$__77-0`|`()`|`void`|`7254076`|`4`|`ae3c8b8d99a39542...`|
|`0x0600480f`|`_Lambda$__77-1`|`()`|`void`|`7254208`|`4`|`ae3c8b8d99a39542...`|
|`0x06004810`|`_Lambda$__77-2`|`()`|`void`|`7254340`|`4`|`ae3c8b8d99a39542...`|
|`0x06004811`|`_Lambda$__77-3`|`()`|`void`|`7254472`|`4`|`ae3c8b8d99a39542...`|
|`0x06004812`|`_Lambda$__77-4`|`()`|`void`|`7254608`|`4`|`ae3c8b8d99a39542...`|
|`0x06004813`|`_Lambda$__77-5`|`object, System.Windows.Forms.FormClosingEventArgs [class:301]`|`void`|`7254768`|`4`|`ae3c8b8d99a39542...`|
|`0x06004814`|`_Lambda$__82-0`|`Kata_pro64_Cad2025.VmbmLightCap [class:4152]`|`bool`|`7255216`|`4`|`66ea1533cb3404e9...`|
|`0x06004815`|`_Lambda$__82-2`|`Kata_pro64_Cad2025.VmbmLightBeam [class:4200]`|`bool`|`7255396`|`4`|`66ea1533cb3404e9...`|
|`0x06004816`|`.cctor`|`()`|`void`|`7255576`|`177`|`388de630e382181d...`|
|`0x06004817`|`f7dEm8qoNKJXBGLK02St`|`()`|`bool`|`7255768`|`4`|`66ea1533cb3404e9...`|
|`0x06004818`|`nIl8TSqobp0nrRH65KeA`|`()`|`Kata_pro64_Cad2025.Form_VMBM_Light_Edit [class:4568]`|`7255788`|`4`|`8969ce0cdaa4a19c...`|

### [VM-268] Methods of `Kata_pro64_Cad2025.VmbmLightEtabsMetadata` (Token `0x02000490`, Displayed: 22/22, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x060048a4`|`.ctor`|`()`|`void`|`7255804`|`4`|`ae3c8b8d99a39542...`|
|`0x060048a5`|`get_BeamCombos`|`()`|`System.Collections.Generic.List`1 [class:217]<string>`|`7256200`|`4`|`8969ce0cdaa4a19c...`|
|`0x060048a6`|`set_BeamCombos`|`System.Collections.Generic.List`1 [class:217]<string>`|`void`|`7256216`|`4`|`ae3c8b8d99a39542...`|
|`0x060048a7`|`get_DesignCodes`|`()`|`System.Collections.Generic.List`1 [class:217]<string>`|`7256320`|`4`|`8969ce0cdaa4a19c...`|
|`0x060048a8`|`set_DesignCodes`|`System.Collections.Generic.List`1 [class:217]<string>`|`void`|`7256336`|`4`|`ae3c8b8d99a39542...`|
|`0x060048a9`|`get_ActiveDesignCode`|`()`|`string`|`7256440`|`4`|`8969ce0cdaa4a19c...`|
|`0x060048aa`|`set_ActiveDesignCode`|`string`|`void`|`7256456`|`4`|`ae3c8b8d99a39542...`|
|`0x060048ab`|`get_FileName`|`()`|`string`|`7256560`|`4`|`8969ce0cdaa4a19c...`|
|`0x060048ac`|`set_FileName`|`string`|`void`|`7256576`|`4`|`ae3c8b8d99a39542...`|
|`0x060048ad`|`get_Stories`|`()`|`System.Collections.Generic.Dictionary`2 [class:389]<string,r8>`|`7256680`|`4`|`8969ce0cdaa4a19c...`|
|`0x060048ae`|`set_Stories`|`System.Collections.Generic.Dictionary`2 [class:389]<string,r8>`|`void`|`7256696`|`4`|`ae3c8b8d99a39542...`|
|`0x060048af`|`get_Combos`|`()`|`System.Collections.Generic.List`1 [class:217]<string>`|`7256800`|`4`|`8969ce0cdaa4a19c...`|
|`0x060048b0`|`set_Combos`|`System.Collections.Generic.List`1 [class:217]<string>`|`void`|`7256816`|`4`|`ae3c8b8d99a39542...`|
|`0x060048b1`|`get_Materials`|`()`|`System.Collections.Generic.List`1 [class:217]<string>`|`7256920`|`4`|`8969ce0cdaa4a19c...`|
|`0x060048b2`|`set_Materials`|`System.Collections.Generic.List`1 [class:217]<string>`|`void`|`7256936`|`4`|`ae3c8b8d99a39542...`|
|`0x060048b3`|`get_RebarMaterials`|`()`|`System.Collections.Generic.List`1 [class:217]<string>`|`7257040`|`4`|`8969ce0cdaa4a19c...`|
|`0x060048b4`|`set_RebarMaterials`|`System.Collections.Generic.List`1 [class:217]<string>`|`void`|`7257056`|`4`|`ae3c8b8d99a39542...`|
|`0x060048b5`|`get_GridSystems`|`()`|`System.Collections.Generic.List`1 [class:217]<string>`|`7257160`|`4`|`8969ce0cdaa4a19c...`|
|`0x060048b6`|`set_GridSystems`|`System.Collections.Generic.List`1 [class:217]<string>`|`void`|`7257176`|`4`|`ae3c8b8d99a39542...`|
|`0x060048b7`|`.cctor`|`()`|`void`|`7257280`|`156`|`0fdb4d9f5b81d196...`|
|`0x060048b8`|`zh75RgqoyXZVd4DKYISk`|`()`|`bool`|`7257448`|`4`|`66ea1533cb3404e9...`|
|`0x060048b9`|`NtbmGMqoR6Z5gkwoj6rf`|`()`|`Kata_pro64_Cad2025.VmbmLightEtabsMetadata [class:4672]`|`7257468`|`4`|`8969ce0cdaa4a19c...`|

### [VM-269] Methods of `Kata_pro64_Cad2025.VmbmLightMesh` (Token `0x02000491`, Displayed: 7/7, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x060048ba`|`.ctor`|`()`|`void`|`7257484`|`4`|`ae3c8b8d99a39542...`|
|`0x060048bb`|`Cells`|`Polygon [class:89], System.Collections.Generic.IEnumerable`1 [class:321]<Kata_Class_Lib_Revit.diem [class:85]>, r8`|`System.Collections.Generic.List`1 [class:217]<Polygon [class:89]>`|`7257608`|`4`|`8969ce0cdaa4a19c...`|
|`0x060048bc`|`m0S8DF650Bk`|`r8, r8, System.Collections.Generic.IEnumerable`1 [class:321]<r8>, r8`|`System.Collections.Generic.List`1 [class:217]<r8>`|`7261156`|`4`|`8969ce0cdaa4a19c...`|
|`0x060048bd`|`OEi8Dvqn5SS`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, bool, r8, bool`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>`|`7261988`|`4`|`8969ce0cdaa4a19c...`|
|`0x060048be`|`.cctor`|`()`|`void`|`7263328`|`177`|`72696b72634429a7...`|
|`0x060048bf`|`kendgGqohmEIL8apc1bu`|`()`|`bool`|`7263520`|`4`|`66ea1533cb3404e9...`|
|`0x060048c0`|`v7hontqoZyqAZZCKkeeF`|`()`|`Kata_pro64_Cad2025.VmbmLightMesh [class:4676]`|`7263540`|`4`|`8969ce0cdaa4a19c...`|

### [VM-270] Methods of `InfoMongDonSave` (Token `0x020004d2`, Displayed: 4/4, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06004b90`|`.ctor`|`()`|`void`|`12849340`|`4`|`ae3c8b8d99a39542...`|
|`0x06004b91`|`.cctor`|`()`|`void`|`12849464`|`177`|`605db6882da54256...`|
|`0x06004b92`|`vCdx6Pqz7RhVMhYQLvIF`|`()`|`bool`|`12849656`|`4`|`66ea1533cb3404e9...`|
|`0x06004b93`|`IF8EyAqzFw0BnLTjPwdx`|`()`|`InfoMongDonSave [class:4936]`|`12849676`|`4`|`8969ce0cdaa4a19c...`|

### [VM-271] Methods of `InfoMongDonRenameSave` (Token `0x020004d3`, Displayed: 5/5, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06004b94`|`.ctor`|`()`|`void`|`12849692`|`4`|`ae3c8b8d99a39542...`|
|`0x06004b95`|`ToNormal`|`()`|`InfoMongDonSave [class:4936]`|`12849816`|`4`|`8969ce0cdaa4a19c...`|
|`0x06004b96`|`.cctor`|`()`|`void`|`12850324`|`177`|`2050df8a0199f519...`|
|`0x06004b97`|`CgUVsvqz1MGNkM9LONuU`|`()`|`bool`|`12850516`|`4`|`66ea1533cb3404e9...`|
|`0x06004b98`|`L7yFPNqzMtYi6QwI9TWK`|`()`|`InfoMongDonRenameSave [class:4940]`|`12850536`|`4`|`8969ce0cdaa4a19c...`|

### [VM-272] Methods of `Kata_pro64_Cad2025.Foundation_Etabs` (Token `0x0200052c`, Displayed: 21/21, Evidence `ev_3bdf54e817a9b5840d6618afe7ca3edeaabd4e8c3021dcb361d608566084bf9d`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x060050f6`|`.cctor`|`()`|`void`|`9226212`|`241`|`84d979b132124a8a...`|
|`0x060050f7`|`Get_Moment_Mong`|`string, string, System.Collections.Generic.IDictionary`2 [class:1725]<string,System.Tuple`3 [class:805]<string,string,r8>>, CSiAPIv1.cSapModel [class:2025]`|`System.Collections.Generic.List`1 [class:217]<MomentMong [class:5312]>`|`9226468`|`4`|`8969ce0cdaa4a19c...`|
|`0x060050f8`|`mc4EMi8S5PG`|`object, object`|`r8`|`9231988`|`4`|`f0ad83c607e59d90...`|
|`0x060050f9`|`Get_JointReaction`|`string`|`System.Collections.Generic.Dictionary`2 [class:389]<string,System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.info_JointReaction_Etabs [class:1045]>>`|`9232440`|`4`|`8969ce0cdaa4a19c...`|
|`0x060050fa`|`Xuat_JointReaction_CotVach_to_cad`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.info_JointReaction_Etabs [class:1045]>`|`void`|`9234348`|`4`|`ae3c8b8d99a39542...`|
|`0x060050fb`|`Xuat_JointReaction_Coc_to_cad`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.info_JointReaction_Etabs [class:1045]>, System.Collections.Generic.List`1 [class:217]<CocChiuTai [class:3568]>, r8, r8`|`void`|`9235308`|`4`|`ae3c8b8d99a39542...`|
|`0x060050fc`|`PromptUserChooseStripTypeAndRebarMaterial`|`StripDesignOption [class:5304]`|`bool`|`9237528`|`4`|`66ea1533cb3404e9...`|
|`0x060050fd`|`IsSelectedStripWidthByFoundation`|`()`|`bool`|`9238256`|`4`|`66ea1533cb3404e9...`|
|`0x060050fe`|`GetSelectedStripWidthValue`|`r8`|`r8`|`9238496`|`4`|`f0ad83c607e59d90...`|
|`0x060050ff`|`ApplyStripDesignOverwrites`|`System.Collections.Generic.List`1 [class:217]<StripEtabs [class:5300]>, string, CSiAPIv1.cSapModel [class:2025], StripDesignOption [class:5304]`|`void`|`9238988`|`4`|`ae3c8b8d99a39542...`|
|`0x06005100`|`N41EMnuw0Dv`|`()`|`System.Collections.Generic.List`1 [class:217]<string>`|`9245304`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005101`|`yoaEM5nuPJc`|`System.Collections.Generic.List`1 [class:217]<string>, object`|`void`|`9247260`|`4`|`ae3c8b8d99a39542...`|
|`0x06005102`|`QcyEMSWMjkU`|`object`|`bool`|`9247540`|`4`|`66ea1533cb3404e9...`|
|`0x06005103`|`cweEMjdkiEs`|`object`|`string`|`9248600`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005104`|`e4iEMx9R5dd`|`object, object`|`bool`|`9250220`|`4`|`66ea1533cb3404e9...`|
|`0x06005105`|`FGtEMDlCFg4`|`object, string[]`|`i4`|`9250568`|`4`|`78d4d2062aa5373e...`|
|`0x06005106`|`nTuEMB5HeMR`|`object`|`string`|`9251224`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005107`|`OTYEMOPO3Gc`|`object, object, object, object`|`string[]`|`9251524`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005108`|`AG0EMVpLQp9`|`object, i4, object`|`bool`|`9256552`|`4`|`66ea1533cb3404e9...`|
|`0x06005109`|`VPUm2mqvLBThIgaxbs0W`|`()`|`bool`|`9257068`|`4`|`66ea1533cb3404e9...`|
|`0x0600510a`|`ynCZiaqvTthZBVdX0WJk`|`()`|`Kata_pro64_Cad2025.Foundation_Etabs [class:5296]`|`9257088`|`4`|`8969ce0cdaa4a19c...`|

### [VM-273] Methods of `ThepBoTriMong` (Token `0x0200052f`, Displayed: 4/4, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06005113`|`.ctor`|`()`|`void`|`12876024`|`4`|`ae3c8b8d99a39542...`|
|`0x06005114`|`.cctor`|`()`|`void`|`12876148`|`156`|`9fbaca7a04e6463b...`|
|`0x06005115`|`A79tKwA8otISIF8iwGeW`|`()`|`bool`|`12876316`|`4`|`66ea1533cb3404e9...`|
|`0x06005116`|`P3EQX1A87t1kNwK97tru`|`()`|`ThepBoTriMong [class:5308]`|`12876336`|`4`|`8969ce0cdaa4a19c...`|

### [VM-274] Methods of `MomentMong` (Token `0x02000530`, Displayed: 4/4, Evidence `ev_1edd133c1013aaa4a08cbd870946b8a512f59e4d891873afbd258c92a6a06c69`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06005117`|`.ctor`|`()`|`void`|`12876352`|`4`|`ae3c8b8d99a39542...`|
|`0x06005118`|`.cctor`|`()`|`void`|`12876596`|`156`|`3fde14ba7ed88c38...`|
|`0x06005119`|`LjqpYAA8vbcgLCmUCv0p`|`()`|`bool`|`12876764`|`4`|`66ea1533cb3404e9...`|
|`0x0600511a`|`kBcPBHA81F4UWrlGbIsQ`|`()`|`MomentMong [class:5312]`|`12876784`|`4`|`8969ce0cdaa4a19c...`|

### [VM-275] Methods of `Kata_pro64_Cad2025.Draw_foudation` (Token `0x02000557`, Displayed: 5/5, Evidence `ev_3bdf54e817a9b5840d6618afe7ca3edeaabd4e8c3021dcb361d608566084bf9d`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06005336`|`.ctor`|`()`|`void`|`9995436`|`4`|`ae3c8b8d99a39542...`|
|`0x06005337`|`QuetMong`|`()`|`System.Tuple`2 [class:285]<InfoMongCoc [class:2996],Kata_Class_Lib_Revit.diem [class:85]>`|`9995560`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005338`|`.cctor`|`()`|`void`|`9998772`|`156`|`0c0393229ad05cf4...`|
|`0x06005339`|`I9bosSq1TvaBKQ1UBR2H`|`()`|`bool`|`9998940`|`4`|`66ea1533cb3404e9...`|
|`0x0600533a`|`IPeBBnq1fvtm5npFGTsZ`|`()`|`Kata_pro64_Cad2025.Draw_foudation [class:5468]`|`9998960`|`4`|`8969ce0cdaa4a19c...`|

### [VM-276] Methods of `Kata_pro64_Cad2025.kata7` (Token `0x02000566`, Displayed: 110/110, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x060053db`|`.cctor`|`()`|`void`|`10614024`|`991`|`4b177f2fc3cdeca1...`|
|`0x060053dc`|`chuyen_ngon_ngu`|`string&, string, string`|`void`|`10615028`|`4`|`ae3c8b8d99a39542...`|
|`0x060053dd`|`chuyen_ngu`|`string, string`|`string`|`10615296`|`4`|`8969ce0cdaa4a19c...`|
|`0x060053de`|`SaPEgthD8XB`|`()`|`bool`|`10615552`|`4`|`66ea1533cb3404e9...`|
|`0x060053df`|`OCuEgdOnNfH`|`()`|`System.Collections.Generic.Dictionary`2 [class:389]<string,oZtWXq0Fj6bwmhNYD9i4 [valuetype:5532]>`|`10616140`|`4`|`8969ce0cdaa4a19c...`|
|`0x060053e0`|`GDOEgJ1BurI`|`System.Collections.Generic.Dictionary`2 [class:389]<string,oZtWXq0Fj6bwmhNYD9i4 [valuetype:5532]>, object, object, object, object`|`void`|`10646988`|`4`|`ae3c8b8d99a39542...`|
|`0x060053e1`|`StartFormLanguageSynchronization`|`()`|`void`|`10647144`|`4`|`ae3c8b8d99a39542...`|
|`0x060053e2`|`RefreshOpenFormLanguages`|`()`|`void`|`10647504`|`4`|`ae3c8b8d99a39542...`|
|`0x060053e3`|`CFvEgrVHRJI`|`object, object`|`void`|`10647732`|`4`|`ae3c8b8d99a39542...`|
|`0x060053e4`|`ApplyFormLanguage`|`System.Windows.Forms.Form [class:61]`|`void`|`10650140`|`4`|`ae3c8b8d99a39542...`|
|`0x060053e5`|`tao_selection`|`string`|`object`|`10652636`|`4`|`8969ce0cdaa4a19c...`|
|`0x060053e6`|`Check_co_sheet`|`string`|`bool`|`10653460`|`4`|`66ea1533cb3404e9...`|
|`0x060053e7`|`lay_sheet`|`string`|`object`|`10655036`|`4`|`8969ce0cdaa4a19c...`|
|`0x060053e8`|`xd_block_kht_lk_export`|`string, string&`|`void`|`10658096`|`4`|`ae3c8b8d99a39542...`|
|`0x060053e9`|`ghi_dim_thep_shop`|`object, string, bool, string, i4, bool`|`void`|`10658440`|`4`|`ae3c8b8d99a39542...`|
|`0x060053ea`|`tinh_dai`|`Kata_Class_Lib_Revit.info_thep [class:793]&, i4`|`i4`|`10670044`|`4`|`78d4d2062aa5373e...`|
|`0x060053eb`|`xd_tam`|`object`|`Kata_Class_Lib_Revit.diem [class:85]`|`10687600`|`4`|`8969ce0cdaa4a19c...`|
|`0x060053ec`|`Tinh_L1_theo_d`|`r8, i4`|`i4`|`10694232`|`4`|`78d4d2062aa5373e...`|
|`0x060053ed`|`Tinh_L_theo_d`|`r8, i4, i4`|`i4`|`10694480`|`4`|`78d4d2062aa5373e...`|
|`0x060053ee`|`tra_lap_comp`|`i4`|`i4`|`10694872`|`4`|`78d4d2062aa5373e...`|
|`0x060053ef`|`tra_lap_tens`|`i4`|`i4`|`10695256`|`4`|`78d4d2062aa5373e...`|
|`0x060053f0`|`tra_anchor_comp`|`i4`|`i4`|`10695644`|`4`|`78d4d2062aa5373e...`|
|`0x060053f1`|`tra_anchor_tens`|`i4`|`i4`|`10696008`|`4`|`78d4d2062aa5373e...`|
|`0x060053f2`|`stt_thep`|`object, string, object`|`string`|`10696352`|`4`|`8969ce0cdaa4a19c...`|
|`0x060053f3`|`instr_cuoi`|`string, string, i4&`|`i4`|`10698512`|`4`|`78d4d2062aa5373e...`|
|`0x060053f4`|`chuyen_info_thep`|`string, i4&, i4&, i4&, object&, bool`|`bool`|`10698868`|`4`|`66ea1533cb3404e9...`|
|`0x060053f5`|`chuyen_thep_san_adappt`|`string, Kata_Class_Lib_Revit.info_thep [class:793]&`|`void`|`10703328`|`4`|`ae3c8b8d99a39542...`|
|`0x060053f6`|`phikt`|`i4`|`string`|`10703932`|`4`|`8969ce0cdaa4a19c...`|
|`0x060053f7`|`Kiem_tra_dim_layer_text`|`r8, r8, bool`|`bool`|`10704620`|`4`|`66ea1533cb3404e9...`|
|`0x060053f8`|`dim_co_x_move`|`Kata_Class_Lib_Revit.diem [class:85][], Kata_Class_Lib_Revit.diem [class:85], i4`|`void`|`10713720`|`4`|`ae3c8b8d99a39542...`|
|`0x060053f9`|`dim_co_y_move`|`Kata_Class_Lib_Revit.diem [class:85][], Kata_Class_Lib_Revit.diem [class:85], i4`|`void`|`10715128`|`4`|`ae3c8b8d99a39542...`|
|`0x060053fa`|`dim_co_x`|`Kata_Class_Lib_Revit.diem [class:85][], Kata_Class_Lib_Revit.diem [class:85]`|`void`|`10716624`|`4`|`ae3c8b8d99a39542...`|
|`0x060053fb`|`dim_co_y`|`Kata_Class_Lib_Revit.diem [class:85][], Kata_Class_Lib_Revit.diem [class:85]`|`object`|`10717596`|`4`|`8969ce0cdaa4a19c...`|
|`0x060053fc`|`dim_al`|`Kata_Class_Lib_Revit.diem [class:85], r8, r8`|`object`|`10718768`|`4`|`8969ce0cdaa4a19c...`|
|`0x060053fd`|`dim_al_3d`|`Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85]`|`object`|`10719552`|`4`|`8969ce0cdaa4a19c...`|
|`0x060053fe`|`dim_al_x`|`Kata_Class_Lib_Revit.diem [class:85], r8, r8`|`object`|`10720348`|`4`|`8969ce0cdaa4a19c...`|
|`0x060053ff`|`dim_al_y`|`Kata_Class_Lib_Revit.diem [class:85], r8, r8`|`object`|`10721360`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005400`|`dim_ro_x`|`Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], i4`|`object`|`10722380`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005401`|`dim_ro_y`|`Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], i4`|`object`|`10723836`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005402`|`dim_ro`|`Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], i4, r8`|`object`|`10725208`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005403`|`dim_diametric`|`Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], i4`|`object`|`10726300`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005404`|`pline6`|`Kata_Class_Lib_Revit.diem [class:85], r8, r8, r8, r8, r8, string`|`void`|`10727100`|`4`|`ae3c8b8d99a39542...`|
|`0x06005405`|`pline4`|`Kata_Class_Lib_Revit.diem [class:85], r8, r8, r8, string, r8`|`void`|`10727976`|`4`|`ae3c8b8d99a39542...`|
|`0x06005406`|`thep_dam_dang01`|`Kata_Class_Lib_Revit.diem [class:85], r8, i4, System.Collections.Generic.SortedDictionary`2 [class:777]<i4,i4>, bool`|`object`|`10728900`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005407`|`thep_dam_dang03`|`Kata_Class_Lib_Revit.diem [class:85], r8, r8, r8, i4, System.Collections.Generic.SortedDictionary`2 [class:777]<i4,i4>, bool`|`object`|`10729920`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005408`|`thep_dam_dang04`|`Kata_Class_Lib_Revit.diem [class:85], r8, r8, r8, i4, System.Collections.Generic.SortedDictionary`2 [class:777]<i4,i4>`|`object`|`10732448`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005409`|`ve_line`|`Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], string, r8`|`object`|`10734068`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600540a`|`ve_poly_layer`|`Kata_Class_Lib_Revit.diem [class:85][], string`|`object`|`10735208`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600540b`|`cat_ngang`|`Kata_Class_Lib_Revit.diem [class:85], r8, r8, bool`|`object`|`10735844`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600540c`|`cat_dung`|`Kata_Class_Lib_Revit.diem [class:85], r8, r8, bool`|`object`|`10737424`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600540d`|`bao_dam`|`Kata_Class_Lib_Revit.diem [class:85], r8, r8, r8, r8, r8`|`void`|`10738712`|`4`|`ae3c8b8d99a39542...`|
|`0x0600540e`|`daiU`|`Kata_Class_Lib_Revit.diem [class:85], r8, r8, r8, r8, string`|`object`|`10739560`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600540f`|`daivong`|`Kata_Class_Lib_Revit.diem [class:85], r8, r8, r8, string, r8, i4, bool`|`object`|`10741684`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005410`|`daivong_U`|`Kata_Class_Lib_Revit.diem [class:85], r8, r8, r8, string, r8`|`object`|`10744464`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005411`|`daivong_35`|`Kata_Class_Lib_Revit.diem [class:85], r8, r8, r8, string, r8, bool`|`object`|`10746472`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005412`|`dai_U`|`Kata_Class_Lib_Revit.diem [class:85], r8, r8, r8, r8, string`|`object`|`10748716`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005413`|`vevong`|`Kata_Class_Lib_Revit.diem [class:85], r8, r8, r8, string, r8`|`void`|`10750076`|`4`|`ae3c8b8d99a39542...`|
|`0x06005414`|`daiC`|`Kata_Class_Lib_Revit.diem [class:85], r8, r8, r8, r8, string&`|`object`|`10752092`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005415`|`daiC04`|`Kata_Class_Lib_Revit.diem [class:85], r8, r8, r8, r8, r8`|`object`|`10754764`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005416`|`ve_dtr`|`Kata_Class_Lib_Revit.diem [class:85], r8, string`|`void`|`10756508`|`4`|`ae3c8b8d99a39542...`|
|`0x06005417`|`ve_elip_noi`|`Kata_Class_Lib_Revit.diem [class:85], r8`|`void`|`10756984`|`4`|`ae3c8b8d99a39542...`|
|`0x06005418`|`tao_hatch_chu_nhat`|`Kata_Class_Lib_Revit.diem [class:85], r8, r8, string, r8, string`|`void`|`10758148`|`4`|`ae3c8b8d99a39542...`|
|`0x06005419`|`Create_Viewport`|`Kata_Class_Lib_Revit.info_viewport [valuetype:1829][]`|`void`|`10758336`|`4`|`ae3c8b8d99a39542...`|
|`0x0600541a`|`ve_le3`|`Kata_Class_Lib_Revit.diem [class:85], r8, r8, r8, string`|`object`|`10761740`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600541b`|`ve_le3_90`|`Kata_Class_Lib_Revit.diem [class:85], r8, r8, r8, string`|`object`|`10761924`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600541c`|`AddText`|`Kata_Class_Lib_Revit.diem [class:85], string, i4, i2, string`|`object`|`10762108`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600541d`|`rong_Text1`|`object&, r8, r8`|`r8`|`10763060`|`4`|`f0ad83c607e59d90...`|
|`0x0600541e`|`rong_Text`|`string, i4, r8, r8`|`r8`|`10764256`|`4`|`f0ad83c607e59d90...`|
|`0x0600541f`|`AddMText`|`Kata_Class_Lib_Revit.diem [class:85], string, i4, r8`|`void`|`10765628`|`4`|`ae3c8b8d99a39542...`|
|`0x06005420`|`pline`|`Kata_Class_Lib_Revit.diem [class:85], r8, r8, r8, string`|`object`|`10767112`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005421`|`pline6_cot`|`Kata_Class_Lib_Revit.diem [class:85], r8, r8, r8, string, r8, r8`|`void`|`10767816`|`4`|`ae3c8b8d99a39542...`|
|`0x06005422`|`pline4_cot`|`Kata_Class_Lib_Revit.diem [class:85], r8, r8, string`|`void`|`10768888`|`4`|`ae3c8b8d99a39542...`|
|`0x06005423`|`thepU_cot`|`Kata_Class_Lib_Revit.diem [class:85], r8, r8, r8, i4`|`object`|`10769656`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005424`|`thepL_cot`|`Kata_Class_Lib_Revit.diem [class:85], r8, r8, i4, string&`|`object`|`10770684`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005425`|`thep_cat_cot`|`Kata_Class_Lib_Revit.diem [class:85], r8, i4, string&`|`object`|`10772520`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005426`|`bao`|`Kata_Class_Lib_Revit.diem [class:85], r8, r8`|`void`|`10773572`|`4`|`ae3c8b8d99a39542...`|
|`0x06005427`|`dua_iso_vao`|`()`|`void`|`10774656`|`4`|`ae3c8b8d99a39542...`|
|`0x06005428`|`dua_iso_vao_TH_bi_thieu`|`()`|`void`|`10776304`|`4`|`ae3c8b8d99a39542...`|
|`0x06005429`|`mo_huong_dan`|`()`|`void`|`10777272`|`4`|`ae3c8b8d99a39542...`|
|`0x0600542a`|`Round_Li`|`r8, bool`|`i4`|`10777756`|`4`|`78d4d2062aa5373e...`|
|`0x0600542b`|`Round_up_Li`|`r8`|`i4`|`10777812`|`4`|`78d4d2062aa5373e...`|
|`0x0600542c`|`Round_up_ke`|`r8`|`i4`|`10777836`|`4`|`78d4d2062aa5373e...`|
|`0x0600542d`|`Round_be_ke`|`r8`|`i4`|`10777884`|`4`|`78d4d2062aa5373e...`|
|`0x0600542e`|`MRound`|`r8, r8`|`i4`|`10777932`|`4`|`78d4d2062aa5373e...`|
|`0x0600542f`|`MRound_up`|`r8, r8`|`i4`|`10778188`|`4`|`78d4d2062aa5373e...`|
|`0x06005430`|`MRound_down`|`r8, r8`|`i4`|`10778464`|`4`|`78d4d2062aa5373e...`|
|`0x06005431`|`gan_cad`|`bool, bool`|`bool`|`10778740`|`4`|`66ea1533cb3404e9...`|
|`0x06005432`|`dang_bi_hack`|`()`|`void`|`10782368`|`4`|`ae3c8b8d99a39542...`|
|`0x06005433`|`mo_wed`|`string`|`void`|`10782760`|`4`|`ae3c8b8d99a39542...`|
|`0x06005434`|`tra_do_dan_tu_setting`|`string, i4`|`r8`|`10783312`|`4`|`f0ad83c607e59d90...`|
|`0x06005435`|`load_cog_hook`|`string`|`void`|`10783768`|`4`|`ae3c8b8d99a39542...`|
|`0x06005436`|`load_unit_mass`|`string`|`void`|`10786184`|`4`|`ae3c8b8d99a39542...`|
|`0x06005437`|`load_lap_anchor`|`string`|`void`|`10787756`|`4`|`ae3c8b8d99a39542...`|
|`0x06005438`|`update_goi_lenh`|`()`|`void`|`10789856`|`4`|`ae3c8b8d99a39542...`|
|`0x06005439`|`kieu_leader_thep_thanh`|`()`|`string`|`10791628`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600543a`|`sset_runlen_add`|`string, object`|`void`|`10791668`|`4`|`ae3c8b8d99a39542...`|
|`0x0600543b`|`sset_block_add`|`string, object`|`void`|`10792592`|`4`|`ae3c8b8d99a39542...`|
|`0x0600543c`|`sset_block_remove`|`string, string`|`void`|`10792960`|`4`|`ae3c8b8d99a39542...`|
|`0x0600543d`|`Tinh_gia_tri_chuoi`|`string, string&`|`r8`|`10793808`|`4`|`f0ad83c607e59d90...`|
|`0x0600543e`|`so_L_hien`|`string`|`i4`|`10797408`|`4`|`78d4d2062aa5373e...`|
|`0x0600543f`|`chuyen_td_line`|`Kata_Class_Lib_Revit.LineShape [class:865]&, Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], r8`|`void`|`10810244`|`4`|`ae3c8b8d99a39542...`|
|`0x06005440`|`tach_text`|`string&, i4&`|`void`|`10810616`|`4`|`ae3c8b8d99a39542...`|
|`0x06005441`|`sort_text`|`string, string`|`bool`|`10811128`|`4`|`66ea1533cb3404e9...`|
|`0x06005442`|`check_toa_do_le`|`r8&, r8&`|`void`|`10811456`|`4`|`ae3c8b8d99a39542...`|
|`0x06005443`|`dmin`|`Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85]`|`Kata_Class_Lib_Revit.diem [class:85]`|`10811748`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005444`|`dmax`|`Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85]`|`Kata_Class_Lib_Revit.diem [class:85]`|`10812024`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005445`|`tao_polyline_2diem_B`|`Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], i4`|`object`|`10812280`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005446`|`Cap_nhat_lineshape`|`Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.LineShape [class:865]`|`void`|`10812876`|`4`|`ae3c8b8d99a39542...`|
|`0x06005447`|`c33plOq1i70kBX5IuJnF`|`()`|`bool`|`10813104`|`4`|`66ea1533cb3404e9...`|
|`0x06005448`|`XQNjRaq1n8DhuX3Cu0jl`|`()`|`Kata_pro64_Cad2025.kata7 [class:5528]`|`10813124`|`4`|`8969ce0cdaa4a19c...`|

### [VM-277] Methods of `Kata_pro64_Cad2025.VmbmLightCadBinding` (Token `0x02000583`, Displayed: 19/19, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x0600555c`|`.ctor`|`()`|`void`|`11061900`|`4`|`ae3c8b8d99a39542...`|
|`0x0600555d`|`get_CapId`|`()`|`string`|`11062024`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600555e`|`set_CapId`|`string`|`void`|`11062040`|`4`|`ae3c8b8d99a39542...`|
|`0x0600555f`|`get_FoundationHandle`|`()`|`i8`|`11062144`|`4`|`0c55072e2c71f84f...`|
|`0x06005560`|`set_FoundationHandle`|`i8`|`void`|`11062160`|`4`|`ae3c8b8d99a39542...`|
|`0x06005561`|`get_NameHandle`|`()`|`i8`|`11062264`|`4`|`0c55072e2c71f84f...`|
|`0x06005562`|`set_NameHandle`|`i8`|`void`|`11062280`|`4`|`ae3c8b8d99a39542...`|
|`0x06005563`|`get_OffsetX`|`()`|`r8`|`11062384`|`4`|`f0ad83c607e59d90...`|
|`0x06005564`|`set_OffsetX`|`r8`|`void`|`11062400`|`4`|`ae3c8b8d99a39542...`|
|`0x06005565`|`get_OffsetY`|`()`|`r8`|`11062504`|`4`|`f0ad83c607e59d90...`|
|`0x06005566`|`set_OffsetY`|`r8`|`void`|`11062520`|`4`|`ae3c8b8d99a39542...`|
|`0x06005567`|`get_Fingerprint`|`()`|`string`|`11062624`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005568`|`set_Fingerprint`|`string`|`void`|`11062640`|`4`|`ae3c8b8d99a39542...`|
|`0x06005569`|`get_CadName`|`()`|`string`|`11062744`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600556a`|`set_CadName`|`string`|`void`|`11062760`|`4`|`ae3c8b8d99a39542...`|
|`0x0600556b`|`Copy`|`()`|`Kata_pro64_Cad2025.VmbmLightCadBinding [class:5644]`|`11062864`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600556c`|`.cctor`|`()`|`void`|`11062892`|`156`|`bd7283a4612b75da...`|
|`0x0600556d`|`a1lifEqM7Hwj7XlB3KuA`|`()`|`bool`|`11063060`|`4`|`66ea1533cb3404e9...`|
|`0x0600556e`|`NIhqvOqMFwsYI7iVqqtr`|`()`|`Kata_pro64_Cad2025.VmbmLightCadBinding [class:5644]`|`11063080`|`4`|`8969ce0cdaa4a19c...`|

### [VM-278] Methods of `Kata_pro64_Cad2025.VmbmLightCadBeamBinding` (Token `0x02000584`, Displayed: 11/11, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x0600556f`|`.ctor`|`()`|`void`|`11063096`|`4`|`ae3c8b8d99a39542...`|
|`0x06005570`|`get_BeamId`|`()`|`string`|`11063220`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005571`|`set_BeamId`|`string`|`void`|`11063236`|`4`|`ae3c8b8d99a39542...`|
|`0x06005572`|`get_Handle`|`()`|`i8`|`11063340`|`4`|`0c55072e2c71f84f...`|
|`0x06005573`|`set_Handle`|`i8`|`void`|`11063356`|`4`|`ae3c8b8d99a39542...`|
|`0x06005574`|`get_CadName`|`()`|`string`|`11063460`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005575`|`set_CadName`|`string`|`void`|`11063476`|`4`|`ae3c8b8d99a39542...`|
|`0x06005576`|`Copy`|`()`|`Kata_pro64_Cad2025.VmbmLightCadBeamBinding [class:5648]`|`11063580`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005577`|`.cctor`|`()`|`void`|`11063608`|`177`|`1e836115f9ed9d37...`|
|`0x06005578`|`FYxOQaqM1WKgnBtjUea0`|`()`|`bool`|`11063800`|`4`|`66ea1533cb3404e9...`|
|`0x06005579`|`HvO4L9qMMoegsVDHEtMN`|`()`|`Kata_pro64_Cad2025.VmbmLightCadBeamBinding [class:5648]`|`11063820`|`4`|`8969ce0cdaa4a19c...`|

### [VM-279] Methods of `Kata_pro64_Cad2025.VmbmLightStripBinding` (Token `0x02000585`, Displayed: 12/12, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x0600557a`|`.ctor`|`()`|`void`|`11063836`|`4`|`ae3c8b8d99a39542...`|
|`0x0600557b`|`get_StripName`|`()`|`string`|`11063960`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600557c`|`set_StripName`|`string`|`void`|`11063976`|`4`|`ae3c8b8d99a39542...`|
|`0x0600557d`|`get_CapId`|`()`|`string`|`11064080`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600557e`|`set_CapId`|`string`|`void`|`11064096`|`4`|`ae3c8b8d99a39542...`|
|`0x0600557f`|`get_Axis`|`()`|`string`|`11064200`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005580`|`set_Axis`|`string`|`void`|`11064216`|`4`|`ae3c8b8d99a39542...`|
|`0x06005581`|`get_Height`|`()`|`r8`|`11064320`|`4`|`f0ad83c607e59d90...`|
|`0x06005582`|`set_Height`|`r8`|`void`|`11064336`|`4`|`ae3c8b8d99a39542...`|
|`0x06005583`|`.cctor`|`()`|`void`|`11064440`|`177`|`290c61b2bc059081...`|
|`0x06005584`|`XnerDOqMKWkmedyEfP9j`|`()`|`bool`|`11064632`|`4`|`66ea1533cb3404e9...`|
|`0x06005585`|`Ry7WgDqMNegGyeNGRq3F`|`()`|`Kata_pro64_Cad2025.VmbmLightStripBinding [class:5652]`|`11064652`|`4`|`8969ce0cdaa4a19c...`|

### [VM-280] Methods of `Kata_pro64_Cad2025.VmbmLightState` (Token `0x02000586`, Displayed: 17/17, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06005586`|`.ctor`|`()`|`void`|`11064668`|`4`|`ae3c8b8d99a39542...`|
|`0x06005587`|`Selected`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`Kata_pro64_Cad2025.VmbmLightIteration [class:4164]`|`11064792`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005588`|`SamePath`|`string, string`|`bool`|`11065136`|`4`|`66ea1533cb3404e9...`|
|`0x06005589`|`FileStamp`|`string`|`string`|`11065480`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600558a`|`NameKey`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`string`|`11066208`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600558b`|`HasCurrentAnalysis`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`bool`|`11066488`|`4`|`66ea1533cb3404e9...`|
|`0x0600558c`|`CadMatches`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`bool`|`11067156`|`4`|`66ea1533cb3404e9...`|
|`0x0600558d`|`NeedsPublish`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`bool`|`11067580`|`4`|`66ea1533cb3404e9...`|
|`0x0600558e`|`HasSteel`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`bool`|`11067852`|`4`|`66ea1533cb3404e9...`|
|`0x0600558f`|`InvalidateSteel`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`void`|`11068352`|`4`|`ae3c8b8d99a39542...`|
|`0x06005590`|`RefreshEligibility`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`void`|`11068552`|`4`|`ae3c8b8d99a39542...`|
|`0x06005591`|`CheckedCombos`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168]`|`System.Collections.Generic.List`1 [class:217]<string>`|`11068920`|`4`|`8969ce0cdaa4a19c...`|
|`0x06005592`|`Reconcile`|`Kata_pro64_Cad2025.VmbmLightSession [class:4168], System.Collections.Generic.IEnumerable`1 [class:321]<Kata_pro64_Cad2025.VmbmLightCap [class:4152]>`|`void`|`11069096`|`4`|`ae3c8b8d99a39542...`|
|`0x06005593`|`AlignPileOrder`|`Kata_pro64_Cad2025.VmbmLightCap [class:4152], Kata_pro64_Cad2025.VmbmLightCap [class:4152]`|`void`|`11070764`|`4`|`ae3c8b8d99a39542...`|
|`0x06005594`|`.cctor`|`()`|`void`|`11071204`|`156`|`493a9b6422d29c61...`|
|`0x06005595`|`vd7FRwqMICg83rUL9POu`|`()`|`bool`|`11071372`|`4`|`66ea1533cb3404e9...`|
|`0x06005596`|`ACsndxqMymNVg2GKdBKN`|`()`|`Kata_pro64_Cad2025.VmbmLightState [class:5656]`|`11071392`|`4`|`8969ce0cdaa4a19c...`|

### [VM-281] Methods of `Kata_pro64_Cad2025.VmbmLightTableSelection` (Token `0x0200058d`, Displayed: 11/11, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x060055bc`|`get_WarningText`|`()`|`string`|`11071408`|`4`|`8969ce0cdaa4a19c...`|
|`0x060055bd`|`get_RestoreError`|`()`|`System.Exception [class:121]`|`11071448`|`4`|`8969ce0cdaa4a19c...`|
|`0x060055be`|`.ctor`|`CSiAPIv1.cDatabaseTables [class:2381], string`|`void`|`11071464`|`4`|`ae3c8b8d99a39542...`|
|`0x060055bf`|`VkXEeUy9w8U`|`string`|`string[]`|`11072568`|`4`|`8969ce0cdaa4a19c...`|
|`0x060055c0`|`M7KEe8gtv2a`|`string, string[]`|`void`|`11074064`|`4`|`ae3c8b8d99a39542...`|
|`0x060055c1`|`i7LEeETdQV6`|`object, object`|`System.InvalidOperationException [class:433]`|`11075232`|`4`|`8969ce0cdaa4a19c...`|
|`0x060055c2`|`JUIEe0J5axE`|`string`|`void`|`11075580`|`4`|`ae3c8b8d99a39542...`|
|`0x060055c3`|`Dispose`|`()`|`void`|`11075972`|`4`|`ae3c8b8d99a39542...`|
|`0x060055c4`|`.cctor`|`()`|`void`|`11077084`|`156`|`dfeb5f5448cac645...`|
|`0x060055c5`|`WcCJhXqMgTy04KZFfbCt`|`()`|`bool`|`11077252`|`4`|`66ea1533cb3404e9...`|
|`0x060055c6`|`LIrFTdqMh8XmY5oRoVw0`|`()`|`Kata_pro64_Cad2025.VmbmLightTableSelection [class:5684]`|`11077272`|`4`|`8969ce0cdaa4a19c...`|

### [VM-282] Methods of `Kata_pro64_Cad2025.kata8` (Token `0x0200058e`, Displayed: 28/28, Evidence `ev_0f669fdb8c0dada755dd0aa0dfe1f17b93bc49277824837c908cc64344458a44`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x060055c7`|`.cctor`|`()`|`void`|`11077288`|`241`|`bfa43553b3a825fa...`|
|`0x060055c8`|`update_xdata_thep`|`Kata_Class_Lib_Revit.info_thep [class:793]&, string`|`void`|`11077544`|`4`|`ae3c8b8d99a39542...`|
|`0x060055c9`|`update_xdata_dim`|`object&, string`|`void`|`11079480`|`4`|`ae3c8b8d99a39542...`|
|`0x060055ca`|`kiemtra_solieuve`|`()`|`bool`|`11081136`|`4`|`66ea1533cb3404e9...`|
|`0x060055cb`|`sua_poly_thep`|`Kata_Class_Lib_Revit.diem [class:85][]&`|`void`|`11085724`|`4`|`ae3c8b8d99a39542...`|
|`0x060055cc`|`Sl_diem_thep`|`string`|`i4`|`11092960`|`4`|`78d4d2062aa5373e...`|
|`0x060055cd`|`Gan_bo_polyline`|`object, object, string`|`void`|`11105584`|`4`|`ae3c8b8d99a39542...`|
|`0x060055ce`|`Ve_thep1`|`Kata_Class_Lib_Revit.diem [class:85], bool`|`object`|`11123224`|`4`|`8969ce0cdaa4a19c...`|
|`0x060055cf`|`Ve_diem_thep`|`()`|`void`|`11182968`|`4`|`ae3c8b8d99a39542...`|
|`0x060055d0`|`Them_doan_ket_thuc`|`Kata_Class_Lib_Revit.diem [class:85][]&, string`|`void`|`11184684`|`4`|`ae3c8b8d99a39542...`|
|`0x060055d1`|`Them_diem_noi_vuong`|`Kata_Class_Lib_Revit.diem [class:85][]&, i4`|`void`|`11186412`|`4`|`ae3c8b8d99a39542...`|
|`0x060055d2`|`Xd_dang_thep_co_ban`|`object`|`string`|`11186884`|`4`|`8969ce0cdaa4a19c...`|
|`0x060055d3`|`Lay_toa_do_pthep`|`object, Kata_Class_Lib_Revit.diem [class:85][]&, bool`|`bool`|`11214560`|`4`|`66ea1533cb3404e9...`|
|`0x060055d4`|`Ktra_Li_min`|`string, r8, r8[], r8`|`bool`|`11235376`|`4`|`66ea1533cb3404e9...`|
|`0x060055d5`|`Xd_chuoi_L_goc_uon_dang00`|`object`|`string`|`11250264`|`4`|`8969ce0cdaa4a19c...`|
|`0x060055d6`|`Ve_thep_tu_goc_uon`|`Kata_Class_Lib_Revit.diem [class:85], string`|`object`|`11253712`|`4`|`8969ce0cdaa4a19c...`|
|`0x060055d7`|`Xd_chuoi_L_goc_uon`|`string[], object, string`|`string`|`11256888`|`4`|`8969ce0cdaa4a19c...`|
|`0x060055d8`|`L_ngang`|`i4, Kata_Class_Lib_Revit.diem [class:85][], i4`|`i8`|`11277108`|`4`|`0c55072e2c71f84f...`|
|`0x060055d9`|`Update_leg`|`object, string[]&, string&`|`void`|`11277528`|`4`|`ae3c8b8d99a39542...`|
|`0x060055da`|`Tinh_L1`|`Kata_Class_Lib_Revit.info_cog_hook [valuetype:769], string, i4&`|`bool`|`11332776`|`4`|`66ea1533cb3404e9...`|
|`0x060055db`|`Gan_stt_0`|`()`|`void`|`11338276`|`4`|`ae3c8b8d99a39542...`|
|`0x060055dc`|`Gan_stt_1`|`()`|`void`|`11341144`|`4`|`ae3c8b8d99a39542...`|
|`0x060055dd`|`Sua_thep_chon_nhiu`|`()`|`bool`|`11344248`|`4`|`66ea1533cb3404e9...`|
|`0x060055de`|`Edit_nhiu_thep_ok`|`()`|`void`|`11347800`|`4`|`ae3c8b8d99a39542...`|
|`0x060055df`|`Chon_edit_moc_dai`|`()`|`bool`|`11355420`|`4`|`66ea1533cb3404e9...`|
|`0x060055e0`|`Edit_moc_dai_ok`|`()`|`void`|`11358612`|`4`|`ae3c8b8d99a39542...`|
|`0x060055e1`|`A6HwUSqMeASGwnZrF1Fv`|`()`|`bool`|`11365920`|`4`|`66ea1533cb3404e9...`|
|`0x060055e2`|`vCvpaCqMLxrMm1YKidXW`|`()`|`Kata_pro64_Cad2025.kata8 [class:5688]`|`11365940`|`4`|`8969ce0cdaa4a19c...`|

---

#### 2.7.1b. Bảng Chi tiết 100% Phương thức (391 Phương thức) của 13 Kiểu DTO con & Form Phụ trợ trong Bằng chứng E8 (`cad2025_sub_dtos.json` — Đồng chiếu `VM-054`, `VM-056`, `VM-057`, `VM-069`)

##### [E8-M01] Methods of `Kata_pro64_Cad2025.Form_Lop_Thep_Them` (Token `0x0200006e`, Displayed: 111/111, Evidence `ev_f94acc3c1c98356a53651451a6de4c2edf1f53989c96f086936ec4a16a60ac33`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x0600098c`|`.ctor`|`()`|`void`|`794104`|`4`|`ae3c8b8d99a39542...`|
|`0x0600098d`|`Dispose`|`bool`|`void`|`794536`|`4`|`ae3c8b8d99a39542...`|
|`0x0600098e`|`pKuv7ol4Ya`|`()`|`void`|`794904`|`4`|`ae3c8b8d99a39542...`|
|`0x0600098f`|`q6M0rI1Rr6p`|`()`|`System.Windows.Forms.TextBox [class:249]`|`812928`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000990`|`p9g0rykvQsC`|`System.Windows.Forms.TextBox [class:249]`|`void`|`812944`|`4`|`ae3c8b8d99a39542...`|
|`0x06000991`|`bTL0rRvYn9m`|`()`|`System.Windows.Forms.TextBox [class:249]`|`813048`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000992`|`jb70rgGSwKe`|`System.Windows.Forms.TextBox [class:249]`|`void`|`813064`|`4`|`ae3c8b8d99a39542...`|
|`0x06000993`|`sxf0rh7JXo8`|`()`|`System.Windows.Forms.TextBox [class:249]`|`813168`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000994`|`rEP0rZD4Byv`|`System.Windows.Forms.TextBox [class:249]`|`void`|`813184`|`4`|`ae3c8b8d99a39542...`|
|`0x06000995`|`BSb0reBf6QO`|`()`|`System.Windows.Forms.TextBox [class:249]`|`813288`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000996`|`ATr0rLBswue`|`System.Windows.Forms.TextBox [class:249]`|`void`|`813304`|`4`|`ae3c8b8d99a39542...`|
|`0x06000997`|`LL70rTCH8C1`|`()`|`System.Windows.Forms.TextBox [class:249]`|`813408`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000998`|`aiR0rfB4QTu`|`System.Windows.Forms.TextBox [class:249]`|`void`|`813424`|`4`|`ae3c8b8d99a39542...`|
|`0x06000999`|`NVa0rcTMJNs`|`()`|`System.Windows.Forms.TextBox [class:249]`|`813528`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600099a`|`uS90r6yrNwX`|`System.Windows.Forms.TextBox [class:249]`|`void`|`813544`|`4`|`ae3c8b8d99a39542...`|
|`0x0600099b`|`k7R0huRyZOy`|`()`|`System.Windows.Forms.BindingSource [class:237]`|`813648`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600099c`|`zNn0hHpucM0`|`System.Windows.Forms.BindingSource [class:237]`|`void`|`813664`|`4`|`ae3c8b8d99a39542...`|
|`0x0600099d`|`get_OK`|`()`|`System.Windows.Forms.Button [class:161]`|`813768`|`4`|`8969ce0cdaa4a19c...`|
|`0x0600099e`|`set_OK`|`System.Windows.Forms.Button [class:161]`|`void`|`813784`|`4`|`ae3c8b8d99a39542...`|
|`0x0600099f`|`Ovr0rWl9rLn`|`()`|`System.Windows.Forms.TextBox [class:249]`|`814192`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009a0`|`fk20rsjNplb`|`System.Windows.Forms.TextBox [class:249]`|`void`|`814208`|`4`|`ae3c8b8d99a39542...`|
|`0x060009a1`|`qVo0rtB6PoL`|`()`|`System.Windows.Forms.TextBox [class:249]`|`814312`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009a2`|`XrB0rdNQ3hf`|`System.Windows.Forms.TextBox [class:249]`|`void`|`814328`|`4`|`ae3c8b8d99a39542...`|
|`0x060009a3`|`n7Q0rJ22Ov0`|`()`|`System.Windows.Forms.TextBox [class:249]`|`814432`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009a4`|`aDt0rrnS17J`|`System.Windows.Forms.TextBox [class:249]`|`void`|`814448`|`4`|`ae3c8b8d99a39542...`|
|`0x060009a5`|`se20rGnYrwe`|`()`|`System.Windows.Forms.TextBox [class:249]`|`814552`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009a6`|`NdX0rlZGreQ`|`System.Windows.Forms.TextBox [class:249]`|`void`|`814568`|`4`|`ae3c8b8d99a39542...`|
|`0x060009a7`|`YBu0ruu34tq`|`()`|`System.Windows.Forms.TextBox [class:249]`|`814672`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009a8`|`HAi0rHL6ND1`|`System.Windows.Forms.TextBox [class:249]`|`void`|`814688`|`4`|`ae3c8b8d99a39542...`|
|`0x060009a9`|`F450h5Oc62a`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`814792`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009aa`|`ec30hSAXSkn`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`814808`|`4`|`ae3c8b8d99a39542...`|
|`0x060009ab`|`YNX0rphOeWD`|`()`|`System.Windows.Forms.ListView [class:849]`|`815512`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009ac`|`Iw50rwlwgT6`|`System.Windows.Forms.ListView [class:849]`|`void`|`815528`|`4`|`ae3c8b8d99a39542...`|
|`0x060009ad`|`TlL0r2AKe4y`|`()`|`System.Windows.Forms.ColumnHeader [class:853]`|`815876`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009ae`|`GXB0rXeUHLX`|`System.Windows.Forms.ColumnHeader [class:853]`|`void`|`815892`|`4`|`ae3c8b8d99a39542...`|
|`0x060009af`|`Dse0riyNTfs`|`()`|`System.Windows.Forms.TextBox [class:249]`|`815996`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009b0`|`D7P0rnwLhGf`|`System.Windows.Forms.TextBox [class:249]`|`void`|`816012`|`4`|`ae3c8b8d99a39542...`|
|`0x060009b1`|`DpK0sc5MnnS`|`()`|`System.Windows.Forms.Label [class:245]`|`816356`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009b2`|`jRQ0s6mcPjx`|`System.Windows.Forms.Label [class:245]`|`void`|`816372`|`4`|`ae3c8b8d99a39542...`|
|`0x060009b3`|`OqP0r5jWkbN`|`()`|`System.Windows.Forms.Label [class:245]`|`816476`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009b4`|`O4n0rSRqtI4`|`System.Windows.Forms.Label [class:245]`|`void`|`816492`|`4`|`ae3c8b8d99a39542...`|
|`0x060009b5`|`HKY0rjfvpFc`|`()`|`System.Windows.Forms.TextBox [class:249]`|`816596`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009b6`|`nB80rxhYI45`|`System.Windows.Forms.TextBox [class:249]`|`void`|`816612`|`4`|`ae3c8b8d99a39542...`|
|`0x060009b7`|`gKt0rDqvcPB`|`()`|`System.Windows.Forms.Button [class:161]`|`816980`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009b8`|`VGI0rBHOaE2`|`System.Windows.Forms.Button [class:161]`|`void`|`816996`|`4`|`ae3c8b8d99a39542...`|
|`0x060009b9`|`ker0rOV0HUn`|`()`|`System.Windows.Forms.Button [class:161]`|`817300`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009ba`|`fdt0rVLPyJa`|`System.Windows.Forms.Button [class:161]`|`void`|`817316`|`4`|`ae3c8b8d99a39542...`|
|`0x060009bb`|`ROn0raBJSBE`|`()`|`System.Windows.Forms.TextBox [class:249]`|`817704`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009bc`|`BRi0rzoRX8y`|`System.Windows.Forms.TextBox [class:249]`|`void`|`817720`|`4`|`ae3c8b8d99a39542...`|
|`0x060009bd`|`CCA0ZowoUwH`|`()`|`System.Windows.Forms.Label [class:245]`|`818088`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009be`|`s3g0Z7VmfVf`|`System.Windows.Forms.Label [class:245]`|`void`|`818104`|`4`|`ae3c8b8d99a39542...`|
|`0x060009bf`|`lts0GU8YYVF`|`()`|`System.Windows.Forms.ColumnHeader [class:853]`|`818208`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009c0`|`KwW0G8bPqbl`|`System.Windows.Forms.ColumnHeader [class:853]`|`void`|`818224`|`4`|`ae3c8b8d99a39542...`|
|`0x060009c1`|`ASA0GEreRGe`|`()`|`System.Windows.Forms.ColumnHeader [class:853]`|`818328`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009c2`|`pAA0G0DjM0S`|`System.Windows.Forms.ColumnHeader [class:853]`|`void`|`818344`|`4`|`ae3c8b8d99a39542...`|
|`0x060009c3`|`DBU0GPDnRQ9`|`()`|`System.Windows.Forms.ColumnHeader [class:853]`|`818448`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009c4`|`wWx0G3eZZMK`|`System.Windows.Forms.ColumnHeader [class:853]`|`void`|`818464`|`4`|`ae3c8b8d99a39542...`|
|`0x060009c5`|`VuF0GqqgU9D`|`()`|`System.Windows.Forms.TextBox [class:249]`|`818568`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009c6`|`zda0GAgehm0`|`System.Windows.Forms.TextBox [class:249]`|`void`|`818584`|`4`|`ae3c8b8d99a39542...`|
|`0x060009c7`|`tue0LObWfVm`|`()`|`System.Windows.Forms.Label [class:245]`|`818688`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009c8`|`SJC0LVQ2df9`|`System.Windows.Forms.Label [class:245]`|`void`|`818704`|`4`|`ae3c8b8d99a39542...`|
|`0x060009c9`|`MZn0fe7UJWt`|`()`|`System.Windows.Forms.Panel [class:373]`|`818808`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009ca`|`d5N0fL1RpR2`|`System.Windows.Forms.Panel [class:373]`|`void`|`818824`|`4`|`ae3c8b8d99a39542...`|
|`0x060009cb`|`IU80GCcoBRa`|`()`|`System.Windows.Forms.ColumnHeader [class:853]`|`818928`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009cc`|`SBE0Gmu6gei`|`System.Windows.Forms.ColumnHeader [class:853]`|`void`|`818944`|`4`|`ae3c8b8d99a39542...`|
|`0x060009cd`|`cAa0GQEQQkm`|`()`|`System.Windows.Forms.ColumnHeader [class:853]`|`819048`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009ce`|`nty0GkFNldv`|`System.Windows.Forms.ColumnHeader [class:853]`|`void`|`819064`|`4`|`ae3c8b8d99a39542...`|
|`0x060009cf`|`Tq30G4xPdxR`|`()`|`System.Windows.Forms.ColumnHeader [class:853]`|`819168`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009d0`|`Msy0GYCyxd6`|`System.Windows.Forms.ColumnHeader [class:853]`|`void`|`819184`|`4`|`ae3c8b8d99a39542...`|
|`0x060009d1`|`ax90GoKUTNh`|`()`|`System.Windows.Forms.TextBox [class:249]`|`819288`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009d2`|`br20G7i2Imo`|`System.Windows.Forms.TextBox [class:249]`|`void`|`819304`|`4`|`ae3c8b8d99a39542...`|
|`0x060009d3`|`aQM0GFgWUOC`|`()`|`System.Windows.Forms.Label [class:245]`|`819672`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009d4`|`dRd0Gvyq4YN`|`System.Windows.Forms.Label [class:245]`|`void`|`819688`|`4`|`ae3c8b8d99a39542...`|
|`0x060009d5`|`xOm0ZQEvAmp`|`()`|`System.Windows.Forms.Label [class:245]`|`819792`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009d6`|`X9v0ZkfUlXU`|`System.Windows.Forms.Label [class:245]`|`void`|`819808`|`4`|`ae3c8b8d99a39542...`|
|`0x060009d7`|`J7o0G1rnl2S`|`()`|`System.Windows.Forms.TextBox [class:249]`|`819912`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009d8`|`UsS0GMyjLr4`|`System.Windows.Forms.TextBox [class:249]`|`void`|`819928`|`4`|`ae3c8b8d99a39542...`|
|`0x060009d9`|`zP60Z43yd3J`|`()`|`System.Windows.Forms.Label [class:245]`|`820272`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009da`|`Ggy0ZYPrMrH`|`System.Windows.Forms.Label [class:245]`|`void`|`820288`|`4`|`ae3c8b8d99a39542...`|
|`0x060009db`|`e8l0G9wwZXw`|`()`|`System.Windows.Forms.TextBox [class:249]`|`820392`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009dc`|`jHP0GKku3Dl`|`System.Windows.Forms.TextBox [class:249]`|`void`|`820408`|`4`|`ae3c8b8d99a39542...`|
|`0x060009dd`|`RLkvF0unXm`|`object, System.EventArgs [class:113]`|`void`|`820776`|`4`|`ae3c8b8d99a39542...`|
|`0x060009de`|`Sqyvv8FKVk`|`()`|`void`|`821308`|`4`|`ae3c8b8d99a39542...`|
|`0x060009df`|`zKev1D68s9`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`823292`|`4`|`ae3c8b8d99a39542...`|
|`0x060009e0`|`OpdvM1cMTd`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`823792`|`4`|`ae3c8b8d99a39542...`|
|`0x060009e1`|`DPdv9nnG3l`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`824056`|`4`|`ae3c8b8d99a39542...`|
|`0x060009e2`|`GRkvKpXA65`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`824448`|`4`|`ae3c8b8d99a39542...`|
|`0x060009e3`|`ve_mc_mong`|`()`|`void`|`824608`|`4`|`ae3c8b8d99a39542...`|
|`0x060009e4`|`ve_thep_them_duoi_mat_cat`|`i4`|`void`|`830100`|`4`|`ae3c8b8d99a39542...`|
|`0x060009e5`|`ve_diem_thep_them_duoi_mat_cat`|`i4`|`void`|`831236`|`4`|`ae3c8b8d99a39542...`|
|`0x060009e6`|`layTapCurveDNMB`|`Kata_Class_Lib_Revit.diem [class:85]`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Line_diem [class:789]>`|`832696`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009e7`|`layTapCurveTNMB`|`Kata_Class_Lib_Revit.diem [class:85]`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Line_diem [class:789]>`|`833988`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009e8`|`layTapCurveDDMB`|`Kata_Class_Lib_Revit.diem [class:85]`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Line_diem [class:789]>`|`835172`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009e9`|`tao_dim_tu_tap_XYZ3`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, System.Drawing.Point [valuetype:173], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], r8, System.Drawing.Color [valuetype:697]`|`void`|`836420`|`4`|`ae3c8b8d99a39542...`|
|`0x060009ea`|`tao_dim_tu_tap_XYZ2`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, System.Drawing.Point [valuetype:173], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], r8, System.Drawing.Color [valuetype:697]`|`void`|`838560`|`4`|`ae3c8b8d99a39542...`|
|`0x060009eb`|`tao_dim_tu_tap_XYZ1`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, System.Drawing.Point [valuetype:173], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], r8, System.Drawing.Color [valuetype:697]`|`void`|`840636`|`4`|`ae3c8b8d99a39542...`|
|`0x060009ec`|`iOpvN97Evc`|`()`|`void`|`843492`|`4`|`ae3c8b8d99a39542...`|
|`0x060009ed`|`dr2vbVELGI`|`object, System.EventArgs [class:113]`|`void`|`845276`|`4`|`ae3c8b8d99a39542...`|
|`0x060009ee`|`d7OvIewGBr`|`object, System.EventArgs [class:113]`|`void`|`845848`|`4`|`ae3c8b8d99a39542...`|
|`0x060009ef`|`V5YvyQ25ar`|`object, System.EventArgs [class:113]`|`void`|`847048`|`4`|`ae3c8b8d99a39542...`|
|`0x060009f0`|`YamvR1OH5G`|`object, System.EventArgs [class:113]`|`void`|`847856`|`4`|`ae3c8b8d99a39542...`|
|`0x060009f1`|`scEvgl7G6U`|`object, System.EventArgs [class:113]`|`void`|`848152`|`4`|`ae3c8b8d99a39542...`|
|`0x060009f2`|`Rkhvh0avJn`|`object, System.EventArgs [class:113]`|`void`|`848452`|`4`|`ae3c8b8d99a39542...`|
|`0x060009f3`|`mruvZdUt1B`|`object, System.EventArgs [class:113]`|`void`|`848748`|`4`|`ae3c8b8d99a39542...`|
|`0x060009f4`|`ci2veVL71e`|`object, System.EventArgs [class:113]`|`void`|`849064`|`4`|`ae3c8b8d99a39542...`|
|`0x060009f5`|`q07vLhohZJ`|`object, System.EventArgs [class:113]`|`void`|`849384`|`4`|`ae3c8b8d99a39542...`|
|`0x060009f6`|`cukvTvKD3x`|`object, System.EventArgs [class:113]`|`void`|`849684`|`4`|`ae3c8b8d99a39542...`|
|`0x060009f7`|`HuGvf5YD5a`|`object, System.Windows.Forms.FormClosingEventArgs [class:301]`|`void`|`849792`|`4`|`ae3c8b8d99a39542...`|
|`0x060009f8`|`.cctor`|`()`|`void`|`849948`|`177`|`2b0c546a29b3bb2d...`|
|`0x060009f9`|`JQBDdNqELWiaRVWrjBff`|`()`|`bool`|`850140`|`4`|`66ea1533cb3404e9...`|
|`0x060009fa`|`XNtcNVqETKaEZUi9jtGM`|`()`|`Kata_pro64_Cad2025.Form_Lop_Thep_Them [class:440]`|`850160`|`4`|`8969ce0cdaa4a19c...`|

##### [E8-M02] Methods of `Kata_pro64_Cad2025.Form_Lop_Thep_Them_Ngang` (Token `0x0200006f`, Displayed: 108/108, Evidence `ev_f94acc3c1c98356a53651451a6de4c2edf1f53989c96f086936ec4a16a60ac33`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x060009fb`|`.ctor`|`()`|`void`|`850176`|`4`|`ae3c8b8d99a39542...`|
|`0x060009fc`|`Dispose`|`bool`|`void`|`850648`|`4`|`ae3c8b8d99a39542...`|
|`0x060009fd`|`b2Z110hCGF`|`()`|`void`|`851056`|`4`|`ae3c8b8d99a39542...`|
|`0x060009fe`|`q6M0rI1Rr6p`|`()`|`System.Windows.Forms.TextBox [class:249]`|`868992`|`4`|`8969ce0cdaa4a19c...`|
|`0x060009ff`|`p9g0rykvQsC`|`System.Windows.Forms.TextBox [class:249]`|`void`|`869008`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a00`|`bTL0rRvYn9m`|`()`|`System.Windows.Forms.TextBox [class:249]`|`869112`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a01`|`jb70rgGSwKe`|`System.Windows.Forms.TextBox [class:249]`|`void`|`869128`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a02`|`sxf0rh7JXo8`|`()`|`System.Windows.Forms.TextBox [class:249]`|`869232`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a03`|`rEP0rZD4Byv`|`System.Windows.Forms.TextBox [class:249]`|`void`|`869248`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a04`|`BSb0reBf6QO`|`()`|`System.Windows.Forms.TextBox [class:249]`|`869352`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a05`|`ATr0rLBswue`|`System.Windows.Forms.TextBox [class:249]`|`void`|`869368`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a06`|`LL70rTCH8C1`|`()`|`System.Windows.Forms.TextBox [class:249]`|`869472`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a07`|`aiR0rfB4QTu`|`System.Windows.Forms.TextBox [class:249]`|`void`|`869488`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a08`|`NVa0rcTMJNs`|`()`|`System.Windows.Forms.TextBox [class:249]`|`869592`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a09`|`uS90r6yrNwX`|`System.Windows.Forms.TextBox [class:249]`|`void`|`869608`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a0a`|`k7R0huRyZOy`|`()`|`System.Windows.Forms.BindingSource [class:237]`|`869712`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a0b`|`zNn0hHpucM0`|`System.Windows.Forms.BindingSource [class:237]`|`void`|`869728`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a0c`|`get_OK`|`()`|`System.Windows.Forms.Button [class:161]`|`869832`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a0d`|`set_OK`|`System.Windows.Forms.Button [class:161]`|`void`|`869848`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a0e`|`Ovr0rWl9rLn`|`()`|`System.Windows.Forms.TextBox [class:249]`|`870172`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a0f`|`fk20rsjNplb`|`System.Windows.Forms.TextBox [class:249]`|`void`|`870188`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a10`|`qVo0rtB6PoL`|`()`|`System.Windows.Forms.TextBox [class:249]`|`870292`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a11`|`XrB0rdNQ3hf`|`System.Windows.Forms.TextBox [class:249]`|`void`|`870308`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a12`|`n7Q0rJ22Ov0`|`()`|`System.Windows.Forms.TextBox [class:249]`|`870412`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a13`|`aDt0rrnS17J`|`System.Windows.Forms.TextBox [class:249]`|`void`|`870428`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a14`|`se20rGnYrwe`|`()`|`System.Windows.Forms.TextBox [class:249]`|`870532`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a15`|`NdX0rlZGreQ`|`System.Windows.Forms.TextBox [class:249]`|`void`|`870548`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a16`|`YBu0ruu34tq`|`()`|`System.Windows.Forms.TextBox [class:249]`|`870652`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a17`|`HAi0rHL6ND1`|`System.Windows.Forms.TextBox [class:249]`|`void`|`870668`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a18`|`F450h5Oc62a`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`870772`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a19`|`ec30hSAXSkn`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`870788`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a1a`|`YNX0rphOeWD`|`()`|`System.Windows.Forms.ListView [class:849]`|`871572`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a1b`|`Iw50rwlwgT6`|`System.Windows.Forms.ListView [class:849]`|`void`|`871588`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a1c`|`TlL0r2AKe4y`|`()`|`System.Windows.Forms.ColumnHeader [class:853]`|`871932`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a1d`|`GXB0rXeUHLX`|`System.Windows.Forms.ColumnHeader [class:853]`|`void`|`871948`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a1e`|`Dse0riyNTfs`|`()`|`System.Windows.Forms.TextBox [class:249]`|`872052`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a1f`|`D7P0rnwLhGf`|`System.Windows.Forms.TextBox [class:249]`|`void`|`872068`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a20`|`DpK0sc5MnnS`|`()`|`System.Windows.Forms.Label [class:245]`|`872496`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a21`|`jRQ0s6mcPjx`|`System.Windows.Forms.Label [class:245]`|`void`|`872512`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a22`|`OqP0r5jWkbN`|`()`|`System.Windows.Forms.Label [class:245]`|`872616`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a23`|`O4n0rSRqtI4`|`System.Windows.Forms.Label [class:245]`|`void`|`872632`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a24`|`HKY0rjfvpFc`|`()`|`System.Windows.Forms.TextBox [class:249]`|`872736`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a25`|`nB80rxhYI45`|`System.Windows.Forms.TextBox [class:249]`|`void`|`872752`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a26`|`aQM0GFgWUOC`|`()`|`System.Windows.Forms.Label [class:245]`|`873120`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a27`|`dRd0Gvyq4YN`|`System.Windows.Forms.Label [class:245]`|`void`|`873136`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a28`|`ax90GoKUTNh`|`()`|`System.Windows.Forms.TextBox [class:249]`|`873240`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a29`|`br20G7i2Imo`|`System.Windows.Forms.TextBox [class:249]`|`void`|`873256`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a2a`|`gKt0rDqvcPB`|`()`|`System.Windows.Forms.Button [class:161]`|`873684`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a2b`|`VGI0rBHOaE2`|`System.Windows.Forms.Button [class:161]`|`void`|`873700`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a2c`|`ker0rOV0HUn`|`()`|`System.Windows.Forms.Button [class:161]`|`874084`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a2d`|`fdt0rVLPyJa`|`System.Windows.Forms.Button [class:161]`|`void`|`874100`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a2e`|`ROn0raBJSBE`|`()`|`System.Windows.Forms.TextBox [class:249]`|`874448`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a2f`|`BRi0rzoRX8y`|`System.Windows.Forms.TextBox [class:249]`|`void`|`874464`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a30`|`CCA0ZowoUwH`|`()`|`System.Windows.Forms.Label [class:245]`|`874852`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a31`|`s3g0Z7VmfVf`|`System.Windows.Forms.Label [class:245]`|`void`|`874868`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a32`|`xOm0ZQEvAmp`|`()`|`System.Windows.Forms.Label [class:245]`|`874972`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a33`|`X9v0ZkfUlXU`|`System.Windows.Forms.Label [class:245]`|`void`|`874988`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a34`|`J7o0G1rnl2S`|`()`|`System.Windows.Forms.TextBox [class:249]`|`875092`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a35`|`UsS0GMyjLr4`|`System.Windows.Forms.TextBox [class:249]`|`void`|`875108`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a36`|`zP60Z43yd3J`|`()`|`System.Windows.Forms.Label [class:245]`|`875496`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a37`|`Ggy0ZYPrMrH`|`System.Windows.Forms.Label [class:245]`|`void`|`875512`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a38`|`e8l0G9wwZXw`|`()`|`System.Windows.Forms.TextBox [class:249]`|`875616`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a39`|`jHP0GKku3Dl`|`System.Windows.Forms.TextBox [class:249]`|`void`|`875632`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a3a`|`lts0GU8YYVF`|`()`|`System.Windows.Forms.ColumnHeader [class:853]`|`875976`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a3b`|`KwW0G8bPqbl`|`System.Windows.Forms.ColumnHeader [class:853]`|`void`|`875992`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a3c`|`ASA0GEreRGe`|`()`|`System.Windows.Forms.ColumnHeader [class:853]`|`876096`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a3d`|`pAA0G0DjM0S`|`System.Windows.Forms.ColumnHeader [class:853]`|`void`|`876112`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a3e`|`DBU0GPDnRQ9`|`()`|`System.Windows.Forms.ColumnHeader [class:853]`|`876216`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a3f`|`wWx0G3eZZMK`|`System.Windows.Forms.ColumnHeader [class:853]`|`void`|`876232`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a40`|`Tq30G4xPdxR`|`()`|`System.Windows.Forms.ColumnHeader [class:853]`|`876336`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a41`|`Msy0GYCyxd6`|`System.Windows.Forms.ColumnHeader [class:853]`|`void`|`876352`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a42`|`cAa0GQEQQkm`|`()`|`System.Windows.Forms.ColumnHeader [class:853]`|`876456`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a43`|`nty0GkFNldv`|`System.Windows.Forms.ColumnHeader [class:853]`|`void`|`876472`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a44`|`IU80GCcoBRa`|`()`|`System.Windows.Forms.ColumnHeader [class:853]`|`876576`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a45`|`SBE0Gmu6gei`|`System.Windows.Forms.ColumnHeader [class:853]`|`void`|`876592`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a46`|`VuF0GqqgU9D`|`()`|`System.Windows.Forms.TextBox [class:249]`|`876696`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a47`|`zda0GAgehm0`|`System.Windows.Forms.TextBox [class:249]`|`void`|`876712`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a48`|`tue0LObWfVm`|`()`|`System.Windows.Forms.Label [class:245]`|`876816`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a49`|`SJC0LVQ2df9`|`System.Windows.Forms.Label [class:245]`|`void`|`876832`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a4a`|`MZn0fe7UJWt`|`()`|`System.Windows.Forms.Panel [class:373]`|`876936`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a4b`|`d5N0fL1RpR2`|`System.Windows.Forms.Panel [class:373]`|`void`|`876952`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a4c`|`vNH1MbjZKK`|`object, System.EventArgs [class:113]`|`void`|`877056`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a4d`|`qQJ19vvj7o`|`()`|`void`|`877588`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a4e`|`Pi11KQCDkg`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`879572`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a4f`|`lNO1N84QoE`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`880052`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a50`|`MfP1b8eDWH`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`880292`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a51`|`MYa1II2DcV`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`880684`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a52`|`ve_mc_mong`|`()`|`void`|`880844`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a53`|`ve_thep_them_duoi_mat_cat`|`i4`|`void`|`885720`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a54`|`ve_diem_thep_them_duoi_mat_cat`|`i4`|`void`|`887508`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a55`|`tao_dim_tu_tap_XYZ3`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, System.Drawing.Point [valuetype:173], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], r8, System.Drawing.Color [valuetype:697]`|`void`|`889232`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a56`|`tao_dim_tu_tap_XYZ2`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, System.Drawing.Point [valuetype:173], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], r8, System.Drawing.Color [valuetype:697]`|`void`|`891292`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a57`|`tao_dim_tu_tap_XYZ1`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, System.Drawing.Point [valuetype:173], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], r8, System.Drawing.Color [valuetype:697]`|`void`|`893472`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a58`|`Cut1y2UNxJ`|`()`|`void`|`896224`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a59`|`mTd1R26sTs`|`object, System.EventArgs [class:113]`|`void`|`897244`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a5a`|`IAf1gF3DSW`|`object, System.EventArgs [class:113]`|`void`|`897860`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a5b`|`jaq1hrEOjG`|`object, System.EventArgs [class:113]`|`void`|`898792`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a5c`|`s7W1ZDU1VK`|`object, System.EventArgs [class:113]`|`void`|`899516`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a5d`|`C331e6KkUl`|`object, System.EventArgs [class:113]`|`void`|`899832`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a5e`|`e9R1Lk4WcH`|`object, System.EventArgs [class:113]`|`void`|`900132`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a5f`|`oCR1TKthLg`|`object, System.EventArgs [class:113]`|`void`|`900432`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a60`|`jL91f1dRUQ`|`object, System.EventArgs [class:113]`|`void`|`900748`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a61`|`lSJ1cgnSi9`|`object, System.EventArgs [class:113]`|`void`|`901088`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a62`|`Kug163Pcdf`|`object, System.EventArgs [class:113]`|`void`|`901364`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a63`|`v4o1WfX0DQ`|`object, System.Windows.Forms.FormClosingEventArgs [class:301]`|`void`|`901472`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a64`|`.cctor`|`()`|`void`|`901628`|`156`|`92b5940d14ea8d13...`|
|`0x06000a65`|`zldAurqEcJKgto5KndoJ`|`()`|`bool`|`901796`|`4`|`66ea1533cb3404e9...`|
|`0x06000a66`|`JFrx7MqE6OWaKQRiT0U2`|`()`|`Kata_pro64_Cad2025.Form_Lop_Thep_Them_Ngang [class:444]`|`901816`|`4`|`8969ce0cdaa4a19c...`|

##### [E8-M03] Methods of `Kata_pro64_Cad2025.Form_Lop_Thep_Them_Tren` (Token `0x02000070`, Displayed: 110/110, Evidence `ev_f94acc3c1c98356a53651451a6de4c2edf1f53989c96f086936ec4a16a60ac33`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06000a67`|`.ctor`|`()`|`void`|`901832`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a68`|`Dispose`|`bool`|`void`|`902264`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a69`|`kpbMK2JroO`|`()`|`void`|`902672`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a6a`|`q6M0rI1Rr6p`|`()`|`System.Windows.Forms.TextBox [class:249]`|`921172`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a6b`|`p9g0rykvQsC`|`System.Windows.Forms.TextBox [class:249]`|`void`|`921188`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a6c`|`bTL0rRvYn9m`|`()`|`System.Windows.Forms.TextBox [class:249]`|`921292`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a6d`|`jb70rgGSwKe`|`System.Windows.Forms.TextBox [class:249]`|`void`|`921308`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a6e`|`sxf0rh7JXo8`|`()`|`System.Windows.Forms.TextBox [class:249]`|`921412`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a6f`|`rEP0rZD4Byv`|`System.Windows.Forms.TextBox [class:249]`|`void`|`921428`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a70`|`BSb0reBf6QO`|`()`|`System.Windows.Forms.TextBox [class:249]`|`921532`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a71`|`ATr0rLBswue`|`System.Windows.Forms.TextBox [class:249]`|`void`|`921548`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a72`|`LL70rTCH8C1`|`()`|`System.Windows.Forms.TextBox [class:249]`|`921652`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a73`|`aiR0rfB4QTu`|`System.Windows.Forms.TextBox [class:249]`|`void`|`921668`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a74`|`NVa0rcTMJNs`|`()`|`System.Windows.Forms.TextBox [class:249]`|`921772`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a75`|`uS90r6yrNwX`|`System.Windows.Forms.TextBox [class:249]`|`void`|`921788`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a76`|`k7R0huRyZOy`|`()`|`System.Windows.Forms.BindingSource [class:237]`|`921892`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a77`|`zNn0hHpucM0`|`System.Windows.Forms.BindingSource [class:237]`|`void`|`921908`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a78`|`get_OK`|`()`|`System.Windows.Forms.Button [class:161]`|`922012`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a79`|`set_OK`|`System.Windows.Forms.Button [class:161]`|`void`|`922028`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a7a`|`Ovr0rWl9rLn`|`()`|`System.Windows.Forms.TextBox [class:249]`|`922456`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a7b`|`fk20rsjNplb`|`System.Windows.Forms.TextBox [class:249]`|`void`|`922472`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a7c`|`qVo0rtB6PoL`|`()`|`System.Windows.Forms.TextBox [class:249]`|`922576`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a7d`|`XrB0rdNQ3hf`|`System.Windows.Forms.TextBox [class:249]`|`void`|`922592`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a7e`|`n7Q0rJ22Ov0`|`()`|`System.Windows.Forms.TextBox [class:249]`|`922696`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a7f`|`aDt0rrnS17J`|`System.Windows.Forms.TextBox [class:249]`|`void`|`922712`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a80`|`se20rGnYrwe`|`()`|`System.Windows.Forms.TextBox [class:249]`|`922816`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a81`|`NdX0rlZGreQ`|`System.Windows.Forms.TextBox [class:249]`|`void`|`922832`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a82`|`YBu0ruu34tq`|`()`|`System.Windows.Forms.TextBox [class:249]`|`922936`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a83`|`HAi0rHL6ND1`|`System.Windows.Forms.TextBox [class:249]`|`void`|`922952`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a84`|`F450h5Oc62a`|`()`|`System.Windows.Forms.GroupBox [class:241]`|`923056`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a85`|`ec30hSAXSkn`|`System.Windows.Forms.GroupBox [class:241]`|`void`|`923072`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a86`|`YNX0rphOeWD`|`()`|`System.Windows.Forms.ListView [class:849]`|`923752`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a87`|`Iw50rwlwgT6`|`System.Windows.Forms.ListView [class:849]`|`void`|`923768`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a88`|`TlL0r2AKe4y`|`()`|`System.Windows.Forms.ColumnHeader [class:853]`|`924132`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a89`|`GXB0rXeUHLX`|`System.Windows.Forms.ColumnHeader [class:853]`|`void`|`924148`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a8a`|`Dse0riyNTfs`|`()`|`System.Windows.Forms.TextBox [class:249]`|`924252`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a8b`|`D7P0rnwLhGf`|`System.Windows.Forms.TextBox [class:249]`|`void`|`924268`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a8c`|`DpK0sc5MnnS`|`()`|`System.Windows.Forms.Label [class:245]`|`924696`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a8d`|`jRQ0s6mcPjx`|`System.Windows.Forms.Label [class:245]`|`void`|`924712`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a8e`|`OqP0r5jWkbN`|`()`|`System.Windows.Forms.Label [class:245]`|`924816`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a8f`|`O4n0rSRqtI4`|`System.Windows.Forms.Label [class:245]`|`void`|`924832`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a90`|`HKY0rjfvpFc`|`()`|`System.Windows.Forms.TextBox [class:249]`|`924936`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a91`|`nB80rxhYI45`|`System.Windows.Forms.TextBox [class:249]`|`void`|`924952`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a92`|`aQM0GFgWUOC`|`()`|`System.Windows.Forms.Label [class:245]`|`925380`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a93`|`dRd0Gvyq4YN`|`System.Windows.Forms.Label [class:245]`|`void`|`925396`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a94`|`ax90GoKUTNh`|`()`|`System.Windows.Forms.TextBox [class:249]`|`925500`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a95`|`br20G7i2Imo`|`System.Windows.Forms.TextBox [class:249]`|`void`|`925516`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a96`|`gKt0rDqvcPB`|`()`|`System.Windows.Forms.Button [class:161]`|`925884`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a97`|`VGI0rBHOaE2`|`System.Windows.Forms.Button [class:161]`|`void`|`925900`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a98`|`ker0rOV0HUn`|`()`|`System.Windows.Forms.Button [class:161]`|`926268`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a99`|`fdt0rVLPyJa`|`System.Windows.Forms.Button [class:161]`|`void`|`926284`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a9a`|`ROn0raBJSBE`|`()`|`System.Windows.Forms.TextBox [class:249]`|`926672`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a9b`|`BRi0rzoRX8y`|`System.Windows.Forms.TextBox [class:249]`|`void`|`926688`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a9c`|`CCA0ZowoUwH`|`()`|`System.Windows.Forms.Label [class:245]`|`927076`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a9d`|`s3g0Z7VmfVf`|`System.Windows.Forms.Label [class:245]`|`void`|`927092`|`4`|`ae3c8b8d99a39542...`|
|`0x06000a9e`|`xOm0ZQEvAmp`|`()`|`System.Windows.Forms.Label [class:245]`|`927196`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000a9f`|`X9v0ZkfUlXU`|`System.Windows.Forms.Label [class:245]`|`void`|`927212`|`4`|`ae3c8b8d99a39542...`|
|`0x06000aa0`|`J7o0G1rnl2S`|`()`|`System.Windows.Forms.TextBox [class:249]`|`927316`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000aa1`|`UsS0GMyjLr4`|`System.Windows.Forms.TextBox [class:249]`|`void`|`927332`|`4`|`ae3c8b8d99a39542...`|
|`0x06000aa2`|`zP60Z43yd3J`|`()`|`System.Windows.Forms.Label [class:245]`|`927700`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000aa3`|`Ggy0ZYPrMrH`|`System.Windows.Forms.Label [class:245]`|`void`|`927716`|`4`|`ae3c8b8d99a39542...`|
|`0x06000aa4`|`e8l0G9wwZXw`|`()`|`System.Windows.Forms.TextBox [class:249]`|`927820`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000aa5`|`jHP0GKku3Dl`|`System.Windows.Forms.TextBox [class:249]`|`void`|`927836`|`4`|`ae3c8b8d99a39542...`|
|`0x06000aa6`|`lts0GU8YYVF`|`()`|`System.Windows.Forms.ColumnHeader [class:853]`|`928180`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000aa7`|`KwW0G8bPqbl`|`System.Windows.Forms.ColumnHeader [class:853]`|`void`|`928196`|`4`|`ae3c8b8d99a39542...`|
|`0x06000aa8`|`ASA0GEreRGe`|`()`|`System.Windows.Forms.ColumnHeader [class:853]`|`928300`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000aa9`|`pAA0G0DjM0S`|`System.Windows.Forms.ColumnHeader [class:853]`|`void`|`928316`|`4`|`ae3c8b8d99a39542...`|
|`0x06000aaa`|`DBU0GPDnRQ9`|`()`|`System.Windows.Forms.ColumnHeader [class:853]`|`928420`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000aab`|`wWx0G3eZZMK`|`System.Windows.Forms.ColumnHeader [class:853]`|`void`|`928436`|`4`|`ae3c8b8d99a39542...`|
|`0x06000aac`|`Tq30G4xPdxR`|`()`|`System.Windows.Forms.ColumnHeader [class:853]`|`928540`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000aad`|`Msy0GYCyxd6`|`System.Windows.Forms.ColumnHeader [class:853]`|`void`|`928556`|`4`|`ae3c8b8d99a39542...`|
|`0x06000aae`|`cAa0GQEQQkm`|`()`|`System.Windows.Forms.ColumnHeader [class:853]`|`928660`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000aaf`|`nty0GkFNldv`|`System.Windows.Forms.ColumnHeader [class:853]`|`void`|`928676`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ab0`|`IU80GCcoBRa`|`()`|`System.Windows.Forms.ColumnHeader [class:853]`|`928780`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ab1`|`SBE0Gmu6gei`|`System.Windows.Forms.ColumnHeader [class:853]`|`void`|`928796`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ab2`|`VuF0GqqgU9D`|`()`|`System.Windows.Forms.TextBox [class:249]`|`928900`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ab3`|`zda0GAgehm0`|`System.Windows.Forms.TextBox [class:249]`|`void`|`928916`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ab4`|`tue0LObWfVm`|`()`|`System.Windows.Forms.Label [class:245]`|`929020`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ab5`|`SJC0LVQ2df9`|`System.Windows.Forms.Label [class:245]`|`void`|`929036`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ab6`|`MZn0fe7UJWt`|`()`|`System.Windows.Forms.Panel [class:373]`|`929140`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ab7`|`d5N0fL1RpR2`|`System.Windows.Forms.Panel [class:373]`|`void`|`929156`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ab8`|`u4BMNPQq4G`|`object, System.EventArgs [class:113]`|`void`|`929260`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ab9`|`B0nMbAJsLS`|`()`|`void`|`929792`|`4`|`ae3c8b8d99a39542...`|
|`0x06000aba`|`p6gMIUZWyJ`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`931856`|`4`|`ae3c8b8d99a39542...`|
|`0x06000abb`|`B9GMy375Zg`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`932376`|`4`|`ae3c8b8d99a39542...`|
|`0x06000abc`|`GkjMRsESrI`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`932616`|`4`|`ae3c8b8d99a39542...`|
|`0x06000abd`|`NuwMgHRuiU`|`object, System.Windows.Forms.MouseEventArgs [class:281]`|`void`|`932968`|`4`|`ae3c8b8d99a39542...`|
|`0x06000abe`|`ve_mc_mong`|`()`|`void`|`933128`|`4`|`ae3c8b8d99a39542...`|
|`0x06000abf`|`ve_thep_them_tren_mat_cat`|`i4`|`void`|`938604`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ac0`|`ve_diem_thep_them_duoi_mat_cat`|`i4`|`void`|`939636`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ac1`|`layTapCurveTNMB`|`Kata_Class_Lib_Revit.diem [class:85]`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Line_diem [class:789]>`|`940992`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ac2`|`layTapCurveTDMB`|`Kata_Class_Lib_Revit.diem [class:85]`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.Line_diem [class:789]>`|`942156`|`4`|`8969ce0cdaa4a19c...`|
|`0x06000ac3`|`tao_dim_tu_tap_XYZ3`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, System.Drawing.Point [valuetype:173], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], r8, System.Drawing.Color [valuetype:697]`|`void`|`943340`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ac4`|`tao_dim_tu_tap_XYZ2`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, System.Drawing.Point [valuetype:173], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], r8, System.Drawing.Color [valuetype:697]`|`void`|`945520`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ac5`|`tao_dim_tu_tap_XYZ1`|`System.Collections.Generic.List`1 [class:217]<Kata_Class_Lib_Revit.diem [class:85]>, System.Drawing.Point [valuetype:173], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], Kata_Class_Lib_Revit.diem [class:85], r8, System.Drawing.Color [valuetype:697]`|`void`|`947532`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ac6`|`G2eMhp9p3i`|`()`|`void`|`950368`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ac7`|`OtkMZeulUp`|`object, System.EventArgs [class:113]`|`void`|`952152`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ac8`|`i2nMeDAp4J`|`object, System.EventArgs [class:113]`|`void`|`952788`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ac9`|`HW1MLX7nyd`|`object, System.EventArgs [class:113]`|`void`|`953780`|`4`|`ae3c8b8d99a39542...`|
|`0x06000aca`|`MFmMTUd1n1`|`object, System.EventArgs [class:113]`|`void`|`954528`|`4`|`ae3c8b8d99a39542...`|
|`0x06000acb`|`P0ZMfBXgAF`|`object, System.EventArgs [class:113]`|`void`|`954828`|`4`|`ae3c8b8d99a39542...`|
|`0x06000acc`|`bZGMcxwKSi`|`object, System.EventArgs [class:113]`|`void`|`955104`|`4`|`ae3c8b8d99a39542...`|
|`0x06000acd`|`gepM69gQlO`|`object, System.EventArgs [class:113]`|`void`|`955400`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ace`|`qpwMW0oNQC`|`object, System.EventArgs [class:113]`|`void`|`955716`|`4`|`ae3c8b8d99a39542...`|
|`0x06000acf`|`HDfMsKwEiQ`|`object, System.EventArgs [class:113]`|`void`|`955992`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ad0`|`eyjMtph1FV`|`object, System.EventArgs [class:113]`|`void`|`956292`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ad1`|`DhXMduO8Z9`|`object, System.Windows.Forms.FormClosingEventArgs [class:301]`|`void`|`956400`|`4`|`ae3c8b8d99a39542...`|
|`0x06000ad2`|`.cctor`|`()`|`void`|`956556`|`156`|`bbe176c808d3e889...`|
|`0x06000ad3`|`De8801qEsdXVE6e5aFxu`|`()`|`bool`|`956724`|`4`|`66ea1533cb3404e9...`|
|`0x06000ad4`|`TuPIUpqEtjk8Ggbua97r`|`()`|`Kata_pro64_Cad2025.Form_Lop_Thep_Them_Tren [class:448]`|`956744`|`4`|`8969ce0cdaa4a19c...`|

##### [E8-M04] Methods of `KataCircle3D` (Token `0x020002ef`, Displayed: 5/5, Evidence `ev_f94acc3c1c98356a53651451a6de4c2edf1f53989c96f086936ec4a16a60ac33`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003a07`|`.ctor`|`()`|`void`|`12198224`|`4`|`ae3c8b8d99a39542...`|
|`0x06003a08`|`.ctor`|`Kata_Class_Lib_Revit.diem [class:85], r8`|`void`|`12198452`|`4`|`ae3c8b8d99a39542...`|
|`0x06003a09`|`.cctor`|`()`|`void`|`12198640`|`156`|`0a7b5fc21e68fcdd...`|
|`0x06003a0a`|`BCu3nmqusvGlM1sb76J1`|`()`|`bool`|`12198808`|`4`|`66ea1533cb3404e9...`|
|`0x06003a0b`|`yd9ROvquteM883ZJ26Ca`|`()`|`KataCircle3D [class:3004]`|`12198828`|`4`|`8969ce0cdaa4a19c...`|

##### [E8-M05] Methods of `LopThepThem` (Token `0x020002f0`, Displayed: 4/4, Evidence `ev_f94acc3c1c98356a53651451a6de4c2edf1f53989c96f086936ec4a16a60ac33`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003a0c`|`.ctor`|`()`|`void`|`12198844`|`4`|`ae3c8b8d99a39542...`|
|`0x06003a0d`|`.cctor`|`()`|`void`|`12198968`|`156`|`ce0e3d2ae80e8d98...`|
|`0x06003a0e`|`dpI60PquJqfXqmgx9K8m`|`()`|`bool`|`12199136`|`4`|`66ea1533cb3404e9...`|
|`0x06003a0f`|`uGJcykqurWbifmrvWfmp`|`()`|`LopThepThem [class:3008]`|`12199156`|`4`|`8969ce0cdaa4a19c...`|

##### [E8-M06] Methods of `Infogrid` (Token `0x020002f3`, Displayed: 4/4, Evidence `ev_f94acc3c1c98356a53651451a6de4c2edf1f53989c96f086936ec4a16a60ac33`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003a18`|`.ctor`|`()`|`void`|`12200252`|`4`|`ae3c8b8d99a39542...`|
|`0x06003a19`|`.cctor`|`()`|`void`|`12200624`|`156`|`4b1d92ca133e2221...`|
|`0x06003a1a`|`F7vOIQquXegtsriMf4ZY`|`()`|`bool`|`12200792`|`4`|`66ea1533cb3404e9...`|
|`0x06003a1b`|`bc3qbjquiUTF1wqK9tKC`|`()`|`Infogrid [class:3020]`|`12200812`|`4`|`8969ce0cdaa4a19c...`|

##### [E8-M07] Methods of `Dlcot` (Token `0x020002f4`, Displayed: 4/4, Evidence `ev_f94acc3c1c98356a53651451a6de4c2edf1f53989c96f086936ec4a16a60ac33`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003a1c`|`.ctor`|`()`|`void`|`12200828`|`4`|`ae3c8b8d99a39542...`|
|`0x06003a1d`|`.cctor`|`()`|`void`|`12201544`|`156`|`9260691a3b42280e...`|
|`0x06003a1e`|`ABopENqu5IKtjveaoQmu`|`()`|`bool`|`12201712`|`4`|`66ea1533cb3404e9...`|
|`0x06003a1f`|`J72xRWquSoURhjA0U891`|`()`|`Dlcot [class:3024]`|`12201732`|`4`|`8969ce0cdaa4a19c...`|

##### [E8-M08] Methods of `GiaCuongThemTheoPolygon` (Token `0x02000314`, Displayed: 6/6, Evidence `ev_f94acc3c1c98356a53651451a6de4c2edf1f53989c96f086936ec4a16a60ac33`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003b60`|`.ctor`|`()`|`void`|`12354528`|`4`|`ae3c8b8d99a39542...`|
|`0x06003b61`|`CopyData`|`GiaCuongThemTheoPolygon [class:3152]`|`void`|`12354796`|`4`|`ae3c8b8d99a39542...`|
|`0x06003b62`|`SimpleLize`|`()`|`GiaCuongThemTheoPolygon [class:3152]`|`12355952`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003b63`|`.cctor`|`()`|`void`|`12356144`|`177`|`6a1938fd93586499...`|
|`0x06003b64`|`aZQSlhqHuSfflG9PIbsP`|`()`|`bool`|`12356336`|`4`|`66ea1533cb3404e9...`|
|`0x06003b65`|`HQFZbLqHH6nlyk7GWLlF`|`()`|`GiaCuongThemTheoPolygon [class:3152]`|`12356356`|`4`|`8969ce0cdaa4a19c...`|

##### [E8-M09] Methods of `LopThepThemV2` (Token `0x02000316`, Displayed: 13/13, Evidence `ev_f94acc3c1c98356a53651451a6de4c2edf1f53989c96f086936ec4a16a60ac33`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003b6c`|`RegenInfoThep`|`()`|`void`|`12356372`|`4`|`ae3c8b8d99a39542...`|
|`0x06003b6d`|`SimpleLize`|`()`|`LopThepThemV2 [class:3160]`|`12356968`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003b6e`|`CopyData`|`LopThepThemV2 [class:3160]`|`void`|`12357056`|`4`|`ae3c8b8d99a39542...`|
|`0x06003b6f`|`ToLopThepThem`|`()`|`LopThepThem [class:3008]`|`12357296`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003b70`|`.ctor`|`LopThepThem [class:3008]`|`void`|`12357460`|`4`|`ae3c8b8d99a39542...`|
|`0x06003b71`|`.ctor`|`()`|`void`|`12358508`|`4`|`ae3c8b8d99a39542...`|
|`0x06003b72`|`_Lambda$__7-0`|`string`|`void`|`12359220`|`4`|`ae3c8b8d99a39542...`|
|`0x06003b73`|`_Lambda$__7-1`|`string`|`void`|`12359344`|`4`|`ae3c8b8d99a39542...`|
|`0x06003b74`|`_Lambda$__7-2`|`string`|`void`|`12359468`|`4`|`ae3c8b8d99a39542...`|
|`0x06003b75`|`_Lambda$__7-3`|`string`|`void`|`12359592`|`4`|`ae3c8b8d99a39542...`|
|`0x06003b76`|`.cctor`|`()`|`void`|`12359716`|`156`|`29dd684a73ccabf5...`|
|`0x06003b77`|`ExRjFJqHwPhPuPI1OuPg`|`()`|`bool`|`12359884`|`4`|`66ea1533cb3404e9...`|
|`0x06003b78`|`kVdimnqH23bTSUWXPNND`|`()`|`LopThepThemV2 [class:3160]`|`12359904`|`4`|`8969ce0cdaa4a19c...`|

##### [E8-M10] Methods of `DlCotV2` (Token `0x02000318`, Displayed: 14/14, Evidence `ev_f94acc3c1c98356a53651451a6de4c2edf1f53989c96f086936ec4a16a60ac33`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003b79`|`.ctor`|`()`|`void`|`12359920`|`4`|`ae3c8b8d99a39542...`|
|`0x06003b7a`|`SimpleLize`|`()`|`DlCotV2 [class:3168]`|`12361408`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003b7b`|`ReloadByDaiCotText`|`string`|`void`|`12361872`|`4`|`ae3c8b8d99a39542...`|
|`0x06003b7c`|`IsCotChuNhatHoacTron`|`()`|`bool`|`12362336`|`4`|`66ea1533cb3404e9...`|
|`0x06003b7d`|`ToDlCot`|`()`|`Dlcot [class:3024]`|`12362452`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003b7e`|`CopyDlCot`|`Dlcot [class:3024]`|`void`|`12362708`|`4`|`ae3c8b8d99a39542...`|
|`0x06003b7f`|`CopyData`|`DlCotV2 [class:3168]`|`void`|`12363444`|`4`|`ae3c8b8d99a39542...`|
|`0x06003b80`|`Clone`|`()`|`DlCotV2 [class:3168]`|`12364628`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003b81`|`GetSoLuongDai`|`()`|`object`|`12364668`|`4`|`8969ce0cdaa4a19c...`|
|`0x06003b82`|`GetAbvDai`|`()`|`r8`|`12364944`|`4`|`f0ad83c607e59d90...`|
|`0x06003b83`|`GetAbvChu`|`()`|`r8`|`12364976`|`4`|`f0ad83c607e59d90...`|
|`0x06003b84`|`.cctor`|`()`|`void`|`12365028`|`156`|`d6b22dfcb5fe326c...`|
|`0x06003b85`|`raZxoHqHS7duq9bGxnqp`|`()`|`bool`|`12365196`|`4`|`66ea1533cb3404e9...`|
|`0x06003b86`|`YWTvSHqHjBUk4wi2mwTv`|`()`|`DlCotV2 [class:3168]`|`12365216`|`4`|`8969ce0cdaa4a19c...`|

##### [E8-M11] Methods of `InfoAreaSpring` (Token `0x02000376`, Displayed: 4/4, Evidence `ev_f94acc3c1c98356a53651451a6de4c2edf1f53989c96f086936ec4a16a60ac33`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003e11`|`.ctor`|`()`|`void`|`12517504`|`4`|`ae3c8b8d99a39542...`|
|`0x06003e12`|`.cctor`|`()`|`void`|`12517848`|`156`|`8f5b471d6557d93d...`|
|`0x06003e13`|`qG659Dq2vRjyfbfLn4Pd`|`()`|`bool`|`12518016`|`4`|`66ea1533cb3404e9...`|
|`0x06003e14`|`q3rBsSq213CvqF4aAD9j`|`()`|`InfoAreaSpring [class:3544]`|`12518036`|`4`|`8969ce0cdaa4a19c...`|

##### [E8-M12] Methods of `InfoText` (Token `0x0200037d`, Displayed: 4/4, Evidence `ev_f94acc3c1c98356a53651451a6de4c2edf1f53989c96f086936ec4a16a60ac33`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x06003e3f`|`.ctor`|`()`|`void`|`12531484`|`4`|`ae3c8b8d99a39542...`|
|`0x06003e40`|`.cctor`|`()`|`void`|`12531608`|`156`|`dfe4bf65e50af6a9...`|
|`0x06003e41`|`fipT71q2Z5xIrHv5HAmT`|`()`|`bool`|`12531776`|`4`|`66ea1533cb3404e9...`|
|`0x06003e42`|`DJMospq2eCAjWfotwrBx`|`()`|`InfoText [class:3572]`|`12531796`|`4`|`8969ce0cdaa4a19c...`|

##### [E8-M13] Methods of `StripDesignOption` (Token `0x0200052e`, Displayed: 4/4, Evidence `ev_f94acc3c1c98356a53651451a6de4c2edf1f53989c96f086936ec4a16a60ac33`)
|Token|Method Name / Attr|Parameters (Resolved)|Return Type|RVA|IL Size|IL SHA-256|
|---|---|---|---|---|---|---|
|`0x0600510f`|`.ctor`|`()`|`void`|`12875540`|`4`|`ae3c8b8d99a39542...`|
|`0x06005110`|`.cctor`|`()`|`void`|`12875820`|`156`|`c70433f99275ec09...`|
|`0x06005111`|`md0jIpA8k0qHbKwYKh2O`|`()`|`bool`|`12875988`|`4`|`66ea1533cb3404e9...`|
|`0x06005112`|`w4kmPkA84CATvYXLI3B4`|`()`|`StripDesignOption [class:5304]`|`12876008`|`4`|`8969ce0cdaa4a19c...`|

---

#### 2.7.2. Chi tiết Các Lớp Chọn lọc và Phân loại Móng trên Revit 2025 (`Kata_Class_Lib_Revit2025.dll` & `Kata_Class_Lib.dll` — `VM-283` .. `VM-288`)

Bằng chứng **E9** (`ev_855a5237...`, `ev_12ac2acf...`), **E10** (`ev_adbc463d9219c90cfb2ded17f6f3e6035c5501afd3b9c7dcca55b8ec96e25022`, SHA-256 `60af382583acd6e7d59a0965cc82b1e2eade559df5adcffbe1f599710fde6bd2`) và **E11** (`ev_834d4667bdd19a6c2de932fe8515809eaf6dfe272ebdea22db8d6f0f489839a6`, SHA-256 `ce59d45152b94b3fee1db55d935bfd4648e52e08516471a61de588cdf7009ced`):

##### `[VM-283]` Type `Kata_Class_Lib_Revit.Loai_dam` (`0x0200005f`) & `Kata_Class_Lib.Loai_dam` (`0x02000072`)

| Assembly | Type Token | Field Token | Field Name | Field Type | Flags | Row Offset | Constant / Literal |
|---|---|---|---|---|---|---|---|
| `Kata_Class_Lib_Revit2025.dll` (**E10**) | `0x0200005f` | `0x0400021a` | `value__` | `int32` (`i4`) | `1542` | `751528` | — |
| `Kata_Class_Lib_Revit2025.dll` (**E10**) | `0x0200005f` | `0x0400021b` | `dam` | `Loai_dam` | `32854` | `751536` | **`0`** (`Int32`) |
| `Kata_Class_Lib_Revit2025.dll` (**E10**) | `0x0200005f` | `0x0400021c` | `giang_mong` | `Loai_dam` | `32854` | `751544` | **`1`** (`Int32`) |
| `Kata_Class_Lib_Revit2025.dll` (**E10**) | `0x0200005f` | `0x0400021d` | `dam_mong` | `Loai_dam` | `32854` | `751552` | **`2`** (`Int32`) |
| `Kata_Class_Lib.dll` (**E11**) | `0x02000072` | `0x04000287` | `value__` | `int32` (`i4`) | `1542` | `623052` | — |
| `Kata_Class_Lib.dll` (**E11**) | `0x02000072` | `0x04000288` | `dam` | `Loai_dam` | `32854` | `623058` | **`0`** (`Int32`) |
| `Kata_Class_Lib.dll` (**E11**) | `0x02000072` | `0x04000289` | `giang_mong` | `Loai_dam` | `32854` | `623064` | **`1`** (`Int32`) |
| `Kata_Class_Lib.dll` (**E11**) | `0x02000072` | `0x0400028a` | `dam_mong` | `Loai_dam` | `32854` | `623070` | **`2`** (`Int32`) |

##### `[VM-284]` Type `Kata_Class_Lib_Revit.Element_Select` (`0x02000071`, `Kata_Class_Lib_Revit2025.dll`, Bằng chứng **E10**)

- **Trường tĩnh (`Field`):** `0x04000292` `IVbK00P4B9cbf5i3T1j` : `Kata_Class_Lib_Revit.Element_Select` (`flags = 19`, `row_offset = 752488`).
- **Danh sách phương thức chọn đối tượng móng và kết cấu trong Revit:**

| Method Token | Tên phương thức | Tham số (Đã phân giải) | Kiểu trả về | RVA | `il_size` | `il_sha256` |
|---|---|---|---|---|---|---|
| `0x06000387` | `chon_1_Column_wall` | `()` | `Autodesk.Revit.DB.Element` | `168056` | `4` | `8969ce0cdaa4a19c...` |
| `0x06000388` | `chon_nothing` | `string` | `Autodesk.Revit.DB.Element` | `168336` | `4` | `8969ce0cdaa4a19c...` |
| `0x06000389` | `chon_1_Column` | `string` | `Autodesk.Revit.DB.Element` | `168896` | `4` | `8969ce0cdaa4a19c...` |
| `0x0600038a` | `chon_1_Column_hoac_Walls` | `string` | `Autodesk.Revit.DB.Element` | `169484` | `4` | `8969ce0cdaa4a19c...` |
| `0x0600038b` | **`quet_chon_mong_coc`** | `string` | `List<Autodesk.Revit.DB.Element>` | `170064` | `4` | `8969ce0cdaa4a19c...` |
| `0x0600038c` | **`quet_chon_mong_don_mong_bang`** | `string` | `List<Autodesk.Revit.DB.Element>` | `170524` | `4` | `8969ce0cdaa4a19c...` |
| `0x0600038d` | `chon_1_stairs` | `string` | `Autodesk.Revit.DB.Element` | `170984` | `4` | `8969ce0cdaa4a19c...` |
| `0x0600038e` | `chon_nhieu_stairs` | `string` | `List<Autodesk.Revit.DB.Element>` | `171264` | `4` | `8969ce0cdaa4a19c...` |
| `0x0600038f` | `chon_1_level` | `()` | `Autodesk.Revit.DB.Element` | `171836` | `4` | `8969ce0cdaa4a19c...` |
| `0x06000390` | **`chon_nhieu_mong`** | `()` | `IList<Autodesk.Revit.DB.Element>` | `172108` | `4` | `8969ce0cdaa4a19c...` |
| `0x06000391` | **`chon_1_mong`** | `string` | `Autodesk.Revit.DB.Element` | `172232` | `4` | `8969ce0cdaa4a19c...` |
| `0x06000392` | **`chon_1_mong_tong_hop`** | `string` | `Autodesk.Revit.DB.Element` | `172792` | `4` | `8969ce0cdaa4a19c...` |
| `0x06000393` | `chon_1doi_tuong_dam_san_pr` | `string` | `Autodesk.Revit.DB.Element` | `173384` | `4` | `8969ce0cdaa4a19c...` |
| `0x06000394` | `chon_1doi_tuong_dam_pr` | `string` | `Autodesk.Revit.DB.Element` | `173972` | `4` | `8969ce0cdaa4a19c...` |
| `0x06000395` | `chon_1_floor_pr` | `string` | `Autodesk.Revit.DB.Floor` | `174560` | `4` | `8969ce0cdaa4a19c...` |
| `0x06000396` | `chon_1doi_tuong_dam_san` | `()` | `Autodesk.Revit.DB.Element` | `175152` | `4` | `8969ce0cdaa4a19c...` |
| `0x06000397` | `chon_1_floor` | `()` | `Autodesk.Revit.DB.Floor` | `175716` | `4` | `8969ce0cdaa4a19c...` |
| `0x06000398` | `chon_1doi_tuong` | `()` | `Autodesk.Revit.DB.Element` | `175992` | `4` | `8969ce0cdaa4a19c...` |
| `0x06000399` | `chon_nhieu_doi_tuong` | `()` | `IList<Autodesk.Revit.DB.Element>` | `176220` | `4` | `8969ce0cdaa4a19c...` |
| `0x0600039a` | `chon_nhieu_dam_san` | `()` | `IList<Autodesk.Revit.DB.Element>` | `176328` | `4` | `8969ce0cdaa4a19c...` |

##### `[VM-285` .. `VM-288]` Bốn Bộ lọc Chọn đối tượng Móng (`ISelectionFilter`) trong `Kata_Class_Lib_Revit2025.dll` (Bằng chứng **E10**)

| Mã quy tắc | Tên lớp Bộ lọc & Type Token | Field Token (Static Self-Ref) | Method Token | Tên phương thức | Tham số (Đã phân giải) | Kiểu trả về | RVA | `il_size` | `il_sha256` |
|---|---|---|---|---|---|---|---|---|---|
| `VM-285` | `Foundation_SelectionFilter` (`0x02000077`) | `0x04000298` (`QL4lTijKoUV1DyuOK3Z`, offset `752536`) | `0x060003c1`<br>`0x060003c2`<br>`0x060003c3`<br>`0x060003c4` | `.ctor`<br>`AllowElement`<br>`AllowReference`<br>`.cctor` | `()`<br>`Autodesk.Revit.DB.Element`<br>`Autodesk.Revit.DB.Reference, Autodesk.Revit.DB.XYZ`<br>`()` | `void`<br>`bool`<br>`bool`<br>`void` | `458572`<br>`458696`<br>`458984`<br>`458996` | `4`<br>`4`<br>`4`<br>`11` | `ae3c8b8d...`<br>`66ea1533...`<br>`66ea1533...`<br>`6388d293...` |
| `VM-286` | `Foundation_Floor_SelectionFilter` (`0x02000078`) | `0x04000299` (`aCqL5Ij3uBPgcO5JbVO`, offset `752544`) | `0x060003c7`<br>`0x060003c8`<br>`0x060003c9`<br>`0x060003ca` | `.ctor`<br>`AllowElement`<br>`AllowReference`<br>`.cctor` | `()`<br>`Autodesk.Revit.DB.Element`<br>`Autodesk.Revit.DB.Reference, Autodesk.Revit.DB.XYZ`<br>`()` | `void`<br>`bool`<br>`bool`<br>`void` | `459044`<br>`459168`<br>`459664`<br>`459676` | `4`<br>`4`<br>`4`<br>`11` | `ae3c8b8d...`<br>`66ea1533...`<br>`66ea1533...`<br>`6388d293...` |
| `VM-287` | `Column_Walls_Foundation_SelectionFilter` (`0x0200007d`) | `0x0400029e` (`q6CWS7jmF8tMVE6f41O`, offset `752584`) | `0x060003e5`<br>`0x060003e6`<br>`0x060003e7`<br>`0x060003e8` | `.ctor`<br>`AllowElement`<br>`AllowReference`<br>`.cctor` | `()`<br>`Autodesk.Revit.DB.Element`<br>`Autodesk.Revit.DB.Reference, Autodesk.Revit.DB.XYZ`<br>`()` | `void`<br>`bool`<br>`bool`<br>`void` | `462156`<br>`462280`<br>`462844`<br>`462856` | `4`<br>`4`<br>`4`<br>`11` | `ae3c8b8d...`<br>`66ea1533...`<br>`66ea1533...`<br>`6388d293...` |
| `VM-288` | `Column_Foundation_SelectionFilter` (`0x0200007e`) | `0x0400029f` (`DlP5HQjA93tehr2dZDZ`, offset `752592`) | `0x060003eb`<br>`0x060003ec`<br>`0x060003ed`<br>`0x060003ee` | `.ctor`<br>`AllowElement`<br>`AllowReference`<br>`.cctor` | `()`<br>`Autodesk.Revit.DB.Element`<br>`Autodesk.Revit.DB.Reference, Autodesk.Revit.DB.XYZ`<br>`()` | `void`<br>`bool`<br>`bool`<br>`void` | `462904`<br>`463028`<br>`463552`<br>`463564` | `4`<br>`4`<br>`4`<br>`11` | `ae3c8b8d...`<br>`66ea1533...`<br>`66ea1533...`<br>`6388d293...` |

---

### 2.8. Cơ chế Bảo vệ Thân hàm CIL trên Đĩa (`4-byte` / `8-byte` CIL Body Stubbing) và Ma trận Mã băm `il_sha256` (`VM-289` .. `VM-295`)

Kết quả kiểm tra tĩnh qua REA (`inspect_managed_members` — **E4**, **E5**, **E6**, **E7**, **E8**, **E10**) chứng minh rằng mọi phương thức thực thi nghiệp vụ (ngoại trừ `.cctor`) trong `Kata_pro64_Cad2025.dll` và `Kata_Class_Lib_Revit2025.dll` đều có phần thân hàm CIL trên đĩa bị thay thế bằng các stub `tiny` (`4 bytes` hoặc `8 bytes`), trong khi khoảng cách giữa các địa chỉ `RVA` liên tiếp vẫn phản ánh kích thước vùng mã gốc đã được đóng gói/bảo vệ.

| Mã quy tắc | Kích thước CIL (`body.il_size`) | Kiểu trả về (`return_type`) | Chuỗi lệnh CIL trên đĩa (`body.anchors`) | Mã băm `body.il_sha256` chuẩn xác | Ví dụ Phương thức đại diện & Bằng chứng REA |
|---|---|---|---|---|---|
| `VM-289` | **`4` bytes** (`tiny` header) | `void` | `IL_0000: nop`<br>`IL_0001: nop`<br>`IL_0002: nop`<br>`IL_0003: ret` | `ae3c8b8d99a39542f78af83dbbb42c81cd94199ec1b5f60a0801063e95842570` *(dạng chuẩn `2879e433...` / `ae3c8b8d...`)* | `Lenh_thep.Ve_mong_new (0x06001fd5)`, `Lenh_thep.Ve_mong_coc1 (0x06001fd6)`, `Mong_coc.Ve_ca_mong_coc (0x0600398b)` / `Ve_mong_coc (0x060039e3)`, `Ve_Mat_Bang_Mong_Module.VMBM (0x06003ded)` (**E4**, **E6**) |
| `VM-290` | **`4` bytes** (`tiny` header) | `class` / `string` / `object` / `T[]` | `IL_0000: nop`<br>`IL_0001: nop`<br>`IL_0002: ldnull`<br>`IL_0003: ret` | `8969ce0cdaa4a19cfb2d960c42f0474d8c8dc0e3a6811cd74a773585b55df5c5` | `Draw_foudation.QuetMong (0x06005337)`, `Foundation_Etabs.Get_Moment_Mong (0x060050f7)`, `AI_DrawIsolatedFoundation_Tool.Execute (0x06002c8d, RVA 4165248)`, `VmbmLightCheck.Evaluate (0x0600441e, RVA 7043452)` *(cùng đại diện `VmbmLightCheck.EvaluateCap (0x0600438e)`) * (**E4**, **E6**) |
| `VM-291` | **`4` bytes** (`tiny` header) | `bool` | `IL_0000: nop`<br>`IL_0001: nop`<br>`IL_0002: ldc.i4.1`<br>`IL_0003: ret` | `66ea1533cb3404e98df1138530e6a0f909a84571c93caf024b305d7ab6ef38a6` *(dạng `2d91ca92...` / `66ea1533...`)* | `Mong_coc.kiemTraDoiXungTheoPhuongb (0x0600399a, RVA 6218532)`, `Ve_Mat_Bang_Mong_Module.UpdateDaiMongBaoCoc (0x06003dec, RVA 6691548)`, `Foundation_SelectionFilter.AllowElement (0x060003c2)` (**E4**, **E10**) |
| `VM-292` | **`4` bytes** (`tiny` header) | `int32` (`i4`) | `IL_0000: nop`<br>`IL_0001: nop`<br>`IL_0002: ldc.i4.0`<br>`IL_0003: ret` | `78d4d2062aa5373e292e75f1421f0fe34005da2961c9d84081e959cf7e0659bf` *(dạng `2d91ca92...` / `78d4d206...`)* | `InfoMongCoc.GetSoThanhThepCot (0x060039fe, RVA 12196912)`, `VmbmLightOptions.get_MaxIterations (0x060042e3, RVA 7016380 / ref 0x0600421b)`, `VmbmLightFootingInput.get_StripIndex (0x06004594, RVA 7110352 / ref 0x0600458e)` (**E5**, **E6**) |
| `VM-293` | **`4` bytes** (`tiny` header) | `int64` (`i8`) | `IL_0000: nop`<br>`IL_0001: ldc.i4.0`<br>`IL_0002: conv.i8`<br>`IL_0003: ret` | `0c55072e2c71f84f8419d203893101e0f40f9220e88967d21f254f08e1640560` | `VmbmLightSupport.get_ColumnHandle (0x06004318)`, `VmbmLightCadBinding.get_FoundationHandle (0x0600555f)` (**E6**) |
| `VM-294` | **`4` bytes** (`tiny` header) | `double` (`r8`) | `IL_0000: nop`<br>`IL_0001: ldc.i4.0`<br>`IL_0002: conv.r8`<br>`IL_0003: ret` | `f0ad83c607e59d904320df8e63d1516f0c0d996570d1a406ab5de1eb53f96bb2` | `VmbmLightGeometry.EdgeDistance (0x060046c3)`, `VmbmLightGeometry.Distance (0x060046c5)`, `Foundation_Etabs.GetSelectedStripWidthValue (0x060050fe)` (**E4**, **E6**) |
| `VM-295` | **`8` bytes** (`tiny` header) | `valuetype` tùy chỉnh (`ViTriDatTen`, `ObjectId`, `KieuTaoDaiMongBaoCoc`, `VmbmLightShallowKind`, `SamplerStatus`, `PointF`) | `IL_0000: nop`<br>`IL_0001: ldnull`<br>`IL_0002: unbox.any <ValueTypeToken>`<br>`IL_0007: ret` | Phụ thuộc token `unbox.any`:<br>• `ViTriDatTen` (`0x0200037b`): `c41f727e2e00669d...`<br>• `ObjectId` (`0x01000037`): `acfcb9c2d49b3dc3...`<br>• `KieuTaoDaiMongBaoCoc` (`0x0200038b`): `6cb8e0e2c07ae864...`<br>• `VmbmLightShallowKind` (`0x02000439`): `327a33a272e7751c...`<br>• `SamplerStatus` (`0x010001d0`): `49e99688c2b312b9...`<br>• `PointF` (`0x01000174`): `5bf071c7791e9522...` | `Ve_Mat_Bang_Mong_Module.GetViTriDatTen (0x06003de2)`, `Ve_Mat_Bang_Mong_Module.EnsureBlockMongCoc (0x06003de7, hash acfcb9c2...)`, `Form_Update_Dai_Mong_Bao_Coc.get_KieuTao (0x06003ebb, hash 6cb8e0e2...)`, `VmbmLightShallowSession.get_Kind (0x060045d1 / ref 0x060045c9)`, `RaiMongJig.Sampler (0x06003c45, hash 49e99688...)` (**E4**, **E5**, **E6**) |

---

## 3. PHẦN B: Các Hành vi Thuật toán Chi tiết Bên trong Thân hàm Bị Che khuất (`VM-U01` .. `VM-U25`)

> **Nguyên tắc minh bạch bằng chứng (REA Governance):**
> Toàn bộ 25 mục quy tắc dưới đây (`VM-U01` .. `VM-U25`) mô tả **giả thuyết suy luận hợp lý (Inferred Hypotheses)** từ chữ ký hàm (`Method Signature`), tên tham số/trường dữ liệu (`DTO Schema`) và luồng gọi tĩnh, đồng thời **ghi nhận rõ phần chưa thể xác minh tĩnh (Unknown On-Disk Internal Logic)** do cơ chế bảo vệ thân hàm CIL `4-byte` / `8-byte` trên đĩa (đã chứng minh tại Mục 2.8, `VM-289`..`VM-295`).

#### Bảng 3.1 — Phần 1 (`VM-U01` .. `VM-U13`)

| Mã quy tắc | Phân nhóm Nghiệp vụ | Phương thức & Token Đích | Giả thuyết Suy luận từ Chữ ký hàm & Schema DTO (Inferred Hypothesis) | Nội dung Chưa xác minh tĩnh & Bằng chứng REA về Stub CIL (Unknown On-Disk Body) |
|---|---|---|---|---|
| `VM-U01` | Vẽ Móng Cọc (`Mong_coc`) | `Ve_ca_mong_coc (0x0600398b)`, `Ve_bao_MC1 (0x0600398d)`, `VeMCMongForm (0x060039e1)`, `VeMBMongForm (0x060039e2)`, `Ve_mong_coc (0x060039e3)` | Điều phối vẽ mặt bằng đài cọc và 2 mặt cắt vuông góc từ `info_mong_coc (0x04001ac7)`, sử dụng các lớp bảo vệ trong `CoversMong (0x020002f2)` và tọa độ cọc `listPointCoc (0x04001ade)`. | Thân hàm trên đĩa có `il_size = 4` (`nop; nop; nop; ret`, `il_sha256 = ae3c8b8d99a39542...`, RVA `6077916`, `6081716`, `6323920`, `6326168`, `6331068`, Bằng chứng **E4** `ev_3bdf54e8...`). |
| `VM-U02` | Vẽ & Tính Thép Đài Cọc (`Mong_coc`) | `Ve_thep_them_duoi_doc (0x06003991)`, `Ve_thep_them_tren_doc (0x06003992)`, `TinhToanThepDocMongCoc (0x06003993)`, `TinhToanThepNgangMongCoc (0x06003994)`, `ve_cot_mong_coc (0x06003995)` | Sinh và tính các thanh cốt thép lớp dưới/lớp trên tăng cường, cốt thép dọc/ngang đài cọc (`thepDuoi1`, `thepDuoi2`, `thepTren1`, `thepTren2`, `thepDai`, `LopThepThemV2`, `GiaCuongThemTheoPolygon`) và thép chờ cổ cột (`DlCotV2`). | Thân hàm trên đĩa có `il_size = 4` (`nop; nop; nop; ret`, `il_sha256 = ae3c8b8d99a39542...`, RVA `6190116`, `6200436`, `6210796`, `6211472`, `6212168`, Bằng chứng **E4** `ev_3bdf54e8...`). |
| `VM-U03` | Tính Số lượng & Khoảng rải Thép Đài Cọc (`Mong_coc` & `InfoMongCoc`) | `Mong_coc.get_length_spacing_so_thanh_kc2ben (0x0600398e)`, `InfoMongCoc.GetSoThanhThepCot (0x060039fe)` | Tính chiều dài miền rải, bước thép, số lượng thanh và khoảng cách 2 bên mép đài có xét đến cờ `phuongB (0x04001ad4)` và lớp bảo vệ hông (`CoversMong.Hong`). | `0x0600398e` có `il_size = 4` (`ae3c8b8d...`, RVA `6186676`, **E4**); `0x060039fe` có `il_size = 4` (`78d4d2062aa5373e...`, RVA `12196912`, **E5**). |
| `VM-U04` | Lưu & Đọc JSON/XData Móng Cọc (`Mong_coc`) | `getInfoMongCocSaveJsonData (0x06003989)`, `getInfoMongCocSaveJsonData2 (0x0600398a)` | Tuần tự hóa dữ liệu móng cọc sang `InfoMongCocSave (0x020002f5)` / `InfoMongCocSaveV2 (0x0200031b)` và lưu vào XData dưới khóa `LOAIDATAMONGCOC (0x04001ad3)`. | Thân hàm trên đĩa có `il_size = 4` (`nop; nop; ldnull; ret`, `il_sha256 = 8969ce0cdaa4a19c...`, RVA `6076344` & `6077884`, Bằng chứng **E4** `ev_3bdf54e8...`, **E5** `ev_1edd133c...`). |
| `VM-U05` | Quét Nhận diện Móng từ Bản vẽ (`Draw_foudation`) | `Draw_foudation.QuetMong (0x06005337, trả về Tuple<InfoMongCoc, diem>)` | Cho phép người dùng quét chọn các đối tượng hình học mặt bằng đài cọc, cọc và cột trên bản vẽ AutoCAD để tái tạo đối tượng `InfoMongCoc` cùng điểm gốc `diem`. | Thân hàm trên đĩa có `il_size = 4` (`nop; nop; ldnull; ret`, `il_sha256 = 8969ce0cdaa4a19c...`, RVA `9995560`, Bằng chứng **E4** `ev_3bdf54e8...`). |
| `VM-U06` | Rải Móng Cọc & Móng Đơn Hàng loạt (`Rai_Mong_Module`) | `Rai_Mong_Module.RM (0x06003c3a)`, `InfoRaiMong.TinhToanMong (0x06003c3e)`, `InfoRaiMongDon (0x02000338)`, `InfoRaiMongDon.TinhToanMong (0x06003c4e)`, `RaiMongJig.Sampler (0x06003c45, hash 49e99688...)` | Dựa trên thông số `InfoRaiMong (0x02000336)` hoặc `InfoRaiMongDon (0x02000338)`, kết hợp `RaiMongJig (0x02000337)` (`Sampler 0x06003c45`) để tính toán và rải các đài cọc / móng đơn vào vị trí chân cột. | `0x06003c3a` (RVA `6496320`), `0x06003c3e` (RVA `12392736`), `0x06003c4e` (RVA `12398372`) có `il_size = 4` (`ae3c8b8d...`); `RaiMongJig.Sampler (0x06003c45)` có `il_size = 8` (RVA `12395160`, `hash 49e99688...`, **E4**, **E5**). |
| `VM-U07` | Tự động Phát sinh Mặt bằng Móng Cổ điển (`Ve_Mat_Bang_Mong_Module`) | `Ve_Mat_Bang_Mong_Module.VMBM (0x06003ded)`, `InfoVeMatBangMong.TinhToanMatBangMong (0x06003df2)`, `InfoMBMongBang.TinhToanBaoLanDauSauKhiQuet (0x06003e1d)`, `InfoMBMongBang.RegenMBMongBang (0x06003e1e)`, `InfoMBMongDon.TinhToanMatBangMong (0x06003ea3)` | Từ phản lực chân cột (`LOAIDATAJOINTREACTION`) và sức chịu tải thiết kế của cọc/đất nền, tự động tính số lượng cọc hoặc kích thước đáy móng đơn/móng băng và phát sinh mặt bằng móng. | Thân hàm trên đĩa có `il_size = 4` (`ae3c8b8d99a39542...`, RVA `6703292`, `12509960`, `12522092`, `12526656`, `12579376`, Bằng chứng **E4** `ev_3bdf54e8...`, **E5** `ev_1edd133c...`). |
| `VM-U08` | Cập nhật & Tái tạo Đường bao Đài Móng (`Ve_Mat_Bang_Mong_Module` & `InfoCotCocGenMong`) | `Ve_Mat_Bang_Mong_Module.UpdateDaiMongBaoCoc (0x06003dec)`, `InfoCotCocGenMong.RegenPlMongByConcaveListCoc (0x06003e69)`, `InfoCotCocGenMong.RegenPlMongByOuterListCoc (0x06003e6a)` | Cập nhật và tái tạo đa giác đường bao đài móng theo danh sách cọc lõm (`RegenPlMongByConcaveListCoc`) hoặc bao ngoài (`RegenPlMongByOuterListCoc`) ứng với `KieuTaoDaiMongBaoCoc (0x0200038b)`. | `0x06003dec` (RVA `6691548`, `66ea1533...`), `0x06003e69` (RVA `12573744`, `ae3c8b8d...`), `0x06003e6a` (RVA `12574952`, `ae3c8b8d...`) đều có `il_size = 4` (**E4**, **E5**). |
| `VM-U09` | Giao tiếp Nội lực & Phản lực ETABS (`Foundation_Etabs`) | `Foundation_Etabs.Get_JointReaction (0x060050f9)`, `Get_Moment_Mong (0x060050f7)`, `ApplyStripDesignOverwrites (0x060050ff)` | Kết nối với mô hình ETABS qua `CSiAPIv1.cSapModel` để đọc phản lực nút tại tầng móng, đọc nội lực dải bản Design Strip (`MomentMong`), và ghi đè bề rộng dải bản/vật liệu cốt thép (`StripDesignOption`). | Thân hàm trên đĩa có `il_size = 4` (`8969ce0c...` / `ae3c8b8d...`, RVA `9226468`..`9238988`, Bằng chứng **E4** `ev_3bdf54e8...`). |
| `VM-U10` | Triển khai Chi tiết Móng Đơn (`view_mong_don` & `view_mong_don_new`) | `view_mong_don_new.ve_hinh_mong_don (0x06001ce9)`, `doi_location (0x06001ce8)`, `doi_du_lieu (0x06001d10)` *(cùng đại diện `view_mong_don_new.ve_mong_don (0x06001b16)`, `GetInfoMongDonSave (0x06001b6c)`, `LoadDataSave (0x06001b6d)`)*, `MongJsonDto (0x0200037e)`, `InfoMongDonSave (0x020004d2)` | Vẽ mặt bằng và mặt cắt móng đơn (hình chữ nhật hoặc vát tháp/bậc cấp theo `zc1`, `zc2`, `zc3`), bố trí cốt thép đáy theo 2 phương X/Y, cổ cột và giằng móng, đồng thời lưu/đọc qua `InfoMongDonSave (0x020004d2)` và `MongJsonDto (0x0200037e)`. | Thân hàm trên đĩa trong `view_mong_don_new (0x020000d0)` có `il_size = 4` (`ae3c8b8d99a39542...`, RVA `2865176`, `2866296`, `2878836`, Bằng chứng **E5** `ev_1edd133c...`). |
| `VM-U11` | Tự động Tính & Chọn Thép Móng (`Form_Chon_Thep_Mong_Tu_Dong`) | Các phương thức tính toán trong `Form_Chon_Thep_Mong_Tu_Dong (0x020000ad)`, trả về `KetQuaTinhThepMongTuDong (0x020000ab)` và `List<PhuongAnThepMong> (0x020000ac)` | Duyệt tổ hợp các đường kính trong danh mục mặc định `[10, 12, 14, 16, 18, 20]` (`0x0400099b`) và khoảng cách `[100, 150, 200]` (`0x0400099d`) để tìm phương án có $A_{s,\text{chọn}} \ge A_{s,\text{yc}}$ với tỷ lệ sử dụng tối ưu. | Thân hàm tính toán trên đĩa có `il_size = 4` (`8969ce0c...` / `ae3c8b8d...`, Bằng chứng **E5** `ev_1edd133c...`), trong khi danh mục mặc định đã xác thực 100% trong `.cctor` (`VM-046`). |
| `VM-U12` | Công cụ AI Vẽ Móng Đơn (`AI_DrawIsolatedFoundation_Tool`) | `CreateParametersSchema (0x06002c8c, RVA 4163300)`, `Execute (0x06002c8d, RVA 4165248)` | Khởi tạo JSON Schema tham số đầu vào (`CreateParametersSchema`) và nhận `JObject` từ tác tử AI (`Execute`), ánh xạ sang thông số móng đơn, thực thi vẽ và trả về kết quả. | Thân hàm `0x06002c8c` (RVA `4163300`) và `0x06002c8d` (RVA `4165248`) trên đĩa có `il_size = 4` (`8969ce0cdaa4a19c...`, Bằng chứng **E4** `ev_3bdf54e8...`). |
| `VM-U13` | Xuất PDF Tham chiếu Dầm/Móng cho AI (`AI_BeamFoundationReferencePdfBuilder`) | `AI_BeamFoundationReferencePdfBuilder.Build(string, string, string) (0x06002417)`, `GetReferencePdfPath(string) (0x06002416)` | Đọc hình học mặt bằng dầm/móng và xuất tệp `"AI_RebuildBeam_Model_Reference.pdf"` (`0x04000fdc`) bằng thư viện `iTextSharp`, sử dụng bảng 9 màu RGB khởi tạo trong `.cctor` (`VM-047`). | Thân hàm `Build` trên đĩa có `il_size = 4` (`8969ce0cdaa4a19c...`, RVA `3520728`, Bằng chứng **E5** `ev_1edd133c...`). |

#### Bảng 3.1 — Phần 2 (`VM-U14` .. `VM-U25`)

| Mã quy tắc | Phân nhóm Nghiệp vụ | Phương thức & Token Đích | Giả thuyết Suy luận từ Chữ ký hàm & Schema DTO (Inferred Hypothesis) | Nội dung Chưa xác minh tĩnh & Bằng chứng REA về Stub CIL (Unknown On-Disk Body) |
|---|---|---|---|---|
| `VM-U14` | Vòng lặp Phân tích & Tối ưu Đài Cọc VMBM Light (`VmbmLightEngine`) | `VmbmLightEngine.Run (0x0600464f)`, `CreateCap (0x06004654)` | Chạy vòng lặp tối đa `VmbmLightOptions.MaxIterations (0x060042e3)`: tạo đài cọc (`CreateCap`), gọi `IVmbmLightAnalysis.Analyze (0x0600464b)` để lấy phản lực cọc từ ETABS và tối ưu số cọc/kích thước đài. | Thân hàm trên đĩa có `il_size = 4` (`ae3c8b8d...` / `8969ce0c...`, RVA `7148992`..`7174152`, Bằng chứng **E6** `ev_0f669fdb...`). |
| `VM-U15` | Kiểm tra Đài Cọc & Chọn Thép VMBM Light (`VmbmLightCheck`) | `VmbmLightCheck.Evaluate (0x0600441e, RVA 7043452)`, `RequireFreeJoint (0x06004419)`, `RequireSpringStiffness (0x0600441a)`, `Finite (0x0600441d)` *(cùng đại diện `Evaluate (0x0600438d, RVA 6977196)`, `EvaluateCap (0x0600438e, RVA 6978488)`, `RequiredArea (0x06004399, RVA 6989164)`, `ChooseSteel (0x0600439a, RVA 6989600)`)*, `VmbmLightCapCheck (0x02000410)`, `VmbmLightCapSteel (0x0200040b)` | Đánh giá kết quả kiểm tra đài cọc (`VmbmLightCapCheck` `0x02000410`): tỷ số chịu lực lớn nhất (`MaxRatio`), phản lực dọc trục nhỏ nhất (`MinAxial`), tổ hợp quyết định (`GoverningCombo`), và bố trí cốt thép đài (`VmbmLightCapSteel` `0x0200040b`, `VmbmLightSteelAxis` `0x020003d8`). | Thân hàm trên đĩa có `il_size = 4` (`8969ce0c...` / `ae3c8b8d...` / `66ea1533...`, RVA `7039852`..`7043452`, Bằng chứng **E6** `ev_0f669fdb...`). |
| `VM-U16` | Kiểm tra & Tự động Tăng Tiết diện Dầm Móng VMBM Light (`VmbmLightBeamDesign`) | `VmbmLightBeamDesign.Check (0x06004495)`, `Evaluate (0x06004490)`, `Resize (0x0600448b)`, `Ensure (0x06004488)`, `VmbmLightBeamCheck (0x0200041b)` | Đọc nội lực dầm móng từ ETABS, đánh giá khả năng chịu uốn/cắt/xoắn trả về `VmbmLightBeamCheck (0x0200041b)`, và nếu vượt quá khả năng chịu lực (`NeedsWidth`, `CanResize`) thì tự động nhảy bậc tiết diện (`Resize`) theo `BeamSizeCatalog`. | Thân hàm trên đĩa có `il_size = 4` (`8969ce0c...` / `66ea1533...` / `ae3c8b8d...`, RVA `7051224`..`7065160`, Bằng chứng **E6** `ev_0f669fdb...`). |
| `VM-U17` | Thuật toán Hình học Đa giác Đài Móng (`VmbmLightGeometry` & `VmbmLightMesh`) | `VmbmLightGeometry.EdgeDistance (0x060046c3)`, `ContainsPolygon (0x060046c4)`, `Distance (0x060046c5)`; `VmbmLightMesh.Cells (0x060048bb)` | Tính khoảng cách từ tâm cọc/cột tới cạnh đa giác đài móng (`EdgeDistance`), kiểm tra đa giác nằm trong đường ranh giới đất (`ContainsPolygon`), khoảng cách tĩnh không giữa 2 đài (`Distance`), và chia lưới phần tử hữu hạn (`VmbmLightMesh.Cells`). | Thân hàm trên đĩa có `il_size = 4` (`f0ad83c6...` / `66ea1533...` / `8969ce0c...`, RVA `7174524`..`7257608`, Bằng chứng **E6** `ev_0f669fdb...`). |
| `VM-U18` | Chỉnh sửa, Xoay & Gộp Đài Móng VMBM Light (`VmbmLightEditing`) | `VmbmLightEditing.Rebuild (0x060046d4)`, `Rotate (0x060046d5)`, `Redistribute (0x060046db)`, `Merge (0x060046dc)`, `MergeNearby (0x060046dd)`, `ReadCad (0x060046df)` | Cho phép người dùng chỉnh sửa số lượng cọc, xoay góc đài cọc (`Rotate`), phân bố lại khoảng cách cọc (`Redistribute`), gộp đài theo khoảng hở `DefaultMergeGap` (`MergeNearby`), và đồng bộ ngược từ CAD (`ReadCad`). | Thân hàm trên đĩa có `il_size = 4` (`ae3c8b8d...` / `8969ce0c...`, RVA `7176876`..`7194508`, Bằng chứng **E6** `ev_0f669fdb...`). |
| `VM-U19` | Vẽ & Đồng bộ Bản vẽ CAD VMBM Light (`VmbmLightCad` & `VmbmLightState`) | `VmbmLightCad.Draw (0x060042c7)`, `SyncNamesToCad (0x060042c8)`; `VmbmLightState.Reconcile (0x06005592)`, `AlignPileOrder (0x06005593)` | Vẽ mặt bằng đài cọc, cọc, dầm móng và nhãn tên lên AutoCAD, lưu liên kết Handle (`VmbmLightCadBinding` `0x02000583`, `VmbmLightCadBeamBinding` `0x02000584`), kiểm tra dấu vân tay hình học (`Tolerance = 0.01`) và đối chiếu thứ tự cọc (`AlignPileOrder`). | Thân hàm trên đĩa có `il_size = 4` (`ae3c8b8d...` / `66ea1533...`, RVA `6991800`..`11070764`, Bằng chứng **E6** `ev_0f669fdb...`). |
| `VM-U20` | Thiết kế & Xuất Móng Nông VMBM Light (`VmbmLightShallowSession`, `VmbmLightShallowCad`, `VmbmLightShallowExport`) | `VmbmLightShallowSession.Regenerate (0x060045fa, RVA 7119648 / ref 0x060045f2, RVA 7114744)`, `FitIsolatedBoundary (0x060045fd, RVA 7123100 / ref 0x060045f5, RVA 7116852)`; `VmbmLightShallowCad.Read (0x06004622, RVA 7128408 / ref 0x0600461a, RVA 7134656)`, `Draw (0x06004624, RVA 7139376 / ref 0x0600461c, RVA 7139376)`; `VmbmLightShallowExport.Run (0x0600454c, RVA 7088116)` | Quản lý phiên thiết kế móng nông (`VmbmLightShallowKind.Isolated = 0` hoặc `Strip = 1`), tự động cắt/dịch tâm móng đơn khi vướng ranh giới đất (`FitIsolatedBoundary`), lưu dữ liệu CAD với khóa `DataKey = "VMBML_SHALLOW"` (`0x040023ba`), và xuất mô hình bản + lò xo nền (`Spring1..3`) sang ETABS. | Thân hàm trên đĩa có `il_size = 4` (`ae3c8b8d...` / `8969ce0c...` / `66ea1533...`, RVA `7088116`..`7139376`, Bằng chứng **E6** `ev_0f669fdb...`). |
| `VM-U21` | Xuất Mô hình Đài Cọc Mới & Hiện hữu sang ETABS (`VmbmLightNewPileExport` & `VmbmLightExistingPileExport`) | `VmbmLightNewPileExport.Run (0x06004539)`; `VmbmLightExistingPileExport.Run (0x06004557)` | Khởi tạo phần tử tấm vỏ (Shell/Slab) cho đài cọc, gán lò xo điểm (`Point Spring` theo `AxialStiffness` và `LateralStiffness`), tạo dải thiết kế `Design Strip` (`VmbmLightStripBinding` `0x02000585`) và dầm móng trong ETABS. | Thân hàm trên đĩa có `il_size = 4` (`ae3c8b8d99a39542...`, RVA `7080980` & `7094428`, Bằng chứng **E6** `ev_0f669fdb...`). |
| `VM-U22` | Lọc & Chọn Cấu kiện Móng trong Revit 2025 (`Foundation_SelectionFilter` & `Element_Select`) | `Foundation_SelectionFilter.AllowElement (0x060003c2)`, `Foundation_Floor_SelectionFilter.AllowElement (0x060003c8)`, `Element_Select.quet_chon_mong_coc (0x0600038b)` | Kiểm tra thuộc tính `Element.Category` trong Revit API (`BuiltInCategory.OST_StructuralFoundation`, kết hợp `OST_Floors`, `OST_StructuralColumns`, `OST_Walls` tùy bộ lọc) và lọc phân biệt đài cọc có cọc phụ thuộc với móng đơn/móng băng. | Thân hàm trên đĩa trong `Kata_Class_Lib_Revit2025.dll` có `il_size = 4` (`66ea1533...` / `8969ce0c...`, RVA `170064`..`463028`, Bằng chứng **E10** `ev_adbc463d...`). |
| `VM-U23` | Triển khai Dầm Móng & Giằng Móng (`Lenh_thep.Vedammong` & `Vegiangmong`) | `Lenh_thep.Vedammong (0x06001fde)`, `Vegiangmong (0x06001fdf)`, `Vegiangmong_shop (0x06001fe0)` | Chuyển chế độ cấu kiện dầm sang `Loai_dam.dam_mong = 2` hoặc `Loai_dam.giang_mong = 1` (`VM-043`), đảo chiều bố trí cốt thép chủ/vùng nối hoặc quy tắc neo vào đài móng so với dầm tầng (`Loai_dam.dam = 0`). | Thân hàm trên đĩa có `il_size = 4` (`ae3c8b8d99a39542...`, RVA `3206128`..`3206496`, Bằng chứng **E6** `ev_0f669fdb...`). |
| `VM-U24` | Cập nhật Móng & Phá Khối Móng (`Lenh_thep.Update_mong` & `PhaMong1`) | `Lenh_thep.Update_mong (0x06001f9e)`, `Lenh_thep.PhaMong1 (0x06001fa0)` | `udmong` đọc lại XData gắn trên các đối tượng móng để cập nhật kích thước/số hiệu thép khi người dùng kéo giãn hình học; `phamong` xóa liên kết XData Kata để biến đối tượng móng thành các thực thể AutoCAD thuần túy. | Thân hàm trên đĩa có `il_size = 4` (`ae3c8b8d99a39542...`, RVA `3195732` & `3195764`, Bằng chứng **E6** `ev_0f669fdb...`). |
| `VM-U25` | Tương tác COM Bảo vệ & Tính toán Legacy (`kata_pro.dll`) | Các giao diện COM trong `C:\kata_pro\kata_pro.dll` (`TYPELIB #1`, `DllGetClassObject` RVA `0x229c`) | Cung cấp dịch vụ kiểm tra khóa cứng/bản quyền và các hàm tiện ích VB6 thế hệ cũ thông qua COM Interop cho các module `.NET`. | Tệp `kata_pro.dll` là binary native 32-bit biên dịch từ VB6 (`MSVBVM60.DLL`, SHA-256 `2c6ab3af...`, Bằng chứng **E13** `ev_4c193f8f...`, `ev_2d35eacf...`), không chứa metadata CLI. |
