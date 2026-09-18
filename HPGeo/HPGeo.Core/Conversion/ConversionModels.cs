using HPGeo.Core.Model;
using HPGeo.Core.Projection;

namespace HPGeo.Core.Conversion;

/// <summary>A survey point read from the drawing, in drawing units (not yet metres).</summary>
public sealed record SurveyPoint(int Index, string Label, PlanePoint DrawingXY, string? SourceHandle = null);

/// <summary>A boundary (polyline) read from the drawing, vertices in drawing units, arcs already flattened.</summary>
public sealed record BoundaryPolyline(string Name, IReadOnlyList<PlanePoint> DrawingVertices, bool Closed, string? SourceHandle = null);

public enum IssueSeverity
{
    Info,
    Warning,
    Error,
}

/// <summary>Something the user should see. <see cref="IssueSeverity.Error"/> issues make the conversion fail.</summary>
public sealed record ConversionIssue(IssueSeverity Severity, string Code, string Message, int? PointIndex = null);

/// <summary>Everything the conversion needs besides the geometry. Built by the UI or the script command.</summary>
public sealed record ConversionOptions(TmParameters Tm, double MetersPerDrawingUnit)
{
    /// <summary>Points above this count only warn (the KMZ still writes); the reference tool cut at 500.</summary>
    public const int PointCountWarningThreshold = 5000;

    public string? ProvinceName { get; init; }
    public string? CatalogLabel { get; init; }
}

public sealed record ConvertedPoint(SurveyPoint Source, PlanePoint GridM, GeoPoint Wgs84, string? Hint);

public sealed record ConvertedBoundary(BoundaryPolyline Source, IReadOnlyList<PlanePoint> GridM, IReadOnlyList<GeoPoint> Wgs84);

public sealed class ConversionResult
{
    public ConversionResult(IReadOnlyList<ConvertedPoint> points, IReadOnlyList<ConvertedBoundary> boundaries,
        IReadOnlyList<ConversionIssue> issues, ConversionOptions options)
    {
        Points = points;
        Boundaries = boundaries;
        Issues = issues;
        Options = options;
    }

    public IReadOnlyList<ConvertedPoint> Points { get; }
    public IReadOnlyList<ConvertedBoundary> Boundaries { get; }
    public IReadOnlyList<ConversionIssue> Issues { get; }
    public ConversionOptions Options { get; }

    public bool Success => Issues.All(i => i.Severity != IssueSeverity.Error);
    public IEnumerable<ConversionIssue> Errors => Issues.Where(i => i.Severity == IssueSeverity.Error);
    public IEnumerable<ConversionIssue> Warnings => Issues.Where(i => i.Severity == IssueSeverity.Warning);

    /// <summary>Centroid of every converted vertex — where a map or Google Earth should open.</summary>
    public GeoPoint? Center
    {
        get
        {
            var all = Points.Select(p => p.Wgs84).Concat(Boundaries.SelectMany(b => b.Wgs84)).ToList();
            if (all.Count == 0) return null;
            return new GeoPoint(all.Average(p => p.LatDeg), all.Average(p => p.LonDeg));
        }
    }
}
