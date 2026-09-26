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
    public Pen Outline { get; private init; } = null!;
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

        return new KataCanvasPalette
        {
            Fill = fill,
            Text = bound,
            MutedText = tag,
            Accent = accent,
            Selected = selected,
            BeamFill = Tint(bound, 24),
            SupportFill = Tint(bound, 52),
            SelectionFill = Tint(selected, 48),
            Outline = Freeze(new Pen(bound, 1.2)),
            Dimension = Freeze(new Pen(tag, 0.8)),
            GridLine = Freeze(new Pen(accent, 0.8) { DashStyle = Dashes(6, 4) }),
            Bubble = Freeze(new Pen(accent, 1.2)),
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
