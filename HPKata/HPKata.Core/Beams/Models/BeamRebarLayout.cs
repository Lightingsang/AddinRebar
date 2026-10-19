using System;
using System.Collections.Generic;
using HPKata.Core.Excel.Data;

namespace HPKata.Core.Beams.Models
{
    /// <summary>
    /// Represents an individual continuous or discrete longitudinal bar run.
    /// </summary>
    public sealed class RebarRunModel
    {
        public string Mark { get; set; } = string.Empty;
        public int DiameterMm { get; set; }
        public int BarCount { get; set; }
        public double StartXMm { get; set; }
        public double LengthMm { get; set; }
        public double ElevationZMm { get; set; }
        public bool IsTopBar { get; set; }
        public int LayerIndex { get; set; } = 1;
        public bool HasStartHook { get; set; }
        public bool HasEndHook { get; set; }
        public double StartHookLengthMm { get; set; }
        public double EndHookLengthMm { get; set; }
        public string ShapeName { get; set; } = "dang01";
    }

    /// <summary>
    /// Represents an extra reinforcement cutoff layer over a support or at midspan.
    /// </summary>
    public sealed class ExtraBarLayerModel
    {
        public int LayerIndex { get; set; } = 1; // 1, 2, 3
        public int BarCount { get; set; }
        public int DiameterMm { get; set; }
        public string RawSpec { get; set; } = string.Empty;
        public double StartStationXMm { get; set; }
        public double EndStationXMm { get; set; }
        public double CutoffLeftMm { get; set; }
        public double CutoffRightMm { get; set; }
        public bool HasLeftHook { get; set; }
        public bool HasRightHook { get; set; }
        public double TotalLengthMm => Math.Max(0.0, EndStationXMm - StartStationXMm);
    }

    /// <summary>
    /// Stirrup shear reinforcement zone parameters across a span.
    /// </summary>
    public sealed class StirrupZoneDistributionModel
    {
        public int SpanIndex { get; set; }
        public int StirrupDiameterMm { get; set; } = 8;
        public double LeftDenseZoneLengthMm { get; set; }
        public double LeftDenseSpacingMm { get; set; } = 100.0;
        public double MidspanSparseZoneLengthMm { get; set; }
        public double MidspanSparseSpacingMm { get; set; } = 200.0;
        public double RightDenseZoneLengthMm { get; set; }
        public double RightDenseSpacingMm { get; set; } = 100.0;
        public int TotalLegCount { get; set; } = 2;
        public int ClosedHoopCount { get; set; } = 1;
        public int UCapCount { get; set; }
        public int CTieCount { get; set; }

        public int EstimatedStirrupCount =>
            (LeftDenseSpacingMm > 0 ? (int)Math.Ceiling(LeftDenseZoneLengthMm / LeftDenseSpacingMm) : 0) +
            (MidspanSparseSpacingMm > 0 ? (int)Math.Ceiling(MidspanSparseZoneLengthMm / MidspanSparseSpacingMm) : 0) +
            (RightDenseSpacingMm > 0 ? (int)Math.Ceiling(RightDenseZoneLengthMm / RightDenseSpacingMm) : 0);
    }

    /// <summary>
    /// Complete reinforcement layout for a continuous concrete beam.
    /// </summary>
    public sealed class BeamRebarLayout
    {
        public string BeamName { get; set; } = string.Empty;
        public double TotalLengthMm { get; set; }
        public KataBeamSheetData? SheetData { get; set; }
        public List<BeamSpanModel> Spans { get; set; } = new List<BeamSpanModel>();
        public List<BeamSupportModel> Supports { get; set; } = new List<BeamSupportModel>();
        public List<RebarRunModel> TopContinuousBars { get; set; } = new List<RebarRunModel>();
        public List<RebarRunModel> BottomContinuousBars { get; set; } = new List<RebarRunModel>();
        public Dictionary<int, List<ExtraBarLayerModel>> SupportTopExtraBars { get; set; } =
            new Dictionary<int, List<ExtraBarLayerModel>>();
        public Dictionary<int, List<ExtraBarLayerModel>> MidspanBottomExtraBars { get; set; } =
            new Dictionary<int, List<ExtraBarLayerModel>>();
        public Dictionary<int, StirrupZoneDistributionModel> StirrupZones { get; set; } =
            new Dictionary<int, StirrupZoneDistributionModel>();
        public List<RebarRunModel> SkinBars { get; set; } = new List<RebarRunModel>();
        public List<BeamIntersectionModel> Intersections { get; set; } = new List<BeamIntersectionModel>();
    }
}
