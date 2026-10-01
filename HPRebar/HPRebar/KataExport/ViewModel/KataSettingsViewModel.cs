using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPRebar.Core.KataRebar.Models;
using HPRebar.KataRebar.Service;

namespace HPRebar.KataExport.ViewModel;

/// <summary>
/// The Kata detailing settings the add-in applies, edited in step with Kata's own "Detail thép" dialog.
/// Accept saves them for every later run; the caller re-plans the loaded sheet.
/// </summary>
public sealed partial class KataSettingsViewModel : ObservableObject
{
    private readonly Action<bool> _close;

    [ObservableProperty] private int _closedStirrupHookAngle;
    [ObservableProperty] private double _closedStirrupHookFactor;
    [ObservableProperty] private int _crossTieHookAngle;
    [ObservableProperty] private double _crossTieHookFactor;
    [ObservableProperty] private double _roundCutExtraMm;
    [ObservableProperty] private double _sideBarAnchorageFactor;
    [ObservableProperty] private double _sideBarTieSpacing;
    [ObservableProperty] private double _curtailedExtensionMm;
    [ObservableProperty] private double _denseZoneHeightFactor;
    [ObservableProperty] private double _endZoneFraction;
    [ObservableProperty] private double _bottomExtraCutFraction;
    [ObservableProperty] private double _minimumLegFactor;
    [ObservableProperty] private double _layerClearGap;
    [ObservableProperty] private double _roundLegMm;
    [ObservableProperty] private double _sideBarRequiredHeight;
    [ObservableProperty] private string _message = string.Empty;

    /// <summary>The settings were accepted but the file could not be written (they still apply to this session).</summary>
    public bool SaveFailed { get; private set; }

    public IReadOnlyList<int> HookAngleOptions { get; } = new[] { 90, 135, 180 };

    /// <param name="close">Called with true after the settings were saved, false on cancel.</param>
    public KataSettingsViewModel(KataSettings initial, Action<bool> close)
    {
        _close = close ?? throw new ArgumentNullException(nameof(close));
        Import(initial ?? KataSettings.Default);
    }

    [RelayCommand]
    private void Accept()
    {
        double[] all =
        {
            ClosedStirrupHookFactor, CrossTieHookFactor, RoundCutExtraMm, SideBarAnchorageFactor, SideBarTieSpacing,
            CurtailedExtensionMm, DenseZoneHeightFactor, EndZoneFraction, BottomExtraCutFraction, MinimumLegFactor, LayerClearGap,
            RoundLegMm, SideBarRequiredHeight
        };
        if (Array.Exists(all, v => double.IsNaN(v) || double.IsInfinity(v)))
        {
            Message = "Có ô không phải số hữu hạn.";
            return;
        }

        if (ClosedStirrupHookFactor <= 0.0 || CrossTieHookFactor <= 0.0 || RoundCutExtraMm < 0.0
            || SideBarAnchorageFactor <= 0.0 || SideBarTieSpacing <= 0.0 || CurtailedExtensionMm < 0.0
            || DenseZoneHeightFactor < 0.0 || EndZoneFraction < 0.0 || EndZoneFraction > 0.5
            || BottomExtraCutFraction < 0.0 || BottomExtraCutFraction >= 0.5
            || MinimumLegFactor <= 0.0 || LayerClearGap < 0.0 || RoundLegMm < 0.0 || SideBarRequiredHeight < 0.0)
        {
            Message = "Các giá trị phải hợp lệ: tỉ lệ vùng đai dày 0 … 0.5, tỉ lệ cắt gia cường bụng 0 … < 0.5 (× Ln); cắt lệch, làm tròn, khe lớp và h cốt giá không âm (0 = tắt).";
            return;
        }

        var settings = new KataSettings
        {
            ClosedStirrupHookAngle = ClosedStirrupHookAngle,
            ClosedStirrupHookFactor = ClosedStirrupHookFactor,
            CrossTieHookAngle = CrossTieHookAngle,
            CrossTieHookFactor = CrossTieHookFactor,
            RoundCutExtraMm = RoundCutExtraMm,
            SideBarAnchorageFactor = SideBarAnchorageFactor,
            SideBarTieSpacing = SideBarTieSpacing,
            CurtailedExtensionMm = CurtailedExtensionMm,
            DenseZoneHeightFactor = DenseZoneHeightFactor,
            EndZoneFraction = EndZoneFraction,
            BottomExtraCutFraction = BottomExtraCutFraction,
            MinimumLegFactor = MinimumLegFactor,
            LayerClearGap = LayerClearGap,
            RoundLegMm = RoundLegMm,
            SideBarRequiredHeight = SideBarRequiredHeight
        };

        SaveFailed = !KataSettingsStore.Save(settings);
        _close(true);
    }

    [RelayCommand]
    private void Cancel() => _close(false);

    [RelayCommand]
    private void ResetDefault() => Import(KataSettings.Default);

    private void Import(KataSettings s)
    {
        ClosedStirrupHookAngle = s.ClosedStirrupHookAngle;
        ClosedStirrupHookFactor = s.ClosedStirrupHookFactor;
        CrossTieHookAngle = s.CrossTieHookAngle;
        CrossTieHookFactor = s.CrossTieHookFactor;
        RoundCutExtraMm = s.RoundCutExtraMm;
        SideBarAnchorageFactor = s.SideBarAnchorageFactor;
        SideBarTieSpacing = s.SideBarTieSpacing;
        CurtailedExtensionMm = s.CurtailedExtensionMm;
        DenseZoneHeightFactor = s.DenseZoneHeightFactor;
        EndZoneFraction = s.EndZoneFraction;
        BottomExtraCutFraction = s.BottomExtraCutFraction;
        MinimumLegFactor = s.MinimumLegFactor;
        LayerClearGap = s.LayerClearGap;
        RoundLegMm = s.RoundLegMm;
        SideBarRequiredHeight = s.SideBarRequiredHeight;
        Message = string.Empty;
    }
}
