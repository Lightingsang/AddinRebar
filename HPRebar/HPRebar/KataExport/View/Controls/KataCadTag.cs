using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Media;
using HPRebar.Core.KataRebar.Calculators;

// WPF types, not the Revit ones the SDK imports globally.
using Brush = System.Windows.Media.Brush;
using FormattedText = System.Windows.Media.FormattedText;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// Kata's tag block kata_block_KHT (scale 25) at an insertion point on screen, in model millimetres scaled like CAD:
/// the text on the leader before the insertion point, the numbers in touching circles beyond it. A tag running left
/// has its text left-aligned after the circles; a stirrup tag (no leader) has its text centred on the row.
/// </summary>
internal sealed class KataCadTag
{
    /// <summary>Segoe UI capitals are this fraction of the font size; Kata's text height is a capital height.</summary>
    private const double CapHeight = 0.70;

    /// <summary>Below this font size (px) the text is not drawn; circles still are.</summary>
    private const double MinFontPx = 1.5;

    private readonly KataCanvasPalette _palette;
    private readonly KataDrawPrimitives _draw;
    private readonly double _scale;

    public KataCadTag(KataCanvasPalette palette, KataDrawPrimitives draw, double scale)
    {
        _palette = palette;
        _draw = draw;
        _scale = scale;
    }

    /// <summary>A tag at the end of a leader: <paramref name="right"/> when the leader runs right to it.</summary>
    public void OnLeader(string text, IReadOnlyList<int> numbers, double insertX, double rowY, bool right)
    {
        double lift = rowY - KataTagStyle.TextLift * _scale;
        if (right) Text(text, insertX - KataTagStyle.TextGapRight * _scale, lift, alignRight: true);
        else Text(text, insertX + KataTagStyle.TextGapLeft * _scale, lift, alignRight: false);
        Circles(numbers, insertX, rowY, right);
    }

    /// <summary>Kata's stirrup tag: text right-aligned before the insertion point, centred on the row; one circle after it.</summary>
    public void Standalone(string value, IReadOnlyList<int> numbers, double insertX, double rowY)
    {
        var text = Formatted(value, _palette.KataTagText);
        if (text is not null)
        {
            double width = Width(value, text);
            _draw.AtWidth(text, insertX - KataTagStyle.TextGapRight * _scale - width, rowY - CapCentre(text), width);
        }

        Circles(numbers, insertX, rowY, right: true);
    }

    /// <summary>Touching circles beyond the insertion point, the numbers in reading order left to right (Kata's SH3 SH4 / SH1 SH2).</summary>
    private void Circles(IReadOnlyList<int> numbers, double insertX, double rowY, bool right)
    {
        double r = KataTagStyle.CircleRadius * _scale;
        int n = numbers.Count;
        for (int k = 0; k < n; k++)
        {
            double cx = right ? insertX + r * (2 * k + 1) : insertX - r * (2 * (n - k) - 1);
            _draw.Circle(null, _palette.KataCircle, cx, rowY, r);
            string value = numbers[k].ToString(CultureInfo.InvariantCulture);
            var number = Formatted(value, _palette.KataNumber);
            if (number is null) continue;
            double width = Width(value, number);
            _draw.AtWidth(number, cx - width / 2.0, rowY - CapCentre(number), width);
        }
    }

    /// <summary>Text with its baseline at <paramref name="baselineY"/>, ending (or starting) at <paramref name="x"/>.</summary>
    private void Text(string value, double x, double baselineY, bool alignRight)
    {
        var text = Formatted(value, _palette.KataTagText);
        if (text is null) return;
        double width = Width(value, text);
        _draw.AtWidth(text, alignRight ? x - width : x, baselineY - text.Baseline, width);
    }

    /// <summary>Width of the text as Kata's font sets it (Segoe UI runs wider).</summary>
    private double Width(string value, FormattedText text) => Math.Min(text.Width, KataTagStyle.CharWidth * value.Length * _scale);

    private FormattedText? Formatted(string value, Brush brush)
    {
        double size = KataTagStyle.TextHeight * _scale / CapHeight;
        return size < MinFontPx ? null : _draw.Text(value, brush, size);
    }

    /// <summary>Distance from the text's top to the middle of its capitals (Kata's text height).</summary>
    private double CapCentre(FormattedText text) => text.Baseline - KataTagStyle.TextHeight * _scale / 2.0;
}
