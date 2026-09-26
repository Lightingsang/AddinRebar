using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataExport.Models;

namespace HPRebar.Core.KataExport.Calculators;

/// <summary>
/// Which foundations count as supports and which standing elements deserve a warning — the decisions the Revit
/// scan makes, kept here so their limits are testable without Revit.
/// </summary>
public static class KataSupportRules
{
    /// <summary>Foundation slabs this thin are lean concrete (blinding), never a support.</summary>
    public const double LeanConcreteMaxMm = 100.0;

    /// <summary>
    /// A foundation longer than this along the run is a strip or raft under it, not a point support; isolated
    /// footings, pile caps and two-column footings stay below it.
    /// </summary>
    public const double MaxFoundationSupportMm = 6000.0;

    /// <summary>Thicknesses come from feet converted to millimetres; a type set to 100 mm reads 100.0000001.</summary>
    private const double ThicknessToleranceMm = 0.5;

    public static bool IsLeanConcrete(double thicknessMm) => thicknessMm <= LeanConcreteMaxMm + ThicknessToleranceMm;

    public static bool IsStripOrRaft(Interval1D footing) => footing.Length > MaxFoundationSupportMm;

    /// <summary>
    /// Columns and walls resting on the beam that are not over a support: those are the transfer loads worth a
    /// warning. One standing over a column, wall or footing support is simply that support's column above.
    /// </summary>
    public static int CountStandingOffSupports(IEnumerable<Interval1D> standing, IReadOnlyCollection<Interval1D> supports) =>
        standing.Count(element => !supports.Any(support => support.Overlaps(element)));

    /// <summary>Supports carrying more than one column above (two-column footings); Kata's row 19 holds only one.</summary>
    public static int CountSupportsWithSeveralColumnsAbove(IEnumerable<Interval1D> supports, IReadOnlyCollection<Interval1D> columnsAbove) =>
        supports.Count(support => columnsAbove.Count(column => column.Overlaps(support, 0.0)) > 1);
}
