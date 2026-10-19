using System;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPKata.Core.Configuration;
using HPKata.Core.Excel;
using HPKata.Core.Excel.Data;

namespace HPKata.UI.ViewModels
{
    public sealed partial class ColumnDetailingViewModel
    {
        private readonly IKataExcelAdapter _excelAdapter;

        [ObservableProperty] private bool _autoSyncOnApply;
        [ObservableProperty] private string _excelStatus = "Kata.xlsm: Sẵn sàng";
        [ObservableProperty] private bool _isExcelBusy;

        [RelayCommand]
        public void OpenExcel()
        {
            try
            {
                string path = KataInstallEnvironment.Current.KataWorkbookPath;
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
                ExcelStatus = "Đang đọc sheet 'Cot'...";
                string path = KataInstallEnvironment.Current.KataWorkbookPath;
                if (_excelAdapter.ReadColumnSheet(path, out var data))
                {
                    PopulateFromSheetData(data);
                    ExcelStatus = $"Đã đọc {Stories.Count} tầng cột '{ColumnMark}' từ Kata.xlsm";
                }
                else
                {
                    ExcelStatus = "Không thể đọc dữ liệu từ sheet 'Cot'";
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
                ExcelStatus = "Đang lưu vào sheet 'Cot'...";
                string path = KataInstallEnvironment.Current.KataWorkbookPath;
                var data = BuildSheetData();
                if (_excelAdapter.WriteColumnSheet(path, in data))
                {
                    ExcelStatus = $"Đã lưu cột '{ColumnMark}' ({Stories.Count} tầng) vào Kata.xlsm";
                }
                else
                {
                    ExcelStatus = "Không thể lưu vào sheet 'Cot'";
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

        private void PopulateFromSheetData(KataColumnSheetData data)
        {
            ColumnMark = data.ColumnMark;
            ConcreteGrade = data.ConcreteGrade;
            MainSteelGrade = data.MainSteelGrade;
            TieSteelGrade = data.TieSteelGrade;
            ConcreteCoverMm = data.ConcreteCoverMm;
            IsShopMode = data.IsShopMode;
            SpliceMethod = data.SpliceMethod;
            LapLengthFactor = data.LapLengthFactor;
            StaggerOffsetMm = data.StaggerOffsetMm;
            MaxCrankStepDownMm = data.MaxCrankStepDownMm;
            TieDiameter = data.TieDiameter;
            DenseSpacing = data.DenseSpacing;
            SparseSpacing = data.SparseSpacing;
            InnerHoopType = data.InnerHoopType;

            Stories.Clear();
            foreach (var s in data.Stories)
            {
                Stories.Add(new ColumnStoryRowViewModel
                {
                    StoryIndex = s.StoryIndex,
                    StoryName = s.StoryName,
                    StoryHeightMm = s.StoryHeightMm,
                    BeamDepthMm = s.BeamDepthMm,
                    WidthMm = s.WidthMm,
                    DepthMm = s.DepthMm,
                    CornerDiameter = s.CornerDiameter,
                    SideCountB = s.SideCountB,
                    SideDiameterB = s.SideDiameterB,
                    SideCountH = s.SideCountH,
                    SideDiameterH = s.SideDiameterH,
                    DenseSpacing = s.DenseSpacing,
                    SparseSpacing = s.SparseSpacing
                });
            }
        }

        private KataColumnSheetData BuildSheetData()
        {
            var data = new KataColumnSheetData
            {
                ColumnMark = ColumnMark,
                ConcreteGrade = ConcreteGrade,
                MainSteelGrade = MainSteelGrade,
                TieSteelGrade = TieSteelGrade,
                ConcreteCoverMm = ConcreteCoverMm,
                IsShopMode = IsShopMode,
                SpliceMethod = SpliceMethod,
                LapLengthFactor = LapLengthFactor,
                StaggerOffsetMm = StaggerOffsetMm,
                MaxCrankStepDownMm = MaxCrankStepDownMm,
                TieDiameter = TieDiameter,
                DenseSpacing = DenseSpacing,
                SparseSpacing = SparseSpacing,
                InnerHoopType = InnerHoopType
            };

            foreach (var s in Stories)
            {
                data.Stories.Add(new KataColumnStoryData
                {
                    StoryIndex = s.StoryIndex,
                    StoryName = s.StoryName,
                    StoryHeightMm = s.StoryHeightMm,
                    BeamDepthMm = s.BeamDepthMm,
                    WidthMm = s.WidthMm,
                    DepthMm = s.DepthMm,
                    CornerDiameter = s.CornerDiameter,
                    SideCountB = s.SideCountB,
                    SideDiameterB = s.SideDiameterB,
                    SideCountH = s.SideCountH,
                    SideDiameterH = s.SideDiameterH,
                    DenseSpacing = s.DenseSpacing,
                    SparseSpacing = s.SparseSpacing
                });
            }

            return data;
        }
    }
}
