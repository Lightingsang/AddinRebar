using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.Core.FoundationRebar.Calculators;
using HPRebar.Core.FoundationRebar.Models;

namespace HPRebar.FoundationRebar.Service;

/// <summary>
/// Reads a Revit Floor's solid, its horizontal top and bottom faces and its bottom outline into a pure
/// <see cref="FoundationGeometrySnapshot"/>; the plan frame itself is fitted by
/// <see cref="FoundationPlanFrameCalculator"/>.
/// </summary>
public static class FoundationSolidFaceReader
{
    private const double Tolerance = 1.0e-6;

    /// <summary>
    /// Extracts the primary non-empty Solid from the floor element geometry.
    /// </summary>
    public static Solid GetSolid(Element element)
    {
        if (element is null) throw new ArgumentNullException(nameof(element));

        var options = new Options { ComputeReferences = true, DetailLevel = ViewDetailLevel.Fine };
        var geomElement = element.get_Geometry(options);
        if (geomElement is null)
            throw new InvalidOperationException($"Floor element {element.Id} has no geometry.");

        Solid? bestSolid = null;
        double maxVolume = 0.0;

        foreach (var geomObj in geomElement)
        {
            if (geomObj is Solid solid && solid.Volume > maxVolume)
            {
                bestSolid = solid;
                maxVolume = solid.Volume;
            }
            else if (geomObj is GeometryInstance instance)
            {
                foreach (var instObj in instance.GetInstanceGeometry())
                {
                    if (instObj is Solid instSolid && instSolid.Volume > maxVolume)
                    {
                        bestSolid = instSolid;
                        maxVolume = instSolid.Volume;
                    }
                }
            }
        }

        if (bestSolid is null || maxVolume <= Tolerance)
            throw new InvalidOperationException($"Floor element {element.Id} does not contain a solid with positive volume.");

        return bestSolid;
    }

    /// <summary>
    /// Identifies horizontal PlanarFaces: Top Face (normal collinear with (0,0,1), max Z)
    /// and Bottom Face (normal collinear with (0,0,-1), min Z).
    /// </summary>
    public static (PlanarFace TopFace, PlanarFace BottomFace) GetTopAndBottomFaces(Solid solid)
    {
        if (solid is null) throw new ArgumentNullException(nameof(solid));

        var planarFaces = solid.Faces.OfType<PlanarFace>().ToList();

        // Top face: normal collinear with (0,0,1) with maximum Z
        var topFaces = planarFaces
            .Where(f => f.FaceNormal.DotProduct(XYZ.BasisZ) > 0.99)
            .OrderByDescending(f => f.Origin.Z)
            .ToList();

        // Bottom face: normal collinear with (0,0,-1) with minimum Z
        var bottomFaces = planarFaces
            .Where(f => f.FaceNormal.DotProduct(-XYZ.BasisZ) > 0.99)
            .OrderBy(f => f.Origin.Z)
            .ToList();

        if (topFaces.Count == 0 || bottomFaces.Count == 0)
            throw new InvalidOperationException("Floor must have horizontal top (+Z) and bottom (-Z) planar faces.");

        return (topFaces[0], bottomFaces[0]);
    }

    /// <summary>
    /// Reads the complete geometry of the floor slab and constructs an immutable <see cref="FoundationGeometrySnapshot"/>.
    /// </summary>
    public static FoundationGeometrySnapshot Read(Element floor)
    {
        var solid = GetSolid(floor);
        var (topFace, bottomFace) = GetTopAndBottomFaces(solid);

        double topZFt = topFace.Origin.Z;
        double bottomZFt = bottomFace.Origin.Z;
        double thicknessFt = topZFt - bottomZFt;

        if (thicknessFt <= Tolerance)
            throw new InvalidOperationException("Floor thickness must be greater than zero.");

        double bottomZMm = RevitUnits.FtToMm(bottomZFt);
        var referenceOrigin = ToMm(bottomFace.Origin);
        var edges = ReadBottomOutline(bottomFace);
        var frame = edges.Count > 0
            ? FoundationPlanFrameCalculator.Compute(edges, referenceOrigin, bottomZMm)
            : FoundationPlanFrameCalculator.Fit(
                Vector3.UnitX, ReadBoundingCorners(solid, bottomZFt), referenceOrigin, bottomZMm);

        return new FoundationGeometrySnapshot(
            frame.Length,
            frame.Width,
            RevitUnits.FtToMm(thicknessFt),
            RevitUnits.FtToMm(topZFt),
            bottomZMm,
            frame.Origin,
            frame.LocalX,
            frame.LocalY,
            Vector3.UnitZ);
    }

    /// <summary>Every edge of the bottom face in millimetres; lines are straight, arcs and splines are not.</summary>
    private static List<FoundationOutlineEdge> ReadBottomOutline(PlanarFace bottomFace)
    {
        var edges = new List<FoundationOutlineEdge>();
        foreach (EdgeArray loop in bottomFace.EdgeLoops)
        {
            foreach (Edge edge in loop)
            {
                var curve = edge.AsCurve();
                edges.Add(new FoundationOutlineEdge(
                    ToMm(curve.GetEndPoint(0)),
                    ToMm(curve.GetEndPoint(1)),
                    IsStraight: curve is Line,
                    RevitUnits.FtToMm(curve.Length)));
            }
        }

        return edges;
    }

    /// <summary>The solid's bounding-box corners on the bottom plane, for a face Revit returns without edges.</summary>
    private static List<Point3> ReadBoundingCorners(Solid solid, double bottomZFt)
    {
        var box = solid.GetBoundingBox();
        return new List<Point3>
        {
            ToMm(new XYZ(box.Min.X, box.Min.Y, bottomZFt)),
            ToMm(new XYZ(box.Max.X, box.Max.Y, bottomZFt)),
            ToMm(new XYZ(box.Min.X, box.Max.Y, bottomZFt)),
            ToMm(new XYZ(box.Max.X, box.Min.Y, bottomZFt))
        };
    }

    private static Point3 ToMm(XYZ point) =>
        new(RevitUnits.FtToMm(point.X), RevitUnits.FtToMm(point.Y), RevitUnits.FtToMm(point.Z));
}
