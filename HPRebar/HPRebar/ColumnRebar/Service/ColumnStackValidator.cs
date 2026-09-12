using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.ColumnRebar.Model;

namespace HPRebar.ColumnRebar.Service;

/// <summary>
///     Checks that a picked run of columns is something the tool can actually reinforce.
///     Rules are tried in order and the first failure is returned, so the user is told about the problem
///     they hit first rather than whichever check happened to run last.
/// </summary>
public static class ColumnStackValidator
{
    private const double Tolerance = 1.0e-9;

    public static ValidationResult Validate(Document document, IReadOnlyList<Element> columns)
    {
        if (columns is null) throw new ArgumentNullException(nameof(columns));

        if (columns.Count == 0) return ValidationResult.Fail(4);

        var rules = new (int Code, Func<bool> Holds)[]
        {
            (1, () => SameFamilyType(columns)),
            (2, () => columns.All(IsVertical)),
            (3, () => columns.All(column => ColumnSolidFaceReader.GetSolids(column).Count == 1)),
            (4, () => IsContinuous(columns)),
            (5, () => columns.All(column => ColumnSolidFaceReader.GetSectionStyle(column) != ColumnSectionStyle.Other)),
            (6, () => IsNotRotated(columns)),
            (7, () => DoesNotGrowUpward(columns)),
            (8, () => SitsWithinTheColumnBelow(columns)),
            (9, () => NoBeamRisesAboveItsColumn(columns, document)),
            (10, () => BeamsAreJoined(columns, document)),
            (11, () => MeetsFoundation(columns[0], document)),
            (12, () => MeetsFloorFoundation(columns[0], document)),
            (13, () => MeetsWallFoundation(columns[0], document)),
            (14, () => MeetsBeamFoundation(columns[0], document))
        };

        foreach (var (code, holds) in rules)
        {
            if (!holds()) return ValidationResult.Fail(code);
        }

        return ValidationResult.Ok;
    }

    private static bool SameFamilyType(IReadOnlyList<Element> columns)
    {
        var first = FamilyTypeName(columns[0]);

        return columns.All(column => FamilyTypeName(column) == first);
    }

