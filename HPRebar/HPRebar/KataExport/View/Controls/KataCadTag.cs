using System.Collections.Generic;
using System.Globalization;
using HPRebar.Core.KataRebar.Calculators;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// Kata's tag block kata_block_KHT (scale 25) at an insertion point on screen, in model millimetres scaled like CAD,
/// laid out as its visibility states set the attributes (T2-DY7.dwg): the number circles touch the insertion point on
/// the far side of the leader, the text sits on the leader before it — the bars on the line (DK), a stirrup's spacing
/// under it (KC) — and the elevation's stirrup tag is one line centred on its row (DKKC). Texts are kata_text
/// (<see cref="KataCadText"/>).
/// </summary>
internal sealed class KataCadTag
{
    private readonly KataCanvasPalette _palette;
    private readonly KataCadText _text;
    private readonly KataDrawPrimitives _draw;
    private readonly double _scale;

    public KataCadTag(KataCanvasPalette palette, KataDrawPrimitives draw, double scale)
    {
        _palette = palette;
        _draw = draw;
        _scale = scale;
        _text = new KataCadText(draw, scale);
    }

    /// <summary>
    /// A tag at the end of a leader (states P11, P21, T11, T21; P12 with a <paramref name="spacing"/>):
    /// <paramref name="right"/> when the leader runs right to it, i.e. the text before the insertion point and the
    /// circles after it.
    /// </summary>
    public void OnLeader(string text, IReadOnlyList<int> numbers, double insertX, double rowY, bool right, string spacing = "")
    {
        double x = right ? insertX - KataTagStyle.TextGapRight * _scale : insertX + KataTagStyle.TextGapLeft * _scale;
        var align = right ? KataCadText.Align.Right : KataCadText.Align.Left;
        _text.Draw(text, _palette.KataTagText, KataTagStyle.TextHeight, x, rowY - KataTagStyle.TextLift * _scale, align);
        _text.Draw(spacing, _palette.KataTagText, KataTagStyle.TextHeight, x, rowY + KataTagStyle.SpacingDrop * _scale, align);
        Circles(numbers, insertX, rowY, right);
    }

    /// <summary>Kata's elevation stirrup tag (T13): one line right-aligned before the insertion point, centred on the row; the circle after it.</summary>
    public void Standalone(string value, IReadOnlyList<int> numbers, double insertX, double rowY)
    {
        _text.DrawCentredOn(value, _palette.KataTagText, KataTagStyle.TextHeight, insertX - KataTagStyle.TextGapRight * _scale,
            rowY, KataCadText.Align.Right);
        Circles(numbers, insertX, rowY, right: true);
    }

    /// <summary>Touching circles beyond the insertion point, the numbers in reading order left to right (Kata's SH3 SH4 / SH1 SH2).</summary>
    private void Circles(IReadOnlyList<int> numbers, double insertX, double rowY, bool right)
    {
        double r = KataTagStyle.CircleRadius * _scale;
        int count = numbers.Count;
        for (int k = 0; k < count; k++)
        {
            double cx = right ? insertX + r * (2 * k + 1) : insertX - r * (2 * (count - k) - 1);
            _draw.Circle(null, _palette.KataCircle, cx, rowY, r);
            string number = numbers[k].ToString(CultureInfo.InvariantCulture);
            _text.DrawCentredOn(number, _palette.KataNumber, KataTagStyle.TextHeight, cx, rowY, KataCadText.Align.Centre);
        }
    }
}
