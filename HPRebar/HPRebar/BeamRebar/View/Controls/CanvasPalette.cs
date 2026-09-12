using System;
using System.Windows;
using System.Windows.Media;
using Serilog;

// Alias WPF types against Revit SDK implicit usings
using Point = System.Windows.Point;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using Brush = System.Windows.Media.Brush;
using FormattedText = System.Windows.Media.FormattedText;

namespace HPRebar.BeamRebar.View.Controls;

/// <summary>
/// Resolves and freezes Pens and Brushes from dynamic theme tokens ({DynamicResource Brush.Canvas.*}).
/// Ensures zero GC pressure during rapid canvas rendering while remaining fully reactive to Dark/Light mode swaps.
/// </summary>
internal sealed class CanvasPalette
{
    private const double OutlineThickness = 1.0;
    private const double MainBarThickness = 2.0;
    private const double SelectedThickness = 2.5;
    private const double AddBarThickness = 1.8;
    private const double SideBarThickness = 1.2;
    private const double StirrupThickness = 1.0;
    private const double DimensionThickness = 0.6;

    public Brush Fill { get; }
    public Pen Outline { get; }
    public Pen MainBar { get; }
    public Pen SelectedMainBar { get; }
    public Pen AddTopBar { get; }
    public Pen AddBottomBar { get; }
    public Pen SideBar { get; }
    public Pen Stirrup { get; }
    public Pen Dimension { get; }
    public Pen DashedDimension { get; }
    public Pen DashedSideBar { get; }
    public Brush Text { get; }
    public Brush SupportFill { get; }
    public Brush Highlight { get; }

    private static readonly DashStyle DefaultDashStyle = CreateDefaultDashStyle();

    private static DashStyle CreateDefaultDashStyle()
    {
        var style = new DashStyle(new double[] { 4, 3 }, 0);
        style.Freeze();
        return style;
    }

    private CanvasPalette(
        Brush fill,
        Pen outline,
        Pen mainBar,
        Pen selectedMainBar,
        Pen addTopBar,
        Pen addBottomBar,
        Pen sideBar,
        Pen stirrup,
        Pen dimension,
        Pen dashedDimension,
        Pen dashedSideBar,
        Brush text,
        Brush supportFill,
        Brush highlight)
    {
        Fill = fill;
        Outline = outline;
        MainBar = mainBar;
        SelectedMainBar = selectedMainBar;
        AddTopBar = addTopBar;
        AddBottomBar = addBottomBar;
        SideBar = sideBar;
        Stirrup = stirrup;
        Dimension = dimension;
        DashedDimension = dashedDimension;
        DashedSideBar = dashedSideBar;
        Text = text;
        SupportFill = supportFill;
        Highlight = highlight;
    }

    /// <summary>Creates a frozen dashed copy of a pen for secondary indications.</summary>
    public static Pen Dashed(Pen source)
    {
        var pen = new Pen(source.Brush, source.Thickness)
        {
            DashStyle = DefaultDashStyle
        };
        pen.Freeze();
        return pen;
    }

    /// <summary>
    /// Reads canvas brushes from the active FrameworkElement theme resources.
    /// Falls back to legible defaults if any token is missing.
    /// </summary>
    public static CanvasPalette From(FrameworkElement element)
    {
        var fill = Resolve(element, "Brush.Canvas.Fill", Color.FromRgb(0x25, 0x25, 0x26));
        var outline = Resolve(element, "Brush.Canvas.Bound", Color.FromRgb(0xCC, 0xCC, 0xCC));
        var mainBar = Resolve(element, "Brush.Canvas.MainBar", Color.FromRgb(0xE5, 0x48, 0x4D));
        var selected = Resolve(element, "Brush.Canvas.MainBar.Selected", Color.FromRgb(0xF5, 0xA6, 0x23));
        var stirrup = Resolve(element, "Brush.Canvas.Stirrup", Color.FromRgb(0x16, 0xC1, 0x72));
        var tag = Resolve(element, "Brush.Canvas.Tag", Color.FromRgb(0x8E, 0x8E, 0x93));

        var supportFill = new SolidColorBrush(Color.FromArgb(50, 150, 150, 150));
        supportFill.Freeze();

        var highlightColor = selected is SolidColorBrush scb ? scb.Color : Colors.Orange;
        var highlight = new SolidColorBrush(Color.FromArgb(40, highlightColor.R, highlightColor.G, highlightColor.B));
        highlight.Freeze();

        return new CanvasPalette(
            fill,
            FrozenPen(outline, OutlineThickness),
            FrozenPen(mainBar, MainBarThickness),
            FrozenPen(selected, SelectedThickness),
            FrozenPen(mainBar, AddBarThickness),
            FrozenPen(mainBar, AddBarThickness),
            FrozenPen(mainBar, SideBarThickness),
            FrozenPen(stirrup, StirrupThickness),
            FrozenPen(tag, DimensionThickness),
            FrozenDashedPen(tag, DimensionThickness),
            FrozenDashedPen(mainBar, SideBarThickness),
            tag,
            supportFill,
            highlight);
    }

    private static Pen FrozenDashedPen(Brush brush, double thickness)
    {
        var pen = new Pen(brush, thickness)
        {
            DashStyle = DefaultDashStyle
        };
        pen.Freeze();
        return pen;
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

        Log.Warning("Canvas brush token {Key} not found; falling back to default color", key);
        var solid = new SolidColorBrush(fallback);
        solid.Freeze();
        return solid;
    }
}
