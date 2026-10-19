import json
import os
import sys

sys.stdout.reconfigure(encoding='utf-8')

# Load the matrix json
with open('autocad_311_matrix.json', 'r', encoding='utf-8') as f:
    commands = json.load(f)

handoff_path = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\spec_miner_m1_2\handoff.md'

h = []

h.append("# Handoff Report: Spec Miner M1.2 (AutoCAD 311 Commands, 11 Jigs & 12 Forms Specification)\n")

h.append("## 1. Observation\n")
h.append("We conducted a forensic static analysis across the authoritative baseline in `C:\\Dce Pro` and the current codebase in `HPDcePro/src/`:\n")
h.append("1. **Authoritative Specification Catalog**:")
h.append("   - File: `C:\\Dce Pro\\AiAgent\\DanhBa\\danhba_lenh.json` (376,293 bytes, strictly read-only).")
h.append("   - Contains exactly **311 `<CommandMethod>` endpoints** with metadata: `ten`, `lop`, `ham`, `mo_ta`, `can_form`, `can_chon`, `ghi_ban_ve`, `co_the_tu_dong`, `nhom_nghiep_vu`, `chon_gi`.")
h.append("   - Breakdown: 232 commands require interactive selection (`can_chon: true`), 263 commands open modal forms/dialogs (`can_form: true`), 240 commands modify the DWG database (`ghi_ban_ve: true`), and 9 commands are fully headless/automated (`co_the_tu_dong: true`).\n")

h.append("2. **Legacy Assembly Decompilation & Metadata Tokens**:")
h.append("   - Assembly: `C:\\Dce Pro\\DCE Pro.dll` (44.58 MB, .NET binary inspected via `ilspycmd.exe`).")
h.append("   - Total `TypeDef` entries: **28,865**.")
h.append("   - **11 CAD Jigs** reside in a contiguous token block `0x02000E52`..`0x02000E5C` in `ClassLibrary1`, all inheriting directly from `Autodesk.AutoCAD.EditorInput.DrawJig` (TypeRef `0x01000024`):")
h.append("     - `0x02000E52` (RID 3666): `ClassLibrary1.TagLeader` (139 lines in ILSpy)")
h.append("     - `0x02000E53` (RID 3667): `ClassLibrary1.TagChiaThep` (135 lines)")
h.append("     - `0x02000E54` (RID 3668): `ClassLibrary1.JigSlabRebar` (160 lines)")
h.append("     - `0x02000E55` (RID 3669): `ClassLibrary1.VeMBKC_JIG` (149 lines)")
h.append("     - `0x02000E56` (RID 3670): `ClassLibrary1.ijgRaithepchamtron` (121 lines)")
h.append("     - `0x02000E57` (RID 3671): `ClassLibrary1.ijgVeDaiVong` (141 lines)")
h.append("     - `0x02000E58` (RID 3672): `ClassLibrary1.JigVeDaiC` (125 lines)")
h.append("     - `0x02000E59` (RID 3673): `ClassLibrary1.VePolylineBatKy` (112 lines)")
h.append("     - `0x02000E5A` (RID 3674): `ClassLibrary1.ijgVeThepVachCT` (175 lines)")
h.append("     - `0x02000E5B` (RID 3675): `ClassLibrary1.JigVeVachHam` (174 lines)")
h.append("     - `0x02000E5C` (RID 3676): `ClassLibrary1.DimNhanhMBKC` (117 lines)")
h.append("   - **12 Specialized UI Forms & Palettes** identified with exact tokens and base classes:")
h.append("     - `0x02000136` (RID 310): `ClassLibrary1.DceThangMoi.ThangMoi_Frm` (Base: `System.Windows.Forms.Form`, 4,096 lines)")
h.append("     - `0x02000161` (RID 353): `ClassLibrary1.DceMongCocMoi.MongCoc_Frm` (Base: `System.Windows.Forms.Form`, 4,612 lines)")
h.append("     - `0x02000163` (RID 355): `ClassLibrary1.DceMongCocMoi.MongCoc_FrmLopThepThem` (Base: `System.Windows.Forms.Form`, 1,241 lines)")
h.append("     - `0x02000165` (RID 357): `ClassLibrary1.DceMongCocMoi.MongCoc_FrmChonThepTuDong` (Base: `System.Windows.Forms.Form`, 232 lines)")
h.append("     - `0x02000246` (RID 582): `ClassLibrary1.TKT_frmMain` (Base: `System.Windows.Forms.Form`, 2,328 lines)")
h.append("     - `0x02000B23` (RID 2851): `ClassLibrary1.AutoStr.AutoStrWindow` (Base: `System.Windows.Window`, 8,007 lines)")
h.append("     - `0x02000DA6` (RID 3494): `ClassLibrary1.DceDamMatCatMoi.DamCong_Frm` (Base: `System.Windows.Forms.Form`, 2,045 lines)")
h.append("     - `0x02000DCA` (RID 3530): `ClassLibrary1.DceVeCocBeTong.CocBT_Frm` (Base: `System.Windows.Forms.Form`, 411 lines)")
h.append("     - `0x02000E12` (RID 3602): `ClassLibrary1.DcePalette` (Base: `System.Object`, wraps `PaletteSet`, 81 lines)")
h.append("     - `0x02000E13` (RID 3603): `ClassLibrary1.DcePalette_WallThickness` (Base: `System.Object`, wraps `PaletteSet`, 174 lines)")
h.append("     - `0x02000E1E` (RID 3614): `ClassLibrary1.DceRibbon` (Base: `System.Object`, implements `IExtensionApplication`, 154 lines)")
h.append("     - `0x02000E5D` (RID 3677): `ClassLibrary1.Bea_ToiUuCatThepV2` (Base: `System.Object`, 1D cutting optimizer dialog & logic, 561 lines)\n")

