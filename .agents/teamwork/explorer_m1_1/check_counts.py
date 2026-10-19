import json
import os
import struct
import sys

sys.stdout.reconfigure(encoding='utf-8')

BASE_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"
DCE_DIR = r"C:\Dce Pro"
DANHBA_DIR = os.path.join(DCE_DIR, "AiAgent", "DanhBa")
HPDCEPRO_DIR = os.path.join(BASE_DIR, "HPDcePro")

# 1. Native C++ Exports
def get_pe_exports(dll_path):
    with open(dll_path, 'rb') as f:
        data = f.read()
    if data[:2] != b'MZ':
        return []
    e_lfanew = struct.unpack('<I', data[0x3C:0x40])[0]
    opt_offset = e_lfanew + 24
    export_rva = struct.unpack('<I', data[opt_offset+112:opt_offset+116])[0]
    num_sections = struct.unpack('<H', data[e_lfanew+6:e_lfanew+8])[0]
    size_opt = struct.unpack('<H', data[e_lfanew+20:e_lfanew+22])[0]
    sec_offset = opt_offset + size_opt
    sections = []
    for i in range(num_sections):
        s_off = sec_offset + i*40
        s_name = data[s_off:s_off+8].rstrip(b'\x00').decode('latin1')
        vsize = struct.unpack('<I', data[s_off+8:s_off+12])[0]
        vrva = struct.unpack('<I', data[s_off+12:s_off+16])[0]
        rsize = struct.unpack('<I', data[s_off+16:s_off+20])[0]
        roffset = struct.unpack('<I', data[s_off+20:s_off+24])[0]
        sections.append({'name': s_name, 'vrva': vrva, 'vsize': vsize, 'roffset': roffset, 'rsize': rsize})
    def rva_to_off(rva):
        for s in sections:
            if s['vrva'] <= rva < s['vrva'] + max(s['vsize'], s['rsize']):
                return s['roffset'] + (rva - s['vrva'])
        return None
    exp_off = rva_to_off(export_rva)
    if not exp_off:
        return []
    num_names = struct.unpack('<I', data[exp_off+0x18:exp_off+0x1C])[0]
    addr_funcs_rva = struct.unpack('<I', data[exp_off+0x1C:exp_off+0x20])[0]
    addr_names_rva = struct.unpack('<I', data[exp_off+0x20:exp_off+0x24])[0]
    addr_ord_rva = struct.unpack('<I', data[exp_off+0x24:exp_off+0x28])[0]
    names_off = rva_to_off(addr_names_rva)
    funcs_off = rva_to_off(addr_funcs_rva)
    ord_off = rva_to_off(addr_ord_rva)
    exports = []
    for i in range(num_names):
        name_rva = struct.unpack('<I', data[names_off + i*4 : names_off + (i+1)*4])[0]
        n_off = rva_to_off(name_rva)
        fn_name = data[n_off:data.find(b'\x00', n_off)].decode('ascii')
        ord_idx = struct.unpack('<H', data[ord_off + i*2 : ord_off + (i+1)*2])[0]
        fn_rva = struct.unpack('<I', data[funcs_off + ord_idx*4 : funcs_off + (ord_idx+1)*4])[0]
        exports.append((i, fn_name, f"0x{fn_rva:04x}"))
    return exports

native_exports = get_pe_exports(os.path.join(DCE_DIR, "DceAnalyssiss64.dll"))

# 2. Catalogs
with open(os.path.join(DANHBA_DIR, "danhba_lenh.json"), 'r', encoding='utf-8') as f:
    cad_commands = json.load(f).get('muc', [])

with open(os.path.join(DANHBA_DIR, "danhba_dce_excel.json"), 'r', encoding='utf-8') as f:
    excel_actions = json.load(f).get('muc', [])

with open(os.path.join(DANHBA_DIR, "danhba_dce_thep.json"), 'r', encoding='utf-8') as f:
    thep_facade = json.load(f).get('muc', [])

with open(os.path.join(DANHBA_DIR, "danhba_mbkc.json"), 'r', encoding='utf-8') as f:
    mbkc_facade = json.load(f).get('muc', [])

