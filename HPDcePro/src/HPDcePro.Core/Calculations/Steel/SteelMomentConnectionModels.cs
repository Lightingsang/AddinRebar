using System;

namespace HPDcePro.Core.Calculations.Steel
{
    public enum BoltGrade
    {
        Grade8_8,
        Grade10_9
    }

    public sealed class SteelMomentConnectionInput
    {
        // Beam geometry
        public double BeamDepthMm { get; set; } = 500.0;
        public double BeamFlangeWidthMm { get; set; } = 200.0;
        public double BeamFlangeThicknessMm { get; set; } = 16.0;
        public double BeamWebThicknessMm { get; set; } = 10.0;

        // Column geometry
        public double ColumnDepthMm { get; set; } = 500.0;
        public double ColumnFlangeThicknessMm { get; set; } = 20.0;
        public double ColumnWebThicknessMm { get; set; } = 12.0;

        // Internal forces
        public double BendingMomentKnm { get; set; } = 180.0;
        public double ShearForceKn { get; set; } = 120.0;
        public double AxialForceKn { get; set; } = 0.0;

        // End plate geometry
        public double EndPlateWidthMm { get; set; } = 220.0;
        public double EndPlateThicknessMm { get; set; } = 25.0;
        public double EndPlateExtensionMm { get; set; } = 100.0;
        public double BoltPitchTensionMm { get; set; } = 55.0;
        public double BoltGaugeMm { get; set; } = 100.0;

        // Bolt specs
        public BoltGrade Grade { get; set; } = BoltGrade.Grade8_8;
        public double BoltDiameterMm { get; set; } = 24.0;
        public int TensionBoltRowCount { get; set; } = 2; // 2 rows = 4 tension bolts
        public int CompressionBoltRowCount { get; set; } = 1; // 1 row = 2 compression bolts

        // Welds
        public double FlangeWeldLegMm { get; set; } = 10.0;
        public double WebWeldLegMm { get; set; } = 8.0;
        public double WeldDesignStrengthMpa { get; set; } = 180.0;

        // Material
        public double YieldStrengthMpa { get; set; } = 345.0;
        public double ColumnYieldStrengthMpa { get; set; } = 345.0;
    }

    public sealed class SteelConnectionResult
    {
        public double FlangeTensionKn { get; set; }
        public double BoltTensionKn { get; set; }
        public double PryingForceKn { get; set; }
        public double TotalBoltTensionKn { get; set; }
        public double BoltTensileCapacityKn { get; set; }
        public double BoltTensionRatio { get; set; }

        public double BoltShearKn { get; set; }
        public double BoltShearCapacityKn { get; set; }
        public double BoltShearRatio { get; set; }
        public double BoltInteractionRatio { get; set; }

        public double RequiredPlateThickness { get; set; }
        public double PlateThicknessRatio { get; set; }

        public double FlangeWeldStressMpa { get; set; }
        public double WebWeldStressMpa { get; set; }
        public double WeldStressRatio { get; set; }

        public double ColumnWebPanelShearKn { get; set; }
        public double ColumnWebShearCapacityKn { get; set; }
        public bool StiffenersRequired { get; set; }
        public double StiffenerThickness { get; set; }

        public bool IsCompliant =>
            BoltInteractionRatio <= 1.0 &&
            PlateThicknessRatio <= 1.0 &&
            WeldStressRatio <= 1.0;
    }
}
