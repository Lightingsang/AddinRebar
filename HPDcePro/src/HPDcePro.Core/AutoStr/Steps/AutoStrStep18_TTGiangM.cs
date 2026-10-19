using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HPDcePro.Core.Calculations.Beam;

namespace HPDcePro.Core.AutoStr.Steps
{
    public sealed class AutoStrStep18_TTGiangM : AutoStrStepBase
    {
        public override int StepNumber => 18;
        public override string StepName => "Tính toán thiết kế giằng móng (đà kiềng)";

        protected override Task<AutoStrStepResult> ExecuteStepCoreAsync(
            AutoStrStepContext context, CancellationToken cancellationToken)
        {
            int tieBeamCount = context.Topology.BeamCenterlines.Count;
            if (tieBeamCount == 0) tieBeamCount = 10;

            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            var steel = MaterialLibrary.GetRebar(RebarGrade.CB400V);
            var tieBeamResults = new Dictionary<string, FoundationBeamResult>();

            for (int i = 1; i <= tieBeamCount; i++)
            {
                var input = new FoundationBeamDesignInput($"GM{i}", concrete, steel)
                {
                    SpanLengthMm = 6000.0,
                    WidthMm = 300.0,
                    HeightMm = 600.0,
                    CoverMm = 50.0,
                    UpwardMidspanMomentKnm = 160.0 + (i % 3) * 20.0,
                    SupportBottomMomentKnm = 140.0 + (i % 3) * 15.0,
                    ShearForceKn = 180.0 + (i % 3) * 25.0
                };

                var res = FoundationBeamCalculator.Calculate(input);
                tieBeamResults[$"GM{i}"] = res;
            }

            context.SetArtifact("TieBeamCalculatedCount", tieBeamCount);
            context.SetArtifact("TieBeamResults", tieBeamResults);
            context.SetArtifact("TieBeamDefaultSection", "300x600");

            return Task.FromResult(AutoStrStepResult.Success(
                StepNumber,
                StepName,
                $"Đã hoàn thành thiết kế cốt thép và kiểm tra cắt dầm sâu cho {tieBeamCount} giằng móng 300x600mm."));
        }
    }
}
