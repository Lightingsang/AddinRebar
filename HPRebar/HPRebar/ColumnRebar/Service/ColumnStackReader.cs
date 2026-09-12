using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.ColumnRebar.Model;
using HPRebar.Core.ColumnRebar.Models;

namespace HPRebar.ColumnRebar.Service;

/// <summary>
///     Turns picked Revit columns into a <see cref="ColumnStack"/>. This is the only place that converts
///     Revit's internal feet to millimetres — everything downstream works in millimetres.
/// </summary>
public static class ColumnStackReader
{
    /// <summary>Reads a validated, bottom-to-top ordered run of columns.</summary>
    public static ColumnStack Read(Document document, IReadOnlyList<Element> columns)
    {
        var style = ColumnSolidFaceReader.GetSectionStyle(columns[0]);
        var datum = FindDatumFace(columns[0], document) ?? ColumnSolidFaceReader.GetBottom(columns[0]);

        var southDatum = style == ColumnSectionStyle.Rectangle ? ColumnSolidFaceReader.GetSouth(columns[0]) : null;
        var westDatum = style == ColumnSectionStyle.Rectangle ? ColumnSolidFaceReader.GetWest(columns[0]) : null;
        var pointDatum = style == ColumnSectionStyle.Circular ? LocationOf(columns[0]) : null;

        var faces = new List<ColumnFaces>(columns.Count);
        var sections = new List<ColumnSection>(columns.Count);

        for (var i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            var columnFaces = ReadFaces(document, column, style);

            faces.Add(columnFaces);
            sections.Add(ReadSection(document, i, style, columnFaces, datum, southDatum, westDatum, pointDatum));
        }

        return new ColumnStack
        {
            Style = style,
            Sections = sections,
            Faces = faces,
            DatumFace = datum,
            SouthDatum = southDatum,
            WestDatum = westDatum,
            PointDatum = pointDatum,
            DimensionFaces = ReadDimensionFaces(document, columns, datum)
        };
    }

    /// <summary>Bottom face of a column — the key the picked elements are sorted on.</summary>
    public static PlanarFace BottomFace(Element column) => ColumnSolidFaceReader.GetBottom(column);

    private static ColumnFaces ReadFaces(Document document, Element column, ColumnSectionStyle style)
    {
        var rectangular = style == ColumnSectionStyle.Rectangle;

        return new ColumnFaces
        {
            Element = column,
            Top = ColumnSolidFaceReader.GetTop(column),
            Bottom = ColumnSolidFaceReader.GetBottom(column),
            South = rectangular ? ColumnSolidFaceReader.GetSouth(column) : null,
            North = rectangular ? ColumnSolidFaceReader.GetNorth(column) : null,
            West = rectangular ? ColumnSolidFaceReader.GetWest(column) : null,
            East = rectangular ? ColumnSolidFaceReader.GetEast(column) : null,
            Cylindricals = rectangular
                ? new List<CylindricalFace>()
                : ColumnSolidFaceReader.GetCylindricalFaces(column),
            LocationPoint = rectangular ? null : LocationOf(column),
            TopLevel = LevelOf(document, column, BuiltInParameter.FAMILY_TOP_LEVEL_PARAM),
            BottomLevel = LevelOf(document, column, BuiltInParameter.FAMILY_BASE_LEVEL_PARAM),
            BeamsAtTop = ColumnNeighbourFinder.GetBeamsAtTop(column, document)
        };
    }

    private static ColumnSection ReadSection(
        Document document,
        int index,
        ColumnSectionStyle style,
        ColumnFaces faces,
        PlanarFace datum,
        PlanarFace? southDatum,
        PlanarFace? westDatum,
        XYZ? pointDatum)
    {
        var (hb, zb) = ReadBeamDepths(faces);

        var section = new ColumnSection
        {
            Index = index,
            Shape = style == ColumnSectionStyle.Rectangle ? SectionShape.Rectangle : SectionShape.Circular,
            Hc = ColumnSolidFaceReader.DistanceMm(faces.Top, faces.Bottom.Origin),
            Hb = hb,
            Zb = zb,
            TopPosition = ColumnSolidFaceReader.DistanceMm(datum, faces.Top.Origin),
            BottomPosition = ColumnSolidFaceReader.DistanceMm(datum, faces.Bottom.Origin)
        };

        if (style == ColumnSectionStyle.Rectangle)
        {
            return section with
            {
                B = ColumnSolidFaceReader.DistanceMm(faces.West!, faces.East!.Origin),
                H = ColumnSolidFaceReader.DistanceMm(faces.South!, faces.North!.Origin),
                WestPosition = ColumnSolidFaceReader.DistanceMm(westDatum!, faces.West!.Origin),
                EastPosition = ColumnSolidFaceReader.DistanceMm(westDatum!, faces.East!.Origin),
                SouthPosition = ColumnSolidFaceReader.DistanceMm(southDatum!, faces.South!.Origin),
                NorthPosition = ColumnSolidFaceReader.DistanceMm(southDatum!, faces.North!.Origin)
            };
        }

        var location = faces.LocationPoint!;

        return section with
        {
            D = ColumnSolidFaceReader.GetDiameterMm(faces.Element),
            CenterX = RevitUnits.FtToMm(location.X - pointDatum!.X),
            CenterY = RevitUnits.FtToMm(location.Y - pointDatum.Y)
        };
    }