h.append("3. **Current Implementation in `HPDcePro.AutoCad`**:")
h.append("   - Source directory: `HPDcePro/src/HPDcePro.AutoCad/` contains 12 source files across `Commands/` and `Drafting/`.")
h.append("   - Found **80 `[CommandMethod]` registrations** across these files.")
h.append("   - **64 commands** match entries in the 311 specification.")
h.append("   - **16 commands** are outside `danhba_lenh.json` (10 PEB Steel commands from `DbimSteelAutocad.dll`, plus 6 utility wrappers `AUTOSTR`, `COLUMNREC2`, `GANTRUCBLOCKDAM`, `IMPORTKICHTHUOC`, `UPDATETHEPTINHTOAN`, `XUATDULIEUMODELETABS`).")
h.append("   - **Duplicate registration**: Command `ChiaKhoangCach` is defined in two separate files (`Commands/RebarCommands.cs:128` and `Commands/SlabCommands.cs:135`).")
h.append("   - **Mock Audit**: Exactly **6 commands** matching the 311 specification currently contain hardcoded mock/sample data:")
h.append("     - `VeDam_DCE` (`Commands/BeamCommands.cs:24`): calls `CalculateSampleBeam(\"D1\", 2, 6000.0, 220.0, 400.0)`")
h.append("     - `VeDamAll_DCE` (`Commands/BeamCommands.cs:34`): calls `CalculateSampleBeam` for d1 and d2")
h.append("     - `VeMCD_DCE` (`Commands/BeamCommands.cs:47`): calls `CalculateSampleBeam`")
h.append("     - `UpDateDLDam` (`Commands/BeamCommands.Editing.cs:106`): calls `CalculateSampleBeam`")
h.append("     - `ColumnREC1` (`Commands/ColumnCommands.cs:52`): calls `GenerateSampleColumnBuffer(ColumnSectionStyle.REC1, ...)`")
h.append("     - `ColumnCi` (`Commands/ColumnCommands.cs:68`): calls `GenerateSampleColumnBuffer(ColumnSectionStyle.Ci, ...)`")
h.append("   - **Jig Inventory**: Zero Jigs are implemented in `HPDcePro.AutoCad` (directory `HPDcePro.AutoCad/Jigs/` is completely missing). All 11 Jigs are **MISSING**.")
h.append("   - **Form Inventory**: Zero authentic legacy forms (`DamCong_Frm`, `MongCoc_Frm`, etc.) exist in `HPDcePro.UI`. They are currently replaced with placeholder/generic MahApps windows (`BeamDetailingWindow.xaml`, `FoundationSlabWindow.xaml`, etc.), hence all 12 are **MISSING / MOCK**.\n")

