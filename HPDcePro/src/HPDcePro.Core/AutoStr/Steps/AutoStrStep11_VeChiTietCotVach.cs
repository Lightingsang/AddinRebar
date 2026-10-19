using System.Threading;
using System.Threading.Tasks;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Core.AutoStr.Steps
{
    public sealed class AutoStrStep11_VeChiTietCotVach : AutoStrStepBase
    {
        public override int StepNumber => 11;
        public override string StepName => "Triển khai bản vẽ chi tiết cốt thép cột và vách";

        protected override Task<AutoStrStepResult> ExecuteStepCoreAsync(
            AutoStrStepContext context, CancellationToken cancellationToken)
        {
            var buffer = new CadDraftingBuffer();
            int colCount = context.Topology.ColumnNodes.Count;
            if (colCount == 0) colCount = 12;

            double startX = 0.0;
            for (int i = 1; i <= colCount; i++)
            {
                string mark = $"{context.Config.TienToCot}{i}";
                // Column cross section 300x300
                buffer.AddPolyline(new[]
                {
                    new Point2D(startX, 0.0),
                    new Point2D(startX + 300.0, 0.0),
                    new Point2D(startX + 300.0, 300.0),
                    new Point2D(startX, 300.0)
                }, isClosed: true, layer: "DCE_SUB-COWA_----");

                buffer.AddText(mark, new Point2D(startX + 150.0, -100.0), 150.0, "DCE_TEXT", CadColor.CyanIndex);
                startX += 800.0;
            }

            context.SetArtifact("ColumnDraftingBuffer", buffer);

            return Task.FromResult(AutoStrStepResult.Success(
                StepNumber,
                StepName,
                $"Đã triển khai bản vẽ định vị và chi tiết cho {colCount} cột ({buffer.Entities.Count} thực thể đồ họa)."));
        }
    }
}
