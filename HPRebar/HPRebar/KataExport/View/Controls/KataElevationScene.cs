using HPRebar.Core.KataExport.Calculators;
using HPRebar.Core.KataExport.Models;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// Where everything goes on the canvas for one paint. The beam band is placed by the viewport (one scale both ways,
/// as in AutoCAD); the bar tags next to it are drawn in millimetres and scale with it; the rows of labels above
/// (letters, dimension chain) and below (dimension chains, grid bubbles) keep fixed pixel sizes, outside the tags.
/// </summary>
internal sealed class KataElevationScene
{
    public const double BubbleRadius = 12.0;
    public const double UpperStubPx = 26.0;
    public const double LowerStubPx = 28.0;
    public const double FootingPx = 26.0;

    /// <summary>Margin above the beam band (bar tags excluded): upper column stubs, top dim chain, column letters, grid line top.</summary>
    public const double AbovePx = 142.0;

    /// <summary>Margin below the band bottom (bar tags excluded): lower column stubs, 2 dim chains, grid bubbles.</summary>
    public const double BelowPx = 150.0;

    /// <param name="tagsAbovePx">Room the bar tags take over the beam at this zoom (0 when they are hidden).</param>
    /// <param name="tagsBelowPx">Room they take under it.</param>
    public KataElevationScene(KataElevation elevation, KataElevationViewport viewport, double widthPx, double heightPx, int selectedColumn,
        double tagsAbovePx = 0.0, double tagsBelowPx = 0.0)
    {
        Elevation = elevation;
        Viewport = viewport;
        Width = widthPx;
        TagsBelowPx = tagsBelowPx;
        Height = heightPx;
        SelectedColumn = selectedColumn;

        BandBottomY = Y(elevation.BottomMm);

        // Above the beam band:
        TopChainY = BandTopY - System.Math.Max(UpperStubPx, tagsAbovePx) - 20.0;
        LetterY = TopChainY - 35.0;
        CaptionY = BandTopY - 10.0;
        GridLineTopY = TopChainY;
        SelectionTopY = LetterY - 2.0;

        // Below the beam band:
        double below = System.Math.Max(LowerStubPx, tagsBelowPx);
        MarkerTextY = BandBottomY + LowerStubPx + 4.0;
        ChainY = BandBottomY + below + 22.0;
        GridChainY = ChainY + 22.0;
        BubbleY = GridChainY + 28.0;
        GridLineBottomY = BubbleY + BubbleRadius + 8.0;
    }

    public KataElevation Elevation { get; }
    public KataElevationViewport Viewport { get; }
    public double Width { get; }

    /// <summary>Room the bar tags take under the beam band at this zoom.</summary>
    public double TagsBelowPx { get; }
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
