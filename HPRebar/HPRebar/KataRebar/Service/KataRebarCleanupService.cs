using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// Makes a re-run replace the previous one: deletes the bars hosted on the picked beams whose comment is
/// exactly the Kata Rebar tag of their host. Bars drawn by hand or by another tool are never deleted.
/// </summary>
public static class KataRebarCleanupService
{
    public static IReadOnlyList<ElementId> FindPrevious(Document doc, IReadOnlyList<Element> hosts)
    {
        var tags = hosts.ToDictionary(h => h.Id, h => h.UniqueId);

        return new FilteredElementCollector(doc)
            .OfClass(typeof(Rebar))
            .WhereElementIsNotElementType()
            .Cast<Rebar>()
            .Where(r => tags.TryGetValue(r.GetHostId(), out var hostUniqueId)
                        && KataRebarTag.IsTagFor(r.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString(), hostUniqueId))
            .Select(r => r.Id)
            .ToList();
    }

    /// <summary>Deletes the previous bars; call inside an open transaction.</summary>
    public static int DeletePrevious(Document doc, IReadOnlyList<Element> hosts)
    {
        var ids = FindPrevious(doc, hosts);
        if (ids.Count > 0) doc.Delete(ids.ToList());
        return ids.Count;
    }
}
