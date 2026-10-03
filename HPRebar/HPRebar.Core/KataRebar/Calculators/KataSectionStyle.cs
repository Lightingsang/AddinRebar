namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Where Kata puts the parts of a beam section and how big they are, in model millimetres (TL 1/25), read from the 27
/// sections of T2-DY7.dwg (DY7, DY14 at E = 350 and 500; all 300 wide). X from the beam's centre line, Z from its top.
/// </summary>
public static class KataSectionStyle
{
    /// <summary>The slab is drawn this far past each beam face, its break lines there.</summary>
    public const double SlabReach = 120.0;

    /// <summary>Half the zigzag of a slab break line, and how far it runs past the slab's faces.</summary>
    public const double BreakSize = 20.0;

    /// <summary>Clear gap Kata draws between two layers of bars.</summary>
    public const double LayerClear = 25.0;

    /// <summary>Hook of the hoop: each leg runs this many stirrup diameters along both axes (40 for Ø8).</summary>
    public const double HoopHookDiameters = 5.0;

    /// <summary>Bulges of the hoop: a 90° corner and the 135° bend into a hook (negative: clockwise).</summary>
    public const double CornerBulge = -0.414214;

    public const double HookBulge = -0.668179;

    /// <summary>A tie's bends: radius two stirrup diameters, centre that radius inside the hoop's line; tails 5.5 diameters.</summary>
    public const double TieBendDiameters = 2.0;

    public const double TieTailDiameters = 5.5;

    /// <summary>Leader arrowheads: kata_dim_25's DIMASZ 1.5, the side bars' double arrows DIMASZ 1, at DIMSCALE 25.</summary>
    public const double ArrowSize = 37.5;

    public const double SmallArrowSize = 25.0;

    /// <summary>Circle drawn round each bar of an inner layer, and the drop of that layer's leader toward the beam centre.</summary>
    public const double MarkRadius = 18.75;

    public const double InnerLayerDrop = 50.0;

    /// <summary>Rows of the outer-layer tags: corner bars, then the bars between them (one more pitch each), over the top.</summary>
    public const double TopCornerRow = 100.0;

    public const double TopMiddleRow = 175.0;

    public const double TopRowPitch = 75.0;

    /// <summary>The same under the soffit (the middle row a larger pitch down).</summary>
    public const double BottomCornerRow = 125.0;

    public const double BottomMiddleRow = 272.0;

    public const double BottomRowPitch = 147.0;

    /// <summary>Tags of the inner-layer ties: over the top, under the soffit; their leader foot halfway to the corner bar.</summary>
    public const double TopTieRow = 295.5;

    public const double BottomTieRow = 301.25;

    /// <summary>Insertion of the inner-layer tags right of the centre line, over and under (b/2 + 370 / 250).</summary>
    public const double TopInnerTagBeyond = 370.0;

    public const double BottomInnerTagBeyond = 250.0;

    /// <summary>Insertion of the inner-layer tie tags left of the beam face.</summary>
    public const double InnerTieTagBeyond = 127.875;

    /// <summary>One layer of side bars: tie leader foot left of the centre line, its row over the bars, its insertion past the face.</summary>
    public const double SideTieFoot = 34.333;

    public const double SideTieRise = 91.0;

    public const double SideTieTagBeyond = 206.375;

    /// <summary>Two or more layers: the tie leaders' and the bar leaders' feet, and their insertions past the faces.</summary>
    public const double SideTiesFoot = 65.333;

    public const double SideTiesTagBeyond = 212.375;

    public const double SideBarsFoot = 59.333;

    public const double SideBarsTagBeyond = 331.5;

    /// <summary>
    /// One layer of side bars: the tag's insertion past the face for a 500-deep section, moving in as the section
    /// deepens (520 at 500, 425 at 600 — a fit of Kata's two cases; its own rule is unknown).
    /// </summary>
    public const double SideBarTagBeyond = 370.0;

    public const double SideBarTagPerDepth = 0.95;

    /// <summary>The hoop's tag: its leader runs this far left from the hoop's side.</summary>
    public const double HoopTagLeader = 238.0;

    /// <summary>
    /// Height of the hoop's tag when side bars are drawn: −0.75 h + 25, 37.5 lower per layer past the first (a fit of
    /// Kata's three cases: −350 at 500, −425 at 600, −462.5 at 600 with two layers; its own rule is unknown).
    /// Without side bars it sits halfway between slab soffit and beam bottom.
    /// </summary>
    public const double HoopTagDepthRatio = 0.75;

    public const double HoopTagLift = 25.0;

    public const double HoopTagPerLayer = 37.5;

    /// <summary>Width dimension under the beam: this far under the soffit, and further when a second row of tags hangs there.</summary>
    public const double WidthDimBelow = 300.0;

    public const double WidthDimSecondRow = 150.0;

    /// <summary>The width dimension stays at least this far under the lowest tag row (Kata: 148.75 under its tie tags).</summary>
    public const double WidthDimUnderTags = 145.0;

    /// <summary>Depth dimensions left of the beam face: slab and the rest, then the whole depth.</summary>
    public const double DepthChainBeyond = 375.0;

    public const double DepthDimBeyond = 506.0;

    /// <summary>The title (kata_block_TD) under the width dimension.</summary>
    public const double TitleBelowDim = 250.0;
}
