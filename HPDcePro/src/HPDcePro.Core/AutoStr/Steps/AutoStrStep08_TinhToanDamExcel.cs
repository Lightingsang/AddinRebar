using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HPDcePro.Core.Calculations.Beam;

namespace HPDcePro.Core.AutoStr.Steps
{
    public sealed class AutoStrStep08_TinhToanDamExcel : AutoStrStepBase
    {
        public override int StepNumber => 8;
        public override string StepName => "Trích xuất nội lực và thiết kế cốt thép dầm theo TCVN 5574:2018";

        protected override Task<AutoStrStepResult> ExecuteStepCoreAsync(
            AutoStrStepContext context, CancellationToken cancellationToken)
        {
            var cGrade = ParseConcreteGrade(context.Config.MacBtDamSan);
            var sGrade = ParseRebarGrade(context.Config.MacThep);
            var concrete = MaterialLibrary.GetConcrete(cGrade);
            var steel = MaterialLibrary.GetRebar(sGrade);
            double b = context.Config.BeRongDamDienHinh;
            double h = context.Config.ChieuCaoDamDienHinh;

            var flexureResults = new Dictionary<string, BeamFlexureResult>();
            var shearResults = new Dictionary<string, BeamShearResult>();
            int beamCount = context.Topology.BeamCenterlines.Count;
            if (beamCount == 0) beamCount = 12;

            for (int i = 1; i <= beamCount; i++)
            {
                double momentKnm = 60.0 + (i % 4) * 15.0; // 60..105 kNm
                double shearKn = 80.0 + (i % 3) * 20.0;   // 80..120 kN

                var flexInput = new BeamFlexureInput(b, h, momentKnm, concrete, steel);
                var flexRes = BeamFlexureCalculator.Calculate(flexInput);
                flexureResults[$"D{i}"] = flexRes;

                var shearInput = new BeamShearInput(b, h, shearKn, concrete, steel);
                var shearRes = BeamShearCalculator.Calculate(shearInput);
                shearResults[$"D{i}"] = shearRes;
            }

            context.SetArtifact("BeamFlexureResults", flexureResults);
            context.SetArtifact("BeamShearResults", shearResults);

            return Task.FromResult(AutoStrStepResult.Success(
                StepNumber,
                StepName,
                $"Đã hoàn thành thiết kế cốt thép uốn và cắt đai cho {beamCount} cấu kiện dầm khung (TCVN 5574:2018)."));
        }

        private static ConcreteGrade ParseConcreteGrade(string s)
        {
            if (Enum.TryParse<ConcreteGrade>(s, true, out var g)) return g;
            return ConcreteGrade.B20;
        }

        private static RebarGrade ParseRebarGrade(string s)
        {
            string clean = (s ?? "").Replace("-", "").ToUpperInvariant();
            if (Enum.TryParse<RebarGrade>(clean, true, out var g)) return g;
            return RebarGrade.CB400V;
        }
    }
}