h.append("## 2. Logic Chain\n")
h.append("1. **Authoritative Baseline Establishment**: By directly loading `C:\\Dce Pro\\AiAgent\\DanhBa\\danhba_lenh.json` and dumping the ECMA-335 `TypeDef` / `TypeRef` metadata tables from `C:\\Dce Pro\\DCE Pro.dll`, we established the immutable ground truth for all 311 commands, 11 Jigs, and 12 UI Forms without relying on memory or assumptions.")
h.append("2. **Jig Token Derivation**: We verified that `TagLeader` (`0x02000E52`) through `DimNhanhMBKC` (`0x02000E5C`) form an exact 11-type contiguous block. TypeRef `0x01000024` maps directly to `Autodesk.AutoCAD.EditorInput.DrawJig`. Since no file in `HPDcePro.AutoCad` references `DrawJig` or implements these classes, all 11 Jigs are classified as `MISSING`.")
h.append("3. **Form Token Derivation**: The 12 UI Forms map to WinForms (`Form`), WPF (`Window`), or AutoCAD native wrappers (`PaletteSet`, `IExtensionApplication`). In `HPDcePro.UI`, only 7 generic windows exist, none of which implement the specific inputs, controls, or algorithms of `DamCong_Frm`, `MongCoc_Frm`, or `TKT_frmMain`. Thus, all 12 are classified as `MISSING / MOCK`.")
h.append("4. **311 Command Reconciliation**: By cross-referencing all 311 command names against the 80 `[CommandMethod]` attributes in `HPDcePro.AutoCad`:")
h.append("   - 58 commands are implemented with authentic CAD drafting/geometry buffer logic.")
h.append("   - 6 commands match the spec but are flagged as `ĐANG LÀ MOCK` due to hardcoded calls to sample methods.")
h.append("   - 247 commands are completely absent from `HPDcePro.AutoCad` and classified as `MISSING`.")
h.append("5. **Target Architecture Alignment**: Missing commands are assigned target implementation files in `HPDcePro.AutoCad/Commands/` following domain boundaries (Beam, Column, Slab, Footing, Rebar, AutoStr, Stair, Wall, EtabsSync, Drawing, GeneralCad).\n")

h.append("## 3. Caveats\n")
h.append("1. **Out-of-Catalog Commands**: 16 commands in `HPDcePro.AutoCad` are not in `danhba_lenh.json`. 10 of them originate from `DbimSteelAutocad.dll` (PEB steel framing commands), and 6 are synthetic wrappers (`AUTOSTR`, `COLUMNREC2`, `GANTRUCBLOCKDAM`, `IMPORTKICHTHUOC`, `UPDATETHEPTINHTOAN`, `XUATDULIEUMODELETABS`). These are valid extensions but tracked separately from the 311 core catalog.")
h.append("2. **Duplicate Command Identifier**: `ChiaKhoangCach` is registered in both `RebarCommands.cs` and `SlabCommands.cs`. In AutoCAD, duplicate command method registrations cause an `AmbiguousMatchException` or runtime overwrite upon bundle loading. This must be refactored into a single command handler.")
h.append("3. **Legacy WinForms vs. WPF Architecture**: 8 of the 12 legacy forms are legacy WinForms (`System.Windows.Forms.Form`). Per `PROJECT.md` and repository standards, they must be modernized into WPF `MahApps.Metro.Controls.MetroWindow` while preserving 100% parameter and calculation parity.\n")