    /// <summary>
    ///     Depth of the beam zone at the column head and how far its soffit sits below the column top.
    ///     Both are zero when no beam frames in, and the bar builder then falls back to a plan dimension.
    /// </summary>
    private static (double Hb, double Zb) ReadBeamDepths(ColumnFaces faces)
    {
        if (faces.BeamsAtTop.Count == 0) return (0d, 0d);

        var beamFaces = faces.BeamsAtTop
            .SelectMany(ColumnSolidFaceReader.GetHorizontalFacesSorted)
            .OrderBy(face => face.Origin.Z)
            .ToList();

        if (beamFaces.Count == 0) return (0d, 0d);

        var soffit = beamFaces[0];
        var depth = beamFaces.Max(face => ColumnSolidFaceReader.DistanceMm(soffit, face.Origin));

        return (depth, ColumnSolidFaceReader.DistanceMm(faces.Top, soffit.Origin));
    }

    /// <summary>
    ///     Faces the dimension pass hangs off, bottom to top: the datum, each column base and head, and the
    ///     soffit and top of every beam zone.
    /// </summary>
    private static IReadOnlyList<PlanarFace> ReadDimensionFaces(
        Document document,
        IReadOnlyList<Element> columns,
        PlanarFace datum)
    {
        var faces = new List<PlanarFace> { datum, ColumnSolidFaceReader.GetBottom(columns[0]) };

        foreach (var column in columns)
        {
            faces.Add(ColumnSolidFaceReader.GetTop(column));

            var beamFaces = ColumnNeighbourFinder.GetBeamsAtTop(column, document)
                .SelectMany(ColumnSolidFaceReader.GetHorizontalFacesSorted)
                .OrderBy(face => face.Origin.Z)
                .ToList();

            if (beamFaces.Count == 0) continue;

            faces.Add(beamFaces[0]);
            faces.Add(beamFaces[beamFaces.Count - 1]);
        }

        return faces;
    }

    /// <summary>
    ///     Top face of whatever the bottom column stands on, searched in the order a structure is normally
    ///     built: foundation, then structural floor, then structural wall, then the beams at its base.
    ///     Null when the column rests on nothing the tool recognises.
    /// </summary>
    public static PlanarFace? FindDatumFace(Element bottomColumn, Document document)
    {
        var foundation = ColumnNeighbourFinder.GetFoundationUnder(bottomColumn, document);

        if (foundation is not null) return LowestHorizontalFace(foundation);

        var floor = ColumnNeighbourFinder.GetFloorUnder(bottomColumn, document);

        if (floor is not null) return LowestHorizontalFace(floor);

        var wall = ColumnNeighbourFinder.GetWallUnder(bottomColumn, document);

        if (wall is not null) return LowestHorizontalFace(wall);

        var beamFaces = ColumnNeighbourFinder.GetBeamsAtBase(bottomColumn, document)
            .SelectMany(ColumnSolidFaceReader.GetHorizontalFacesSorted)
            .OrderBy(face => face.Origin.Z)
            .ToList();

        return beamFaces.Count == 0 ? null : beamFaces[0];
    }

    private static PlanarFace? LowestHorizontalFace(Element element)
    {
        var faces = ColumnSolidFaceReader.GetHorizontalFacesSorted(element);

        return faces.Count == 0 ? null : faces[0];
    }

    private static XYZ? LocationOf(Element element) => (element.Location as LocationPoint)?.Point;

    private static Level? LevelOf(Document document, Element element, BuiltInParameter parameter)
    {
        var id = element.get_Parameter(parameter)?.AsElementId();

        return id is null ? null : document.GetElement(id) as Level;
    }
}
