namespace HPRebar.Core.ColumnRebar.Models;

/// <summary>How a main bar starts at the bottom of its column segment.</summary>
public enum BottomDowelStyle
{
    /// <summary>The bar starts its lap length above the segment base (the window's 0).</summary>
    StartAboveBase = 0,

    /// <summary>The bar runs down past the base, optionally with a hook (any other number in the window).</summary>
    RunPastBase = 1
}
