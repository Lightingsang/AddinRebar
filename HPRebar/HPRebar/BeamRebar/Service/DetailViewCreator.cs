using System;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.BeamRebar.Model;
using Serilog;
// The feature's own View namespace shadows Autodesk.Revit.DB.View inside HPRebar.BeamRebar,
// so the Revit type is aliased wherever it is named directly.
using RevitView = Autodesk.Revit.DB.View;

namespace HPRebar.BeamRebar.Service;

/// <summary>
/// Creates longitudinal elevation detail section views capturing the continuous beam run.
/// </summary>
public static class DetailViewCreator
{
    private const string ViewTypeName = "@BeamDetail";

    public static ViewFamilyType? ResolveViewType(Document document, string name)
    {
        var sectionTypes = new FilteredElementCollector(document)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .Where(type => type.ViewFamily == ViewFamily.Detail || type.ViewFamily == ViewFamily.Section)
            .ToList();

        // 1. Exact name match
        var existing = sectionTypes.FirstOrDefault(type => type.Name == name);
        if (existing is not null) return existing;

        // 2. Prefer Detail family template, fallback to Section
        var template = sectionTypes.FirstOrDefault(type => type.ViewFamily == ViewFamily.Detail)
                       ?? sectionTypes.FirstOrDefault(type => type.ViewFamily == ViewFamily.Section);

        return template;
    }

    public static ViewSection? Create(
        Document document,
        BeamStack stack,
        BeamAnnotationSettings settings)
    {
        var viewType = ResolveViewType(document, ViewTypeName);
        if (viewType is null)
        {
            Log.Warning("The document has no detail or section view family type; elevation view was skipped.");
            return null;
        }

        var totalLengthFt = RevitUnits.MmToFt(stack.ContinuousStack.TotalLength);
        var maxHeightFt = RevitUnits.MmToFt(stack.ContinuousStack.MaxHeight);
        var maxWidthFt = RevitUnits.MmToFt(stack.MaxWidthMm);
        var marginFt = RevitUnits.MmToFt(settings.ViewMargin);

        XYZ axisDir = stack.BeamDirection;
        XYZ sideDir = stack.TransverseDirection;

        // Centerpoint of continuous beam assembly
        XYZ centerPoint = stack.OriginPoint 
            + (totalLengthFt * 0.5) * axisDir 
            + (stack.TopElevationFt - maxHeightFt * 0.5) * XYZ.BasisZ;

        var transform = Transform.Identity;
        transform.Origin = centerPoint;
        transform.BasisX = axisDir;
        transform.BasisY = XYZ.BasisZ;
        transform.BasisZ = sideDir;

        var box = new BoundingBoxXYZ
        {
            Transform = transform,
            Min = new XYZ(-totalLengthFt * 0.5 - marginFt, -maxHeightFt * 0.5 - marginFt, -maxWidthFt * 0.5 - marginFt),
            Max = new XYZ(totalLengthFt * 0.5 + marginFt, maxHeightFt * 0.5 + marginFt, maxWidthFt * 0.5 + marginFt)
        };

        ViewSection view;
        if (viewType.ViewFamily == ViewFamily.Detail)
        {
            view = ViewSection.CreateDetail(document, viewType.Id, box);
        }
        else
        {
            view = ViewSection.CreateSection(document, viewType.Id, box);
        }

        Rename(view, settings.DetailViewName);

        // Hide crop boundary box
        view.get_Parameter(BuiltInParameter.VIEWER_CROP_REGION_VISIBLE)?.Set(0);

        ApplyTemplate(view, settings.DetailTemplate);

        ApplyScale(document, view, settings.ElevationScale);

        return view;
    }

    internal static void Rename(RevitView view, string name)
    {
        try
        {
            view.Name = name;
        }
        catch (Exception)
        {
            var fallback = name + "A";
            Log.Warning("A view named {Name} already exists; new view renamed to {Fallback}", name, fallback);
            try
            {
                view.Name = fallback;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not name view {Fallback}; retaining default Revit name {Default}", fallback, view.Name);
            }
        }
    }

    /// <summary>Assigns the template when Revit accepts it for this view; otherwise the view keeps its own settings.</summary>
    internal static void ApplyTemplate(ViewSection view, RevitView? template)
    {
        if (template is null)
        {
            return;
        }

        if (!view.IsValidViewTemplate(template.Id))
        {
            Log.Warning("View template {Template} does not apply to view {View}; template not assigned", template.Name, view.Name);
            return;
        }

        view.ViewTemplateId = template.Id;
    }

    /// <summary>
    /// Sets the scale typed on the Views tab, unless the view template controls the scale: the template
    /// is the office standard, so it wins and the run only logs that the tab's value was not used.
    /// </summary>
    private static void ApplyScale(Document document, ViewSection view, int scale)
    {
        if (scale <= 0)
        {
            return;
        }

        if (document.GetElement(view.ViewTemplateId) is RevitView template
            && !template.GetNonControlledTemplateParameterIds().Contains(new ElementId(BuiltInParameter.VIEW_SCALE)))
        {
            Log.Information("View template {Template} controls the scale; elevation scale 1:{Scale} not applied", template.Name, scale);
            return;
        }

        view.Scale = scale;
    }
}
