using System.Collections.ObjectModel;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPAutoCad.Core.SmartPlot.Models;
using HPAutoCad.Core.SmartPlot.Services;
using HPAutoCad.SmartPlot.Cad.Plot;
using HPAutoCad.SmartPlot.Cad.Providers;

namespace HPAutoCad.SmartPlot.UI;

/// <summary>
/// Core ViewModel for Smart Plot Pro managing plot configuration, frame scanning,
/// sorting, publishing progress, preset loading/saving, and modeless user interaction.
/// </summary>
public sealed partial class SmartPlotViewModel : ObservableObject, IDisposable
{
    private readonly Document? _doc;
    private readonly IFrameProvider _frameProvider;
    private readonly IAutoCadPlotEngine _plotEngine;
    private readonly IPlotOrderService _plotOrderService;
    private readonly IFileNameService _fileNameService;
    private readonly IPresetService _presetService;

    private CancellationTokenSource? _cts;

    public event Action<Action<ObjectId>>? PickFrameRequested;
    public event Action? CloseRequested;

    // Collections
    public ObservableCollection<PlotItemViewModel> Items { get; } = [];
    public ObservableCollection<string> Devices { get; } = [];
    public ObservableCollection<string> MediaSizes { get; } = [];
    public ObservableCollection<string> PlotStyles { get; } = [];
    public ObservableCollection<string> AvailableBlocks { get; } = [];
    public ObservableCollection<string> AvailableLayers { get; } = [];
    public ObservableCollection<PlotPreset> Presets { get; } = [];

    // Header & Document info
    public string ActiveDocumentName => _doc?.Name != null ? Path.GetFileName(_doc.Name) : "Không có bản vẽ";

    // Frame Source Configuration
    [ObservableProperty]
    private FrameSourceType _frameSource = FrameSourceType.Block;

    [ObservableProperty]
    private string _blockName = string.Empty;

    [ObservableProperty]
    private string _sheetNumberAttribute = "SOHIEU";

    [ObservableProperty]
    private string _sheetTitleAttribute = "TENTIEUDE";

    [ObservableProperty]
    private string _layerName = "0";

    [ObservableProperty]
    private string _layoutRange = "All";

    // Printer & Paper Configuration
    [ObservableProperty]
    private string _deviceName = "AutoCAD PDF (General Documentation).pc3";

    [ObservableProperty]
    private string _mediaName = "ISO_full_bleed_A1_(841.00_x_594.00_MM)";

    [ObservableProperty]
    private string _plotStyle = "monochrome.ctb";

    // Orientation & Scale
    [ObservableProperty]
    private OrientationMode _orientation = OrientationMode.Auto;

    [ObservableProperty]
    private bool _centerPlot = true;

    [ObservableProperty]
    private bool _fitToPaper = true;

    // Output & Naming
    [ObservableProperty]
    private OutputMode _outputMode = OutputMode.SingleFiles;

    [ObservableProperty]
    private string _outputFolder = string.Empty;

    [ObservableProperty]
    private string _fileNamePattern = "{Prefix}_{Layout}_{SheetNo}_{Title}";

    [ObservableProperty]
    private string _fileNamePrefix = "HP";

    [ObservableProperty]
    private string _mergedFileName = "MergedPlot.pdf";

    [ObservableProperty]
    private double _toleranceBandYRatio = 0.5;

    // Presets
    [ObservableProperty]
    private PlotPreset? _selectedPreset;

    [ObservableProperty]
    private string _presetNameInput = string.Empty;

    // Runtime state & Progress
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isPlotting;

    [ObservableProperty]
    private int _progressCurrent;

    [ObservableProperty]
    private int _progressTotal;

    [ObservableProperty]
    private string _progressText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Sẵn sàng";

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private int _totalCount;

    public SmartPlotViewModel(
        Document? doc = null,
        IFrameProvider? frameProvider = null,
        IAutoCadPlotEngine? plotEngine = null,
        IPlotOrderService? plotOrderService = null,
        IFileNameService? fileNameService = null,
        IPresetService? presetService = null)
    {
        _doc = doc ?? Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
        _frameProvider = frameProvider ?? new AutoCadFrameProvider();
        _plotEngine = plotEngine ?? new AutoCadPlotEngine();
        _plotOrderService = plotOrderService ?? new PlotOrderService();
        _fileNameService = fileNameService ?? new FileNameService();
        _presetService = presetService ?? new PresetService();

        InitializeDefaults();
        InitializeFromDocument();
        _ = LoadPresetsAsync();
    }

