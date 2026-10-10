using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.KataExport.ViewModel.Tabs;

/// <summary>
/// Tab "Detail thép" of the settings dialog, laid out as Kata's: the settings the bars are drawn with, the beam options
/// kept for later ("chưa áp dụng") and the bar length / lap settings of the shop drawings.
/// </summary>
public sealed partial class KataDetailSettingsTabViewModel : ObservableObject
{
    // Drawn with.
    [ObservableProperty] private int _closedStirrupHookAngle;
    [ObservableProperty] private double _closedStirrupHookFactor;
    [ObservableProperty] private int _crossTieHookAngle;
    [ObservableProperty] private double _crossTieHookFactor;
    [ObservableProperty] private double _roundCutExtraMm;
    [ObservableProperty] private double _sideBarAnchorageFactor;
    [ObservableProperty] private int _layerTieMinBarCount;
    [ObservableProperty] private double _crankMinDiameter;
    [ObservableProperty] private double _crankSlope;
    [ObservableProperty] private double _curtailedExtensionMm;
    [ObservableProperty] private double _denseZoneHeightFactor;
    [ObservableProperty] private double _endZoneFraction;
    [ObservableProperty] private double _bottomExtraCutFraction;
    [ObservableProperty] private double _minimumLegFactor;
    [ObservableProperty] private double _layerClearGap;
    [ObservableProperty] private double _roundLegMm;
    [ObservableProperty] private double _sideBarRequiredHeight;
    [ObservableProperty] private bool _showBarEndMarks;
    [ObservableProperty] private bool _createKataDrawings;

    // Beam options kept for later.
    [ObservableProperty] private bool _bottomLayerNoBend;
    [ObservableProperty] private bool _topNotAnchoredIntoColumnBelow;
    [ObservableProperty] private bool _alwaysBend;
    [ObservableProperty] private double _alwaysBendFactor;
    [ObservableProperty] private bool _cutContinuousBarsAtSpanEnds;
    [ObservableProperty] private bool _differentNumbersForIdenticalBars;

    // Shop drawings.
    [ObservableProperty] private double _couplerMinDiameter;
    [ObservableProperty] private double _maxBarLengthMm;
    [ObservableProperty] private double _minLengthForCuttingMm;
    [ObservableProperty] private double _minBarLengthFactor;
    [ObservableProperty] private double _roundLapMm;
    [ObservableProperty] private bool _topLapAtMidspan;
    [ObservableProperty] private bool _topLapAtSupport;
    [ObservableProperty] private bool _bottomLapAtSupport;
    [ObservableProperty] private bool _bottomLapAtMidspan;
    [ObservableProperty] private double _topLapZoneFraction;
    [ObservableProperty] private int _topLapZoneOrigin;
    [ObservableProperty] private double _bottomLapZoneFraction;
    [ObservableProperty] private int _bottomLapZoneOrigin;
    [ObservableProperty] private bool _preferFewerLaps;

    public IReadOnlyList<int> HookAngleOptions { get; } = new[] { 90, 135, 180 };

    public IReadOnlyList<double> CrankMinDiameterOptions => KataSettingsChoices.CrankMinDiameters;

    public IReadOnlyList<double> CrankSlopeOptions => KataSettingsChoices.CrankSlopes;

    public IReadOnlyList<double> CouplerMinDiameterOptions => KataSettingsChoices.CouplerMinDiameters;

    /// <summary>Set by <see cref="Import"/> when a saved combo value was outside its list and the default is shown instead.</summary>
    public string? ImportNotice { get; private set; }

    /// <summary>Where a lap zone is measured from: index 0 = support face ("mép"), 1 = support centre ("tâm").</summary>
    public IReadOnlyList<string> LapOriginOptions { get; } = new[] { "mép", "tâm" };