# 3. Known CAD commands in HPDcePro.AutoCad
existing_cad_commands = {
    # Beam
    "ShopDrawingBeam": "src/HPDcePro.AutoCad/Commands/BeamCommands.cs",
    "VeDam_DCE": "src/HPDcePro.AutoCad/Commands/BeamCommands.cs",
    "VeDamAll_DCE": "src/HPDcePro.AutoCad/Commands/BeamCommands.cs",
    "VeMCD_DCE": "src/HPDcePro.AutoCad/Commands/BeamCommands.cs",
    "VeBlockDam2": "src/HPDcePro.AutoCad/Commands/BeamCommands.cs",
    "VeBlockCaoDoMB": "src/HPDcePro.AutoCad/Commands/BeamCommands.cs",
    "VeBlockTrucDam": "src/HPDcePro.AutoCad/Commands/BeamCommands.cs",
    "VeBlockChieuDaySan": "src/HPDcePro.AutoCad/Commands/BeamCommands.cs",
    "VeBlockGiatCaoDoSan": "src/HPDcePro.AutoCad/Commands/BeamCommands.cs",
    "VeBlockCaoDoMC": "src/HPDcePro.AutoCad/Commands/BeamCommands.cs",
    "VeBlockSection": "src/HPDcePro.AutoCad/Commands/BeamCommands.cs",
    "ImportKichThuoc": "src/HPDcePro.AutoCad/Commands/BeamCommands.cs",
    "XemLienKetTenCauKien": "src/HPDcePro.AutoCad/Commands/BeamCommands.cs",
    "BlockBeam_Size": "src/HPDcePro.AutoCad/Commands/BeamCommands.Editing.cs",
    "BlockBeam_Name": "src/HPDcePro.AutoCad/Commands/BeamCommands.Editing.cs",
    "BlockBeam_NameSize": "src/HPDcePro.AutoCad/Commands/BeamCommands.Editing.cs",
    "BlockBeam_Hide": "src/HPDcePro.AutoCad/Commands/BeamCommands.Editing.cs",
    "BlockBeam_Width": "src/HPDcePro.AutoCad/Commands/BeamCommands.Editing.cs",
    "DatTenCauKienDam": "src/HPDcePro.AutoCad/Commands/BeamCommands.Editing.cs",
    "GanTrucBlockDam": "src/HPDcePro.AutoCad/Commands/BeamCommands.Editing.cs",
    "UpDateDLDam": "src/HPDcePro.AutoCad/Commands/BeamCommands.Editing.cs",
    "XemLienKetThep": "src/HPDcePro.AutoCad/Commands/BeamCommands.Editing.cs",
    "DatTenLineDam2": "src/HPDcePro.AutoCad/Commands/BeamCommands.Editing.cs",
    "GanLienKetThep": "src/HPDcePro.AutoCad/Commands/BeamCommands.Editing.cs",
    "XoaLienKetThep": "src/HPDcePro.AutoCad/Commands/BeamCommands.Editing.cs",
    "GanDLLineDam": "src/HPDcePro.AutoCad/Commands/BeamCommands.Editing.cs",
    "XoaDLDataDCE": "src/HPDcePro.AutoCad/Commands/BeamCommands.Editing.cs",
    "XemLienKetMBCK": "src/HPDcePro.AutoCad/Commands/BeamCommands.Editing.cs",
    "XuatDuLieuDam": "src/HPDcePro.AutoCad/Commands/BeamCommands.Editing.cs",
    # Column
    "VeCotDcePro": "src/HPDcePro.AutoCad/Commands/ColumnCommands.cs",
    "VeVachDcePro": "src/HPDcePro.AutoCad/Commands/ColumnCommands.cs",
    "ColumnREC1": "src/HPDcePro.AutoCad/Commands/ColumnCommands.cs",
    "ColumnREC2": "src/HPDcePro.AutoCad/Commands/ColumnCommands.cs",
    "ColumnCi": "src/HPDcePro.AutoCad/Commands/ColumnCommands.cs",
    "ImportKichThuocCot": "src/HPDcePro.AutoCad/Commands/ColumnCommands.cs",
    "ImportHinhDangCot": "src/HPDcePro.AutoCad/Commands/ColumnCommands.cs",
    "UpdateThepTinhToan": "src/HPDcePro.AutoCad/Commands/ColumnCommands.cs",
    # Footing
    "VeBlockCoc": "src/HPDcePro.AutoCad/Commands/FootingCommands.cs",
    "DanhTenCoc": "src/HPDcePro.AutoCad/Commands/FootingCommands.cs",
    "XuatToaDoCoc": "src/HPDcePro.AutoCad/Commands/FootingCommands.cs",
    "StripMong": "src/HPDcePro.AutoCad/Commands/FootingCommands.cs",
    "TextDaiMong": "src/HPDcePro.AutoCad/Commands/FootingCommands.cs",
    "VeMongCoc_DCE": "src/HPDcePro.AutoCad/Commands/FootingCommands.cs",
    "VeMongDon_DCE": "src/HPDcePro.AutoCad/Commands/FootingCommands.cs",
    "TMCoc": "src/HPDcePro.AutoCad/Commands/FootingCommands.cs",
    "RaiDaiCoc": "src/HPDcePro.AutoCad/Commands/FootingCommands.cs",
    "VeBlockLanhTo": "src/HPDcePro.AutoCad/Commands/FootingCommands.cs",
    "VeMatCatLanhTo": "src/HPDcePro.AutoCad/Commands/FootingCommands.cs",
    "VeThang_DCE": "src/HPDcePro.AutoCad/Commands/FootingCommands.cs",
    # Rebar
    "ToiUuCatThep": "src/HPDcePro.AutoCad/Commands/RebarCommands.cs",
    "TKTSSOLUONGCAUKIEN": "src/HPDcePro.AutoCad/Commands/RebarCommands.cs",
    "TKTSDUONGKINHTHEP": "src/HPDcePro.AutoCad/Commands/RebarCommands.cs",
    "tkt_dce": "src/HPDcePro.AutoCad/Commands/RebarCommands.cs",
    "stk_dce": "src/HPDcePro.AutoCad/Commands/RebarCommands.cs",
    "TongHopThongKe_dce": "src/HPDcePro.AutoCad/Commands/RebarCommands.cs",
    "MaTTThep": "src/HPDcePro.AutoCad/Commands/RebarCommands.cs",
    "QuetThongKe_dce": "src/HPDcePro.AutoCad/Commands/RebarCommands.cs",
    "ChiaKhoangCach": "src/HPDcePro.AutoCad/Commands/RebarCommands.cs",
    "SuaCKThep": "src/HPDcePro.AutoCad/Commands/RebarCommands.cs",
    # Slab
    "VeThepSanTopX": "src/HPDcePro.AutoCad/Commands/SlabCommands.cs",
    "VeThepSanTopY": "src/HPDcePro.AutoCad/Commands/SlabCommands.cs",
    "SoHieuThepSan": "src/HPDcePro.AutoCad/Commands/SlabCommands.cs",
    "VeThepSanBotX": "src/HPDcePro.AutoCad/Commands/SlabCommands.cs",
    "VeThepSanBotY": "src/HPDcePro.AutoCad/Commands/SlabCommands.cs",
    "ThongKeThepSan": "src/HPDcePro.AutoCad/Commands/SlabCommands.cs",
    # AutoStr / Etabs / UI
    "AutoStr": "src/HPDcePro.AutoCad/Commands/AutoStrCommands.cs",
    "XuatDuLieuModelEtabs": "src/HPDcePro.AutoCad/Commands/AutoStrCommands.cs",
    "UpdateModelEtabs": "src/HPDcePro.AutoCad/Commands/AutoStrCommands.cs",
    "taomv": "src/HPDcePro.AutoCad/Commands/AutoStrCommands.cs",
    "GETFORM": "src/HPDcePro.AutoCad/Commands/AutoStrCommands.cs",
    # Steel
    "DceSteelFrame": "src/HPDcePro.AutoCad/Commands/SteelCadCommands.cs",
    "DceSteelTruss": "src/HPDcePro.AutoCad/Commands/SteelCadCommands.cs",
    "DceSteelHaunch": "src/HPDcePro.AutoCad/Commands/SteelCadCommands.cs",
    "DceSteelBasePlate": "src/HPDcePro.AutoCad/Commands/SteelCadCommands.cs",
    "DceSteelPurlin": "src/HPDcePro.AutoCad/Commands/SteelCadCommands.cs",
    "DceSteelBrace": "src/HPDcePro.AutoCad/Commands/SteelCadCommands.cs",
    "DceSteelCrane": "src/HPDcePro.AutoCad/Commands/SteelCadCommands.cs",
    "DceSteelGirt": "src/HPDcePro.AutoCad/Commands/SteelCadCommands.cs",
    "DceSteelApex": "src/HPDcePro.AutoCad/Commands/SteelCadCommands.cs",
    "DceSteelEave": "src/HPDcePro.AutoCad/Commands/SteelCadCommands.cs"
}