    private void InitializeDefaults()
    {
        // Standard PDF devices
        Devices.Add("AutoCAD PDF (General Documentation).pc3");
        Devices.Add("AutoCAD PDF (High Quality Print).pc3");
        Devices.Add("AutoCAD PDF (Smallest File).pc3");
        Devices.Add("AutoCAD PDF (Web and Mobile).pc3");
        Devices.Add("DWG To PDF.pc3");

        // Standard ISO full bleed media sizes
        MediaSizes.Add("ISO_full_bleed_A0_(1189.00_x_841.00_MM)");
        MediaSizes.Add("ISO_full_bleed_A1_(841.00_x_594.00_MM)");
        MediaSizes.Add("ISO_full_bleed_A2_(594.00_x_420.00_MM)");
        MediaSizes.Add("ISO_full_bleed_A3_(420.00_x_297.00_MM)");
        MediaSizes.Add("ISO_full_bleed_A4_(297.00_x_210.00_MM)");

        // Standard plot styles
        PlotStyles.Add("monochrome.ctb");
        PlotStyles.Add("acad.ctb");
        PlotStyles.Add("grayscale.ctb");
        PlotStyles.Add("screening 100%.ctb");
        PlotStyles.Add("acad.stb");

        // Default output folder
        OutputFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    public void InitializeFromDocument()
    {
        if (_doc is null) return;

        try
        {
            using (_doc.LockDocument())
            using (var tr = _doc.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(_doc.Database.BlockTableId, OpenMode.ForRead);
                AvailableBlocks.Clear();
                foreach (ObjectId btrId in bt)
                {
                    var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
                    if (!btr.IsAnonymous && !btr.IsLayout)
                    {
                        AvailableBlocks.Add(btr.Name);
                    }
                }

                var lt = (LayerTable)tr.GetObject(_doc.Database.LayerTableId, OpenMode.ForRead);
                AvailableLayers.Clear();
                foreach (ObjectId ltrId in lt)
                {
                    var ltr = (LayerTableRecord)tr.GetObject(ltrId, OpenMode.ForRead);
                    AvailableLayers.Add(ltr.Name);
                }

                tr.Commit();
            }

            if (string.IsNullOrEmpty(BlockName) && AvailableBlocks.Count > 0)
            {
                var candidate = AvailableBlocks.FirstOrDefault(b =>
                    b.Contains("khung", StringComparison.OrdinalIgnoreCase) ||
                    b.Contains("frame", StringComparison.OrdinalIgnoreCase) ||
                    b.Contains("title", StringComparison.OrdinalIgnoreCase)) ?? AvailableBlocks[0];
                BlockName = candidate;
            }

            if (AvailableLayers.Count > 0 && !AvailableLayers.Contains(LayerName))
            {
                LayerName = AvailableLayers[0];
            }
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Không thể nạp thông tin bản vẽ: {ex.Message}";
        }
    }

    public PlotConfiguration ToConfiguration()
    {
        return new PlotConfiguration
        {
            DeviceName = DeviceName,
            MediaName = MediaName,
            PlotStyle = PlotStyle,
            Orientation = Orientation,
            OutputMode = OutputMode,
            OutputFolder = OutputFolder,
            FileNamePattern = FileNamePattern,
            FileNamePrefix = FileNamePrefix,
            MergedFileName = MergedFileName,
            FrameSource = FrameSource,
            BlockName = BlockName,
            SheetNumberAttribute = SheetNumberAttribute,
            SheetTitleAttribute = SheetTitleAttribute,
            LayerName = LayerName,
            LayoutRange = LayoutRange,
            CenterPlot = CenterPlot,
            FitToPaper = FitToPaper,
            ToleranceBandYRatio = ToleranceBandYRatio
        };
    }

    public void ApplyConfiguration(PlotConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        DeviceName = config.DeviceName;
        MediaName = config.MediaName;
        PlotStyle = config.PlotStyle;
        Orientation = config.Orientation;
        OutputMode = config.OutputMode;
        if (!string.IsNullOrWhiteSpace(config.OutputFolder)) OutputFolder = config.OutputFolder;
        FileNamePattern = config.FileNamePattern;
        FileNamePrefix = config.FileNamePrefix;
        MergedFileName = config.MergedFileName;
        FrameSource = config.FrameSource;
        BlockName = config.BlockName;
        SheetNumberAttribute = config.SheetNumberAttribute;
        SheetTitleAttribute = config.SheetTitleAttribute;
        LayerName = config.LayerName;
        LayoutRange = config.LayoutRange;
        CenterPlot = config.CenterPlot;
        FitToPaper = config.FitToPaper;
        ToleranceBandYRatio = config.ToleranceBandYRatio;
    }

    public void OnEntityPicked(ObjectId id)
    {
        if (_doc is null || id.IsNull) return;

        try
        {
            using (_doc.LockDocument())
            using (var tr = _doc.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(id, OpenMode.ForRead);
                if (ent is BlockReference blkRef)
                {
                    string effectiveName;
                    if (blkRef.IsDynamicBlock)
                    {
                        var btr = (BlockTableRecord)tr.GetObject(blkRef.DynamicBlockTableRecord, OpenMode.ForRead);
                        effectiveName = btr.Name;
                    }
                    else
                    {
                        effectiveName = blkRef.Name;
                    }

                    FrameSource = FrameSourceType.Block;
                    BlockName = effectiveName;
                    StatusMessage = $"Đã chọn khung Block: {effectiveName}";
                }
                else if (ent is Autodesk.AutoCAD.DatabaseServices.Polyline poly)
                {
                    FrameSource = FrameSourceType.Layer;
                    LayerName = poly.Layer;
                    StatusMessage = $"Đã chọn khung Polyline trên Layer: {poly.Layer}";
                }
                else if (ent is Autodesk.AutoCAD.DatabaseServices.Polyline2d poly2d)
                {
                    FrameSource = FrameSourceType.Layer;
                    LayerName = poly2d.Layer;
                    StatusMessage = $"Đã chọn khung Polyline2d trên Layer: {poly2d.Layer}";
                }
                tr.Commit();
            }

            _ = ScanFramesAsync();
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Lỗi khi đọc đối tượng chọn: {ex.Message}";
        }
    }

    [RelayCommand]
    private void PickFrame()
    {
        PickFrameRequested?.Invoke(OnEntityPicked);
    }

    [RelayCommand]
    private async Task ScanFramesAsync()
    {
        if (_doc is null)
        {
            StatusMessage = "Không có bản vẽ nào đang mở.";
            return;
        }

        IsBusy = true;
        IsScanning = true;
        StatusMessage = "Đang quét khung bản vẽ...";
        _cts = new CancellationTokenSource();

        try
        {
            var config = ToConfiguration();
            var options = FrameScanOptions.FromConfiguration(config);
            var items = await _frameProvider.ScanFramesAsync(_doc, FrameSource, options, _cts.Token);

            var sorted = _plotOrderService.Sort(items, ToleranceBandYRatio);

            Items.Clear();
            foreach (var item in sorted)
            {
                var itemVm = new PlotItemViewModel(item);
                itemVm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(PlotItemViewModel.IsSelected))
                    {
                        UpdateCounts();
                    }
                };
                Items.Add(itemVm);
            }

            UpdateCounts();
            StatusMessage = $"Đã tìm thấy {Items.Count} khung bản vẽ.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Đã hủy quét khung.";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Lỗi khi quét khung: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            IsScanning = false;
        }
    }

    [RelayCommand]
    private async Task PlotSelectedAsync()
    {
        await ExecutePlotAsync(onlySelected: true);
    }

    [RelayCommand]
    private async Task PlotAllAsync()
    {
        await ExecutePlotAsync(onlySelected: false);
    }

    private async Task ExecutePlotAsync(bool onlySelected)
    {
        if (_doc is null)
        {
            StatusMessage = "Không có bản vẽ nào đang mở.";
            return;
        }

        var targets = (onlySelected ? Items.Where(i => i.IsSelected) : Items)
            .Select(vm => vm.ToModel())
            .ToList();

        if (targets.Count == 0)
        {
            StatusMessage = "Không có khung nào được chọn để in.";
            return;
        }

        IsBusy = true;
        IsPlotting = true;
        ProgressCurrent = 0;
        ProgressTotal = targets.Count;
        ProgressText = $"0 / {targets.Count}";
        StatusMessage = "Đang khởi tạo tiến trình in...";
        _cts = new CancellationTokenSource();

        var progress = new Progress<PlotProgressUpdate>(update =>
        {
            ProgressCurrent = update.CurrentIndex;
            ProgressTotal = update.TotalCount;
            ProgressText = $"{update.CurrentIndex} / {update.TotalCount}: {update.SheetName}";
            StatusMessage = update.StatusMessage;

            var match = Items.FirstOrDefault(i => i.Model.Id == update.SheetName || i.Model.DisplayName == update.SheetName);
            if (match is not null)
            {
                match.Status = update.IsCompleted ? "Plotted" : "In progress";
            }
        });

        try
        {
            var config = ToConfiguration();
            var result = await _plotEngine.PlotAsync(_doc, config, targets, progress, _cts.Token);

            if (result.Success)
            {
                StatusMessage = $"In hoàn tất ({result.PlottedSheets}/{result.TotalSheets} bản vẽ). Thời gian: {result.ElapsedTime.TotalSeconds:F1}s";
            }
            else
            {
                var errorText = result.Errors.Count > 0 ? string.Join("; ", result.Errors) : "Lỗi không xác định";
                StatusMessage = $"In thất bại: {errorText}";
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Người dùng đã hủy tiến trình in.";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Lỗi khi in: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            IsPlotting = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _cts?.Cancel();
        StatusMessage = "Đang hủy tác vụ...";
    }

    [RelayCommand]
    private void Close()
    {
        CloseRequested?.Invoke();
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var item in Items) item.IsSelected = true;
        UpdateCounts();
    }

    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var item in Items) item.IsSelected = false;
        UpdateCounts();
    }

    [RelayCommand]
    private void InvertSelection()
    {
        foreach (var item in Items) item.IsSelected = !item.IsSelected;
        UpdateCounts();
    }

    [RelayCommand]
    private void AutoSortItems()
    {
        if (Items.Count == 0) return;
        var models = Items.Select(i => i.ToModel()).ToList();
        var sorted = _plotOrderService.Sort(models, ToleranceBandYRatio);
        Items.Clear();
        foreach (var m in sorted)
        {
            var itemVm = new PlotItemViewModel(m);
            itemVm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(PlotItemViewModel.IsSelected))
                {
                    UpdateCounts();
                }
            };
            Items.Add(itemVm);
        }
        UpdateCounts();
    }

    [RelayCommand]
    private void MoveItemUp(PlotItemViewModel? item)
    {
        if (item is null) return;
        int idx = Items.IndexOf(item);
        if (idx > 0)
        {
            Items.Move(idx, idx - 1);
            ReindexItems();
        }
    }

    [RelayCommand]
    private void MoveItemDown(PlotItemViewModel? item)
    {
        if (item is null) return;
        int idx = Items.IndexOf(item);
        if (idx >= 0 && idx < Items.Count - 1)
        {
            Items.Move(idx, idx + 1);
            ReindexItems();
        }
    }

    private void ReindexItems()
    {
        for (int i = 0; i < Items.Count; i++)
        {
            Items[i].Order = i + 1;
        }
    }

    [RelayCommand]
    private void BrowseOutputFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Chọn thư mục xuất PDF",
            InitialDirectory = Directory.Exists(OutputFolder) ? OutputFolder : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (dialog.ShowDialog() == true)
        {
            OutputFolder = dialog.FolderName;
        }
    }

    [RelayCommand]
    private async Task LoadPresetsAsync()
    {
        try
        {
            var collection = await _presetService.LoadPresetsAsync();
            Presets.Clear();
            foreach (var p in collection.Presets)
            {
                Presets.Add(p);
            }
            if (SelectedPreset is null && Presets.Count > 0)
            {
                SelectedPreset = Presets[0];
            }
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Không thể tải cấu hình mẫu: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ApplyPreset()
    {
        if (SelectedPreset is not null)
        {
            ApplyConfiguration(SelectedPreset.Config);
            StatusMessage = $"Đã nạp cấu hình mẫu '{SelectedPreset.Name}'.";
        }
    }

    [RelayCommand]
    private async Task SavePresetAsync()
    {
        if (string.IsNullOrWhiteSpace(PresetNameInput))
        {
            StatusMessage = "Vui lòng nhập tên cấu hình mẫu cần lưu.";
            return;
        }

        try
        {
            var config = ToConfiguration();
            var preset = new PlotPreset
            {
                Name = PresetNameInput.Trim(),
                Config = config
            };

            var existing = Presets.FirstOrDefault(p => string.Equals(p.Name, preset.Name, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                Presets.Remove(existing);
            }
            Presets.Add(preset);
            SelectedPreset = preset;

            await _presetService.SavePresetsAsync(new PlotPresetCollection { Presets = Presets.ToList() });
            StatusMessage = $"Đã lưu cấu hình mẫu '{preset.Name}'.";
            PresetNameInput = string.Empty;
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Lỗi khi lưu cấu hình mẫu: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeletePresetAsync()
    {
        if (SelectedPreset is null) return;

        try
        {
            var name = SelectedPreset.Name;
            Presets.Remove(SelectedPreset);
            SelectedPreset = Presets.FirstOrDefault();

            await _presetService.SavePresetsAsync(new PlotPresetCollection { Presets = Presets.ToList() });
            StatusMessage = $"Đã xóa cấu hình mẫu '{name}'.";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Lỗi khi xóa cấu hình mẫu: {ex.Message}";
        }
    }

    public void UpdateCounts()
    {
        SelectedCount = Items.Count(i => i.IsSelected);
        TotalCount = Items.Count;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
