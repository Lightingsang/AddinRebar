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
    public sealed partial class WallDetailingViewModel
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
                ExcelStatus = "Đang đọc sheet 'Vach'...";
                string path = KataInstallEnvironment.Current.KataWorkbookPath;
                if (_excelAdapter.ReadWallSheet(path, out var data))
                {
                    PopulateFromSheetData(data);
                    ExcelStatus = $"Đã đọc vách '{WallMark}' ({Openings.Count} lỗ mở) từ Kata.xlsm";
                }
                else
                {
                    ExcelStatus = "Không thể đọc dữ liệu từ sheet 'Vach'";
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
                ExcelStatus = "Đang lưu vào sheet 'Vach'...";
                string path = KataInstallEnvironment.Current.KataWorkbookPath;
                var data = BuildSheetData();
                if (_excelAdapter.WriteWallSheet(path, in data))
                {
                    ExcelStatus = $"Đã lưu vách '{WallMark}' vào Kata.xlsm";
                }
                else
                {
                    ExcelStatus = "Không thể lưu vào sheet 'Vach'";
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

        private void PopulateFromSheetData(KataWallSheetData data)
        {
            WallMark = data.WallMark;
            ShapeType = data.ShapeType;
            ThicknessMm = data.ThicknessMm;
            LengthMm = data.LengthMm;
            StoryHeightMm = data.StoryHeightMm;
            StoryCount = data.StoryCount;
            ConcreteGrade = data.ConcreteGrade;
            ConcreteCoverMm = data.ConcreteCoverMm;
            HasBoundaryElements = data.HasBoundaryElements;
            BoundaryLengthMm = data.BoundaryLengthMm;
            BoundaryBarCount = data.BoundaryBarCount;
            BoundaryBarDiameter = data.BoundaryBarDiameter;
            BoundaryTieDiameter = data.BoundaryTieDiameter;
            BoundaryTieSpacing = data.BoundaryTieSpacing;
            IsDoubleCurtain = data.IsDoubleCurtain;
            WebVerticalDiameter = data.WebVerticalDiameter;
            WebVerticalSpacing = data.WebVerticalSpacing;
            WebHorizontalDiameter = data.WebHorizontalDiameter;
            WebHorizontalSpacing = data.WebHorizontalSpacing;
            CrossTieDiameter = data.CrossTieDiameter;
            HasUBarEdgeCaps = data.HasUBarEdgeCaps;

            Openings.Clear();
            foreach (var op in data.Openings)
            {
                Openings.Add(new WallOpeningViewModel
                {
                    OpeningMark = op.OpeningMark,
                    XOffsetMm = op.XOffsetMm,
                    YOffsetMm = op.YOffsetMm,
                    WidthMm = op.WidthMm,
                    HeightMm = op.HeightMm,
                    HasDiagonalBars = op.HasDiagonalBars,
                    DiagonalDiameter = op.DiagonalDiameter,
                    JambExtraDiameter = op.JambExtraDiameter,
                    HeaderExtraDiameter = op.HeaderExtraDiameter
                });
            }
        }

        private KataWallSheetData BuildSheetData()
        {
            var data = new KataWallSheetData
            {
                WallMark = WallMark,
                ShapeType = ShapeType,
                ThicknessMm = ThicknessMm,
                LengthMm = LengthMm,
                StoryHeightMm = StoryHeightMm,
                StoryCount = StoryCount,
                ConcreteGrade = ConcreteGrade,
                ConcreteCoverMm = ConcreteCoverMm,
                HasBoundaryElements = HasBoundaryElements,
                BoundaryLengthMm = BoundaryLengthMm,
                BoundaryBarCount = BoundaryBarCount,
                BoundaryBarDiameter = BoundaryBarDiameter,
                BoundaryTieDiameter = BoundaryTieDiameter,
                BoundaryTieSpacing = BoundaryTieSpacing,
                IsDoubleCurtain = IsDoubleCurtain,
                WebVerticalDiameter = WebVerticalDiameter,
                WebVerticalSpacing = WebVerticalSpacing,
                WebHorizontalDiameter = WebHorizontalDiameter,
                WebHorizontalSpacing = WebHorizontalSpacing,
                CrossTieDiameter = CrossTieDiameter,
                HasUBarEdgeCaps = HasUBarEdgeCaps
            };

            foreach (var op in Openings)
            {
                data.Openings.Add(new KataWallOpeningData
                {
                    OpeningMark = op.OpeningMark,
                    XOffsetMm = op.XOffsetMm,
                    YOffsetMm = op.YOffsetMm,
                    WidthMm = op.WidthMm,
                    HeightMm = op.HeightMm,
                    HasDiagonalBars = op.HasDiagonalBars,
                    DiagonalDiameter = op.DiagonalDiameter,
                    JambExtraDiameter = op.JambExtraDiameter,
                    HeaderExtraDiameter = op.HeaderExtraDiameter
                });
            }

            return data;
        }
    }
}
