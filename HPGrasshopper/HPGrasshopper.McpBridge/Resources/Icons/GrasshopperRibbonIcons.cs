namespace HPGrasshopper.McpBridge.Resources.Icons;

using System;
using System.Windows;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

/// <summary>
///     Vector ribbon icons for HPGrasshopper MCP Bridge.
///     Rendered as sharp vector DrawingImage at any DPI, ink follows Revit UI theme.
/// </summary>
public static class GrasshopperRibbonIcons
{
    private static readonly Brush GhGreen = Frozen(new SolidColorBrush(Color.FromRgb(0x7C, 0xB3, 0x42)));
    private static readonly Brush GhBlue = Frozen(new SolidColorBrush(Color.FromRgb(0x06, 0x96, 0xD7)));

    public static ImageSource CreateMcpIcon(bool darkTheme)
    {
        var ink = Frozen(new SolidColorBrush(darkTheme ? Color.FromRgb(0xE6, 0xE6, 0xE6) : Color.FromRgb(0x3C, 0x3C, 0x3C)));

        // Grasshopper icon: stylized Grasshopper head / geometric nodes:
        // Window outline: M2,4 H30 V28 H2 Z M4,8 H28 V26 H4 Z
        // Grasshopper antenna / node 1: M8,12 H14 V18 H8 Z
        // Grasshopper antenna / node 2: M18,12 H24 V18 H18 Z
        // Central parametric curve connector: M14,15 C16,15 16,21 18,21
        return Glyph(
            (ink, "M2,4 H30 V28 H2 Z M4,8 H28 V26 H4 Z"),
            (GhGreen, "M8,12 H14 V18 H8 Z M18,12 H24 V18 H18 Z"),
            (GhBlue, "M14,15 H16 V22 H18"));
    }

    public static bool RevitIsDark()
    {
        try
        {
            return Autodesk.Revit.UI.UIThemeManager.CurrentTheme == Autodesk.Revit.UI.UITheme.Dark;
        }
        catch
        {
            return true;
        }
    }

    private static ImageSource Glyph(params (Brush Brush, string Path)[] parts)
    {
        var group = new DrawingGroup();
        foreach (var (brush, path) in parts)
        {
            group.Children.Add(new GeometryDrawing(brush, null, Geometry.Parse(path)));
        }

        group.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 32, 32))));
        return Frozen(new DrawingImage(group));
    }

    private static T Frozen<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