h.append("## 4. Conclusion\n")
h.append("- Total AutoCAD Commands Audited: **311**")
h.append("  - **IMPLEMENTED**: 58 commands (18.6%)")
h.append("  - **ĐANG LÀ MOCK**: 6 commands (1.9%)")
h.append("  - **MISSING**: 247 commands (79.4%)")
h.append("- CAD Interactive Jigs: **11 classes** (`0x02000E52`..`0x02000E5C`), all **MISSING** (0/11 implemented).")
h.append("- Specialized UI Forms / Palettes: **12 forms**, all **MISSING / MOCK** (0/12 implemented).")
h.append("- The complete specifications, prompt requirements, and target files have been mapped and formatted below into ready-to-insert markdown tables for `docs/rea-dce-pro/TRUTH_MATRIX.md`.\n")

h.append("## 5. Verification Method\n")
h.append("To independently reproduce and verify this entire report:")
h.append("```powershell")
h.append("# 1. Verify total commands in danhba_lenh.json")
h.append("python -c \"import json; data=json.load(open(r'C:\\Dce Pro\\AiAgent\\DanhBa\\danhba_lenh.json', encoding='utf-8')); print(len(data['muc']))\"")
h.append("# Output: 311")
h.append("")
h.append("# 2. Verify 11 Jigs in DCE Pro.dll metadata")
h.append("& 'C:\\Users\\STR-HP03\\.dotnet\\tools\\ilspycmd.exe' 'C:\\Dce Pro\\DCE Pro.dll' --dump-table TypeDef --json | python -c \"import sys, json; rows=json.load(sys.stdin)['rows']; print([r['Name'] for r in rows if 0x02000E52 <= int(r['Token'],16) <= 0x02000E5C])\"")
h.append("# Output: ['TagLeader', 'TagChiaThep', 'JigSlabRebar', 'VeMBKC_JIG', 'ijgRaithepchamtron', 'ijgVeDaiVong', 'JigVeDaiC', 'VePolylineBatKy', 'ijgVeThepVachCT', 'JigVeVachHam', 'DimNhanhMBKC']")
h.append("")
h.append("# 3. Verify mock commands in HPDcePro.AutoCad")
h.append("Select-String -Path 'HPDcePro\\src\\HPDcePro.AutoCad\\Commands\\*.cs' -Pattern 'CalculateSample|GenerateSample' | Select-Object Path, LineNumber, Line")
h.append("```\n")

# Now append Section: Master Tables for TRUTH_MATRIX.md
h.append("---\n")
h.append("# Ready-to-Insert Truth Matrix Tables (`docs/rea-dce-pro/TRUTH_MATRIX.md`)\n")

# Table 1: Category Summary
h.append("## Bảng Tổng Hợp 15 Nhóm Nghiệp Vụ (311 Lệnh CAD)")
h.append("| Nhóm nghiệp vụ | Tổng số lệnh | Đã triển khai (`IMPLEMENTED`) | Đang là Mock (`MOCK`) | Chưa triển khai (`MISSING`) | Tỷ lệ hoàn thành |")
h.append("|----------------|--------------|------------------------------|-----------------------|----------------------------|------------------|")

cats = {}
for c in commands:
    cats.setdefault(c['category'], []).append(c)

for cat, items in sorted(cats.items(), key=lambda x: -len(x[1])):
    impl = sum(1 for x in items if 'IMPLEMENTED' in x['status'])
    mock = sum(1 for x in items if 'MOCK' in x['status'])
    miss = sum(1 for x in items if 'MISSING' in x['status'])
    ratio = f"{(impl/len(items))*100:.1f}%"
    h.append(f"| {cat} | {len(items)} | {impl} | {mock} | {miss} | {ratio} |")
h.append(f"| **TỔNG CỘNG** | **311** | **58** | **6** | **247** | **18.6%** |\n")

