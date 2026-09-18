using System.Windows;
using System.Windows.Media;

namespace HPCivil3d.McpBridge.Loader.Ribbon;

/// <summary>
///     The Ribbon icon, drawn in code: nothing to ship beside the DLL, nothing that depends on a path, crisp at
///     any DPI. One glyph in a 32×32 box whose coordinates are all even, so the 16-px small image AdWindows
///     derives from it is an exact half — every edge lands on a pixel boundary at 100 % and 200 % DPI, no
///     anti-aliased fringe. The frame's inner rectangle is a hole (even-odd fill, the default of
///     <see cref="Geometry.Parse"/>), so the glyph never paints the panel background. The ink follows
///     AutoCAD's COLORTHEME; the accent is the HP MCP blue shared with the Navisworks bridge icon.
/// </summary>
internal sealed class RibbonIcons
{
    private static readonly Brush Accent = Frozen(new SolidColorBrush(Color.FromRgb(0x06, 0x96, 0xD7)));

    public RibbonIcons(bool darkTheme)
    {
        var ink = Frozen(new SolidColorBrush(darkTheme ? Color.FromRgb(0xE6, 0xE6, 0xE6) : Color.FromRgb(0x3C, 0x3C, 0x3C)));
        // A window with a title bar (the status window) holding a plug (the connection the bridge offers):
        // frame 2 px, title bar 4 px; plug = two prongs, body, cable.
        McpBridge = Glyph(
            (ink, "M2,4 H30 V28 H2 Z M4,8 H28 V26 H4 Z"),
            (Accent, "M10,10 H12 V14 H10 Z M20,10 H22 V14 H20 Z M8,14 H24 V20 H8 Z M14,20 H18 V24 H14 Z"));
    }

    /// <summary>Window with a plug — the MCP bridge status window.</summary>
    public ImageSource McpBridge { get; }

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
