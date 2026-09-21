using System.Windows;
using System.Windows.Media;

namespace HPAutoCad.Loader.Ribbon;

/// <summary>
/// Resolution-independent vector Ribbon icons for HPGeoLink in AutoCAD 2026.
/// All icons are drawn in code within a 32×32 box using even coordinates to guarantee crisp 16×16 downsampling.
/// The ink dynamically adjusts to AutoCAD's COLORTHEME (dark vs light); the accent is HP Blue (#0696D7).
/// </summary>
internal sealed class RibbonIcons
{
    private static readonly Brush Accent = Frozen(new SolidColorBrush(Color.FromRgb(0x06, 0x96, 0xD7)));

    public RibbonIcons(bool darkTheme)
    {
        var ink = Frozen(new SolidColorBrush(darkTheme ? Color.FromRgb(0xE6, 0xE6, 0xE6) : Color.FromRgb(0x3C, 0x3C, 0x3C)));

        // 1. KMZ: Globe with equator/meridian and survey pin
        Kmz = Glyph(
            (ink, "M4,16 A12,12 0 1 0 28,16 A12,12 0 1 0 4,16 Z M6,16 A10,10 0 1 1 26,16 A10,10 0 1 1 6,16 Z"),
            (ink, "M4,15 H28 V17 H4 Z M15,4 H17 V28 H15 Z"),
            (Accent, "M22,4 A6,6 0 0 1 28,10 C28,14 22,20 22,20 C22,20 16,14 16,10 A6,6 0 0 1 22,4 Z M22,8 A2,2 0 1 0 22,12 A2,2 0 1 0 22,8 Z"));

        // 2. Import: Drawing frame with inbound arrow
        Import = Glyph(
            (ink, "M4,12 H20 V28 H4 Z M6,14 H18 V26 H6 Z M6,20 H18 V22 H6 Z M11,14 H13 V26 H11 Z"),
            (Accent, "M20,4 H24 V12 H28 L22,18 L16,12 H20 Z"));

        // 3. Map: Quadrant satellite map tile grid with terrain peak
        Map = Glyph(
            (ink, "M4,6 H28 V26 H4 Z M6,8 H26 V24 H6 Z M6,15 H26 V17 H6 Z M15,8 H17 V24 H15 Z"),
            (Accent, "M6,22 L12,14 L16,18 L20,12 L26,22 Z"));

        // 4. Info: Circular diagnostic ring with 'i' badge
        Info = Glyph(
            (ink, "M4,16 A12,12 0 1 0 28,16 A12,12 0 1 0 4,16 Z M6,16 A10,10 0 1 1 26,16 A10,10 0 1 1 6,16 Z"),
            (Accent, "M14,8 H18 V12 H14 Z M12,14 H18 V22 H20 V24 H12 V22 H14 V16 H12 Z"));

        // 5. KmzScript: Console prompt window with pin
        KmzScript = Glyph(
            (ink, "M4,6 H28 V26 H4 Z M6,10 H26 V24 H6 Z M4,6 H28 V10 H4 Z"),
            (ink, "M8,14 L12,17 L8,20 L8,18 L10,17 L8,15 Z M14,20 H20 V22 H14 Z"),
            (Accent, "M20,12 A4,4 0 0 1 24,16 C24,19 20,22 20,22 C20,22 16,19 16,16 A4,4 0 0 1 20,12 Z M20,15 A1,1 0 1 0 20,17 A1,1 0 1 0 20,15 Z"));

        // 6. Plot: Printer chassis with outbound paper sheet and laser/plot accent line
        Plot = Glyph(
            (ink, "M4,16 H28 V26 H4 Z M6,18 H26 V24 H6 Z"),                         // Printer base body
            (ink, "M8,6 H20 L24,10 V16 H8 Z M20,6 V10 H24"),                        // Paper sheet feeding out with corner fold
            (Accent, "M10,12 H18 V14 H10 Z M10,20 H22 V22 H10 Z"),                  // Document content line + output feed line
            (Accent, "M22,17 A1.5,1.5 0 1 0 25,17 A1.5,1.5 0 1 0 22,17 Z")          // Status LED indicator
        );
    }

    public ImageSource Kmz { get; }
    public ImageSource Import { get; }
    public ImageSource Map { get; }
    public ImageSource Info { get; }
    public ImageSource KmzScript { get; }
    public ImageSource Plot { get; }

    private static ImageSource Glyph(params (Brush Brush, string Path)[] parts)
    {
        var group = new DrawingGroup();
        foreach (var (brush, path) in parts)
        {
            group.Children.Add(new GeometryDrawing(brush, null, Geometry.Parse(path)));
        }
        // Transparent 32x32 boundary ensures proper centering across all glyph extents
        group.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 32, 32))));
        return Frozen(new DrawingImage(group));
    }

    private static T Frozen<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
