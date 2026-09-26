using HPRebar.Core.KataExport.Calculators;
using HPRebar.Core.KataExport.Models;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// Where everything goes on the canvas for one paint: the horizontal viewport, the exaggerated vertical scale
/// and the fixed rows above and below the beam band (grid bubbles, stubs, letters, dimension chains, captions).
/// </summary>
internal sealed class KataElevationScene
{
    public const double BubbleY = 16.0;
    public const double BubbleRadius = 11.0;
    public const double UpperStubPx = 24.0;
    public const double LowerStubPx = 32.0;
    public const double FootingPx = 26.0;
    public const double MinBeamPx = 60.0;

    private const double BeamTopY = 84.0;
    private const double BelowBandPx = 150.0;
    private const double MinBandPx = 40.0;
    private const double MaxBandPx = 150.0;

    public KataElevationScene(KataElevation elevation, KataElevationViewport viewport, double widthPx, double heightPx, int selectedColumn)
    {
        Elevation = elevation;
        Viewport = viewport;
        Width = widthPx;
        Height = heightPx;
        SelectedColumn = selectedColumn;

        double bandDepth = elevation.TopMm - elevation.BottomMm;
        double maxBand = System.Math.Max(MinBandPx, System.Math.Min(MaxBandPx, heightPx - BeamTopY - BelowBandPx));
        VerticalScale = KataElevationViewport.VerticalScale(viewport.Scale, elevation.MaxBeamHeightMm, bandDepth, MinBeamPx, maxBand);

        BandBottomY = Y(elevation.BottomMm);
        MarkerTextY = BandBottomY + LowerStubPx + 4.0;
        LetterY = MarkerTextY + 16.0;
        ChainY = LetterY + 34.0;
        GridChainY = ChainY + 26.0;
        CaptionY = GridChainY + 8.0;
    }

    public KataElevation Elevation { get; }
    public KataElevationViewport Viewport { get; }
    public double Width { get; }
    public double Height { get; }
    public int SelectedColumn { get; }
    public double VerticalScale { get; }

    public double BandTopY => BeamTopY;
    public double BandBottomY { get; }

    /// <summary>Row of crossing-beam texts, just under the column stubs.</summary>
    public double MarkerTextY { get; }

    public double LetterY { get; }
    public double ChainY { get; }
    public double GridChainY { get; }
    public double CaptionY { get; }

    public double X(double station) => Viewport.ToScreen(station);

    /// <summary>Screen y of a height relative to the reference level.</summary>
    public double Y(double heightMm) => BeamTopY + (Elevation.TopMm - heightMm) * VerticalScale;

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
