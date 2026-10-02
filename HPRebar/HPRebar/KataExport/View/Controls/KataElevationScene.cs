using HPRebar.Core.KataExport.Calculators;
using HPRebar.Core.KataExport.Models;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// Where everything goes on the canvas for one paint. The beam band is placed by the viewport (zoom and pan in both
/// directions); the rows of labels above it (grid bubbles) and below it (letters, dimension chains, captions) keep
/// fixed pixel distances from the band, so they move with the drawing like annotations in CAD and never scale.
/// </summary>
internal sealed class KataElevationScene
{
    public const double BubbleRadius = 12.0;
    public const double UpperStubPx = 26.0;
    public const double LowerStubPx = 28.0;
    public const double FootingPx = 26.0;

    /// <summary>Margin above the beam band: upper column stubs, top dim chain, column letters, grid line top, bar tags.</summary>
    public const double AbovePx = 142.0;

    /// <summary>Margin below the band bottom: lower column stubs, 2 dim chains, grid bubbles, one row of bar tags.</summary>
    public const double BelowPx = 150.0;

    /// <summary>The deepest beam is never drawn shallower than this, however long the run.</summary>
    private const double MinBeamPx = 60.0;

    public KataElevationScene(KataElevation elevation, KataElevationViewport viewport, double widthPx, double heightPx, int selectedColumn)
    {
        Elevation = elevation;
        Viewport = viewport;
        Width = widthPx;
        Height = heightPx;
        SelectedColumn = selectedColumn;

        BandBottomY = Y(elevation.BottomMm);

        // Above the beam band:
        TopChainY = BandTopY - UpperStubPx - 20.0;
        LetterY = TopChainY - 35.0;
        CaptionY = BandTopY - 10.0;
        GridLineTopY = TopChainY;
        SelectionTopY = LetterY - 2.0;

        // Below the beam band:
        MarkerTextY = BandBottomY + LowerStubPx + 4.0;
        ChainY = BandBottomY + LowerStubPx + 22.0;
        GridChainY = ChainY + 22.0;
        BubbleY = GridChainY + 28.0;
        GridLineBottomY = BubbleY + BubbleRadius + 8.0;
    }

    public KataElevation Elevation { get; }
    public KataElevationViewport Viewport { get; }
    public double Width { get; }
    public double Height { get; }
    public int SelectedColumn { get; }

    public double BandTopY => Viewport.OffsetYPx;
    public double BandBottomY { get; }
    public double BubbleY { get; }

    /// <summary>Row of crossing-beam texts, just under the column stubs.</summary>
    public double MarkerTextY { get; }

    public double LetterY { get; }
    public double TopChainY { get; }
    public double SelectionTopY { get; }
    public double ChainY { get; }
    public double GridChainY { get; }
    public double CaptionY { get; }
    public double GridLineTopY { get; }
    public double GridLineBottomY { get; }

    /// <summary>Vertical scale that makes the deepest beam <see cref="MinBeamPx"/> tall when the run is zoomed out.</summary>
    public static double MinVerticalScale(KataElevation elevation) => MinBeamPx / System.Math.Max(1.0, elevation.MaxBeamHeightMm);

    public double X(double station) => Viewport.ToScreen(station);

    /// <summary>Screen y of a height relative to the reference level.</summary>
    public double Y(double heightMm) => Viewport.ToScreenY(Elevation.TopMm - heightMm);

    /// <summary>Top and bottom of the beams over <paramref name="extent"/> (the whole band when none reaches it).</summary>
    public (double Top, double Bottom) BeamFaces(Interval1D extent)
    {
        double top = double.NegativeInfinity, bottom = double.PositiveInfinity;
        foreach (var beam in Elevation.Beams)
        {
            if (!beam.Extent.Overlaps(extent)) continue;
            top = System.Math.Max(top, beam.TopMm);
            bottom = System.Math.Min(bottom, beam.BottomMm);
        }

        return double.IsInfinity(top) ? (Elevation.TopMm, Elevation.BottomMm) : (top, bottom);
    }

    /// <summary>A column's screen span, at least <paramref name="minPx"/> wide so zero-width columns stay visible.</summary>
    public (double Left, double Right) ScreenSpan(Interval1D extent, double minPx)
    {
        double left = X(extent.Start), right = X(extent.End);
        if (right - left >= minPx) return (left, right);
        double mid = (left + right) / 2.0;
        return (mid - minPx / 2.0, mid + minPx / 2.0);
    }

    public bool IsVisible(double left, double right) => right >= 0 && left <= Width;
}
