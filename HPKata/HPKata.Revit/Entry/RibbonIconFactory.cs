using System.Windows;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace HPKata.Revit.Entry;

/// <summary>
/// Dynamic vector icon factory for HPKata Revit ribbon buttons.
/// Produces 32x32 vector DrawingImages that adapt cleanly to Revit Dark/Light themes.
/// </summary>
public static class RibbonIconFactory
{
    private static readonly Brush SteelOrange = Freeze(new SolidColorBrush(Color.FromRgb(0xE0, 0x64, 0x1E)));
    private static readonly Brush AccentBlue = Freeze(new SolidColorBrush(Color.FromRgb(0x06, 0x96, 0xD7)));

    public static ImageSource CreateSetupIcon(bool isDark) =>
        Glyph(GetInk(isDark), "M12,4 H20 V8 H12 Z M4,12 H8 V20 H4 Z M24,12 H28 V20 H24 Z M12,24 H20 V28 H12 Z M10,10 H22 V22 H10 Z");

    public static ImageSource CreateDetailingIcon(bool isDark) =>
        Glyph(SteelOrange, "M4,6 H28 V8 H4 Z M6,12 H26 V14 H6 Z M8,18 H24 V20 H8 Z M4,24 H28 V26 H4 Z");

    public static ImageSource CreateScheduleIcon(bool isDark) =>
        Glyph(GetInk(isDark), "M2,4 H30 V28 H2 Z M4,6 H28 V10 H4 Z M4,12 H14 V26 H4 Z M16,12 H28 V26 H16 Z");

    public static ImageSource CreateCuttingStockIcon(bool isDark) =>
        Glyph(SteelOrange, "M2,8 H30 V12 H2 Z M4,18 H14 V22 H4 Z M18,18 H28 V22 H18 Z M15,16 H17 V24 H15 Z");

    public static ImageSource CreateExportExcelIcon(bool isDark) =>
        Glyph(AccentBlue, "M4,4 H28 V28 H4 Z M10,10 L14,16 L10,22 H13 L15.5,18 L18,22 H21 L17,16 L21,10 H18 L15.5,14 L13,10 Z");

    public static ImageSource CreateBeamIcon(bool isDark) =>
        Glyph(SteelOrange, "M2,8 H30 V14 H2 Z M2,16 H30 V24 H2 Z M6,18 H26 V22 H6 Z");

    public static ImageSource CreateColumnWallIcon(bool isDark) =>
        Glyph(GetInk(isDark), "M10,2 H22 V30 H10 Z M13,5 H19 V27 H13 Z");

    public static ImageSource CreateFoundationIcon(bool isDark) =>
        Glyph(SteelOrange, "M12,4 H20 V16 H12 Z M4,16 H28 V26 H4 Z");

    public static ImageSource CreateSlabIcon(bool isDark) =>
        Glyph(AccentBlue, "M3,6 H29 V26 H3 Z M3,12 H29 M3,18 H29 M11,6 V26 M21,6 V26");

    public static ImageSource CreateStairIcon(bool isDark) =>
        Glyph(GetInk(isDark), "M4,26 H12 V20 H18 V14 H24 V8 H28 V28 H4 Z");

    public static ImageSource CreateEtabsIcon(bool isDark) =>
        Glyph(AccentBlue, "M4,26 L16,4 L28,26 Z M10,20 H22");

    public static ImageSource CreateQsIcon(bool isDark) =>
        Glyph(SteelOrange, "M4,6 H28 V26 H4 Z M8,10 H24 V14 H8 Z M8,18 H18 V22 H8 Z");

    public static ImageSource CreateLayoutIcon(bool isDark) =>
        Glyph(GetInk(isDark), "M2,4 H30 V28 H2 Z M4,6 H20 V26 H4 Z M22,6 H28 V26 H22 Z");

    private static Brush GetInk(bool isDark) =>
        Freeze(new SolidColorBrush(isDark ? Color.FromRgb(0xE6, 0xE6, 0xE6) : Color.FromRgb(0x3C, 0x3C, 0x3C)));

    private static ImageSource Glyph(Brush brush, string pathData)
    {
        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing(brush, null, Geometry.Parse(pathData)));
        group.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 32, 32))));
        return Freeze(new DrawingImage(group));
    }

    private static T Freeze<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
