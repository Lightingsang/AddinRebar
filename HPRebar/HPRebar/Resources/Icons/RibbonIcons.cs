using System.Windows;
using System.Windows.Media;
// The SDK's global using of Autodesk.Revit.DB brings its own Color; this file only ever means the WPF one.
using Color = System.Windows.Media.Color;

namespace HPRebar.Resources.Icons;

/// <summary>
///     The ribbon icons, drawn in code: nothing to ship beside the DLL, crisp at any DPI, and the ink follows
///     Revit's UI theme. Every glyph lives in a 32×32 box whose coordinates are all even, so the 16-px small
///     image is an exact half — every edge lands on a pixel boundary at 100 % and 200 % DPI. Outlines are
///     drawn as filled rings (even-odd fill, the default of <see cref="Geometry.Parse"/>), never as strokes,
///     so nothing blurs when the ribbon scales them. The same file is linked into HPRebar.McpBridge, whose
///     one button uses the window-with-plug glyph shared with the AutoCAD and Navisworks bridges.
/// </summary>
public sealed class RibbonIcons
{
    /// <summary>Steel: the bars and ties every rebar glyph is about.</summary>
    private static readonly Brush Steel = Frozen(new SolidColorBrush(Color.FromRgb(0xE0, 0x64, 0x1E)));

    /// <summary>HP MCP blue, the accent of every HP MCP bridge icon.</summary>
    private static readonly Brush McpAccent = Frozen(new SolidColorBrush(Color.FromRgb(0x06, 0x96, 0xD7)));

    public RibbonIcons(bool darkTheme)
    {
        var ink = Frozen(new SolidColorBrush(darkTheme ? Color.FromRgb(0xE6, 0xE6, 0xE6) : Color.FromRgb(0x3C, 0x3C, 0x3C)));

        // Column section: concrete outline (2 px ring), one tie (2 px ring inside), four corner bars (4 px squares).
        ColumnRebar = Glyph(
            (ink, "M2,2 H30 V30 H2 Z M4,4 H28 V28 H4 Z"),
            (ink, "M6,6 H26 V26 H6 Z M8,8 H24 V24 H8 Z"),
            (Steel, "M8,8 H12 V12 H8 Z M20,8 H24 V12 H20 Z M8,20 H12 V24 H8 Z M20,20 H24 V24 H20 Z"));

        // Beam elevation: concrete outline (2 px ring), top and bottom bars (2 px), four stirrups (2 px, between the bars).
        BeamRebar = Glyph(
            (ink, "M2,4 H30 V28 H2 Z M4,6 H28 V26 H4 Z"),
            (Steel, "M6,8 H26 V10 H6 Z M6,22 H26 V24 H6 Z"),
            (Steel, "M6,10 H8 V22 H6 Z M12,10 H14 V22 H12 Z M18,10 H20 V22 H18 Z M24,10 H26 V22 H24 Z"));

        // Footing section: column stub over a wide pad (one 2 px ring), two bottom mats (bars 2 px, dowel dots 2 px).
        FoundationRebar = Glyph(
            (ink, "M12,2 H20 V16 H30 V30 H2 V16 H12 Z M14,4 H18 V18 H4 V28 H28 V18 H18 Z"),
            (Steel, "M6,24 H26 V26 H6 Z"),
            (Steel, "M6,20 H8 V22 H6 Z M12,20 H14 V22 H12 Z M18,20 H20 V22 H18 Z M24,20 H26 V22 H24 Z"),
            (Steel, "M14,6 H16 V16 H14 Z"));

        // Execute: a play triangle — the template command the button still points at.
        Execute = Glyph((ink, "M8,4 L28,16 L8,28 Z"));

        // A window with a title bar (the status window) holding a plug (the connection the bridge offers):
        // frame 2 px, title bar 4 px; plug = two prongs, body, cable. Identical to the AutoCAD and Navisworks bridges.
        McpBridge = Glyph(
            (ink, "M2,4 H30 V28 H2 Z M4,8 H28 V26 H4 Z"),
            (McpAccent, "M10,10 H12 V14 H10 Z M20,10 H22 V14 H20 Z M8,14 H24 V20 H8 Z M14,20 H18 V24 H14 Z"));
    }

    /// <summary>Column section with a tie and four corner bars.</summary>
    public ImageSource ColumnRebar { get; }

    /// <summary>Beam elevation with top/bottom bars and stirrups.</summary>
    public ImageSource BeamRebar { get; }

    /// <summary>Footing with a column stub and the bottom reinforcement mat.</summary>
    public ImageSource FoundationRebar { get; }

    /// <summary>Play triangle for the template "Execute" command.</summary>
    public ImageSource Execute { get; }

    /// <summary>Window with a plug — the MCP bridge status window.</summary>
    public ImageSource McpBridge { get; }

    /// <summary>True when Revit's UI theme is dark (2024+); older versions have one light theme.</summary>
    public static bool RevitIsDark()
    {
        // Multi-version: UIThemeManager exists since Revit 2024
#if REVIT2024_OR_GREATER
        return Autodesk.Revit.UI.UIThemeManager.CurrentTheme == Autodesk.Revit.UI.UITheme.Dark;
#else
        return false;
#endif
    }

    private static ImageSource Glyph(params (Brush Brush, string Path)[] parts)
    {
        var group = new DrawingGroup();
        foreach (var (brush, path) in parts) group.Children.Add(new GeometryDrawing(brush, null, Geometry.Parse(path)));
        // Keep the full 32×32 box so every glyph is centred the same way whatever its own extent.
        group.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 32, 32))));
        return Frozen(new DrawingImage(group));
    }

    private static T Frozen<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
