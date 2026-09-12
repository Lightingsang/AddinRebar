using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Serilog;

namespace HPRebar.ColumnRebar.Service;

/// <summary>
///     Pulls the faces of an element's solid out of Revit geometry. Faces are identified by the direction
///     they point in, not by name, so the readers work whatever family the column comes from.
/// </summary>
public static class ColumnSolidFaceReader
{
    private const double AngleTolerance = 1.0e-9;
    private static readonly XYZ South = -XYZ.BasisY;
    private static readonly XYZ West = -XYZ.BasisX;

    /// <summary>Every solid with real volume, including those inside a geometry instance.</summary>
    public static IReadOnlyList<Solid> GetSolids(Element element)
    {
        if (element is null) throw new ArgumentNullException(nameof(element));

        var options = new Options { ComputeReferences = true };
        var geometry = element.get_Geometry(options);
        var solids = new List<Solid>();

        if (geometry is null) return solids;

        foreach (var geometryObject in geometry)
        {
            switch (geometryObject)
            {
                case Solid solid when solid.Volume != 0:
                    solids.Add(solid);
                    break;

                case GeometryInstance instance:
                    foreach (var nested in instance.GetInstanceGeometry())
                    {
                        if (nested is Solid nestedSolid && nestedSolid.Volume != 0)
                        {
                            solids.Add(nestedSolid);
                        }
                    }

                    break;
            }
        }

        return solids;
    }

    /// <summary>The element's single solid, or null when it is built from none or several.</summary>
    public static Solid? GetSingleSolid(Element element)
    {
        var solids = GetSolids(element);

        return solids.Count == 1 ? solids[0] : null;
    }

    /// <summary>Solid the readers work on. Callers validate the single-solid rule before getting here.</summary>
    private static Solid RequireSingleSolid(Element element) =>
        GetSingleSolid(element)
        ?? throw new InvalidOperationException($"Element {element.Id} must be built from exactly one solid.");

    /// <summary>Faces whose normal points straight up or straight down.</summary>
    public static IReadOnlyList<PlanarFace> GetHorizontalFaces(Element element) =>
        HorizontalFacesOf(RequireSingleSolid(element));

    /// <summary>Faces whose normal is perpendicular to Z, i.e. the sides.</summary>
    public static IReadOnlyList<PlanarFace> GetVerticalFaces(Element element) =>
        RequireSingleSolid(element).Faces
            .OfType<PlanarFace>()
            .Where(face => IsAngle(face.FaceNormal.AngleTo(XYZ.BasisZ), Math.PI / 2))
            .ToList();

    public static IReadOnlyList<CylindricalFace> GetCylindricalFaces(Element element) =>
        RequireSingleSolid(element).Faces.OfType<CylindricalFace>().ToList();

    /// <summary>Lowest horizontal face.</summary>
    public static PlanarFace GetBottom(Element element) => SortedHorizontalFaces(element)[0];

    /// <summary>Highest horizontal face. A column is expected to have exactly two.</summary>
    public static PlanarFace GetTop(Element element)
    {
        var faces = SortedHorizontalFaces(element);

        return faces[faces.Count - 1];
    }

    public static PlanarFace? GetSouth(Element element) => SideFacing(element, South, West);

    public static PlanarFace? GetNorth(Element element) => SideFacing(element, XYZ.BasisY, XYZ.BasisX);

    public static PlanarFace? GetWest(Element element) => SideFacing(element, West, XYZ.BasisY);

    public static PlanarFace? GetEast(Element element) => SideFacing(element, XYZ.BasisX, South);

    /// <summary>
    ///     Horizontal faces of a neighbouring element (beam, floor, wall, foundation), lowest first.
    ///     Sorting matters: callers read the soffit off the first entry and the top off the last.
    /// </summary>
    public static IReadOnlyList<PlanarFace> GetHorizontalFacesSorted(Element element)
    {
        var solid = GetSingleSolid(element);

        if (solid is null) return new List<PlanarFace>();

        return HorizontalFacesOf(solid).OrderBy(face => face.Origin.Z).ToList();
    }

    /// <summary>Signed-free distance from a point to a plane, in millimetres.</summary>
    public static double DistanceMm(PlanarFace plane, XYZ point) =>
        RevitUnits.FtToMm(Math.Abs((point - plane.Origin).DotProduct(plane.FaceNormal)));

    /// <summary>Drops a point onto a plane along the plane normal.</summary>
    public static XYZ ProjectToPlane(XYZ point, PlanarFace plane)
    {
        var toOrigin = plane.Origin - point;
        var along = toOrigin.DotProduct(plane.FaceNormal);

        return Math.Abs(along) < 1e-9 ? point : point + plane.FaceNormal * along;
    }

