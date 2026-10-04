namespace HPRebar.Core.ColumnRebar.Models;

/// <summary>
/// The intermediate tie the window offers, in its list order: one closed tie sized by its leg length, or single
/// cross-ties that differ only in how their ends are hooked. The numbers are the window's list positions; the
/// add-in picks the cross-tie shape from them.
/// </summary>
public enum CrossTieKind
{
    /// <summary>One closed inner tie, as wide (or deep) as its leg length.</summary>
    ClosedTie = 0,

    /// <summary>Single cross-ties with 90° hooks.</summary>
    Hooks90 = 1,

    /// <summary>Single cross-ties with 135° hooks.</summary>
    Hooks135 = 2,

    /// <summary>Single cross-ties with 180° hooks.</summary>
    Hooks180 = 3
}
