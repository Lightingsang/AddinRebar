using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.Core.FoundationRebar.Models;

namespace HPRebar.FoundationRebar.Service;

/// <summary>
/// Extracts clean 3D solid geometry, horizontal planar faces, and oriented bounding frame
/// from a Revit Floor element into a pure <see cref="FoundationGeometrySnapshot"/>.
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

        double topZMm = RevitUnits.FtToMm(topZFt);
        double bottomZMm = RevitUnits.FtToMm(bottomZFt);
        double thicknessMm = RevitUnits.FtToMm(thicknessFt);

        // Determine primary orientation vector from dominant boundary edge of bottom face
        XYZ dominantDir = XYZ.BasisX;
        double maxEdgeLen = 0.0;

        foreach (EdgeArray loop in bottomFace.EdgeLoops)
        {
            foreach (Edge edge in loop)
            {
                var curve = edge.AsCurve();
                if (curve is Line line)
                {
                    double len = line.Length;
                    if (len > maxEdgeLen)
                    {
                        XYZ dir = (line.GetEndPoint(1) - line.GetEndPoint(0)).Normalize();
                        XYZ horizDir = new XYZ(dir.X, dir.Y, 0.0);
                        if (horizDir.GetLength() > 0.01)
                        {
                            maxEdgeLen = len;
                            dominantDir = horizDir.Normalize();
                        }
                    }
                }
            }
        }

        // Canonical orientation: ensure dominantDir points into positive half-plane
        if (dominantDir.X < -1e-6 || (Math.Abs(dominantDir.X) <= 1e-6 && dominantDir.Y < 0))
        {
            dominantDir = -dominantDir;
        }

        XYZ ux = dominantDir;
        XYZ uz = XYZ.BasisZ;
        XYZ uy = uz.CrossProduct(ux).Normalize();

        // Project boundary points to find local 2D oriented bounds [minU, maxU] x [minV, maxV]
        var samplePoints = new List<XYZ>();
        foreach (EdgeArray loop in bottomFace.EdgeLoops)
        {
            foreach (Edge edge in loop)
            {
                samplePoints.Add(edge.AsCurve().GetEndPoint(0));
                samplePoints.Add(edge.AsCurve().GetEndPoint(1));
            }
        }

        if (samplePoints.Count == 0)
        {
            var bbox = solid.GetBoundingBox();
            samplePoints.Add(new XYZ(bbox.Min.X, bbox.Min.Y, bottomZFt));
            samplePoints.Add(new XYZ(bbox.Max.X, bbox.Max.Y, bottomZFt));
            samplePoints.Add(new XYZ(bbox.Min.X, bbox.Max.Y, bottomZFt));
            samplePoints.Add(new XYZ(bbox.Max.X, bbox.Min.Y, bottomZFt));
        }

        XYZ refOrigin = bottomFace.Origin;
        double minU = double.MaxValue, maxU = double.MinValue;
        double minV = double.MaxValue, maxV = double.MinValue;

        foreach (var pt in samplePoints)
        {
            XYZ d = pt - refOrigin;
            double u = d.DotProduct(ux);
            double v = d.DotProduct(uy);
            if (u < minU) minU = u;
            if (u > maxU) maxU = u;
            if (v < minV) minV = v;
            if (v > maxV) maxV = v;
        }

        double lengthFt = maxU - minU;
        double widthFt = maxV - minV;

        double lengthMm = RevitUnits.FtToMm(lengthFt);
        double widthMm = RevitUnits.FtToMm(widthFt);

        // Origin in world coordinates (bottom face corner where u = minU, v = minV, z = bottomZFt)
        XYZ originFt = refOrigin + (ux * minU) + (uy * minV);
        originFt = new XYZ(originFt.X, originFt.Y, bottomZFt);

        var originCore = new Point3(
            RevitUnits.FtToMm(originFt.X),
            RevitUnits.FtToMm(originFt.Y),
            RevitUnits.FtToMm(originFt.Z));

        var uxCore = new Vector3(ux.X, ux.Y, ux.Z).Normalize();
        var uyCore = new Vector3(uy.X, uy.Y, uy.Z).Normalize();
        var uzCore = new Vector3(uz.X, uz.Y, uz.Z).Normalize();

        return new FoundationGeometrySnapshot(
            lengthMm,
            widthMm,
            thicknessMm,
            topZMm,
            bottomZMm,
            originCore,
            uxCore,
            uyCore,
            uzCore);
    }
}
