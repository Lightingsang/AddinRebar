using System;
using System.Collections.Generic;

namespace HPDcePro.Core.Serialization
{
    /// <summary>
    /// Root configuration model representing an AutoStr project file (.autostr).
    /// Fully compatible with AutoStr/DuAnMau.autostr.
    /// </summary>
    public class AutoStrConfig
    {
        public string TenDuAn { get; set; } = "Dự án mới";
        public string ProjectRootPath { get; set; } = string.Empty;
        public string FileGhiChuChung { get; set; } = string.Empty;
        public string FileKhungTen { get; set; } = string.Empty;
        public string FileBlockTenBanVe { get; set; } = string.Empty;
        public string FileChiTietCoc { get; set; } = string.Empty;
        public string TenCongTrinh { get; set; } = string.Empty;
        public string DiaChiCongTrinh { get; set; } = string.Empty;
        public string ChuDauTu { get; set; } = string.Empty;
        public string ExcelGocPath { get; set; } = string.Empty;

        public string MacBtCotVach { get; set; } = "B20";
        public string MacBtDamSan { get; set; } = "B20";
        public string MacThep { get; set; } = "CB400-V";

        public double BeRongDamDienHinh { get; set; } = 220.0;
        public double ChieuCaoDamDienHinh { get; set; } = 400.0;
        public double BeRongDam2 { get; set; } = 220.0;
        public double ChieuCaoDam2 { get; set; } = 400.0;
        public double BeRongDam3 { get; set; } = 220.0;
        public double ChieuCaoDam3 { get; set; } = 300.0;
        public double ChieuDaySanDienHinh { get; set; } = 120.0;
        public double ChieuDaySanWC { get; set; } = 100.0;

        public bool GopChiTietDamGiongNhau { get; set; } = false;
        public string TienToCot { get; set; } = "C";
        public bool GopCotGiongNhau { get; set; } = true;
        public bool CotChoTangTietDien { get; set; } = true;
        public double CotHeSoTangToiDa { get; set; } = 2.0;
        public double CotBuocTang { get; set; } = 50.0;
        public bool CocTuTangKhiKhongDat { get; set; } = true;
        public bool SanSapXepTagThep { get; set; } = true;
        public bool MbkcKyHieuSan { get; set; } = true;

        public bool QuyDinhThepMong { get; set; } = false;
        public string ThepMongTrenX { get; set; } = "14a100";
        public string ThepMongTrenY { get; set; } = "14a100";
        public string ThepMongDuoiX { get; set; } = "14a100";
        public string ThepMongDuoiY { get; set; } = "14a100";
        public double MongDayBTLot { get; set; } = 100.0;
        public double MongBTLotMoRong { get; set; } = 50.0;
        public double MongBTBVTren { get; set; } = 35.0;
        public double MongBTBVDuoi { get; set; } = 135.0;
        public double MongBTBVThanh { get; set; } = 50.0;
        public double MongNgamCoc { get; set; } = 100.0;
        public string PrefixTenDai { get; set; } = "ĐC.";

        public double KhoiLuongRiengGach { get; set; } = 1500.0;
        public double KhoiLuongRiengVua { get; set; } = 1800.0;
        public double ChieuDayTrat { get; set; } = 30.0;
        public double SaiSoBatDiem { get; set; } = 200.0;

        public string TyLeMBKC { get; set; } = "1:75";
        public string NetIn { get; set; } = "monochrome.ctb";
        public string MayIn { get; set; } = "DWG To PDF.pc3";
        public string KhoGiay { get; set; } = "ISO full bleed A3 (297.00 x 420.00 MM)";
        public bool LuonGhiDe { get; set; } = true;
        public bool TachFileEtabsMong { get; set; } = true;
        public bool TuPhucHoiUngDung { get; set; } = true;
        public bool AiNghiemThuMoiBuoc { get; set; } = true;
        public string AiNghiemThuCacBuoc { get; set; } = "4,5,6,7,13,14,15,20";

        public AutoStrLayers Layers { get; set; } = new AutoStrLayers();
        public AutoStrFolders Folders { get; set; } = new AutoStrFolders();
        public AutoStrFilePrefixes FilePrefixes { get; set; } = new AutoStrFilePrefixes();
        public AutoStrLayoutSettings LayoutSettings { get; set; } = new AutoStrLayoutSettings();
        public AutoStrSheetSetSettings SheetSetSettings { get; set; } = new AutoStrSheetSetSettings();
        public AutoStrDrawingScales DrawingScales { get; set; } = new AutoStrDrawingScales();

        public ComboSettings ComboSettings { get; set; } = new ComboSettings();
        public BeamColumnComboSettings BeamColumnCombo { get; set; } = new BeamColumnComboSettings();
        public PointSpringSettings PointSpring { get; set; } = new PointSpringSettings();
        public List<PointSpringItem> PointSpringList { get; set; } = new List<PointSpringItem>();
        public PileCheckComboSettings PileCheckCombo { get; set; } = new PileCheckComboSettings();
        public SlabRebarConfig SlabRebar { get; set; } = new SlabRebarConfig();
        public List<AutoStrFloorInfo> Floors { get; set; } = new List<AutoStrFloorInfo>();
        public List<bool> StepEnabled { get; set; } = new List<bool>();
    }
}
