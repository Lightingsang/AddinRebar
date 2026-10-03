using System.Linq;
using System.Windows.Media;

// WPF types, not the Revit ones the SDK imports globally.
using Brush = System.Windows.Media.Brush;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// Kata's lineweights in whole device pixels at any zoom, as AutoCAD shows them with LWDISPLAY on: 0.35 (bars,
/// stirrups) 3 px, 0.20 (concrete seen) 2 px, thinner 1 px; a linetype's dashes scaled as drawn.
/// </summary>
internal static class KataCadPens
{
    public const double BarPx = 3.0;
    public const double OutlinePx = 2.0;
    public const double ThinPx = 1.0;

    /// <summary>A pen <paramref name="px"/> device pixels wide, dashed by <paramref name="dashesMm"/> at <paramref name="scale"/> px/mm (solid once its dashes shrink under 2 px).</summary>
    public static Pen Lineweight(Brush brush, double px, double pixelsPerDip, double scale, double[]? dashesMm = null)
    {
        double thickness = px / pixelsPerDip;
        var pen = new Pen(brush, thickness) { LineJoin = PenLineJoin.Round, StartLineCap = PenLineCap.Flat, EndLineCap = PenLineCap.Flat };
        if (dashesMm is not null && dashesMm.Min() * scale >= 2.0)
            pen.DashStyle = new DashStyle(dashesMm.Select(d => d * scale / thickness), 0.0);
        pen.Freeze();
        return pen;
    }
}
