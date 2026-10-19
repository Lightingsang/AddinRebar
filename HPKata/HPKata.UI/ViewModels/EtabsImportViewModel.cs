using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPKata.Core.Beams.Calculators;
using HPKata.Core.Beams.Etabs;
using HPKata.Core.Beams.Models;

namespace HPKata.UI.ViewModels
{
    /// <summary>
    /// ViewModel for importing, optimizing, and batch generating rebar from ETABS frame design tables.
    /// </summary>
    public sealed partial class EtabsImportViewModel : ObservableObject
    {
        public ObservableCollection<EtabsBeamRowViewModel> Beams { get; } = new();
        public ObservableCollection<string> Stories { get; } = new();

        [ObservableProperty] private string _tableInputText = string.Empty;
        [ObservableProperty] private string _selectedStory = "All";
        [ObservableProperty] private string _beamSearchFilter = string.Empty;
        [ObservableProperty] private double _maxWastePercent = 15.0;
        [ObservableProperty] private string _statusMessage = "Sẵn sàng nhập dữ liệu bảng ETABS.";

        public event Action? RequestClose;
        public List<BeamRebarLayout> ResultLayouts { get; } = new();

        public EtabsImportViewModel()
        {
            Stories.Add("All");
        }

        [RelayCommand]
        private void ParseTableText()
        {
            if (string.IsNullOrWhiteSpace(TableInputText))
            {
                StatusMessage = "Vui lòng dán nội dung bảng ETABS trước khi phân tích.";
                return;
            }

            Beams.Clear();
            Stories.Clear();
            Stories.Add("All");

            var rows = EtabsTextTableParser.ParseDelimitedRows(TableInputText);
            var mappedFrames = EtabsDesignAreaMapper.MapDesignTable(rows);

            var storySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in mappedFrames)
            {
                var frame = kvp.Value;
                if (!string.IsNullOrWhiteSpace(frame.Story))
                {
                    storySet.Add(frame.Story);
                }

                var rowVm = new EtabsBeamRowViewModel
                {
                    IsSelected = true,
                    StoryName = frame.Story,
                    RunName = frame.FrameName,
                    Dimensions = $"{frame.WidthMm:F0}x{frame.HeightMm:F0}",
                    LengthM = Math.Round(frame.LengthMm / 1000.0, 2),
                    FrameDesign = frame
                };

                // Auto-run initial optimizer
                OptimizeRowRebar(rowVm, frame);
                Beams.Add(rowVm);
            }

            foreach (var s in storySet)
            {
                Stories.Add(s);
            }

            StatusMessage = $"Đã nạp thành công {Beams.Count} dầm từ bảng ETABS.";
        }

        [RelayCommand]
        private void AutoSelectRebar()
        {
            int updated = 0;
            foreach (var row in Beams)
            {
                if (!row.IsSelected || row.FrameDesign == null) continue;
                OptimizeRowRebar(row, row.FrameDesign);
                updated++;
            }
            StatusMessage = $"Đã tối ưu cốt thép TCVN 5574 cho {updated} dầm được chọn.";
        }

        private void OptimizeRowRebar(EtabsBeamRowViewModel row, EtabsFrameDesign frame)
        {
            double maxTopAs = Math.Max(frame.AsTopLeftRequired, Math.Max(frame.AsTopMidRequired, frame.AsTopRightRequired));
            double maxBotAs = Math.Max(frame.AsBotLeftRequired, Math.Max(frame.AsBotMidRequired, frame.AsBotRightRequired));

            var topOpt = TcvnBeamRebarOptimizer.Optimize(maxTopAs, frame.WidthMm, isTopBar: true);
            var botOpt = TcvnBeamRebarOptimizer.Optimize(maxBotAs, frame.WidthMm, isTopBar: false);

            row.TopRebarText = topOpt.Notation;
            row.BotRebarText = botOpt.Notation;
            row.IsCompliant = topOpt.IsCompliant && botOpt.IsCompliant;
            row.StatusText = row.IsCompliant ? "Đạt TCVN" : "Cần 2 lớp thép";
        }

        [RelayCommand]
        private void ApplyImport()
        {
            ResultLayouts.Clear();
            int index = 0;

            foreach (var row in Beams)
            {
                if (!row.IsSelected || row.FrameDesign == null) continue;
                var layout = EtabsBeamImportService.ConvertFrameToBeamLayout(row.FrameDesign, index++);
                ResultLayouts.Add(layout);
            }

            RequestClose?.Invoke();
        }

        [RelayCommand]
        private void Cancel()
        {
            ResultLayouts.Clear();
            RequestClose?.Invoke();
        }
    }
}
