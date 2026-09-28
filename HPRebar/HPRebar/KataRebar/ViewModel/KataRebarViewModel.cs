using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using HPRebar.KataRebar.Excel;
using HPRebar.KataRebar.Model;
using HPRebar.KataRebar.Service;
using Microsoft.Win32;
using Serilog;
using RevitUnits = HPRebar.KataExport.Service.RevitUnits;

namespace HPRebar.KataRebar.ViewModel;

public sealed record KataSpanPreviewItem
{
    public int SpanNumber { get; init; }
    public double LeftColumnWidthMm { get; init; }
    public double ClearSpanLengthMm { get; init; }
    public double RightColumnWidthMm { get; init; }
    public string StirrupSpacing { get; init; } = "";
    public string TopDrop { get; init; } = "";
    public string SoffitDrop { get; init; } = "";
}

public sealed record KataBarLayerPreviewItem
{
    public string Category { get; init; } = "";
    public string Location { get; init; } = "";
    public string Notation { get; init; } = "";
    public int Count { get; init; }
    public double DiameterMm { get; init; }
    public string Details { get; init; } = "";
}

public sealed partial class KataRebarViewModel : ObservableObject
{
    private readonly IKataRebarRunner _runner;
    private readonly Document _document;
    private readonly KataRebarTypeResolver _typeResolver;
    private KataBeamMatchResult? _matchResult;
    private KataBeamRebarSpec? _spec;
    private KataRebarLayoutResult? _layout;
    private IReadOnlyList<ElementId> _selectedBeamIds;

    public event Action? CloseRequested;

    [ObservableProperty] private string _excelSourcePath = string.Empty;
    [ObservableProperty] private bool _isComActive;
    [ObservableProperty] private string _activeWorkbookName = string.Empty;
    [ObservableProperty] private string _beamName = string.Empty;
    [ObservableProperty] private string _dimensionsText = string.Empty;
    [ObservableProperty] private string _levelText = string.Empty;
    [ObservableProperty] private string _totalLengthText = string.Empty;
    [ObservableProperty] private string _spansCountText = string.Empty;
    [ObservableProperty] private string _steelWeightText = string.Empty;
    [ObservableProperty] private string _totalBarsCountText = string.Empty;

    [ObservableProperty] private IReadOnlyList<KataBarTypeMappingItem> _barTypeMappings = Array.Empty<KataBarTypeMappingItem>();
    [ObservableProperty] private IReadOnlyList<KataSpanPreviewItem> _spansPreview = Array.Empty<KataSpanPreviewItem>();
    [ObservableProperty] private IReadOnlyList<KataStirrupZoneResult> _stirrupZonesPreview = Array.Empty<KataStirrupZoneResult>();
    [ObservableProperty] private IReadOnlyList<KataBarLayerPreviewItem> _barLayersPreview = Array.Empty<KataBarLayerPreviewItem>();

