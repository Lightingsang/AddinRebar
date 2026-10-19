using System;
using System.Collections.Generic;
using HPDcePro.Core.Calculations.Beam;

namespace HPDcePro.Core.Calculations.Foundation
{
    public enum PileLayoutPattern
    {
        Custom,
        SinglePile,
        TwoPile,
        ThreePileTriangular,
        FourPileRectangular,
        FivePileDice,
        SixPileRectangular
    }

    public sealed class PileCoordinateItem
    {
        public string PileId { get; set; } = string.Empty;
        public double XMm { get; set; }
        public double YMm { get; set; }
        public double DiameterMm { get; set; } = 400.0;

        public PileCoordinateItem() { }

        public PileCoordinateItem(string pileId, double xMm, double yMm, double diameterMm = 400.0)
        {
            PileId = pileId;
            XMm = xMm;
            YMm = yMm;
            DiameterMm = diameterMm;
        }
    }

    public sealed class PileReactionResult
    {
        public string PileId { get; set; } = string.Empty;
        public double XMm { get; set; }
        public double YMm { get; set; }
        public double ReactionKn { get; set; }
        public bool IsTension => ReactionKn < 0.0;
        public double UtilizationRatio { get; set; }

        public PileReactionResult() { }

        public PileReactionResult(string pileId, double xMm, double yMm, double reactionKn, double capacityKn)
        {
            PileId = pileId;
            XMm = xMm;
            YMm = yMm;
            ReactionKn = reactionKn;
            UtilizationRatio = capacityKn > 0 ? reactionKn / capacityKn : 0.0;
        }
    }

    public sealed class PileCoordinateScheduleItem
    {
        public int Index { get; set; }
        public string PileId { get; set; } = string.Empty;
        public double XLocalM { get; set; }
        public double YLocalM { get; set; }
        public double DiameterM { get; set; }
        public double DesignCapacityKn { get; set; }
        public double ReactionKn { get; set; }
        public string Status { get; set; } = "PASS";
    }

    public sealed class PileCapInput
    {
        public string Mark { get; set; } = "DC-1";
        public double WidthBMm { get; set; } = 1800.0;
        public double LengthLMm { get; set; } = 1800.0;
        public double ThicknessHMm { get; set; } = 900.0;
        public double ColumnWidthBMm { get; set; } = 400.0;
        public double ColumnHeightHMm { get; set; } = 400.0;
        public ConcreteProperties Concrete { get; set; }
        public RebarProperties Rebar { get; set; }
        public double ConcreteCoverMm { get; set; } = 50.0;
        public IReadOnlyList<PileCoordinateItem> Piles { get; set; } = Array.Empty<PileCoordinateItem>();
        public double AxialForceNKn { get; set; } = 1500.0;
        public double MomentMxKnm { get; set; } = 50.0;
        public double MomentMyKnm { get; set; } = 40.0;
        public double PileCapacityRcKn { get; set; } = 600.0;
        public double PileCapDensityKnM3 { get; set; } = 25.0;
        public double SoilDepthM { get; set; } = 1.0;
        public double SoilDensityKnM3 { get; set; } = 18.0;

        public PileCapInput(
            string mark, double widthB, double lengthL, double thicknessH,
            double colB, double colH, ConcreteProperties concrete, RebarProperties rebar,
            IReadOnlyList<PileCoordinateItem> piles, double nKn, double mxKnm, double myKnm,
            double rcKn)
        {
            Mark = mark;
            WidthBMm = widthB;
            LengthLMm = lengthL;
            ThicknessHMm = thicknessH;
            ColumnWidthBMm = colB;
            ColumnHeightHMm = colH;
            Concrete = concrete ?? throw new ArgumentNullException(nameof(concrete));
            Rebar = rebar ?? throw new ArgumentNullException(nameof(rebar));
            Piles = piles ?? Array.Empty<PileCoordinateItem>();
            AxialForceNKn = nKn;
            MomentMxKnm = mxKnm;
            MomentMyKnm = myKnm;
            PileCapacityRcKn = rcKn;
        }
    }

    public sealed class PileCapResult
    {
        public string Mark { get; set; } = string.Empty;
        public int PileCount { get; set; }
        public IReadOnlyList<PileReactionResult> Piles { get; set; } = Array.Empty<PileReactionResult>();
        public double MaxReactionKn { get; set; }
        public double MinReactionKn { get; set; }
        public double AvgReactionKn { get; set; }
        public double PileCapacityRatio { get; set; }
        public double SelfWeightAndSoilKn { get; set; }
        public double ColumnPunchingForceKn { get; set; }
        public double ColumnPunchingCapacityKn { get; set; }
        public double ColumnPunchingRatio { get; set; }
        public bool IsColumnPunchingSafe => ColumnPunchingRatio <= 1.0;
        public double CornerPilePunchingForceKn { get; set; }
        public double CornerPilePunchingCapacityKn { get; set; }
        public double CornerPilePunchingRatio { get; set; }
        public double OneWayShearForceKn { get; set; }
        public double OneWayShearCapacityKn { get; set; }
        public double OneWayShearRatio { get; set; }
        public double DesignMomentMxKnm { get; set; }
        public double RequiredAsYMm2 { get; set; }
        public double DesignMomentMyKnm { get; set; }
        public double RequiredAsXMm2 { get; set; }
        public bool IsSafe { get; set; }
        public string StatusSummary { get; set; } = string.Empty;
        public IReadOnlyList<PileCoordinateScheduleItem> CoordinateSchedule { get; set; } =
            Array.Empty<PileCoordinateScheduleItem>();
    }
}
