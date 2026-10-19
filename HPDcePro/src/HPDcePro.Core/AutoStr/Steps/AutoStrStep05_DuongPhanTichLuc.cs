using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Core.AutoStr.Steps
{
    public sealed class AutoStrStep05_DuongPhanTichLuc : AutoStrStepBase
    {
        public override int StepNumber => 5;
        public override string StepName => "Sinh đường phân tích lực và sơ đồ tính khung";

        protected override Task<AutoStrStepResult> ExecuteStepCoreAsync(
            AutoStrStepContext context, CancellationToken cancellationToken)
        {
            var wireframes = new List<Segment2D>();
            foreach (var beam in context.Topology.BeamCenterlines)
            {
                wireframes.Add(beam);
            }

            context.SetArtifact("AnalyticalWireframes", wireframes);
            context.SetArtifact("AnalyticalMemberCount", wireframes.Count);

            return Task.FromResult(AutoStrStepResult.Success(
                StepNumber,
                StepName,
                $"Đã sinh sơ đồ đường phân tích lực gồm {wireframes.Count} phần tử thanh."));
        }
    }
}
