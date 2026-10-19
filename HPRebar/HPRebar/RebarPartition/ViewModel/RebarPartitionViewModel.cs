using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPRebar.RebarPartition.Model;

namespace HPRebar.RebarPartition.ViewModel;

public sealed partial class RebarPartitionViewModel : ObservableObject
{
    private readonly RebarPartitionExternalEventHandler _handler;

    [ObservableProperty]
    private PartitionScope _selectedScope = PartitionScope.ActiveView;

    [ObservableProperty]
    private PartitionSourceParameter _selectedSource = PartitionSourceParameter.ElementName;

    [ObservableProperty]
    private string _customParameterName = "Element Name";

    [ObservableProperty]
    private string _prefix = string.Empty;

    [ObservableProperty]
    private string _suffix = string.Empty;

    [ObservableProperty]
    private bool _assignNumberElement = true;

    [ObservableProperty]
    private bool _groupContinuousGeometry = true;

    [ObservableProperty]
    private string _numberElementParamName = "Number Element";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Sẵn sàng. Chọn thông số và nhấn 'Xem trước'.";

    [ObservableProperty]
    private int _totalFound;

    [ObservableProperty]
    private int _totalToUpdate;

    [ObservableProperty]
    private int _totalMatching;

    public ObservableCollection<HostCategoryOption> HostCategories { get; } = new();

    public ObservableCollection<RebarPartitionPreviewItem> PreviewItems { get; } = new();

    public event Action? RequestClose;

    public bool IsCustomParamVisible => SelectedSource == PartitionSourceParameter.Custom;

    public RebarPartitionViewModel(RebarPartitionExternalEventHandler handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));

        InitializeHostCategories();
    }

    partial void OnSelectedSourceChanged(PartitionSourceParameter value)
    {
        OnPropertyChanged(nameof(IsCustomParamVisible));
    }

    [RelayCommand]
    private async Task PreviewAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        StatusMessage = "Đang quét và phân tích dữ liệu thép từ Revit...";

        try
        {
            var settings = new RebarPartitionSettings
            {
                Scope = SelectedScope,
                SourceParameter = SelectedSource,
                CustomParameterName = CustomParameterName,
                Prefix = Prefix,
                Suffix = Suffix,
                AssignNumberElement = AssignNumberElement,
                GroupContinuousGeometry = GroupContinuousGeometry,
                NumberElementParameterName = NumberElementParamName,
                EnabledCategories = new HashSet<BuiltInCategory>(
                    HostCategories.Where(c => c.IsSelected).Select(c => c.Category))
            };

            var items = await _handler.BuildPreviewAsync(settings);

            PreviewItems.Clear();
            foreach (var item in items)
            {
                PreviewItems.Add(item);
            }

            TotalFound = items.Count;
            TotalToUpdate = items.Count(i => i.Status == ItemStatus.ReadyToUpdate);
            TotalMatching = items.Count(i => i.Status == ItemStatus.AlreadyMatching);

            StatusMessage = $"Tìm thấy {TotalFound} thanh thép: {TotalToUpdate} cần cập nhật, {TotalMatching} đã khớp.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi khi phân tích: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ApplyAsync()
    {
        if (IsBusy) return;

        var itemsToApply = PreviewItems.Where(i => i.IsIncluded && i.CanApply).ToList();
        if (itemsToApply.Count == 0)
        {
            StatusMessage = "Không có thanh thép nào được chọn để áp dụng.";
            return;
        }

        IsBusy = true;
        StatusMessage = $"Đang cập nhật Partition cho {itemsToApply.Count} thanh thép...";

        try
        {
            var settings = new RebarPartitionSettings
            {
                Scope = SelectedScope,
                SourceParameter = SelectedSource,
                CustomParameterName = CustomParameterName,
                Prefix = Prefix,
                Suffix = Suffix,
                AssignNumberElement = AssignNumberElement,
                GroupContinuousGeometry = GroupContinuousGeometry,
                NumberElementParameterName = NumberElementParamName,
                EnabledCategories = new HashSet<BuiltInCategory>(
                    HostCategories.Where(c => c.IsSelected).Select(c => c.Category))
            };

            var result = await _handler.ApplyAsync(itemsToApply, settings);
            if (result.Success)
            {
                StatusMessage = result.Message;
                // Refresh preview to show updated status
                await PreviewAsync();
            }
            else
            {
                StatusMessage = $"Lỗi: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi thực thi: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var item in PreviewItems.Where(i => i.CanApply))
        {
            item.IsIncluded = true;
        }
    }

    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var item in PreviewItems)
        {
            item.IsIncluded = false;
        }
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke();
    }

    private void InitializeHostCategories()
    {
        HostCategories.Add(new HostCategoryOption(BuiltInCategory.OST_StructuralFraming, "Dầm (Framing)", true));
        HostCategories.Add(new HostCategoryOption(BuiltInCategory.OST_StructuralColumns, "Cột (Columns)", true));
        HostCategories.Add(new HostCategoryOption(BuiltInCategory.OST_StructuralFoundation, "Móng (Foundations)", true));
        HostCategories.Add(new HostCategoryOption(BuiltInCategory.OST_Walls, "Vách / Tường (Walls)", true));
        HostCategories.Add(new HostCategoryOption(BuiltInCategory.OST_Floors, "Sàn (Floors)", true));
        HostCategories.Add(new HostCategoryOption(BuiltInCategory.OST_StructConnections, "Liên kết kết cấu", true));
    }
}
