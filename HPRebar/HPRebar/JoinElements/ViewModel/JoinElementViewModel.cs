using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPRebar.JoinElements.Model;

namespace HPRebar.JoinElements.ViewModel;

public sealed partial class JoinElementViewModel : ObservableObject
{
    private readonly JoinElementExternalEventHandler _handler;

    [ObservableProperty]
    private JoinScope _selectedScope = JoinScope.ActiveView;

    [ObservableProperty]
    private bool _unjoinFirst;

    [ObservableProperty]
    private bool _smartSkip = true;

    [ObservableProperty]
    private bool _cleanJoinedNonIntersecting = true;

    [ObservableProperty]
    private bool _switchOrderToPriority = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    public bool IsNotBusy => !IsBusy;

    [ObservableProperty]
    private string _statusMessage = "Sẵn sàng. Chọn phạm vi và cặp cấu kiện rồi nhấn 'CHẠY AUTO JOIN'.";

    [ObservableProperty]
    private int _totalCandidatePairs;

    [ObservableProperty]
    private int _totalJoined;

    [ObservableProperty]
    private int _totalSwitched;

    [ObservableProperty]
    private int _totalSkipped;

    [ObservableProperty]
    private string _executionTimeText = "-";

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _progressText = "";

    public ObservableCollection<JoinCategoryPairOption> CategoryPairs { get; } = new();

    public event Action? RequestClose;

    public JoinElementViewModel(JoinElementExternalEventHandler handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        InitializeCategoryPairs();
    }

    [RelayCommand]
    private async Task RunAutoJoinAsync()
    {
        if (IsBusy) return;

        var selectedPairs = CategoryPairs.Where(p => p.IsSelected).ToList();
        if (selectedPairs.Count == 0)
        {
            StatusMessage = "Vui lòng chọn ít nhất một cặp cấu kiện để xử lý.";
            return;
        }

        IsBusy = true;
        ProgressPercentage = 0;
        ProgressText = "0%";
        StatusMessage = "Đang quét va chạm hình học và thực hiện Auto Join...";

        var progress = new Progress<JoinProgressReport>(report =>
        {
            ProgressPercentage = report.Percentage;
            ProgressText = $"{report.Percentage:F0}%";
            StatusMessage = report.StepDescription;
        });

        try
        {
            var settings = new JoinElementSettings
            {
                Scope = SelectedScope,
                UnjoinFirst = UnjoinFirst,
                SmartSkip = SmartSkip,
                CleanJoinedNonIntersecting = CleanJoinedNonIntersecting,
                SwitchOrderToPriority = SwitchOrderToPriority,
                CategoryPairs = selectedPairs
            };

            var result = await _handler.ExecuteJoinAsync(settings, progress);
            if (result.Success)
            {
                TotalCandidatePairs = result.TotalPairsChecked;
                TotalJoined = result.TotalJoined;
                TotalSwitched = result.TotalSwitched;
                TotalSkipped = result.TotalSkipped;
                ExecutionTimeText = $"{result.Duration.TotalSeconds:F2}s";
                StatusMessage = result.Message;
                ProgressPercentage = 100;
                ProgressText = "100%";
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
    private void SelectAllPairs()
    {
        foreach (var p in CategoryPairs)
        {
            p.IsSelected = true;
        }
    }

    [RelayCommand]
    private void DeselectAllPairs()
    {
        foreach (var p in CategoryPairs)
        {
            p.IsSelected = false;
        }
    }

    [RelayCommand]
    private void ResetDefaultPriority()
    {
        CategoryPairs.Clear();
        InitializeCategoryPairs();
        StatusMessage = "Đã khôi phục thứ tự ưu tiên cắt gọt chuẩn BIM kết cấu.";
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke();
    }

    private void InitializeCategoryPairs()
    {
        // 1. Cột cắt Sàn (Cột là cấu kiện chịu lực chính)
        CategoryPairs.Add(new JoinCategoryPairOption(
            BuiltInCategory.OST_StructuralColumns,
            BuiltInCategory.OST_Floors,
            "Cột (Columns)",
            "Sàn (Floors)",
            isSelected: true,
            primaryCutsSecondary: true));

        // 2. Cột cắt Dầm
        CategoryPairs.Add(new JoinCategoryPairOption(
            BuiltInCategory.OST_StructuralColumns,
            BuiltInCategory.OST_StructuralFraming,
            "Cột (Columns)",
            "Dầm (Framing)",
            isSelected: true,
            primaryCutsSecondary: true));

        // 3. Cột cắt Vách
        CategoryPairs.Add(new JoinCategoryPairOption(
            BuiltInCategory.OST_StructuralColumns,
            BuiltInCategory.OST_Walls,
            "Cột (Columns)",
            "Vách (Walls)",
            isSelected: true,
            primaryCutsSecondary: true));

        // 4. Vách cắt Dầm
        CategoryPairs.Add(new JoinCategoryPairOption(
            BuiltInCategory.OST_Walls,
            BuiltInCategory.OST_StructuralFraming,
            "Vách (Walls)",
            "Dầm (Framing)",
            isSelected: true,
            primaryCutsSecondary: true));

        // 5. Vách cắt Sàn
        CategoryPairs.Add(new JoinCategoryPairOption(
            BuiltInCategory.OST_Walls,
            BuiltInCategory.OST_Floors,
            "Vách (Walls)",
            "Sàn (Floors)",
            isSelected: true,
            primaryCutsSecondary: true));

        // 6. Dầm cắt Sàn
        CategoryPairs.Add(new JoinCategoryPairOption(
            BuiltInCategory.OST_StructuralFraming,
            BuiltInCategory.OST_Floors,
            "Dầm (Framing)",
            "Sàn (Floors)",
            isSelected: true,
            primaryCutsSecondary: true));

        // 7. Móng & Cột (Móng cắt Cột)
        CategoryPairs.Add(new JoinCategoryPairOption(
            BuiltInCategory.OST_StructuralFoundation,
            BuiltInCategory.OST_StructuralColumns,
            "Móng (Foundation)",
            "Cột (Columns)",
            isSelected: false,
            primaryCutsSecondary: true));
    }
}
