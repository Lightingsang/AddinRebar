using Autodesk.Revit.DB;
using HPRebar.ColumnRebar.Model;
using HPRebar.Core.ColumnRebar.Models;

namespace HPRebar.ColumnRebar.Service;

/// <summary>
///     The two in-plan directions a drawing of one column segment should be laid out along: the column's
///     own east and north face normals. Sections and the annotation written into them follow the column
///     rather than the project axes, so a column rotated in plan gets a drawing that lines up with it.
///
///     A column square to the project gives back the world axes, which is what the tool always used, so
///     the drawings it already produces do not move.
/// </summary>
internal static class ColumnPlanAxes
{
    /// <summary>
    ///     East and north of <paramref name="faces"/>. A circular section has no orientation to follow and
    ///     no side faces to read it from, so it keeps the world axes.
    /// </summary>
    public static (XYZ East, XYZ North) Of(ColumnFaces faces, ColumnSection section)
    {
        if (section.Shape != SectionShape.Rectangle || faces.East is null || faces.North is null)
        {
            return (XYZ.BasisX, XYZ.BasisY);
        }

        return (faces.East.FaceNormal, faces.North.FaceNormal);
    }
}