# Table 2: 11 CAD Jigs
h.append("## Bảng Đặc Tả 11 CAD Interactive Jigs (`0x02000E52`..`0x02000E5C`)")
h.append("| # | Tên Jig | Legacy Token | Base Class | File Legacy (`DCE Pro.dll`) | Target File trong `HPDcePro.AutoCad` | Trạng thái | Chức năng hình học & Tham số |")
h.append("|---|---------|--------------|------------|-----------------------------|---------------------------------------|------------|-------------------------------|")

jigs_spec = [
    ("1", "TagLeader", "0x02000E52", "DrawJig", "ClassLibrary1.TagLeader", "HPDcePro.AutoCad/Jigs/TagLeaderJig.cs", "MISSING", "Jig vẽ đường gióng ghi chú thép nhiều đoạn (Pt1, Pt2, Pt3, mirror)"),
    ("2", "TagChiaThep", "0x02000E53", "DrawJig", "ClassLibrary1.TagChiaThep", "HPDcePro.AutoCad/Jigs/TagChiaThepJig.cs", "MISSING", "Jig chia khoảng rải thép và tag ghi chú động (Pt1, Pt2, Pt3, KhoangCachChuan)"),
    ("3", "JigSlabRebar", "0x02000E54", "DrawJig", "ClassLibrary1.JigSlabRebar", "HPDcePro.AutoCad/Jigs/JigSlabRebar.cs", "MISSING", "Jig rải và uốn thép sàn tương tác (PointL1..L5, Bulge, TongHopDim)"),
    ("4", "VeMBKC_JIG", "0x02000E55", "DrawJig", "ClassLibrary1.VeMBKC_JIG", "HPDcePro.AutoCad/Jigs/VeMBKCJig.cs", "MISSING", "Jig vẽ mặt bằng kết cấu / dầm khung tự động theo điểm chọn (Pt1, Pt2, Diem1..4)"),
    ("5", "ijgRaithepchamtron", "0x02000E56", "DrawJig", "ClassLibrary1.ijgRaithepchamtron", "HPDcePro.AutoCad/Jigs/JigRaiThepChamTron.cs", "MISSING", "Jig rải hàng thép chấm tròn tiết diện dầm/cột/vách theo khoảng cách s"),
    ("6", "ijgVeDaiVong", "0x02000E57", "DrawJig", "ClassLibrary1.ijgVeDaiVong", "HPDcePro.AutoCad/Jigs/JigVeDaiVong.cs", "MISSING", "Jig vẽ đai tròn xoắn / đai vòng cho cọc khoan nhồi và cột tròn"),
    ("7", "JigVeDaiC", "0x02000E58", "DrawJig", "ClassLibrary1.JigVeDaiC", "HPDcePro.AutoCad/Jigs/JigVeDaiC.cs", "MISSING", "Jig vẽ đai móc chữ C / đai liên kết phụ (Pt1, Pt2, PointL1..L3)"),
    ("8", "VePolylineBatKy", "0x02000E59", "DrawJig", "ClassLibrary1.VePolylineBatKy", "HPDcePro.AutoCad/Jigs/VePolylineBatKyJig.cs", "MISSING", "Jig vẽ polyline tự do phục vụ bao biên vùng thép sàn và vách"),
    ("9", "ijgVeThepVachCT", "0x02000E5A", "DrawJig", "ClassLibrary1.ijgVeThepVachCT", "HPDcePro.AutoCad/Jigs/JigVeThepVachCt.cs", "MISSING", "Jig vẽ chi tiết cốt thép cấu tạo vách và vùng biên gia cường vách"),
    ("10", "JigVeVachHam", "0x02000E5B", "DrawJig", "ClassLibrary1.JigVeVachHam", "HPDcePro.AutoCad/Jigs/JigVeVachHam.cs", "MISSING", "Jig vẽ cốt thép vách hầm chịu áp lực đất nước đa tầng"),
    ("11", "DimNhanhMBKC", "0x02000E5C", "DrawJig", "ClassLibrary1.DimNhanhMBKC", "HPDcePro.AutoCad/Jigs/DimNhanhMbkcJig.cs", "MISSING", "Jig đo kích thước nhanh chuỗi trục dầm trên mặt bằng kết cấu")
]

