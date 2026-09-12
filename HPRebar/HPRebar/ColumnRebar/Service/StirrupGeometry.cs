using System;
using Autodesk.Revit.DB;
using HPRebar.ColumnRebar.Model;

namespace HPRebar.ColumnRebar.Service;

/// <summary>
///     Where a tie sits and how big it is, in Revit world coordinates.
///     <see cref="Origin"/> is the corner Revit hangs the shape off; <see cref="XVector"/> and
///     <see cref="YVector"/> orient the shape's own plane; <see cref="Width"/> and <see cref="Height"/>
///     stretch it to the concrete inside cover.
/// </summary>
internal readonly struct StirrupPlacement
{
    public StirrupPlacement(XYZ origin, XYZ xVector, XYZ yVector, XYZ width, XYZ height)
    {
        Origin = origin;
        XVector = xVector;
        YVector = yVector;
        Width = width;
        Height = height;
    }

    public XYZ Origin { get; }

    public XYZ XVector { get; }

    public XYZ YVector { get; }

    public XYZ Width { get; }

    public XYZ Height { get; }
}

/// <summary>
///     Builds the placement frames the tie creators feed to Revit. Every length arriving here is
///     millimetres; everything leaving is feet.
/// </summary>
internal static class StirrupGeometry
{
    /// <summary>
    ///     Perimeter tie of a rectangular section. <paramref name="insetX"/> and <paramref name="insetY"/>
    ///     pull the tie in from the west and south faces, which is how the narrower cross-ties are placed.
    /// </summary>
    public static StirrupPlacement Rectangle(
        ColumnFaces faces,
        double coverMm,
        double widthMm,
        double depthMm,
        double insetXMm,
        double insetYMm,
        double heightMm)
    {
        var cover = RevitUnits.MmToFt(coverMm);
        var insetX = RevitUnits.MmToFt(insetXMm);
        var insetY = RevitUnits.MmToFt(insetYMm);

        var east = faces.East!.FaceNormal;
        var north = faces.North!.FaceNormal;

        var southWest = ColumnSolidFaceReader.ProjectToPlane(faces.South!.Origin, faces.West!);
        var atCover = southWest + (insetX + cover) * east + (insetY + cover) * north;
        var onBase = ColumnSolidFaceReader.ProjectToPlane(atCover, faces.Bottom);
        var origin = onBase + RevitUnits.MmToFt(heightMm) * XYZ.BasisZ;

        var rotation = Transform.CreateRotation(east, Math.PI / 2);

        var westEdge = ColumnSolidFaceReader.ProjectToPlane(faces.West!.Origin, faces.South!);
        var start = westEdge + (cover + insetX) * east;
        var width = westEdge + (RevitUnits.MmToFt(widthMm) - cover) * east - start;
        var height = start + (RevitUnits.MmToFt(depthMm) - cover) * north - (start + (cover + insetY) * north);

        return new StirrupPlacement(origin, rotation.BasisZ, rotation.BasisX, width, height);
    }

    /// <summary>Perimeter tie of a circular section, sized to the square that just contains it.</summary>
    public static StirrupPlacement Circle(ColumnFaces faces, double diameterMm, double coverMm, double heightMm)
    {
        var centre = ColumnSolidFaceReader.ProjectToPlane(faces.LocationPoint!, faces.Bottom);
        var cover = RevitUnits.MmToFt(coverMm);
        var diameter = RevitUnits.MmToFt(diameterMm);
        var half = diameter * 0.5 - cover;

        var corner = centre - half * XYZ.BasisX - half * XYZ.BasisY;
        var origin = corner + RevitUnits.MmToFt(heightMm) * XYZ.BasisZ;

        var rotation = Transform.CreateRotation(XYZ.BasisX, Math.PI / 2);
        var span = diameter - 2 * cover;

        return new StirrupPlacement(origin, rotation.BasisZ, rotation.BasisX, span * XYZ.BasisX, span * XYZ.BasisY);
    }

    /// <summary>
    ///     A cross-tie spanning south to north at a given distance east of the west face.
    ///     Its width vector is the unit east normal because the shape has no width to stretch — only its
    ///     span across the section matters.
    /// </summary>
    public static StirrupPlacement CrossTieAcrossDepth(
        ColumnFaces faces,
        double coverMm,
        double depthMm,
        double offsetXMm,
        double heightMm)
    {
        var cover = RevitUnits.MmToFt(coverMm);
        var east = faces.East!.FaceNormal;
        var south = faces.South!.FaceNormal;

        var northWest = ColumnSolidFaceReader.ProjectToPlane(faces.West!.Origin, faces.North!);
        var atCover = northWest + cover * south + RevitUnits.MmToFt(offsetXMm) * east;
        var onBase = ColumnSolidFaceReader.ProjectToPlane(atCover, faces.Bottom);
        var origin = onBase + RevitUnits.MmToFt(heightMm) * XYZ.BasisZ;

        var rotation = Transform.CreateRotation(east, Math.PI / 2);
        var span = (RevitUnits.MmToFt(depthMm) - 2 * cover) * south;

        return new StirrupPlacement(origin, rotation.BasisZ, rotation.BasisX, east, span);
    }

