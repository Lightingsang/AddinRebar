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

    [ObservableProperty] private double _maxBarLength;
    [ObservableProperty] private int _closedStirrupHookAngle;
    [ObservableProperty] private double _closedStirrupHookFactor;
    [ObservableProperty] private int _crossTieHookAngle;
    [ObservableProperty] private double _crossTieHookFactor;
    [ObservableProperty] private double _roundCutExtraMm;
    [ObservableProperty] private double _sideBarAnchorageFactor;
    [ObservableProperty] private double _sideBarTieSpacing;
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
        if (MaxBarLength <= 0.0 || ClosedStirrupHookFactor <= 0.0 || CrossTieHookFactor <= 0.0 || RoundCutExtraMm < 0.0
            || SideBarAnchorageFactor <= 0.0 || SideBarTieSpacing <= 0.0)
        {
            Message = "Các giá trị phải dương (làm tròn có thể là 0 = không làm tròn).";
            return;
        }

        var settings = new KataSettings
        {
            MaxBarLength = MaxBarLength,
            ClosedStirrupHookAngle = ClosedStirrupHookAngle,
            ClosedStirrupHookFactor = ClosedStirrupHookFactor,
            CrossTieHookAngle = CrossTieHookAngle,
            CrossTieHookFactor = CrossTieHookFactor,
            RoundCutExtraMm = RoundCutExtraMm,
            SideBarAnchorageFactor = SideBarAnchorageFactor,
            SideBarTieSpacing = SideBarTieSpacing
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
        MaxBarLength = s.MaxBarLength;
        ClosedStirrupHookAngle = s.ClosedStirrupHookAngle;
        ClosedStirrupHookFactor = s.ClosedStirrupHookFactor;
        CrossTieHookAngle = s.CrossTieHookAngle;
        CrossTieHookFactor = s.CrossTieHookFactor;
        RoundCutExtraMm = s.RoundCutExtraMm;
        SideBarAnchorageFactor = s.SideBarAnchorageFactor;
        SideBarTieSpacing = s.SideBarTieSpacing;
        Message = string.Empty;
    }
}
