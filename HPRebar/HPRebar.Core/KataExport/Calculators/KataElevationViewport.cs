using System;
using HPRebar.Core.KataExport.Models;

namespace HPRebar.Core.KataExport.Calculators;

/// <summary>
/// Horizontal placement of the elevation on screen: screen x = <see cref="OffsetPx"/> + station × <see cref="Scale"/>.
/// Immutable; every gesture returns a new viewport. Vertical placement is separate (<see cref="VerticalScale"/>)
/// because the beam depth is exaggerated to stay readable when a 30 m run fits in 800 px.
/// </summary>
public readonly record struct KataElevationViewport(double Scale, double OffsetPx)
{
    public double ToScreen(double station) => OffsetPx + station * Scale;

    public double ToStation(double screenX) => (screenX - OffsetPx) / Scale;

    /// <summary>The whole <paramref name="range"/> centred in <paramref name="widthPx"/> with <paramref name="marginPx"/> each side.</summary>
    public static KataElevationViewport Fit(Interval1D range, double widthPx, double marginPx)
    {
        double usable = Math.Max(1.0, widthPx - 2.0 * marginPx);
        double scale = usable / Math.Max(1.0, range.Length);
        double offset = (widthPx - range.Length * scale) / 2.0 - range.Start * scale;
        return new KataElevationViewport(scale, offset);
    }

    /// <summary>Zoom by <paramref name="factor"/> keeping the station under <paramref name="anchorPx"/> still.</summary>
    public KataElevationViewport ZoomAt(double factor, double anchorPx, double minScale, double maxScale)
    {
        double scale = Math.Max(minScale, Math.Min(maxScale, Scale * factor));
        double station = ToStation(anchorPx);
        return new KataElevationViewport(scale, anchorPx - station * scale);
    }

    public KataElevationViewport PanBy(double deltaPx) => this with { OffsetPx = OffsetPx + deltaPx };

    /// <summary>
    /// Pans the least amount that brings <paramref name="range"/> inside the margins; zoom is kept. A range wider
    /// than the view stays put while any of it is on screen (clicking a long span must not make the view jump),
    /// and is centred otherwise.
    /// </summary>
    public KataElevationViewport EnsureVisible(Interval1D range, double widthPx, double marginPx)
    {
        double left = ToScreen(range.Start), right = ToScreen(range.End);
        if (right - left > widthPx - 2.0 * marginPx)
            return right < marginPx || left > widthPx - marginPx ? PanBy(widthPx / 2.0 - (left + right) / 2.0) : this;
        if (left < marginPx) return PanBy(marginPx - left);
        if (right > widthPx - marginPx) return PanBy(widthPx - marginPx - right);
        return this;
    }

    /// <summary>
    /// Pixels per millimetre vertically: the horizontal scale, raised so the deepest beam is at least
    /// <paramref name="minBeamPx"/> tall, then capped so the whole beam band (steps included) stays within
    /// <paramref name="maxBandPx"/>. Heights keep their proportions to each other.
    /// </summary>
    public static double VerticalScale(double horizontalScale, double maxBeamHeightMm, double bandDepthMm, double minBeamPx, double maxBandPx)
    {
        double scale = Math.Max(horizontalScale, minBeamPx / Math.Max(1.0, maxBeamHeightMm));
        return Math.Min(scale, maxBandPx / Math.Max(1.0, bandDepthMm));
    }
}
