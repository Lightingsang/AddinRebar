using System;

namespace HPDcePro.Core.Serialization
{
    /// <summary>
    /// Represents structural story definition and floor parameters in an AutoStr project.
    /// </summary>
    public class AutoStrFloorInfo
    {
        public int StoryIndex { get; set; } = 1;
        public string TenTang { get; set; } = "TẦNG 1";
        public string KyHieuTang { get; set; } = "T1";
        public string FilePathKienTruc { get; set; } = string.Empty;
        public double ChieuCaoTang { get; set; } = 3600.0;
        public int SoTang { get; set; } = 1;
        public bool SuDung { get; set; } = true;
        public double CaoDoTang { get; set; } = 3600.0;
        public double BeRongDam { get; set; } = 220.0;
        public double ChieuCaoDam { get; set; } = 400.0;
        public double ChieuDaySan { get; set; } = 120.0;
        public double ChieuDaySanWC { get; set; } = 100.0;
        public double HoatTai { get; set; } = 1.5;
        public double TinhTai { get; set; } = 1.5;

        public override string ToString() => $"Floor[{StoryIndex}: {TenTang} ({KyHieuTang}), H={ChieuCaoTang}mm, Elev={CaoDoTang}mm]";
    }
}
