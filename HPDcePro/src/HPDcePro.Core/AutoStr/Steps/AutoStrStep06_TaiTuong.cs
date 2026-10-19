using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HPDcePro.Core.AutoStr.Steps
{
    public sealed class AutoStrStep06_TaiTuong : AutoStrStepBase
    {
        public override int StepNumber => 6;
        public override string StepName => "Tính toán và gán tải trọng tường tác dụng lên dầm";

        protected override Task<AutoStrStepResult> ExecuteStepCoreAsync(
            AutoStrStepContext context, CancellationToken cancellationToken)
        {
            double rhoGach = context.Config.KhoiLuongRiengGach; // kg/m3 e.g. 1500
            double rhoVua = context.Config.KhoiLuongRiengVua;   // kg/m3 e.g. 1800
            double dTrat = context.Config.ChieuDayTrat / 1000.0; // m e.g. 0.03
            double tTuong = 0.200; // m (tường 200)
            double hTang = 3.6; // m
            double hDam = context.Config.ChieuCaoDamDienHinh / 1000.0; // m e.g. 0.4
            double hNet = Math.Max(0.5, hTang - hDam);

            // q = (rhoGach * tTuong + 2 * rhoVua * dTrat) * g * hNet in kN/m
            double g = 9.81 / 1000.0;
            double qWall = (rhoGach * tTuong + 2.0 * rhoVua * dTrat) * g * hNet;

            var beamLoads = new Dictionary<string, double>();
            int count = context.Topology.BeamCenterlines.Count;
            for (int i = 1; i <= count; i++)
            {
                beamLoads[$"D{i}"] = Math.Round(qWall, 2);
            }

            context.SetArtifact("WallLineLoads", beamLoads);
            context.SetArtifact("TypicalWallLoadKnM", Math.Round(qWall, 2));

            return Task.FromResult(AutoStrStepResult.Success(
                StepNumber,
                StepName,
                $"Đã tính toán và gán tải trọng tường xây: q_wall = {qWall:F2} kN/m cho {count} cấu kiện dầm."));
        }
    }
}
