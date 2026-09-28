using System.Windows;
using System.Windows.Media;
using Serilog;

// WPF types, not the Revit ones the SDK imports globally.
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// Frozen pens and brushes of the elevation canvas, read from the window's theme tokens. The canvas drops its
/// palette whenever <c>Brush.Canvas.Fill</c> changes, so a Dark/Light switch repaints with the new tokens.
/// </summary>
internal sealed class KataCanvasPalette
{
    private KataCanvasPalette()
    {
    }

    public Brush Fill { get; private init; } = null!;
    public Brush Text { get; private init; } = null!;
    public Brush MutedText { get; private init; } = null!;
    public Brush Accent { get; private init; } = null!;
    public Brush Selected { get; private init; } = null!;
    public Brush BeamFill { get; private init; } = null!;
    public Brush SupportFill { get; private init; } = null!;
    public Brush SelectionFill { get; private init; } = null!;
    public Brush DimText { get; private init; } = null!;
    public Brush BubbleText { get; private init; } = null!;
    public Pen Outline { get; private init; } = null!;
    public Pen BreakLine { get; private init; } = null!;
    public Pen Dimension { get; private init; } = null!;
    public Pen GridLine { get; private init; } = null!;
    public Pen Bubble { get; private init; } = null!;
    public Pen Marker { get; private init; } = null!;
    public Pen FreeEnd { get; private init; } = null!;

    public static KataCanvasPalette From(FrameworkElement element)
    {
        var fill = Resolve(element, "Brush.Canvas.Fill", Color.FromRgb(0x25, 0x25, 0x26));
        var bound = Resolve(element, "Brush.Canvas.Bound", Color.FromRgb(0xCC, 0xCC, 0xCC));
        var tag = Resolve(element, "Brush.Canvas.Tag", Color.FromRgb(0x8E, 0x8E, 0x93));
        var accent = Resolve(element, "Brush.Accent", Color.FromRgb(0x06, 0x96, 0xD7));
        var selected = Resolve(element, "Brush.Canvas.MainBar.Selected", Color.FromRgb(0xF5, 0xA6, 0x23));
        var warning = Resolve(element, "Brush.Warning", Color.FromRgb(0xF5, 0xA6, 0x23));
        var marker = Resolve(element, "Brush.Canvas.MainBar", Color.FromRgb(0xE5, 0x48, 0x4D));

        bool isDark = fill is SolidColorBrush sb && (sb.Color.R * 0.299 + sb.Color.G * 0.587 + sb.Color.B * 0.114) < 128;

        // CAD Cyan outline (#00E5FF in dark mode, deep teal #00838F in light mode)
        var outlineBrush = new SolidColorBrush(isDark ? Color.FromRgb(0x00, 0xE5, 0xFF) : Color.FromRgb(0x00, 0x83, 0x8F));
        outlineBrush.Freeze();

        // CAD Green text (#00E676 in dark mode, deep forest green #1B5E20 in light mode)
        var dimTextBrush = new SolidColorBrush(isDark ? Color.FromRgb(0x00, 0xE6, 0x76) : Color.FromRgb(0x1B, 0x5E, 0x20));
        dimTextBrush.Freeze();

        // CAD Gray 128, 128, 128 (#808080) as requested for Dim lines, witness lines, grid lines, and break lines
        var cadGrayBrush = new SolidColorBrush(isDark ? Color.FromRgb(128, 128, 128) : Color.FromRgb(115, 115, 115));
        cadGrayBrush.Freeze();

        // Bubble circle and text (white/light silver in dark mode, dark slate in light mode)
        var bubbleBorderBrush = new SolidColorBrush(isDark ? Color.FromRgb(0xD0, 0xD0, 0xD0) : Color.FromRgb(0x42, 0x42, 0x42));
        bubbleBorderBrush.Freeze();
        var bubbleTextBrush = new SolidColorBrush(isDark ? Color.FromRgb(0xFF, 0xFF, 0xFF) : Color.FromRgb(0x21, 0x21, 0x21));
        bubbleTextBrush.Freeze();

        return new KataCanvasPalette
        {
            Fill = fill,
            Text = bound,
            MutedText = tag,
            Accent = accent,
            Selected = selected,
            DimText = dimTextBrush,
            BubbleText = bubbleTextBrush,
            BeamFill = Tint(outlineBrush, 12),
            SupportFill = Tint(outlineBrush, 20),
            SelectionFill = Tint(selected, 48),
            Outline = Freeze(new Pen(outlineBrush, 1.4)),
            BreakLine = Freeze(new Pen(cadGrayBrush, 1.2)),
            Dimension = Freeze(new Pen(cadGrayBrush, 0.8)),
            GridLine = Freeze(new Pen(cadGrayBrush, 0.8)),
            Bubble = Freeze(new Pen(bubbleBorderBrush, 1.2)),
            Marker = Freeze(new Pen(marker, 1.2) { DashStyle = Dashes(5, 2, 1, 2) }),
            FreeEnd = Freeze(new Pen(warning, 1.4) { DashStyle = Dashes(3, 2) })
        };
    }

    private static DashStyle Dashes(params double[] pattern)
    {
        var style = new DashStyle(pattern, 0);
        style.Freeze();
        return style;
    }

    private static Pen Freeze(Pen pen)
    {
        pen.Freeze();
        return pen;
    }

    /// <summary>The token's colour at <paramref name="alpha"/>, for translucent fills that work on both palettes.</summary>
    private static Brush Tint(Brush brush, byte alpha)
    {
        var color = brush is SolidColorBrush solid ? solid.Color : Color.FromRgb(0x80, 0x80, 0x80);
        var tint = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
        tint.Freeze();
        return tint;
    }

    private static Brush Resolve(FrameworkElement element, string key, Color fallback)
    {
        if (element.TryFindResource(key) is Brush brush)
        {
            var copy = brush.IsFrozen ? brush : brush.Clone();
            if (copy.CanFreeze) copy.Freeze();
            return copy;
        }

        Log.Warning("Kata Export: canvas token {Key} not found, using a default colour", key);
        var solid = new SolidColorBrush(fallback);
        solid.Freeze();
        return solid;
    }
}
