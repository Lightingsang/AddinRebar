using CommunityToolkit.Mvvm.ComponentModel;
using HPKata.Core.Configuration;

namespace HPKata.UI.ViewModels
{
    public sealed partial class SetupViewModel
    {
        // Category 4: Rebar & Materials
        [ObservableProperty] private double _concreteCoverMm = 30.0;
        [ObservableProperty] private int _baseBarDiameter = 16;
        [ObservableProperty] private int _drawFilletRadius = 1;
        [ObservableProperty] private int _applyBendDeduction;
        [ObservableProperty] private int _autoRenumberMarks = 1;
        [ObservableProperty] private int _lengthRoundingMode;
        [ObservableProperty] private int _drawCenterlines;
        [ObservableProperty] private string _reservedField54 = string.Empty;
        [ObservableProperty] private string _reservedField56 = string.Empty;
        [ObservableProperty] private double _cutLengthRoundStep = 50.0;

        // Category 5: Spacing & Lap Splice Rules
        [ObservableProperty] private double _lapLengthMultiplier = 30.0;
        [ObservableProperty] private int _staggeredLapMode = 1;
        [ObservableProperty] private double _tensionAnchorMult = 40.0;
        [ObservableProperty] private double _colSpliceRatio = 1.0 / 6.0;
        [ObservableProperty] private double _beamTopCutoffL1 = 0.2;
        [ObservableProperty] private string _beamCutoffDatumL1 = " mép";
        [ObservableProperty] private double _beamBotCutoff;
        [ObservableProperty] private int _denseZoneLayout;
        [ObservableProperty] private double _beamTopCutoffL2 = 0.25;
        [ObservableProperty] private string _beamCutoffDatumL2 = " mép";
        [ObservableProperty] private int _secondaryBarLength = 8800;
        [ObservableProperty] private double _staggeredOffsetMm = 50.0;

        // Category 6: Hooks & Secondary Reinforcement
        [ObservableProperty] private string _defaultHookAngle = "135/180";
        [ObservableProperty] private int _drawElevationHooks = 1;
        [ObservableProperty] private string _stirrupClosureType = "Closed";
        [ObservableProperty] private double _footingHookMult;
        [ObservableProperty] private double _slabHookMult;
        [ObservableProperty] private double _wallHookMult;
        [ObservableProperty] private string _secBeamStirrupAdd1 = "5f10a50";
        [ObservableProperty] private int _secBeamGroup1 = 1;
        [ObservableProperty] private string _secBeamAntiCrack1 = "2f14";
        [ObservableProperty] private double _secBeamZoneLen1 = 150.0;
        [ObservableProperty] private string _secBeamStirrupAdd2 = "5f10a50";
        [ObservableProperty] private int _secBeamGroup2 = 1;
        [ObservableProperty] private string _secBeamAntiCrack2 = "2f14";
        [ObservableProperty] private double _secBeamZoneLen2 = 150.0;

        // Category 7: Scheduling, Couplers & Fabrication
        [ObservableProperty] private int _groupSameRebar = 1;
        [ObservableProperty] private string _scheduleDiaPrefix = "Ø";
        [ObservableProperty] private int _standardBarLength = 11700;
        [ObservableProperty] private int _minRemnantLength = 100;
        [ObservableProperty] private int _couplerMinDia;
        [ObservableProperty] private string _varBarNaming = "1a, 1b...";
        [ObservableProperty] private int _drawShapeInSched = 1;
        [ObservableProperty] private double _scheduleRowHeight = 7.5;
        [ObservableProperty] private double _schedShapeColWidth = 25.0;
        [ObservableProperty] private string _reservedField47 = string.Empty;
        [ObservableProperty] private int _schedExportFormat;
        [ObservableProperty] private int _couplerSpec;
        [ObservableProperty] private string _tableTemplate = "Default";
        [ObservableProperty] private string _reservedField61 = string.Empty;
        [ObservableProperty] private string _reservedField63 = string.Empty;
        [ObservableProperty] private int _massUnitDisplay;
        [ObservableProperty] private int _totalLengthRound;
        [ObservableProperty] private int _couplerSepSchedule;
        [ObservableProperty] private string _couplerEndTreat1 = "None";
        [ObservableProperty] private string _couplerEndTreat2 = "None";
        [ObservableProperty] private int _printQrCodeOnTag;
        [ObservableProperty] private int _qsFormulaMode;

