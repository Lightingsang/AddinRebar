using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPKata.Core.DrawingLayout.Calculators;
using HPKata.Core.DrawingLayout.Models;

namespace HPKata.UI.ViewModels
{
    public sealed partial class DrawingUtilityViewModel : ObservableObject
    {
        // Grid System Properties
        [ObservableProperty] private string _xSpansText = "4000, 5000, 6000, 4000";
        [ObservableProperty] private string _ySpansText = "5000, 6000, 5000";
        [ObservableProperty] private double _scale = 100.0;
        [ObservableProperty] private string _gridStatusMessage = "Sẵn sàng sinh lưới trục.";

        // Sheet Arrangement Properties
        [ObservableProperty] private SheetFormat _selectedFormat = SheetFormat.A1;
        [ObservableProperty] private bool _isLandscape = true;
        [ObservableProperty] private string _sheetStatusMessage = "Sẵn sàng sắp xếp bản vẽ.";

        // Drawing Register Properties
        [ObservableProperty] private string _indexStatusMessage = "Sẵn sàng lập danh mục.";

        public ObservableCollection<GridLineModel> GeneratedAxes { get; } = new ObservableCollection<GridLineModel>();
        public ObservableCollection<DrawingBlockItem> BlocksToArrange { get; } = new ObservableCollection<DrawingBlockItem>();
        public ObservableCollection<ArrangedSheetResult> ArrangedSheets { get; } = new ObservableCollection<ArrangedSheetResult>();
        public ObservableCollection<DrawingIndexItem> IndexItems { get; } = new ObservableCollection<DrawingIndexItem>();
        [ObservableProperty] private DrawingIndexTableDto? _generatedTable;

        public DrawingUtilityViewModel()
        {
            LoadSampleBlocks();
            LoadSampleIndex();
        }

        [RelayCommand]
        public void GenerateGrid()
        {
            var xSpans = ParseSpans(XSpansText);
            var ySpans = ParseSpans(YSpansText);

            var system = GridAxisGenerator.GenerateOrthogonal(xSpans, ySpans, Scale);
            GeneratedAxes.Clear();
            foreach (var ax in system.XAxes.Concat(system.YAxes))
            {
                GeneratedAxes.Add(ax);
            }

            GridStatusMessage = $"Đã sinh {system.XAxes.Count} trục X và {system.YAxes.Count} trục Y (Tỷ lệ 1:{Scale}).";
        }

        [RelayCommand]
        public void ArrangeSheets()
        {
            var sheetDef = SheetSizeDefinition.Create(SelectedFormat, IsLandscape);
            var results = SheetArrangerCalculator.ArrangeBlocks(BlocksToArrange, sheetDef);

            ArrangedSheets.Clear();
            foreach (var res in results)
            {
                ArrangedSheets.Add(res);
            }

            SheetStatusMessage = $"Đã sắp xếp {BlocksToArrange.Count} chi tiết lên {ArrangedSheets.Count} bản vẽ khổ {SelectedFormat}.";
        }

        [RelayCommand]
        public void GenerateIndex()
        {
            GeneratedTable = DrawingIndexAggregator.BuildTable(IndexItems);
            IndexStatusMessage = $"Đã tổng hợp danh mục {GeneratedTable.TotalSheets} bản vẽ. Cảnh báo: {GeneratedTable.Warnings.Count}.";
        }

        private static List<double> ParseSpans(string input)
        {
            var list = new List<double>();
            if (string.IsNullOrWhiteSpace(input)) return list;

            var parts = input.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (double.TryParse(part.Trim(), out double val) && val > 0)
                {
                    list.Add(val);
                }
            }
            return list.Count > 0 ? list : new List<double> { 5000.0 };
        }

        public void LoadSampleBlocks()
        {
            BlocksToArrange.Clear();
            BlocksToArrange.Add(new DrawingBlockItem("Mặt bằng định vị dầm sàn", 350, 250));
            BlocksToArrange.Add(new DrawingBlockItem("Chi tiết dầm D1 nhịp 1-3", 400, 150));
            BlocksToArrange.Add(new DrawingBlockItem("Chi tiết dầm D2 nhịp A-C", 380, 140));
            BlocksToArrange.Add(new DrawingBlockItem("Bảng thống kê cốt thép dầm", 250, 300));
        }

        public void LoadSampleIndex()
        {
            IndexItems.Clear();
            IndexItems.Add(new DrawingIndexItem(1, "KC-01", "Ghi chú chung và quy cách nối cốt thép"));
            IndexItems.Add(new DrawingIndexItem(2, "KC-02", "Mặt bằng móng và chi tiết đài cọc"));
            IndexItems.Add(new DrawingIndexItem(3, "KC-03", "Mặt bằng định vị cột tầng 1"));
            IndexItems.Add(new DrawingIndexItem(4, "KC-04", "Chi tiết kết cấu dầm sàn tầng 2"));
        }
    }
}
