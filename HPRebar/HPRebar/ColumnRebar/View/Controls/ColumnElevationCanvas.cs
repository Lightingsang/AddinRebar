using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using HPRebar.ColumnRebar.ViewModel;

namespace HPRebar.ColumnRebar.View.Controls;

/// <summary>
///     The elevation preview. Draws straight to a drawing context rather than building shape objects, so a
///     tall stack with hundreds of ties stays cheap to redraw while the user types.
/// </summary>
public sealed class ColumnElevationCanvas : FrameworkElement
{
    /// <summary>Redraws are coalesced over this window so a burst of keystrokes costs one render.</summary>
    private static readonly System.TimeSpan RedrawDelay = System.TimeSpan.FromMilliseconds(50);

    public static readonly DependencyProperty SessionProperty = DependencyProperty.Register(
        nameof(Session), typeof(ColumnRebarSession), typeof(ColumnElevationCanvas),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnSessionChanged));

    public static readonly DependencyProperty SelectedBarNumberProperty = DependencyProperty.Register(
        nameof(SelectedBarNumber), typeof(int), typeof(ColumnElevationCanvas),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

    private readonly DispatcherTimer _redraw;
    private ElevationPainter? _painter;

    public ColumnElevationCanvas()
    {
        _redraw = new DispatcherTimer(DispatcherPriority.Background) { Interval = RedrawDelay };
        _redraw.Tick += (_, _) =>
        {
            _redraw.Stop();
            _painter = null;
            InvalidateMeasure();
            InvalidateVisual();
        };

        Unloaded += (_, _) =>
        {
            _redraw.Stop();
            Detach(Session);
        };
    }

    public ColumnRebarSession? Session
    {
        get => (ColumnRebarSession?)GetValue(SessionProperty);
        set => SetValue(SessionProperty, value);
    }

    /// <summary>Bar to pick out in the accent colour. Zero highlights nothing.</summary>
    public int SelectedBarNumber
    {
        get => (int)GetValue(SelectedBarNumberProperty);
        set => SetValue(SelectedBarNumberProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var painter = Painter();

        return painter is null ? new Size(0, 0) : new Size(painter.Width, painter.Height);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var session = Session;
        var painter = Painter();

        if (session is null || painter is null) return;

        DrawPrimitives.PixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        // A background of its own, so the drawing reads the same whatever is behind the control.
        drawingContext.DrawRectangle(
            CanvasPalette.From(this).Fill, null, new Rect(0, 0, painter.Width, painter.Height));

        painter.Paint(drawingContext, session, session.SelectedColumnIndex, SelectedBarNumber);
    }

    private ElevationPainter? Painter()
    {
        var session = Session;

        if (session is null || session.Columns.Count == 0) return null;

        return _painter ??= new ElevationPainter(CanvasPalette.From(this), session.Stack.Sections);
    }

    private static void OnSessionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var canvas = (ColumnElevationCanvas)sender;

        canvas.Detach(args.OldValue as ColumnRebarSession);
        canvas.Attach(args.NewValue as ColumnRebarSession);
        canvas._painter = null;
        canvas.InvalidateVisual();
    }

    /// <summary>
    ///     Listens to every column's settings as well as the session itself, so any edit in any tab shows up
    ///     here without the tabs having to know the canvas exists.
    /// </summary>
    private void Attach(ColumnRebarSession? session)
    {
        if (session is null) return;

        session.PropertyChanged += OnSourceChanged;

        foreach (var column in session.Columns) column.PropertyChanged += OnSourceChanged;
    }

    private void Detach(ColumnRebarSession? session)
    {
        if (session is null) return;

        session.PropertyChanged -= OnSourceChanged;

        foreach (var column in session.Columns) column.PropertyChanged -= OnSourceChanged;
    }

    private void OnSourceChanged(object? sender, PropertyChangedEventArgs args)
    {
        _redraw.Stop();
        _redraw.Start();
    }
}
