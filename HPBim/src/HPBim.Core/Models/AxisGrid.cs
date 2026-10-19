namespace HPBim.Core.Models;

using System;
using System.Text.Json.Serialization;
using HPBim.Core.Geometry;

public sealed record AxisGrid
{
    public string Name { get; init; } = string.Empty;
    public double CoordinateMm { get; init; }
    public GridDirection Direction { get; init; }
    public Point2D StartPoint { get; init; }
    public Point2D EndPoint { get; init; }

    [JsonIgnore]
    public double LengthMm => StartPoint.DistanceTo(EndPoint);

    public AxisGrid() { }

    public AxisGrid(string name, double coordinateMm, GridDirection direction, Point2D startPoint, Point2D endPoint)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        CoordinateMm = coordinateMm;
        Direction = direction;
        StartPoint = startPoint;
        EndPoint = endPoint;
    }

    public static AxisGrid CreateOrthogonal(
        string name, double coordinateMm, GridDirection direction, 
        double minExtentMm, double maxExtentMm, double extensionMm = 1500.0)
    {
        double pStart = minExtentMm - extensionMm;
        double pEnd = maxExtentMm + extensionMm;

        Point2D start = direction == GridDirection.X 
            ? new Point2D(coordinateMm, pStart) 
            : new Point2D(pStart, coordinateMm);

        Point2D end = direction == GridDirection.X 
            ? new Point2D(coordinateMm, pEnd) 
            : new Point2D(pEnd, coordinateMm);

        return new AxisGrid(name, coordinateMm, direction, start, end);
    }
}
