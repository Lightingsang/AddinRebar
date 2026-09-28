using System;
using System.Collections.Generic;

namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// Specification for transverse beam stirrups.
/// </summary>
public sealed record KataStirrupSpec
{
    /// <summary>Bar diameter of stirrup in mm (sheet Dam cell G6, e.g. 10).</summary>
    public double Diameter { get; init; } = 10.0;

    /// <summary>Spacing in dense zone near support in mm (sheet Dam cell G7, e.g. 150).</summary>
    public double SupportSpacing { get; init; } = 150.0;

    /// <summary>Spacing in sparse zone at mid-span in mm (sheet Dam cell G8, e.g. 200).</summary>
    public double MidspanSpacing { get; init; } = 200.0;

    /// <summary>Optional spacing in dense zone at right support if asymmetric in mm (e.g. from a100/200/50).</summary>
    public double? EndSupportSpacing { get; init; }

    /// <summary>Spacing along cantilever spans in mm (sheet Dam cell G9, e.g. 150).</summary>
    public double CantileverSpacing { get; init; } = 150.0;

    /// <summary>Default number of vertical legs (sheet Dam cell I8, e.g. 2 or 4).</summary>
    public int DefaultLegCount { get; init; } = 2;

    /// <summary>Stirrup branch shape details from rows 25-27 (Closed hoop, Cap U, Cross tie C).</summary>
    public IReadOnlyList<KataStirrupBranchSpec> Branches { get; init; } = Array.Empty<KataStirrupBranchSpec>();
}

/// <summary>
/// Specification for a single branch type in the composite stirrup assembly (rows 25-27).
/// </summary>
public sealed record KataStirrupBranchSpec(
    KataStirrupShapeType ShapeType = KataStirrupShapeType.ClosedHoop,
    string Position = ""
);