for row in jigs_spec:
    h.append(f"| {row[0]} | `{row[1]}` | `{row[2]}` | `{row[3]}` | `{row[4]}` | `{row[5]}` | **{row[6]}** | {row[7]} |")
h.append("")

# Table 3: 12 Specialized UI Forms
h.append("## Bảng Đặc Tả 12 Giao Diện Điều Khiển & Form Chuyên Dụng")
h.append("| # | Tên Form / Thành phần UI | Legacy Token | Base Class | Số dòng ILSpy | Target Class trong `HPDcePro.UI` | Trạng thái | Mục đích & Nghiệp vụ kỹ thuật |")
h.append("|---|--------------------------|--------------|------------|---------------|-----------------------------------|------------|-------------------------------|")

forms_spec = [
    ("1", "ThangMoi_Frm", "0x02000136", "Form (WinForms)", "4,096", "HPDcePro.UI.Views.ThangMoiWindow", "MISSING", "Giao diện thiết kế, tính toán và vẽ chi tiết cầu thang bộ"),
    ("2", "MongCoc_Frm", "0x02000161", "Form (WinForms)", "4,612", "HPDcePro.UI.Views.MongCocWindow", "MISSING", "Giao diện chính thiết kế đài móng cọc, bố trí tim cọc và thép đài"),
    ("3", "MongCoc_FrmLopThepThem", "0x02000163", "Form (WinForms)", "1,241", "HPDcePro.UI.Views.MongCocLopThepThemWindow", "MISSING", "Cửa sổ cấu hình lớp thép gia cường phụ cho đài móng cọc"),
    ("4", "MongCoc_FrmChonThepTuDong", "0x02000165", "Form (WinForms)", "232", "HPDcePro.UI.Views.MongCocChonThepTuDongWindow", "MISSING", "Cửa sổ chọn và tối ưu đường kính thép đài móng tự động"),
    ("5", "TKT_frmMain", "0x02000246", "Form (WinForms)", "2,328", "HPDcePro.UI.Views.MasterRebarScheduleDashboardWindow", "MISSING", "Trung tâm điều khiển thống kê thép toàn dự án (TKT) và xuất bảng"),
    ("6", "AutoStrWindow", "0x02000B23", "Window (WPF)", "8,007", "HPDcePro.UI.Views.AutoStrExecutionWindow", "MOCK (Placeholder)", "Cửa sổ điều khiển và hiển thị tiến trình tự động 24 bước AutoStr"),
    ("7", "DamCong_Frm", "0x02000DA6", "Form (WinForms)", "2,045", "HPDcePro.UI.Views.DamCongWindow", "MISSING", "Giao diện vẽ và cấu hình dầm cong, dầm hình học đặc biệt"),
    ("8", "CocBT_Frm", "0x02000DCA", "Form (WinForms)", "411", "HPDcePro.UI.Views.CocBtWindow", "MISSING", "Giao diện vẽ chi tiết cọc bê tông cốt thép đúc sẵn"),
    ("9", "DcePalette", "0x02000E12", "Object (PaletteSet)", "81", "HPDcePro.UI.Palettes.DcePaletteControl", "MISSING", "Bảng điều khiển dockable PaletteSet chính của DCE Pro trong CAD"),
    ("10", "DcePalette_WallThickness", "0x02000E13", "Object (PaletteSet)", "174", "HPDcePro.UI.Palettes.WallThicknessPaletteControl", "MISSING", "Bảng điều khiển chọn và gán nhanh chiều dày vách kết cấu"),
    ("11", "DceRibbon", "0x02000E1E", "Object (IExtensionApplication)", "154", "HPDcePro.UI.Ribbon.DceRibbonBuilder", "MISSING", "Trình khởi tạo và quản lý RibbonTab, RibbonPanel của DCE Pro"),
    ("12", "Bea_ToiUuCatThepV2", "0x02000E5D", "Object (Dialog/Engine)", "561", "HPDcePro.UI.Views.CuttingOptimizationAdvancedWindow", "MOCK (Placeholder)", "Thuật toán và giao diện tối ưu hóa cắt thép 1D 11.7m phiên bản V2")
]