# 4. Revit Commands
revit_commands = [
    ("DBimRevit.AdskApplication", "0x0200000A", "IExternalApplication - Creates DBIM Revit Ribbon & PushButtons", "src/HPDcePro.Revit/DceProRevitApplication.cs", "HOÀN THÀNH"),
    ("DBimRevit.CommandLogin", "0x02000011", "Cloud Account Login", "src/HPDcePro.Revit/Commands/CommandLogin.cs", "CHƯA GIẢI MÃ ĐƯỢC"),
    ("DBimRevit.CommandLogout", "0x02000012", "Cloud Account Logout", "src/HPDcePro.Revit/Commands/CommandLogout.cs", "CHƯA GIẢI MÃ ĐƯỢC"),
    ("DBimRevit.CommandAddBeam", "0x0200001B", "Generate Structural Beams from CAD framing plan coordinates", "src/HPDcePro.Revit/Commands/CommandAddBeam.cs", "CHƯA GIẢI MÃ ĐƯỢC"),
    ("DBimRevit.CommandAddColumn", "0x0200001F", "Generate Structural Columns from CAD framing plan coordinates", "src/HPDcePro.Revit/Commands/CommandAddColumn.cs", "CHƯA GIẢI MÃ ĐƯỢC"),
    ("DBimRevit.CommandAddGird", "0x02000021", "Generate Structural Grid Lines from CAD grid lines", "src/HPDcePro.Revit/Commands/CommandAddGrid.cs", "CHƯA GIẢI MÃ ĐƯỢC"),
    ("DBimRevit.CommandAddSlab", "0x02000022", "Generate Structural Floor Slabs from CAD boundary polylines", "src/HPDcePro.Revit/Commands/CommandAddSlab.cs", "CHƯA GIẢI MÃ ĐƯỢC"),
    ("DBimRevit.CommandAddWall", "0x02000025", "Generate Structural Walls / Cores from CAD wall polylines", "src/HPDcePro.Revit/Commands/CommandAddWall.cs", "CHƯA GIẢI MÃ ĐƯỢC"),
    ("DBimRevit.CommandAutoCut", "0x02000028", "Auto-Cut intersecting structural framing & wall elements", "src/HPDcePro.Revit/Commands/CommandAutoCut.cs", "CHƯA GIẢI MÃ ĐƯỢC"),
    ("DBimRevit.CommandAutoJoin", "0x0200002A", "Auto-Join framing, columns, slabs, and walls", "src/HPDcePro.Revit/Commands/CommandAutoJoin.cs", "CHƯA GIẢI MÃ ĐƯỢC"),
    ("DBimRevit.CommandBatchUnjoin", "0x0200002C", "Batch Unjoin structural elements", "src/HPDcePro.Revit/Commands/CommandBatchUnjoin.cs", "CHƯA GIẢI MÃ ĐƯỢC"),
    ("DBimRevit.CommandBeamDisallowJoin", "0x0200002E", "Toggle Disallow Join at beam end handles", "src/HPDcePro.Revit/Commands/CommandBeamDisallowJoin.cs", "CHƯA GIẢI MÃ ĐƯỢC"),
    ("DBimRevit.CommandCreateCope", "0x02000031", "Apply structural coping between beams", "src/HPDcePro.Revit/Commands/CommandCreateCope.cs", "CHƯA GIẢI MÃ ĐƯỢC"),
    ("DBimRevit.CommandJoinByOrder", "0x02000034", "Reorder Join precedence (Column > Wall > Beam > Slab)", "src/HPDcePro.Revit/Commands/CommandJoinByOrder.cs", "CHƯA GIẢI MÃ ĐƯỢC"),
    ("DBimRevit.DceBeamBreaks", "0x0200003A", "Split continuous beams at column/support intersections", "src/HPDcePro.Revit/Commands/DceBeamBreaks.cs", "CHƯA GIẢI MÃ ĐƯỢC"),
    ("DBimRevit.CommandDatTenDam", "0x0200004A", "Auto-Name beams sequentially along grid lines", "src/HPDcePro.Revit/Commands/CommandDatTenDam.cs", "CHƯA GIẢI MÃ ĐƯỢC"),
    ("DBimRevit.CommandDceSetup", "0x0200004D", "Open DCE Revit Configuration & Family Mapping dialog", "src/HPDcePro.Revit/Commands/CommandDceSetup.cs", "CHƯA GIẢI MÃ ĐƯỢC"),
    ("DBimRevit.CommandExportBeamToExcel", "0x0200004F", "Export beam geometry and parameters to Excel calculation workbook", "src/HPDcePro.Revit/ExportFramingCommand.cs", "ĐANG LÀ MOCK"),
    ("DBimRevit.CommandExportToAutocad", "0x02000051", "Export Revit framing plan view to DWG with DCE XData", "src/HPDcePro.Revit/Commands/CommandExportToAutocad.cs", "CHƯA GIẢI MÃ ĐƯỢC"),
    ("DBimRevit.CommandCreateAnalytical", "0x02000053", "Generate Analytical Model (Member/Panel) from physical elements", "src/HPDcePro.Revit/Commands/CommandCreateAnalytical.cs", "CHƯA GIẢI MÃ ĐƯỢC"),
    ("DBimRevit.CommandEtabsToRevit", "0x0200005B", "Import geometry and member sections from ETABS into Revit", "src/HPDcePro.Revit/Commands/CommandEtabsToRevit.cs", "CHƯA GIẢI MÃ ĐƯỢC"),
    ("DBimRevit.CommandExportToEtabs", "0x0200005D", "Export Revit framing model to ETABS (.sdb / OAPI)", "src/HPDcePro.Revit/Commands/CommandExportToEtabs.cs", "CHƯA GIẢI MÃ ĐƯỢC"),
    ("DBimRevit.CommandXuatKhoiLuong", "0x02000064", "Extract Bill of Quantities (concrete volume, formwork, rebar)", "src/HPDcePro.Revit/Commands/CommandXuatKhoiLuong.cs", "CHƯA GIẢI MÃ ĐƯỢC"),
    ("DBimRevit.CommandAddRebarBeam", "0x02000072", "Automated 3D rebar generation for continuous host beams", "src/HPDcePro.Revit/RevitRebarPlacementService.cs", "ĐANG LÀ MOCK")
]

