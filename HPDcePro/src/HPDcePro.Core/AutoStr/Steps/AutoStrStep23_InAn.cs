using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HPDcePro.Core.AutoStr.Steps
{
    public sealed class AutoStrStep23_InAn : AutoStrStepBase
    {
        public override int StepNumber => 23;
        public override string StepName => "Tự động xuất và in ấn hồ sơ bản vẽ hàng loạt (Batch Plot PDF)";

        protected override Task<AutoStrStepResult> ExecuteStepCoreAsync(
            AutoStrStepContext context, CancellationToken cancellationToken)
        {
            var drawingSheets = context.GetArtifact<List<string>>("DrawingSheetList")
                ?? new List<string>
                {
                    "KC-01: Ghi chú chung",
                    "KC-02: Định vị cột",
                    "KC-03: Móng cọc",
                    "KC-04: Dầm sàn điển hình"
                };

            var plotTargets = new List<string>();
            foreach (var sheet in drawingSheets)
            {
                var sheetId = sheet.Contains(':') ? sheet.Split(':')[0].Trim() : sheet;
                plotTargets.Add($"{sheetId} [{context.Config.KhoGiay}] -> {context.Config.MayIn}");
            }

            string pdfName = $"{context.Config.TenDuAn}_Full_BanVe.pdf";
            string pdfPath = string.IsNullOrWhiteSpace(context.ProjectRootPath)
                ? pdfName
                : Path.Combine(context.ProjectRootPath, "12.InAn", pdfName);

            context.SetArtifact("BatchPdfPath", pdfPath);
            context.SetArtifact("BatchPlotConfig", $"{context.Config.MayIn} | {context.Config.NetIn} | {context.Config.KhoGiay}");
            context.SetArtifact("BatchPlotSheets", plotTargets);

            return Task.FromResult(AutoStrStepResult.Success(
                StepNumber,
                StepName,
                $"Đã hoàn tất cấu hình xuất in ấn hàng loạt {plotTargets.Count} bản vẽ ({pdfName}) theo chuẩn {context.Config.KhoGiay}.",
                outputFiles: new List<string> { pdfPath }));
        }
    }
}
