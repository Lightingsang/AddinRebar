using System.Globalization;
using System.Windows;
using System.Windows.Media;

// WPF types, not the Revit ones the SDK imports globally.
using Brush = System.Windows.Media.Brush;
using FormattedText = System.Windows.Media.FormattedText;
using Point = System.Windows.Point;

namespace HPRebar.KataExport.View.Controls;

/// <summary>Lines, boxes, texts and dimension pieces for the elevation canvas, in screen pixels.</summary>
internal sealed class KataDrawPrimitives
{
    public const double TextSize = 11.0;
    public const double SmallTextSize = 9.5;
    private const double TickHalf = 4.0;

    private static readonly Typeface Regular = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
    private static readonly Typeface Bold = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

    private readonly DrawingContext _dc;
    private readonly double _pixelsPerDip;

    public KataDrawPrimitives(DrawingContext dc, double pixelsPerDip)
    {
        _dc = dc;
        _pixelsPerDip = pixelsPerDip;
    }

    public void Line(Pen pen, double x1, double y1, double x2, double y2) =>
        _dc.DrawLine(pen, new Point(x1, y1), new Point(x2, y2));

    public void Box(Brush? fill, Pen? pen, double left, double top, double right, double bottom) =>
        _dc.DrawRectangle(fill, pen, new Rect(new Point(left, top), new Point(right, bottom)));

    public void Circle(Brush? fill, Pen pen, double x, double y, double radius) =>
        _dc.DrawEllipse(fill, pen, new Point(x, y), radius, radius);

    public FormattedText Text(string value, Brush brush, double size = TextSize, bool bold = false) =>
        new(value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, bold ? Bold : Regular, size, brush, _pixelsPerDip);

    /// <summary>Draws <paramref name="text"/> centred on <paramref name="x"/> with its top at <paramref name="top"/>.</summary>
    public void Centered(FormattedText text, double x, double top) =>
        _dc.DrawText(text, new Point(x - text.Width / 2.0, top));

    public void At(FormattedText text, double left, double top) => _dc.DrawText(text, new Point(left, top));

    /// <summary>A dimension tick (CAD architectural 45-degree slash) at <paramref name="x"/> on the line at <paramref name="y"/>.</summary>
    public void Tick(Pen pen, double x, double y)
    {
        Line(pen, x - 3.0, y + 3.0, x + 3.0, y - 3.0);
    }

    /// <summary>Draws a horizontal breakline (zigzag cut mark) from <paramref name="x1"/> to <paramref name="x2"/> at <paramref name="y"/>, protruding past the edges by <paramref name="overhang"/>.</summary>
    public void BreakLineHorizontal(Pen pen, double x1, double x2, double y, double overhang = 4.5, double? centerOverride = null)
    {
        if (x2 < x1) (x1, x2) = (x2, x1);
        double left = x1 - overhang;
        double right = x2 + overhang;
        double width = right - left;
        if (width < 8.0)
        {
            Line(pen, left, y, right, y);
            return;
        }

        double d = System.Math.Min(4.0, width / 4.0);
        double mid = centerOverride is { } c && c - d >= left && c + d <= right ? c : (left + right) / 2.0;
        double h = 4.5;

        // Line segment before zigzag
        Line(pen, left, y, mid - d, y);
        // Zigzag peak and valley
        Line(pen, mid - d, y, mid - d / 3.0, y - h);
        Line(pen, mid - d / 3.0, y - h, mid + d / 3.0, y + h);
        Line(pen, mid + d / 3.0, y + h, mid + d, y);
        // Line segment after zigzag
        Line(pen, mid + d, y, right, y);
    }
}
