using System;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPKata.Core.Excel;
using HPKata.Core.Excel.Data;
using HPKata.Core.Excel.Session;

namespace HPKata.UI.ViewModels
{
    public sealed partial class BeamDetailingViewModel
    {
        private readonly IKataExcelAdapter _excelAdapter;
        private KataBeamSheetData? _lastSheetData;

        [ObservableProperty] private bool _autoSyncOnApply;
        [ObservableProperty] private string _excelStatus = "Kata.xlsm: Sẵn sàng";
        [ObservableProperty] private bool _isExcelBusy;

        [RelayCommand]
        public void OpenExcel()
        {
            try
            {
                string path = KataExcelSessionProvider.ResolveBestWorkbookPath(HPKata.Core.Configuration.KataInstallEnvironment.Current.KataWorkbookPath);
                if (!File.Exists(path))
                {
                    ExcelStatus = $"Không tìm thấy: {Path.GetFileName(path)}";
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
                ExcelStatus = "Đã mở Kata.xlsm trên Excel";
            }
            catch (Exception ex)
            {
                ExcelStatus = $"Lỗi mở Excel: {ex.Message}";
            }
        }

        [RelayCommand]
        public void ReadExcel()
        {
            try
            {
                IsExcelBusy = true;
                ExcelStatus = "Đang đọc sheet 'Dam'...";
                string path = KataExcelSessionProvider.ResolveBestWorkbookPath(null);
                if (_excelAdapter.ReadBeamSheet(path, out var data))
                {
                    _lastSheetData = data;
                    PopulateFromSheetData(data);
                    ExcelStatus = $"Đã đọc {Spans.Count} nhịp dầm '{BeamMark}' từ Kata.xlsm";
                }
                else
                {
                    ExcelStatus = "Không thể đọc dữ liệu từ sheet 'Dam'";
                }
            }
            catch (Exception ex)
            {
                ExcelStatus = $"Lỗi đọc: {ex.Message}";
            }
            finally
            {
                IsExcelBusy = false;
            }
        }

        [RelayCommand]
        public void WriteExcel()
        {
            try
            {
                IsExcelBusy = true;
                ExcelStatus = "Đang lưu vào sheet 'Dam'...";
                string path = KataExcelSessionProvider.ResolveBestWorkbookPath(null);
                var data = BuildSheetData();
                if (_excelAdapter.WriteBeamSheet(path, in data))
                {
                    _lastSheetData = data;
                    ExcelStatus = $"Đã lưu dầm '{BeamMark}' ({Spans.Count} nhịp) vào Kata.xlsm";
                }
                else
                {
                    ExcelStatus = "Không thể lưu vào sheet 'Dam'";
                }
            }
            catch (Exception ex)
            {
                ExcelStatus = $"Lỗi ghi: {ex.Message}";
            }
            finally
            {
                IsExcelBusy = false;
            }
        }

        private void PopulateFromSheetData(KataBeamSheetData data)
        {
            BeamMark = data.BeamMark;
            StoryName = data.StoryName;
            ConcreteGrade = data.ConcreteGrade;
            MainSteelGrade = data.MainSteelGrade;
            StirrupSteelGrade = data.StirrupSteelGrade;
            ConcreteCoverMm = data.ConcreteCoverMm;
            IsShopMode = data.IsShopMode;
            TopContinuousCount = data.TopContinuousCount;
            TopContinuousDiameter = data.TopContinuousDiameter;
            BotContinuousCount = data.BotContinuousCount;
            BotContinuousDiameter = data.BotContinuousDiameter;
            HasSideBars = data.HasSideBars;
            SideBarCount = data.SideBarCount;
            SideBarDiameter = data.SideBarDiameter;
            StirrupDiameter = data.StirrupDiameter;
            DenseSpacing = data.DenseSpacing;
            SparseSpacing = data.SparseSpacing;
            LegCount = data.LegCount;

            Spans.Clear();
            foreach (var s in data.Spans)
            {
                Spans.Add(new BeamSpanViewModel
                {
                    SpanIndex = s.SpanIndex,
                    SpanName = s.SpanName,
                    ClearLengthMm = s.ClearLengthMm,
                    WidthMm = s.WidthMm,
                    HeightMm = s.HeightMm,
                    LeftSupportWidthMm = s.LeftSupportWidthMm,
                    RightSupportWidthMm = s.RightSupportWidthMm,
                    IsCantileverLeft = s.IsCantileverLeft,
                    IsCantileverRight = s.IsCantileverRight
                });
            }
        }

        private KataBeamSheetData BuildSheetData()
        {
            var data = _lastSheetData ?? new KataBeamSheetData();
            data.BeamMark = BeamMark;
            data.StoryName = StoryName;
            data.ConcreteGrade = ConcreteGrade;
            data.MainSteelGrade = MainSteelGrade;
            data.StirrupSteelGrade = StirrupSteelGrade;
            data.ConcreteCoverMm = ConcreteCoverMm;
            data.IsShopMode = IsShopMode;
            data.TopContinuousCount = TopContinuousCount;
            data.TopContinuousDiameter = TopContinuousDiameter;
            data.BotContinuousCount = BotContinuousCount;
            data.BotContinuousDiameter = BotContinuousDiameter;
            data.HasSideBars = HasSideBars;
            data.SideBarCount = SideBarCount;
            data.SideBarDiameter = SideBarDiameter;
            data.SkinBarDiameterMm = SideBarDiameter;
            data.SkinBarLayers = HasSideBars ? Math.Max(1, SideBarCount / 2) : 0;
            data.StirrupDiameter = StirrupDiameter;
            data.DenseSpacing = DenseSpacing;
            data.SparseSpacing = SparseSpacing;
            data.LegCount = LegCount;
            data.TopContinuousRebar = $"{TopContinuousCount}f{TopContinuousDiameter}";
            data.BotContinuousRebar = $"{BotContinuousCount}f{BotContinuousDiameter}";
            data.StirrupNearSupport = $"a{(int)Math.Round(DenseSpacing)}";
            data.StirrupMidSpan = $"a{(int)Math.Round(SparseSpacing)}";

            for (int i = 0; i < Spans.Count; i++)
            {
                var span = Spans[i];
                if (i < data.Spans.Count)
                {
                    data.Spans[i].SpanName = span.SpanName;
                    data.Spans[i].ClearLengthMm = span.ClearLengthMm;
                    data.Spans[i].WidthMm = span.WidthMm;
                    data.Spans[i].HeightMm = span.HeightMm;
                    data.Spans[i].LeftSupportWidthMm = span.LeftSupportWidthMm;
                    data.Spans[i].RightSupportWidthMm = span.RightSupportWidthMm;
                    data.Spans[i].IsCantileverLeft = span.IsCantileverLeft;
                    data.Spans[i].IsCantileverRight = span.IsCantileverRight;
                }
                else
                {
                    data.Spans.Add(new KataBeamSpanData
                    {
                        SpanIndex = span.SpanIndex,
                        SpanName = span.SpanName,
                        ClearLengthMm = span.ClearLengthMm,
                        WidthMm = span.WidthMm,
                        HeightMm = span.HeightMm,
                        LeftSupportWidthMm = span.LeftSupportWidthMm,
                        RightSupportWidthMm = span.RightSupportWidthMm,
                        IsCantileverLeft = span.IsCantileverLeft,
                        IsCantileverRight = span.IsCantileverRight
                    });
                }
            }

            while (data.Spans.Count > Spans.Count)
            {
                data.Spans.RemoveAt(data.Spans.Count - 1);
            }

            if (data.Supports.Count == 0 && data.Spans.Count > 0)
            {
                for (int i = 0; i <= data.Spans.Count; i++)
                {
                    double w = i < data.Spans.Count ? data.Spans[i].LeftSupportWidthMm : data.Spans[data.Spans.Count - 1].RightSupportWidthMm;
                    data.Supports.Add(new KataBeamSupportData
                    {
                        Index = i,
                        Name = $"Gối {i + 1}",
                        WidthMm = w > 0 ? w : 400.0,
                        ColumnGridAxis = (i + 1).ToString()
                    });
                }
            }

            return data;
        }
    }
}
