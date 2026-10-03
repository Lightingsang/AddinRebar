using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using HPRebar.ColumnRebar.Model;
using HPRebar.Core.ColumnRebar;
using HPRebar.Core.ColumnRebar.Models;
using HPRebar.Core.Shared;

namespace HPRebar.ColumnRebar.ViewModel;

/// <summary>
///     Editable settings for one column segment. Holds what the user can change; the read-only geometry
///     stays on <see cref="Section"/>.
/// </summary>
public sealed partial class ColumnSpecEditor : ObservableObject
{
    [ObservableProperty] private int _barsAlongWidth = 2;
    [ObservableProperty] private int _barsAlongDepth = 2;
    [ObservableProperty] private int _barsAround = 4;
    [ObservableProperty] private double _cover;
    [ObservableProperty] private double _splitOverlap = 50;
    [ObservableProperty] private double _overlapFactor = 35;

    [ObservableProperty] private RebarTypeInfo _mainBarType = null!;
    [ObservableProperty] private RebarTypeInfo _stirrupBarType = null!;
    [ObservableProperty] private RebarTypeInfo _tieBarType = null!;

    [ObservableProperty] private int _distributionType;
    [ObservableProperty] private double _spacing;
    [ObservableProperty] private double _spacingDense;
    [ObservableProperty] private double _spacingSparse;
    [ObservableProperty] private bool _tiesUpToBeams;

    [ObservableProperty] private bool _addHorizontalTies;
    [ObservableProperty] private int _horizontalTieType;
    [ObservableProperty] private int _horizontalTieCount = 1;
    [ObservableProperty] private double _horizontalTieLeg;

    [ObservableProperty] private bool _addVerticalTies;
    [ObservableProperty] private int _verticalTieType;
    [ObservableProperty] private int _verticalTieCount = 1;
    [ObservableProperty] private double _verticalTieLeg;

    public ColumnSpecEditor(ColumnSection section, ColumnRebarSpec spec)
    {
        Section = section;
        Splices = new ObservableCollection<BarSpliceEditor>();
        Load(spec);
    }

    /// <summary>Geometry read out of the model. Not editable.</summary>
    public ColumnSection Section { get; }

    /// <summary>One entry per main bar, in bar-number order.</summary>
    public ObservableCollection<BarSpliceEditor> Splices { get; }

    /// <summary>One-based label used in the column picker.</summary>
    public string DisplayName => $"C{Section.Index + 1}";

    public bool IsRectangular => Section.Shape == SectionShape.Rectangle;

    /// <summary>Bars this layout produces, kept in step with the counts above.</summary>
    public int BarCount => ToLayout().BarCount;

    /// <summary>
    ///     Whether the bar counts are ones the layout calculator will accept. The calculator throws on
    ///     anything else, and a half-typed number in a text box is enough to get there, so everything that
    ///     draws or schedules asks this first.
    /// </summary>
    public bool IsLayoutValid =>
        IsRectangular
            ? BarsAlongWidth >= 2 && BarsAlongDepth >= 2
            : BarsAround > 0 && BarsAround % 4 == 0;

    /// <summary>
    ///     Everything that has to hold before this column can be built. Returns the first problem found so
    ///     the user is told about one thing at a time.
    /// </summary>
    public bool Validate(out string reason)
    {
        reason = string.Empty;

        if (MainBarType is null || StirrupBarType is null || TieBarType is null)
        {
            reason = "pick a bar type for the bars and the ties.";
            return false;
        }

        if (!IsLayoutValid)
        {
            reason = IsRectangular
                ? "at least two bars are needed along each side."
                : "the bar count around a circular column must be a positive multiple of four.";

            return false;
        }

        // The bars have to physically fit inside the cover and the ties.
        var clearance = 2 * Cover + 2 * StirrupBarType.DiameterMm + MainBarType.DiameterMm;

        var narrowest = IsRectangular ? System.Math.Min(Section.B, Section.H) : Section.D;

        if (clearance >= narrowest)
        {
            reason = $"cover and bar sizes leave no room inside a {narrowest:0} mm section.";
            return false;
        }

        return ValidateTies(out reason);
    }

    /// <summary>
    ///     Tie spacing has to produce a bar count Revit will accept. A spacing of a few millimetres over a
    ///     storey height quietly asks for thousands of ties, which the API refuses.
    /// </summary>
    private bool ValidateTies(out string reason)
    {
        reason = string.Empty;

        var run = StirrupRunLength;

        if (run <= 0)
        {
            reason = "the beam is as deep as the column, leaving nowhere to put ties.";
            return false;
        }

        var spacings = DistributionType == 0
            ? new[] { Spacing }
            : new[] { SpacingDense, SpacingSparse };

        foreach (var spacing in spacings)
        {
            if (spacing <= 0)
            {
                reason = "tie spacing must be greater than zero.";
                return false;
            }

            if ((int)(run / spacing) + 1 > RevitRebarLimits.MaxBarPositions)
            {
                reason = $"a spacing of {spacing:0} mm needs more than {RevitRebarLimits.MaxBarPositions} ties, which Revit will not accept.";
                return false;
            }
        }

        if (AddHorizontalTies && HorizontalTieType == 0 && HorizontalTieLeg <= 0)
        {
            reason = "give the horizontal cross-tie a leg length.";
            return false;
        }

        if (AddVerticalTies && VerticalTieType == 0 && VerticalTieLeg <= 0)
        {
            reason = "give the vertical cross-tie a leg length.";
            return false;
        }

        if (AddHorizontalTies && HorizontalTieType != 0 && HorizontalTieCount < 1)
        {
            reason = "at least one horizontal cross-tie is required.";
            return false;
        }

        if (AddVerticalTies && VerticalTieType != 0 && VerticalTieCount < 1)
        {
            reason = "at least one vertical cross-tie is required.";
            return false;
        }

        return true;
    }

