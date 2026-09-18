using System.Windows;
using System.Windows.Media;

namespace HPGeo.AutoCad.Loader.Ribbon;

/// <summary>
/// The Ribbon icon, drawn in code so nothing ships beside the DLL and it stays crisp at any DPI. One glyph
/// in a 32×32 box with even coordinates, so the 16-px small image AdWindows derives is an exact half. The
/// ink follows AutoCAD's COLORTHEME; the accent is the HP blue the other HP add-ins use.
/// </summary>
internal sealed class RibbonIcons
{
    private static readonly Brush Accent = Frozen(new SolidColorBrush(Color.FromRgb(0x06, 0x96, 0xD7)));

    public RibbonIcons(bool darkTheme)
    {
        var ink = Frozen(new SolidColorBrush(darkTheme ? Color.FromRgb(0xE6, 0xE6, 0xE6) : Color.FromRgb(0x3C, 0x3C, 0x3C)));
        // A globe (ring with a meridian and the equator) and a survey marker pin dropped on it.
        Kmz = Glyph(
            (ink, "M4,16 A12,12 0 1 0 28,16 A12,12 0 1 0 4,16 Z M6,16 A10,10 0 1 1 26,16 A10,10 0 1 1 6,16 Z"),
            (ink, "M4,15 H28 V17 H4 Z M15,4 H17 V28 H15 Z"),
            (Accent, "M22,4 A6,6 0 0 1 28,10 C28,14 22,20 22,20 C22,20 16,14 16,10 A6,6 0 0 1 22,4 Z M22,8 A2,2 0 1 0 22,12 A2,2 0 1 0 22,8 Z"));
    }

    /// <summary>Globe with a pin — export to KMZ.</summary>
    public ImageSource Kmz { get; }

    private static ImageSource Glyph(params (Brush Brush, string Path)[] parts)
    {
        var group = new DrawingGroup();
        foreach (var (brush, path) in parts) group.Children.Add(new GeometryDrawing(brush, null, Geometry.Parse(path)));
        // Keep the full 32×32 box so the glyph is centred the same way whatever its own extent.
        group.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 32, 32))));
        return Frozen(new DrawingImage(group));
    }

    private static T Frozen<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
