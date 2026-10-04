namespace HPRebar.Core.ColumnRebar.Models;

/// <summary>
/// How a column segment's ties are spread over its run, in the order the window lists them: even, then dense
/// ends around a sparse middle of growing length. The numbers are the window's list positions.
/// </summary>
public enum TieLayout
{
    /// <summary>One even group at <see cref="StirrupSpec.S"/> over the whole run.</summary>
    Even = 0,

    /// <summary>Dense ends a quarter of the run each, sparse middle half.</summary>
    SparseMiddleHalf = 1,

    /// <summary>Dense ends a sixth of the run each, sparse middle two thirds.</summary>
    SparseMiddleTwoThirds = 2,

    /// <summary>Dense ends an eighth of the run each, sparse middle three quarters.</summary>
    SparseMiddleThreeQuarters = 3
}
