using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Core.AutoStr.Steps
{
    public class PileCapLayoutDefinition
    {
        public string Mark { get; set; } = string.Empty;
        public Point2D Location { get; set; }
        public int PileCount { get; set; } = 4;
        public double PileCapacityKn { get; set; } = 350.0;
        public double MaxPileReactionKn { get; set; } = 320.0;
        public double WidthMm { get; set; } = 1400.0;
        public double LengthMm { get; set; } = 1400.0;
        public double DepthMm { get; set; } = 800.0;
        public bool IsCapacityExceeded => MaxPileReactionKn > PileCapacityKn;
    }

    public sealed class AutoStrStep13_MBMongCoc : AutoStrStepBase
    {
        public override int StepNumber => 13;
        public override string StepName => "Bố trí mặt bằng móng cọc và định vị đài cọc";

        protected override Task<AutoStrStepResult> ExecuteStepCoreAsync(
            AutoStrStepContext context, CancellationToken cancellationToken)
        {
            var caps = context.GetArtifact<List<PileCapLayoutDefinition>>("PileCapDefinitions");
            if (caps == null)
            {
                caps = new List<PileCapLayoutDefinition>();
                int colCount = context.Topology.ColumnNodes.Count;
                if (colCount == 0) colCount = 12;

                for (int i = 1; i <= colCount; i++)
                {
                    var pt = (context.Topology.ColumnNodes.Count >= i)
                        ? context.Topology.ColumnNodes[i - 1]
                        : new Point2D((i - 1) * 4000.0, 0.0);

                    caps.Add(new PileCapLayoutDefinition
                    {
                        Mark = $"{context.Config.PrefixTenDai}{i}",
                        Location = pt,
                        PileCount = 4,
                        PileCapacityKn = 350.0,
                        MaxPileReactionKn = 320.0,
                        WidthMm = 1400.0,
                        LengthMm = 1400.0,
                        DepthMm = 800.0
                    });
                }
            }

            context.SetArtifact("PileCapDefinitions", caps);
            context.SetArtifact("PileCapCount", caps.Count);

            return Task.FromResult(AutoStrStepResult.Success(
                StepNumber,
                StepName,
                $"Đã bố trí mặt bằng móng gồm {caps.Count} đài cọc."));
        }
    }
}
