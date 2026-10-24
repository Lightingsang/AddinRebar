using System.Globalization;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace HPWebHost.Rendering;

/// <summary>
///     Writes what a WPF <see cref="DrawingContext" /> recorded as SVG, so a window's existing painters can draw for a web
///     page unchanged: groups keep their transform, clip and opacity; geometries become paths with the pen's width, caps,
///     joins and dashes; glyph runs become text stretched to WPF's advance width (the browser's font metrics differ by a
///     pixel or two). Brushes other than solid colours are written with no fill — the painters this serves use none.
/// </summary>
public static class DrawingSvgWriter
{
    /// <param name="drawing">What was recorded, typically <see cref="DrawingVisual.Drawing" />; null writes an empty picture.</param>
    /// <param name="width">Width of the picture in device-independent pixels (the SVG's view box).</param>
    /// <param name="height">Height of the picture in device-independent pixels.</param>
    public static string Write(Drawing? drawing, double width, double height)
    {
        var writer = new Writer();
        writer.Svg.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 ")
            .Append(Number(width)).Append(' ').Append(Number(height))
            .Append("\" width=\"").Append(Number(width)).Append("\" height=\"").Append(Number(height)).Append("\">");
        if (drawing is not null)
        {
            writer.Drawing(drawing);
        }

        if (writer.Defs.Length > 0)
        {
            writer.Svg.Append("<defs>").Append(writer.Defs).Append("</defs>");
        }

        return writer.Svg.Append("</svg>").ToString();
    }

    /// <summary>A path's <c>d</c> attribute for <paramref name="geometry" /> with its own transform applied.</summary>
    public static string PathData(Geometry geometry)
    {
        var path = PathGeometry.CreateFromGeometry(geometry);
        var matrix = path.Transform?.Value ?? Matrix.Identity;
        if (!IsSimilarity(matrix) && path.Figures.Any(f => f.Segments.OfType<ArcSegment>().Any()))
        {
            // A rotation, skew or uneven scale changes the arc's ellipse itself: write the flattened outline instead.
            var untransformed = path.Clone();
            untransformed.Transform = null;
            path = untransformed.GetFlattenedPathGeometry();
        }

        var data = new StringBuilder();
        foreach (var figure in path.Figures)
        {
            Command(data, 'M', matrix, figure.StartPoint);
            foreach (var segment in figure.Segments)
            {
                Segment(data, matrix, segment);
            }

            if (figure.IsClosed)
            {
                data.Append('Z');
            }
        }

        return data.ToString().TrimEnd();
    }

    private static void Segment(StringBuilder data, Matrix matrix, PathSegment segment)
    {
        switch (segment)
        {
            case LineSegment line:
                Command(data, 'L', matrix, line.Point);
                break;
            case PolyLineSegment polyLine:
                Command(data, 'L', matrix, [.. polyLine.Points]);
                break;
            case BezierSegment bezier:
                Command(data, 'C', matrix, bezier.Point1, bezier.Point2, bezier.Point3);
                break;
            case PolyBezierSegment polyBezier:
                Command(data, 'C', matrix, [.. polyBezier.Points]);
                break;
            case QuadraticBezierSegment quadratic:
                Command(data, 'Q', matrix, quadratic.Point1, quadratic.Point2);
                break;
            case PolyQuadraticBezierSegment polyQuadratic:
                Command(data, 'Q', matrix, [.. polyQuadratic.Points]);
                break;
            case ArcSegment arc:
                Arc(data, matrix, arc);
                break;
        }
    }

    /// <summary>A translation with a uniform positive scale: an arc keeps its shape, only its size and place change.</summary>
    private static bool IsSimilarity(Matrix matrix) =>
        Math.Abs(matrix.M12) < 1e-9 && Math.Abs(matrix.M21) < 1e-9 && Math.Abs(matrix.M11 - matrix.M22) < 1e-9 && matrix.M11 > 0;