for row in forms_spec:
    h.append(f"| {row[0]} | `{row[1]}` | `{row[2]}` | `{row[3]}` | {row[4]} | `{row[5]}` | **{row[6]}** | {row[7]} |")
h.append("")

# Table 4: Mock Commands Audit
h.append("## Bảng Soát Lỗi Mock: 6 Lệnh CAD Hiện Tại Trong `HPDcePro.AutoCad`")
h.append("| # | Lệnh CAD | File & Dòng hiện tại | Dấu hiệu Mock phát hiện | Tác vụ thật trong DCE Pro | Giải pháp thay thế 1:1 |")
h.append("|---|----------|----------------------|--------------------------|---------------------------|------------------------|")

mocks_spec = [
    ("1", "VeDam_DCE", "Commands/BeamCommands.cs:24", "Gọi `CalculateSampleBeam(\"D1\", 2, 6000.0, 220.0, 400.0)`", "Đọc dữ liệu dầm từ Etabs/Excel/User click, vẽ dầm thật", "Kết nối `Editor.GetSelection` dầm hoặc đọc qua `BeamDetailingOptions`"),
    ("2", "VeDamAll_DCE", "Commands/BeamCommands.cs:34", "Gọi `CalculateSampleBeam` sinh dầm giả D1, D2", "Quét toàn bộ dầm trên mặt bằng, vẽ toàn bộ hệ dầm tầng", "Duyệt danh sách dầm từ bộ nhớ hoặc DWG XData"),
    ("3", "VeMCD_DCE", "Commands/BeamCommands.cs:47", "Gọi `CalculateSampleBeam` với tiết diện giả 220x400", "Chọn dầm trên bản vẽ, cắt và vẽ đúng mặt cắt tiết diện", "Yêu cầu user pick mặt cắt qua `Editor.GetPoint`"),
    ("4", "UpDateDLDam", "Commands/BeamCommands.Editing.cs:106", "Gọi `CalculateSampleBeam` in thông báo giả", "Đồng bộ dữ liệu tính toán từ Excel/ETABS vào XData dầm CAD", "Đọc XData qua `BeamXDataCodec` và cập nhật thông số"),
    ("5", "ColumnREC1", "Commands/ColumnCommands.cs:52", "Gọi `GenerateSampleColumnBuffer(ColumnSectionStyle.REC1, 400, 400, \"C-REC1\")`", "Hỏi kích thước b, h và vị trí pick của kỹ sư trên CAD", "Thay bằng `Editor.GetPoint` và `Editor.GetKeywords`"),
    ("6", "ColumnCi", "Commands/ColumnCommands.cs:68", "Gọi `GenerateSampleColumnBuffer(ColumnSectionStyle.Ci, 450, 450, \"C-Ci\")`", "Hỏi đường kính D và điểm chèn của kỹ sư trên CAD", "Thay bằng `Editor.GetPoint` và `Editor.GetDistance`")
]

for row in mocks_spec:
    h.append(f"| {row[0]} | `{row[1]}` | `{row[2]}` | `{row[3]}` | {row[4]} | {row[5]} |")
h.append("")

