using Autodesk.Revit.DB;
using HPRebar.ColumnRebar.Model;
using HPRebar.Core.ColumnRebar.Models;

namespace HPRebar.ColumnRebar.Service;

/// <summary>
///     Converts a point in the stack's millimetre frame back into Revit world coordinates.
///     The frame's origin is the bottom column's south-west corner dropped onto the datum face, and its
///     axes are that column's own east and north face normals — so the mapping still holds when the whole
///     stack is rotated in plan.
/// </summary>
public sealed class PointMapper
{
    private readonly XYZ _origin;
    private readonly XYZ _east;
    private readonly XYZ _north;

    private PointMapper(XYZ origin, XYZ east, XYZ north)
    {
        _origin = origin;
        _east = east;
        _north = north;
    }

    public static PointMapper For(ColumnStack stack)
    {
        if (stack.Style == ColumnSectionStyle.Rectangle)
        {
            var corner = ColumnSolidFaceReader.ProjectToPlane(stack.WestDatum!.Origin, stack.SouthDatum!);
            var origin = ColumnSolidFaceReader.ProjectToPlane(corner, stack.DatumFace);

            var faces = stack.Faces[0];

            return new PointMapper(origin, faces.East!.FaceNormal, faces.North!.FaceNormal);
        }

        // A circular stack has no faces to align to, so it uses the world axes about its centre point.
        var centre = ColumnSolidFaceReader.ProjectToPlane(stack.PointDatum!, stack.DatumFace);

        return new PointMapper(centre, XYZ.BasisX, XYZ.BasisY);
    }

    /// <summary>Millimetre point in the stack frame to a Revit world point in feet.</summary>
    public XYZ ToXyz(Point3 point) =>
        _origin
        + RevitUnits.MmToFt(point.X) * _east
        + RevitUnits.MmToFt(point.Y) * _north
        + RevitUnits.MmToFt(point.Z) * XYZ.BasisZ;
}
