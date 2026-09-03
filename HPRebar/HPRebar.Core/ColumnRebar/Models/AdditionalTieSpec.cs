namespace HPRebar.Core.ColumnRebar.Models;

/// <summary>Intermediate cross-tie settings, horizontal and vertical legs kept separate. Millimetres.</summary>
public sealed record AdditionalTieSpec
{
    public bool AddH { get; init; }

    /// <summary>0 places horizontal legs by spacing <see cref="AH"/>; anything else places <see cref="NH"/> of them.</summary>
    public int TypeH { get; init; }

    public int NH { get; init; } = 1;

    public double AH { get; init; }

    public bool AddV { get; init; }

    /// <summary>0 places vertical legs by spacing <see cref="AV"/>; anything else places <see cref="NV"/> of them.</summary>
    public int TypeV { get; init; }

    public int NV { get; init; } = 1;

    public double AV { get; init; }
}
