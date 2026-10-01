using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using HPRebar.Core.KataExport.Models;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// Renders 2D reinforcing bars and stirrups onto the beam elevation canvas.
/// Translates 3D beam-local coordinates (X along axis, Z from beam top) into screen pixels.
/// </summary>
internal sealed class KataElevationRebarPainter
{
    private readonly KataElevationScene _scene;
    private readonly KataCanvasPalette _palette;
    private readonly KataDrawPrimitives _draw;
    private readonly KataRebarLayoutResult _layout;
    private readonly KataStationMap _map;

    /// <param name="map">Places the layout's local X on the drawing's stations.</param>
    public KataElevationRebarPainter(
        KataElevationScene scene,
        KataCanvasPalette palette,
        KataDrawPrimitives draw,
        KataRebarLayoutResult layout,
        KataStationMap map)
    {
        _map = map ?? throw new ArgumentNullException(nameof(map));
        _scene = scene ?? throw new ArgumentNullException(nameof(scene));
        _palette = palette ?? throw new ArgumentNullException(nameof(palette));
        _draw = draw ?? throw new ArgumentNullException(nameof(draw));
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
    }

    public void Paint()
    {
        PaintStirrupZones();
        PaintBarSets();
        PaintSideBars();
        PaintMainBars();
        PaintExtraBottomBars();
        PaintExtraTopBars();
    }

    private void PaintStirrupZones()
    {
        foreach (var zone in _layout.StirrupZones)
        {
            if (zone.Stations.Count == 0) continue;

            double startX = X(zone.StartStationX);
            double endX = X(zone.EndStationX);
            if (!_scene.IsVisible(Math.Min(startX, endX) - 10, Math.Max(startX, endX) + 10)) continue;

            var (s0, s1) = _map.ToStations(zone.StartStationX, zone.EndStationX);
            var (topMm, bottomMm) = _scene.BeamFaces(new Interval1D(s0, s1));
            double topY = _scene.Y(topMm - 30.0);
            double bottomY = _scene.Y(bottomMm + 30.0);

            // Shaded zone box
            _draw.Box(_palette.RebarStirrupZoneFill, null, Math.Min(startX, endX), topY, Math.Max(startX, endX), bottomY);

            // Vertical stirrup ticks
            foreach (double st in zone.Stations)
            {
                double sx = X(st);
                _draw.Line(_palette.RebarStirrup, sx, topY, sx, bottomY);
            }

            // Zone annotation (e.g. 12Ø8a100)
            if (zone.Count > 0 && Math.Abs(endX - startX) > 36.0)
            {
                double midX = (startX + endX) / 2.0;
                double stirrupDiameter = _layout.IndividualStirrups.FirstOrDefault(b => b.HostSpanIndex == zone.SpanIndex)?.Diameter ?? 0.0;
                string text = $"{zone.Count}Ø{stirrupDiameter:0}a{zone.Spacing:0}";
                var formatted = _draw.Text(text, _palette.MutedText, KataDrawPrimitives.SmallTextSize, bold: false);
                _draw.Centered(formatted, midX, (topY + bottomY) / 2.0 - formatted.Height / 2.0);
            }
        }
    }

    /// <summary>
    /// Inner stirrups and C ties along the beam: an upright one as a dashed stroke over its height, a tie across
    /// the beam as a small ring at its level, at every station.
    /// </summary>
    private void PaintBarSets()
    {
        foreach (var set in _layout.BarSets)
        {
            var p = set.Shape.Points;
            if (p.Count < 2 || set.Count == 0) continue;
            double zTop = p.Max(q => q.Z), zBottom = p.Min(q => q.Z);
            double yTop = _scene.Y(_scene.Elevation.TopMm + zTop), yBottom = _scene.Y(_scene.Elevation.TopMm + zBottom);

            foreach (double station in set.Stations)
            {
                double sx = X(station);
                if (!_scene.IsVisible(sx - 3, sx + 3)) continue;
                if (yBottom - yTop > 3.0)
                    _draw.Line(_palette.RebarSide, sx, yTop, sx, yBottom);
                else
                    _draw.Circle(null, _palette.RebarSide, sx, (yTop + yBottom) / 2.0, 1.8);
            }
        }
    }

    /// <summary>Screen X of a layout-local station.</summary>
    private double X(double localX) => _scene.X(_map.ToStation(localX));

    private void PaintMainBars()
    {
        // 1. Top Continuous
        PaintDistinctCurves(_layout.MainTopBars, _palette.RebarMainTop);

        // 2. Bottom Continuous
        PaintDistinctCurves(_layout.MainBottomBars, _palette.RebarMainBottom);
    }

    private void PaintExtraTopBars()
    {
        var layer1 = _layout.ExtraTopBars.Where(b => b.Layer == 1).ToList();
        var layer2 = _layout.ExtraTopBars.Where(b => b.Layer > 1).ToList();

        PaintDistinctCurves(layer1, _palette.RebarExtraTop1);
        PaintDistinctCurves(layer2, _palette.RebarExtraTop2);
    }

    private void PaintExtraBottomBars()
    {
        var layer1 = _layout.ExtraBottomBars.Where(b => b.Layer == 1).ToList();
        var layer2 = _layout.ExtraBottomBars.Where(b => b.Layer > 1).ToList();

        PaintDistinctCurves(layer1, _palette.RebarExtraBottom1);
        PaintDistinctCurves(layer2, _palette.RebarExtraBottom2);
    }

    private void PaintSideBars()
    {
        PaintDistinctCurves(_layout.SideBars, _palette.RebarSide);
    }

    private void PaintDistinctCurves(IReadOnlyList<KataRebarCurve> bars, Pen pen)
    {
        if (bars.Count == 0) return;

        // Group by 2D geometry (X and Z) so identical side-by-side bars in the same layer only draw once
        var drawnProfiles = new HashSet<string>();

        foreach (var bar in bars)
        {
            var pts = bar.Polyline.Points;
            if (pts.Count < 2) continue;

            string key = string.Join(";", pts.Select(p => $"{p.X:0.0},{p.Z:0.0}"));
            if (!drawnProfiles.Add(key)) continue;

            for (int i = 0; i < pts.Count - 1; i++)
            {
                var p1 = pts[i];
                var p2 = pts[i + 1];

                double x1 = X(p1.X);
                double y1 = _scene.Y(_scene.Elevation.TopMm + p1.Z);
                double x2 = X(p2.X);
                double y2 = _scene.Y(_scene.Elevation.TopMm + p2.Z);

                if (_scene.IsVisible(Math.Min(x1, x2) - 5, Math.Max(x1, x2) + 5))
                {
                    _draw.Line(pen, x1, y1, x2, y2);
                }
            }
        }
    }
}
