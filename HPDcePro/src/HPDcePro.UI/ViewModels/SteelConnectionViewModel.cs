using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Steel;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;

namespace HPDcePro.UI.ViewModels
{
    public sealed partial class SteelConnectionViewModel : ObservableObject
    {
        // --- Tab 1: Moment Connection ---
        [ObservableProperty] private double _beamDepthMm = 500.0;
        [ObservableProperty] private double _beamFlangeWidthMm = 200.0;
        [ObservableProperty] private double _beamFlangeThicknessMm = 16.0;
        [ObservableProperty] private double _beamWebThicknessMm = 10.0;
        [ObservableProperty] private double _columnDepthMm = 500.0;
        [ObservableProperty] private double _columnFlangeThicknessMm = 20.0;
        [ObservableProperty] private double _columnWebThicknessMm = 12.0;
        [ObservableProperty] private double _momentKnm = 180.0;
        [ObservableProperty] private double _shearKn = 120.0;
        [ObservableProperty] private double _axialKn = 0.0;
        [ObservableProperty] private double _endPlateWidthMm = 220.0;
        [ObservableProperty] private double _endPlateThicknessMm = 25.0;
        [ObservableProperty] private double _endPlateExtensionMm = 100.0;
        [ObservableProperty] private double _boltPitchMm = 55.0;
        [ObservableProperty] private double _boltDiameterMm = 24.0;
        [ObservableProperty] private BoltGrade _selectedBoltGrade = BoltGrade.Grade8_8;
        [ObservableProperty] private int _tensionBoltRows = 2;
        [ObservableProperty] private int _compressionBoltRows = 1;
        [ObservableProperty] private double _flangeWeldLegMm = 10.0;
        [ObservableProperty] private double _webWeldLegMm = 8.0;

        [ObservableProperty] private double _requiredPlateThicknessMm;
        [ObservableProperty] private double _boltTensionRatio;
        [ObservableProperty] private double _boltShearRatio;
        [ObservableProperty] private double _boltInteractionRatio;
        [ObservableProperty] private double _weldStressRatio;
        [ObservableProperty] private bool _stiffenersRequired;
        [ObservableProperty] private double _stiffenerThicknessMm;
        [ObservableProperty] private string _momentConnectionStatus = "PASS";
        [ObservableProperty] private string _momentConnectionSummary = string.Empty;

        // --- Tab 2: Base Plate ---
        [ObservableProperty] private double _baseColDepthMm = 400.0;
        [ObservableProperty] private double _baseColWidthMm = 250.0;
        [ObservableProperty] private double _plateLengthMm = 600.0;
        [ObservableProperty] private double _plateWidthMm = 400.0;
        [ObservableProperty] private double _plateThicknessMm = 35.0;
        [ObservableProperty] private double _baseAxialKn = 450.0;
        [ObservableProperty] private double _baseMomentKnm = 95.0;
        [ObservableProperty] private double _baseShearKn = 65.0;
        [ObservableProperty] private ConcreteGrade _selectedPedestalGrade = ConcreteGrade.B25;
        [ObservableProperty] private double _pedestalLengthMm = 800.0;
        [ObservableProperty] private double _pedestalWidthMm = 600.0;
        [ObservableProperty] private int _anchorBoltCount = 4;
        [ObservableProperty] private double _anchorBoltDiameterMm = 24.0;
        [ObservableProperty] private double _anchorBoltDistanceMm = 480.0;

        [ObservableProperty] private double _bearingStressMpa;
        [ObservableProperty] private double _allowableBearingMpa;
        [ObservableProperty] private double _bearingStressRatio;
        [ObservableProperty] private double _requiredBasePlateThicknessMm;
        [ObservableProperty] private double _anchorTensionKn;
        [ObservableProperty] private double _anchorTensionRatio;
        [ObservableProperty] private double _embedmentLengthMm;
        [ObservableProperty] private string _basePlateStatus = "PASS";
        [ObservableProperty] private string _basePlateSummary = string.Empty;

