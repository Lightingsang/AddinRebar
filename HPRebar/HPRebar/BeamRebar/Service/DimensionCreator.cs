using System;
using Autodesk.Revit.DB;
using HPRebar.BeamRebar.Model;
using HPRebar.Core.BeamRebar.Models;
using Serilog;

namespace HPRebar.BeamRebar.Service;

/// <summary>
/// Places parametric dimension lines on continuous beam elevation and cross-section views.
/// Converts 3D PlanarFace SURFACE references to view-compatible LINEAR references.
/// </summary>
public static class DimensionCreator
{
    /// <summary>
    /// Converts a 3D planar face reference (SURFACE) to an edge reference (LINEAR)
    /// required by Revit NewDimension in ViewSection.
    /// </summary>
    internal static Reference ToLinearReference(Document document, PlanarFace face)
    {
        if (face.Reference is null) throw new InvalidOperationException("Face does not contain a valid Revit geometry reference.");
        var surface = face.Reference.ConvertToStableRepresentation(document);
        var linear = surface.Replace("SURFACE", "LINEAR");
        return Reference.ParseFromStableRepresentation(document, linear);
    }

    public static int CreateOnElevation(
        Document document,
        ViewSection view,
        BeamStack stack,
        BeamAnnotationSettings settings)
    {
        if (settings.DimensionType is null || stack.SupportFaces.Count < 2) return 0;

        int created = 0;
        try
        {
            // 1. Span Dimension Chain
            var spanRefs = new ReferenceArray();
            foreach (var face in stack.SupportFaces)
            {
                if (face.Reference is not null)
                {
                    spanRefs.Append(ToLinearReference(document, face));
                }
            }

            if (spanRefs.Size >= 2)
            {
                var line = ElevationSpanLine(view, stack, settings);
                document.Create.NewDimension(view, line, spanRefs, settings.DimensionType);
                created++;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not create span dimensions on elevation view {View}; skipping.", view.Name);
        }

        try
        {
            // 2. Height / Level Dimension
            var heightRefs = new ReferenceArray();
            if (stack.TopDatum?.Reference is not null && stack.BottomDatum?.Reference is not null)
            {
                heightRefs.Append(ToLinearReference(document, stack.TopDatum));
                heightRefs.Append(ToLinearReference(document, stack.BottomDatum));

                if (heightRefs.Size == 2)
                {
                    var line = ElevationHeightLine(view, stack, settings);
                    document.Create.NewDimension(view, line, heightRefs, settings.DimensionType);
                    created++;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not create height dimension on elevation view {View}; skipping.", view.Name);
        }

        return created;
    }

    public static int CreateOnSection(
        Document document,
        ViewSection view,
        BeamFaces faces,
        BeamSpan span,
        BeamAnnotationSettings settings)
    {
        if (settings.DimensionType is null) return 0;
        int created = 0;

        // 1. Width Dimension B
        try
        {
            if (faces.Left?.Reference is not null && faces.Right?.Reference is not null)
            {
                var refs = new ReferenceArray();
                refs.Append(ToLinearReference(document, faces.Left!));
                refs.Append(ToLinearReference(document, faces.Right!));

                var line = SectionWidthLine(view, span, settings);
                document.Create.NewDimension(view, line, refs, settings.DimensionType);
                created++;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not create width dimension on section view {View}; skipping.", view.Name);
        }

        // 2. Height Dimension H
        try
        {
            if (faces.Top?.Reference is not null && faces.Bottom?.Reference is not null)
            {
                var refs = new ReferenceArray();
                refs.Append(ToLinearReference(document, faces.Top!));
                refs.Append(ToLinearReference(document, faces.Bottom!));

                var line = SectionHeightLine(view, span, settings);
                document.Create.NewDimension(view, line, refs, settings.DimensionType);
                created++;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not create height dimension on section view {View}; skipping.", view.Name);
        }

        return created;
    }

    private static Line ElevationSpanLine(ViewSection view, BeamStack stack, BeamAnnotationSettings settings)
    {
        double offsetFt = RevitUnits.MmToFt(settings.DimensionOffsetV);
        XYZ start = stack.OriginPoint - offsetFt * XYZ.BasisZ;
        XYZ end = stack.OriginPoint
                  + RevitUnits.MmToFt(stack.ContinuousStack.TotalLength) * stack.BeamDirection
                  - offsetFt * XYZ.BasisZ;
        return Line.CreateBound(start, end);
    }

    private static Line ElevationHeightLine(ViewSection view, BeamStack stack, BeamAnnotationSettings settings)
    {
        double offsetFt = RevitUnits.MmToFt(settings.DimensionOffsetH);
        XYZ start = stack.OriginPoint - offsetFt * stack.BeamDirection;
        XYZ end = start + RevitUnits.MmToFt(stack.MaxHeightMm) * XYZ.BasisZ;
        return Line.CreateBound(start, end);
    }

    private static Line SectionWidthLine(ViewSection view, BeamSpan span, BeamAnnotationSettings settings)
    {
        double offsetFt = RevitUnits.MmToFt(settings.DimensionOffsetV);
        XYZ center = view.Origin;
        XYZ left = center - (RevitUnits.MmToFt(span.Width) * 0.5) * view.RightDirection + offsetFt * view.UpDirection;
        XYZ right = center + (RevitUnits.MmToFt(span.Width) * 0.5) * view.RightDirection + offsetFt * view.UpDirection;
        return Line.CreateBound(left, right);
    }

    private static Line SectionHeightLine(ViewSection view, BeamSpan span, BeamAnnotationSettings settings)
    {
        double offsetFt = RevitUnits.MmToFt(settings.DimensionOffsetH);
        XYZ center = view.Origin;
        XYZ bottom = center - (RevitUnits.MmToFt(span.Width) * 0.5 + offsetFt) * view.RightDirection - (RevitUnits.MmToFt(span.Height) * 0.5) * view.UpDirection;
        XYZ top = bottom + RevitUnits.MmToFt(span.Height) * view.UpDirection;
        return Line.CreateBound(bottom, top);
    }

    /// <summary>Upper bound of the dimensions a run draws: span chain + height on the elevation, width + height per section.</summary>
    public static int PlannedCount(bool onElevation, int sectionCount) => (onElevation ? 2 : 0) + sectionCount * 2;
}
