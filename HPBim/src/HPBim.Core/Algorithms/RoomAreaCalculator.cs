namespace HPBim.Core.Algorithms;

using System;
using System.Collections.Generic;
using HPBim.Core.Geometry;
using HPBim.Core.Models;

public sealed record RoomValidationResult(
    bool IsValid,
    bool IsEnclosed,
    bool IsTagInside,
    double ComputedAreaM2,
    double? TheoreticalAreaM2,
    double? AreaDeltaM2,
    string? Message);

public sealed class RoomAreaCalculator
{
    private const double Epsilon = 1e-6;

    public double ComputeAreaM2(Polygon2D polygon)
    {
        if (polygon == null) throw new ArgumentNullException(nameof(polygon));
        return polygon.Area / 1_000_000.0;
    }

    public bool IsEnclosed(Polygon2D polygon)
    {
        if (polygon == null || polygon.Vertices.Count < 3)
        {
            return false;
        }
        return Math.Abs(polygon.Area) > Epsilon;
    }

    public bool ContainsPoint(Polygon2D polygon, Point2D point)
    {
        if (polygon == null || polygon.Vertices.Count < 3)
        {
            return false;
        }

        var vertices = polygon.Vertices;
        int n = vertices.Count;
        bool inside = false;

        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            var vi = vertices[i];
            var vj = vertices[j];

            if (((vi.Y > point.Y) != (vj.Y > point.Y)) &&
                (point.X < (vj.X - vi.X) * (point.Y - vi.Y) / (vj.Y - vi.Y) + vi.X))
            {
                inside = !inside;
            }
        }

        return inside;
    }

    public RoomValidationResult ValidateRoom(RoomDef room, double areaToleranceRatio = 0.05)
    {
        if (room == null) throw new ArgumentNullException(nameof(room));
        if (room.BoundaryPolygon == null)
        {
            return new RoomValidationResult(
                IsValid: false,
                IsEnclosed: false,
                IsTagInside: false,
                ComputedAreaM2: 0.0,
                TheoreticalAreaM2: room.TheoreticalAreaM2,
                AreaDeltaM2: null,
                Message: "Room has no boundary polygon.");
        }

        bool isEnclosed = IsEnclosed(room.BoundaryPolygon);
        bool isTagInside = ContainsPoint(room.BoundaryPolygon, room.TagLocation);
        double computedAreaM2 = ComputeAreaM2(room.BoundaryPolygon);

        double? delta = null;
        bool areaMatches = true;
        if (room.TheoreticalAreaM2.HasValue)
        {
            delta = Math.Abs(computedAreaM2 - room.TheoreticalAreaM2.Value);
            double maxAllowedDiff = room.TheoreticalAreaM2.Value * areaToleranceRatio;
            areaMatches = delta.Value <= maxAllowedDiff;
        }

        bool isValid = isEnclosed && isTagInside && areaMatches;
        string? message = isValid ? null : BuildFailureMessage(isEnclosed, isTagInside, areaMatches);

        return new RoomValidationResult(
            IsValid: isValid,
            IsEnclosed: isEnclosed,
            IsTagInside: isTagInside,
            ComputedAreaM2: computedAreaM2,
            TheoreticalAreaM2: room.TheoreticalAreaM2,
            AreaDeltaM2: delta,
            Message: message);
    }

    private static string BuildFailureMessage(bool isEnclosed, bool isTagInside, bool areaMatches)
    {
        var issues = new List<string>(3);
        if (!isEnclosed) issues.Add("Polygon is not enclosed or has zero area");
        if (!isTagInside) issues.Add("Tag location is outside room boundary");
        if (!areaMatches) issues.Add("Computed area deviates from theoretical area");
        return string.Join("; ", issues);
    }
}
