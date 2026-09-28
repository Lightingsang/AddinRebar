using System;
using System.Collections.Generic;

namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// Reinforcement and geometric specification for a clear span (even columns in sheet Dam).
/// </summary>
public sealed record KataSpanRebarSpec
{
    /// <summary>0-based index of this span along the beam run.</summary>
    public int SpanIndex { get; init; }

    /// <summary>1-based sheet column this entry was read from (3 = C); 0 when the spec was not read from a sheet.</summary>
    public int SheetColumn { get; init; }

    /// <summary>Clear span length Ln in mm (sheet Dam row 11).</summary>
    public double Length { get; init; }

    /// <summary>Bottom extra positive reinforcement bars - Layer 1 (sheet Dam row 18).</summary>
    public IReadOnlyList<KataBarItem> BottomExtraLayer1 { get; init; } = Array.Empty<KataBarItem>();

    /// <summary>Bottom extra positive reinforcement bars - Layer 2 (sheet Dam row 17).</summary>
    public IReadOnlyList<KataBarItem> BottomExtraLayer2 { get; init; } = Array.Empty<KataBarItem>();

    /// <summary>Row 18 as written in the sheet, kept to report a cell none of which reads as bars.</summary>
    public string BottomExtraLayer1Text { get; init; } = "";

    /// <summary>Row 17 as written in the sheet.</summary>
    public string BottomExtraLayer2Text { get; init; } = "";

    /// <summary>Convenience accessor for both bottom extra bar layers.</summary>
    public IReadOnlyList<IReadOnlyList<KataBarItem>> AllBottomExtraLayers =>
        new[] { BottomExtraLayer1, BottomExtraLayer2 };

    /// <summary>Web skin / side reinforcement bars for this span (sheet Dam row 20 or global).</summary>
    public IReadOnlyList<KataBarItem> SideBars { get; init; } = Array.Empty<KataBarItem>();

    /// <summary>Optional stirrup specification overriding global stirrups for this span (sheet Dam row 22).</summary>
    public KataStirrupSpec? StirrupOverride { get; init; }

    /// <summary>Top surface drop/step offset across this span in mm (sheet Dam row 19, e.g. -50).</summary>
    public double TopDrop { get; init; }

    /// <summary>Bottom soffit drop/step offset across this span in mm (sheet Dam row 21, e.g. -100).</summary>
    public double SoffitDrop { get; init; }

    /// <summary>Bars associated with top drop change (e.g. from "100;5f25").</summary>
    public IReadOnlyList<KataBarItem> TopDropBars { get; init; } = Array.Empty<KataBarItem>();

    /// <summary>Bars associated with bottom soffit drop change (e.g. from "-100;5f20").</summary>
    public IReadOnlyList<KataBarItem> SoffitDropBars { get; init; } = Array.Empty<KataBarItem>();
}
