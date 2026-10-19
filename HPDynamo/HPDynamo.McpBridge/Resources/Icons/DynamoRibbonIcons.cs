namespace HPDynamo.McpBridge.Resources.Icons;

using System;
using System.Windows;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

/// <summary>
///     Vector ribbon icons for HPDynamo MCP Bridge.
///     Rendered as sharp vector DrawingImage at any DPI, ink follows Revit UI theme.
/// </summary>
public static class DynamoRibbonIcons
{
    private static readonly Brush DynamoCyan = Frozen(new SolidColorBrush(Color.FromRgb(0x06, 0x96, 0xD7)));
    private static readonly Brush DynamoOrange = Frozen(new SolidColorBrush(Color.FromRgb(0xE0, 0x64, 0x1E)));

    public static ImageSource CreateMcpIcon(bool darkTheme)
    {
        var ink = Frozen(new SolidColorBrush(darkTheme ? Color.FromRgb(0xE6, 0xE6, 0xE6) : Color.FromRgb(0x3C, 0x3C, 0x3C)));

        // Window outline (32x32), title bar, and connected Dynamo graph nodes:
        // Window frame: M2,4 H30 V28 H2 Z M4,8 H28 V26 H4 Z
        // Left Node: M7,11 H13 V17 H7 Z
        // Right Node: M19,17 H25 V23 H19 Z
        // Wire & Plug: M13,13 H16 V19 H19 V21 H14 V15 H13 Z
        return Glyph(
            (ink, "M2,4 H30 V28 H2 Z M4,8 H28 V26 H4 Z"),
            (DynamoCyan, "M7,11 H13 V17 H7 Z M19,17 H25 V23 H19 Z"),
            (DynamoOrange, "M13,13 H16 V19 H19 V21 H14 V15 H13 Z"));
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

        // Full 32x32 bounding box so Revit scales both 32x32 and 16x16 accurately
        group.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 32, 32))));
        return Frozen(new DrawingImage(group));
    }

    private static T Frozen<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
