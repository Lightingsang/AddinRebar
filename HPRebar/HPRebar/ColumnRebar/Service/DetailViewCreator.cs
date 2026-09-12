using System;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.ColumnRebar.Model;
using HPRebar.Core.ColumnRebar.Models;
using Serilog;
// The feature's own View namespace shadows Autodesk.Revit.DB.View inside HPRebar.ColumnRebar,
// so the Revit type is aliased wherever it is named directly.
using RevitView = Autodesk.Revit.DB.View;

namespace HPRebar.ColumnRebar.Service;

/// <summary>
///     Creates the two elevation views of the stack: one looking along the width, one along the depth.
///     Both are cut through the middle of the stack and sized to take the whole height.
/// </summary>
public static class DetailViewCreator
{
    /// <summary>Name of the view family type the tool makes its elevations with.</summary>
    private const string ViewTypeName = "@ColumnDetail";

    public static (ViewSection? X, ViewSection? Y) Create(
        Document document,
        ColumnStack stack,
        AnnotationSettings settings)
    {
        var viewType = ResolveViewType(document, ViewTypeName);

        if (viewType is null)
        {
            Log.Warning("The document has no section view family type; the elevation views were skipped");

            return (null, null);
        }

        var (nameX, nameY) = settings.DetailViewNames();
        var top = stack.Sections[stack.Sections.Count - 1].TopPosition;
        var centre = StackCentre(stack, top);

        var x = Create(document, viewType, settings, centre, AcrossDirection(stack, acrossWidth: true),
            PlanHalfWidth(stack, acrossWidth: true), top, nameX);

        var y = Create(document, viewType, settings, centre, AcrossDirection(stack, acrossWidth: false),
            PlanHalfWidth(stack, acrossWidth: false), top, nameY);

        return (x, y);
    }

    /// <summary>
    ///     Finds the named view family type, creating it from any section type the first time the tool runs
    ///     in a document.
    /// </summary>
    public static ViewFamilyType? ResolveViewType(Document document, string name)
    {
        var sectionTypes = new FilteredElementCollector(document)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .Where(type => type.ViewFamily == ViewFamily.Section)
            .ToList();

        var existing = sectionTypes.FirstOrDefault(type => type.Name == name);

        if (existing is not null) return existing;

        var template = sectionTypes.FirstOrDefault();

        return template?.Duplicate(name) as ViewFamilyType;
    }

    private static ViewSection Create(
        Document document,
        ViewFamilyType viewType,
        AnnotationSettings settings,
        XYZ centre,
        XYZ across,
        double halfWidthMm,
        double heightMm,
        string name)
    {
        var margin = RevitUnits.MmToFt(settings.ViewMargin);
        var halfWidth = RevitUnits.MmToFt(halfWidthMm);
        var height = RevitUnits.MmToFt(heightMm);

        var transform = Transform.Identity;
        transform.Origin = centre;
        transform.BasisX = across;
        transform.BasisY = XYZ.BasisZ;
        transform.BasisZ = across.CrossProduct(XYZ.BasisZ);

        var box = new BoundingBoxXYZ
        {
            Transform = transform,
            Min = new XYZ(-halfWidth - margin, -height * 0.5 - margin, 0),
            Max = new XYZ(halfWidth + margin, height * 0.5 + margin, margin)
        };

        var view = ViewSection.CreateSection(document, viewType.Id, box);

        Rename(view, name);

        view.get_Parameter(BuiltInParameter.VIEWER_CROP_REGION_VISIBLE)?.Set(0);

        if (settings.DetailTemplate is not null) view.ViewTemplateId = settings.DetailTemplate.Id;

        return view;
    }

    /// <summary>
    ///     Names the view, falling back to a suffixed name when the document already has one by that name.
    ///     A clashing name is not worth failing the whole run over.
    /// </summary>
    internal static void Rename(RevitView view, string name)
    {
        try
        {
            view.Name = name;
        }
        catch (Exception)
        {
            var fallback = name + "A";

            Log.Warning("A view named {Name} already exists; the new view was named {Fallback}", name, fallback);

            try
            {
                view.Name = fallback;
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Could not name the view {Fallback} either; Revit's default name is kept", fallback);
            }
        }
    }

    /// <summary>Mid-height point of the stack, on the axis of its topmost segment.</summary>
    private static XYZ StackCentre(ColumnStack stack, double topMm)
    {
        var topFaces = stack.Faces[stack.Faces.Count - 1];

        XYZ plan;

        if (stack.Style == ColumnSectionStyle.Rectangle)
        {
            var section = stack.Sections[stack.Sections.Count - 1];
            var westward = ColumnSolidFaceReader.ProjectToPlane(topFaces.West!.Origin, topFaces.South!);

            plan = westward
                   + RevitUnits.MmToFt(section.B) * 0.5 * topFaces.East!.FaceNormal
                   + RevitUnits.MmToFt(section.H) * 0.5 * topFaces.North!.FaceNormal;
        }
        else
        {
            plan = topFaces.LocationPoint!;
        }

        var onDatum = ColumnSolidFaceReader.ProjectToPlane(plan, stack.DatumFace);

        return onDatum + RevitUnits.MmToFt(topMm) * 0.5 * XYZ.BasisZ;
    }

    private static XYZ AcrossDirection(ColumnStack stack, bool acrossWidth)
    {
        if (stack.Style != ColumnSectionStyle.Rectangle) return acrossWidth ? XYZ.BasisX : XYZ.BasisY;

        var faces = stack.Faces[0];

        return acrossWidth ? faces.East!.FaceNormal : faces.North!.FaceNormal;
    }

    private static double PlanHalfWidth(ColumnStack stack, bool acrossWidth)
    {
        var first = stack.Sections[0];

        if (first.Shape != SectionShape.Rectangle) return first.D * 0.5;

        return (acrossWidth ? first.B : first.H) * 0.5;
    }
}
