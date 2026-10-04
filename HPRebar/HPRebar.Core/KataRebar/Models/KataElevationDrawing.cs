using System;
using System.Collections.Generic;
using System.Globalization;

namespace HPRebar.Core.KataRebar.Models;

/// <summary>The Kata layer a line belongs to; it decides colour, lineweight and linetype on screen.</summary>
public enum KataDrawingPen
{
    /// <summary>kata_net thay: concrete seen (cyan, 0.20).</summary>
    Outline,

    /// <summary>kata_net khuat: concrete hidden — column tops, slab soffit (grey, HIDDEN, 0.05).</summary>
    Hidden,

    /// <summary>kata_grid: grid lines (grey, CENTER, 0.05).</summary>
    Grid,

    /// <summary>kata_dim: break lines of the column stubs (grey, 0.05).</summary>
    Thin,

    /// <summary>kata_thep chu: longitudinal bars (red, 0.35).</summary>
    Bar,

    /// <summary>kata_thep dai: stirrups (magenta, 0.35).</summary>
    Stirrup
}

/// <summary>
/// A polyline of the drawing; X along the beam from the outer face of its first support, Z up from its top.
/// <paramref name="Keys"/> name the bars or stirrup zones a bar or stirrup line stands for (<see cref="Calculators.KataLayoutRemoval"/>).
/// </summary>
public sealed record KataDrawingLine(KataDrawingPen Pen, IReadOnlyList<(double X, double Z)> Points, IReadOnlyList<string>? Keys = null);

/// <summary>Dimension style: kata_dim_25 (ticks, text) or kata_rai_thep (a stirrup run, no text, run arrows).</summary>
public enum KataDimStyle
{
    Kata,
    Run
}

/// <summary>
/// A linear dimension between two points, measured along X (or Z when <paramref name="Vertical"/>), its line at
/// <paramref name="LineAt"/> — a height for a horizontal dimension, a station for a vertical one.
/// </summary>
public sealed record KataDrawingDim(double X1, double Z1, double X2, double Z2, bool Vertical, double LineAt, KataDimStyle Style = KataDimStyle.Kata)
{
    public double Value => Vertical ? Math.Abs(Z2 - Z1) : Math.Abs(X2 - X1);

    public string Text => Style == KataDimStyle.Run ? "" : Math.Round(Value).ToString("0", CultureInfo.InvariantCulture);
}

/// <summary>A section flag (kata_block_SECBAL) at a cut, over the beam or mirrored under it.</summary>
public sealed record KataDrawingFlag(double X, double Z, int Number, bool Below);

/// <summary>A grid bubble (kata_block_GRID) centred at (X, Z).</summary>
public sealed record KataDrawingBubble(double X, double Z, string Name);

/// <summary>Level mark (kata_block_CT) with its text, the triangle's tip at (X, Z).</summary>
public sealed record KataDrawingLevel(double X, double Z, string Text);

/// <summary>Title (kata_block_TD): beam name, count and length, underlined, and the scale under it.</summary>
public sealed record KataDrawingTitle(double X, double Z, string Name, string Scale);

/// <summary>
/// Kata's elevation of one beam run as its drawing lays it out: outline, bars, stirrups, dimensions, section flags,
/// grids, level and title. The bar tags are <see cref="Calculators.KataBarTagBuilder"/>'s.
/// </summary>
/// <param name="MinX">Leftmost and rightmost extent of everything drawn (mm).</param>
/// <param name="Top">Highest and lowest extent of everything drawn (mm).</param>
public sealed record KataElevationDrawing(
    IReadOnlyList<KataDrawingLine> Lines,
    IReadOnlyList<KataDrawingDim> Dims,
    IReadOnlyList<KataDrawingFlag> Flags,
    IReadOnlyList<KataDrawingBubble> Bubbles,
    KataDrawingLevel? Level,
    KataDrawingTitle? Title,
    double MinX,
    double MaxX,
    double Top,
    double Bottom);
