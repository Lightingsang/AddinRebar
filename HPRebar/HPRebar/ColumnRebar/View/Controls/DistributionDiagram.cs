using System.Windows;
using System.Windows.Media;
using HPRebar.Core.ColumnRebar;

namespace HPRebar.ColumnRebar.View.Controls;

/// <summary>
///     A small sketch of the tie spacing pattern: a column outline with tie marks laid out the way the
///     chosen distribution type will place them. Sizes are illustrative, not to scale.
/// </summary>
public sealed class DistributionDiagram : FrameworkElement
{
    private const double DiagramWidth = 90;
    private const double DiagramHeight = 180;
    private const double Inset = 12;

    /// <summary>How much of the sketch height the beam band at the head takes up.</summary>
    private const double BeamBandFraction = 0.12;

    /// <summary>A stand-in run length; the diagram shows proportions, so the absolute value does not matter.</summary>
    private const double SampleRun = 3000;

    private const double SampleDenseSpacing = 100;
    private const double SampleSparseSpacing = 220;

    public static readonly DependencyProperty DistributionTypeProperty = DependencyProperty.Register(
        nameof(DistributionType), typeof(int), typeof(DistributionDiagram),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TiesUpToBeamsProperty = DependencyProperty.Register(
        nameof(TiesUpToBeams), typeof(bool), typeof(DistributionDiagram),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Spacing pattern, 0 for even and 1 to 3 for the dense/sparse/dense layouts.</summary>
    public int DistributionType
    {
        get => (int)GetValue(DistributionTypeProperty);
        set => SetValue(DistributionTypeProperty, value);
    }

    /// <summary>Whether the ties carry on up through the beam depth.</summary>
    public bool TiesUpToBeams
    {
        get => (bool)GetValue(TiesUpToBeamsProperty);
        set => SetValue(TiesUpToBeamsProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) => new(DiagramWidth, DiagramHeight);

    protected override void OnRender(DrawingContext drawingContext)
    {
        DrawPrimitives.PixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        var palette = CanvasPalette.From(this);

        drawingContext.DrawRectangle(palette.Fill, null, new Rect(0, 0, DiagramWidth, DiagramHeight));

        var top = Inset;
        var bottom = DiagramHeight - Inset;
        var left = Inset;
        var right = DiagramWidth - Inset;

        DrawPrimitives.Box(drawingContext, palette.Outline, left, top, right - left, bottom - top);

        // The beam band at the head, drawn only when the ties stop below it.
        if (!TiesUpToBeams)
        {
            var soffit = top + (bottom - top) * BeamBandFraction;

            DrawPrimitives.Line(drawingContext, palette.Outline, left - 6, soffit, right + 6, soffit);
        }

        var spec = new HPRebar.Core.ColumnRebar.Models.StirrupSpec
        {
            Layout = (HPRebar.Core.ColumnRebar.Models.TieLayout)DistributionType,
            S = SampleSparseSpacing,
            S1 = SampleDenseSpacing,
            S2 = SampleSparseSpacing,
            IsTiesUp = TiesUpToBeams
        };

        var runs = StirrupDistributionCalculator.Compute(SampleRun, spec);

        // Ties always start at the base; they stop at the soffit unless they carry on through the beam.
        var usable = TiesUpToBeams ? bottom - top : bottom - top - (bottom - top) * BeamBandFraction;

        foreach (var run in runs)
        {
            for (var i = 0; i < run.Count; i++)
            {
                var height = (run.StartOffset + i * run.Spacing) / SampleRun;

                if (height is < 0 or > 1) continue;

                var y = bottom - height * usable;

                DrawPrimitives.Line(drawingContext, palette.Stirrup, left + 4, y, right - 4, y);
            }
        }
    }
}
