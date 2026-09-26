using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.KataExport.Model;
using RevitView = Autodesk.Revit.DB.View;

namespace HPRebar.KataExport.Service;

/// <summary>
/// Elements around a beam run that exist in the view's phase and belong to the model as built: demolished or
/// future elements and secondary design options would otherwise add supports that are not there.
/// </summary>
public static class KataCandidateCollector
{
    public static IEnumerable<Element> Near(Document doc, RevitView view, KataRunGeometry run, double marginFt, double belowFt, double aboveFt, params BuiltInCategory[] categories)
    {
        var boxes = run.Pieces.Select(p => p.Element.get_BoundingBox(null)).Where(b => b is not null).Select(b => b!).ToList();
        if (boxes.Count == 0) return Enumerable.Empty<Element>();

        var outline = new Outline(
            new XYZ(boxes.Min(b => b.Min.X) - marginFt, boxes.Min(b => b.Min.Y) - marginFt, boxes.Min(b => b.Min.Z) - belowFt),
            new XYZ(boxes.Max(b => b.Max.X) + marginFt, boxes.Max(b => b.Max.Y) + marginFt, boxes.Max(b => b.Max.Z) + aboveFt));

        var phaseId = view.get_Parameter(BuiltInParameter.VIEW_PHASE)?.AsElementId();

        return new FilteredElementCollector(doc)
            .WhereElementIsNotElementType()
            .WherePasses(new ElementMulticategoryFilter(categories))
            .WherePasses(new BoundingBoxIntersectsFilter(outline))
            .Where(e => IsBuilt(e, phaseId));
    }

    private static bool IsBuilt(Element element, ElementId? phaseId)
    {
        if (element.DesignOption is { IsPrimary: false }) return false;
        if (phaseId is null || phaseId == ElementId.InvalidElementId) return true;

        var status = element.GetPhaseStatus(phaseId);
        return status != ElementOnPhaseStatus.Demolished
               && status != ElementOnPhaseStatus.Future
               && status != ElementOnPhaseStatus.Past;
    }
}
