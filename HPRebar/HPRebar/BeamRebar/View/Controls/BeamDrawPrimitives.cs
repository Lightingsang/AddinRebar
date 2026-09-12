using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

// Alias WPF types against Revit SDK implicit usings
using Point = System.Windows.Point;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using Brush = System.Windows.Media.Brush;
using FormattedText = System.Windows.Media.FormattedText;

namespace HPRebar.BeamRebar.View.Controls;

/// <summary>
/// High-performance 2D drawing primitives for beam elevation and cross-section preview canvases.
/// </summary>
internal static class BeamDrawPrimitives
{
    private const double CaptionSize = 10.5;
    private const double TickLength = 3.5;

    private static readonly Typeface Face = new("Segoe UI");

    public static double PixelsPerDip { get; set; } = 1.0;

    public static void Line(DrawingContext context, Pen pen, double x1, double y1, double x2, double y2) =>
        context.DrawLine(pen, new Point(x1, y1), new Point(x2, y2));

    public static void Box(DrawingContext context, Pen pen, double left, double top, double width, double height) =>
        context.DrawRectangle(null, pen, new Rect(left, top, width, height));

    public static void FilledBox(DrawingContext context, Brush brush, double left, double top, double width, double height) =>
        context.DrawRectangle(brush, null, new Rect(left, top, width, height));

    public static void Circle(DrawingContext context, Brush? fill, Pen? pen, double centreX, double centreY, double radius) =>
        context.DrawEllipse(fill, pen, new Point(centreX, centreY), radius, radius);

    public static void Polyline(DrawingContext context, Pen pen, IReadOnlyList<Point> points)
    {
        for (int i = 1; i < points.Count; i++)
        {
            context.DrawLine(pen, points[i - 1], points[i]);
        }
    }

    public static void DimensionHorizontal(
        DrawingContext context,
        Pen pen,
        Brush textBrush,
        double left,
        double y,
        double width,
        double value,
        string? prefix = null)
    {
        if (width <= 0) return;

        Line(context, pen, left, y, left + width, y);
        Line(context, pen, left, y - TickLength, left, y + TickLength);
        Line(context, pen, left + width, y - TickLength, left + width, y + TickLength);

        string text = prefix is not null ? $"{prefix} {value:0}" : $"{value:0}";
        var label = Text(text, textBrush);
        context.DrawText(label, new Point(left + (width - label.Width) / 2, y - label.Height - 2));
    }

    public static void DimensionVertical(
        DrawingContext context,
        Pen pen,
        Brush textBrush,
        double x,
        double top,
        double height,
        double value,
        string? prefix = null)
    {
        if (height <= 0) return;

        Line(context, pen, x, top, x, top + height);
        Line(context, pen, x - TickLength, top, x + TickLength, top);
        Line(context, pen, x - TickLength, top + height, x + TickLength, top + height);

        string text = prefix is not null ? $"{prefix} {value:0}" : $"{value:0}";
        var label = Text(text, textBrush);
        var origin = new Point(x - label.Height - 3, top + (height + label.Width) / 2);

        context.PushTransform(new RotateTransform(-90, origin.X, origin.Y));
        context.DrawText(label, origin);
        context.Pop();
    }

    public static void Caption(DrawingContext context, Brush brush, string text, double x, double y, bool center = false)
    {
        var label = Text(text, brush);
        double drawX = center ? x - (label.Width / 2) : x;
        context.DrawText(label, new Point(drawX, y));
    }

    private static FormattedText Text(string value, Brush brush) =>
        new(value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Face, CaptionSize, brush, PixelsPerDip);
}
