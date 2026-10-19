using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPKata.Core.Columns.Etabs;
using HPKata.Core.Columns.Models;
using HPKata.Core.Walls.Etabs;
using HPKata.Core.Walls.Models;

namespace HPKata.UI.ViewModels
{
    /// <summary>
    /// ViewModel for importing ETABS column design and wall pier design tables.
    /// </summary>
    public sealed partial class EtabsColumnWallImportViewModel : ObservableObject
    {
        [ObservableProperty] private string _rawTableText = string.Empty;
        [ObservableProperty] private string _importType = "Column";
        [ObservableProperty] private int _importedCount;
        [ObservableProperty] private string _statusMessage = "Dán bảng ETABS vào đây và bấm 'Phân tích dữ liệu'";

        public List<ColumnRebarLayout> ResultColumns { get; } = new();
        public List<ShearWallModel> ResultWalls { get; } = new();

        public event Action? RequestClose;

        [RelayCommand]
        private void Parse()
        {
            if (string.IsNullOrWhiteSpace(RawTableText))
            {
                StatusMessage = "Nội dung bảng trống.";
                return;
            }

            if (ImportType.Equals("Column", StringComparison.OrdinalIgnoreCase))
            {
                ParseColumns();
            }
            else
            {
                ParseWalls();
            }
        }

        private void ParseColumns()
        {
            var service = new EtabsColumnImportService();
            var cols = service.ImportFromTableText(RawTableText);
            ResultColumns.Clear();
            ResultColumns.AddRange(cols);
            ImportedCount = ResultColumns.Count;
            StatusMessage = $"Đã nhận diện thành công {ImportedCount} cột.";
        }

        private void ParseWalls()
        {
            var service = new EtabsWallImportService();
            var walls = service.ImportFromTableText(RawTableText);
            ResultWalls.Clear();
            ResultWalls.AddRange(walls);
            ImportedCount = ResultWalls.Count;
            StatusMessage = $"Đã nhận diện thành công {ImportedCount} vách/lõi.";
        }

        [RelayCommand]
        private void Apply()
        {
            if (ImportedCount == 0)
            {
                Parse();
            }
            RequestClose?.Invoke();
        }
    }
}
