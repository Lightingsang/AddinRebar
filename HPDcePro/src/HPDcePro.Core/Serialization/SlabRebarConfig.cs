using System;

namespace HPDcePro.Core.Serialization
{
    /// <summary>
    /// Configuration for floor slab reinforcement detailing parameters and spacing.
    /// </summary>
    public class SlabRebarConfig
    {
        public int DuongKinhDuoi_X { get; set; } = 10;
        public int KhoangCachDuoi_X { get; set; } = 200;
        public int DuongKinhDuoi_Y { get; set; } = 10;
        public int KhoangCachDuoi_Y { get; set; } = 200;
        public int GcDkDuoi_X { get; set; } = 0;
        public int GcADuoi_X { get; set; } = 0;
        public int GcDkDuoi_Y { get; set; } = 0;
        public int GcADuoi_Y { get; set; } = 0;
        public int DuongKinhTren_X { get; set; } = 0;
        public int KhoangCachTren_X { get; set; } = 0;
        public int DuongKinhTren_Y { get; set; } = 0;
        public int KhoangCachTren_Y { get; set; } = 0;
        public int GcDkTren_X { get; set; } = 10;
        public int GcATren_X { get; set; } = 200;
        public int GcDkTren_Y { get; set; } = 10;
        public int GcATren_Y { get; set; } = 200;
        public double TyLeMu { get; set; } = 4.0;
        public double TyLeGcDuoi { get; set; } = 4.0;
        public double OffsetMepSan { get; set; } = 30.0;
        public double NeoThepDuoi { get; set; } = 40.0;
        public int NeoModeDuoi { get; set; } = 0;
        public double NeoThepTren { get; set; } = 40.0;
        public int NeoModeTren { get; set; } = 0;
        public double OffsetMepDamSongSong { get; set; } = 50.0;
        public double ChieuSauNhanThep { get; set; } = 50.0;
        public double ChieuDaiNhipMin { get; set; } = 2500.0;
        public bool NhipChiDamChinh { get; set; } = true;
        public bool GopThepSan { get; set; } = true;
    }
}
