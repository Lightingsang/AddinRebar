namespace HPBim.Core.Coordination;

using System;
using System.Collections.Generic;

/// <summary>
/// Source geometry and design specification extracted from CAD (DWG) drawings.
/// </summary>
public sealed record CadSourceSpec
{
    public string DwgSheet { get; init; } = string.Empty;
    public string Handle { get; init; } = string.Empty;
    public string Layer { get; init; } = string.Empty;
    public double? TopElevation { get; init; }
    public double? BottomElevation { get; init; }
    public double? Width { get; init; }
    public double? Height { get; init; }
    public double? Diameter { get; init; }
    public IReadOnlyList<string> Supports { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> CrossCheckSources { get; init; } = Array.Empty<string>();
    public bool CrossCheckPassed { get; init; } = false;
    public string RawNotes { get; init; } = string.Empty;
}