    /// <summary>Only reached under a similarity transform (<see cref="PathData" /> flattens arcs under any other).</summary>
    private static void Arc(StringBuilder data, Matrix matrix, ArcSegment arc)
    {
        double scale = Math.Sqrt(Math.Abs(matrix.Determinant));
        var end = matrix.Transform(arc.Point);
        data.Append('A').Append(Number(arc.Size.Width * scale)).Append(' ').Append(Number(arc.Size.Height * scale)).Append(' ')
            .Append(Number(arc.RotationAngle)).Append(' ').Append(arc.IsLargeArc ? '1' : '0').Append(' ')
            .Append(arc.SweepDirection == SweepDirection.Clockwise ? '1' : '0').Append(' ')
            .Append(Number(end.X)).Append(' ').Append(Number(end.Y)).Append(' ');
    }

    private static void Command(StringBuilder data, char command, Matrix matrix, params Point[] points)
    {
        data.Append(command);
        foreach (var point in points)
        {
            var p = matrix.Transform(point);
            data.Append(Number(p.X)).Append(' ').Append(Number(p.Y)).Append(' ');
        }
    }

    private static string Number(double value) =>
        Math.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>Scale and rotation terms keep six decimals: 0.005 of error on a scale is pixels at the far end of a run.</summary>
    private static string Coefficient(double value) =>
        Math.Round(value, 6).ToString("0.######", CultureInfo.InvariantCulture);

