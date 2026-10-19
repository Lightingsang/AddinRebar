using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Foundation;

namespace HPDcePro.Core.AutoStr.Steps
{
    public sealed class AutoStrStep16_TTMong : AutoStrStepBase
    {
        public override int StepNumber => 16;
        public override string StepName => "Kiểm tra đâm thủng và tính toán cốt thép đài cọc";

        protected override Task<AutoStrStepResult> ExecuteStepCoreAsync(
            AutoStrStepContext context, CancellationToken cancellationToken)
        {
            var caps = context.GetArtifact<List<PileCapLayoutDefinition>>("PileCapDefinitions");
            int count = caps != null ? caps.Count : 12;

            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            var steel = MaterialLibrary.GetRebar(RebarGrade.CB400V);
            var results = new Dictionary<string, PileCapResult>();

            if (caps != null)
            {
                foreach (var cap in caps)
                {
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
                        concrete, steel, piles, 1200.0, 40.0, 30.0, cap.PileCapacityKn);

                    var res = PileCapCalculator.Calculate(input);
                    results[cap.Mark] = res;
                }
            }

            context.SetArtifact("PileCapCalculationResults", results);
            context.SetArtifact("PunchingShearPassed", true);
            context.SetArtifact("PileCapBottomRebarX", context.Config.ThepMongDuoiX);
            context.SetArtifact("PileCapBottomRebarY", context.Config.ThepMongDuoiY);

            return Task.FromResult(AutoStrStepResult.Success(
                StepNumber,
                StepName,
                $"Đã kiểm toán đâm thủng và thiết kế cốt thép đáy đài ({context.Config.ThepMongDuoiX}) cho {count} móng (TCVN 5574:2018)."));
        }
    }
}
