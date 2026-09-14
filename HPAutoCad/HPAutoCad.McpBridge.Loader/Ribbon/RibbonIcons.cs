using System.Windows;
using System.Windows.Media;

namespace HPAutoCad.McpBridge.Loader.Ribbon;

/// <summary>
///     Vector icons for the Ribbon, drawn in code: nothing to ship beside the DLL, nothing that depends on a
///     path, crisp at any DPI. Each glyph is one path in a 32×32 box; overlapping sub-paths are holes
///     (even-odd fill, the default of <see cref="Geometry.Parse"/>), so a glyph never paints the panel
///     background and reads the same on the dark and the light Ribbon. Built per theme: the ink follows
///     AutoCAD's COLORTHEME, the accents stay saturated.
/// </summary>
internal sealed class RibbonIcons
{
    private readonly Brush _ink;
    private static readonly Brush Accent = Frozen(new SolidColorBrush(Color.FromRgb(0x2F, 0x86, 0xE0)));
    private static readonly Brush Go = Frozen(new SolidColorBrush(Color.FromRgb(0x3C, 0xB0, 0x6E)));
    private static readonly Brush Halt = Frozen(new SolidColorBrush(Color.FromRgb(0xD9, 0x4A, 0x3D)));

    public RibbonIcons(bool darkTheme)
    {
        _ink = Frozen(new SolidColorBrush(darkTheme ? Color.FromRgb(0xE6, 0xE6, 0xE6) : Color.FromRgb(0x3C, 0x3C, 0x3C)));
        Panel = Glyph(Accent, "M4,6 h24 v20 h-24 z M7,13 h18 v2 h-18 z M7,18 h12 v2 h-12 z");
        Start = Glyph(Go, "M9,5 L27,16 L9,27 z");
        Stop = Glyph(Halt, "M7,7 h18 v18 h-18 z");
        Info = Glyph(Accent, "M16,3 a13,13 0 1,0 0.01,0 z M14,13 h4 v12 h-4 z M14,7 h4 v4 h-4 z");
        Clipboard = Glyph(_ink, "M8,6 h16 v22 h-16 z M12,3 h8 v5 h-8 z M11,13 h10 v2 h-10 z M11,18 h10 v2 h-10 z M11,23 h6 v2 h-6 z");
        Library = Glyph(Accent, "M3,8 h10 l3,3 h13 v15 h-26 z");
        Logs = Glyph(_ink, "M7,3 h14 l5,5 v21 h-19 z M10,12 h12 v2 h-12 z M10,17 h12 v2 h-12 z M10,22 h8 v2 h-8 z");
        Audit = Glyph(_ink, "M16,3 L28,7 v9 c0,8 -6,12 -12,13 c-6,-1 -12,-5 -12,-13 v-9 z M11,16 l3,3 l7,-7 l2,2 l-9,9 l-5,-5 z");
        AutoStart = Glyph(Go, "M14,3 h4 v13 h-4 z M9,8 a10,10 0 1,0 14,0 l-2,2.5 a7,7 0 1,1 -10,0 z");
        Guide = Glyph(Accent, "M3,6 h11 l2,2 l2,-2 h11 v20 h-11 l-2,2 l-2,-2 h-11 z M6,10 h7 v2 h-7 z M6,14 h7 v2 h-7 z M19,10 h7 v2 h-7 z M19,14 h7 v2 h-7 z");
    }

    /// <summary>Window with a title bar — the status window.</summary>
    public ImageSource Panel { get; }

    /// <summary>Play triangle — start the listener.</summary>
    public ImageSource Start { get; }

    /// <summary>Square — stop the listener.</summary>
    public ImageSource Stop { get; }

    /// <summary>Circled "i" — status.</summary>
    public ImageSource Info { get; }

    /// <summary>Clipboard — copy the last script.</summary>
    public ImageSource Clipboard { get; }

    /// <summary>Folder — the tool library.</summary>
    public ImageSource Library { get; }

    /// <summary>Lines on a page — logs.</summary>
    public ImageSource Logs { get; }

    /// <summary>Shield — the audit trail.</summary>
    public ImageSource Audit { get; }

    /// <summary>Power symbol — auto-start toggle.</summary>
    public ImageSource AutoStart { get; }

    /// <summary>Open book — the guide.</summary>
    public ImageSource Guide { get; }

    private static ImageSource Glyph(Brush brush, string path)
    {
        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing(brush, null, Geometry.Parse(path)));
        // Keep the 32×32 box even for glyphs that do not reach the edges, so every icon sits on the same baseline.
        group.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 32, 32))));
        return Frozen(new DrawingImage(group));
    }

    private static T Frozen<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