    private static string? FamilyTypeName(Element column) =>
        column.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM)?.AsValueString();

    private static bool IsVertical(Element column) =>
        column.get_Parameter(BuiltInParameter.SLANTED_COLUMN_TYPE_PARAM)?.AsValueString() == "Vertical";

    /// <summary>Each column's base must land exactly on the head of the one below it.</summary>
    private static bool IsContinuous(IReadOnlyList<Element> columns)
    {
        for (var i = 1; i < columns.Count; i++)
        {
            var below = ColumnSolidFaceReader.GetTop(columns[i - 1]);
            var above = ColumnSolidFaceReader.GetBottom(columns[i]);

            if (!AreEqual(ColumnSolidFaceReader.DistanceMm(below, above.Origin), 0d)) return false;
        }

        return true;
    }

    /// <summary>Bars can only splice straight through if the sections share the same orientation.</summary>
    private static bool IsNotRotated(IReadOnlyList<Element> columns)
    {
        for (var i = 1; i < columns.Count; i++)
        {
            if (!BothRectangular(columns[i - 1], columns[i])) continue;

            var pairs = new[]
            {
                (ColumnSolidFaceReader.GetSouth(columns[i - 1]), ColumnSolidFaceReader.GetSouth(columns[i])),
                (ColumnSolidFaceReader.GetNorth(columns[i - 1]), ColumnSolidFaceReader.GetNorth(columns[i])),
                (ColumnSolidFaceReader.GetEast(columns[i - 1]), ColumnSolidFaceReader.GetEast(columns[i])),
                (ColumnSolidFaceReader.GetWest(columns[i - 1]), ColumnSolidFaceReader.GetWest(columns[i]))
            };

            foreach (var (below, above) in pairs)
            {
                if (below is null || above is null) return false;
                if (!AreEqual(below.FaceNormal.AngleTo(above.FaceNormal), 0d)) return false;
            }
        }

        return true;
    }

    /// <summary>A column may never be wider than the one carrying it.</summary>
    private static bool DoesNotGrowUpward(IReadOnlyList<Element> columns)
    {
        for (var i = 1; i < columns.Count; i++)
        {
            var below = columns[i - 1];
            var above = columns[i];

            if (BothRectangular(below, above))
            {
                if (Width(below) < Width(above) || Depth(below) < Depth(above)) return false;
            }
            else if (BothCircular(below, above))
            {
                if (ColumnSolidFaceReader.GetDiameterMm(below) < ColumnSolidFaceReader.GetDiameterMm(above)) return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     The upper section has to sit inside the lower one in plan. For a rectangle each upper face is
    ///     inside when its distances to the two opposite lower faces add up to no more than the span between
    ///     them; a circle is inside when the offset plus its own radius stays within the lower radius.
    /// </summary>
    private static bool SitsWithinTheColumnBelow(IReadOnlyList<Element> columns)
    {
        for (var i = 1; i < columns.Count; i++)
        {
            var below = columns[i - 1];
            var above = columns[i];

            if (BothRectangular(below, above))
            {
                var south1 = ColumnSolidFaceReader.GetSouth(below)!;
                var north1 = ColumnSolidFaceReader.GetNorth(below)!;
                var east1 = ColumnSolidFaceReader.GetEast(below)!;
                var west1 = ColumnSolidFaceReader.GetWest(below)!;

                var span = ColumnSolidFaceReader.DistanceMm(south1, north1.Origin);
                var depthSpan = ColumnSolidFaceReader.DistanceMm(east1, west1.Origin);

                var checks = new[]
                {
                    (Straddled(south1, north1, ColumnSolidFaceReader.GetSouth(above)!), span),
                    (Straddled(south1, north1, ColumnSolidFaceReader.GetNorth(above)!), span),
                    (Straddled(east1, west1, ColumnSolidFaceReader.GetEast(above)!), depthSpan),
                    (Straddled(east1, west1, ColumnSolidFaceReader.GetWest(above)!), depthSpan)
                };

                if (checks.Any(check => check.Item1 > check.Item2)) return false;
            }
            else if (BothCircular(below, above))
            {
                var lower = ColumnSolidFaceReader.GetDiameterMm(below);
                var upper = ColumnSolidFaceReader.GetDiameterMm(above);

                var offset = OffsetMm(below, above);

                if (AreEqual(lower, upper))
                {
                    if (!AreEqual(offset, 0d)) return false;
                }
                else if (offset + upper / 2 > lower / 2)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool NoBeamRisesAboveItsColumn(IReadOnlyList<Element> columns, Document document)
    {
        foreach (var column in columns)
        {
            var top = ColumnSolidFaceReader.GetTop(column);

            foreach (var beam in ColumnNeighbourFinder.GetBeamsAtTop(column, document))
            {
                var faces = ColumnSolidFaceReader.GetHorizontalFacesSorted(beam);

                if (faces.Count == 0) continue;
                if (faces[faces.Count - 1].Origin.Z > top.Origin.Z) return false;
            }
        }

        return true;
    }

    private static bool BeamsAreJoined(IReadOnlyList<Element> columns, Document document)
    {
        foreach (var column in columns)
        {
            foreach (var beam in ColumnNeighbourFinder.GetBeamsAtTop(column, document))
            {
                if (!JoinGeometryUtils.AreElementsJoined(document, beam, column)) return false;
                if (JoinGeometryUtils.IsCuttingElementInJoin(document, beam, column)) return false;
            }
        }

        return true;
    }

    private static bool MeetsFoundation(Element bottom, Document document)
    {
        var foundation = ColumnNeighbourFinder.GetFoundationUnder(bottom, document);

        return foundation is null || SitsOn(bottom, foundation, document, useTopFace: true);
    }

    private static bool MeetsFloorFoundation(Element bottom, Document document)
    {
        if (ColumnNeighbourFinder.GetFoundationUnder(bottom, document) is not null) return true;

        var floor = ColumnNeighbourFinder.GetFloorUnder(bottom, document);

        return floor is null || SitsOn(bottom, floor, document, useTopFace: true);
    }

    private static bool MeetsWallFoundation(Element bottom, Document document)
    {
        if (ColumnNeighbourFinder.GetFoundationUnder(bottom, document) is not null) return true;
        if (ColumnNeighbourFinder.GetFloorUnder(bottom, document) is not null) return true;

        var wall = ColumnNeighbourFinder.GetWallUnder(bottom, document);

        return wall is null || SitsOn(bottom, wall, document, useTopFace: true);
    }

    /// <summary>Last resort: the column sits on beams, which are not joined to it, only touched.</summary>
    private static bool MeetsBeamFoundation(Element bottom, Document document)
    {
        if (ColumnNeighbourFinder.GetFoundationUnder(bottom, document) is not null) return true;
        if (ColumnNeighbourFinder.GetFloorUnder(bottom, document) is not null) return true;
        if (ColumnNeighbourFinder.GetWallUnder(bottom, document) is not null) return true;

        var beamFaces = ColumnNeighbourFinder.GetBeamsAtBase(bottom, document)
            .SelectMany(ColumnSolidFaceReader.GetHorizontalFacesSorted)
            .OrderBy(face => face.Origin.Z)
            .ToList();

        if (beamFaces.Count == 0) return true;

        var baseFace = ColumnSolidFaceReader.GetBottom(bottom);

        return AreEqual(ColumnSolidFaceReader.DistanceMm(beamFaces[0], baseFace.Origin), 0d);
    }

    private static bool SitsOn(Element column, Element support, Document document, bool useTopFace)
    {
        if (!JoinGeometryUtils.AreElementsJoined(document, column, support)) return false;
        if (JoinGeometryUtils.IsCuttingElementInJoin(document, column, support)) return false;

        var faces = ColumnSolidFaceReader.GetHorizontalFacesSorted(support);

        if (faces.Count == 0) return true;

        var contact = useTopFace ? faces[faces.Count - 1] : faces[0];
        var baseFace = ColumnSolidFaceReader.GetBottom(column);

        return AreEqual(ColumnSolidFaceReader.DistanceMm(contact, baseFace.Origin), 0d);
    }

    private static double Straddled(PlanarFace first, PlanarFace second, PlanarFace probe) =>
        ColumnSolidFaceReader.DistanceMm(first, probe.Origin) + ColumnSolidFaceReader.DistanceMm(second, probe.Origin);

    private static double Width(Element column) =>
        ColumnSolidFaceReader.DistanceMm(ColumnSolidFaceReader.GetSouth(column)!, ColumnSolidFaceReader.GetNorth(column)!.Origin);

    private static double Depth(Element column) =>
        ColumnSolidFaceReader.DistanceMm(ColumnSolidFaceReader.GetEast(column)!, ColumnSolidFaceReader.GetWest(column)!.Origin);

    private static double OffsetMm(Element below, Element above)
    {
        var first = (below.Location as LocationPoint)?.Point;
        var second = (above.Location as LocationPoint)?.Point;

        return first is null || second is null ? 0d : RevitUnits.FtToMm(first.DistanceTo(second));
    }

    private static bool BothRectangular(Element below, Element above) =>
        ColumnSolidFaceReader.GetSectionStyle(below) == ColumnSectionStyle.Rectangle
        && ColumnSolidFaceReader.GetSectionStyle(above) == ColumnSectionStyle.Rectangle;

    private static bool BothCircular(Element below, Element above) =>
        ColumnSolidFaceReader.GetSectionStyle(below) == ColumnSectionStyle.Circular
        && ColumnSolidFaceReader.GetSectionStyle(above) == ColumnSectionStyle.Circular;

    private static bool AreEqual(double first, double second) =>
        second - Tolerance < first && first < second + Tolerance;
}
