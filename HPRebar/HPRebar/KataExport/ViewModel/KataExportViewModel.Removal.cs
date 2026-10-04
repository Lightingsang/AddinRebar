using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using Serilog;

namespace HPRebar.KataExport.ViewModel;

/// <summary>
/// Bar groups struck off Kata's elevation on the canvas: the plan shown and generated is the planned one without
/// them, numbered again. The removals belong to one preview: a new read of the sheet starts from the full plan again,
/// and the sheet itself is never changed.
/// </summary>
public sealed partial class KataExportViewModel
{
    private readonly List<IReadOnlyList<string>> _removals = new();
    private IReadOnlyList<string> _removalMessages = Array.Empty<string>();

    /// <summary>The plan as the sheet gives it, before any group was removed.</summary>
    private KataRebarPlan? _plannedPlan;

    /// <summary>Keys of every group removed so far, in the order they were removed.</summary>
    private IReadOnlyCollection<string> RemovedKeys => _removals.SelectMany(k => k).Distinct(StringComparer.Ordinal).ToList();

    private bool CanEditBars() => !IsBusy && _plannedPlan is not null;

    [RelayCommand(CanExecute = nameof(CanEditBars))]
    private void RemoveBars(IReadOnlyList<string>? keys)
    {
        if (keys is null || keys.Count == 0 || _plannedPlan is null) return;
        _removals.Add(keys.ToList());
        ApplyRemovals($"Đã xóa {keys.Count} thanh/vùng đai khỏi bản vẽ và đánh số lại — Ctrl+Z để hoàn tác.");
    }

    private bool CanUndoRemoveBars() => CanEditBars() && _removals.Count > 0;

    [RelayCommand(CanExecute = nameof(CanUndoRemoveBars))]
    private void UndoRemoveBars()
    {
        if (_removals.Count == 0) return;
        _removals.RemoveAt(_removals.Count - 1);
        ApplyRemovals(_removals.Count == 0 ? "Đã hoàn tác: thép đủ như sheet Dam." : "Đã hoàn tác lần xóa gần nhất.");
    }

    private void ApplyRemovals(string message)
    {
        if (_plannedPlan is null) return;

        var keys = RemovedKeys;
        var plan = KataLayoutRemoval.Remove(_plannedPlan, keys, out var unknown);
        if (unknown.Count > 0) Log.Warning("Kata Export: {Count} removed bar key(s) are not in the plan", unknown.Count);

        RebarPlan = plan;
        RebarLayout = plan.Layout;
        _removalMessages = RemovalWarnings(keys).ToList();
        PublishWarnings();
        UndoRemoveBarsCommand.NotifyCanExecuteChanged();
        ShowState($"{message} Còn {plan.Layout.TotalBarCount} thanh, {plan.Layout.TotalSteelWeightKg:0.0} kg.");
    }

    /// <summary>A beam without its main bars or its hoops is unusual: said once, it is still the user's call.</summary>
    private static IEnumerable<string> RemovalWarnings(IReadOnlyCollection<string> keys)
    {
        if (keys.Count == 0) yield break;
        if (keys.Any(k => k.StartsWith("bar|MainTop|", StringComparison.Ordinal) || k.StartsWith("bar|MainBottom|", StringComparison.Ordinal)))
            yield return WarningPrefix + "Đã xóa thép chủ trên canvas — dầm sẽ thiếu thép chủ khi Tạo thép.";
        if (keys.Any(k => k.StartsWith("zone|", StringComparison.Ordinal)))
            yield return WarningPrefix + "Đã xóa vùng đai trên canvas — các vùng này không có đai (cả đai U/C trong vùng) khi Tạo thép.";
        if (keys.Any(k => k.StartsWith("bar|SideBar|", StringComparison.Ordinal) || k.StartsWith("bar|ExtraTop|", StringComparison.Ordinal)
                || k.StartsWith("bar|ExtraBottom|", StringComparison.Ordinal)))
            yield return WarningPrefix + "Đai C giữ cốt giá / kê lớp thép 2 không xóa theo thanh đã xóa — kiểm tra lại các đai C này.";
        yield return WarningPrefix + $"{keys.Count} thanh/vùng đai đã xóa trên canvas không được tạo trong Revit; sheet Dam không đổi.";
    }

    /// <summary>A new plan from the sheet: nothing removed yet.</summary>
    /// <returns>How many removals were dropped.</returns>
    private int ResetRemovals(KataRebarPlan? planned)
    {
        int dropped = _removals.Count;
        _plannedPlan = planned;
        _removals.Clear();
        _removalMessages = Array.Empty<string>();
        RemoveBarsCommand.NotifyCanExecuteChanged();
        UndoRemoveBarsCommand.NotifyCanExecuteChanged();
        return dropped;
    }
}
