using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using HPRebar.KataRebar.Model;
using HPRebar.KataRebar.Service;
using Serilog;

namespace HPRebar.KataRebar.ViewModel;

/// <summary>
/// Kata Rebar window: reads sheet 'Dam' of the open Kata workbook, measures the picked beam, previews the
/// plan (bars, stirrup zones, what blocks and what is left out) and asks Revit to draw it.
/// </summary>
public sealed partial class KataRebarViewModel : ObservableObject
{
    private readonly IKataRebarRunner _runner;
    private readonly KataRebarTypeResolver _typeResolver;
    private KataBeamRebarSpec? _spec;
    private KataBeamMatchResult? _match;
    private IReadOnlyList<ElementId> _beamIds;

    public event Action? CloseRequested;

    [ObservableProperty] private string _workbookName = "(chưa đọc)";
    [ObservableProperty] private string _beamName = string.Empty;
    [ObservableProperty] private string _dimensionsText = string.Empty;
    [ObservableProperty] private string _levelText = string.Empty;
    [ObservableProperty] private string _spansCountText = string.Empty;
    [ObservableProperty] private string _totalLengthText = string.Empty;
    [ObservableProperty] private string _steelWeightText = string.Empty;
    [ObservableProperty] private string _matchStatusText = "Chưa chọn dầm trong Revit";
    [ObservableProperty] private IReadOnlyList<KataBarTypeMappingItem> _barTypeMappings = Array.Empty<KataBarTypeMappingItem>();
    [ObservableProperty] private IReadOnlyList<KataSpanPreviewItem> _spansPreview = Array.Empty<KataSpanPreviewItem>();
    [ObservableProperty] private IReadOnlyList<KataStirrupZoneResult> _stirrupZonesPreview = Array.Empty<KataStirrupZoneResult>();
    [ObservableProperty] private IReadOnlyList<KataBarLayerPreviewItem> _barLayersPreview = Array.Empty<KataBarLayerPreviewItem>();
    [ObservableProperty] private IReadOnlyList<string> _messages = Array.Empty<string>();
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _hasError;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateRebarCommand))]
    private bool _canGenerate;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateRebarCommand), nameof(RepickBeamsCommand), nameof(RefreshFromExcelCommand))]
    private bool _isBusy;

    public KataRebarViewModel(Document document, IReadOnlyList<ElementId> initialBeamIds, IKataRebarRunner runner)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _typeResolver = new KataRebarTypeResolver(document ?? throw new ArgumentNullException(nameof(document)));
        _beamIds = initialBeamIds ?? Array.Empty<ElementId>();

        LoadSheet();
        if (_beamIds.Count > 0) _ = MeasureAsync(_beamIds);
    }

    private bool IsIdle => !IsBusy;

    private bool CanRunGenerate => CanGenerate && !IsBusy;

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private void RefreshFromExcel() => LoadSheet();

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private async Task RepickBeams()
    {
        await Busy("Chọn dầm trong Revit rồi bấm Finish...", async () =>
        {
            var result = await _runner.PickBeamsAsync();
            if (result is null)
                Report("Đã huỷ chọn dầm.", false);
            else
                ApplyMatch(result);
        });
    }

    [RelayCommand(CanExecute = nameof(CanRunGenerate))]
    private async Task GenerateRebar()
    {
        if (_spec is null) return;

        var unmatched = BarTypeMappings.Where(m => m.SelectedType is null).Select(m => $"{m.Role} Ø{m.DiameterMm:0.#}").ToList();
        if (unmatched.Count > 0)
        {
            Report($"Chưa có RebarBarType cho: {string.Join(", ", unmatched)}. Chọn kiểu khác trong bảng, hoặc tải kiểu thép vào dự án rồi đóng và mở lại cửa sổ Kata Rebar.", true);
            return;
        }

        var barTypeIds = BarTypeMappings.ToDictionary(m => m.DiameterMm, m => m.SelectedType!.Id);
        await Busy("Đang vẽ thép trong Revit...", async () =>
        {
            var result = await _runner.GenerateAsync(_beamIds, _spec, barTypeIds);
            string warnings = result.RevitWarnings.Count > 0 ? $" Revit cảnh báo {result.RevitWarnings.Count} lần (xem log)." : "";
            Report(result.Message + warnings, !result.IsSuccess);
        });
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();

    private void LoadSheet()
    {
        var read = KataDamComReader.Read();
        if (!read.IsSuccess)
        {
            ForgetSheet();
            Report(read.Error, true);
            return;
        }

        try
        {
            _spec = KataDamSheetParser.Parse(read.Cells!);
            WorkbookName = read.WorkbookName;
            var plan = Replan();
            BarTypeMappings = KeepChoices(_typeResolver.BuildMappingItems(plan), BarTypeMappings);
            Report($"Đã đọc sheet Dam của '{read.WorkbookName}'.", false);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or FormatException)
        {
            Log.Error(ex, "Kata Rebar: sheet Dam could not be parsed");
            ForgetSheet();
            Report($"Không phân tích được sheet Dam: {ex.Message}", true);
        }
    }

    /// <summary>A sheet that could not be read must not stay ready to draw.</summary>
    private void ForgetSheet()
    {
        _spec = null;
        WorkbookName = "(chưa đọc)";
        BarTypeMappings = Array.Empty<KataBarTypeMappingItem>();
        Replan();
    }

    /// <summary>Keeps the type the user picked for a diameter when the sheet is read again.</summary>
    private static IReadOnlyList<KataBarTypeMappingItem> KeepChoices(IReadOnlyList<KataBarTypeMappingItem> fresh, IReadOnlyList<KataBarTypeMappingItem> previous)
    {
        foreach (var item in fresh)
        {
            var chosen = previous.FirstOrDefault(p => p.DiameterMm == item.DiameterMm)?.SelectedType;
            if (chosen is not null) item.SelectedType = chosen;
        }

        return fresh;
    }

    private async Task MeasureAsync(IReadOnlyList<ElementId> beamIds)
    {
        await Busy("Đang đo dầm trong Revit...", async () => ApplyMatch(await _runner.MeasureBeamsAsync(beamIds)));
    }

    private void ApplyMatch(KataBeamMatchResult result)
    {
        _match = result;
        if (result.IsSuccess) _beamIds = result.BeamIds;
        MatchStatusText = result.IsSuccess ? result.Message : $"Không đo được dầm: {result.Message}";
        var plan = Replan();
        if (_spec is not null)
            BarTypeMappings = KeepChoices(_typeResolver.BuildMappingItems(plan), BarTypeMappings);
        Report(CanGenerate ? "Sẵn sàng vẽ thép." : "Chưa vẽ được — xem tab Cảnh báo.", !CanGenerate);
    }

    /// <summary>Plans the sheet on the measured beam (or on the sheet alone before a pick) and refreshes the preview.</summary>
    private KataRebarPlan Replan()
    {
        var measured = _match is { IsSuccess: true } ? _match.Measured : null;
        var plan = KataRebarPlanner.Plan(_spec ?? new KataBeamRebarSpec(), measured);
        var spec = plan.Spec;

        BeamName = spec.BeamName;
        DimensionsText = $"{spec.Width:0} × {spec.Height:0} mm";
        LevelText = string.IsNullOrWhiteSpace(spec.LevelElevation) ? "-" : spec.LevelElevation;
        SpansCountText = $"{spec.Spans.Count} nhịp ({spec.Supports.Count} gối)";
        TotalLengthText = $"{spec.CalculateTotalLengthMm():0} mm";
        SteelWeightText = $"{plan.Layout.TotalSteelWeightKg:0.#} kg";
        SpansPreview = KataRebarPreviewBuilder.Spans(plan);
        BarLayersPreview = KataRebarPreviewBuilder.Bars(plan);
        StirrupZonesPreview = plan.Layout.StirrupZones;
        Messages = _spec is null ? Array.Empty<string>() : KataRebarPreviewBuilder.Messages(plan, _match);
        CanGenerate = _spec is not null && measured is not null && plan.CanGenerate;
        return plan;
    }

    private async Task Busy(string status, Func<Task> work)
    {
        IsBusy = true;
        Report(status, false);
        try
        {
            await work();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Kata Rebar: window action failed");
            Report($"Lỗi: {ex.Message}", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Report(string message, bool isError)
    {
        StatusMessage = message;
        HasError = isError;
    }
}
