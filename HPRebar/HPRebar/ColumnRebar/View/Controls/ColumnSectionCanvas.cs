using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using HPRebar.ColumnRebar.ViewModel;
using HPRebar.Core.ColumnRebar;

namespace HPRebar.ColumnRebar.View.Controls;

/// <summary>
///     Plan view of the column being edited. Shows the bar arrangement the current counts produce, so the
///     user can see the layout before anything is built.
/// </summary>
public sealed class ColumnSectionCanvas : FrameworkElement
{
    private static readonly System.TimeSpan RedrawDelay = System.TimeSpan.FromMilliseconds(50);

    public static readonly DependencyProperty ColumnProperty = DependencyProperty.Register(
        nameof(Column), typeof(ColumnSpecEditor), typeof(ColumnSectionCanvas),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnColumnChanged));

    public static readonly DependencyProperty SelectedBarNumberProperty = DependencyProperty.Register(
        nameof(SelectedBarNumber), typeof(int), typeof(ColumnSectionCanvas),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>
    ///     Drawn faintly behind <see cref="Column"/>, for the dowel tabs where the section above and the
    ///     section below are compared.
    /// </summary>
    public static readonly DependencyProperty GhostColumnProperty = DependencyProperty.Register(
        nameof(GhostColumn), typeof(ColumnSpecEditor), typeof(ColumnSectionCanvas),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    private readonly DispatcherTimer _redraw;

    public ColumnSectionCanvas()
    {
        _redraw = new DispatcherTimer(DispatcherPriority.Background) { Interval = RedrawDelay };
        _redraw.Tick += (_, _) =>
        {
            _redraw.Stop();
            InvalidateMeasure();
            InvalidateVisual();
        };

        Unloaded += (_, _) =>
        {
            _redraw.Stop();
            Detach(Column);
        };
    }

    public ColumnSpecEditor? Column
    {
        get => (ColumnSpecEditor?)GetValue(ColumnProperty);
        set => SetValue(ColumnProperty, value);
    }

    public ColumnSpecEditor? GhostColumn
    {
        get => (ColumnSpecEditor?)GetValue(GhostColumnProperty);
        set => SetValue(GhostColumnProperty, value);
    }

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
        var column = Column;
        var painter = Painter();

        if (column is null || painter is null) return;

        DrawPrimitives.PixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        var palette = CanvasPalette.From(this);

        drawingContext.DrawRectangle(palette.Fill, null, new Rect(0, 0, painter.Width, painter.Height));

        // The section that is not being edited goes down first, dashed, so the live one reads on top.
        if (GhostColumn is { } ghost)
        {
            new SectionPainter(palette, ghost.Section, Scale(column)).Paint(drawingContext, ghost, 0, dashed: true);
        }

        painter.Paint(drawingContext, column, SelectedBarNumber);
    }

    private SectionPainter? Painter()
    {
        var column = Column;

        return column is null ? null : new SectionPainter(CanvasPalette.From(this), column.Section, Scale(column));
    }

    /// <summary>
    ///     One scale for both sections, taken from the larger of the two, so a narrower section above reads
    ///     as genuinely narrower rather than being redrawn to fill the same box.
    /// </summary>
    private double Scale(ColumnSpecEditor column)
    {
        var sections = GhostColumn is { } ghost
            ? new[] { column.Section, ghost.Section }
            : new[] { column.Section };

        return CanvasScaleCalculator.Section(sections);
    }

    private static void OnColumnChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var canvas = (ColumnSectionCanvas)sender;

        canvas.Detach(args.OldValue as ColumnSpecEditor);
        canvas.Attach(args.NewValue as ColumnSpecEditor);
        canvas.InvalidateVisual();
    }

    private void Attach(ColumnSpecEditor? column)
    {
        if (column is not null) column.PropertyChanged += OnSourceChanged;
    }

    private void Detach(ColumnSpecEditor? column)
    {
        if (column is not null) column.PropertyChanged -= OnSourceChanged;
    }

    private void OnSourceChanged(object? sender, PropertyChangedEventArgs args)
    {
        _redraw.Stop();
        _redraw.Start();
    }
}
