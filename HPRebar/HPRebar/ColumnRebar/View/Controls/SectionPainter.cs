using System.Windows.Media;
using HPRebar.ColumnRebar.ViewModel;
using HPRebar.Core.ColumnRebar;
using HPRebar.Core.ColumnRebar.Models;

namespace HPRebar.ColumnRebar.View.Controls;

/// <summary>
///     Draws one column section looking down: the concrete outline, the tie inside the cover, and every
///     main bar as a filled dot with its number beside it.
/// </summary>
internal sealed class SectionPainter
{
    private const double Margin = 40;
    private const double MinimumBarRadius = 2.5;

    private readonly CanvasPalette _palette;
    private readonly double _scale;
    private readonly ColumnSection _section;

    public SectionPainter(CanvasPalette palette, ColumnSection section, double scale)
    {
        _palette = palette;
        _section = section;
        _scale = scale <= 0 ? 1 : scale;
    }

    public double Width => PlanWidth / _scale + 2 * Margin;

    public double Height => PlanHeight / _scale + 2 * Margin;

    private double PlanWidth => _section.Shape == SectionShape.Rectangle ? _section.B : _section.D;

    private double PlanHeight => _section.Shape == SectionShape.Rectangle ? _section.H : _section.D;

    /// <summary>
    ///     Draws the section. <paramref name="dashed"/> is used for the section that is not the one being
    ///     edited, when two are shown over each other.
    /// </summary>
    public void Paint(DrawingContext context, ColumnSpecEditor column, int selectedBar, bool dashed = false)
    {
        var outline = dashed ? CanvasPalette.Dashed(_palette.Outline) : _palette.Outline;
        var tie = dashed ? CanvasPalette.Dashed(_palette.Stirrup) : _palette.Stirrup;

        PaintOutline(context, outline);
        PaintTie(context, tie, column.Cover);

        if (!dashed) PaintBars(context, column, selectedBar);
    }

    private void PaintOutline(DrawingContext context, Pen pen)
    {
        if (_section.Shape == SectionShape.Rectangle)
        {
            DrawPrimitives.Box(context, pen, Margin, Margin, _section.B / _scale, _section.H / _scale);

            DrawPrimitives.DimensionHorizontal(
                context, _palette.Dimension, _palette.Text,
                Margin, Height - Margin / 2, _section.B / _scale, _section.B);

            DrawPrimitives.DimensionVertical(
                context, _palette.Dimension, _palette.Text,
                Margin / 2, Margin, _section.H / _scale, _section.H);

            return;
        }

        var radius = _section.D / 2 / _scale;

        DrawPrimitives.Circle(context, null, pen, Margin + radius, Margin + radius, radius);

        DrawPrimitives.DimensionHorizontal(
            context, _palette.Dimension, _palette.Text,
            Margin, Height - Margin / 2, _section.D / _scale, _section.D);
    }

    private void PaintTie(DrawingContext context, Pen pen, double coverMm)
    {
        var cover = coverMm / _scale;

        if (_section.Shape == SectionShape.Rectangle)
        {
            var width = _section.B / _scale - 2 * cover;
            var height = _section.H / _scale - 2 * cover;

            if (width <= 0 || height <= 0) return;

            DrawPrimitives.Box(context, pen, Margin + cover, Margin + cover, width, height);

            return;
        }

        var radius = _section.D / 2 / _scale - cover;

        if (radius <= 0) return;

        DrawPrimitives.Circle(context, null, pen, Margin + _section.D / 2 / _scale, Margin + _section.D / 2 / _scale, radius);
    }

    private void PaintBars(DrawingContext context, ColumnSpecEditor column, int selectedBar)
    {
        var layout = column.ToLayout();

        // Bar counts mid-edit are not a layout; nothing to draw until they make sense again.
        if (layout.BarDiameter <= 0 || !ColumnSpecRules.IsLayoutValid(_section.Shape, layout)) return;

        var bars = BarLayoutCalculator.Compute(_section, layout);

        var radius = System.Math.Max(MinimumBarRadius, layout.BarDiameter / 2 / _scale);

        foreach (var bar in bars)
        {
            var (x, y) = ToCanvas(bar);
            var brush = bar.BarNumber == selectedBar ? _palette.SelectedMainBar.Brush : _palette.MainBar.Brush;

            DrawPrimitives.Circle(context, brush, null, x, y, radius);
            DrawPrimitives.Caption(context, _palette.Text, bar.BarNumber.ToString(), x + radius + 2, y - radius - 8);
        }
    }

    /// <summary>
    ///     Section positions are measured from the stack datum, so they are shifted back to this section's
    ///     own corner before being drawn. Canvas Y grows downward, hence the flip.
    /// </summary>
    private (double X, double Y) ToCanvas(BarPosition bar)
    {
        if (_section.Shape == SectionShape.Rectangle)
        {
            return (
                Margin + (bar.X0 - _section.WestPosition) / _scale,
                Margin + (_section.H - (bar.Y0 - _section.SouthPosition)) / _scale);
        }

        var half = _section.D / 2;

        return (
            Margin + (bar.X0 - _section.CenterX + half) / _scale,
            Margin + (half - (bar.Y0 - _section.CenterY)) / _scale);
    }
}
