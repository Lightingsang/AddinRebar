using System;
using System.Collections.Generic;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Core.Calculations.Steel
{
    public enum FrameRafterSide
    {
        Left,
        Right
    }

    public sealed class GableFrameInput
    {
        public double SpanMm { get; set; } = 24000.0;
        public double EaveHeightMm { get; set; } = 7000.0;
        public double RoofSlopePercent { get; set; } = 10.0;
        public double BaySpacingMm { get; set; } = 6000.0;
        public double DeadLoadKnM2 { get; set; } = 0.15;
        public double LiveLoadKnM2 { get; set; } = 0.30;
        public double WindPressureKpa { get; set; } = 0.83;

        // Tapered column
        public double ColumnStartDepthMm { get; set; } = 400.0;
        public double ColumnEndDepthMm { get; set; } = 750.0;
        public double ColumnFlangeWidthMm { get; set; } = 250.0;
        public double ColumnFlangeThicknessMm { get; set; } = 12.0;
        public double ColumnWebThicknessMm { get; set; } = 8.0;

        // Tapered rafter
        public double RafterStartDepthMm { get; set; } = 750.0;
        public double RafterEndDepthMm { get; set; } = 450.0;
        public double RafterFlangeWidthMm { get; set; } = 220.0;
        public double RafterFlangeThicknessMm { get; set; } = 10.0;
        public double RafterWebThicknessMm { get; set; } = 6.0;

        // Purlin spacing target
        public double TargetPurlinSpacingMm { get; set; } = 1350.0;
        public double EavePurlinOffsetMm { get; set; } = 150.0;
        public double RidgePurlinOffsetMm { get; set; } = 150.0;

        public double YieldStrengthMpa { get; set; } = 345.0;
        public double ElasticModulusMpa { get; set; } = 206000.0;
    }

    public sealed class PurlinRun
    {
        public int Index { get; set; }
        public FrameRafterSide Side { get; set; }
        public double SlopeDistanceMm { get; set; }
        public Point2D Coordinate { get; set; } = new(0, 0);
    }

    public sealed class TaperedSectionAtX
    {
        public double XFromStartMm { get; set; }
        public double DepthMm { get; set; }
        public double FlangeWidthMm { get; set; }
        public double FlangeThicknessMm { get; set; }
        public double WebThicknessMm { get; set; }
        public double AreaMm2 { get; set; }
        public double IxMm4 { get; set; }
        public double WxMm3 { get; set; }
    }

    public sealed class GableFrameResult
    {
        public double ApexHeightMm { get; set; }
        public double RafterSlopeAngleDeg { get; set; }
        public double RafterSlopeLengthMm { get; set; }
        public Point2D BaseLeft { get; set; }
        public Point2D EaveLeft { get; set; }
        public Point2D Apex { get; set; }
        public Point2D EaveRight { get; set; }
        public Point2D BaseRight { get; set; }

        public IReadOnlyList<PurlinRun> Purlins { get; set; } = Array.Empty<PurlinRun>();
        public double ActualPurlinSpacingMm { get; set; }
        public int PurlinCountPerRafter { get; set; }

        public double FactoredLoadKnM { get; set; }
        public double MaxKneeMomentKnm { get; set; }
        public double MaxApexMomentKnm { get; set; }
        public double BaseVerticalReactionKn { get; set; }
        public double BaseHorizontalThrustKn { get; set; }

        public double KneeStressRatio { get; set; }
        public double ApexStressRatio { get; set; }
        public double EstimatedRidgeDeflectionMm { get; set; }
        public double AllowableRidgeDeflectionMm { get; set; }
        public bool IsStressCompliant => KneeStressRatio <= 1.0 && ApexStressRatio <= 1.0;
        public bool IsDeflectionCompliant => EstimatedRidgeDeflectionMm <= AllowableRidgeDeflectionMm;
        public bool IsCompliant => IsStressCompliant && IsDeflectionCompliant;
    }
}
