using System;
using System.Collections.Generic;

namespace HPDcePro.Core.Serialization
{
    /// <summary>
    /// Configuration for structural load combinations under TCVN 2737:2023.
    /// </summary>
    public class ComboSettings
    {
        public bool CoDongDat { get; set; } = false;
        public bool CoBan { get; set; } = true;
        public string CapCongTrinh { get; set; } = "C2";
        public double FSNhanThem { get; set; } = 1.0;
        public string LoadCaseTaiTuong { get; set; } = "BW";
        public string LoadCaseTinhTai { get; set; } = "SDL";
        public string LoadCaseHoatTai { get; set; } = "LL";
        public List<string> LoadCases { get; set; } = new List<string>();
        public List<string> LoadCaseTypes { get; set; } = new List<string>();
        public List<string> ULS_Names { get; set; } = new List<string>();
        public List<string> SLS_Names { get; set; } = new List<string>();
        public Dictionary<string, List<double>> ULS_Coeffs { get; set; } = new Dictionary<string, List<double>>();
        public Dictionary<string, List<double>> SLS_Coeffs { get; set; } = new Dictionary<string, List<double>>();
    }

    /// <summary>
    /// Beam and column combination references for internal force extractions.
    /// </summary>
    public class BeamColumnComboSettings
    {
        public List<string> SourceULS { get; set; } = new List<string>();
        public List<string> SourceSLS { get; set; } = new List<string>();
        public List<string> BeamULS { get; set; } = new List<string>();
        public bool TinhNut { get; set; } = true;
        public List<string> NutTongTai { get; set; } = new List<string>();
        public List<string> NutDaiHan { get; set; } = new List<string>();
        public List<string> ColumnULS { get; set; } = new List<string>();
    }

    /// <summary>
    /// Pile capacity check combination settings.
    /// </summary>
    public class PileCheckComboSettings
    {
        public List<string> SourceAll { get; set; } = new List<string>();
        public List<string> PileCheck { get; set; } = new List<string>();
    }
}
