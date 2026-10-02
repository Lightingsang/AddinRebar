using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPRebar.Core.KataExport.Models;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using HPRebar.KataExport.Model;
using HPRebar.KataRebar.Model;
using HPRebar.KataRebar.Service;
using Serilog;

namespace HPRebar.KataExport.ViewModel;

/// <summary>
/// The way back from Excel: the sheet Dam the user filled with bars is read, planned on the beams Revit
/// measures (on the API thread) and drawn on the canvas; generation re-plans the same sheet the same way,
/// so the canvas shows what goes into the model.
/// </summary>
public sealed partial class KataExportViewModel
{
    private const string BlockingPrefix = "[Chặn] ";
    private const string WarningPrefix = "[Cảnh báo] ";
    private const string SkippedPrefix = "[Bỏ qua] ";

    private readonly KataRebarTypeResolver? _typeResolver;
    private IReadOnlyList<string> _exportWarnings = Array.Empty<string>();
    private IReadOnlyList<string> _rebarMessages = Array.Empty<string>();

    [ObservableProperty] private KataBeamRebarSpec? _rebarSpec;
    [ObservableProperty] private KataRebarPlan? _rebarPlan;
    [ObservableProperty] private KataRebarLayoutResult? _rebarLayout;
    [ObservableProperty] private KataStationMap _rebarStationMap = KataStationMap.Identity;
    [ObservableProperty] private bool _showRebar = true;
    [ObservableProperty] private bool _showSection = true;
    [ObservableProperty] private bool _showBarTags = true;
    [ObservableProperty] private IReadOnlyList<KataBarTypeMappingItem> _barTypeMappings = Array.Empty<KataBarTypeMappingItem>();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateRebarCommand))]
    private bool _canGenerateRebar;

    private bool CanLoadRebar() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanLoadRebar))]
    private async Task LoadRebarAsync()
    {
        var read = KataDamComReader.Read();
        if (!read.IsSuccess)
        {
            ShowState($"Không đọc được Excel: {read.Error}", error: true);
            return;
        }

        try
        {
            RebarSpec = KataDamSheetParser.Parse(read.Cells!);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or FormatException)
        {
            Log.Error(ex, "Kata Export: sheet Dam could not be parsed");
            ShowState($"Không phân tích được sheet Dam: {ex.Message}", error: true);
            return;
        }

        await PreviewRebarAsync();
    }

    /// <summary>Plans the loaded sheet on the current beams with the current settings and shows the result.</summary>
    private async Task PreviewRebarAsync()
    {
        if (RebarSpec is null) return;

        IsBusy = true;
        ShowState("Đang đo dầm trong Revit và bố trí thép...");
        try
        {
            var preview = await _runner.PreviewRebarAsync(_session.BeamIds, RebarSpec, KataSettingsStore.Load(), IsReverse);
            ApplyPreview(preview);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Kata Export: rebar preview failed");
            ClearRebar();
            ShowState($"Không bố trí được thép: {ex.Message}", error: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyPreview(KataRebarPreview preview)
    {
        if (!preview.IsSuccess || preview.Plan is null)
        {
            ClearRebar();
            ShowState(preview.Message, error: true);
            return;
        }

        var plan = preview.Plan;
        RebarPlan = plan;
        RebarLayout = plan.Layout;
        BarTypeMappings = _typeResolver?.BuildMappingItems(plan) ?? Array.Empty<KataBarTypeMappingItem>();
        RefreshStationMap();

        var messages = new List<string>();
        messages.AddRange(plan.Blocking.Select(m => BlockingPrefix + m));
        messages.AddRange(NameCheck(plan).Select(m => WarningPrefix + m));
        messages.AddRange(plan.Warnings.Concat(preview.MeasureWarnings).Distinct().Select(m => WarningPrefix + m));
        messages.AddRange(plan.Skipped.Select(m => SkippedPrefix + m));
        _rebarMessages = messages;
        PublishWarnings();

        CanGenerateRebar = plan.CanGenerate;
        ShowState(plan.CanGenerate
                ? $"Đã đọc thép dầm '{plan.Spec.BeamName}': {plan.Layout.TotalBarCount} thanh, {plan.Layout.TotalSteelWeightKg:0.0} kg — kiểm tra rồi bấm Tạo thép."
                : $"Thép dầm '{plan.Spec.BeamName}' chưa vẽ được: {plan.Blocking.Count} mục [Chặn] trong danh sách cảnh báo.",
            error: !plan.CanGenerate);
    }

    /// <summary>A sheet naming another beam than the one exported is worth a look, but the geometry decides.</summary>
    private IEnumerable<string> NameCheck(KataRebarPlan plan)
    {
        string sheetName = plan.Spec.BeamName?.Trim() ?? string.Empty;
        string exported = BeamNameValue?.Trim() ?? string.Empty;
        if (sheetName.Length > 0 && exported.Length > 0 && exported != "-" && !string.Equals(sheetName, exported, StringComparison.OrdinalIgnoreCase))
            yield return $"B3 của sheet là '{sheetName}' nhưng dải dầm đang chọn xuất ra tên '{exported}' — kiểm tra đúng dầm.";
    }

    /// <summary>Places the plan's local X on the canvas stations: the drawing may list the run the other way.</summary>
    private void RefreshStationMap()
    {
        var supports = Elevation?.Columns.Where(c => c.Kind == KataColumnKind.Support).ToList();
        if (RebarPlan is null || supports is null || supports.Count == 0)
        {
            RebarStationMap = KataStationMap.Identity;
            return;
        }

        var first = supports[0].Extent;
        var last = supports[supports.Count - 1].Extent;
        RebarStationMap = KataStationMap.For(
            Math.Min(first.Start, first.End),
            Math.Max(last.Start, last.End),
            sameOrder: RebarPlan.Reversed == IsReverse);
    }

    private void ClearRebar()
    {
        RebarPlan = null;
        RebarLayout = null;
        BarTypeMappings = Array.Empty<KataBarTypeMappingItem>();
        CanGenerateRebar = false;
        _rebarMessages = Array.Empty<string>();
        PublishWarnings();
    }

    private void PublishWarnings() => Warnings = _exportWarnings.Concat(_rebarMessages).Distinct().ToList();

    private bool CanRunGenerateRebar() => CanGenerateRebar && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanRunGenerateRebar))]
    private async Task GenerateRebarAsync()
    {
        if (RebarSpec is null) return;

        var unmatched = BarTypeMappings.Where(m => m.SelectedType is null).Select(m => $"{m.Role} Ø{m.DiameterMm:0.#}").ToList();
        if (unmatched.Count > 0)
        {
            ShowState($"Chưa có kiểu thép (RebarBarType) cho: {string.Join(", ", unmatched)} — tải kiểu thép vào dự án.", error: true);
            return;
        }

        var barTypeIds = BarTypeMappings.ToDictionary(m => m.DiameterMm, m => m.SelectedType!.Id);
        IsBusy = true;
        ShowState("Đang tạo cốt thép vào Revit...");
        try
        {
            var result = await _runner.GenerateRebarAsync(_session.BeamIds, RebarSpec, KataSettingsStore.Load(), IsReverse, barTypeIds);
            ShowState(result.Message, error: !result.IsSuccess);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Kata Export: rebar generation failed");
            ShowState($"Lỗi tạo thép: {ex.Message}", error: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanLoadRebar))]
    private async Task OpenSettingsAsync()
    {
        View.KataSettingsView? window = null;
        bool accepted = false;
        var viewModel = new KataSettingsViewModel(KataSettingsStore.Load(), ok =>
        {
            accepted = ok;
            window?.Close();
        });

        // Revit hosts no WPF Application, so the owner is this view model's own window.
        window = new View.KataSettingsView(viewModel) { Owner = OwnWindow() };
        window.ShowDialog();

        if (viewModel.SaveFailed)
            ShowState("Không ghi được file thiết lập; thông số chỉ dùng trong phiên Revit này.", error: true);
        if (accepted && RebarSpec is not null) await PreviewRebarAsync();
    }

    private System.Windows.Window? OwnWindow() =>
        System.Windows.PresentationSource.CurrentSources
            .OfType<System.Windows.Interop.HwndSource>()
            .Select(source => source.RootVisual)
            .OfType<System.Windows.Window>()
            .FirstOrDefault(w => ReferenceEquals(w.DataContext, this));

    partial void OnIsBusyChanged(bool value)
    {
        LoadRebarCommand.NotifyCanExecuteChanged();
        GenerateRebarCommand.NotifyCanExecuteChanged();
        OpenSettingsCommand.NotifyCanExecuteChanged();
    }
}
