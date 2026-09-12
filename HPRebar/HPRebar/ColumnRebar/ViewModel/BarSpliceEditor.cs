using CommunityToolkit.Mvvm.ComponentModel;
using HPRebar.Core.ColumnRebar.Models;

namespace HPRebar.ColumnRebar.ViewModel;

/// <summary>
///     Editable view of one bar's splice settings. The core model is immutable, so the window edits this
///     and converts back on the way out.
/// </summary>
public sealed partial class BarSpliceEditor : ObservableObject
{
    [ObservableProperty] private bool _isTopDowels;
    [ObservableProperty] private int _topDowelsType;
    [ObservableProperty] private double _laTop;
    [ObservableProperty] private double _lbTop;
    [ObservableProperty] private bool _isBottomDowels;
    [ObservableProperty] private int _bottomDowelsType;
    [ObservableProperty] private double _laBottom;
    [ObservableProperty] private double _lbBottom;
    [ObservableProperty] private double _lcBottom;

    public BarSpliceEditor(int barNumber, SpliceSpec spec)
    {
        BarNumber = barNumber;
        Load(spec);
    }

    /// <summary>One-based bar number, matching the numbering used everywhere else.</summary>
    public int BarNumber { get; }

    public void Load(SpliceSpec spec)
    {
        IsTopDowels = spec.IsTopDowels;
        TopDowelsType = spec.TopDowelsType;
        LaTop = spec.LaTop;
        LbTop = spec.LbTop;
        IsBottomDowels = spec.IsBottomDowels;
        BottomDowelsType = spec.BottomDowelsType;
        LaBottom = spec.LaBottom;
        LbBottom = spec.LbBottom;
        LcBottom = spec.LcBottom;
    }

    public SpliceSpec ToSpec() => new()
    {
        IsTopDowels = IsTopDowels,
        TopDowelsType = TopDowelsType,
        LaTop = LaTop,
        LbTop = LbTop,
        IsBottomDowels = IsBottomDowels,
        BottomDowelsType = BottomDowelsType,
        LaBottom = LaBottom,
        LbBottom = LbBottom,
        LcBottom = LcBottom
    };
}