# 5. CAD Jigs (11)
cad_jigs = [
    ("ClassLibrary1.VeMBKC_JIG", "0x02000E55", "DrawJig: Interactive framing beam layout preview", "src/HPDcePro.AutoCad/Jigs/VeMBKCJig.cs", "ĐANG LÀ MOCK"),
    ("ClassLibrary1.JigSlabRebar", "0x02000E54", "DrawJig: Dynamic slab rebar boundary and span preview", "src/HPDcePro.AutoCad/Jigs/JigSlabRebar.cs", "ĐANG LÀ MOCK"),
    ("ClassLibrary1.TagLeader", "0x02000E52", "DrawJig: Interactive leader line and callout text placement", "src/HPDcePro.AutoCad/Jigs/TagLeaderJig.cs", "ĐANG LÀ MOCK"),
    ("ClassLibrary1.TagChiaThep", "0x02000E53", "DrawJig: Rebar division callout tag rubber-banding", "src/HPDcePro.AutoCad/Jigs/TagChiaThepJig.cs", "ĐANG LÀ MOCK"),
    ("ClassLibrary1.ijgRaithepchamtron", "0x02000E56", "DrawJig: Dynamic dot/circle rebar distribution preview", "src/HPDcePro.AutoCad/Jigs/JigRaiThepChamTron.cs", "ĐANG LÀ MOCK"),
    ("ClassLibrary1.ijgVeDaiVong", "0x02000E57", "DrawJig: Dynamic closed hoop stirrup sizing preview", "src/HPDcePro.AutoCad/Jigs/JigVeDaiVong.cs", "ĐANG LÀ MOCK"),
    ("ClassLibrary1.JigVeDaiC", "0x02000E58", "DrawJig: Dynamic C-tie / cross-tie sizing preview", "src/HPDcePro.AutoCad/Jigs/JigVeDaiC.cs", "ĐANG LÀ MOCK"),
    ("ClassLibrary1.VePolylineBatKy", "0x02000E59", "DrawJig: Arbitrary polyline rebar path drafting", "src/HPDcePro.AutoCad/Jigs/VePolylineBatKyJig.cs", "ĐANG LÀ MOCK"),
    ("ClassLibrary1.ijgVeThepVachCT", "0x02000E5A", "DrawJig: Core shear wall rebar cage dynamic sizing", "src/HPDcePro.AutoCad/Jigs/JigVeThepVachCt.cs", "ĐANG LÀ MOCK"),
    ("ClassLibrary1.JigVeVachHam", "0x02000E5B", "DrawJig: Basement retaining wall rebar cage dynamic preview", "src/HPDcePro.AutoCad/Jigs/JigVeVachHam.cs", "ĐANG LÀ MOCK"),
    ("ClassLibrary1.DimNhanhMBKC", "0x02000E5C", "DrawJig: Rapid dimension chain alignment across framing", "src/HPDcePro.AutoCad/Jigs/DimNhanhMbkcJig.cs", "ĐANG LÀ MOCK")
]

