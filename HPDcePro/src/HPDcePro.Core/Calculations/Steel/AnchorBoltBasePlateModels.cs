using System;
using HPDcePro.Core.Calculations.Beam;

namespace HPDcePro.Core.Calculations.Steel
{
    public sealed class AnchorBoltBasePlateInput
    {
        // Column profile
        public double ColumnDepthMm { get; set; } = 400.0;
        public double ColumnWidthMm { get; set; } = 250.0;
        public double ColumnFlangeThicknessMm { get; set; } = 12.0;
        public double ColumnWebThicknessMm { get; set; } = 8.0;

        // Base plate dimensions
        public double PlateLengthLMm { get; set; } = 600.0;
        public double PlateWidthBMm { get; set; } = 400.0;
        public double PlateThicknessMm { get; set; } = 25.0;

        // Pedestal
        public ConcreteGrade PedestalGrade { get; set; } = ConcreteGrade.B25;
        public double PedestalLengthMm { get; set; } = 800.0;
        public double PedestalWidthMm { get; set; } = 600.0;

        // Loads
        public double AxialForceKn { get; set; } = 450.0; // Compression > 0
        public double BendingMomentKnm { get; set; } = 95.0;
        public double ShearForceKn { get; set; } = 65.0;

        // Anchor bolts
        public int AnchorBoltCount { get; set; } = 4;
        public double AnchorBoltDiameterMm { get; set; } = 24.0;
        public double BoltDistanceBetweenRowsMm { get; set; } = 480.0;
        public double AnchorYieldStrengthMpa { get; set; } = 300.0;

        // Plate material
        public double PlateYieldStrengthMpa { get; set; } = 245.0;
    }

    public sealed class BasePlateResult
    {
        public double BearingStress { get; set; }
        public double MinBearingStress { get; set; }
        public double AllowableBearingStress { get; set; }
        public double BearingStressRatio { get; set; }

        public double RequiredPlateThickness { get; set; }
        public double PlateThicknessRatio { get; set; }

        public int AnchorBoltCount { get; set; }
        public double AnchorBoltDiameter { get; set; }
        public double AnchorTensionForceKn { get; set; }
        public double AnchorTensileCapacityKn { get; set; }
        public double AnchorTensionRatio { get; set; }
        public double EmbedmentLength { get; set; }

        public double BaseShearCapacityKn { get; set; }
        public double BaseShearRatio { get; set; }

        public bool IsBearingSafe => BearingStressRatio <= 1.0;
        public bool IsPlateThicknessSafe => PlateThicknessRatio <= 1.0;
        public bool IsAnchorTensionSafe => AnchorTensionRatio <= 1.0;
        public bool IsShearSafe => BaseShearRatio <= 1.0;
        public bool IsCompliant => IsBearingSafe && IsPlateThicknessSafe && IsAnchorTensionSafe && IsShearSafe;
    }
}
