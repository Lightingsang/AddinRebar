using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.BeamRebar.Model;
using Serilog;

namespace HPRebar.BeamRebar.Service;

/// <summary>
/// Extracts clean 3D solids and boundary planar faces from Revit structural framing elements.
/// </summary>
public static class BeamSolidFaceReader
{
    private const double Tolerance = 1.0e-6;

    /// <summary>Retrieves all solids with non-zero volume from the element geometry.</summary>
    public static IReadOnlyList<Solid> GetSolids(Element element)
    {
        if (element is null) throw new ArgumentNullException(nameof(element));

        var options = new Options { ComputeReferences = true, DetailLevel = ViewDetailLevel.Fine };
        var geometry = element.get_Geometry(options);
        var solids = new List<Solid>();

        if (geometry is null) return solids;

        foreach (var obj in geometry)
        {
            switch (obj)
            {
                case Solid solid when solid.Volume > Tolerance:
                    solids.Add(solid);
                    break;
                case GeometryInstance instance:
                    foreach (var instObj in instance.GetInstanceGeometry())
                    {
                        if (instObj is Solid instSolid && instSolid.Volume > Tolerance)
                        {
                            solids.Add(instSolid);
                        }
                    }
                    break;
            }
        }

        return solids;
    }

    /// <summary>Gets the single solid of an element or null if not built from exactly one solid.</summary>
    public static Solid? GetSingleSolid(Element element)
    {
        var solids = GetSolids(element);
        return solids.Count == 1 ? solids[0] : null;
    }

    /// <summary>Requires that the element contains exactly one solid with real volume.</summary>
    public static Solid RequireSingleSolid(Element element) =>
        GetSingleSolid(element)
        ?? throw new InvalidOperationException($"Beam element {element.Id} must contain exactly one solid with positive volume.");

    /// <summary>Extracts planar faces whose normal points vertically (+Z or -Z).</summary>
    public static IReadOnlyList<PlanarFace> GetHorizontalFaces(Element element)
    {
        var solid = RequireSingleSolid(element);
        return solid.Faces.OfType<PlanarFace>()
            .Where(f => Math.Abs(Math.Abs(f.FaceNormal.Z) - 1.0) < Tolerance)
            .OrderBy(f => f.Origin.Z)
            .ToList();
    }

    /// <summary>Gets the lowest horizontal planar face (bottom soffit).</summary>
    public static PlanarFace GetBottom(Element element)
    {
        var faces = GetHorizontalFaces(element);
        if (faces.Count == 0) throw new InvalidOperationException($"Beam {element.Id} has no horizontal bottom face.");
        return faces[0];
    }

    /// <summary>Gets the highest horizontal planar face (top surface).</summary>
    public static PlanarFace GetTop(Element element)
    {
        var faces = GetHorizontalFaces(element);
        if (faces.Count == 0) throw new InvalidOperationException($"Beam {element.Id} has no horizontal top face.");
        return faces[faces.Count - 1];
    }

    /// <summary>Extracts vertical planar faces whose normal is orthogonal to the Z axis.</summary>
    public static IReadOnlyList<PlanarFace> GetVerticalFaces(Element element)
    {
        var solid = RequireSingleSolid(element);
        return solid.Faces.OfType<PlanarFace>()
            .Where(f => Math.Abs(f.FaceNormal.Z) < Tolerance)
            .ToList();
    }

    /// <summary>Gets the left lateral vertical face pointing along +Y_beam.</summary>
    public static PlanarFace? GetLeftFace(Element element, XYZ transverseAxis) =>
        GetVerticalFaces(element).FirstOrDefault(f => f.FaceNormal.DotProduct(transverseAxis) > 0.8);

    /// <summary>Gets the right lateral vertical face pointing along -Y_beam.</summary>
    public static PlanarFace? GetRightFace(Element element, XYZ transverseAxis) =>
        GetVerticalFaces(element).FirstOrDefault(f => f.FaceNormal.DotProduct(-transverseAxis) > 0.8);

    /// <summary>Gets the start end-cut face pointing along -X_beam.</summary>
    public static PlanarFace? GetStartFace(Element element, XYZ beamAxis) =>
        GetVerticalFaces(element).FirstOrDefault(f => f.FaceNormal.DotProduct(-beamAxis) > 0.8);

    /// <summary>Gets the end end-cut face pointing along +X_beam.</summary>
    public static PlanarFace? GetEndFace(Element element, XYZ beamAxis) =>
        GetVerticalFaces(element).FirstOrDefault(f => f.FaceNormal.DotProduct(beamAxis) > 0.8);

    /// <summary>
    /// Evaluates if the beam has a standard rectangular cross-section.
    /// Checks for 2 horizontal faces (top/bottom) and 4 vertical faces (left/right/start/end).
    /// </summary>
    public static BeamSectionStyle GetSectionStyle(Element element, XYZ beamAxis, XYZ transverseAxis)
    {
        var solid = GetSingleSolid(element);
        if (solid is null) return BeamSectionStyle.Other;

        var horizontal = GetHorizontalFaces(element);
        var vertical = GetVerticalFaces(element);

        if (horizontal.Count < 2 || vertical.Count < 4)
        {
            Log.Warning("Beam {Id}: Not rectangular. Horizontal={HCount}, Vertical={VCount}",
                element.Id, horizontal.Count, vertical.Count);
            return BeamSectionStyle.Other;
        }

        var left = GetLeftFace(element, transverseAxis);
        var right = GetRightFace(element, transverseAxis);
        var start = GetStartFace(element, beamAxis);
        var end = GetEndFace(element, beamAxis);

        if (left is null || right is null || start is null || end is null)
        {
            Log.Warning("Beam {Id}: Could not identify all 4 vertical faces along beam axis.", element.Id);
            return BeamSectionStyle.Other;
        }

        // Validate parallelism of opposed faces
        bool opposedSides = Math.Abs(left.FaceNormal.AngleTo(right.FaceNormal) - Math.PI) < 0.05;
        bool opposedEnds = Math.Abs(start.FaceNormal.AngleTo(end.FaceNormal) - Math.PI) < 0.05;
        bool orthogonal = Math.Abs(left.FaceNormal.AngleTo(beamAxis) - (Math.PI / 2.0)) < 0.05;

        if (!opposedSides || !opposedEnds || !orthogonal)
        {
            Log.Warning("Beam {Id}: Faces are not strictly orthogonal and rectangular.", element.Id);
            return BeamSectionStyle.Other;
        }

        return BeamSectionStyle.Rectangle;
    }

    /// <summary>Calculates signed-free distance from a point to a plane in millimetres.</summary>
    public static double DistanceMm(PlanarFace plane, XYZ point) =>
        RevitUnits.FtToMm(Math.Abs((point - plane.Origin).DotProduct(plane.FaceNormal)));

    /// <summary>Measures beam cross-section width b in millimetres between lateral faces.</summary>
    public static double GetWidthMm(Element element, XYZ transverseAxis)
    {
        var left = GetLeftFace(element, transverseAxis);
        var right = GetRightFace(element, transverseAxis);
        if (left is null || right is null) return 0.0;
        return DistanceMm(left, right.Origin);
    }

    /// <summary>Measures beam total depth h in millimetres between top and bottom faces.</summary>
    public static double GetHeightMm(Element element)
    {
        var top = GetTop(element);
        var bottom = GetBottom(element);
        return DistanceMm(top, bottom.Origin);
    }
}