    private static string Color(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private static string Attribute(string text) => SecurityElement.Escape(text) ?? string.Empty;

    private sealed class Writer
    {
        private int _clipCount;

        public StringBuilder Svg { get; } = new();

        public StringBuilder Defs { get; } = new();

        public void Drawing(Drawing drawing)
        {
            switch (drawing)
            {
                case DrawingGroup group:
                    Group(group);
                    break;
                case GeometryDrawing geometry:
                    Geometry(geometry);
                    break;
                case GlyphRunDrawing text:
                    Text(text);
                    break;
            }
        }

        // WPF applies a group's clip and opacity to its children first and its transform last, outermost.
        private void Group(DrawingGroup group)
        {
            var transform = group.Transform?.Value ?? Matrix.Identity;
            bool transformed = !transform.IsIdentity;
            bool clipped = group.ClipGeometry is not null;
            bool faded = group.Opacity < 1.0;

            if (transformed)
            {
                Svg.Append("<g transform=\"matrix(").Append(Coefficient(transform.M11)).Append(' ').Append(Coefficient(transform.M12)).Append(' ')
                    .Append(Coefficient(transform.M21)).Append(' ').Append(Coefficient(transform.M22)).Append(' ')
                    .Append(Number(transform.OffsetX)).Append(' ').Append(Number(transform.OffsetY)).Append(")\">");
            }

            if (clipped || faded)
            {
                Svg.Append("<g");
                if (clipped)
                {
                    string id = $"c{++_clipCount}";
                    Defs.Append("<clipPath id=\"").Append(id).Append("\"><path d=\"").Append(PathData(group.ClipGeometry!)).Append("\"/></clipPath>");
                    Svg.Append(" clip-path=\"url(#").Append(id).Append(")\"");
                }

                if (faded)
                {
                    Svg.Append(" opacity=\"").Append(Number(group.Opacity)).Append('"');
                }

                Svg.Append('>');
            }

            foreach (var child in group.Children)
            {
                Drawing(child);
            }

            if (clipped || faded)
            {
                Svg.Append("</g>");
            }

            if (transformed)
            {
                Svg.Append("</g>");
            }
        }

        private void Geometry(GeometryDrawing drawing)
        {
            if (drawing.Geometry is not { } geometry || geometry.IsEmpty())
            {
                return;
            }

            var pen = drawing.Pen is { Brush: SolidColorBrush, Thickness: > 0 } p ? p : null;
            var fill = drawing.Brush as SolidColorBrush;
            if (pen is null && fill is null)
            {
                return;
            }

            Svg.Append("<path d=\"").Append(PathData(geometry)).Append('"');
            Paint("fill", fill);
            if (fill is not null && PathGeometry.CreateFromGeometry(geometry).FillRule == FillRule.EvenOdd)
            {
                Svg.Append(" fill-rule=\"evenodd\"");
            }

            if (pen is not null)
            {
                Stroke(pen);
            }

            Svg.Append("/>");
        }

        private void Stroke(Pen pen)
        {
            Paint("stroke", (SolidColorBrush)pen.Brush);
            Svg.Append(" stroke-width=\"").Append(Number(pen.Thickness)).Append('"');
            string cap = pen.StartLineCap switch { PenLineCap.Round => "round", PenLineCap.Square => "square", _ => "butt" };
            if (cap != "butt")
            {
                Svg.Append(" stroke-linecap=\"").Append(cap).Append('"');
            }

            string join = pen.LineJoin switch { PenLineJoin.Round => "round", PenLineJoin.Bevel => "bevel", _ => "miter" };
            if (join != "miter")
            {
                Svg.Append(" stroke-linejoin=\"").Append(join).Append('"');
            }
            else if (Math.Abs(pen.MiterLimit - 4.0) > 1e-9)
            {
                Svg.Append(" stroke-miterlimit=\"").Append(Number(Math.Max(1.0, pen.MiterLimit))).Append('"');
            }

            // WPF dashes are multiples of the pen's thickness; SVG's are lengths.
            if (pen.DashStyle is { Dashes.Count: > 0 } dash)
            {
                Svg.Append(" stroke-dasharray=\"").Append(string.Join(" ", dash.Dashes.Select(d => Number(Math.Max(d * pen.Thickness, 0.01))))).Append('"');
                if (Math.Abs(dash.Offset) > 1e-9)
                {
                    Svg.Append(" stroke-dashoffset=\"").Append(Number(dash.Offset * pen.Thickness)).Append('"');
                }
            }
        }

        private void Text(GlyphRunDrawing drawing)
        {
            var run = drawing.GlyphRun;
            if (run?.Characters is not { Count: > 0 } characters || drawing.ForegroundBrush is not SolidColorBrush brush)
            {
                return;
            }

            var typeface = run.GlyphTypeface;
            string family = typeface.Win32FamilyNames.Values.FirstOrDefault() ?? typeface.FamilyNames.Values.FirstOrDefault() ?? "Segoe UI";
            string baseFamily = typeface.FamilyNames.Values.FirstOrDefault() ?? family;
            double advance = run.AdvanceWidths.Sum();
            var origin = run.BaselineOrigin;

            Svg.Append("<text x=\"").Append(Number(origin.X)).Append("\" y=\"").Append(Number(origin.Y))
                .Append("\" font-size=\"").Append(Number(run.FontRenderingEmSize))
                .Append("\" font-family=\"'").Append(Attribute(family)).Append("', '").Append(Attribute(baseFamily)).Append("', sans-serif\"")
                .Append(" font-weight=\"").Append(typeface.Weight.ToOpenTypeWeight()).Append('"');
            if (typeface.Style != FontStyles.Normal)
            {
                Svg.Append(" font-style=\"italic\"");
            }

            Paint("fill", brush);
            if (advance > 0)
            {
                Svg.Append(" textLength=\"").Append(Number(advance)).Append("\" lengthAdjust=\"spacingAndGlyphs\"");
            }

            Svg.Append(" xml:space=\"preserve\">").Append(Attribute(new string([.. characters]))).Append("</text>");
        }

        private void Paint(string attribute, SolidColorBrush? brush)
        {
            if (brush is null)
            {
                Svg.Append(' ').Append(attribute).Append("=\"none\"");
                return;
            }

            Svg.Append(' ').Append(attribute).Append("=\"").Append(Color(brush.Color)).Append('"');
            double opacity = brush.Color.A / 255.0 * brush.Opacity;
            if (opacity < 1.0)
            {
                Svg.Append(' ').Append(attribute).Append("-opacity=\"").Append(Number(opacity)).Append('"');
            }
        }
    }
}
