using System.Globalization;

namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// How a kata_block_KHT tag sets its text, the last digit of the block's visibility state (P11, P12, T13…).
/// </summary>
public enum KataTagLayout
{
    /// <summary>One line on the leader (attribute DK): "2Ø18".</summary>
    OneLine = 1,

    /// <summary>The bars on the leader, the spacing under it (DK over KC): "Ø8" / "a500", every stirrup tag of a section.</summary>
    SpacingBelow = 2,

    /// <summary>One line centred on the row (DKKC), no leader: the elevation's stirrup tags "Ø8a100".</summary>
    Centred = 3
}

/// <summary>The visibility states of kata_block_KHT as Kata's drawings name them.</summary>
public static class KataTagState
{
    /// <summary>
    /// P when the circles sit before the insertion point (the leader runs left), T when they follow it, then the number
    /// of circles, then the <see cref="KataTagLayout"/>: P11, T21, P12, T13.
    /// </summary>
    public static string Of(bool pointsRight, int circles, KataTagLayout layout) =>
        string.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", pointsRight ? 'T' : 'P', circles, (int)layout);
}
