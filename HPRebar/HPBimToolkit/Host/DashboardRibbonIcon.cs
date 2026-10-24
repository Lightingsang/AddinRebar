using System.Windows;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace HPBimToolkit.Host;

/// <summary>
///     The Dashboard button glyph drawn in code: a panel with three rising bars. 32×32 with even coordinates, so the
///     ribbon's 16-px slot is an exact half; ink follows Revit's UI theme, the bars carry the HP accent.
/// </summary>
public static class DashboardRibbonIcon
{
    private static readonly Color Accent = Color.FromRgb(0x06, 0x96, 0xD7);

    public static ImageSource Create(bool isDarkTheme)
    {
        var ink = new SolidColorBrush(isDarkTheme ? Color.FromRgb(0xE6, 0xE6, 0xE6) : Color.FromRgb(0x3C, 0x3C, 0x3C));
        var accent = new SolidColorBrush(Accent);

        var drawing = new DrawingGroup();
        drawing.Children.Add(new GeometryDrawing(null, new Pen(ink, 2), new RectangleGeometry(new Rect(3, 5, 26, 22), 2, 2)));
        drawing.Children.Add(new GeometryDrawing(accent, null, new RectangleGeometry(new Rect(8, 18, 4, 6))));
        drawing.Children.Add(new GeometryDrawing(accent, null, new RectangleGeometry(new Rect(14, 14, 4, 10))));
        drawing.Children.Add(new GeometryDrawing(accent, null, new RectangleGeometry(new Rect(20, 10, 4, 14))));

        // Pin the canvas to 32×32 so the glyph keeps its margins when the ribbon scales it.
        drawing.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 32, 32))));

        var image = new DrawingImage(drawing);
        image.Freeze();
        return image;
    }
}
