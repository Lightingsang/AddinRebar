using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace HPRebar.ColumnRebar.Service;

/// <summary>
///     Finds the elements around a column that decide where its bars start and stop: the beams framing into
///     its head, and whatever it stands on.
/// </summary>
public static class ColumnNeighbourFinder
{
    /// <summary>Beams whose reference level matches the column's top level and which have horizontal faces.</summary>
    public static IReadOnlyList<Element> GetBeamsAtTop(Element column, Document document) =>
        GetBeamsAtLevel(column, document, BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);

    /// <summary>Beams sitting at the column's base level — the fallback datum when there is no foundation.</summary>
    public static IReadOnlyList<Element> GetBeamsAtBase(Element column, Document document) =>
        GetBeamsAtLevel(column, document, BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);

    /// <summary>Structural foundation under the column, or null when it is not on the lowest level.</summary>
    public static Element? GetFoundationUnder(Element column, Document document) =>
        FindSupport(
            column,
            document,
            BuiltInCategory.OST_StructuralFoundation,
            (element, levelId) => element.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM)?.AsElementId() == levelId,
            requireExactlyTwoFaces: false);

    /// <summary>Structural floor acting as the foundation, or null.</summary>
    public static Element? GetFloorUnder(Element column, Document document) =>
        FindSupport(
            column,
            document,
            BuiltInCategory.OST_Floors,
            (element, levelId) =>
                element.get_Parameter(BuiltInParameter.LEVEL_PARAM)?.AsElementId() == levelId
                && element.get_Parameter(BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL)?.AsInteger() == 1,
            requireExactlyTwoFaces: true);

    /// <summary>Structural wall acting as the foundation, or null.</summary>
    public static Element? GetWallUnder(Element column, Document document) =>
        FindSupport(
            column,
            document,
            BuiltInCategory.OST_Walls,
            (element, levelId) =>
                element.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE)?.AsElementId() == levelId
                && element.get_Parameter(BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT)?.AsInteger() == 1,
            requireExactlyTwoFaces: true);

    /// <summary>Lowest level in the document by elevation.</summary>
    public static Level? GetLowestLevel(Document document) =>
        new FilteredElementCollector(document)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .OrderBy(level => level.Elevation)
            .FirstOrDefault();

    private static IReadOnlyList<Element> GetBeamsAtLevel(Element column, Document document, BuiltInParameter levelParameter)
    {
        var levelId = column.get_Parameter(levelParameter)?.AsElementId();

        if (levelId is null) return new List<Element>();

        return CollectIntersecting(column, document, BuiltInCategory.OST_StructuralFraming)
            .Where(beam => beam.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM)?.AsElementId() == levelId)
            .Where(beam => ColumnSolidFaceReader.GetHorizontalFacesSorted(beam).Count != 0)
            .GroupBy(beam => beam.Id)
            .Select(group => group.First())
            .ToList();
    }

    /// <summary>
    ///     A support only counts when the column starts on the lowest level of the project, is intersected by
    ///     the candidate, and that candidate is a single solid with usable horizontal faces.
    /// </summary>
    private static Element? FindSupport(
        Element column,
        Document document,
        BuiltInCategory category,
        System.Func<Element, ElementId, bool> matchesLevel,
        bool requireExactlyTwoFaces)
    {
        var lowest = GetLowestLevel(document);

        if (lowest is null) return null;

        var baseLevelId = column.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM)?.AsElementId();

        // Compare the column's base level against the lowest level's id, not the Level object itself.
        if (baseLevelId is null || baseLevelId != lowest.Id) return null;

        var candidate = CollectIntersecting(column, document, category)
            .FirstOrDefault(element => matchesLevel(element, baseLevelId));

        if (candidate is null) return null;
        if (ColumnSolidFaceReader.GetSingleSolid(candidate) is null) return null;

        var faces = ColumnSolidFaceReader.GetHorizontalFacesSorted(candidate);

        if (requireExactlyTwoFaces) return faces.Count == 2 ? candidate : null;

        return faces.Count == 0 ? null : candidate;
    }

    private static IEnumerable<Element> CollectIntersecting(Element column, Document document, BuiltInCategory category)
    {
        var box = column.get_BoundingBox(null);

        if (box is null) return Enumerable.Empty<Element>();

        var filter = new LogicalAndFilter(
            new BoundingBoxIntersectsFilter(new Outline(box.Min, box.Max)),
            new ElementCategoryFilter(category));

        return new FilteredElementCollector(document)
            .WherePasses(filter)
            .WhereElementIsNotElementType();
    }
}
