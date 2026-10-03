using System.Windows;
using HPRebar.Core.KataExport.Calculators;
using HPRebar.Core.KataExport.Models;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.KataExport.View.Controls;

/// <summary>Framing of the elevation canvas: "zoom extents", a column brought into view, the room around the band.</summary>
public sealed partial class KataElevationCanvas
{
    /// <summary>Kata's level mark runs this far right of its tip (lines 289, text after them).</summary>
    private const double LevelMarkReach = 400.0;

    /// <summary>Whether the run is drawn as Kata's elevation (the bars are shown and planned).</summary>
    private bool KataMode => ShowRebar && Drawing() is not null && !ReferenceEquals(RebarPlan, _failedPaint);

    /// <summary>
    /// Millimetres drawn over and under the beam band: Kata's elevation reaches from its flags to its title, or further
    /// where a cut carries more rows of bar tags; otherwise nothing in millimetres (the labels around the band keep
    /// fixed pixel rows).
    /// </summary>
    private (double Above, double Below) Band()
    {
        if (!KataMode || Elevation is not { } elevation || Drawing() is not { } drawing) return (0.0, 0.0);
        double depth = elevation.TopMm - elevation.BottomMm;
        var tags = ShowBarTags ? KataBarTagBuilder.Band(drawing.Tags, depth) : (Above: 0.0, Below: 0.0);
        return (Math.Max(Math.Max(0.0, drawing.Elevation.Top), tags.Above),
            Math.Max(Math.Max(0.0, -drawing.Elevation.Bottom - depth), tags.Below));
    }

    /// <summary>Pixels kept free over and under what is drawn in millimetres.</summary>
    private (double Above, double Below) MarginRows() =>
        KataMode ? (KataMarginPx, KataMarginPx) : (KataElevationScene.AbovePx, KataElevationScene.BelowPx);

    /// <summary>
    /// The whole run, widened to Kata's level mark and depth dimensions left of it when those are drawn (and by the
    /// level mark's own length either way: its lines and text run to the right on screen even when the run is mirrored).
    /// </summary>
    private Interval1D FrameRange(KataElevation elevation, Interval1D range)
    {
        if (!KataMode || Drawing() is not { } drawing || range != elevation.Bounds) return range;
        var map = RebarStationMap ?? KataStationMap.Identity;
        double a = map.ToStation(drawing.Elevation.MinX), b = map.ToStation(drawing.Elevation.MaxX);
        return new Interval1D(Math.Min(range.Start, Math.Min(a, b)) - LevelMarkReach, Math.Max(range.End, Math.Max(a, b)) + LevelMarkReach);
    }

    /// <summary>Canvas width left of the section panel (Kata's drawing) or card (the plain elevation).</summary>
    private double UsableWidth()
    {
        double reserved = KataMode
            ? SectionPanel()?.Width ?? 0.0
            : ShowSection && RebarPlan is not null ? KataElevationSectionPainter.ReservedWidth(ActualWidth, ActualHeight) : 0.0;
        return Math.Max(1.0, ActualWidth - reserved);
    }

    /// <summary>
    /// "Zoom extents" on <paramref name="range"/>: across the width (capped at <see cref="FocusMaxScale"/>), zoomed out
    /// further if the beam band, its tags and its labels would not fit the height, then centred. One scale both ways.
    /// </summary>
    private KataElevationViewport Frame(KataElevation elevation, Interval1D range, double marginPx)
    {
        // The section card sits over the right of the canvas: the framed run stays left of it.
        double width = UsableWidth(), height = Math.Max(1.0, ActualHeight);
        var (above, below) = Band();
        var (abovePx, belowPx) = MarginRows();
        double depth = elevation.TopMm - elevation.BottomMm + above + below;
        var viewport = KataElevationViewport.Fit(range, width, marginPx);
        if (viewport.Scale > FocusMaxScale) viewport = viewport.ZoomAt(FocusMaxScale / viewport.Scale, width / 2.0, 0.0, 0.0, FocusMaxScale);
        viewport = viewport.ShrinkToHeight(depth, height, abovePx, belowPx, width / 2.0);
        viewport = viewport.CentreBand(depth, height, abovePx, belowPx);
        return viewport.PanBy(0.0, above * viewport.Scale);
    }

    /// <summary>Zooming out stops at a twentieth of the whole run's "zoom extents".</summary>
    private double MinScale(KataElevation elevation) =>
        Frame(elevation, FrameRange(elevation, elevation.Bounds), MarginPx).Scale * MinScaleOfExtents;

    private void FrameAll()
    {
        if (Elevation is not { } elevation) return;
        _viewport = Frame(elevation, FrameRange(elevation, elevation.Bounds), MarginPx);
        _userFramed = false;
        InvalidateVisual();
    }

    private void FrameColumn(int index)
    {
        if (Elevation is not { } elevation || index < 0 || index >= elevation.Columns.Count || ActualWidth < 1) return;
        _viewport = Frame(elevation, elevation.FocusRange(index), FocusMarginPx);
        _userFramed = true;
        InvalidateVisual();
    }
}
