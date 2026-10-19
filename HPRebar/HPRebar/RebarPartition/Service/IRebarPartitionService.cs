using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HPRebar.RebarPartition.Model;

namespace HPRebar.RebarPartition.Service;

/// <summary>
/// Service interface for analyzing rebars and applying partition parameters.
/// </summary>
public interface IRebarPartitionService
{
    /// <summary>
    /// Analyzes the model according to the provided settings and generates a preview list.
    /// </summary>
    IReadOnlyList<RebarPartitionPreviewItem> BuildPreview(
        Document doc,
        UIDocument? uidoc,
        RebarPartitionSettings settings);

    /// <summary>
    /// Commits the partition assignment to the selected rebar elements inside a Revit Transaction.
    /// </summary>
    RebarPartitionResult ApplyPartitions(
        Document doc,
        IReadOnlyList<RebarPartitionPreviewItem> itemsToApply,
        RebarPartitionSettings? settings = null);
}
