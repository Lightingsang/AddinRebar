using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace HPRebar.RebarPartition.Model;

/// <summary>
/// Settings for rebar partition generation configured by the user.
/// </summary>
public sealed class RebarPartitionSettings
{
    public PartitionScope Scope { get; set; } = PartitionScope.ActiveView;
    public PartitionSourceParameter SourceParameter { get; set; } = PartitionSourceParameter.ElementName;
    public string CustomParameterName { get; set; } = "Element Name";
    public string Prefix { get; set; } = string.Empty;
    public string Suffix { get; set; } = string.Empty;
    public HashSet<BuiltInCategory> EnabledCategories { get; set; } = new();
    public bool OverwriteExisting { get; set; } = true;
    public bool SkipIfTargetEmpty { get; set; } = true;
    public bool AssignNumberElement { get; set; } = true;
    public bool GroupContinuousGeometry { get; set; } = true;
    public string NumberElementParameterName { get; set; } = "Number Element";
}

/// <summary>
/// Execution result of partition assignment.
/// </summary>
public sealed class RebarPartitionResult
{
    public bool Success { get; }
    public int TotalProcessed { get; }
    public int UpdatedCount { get; }
    public int SkippedCount { get; }
    public int ErrorCount { get; }
    public string Message { get; }

    public RebarPartitionResult(
        bool success,
        int totalProcessed,
        int updatedCount,
        int skippedCount,
        int errorCount,
        string message)
    {
        Success = success;
        TotalProcessed = totalProcessed;
        UpdatedCount = updatedCount;
        SkippedCount = skippedCount;
        ErrorCount = errorCount;
        Message = message;
    }

    public static RebarPartitionResult Succeeded(int total, int updated, int skipped) =>
        new(true, total, updated, skipped, 0, $"Cập nhật thành công {updated} thanh thép (Bỏ qua {skipped}/{total}).");

    public static RebarPartitionResult Failed(string message) =>
        new(false, 0, 0, 0, 1, message);
}
