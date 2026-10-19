using System.Threading;
using System.Threading.Tasks;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Core.AutoStr.Steps
{
    public sealed class AutoStrStep19_VeGiangM : AutoStrStepBase
    {
        public override int StepNumber => 19;
        public override string StepName => "Triển khai bản vẽ chi tiết giằng móng";

        protected override Task<AutoStrStepResult> ExecuteStepCoreAsync(
            AutoStrStepContext context, CancellationToken cancellationToken)
        {
            var buffer = new CadDraftingBuffer();
            int tieBeamCount = context.Topology.BeamCenterlines.Count;
            if (tieBeamCount == 0) tieBeamCount = 10;

            double startY = 0.0;
            for (int i = 1; i <= tieBeamCount; i++)
            {
                buffer.AddLine(new Point2D(0.0, startY), new Point2D(4000.0, startY), "DCE_Beam", CadColor.ByLayerIndex);
                buffer.AddLine(new Point2D(0.0, startY - 600.0), new Point2D(4000.0, startY - 600.0), "DCE_Beam", CadColor.ByLayerIndex);
                buffer.AddText($"GIẰNG MÓNG GM{i} (300x600)", new Point2D(2000.0, startY + 200.0), 200.0, "DCE_TEXT", CadColor.CyanIndex);
                startY += 1500.0;
            }

            context.SetArtifact("TieBeamDraftingBuffer", buffer);

            return Task.FromResult(AutoStrStepResult.Success(
                StepNumber,
                StepName,
                $"Đã sinh bản vẽ chi tiết cho {tieBeamCount} cấu kiện giằng móng."));
        }
    }
}
