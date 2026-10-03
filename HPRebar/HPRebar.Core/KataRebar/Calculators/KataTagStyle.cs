namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Sizes of Kata's elevation tags and bar lines in model millimetres, read from its drawings at TL 1/25 (block
/// kata_block_KHT inserted at scale 25: 2.5 mm text and circles on paper are 62.5 mm in the model).
/// </summary>
public static class KataTagStyle
{
    /// <summary>Height of the tag text and radius of the number circles.</summary>
    public const double TextHeight = 62.5;

    public const double CircleRadius = 62.5;

    /// <summary>Kata's text runs 0.68 of its height per character ("2Ø18+1Ø18": 383 mm at 62.5).</summary>
    public const double CharWidth = 0.68 * TextHeight;

    /// <summary>Text right-aligned this far before the insertion point (tags pointing right).</summary>
    public const double TextGapRight = 21.25;

    /// <summary>Text left-aligned this far after the insertion point (tags pointing left).</summary>
    public const double TextGapLeft = 12.5;

    /// <summary>Text baseline above the leader's horizontal line.</summary>
    public const double TextLift = 21.25;

    /// <summary>First row of tags over the beam top face; each next row one pitch further out.</summary>
    public const double FirstRowAbove = 100.0;

    /// <summary>First row of tags under the span's soffit.</summary>
    public const double FirstRowBelow = 137.5;

    public const double RowPitch = 137.5;

    /// <summary>Row of the stirrup tags over the beam top face.</summary>
    public const double StirrupRow = 387.5;

    /// <summary>The stirrup row sits this far over the outermost row of bar tags (387.5 over row 2 at 237.5).</summary>
    public const double StirrupOverLastRow = 150.0;

    /// <summary>A stirrup tag's insertion point is this far past the middle of its zone.</summary>
    public const double StirrupShift = 125.0;

    /// <summary>Horizontal part of a leader: at least this long, else <see cref="LeaderPerChar"/> per character.</summary>
    public const double MinLeader = 200.0;

    public const double LeaderPerChar = 48.0;

    /// <summary>Leader arrowhead length (DIMASZ 1.5 at DIMSCALE 25).</summary>
    public const double ArrowSize = 37.5;

    /// <summary>End tick of a bar line: back along the bar, and toward its inside.</summary>
    public const double TickAlong = 75.0;

    public const double TickAcross = 25.0;

    /// <summary>Radius of the bends of a hooked bar.</summary>
    public const double BendRadius = 20.0;

    /// <summary>Elevation stirrup strokes stop this far inside the beam faces.</summary>
    public const double StirrupInset = 25.0;


    public static double LeaderLength(string text) => System.Math.Max(MinLeader, LeaderPerChar * text.Length);
}