        // --- Tab 3: Gable Frame ---
        [ObservableProperty] private double _frameSpanMm = 24000.0;
        [ObservableProperty] private double _frameEaveHeightMm = 7000.0;
        [ObservableProperty] private double _frameSlopePercent = 10.0;
        [ObservableProperty] private double _frameBaySpacingMm = 6000.0;
        [ObservableProperty] private double _frameDeadLoadKnM2 = 0.15;
        [ObservableProperty] private double _frameLiveLoadKnM2 = 0.30;
        [ObservableProperty] private double _targetPurlinSpacingMm = 1350.0;

        [ObservableProperty] private double _apexHeightMm;
        [ObservableProperty] private double _rafterSlopeAngleDeg;
        [ObservableProperty] private double _actualPurlinSpacingMm;
        [ObservableProperty] private int _purlinsPerRafter;
        [ObservableProperty] private double _kneeMomentKnm;
        [ObservableProperty] private double _apexMomentKnm;
        [ObservableProperty] private double _kneeStressRatio;
        [ObservableProperty] private double _apexStressRatio;
        [ObservableProperty] private double _ridgeDeflectionMm;
        [ObservableProperty] private double _allowableDeflectionMm;
        [ObservableProperty] private string _gableFrameStatus = "PASS";

        public SteelConnectionViewModel()
        {
            CalculateMomentConnection();
            CalculateBasePlate();
            CalculateGableFrame();
        }

        [RelayCommand]
        public void CalculateMomentConnection()
        {
            var input = new SteelMomentConnectionInput
            {
                BeamDepthMm = BeamDepthMm,
                BeamFlangeWidthMm = BeamFlangeWidthMm,
                BeamFlangeThicknessMm = BeamFlangeThicknessMm,
                BeamWebThicknessMm = BeamWebThicknessMm,
                ColumnDepthMm = ColumnDepthMm,
                ColumnFlangeThicknessMm = ColumnFlangeThicknessMm,
                ColumnWebThicknessMm = ColumnWebThicknessMm,
                BendingMomentKnm = MomentKnm,
                ShearForceKn = ShearKn,
                AxialForceKn = AxialKn,
                EndPlateWidthMm = EndPlateWidthMm,
                EndPlateThicknessMm = EndPlateThicknessMm,
                EndPlateExtensionMm = EndPlateExtensionMm,
                BoltPitchTensionMm = BoltPitchMm,
                BoltDiameterMm = BoltDiameterMm,
                Grade = SelectedBoltGrade,
                TensionBoltRowCount = TensionBoltRows,
                CompressionBoltRowCount = CompressionBoltRows,
                FlangeWeldLegMm = FlangeWeldLegMm,
                WebWeldLegMm = WebWeldLegMm
            };

            var res = SteelMomentConnectionCalculator.Calculate(input);
            RequiredPlateThicknessMm = res.RequiredPlateThickness;
            BoltTensionRatio = res.BoltTensionRatio;
            BoltShearRatio = res.BoltShearRatio;
            BoltInteractionRatio = res.BoltInteractionRatio;
            WeldStressRatio = res.WeldStressRatio;
            StiffenersRequired = res.StiffenersRequired;
            StiffenerThicknessMm = res.StiffenerThickness;
            MomentConnectionStatus = res.IsCompliant ? "PASS" : "FAIL";
            MomentConnectionSummary = $"tp,req={res.RequiredPlateThickness:F1}mm (t={EndPlateThicknessMm:F1}mm), Bolt={res.BoltInteractionRatio:F2}, Weld={res.WeldStressRatio:F2}";
        }

