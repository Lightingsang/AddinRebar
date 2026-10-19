using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPDcePro.Core.AutoStr;
using HPDcePro.Core.Serialization;

namespace HPDcePro.UI.ViewModels
{
    public sealed class AutoStrStepStatusItem : ObservableObject
    {
        public int StepNumber { get; set; }
        public string StepName { get; set; } = string.Empty;
        public string Status { get; set; } = "Chờ xử lý";
        public string Message { get; set; } = string.Empty;
    }

    public sealed partial class AutoStrExecutionViewModel : ObservableObject
    {
        private CancellationTokenSource? _cts;

        [ObservableProperty]
        private string _projectName = "Dự án mẫu AutoStr 2026";

        [ObservableProperty]
        private double _overallProgressPercentage;

        [ObservableProperty]
        private int _currentStepNumber;

        [ObservableProperty]
        private string _currentStepName = "Sẵn sàng";

        [ObservableProperty]
        private bool _isExecuting;

        public ObservableCollection<AutoStrStepStatusItem> Steps { get; } = new();
        public ObservableCollection<string> ExecutionLogs { get; } = new();

        public AutoStrExecutionViewModel()
        {
            InitializeStepList();
        }

        private void InitializeStepList()
        {
            string[] defaultStepNames =
            {
                "Tạo SheetSet", "Lưới trục", "Định vị cột", "Mặt bằng kết cấu", "Đường phân tích lực",
                "Tải tường", "Build Etabs Model", "Tính toán dầm", "Vẽ chi tiết dầm", "Tính toán cột",
                "Vẽ chi tiết cột vách", "Vẽ chi tiết sàn", "Mặt bằng móng cọc", "Đường phân tích lực móng",
                "Etabs móng", "Tính toán móng", "Vẽ móng", "Tính toán giằng móng", "Vẽ giằng móng",
                "Cấu kiện phụ", "Danh mục bản vẽ & BOQ", "Mở SheetSet", "In ấn PDF", "Thuyết minh tính toán"
            };

            for (int i = 0; i < defaultStepNames.Length; i++)
            {
                Steps.Add(new AutoStrStepStatusItem
                {
                    StepNumber = i + 1,
                    StepName = defaultStepNames[i],
                    Status = "Chờ xử lý"
                });
            }
        }

        [RelayCommand]
        public async Task StartPipelineAsync()
        {
            if (IsExecuting) return;

            IsExecuting = true;
            _cts = new CancellationTokenSource();
            ExecutionLogs.Add($"[{DateTime.Now:HH:mm:ss}] Bắt đầu thực thi quy trình AutoStr 24 bước...");

            var config = new AutoStrConfig
            {
                ProjectRootPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "HPDcePro_AutoStr")
            };
            var orch = new AutoStrOrchestrator(config);
            orch.ProgressChanged += (s, e) =>
            {
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    CurrentStepNumber = e.StepNumber;
                    CurrentStepName = e.StepName;
                    OverallProgressPercentage = e.Percent;
                    ExecutionLogs.Add($"[{DateTime.Now:HH:mm:ss}] Bước {e.StepNumber}: {e.StepName} ({e.Percent:F0}%)");

                    if (e.StepNumber >= 1 && e.StepNumber <= Steps.Count)
                    {
                        Steps[e.StepNumber - 1].Status = "Đang chạy";
                        Steps[e.StepNumber - 1].Message = e.StatusMessage;
                        if (e.StepNumber > 1)
                        {
                            Steps[e.StepNumber - 2].Status = "Hoàn thành";
                        }
                    }
                });
            };

            try
            {
                var result = await orch.ExecuteAsync(_cts.Token);
                if (result.IsSuccess)
                {
                    ExecutionLogs.Add($"[{DateTime.Now:HH:mm:ss}] Hoàn tất 24 bước AutoStr thành công! (Thời gian: {result.TotalDuration.TotalSeconds:F1}s)");
                    if (Steps.Count > 0) Steps[Steps.Count - 1].Status = "Hoàn thành";
                }
            }
            catch (Exception ex)
            {
                // Reason: Handle user cancellation or step failure during pipeline execution.
                ExecutionLogs.Add($"[{DateTime.Now:HH:mm:ss}] Lỗi thực thi hoặc đã hủy: {ex.Message}");
            }
            finally
            {
                IsExecuting = false;
            }
        }

        [RelayCommand]
        public void Cancel()
        {
            _cts?.Cancel();
            IsExecuting = false;
            ExecutionLogs.Add($"[{DateTime.Now:HH:mm:ss}] Đã gửi lệnh hủy tiến trình.");
        }
    }
}
