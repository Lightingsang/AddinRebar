using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

// The Revit SDK's implicit usings pull in Autodesk.Revit.DB, which has its own Point, Color and
// FormattedText. These aliases keep the drawing code on the WPF types.
using Point = System.Windows.Point;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using Brush = System.Windows.Media.Brush;
using FormattedText = System.Windows.Media.FormattedText;

namespace HPRebar.ColumnRebar.View.Controls;

/// <summary>
///     The handful of shapes the preview drawings are built from. Everything works in already-scaled canvas
///     coordinates; converting from millimetres is the caller's job.
/// </summary>
internal static class DrawPrimitives
{
    private const double CaptionSize = 11;
    private const double TickLength = 4;

    private static readonly Typeface Face = new("Segoe UI");

    public static void Line(DrawingContext context, Pen pen, double x1, double y1, double x2, double y2) =>
        context.DrawLine(pen, new Point(x1, y1), new Point(x2, y2));

    /// <summary>An unfilled box.</summary>
    public static void Box(DrawingContext context, Pen pen, double left, double top, double width, double height) =>
        context.DrawRectangle(null, pen, new Rect(left, top, width, height));

    public static void FilledBox(DrawingContext context, Brush brush, double left, double top, double width, double height) =>
        context.DrawRectangle(brush, null, new Rect(left, top, width, height));

    public static void Circle(DrawingContext context, Brush? fill, Pen? pen, double centreX, double centreY, double radius) =>
        context.DrawEllipse(fill, pen, new Point(centreX, centreY), radius, radius);

    /// <summary>An open polyline through the given points.</summary>
    public static void Polyline(DrawingContext context, Pen pen, IReadOnlyList<Point> points)
    {
        for (var i = 1; i < points.Count; i++)
        {
            context.DrawLine(pen, points[i - 1], points[i]);
        }
    }

    /// <summary>
    ///     A horizontal dimension: a line with end ticks and the measured value written above it.
    ///     <paramref name="value"/> is the number to print, in millimetres.
    /// </summary>
    public static void DimensionHorizontal(
        DrawingContext context,
        Pen pen,
        Brush textBrush,
        double left,
        double y,
        double width,
        double value)
    {
        if (width <= 0) return;

        Line(context, pen, left, y, left + width, y);
        Line(context, pen, left, y - TickLength, left, y + TickLength);
        Line(context, pen, left + width, y - TickLength, left + width, y + TickLength);

        var label = Text(value, textBrush);

        context.DrawText(label, new Point(left + (width - label.Width) / 2, y - label.Height - 2));
    }

    /// <summary>A vertical dimension, its value written to the left of the line and rotated upright.</summary>
    public static void DimensionVertical(
        DrawingContext context,
        Pen pen,
        Brush textBrush,
        double x,
        double top,
        double height,
        double value)
    {
        if (height <= 0) return;

        Line(context, pen, x, top, x, top + height);
        Line(context, pen, x - TickLength, top, x + TickLength, top);
        Line(context, pen, x - TickLength, top + height, x + TickLength, top + height);

        var label = Text(value, textBrush);
        var origin = new Point(x - label.Height - 2, top + (height + label.Width) / 2);

        context.PushTransform(new RotateTransform(-90, origin.X, origin.Y));
        context.DrawText(label, origin);
        context.Pop();
    }

    /// <summary>A short caption at a point, used for bar numbers and labels.</summary>
    public static void Caption(DrawingContext context, Brush brush, string text, double x, double y)
    {
        var label = Text(text, brush);

        context.DrawText(label, new Point(x, y));
    }

    /// <summary>
    ///     Display scale of the screen the drawing is on. Text has to be laid out against it or it comes out
    ///     blurry on a high-DPI monitor. Set once per render pass by the control.
    /// </summary>
    public static double PixelsPerDip { get; set; } = 1.0;

    private static FormattedText Text(double value, Brush brush) =>
        Text(value.ToString("0", CultureInfo.InvariantCulture), brush);

    private static FormattedText Text(string value, Brush brush) =>
        new(value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Face, CaptionSize, brush, PixelsPerDip);
}
