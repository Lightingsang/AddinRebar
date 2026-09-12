using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using HPRebar.Core.FoundationRebar.Models;

namespace HPRebar.FoundationRebar.ViewModel;

/// <summary>
/// ViewModel binding rebar configuration inputs (diameters, spacings, covers, top mat toggle, hooks).
/// </summary>
public sealed partial class FoundationSettingViewModel : ObservableObject
{
    [ObservableProperty]
    private double _diameterBottomX = 16.0;

    [ObservableProperty]
    private double _diameterBottomY = 16.0;

    [ObservableProperty]
    private double _diameterTopX = 12.0;

    [ObservableProperty]
    private double _diameterTopY = 12.0;

    [ObservableProperty]
    private double _spacingBottomX = 150.0;

    [ObservableProperty]
    private double _spacingBottomY = 150.0;

    [ObservableProperty]
    private double _spacingTopX = 200.0;

    [ObservableProperty]
    private double _spacingTopY = 200.0;

    [ObservableProperty]
    private double _coverTop = 50.0;

    [ObservableProperty]
    private double _coverBottom = 50.0;

    [ObservableProperty]
    private double _coverSide = 50.0;

    [ObservableProperty]
    private bool _isTopMatEnabled = true;

    [ObservableProperty]
    private FoundationHookType _hookType = FoundationHookType.None;

    [ObservableProperty]
    private double _hookLength = 0.0;

    public ObservableCollection<double> StandardDiameters { get; } = new()
    {
        10.0, 12.0, 14.0, 16.0, 18.0, 20.0, 22.0, 25.0, 28.0, 32.0
    };

    public ObservableCollection<FoundationHookType> AvailableHookTypes { get; } = new()
    {
        FoundationHookType.None,
        FoundationHookType.Hook90Degrees
    };

    public FoundationSettingViewModel()
    {
    }

    public FoundationSettingViewModel(FoundationRebarSpec spec)
    {
        if (spec != null)
        {
            _diameterBottomX = spec.DiameterBottomX;
            _diameterBottomY = spec.DiameterBottomY;
            _diameterTopX = spec.DiameterTopX;
            _diameterTopY = spec.DiameterTopY;
            _spacingBottomX = spec.SpacingBottomX;
            _spacingBottomY = spec.SpacingBottomY;
            _spacingTopX = spec.SpacingTopX;
            _spacingTopY = spec.SpacingTopY;
            _coverTop = spec.CoverTop;
            _coverBottom = spec.CoverBottom;
            _coverSide = spec.CoverSide;
            _isTopMatEnabled = spec.IsTopMatEnabled;
            _hookType = spec.HookType;
            _hookLength = spec.HookLength;
        }
    }

    public FoundationRebarSpec ToSpec() => new(
        diameterBottomX: DiameterBottomX,
        diameterBottomY: DiameterBottomY,
        diameterTopX: DiameterTopX,
        diameterTopY: DiameterTopY,
        spacingBottomX: SpacingBottomX,
        spacingBottomY: SpacingBottomY,
        spacingTopX: SpacingTopX,
        spacingTopY: SpacingTopY,
        coverTop: CoverTop,
        coverBottom: CoverBottom,
        coverSide: CoverSide,
        isTopMatEnabled: IsTopMatEnabled,
        hookType: HookType,
        hookLength: HookLength);
}
