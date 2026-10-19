namespace HPBim.Core.Algorithms;

using System;
using System.Collections.Generic;
using HPBim.Core.Geometry;
using HPBim.Core.Models;

public sealed record OpeningPlacement(
    string OpeningId,
    string HostWallId,
    Point2D StartPoint2D,
    Point2D CenterPoint2D,
    Point2D EndPoint2D,
    Point3D InsertionPoint3D,
    double WallAngleDeg,
    double WidthMm,
    double HeightMm,
    double SillHeightMm,
    OpeningKind Kind,
    OpeningSwing Swing,
    Polygon2D? SwingClearancePolygon);

public sealed class OpeningPlacementEngine
{
    public OpeningPlacement ComputePlacement(WallDef wall, OpeningDef opening, double baseElevationMm = 0.0)
    {
        if (wall == null) throw new ArgumentNullException(nameof(wall));
        if (opening == null) throw new ArgumentNullException(nameof(opening));

        Vector2D u = wall.Direction;
        Vector2D n = wall.Normal;
        Point2D p1 = wall.StartPoint;

        double startDist = opening.DistanceAlongWallMm;
        double centerDist = opening.CenterDistanceMm;
        double endDist = startDist + opening.WidthMm;

        Point2D pStart = p1 + u * startDist;
        Point2D pCenter = p1 + u * centerDist;
        Point2D pEnd = p1 + u * endDist;

        double zInsertion = baseElevationMm + wall.BaseOffsetMm + opening.SillHeightMm;
        Point3D insertion3D = new Point3D(pCenter.X, pCenter.Y, zInsertion);

        double wallAngleDeg = Math.Atan2(u.Y, u.X) * (180.0 / Math.PI);

        Polygon2D? clearancePoly = null;
        if (opening.Kind == OpeningKind.Door || opening.Kind == OpeningKind.Door2)
        {
            clearancePoly = ComputeSwingClearance(pStart, u, n, wall.ThicknessMm, opening);
        }

        return new OpeningPlacement(
            OpeningId: opening.Id,
            HostWallId: wall.Id,
            StartPoint2D: pStart,
            CenterPoint2D: pCenter,
            EndPoint2D: pEnd,
            InsertionPoint3D: insertion3D,
            WallAngleDeg: wallAngleDeg,
            WidthMm: opening.WidthMm,
            HeightMm: opening.HeightMm,
            SillHeightMm: opening.SillHeightMm,
            Kind: opening.Kind,
            Swing: opening.Swing,
            SwingClearancePolygon: clearancePoly);
    }

    public IReadOnlyList<OpeningPlacement> ComputeAllPlacements(WallDef wall, double baseElevationMm = 0.0)
    {
        if (wall == null) throw new ArgumentNullException(nameof(wall));
        if (wall.Openings == null || wall.Openings.Count == 0)
        {
            return Array.Empty<OpeningPlacement>();
        }

        var results = new List<OpeningPlacement>(wall.Openings.Count);
        foreach (var op in wall.Openings)
        {
            results.Add(ComputePlacement(wall, op, baseElevationMm));
        }
        return results;
    }

    public Polygon2D ComputeSwingClearance(
        Point2D pStart,
        Vector2D u,
        Vector2D n,
        double wallThicknessMm,
        OpeningDef opening)
    {
        double width = opening.WidthMm;
        double side = opening.Swing == OpeningSwing.Right ? -1.0 : 1.0;

        Point2D hinge = pStart + n * (side * wallThicknessMm / 2.0);
        Point2D closedLeaf = hinge + u * width;

        var vertices = new List<Point2D> { hinge, closedLeaf };
        const int arcSegments = 6;
        double startAngle = Math.Atan2(u.Y, u.X);
        double sweep = side * (-Math.PI / 2.0);

        for (int i = 1; i <= arcSegments; i++)
        {
            double angle = startAngle + sweep * (i / (double)arcSegments);
            vertices.Add(new Point2D(
                hinge.X + width * Math.Cos(angle),
                hinge.Y + width * Math.Sin(angle)));
        }

        return new Polygon2D(vertices);
    }
}
