using System.Windows;
using System.Windows.Media;

namespace HPAutoCad.McpBridge.Loader.Ribbon;

/// <summary>
///     Vector icons for the Ribbon, drawn in code: nothing to ship beside the DLL, nothing that depends on a
///     path, crisp at any DPI. Each glyph is a path in a 32×32 box; the same drawing serves the 16 px and
///     32 px slots because WPF scales geometry.
/// </summary>
internal static class RibbonIcons
{
    private static readonly Brush Ink = Frozen(new SolidColorBrush(Color.FromRgb(0x3C, 0x3C, 0x3C)));
    private static readonly Brush Accent = Frozen(new SolidColorBrush(Color.FromRgb(0x1F, 0x6F, 0xC5)));
    private static readonly Brush Go = Frozen(new SolidColorBrush(Color.FromRgb(0x2E, 0x8B, 0x57)));
    private static readonly Brush Halt = Frozen(new SolidColorBrush(Color.FromRgb(0xC0, 0x39, 0x2B)));

    /// <summary>Window with a title bar — the status window.</summary>
    public static ImageSource Panel { get; } = Glyph(Accent, "M4,6 h24 v20 h-24 z M4,6 h24 v5 h-24 z", "M8,15 h10 v2 h-10 z M8,19 h16 v2 h-16 z");

    /// <summary>Play triangle — start the listener.</summary>
    public static ImageSource Start { get; } = Glyph(Go, "M9,5 L27,16 L9,27 z");

    /// <summary>Square — stop the listener.</summary>
    public static ImageSource Stop { get; } = Glyph(Halt, "M7,7 h18 v18 h-18 z");

    /// <summary>Circled "i" — status.</summary>
    public static ImageSource Info { get; } = Glyph(Accent, "M16,3 a13,13 0 1,0 0.01,0 z M14,13 h4 v12 h-4 z M14,7 h4 v4 h-4 z", null, evenOdd: true);

    /// <summary>Clipboard — copy the last script.</summary>
    public static ImageSource Clipboard { get; } = Glyph(Ink, "M8,6 h16 v22 h-16 z M12,3 h8 v5 h-8 z", "M11,13 h10 v2 h-10 z M11,18 h10 v2 h-10 z M11,23 h6 v2 h-6 z");

    /// <summary>Folder — the tool library.</summary>
    public static ImageSource Library { get; } = Glyph(Accent, "M3,8 h10 l3,3 h13 v15 h-26 z");

    /// <summary>Lines on a page — logs.</summary>
    public static ImageSource Logs { get; } = Glyph(Ink, "M7,3 h14 l5,5 v21 h-19 z", "M10,12 h12 v2 h-12 z M10,17 h12 v2 h-12 z M10,22 h8 v2 h-8 z");

    /// <summary>Shield — the audit trail.</summary>
    public static ImageSource Audit { get; } = Glyph(Ink, "M16,3 L28,7 v9 c0,8 -6,12 -12,13 c-6,-1 -12,-5 -12,-13 v-9 z", "M11,16 l3,3 l7,-7 l2,2 l-9,9 l-5,-5 z");

    /// <summary>Power symbol — auto-start toggle.</summary>
    public static ImageSource AutoStart { get; } = Glyph(Go, "M14,3 h4 v13 h-4 z", "M9,8 a10,10 0 1,0 14,0 l-2,2.5 a7,7 0 1,1 -10,0 z");

    /// <summary>Open book — the guide.</summary>
    public static ImageSource Guide { get; } = Glyph(Accent, "M3,6 h11 l2,2 l2,-2 h11 v20 h-11 l-2,2 l-2,-2 h-11 z", "M6,10 h7 v2 h-7 z M6,14 h7 v2 h-7 z M19,10 h7 v2 h-7 z M19,14 h7 v2 h-7 z");

    private static ImageSource Glyph(Brush brush, string outline, string? cutout = null, bool evenOdd = false)
    {
        var group = new DrawingGroup();
        var shape = Geometry.Parse(outline);
        if (evenOdd && shape is PathGeometry path) path.FillRule = FillRule.EvenOdd;
        group.Children.Add(new GeometryDrawing(brush, null, shape));
        if (cutout is not null) group.Children.Add(new GeometryDrawing(Brushes.White, null, Geometry.Parse(cutout)));
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
