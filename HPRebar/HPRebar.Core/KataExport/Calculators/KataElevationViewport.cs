using System;
using HPRebar.Core.KataExport.Models;

namespace HPRebar.Core.KataExport.Calculators;

/// <summary>
/// Placement of the elevation on screen, one scale in both directions as in AutoCAD:
/// screen x = <see cref="OffsetPx"/> + station × <see cref="Scale"/>, and
/// screen y = <see cref="OffsetYPx"/> + (depth below the highest beam top) × <see cref="Scale"/>.
/// Immutable; every gesture returns a new viewport.
/// </summary>
public readonly record struct KataElevationViewport(double Scale, double OffsetPx, double OffsetYPx = 0.0)
{
    public double ToScreen(double station) => OffsetPx + station * Scale;

    public double ToStation(double screenX) => (screenX - OffsetPx) / Scale;

    /// <summary>Screen y of a point <paramref name="depthMm"/> below the highest beam top.</summary>
    public double ToScreenY(double depthMm) => OffsetYPx + depthMm * Scale;

    /// <summary>The whole <paramref name="range"/> centred in <paramref name="widthPx"/> with <paramref name="marginPx"/> each side.</summary>
    public static KataElevationViewport Fit(Interval1D range, double widthPx, double marginPx)
    {
        double usable = Math.Max(1.0, widthPx - 2.0 * marginPx);
        double scale = usable / Math.Max(1.0, range.Length);
        double offset = (widthPx - range.Length * scale) / 2.0 - range.Start * scale;
        return new KataElevationViewport(scale, offset, 0.0);
    }

    /// <summary>
    /// Places the beam band (<paramref name="bandDepthMm"/> deep, with <paramref name="abovePx"/> of labels over it and
    /// <paramref name="belowPx"/> under it) in the middle of <paramref name="heightPx"/>; when it does not fit, the top
    /// labels stay on screen.
    /// </summary>
    public KataElevationViewport CentreBand(double bandDepthMm, double heightPx, double abovePx, double belowPx)
    {
        double total = abovePx + bandDepthMm * Scale + belowPx;
        double top = total <= heightPx ? (heightPx - total) / 2.0 : 0.0;
        return this with { OffsetYPx = top + abovePx };
    }

    /// <summary>
    /// Zooms out, keeping <paramref name="anchorX"/> still, until the beam band and its labels fit in
    /// <paramref name="heightPx"/> — the vertical half of a "zoom extents". Nothing changes when they already fit.
    /// </summary>
    public KataElevationViewport ShrinkToHeight(double bandDepthMm, double heightPx, double abovePx, double belowPx, double anchorX)
    {
        double room = heightPx - abovePx - belowPx;
        if (room <= 0.0 || bandDepthMm <= 0.0 || bandDepthMm * Scale <= room) return this;

        double wanted = room / bandDepthMm;
        return wanted < Scale ? ZoomAt(wanted / Scale, anchorX, 0.0, 0.0, Scale) : this;
    }

    /// <summary>
    /// Zoom by <paramref name="factor"/> keeping the point under (<paramref name="anchorX"/>, <paramref name="anchorY"/>)
    /// still. The limits only stop movement in their own direction: a view already past one (the window was resized)
    /// is never pulled back by the next step.
    /// </summary>
    public KataElevationViewport ZoomAt(double factor, double anchorX, double anchorY, double minScale, double maxScale)
    {
        double target = Scale * factor;
        double scale = factor >= 1.0 ? Math.Min(Math.Max(maxScale, Scale), target) : Math.Max(Math.Min(minScale, Scale), target);
        if (!(scale > 0.0) || double.IsInfinity(scale) || !(Scale > 0.0)) return this;

        double station = ToStation(anchorX);
        double offsetY = anchorY - (anchorY - OffsetYPx) * scale / Scale;
        return this with { Scale = scale, OffsetPx = anchorX - station * scale, OffsetYPx = offsetY };
    }

    public KataElevationViewport PanBy(double deltaX, double deltaY = 0.0) =>
        this with { OffsetPx = OffsetPx + deltaX, OffsetYPx = OffsetYPx + deltaY };

    /// <summary>
    /// Pans the least amount horizontally that brings <paramref name="range"/> inside the margins; zoom and vertical
    /// position are kept. A range wider than the view stays put while any of it is on screen (clicking a long span
    /// must not make the view jump), and is centred otherwise.
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
}