        public void LoadFromConfig(KataConfig config)
        {
            _isInitializing = true;
            try
            {
                LoadDrawingLayers(config);
                LoadRebarRuleSettings(config);
                LoadCuttingStockSettings(config);
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private void LoadRebarRuleSettings(KataConfig config)
        {
            ConcreteCoverMm = config.ConcreteCoverMm;
            BaseBarDiameter = config.BaseBarDiameter;
            DrawFilletRadius = config.DrawFilletRadius;
            ApplyBendDeduction = config.ApplyBendDeduction;
            AutoRenumberMarks = config.AutoRenumberMarks;
            LengthRoundingMode = config.LengthRoundingMode;
            DrawCenterlines = config.DrawCenterlines;
            ReservedField54 = config.ReservedField54;
            ReservedField56 = config.ReservedField56;
            CutLengthRoundStep = config.CutLengthRoundStep;

            LapLengthMultiplier = config.LapLengthMultiplier;
            StaggeredLapMode = config.StaggeredLapMode;
            TensionAnchorMult = config.TensionAnchorMult;
            ColSpliceRatio = config.ColSpliceRatio;
            BeamTopCutoffL1 = config.BeamTopCutoffL1;
            BeamCutoffDatumL1 = config.BeamCutoffDatumL1;
            BeamBotCutoff = config.BeamBotCutoff;
            DenseZoneLayout = config.DenseZoneLayout;
            BeamTopCutoffL2 = config.BeamTopCutoffL2;
            BeamCutoffDatumL2 = config.BeamCutoffDatumL2;
            SecondaryBarLength = config.SecondaryBarLength;
            StaggeredOffsetMm = config.StaggeredOffsetMm;
        }

        private void LoadCuttingStockSettings(KataConfig config)
        {
            DefaultHookAngle = config.DefaultHookAngle;
            DrawElevationHooks = config.DrawElevationHooks;
            StirrupClosureType = config.StirrupClosureType;
            FootingHookMult = config.FootingHookMult;
            SlabHookMult = config.SlabHookMult;
            WallHookMult = config.WallHookMult;
            SecBeamStirrupAdd1 = config.SecBeamStirrupAdd1;
            SecBeamGroup1 = config.SecBeamGroup1;
            SecBeamAntiCrack1 = config.SecBeamAntiCrack1;
            SecBeamZoneLen1 = config.SecBeamZoneLen1;
            SecBeamStirrupAdd2 = config.SecBeamStirrupAdd2;
            SecBeamGroup2 = config.SecBeamGroup2;
            SecBeamAntiCrack2 = config.SecBeamAntiCrack2;
            SecBeamZoneLen2 = config.SecBeamZoneLen2;

            GroupSameRebar = config.GroupSameRebar;
            ScheduleDiaPrefix = config.ScheduleDiaPrefix;
            StandardBarLength = config.StandardBarLength;
            MinRemnantLength = config.MinRemnantLength;
            CouplerMinDia = config.CouplerMinDia;
            VarBarNaming = config.VarBarNaming;
            DrawShapeInSched = config.DrawShapeInSched;
            ScheduleRowHeight = config.ScheduleRowHeight;
            SchedShapeColWidth = config.SchedShapeColWidth;
            ReservedField47 = config.ReservedField47;
            SchedExportFormat = config.SchedExportFormat;
            CouplerSpec = config.CouplerSpec;
            TableTemplate = config.TableTemplate;
            ReservedField61 = config.ReservedField61;
            ReservedField63 = config.ReservedField63;
            MassUnitDisplay = config.MassUnitDisplay;
            TotalLengthRound = config.TotalLengthRound;
            CouplerSepSchedule = config.CouplerSepSchedule;
            CouplerEndTreat1 = config.CouplerEndTreat1;
            CouplerEndTreat2 = config.CouplerEndTreat2;
            PrintQrCodeOnTag = config.PrintQrCodeOnTag;
            QsFormulaMode = config.QsFormulaMode;
        }

        public KataConfig ToConfig()
        {
            var config = new KataConfig();
            SaveDrawingLayers(config);
            ApplyRebarRuleSettings(config);
            ApplyCuttingStockSettings(config);
            return config;
        }

        private void ApplyRebarRuleSettings(KataConfig config)
        {
            config.ConcreteCoverMm = ConcreteCoverMm;
            config.BaseBarDiameter = BaseBarDiameter;
            config.DrawFilletRadius = DrawFilletRadius;
            config.ApplyBendDeduction = ApplyBendDeduction;
            config.AutoRenumberMarks = AutoRenumberMarks;
            config.LengthRoundingMode = LengthRoundingMode;
            config.DrawCenterlines = DrawCenterlines;
            config.ReservedField54 = ReservedField54;
            config.ReservedField56 = ReservedField56;
            config.CutLengthRoundStep = CutLengthRoundStep;

            config.LapLengthMultiplier = LapLengthMultiplier;
            config.StaggeredLapMode = StaggeredLapMode;
            config.TensionAnchorMult = TensionAnchorMult;
            config.ColSpliceRatio = ColSpliceRatio;
            config.BeamTopCutoffL1 = BeamTopCutoffL1;
            config.BeamCutoffDatumL1 = BeamCutoffDatumL1;
            config.BeamBotCutoff = BeamBotCutoff;
            config.DenseZoneLayout = DenseZoneLayout;
            config.BeamTopCutoffL2 = BeamTopCutoffL2;
            config.BeamCutoffDatumL2 = BeamCutoffDatumL2;
            config.SecondaryBarLength = SecondaryBarLength;
            config.StaggeredOffsetMm = StaggeredOffsetMm;
        }

        private void ApplyCuttingStockSettings(KataConfig config)
        {
            config.DefaultHookAngle = DefaultHookAngle;
            config.DrawElevationHooks = DrawElevationHooks;
            config.StirrupClosureType = StirrupClosureType;
            config.FootingHookMult = FootingHookMult;
            config.SlabHookMult = SlabHookMult;
            config.WallHookMult = WallHookMult;
            config.SecBeamStirrupAdd1 = SecBeamStirrupAdd1;
            config.SecBeamGroup1 = SecBeamGroup1;
            config.SecBeamAntiCrack1 = SecBeamAntiCrack1;
            config.SecBeamZoneLen1 = SecBeamZoneLen1;
            config.SecBeamStirrupAdd2 = SecBeamStirrupAdd2;
            config.SecBeamGroup2 = SecBeamGroup2;
            config.SecBeamAntiCrack2 = SecBeamAntiCrack2;
            config.SecBeamZoneLen2 = SecBeamZoneLen2;

            config.GroupSameRebar = GroupSameRebar;
            config.ScheduleDiaPrefix = ScheduleDiaPrefix;
            config.StandardBarLength = StandardBarLength;
            config.MinRemnantLength = MinRemnantLength;
            config.CouplerMinDia = CouplerMinDia;
            config.VarBarNaming = VarBarNaming;
            config.DrawShapeInSched = DrawShapeInSched;
            config.ScheduleRowHeight = ScheduleRowHeight;
            config.SchedShapeColWidth = SchedShapeColWidth;
            config.ReservedField47 = ReservedField47;
            config.SchedExportFormat = SchedExportFormat;
            config.CouplerSpec = CouplerSpec;
            config.TableTemplate = TableTemplate;
            config.ReservedField61 = ReservedField61;
            config.ReservedField63 = ReservedField63;
            config.MassUnitDisplay = MassUnitDisplay;
            config.TotalLengthRound = TotalLengthRound;
            config.CouplerSepSchedule = CouplerSepSchedule;
            config.CouplerEndTreat1 = CouplerEndTreat1;
            config.CouplerEndTreat2 = CouplerEndTreat2;
            config.PrintQrCodeOnTag = PrintQrCodeOnTag;
            config.QsFormulaMode = QsFormulaMode;
        }
    }
}