    /// <summary>Diameter of a circular column, in millimetres.</summary>
    public static double GetDiameterMm(Element element)
    {
        var cylindricals = GetCylindricalFaces(element);

        if (cylindricals.Count == 0) return 0d;

        return cylindricals[0].GetSurface() is CylindricalSurface surface
            ? 2 * RevitUnits.FtToMm(Math.Abs(surface.Radius))
            : 0d;
    }

    /// <summary>
    ///     Which of the three shapes the tool recognises this column is: a box of four sides, a cylinder,
    ///     or something it cannot reinforce.
    /// </summary>
    public static Model.ColumnSectionStyle GetSectionStyle(Element element)
    {
        var horizontal = GetHorizontalFaces(element);
        var vertical = GetVerticalFaces(element);
        var cylindrical = GetCylindricalFaces(element);
        var total = GetSingleSolid(element)?.Faces.Size ?? 0;

        if (horizontal.Count == 2 && vertical.Count == 4 && cylindrical.Count == 0)
        {
            var south = GetSouth(element);
            var north = GetNorth(element);
            var east = GetEast(element);
            var west = GetWest(element);

            if (south is null || north is null || east is null || west is null)
            {
                // Four vertical faces, but they do not point the four ways the tool expects. A column
                // rotated off the project axes lands here.
                Log.Warning(
                    "Column {Id}: 4 vertical faces but only {Found} of south/north/east/west could be identified, "
                    + "so it is not usable as a rectangle. Face normals: {Normals}",
                    element.Id,
                    new[] { south, north, east, west }.Count(face => face is not null),
                    string.Join(" | ", vertical.Select(Describe)));

                return Model.ColumnSectionStyle.Other;
            }

            var opposed = IsAngle(south.FaceNormal.AngleTo(north.FaceNormal), Math.PI)
                          && IsAngle(east.FaceNormal.AngleTo(west.FaceNormal), Math.PI);

            var square = IsAngle(south.FaceNormal.AngleTo(east.FaceNormal), Math.PI / 2);

            if (opposed && square) return Model.ColumnSectionStyle.Rectangle;

            Log.Warning(
                "Column {Id}: the four side faces are not a rectangle. "
                + "south-north {SouthNorth:0.#########} rad (want pi), east-west {EastWest:0.#########} rad (want pi), "
                + "south-east {SouthEast:0.#########} rad (want pi/2)",
                element.Id,
                south.FaceNormal.AngleTo(north.FaceNormal),
                east.FaceNormal.AngleTo(west.FaceNormal),
                south.FaceNormal.AngleTo(east.FaceNormal));

            return Model.ColumnSectionStyle.Other;
        }

        if (horizontal.Count == 2 && vertical.Count == 0 && cylindrical.Count == 2)
        {
            return Model.ColumnSectionStyle.Circular;
        }

        // The face tally is what usually explains a rejection: joining a column to a beam or foundation
        // cuts its solid and adds faces, and so does any chamfer or notch in the family.
        Log.Warning(
            "Column {Id} is neither rectangular nor circular: {Total} face(s) total — "
            + "{Horizontal} horizontal (want 2), {Vertical} vertical (want 4 for a rectangle, 0 for a circle), "
            + "{Cylindrical} cylindrical (want 0 for a rectangle, 2 for a circle)",
            element.Id, total, horizontal.Count, vertical.Count, cylindrical.Count);

        return Model.ColumnSectionStyle.Other;
    }

    /// <summary>Face normal in a form that reads in a log line.</summary>
    private static string Describe(PlanarFace face) =>
        $"({face.FaceNormal.X:0.###}, {face.FaceNormal.Y:0.###}, {face.FaceNormal.Z:0.###})";

    private static IReadOnlyList<PlanarFace> SortedHorizontalFaces(Element element) =>
        HorizontalFacesOf(RequireSingleSolid(element)).OrderBy(face => face.Origin.Z).ToList();

    private static List<PlanarFace> HorizontalFacesOf(Solid solid) =>
        solid.Faces
            .OfType<PlanarFace>()
            .Where(face => IsAngle(face.FaceNormal.AngleTo(XYZ.BasisZ), 0)
                           || IsAngle(face.FaceNormal.AngleTo(XYZ.BasisZ), Math.PI))
            .ToList();

    /// <summary>
    ///     The side face pointing mostly along <paramref name="towards"/>. The second direction rules out the
    ///     neighbouring face on a square column, where both are within the same quarter turn of the first axis.
    /// </summary>
    private static PlanarFace? SideFacing(Element element, XYZ towards, XYZ notTowards)
    {
        PlanarFace? match = null;

        foreach (var face in GetVerticalFaces(element))
        {
            var alignment = face.FaceNormal.AngleTo(towards);

            if (alignment >= 0
                && alignment < Math.PI / 4
                && face.FaceNormal.AngleTo(notTowards) > Math.PI / 4)
            {
                match = face;
            }
        }

        return match;
    }

    private static bool IsAngle(double actual, double expected) =>
        actual > expected - AngleTolerance && actual < expected + AngleTolerance;
}
