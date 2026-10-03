using System;
using System.Globalization;
using System.Linq;
using HPRebar.Core.KataExport.Models;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// Kata's tags (<see cref="KataBarTagBuilder"/>) drawn the way its kata_block_KHT sits on its LEADER, in model
/// millimetres scaled with the view like CAD: an arrow on each bar, the leader up (or down) to its row and along it,
/// the text on that horizontal part, the numbers in touching circles beyond its end. A tag running left has its text
/// left-aligned after the circles; a stirrup tag has no leader, its text centred on the row before the circle.
/// </summary>
internal sealed class KataElevationBarTagPainter
{
    /// <summary>Segoe UI capitals are this fraction of the font size; Kata's text height is a capital height.</summary>
    private const double CapHeight = 0.70;

    /// <summary>Below this font size (px) the text is not drawn; leaders and circles still are.</summary>
    private const double MinFontPx = 1.5;

    private readonly KataElevationScene _scene;
    private readonly KataCanvasPalette _palette;
    private readonly KataDrawPrimitives _draw;
    private readonly KataRebarDrawing _drawing;
    private readonly KataStationMap _map;

    public KataElevationBarTagPainter(KataElevationScene scene, KataCanvasPalette palette, KataDrawPrimitives draw, KataRebarDrawing drawing, KataStationMap map)
    {
        _scene = scene ?? throw new ArgumentNullException(nameof(scene));
        _palette = palette ?? throw new ArgumentNullException(nameof(palette));
        _draw = draw ?? throw new ArgumentNullException(nameof(draw));
        _drawing = drawing ?? throw new ArgumentNullException(nameof(drawing));
        _map = map ?? throw new ArgumentNullException(nameof(map));
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

            if (tag.Kind == KataTagKind.Stirrups) PaintStirrupTag(tag, insertX, rowY);
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
        bool right = insertX >= x;
        double lift = rowY - KataTagStyle.TextLift * Scale;
        if (right) Text(tag.Text, _palette.KataTagText, insertX - KataTagStyle.TextGapRight * Scale, lift, alignRight: true);
        else Text(tag.Text, _palette.KataTagText, insertX + KataTagStyle.TextGapLeft * Scale, lift, alignRight: false);
        Circles(tag, insertX, rowY, right);
    }

    private void PaintStirrupTag(KataBarTag tag, double insertX, double rowY)
    {
        // Kata's T13: text right-aligned before the insertion point, centred on the row; one circle after it.
        var text = Formatted(tag.Text, _palette.KataTagText);
        if (text is not null)
        {
            double width = Width(tag.Text, text);
            _draw.AtWidth(text, insertX - KataTagStyle.TextGapRight * Scale - width, rowY - CapCentre(text), width);
        }
        Circles(tag, insertX, rowY, right: true);
    }

    /// <summary>Touching circles beyond the insertion point, the numbers in reading order left to right (Kata's SH3 SH4 / SH1 SH2).</summary>
    private void Circles(KataBarTag tag, double insertX, double rowY, bool right)
    {
        double r = KataTagStyle.CircleRadius * Scale;
        int n = tag.Numbers.Count;
        for (int k = 0; k < n; k++)
        {
            double cx = right ? insertX + r * (2 * k + 1) : insertX - r * (2 * (n - k) - 1);
            _draw.Circle(null, _palette.KataCircle, cx, rowY, r);
            string value = tag.Numbers[k].ToString(CultureInfo.InvariantCulture);
            var number = Formatted(value, _palette.KataNumber);
            if (number is null) continue;
            double width = Width(value, number);
            _draw.AtWidth(number, cx - width / 2.0, rowY - CapCentre(number), width);
        }
    }

    /// <summary>Text with its baseline at <paramref name="baselineY"/>, ending (or starting) at <paramref name="x"/>.</summary>
    private void Text(string value, System.Windows.Media.Brush brush, double x, double baselineY, bool alignRight)
    {
        var text = Formatted(value, brush);
        if (text is null) return;
        double width = Width(value, text);
        _draw.AtWidth(text, alignRight ? x - width : x, baselineY - text.Baseline, width);
    }

    /// <summary>Width of the text as Kata's font sets it (Segoe UI runs wider).</summary>
    private double Width(string value, System.Windows.Media.FormattedText text) =>
        Math.Min(text.Width, KataTagStyle.CharWidth * value.Length * Scale);

    private System.Windows.Media.FormattedText? Formatted(string value, System.Windows.Media.Brush brush)
    {
        double size = KataTagStyle.TextHeight * Scale / CapHeight;
        return size < MinFontPx ? null : _draw.Text(value, brush, size);
    }

    /// <summary>Distance from the text's top to the middle of its capitals (Kata's text height).</summary>
    private double CapCentre(System.Windows.Media.FormattedText text) => text.Baseline - KataTagStyle.TextHeight * Scale / 2.0;

    private double X(double localX) => _scene.X(_map.ToStation(localX));

    private double Y(double z) => _scene.Y(_scene.Elevation.TopMm + z);
}