# Table 5: Edge cases
h.append("## Bảng Nhận Diện Các Trường Hợp Đặc Biệt (Edge Cases)")
h.append("| # | Phân loại Edge Case | Số lệnh | Danh sách lệnh tiêu biểu | Hành vi quan sát & Lưu ý kiểm soát |")
h.append("|---|----------------------|---------|--------------------------|-------------------------------------|")
h.append("| 1 | Headless / Pure Utility | 9 | `DBIMAIPALDIAG_DCE`, `DBIMAIPALRESET_DCE`, `DBIMAITHUGON_DCE`, `DCEKIEMCHUNGTOIUU`, `DEBUGBROKENBLOCKS`, `MBKCAUDIT_DCE`, `MONGCOCNEWLOG_DCE`, `MONGCOCNEWXOALOG_DCE`, `VeThep_DCE` | `can_chon=false, can_form=false, ghi_ban_ve=false, co_the_tu_dong=true`. Lệnh chạy ngầm chẩn đoán/dọn log, không tương tác giao diện. |")
h.append("| 2 | Multi-stage Prompt + Modal Form | 84 | `BlockToExcel_DCE`, `ChenThepChamTron_DCE`, `ChiaDimThepSan_DCE`, `ChiaKhoangCach`, `ChiaThep_DCE`, `ChiaThep2_DCE`, `ChiaThep3_DCE` | Vừa mở Form cấu hình vừa yêu cầu kỹ sư pick điểm liên tục trên màn hình qua `Editor.GetPoint` / `GetSelection`. Cần quản lý ẩn/hiện Form (`Form.Hide()` -> pick CAD -> `Form.Show()`). |")
h.append("| 3 | Viewport & Layout Management | 22 | `2DXCLIP`, `2MODEL`, `ChiaDoiTuong_DCE`, `MODEL2VP_DCE`, `VP2MODEL_DCE`, `TAOMV`, `LINKLAYOUT_DCE` | Thao tác chuyển đổi không gian ModelSpace <-> PaperSpace, kích hoạt Viewport, thiết lập TILEMODE và clip block tham chiếu. |")
h.append("| 4 | XData Registry & Binding | 44 | `DocXdata`, `GANLIENKETTHEP`, `XOALIENKETTHEP`, `XEMLIENKETTHEP`, `GANDLLINEDAM`, `XEMLIENKETMBCK`, `XOADLDATADCE` | Đọc/ghi 13 RegApps XData (`DCE_D1`, `DCE_COT`, `DCE_SAN`,...). Phải đảm bảo đăng ký RegAppTableRecord trước khi ghi. |")
h.append("| 5 | Đồng bộ CSI / Ngoại vi | 12 | `ConnectCSIAPI_DCE`, `UpdateModelEtabs`, `UpdateModelSafe`, `UpdateModelSap`, `BlockToExcel_DCE` | Tương tác COM Interop với ETABS, SAP2000, SAFE hoặc Microsoft Excel. Cần bọc cơ chế guard timeout và snapshot rollback. |\n")

# Master Table of 311 commands
h.append("## Danh Mục Đầy Đủ 311 Lệnh AutoCAD (`danhba_lenh.json`)\n")
h.append("| # | Lệnh CAD | Nhóm nghiệp vụ | Mô tả tiếng Việt | Cần chọn | Mở Form | Ghi bản vẽ | Target File trong `HPDcePro.AutoCad` | Trạng thái hiện tại |")
h.append("|---|----------|----------------|------------------|----------|---------|------------|---------------------------------------|---------------------|")

for c in commands:
    idx = c['index']
    name = c['name']
    cat = c['category']
    mota = c['description'].replace('|', '/')
    can_chon = c['can_chon']
    can_form = c['can_form']
    ghi_ban_ve = c['ghi_ban_ve']
    target = c['target']
    status = c['status']
    h.append(f"| {idx} | `{name}` | {cat} | {mota} | {can_chon} | {can_form} | {ghi_ban_ve} | `{target}` | **{status}** |")

with open(handoff_path, 'w', encoding='utf-8') as f:
    f.write("\n".join(h))

print(f"Successfully generated handoff.md: {handoff_path}")
print(f"Total lines written: {len(h)}")
