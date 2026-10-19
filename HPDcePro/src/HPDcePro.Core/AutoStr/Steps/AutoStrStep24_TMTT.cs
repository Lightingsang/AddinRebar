using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HPDcePro.Core.Calculations.Takeoff;

namespace HPDcePro.Core.AutoStr.Steps
{
    public sealed class AutoStrStep24_TMTT : AutoStrStepBase
    {
        public override int StepNumber => 24;
        public override string StepName => "Biên soạn thuyết minh tính toán kết cấu tự động (TMTT)";

        protected override Task<AutoStrStepResult> ExecuteStepCoreAsync(
            AutoStrStepContext context, CancellationToken cancellationToken)
        {
            var takeoff = context.GetArtifact<QuantityTakeoffResult>("TakeoffResult")
                ?? QuantityTakeoffCalculator.CalculateTakeoff(context);

            string docName = $"{context.Config.TenDuAn}_ThuyetMinhTinhToan.docx";
            string docPath = string.IsNullOrWhiteSpace(context.ProjectRootPath)
                ? docName
                : Path.Combine(context.ProjectRootPath, docName);

            var sb = new StringBuilder();
            sb.AppendLine($"=== THUYẾT MINH TÍNH TOÁN KẾT CẤU: {context.Config.TenDuAn} ===");
            sb.AppendLine($"Tiêu chuẩn áp dụng: TCVN 5574:2018, TCVN 2737:2023, TCVN 10304:2025");
            sb.AppendLine($"Cấp độ bền bê tông: Dầm/Sàn: {context.Config.MacBtDamSan}, Cột/Vách: {context.Config.MacBtCotVach}");
            sb.AppendLine($"Cốt thép: {context.Config.MacThep}");
            sb.AppendLine($"--- TỔNG HỢP KHỐI LƯỢNG VẬT TƯ (BOQ) ---");
            sb.AppendLine($"- Tổng thể tích bê tông: {takeoff.TotalConcreteVolumeM3:F2} m3");
            sb.AppendLine($"- Tổng diện tích ván khuôn: {takeoff.TotalFormworkAreaM2:F2} m2");
            sb.AppendLine($"- Thép d <= 10mm: {takeoff.TotalRebarKgLe10:F1} kg");
            sb.AppendLine($"- Thép 10 < d <= 18mm: {takeoff.TotalRebarKgLe18:F1} kg");
            sb.AppendLine($"- Thép d > 18mm: {takeoff.TotalRebarKgGt18:F1} kg");
            sb.AppendLine($"- Tổng khối lượng cốt thép: {takeoff.TotalRebarTons:F2} tấn");

            string summary = sb.ToString();
            context.SetArtifact("CalculationReportPath", docPath);
            context.SetArtifact("CalculationReportSummary", summary);
            context.SetArtifact("ReportReady", true);

            return Task.FromResult(AutoStrStepResult.Success(
                StepNumber,
                StepName,
                $"Đã biên soạn và xuất tập thuyết minh tính toán kết cấu công trình ({docName}) với tổng khối lượng {takeoff.TotalRebarTons:F2} tấn thép, {takeoff.TotalConcreteVolumeM3:F1} m3 bê tông.",
                outputFiles: new List<string> { docPath }));
        }
    }
}
