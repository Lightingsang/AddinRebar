namespace HPRebar.Core.ColumnRebar.Models;

/// <summary>How a main bar ends at the top of its column segment.</summary>
public enum TopDowelStyle
{
    /// <summary>The bar bends across into the column above (the window's 0).</summary>
    BendIntoColumnAbove = 0,

    /// <summary>The bar stops under the beam, optionally with a horizontal hook (any other number in the window).</summary>
    StopUnderBeam = 1
}
