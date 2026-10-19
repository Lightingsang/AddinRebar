using System;

namespace HPDcePro.Core.Serialization
{
    /// <summary>
    /// Configuration for soil-structure interaction spring parameters and foundation geometry.
    /// </summary>
    public class PointSpringSettings
    {
        public string PropName { get; set; } = string.Empty;
        public double TransX { get; set; } = 0.0;
        public double TransY { get; set; } = 0.0;
        public double TransZ { get; set; } = 0.0;
        public double RotX { get; set; } = 0.0;
        public double RotY { get; set; } = 0.0;
        public double RotZ { get; set; } = 0.0;
        public double KhoangCachCoc { get; set; } = 3.0;
        public double MepCocDai { get; set; } = 250.0;
        public double ChieuCaoDaiMong { get; set; } = 700.0;
        public double RongDamMong { get; set; } = 300.0;
        public double CaoDamMong { get; set; } = 600.0;
        public double HeSoAnToanCoc { get; set; } = 1.265;
        public double KcCocRanhDat { get; set; } = 500.0;
        public string PrefixTenCoc { get; set; } = "C";
        public int SoBatDauCoc { get; set; } = 1;
        public double HeSoEpCocPmin { get; set; } = 2.0;
        public double HeSoEpCocPmax { get; set; } = 2.5;
    }

    /// <summary>
    /// Individual pile/spring property item.
    /// </summary>
    public class PointSpringItem
    {
        public string Name { get; set; } = "SP1";
        public string LoaiCoc { get; set; } = "CV1";
        public int DuongKinh { get; set; } = 200;
        public double SucChiuTai { get; set; } = 200.0;
        public double TransX { get; set; } = 2000.0;
        public double TransY { get; set; } = 2000.0;
        public double TransZ { get; set; } = 20000.0;
        public double RotX { get; set; } = 0.0;
        public double RotY { get; set; } = 0.0;
        public double RotZ { get; set; } = 0.0;
        public string GhiChu { get; set; } = string.Empty;
        public bool IsSelected { get; set; } = true;
    }
}
