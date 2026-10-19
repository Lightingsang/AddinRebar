namespace HPBim.Core.Verification;

using System;
using System.Collections.Generic;
using HPBim.Core.Geometry;

public sealed record ActualElementGeometry
{
    public string Key { get; init; } = string.Empty;
    public string? UniqueId { get; init; }
    public long? ElementId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string FamilyName { get; init; } = string.Empty;
    public string TypeName { get; init; } = string.Empty;
    public bool HasGeometry { get; init; }

    // Spatial coordinates (mm)
    public Point3D? Centroid { get; init; }
    public Point3D? CurveStart { get; init; }
    public Point3D? CurveEnd { get; init; }
    public double? RotationDegrees { get; init; }

    // Vertical elevations (mm)
    public double? TopElevationMm { get; init; }
    public double? BottomElevationMm { get; init; }

    // Cross-section / profile dimensions (mm)
    public double? WidthMm { get; init; }
    public double? HeightMm { get; init; }
    public double? ThicknessMm { get; init; }

    public IReadOnlyDictionary<string, double> MeasuredParameters { get; init; } = new Dictionary<string, double>();
    public string? MeasurementNotes { get; init; }
}
