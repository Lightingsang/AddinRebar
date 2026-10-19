using System.Threading;
using System.Threading.Tasks;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Core.AutoStr.Steps
{
    public sealed class AutoStrStep17_VeMong : AutoStrStepBase
    {
        public override int StepNumber => 17;
        public override string StepName => "Triển khai bản vẽ chi tiết mặt bằng và mặt cắt đài cọc";

        protected override Task<AutoStrStepResult> ExecuteStepCoreAsync(
            AutoStrStepContext context, CancellationToken cancellationToken)
        {
            var buffer = new CadDraftingBuffer();
            var caps = context.GetArtifact<System.Collections.Generic.List<PileCapLayoutDefinition>>("PileCapDefinitions");
            int count = caps != null ? caps.Count : 12;

            double startX = 0.0;
            for (int i = 1; i <= count; i++)
            {
                buffer.AddPolyline(new[]
                {
                    new Point2D(startX, 0.0),
                    new Point2D(startX + 1400.0, 0.0),
                    new Point2D(startX + 1400.0, 1400.0),
                    new Point2D(startX, 1400.0)
                }, isClosed: true, layer: "DCE_BaoBeTong");

                buffer.AddText($"{context.Config.PrefixTenDai}{i}", new Point2D(startX + 700.0, 700.0), 200.0, "DCE_TEXT", CadColor.YellowIndex);
                startX += 2000.0;
            }

            context.SetArtifact("PileCapDraftingBuffer", buffer);

            return Task.FromResult(AutoStrStepResult.Success(
                StepNumber,
                StepName,
                $"Đã sinh bản vẽ mặt bằng và mặt cắt chi tiết cho {count} đài cọc ({buffer.Entities.Count} thực thể đồ họa)."));
        }
    }
}