# 6. UI Forms & Palettes (12)
ui_forms = [
    ("ClassLibrary1.DceDamMatCatMoi.DamCong_Frm", "0x02000DA6", "Specialized curved beam cross-section detailing dialog", "src/HPDcePro.UI/Views/DamCongWindow.xaml", "ĐANG LÀ MOCK"),
    ("ClassLibrary1.DceMongCocMoi.MongCoc_Frm", "0x02000161", "Multi-pile cap geometry & coordinate configuration dialog", "src/HPDcePro.UI/Views/MongCocWindow.xaml", "ĐANG LÀ MOCK"),
    ("ClassLibrary1.DceMongCocMoi.MongCoc_FrmLopThepThem", "0x02000163", "Additional reinforcement layer configuration for pile caps", "src/HPDcePro.UI/Views/MongCocLopThepThemWindow.xaml", "ĐANG LÀ MOCK"),
    ("ClassLibrary1.DceMongCocMoi.MongCoc_FrmChonThepTuDong", "0x02000165", "Automated pile cap rebar selection & optimization dialog", "src/HPDcePro.UI/Views/MongCocChonThepTuDongWindow.xaml", "ĐANG LÀ MOCK"),
    ("ClassLibrary1.DceThangMoi.ThangMoi_Frm", "0x02000136", "Staircase geometry, risers, treads, and landing detailing dialog", "src/HPDcePro.UI/Views/ThangMoiWindow.xaml", "ĐANG LÀ MOCK"),
    ("ClassLibrary1.DceVeCocBeTong.CocBT_Frm", "0x02000DCA", "Precast reinforced concrete pile detailing & rebar layout", "src/HPDcePro.UI/Views/CocBtWindow.xaml", "ĐANG LÀ MOCK"),
    ("ClassLibrary1.TKT_frmMain", "0x02000246", "Master rebar schedule dashboard, aggregation, and editing hub", "src/HPDcePro.UI/Views/MasterRebarScheduleDashboardWindow.xaml", "ĐANG LÀ MOCK"),
    ("ClassLibrary1.AutoStr.AutoStrWindow", "0x02000B23", "AutoStr live 24-step project execution visualizer", "src/HPDcePro.UI/Views/AutoStrExecutionWindow.xaml", "ĐANG LÀ MOCK"),
    ("ClassLibrary1.Bea_ToiUuCatThepV2", "0x02000E5D", "1D commercial rebar cutting stock optimizer dialog (11.7m)", "src/HPDcePro.UI/Views/CuttingOptimizationAdvancedWindow.xaml", "ĐANG LÀ MOCK"),
    ("ClassLibrary1.DcePalette", "0x02000E12", "AutoCAD dockable PaletteSet tool control center", "src/HPDcePro.AutoCad/Palettes/DcePaletteControl.xaml", "ĐANG LÀ MOCK"),
    ("ClassLibrary1.DcePalette_WallThickness", "0x02000E13", "Dockable tool palette for quick wall thickness selection", "src/HPDcePro.AutoCad/Palettes/WallThicknessPaletteControl.xaml", "ĐANG LÀ MOCK"),
    ("ClassLibrary1.DceRibbon", "0x02000E1E", "AutoCAD Ribbon tab and push button factory", "src/HPDcePro.AutoCad/Ribbon/DceRibbonBuilder.cs", "ĐANG LÀ MOCK")
]

