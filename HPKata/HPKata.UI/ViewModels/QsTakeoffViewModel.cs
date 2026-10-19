using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPKata.Core.QuantitySurveying.Models;
using HPKata.Core.QuantitySurveying.Persistence;

namespace HPKata.UI.ViewModels
{
    public sealed partial class QsTakeoffViewModel : ObservableObject
    {
        [ObservableProperty] private bool _deductIntersections = true;
        [ObservableProperty] private double _totalVolumeM3;
        [ObservableProperty] private double _totalFormworkM2;
        [ObservableProperty] private double _footingVolumeM3;
        [ObservableProperty] private double _columnVolumeM3;
        [ObservableProperty] private double _wallVolumeM3;
        [ObservableProperty] private double _beamVolumeM3;
        [ObservableProperty] private double _slabVolumeM3;
        [ObservableProperty] private double _stairVolumeM3;
        [ObservableProperty] private string _statusMessage = "Sẵn sàng tính toán khối lượng.";

        public ObservableCollection<QsTakeoffItem> Items { get; } = new ObservableCollection<QsTakeoffItem>();
        private readonly List<QsElementModel> _rawElements = new List<QsElementModel>();

        public QsTakeoffViewModel()
        {
            LoadSampleData();
            Recalculate();
        }

        public void SetElements(IEnumerable<QsElementModel> elements)
        {
            _rawElements.Clear();
            if (elements != null)
            {
                _rawElements.AddRange(elements);
            }
            Recalculate();
        }

        [RelayCommand]
        public void Recalculate()
        {
            var summary = QsScheduleAggregator.Aggregate(_rawElements);
            Items.Clear();
            foreach (var it in summary.Items)
            {
                Items.Add(it);
            }

            TotalVolumeM3 = summary.TotalVolumeM3;
            TotalFormworkM2 = summary.TotalFormworkM2;
            FootingVolumeM3 = summary.FootingVolumeM3;
            ColumnVolumeM3 = summary.ColumnVolumeM3;
            WallVolumeM3 = summary.WallVolumeM3;
            BeamVolumeM3 = summary.BeamVolumeM3;
            SlabVolumeM3 = summary.SlabVolumeM3;
            StairVolumeM3 = summary.StairVolumeM3;

            StatusMessage = $"Đã tính toán {Items.Count} cấu kiện. Tổng bê tông: {TotalVolumeM3:F2} m³, Cốp pha: {TotalFormworkM2:F2} m²";
        }

        [RelayCommand]
        public void ExportExcel(string? targetPath = null)
        {
            string path = targetPath ?? Path.Combine(Path.GetTempPath(), $"Kata_QS_{Guid.NewGuid():N}.xlsx");
            var summary = QsScheduleAggregator.Aggregate(_rawElements);
            QsWorkbookGenerator.ExportToFile(summary, path);
            StatusMessage = $"Đã xuất bảng khối lượng thành công: {path}";
        }

        public void LoadSampleData()
        {
            _rawElements.Clear();
            var col1 = new QsElementModel
            {
                Id = "C1", Mark = "C1", MemberType = QsMemberType.Column,
                WidthMm = 400, HeightMm = 400, LengthMm = 3600, MemberCount = 4
            };
            var beam1 = new QsElementModel
            {
                Id = "D1", Mark = "D1", MemberType = QsMemberType.Beam,
                WidthMm = 250, HeightMm = 500, LengthMm = 6000, MemberCount = 2,
                IntersectingElementIds = new List<string> { "C1" }
            };
            var slab1 = new QsElementModel
            {
                Id = "S1", Mark = "S1", MemberType = QsMemberType.Slab,
                LengthMm = 6000, WidthMm = 4000, ThicknessMm = 120, MemberCount = 1,
                IntersectingElementIds = new List<string> { "C1", "D1" }
            };

            _rawElements.Add(col1);
            _rawElements.Add(beam1);
            _rawElements.Add(slab1);
        }
    }
}
