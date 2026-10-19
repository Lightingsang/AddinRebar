namespace HPRebar.RebarPartition.Model;

/// <summary>
/// Specifies the scope of rebars to collect for partition assignment.
/// </summary>
public enum PartitionScope
{
    /// <summary>Process only currently selected rebars or hosts in Revit.</summary>
    ActiveSelection,

    /// <summary>Process all rebars visible in the active Revit view.</summary>
    ActiveView,

    /// <summary>Process all rebars in the entire Revit project.</summary>
    EntireModel
}
