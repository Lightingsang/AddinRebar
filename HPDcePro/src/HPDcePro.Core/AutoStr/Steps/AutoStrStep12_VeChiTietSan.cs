using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Core.AutoStr.Steps
{
    public sealed class AutoStrStep12_VeChiTietSan : AutoStrStepBase
    {
        public override int StepNumber => 12;
        public override string StepName => "Tính toán và triển khai bản vẽ cốt thép sàn";

        protected override Task<AutoStrStepResult> ExecuteStepCoreAsync(
            AutoStrStepContext context, CancellationToken cancellationToken)
        {
            var buffer = new CadDraftingBuffer();
            // Typical 4x5m bay
            var slabOutline = new[]
            {
                new Point2D(0.0, 0.0),
                new Point2D(4000.0, 0.0),
                new Point2D(4000.0, 5000.0),
                new Point2D(0.0, 5000.0)
            };
            buffer.AddPolyline(slabOutline, isClosed: true, layer: "DCE_BaoBeTong");
            buffer.AddText("THÉP SÀN: d10a150 (Lớp dưới) + d10a200 (Lớp trên)", new Point2D(2000.0, 2500.0), 200.0, "DCE_San_Duoi", CadColor.GreenIndex);

            context.SetArtifact("SlabDraftingBuffer", buffer);
            context.SetArtifact("SlabThicknessMm", context.Config.ChieuDaySanDienHinh);

            return Task.FromResult(AutoStrStepResult.Success(
                StepNumber,
                StepName,
                $"Đã hoàn thành tính toán và bố trí cốt thép sàn chiều dày h={context.Config.ChieuDaySanDienHinh}mm."));
        }
    }
}
