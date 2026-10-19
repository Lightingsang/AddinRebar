namespace HPKata.Core.Configuration
{
    /// <summary>
    /// Strongly-typed model for the 86 KataPro configuration parameters defined in Goi lenh.lsp (lines 152-237).
    /// </summary>
    public class KataConfig
    {
        // 1-11: CAD Layers
        public string LayerDim { get; set; } = "kata_dim";
        public string LayerNetThay { get; set; } = "kata_net thay";
        public string LayerNetKhung { get; set; } = "kata_net khung";
        public string LayerNetKhuat { get; set; } = "kata_net khuat";
        public string LayerGrid { get; set; } = "kata_grid";
        public string LayerThepChu { get; set; } = "kata_thep chu";
        public string LayerThepDai { get; set; } = "kata_thep dai";
        public string LayerThepDaiPhu { get; set; } = "kata_thep dai";
        public string LayerNetManh { get; set; } = "kata_net manh";
        public string LayerText { get; set; } = "kata_text";
        public string LayerTextTag { get; set; } = "kata_text";

        // 12-15: Notations and DimStyle
        public string SpacingPrefix { get; set; } = "a";
        public string SmallBarPrefix { get; set; } = "Ø";
        public string DeformedBarPrefix { get; set; } = "Ø";
        public string DimStyleName { get; set; } = "kata_dim_iso";

        // 16-25: Rebar detailing rules
        public double ConcreteCoverMm { get; set; } = 30.0;
        public string DefaultHookAngle { get; set; } = "135/180";
        public double DetailDrawingScale { get; set; } = 0.04; // 1/25
        public int BaseBarDiameter { get; set; } = 16;
        public double LapLengthMultiplier { get; set; } = 30.0;
        public int StaggeredLapMode { get; set; } = 1;
        public int DrawFilletRadius { get; set; } = 1;
        public int GroupSameRebar { get; set; } = 1;
        public int ApplyBendDeduction { get; set; } = 0;
        public int AutoRenumberMarks { get; set; } = 1;

        // 26-34: Splice, hooks, and stock lengths
        public double TensionAnchorMult { get; set; } = 40.0;
        public double ColSpliceRatio { get; set; } = 1.0 / 6.0;
        public int DrawElevationHooks { get; set; } = 1;
        public int DimStyleOverride { get; set; } = 0;
        public int TagStyle { get; set; } = 0;
        public string ScheduleDiaPrefix { get; set; } = "Ø";
        public int StandardBarLength { get; set; } = 11700;
        public int MinRemnantLength { get; set; } = 100;
        public int CouplerMinDia { get; set; } = 0;

        // 35-46: Beam cutoffs and schedule display
        public double BeamTopCutoffL1 { get; set; } = 0.2;
        public string BeamCutoffDatumL1 { get; set; } = " mép";
        public double BeamBotCutoff { get; set; } = 0.0;
        public int DenseZoneLayout { get; set; } = 0;
        public int LengthRoundingMode { get; set; } = 0;
        public string VarBarNaming { get; set; } = "1a, 1b...";
        public int DrawShapeInSched { get; set; } = 1;
        public double AnnotationTextH { get; set; } = 5.0;
        public int DrawCenterlines { get; set; } = 0;
        public string TagSeparator { get; set; } = "-";
        public double ScheduleRowHeight { get; set; } = 7.5;
        public double SchedShapeColWidth { get; set; } = 25.0;

        // 47-56: Secondary lengths, templates, locale
        public string ReservedField47 { get; set; } = string.Empty;
        public double BeamTopCutoffL2 { get; set; } = 0.25;
        public string BeamCutoffDatumL2 { get; set; } = " mép";
        public int SecondaryBarLength { get; set; } = 8800;
        public string LanguageUiSched { get; set; } = "Tiếng Việt";
        public int SchedExportFormat { get; set; } = 0;
        public int CouplerSpec { get; set; } = 0;
        public string ReservedField54 { get; set; } = string.Empty;
        public string TableTemplate { get; set; } = "Default";
        public string ReservedField56 { get; set; } = string.Empty;

        // 57-68: Hook multipliers, units, rounding
        public string StirrupClosureType { get; set; } = "Closed";
        public double FootingHookMult { get; set; } = 0.0;
        public double SlabHookMult { get; set; } = 0.0;
        public double WallHookMult { get; set; } = 0.0;
        public string ReservedField61 { get; set; } = string.Empty;
        public int TitleScaleMode { get; set; } = 0;
        public string ReservedField63 { get; set; } = string.Empty;
        public int AutoDimRebar { get; set; } = 0;
        public string LengthUnitDisplay { get; set; } = "mm";
        public double CutLengthRoundStep { get; set; } = 50.0;
        public int MassUnitDisplay { get; set; } = 0;
        public int TotalLengthRound { get; set; } = 0;

        // 69-78: Leaders and secondary beam reinforcement
        public int DrawLeaderArrow { get; set; } = 1;
        public int LeaderArrowType { get; set; } = 0;
        public string SecBeamStirrupAdd1 { get; set; } = "5f10a50";
        public int SecBeamGroup1 { get; set; } = 1;
        public string SecBeamAntiCrack1 { get; set; } = "2f14";
        public double SecBeamZoneLen1 { get; set; } = 150.0;
        public string SecBeamStirrupAdd2 { get; set; } = "5f10a50";
        public int SecBeamGroup2 { get; set; } = 1;
        public string SecBeamAntiCrack2 { get; set; } = "2f14";
        public double SecBeamZoneLen2 { get; set; } = 150.0;

        // 79-86: Tags, couplers, and sync
        public double TagCircleRadius { get; set; } = 10.0;
        public double StaggeredOffsetMm { get; set; } = 50.0;
        public int CouplerSepSchedule { get; set; } = 0;
        public string CouplerEndTreat1 { get; set; } = "None";
        public string CouplerEndTreat2 { get; set; } = "None";
        public int PrintQrCodeOnTag { get; set; } = 0;
        public int RealtimeSync { get; set; } = 0;
        public int QsFormulaMode { get; set; } = 0;
    }
}
