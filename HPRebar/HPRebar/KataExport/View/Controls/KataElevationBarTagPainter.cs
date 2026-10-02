using System;
using System.Linq;
using System.Windows.Media;
using HPRebar.Core.KataExport.Models;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// Kata's bar tags on the elevation (<see cref="KataBarTagBuilder"/>): a thin leader from the bar to a row of tags at
/// the top edge of the drawing (top bars) or at its bottom edge (bottom and side bars), each tag the bar numbers in
/// circles joined by "+" and the bar text. Above the beam the inner rows (14-16) have a row of their own; below it
/// every tag shares one row (the drawing has no room for a second under the grid bubbles) and slides sideways to the
/// nearest free place, its leader turning along the row. A tag with no room left is not drawn.
/// </summary>
internal sealed class KataElevationBarTagPainter
{
    private const double Radius = 7.0;
    private const double Gap = 3.0;

    private readonly KataElevationScene _scene;
    private readonly KataCanvasPalette _palette;
    private readonly KataDrawPrimitives _draw;
    private readonly KataRebarPlan _plan;
    private readonly KataStationMap _map;

    public KataElevationBarTagPainter(KataElevationScene scene, KataCanvasPalette palette, KataDrawPrimitives draw, KataRebarPlan plan, KataStationMap map)
    {
        _scene = scene ?? throw new ArgumentNullException(nameof(scene));
        _palette = palette ?? throw new ArgumentNullException(nameof(palette));
        _draw = draw ?? throw new ArgumentNullException(nameof(draw));
        _plan = plan ?? throw new ArgumentNullException(nameof(plan));
        _map = map ?? throw new ArgumentNullException(nameof(map));
    }

    /// <summary>Spacing of the outer and inner rows of tags above the beam.</summary>
    public const double RowPitch = 18.0;

    /// <summary>Farthest a tag slides along its row to find room (px).</summary>
    private const double MaxSlide = 160.0;

    /// <summary>
    /// Row of a tag: above the column letters, the inner rows one pitch higher; below the beam, one row under the
    /// grid lines.
    /// </summary>
    public static double RowY(KataElevationScene scene, bool above, bool inner) => above
        ? scene.LetterY - 22.0 - (inner ? RowPitch : 0.0)
        : scene.GridLineBottomY + 13.0;

    public void Paint()
    {
        var tags = KataBarTagBuilder.Build(_plan.Layout, KataBeamStations.From(_plan.Spec));
        var leader = new Pen(_palette.MutedText, 0.6) { DashStyle = new DashStyle(new[] { 3.0, 2.0 }, 0) };
        leader.Freeze();
        var ring = new Pen(_palette.Accent, 1.0);
        ring.Freeze();

        var lanes = new[] { new KataLabelLane(), new KataLabelLane(), new KataLabelLane() };
        foreach (var tag in tags)
        {
            double foot = _scene.X(_map.ToStation(tag.X));
            double rowY = RowY(_scene, tag.Above, tag.Inner);
            var lane = lanes[tag.Above ? (tag.Inner ? 1 : 0) : 2];
            var text = _draw.Text(tag.Text, _palette.RebarText, KataDrawPrimitives.SmallTextSize);
            var plus = _draw.Text("+", _palette.MutedText, KataDrawPrimitives.SmallTextSize);
            double width = tag.Numbers.Count * 2.0 * Radius + (tag.Numbers.Count - 1) * (plus.Width + 2.0) + Gap + text.Width;
            if (!_scene.IsVisible(foot - Radius, foot - Radius + width)) continue;
            if (lane.PlaceNear(foot - Radius, width, MaxSlide) is not { } left) continue;

            // The leader runs straight from the bar to its tag; one that had to slide turns just short of the row, on
            // the beam's side, so it never crosses the tags already placed there.
            double x = left + Radius;
            double footY = _scene.Y(_scene.Elevation.TopMm + tag.Z);
            double edgeY = tag.Above ? rowY + Radius : rowY - Radius;
            if (Math.Abs(x - foot) < 0.5)
            {
                _draw.Line(leader, foot, footY, foot, edgeY);
            }
            else
            {
                double turnY = tag.Above ? edgeY + 4.0 : edgeY - 4.0;
                _draw.Line(leader, foot, footY, foot, turnY);
                _draw.Line(leader, foot, turnY, x, turnY);
                _draw.Line(leader, x, turnY, x, edgeY);
            }

            _draw.Circle(null, ring, foot, footY, 1.6);

            double cx = x;
            for (int i = 0; i < tag.Numbers.Count; i++)
            {
                if (i > 0)
                {
                    _draw.At(plus, cx + Radius + 1.0, rowY - plus.Height / 2.0);
                    cx += 2.0 * Radius + plus.Width + 2.0;
                }

                _draw.Circle(_palette.Fill, ring, cx, rowY, Radius);
                var number = _draw.Text(tag.Numbers[i].ToString(System.Globalization.CultureInfo.InvariantCulture), _palette.Accent, KataDrawPrimitives.SmallTextSize - 1.0, bold: true);
                _draw.Centered(number, cx, rowY - number.Height / 2.0);
            }

            _draw.At(text, cx + Radius + Gap, rowY - text.Height / 2.0);
        }
    }
}
