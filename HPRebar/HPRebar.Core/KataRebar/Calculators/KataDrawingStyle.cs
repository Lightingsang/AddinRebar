namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Where Kata puts the parts of a beam elevation and how big they are, in model millimetres (TL 1/25: its blocks are
/// inserted at scale 25 and dimension style kata_dim_25 has DIMSCALE 25). Heights are from the beam top; the rows
/// under the beam hang from the bottom of the column stubs, <see cref="StubBelow"/> under the lowest soffit.
/// </summary>
public static class KataDrawingStyle
{
    /// <summary>Column stubs reach this far over the beam top and under the lowest soffit.</summary>
    public const double StubAbove = 150.0;

    public const double StubBelow = 150.0;

    /// <summary>Half the zigzag of a break line, at most; a narrow column gets a sixth of its width.</summary>
    public const double BreakSize = 50.0;

    /// <summary>Dimension line of the support / stirrup-zone chain over the beam.</summary>
    public const double TopChainZ = 462.0;

    /// <summary>Insertion of the section flags over the beam.</summary>
    public const double FlagAboveZ = 587.5;

    /// <summary>A flag's stem reaches this far past its insertion (kata_block_SECBAL: 4.357 × 25).</summary>
    public const double FlagHeight = 108.9;

    /// <summary>Grid lines start this high over the beam.</summary>
    public const double GridTopZ = 662.5;

    /// <summary>Rows under the stub bottom: bar-cut chain, grid chain, grid line end, bubbles and lower flags, title.</summary>
    public const double BottomChainBelow = 300.0;

    public const double AxisChainBelow = 475.0;
    public const double GridEndBelow = 500.0;
    public const double BubbleBelow = 675.0;
    public const double TitleBelow = 950.0;

    /// <summary>Stagger dimensions of additional bars sit this far over the layer-1 bar.</summary>
    public const double StaggerDimAbove = 125.0;

    /// <summary>Depth dimensions left of the beam: the span's whole depth, then slab and the rest.</summary>
    public const double DepthDimX = -350.0;

    public const double SlabDimX = -200.0;

    /// <summary>Level mark (block kata_block_CT) at the beam top, this far left of the beam's start.</summary>
    public const double LevelX = -475.0;

    /// <summary>Stirrup-zone run dimension when the sheet gives no slab (B7 empty).</summary>
    public const double DefaultSlab = 120.0;

    /// <summary>Kata dimension text (DIMTXT 2.5), gap to the line (DIMGAP 0.625), arch tick (DIMASZ 1.5).</summary>
    public const double DimTextHeight = 62.5;

    public const double DimTextGap = 15.625;
    public const double DimTickSize = 37.5;

    /// <summary>Width of the slash of kata_block_ArchTick (0.15 of the tick size).</summary>
    public const double DimTickWidth = 0.15 * DimTickSize;

    /// <summary>Extension lines: fixed length from the dimension line (DIMFXL 3.75) and beyond it (DIMEXE 1.5).</summary>
    public const double ExtensionLength = 93.75;

    public const double ExtensionBeyond = 37.5;

    /// <summary>The dimension line runs past each extension line (DIMDLE 1.2).</summary>
    public const double DimLineBeyond = 30.0;

    /// <summary>kata_rai_thep: run tick arrow (kata_block_RUNTIC, DIMASZ 1) and its cross stroke.</summary>
    public const double RunArrowLength = 67.5;

    public const double RunTickHalf = 18.75;

    /// <summary>Grid bubble (kata_block_GRID): circle radius, text height, ticks out to 175.</summary>
    public const double BubbleRadius = 125.0;

    public const double BubbleTextHeight = 125.0;
    public const double BubbleTickEnd = 175.0;

    /// <summary>Title (kata_block_TD): name and its scale line.</summary>
    public const double TitleTextHeight = 125.0;

    public const double TitleNameLift = 36.8;
    public const double TitleScaleDrop = 100.0;
    public const string TitleScale = "TL: 1/25";

    /// <summary>Rough text width per character of Kata's kata_text style (Arial, width factor 0.8).</summary>
    public const double CharWidthRatio = 0.68;

    /// <summary>Linetype patterns scaled as drawn: HIDDEN × 18.75 and CENTER × 12.5 (dash, gap, ...).</summary>
    public static readonly double[] HiddenDashes = { 6.35 * 18.75, 3.175 * 18.75 };

    public static readonly double[] CenterDashes = { 31.75 * 12.5, 6.35 * 12.5, 6.35 * 12.5, 6.35 * 12.5 };
}
