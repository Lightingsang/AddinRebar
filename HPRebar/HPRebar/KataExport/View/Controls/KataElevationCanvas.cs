using System.Windows;
using System.Windows.Media;
using HPRebar.Core.KataExport.Calculators;
using HPRebar.Core.KataExport.Models;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.KataExport.ViewModel;
using Serilog;

// WPF types, not the Revit ones the SDK imports globally.
using Brush = System.Windows.Media.Brush;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// The beam run as an elevation, one scale both ways and zoomed and panned like AutoCAD (mouse handling in
/// <c>KataElevationCanvas.Input.cs</c>). A click selects the Kata column under the cursor, two-way bound to the
/// preview table; the view model frames a span or the whole run through <see cref="FocusRequest"/>.
/// </summary>
public sealed partial class KataElevationCanvas : FrameworkElement
{
    private readonly HashSet<string> _overlayFailures = new();

    private const double MarginPx = 36.0;
    private const double FocusMarginPx = 60.0;

    /// <summary>Wheel limit — 20 px per mm, deep enough to read a 2.5 mm Kata tag text at any print scale.</summary>
    private const double MaxScale = 20.0;

    /// <summary>Zooming out stops at this fraction of "zoom extents".</summary>
    private const double MinScaleOfExtents = 1.0 / 20.0;

    /// <summary>Framing a span never zooms in further than this, so its neighbours and labels stay in view.</summary>
    private const double FocusMaxScale = 1.2;

    /// <summary>Room kept over and under Kata's elevation when it is framed.</summary>
    private const double KataMarginPx = 12.0;

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
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnCardChanged));

    public static readonly DependencyProperty RebarStationMapProperty = DependencyProperty.Register(
        nameof(RebarStationMap), typeof(KataStationMap), typeof(KataElevationCanvas),
        new FrameworkPropertyMetadata(KataStationMap.Identity, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ShowRebarProperty = DependencyProperty.Register(
        nameof(ShowRebar), typeof(bool), typeof(KataElevationCanvas),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender, OnCardChanged));

    public static readonly DependencyProperty ShowBarTagsProperty = DependencyProperty.Register(
        nameof(ShowBarTags), typeof(bool), typeof(KataElevationCanvas),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender, OnCardChanged));

    public static readonly DependencyProperty ShowSectionProperty = DependencyProperty.Register(
        nameof(ShowSection), typeof(bool), typeof(KataElevationCanvas),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender, OnCardChanged));

    private KataCanvasPalette? _palette;
    private KataElevationViewport? _viewport;
    private bool _userFramed;
    private KataRebarDrawing? _drawing;

    // Plans whose drawing could not be built, or not painted: not tried again on every repaint until the plan changes.
    private KataRebarPlan? _failedPlan;
    private KataRebarPlan? _failedPaint;

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

    /// <summary>Kata's bar numbers and tags over the drawn bars (and on the stirrup zones).</summary>
    public bool ShowBarTags
    {
        get => (bool)GetValue(ShowBarTagsProperty);
        set => SetValue(ShowBarTagsProperty, value);
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

        var viewport = _viewport ??= Frame(elevation, FrameRange(elevation, elevation.Bounds), MarginPx);
        var (above, below) = Band();
        var scene = new KataElevationScene(elevation, viewport, ActualWidth, ActualHeight, SelectedColumnIndex,
            above * viewport.Scale, below * viewport.Scale);
        var map = RebarStationMap ?? KataStationMap.Identity;
        var drawing = Drawing();

        // With the bars shown the run is drawn as Kata's elevation; without them (or if that fails) as read from Revit.
        bool kata = KataMode && drawing is not null
            && Overlay("kata drawing", () => new KataElevationCadPainter(scene, palette, draw, drawing.Elevation, map).Paint());
        if (KataMode && !kata)
        {
            // Frame again for the labels of the plain elevation and paint that instead.
            _failedPaint = RebarPlan;
            if (!_userFramed) _viewport = null;
            Dispatcher.BeginInvoke(new Action(InvalidateVisual));
        }

        if (kata)
        {
            if (ShowBarTags) Overlay("bar tags", () => new KataElevationBarTagPainter(scene, palette, draw, drawing!, map).Paint());
        }
        else
        {
            Overlay("elevation", () => new KataElevationPainter(scene, palette, draw).Paint());
            Overlay("annotations", () => new KataElevationAnnotations(scene, palette, draw).Paint());
        }

        if (ShowSection && drawing is not null)
            Overlay("sections", () => new KataElevationSectionPainter(scene, drawing, map, palette, draw, markers: !kata).Paint());
    }

    /// <summary>
    /// Draws one layer of the drawing. An exception escaping OnRender takes the whole modeless window down inside Revit,
    /// so a failing overlay is logged (once per message, the canvas re-renders on every mouse move) and skipped.
    /// </summary>
    private bool Overlay(string name, Action paint)
    {
        try
        {
            paint();
            return true;
        }
        catch (Exception ex)
        {
            if (_overlayFailures.Add($"{name}|{ex.GetType().Name}|{ex.TargetSite}"))
                Log.Error(ex, "Kata Export: drawing the {Overlay} overlay failed", name);
            return false;
        }
    }

    /// <summary>The plan's tags, cuts and drafted bars, rebuilt only when the plan changes.</summary>
    private KataRebarDrawing? Drawing()
    {
        if (RebarPlan is not { } plan || ReferenceEquals(plan, _failedPlan))
        {
            _drawing = null;
            return null;
        }

        try
        {
            return _drawing = KataRebarDrawing.For(plan, _drawing);
        }
        catch (Exception ex)
        {
            _failedPlan = plan;
            if (_overlayFailures.Add($"drawing|{ex.GetType().Name}|{ex.TargetSite}"))
                Log.Error(ex, "Kata Export: laying out the bars of the elevation failed");
            return null;
        }
    }

    private void OnResized()
    {
        if (!_userFramed) _viewport = null;
        InvalidateVisual();
    }

    /// <summary>The section card or the tags appearing or going change the room left for the run: frame it again unless the user zoomed.</summary>
    private static void OnCardChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var canvas = (KataElevationCanvas)sender;
        if (!canvas._userFramed) canvas._viewport = null;
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
        canvas._viewport = viewport.EnsureVisible(elevation.Columns[index].Extent, canvas.UsableWidth(), MarginPx);
    }

    private static void OnFocusRequested(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var canvas = (KataElevationCanvas)sender;
        if (args.NewValue is not KataViewFocus focus) return;
        if (focus.ColumnIndex is { } index) canvas.FrameColumn(index);
        else canvas.FrameAll();
    }
}
