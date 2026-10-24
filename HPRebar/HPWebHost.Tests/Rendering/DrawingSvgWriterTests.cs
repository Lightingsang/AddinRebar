using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using HPWebHost.Rendering;
using Xunit;

namespace HPWebHost.Tests.Rendering;

/// <summary>Records drawings the way the painters do (a DrawingContext) and reads the SVG back as XML.</summary>
public sealed class DrawingSvgWriterTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    [Fact]
    public void Write_Line_IsAPathWithThePensColourAndWidth()
    {
        var svg = Render(dc => dc.DrawLine(new Pen(Brushes.Red, 2.0), new Point(0, 0), new Point(10, 5)));

        var path = Assert.Single(svg.Descendants(Svg + "path"));
        Assert.Equal("M0 0 L10 5", (string?)path.Attribute("d"));
        Assert.Equal("#FF0000", (string?)path.Attribute("stroke"));
        Assert.Equal("2", (string?)path.Attribute("stroke-width"));
        Assert.Equal("none", (string?)path.Attribute("fill"));
    }

    [Fact]
    public void Write_TransformAndClip_TransformOutsideClipInside()
    {
        var svg = Render(dc =>
        {
            dc.PushTransform(new TranslateTransform(5, 7));
            dc.PushClip(new RectangleGeometry(new Rect(0, 0, 4, 4)));
            dc.DrawRectangle(Brushes.Blue, null, new Rect(0, 0, 8, 8));
            dc.Pop();
            dc.Pop();
        });

        var path = Assert.Single(svg.Descendants(Svg + "path"), p => p.Parent?.Name != Svg + "clipPath");
        var clipped = path.Ancestors(Svg + "g").First(g => g.Attribute("clip-path") is not null);
        var transformed = clipped.Ancestors(Svg + "g").First(g => g.Attribute("transform") is not null);
        Assert.Equal("matrix(1 0 0 1 5 7)", (string?)transformed.Attribute("transform"));
        Assert.Single(svg.Descendants(Svg + "clipPath"));
        Assert.Equal("#0000FF", (string?)path.Attribute("fill"));
    }

    [Fact]
    public void Write_DashedPen_ScalesDashesByThickness()
    {
        var pen = new Pen(Brushes.Black, 2.0) { DashStyle = new DashStyle([3.0, 1.0], 0.5) };

        var path = Assert.Single(Render(dc => dc.DrawLine(pen, new Point(0, 0), new Point(20, 0))).Descendants(Svg + "path"));

        Assert.Equal("6 2", (string?)path.Attribute("stroke-dasharray"));
        Assert.Equal("1", (string?)path.Attribute("stroke-dashoffset"));
    }

    [Fact]
    public void Write_TranslucentFill_WritesFillOpacity()
    {
        var brush = new SolidColorBrush(Color.FromArgb(128, 0x10, 0x20, 0x30));

        var path = Assert.Single(Render(dc => dc.DrawRectangle(brush, null, new Rect(0, 0, 2, 2))).Descendants(Svg + "path"));

        Assert.Equal("#102030", (string?)path.Attribute("fill"));
        Assert.Equal("0.5", (string?)path.Attribute("fill-opacity"));
    }

    [Fact]
    public void Write_Ellipse_IsCurvesNotNothing()
    {
        var path = Assert.Single(Render(dc => dc.DrawEllipse(null, new Pen(Brushes.Green, 1), new Point(5, 5), 3, 3)).Descendants(Svg + "path"));

        Assert.Contains("C", (string?)path.Attribute("d"));
    }

    [Fact]
    public void Write_FormattedText_IsTextAtTheBaselineWithWpfsAdvance()
    {
        var text = new FormattedText("Nhịp 2", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), 11.0, Brushes.White, 1.0);

        var element = Assert.Single(Render(dc => dc.DrawText(text, new Point(10, 20))).Descendants(Svg + "text"));

        Assert.Equal("Nhịp 2", element.Value);
        Assert.Equal("11", (string?)element.Attribute("font-size"));
        Assert.Equal("#FFFFFF", (string?)element.Attribute("fill"));
        Assert.True(double.Parse((string)element.Attribute("y")!, CultureInfo.InvariantCulture) > 20, "y is the baseline, below the text's top");
        Assert.Equal(text.WidthIncludingTrailingWhitespace, double.Parse((string)element.Attribute("textLength")!, CultureInfo.InvariantCulture), 0.05);
    }

    [Fact]
    public void PathData_ArcUnderRotation_IsFlattenedFromItsRealStart()
    {
        var figure = new PathFigure { StartPoint = new Point(10, 0), Segments = { new ArcSegment(new Point(0, 10), new Size(10, 10), 0, false, SweepDirection.Clockwise, true) } };
        var geometry = new PathGeometry([figure]) { Transform = new RotateTransform(90) };

        string data = DrawingSvgWriter.PathData(geometry);

        Assert.DoesNotContain("A", data);
        Assert.StartsWith("M0 10 ", data); // (10, 0) rotated by 90° about the origin
    }

    [Fact]
    public void PathData_ArcUnderTranslation_StaysAnArc()
    {
        var figure = new PathFigure { StartPoint = new Point(10, 0), Segments = { new ArcSegment(new Point(0, 10), new Size(10, 10), 0, false, SweepDirection.Clockwise, true) } };
        var geometry = new PathGeometry([figure]) { Transform = new TranslateTransform(5, 5) };

        Assert.Equal("M15 5 A10 10 0 0 1 5 15", DrawingSvgWriter.PathData(geometry));
    }

    [Fact]
    public void Write_ScaleKeepsSixDecimals()
    {
        var svg = Render(dc =>
        {
            dc.PushTransform(new ScaleTransform(0.857142, 1));
            dc.DrawRectangle(Brushes.Blue, null, new Rect(0, 0, 1, 1));
            dc.Pop();
        });

        Assert.Equal("matrix(0.857142 0 0 1 0 0)", (string?)svg.Descendants(Svg + "g").Single().Attribute("transform"));
    }

    [Fact]
    public void Write_TextWithMarkup_IsEscaped()
    {
        var text = new FormattedText("<b>&\"x\"", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 10.0, Brushes.Black, 1.0);

        var element = Assert.Single(Render(dc => dc.DrawText(text, new Point(0, 0))).Descendants(Svg + "text"));

        Assert.Equal("<b>&\"x\"", element.Value);
        Assert.Empty(element.Elements());
    }

    [Fact]
    public void Write_GroupOpacity_IsWritten()
    {
        var svg = Render(dc =>
        {
            dc.PushOpacity(0.4);
            dc.DrawRectangle(Brushes.Red, null, new Rect(0, 0, 1, 1));
            dc.Pop();
        });

        Assert.Equal("0.4", (string?)svg.Descendants(Svg + "g").Single().Attribute("opacity"));
    }

    [Fact]
    public void Write_NoDrawing_IsAnEmptyPictureOfTheGivenSize()
    {
        var svg = XDocument.Parse(DrawingSvgWriter.Write(null, 300, 120)).Root!;

        Assert.Equal("0 0 300 120", (string?)svg.Attribute("viewBox"));
        Assert.Empty(svg.Elements());
    }

    private static XElement Render(Action<DrawingContext> paint)
    {
        var group = new DrawingGroup();
        using (var dc = group.Open())
        {
            paint(dc);
        }

        return XDocument.Parse(DrawingSvgWriter.Write(group, 100, 100)).Root!;
    }
}