    public void Import(KataSettingsFile file)
    {
        var s = file.Drawing;
        var d = KataSettings.Default;
        var replaced = new List<string>();
        ClosedStirrupHookAngle = s.ClosedStirrupHookAngle;
        ClosedStirrupHookFactor = s.ClosedStirrupHookFactor;
        CrossTieHookAngle = s.CrossTieHookAngle;
        CrossTieHookFactor = s.CrossTieHookFactor;
        RoundCutExtraMm = s.RoundCutExtraMm;
        SideBarAnchorageFactor = s.SideBarAnchorageFactor;
        LayerTieMinBarCount = s.LayerTieMinBarCount;
        CrankMinDiameter = KataSettingsChoices.Pick(KataSettingsChoices.CrankMinDiameters, s.CrankMinDiameter, d.CrankMinDiameter, out bool crankOk);
        if (!crankOk) replaced.Add($"Ø bẻ cổ chai {s.CrankMinDiameter:0.##}");
        CrankSlope = KataSettingsChoices.Pick(KataSettingsChoices.CrankSlopes, s.CrankSlope, d.CrankSlope, out bool slopeOk);
        if (!slopeOk) replaced.Add($"tỷ lệ cổ chai 1/{s.CrankSlope:0.##}");
        CurtailedExtensionMm = s.CurtailedExtensionMm;
        DenseZoneHeightFactor = s.DenseZoneHeightFactor;
        EndZoneFraction = s.EndZoneFraction;
        BottomExtraCutFraction = s.BottomExtraCutFraction;
        MinimumLegFactor = s.MinimumLegFactor;
        LayerClearGap = s.LayerClearGap;
        RoundLegMm = s.RoundLegMm;
        SideBarRequiredHeight = s.SideBarRequiredHeight;
        ShowBarEndMarks = s.ShowBarEndMarks;
        CreateKataDrawings = s.CreateKataDrawings;

        var p = file.Pending;
        BottomLayerNoBend = p.BottomLayerNoBend;
        TopNotAnchoredIntoColumnBelow = p.TopNotAnchoredIntoColumnBelow;
        AlwaysBend = p.AlwaysBend;
        AlwaysBendFactor = p.AlwaysBendFactor;
        CutContinuousBarsAtSpanEnds = p.CutContinuousBarsAtSpanEnds;
        DifferentNumbersForIdenticalBars = p.DifferentNumbersForIdenticalBars;

        var shop = file.Shop;
        CouplerMinDiameter = KataSettingsChoices.Pick(KataSettingsChoices.CouplerMinDiameters, shop.CouplerMinDiameter,
            KataShopSettings.Default.CouplerMinDiameter, out bool couplerOk);
        if (!couplerOk) replaced.Add($"Ø coupler {shop.CouplerMinDiameter:0.##}");
        MaxBarLengthMm = shop.MaxBarLengthMm;
        MinLengthForCuttingMm = shop.MinLengthForCuttingMm;
        MinBarLengthFactor = shop.MinBarLengthFactor;
        RoundLapMm = shop.RoundLapMm;
        TopLapAtMidspan = shop.TopLapAtMidspan;
        TopLapAtSupport = shop.TopLapAtSupport;
        BottomLapAtSupport = shop.BottomLapAtSupport;
        BottomLapAtMidspan = shop.BottomLapAtMidspan;
        TopLapZoneFraction = shop.TopLapZoneFraction;
        TopLapZoneOrigin = shop.TopLapZoneFromCentre ? 1 : 0;
        BottomLapZoneFraction = shop.BottomLapZoneFraction;
        BottomLapZoneOrigin = shop.BottomLapZoneFromCentre ? 1 : 0;
        PreferFewerLaps = shop.PreferFewerLaps;

        ImportNotice = replaced.Count == 0
            ? null
            : $"File thiết lập có {string.Join(", ", replaced)} ngoài danh sách: hộp thoại hiện mặc định; thép vẫn vẽ theo giá trị cũ tới khi bấm Chấp nhận.";
    }

