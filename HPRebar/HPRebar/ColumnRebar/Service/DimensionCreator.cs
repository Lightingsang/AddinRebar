using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using HPRebar.ColumnRebar.Model;
using HPRebar.Core.ColumnRebar.Models;
using Serilog;

namespace HPRebar.ColumnRebar.Service;

/// <summary>
///     Adds the dimensions to the views: the run of levels down the elevations, and the two plan
///     dimensions on each cross-section.
///
///     Every call is guarded. A dimension that Revit refuses is logged and skipped — a missing dimension is
///     a far better outcome than losing the reinforcement that was just built.
/// </summary>
public static class DimensionCreator
{
    /// <summary>Levels and beam faces stacked up the elevation, dimensioned as one chain.</summary>
    public static int CreateOnElevation(
        Document document,
        ViewSection view,
        ColumnStack stack,
        AnnotationSettings settings,
        bool acrossWidth)
    {
        if (settings.DimensionType is null || stack.DimensionFaces.Count < 2) return 0;

        try
        {
            var line = ElevationLine(view, stack, settings, acrossWidth);
            var references = new ReferenceArray();

            foreach (var face in stack.DimensionFaces)
            {
                if (face.Reference is not null) references.Append(face.Reference);
            }

            if (references.Size < 2) return 0;

            document.Create.NewDimension(view, line, references, settings.DimensionType);

            return 1;
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Could not dimension the elevation view {View}; it was skipped", view.Name);

            return 0;
        }
    }

    /// <summary>Width and depth of one segment, dimensioned on its cross-section view.</summary>
    public static int CreateOnSection(
        Document document,
        ViewSection view,
        ColumnStack stack,
        int index,
        AnnotationSettings settings)
    {
        if (settings.DimensionType is null) return 0;
        if (stack.Sections[index].Shape != SectionShape.Rectangle) return 0;

        var faces = stack.Faces[index];
        var created = 0;

        created += CreateSpan(document, view, settings, faces.South!, faces.North!, faces.West!, settings.DimensionOffsetV);
        created += CreateSpan(document, view, settings, faces.West!, faces.East!, faces.North!, settings.DimensionOffsetH);

        return created;
    }

    /// <summary>
    ///     One dimension between two opposite faces, its witness line offset clear of a third face.
    /// </summary>
    private static int CreateSpan(
        Document document,
        ViewSection view,
        AnnotationSettings settings,
        PlanarFace first,
        PlanarFace second,
        PlanarFace offsetFrom,
        double offsetMm)
    {
        try
        {
            var offset = RevitUnits.MmToFt(offsetMm);

            var start = ColumnSolidFaceReader.ProjectToPlane(view.Origin, first);
            var end = ColumnSolidFaceReader.ProjectToPlane(view.Origin, second);

            start = ColumnSolidFaceReader.ProjectToPlane(start, offsetFrom) + offset * offsetFrom.FaceNormal;
            end = ColumnSolidFaceReader.ProjectToPlane(end, offsetFrom) + offset * offsetFrom.FaceNormal;

            if (start.DistanceTo(end) < 1e-6) return 0;

            var references = new ReferenceArray();
            references.Append(ToLinearReference(document, first));
            references.Append(ToLinearReference(document, second));

            document.Create.NewDimension(view, Line.CreateBound(start, end), references, settings.DimensionType);

            return 1;
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Could not dimension the section view {View}; it was skipped", view.Name);

            return 0;
        }
    }

    /// <summary>
    ///     Revit will only dimension to a LINEAR reference inside a section view, but a face hands back a
    ///     SURFACE one. Rewriting that token in the stable representation is the only way to get a usable
    ///     reference without running a ReferenceIntersector over the geometry.
    ///
    ///     This is undocumented and may stop working in a future Revit release, so every caller must catch
    ///     and skip the dimension rather than let the failure escape.
    /// </summary>
    private static Reference ToLinearReference(Document document, PlanarFace face)
    {
        var surface = face.Reference.ConvertToStableRepresentation(document);
        var linear = surface.Replace("SURFACE", "LINEAR");

        return Reference.ParseFromStableRepresentation(document, linear);
    }

    /// <summary>Where the elevation dimension chain runs: down the side of the view, clear of the column.</summary>
    private static Line ElevationLine(
        ViewSection view,
        ColumnStack stack,
        AnnotationSettings settings,
        bool acrossWidth)
    {
        var first = stack.Sections[0];

        var half = first.Shape == SectionShape.Rectangle
            ? (acrossWidth ? first.B : first.H) * 0.5
            : first.D * 0.5;

        var standOff = RevitUnits.MmToFt(half + settings.DimensionOffsetH);

        var lowest = stack.DimensionFaces[0];
        var highest = stack.DimensionFaces[stack.DimensionFaces.Count - 1];

        var bottom = ColumnSolidFaceReader.ProjectToPlane(view.Origin, lowest) - standOff * view.RightDirection;
        var top = ColumnSolidFaceReader.ProjectToPlane(view.Origin, highest) - standOff * view.RightDirection;

        return Line.CreateBound(bottom, top);
    }

    /// <summary>How many dimensions a run will attempt, for the progress bar.</summary>
    public static int PlannedCount(ColumnStack stack) => 2 + stack.Sections.Count * 2;

    /// <summary>Faces the elevation chain will use, exposed so callers can check there is enough to dimension.</summary>
    public static IReadOnlyList<PlanarFace> ElevationFaces(ColumnStack stack) => stack.DimensionFaces;
}
