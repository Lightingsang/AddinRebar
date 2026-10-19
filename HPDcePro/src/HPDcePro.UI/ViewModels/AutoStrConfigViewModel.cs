using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPDcePro.Core.AutoStr;
using HPDcePro.Core.Serialization;

namespace HPDcePro.UI.ViewModels
{
    public sealed partial class FloorItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _storyIndex = 1;

        [ObservableProperty]
        private string _tenTang = "TẦNG 1";

        [ObservableProperty]
        private string _kyHieuTang = "T1";

        [ObservableProperty]
        private double _chieuCaoTang = 3600.0;

        [ObservableProperty]
        private double _caoDoTang = 3600.0;

        [ObservableProperty]
        private bool _suDung = true;

        public FloorItemViewModel() { }

        public FloorItemViewModel(int index, string name, string code, double height, double elev)
        {
            StoryIndex = index;
            TenTang = name;
            KyHieuTang = code;
            ChieuCaoTang = height;
            CaoDoTang = elev;
            SuDung = true;
        }

        public AutoStrFloorInfo ToDomain() => new()
        {
            StoryIndex = StoryIndex,
            TenTang = TenTang,
            KyHieuTang = KyHieuTang,
            ChieuCaoTang = ChieuCaoTang,
            CaoDoTang = CaoDoTang,
            SuDung = SuDung
        };
    }

    public sealed partial class StepItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _stepNumber;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private bool _isEnabled = true;

        public StepItemViewModel(int num, string name, bool enabled = true)
        {
            StepNumber = num;
            Name = name;
            IsEnabled = enabled;
        }
    }

    public sealed partial class AutoStrConfigViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _tenDuAn = "Dự án mẫu HPDcePro";

        [ObservableProperty]
        private string _tenCongTrinh = "Tòa nhà hỗn hợp thương mại";

        [ObservableProperty]
        private string _diaChiCongTrinh = "Hà Nội";

        [ObservableProperty]
        private string _chuDauTu = "Chủ đầu tư";

        [ObservableProperty]
        private string _macBtCotVach = "B30";

        [ObservableProperty]
        private string _macBtDamSan = "B25";

        [ObservableProperty]
        private string _macThep = "CB400-V";

        [ObservableProperty]
        private string _filePath = string.Empty;

        [ObservableProperty]
        private string _statusMessage = "Sẵn sàng thực thi pipeline 24 bước AutoStr.";

        public ObservableCollection<FloorItemViewModel> Floors { get; } = new();
        public ObservableCollection<StepItemViewModel> Steps { get; } = new();

        public IReadOnlyList<string> AvailableConcreteGrades { get; } = new[] { "B20", "B25", "B30", "B35", "B40" };
        public IReadOnlyList<string> AvailableSteelGrades { get; } = new[] { "CB300-V", "CB400-V", "CB500-V" };

        public AutoStrConfigViewModel()
        {
            InitializeDefaultFloors();
            InitializeSteps();
        }

        private void InitializeDefaultFloors()
        {
            Floors.Add(new FloorItemViewModel(1, "TẦNG 1", "T1", 3900.0, 3900.0));
            Floors.Add(new FloorItemViewModel(2, "TẦNG 2", "T2", 3600.0, 7500.0));
            Floors.Add(new FloorItemViewModel(3, "TẦNG 3", "T3", 3600.0, 11100.0));
            Floors.Add(new FloorItemViewModel(4, "TẦNG TUM", "TUM", 3000.0, 14100.0));
        }

        private void InitializeSteps()
        {
            string[] stepNames =
            {
                "Khởi tạo SheetSet", "Tạo Level & Story", "Import Lưới trục Grid", "Vẽ Cột định hình",
                "Vẽ Vách cứng", "Tạo Dầm chính", "Tạo Dầm phụ", "Chia Ô sàn & Tải trọng",
                "Tính Thép sàn TCVN", "Vẽ Thép sàn lớp dưới", "Vẽ Thép sàn lớp trên", "Shop drawing Dầm",
                "Mặt cắt dầm", "Bảng thống kê dầm", "Shop drawing Cột", "Mặt cắt cột",
                "Bảng thống kê cột", "Mặt bằng kết cấu móng", "Chi tiết đài móng", "Thống kê thép móng",
                "Thuyết minh tính toán", "Xuất mô hình ETABS", "Tối ưu cắt thép 11.7m", "In ấn hàng loạt"
            };

            for (int i = 0; i < stepNames.Length; i++)
            {
                Steps.Add(new StepItemViewModel(i + 1, stepNames[i], true));
            }
        }

        [RelayCommand]
        public void SelectAllSteps()
        {
            foreach (var step in Steps) step.IsEnabled = true;
        }

        [RelayCommand]
        public void DeselectAllSteps()
        {
            foreach (var step in Steps) step.IsEnabled = false;
        }

        [RelayCommand]
        public void AddFloor()
        {
            int nextIdx = Floors.Count + 1;
            double prevElev = Floors.Count > 0 ? Floors.Last().CaoDoTang : 0.0;
            Floors.Add(new FloorItemViewModel(nextIdx, $"TẦNG {nextIdx}", $"T{nextIdx}", 3600.0, prevElev + 3600.0));
        }

        [RelayCommand]
        public void RemoveFloor(FloorItemViewModel? item)
        {
            if (item != null) Floors.Remove(item);
        }

        [RelayCommand]
        public void LoadFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
            var cfg = AutoStrProjectSerializer.DeserializeFromFile(path);
            TenDuAn = cfg.TenDuAn;
            TenCongTrinh = cfg.TenCongTrinh;
            DiaChiCongTrinh = cfg.DiaChiCongTrinh;
            ChuDauTu = cfg.ChuDauTu;
            MacBtCotVach = cfg.MacBtCotVach;
            MacBtDamSan = cfg.MacBtDamSan;
            MacThep = cfg.MacThep;
            FilePath = path;

            if (cfg.Floors?.Count > 0)
            {
                Floors.Clear();
                foreach (var f in cfg.Floors)
                {
                    Floors.Add(new FloorItemViewModel(f.StoryIndex, f.TenTang, f.KyHieuTang, f.ChieuCaoTang, f.CaoDoTang)
                    {
                        SuDung = f.SuDung
                    });
                }
            }
            StatusMessage = $"Đã nạp dự án: {TenDuAn} ({Floors.Count} tầng)";
        }

        public AutoStrConfig ToDomainConfig() => new()
        {
            TenDuAn = TenDuAn,
            TenCongTrinh = TenCongTrinh,
            DiaChiCongTrinh = DiaChiCongTrinh,
            ChuDauTu = ChuDauTu,
            MacBtCotVach = MacBtCotVach,
            MacBtDamSan = MacBtDamSan,
            MacThep = MacThep,
            Floors = Floors.Select(f => f.ToDomain()).ToList(),
            StepEnabled = Steps.Select(s => s.IsEnabled).ToList()
        };

        [RelayCommand]
        public void SaveToFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            AutoStrProjectSerializer.SerializeToFile(ToDomainConfig(), path);
            FilePath = path;
            StatusMessage = $"Đã lưu tệp: {Path.GetFileName(path)}";
        }

        [RelayCommand]
        public async Task RunPipelineAsync()
        {
            var enabledNums = Steps.Where(s => s.IsEnabled).Select(s => s.StepNumber).ToHashSet();
            if (enabledNums.Count == 0)
            {
                StatusMessage = "Vui lòng chọn ít nhất 1 bước để thực thi.";
                return;
            }

            var orchestrator = new AutoStrOrchestrator(ToDomainConfig());
            orchestrator.Steps.RemoveAll(s => !enabledNums.Contains(s.StepNumber));
            orchestrator.ProgressChanged += (_, p) => StatusMessage = p.StatusMessage;

            StatusMessage = $"Đang thực thi {orchestrator.Steps.Count} bước AutoStr...";
            var result = await orchestrator.ExecuteAsync().ConfigureAwait(true);
            StatusMessage = result.IsSuccess
                ? $"Hoàn tất {result.CompletedStepsCount}/{orchestrator.Steps.Count} bước ({result.TotalDuration.TotalMilliseconds:F0} ms)."
                : $"Dừng tại bước lỗi ({result.CompletedStepsCount}/{orchestrator.Steps.Count} bước hoàn tất).";
        }
    }
}
