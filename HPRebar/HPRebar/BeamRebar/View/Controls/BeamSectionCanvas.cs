using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using HPRebar.BeamRebar.ViewModel;
using HPRebar.Core.BeamRebar.Calculators;

// Alias WPF types against Revit SDK implicit usings
using Point = System.Windows.Point;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using Brush = System.Windows.Media.Brush;
using FormattedText = System.Windows.Media.FormattedText;

namespace HPRebar.BeamRebar.View.Controls;

/// <summary>
/// Interactive WPF FrameworkElement preview canvas rendering the beam cross-section cut.
/// Updates reactively when cover, dimensions, main bars, skin bars, or ties change.
/// </summary>
public sealed class BeamSectionCanvas : FrameworkElement
{
    private static readonly TimeSpan RedrawDelay = TimeSpan.FromMilliseconds(50);

    public static readonly DependencyProperty SessionProperty = DependencyProperty.Register(
        nameof(Session), typeof(BeamRebarSession), typeof(BeamSectionCanvas),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnSessionChanged));

    public static readonly DependencyProperty IsSupportSectionProperty = DependencyProperty.Register(
        nameof(IsSupportSection), typeof(bool), typeof(BeamSectionCanvas),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    private readonly DispatcherTimer _redraw;
    private CanvasPalette? _palette;

    public BeamSectionCanvas()
    {
        _redraw = new DispatcherTimer(DispatcherPriority.Background) { Interval = RedrawDelay };
        _redraw.Tick += (_, _) =>
        {
            _redraw.Stop();
            InvalidateMeasure();
            InvalidateVisual();
        };

        Loaded += (_, _) => _palette = null;
        Unloaded += (_, _) =>
        {
            _redraw.Stop();
            Detach(Session);
            _palette = null;
        };
    }

    public BeamRebarSession? Session
    {
        get => (BeamRebarSession?)GetValue(SessionProperty);
        set => SetValue(SessionProperty, value);
    }

    public bool IsSupportSection
    {
        get => (bool)GetValue(IsSupportSectionProperty);
        set => SetValue(IsSupportSectionProperty, value);
    }

    internal void InvalidatePalette()
    {
        _palette = null;
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var session = Session;
        if (session is null || session.Stack.Spans.Count == 0)
        {
            return new Size(0, 0);
        }

        double width = double.IsInfinity(availableSize.Width) || availableSize.Width <= 0 ? 220 : availableSize.Width;
        double height = double.IsInfinity(availableSize.Height) || availableSize.Height <= 0 ? 220 : availableSize.Height;
        return new Size(width, height);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var session = Session;
        if (session is null || session.Stack.Spans.Count == 0) return;

        var span = session.SelectedSpan ?? session.Stack.Spans[0];
        double width = Math.Max(120.0, ActualWidth > 0 ? ActualWidth : 220.0);
        double height = Math.Max(120.0, ActualHeight > 0 ? ActualHeight : 220.0);

        BeamDrawPrimitives.PixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var palette = _palette ??= CanvasPalette.From(this);

        // Draw canvas surface background
        drawingContext.DrawRectangle(palette.Fill, null, new Rect(0, 0, width, height));

        var transform = BeamCanvasTransformCalculator.ComputeSectionTransform(
            span.Width, span.Height, width, height, 32.0);

        var painter = new BeamSectionPainter(palette, transform, session, span, IsSupportSection);
        painter.Paint(drawingContext);
    }

    private static void OnSessionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var canvas = (BeamSectionCanvas)sender;
        canvas._palette = null;
        canvas.Detach(args.OldValue as BeamRebarSession);
        canvas.Attach(args.NewValue as BeamRebarSession);
        canvas.InvalidateMeasure();
        canvas.InvalidateVisual();
    }

    private void Attach(BeamRebarSession? session)
    {
        if (session is null) return;
        session.PropertyChanged += OnSourceChanged;

        foreach (var s in session.SupportTopBars) s.PropertyChanged += OnSourceChanged;
        foreach (var s in session.SpanBottomBars) s.PropertyChanged += OnSourceChanged;
    }

    private void Detach(BeamRebarSession? session)
    {
        if (session is null) return;
        session.PropertyChanged -= OnSourceChanged;

        foreach (var s in session.SupportTopBars) s.PropertyChanged -= OnSourceChanged;
        foreach (var s in session.SpanBottomBars) s.PropertyChanged -= OnSourceChanged;
    }

    private void OnSourceChanged(object? sender, PropertyChangedEventArgs args)
    {
        _redraw.Stop();
        _redraw.Start();
    }
}
