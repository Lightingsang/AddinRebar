using System;

namespace HPDcePro.Core.Serialization
{
    public class AutoStrLayers
    {
        public string LayerLuoiTruc { get; set; } = "KT_TRUC";
        public string LayerDimTruc { get; set; } = "KT_DIM_TRUC";
        public string LayerCot { get; set; } = "KT_COT";
        public string LayerDam { get; set; } = "DAM";
        public string LayerBaoSan { get; set; } = "KT_BAO_SAN";
        public string LayerLoMo { get; set; } = "KT_LO_MO";
        public string LayerNhaVeSinh { get; set; } = "KT_SAN_HA_COT";
        public string LayerTuong { get; set; } = "KT_TUONG";
        public string LayerKhungRectang { get; set; } = "KhungTenKC";
        public string LayerRanhDat { get; set; } = "KT_RANH_DAT";
        public string LayerKcTruc { get; set; } = "DCE_Truc";
        public string LayerKcDim { get; set; } = "DCE_Dim";
        public string LayerKcCot { get; set; } = "DCE_SUB-COWA_----";
        public string LayerKcDam { get; set; } = "DCE_Beam";
        public string LayerKcBaoSan { get; set; } = "DCE_BaoBeTong";
        public string LayerKcLoMo { get; set; } = "DCE_Gen_Open";
        public string LayerKcNvs { get; set; } = "DCE_NetThay";
        public string LayerKcText { get; set; } = "Dce_text";
        public string LayerThepDuoiSuot { get; set; } = "DCE_San_Duoi";
        public string LayerThepDuoiGc { get; set; } = "DCE_San_Duoi_GC";
        public string LayerThepTrenSuot { get; set; } = "DCE_San_Tren";
        public string LayerThepTrenGc { get; set; } = "DCE_San_Tren_GC";
        public string LayerDptlBeam { get; set; } = "Beam";
        public string LayerDptlColumn { get; set; } = "Column";
        public string LayerDptlWall { get; set; } = "Wall";
        public string LayerDptlSlab { get; set; } = "Slab";
        public string LayerDptlOpening { get; set; } = "Opening";
    }

    public class AutoStrFolders
    {
        public string Folder01 { get; set; } = "01.SheetSet";
        public string Folder02 { get; set; } = "02.Xref";
        public string Folder04 { get; set; } = "06.MatBangMong";
        public string Folder05 { get; set; } = "07.MatBangKetCau";
        public string Folder06 { get; set; } = "08.MatBangCotVach";
        public string Folder07 { get; set; } = "09.ChiTietCotVach";
        public string Folder08 { get; set; } = "10.ChiTietDam";
        public string Folder09 { get; set; } = "11.ChiTietSan";
        public string Folder10 { get; set; } = "12.CauKienPhu";
        public string Folder11 { get; set; } = "03.DuongPhanTichLuc";
        public string Folder13 { get; set; } = "04.Etabs";
        public string Folder14 { get; set; } = "05.ExcelTinhToan";
    }

    public class AutoStrFilePrefixes
    {
        public string PrefixMBKC { get; set; } = "MBKC";
        public string PrefixMBDVColumn { get; set; } = "MBDV_Cot";
        public string PrefixChiTietDam { get; set; } = "ChiTietDam";
        public string PrefixChiTietCot { get; set; } = "ChiTietCot";
        public string PrefixChiTietSan { get; set; } = "ChiTietSan";
        public string PrefixMBDVCoc { get; set; } = "MBDV_Coc";
        public string PrefixChiTietMong { get; set; } = "ChiTietMong";
        public string PrefixMBKCMong { get; set; } = "MBKC_Mong";
        public string PrefixChiTietDamMong { get; set; } = "ChiTietDamMong";
        public string PrefixMBDVBe { get; set; } = "MBDV_Be";
        public string PrefixChiTietBe { get; set; } = "ChiTietBe";
        public string PrefixChiTietThang { get; set; } = "ChiTietThang";
        public string PrefixChiTietLanhTo { get; set; } = "ChiTietLanhTo";
    }

    public class AutoStrLayoutSettings
    {
        public double PadX { get; set; } = 0.0;
        public double PadY { get; set; } = 0.0;
        public double SaiSo { get; set; } = 3.0;
        public double DoanChongLan { get; set; } = 500.0;
        public bool CatDungTruc { get; set; } = true;
        public bool CatDungTrucKeoDaiHetKhung { get; set; } = true;
    }

    public class AutoStrSheetSetSettings
    {
        public string SheetSetName { get; set; } = string.Empty;
        public string SubsetMBKC { get; set; } = "MẶT BẰNG KẾT CẤU";
        public string SubsetChiTietDam { get; set; } = "CHI TIẾT DẦM";
        public string SubsetChiTietCot { get; set; } = "CHI TIẾT CỘT VÁCH";
        public string SubsetChiTietSan { get; set; } = "CHI TIẾT SÀN";
        public string SubsetMBCot { get; set; } = "MẶT BẰNG CỘT VÁCH";
        public string SubsetGhiChu { get; set; } = "GHI CHÚ CHUNG";
        public string SubsetPhanNgam { get; set; } = "PHẦN NGẦM";
        public string SubsetCauKienPhu { get; set; } = "CẤU KIỆN PHỤ";
        public string SubsetPhanTichLuc { get; set; } = "PHÂN TÍCH LỰC";
        public string SheetNumberPrefix { get; set; } = "KC-";
        public int SheetNumberStart { get; set; } = 1;
    }

    public class AutoStrDrawingScales
    {
        public string TyLeChiTietDam { get; set; } = "1:75";
        public string TyLeChiTietCot { get; set; } = "1:75";
        public string TyLeChiTietSan { get; set; } = "1:75";
        public string TyLeMBCot { get; set; } = "1:75";
    }
}
