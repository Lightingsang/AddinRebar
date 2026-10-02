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

    /// <summary>Spacing of the C ties from cell J7 ("a500" = 500 mm, Kata's template value); null when the sheet's J7 is empty or unreadable.</summary>
    public double? TieSpacing { get; init; } = 500.0;

    /// <summary>Cell J7 as written, for the warning when it cannot be read.</summary>
    public string TieSpacingText { get; init; } = "";

    /// <summary>Option group "Khoảng cách đai gia cường" (cell I8): evenly at J7, or at every outer hoop.</summary>
    public KataTieSpacingMode TieSpacingMode { get; init; } = KataTieSpacingMode.Uniform;

    /// <summary>Stirrup branch shape details from rows 25-27 (Closed hoop, Cap U, Cross tie C).</summary>
    public IReadOnlyList<KataStirrupBranchSpec> Branches { get; init; } = Array.Empty<KataStirrupBranchSpec>();
}

/// <summary>
/// One stirrup of the composite stirrup assembly. The outer closed hoop is always present; rows 25-44 of
/// each column pair add inner stirrups: the type in the left column ("Đai □", "Đai U", "Đai C") and the
/// main bars it wraps in the right one ("3-4", "2").
/// </summary>
/// <param name="Position">Bars wrapped by an inner stirrup ("3-4"); "Outer" for the outer hoop.</param>
/// <param name="Address">Cell holding the stirrup type, empty for the implied outer hoop.</param>
public sealed record KataStirrupBranchSpec(
    KataStirrupShapeType ShapeType = KataStirrupShapeType.ClosedHoop,
    string Position = "",
    string Address = "")
{
    public const string OuterPosition = "Outer";

    /// <summary>The outer closed hoop that every section has.</summary>
    public static readonly KataStirrupBranchSpec Outer = new(KataStirrupShapeType.ClosedHoop, OuterPosition);

    public bool IsOuterHoop => ShapeType == KataStirrupShapeType.ClosedHoop && Position == OuterPosition;
}
