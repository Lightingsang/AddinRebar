using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Core.AutoStr.Steps
{
    public class BeamDefinition
    {
        public string Mark { get; set; } = string.Empty;
        public Segment2D Centerline { get; set; } = new Segment2D(Point2D.Zero, Point2D.Zero);
        public double WidthMm { get; set; } = 220.0;
        public double HeightMm { get; set; } = 400.0;
        public double LengthMm => Centerline.Length;
    }

    public sealed class AutoStrStep04_MBKC : AutoStrStepBase
    {
        public override int StepNumber => 4;
        public override string StepName => "Khởi tạo mặt bằng kết cấu và hệ dầm sàn";

        protected override Task<AutoStrStepResult> ExecuteStepCoreAsync(
            AutoStrStepContext context, CancellationToken cancellationToken)
        {
            var beams = new List<BeamDefinition>();
            double b = context.Config.BeRongDamDienHinh;
            double h = context.Config.ChieuCaoDamDienHinh;

            var nodes = context.Topology.ColumnNodes;
            int beamIndex = 1;

            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    double dist = nodes[i].DistanceTo(nodes[j]);
                    if (dist > 3500.0 && dist <= 5500.0)
                    {
                        var seg = new Segment2D(nodes[i], nodes[j]);
                        context.Topology.BeamCenterlines.Add(seg);
                        beams.Add(new BeamDefinition
                        {
                            Mark = $"D{beamIndex++}",
                            Centerline = seg,
                            WidthMm = b,
                            HeightMm = h
                        });
                    }
                }
            }

            context.SetArtifact("BeamDefinitions", beams);
            context.SetArtifact("BeamCount", beams.Count);

            return Task.FromResult(AutoStrStepResult.Success(
                StepNumber,
                StepName,
                $"Đã sinh thành công mặt bằng kết cấu với {beams.Count} cấu kiện dầm khung."));
        }
    }
}
