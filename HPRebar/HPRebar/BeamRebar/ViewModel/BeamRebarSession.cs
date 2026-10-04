using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using HPRebar.BeamRebar.Model;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;

namespace HPRebar.BeamRebar.ViewModel;

/// <summary>
/// Observable editor for additional top reinforcement centered over a specific support node.
/// </summary>
public sealed partial class SupportTopBarEditor : ObservableObject
{
    public int SupportIndex { get; }
    public string SupportName { get; }

    [ObservableProperty] private int _layer1Count = 2;
    [ObservableProperty] private double _layer1ExtensionRatio = 1.0 / 3.0;
    [ObservableProperty] private bool _enableLayer2;
    [ObservableProperty] private int _layer2Count = 2;
    [ObservableProperty] private double _layer2ExtensionRatio = 1.0 / 4.0;
    [ObservableProperty] private double _layerGap = 50.0;
    [ObservableProperty] private RebarTypeInfo? _barType;
    [ObservableProperty] private EndAnchorageType _exteriorAnchorage = EndAnchorageType.Hook90Down;
    [ObservableProperty] private double _exteriorHookLength;

    public SupportTopBarEditor(int supportIndex, string supportName, RebarTypeInfo? defaultBarType = null)
    {
        SupportIndex = supportIndex;
        SupportName = supportName;
        _barType = defaultBarType;
    }

    public SupportAdditionalTopBarConfig ToConfig() => new()
    {
        SupportIndex = SupportIndex,
        Layer1Count = Layer1Count,
        Layer1Diameter = BarType?.DiameterMm ?? 18.0,
        Layer1ExtensionRatio = Layer1ExtensionRatio,
        Layer2Count = EnableLayer2 ? Layer2Count : 0,
        Layer2Diameter = BarType?.DiameterMm ?? 18.0,
        Layer2ExtensionRatio = Layer2ExtensionRatio,
        LayerGap = LayerGap,
        ExteriorEndAnchorage = ExteriorAnchorage,
        ExteriorHookLength = ExteriorHookLength,
        BarTypeName = BarType?.Name ?? string.Empty
    };

    public void CopyFrom(SupportTopBarEditor source)
    {
        Layer1Count = source.Layer1Count;
        Layer1ExtensionRatio = source.Layer1ExtensionRatio;
        EnableLayer2 = source.EnableLayer2;
        Layer2Count = source.Layer2Count;
        Layer2ExtensionRatio = source.Layer2ExtensionRatio;
        LayerGap = source.LayerGap;
        BarType = source.BarType;
        ExteriorAnchorage = source.ExteriorAnchorage;
        ExteriorHookLength = source.ExteriorHookLength;
    }
}

/// <summary>
/// Observable editor for additional bottom reinforcement placed in the midspan region of a span.
/// </summary>
public sealed partial class SpanBottomBarEditor : ObservableObject
{
    public int SpanIndex { get; }
    public string SpanName { get; }

    [ObservableProperty] private int _layer1Count = 2;
    [ObservableProperty] private double _cutoffRatio = 1.0 / 7.0;
    [ObservableProperty] private bool _enableLayer2;
    [ObservableProperty] private int _layer2Count = 2;
    [ObservableProperty] private double _layerGap = 50.0;
    [ObservableProperty] private RebarTypeInfo? _barType;

    public SpanBottomBarEditor(int spanIndex, string spanName, RebarTypeInfo? defaultBarType = null)
    {
        SpanIndex = spanIndex;
        SpanName = spanName;
        _barType = defaultBarType;
    }

    public SpanAdditionalBottomBarConfig ToConfig() => new()
    {
        SpanIndex = SpanIndex,
        Layer1Count = Layer1Count,
        Layer1Diameter = BarType?.DiameterMm ?? 18.0,
        CutoffRatio = CutoffRatio,
        Layer2Count = EnableLayer2 ? Layer2Count : 0,
        Layer2Diameter = BarType?.DiameterMm ?? 18.0,
        LayerGap = LayerGap,
        BarTypeName = BarType?.Name ?? string.Empty
    };

    public void CopyFrom(SpanBottomBarEditor source)
    {
        Layer1Count = source.Layer1Count;
        CutoffRatio = source.CutoffRatio;
        EnableLayer2 = source.EnableLayer2;
        Layer2Count = source.Layer2Count;
        LayerGap = source.LayerGap;
        BarType = source.BarType;
    }
}

