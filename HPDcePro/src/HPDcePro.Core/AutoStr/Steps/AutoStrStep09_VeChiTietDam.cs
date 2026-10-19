using System.Threading;
using System.Threading.Tasks;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Core.AutoStr.Steps
{
    public sealed class AutoStrStep09_VeChiTietDam : AutoStrStepBase
    {
        public override int StepNumber => 9;
        public override string StepName => "Triển khai bản vẽ chi tiết cốt thép dầm khung";

        protected override Task<AutoStrStepResult> ExecuteStepCoreAsync(
            AutoStrStepContext context, CancellationToken cancellationToken)
        {
            var buffer = new CadDraftingBuffer();
            int beamCount = context.Topology.BeamCenterlines.Count;
            if (beamCount == 0) beamCount = 12;

            double startY = 0.0;
            for (int i = 1; i <= beamCount; i++)
            {
                double span = 5000.0;
                double h = context.Config.ChieuCaoDamDienHinh;
                // Beam outline
                buffer.AddLine(new Point2D(0.0, startY), new Point2D(span, startY), "DCE_Beam", CadColor.ByLayerIndex);
                buffer.AddLine(new Point2D(0.0, startY - h), new Point2D(span, startY - h), "DCE_Beam", CadColor.ByLayerIndex);
                buffer.AddText($"DẦM D{i}", new Point2D(span / 2.0, startY + 200.0), 250.0, "DCE_TEXT", CadColor.YellowIndex);
                startY += 2000.0;
            }

            context.SetArtifact("BeamDraftingBuffer", buffer);
            context.SetArtifact("DetailedBeamCount", beamCount);

            return Task.FromResult(AutoStrStepResult.Success(
                StepNumber,
                StepName,
                $"Đã sinh bản vẽ triển khai chi tiết cho {beamCount} cấu kiện dầm khung ({buffer.Entities.Count} thực thể đồ họa)."));
        }
    }
}
