using System;
using System.Windows.Media;
using HPRebar.Core.KataRebar.Calculators;

// WPF types, not the Revit ones the SDK imports globally.
using Brush = System.Windows.Media.Brush;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// Kata's texts (style kata_text: Arial, width factor 0.8) at a height in model millimetres, scaled with the view:
/// the height is the capital height, the width squeezed to Kata's (<see cref="KataDrawingStyle.CharWidthRatio"/> of
/// the height per character), never drawn below <see cref="MinFontPx"/>.
/// </summary>
internal sealed class KataCadText
{
    /// <summary>Segoe UI capitals are this fraction of the font size.</summary>
    private const double CapHeight = 0.70;

    private const double MinFontPx = 1.5;

    private readonly KataDrawPrimitives _draw;
    private readonly double _scale;

    public KataCadText(KataDrawPrimitives draw, double scale)
    {
        _draw = draw;
        _scale = scale;
    }

    public enum Align
    {
        Left,
        Centre,
        Right
    }

    /// <summary>
    /// Draws <paramref name="value"/> <paramref name="heightMm"/> high with its baseline through (<paramref name="x"/>,
    /// <paramref name="baselineY"/>) aligned there as asked; <paramref name="vertical"/> turns it to read upwards.
    /// Returns the width drawn (px), 0 when too small to draw.
    /// </summary>
    public double Draw(string value, Brush brush, double heightMm, double x, double baselineY, Align align, bool vertical = false)
    {
        if (string.IsNullOrEmpty(value)) return 0.0;
        double size = heightMm * _scale / CapHeight;
        if (size < MinFontPx) return 0.0;

        var text = _draw.Text(value, brush, size);
        double width = Math.Min(text.Width, KataDrawingStyle.CharWidthRatio * heightMm * value.Length * _scale);
        double left = align switch
        {
            Align.Left => 0.0,
            Align.Centre => -width / 2.0,
            _ => -width
        };

        _draw.Push(new TranslateTransform(x, baselineY));
        if (vertical) _draw.Push(new RotateTransform(-90.0));
        try
        {
            _draw.AtWidth(text, left, -text.Baseline, width);
        }
        finally
        {
            if (vertical) _draw.Pop();
            _draw.Pop();
        }

        return width;
    }
}
