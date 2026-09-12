using System.Collections.Generic;
using HPRebar.Core.ColumnRebar.Models;

namespace HPRebar.ColumnRebar.Model;

/// <summary>Everything the user chooses for one column segment. Lengths are millimetres.</summary>
public sealed record ColumnRebarSpec
{
    public BarLayoutSpec Layout { get; init; } = new();

    /// <summary>One entry per main bar, in bar-number order.</summary>
    public IReadOnlyList<SpliceSpec> Splices { get; init; } = new List<SpliceSpec>();

    public StirrupSpec Stirrups { get; init; } = new();

    public AdditionalTieSpec Ties { get; init; } = new();

    public RebarTypeInfo MainBarType { get; init; } = null!;

    public RebarTypeInfo StirrupBarType { get; init; } = null!;

    public RebarTypeInfo TieBarType { get; init; } = null!;

    /// <summary>Value written to the rebar Partition parameter, so a column's bars group in schedules.</summary>
    public string PartitionName { get; init; } = "Column";
}
