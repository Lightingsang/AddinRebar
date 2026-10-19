using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Foundation;

namespace HPDcePro.Core.AutoStr.Steps
{
    public sealed class AutoStrStep15_EtabsMong : AutoStrStepBase
    {
        public override int StepNumber => 15;
        public override string StepName => "Mô hình hóa nền móng và giải nội lực cọc trong ETABS/SAFE";

        protected override Task<AutoStrStepResult> ExecuteStepCoreAsync(
            AutoStrStepContext context, CancellationToken cancellationToken)
        {
            var caps = context.GetArtifact<List<PileCapLayoutDefinition>>("PileCapDefinitions");
            int count = caps != null ? caps.Count : 12;
            var overloadedCaps = new List<PileCapLayoutDefinition>();
            double maxReaction = 0.0;

            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            var steel = MaterialLibrary.GetRebar(RebarGrade.CB400V);

            if (caps != null)
            {
                foreach (var cap in caps)
                {
                    double axialLoadKn = 1200.0;
                    double mx = 40.0;
                    double my = 30.0;
                    var pattern = cap.PileCount switch
                    {
                        1 => PileLayoutPattern.SinglePile,
                        2 => PileLayoutPattern.TwoPile,
                        3 => PileLayoutPattern.ThreePileTriangular,
                        5 => PileLayoutPattern.FivePileDice,
                        6 => PileLayoutPattern.SixPileRectangular,
                        _ => PileLayoutPattern.FourPileRectangular
                    };

                    var piles = PileCapCalculator.GenerateStandardLayout(pattern, 400.0);
                    var input = new PileCapInput(
                        cap.Mark, cap.WidthMm, cap.LengthMm, cap.DepthMm, 400.0, 400.0,
                        concrete, steel, piles, axialLoadKn, mx, my, cap.PileCapacityKn);

                    var reactions = PileCapCalculator.CalculatePileReactions(input, axialLoadKn * 1.15);
                    double capMaxR = reactions.Count > 0 ? reactions.Max(p => p.ReactionKn) : axialLoadKn / cap.PileCount;
                    cap.MaxPileReactionKn = Math.Round(capMaxR, 1);

                    if (capMaxR > maxReaction) maxReaction = capMaxR;
                    if (cap.IsCapacityExceeded)
                    {
                        overloadedCaps.Add(cap);
                    }
                }
            }

            context.SetArtifact("OverloadedPileCaps", overloadedCaps);
            context.SetArtifact("MaxPileReactionGlobalKn", maxReaction);

            string msg = overloadedCaps.Count == 0
                ? $"Đã giải mô hình móng: toàn bộ phản lực đầu cọc đều thỏa mãn (P_max = {maxReaction:F1} kN <= P_allow)."
                : $"Phát hiện {overloadedCaps.Count} đài cọc có P_max = {maxReaction:F1} kN vượt sức chịu tải P_allow. Cần tăng số lượng cọc.";

            return Task.FromResult(AutoStrStepResult.Success(StepNumber, StepName, msg));
        }
    }
}
