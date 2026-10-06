using CommunityToolkit.Mvvm.ComponentModel;

namespace HPRebar.KataExport.ViewModel.Tabs;

/// <summary>One editable row of the unit mass table (bar diameter, kg per metre).</summary>
public sealed partial class KataMassRowViewModel : ObservableObject
{
    [ObservableProperty] private double _diameter;
    [ObservableProperty] private double _kgPerM;

    /// <summary>A row the user added and left empty: ignored on save.</summary>
    public bool IsBlank => Diameter == 0.0 && KgPerM == 0.0;
}

/// <summary>One editable row of the lap / anchorage table (mm).</summary>
public sealed partial class KataLapRowViewModel : ObservableObject
{
    [ObservableProperty] private double _diameter;
    [ObservableProperty] private double _lapCompression;
    [ObservableProperty] private double _lapTension;
    [ObservableProperty] private double _anchorCompression;
    [ObservableProperty] private double _anchorTension;

    public bool IsBlank => Diameter == 0.0 && LapCompression == 0.0 && LapTension == 0.0 && AnchorCompression == 0.0 && AnchorTension == 0.0;
}