# 7. AutoStr 24 Steps
autostr_steps = [
    ("AutoStrStep.S01_TaoSheetSet", "0x02000B24", "Step 1: Create sheet set (.dst) and drawing register", "src/HPDcePro.Core/AutoStr/Steps/AutoStrStep01_TaoSheetSet.cs", "HOÀN THÀNH"),
    ("AutoStrStep.S02_LuoiTrucExrefCot", "0x02000B24", "Step 2: Generate structural grid lines and column xrefs", "src/HPDcePro.Core/AutoStr/Steps/AutoStrStep02_LuoiTruc.cs", "HOÀN THÀNH"),
    ("AutoStrStep.S03_DinhViCot", "0x02000B24", "Step 3: Column layout and dimension tagging", "src/HPDcePro.Core/AutoStr/Steps/AutoStrStep03_DinhViCot.cs", "HOÀN THÀNH"),
    ("AutoStrStep.S04_MBKC", "0x02000B24", "Step 4: Structural framing plan (MBKC) generation", "src/HPDcePro.Core/AutoStr/Steps/AutoStrStep04_MBKC.cs", "HOÀN THÀNH"),
    ("AutoStrStep.S05_DuongPhanTichLuc", "0x02000B24", "Step 5: Tributary load distribution line calculation", "src/HPDcePro.Core/AutoStr/Steps/AutoStrStep05_DuongPhanTichLuc.cs", "HOÀN THÀNH"),
    ("AutoStrStep.S06_TaiTuong", "0x02000B24", "Step 6: Wall load mapping and partition evaluation", "src/HPDcePro.Core/AutoStr/Steps/AutoStrStep06_TaiTuong.cs", "HOÀN THÀNH"),
    ("AutoStrStep.S07_BuildEtabsModel", "0x02000B24", "Step 7: Automated 3D finite-element model construction in ETABS", "src/HPDcePro.Core/AutoStr/Steps/AutoStrStep07_BuildEtabsModel.cs", "ĐANG LÀ MOCK"),
    ("AutoStrStep.S08_TinhToanDamExcel", "0x02000B24", "Step 8: Automated continuous beam calculation via Excel", "src/HPDcePro.Core/AutoStr/Steps/AutoStrStep08_TinhToanDamExcel.cs", "ĐANG LÀ MOCK"),
    ("AutoStrStep.S09_VeChiTietDam", "0x02000B24", "Step 9: Automated beam longitudinal & cross-section drafting", "src/HPDcePro.Core/AutoStr/Steps/AutoStrStep09_VeChiTietDam.cs", "HOÀN THÀNH"),
    ("AutoStrStep.S10_TinhToanCotExcel", "0x02000B24", "Step 10: Column P-M-M interaction calculation via Excel", "src/HPDcePro.Core/AutoStr/Steps/AutoStrStep10_TinhToanCotExcel.cs", "ĐANG LÀ MOCK"),
    ("AutoStrStep.S11_VeChiTietCotVach", "0x02000B24", "Step 11: Column & shear wall detailing drafting", "src/HPDcePro.Core/AutoStr/Steps/AutoStrStep11_VeChiTietCotVach.cs", "HOÀN THÀNH"),
    ("AutoStrStep.S12_VeChiTietSan", "0x02000B24", "Step 12: Floor slab top & bottom reinforcement drafting", "src/HPDcePro.Core/AutoStr/Steps/AutoStrStep12_VeChiTietSan.cs", "HOÀN THÀNH"),
    ("AutoStrStep.S13_MBMongCoc", "0x02000B24", "Step 13: Pile foundation framing plan generation", "src/HPDcePro.Core/AutoStr/Steps/AutoStrStep13_MBMongCoc.cs", "HOÀN THÀNH"),
    ("AutoStrStep.S14_DPTLMong", "0x02000B24", "Step 14: Foundation load path & reaction evaluation", "src/HPDcePro.Core/AutoStr/Steps/AutoStrStep14_DPTLMong.cs", "HOÀN THÀNH"),
    ("AutoStrStep.S15_EtabsMong", "0x02000B24", "Step 15: Foundation reaction extraction from ETABS/SAFE", "src/HPDcePro.Core/AutoStr/Steps/AutoStrStep15_EtabsMong.cs", "ĐANG LÀ MOCK"),
    ("AutoStrStep.S16_TTMong", "0x02000B24", "Step 16: Automated pile cap punching shear & flexure calculation", "src/HPDcePro.Core/AutoStr/Steps/AutoStrStep16_TTMong.cs", "ĐANG LÀ MOCK"),
    ("AutoStrStep.S17_VeMong", "0x02000B24", "Step 17: Foundation detailing drafting (pile caps & pad footings)", "src/HPDcePro.Core/AutoStr/Steps/AutoStrStep17_VeMong.cs", "HOÀN THÀNH"),
    ("AutoStrStep.S18_TTGiangM", "0x02000B24", "Step 18: Tie beam calculation (soil pressure & differential settlement)", "src/HPDcePro.Core/AutoStr/Steps/AutoStrStep18_TTGiangM.cs", "ĐANG LÀ MOCK"),
    ("AutoStrStep.S19_VeGiangM", "0x02000B24", "Step 19: Tie beam detailing drafting", "src/HPDcePro.Core/AutoStr/Steps/AutoStrStep19_VeGiangM.cs", "HOÀN THÀNH"),
    ("AutoStrStep.S20_CKPhu", "0x02000B24", "Step 20: Secondary structural elements (stairs, lintels, canopies)", "src/HPDcePro.Core/AutoStr/Steps/AutoStrStep20_CKPhu.cs", "HOÀN THÀNH"),
    ("AutoStrStep.S21_DanhMuc", "0x02000B24", "Step 21: Master drawing list & index compilation", "src/HPDcePro.Core/AutoStr/Steps/AutoStrStep21_DanhMuc.cs", "HOÀN THÀNH"),
    ("AutoStrStep.S22_MoSheetSet", "0x02000B24", "Step 22: Sheet set assembly and layout initialization", "src/HPDcePro.Core/AutoStr/Steps/AutoStrStep22_MoSheetSet.cs", "HOÀN THÀNH"),
    ("AutoStrStep.S23_InAn", "0x02000B24", "Step 23: Batch plotting & PDF publication", "src/HPDcePro.Core/AutoStr/Steps/AutoStrStep23_InAn.cs", "ĐANG LÀ MOCK"),
    ("AutoStrStep.S24_TMTT", "0x02000B24", "Step 24: Structural calculation report (thuyết minh) generation", "src/HPDcePro.Core/AutoStr/Steps/AutoStrStep24_TMTT.cs", "ĐANG LÀ MOCK")
]

print("=== TOTAL ENTITY COUNTS ===")
print(f"Subsystem 1 (AutoCAD Commands): {len(cad_commands)}")
print(f"Subsystem 2 (Excel Ribbon Actions): {len(excel_actions)}")
print(f"Subsystem 3 (DceSteel Facade): {len(thep_facade)}")
print(f"Subsystem 4 (MBKC Facade): {len(mbkc_facade)}")
print(f"Subsystem 5 (Native C++ Exports): {len(native_exports)}")
print(f"Subsystem 6 (Revit 2026 Commands): {len(revit_commands)}")
print(f"Subsystem 7 (CAD Jigs): {len(cad_jigs)}")
print(f"Subsystem 8 (UI Forms & Palettes): {len(ui_forms)}")
print(f"Subsystem 9 (AutoStr Steps): {len(autostr_steps)}")
total = len(cad_commands) + len(excel_actions) + len(thep_facade) + len(mbkc_facade) + len(native_exports) + len(revit_commands) + len(cad_jigs) + len(ui_forms) + len(autostr_steps)
print(f"Grand Total Tracked Endpoints: {total}")
