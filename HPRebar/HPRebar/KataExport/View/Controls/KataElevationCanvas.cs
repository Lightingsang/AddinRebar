using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using HPRebar.Core.KataExport.Calculators;
using HPRebar.Core.KataExport.Models;
using HPRebar.KataExport.ViewModel;

// WPF types, not the Revit ones the SDK imports globally.
using Brush = System.Windows.Media.Brush;
using Point = System.Windows.Point;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// The beam run as an elevation. Wheel zooms around the cursor, dragging pans, a click selects the Kata column
/// under the cursor (two-way bound to the preview table), a double click frames the whole run. The view model
/// frames a span or the whole run through <see cref="FocusRequest"/>.
/// </summary>
public sealed class KataElevationCanvas : FrameworkElement
{
    private const double MarginPx = 36.0;
    private const double FocusMarginPx = 60.0;
    private const double MaxScale = 1.2;
    private const double ZeroWidthReachPx = 7.0;
    private const double DragThresholdPx = 4.0;

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

    private KataCanvasPalette? _palette;
    private KataElevationViewport? _viewport;
    private bool _userFramed;
    private Point? _pressAt;
    private double _lastDragX;
    private bool _dragging;

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

    protected override void OnRender(DrawingContext drawingContext)
    {
        var palette = _palette ??= KataCanvasPalette.From(this);
        var draw = new KataDrawPrimitives(drawingContext, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        if (Elevation is not { } elevation || ActualWidth < 1 || ActualHeight < 1)
        {
            draw.Box(palette.Fill, null, 0, 0, ActualWidth, ActualHeight);
            return;
        }

        var viewport = _viewport ??= FitAll(elevation);
        var scene = new KataElevationScene(elevation, viewport, ActualWidth, ActualHeight, SelectedColumnIndex);
        new KataElevationPainter(scene, palette, draw).Paint();
        new KataElevationAnnotations(scene, palette, draw).Paint();
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (Elevation is not { } elevation || _viewport is not { } viewport) return;

        double factor = Math.Pow(1.2, e.Delta / 120.0);
        _viewport = viewport.ZoomAt(factor, e.GetPosition(this).X, MinScale(elevation), MaxScale);
        _userFramed = true;
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();
        if (e.ClickCount == 2)
        {
            FrameAll();
            e.Handled = true;
            return;
        }

        _pressAt = e.GetPosition(this);
        _lastDragX = _pressAt.Value.X;
        _dragging = false;
        CaptureMouse();
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_pressAt is not { } pressAt || _viewport is not { } viewport) return;
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            EndPress();
            return;
        }

        double x = e.GetPosition(this).X;
        if (!_dragging && Math.Abs(x - pressAt.X) < DragThresholdPx) return;

        _dragging = true;
        Cursor = Cursors.SizeWE;
        _viewport = viewport.PanBy(x - _lastDragX);
        _lastDragX = x;
        _userFramed = true;
        InvalidateVisual();
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_pressAt is null) return;

        if (!_dragging && Elevation is { } elevation && _viewport is { } viewport)
        {
            double station = viewport.ToStation(e.GetPosition(this).X);
            // SetCurrentValue keeps the binding to the view model alive whatever its mode.
            if (elevation.ColumnAt(station, ZeroWidthReachPx / viewport.Scale) is { } column)
                SetCurrentValue(SelectedColumnIndexProperty, column);
        }

        EndPress();
        e.Handled = true;
    }

    /// <summary>Alt+Tab or a Revit dialog can take the capture mid-drag; the next move must not keep panning.</summary>
    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        _pressAt = null;
        _dragging = false;
        Cursor = null;
    }

    private void EndPress()
    {
        _pressAt = null;
        _dragging = false;
        Cursor = null;
        if (IsMouseCaptured) ReleaseMouseCapture();
    }

    private KataElevationViewport FitAll(KataElevation elevation) =>
        KataElevationViewport.Fit(elevation.Bounds, Math.Max(1.0, ActualWidth), MarginPx);

    /// <summary>Zooming out stops a little past the whole run; zooming in stops at a legible 1.2 px per mm.</summary>
    private double MinScale(KataElevation elevation) => FitAll(elevation).Scale * 0.8;

    private void FrameAll()
    {
        if (Elevation is not { } elevation) return;
        _viewport = FitAll(elevation);
        _userFramed = false;
        InvalidateVisual();
    }

    private void FrameColumn(int index)
    {
        if (Elevation is not { } elevation || index < 0 || index >= elevation.Columns.Count || ActualWidth < 1) return;

        // A short span would be blown up past legibility; cap the zoom and keep it centred.
        var viewport = KataElevationViewport.Fit(elevation.FocusRange(index), ActualWidth, FocusMarginPx);
        if (viewport.Scale > MaxScale) viewport = viewport.ZoomAt(MaxScale / viewport.Scale, ActualWidth / 2.0, 0.0, MaxScale);
        _viewport = viewport;
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

        // A column picked in the table is brought into view without changing the zoom.
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
