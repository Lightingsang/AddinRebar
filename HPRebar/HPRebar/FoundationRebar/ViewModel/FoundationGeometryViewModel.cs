using CommunityToolkit.Mvvm.ComponentModel;
using HPRebar.Core.FoundationRebar.Models;

namespace HPRebar.FoundationRebar.ViewModel;

/// <summary>
/// ViewModel representing geometric dimensions and world elevations of the selected foundation slab.
/// </summary>
public sealed partial class FoundationGeometryViewModel : ObservableObject
{
    [ObservableProperty]
    private double _length;

    [ObservableProperty]
    private double _width;

    [ObservableProperty]
    private double _thickness;

    [ObservableProperty]
    private double _topElevation;

    [ObservableProperty]
    private double _bottomElevation;

    public FoundationGeometryViewModel(FoundationGeometrySnapshot snapshot)
    {
        if (snapshot != null)
        {
            _length = snapshot.Length;
            _width = snapshot.Width;
            _thickness = snapshot.Thickness;
            _topElevation = snapshot.TopZ;
            _bottomElevation = snapshot.BottomZ;
        }
    }
}
