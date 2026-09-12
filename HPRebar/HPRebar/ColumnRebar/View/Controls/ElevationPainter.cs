using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using HPRebar.ColumnRebar.ViewModel;
using HPRebar.Core.ColumnRebar;
using HPRebar.Core.ColumnRebar.Models;
using Point = System.Windows.Point;

namespace HPRebar.ColumnRebar.View.Controls;

/// <summary>
///     Draws the whole column stack in elevation, twice: looking north (so width reads across) on the left,
///     and looking east (so depth reads across) on the right. Both share one vertical scale, which is what
///     lets the eye compare them.
/// </summary>
internal sealed class ElevationPainter
{
    private const double MarginLeft = 100;
    private const double ViewGap = 60;

    private readonly CanvasPalette _palette;
    private readonly ElevationLayout _layout;
    private readonly double _widestPlan;

    public ElevationPainter(CanvasPalette palette, IReadOnlyList<ColumnSection> sections)
    {
        _palette = palette;
        _layout = CanvasScaleCalculator.Elevation(sections);
        _widestPlan = sections[0].Shape == SectionShape.Rectangle
            ? sections.Max(section => section.B)
            : sections.Max(section => section.D);
    }

    public double Width => _layout.Width;

    public double Height => _layout.Height;

    public void Paint(DrawingContext context, ColumnRebarSession session, int selectedColumn, int selectedBar)
    {
        var rightOrigin = MarginLeft + _widestPlan / _layout.Scale + ViewGap;

        for (var i = 0; i < session.Columns.Count; i++)
        {
            var column = session.Columns[i];
            var above = i + 1 < session.Columns.Count ? session.Columns[i + 1] : null;
            var selected = i == selectedColumn;

            PaintSegment(context, column, above, MarginLeft, acrossWidth: true, selected, selectedBar);
            PaintSegment(context, column, above, rightOrigin, acrossWidth: false, selected, selectedBar);
        }

        PaintGround(context, session.Columns[0].Section, MarginLeft, rightOrigin);
    }

    private void PaintSegment(
        DrawingContext context,
        ColumnSpecEditor column,
        ColumnSpecEditor? above,
        double origin,
        bool acrossWidth,
        bool selected,
        int selectedBar)
    {
        var section = column.Section;
        var (near, far) = PlanExtent(section, acrossWidth);

        var left = origin + near / _layout.Scale;
        var right = origin + far / _layout.Scale;
        var top = ToCanvasY(section.TopPosition);
        var bottom = ToCanvasY(section.BottomPosition);

        if (selected)
        {
            DrawPrimitives.FilledBox(context, _palette.Highlight, left, top, right - left, bottom - top);
        }

        DrawPrimitives.Box(context, _palette.Outline, left, top, right - left, bottom - top);

        PaintBeamZone(context, section, left, right);
        PaintStirrups(context, column, left, right);
        PaintMainBars(context, column, above, origin, acrossWidth, selectedBar);

        // Overall height, read down the outside of the left-hand view only.
        if (acrossWidth)
        {
            DrawPrimitives.DimensionVertical(
                context, _palette.Dimension, _palette.Text,
                origin - 40, top, bottom - top, section.Hc);
        }

        DrawPrimitives.DimensionHorizontal(
            context, _palette.Dimension, _palette.Text,
            left, bottom + 18, right - left, far - near);
    }

    /// <summary>The beam framing into the segment head, shown as the band the top bars have to bend under.</summary>
    private void PaintBeamZone(DrawingContext context, ColumnSection section, double left, double right)
    {
        if (section.Hb <= 0 && section.Zb <= 0) return;

        var soffit = ToCanvasY(section.TopPosition - section.Zb - section.Hb);
        var head = ToCanvasY(section.TopPosition - section.Zb);
        var overhang = 24;

        DrawPrimitives.Line(context, _palette.Outline, left - overhang, soffit, right + overhang, soffit);
        DrawPrimitives.Line(context, _palette.Outline, left - overhang, head, right + overhang, head);
        DrawPrimitives.Line(context, _palette.Outline, left - overhang, soffit, left - overhang, head);
        DrawPrimitives.Line(context, _palette.Outline, right + overhang, soffit, right + overhang, head);
    }

    /// <summary>Each tie is one horizontal line, spaced exactly as the creation service will place them.</summary>
    private void PaintStirrups(DrawingContext context, ColumnSpecEditor column, double left, double right)
    {
        var section = column.Section;
        var spec = new StirrupSpec
        {
            TypeDis = column.DistributionType,
            S = column.Spacing,
            S1 = column.SpacingDense,
            S2 = column.SpacingSparse,
            IsTiesUp = column.TiesUpToBeams
        };

        IReadOnlyList<StirrupRun> runs;

        try
        {
            var length = StirrupDistributionCalculator.ComputeRunLength(section, spec.IsTiesUp);
            runs = StirrupDistributionCalculator.Compute(length, spec);
        }
        catch (System.ArgumentOutOfRangeException)
        {
            // Spacing not filled in yet. The footer already tells the user; drawing nothing is enough here.
            return;
        }

        var inset = column.Cover / _layout.Scale;

        foreach (var run in runs)
        {
            for (var i = 0; i < run.Count; i++)
            {
                var height = section.BottomPosition + run.StartOffset + i * run.Spacing;

                DrawPrimitives.Line(context, _palette.Stirrup, left + inset, ToCanvasY(height), right - inset, ToCanvasY(height));
            }
        }
    }

    /// <summary>Every main bar, projected onto whichever elevation is being drawn.</summary>
    private void PaintMainBars(
        DrawingContext context,
        ColumnSpecEditor column,
        ColumnSpecEditor? above,
        double origin,
        bool acrossWidth,
        int selectedBar)
    {
        foreach (var polyline in ElevationBars.For(column, above))
        {
            var points = polyline.Points
                .Select(point => new Point(
                    origin + (acrossWidth ? point.X : point.Y) / _layout.Scale,
                    ToCanvasY(point.Z)))
                .ToList();

            var pen = polyline.BarNumber == selectedBar ? _palette.SelectedMainBar : _palette.MainBar;

            DrawPrimitives.Polyline(context, pen, points);
        }
    }

    /// <summary>The face the stack stands on.</summary>
    private void PaintGround(DrawingContext context, ColumnSection first, double leftOrigin, double rightOrigin)
    {
        var y = ToCanvasY(first.BottomPosition);

        DrawPrimitives.Line(context, _palette.Outline, leftOrigin - 50, y, rightOrigin + _widestPlan / _layout.Scale + 50, y);
    }

    private static (double Near, double Far) PlanExtent(ColumnSection section, bool acrossWidth)
    {
        if (section.Shape == SectionShape.Rectangle)
        {
            return acrossWidth
                ? (section.WestPosition, section.EastPosition)
                : (section.SouthPosition, section.NorthPosition);
        }

        var centre = acrossWidth ? section.CenterX : section.CenterY;

        return (centre - section.D / 2, centre + section.D / 2);
    }

    private double ToCanvasY(double height) => _layout.Baseline - height / _layout.Scale;
}