    /// <summary>A cross-tie spanning west to east at a given distance north of the south face.</summary>
    public static StirrupPlacement CrossTieAcrossWidth(
        ColumnFaces faces,
        double coverMm,
        double widthMm,
        double offsetYMm,
        double heightMm)
    {
        var cover = RevitUnits.MmToFt(coverMm);
        var east = faces.East!.FaceNormal;
        var north = faces.North!.FaceNormal;

        var southWest = ColumnSolidFaceReader.ProjectToPlane(faces.South!.Origin, faces.West!);
        var atCover = southWest + cover * east + RevitUnits.MmToFt(offsetYMm) * north;
        var onBase = ColumnSolidFaceReader.ProjectToPlane(atCover, faces.Bottom);
        var origin = onBase + RevitUnits.MmToFt(heightMm) * XYZ.BasisZ;

        var rotation = Transform.CreateRotation(east, Math.PI / 2);
        var span = (RevitUnits.MmToFt(widthMm) - 2 * cover) * east;

        return new StirrupPlacement(origin, rotation.BasisZ, rotation.BasisX, north, span);
    }

    /// <summary>
    ///     The square tie laid over a circular section, sized to the circle's diagonal so its corners reach
    ///     the concrete rather than cutting across it.
    /// </summary>
    public static StirrupPlacement DiagonalTieOnCircle(
        ColumnFaces faces,
        double diameterMm,
        double coverMm,
        double tieDiameterMm,
        double heightMm)
    {
        var centre = ColumnSolidFaceReader.ProjectToPlane(faces.LocationPoint!, faces.Bottom);
        var span = RevitUnits.MmToFt(Math.Sqrt(2) * diameterMm * 0.5 - coverMm + tieDiameterMm);

        var corner = centre - span * 0.5 * XYZ.BasisX - span * 0.5 * XYZ.BasisY;
        var origin = corner + RevitUnits.MmToFt(heightMm) * XYZ.BasisZ;

        var rotation = Transform.CreateRotation(XYZ.BasisX, Math.PI / 2);

        return new StirrupPlacement(origin, rotation.BasisZ, rotation.BasisX, span * XYZ.BasisX, span * XYZ.BasisY);
    }

    /// <summary>A cross-tie laid west to east across a circular section, one bar diameter off centre.</summary>
    public static StirrupPlacement CrossTieOnCircleAcrossX(
        ColumnFaces faces,
        double diameterMm,
        double coverMm,
        double tieDiameterMm,
        double heightMm)
    {
        var centre = ColumnSolidFaceReader.ProjectToPlane(faces.LocationPoint!, faces.Bottom);
        var cover = RevitUnits.MmToFt(coverMm);
        var reach = RevitUnits.MmToFt(diameterMm / 2) - cover;

        var start = centre - reach * XYZ.BasisX - RevitUnits.MmToFt(tieDiameterMm) * XYZ.BasisY;
        var origin = start + RevitUnits.MmToFt(heightMm) * XYZ.BasisZ;

        var rotation = Transform.CreateRotation(XYZ.BasisX, Math.PI / 2);
        var span = (RevitUnits.MmToFt(diameterMm) - 2 * cover) * XYZ.BasisX;

        return new StirrupPlacement(origin, rotation.BasisZ, rotation.BasisX, XYZ.BasisY, span);
    }

    /// <summary>A cross-tie laid south to north across a circular section, one bar diameter off centre.</summary>
    public static StirrupPlacement CrossTieOnCircleAcrossY(
        ColumnFaces faces,
        double diameterMm,
        double coverMm,
        double tieDiameterMm,
        double heightMm)
    {
        var centre = ColumnSolidFaceReader.ProjectToPlane(faces.LocationPoint!, faces.Bottom);
        var cover = RevitUnits.MmToFt(coverMm);
        var reach = RevitUnits.MmToFt(diameterMm / 2) - cover;

        var start = centre + reach * XYZ.BasisY - RevitUnits.MmToFt(tieDiameterMm) * XYZ.BasisX;
        var origin = start + RevitUnits.MmToFt(heightMm) * XYZ.BasisZ;

        var rotation = Transform.CreateRotation(XYZ.BasisX, Math.PI / 2);
        var span = -(RevitUnits.MmToFt(diameterMm) - 2 * cover) * XYZ.BasisY;

        return new StirrupPlacement(origin, rotation.BasisZ, rotation.BasisX, XYZ.BasisX, span);
    }
}
