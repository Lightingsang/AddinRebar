using System;
using System.Collections.Generic;

namespace HPKata.Core.Serialization
{
    public class KataTagBlockData
    {
        public string SH1 { get; set; } = string.Empty;
        public string DK1 { get; set; } = string.Empty;
        public string KC1 { get; set; } = string.Empty;
        public string DKKC1 { get; set; } = string.Empty;
        public string SH2 { get; set; } = string.Empty;
        public string DK2 { get; set; } = string.Empty;
        public string KC2 { get; set; } = string.Empty;
        public string DKKC2 { get; set; } = string.Empty;
    }

    public class KataBeamPlanBlockData
    {
        public string NameBeam { get; set; } = string.Empty;
        public double WidthMm { get; set; }
        public double HeightMm { get; set; }
        public string Level { get; set; } = string.Empty;
        public double SpanMm { get; set; }
    }

    public class KataBeamRebarBlockData
    {
        public string StirrupClosure { get; set; } = "Closed";
        public string TopRebar { get; set; } = string.Empty;
        public string BottomRebar { get; set; } = string.Empty;
        public string SkinRebar { get; set; } = string.Empty;
        public double CantileverMm { get; set; }
        public string StirrupSpacing { get; set; } = string.Empty;
    }

    public class KataCouplerBlockData
    {
        public double LengthMm { get; set; } = 80.0;
        public int DiameterMm { get; set; } = 20;
        public int MemberCount { get; set; } = 1;
        public int CouplersPerMember { get; set; } = 1;
        public int TotalCouplers => MemberCount * CouplersPerMember;
        public string Notes { get; set; } = "Ren thẳng ISO";
    }

    /// <summary>
    /// Bidirectional mapper between AutoCAD block attribute dictionaries and strongly-typed structural models.
    /// </summary>
    public static class KataBlockAttributeSerializer
    {
        public static KataTagBlockData ReadTagBlock(IDictionary<string, string> attributes)
        {
            var data = new KataTagBlockData();
            if (attributes == null) return data;

            data.SH1 = GetVal(attributes, "SH1");
            data.DK1 = GetVal(attributes, "DK1");
            data.KC1 = GetVal(attributes, "KC1");
            data.DKKC1 = GetVal(attributes, "DKKC1");
            data.SH2 = GetVal(attributes, "SH2");
            data.DK2 = GetVal(attributes, "DK2");
            data.KC2 = GetVal(attributes, "KC2");
            data.DKKC2 = GetVal(attributes, "DKKC2");
            return data;
        }

        public static Dictionary<string, string> WriteTagBlock(KataTagBlockData data)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["SH1"] = data.SH1,
                ["DK1"] = data.DK1,
                ["KC1"] = data.KC1,
                ["DKKC1"] = data.DKKC1,
                ["SH2"] = data.SH2,
                ["DK2"] = data.DK2,
                ["KC2"] = data.KC2,
                ["DKKC2"] = data.DKKC2
            };
        }

        public static KataBeamPlanBlockData ReadBeamPlan(IDictionary<string, string> attributes)
        {
            var data = new KataBeamPlanBlockData();
            if (attributes == null) return data;

            data.NameBeam = GetVal(attributes, "NAME_BEAM");
            data.WidthMm = ParseDouble(GetVal(attributes, "B"));
            data.HeightMm = ParseDouble(GetVal(attributes, "H"));
            data.Level = GetVal(attributes, "LEVEL");
            data.SpanMm = ParseDouble(GetVal(attributes, "SPAN"));
            return data;
        }

        public static Dictionary<string, string> WriteBeamPlan(KataBeamPlanBlockData data)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["NAME_BEAM"] = data.NameBeam,
                ["B"] = data.WidthMm.ToString("F0"),
                ["H"] = data.HeightMm.ToString("F0"),
                ["LEVEL"] = data.Level,
                ["SPAN"] = data.SpanMm.ToString("F0")
            };
        }

        public static KataCouplerBlockData ReadCoupler(IDictionary<string, string> attributes)
        {
            var data = new KataCouplerBlockData();
            if (attributes == null) return data;

            data.LengthMm = ParseDouble(GetVal(attributes, "L"), 80.0);
            data.DiameterMm = (int)ParseDouble(GetVal(attributes, "DK"), 20.0);
            data.MemberCount = (int)ParseDouble(GetVal(attributes, "SCK"), 1.0);
            data.CouplersPerMember = (int)ParseDouble(GetVal(attributes, "SL1"), 1.0);
            data.Notes = GetVal(attributes, "GHI_CHU");
            return data;
        }

        public static Dictionary<string, string> WriteCoupler(KataCouplerBlockData data)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["L"] = data.LengthMm.ToString("F0"),
                ["DK"] = data.DiameterMm.ToString(),
                ["SCK"] = data.MemberCount.ToString(),
                ["SL1"] = data.CouplersPerMember.ToString(),
                ["SL"] = data.TotalCouplers.ToString(),
                ["GHI_CHU"] = data.Notes
            };
        }

        private static string GetVal(IDictionary<string, string> dict, string key)
        {
            return dict.TryGetValue(key, out string? v) ? v ?? string.Empty : string.Empty;
        }

        private static double ParseDouble(string str, double fallback = 0.0)
        {
            return double.TryParse(str, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double val) ? val : fallback;
        }
    }
}
