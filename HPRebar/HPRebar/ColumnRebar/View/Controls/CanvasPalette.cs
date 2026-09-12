using System.Windows;
using System.Windows.Media;

// The Revit SDK's implicit usings pull in Autodesk.Revit.DB, which has its own Point, Color and
// FormattedText. These aliases keep the drawing code on the WPF types.
using Point = System.Windows.Point;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using Brush = System.Windows.Media.Brush;
using FormattedText = System.Windows.Media.FormattedText;
using Serilog;

namespace HPRebar.ColumnRebar.View.Controls;

/// <summary>
///     The pens and brushes the preview drawings use, resolved from the active theme so the canvas follows
///     the light or dark swap like the rest of the window. Everything is frozen: the same pen is reused on
///     every render pass.
/// </summary>
internal sealed class CanvasPalette
{
    private const double MainBarThickness = 2.0;
    private const double StirrupThickness = 1.2;
    private const double OutlineThickness = 0.8;
    private const double DimensionThickness = 0.5;

    private CanvasPalette(
        Brush fill,
        Pen outline,
        Pen mainBar,
        Pen selectedMainBar,
        Pen stirrup,
        Pen dimension,
        Brush text,
        Brush highlight)
    {
        Fill = fill;
        Outline = outline;
        MainBar = mainBar;
        SelectedMainBar = selectedMainBar;
        Stirrup = stirrup;
        Dimension = dimension;
        Text = text;
        Highlight = highlight;
    }

    public Brush Fill { get; }

    public Pen Outline { get; }

    public Pen MainBar { get; }

    public Pen SelectedMainBar { get; }

    public Pen Stirrup { get; }

    public Pen Dimension { get; }

    public Brush Text { get; }

    /// <summary>Wash drawn over the column segment the user is editing.</summary>
    public Brush Highlight { get; }

    /// <summary>A dashed copy of a pen, for showing the section that is not the one being edited.</summary>
    public static Pen Dashed(Pen source)
    {
        var pen = new Pen(source.Brush, source.Thickness)
        {
            DashStyle = new DashStyle(new double[] { 4, 3 }, 0)
        };

        pen.Freeze();

        return pen;
    }

    /// <summary>
    ///     Reads the canvas colours out of the element's resources. A theme that is missing a key falls back
    ///     to a readable grey rather than throwing, so a partial theme still renders something.
    /// </summary>
    public static CanvasPalette From(FrameworkElement element)
    {
        var fill = Resolve(element, "Brush.Canvas.Fill", Colors.Gainsboro);
        var outline = Resolve(element, "Brush.Canvas.Bound", Colors.Black);
        var mainBar = Resolve(element, "Brush.Canvas.MainBar", Colors.Black);
        var selected = Resolve(element, "Brush.Canvas.MainBar.Selected", Colors.Red);
        var stirrup = Resolve(element, "Brush.Canvas.Stirrup", Colors.DarkGreen);
        var tag = Resolve(element, "Brush.Canvas.Tag", Colors.Chocolate);

        var highlight = new SolidColorBrush(((SolidColorBrush)selected).Color) { Opacity = 0.15 };
        highlight.Freeze();

        return new CanvasPalette(
            fill,
            FrozenPen(outline, OutlineThickness),
            FrozenPen(mainBar, MainBarThickness),
            FrozenPen(selected, MainBarThickness),
            FrozenPen(stirrup, StirrupThickness),
            FrozenPen(tag, DimensionThickness),
            tag,
            highlight);
    }

    private static Pen FrozenPen(Brush brush, double thickness)
    {
        var pen = new Pen(brush, thickness);
        pen.Freeze();

        return pen;
    }

    private static Brush Resolve(FrameworkElement element, string key, Color fallback)
    {
        if (element.TryFindResource(key) is Brush brush)
        {
            if (brush.CanFreeze && !brush.IsFrozen) brush = brush.Clone();
            if (brush.CanFreeze) brush.Freeze();

            return brush;
        }

        Log.Warning("Canvas brush {Key} is missing from the theme; falling back to a default colour", key);

        var solid = new SolidColorBrush(fallback);
        solid.Freeze();

        return solid;
    }
}
