using System.Globalization;
using System.Windows;
using System.Windows.Media;

// WPF types, not the Revit ones the SDK imports globally.
using Brush = System.Windows.Media.Brush;
using FontFamily = System.Windows.Media.FontFamily;
using FormattedText = System.Windows.Media.FormattedText;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// Kata's texts in its text style kata_text (Arial, width factor 0.8, T2-DY7.dwg) at a height in model millimetres,
/// scaled with the view. As in AutoCAD the height is the height of the capitals, and the glyphs are squeezed to 0.8 of
/// their width; nothing is drawn below <see cref="MinFontPx"/>.
/// </summary>
internal sealed class KataCadText
{
    /// <summary>kata_text's width factor.</summary>
    private const double WidthFactor = 0.8;

    private const double MinFontPx = 1.5;

    /// <summary>Arial's capital height as a fraction of its font size, when the glyphs cannot be read.</summary>
    private const double ArialCapsHeightFallback = 0.716;

    private static readonly Typeface Arial = new(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

    private static readonly double CapsHeight = Arial.TryGetGlyphTypeface(out var glyphs) ? glyphs.CapsHeight : ArialCapsHeightFallback;

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
        double size = heightMm * _scale / CapsHeight;
        if (size < MinFontPx) return 0.0;

        var text = new FormattedText(value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Arial, size, brush, _draw.PixelsPerDip);
        double width = text.Width * WidthFactor;
        double left = align switch
        {
            Align.Left => 0.0,
            Align.Centre => -width / 2.0,
            _ => -width
        };

        _draw.Push(new TranslateTransform(x, baselineY));
        if (vertical) _draw.Push(new RotateTransform(-90.0));
        _draw.Push(new ScaleTransform(WidthFactor, 1.0));
        try
        {
            _draw.At(text, left / WidthFactor, -text.Baseline);
        }
        finally
        {
            _draw.Pop();
            if (vertical) _draw.Pop();
            _draw.Pop();
        }

        return width;
    }

    /// <summary>Draws <paramref name="value"/> with the middle of its capitals on <paramref name="middleY"/>.</summary>
    public double DrawCentredOn(string value, Brush brush, double heightMm, double x, double middleY, Align align) =>
        Draw(value, brush, heightMm, x, middleY + heightMm * _scale / 2.0, align);
}
