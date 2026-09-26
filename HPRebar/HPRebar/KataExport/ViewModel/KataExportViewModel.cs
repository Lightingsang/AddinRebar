using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPRebar.Core.KataExport.Calculators;
using HPRebar.Core.KataExport.Models;
using HPRebar.KataExport.Model;
using HPRebar.KataExport.Service;
using Serilog;

namespace HPRebar.KataExport.ViewModel;

/// <summary>
/// One beam run, the sheet built from it and the Excel target. The sheet is built once per change of
/// run, parameters or direction, and the same instance is previewed and written, so what is shown is
/// exactly what goes to Excel. Sheet errors and Excel errors are kept apart so one never hides the other.
/// </summary>
public sealed partial class KataExportViewModel : ObservableObject
{
    private readonly IKataExportRunner _runner;
    private KataExportSession _session;
    private KataSheet? _sheet;
    private string? _sheetError;
    private string? _excelError;
    private string _targetFullName = string.Empty;

    public event Action? CloseRequested;

    [ObservableProperty] private string _selectedNameParameter = string.Empty;
    [ObservableProperty] private string _selectedCountParameter = string.Empty;
    [ObservableProperty] private bool _isReverse;
    [ObservableProperty] private string _targetWorkbook = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _hasError;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportCommand), nameof(RepickCommand))]
    private bool _isBusy;

    [ObservableProperty] private string _beamCountText = string.Empty;
    [ObservableProperty] private string _totalLengthText = string.Empty;
    [ObservableProperty] private string _sectionText = string.Empty;
    [ObservableProperty] private string _elevationText = string.Empty;
    [ObservableProperty] private string _slabThicknessText = string.Empty;
    [ObservableProperty] private string _axisText = string.Empty;
    [ObservableProperty] private string _axisOffsetText = string.Empty;
    [ObservableProperty] private IReadOnlyList<string> _availableParameters = Array.Empty<string>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasWarnings))]
    private IReadOnlyList<string> _warnings = Array.Empty<string>();

    [ObservableProperty] private string _headerLine = string.Empty;
    [ObservableProperty] private IReadOnlyList<KataPreviewColumn> _previewColumns = Array.Empty<KataPreviewColumn>();
    [ObservableProperty] private KataElevation? _elevation;

    public KataExportViewModel(KataExportSession session, IKataExportRunner runner)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        LoadSession();
        ProbeWorkbook();
    }

    public bool HasWarnings => Warnings.Count > 0;

    partial void OnSelectedNameParameterChanged(string value) => RebuildSheet();
    partial void OnSelectedCountParameterChanged(string value) => RebuildSheet();

    partial void OnIsReverseChanged(bool value)
    {
        int selected = SelectedColumnIndex;
        RebuildSheet();

        // The same element is now at the mirrored column; the run is framed whole again.
        int count = Elevation?.Columns.Count ?? 0;
        SelectedColumnIndex = selected >= 0 && selected < count ? count - 1 - selected : -1;
        FrameAll();
    }

    private void LoadSession()
    {
        AvailableParameters = _session.ParameterNames;

        // Without the office parameters, B3/B4 stay empty rather than taking an unrelated parameter.
        SelectedNameParameter = _session.DefaultName ?? string.Empty;
        SelectedCountParameter = _session.DefaultCount ?? string.Empty;

        BeamCountText = $"{_session.Pieces.Count} phần tử dầm";
        TotalLengthText = $"{_session.Pieces.Max(p => p.Extent.End) - _session.Pieces.Min(p => p.Extent.Start):N0} mm";
        SectionText = $"{_session.WidthMm:N0} x {_session.HeightMm:N0} mm";
        ElevationText = KataFormat.Elevation(_session.LevelElevationMm);
        SlabThicknessText = _session.SlabThicknessMm is { } slab ? $"{slab:N0} mm" : "0 mm (không thấy sàn)";
        AxisText = _session.AxisGridName ?? "(không thấy trục dọc dầm)";

        SelectedColumnIndex = -1;
        RebuildSheet();
        FrameAll();
    }

    private void RebuildSheet()
    {
        int selected = SelectedColumnIndex;
        try
        {
            // Sheet, drawing and table are built first and published together: what is shown is what goes to Excel.
            var input = _session.ToInput(NullIfEmpty(SelectedNameParameter), NullIfEmpty(SelectedCountParameter));
            var options = new KataBuildOptions { Reverse = IsReverse };
            var sheet = KataRowBuilder.Build(input, options);
            var elevation = KataElevationBuilder.Build(input, options, sheet);
            var columns = KataPreviewBuilder.Columns(elevation);

            _sheet = sheet;
            _sheetError = null;
            Elevation = elevation;
            HeaderLine = KataPreviewBuilder.HeaderLine(sheet);
            PreviewColumns = columns;
            // A new item list clears the table's selection; the same run keeps the column the user picked.
            SelectedColumnIndex = selected < columns.Count ? selected : -1;
            Warnings = _session.Warnings.Concat(sheet.Warnings).Distinct().ToList();
            AxisOffsetText = $"B9 = {sheet.HeaderColumn[6]} mm";
        }
        catch (Exception ex)
        {
            // Every failure leaves nothing to export; one thrown from a bound setter would otherwise be swallowed
            // by WPF and leave the old preview beside a new sheet.
            if (ex is not (ArgumentException or InvalidOperationException)) Log.Error(ex, "Kata Export: could not build the sheet");
            _sheet = null;
            _sheetError = $"Không lập được bảng Kata: {ex.Message}";
            Elevation = null;
            HeaderLine = string.Empty;
            PreviewColumns = Array.Empty<KataPreviewColumn>();
            SelectedColumnIndex = -1;
            Warnings = _session.Warnings;
        }

        SelectionText = KataPreviewBuilder.Describe(Elevation, SelectedColumnIndex);
        ShowState();
        ExportCommand.NotifyCanExecuteChanged();
    }

    private void ProbeWorkbook()
    {
        var probe = KataExcelWriter.Probe();
        _targetFullName = probe.Success ? probe.WorkbookFullName : string.Empty;
        _excelError = probe.Success ? null : probe.ErrorMessage;
        TargetWorkbook = probe.Success ? $"Workbook đích: {probe.WorkbookName} (sheet Dam)" : "Chưa có workbook Kata (sheet Dam) đang kích hoạt";
        ShowState();
        ExportCommand.NotifyCanExecuteChanged();
    }

    private void ShowState(string? message = null, bool error = false)
    {
        HasError = error || _sheetError is not null || (message is null && _excelError is not null);
        StatusMessage = message ?? _sheetError ?? _excelError ?? "Sẵn sàng ghi sang Excel.";
    }

    private bool CanExport() => !IsBusy && _sheet is not null && _excelError is null;

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportAsync()
    {
        IsBusy = true;
        ShowState("Đang ghi sang Excel...");
        try
        {
            var result = KataExcelWriter.Write(_sheet!, _targetFullName);
            if (!result.Success)
            {
                ShowState(result.ErrorMessage, error: true);
                return;
            }

            ShowState($"Đã ghi {result.ColumnsWritten} cột vào '{result.WorkbookName}' (sheet Dam).");
            try
            {
                await _runner.HighlightAsync(_session.BeamIds);
            }
            catch (Exception ex)
            {
                // The export itself succeeded; only the re-selection in Revit failed.
                Log.Warning(ex, "Kata Export: could not select the exported beams");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Kata Export: Excel write failed");
            ShowState($"Lỗi ghi Excel: {ex.Message}", error: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanRepick() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanRepick))]
    private async Task RepickAsync()
    {
        IsBusy = true;
        ShowState("Chọn dải dầm trên Revit (Finish để xong, Esc để huỷ)...");
        try
        {
            var session = await _runner.RepickAsync();
            if (session is null)
            {
                ShowState("Đã huỷ chọn lại dầm.");
                return;
            }

            _session = session;
            LoadSession();
            ProbeWorkbook();
        }
        catch (Exception ex)
        {
            // A reader error must reach the status line, never the dispatcher (an unhandled error there can take Revit down).
            Log.Error(ex, "Kata Export: re-pick failed");
            ShowState($"Không đọc được dải dầm: {ex.Message}", error: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void RefreshProbe() => ProbeWorkbook();

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();

    private static string? NullIfEmpty(string value) => string.IsNullOrEmpty(value) ? null : value;
}