/// <summary>
/// Maintains user input state, continuous beam geometry, and rebar specifications during the dialog session.
/// </summary>
public sealed partial class BeamRebarSession : ObservableObject
{
    // --- Selection State ---
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedSpan))]
    [NotifyPropertyChangedFor(nameof(SelectedSpanEditor))]
    private int _selectedSpanIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedSupport))]
    [NotifyPropertyChangedFor(nameof(SelectedSupportEditor))]
    private int _selectedSupportIndex;

    // --- Main Reinforcement Properties ---
    [ObservableProperty] private int _topBarCount = 2;
    [ObservableProperty] private RebarTypeInfo? _topBarType;
    [ObservableProperty] private EndAnchorageType _topStartAnchorage = EndAnchorageType.Hook90Down;
    [ObservableProperty] private EndAnchorageType _topEndAnchorage = EndAnchorageType.Hook90Down;
    [ObservableProperty] private double _topStartHookLength;
    [ObservableProperty] private double _topEndHookLength;

    [ObservableProperty] private int _bottomBarCount = 2;
    [ObservableProperty] private RebarTypeInfo? _bottomBarType;
    [ObservableProperty] private EndAnchorageType _bottomStartAnchorage = EndAnchorageType.Hook90Up;
    [ObservableProperty] private EndAnchorageType _bottomEndAnchorage = EndAnchorageType.Hook90Up;
    [ObservableProperty] private double _bottomStartHookLength;
    [ObservableProperty] private double _bottomEndHookLength;

    [ObservableProperty] private double _cover = 25.0;
    [ObservableProperty] private double _maxStockLength = 11700.0;
    [ObservableProperty] private double _lapFactor = 40.0;
    [ObservableProperty] private bool _enableStagger = true;

    // --- Stirrups Properties ---
    [ObservableProperty] private StirrupLayout _stirrupLayout = StirrupLayout.ThreeZoneL4;
    [ObservableProperty] private RebarTypeInfo? _stirrupBarType;
    [ObservableProperty] private double _stirrupSpacingDense = 100.0;
    [ObservableProperty] private double _stirrupSpacingSparse = 200.0;
    [ObservableProperty] private double _stirrupStartOffset = 50.0;
    [ObservableProperty] private bool _includeStirrupsInNodes;
    [ObservableProperty] private double _nodeSpacing = 150.0;

    // --- Side (Skin) Bars Properties ---
    [ObservableProperty] private bool _autoSkinBars = true;
    [ObservableProperty] private double _depthThreshold = 700.0;
    [ObservableProperty] private RebarTypeInfo? _sideBarType;
    [ObservableProperty] private double _maxVerticalSpacing = 300.0;
    [ObservableProperty] private bool _includeCrossTies = true;
    [ObservableProperty] private RebarTypeInfo? _crossTieBarType;
    [ObservableProperty] private double _crossTieSpacing = 400.0;

    // --- Special Bars Properties ---
    [ObservableProperty] private bool _enableHangingStirrups = true;
    [ObservableProperty] private int _hangingStirrupsPerSide = 3;
    [ObservableProperty] private double _hangingStirrupSpacing = 50.0;
    [ObservableProperty] private bool _enableDiagonalTies;
    [ObservableProperty] private int _diagonalTieCount = 2;
    [ObservableProperty] private double _diagonalTieDiameter = 14.0;

    // --- Views and Annotations Properties ---
    [ObservableProperty] private bool _createElevationView = BeamViewOptions.Default.CreateElevationView;
    [ObservableProperty] private string _detailViewName = BeamViewOptions.Default.DetailViewName;
    [ObservableProperty] private int _elevationScale = BeamViewOptions.Default.ElevationScale;
    [ObservableProperty] private bool _createSectionViews = BeamViewOptions.Default.CreateSectionViews;
    [ObservableProperty] private int _sectionsPerSpan = BeamViewOptions.Default.SectionsPerSpan;
    [ObservableProperty] private string _sectionPrefix = BeamViewOptions.Default.SectionPrefix;
    [ObservableProperty] private bool _createDimensions = BeamViewOptions.Default.CreateDimensions;
    [ObservableProperty] private bool _createTags = BeamViewOptions.Default.CreateTables;
    [ObservableProperty] private string _partitionName = "Beam Rebar";

    public BeamRebarSession(
        BeamStack stack,
        BeamRebarSpec spec,
        IReadOnlyList<RebarTypeInfo> barTypes)
    {
        Stack = stack;
        Spec = spec;
        BarTypes = new ObservableCollection<RebarTypeInfo>(barTypes);

        InitDefaultsFromSpec(spec);
        InitAdditionalBarEditors();
    }

    public BeamRebarSession(
        BeamStack stack,
        IReadOnlyList<BeamFaces> faces,
        BeamRebarSpec spec,
        IReadOnlyList<RebarTypeInfo> barTypes)
        : this(stack, spec, barTypes)
    {
        Faces = faces;
    }

    public BeamStack Stack { get; }
    public IReadOnlyList<BeamFaces>? Faces { get; }
    public BeamRebarSpec Spec { get; }
    public ObservableCollection<RebarTypeInfo> BarTypes { get; }

    public ObservableCollection<SupportTopBarEditor> SupportTopBars { get; } = new();
    public ObservableCollection<SpanBottomBarEditor> SpanBottomBars { get; } = new();

    public BeamSpan? SelectedSpan =>
        SelectedSpanIndex >= 0 && SelectedSpanIndex < Stack.Spans.Count ? Stack.Spans[SelectedSpanIndex] : null;

    public BeamSupportNode? SelectedSupport =>
        SelectedSupportIndex >= 0 && SelectedSupportIndex < Stack.Supports.Count ? Stack.Supports[SelectedSupportIndex] : null;

    public SupportTopBarEditor? SelectedSupportEditor
    {
        get => SelectedSupportIndex >= 0 && SelectedSupportIndex < SupportTopBars.Count ? SupportTopBars[SelectedSupportIndex] : null;
        set
        {
            if (value is not null)
            {
                int idx = SupportTopBars.IndexOf(value);
                if (idx >= 0 && idx != SelectedSupportIndex)
                {
                    SelectedSupportIndex = idx;
                }
            }
        }
    }

    public SpanBottomBarEditor? SelectedSpanEditor
    {
        get => SelectedSpanIndex >= 0 && SelectedSpanIndex < SpanBottomBars.Count ? SpanBottomBars[SelectedSpanIndex] : null;
        set
        {
            if (value is not null)
            {
                int idx = SpanBottomBars.IndexOf(value);
                if (idx >= 0 && idx != SelectedSpanIndex)
                {
                    SelectedSpanIndex = idx;
                }
            }
        }
    }

    public int SpanCount => Stack.Spans.Count;
    public int SupportCount => Stack.Supports.Count;
    public double TotalLength => Stack.ContinuousStack.TotalLength;
    public double MaxHeight => Stack.ContinuousStack.MaxHeight;

    private void InitDefaultsFromSpec(BeamRebarSpec spec)
    {
        TopBarCount = spec.MainBars.TopCount;
        BottomBarCount = spec.MainBars.BottomCount;
        Cover = spec.Stirrups.Cover;
        MaxStockLength = spec.MainBars.MaxStockLength;
        LapFactor = spec.MainBars.LapFactor;
        EnableStagger = spec.MainBars.EnableStagger;
        TopStartAnchorage = spec.MainBars.TopStartAnchorage;
        TopEndAnchorage = spec.MainBars.TopEndAnchorage;
        BottomStartAnchorage = spec.MainBars.BottomStartAnchorage;
        BottomEndAnchorage = spec.MainBars.BottomEndAnchorage;

        StirrupLayout = spec.Stirrups.Layout;
        StirrupSpacingDense = spec.Stirrups.SpacingDense;
        StirrupSpacingSparse = spec.Stirrups.SpacingSparse;
        StirrupStartOffset = spec.Stirrups.StartOffset;
        IncludeStirrupsInNodes = spec.Stirrups.IncludeStirrupsInNodes;
        NodeSpacing = spec.Stirrups.NodeSpacing;

        AutoSkinBars = spec.SideBars.AutoSkinBars;
        DepthThreshold = spec.SideBars.DepthThreshold;
        MaxVerticalSpacing = spec.SideBars.MaxVerticalSpacing;
        IncludeCrossTies = spec.SideBars.IncludeCrossTies;
        CrossTieSpacing = spec.SideBars.CrossTieSpacing;

        EnableHangingStirrups = spec.SpecialBars.EnableHangingStirrups;
        HangingStirrupsPerSide = spec.SpecialBars.HangingStirrupsPerSide;
        HangingStirrupSpacing = spec.SpecialBars.HangingStirrupSpacing;
        EnableDiagonalTies = spec.SpecialBars.EnableDiagonalTies;
        DiagonalTieCount = spec.SpecialBars.DiagonalTieCount;
        DiagonalTieDiameter = spec.SpecialBars.DiagonalTieDiameter;

        PartitionName = spec.PartitionName;

        // Resolve Bar Types
        TopBarType = spec.MainBarType ?? FindBarType(spec.MainBars.TopBarTypeName, spec.MainBars.TopDiameter);
        BottomBarType = spec.MainBarType ?? FindBarType(spec.MainBars.BottomBarTypeName, spec.MainBars.BottomDiameter);
        StirrupBarType = spec.StirrupBarType ?? FindBarType(spec.Stirrups.BarTypeName, spec.Stirrups.Diameter);
        SideBarType = spec.SideBarType ?? FindBarType(spec.SideBars.SideBarTypeName, spec.SideBars.Diameter);
        CrossTieBarType = spec.TieBarType ?? FindBarType(spec.SideBars.CrossTieBarTypeName, spec.SideBars.CrossTieDiameter);
    }

    private void InitAdditionalBarEditors()
    {
        var defaultAddBarType = Spec.AddTopBarType ?? FindBarType(string.Empty, 18.0) ?? BarTypes.FirstOrDefault();

        for (int i = 0; i < Stack.Supports.Count; i++)
        {
            var support = Stack.Supports[i];
            var name = !string.IsNullOrEmpty(support.Name) ? support.Name : $"Support {i + 1}";
            var editor = new SupportTopBarEditor(i, name, defaultAddBarType);

            if (i < Spec.AdditionalTopBars.Count)
            {
                var cfg = Spec.AdditionalTopBars[i];
                editor.Layer1Count = cfg.Layer1Count;
                editor.Layer1ExtensionRatio = cfg.Layer1ExtensionRatio;
                editor.EnableLayer2 = cfg.Layer2Count > 0;
                editor.Layer2Count = Math.Max(2, cfg.Layer2Count);
                editor.Layer2ExtensionRatio = cfg.Layer2ExtensionRatio;
                editor.LayerGap = cfg.LayerGap;
                editor.ExteriorAnchorage = cfg.ExteriorEndAnchorage;
                editor.ExteriorHookLength = cfg.ExteriorHookLength;
            }

            editor.PropertyChanged += ForwardItemPropertyChanged;
            SupportTopBars.Add(editor);
        }

        for (int i = 0; i < Stack.Spans.Count; i++)
        {
            var span = Stack.Spans[i];
            var name = !string.IsNullOrEmpty(span.Name) ? span.Name : $"Span {i + 1}";
            var editor = new SpanBottomBarEditor(i, name, defaultAddBarType);

            if (i < Spec.AdditionalBottomBars.Count)
            {
                var cfg = Spec.AdditionalBottomBars[i];
                editor.Layer1Count = cfg.Layer1Count;
                editor.CutoffRatio = cfg.CutoffRatio;
                editor.EnableLayer2 = cfg.Layer2Count > 0;
                editor.Layer2Count = Math.Max(2, cfg.Layer2Count);
                editor.LayerGap = cfg.LayerGap;
            }

            editor.PropertyChanged += ForwardItemPropertyChanged;
            SpanBottomBars.Add(editor);
        }
    }

    private void ForwardItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(SupportTopBars));
        OnPropertyChanged(nameof(SpanBottomBars));
    }

    private RebarTypeInfo? FindBarType(string name, double diameter)
    {
        if (!string.IsNullOrEmpty(name))
        {
            var match = BarTypes.FirstOrDefault(b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;
        }

        return BarTypes.FirstOrDefault(b => Math.Abs(b.DiameterMm - diameter) < 1.0)
               ?? BarTypes.FirstOrDefault();
    }

    public void ApplySelectedSupportToAll()
    {
        if (SelectedSupportEditor is not { } current) return;
        foreach (var editor in SupportTopBars)
        {
            if (editor.SupportIndex != current.SupportIndex)
            {
                editor.CopyFrom(current);
            }
        }
    }

    public void ApplySelectedSpanToAll()
    {
        if (SelectedSpanEditor is not { } current) return;
        foreach (var editor in SpanBottomBars)
        {
            if (editor.SpanIndex != current.SpanIndex)
            {
                editor.CopyFrom(current);
            }
        }
    }

    public bool Validate(out string errorMessage)
    {
        bool barTypesChosen = TopBarType is not null && BottomBarType is not null && StirrupBarType is not null;
        string? problem = BeamSpecRules.FirstProblem(
            ToMainBarSpec(), ToStirrupSpec(), ToSideBarSpec(), barTypesChosen, ViewNamesToCreate(), Stack.Spans);

        errorMessage = problem ?? string.Empty;
        return problem is null;
    }

    public BeamRebarSpec ToSpec()
    {
        var supportConfigs = SupportTopBars.Select(s => s.ToConfig()).ToList();
        var spanConfigs = SpanBottomBars.Select(s => s.ToConfig()).ToList();

        return new BeamRebarSpec
        {
            Stirrups = ToStirrupSpec(),
            MainBars = ToMainBarSpec(),
            AdditionalBars = new BeamAdditionalBarSpec
            {
                SupportTopBars = supportConfigs,
                SpanBottomBars = spanConfigs
            },
            SideBars = ToSideBarSpec(),
            SpecialBars = new BeamSpecialBarSpec
            {
                EnableHangingStirrups = EnableHangingStirrups,
                HangingStirrupsPerSide = HangingStirrupsPerSide,
                HangingStirrupDiameter = StirrupBarType?.DiameterMm ?? 8.0,
                HangingStirrupSpacing = HangingStirrupSpacing,
                EnableDiagonalTies = EnableDiagonalTies,
                DiagonalTieCount = DiagonalTieCount,
                DiagonalTieDiameter = DiagonalTieDiameter,
                HangingStirrupTypeName = StirrupBarType?.Name ?? string.Empty
            },
            MainBarType = TopBarType,
            StirrupBarType = StirrupBarType,
            AddTopBarType = SupportTopBars.FirstOrDefault()?.BarType ?? TopBarType,
            AddBottomBarType = SpanBottomBars.FirstOrDefault()?.BarType ?? BottomBarType,
            SideBarType = SideBarType,
            TieBarType = CrossTieBarType,
            PartitionName = PartitionName,
            Views = ToViewOptions()
        };
    }

    private BeamStirrupSpec ToStirrupSpec() => new()
    {
        Layout = StirrupLayout,
        Diameter = StirrupBarType?.DiameterMm ?? 8.0,
        Cover = Cover,
        SpacingDense = StirrupSpacingDense,
        SpacingSparse = StirrupSpacingSparse,
        StartOffset = StirrupStartOffset,
        IncludeStirrupsInNodes = IncludeStirrupsInNodes,
        NodeSpacing = NodeSpacing,
        BarTypeName = StirrupBarType?.Name ?? string.Empty
    };

    private BeamMainBarSpec ToMainBarSpec() => new()
    {
        TopCount = TopBarCount,
        TopDiameter = TopBarType?.DiameterMm ?? 20.0,
        TopCover = Cover,
        TopStartAnchorage = TopStartAnchorage,
        TopEndAnchorage = TopEndAnchorage,
        TopStartHookLength = TopStartHookLength,
        TopEndHookLength = TopEndHookLength,
        TopBarTypeName = TopBarType?.Name ?? string.Empty,

        BottomCount = BottomBarCount,
        BottomDiameter = BottomBarType?.DiameterMm ?? 20.0,
        BottomCover = Cover,
        BottomStartAnchorage = BottomStartAnchorage,
        BottomEndAnchorage = BottomEndAnchorage,
        BottomStartHookLength = BottomStartHookLength,
        BottomEndHookLength = BottomEndHookLength,
        BottomBarTypeName = BottomBarType?.Name ?? string.Empty,

        MaxStockLength = MaxStockLength,
        LapFactor = LapFactor,
        EnableStagger = EnableStagger
    };

    private BeamSideBarSpec ToSideBarSpec() => new()
    {
        AutoSkinBars = AutoSkinBars,
        DepthThreshold = DepthThreshold,
        Diameter = SideBarType?.DiameterMm ?? 12.0,
        MaxVerticalSpacing = MaxVerticalSpacing,
        Cover = Cover,
        IncludeCrossTies = IncludeCrossTies,
        CrossTieDiameter = CrossTieBarType?.DiameterMm ?? 8.0,
        CrossTieSpacing = CrossTieSpacing,
        SideBarTypeName = SideBarType?.Name ?? string.Empty,
        CrossTieBarTypeName = CrossTieBarType?.Name ?? string.Empty
    };

    /// <summary>The detail view name when any view is made, followed by the section prefix when sections are.</summary>
    private string ViewNamesToCreate()
    {
        string names = CreateElevationView || CreateSectionViews ? DetailViewName : string.Empty;
        return CreateSectionViews ? names + SectionPrefix : names;
    }

    private BeamViewOptions ToViewOptions()
    {
        var defaults = BeamViewOptions.Default;
        return new BeamViewOptions
        {
            CreateElevationView = CreateElevationView,
            DetailViewName = string.IsNullOrWhiteSpace(DetailViewName) ? defaults.DetailViewName : DetailViewName.Trim(),
            ElevationScale = ElevationScale,
            CreateSectionViews = CreateSectionViews,
            SectionsPerSpan = SectionsPerSpan,
            SectionPrefix = string.IsNullOrWhiteSpace(SectionPrefix) ? defaults.SectionPrefix : SectionPrefix.Trim(),
            CreateDimensions = CreateDimensions,
            CreateTables = CreateTags
        };
    }
}