        [RelayCommand]
        public void CalculateBasePlate()
        {
            var input = new AnchorBoltBasePlateInput
            {
                ColumnDepthMm = BaseColDepthMm,
                ColumnWidthMm = BaseColWidthMm,
                PlateLengthLMm = PlateLengthMm,
                PlateWidthBMm = PlateWidthMm,
                PlateThicknessMm = PlateThicknessMm,
                PedestalGrade = SelectedPedestalGrade,
                PedestalLengthMm = PedestalLengthMm,
                PedestalWidthMm = PedestalWidthMm,
                AxialForceKn = BaseAxialKn,
                BendingMomentKnm = BaseMomentKnm,
                ShearForceKn = BaseShearKn,
                AnchorBoltCount = AnchorBoltCount,
                AnchorBoltDiameterMm = AnchorBoltDiameterMm,
                BoltDistanceBetweenRowsMm = AnchorBoltDistanceMm
            };

            var res = AnchorBoltBasePlateCalculator.Calculate(input);
            BearingStressMpa = res.BearingStress;
            AllowableBearingMpa = res.AllowableBearingStress;
            BearingStressRatio = res.BearingStressRatio;
            RequiredBasePlateThicknessMm = res.RequiredPlateThickness;
            AnchorTensionKn = res.AnchorTensionForceKn;
            AnchorTensionRatio = res.AnchorTensionRatio;
            EmbedmentLengthMm = res.EmbedmentLength;
            BasePlateStatus = res.IsCompliant ? "PASS" : "FAIL";
            BasePlateSummary = $"sigma_c={res.BearingStress:F1}MPa (<= {res.AllowableBearingStress:F1}MPa), tp={res.RequiredPlateThickness:F1}mm, Lembed={res.EmbedmentLength:F0}mm";
        }

        [RelayCommand]
        public void CalculateGableFrame()
        {
            var input = new GableFrameInput
            {
                SpanMm = FrameSpanMm,
                EaveHeightMm = FrameEaveHeightMm,
                RoofSlopePercent = FrameSlopePercent,
                BaySpacingMm = FrameBaySpacingMm,
                DeadLoadKnM2 = FrameDeadLoadKnM2,
                LiveLoadKnM2 = FrameLiveLoadKnM2,
                TargetPurlinSpacingMm = TargetPurlinSpacingMm
            };

            var res = PreGableFrameCalculator.Calculate(input);
            ApexHeightMm = res.ApexHeightMm;
            RafterSlopeAngleDeg = res.RafterSlopeAngleDeg;
            ActualPurlinSpacingMm = res.ActualPurlinSpacingMm;
            PurlinsPerRafter = res.PurlinCountPerRafter;
            KneeMomentKnm = res.MaxKneeMomentKnm;
            ApexMomentKnm = res.MaxApexMomentKnm;
            KneeStressRatio = res.KneeStressRatio;
            ApexStressRatio = res.ApexStressRatio;
            RidgeDeflectionMm = res.EstimatedRidgeDeflectionMm;
            AllowableDeflectionMm = res.AllowableRidgeDeflectionMm;
            GableFrameStatus = res.IsCompliant ? "PASS" : "FAIL";
        }

        public CadDraftingBuffer? LastGeneratedBuffer { get; private set; }

        [RelayCommand]
        public void ExportCadDrafting()
        {
            var buffer = new CadDraftingBuffer();
            // Draw portal frame centerline
            buffer.AddLine(new Point2D(0, 0), new Point2D(0, FrameEaveHeightMm), "DCE_Steel_Frame", CadColor.CyanIndex, 0.5);
            buffer.AddLine(new Point2D(0, FrameEaveHeightMm), new Point2D(FrameSpanMm * 0.5, ApexHeightMm), "DCE_Steel_Frame", CadColor.CyanIndex, 0.5);
            buffer.AddLine(new Point2D(FrameSpanMm * 0.5, ApexHeightMm), new Point2D(FrameSpanMm, FrameEaveHeightMm), "DCE_Steel_Frame", CadColor.CyanIndex, 0.5);
            buffer.AddLine(new Point2D(FrameSpanMm, FrameEaveHeightMm), new Point2D(FrameSpanMm, 0), "DCE_Steel_Frame", CadColor.CyanIndex, 0.5);
            buffer.AddText($"PORTAL FRAME L={FrameSpanMm:F0} H={FrameEaveHeightMm:F0} (Apex={ApexHeightMm:F0})", new Point2D(0, -500), 200.0, "DCE_Steel_Text", CadColor.YellowIndex);
            LastGeneratedBuffer = buffer;
        }
    }
}
