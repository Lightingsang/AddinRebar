using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HPRebar.RebarPartition.Model;

/// <summary>
/// Status state of a rebar preview row.
/// </summary>
public enum ItemStatus
{
    ReadyToUpdate,
    AlreadyMatching,
    MissingHost,
    MissingParameter
}

/// <summary>
/// Represents a row in the Preview DataGrid for rebar partition assignment.
/// </summary>
public sealed partial class RebarPartitionPreviewItem : ObservableObject
{
    public ElementId RebarId { get; }
    public ElementId? HostId { get; }

#if REVIT2024_OR_GREATER
    public string RebarIdText => RebarId.Value.ToString();
    public string HostIdText => HostId?.Value.ToString() ?? "-";
#else
    public string RebarIdText => RebarId.IntegerValue.ToString();
    public string HostIdText => HostId?.IntegerValue.ToString() ?? "-";
#endif

    public string HostCategory { get; }
    public string HostName { get; }
    public string SourceValue { get; }
    public string CurrentPartition { get; }
    public string TargetPartition { get; }
    public int HostCount { get; }
    public string HostCountText => HostCount > 0 ? HostCount.ToString() : "-";
    public ItemStatus Status { get; }

    [ObservableProperty]
    private bool _isIncluded;

    public string StatusText => Status switch
    {
        ItemStatus.ReadyToUpdate => "Cần cập nhật",
        ItemStatus.AlreadyMatching => "Đã trùng khớp",
        ItemStatus.MissingHost => "Không có Host",
        ItemStatus.MissingParameter => "Không thấy tham số",
        _ => "Không xác định"
    };

    public bool CanApply => Status == ItemStatus.ReadyToUpdate && !string.IsNullOrWhiteSpace(TargetPartition);

    public RebarPartitionPreviewItem(
        ElementId rebarId,
        ElementId? hostId,
        string hostCategory,
        string hostName,
        string sourceValue,
        string currentPartition,
        string targetPartition,
        ItemStatus status,
        int hostCount = 1)
    {
        RebarId = rebarId;
        HostId = hostId;
        HostCategory = hostCategory;
        HostName = hostName;
        SourceValue = sourceValue;
        CurrentPartition = currentPartition;
        TargetPartition = targetPartition;
        HostCount = hostCount;
        Status = status;
        _isIncluded = status == ItemStatus.ReadyToUpdate;
    }
}