    /// <summary>What is wrong with the tab, or null.</summary>
    public string? Validate()
    {
        double[] all =
        {
            ClosedStirrupHookFactor, CrossTieHookFactor, RoundCutExtraMm, SideBarAnchorageFactor, CrankMinDiameter, CrankSlope,
            CurtailedExtensionMm, DenseZoneHeightFactor, EndZoneFraction, BottomExtraCutFraction, MinimumLegFactor, LayerClearGap,
            RoundLegMm, SideBarRequiredHeight, AlwaysBendFactor, CouplerMinDiameter, MaxBarLengthMm, MinLengthForCuttingMm,
            MinBarLengthFactor, RoundLapMm, TopLapZoneFraction, BottomLapZoneFraction
        };
        if (Array.Exists(all, v => double.IsNaN(v) || double.IsInfinity(v)))
            return "Detail thép: có ô không phải số hữu hạn.";

        if (ClosedStirrupHookFactor <= 0.0 || CrossTieHookFactor <= 0.0 || RoundCutExtraMm < 0.0
            || SideBarAnchorageFactor <= 0.0 || LayerTieMinBarCount < 2 || CrankMinDiameter < 0.0 || CrankSlope <= 0.0 || CurtailedExtensionMm < 0.0
            || DenseZoneHeightFactor < 0.0 || EndZoneFraction < 0.0 || EndZoneFraction > 0.5
            || BottomExtraCutFraction < 0.0 || BottomExtraCutFraction >= 0.5
            || MinimumLegFactor < 0.0 || LayerClearGap < 0.0 || RoundLegMm < 0.0 || SideBarRequiredHeight < 0.0)
            return "Detail thép: tỉ lệ vùng đai dày 0 … 0.5, tỉ lệ cắt gia cường bụng 0 … < 0.5 (× Ln); cắt lệch, làm tròn, khe lớp và h cốt giá không âm (0 = tắt); thanh C kê từ 2 thanh trở lên; Ø bẻ cổ chai không âm; tỷ lệ nhấn cổ chai 1/n với n dương.";

        if (AlwaysBendFactor < 0.0 || CouplerMinDiameter < 0.0 || MaxBarLengthMm <= 0.0 || MinLengthForCuttingMm < 0.0
            || MinBarLengthFactor < 0.0 || RoundLapMm < 0.0
            || TopLapZoneFraction < 0.0 || TopLapZoneFraction > 0.5 || BottomLapZoneFraction < 0.0 || BottomLapZoneFraction > 0.5)
            return "Detail thép: chiều dài thép tối đa phải dương, vùng nối 0 … 0.5 L, các ô còn lại không âm.";

        return null;
    }

    /// <summary>The drawing settings of this tab over <paramref name="start"/> (the joint defaults come from their own tab).</summary>
    public KataSettings ExportDrawing(KataSettings start) => start with
    {
        ClosedStirrupHookAngle = ClosedStirrupHookAngle,
        ClosedStirrupHookFactor = ClosedStirrupHookFactor,
        CrossTieHookAngle = CrossTieHookAngle,
        CrossTieHookFactor = CrossTieHookFactor,
        RoundCutExtraMm = RoundCutExtraMm,
        SideBarAnchorageFactor = SideBarAnchorageFactor,
        LayerTieMinBarCount = LayerTieMinBarCount,
        CrankMinDiameter = CrankMinDiameter,
        CrankSlope = CrankSlope,
        CurtailedExtensionMm = CurtailedExtensionMm,
        DenseZoneHeightFactor = DenseZoneHeightFactor,
        EndZoneFraction = EndZoneFraction,
        BottomExtraCutFraction = BottomExtraCutFraction,
        MinimumLegFactor = MinimumLegFactor,
        LayerClearGap = LayerClearGap,
        RoundLegMm = RoundLegMm,
        SideBarRequiredHeight = SideBarRequiredHeight,
        ShowBarEndMarks = ShowBarEndMarks,
        CreateKataDrawings = CreateKataDrawings
    };

    public KataBeamOptions ExportPending() => new()
    {
        BottomLayerNoBend = BottomLayerNoBend,
        TopNotAnchoredIntoColumnBelow = TopNotAnchoredIntoColumnBelow,
        AlwaysBend = AlwaysBend,
        AlwaysBendFactor = AlwaysBendFactor,
        CutContinuousBarsAtSpanEnds = CutContinuousBarsAtSpanEnds,
        DifferentNumbersForIdenticalBars = DifferentNumbersForIdenticalBars
    };

    /// <summary>The shop settings of this tab over <paramref name="start"/> (laps and tables come from "Thông số đặc thù").</summary>
    public KataShopSettings ExportShop(KataShopSettings start) => start with
    {
        CouplerMinDiameter = CouplerMinDiameter,
        MaxBarLengthMm = MaxBarLengthMm,
        MinLengthForCuttingMm = MinLengthForCuttingMm,
        MinBarLengthFactor = MinBarLengthFactor,
        RoundLapMm = RoundLapMm,
        TopLapAtMidspan = TopLapAtMidspan,
        TopLapAtSupport = TopLapAtSupport,
        BottomLapAtSupport = BottomLapAtSupport,
        BottomLapAtMidspan = BottomLapAtMidspan,
        TopLapZoneFraction = TopLapZoneFraction,
        TopLapZoneFromCentre = TopLapZoneOrigin == 1,
        BottomLapZoneFraction = BottomLapZoneFraction,
        BottomLapZoneFromCentre = BottomLapZoneOrigin == 1,
        PreferFewerLaps = PreferFewerLaps
    };
}
