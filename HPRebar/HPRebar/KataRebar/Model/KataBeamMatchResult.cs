using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using HPRebar.BeamRebar.Service;
using HPRebar.KataExport.Service;

namespace HPRebar.KataRebar.Model;

/// <summary>
/// Result of matching Revit plan framing elements with a KataBeamRebarSpec.
/// Contains the ordered host beams and spatial coordinate mapping to Revit world space.
/// </summary>
public sealed class KataBeamMatchResult
{
    public bool IsSuccess { get; init; }
    public string Message { get; init; } = "";
    public IReadOnlyList<FamilyInstance> OrderedBeams { get; init; } = Array.Empty<FamilyInstance>();
    public PointMapper? PointMapper { get; init; }
    public KataAxisFrame? AxisFrame { get; init; }
    public double BeamTopElevationFt { get; init; }
    public double BeamWidthMm { get; init; }
    public double BeamHeightMm { get; init; }
    public double TotalRunLengthMm { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