    /// <summary>Length of the tie run for the current tie settings.</summary>
    public double StirrupRunLength => StirrupDistributionCalculator.ComputeRunLength(Section, TiesUpToBeams);

    public void Load(ColumnRebarSpec spec)
    {
        BarsAlongWidth = spec.Layout.Nx < 2 ? 2 : spec.Layout.Nx;
        BarsAlongDepth = spec.Layout.Ny < 2 ? 2 : spec.Layout.Ny;
        BarsAround = spec.Layout.Nd < 4 ? 4 : spec.Layout.Nd;
        Cover = spec.Layout.Cover;
        SplitOverlap = spec.Layout.SplitOverlap;
        OverlapFactor = spec.Layout.OverlapFactor;

        MainBarType = spec.MainBarType;
        StirrupBarType = spec.StirrupBarType;
        TieBarType = spec.TieBarType;

        DistributionType = spec.Stirrups.TypeDis;
        Spacing = spec.Stirrups.S;
        SpacingDense = spec.Stirrups.S1;
        SpacingSparse = spec.Stirrups.S2;
        TiesUpToBeams = spec.Stirrups.IsTiesUp;

        AddHorizontalTies = spec.Ties.AddH;
        HorizontalTieType = spec.Ties.TypeH;
        HorizontalTieCount = spec.Ties.NH;
        HorizontalTieLeg = spec.Ties.AH;

        AddVerticalTies = spec.Ties.AddV;
        VerticalTieType = spec.Ties.TypeV;
        VerticalTieCount = spec.Ties.NV;
        VerticalTieLeg = spec.Ties.AV;

        LoadSplices(spec.Splices);
    }

    /// <summary>
    ///     Rebuilds the per-bar splice list to match the current bar count, keeping whatever the user
    ///     already set on the bars that survive the change.
    /// </summary>
    public void SyncSpliceCount()
    {
        // The bar counts are assigned before the bar type during a load, and a new splice needs the bar
        // diameter to work out its lap. The load calls this again once everything is in place.
        if (MainBarType is null) return;

        var wanted = BarCount;

        while (Splices.Count > wanted) Splices.RemoveAt(Splices.Count - 1);

        while (Splices.Count < wanted)
        {
            var barNumber = Splices.Count + 1;

            Splices.Add(new BarSpliceEditor(
                barNumber,
                SpliceSpec.Default(barNumber, MainBarType.DiameterMm, SplitOverlap, OverlapFactor)));
        }

        OnPropertyChanged(nameof(BarCount));
    }

    public BarLayoutSpec ToLayout() => new()
    {
        Nx = IsRectangular ? BarsAlongWidth : 0,
        Ny = IsRectangular ? BarsAlongDepth : 0,
        Nd = IsRectangular ? 0 : BarsAround,
        BarDiameter = MainBarType?.DiameterMm ?? 0,
        StirrupDiameter = StirrupBarType?.DiameterMm ?? 0,
        Cover = Cover,
        SplitOverlap = SplitOverlap,
        OverlapFactor = OverlapFactor
    };

    public ColumnRebarSpec ToSpec(string partitionName) => new()
    {
        Layout = ToLayout(),
        Splices = Splices.Select(splice => splice.ToSpec()).ToList(),
        Stirrups = new StirrupSpec
        {
            TypeDis = DistributionType,
            S = Spacing,
            S1 = SpacingDense,
            S2 = SpacingSparse,
            IsTiesUp = TiesUpToBeams
        },
        Ties = new AdditionalTieSpec
        {
            AddH = AddHorizontalTies,
            TypeH = HorizontalTieType,
            NH = HorizontalTieCount,
            AH = HorizontalTieLeg,
            AddV = AddVerticalTies,
            TypeV = VerticalTieType,
            NV = VerticalTieCount,
            AV = VerticalTieLeg
        },
        MainBarType = MainBarType,
        StirrupBarType = StirrupBarType,
        TieBarType = TieBarType,
        PartitionName = partitionName
    };

    private void LoadSplices(System.Collections.Generic.IReadOnlyList<SpliceSpec> splices)
    {
        Splices.Clear();

        for (var i = 0; i < splices.Count; i++)
        {
            Splices.Add(new BarSpliceEditor(i + 1, splices[i]));
        }

        // Tops up the list if the incoming settings were shorter than the bar counts just applied.
        SyncSpliceCount();
    }

    partial void OnBarsAlongWidthChanged(int value) => SyncSpliceCount();

    partial void OnBarsAlongDepthChanged(int value) => SyncSpliceCount();

    partial void OnBarsAroundChanged(int value) => SyncSpliceCount();

    partial void OnTiesUpToBeamsChanged(bool value) => OnPropertyChanged(nameof(StirrupRunLength));

    // The lap length is a function of the bar diameter and the splice settings, so a change to any of them
    // invalidates every lap already stored. The original tool rebuilt its bars outright for the same reason;
    // per-bar edits made before the change are lost.
    partial void OnMainBarTypeChanged(RebarTypeInfo value) => ResetSpliceLaps();

    partial void OnSplitOverlapChanged(double value) => ResetSpliceLaps();

    partial void OnOverlapFactorChanged(double value) => ResetSpliceLaps();

    private void ResetSpliceLaps()
    {
        if (MainBarType is null) return;

        foreach (var splice in Splices)
        {
            splice.Load(SpliceSpec.Default(splice.BarNumber, MainBarType.DiameterMm, SplitOverlap, OverlapFactor));
        }
    }
}
