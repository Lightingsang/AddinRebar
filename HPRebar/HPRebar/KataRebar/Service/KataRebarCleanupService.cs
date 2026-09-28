using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// Enforces idempotency by discovering and removing previously generated Kata rebar instances
/// hosted on the selected beam run using their Partition parameter.
/// </summary>
public static class KataRebarCleanupService
{
    public const string LegacyCommentPrefix = "HPRebar_Kata_";

    /// <summary>
    /// Finds all Rebar instances hosted on any element of the given beam run whose Partition parameter
    /// matches beamName (or legacy "Kata_{beamName}" / legacy comment for backward compatibility).
    /// </summary>
    public static IReadOnlyList<ElementId> FindExistingKataRebars(
        Document doc,
        IReadOnlyList<Element> hostBeams,
        string beamName)
    {
        if (hostBeams == null || hostBeams.Count == 0)
            return Array.Empty<ElementId>();

        var hostIds = new HashSet<ElementId>(hostBeams.Select(b => b.Id));
        string targetPartition = beamName?.Trim() ?? "";
        string legacyPartition = $"Kata_{targetPartition}";
        string legacyComment = $"{LegacyCommentPrefix}{targetPartition}";

        return new FilteredElementCollector(doc)
            .OfClass(typeof(Rebar))
            .WhereElementIsNotElementType()
            .Cast<Rebar>()
            .Where(r =>
            {
                var hostId = r.GetHostId();
                if (!hostIds.Contains(hostId)) return false;

                var partition = r.get_Parameter(BuiltInParameter.NUMBER_PARTITION_PARAM)?.AsString();
                if (!string.IsNullOrEmpty(partition))
                {
                    if (!string.IsNullOrWhiteSpace(targetPartition) &&
                        (partition.Equals(targetPartition, StringComparison.OrdinalIgnoreCase) ||
                         partition.Equals(legacyPartition, StringComparison.OrdinalIgnoreCase)))
                    {
                        return true;
                    }
                }

                // Backward compatibility: match legacy comments if present from previous builds
                var comment = r.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString();
                if (!string.IsNullOrEmpty(comment))
                {
                    if (!string.IsNullOrWhiteSpace(targetPartition) &&
                        comment.Equals(legacyComment, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    if (string.IsNullOrWhiteSpace(targetPartition) &&
                        comment.StartsWith(LegacyCommentPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            })
            .Select(r => r.Id)
            .ToList();
    }

    /// <summary>
    /// Deletes all discovered prior Kata rebar instances from the Revit document.
    /// Must be called within an active Transaction.
    /// </summary>
    public static int DeleteExistingKataRebars(
        Document doc,
        IReadOnlyList<Element> hostBeams,
        string beamName)
    {
        var idsToDelete = FindExistingKataRebars(doc, hostBeams, beamName);
        if (idsToDelete.Count > 0)
        {
            doc.Delete(idsToDelete.ToList());
        }
        return idsToDelete.Count;
    }
}