    [ObservableProperty] private bool _isBeamMatched;
    [ObservableProperty] private string _matchStatusText = "Chưa kết nối dầm Revit";
    [ObservableProperty] private IReadOnlyList<string> _warnings = Array.Empty<string>();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateRebarCommand), nameof(RepickBeamsCommand), nameof(RefreshFromExcelCommand))]
    private bool _isBusy;

    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _hasError;

    public KataRebarViewModel(
        Document document,
        IReadOnlyList<ElementId> initialBeamIds,
        IKataRebarRunner runner)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _selectedBeamIds = initialBeamIds ?? Array.Empty<ElementId>();
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _typeResolver = new KataRebarTypeResolver(_document);

        // Initial load from active Excel or prompt
        LoadInitialData();
    }

    public KataBeamRebarSpec? CurrentSpec => _spec;
    public KataRebarLayoutResult? CurrentLayout => _layout;
    public KataBeamMatchResult? CurrentMatchResult => _matchResult;

    private void LoadInitialData()
    {
        // 1. Try reading active Excel via COM
        if (ComKataDamReader.TryReadActiveSheet(out var table, out var comError))
        {
            IsComActive = true;
            ActiveWorkbookName = "Excel đang mở";
            ExcelSourcePath = "Active Excel";
            ParseAndCompute(table!);
        }
        else
        {
            IsComActive = false;
            StatusMessage = string.IsNullOrEmpty(comError)
                ? "Không phát hiện Excel đang mở chứa sheet 'Dam'. Hãy mở file Kata hoặc chọn file bên dưới."
                : comError;
        }
    }

    [RelayCommand]
    private void RefreshFromExcel()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Đang đọc lại dữ liệu sheet Dam...";
        HasError = false;

        try
        {
            if (IsComActive && ComKataDamReader.TryReadActiveSheet(out var table, out _))
            {
                ActiveWorkbookName = "Excel đang mở";
                ExcelSourcePath = "Active Excel";
                ParseAndCompute(table!);
                StatusMessage = "Đã tải dữ liệu từ Excel đang mở thành công.";
            }
            else if (!string.IsNullOrEmpty(ExcelSourcePath) && File.Exists(ExcelSourcePath))
            {
                if (ClosedXmlKataDamReader.TryReadFromFile(ExcelSourcePath, out var fileTable, out var fileErr))
                {
                    ParseAndCompute(fileTable!);
                    StatusMessage = $"Đã tải dữ liệu từ file '{Path.GetFileName(ExcelSourcePath)}' thành công.";
                }
                else
                {
                    HasError = true;
                    StatusMessage = fileErr ?? "Không đọc được file Excel.";
                }
            }
            else
            {
                // Try active COM again
                if (ComKataDamReader.TryReadActiveSheet(out var retryTable, out var retryErr))
                {
                    IsComActive = true;
                    ActiveWorkbookName = "Excel đang mở";
                    ExcelSourcePath = "Active Excel";
                    ParseAndCompute(retryTable!);
                    StatusMessage = "Đã phát hiện và tải dữ liệu từ Excel đang mở.";
                }
                else
                {
                    HasError = true;
                    StatusMessage = retryErr ?? "Chưa có nguồn dữ liệu Excel hợp lệ.";
                }
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Lỗi khi đọc Excel: {ex.Message}";
            Log.Error(ex, "Lỗi RefreshFromExcel");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void BrowseFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn file KATA Excel (.xlsm / .xlsx)",
            Filter = "Excel Files (*.xlsm;*.xlsx)|*.xlsm;*.xlsx|All Files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == true)
        {
            ExcelSourcePath = dialog.FileName;
            IsComActive = false;
            ActiveWorkbookName = Path.GetFileName(dialog.FileName);

            if (ClosedXmlKataDamReader.TryReadFromFile(dialog.FileName, out var table, out var error))
            {
                ParseAndCompute(table!);
                StatusMessage = $"Đã đọc thành công file '{ActiveWorkbookName}'.";
                HasError = false;
            }
            else
            {
                HasError = true;
                StatusMessage = error ?? "Không đọc được file Excel đã chọn.";
            }
        }
    }

    private void ParseAndCompute(HPRebar.Core.KataRebar.Parsers.IKataDamCellAccessor accessor)
    {
        try
        {
            _spec = KataDamSheetParser.Parse(accessor);

            BeamName = _spec.BeamName;
            DimensionsText = $"{_spec.Width:0} × {_spec.Height:0} mm";
            LevelText = !string.IsNullOrEmpty(_spec.LevelElevation) ? _spec.LevelElevation : "Theo mô hình";
            SpansCountText = $"{_spec.Spans.Count} nhịp ({_spec.Supports.Count} gối)";

            // Calculate 3D Rebar geometry
            _layout = KataRebarCalculator.Calculate(_spec);
            SteelWeightText = $"{_layout.TotalSteelWeightKg:0.#} kg";
            TotalBarsCountText = $"{_layout.TotalBarCount} thanh/cụm";

            double totalLen = _spec.Spans.Sum(s => s.Length) + _spec.Supports.Sum(s => s.ColumnWidth);
            TotalLengthText = $"{totalLen:0} mm";

            // Update UI Previews
            BarTypeMappings = _typeResolver.BuildMappingItems(_spec);
            SpansPreview = BuildSpansPreview(_spec);
            StirrupZonesPreview = _layout.StirrupZones;
            BarLayersPreview = BuildBarLayersPreview(_spec);

            // Re-match Revit beams if already selected
            if (_selectedBeamIds.Count > 0)
            {
                _ = TriggerMatchAsync(_selectedBeamIds);
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Lỗi phân tích bảng tính Kata: {ex.Message}";
            Log.Error(ex, "Lỗi ParseAndCompute trong KataRebarViewModel");
        }
    }

    private static IReadOnlyList<KataSpanPreviewItem> BuildSpansPreview(KataBeamRebarSpec spec)
    {
        var list = new List<KataSpanPreviewItem>();
        for (int i = 0; i < spec.Spans.Count; i++)
        {
            var span = spec.Spans[i];
            double leftCol = (i < spec.Supports.Count) ? spec.Supports[i].ColumnWidth : 0.0;
            double rightCol = (i + 1 < spec.Supports.Count) ? spec.Supports[i + 1].ColumnWidth : 0.0;

            string stirrupStr = $"d{spec.GlobalStirrup.Diameter:0} a{spec.GlobalStirrup.SupportSpacing:0}/{spec.GlobalStirrup.MidspanSpacing:0}";
            if (span.StirrupOverride is not null)
            {
                stirrupStr += $" (riêng: a{span.StirrupOverride.SupportSpacing:0}/{span.StirrupOverride.MidspanSpacing:0})";
            }

            list.Add(new KataSpanPreviewItem
            {
                SpanNumber = i + 1,
                LeftColumnWidthMm = leftCol,
                ClearSpanLengthMm = span.Length,
                RightColumnWidthMm = rightCol,
                StirrupSpacing = stirrupStr,
                TopDrop = span.TopDrop != 0.0 ? $"{span.TopDrop:+0;-0} mm" : "-",
                SoffitDrop = span.SoffitDrop != 0.0 ? $"{span.SoffitDrop:+0;-0} mm" : "-"
            });
        }
        return list;
    }

    private static IReadOnlyList<KataBarLayerPreviewItem> BuildBarLayersPreview(KataBeamRebarSpec spec)
    {
        var list = new List<KataBarLayerPreviewItem>();

        if (!spec.TopContinuous.IsEmpty)
        {
            list.Add(new KataBarLayerPreviewItem
            {
                Category = "Thép chủ dọc",
                Location = "Trên suốt dầm",
                Notation = spec.TopContinuous.RawNotation,
                Count = spec.TopContinuous.Count,
                DiameterMm = spec.TopContinuous.Diameter,
                Details = "Chạy suốt từ đầu dầm đến cuối dầm kèm neo gối biên 90°"
            });
        }

        if (!spec.BottomContinuous.IsEmpty)
        {
            list.Add(new KataBarLayerPreviewItem
            {
                Category = "Thép chủ dọc",
                Location = "Dưới suốt dầm",
                Notation = spec.BottomContinuous.RawNotation,
                Count = spec.BottomContinuous.Count,
                DiameterMm = spec.BottomContinuous.Diameter,
                Details = "Chạy suốt từ đầu dầm đến cuối dầm kèm neo gối biên 90°"
            });
        }

        for (int i = 0; i < spec.Supports.Count; i++)
        {
            var supp = spec.Supports[i];
            int layerIdx = 1;
            foreach (var layerList in supp.AllTopExtraLayers)
            {
                foreach (var barItem in layerList)
                {
                    if (!barItem.IsEmpty)
                    {
                        list.Add(new KataBarLayerPreviewItem
                        {
                            Category = "Tăng cường gối (âm)",
                            Location = $"Gối {i + 1} (L{layerIdx})",
                            Notation = barItem.RawNotation,
                            Count = barItem.Count,
                            DiameterMm = barItem.Diameter,
                            Details = $"Cắt L/{spec.TopCutoffRatioLayer1:0.##} hai bên gối"
                        });
                    }
                }
                layerIdx++;
            }
        }

        for (int i = 0; i < spec.Spans.Count; i++)
        {
            var span = spec.Spans[i];
            int layerIdx = 1;
            foreach (var layerList in span.AllBottomExtraLayers)
            {
                foreach (var barItem in layerList)
                {
                    if (!barItem.IsEmpty)
                    {
                        list.Add(new KataBarLayerPreviewItem
                        {
                            Category = "Tăng cường nhịp (dương)",
                            Location = $"Nhịp {i + 1} (L{layerIdx})",
                            Notation = barItem.RawNotation,
                            Count = barItem.Count,
                            DiameterMm = barItem.Diameter,
                            Details = "Cắt cách mép cột L/7"
                        });
                    }
                }
                layerIdx++;
            }

            foreach (var sideBar in span.SideBars)
            {
                if (!sideBar.IsEmpty)
                {
                    list.Add(new KataBarLayerPreviewItem
                    {
                        Category = "Thép giá / cấu tạo",
                        Location = $"Nhịp {i + 1}",
                        Notation = sideBar.RawNotation,
                        Count = sideBar.Count,
                        DiameterMm = sideBar.Diameter,
                        Details = "Thép chống phình / co ngót theo nhịp"
                    });
                }
            }
        }

        foreach (var sideBar in spec.GlobalSideBars)
        {
            if (!sideBar.IsEmpty)
            {
                list.Add(new KataBarLayerPreviewItem
                {
                    Category = "Thép giá chung",
                    Location = "Toàn bộ dầm",
                    Notation = sideBar.RawNotation,
                    Count = sideBar.Count,
                    DiameterMm = sideBar.Diameter,
                    Details = "Thép mang hai bên thân dầm (h >= 700mm)"
                });
            }
        }

        return list;
    }

    [RelayCommand]
    private async Task RepickBeams()
    {
        if (IsBusy) return;
        if (_spec is null)
        {
            StatusMessage = "Hãy nạp dữ liệu từ sheet Dam của Kata trước khi chọn dầm.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Vui lòng chọn dải dầm trong Revit và nhấn Finish...";
        try
        {
            var result = await _runner.RepickBeamsAsync(_spec);
            if (result is not null && result.IsSuccess)
            {
                _matchResult = result;
                _selectedBeamIds = result.OrderedBeams.Select(b => b.Id).ToList();
                IsBeamMatched = true;
                MatchStatusText = $"Đã khớp {result.OrderedBeams.Count} đoạn dầm (Cao độ đỉnh: {RevitUnits.FtToMm(result.BeamTopElevationFt):0} mm)";
                Warnings = result.Warnings;
                StatusMessage = "Đã khớp dầm thành công. Sẵn sàng tạo thép.";
                HasError = false;
            }
            else if (result is not null)
            {
                IsBeamMatched = false;
                MatchStatusText = "Không khớp dầm";
                StatusMessage = result.Message;
                HasError = true;
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Lỗi khi chọn dầm: {ex.Message}";
            Log.Error(ex, "Lỗi RepickBeams");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task TriggerMatchAsync(IReadOnlyList<ElementId> beamIds)
    {
        if (_spec is null || beamIds.Count == 0) return;

        try
        {
            var result = await _runner.MatchBeamsAsync(beamIds, _spec);
            if (result is not null && result.IsSuccess)
            {
                _matchResult = result;
                IsBeamMatched = true;
                MatchStatusText = $"Đã khớp {result.OrderedBeams.Count} đoạn dầm ({result.TotalRunLengthMm:0} mm)";
                Warnings = result.Warnings;
            }
            else if (result is not null)
            {
                IsBeamMatched = false;
                MatchStatusText = result.Message;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Lỗi khi tự động khớp dầm");
        }
    }

    [RelayCommand]
    private async Task GenerateRebar()
    {
        if (IsBusy) return;

        if (_spec is null || _layout is null)
        {
            StatusMessage = "Chưa có thông số thép từ bảng tính Kata.";
            HasError = true;
            return;
        }

        if (_matchResult is null || !_matchResult.IsSuccess || _matchResult.OrderedBeams.Count == 0)
        {
            StatusMessage = "Chưa khớp dầm trong mô hình Revit. Vui lòng bấm 'Chọn dầm' trước.";
            HasError = true;
            return;
        }

        // Build resolved bar type dictionary from user overrides
        var resolvedDict = new Dictionary<double, RebarBarType>();
        foreach (var mapping in BarTypeMappings)
        {
            if (mapping.SelectedType?.BarType is not null)
            {
                resolvedDict[mapping.DiameterMm] = mapping.SelectedType.BarType;
            }
        }

        IsBusy = true;
        StatusMessage = "Đang tạo thép 3D trong Revit...";
        HasError = false;

        try
        {
            var genResult = await _runner.GenerateRebarAsync(_matchResult, _spec, _layout, resolvedDict);

            if (genResult.IsSuccess)
            {
                StatusMessage = genResult.Message;
                HasError = false;
            }
            else
            {
                StatusMessage = genResult.Message;
                HasError = true;
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Lỗi khi tạo cốt thép: {ex.Message}";
            Log.Error(ex, "Lỗi GenerateRebar");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Close()
    {
        CloseRequested?.Invoke();
    }
}
