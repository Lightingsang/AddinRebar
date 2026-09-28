using System.Windows;
using System.Windows.Media;
using HPRebar.Core.KataExport.Calculators;
using HPRebar.Core.KataExport.Models;
using HPRebar.Core.KataRebar.Models;
using HPRebar.KataExport.ViewModel;

// WPF types, not the Revit ones the SDK imports globally.
using Brush = System.Windows.Media.Brush;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// The beam run as an elevation, zoomed and panned in both directions like a CAD view (mouse handling in
/// <c>KataElevationCanvas.Input.cs</c>). A click selects the Kata column under the cursor, two-way bound to the
/// preview table; the view model frames a span or the whole run through <see cref="FocusRequest"/>.
/// </summary>
public sealed partial class KataElevationCanvas : FrameworkElement
{
    private const double MarginPx = 36.0;
    private const double FocusMarginPx = 60.0;

    /// <summary>Wheel limit — 3 px per mm: a 50 mm offset is 150 px wide, enough to read any Kata dimension.</summary>
    private const double MaxScale = 3.0;

    /// <summary>Framing a span never zooms in further than this, so its neighbours and labels stay in view.</summary>
    private const double FocusMaxScale = 1.2;

    public static readonly DependencyProperty ElevationProperty = DependencyProperty.Register(
        nameof(Elevation), typeof(KataElevation), typeof(KataElevationCanvas),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnElevationChanged));

    public static readonly DependencyProperty SelectedColumnIndexProperty = DependencyProperty.Register(
        nameof(SelectedColumnIndex), typeof(int), typeof(KataElevationCanvas),
        new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender, OnSelectedChanged));

    public static readonly DependencyProperty FocusRequestProperty = DependencyProperty.Register(
        nameof(FocusRequest), typeof(KataViewFocus), typeof(KataElevationCanvas),
        new FrameworkPropertyMetadata(null, OnFocusRequested));

    /// <summary>Bound to <c>{DynamicResource Brush.Canvas.Fill}</c>: when the theme swaps it, the palette is rebuilt.</summary>
    public static readonly DependencyProperty SurfaceBrushProperty = DependencyProperty.Register(
        nameof(SurfaceBrush), typeof(Brush), typeof(KataElevationCanvas),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, (d, _) => ((KataElevationCanvas)d)._palette = null));

    public static readonly DependencyProperty RebarPlanProperty = DependencyProperty.Register(
        nameof(RebarPlan), typeof(KataRebarPlan), typeof(KataElevationCanvas),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty RebarStationMapProperty = DependencyProperty.Register(
        nameof(RebarStationMap), typeof(KataStationMap), typeof(KataElevationCanvas),
        new FrameworkPropertyMetadata(KataStationMap.Identity, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ShowRebarProperty = DependencyProperty.Register(
        nameof(ShowRebar), typeof(bool), typeof(KataElevationCanvas),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ShowSectionProperty = DependencyProperty.Register(
        nameof(ShowSection), typeof(bool), typeof(KataElevationCanvas),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    private KataCanvasPalette? _palette;
    private KataElevationViewport? _viewport;
    private bool _userFramed;

    public KataElevationCanvas()
    {
        Focusable = true;
        ClipToBounds = true;
        SizeChanged += (_, _) => OnResized();
    }

    public KataElevation? Elevation
    {
        get => (KataElevation?)GetValue(ElevationProperty);
        set => SetValue(ElevationProperty, value);
    }

    public int SelectedColumnIndex
    {
        get => (int)GetValue(SelectedColumnIndexProperty);
        set => SetValue(SelectedColumnIndexProperty, value);
    }

    public KataViewFocus? FocusRequest
    {
        get => (KataViewFocus?)GetValue(FocusRequestProperty);
        set => SetValue(FocusRequestProperty, value);
    }

    public Brush? SurfaceBrush
    {
        get => (Brush?)GetValue(SurfaceBrushProperty);
        set => SetValue(SurfaceBrushProperty, value);
    }

    /// <summary>The sheet's bars planned on the measured beams; null before the sheet is read back.</summary>
    public KataRebarPlan? RebarPlan
    {
        get => (KataRebarPlan?)GetValue(RebarPlanProperty);
        set => SetValue(RebarPlanProperty, value);
    }

    /// <summary>Places the rebar layout's local X on this drawing's stations.</summary>
    public KataStationMap RebarStationMap
    {
        get => (KataStationMap)GetValue(RebarStationMapProperty);
        set => SetValue(RebarStationMapProperty, value);
    }

    public bool ShowRebar
    {
        get => (bool)GetValue(ShowRebarProperty);
        set => SetValue(ShowRebarProperty, value);
    }

    public bool ShowSection
    {
        get => (bool)GetValue(ShowSectionProperty);
        set => SetValue(ShowSectionProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var palette = _palette ??= KataCanvasPalette.From(this);
        var draw = new KataDrawPrimitives(drawingContext, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        if (Elevation is not { } elevation || ActualWidth < 1 || ActualHeight < 1)
        {
            draw.Box(palette.Fill, null, 0, 0, ActualWidth, ActualHeight);
            return;
        }

        var viewport = _viewport ??= Frame(elevation, elevation.Bounds, MarginPx);
        var scene = new KataElevationScene(elevation, viewport, ActualWidth, ActualHeight, SelectedColumnIndex);
        new KataElevationPainter(scene, palette, draw).Paint();
        new KataElevationAnnotations(scene, palette, draw).Paint();

        var map = RebarStationMap ?? KataStationMap.Identity;
        if (ShowRebar && RebarPlan is not null)
        {
            new KataElevationRebarPainter(scene, palette, draw, RebarPlan.Layout, map).Paint();
        }

        if (ShowSection && RebarPlan is not null)
        {
            new KataElevationSectionPainter(scene, RebarPlan, map, palette, draw).Paint();
        }
    }

    /// <summary>
    /// "Zoom extents" on <paramref name="range"/>: across the width (capped at <see cref="FocusMaxScale"/>), zoomed out
    /// further if the beam band and its labels — the row-11 chain included — would not fit the height, then centred.
    /// </summary>
    private KataElevationViewport Frame(KataElevation elevation, Interval1D range, double marginPx)
    {
        double width = Math.Max(1.0, ActualWidth), height = Math.Max(1.0, ActualHeight);
        double depth = elevation.TopMm - elevation.BottomMm;
        var viewport = KataElevationViewport.Fit(range, width, marginPx, KataElevationScene.MinVerticalScale(elevation));
        if (viewport.Scale > FocusMaxScale) viewport = viewport.ZoomAt(FocusMaxScale / viewport.Scale, width / 2.0, 0.0, 0.0, FocusMaxScale);
        viewport = viewport.ShrinkToHeight(depth, height, KataElevationScene.AbovePx, KataElevationScene.BelowPx, width / 2.0);
        return viewport.CentreBand(depth, height, KataElevationScene.AbovePx, KataElevationScene.BelowPx);
    }

    /// <summary>Zooming out stops at half the whole run.</summary>
    private double MinScale(KataElevation elevation) =>
        KataElevationViewport.Fit(elevation.Bounds, Math.Max(1.0, ActualWidth), MarginPx).Scale * 0.5;

    private void FrameAll()
    {
        if (Elevation is not { } elevation) return;
        _viewport = Frame(elevation, elevation.Bounds, MarginPx);
        _userFramed = false;
        InvalidateVisual();
    }

    private void FrameColumn(int index)
    {
        if (Elevation is not { } elevation || index < 0 || index >= elevation.Columns.Count || ActualWidth < 1) return;
        _viewport = Frame(elevation, elevation.FocusRange(index), FocusMarginPx);
        _userFramed = true;
        InvalidateVisual();
    }

    private void OnResized()
    {
        if (!_userFramed) _viewport = null;
        InvalidateVisual();
    }

    private static void OnElevationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var canvas = (KataElevationCanvas)sender;
        var before = args.OldValue as KataElevation;
        var after = args.NewValue as KataElevation;

        // A parameter change rebuilds the same geometry: keep the user's framing. A new run starts framed whole.
        if (before is null || after is null || before.Bounds != after.Bounds || before.Columns.Count != after.Columns.Count)
        {
            canvas._viewport = null;
            canvas._userFramed = false;
        }
    }

    private static void OnSelectedChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var canvas = (KataElevationCanvas)sender;
        int index = (int)args.NewValue;
        if (canvas.Elevation is not { } elevation || canvas._viewport is not { } viewport || index < 0 || index >= elevation.Columns.Count) return;

        // A column picked in the table is brought into view sideways, without changing the zoom or the height.
        canvas._viewport = viewport.EnsureVisible(elevation.Columns[index].Extent, canvas.ActualWidth, MarginPx);
    }

    private static void OnFocusRequested(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var canvas = (KataElevationCanvas)sender;
        if (args.NewValue is not KataViewFocus focus) return;
        if (focus.ColumnIndex is { } index) canvas.FrameColumn(index);
        else canvas.FrameAll();
    }
}
