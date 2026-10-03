using System;
using HPRebar.Core.KataExport.Models;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// Kata's tags (<see cref="KataBarTagBuilder"/>) drawn the way its kata_block_KHT sits on its LEADER, in model
/// millimetres scaled with the view like CAD: an arrow on each bar, the leader up (or down) to its row and along it,
/// the tag itself <see cref="KataCadTag"/>'s.
/// </summary>
internal sealed class KataElevationBarTagPainter
{
    private readonly KataElevationScene _scene;
    private readonly KataCanvasPalette _palette;
    private readonly KataDrawPrimitives _draw;
    private readonly KataRebarDrawing _drawing;
    private readonly KataStationMap _map;
    private readonly KataCadTag _tag;

    public KataElevationBarTagPainter(KataElevationScene scene, KataCanvasPalette palette, KataDrawPrimitives draw, KataRebarDrawing drawing, KataStationMap map)
    {
        _scene = scene ?? throw new ArgumentNullException(nameof(scene));
        _palette = palette ?? throw new ArgumentNullException(nameof(palette));
        _draw = draw ?? throw new ArgumentNullException(nameof(draw));
        _drawing = drawing ?? throw new ArgumentNullException(nameof(drawing));
        _map = map ?? throw new ArgumentNullException(nameof(map));
        _tag = new KataCadTag(palette, draw, scene.Viewport.Scale);
    }

    private double Scale => _scene.Viewport.Scale;

    public void Paint()
    {
        foreach (var tag in _drawing.Tags)
        {
            double x = X(tag.X), insertX = X(tag.InsertX), rowY = Y(tag.RowZ);
            // Kata's stirrup tag sits 125 mm right of its zone's middle as drawn, whichever way the run is listed.
            if (tag.Kind == KataTagKind.Stirrups)
                insertX = X(tag.X - KataTagStyle.StirrupShift) + KataTagStyle.StirrupShift * Scale;
            double reach = (KataTagStyle.LeaderPerChar * tag.Text.Length + 4.0 * KataTagStyle.CircleRadius) * Scale;
            if (!_scene.IsVisible(Math.Min(x, insertX) - reach, Math.Max(x, insertX) + reach)) continue;

            if (tag.Kind == KataTagKind.Stirrups) _tag.Standalone(tag.Text, tag.Numbers, insertX, rowY);
            else PaintLeaderTag(tag, x, insertX, rowY);
        }
    }

    private void PaintLeaderTag(KataBarTag tag, double x, double insertX, double rowY)
    {
        foreach (double z in tag.FootZ)
        {
            double footY = Y(z);
            _draw.Line(_palette.KataLeader, x, footY, x, rowY);
            _draw.Arrow(_palette.KataLeaderBrush, x, footY, x, rowY, KataTagStyle.ArrowSize * Scale);
        }

        _draw.Line(_palette.KataLeader, x, rowY, insertX, rowY);

        // On screen the tag may run the other way than in the layout (the drawing lists the run backwards).
        _tag.OnLeader(tag.Text, tag.Numbers, insertX, rowY, right: insertX >= x);
    }

    private double X(double localX) => _scene.X(_map.ToStation(localX));

    private double Y(double z) => _scene.Y(_scene.Elevation.TopMm + z);
}
