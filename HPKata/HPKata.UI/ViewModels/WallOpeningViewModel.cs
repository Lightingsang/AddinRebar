using CommunityToolkit.Mvvm.ComponentModel;

namespace HPKata.UI.ViewModels
{
    /// <summary>
    /// ViewModel representing a wall opening and its trimmer reinforcement settings.
    /// </summary>
    public sealed partial class WallOpeningViewModel : ObservableObject
    {
        [ObservableProperty] private string _openingMark = "OP1";
        [ObservableProperty] private double _xOffsetMm = 1000.0;
        [ObservableProperty] private double _yOffsetMm;
        [ObservableProperty] private double _widthMm = 1000.0;
        [ObservableProperty] private double _heightMm = 2100.0;

        [ObservableProperty] private bool _hasDiagonalBars = true;
        [ObservableProperty] private int _diagonalDiameter = 16;
        [ObservableProperty] private int _jambExtraDiameter = 16;
        [ObservableProperty] private int _headerExtraDiameter = 16;
    }
}
