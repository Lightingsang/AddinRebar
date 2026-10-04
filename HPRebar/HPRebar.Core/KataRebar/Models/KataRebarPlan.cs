using System;
using System.Collections.Generic;

namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// Everything needed to draw one beam: the effective spec (sheet values replaced by Revit geometry and
/// reduced to what this version draws), the rules, the layout and the reasons anything was left out.
/// </summary>
public sealed record KataRebarPlan
{
    public KataBeamRebarSpec Spec { get; init; } = new();

    public KataDetailingRules Rules { get; init; } = new();

    public KataRebarLayoutResult Layout { get; init; } = new();

    /// <summary>The sheet describes the run from Revit's far end: local X runs against the Revit axis.</summary>
    public bool Reversed { get; init; }

    /// <summary>
    /// Sheet input this version does not draw yet, one line per cell; a line with an outcome after the meaning says what
    /// was drawn or kept instead (a joint's bottom bars drawn by the span rule, row 24 "*" honoured).
    /// </summary>
    public IReadOnlyList<string> Skipped { get; init; } = Array.Empty<string>();

    /// <summary>Reasons nothing can be drawn.</summary>
    public IReadOnlyList<string> Blocking { get; init; } = Array.Empty<string>();

    /// <summary>Differences and adjustments worth reading; they do not stop the run.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public bool CanGenerate => Blocking.Count == 0;
}
