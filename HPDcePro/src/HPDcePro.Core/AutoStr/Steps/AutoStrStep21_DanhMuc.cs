using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HPDcePro.Core.Calculations.Takeoff;

namespace HPDcePro.Core.AutoStr.Steps
{
    public sealed class AutoStrStep21_DanhMuc : AutoStrStepBase
    {
        public override int StepNumber => 21;
        public override string StepName => "Lập danh mục bản vẽ và tính toán bóc tách khối lượng (BOQ)";

        protected override Task<AutoStrStepResult> ExecuteStepCoreAsync(
            AutoStrStepContext context, CancellationToken cancellationToken)
        {
            var drawings = new List<string>
            {
                "KC-01: Ghi chú chung và quy cách nối cốt thép",
                "KC-02: Mặt bằng định vị cột vách",
                "KC-03: Mặt bằng móng và chi tiết đài cọc",
                "KC-04: Chi tiết giằng móng",
                "KC-05: Mặt bằng kết cấu dầm sàn Tầng 1",
                "KC-06: Mặt bằng kết cấu dầm sàn Tầng 2-3",
                "KC-07: Mặt bằng kết cấu dầm sàn Tầng Mái",
                "KC-08: Chi tiết cốt thép dầm khung điển hình",
                "KC-09: Chi tiết cốt thép cột vách",
                "KC-10: Chi tiết cầu thang bộ và lanh tô",
                "KC-11: Bảng thống kê và tổng hợp cốt thép",
                "KC-12: Bảng tổng hợp khối lượng vật tư (BOQ)"
            };

            var boq = QuantityTakeoffCalculator.CalculateTakeoff(context);

            context.SetArtifact("DrawingSheetList", drawings);
            context.SetArtifact("TakeoffResult", boq);

            return Task.FromResult(AutoStrStepResult.Success(
                StepNumber,
                StepName,
                $"Đã lập danh mục {drawings.Count} bản vẽ và tổng hợp khối lượng: Bê tông = {boq.TotalConcreteVolumeM3:F1} m3, Ván khuôn = {boq.TotalFormworkAreaM2:F1} m2, Thép = {boq.TotalRebarTons:F2} tấn."));
        }
    }
}
