using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using HPRebar.ColumnRebar.Model;
using HPRebar.Core.ColumnRebar;
using HPRebar.Core.ColumnRebar.Models;
using Serilog;

namespace HPRebar.ColumnRebar.Service;

/// <summary>
///     Creates one cross-section view per column segment, cut where the ties are at their widest spacing —
///     the middle of an evenly tied column, or the middle of the sparse zone otherwise. That is the section
///     a drawing needs to show.
/// </summary>
public static class SectionViewCreator
{
    private const string ViewTypeName = "@ColumnSection";

    public static IReadOnlyList<ViewSection> Create(
        Document document,
        ColumnStack stack,
        IReadOnlyList<ColumnRebarSpec> specs,
        AnnotationSettings settings,
        ViewNaming naming)
    {
        var views = new List<ViewSection>();
        var viewType = DetailViewCreator.ResolveViewType(document, ViewTypeName);

        if (viewType is null)
        {
            Log.Warning("The document has no section view family type; the cross-section views were skipped");

            return views;
        }

        for (var i = 0; i < stack.Sections.Count; i++)
        {
            var section = stack.Sections[i];
            var height = CutHeight(section, specs[i].Stirrups);

            views.Add(Create(document, viewType, settings, stack, i, height, naming.SectionName(i + 1)));
        }

        return views;
    }

    /// <summary>
    ///     Where up the segment to cut, measured from its base: mid-height for an evenly tied column, and
    ///     the middle of the sparse zone for the zoned layouts.
    /// </summary>
    public static double CutHeight(ColumnSection section, StirrupSpec spec)
    {
        var run = StirrupDistributionCalculator.ComputeRunLength(section, spec.IsTiesUp);

        if (spec.TypeDis == 0) return run * 0.5;

        var (dense, sparse) = StirrupDistributionCalculator.ComputeZones(run, spec.TypeDis);

        return dense + sparse * 0.5;
    }

    private static ViewSection Create(
        Document document,
        ViewFamilyType viewType,
        AnnotationSettings settings,
        ColumnStack stack,
        int index,
        double heightMm,
        string name)
    {
        var section = stack.Sections[index];
        var faces = stack.Faces[index];
        var margin = RevitUnits.MmToFt(settings.ViewMargin);

        var width = RevitUnits.MmToFt(section.Shape == SectionShape.Rectangle ? section.B : section.D);
        var depth = RevitUnits.MmToFt(section.Shape == SectionShape.Rectangle ? section.H : section.D);

        var centre = SectionCentre(faces, section) + RevitUnits.MmToFt(heightMm) * XYZ.BasisZ;
        var (east, north) = ColumnPlanAxes.Of(faces, section);

        // Looking down rather than up, which is what a plan-style section wants. The in-plane axes are the
        // column's own, so the crop below — sized from B and H, which are measured along those axes — boxes
        // the section rather than cutting its corners off when the column is rotated in plan. On a column
        // square to the project this is the rotation of pi about X that it has always been.
        var transform = Transform.Identity;
        transform.Origin = centre;
        transform.BasisX = east;
        transform.BasisY = -north;
        transform.BasisZ = east.CrossProduct(-north);

        var box = new BoundingBoxXYZ
        {
            Transform = transform,

            // Extra room on the right for the bar table the tag pass writes there.
            Min = new XYZ(-width / 2 - margin / 2, -depth / 2 - margin / 2, 0),
            Max = new XYZ(width / 2 + 2.5 * margin, depth / 2 + margin / 2, margin / 2)
        };

        var view = ViewSection.CreateSection(document, viewType.Id, box);

        view.get_Parameter(BuiltInParameter.VIEWER_CROP_REGION_VISIBLE)?.Set(0);

        if (settings.SectionTemplate is not null) view.ViewTemplateId = settings.SectionTemplate.Id;

        DetailViewCreator.Rename(view, name);

        return view;
    }

    /// <summary>Centre of the segment in plan, dropped onto its own base face.</summary>
    private static XYZ SectionCentre(ColumnFaces faces, ColumnSection section)
    {
        if (section.Shape != SectionShape.Rectangle)
        {
            return ColumnSolidFaceReader.ProjectToPlane(faces.LocationPoint!, faces.Bottom);
        }

        var southWest = ColumnSolidFaceReader.ProjectToPlane(faces.West!.Origin, faces.South!);

        var centre = southWest
                     + RevitUnits.MmToFt(section.B) * 0.5 * faces.East!.FaceNormal
                     + RevitUnits.MmToFt(section.H) * 0.5 * faces.North!.FaceNormal;

        return ColumnSolidFaceReader.ProjectToPlane(centre, faces.Bottom);
    }
}
