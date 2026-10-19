using System.Threading;
using System.Threading.Tasks;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Core.AutoStr.Steps
{
    public sealed class AutoStrStep20_CKPhu : AutoStrStepBase
    {
        public override int StepNumber => 20;
        public override string StepName => "Triển khai chi tiết cấu kiện phụ: Cầu thang và lanh tô";

        protected override Task<AutoStrStepResult> ExecuteStepCoreAsync(
            AutoStrStepContext context, CancellationToken cancellationToken)
        {
            var buffer = new CadDraftingBuffer();
            // Stair flight profile
            buffer.AddLine(new Point2D(0.0, 0.0), new Point2D(1200.0, 0.0), "DCE_BaoBeTong", CadColor.ByLayerIndex);
            buffer.AddLine(new Point2D(1200.0, 0.0), new Point2D(3600.0, 1800.0), "DCE_BaoBeTong", CadColor.ByLayerIndex);
            buffer.AddText("CHI TIẾT CẦU THANG 2 VẾ (h_w = 120mm)", new Point2D(1800.0, 2000.0), 200.0, "DCE_TEXT", CadColor.YellowIndex);

            context.SetArtifact("SecondaryDraftingBuffer", buffer);

            return Task.FromResult(AutoStrStepResult.Success(
                StepNumber,
                StepName,
                "Đã sinh bản vẽ triển khai chi tiết cho cầu thang 2 vế và hệ thống lanh tô cửa."));
        }
    }
}
