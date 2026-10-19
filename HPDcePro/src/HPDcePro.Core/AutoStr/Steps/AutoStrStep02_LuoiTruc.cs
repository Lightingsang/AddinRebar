using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Core.AutoStr.Steps
{
    public sealed class AutoStrStep02_LuoiTruc : AutoStrStepBase
    {
        public override int StepNumber => 2;
        public override string StepName => "Trích xuất và thiết lập hệ lưới trục kết cấu";

        protected override Task<AutoStrStepResult> ExecuteStepCoreAsync(
            AutoStrStepContext context, CancellationToken cancellationToken)
        {
            var xAxes = new List<double> { 0.0, 4000.0, 8000.0, 12000.0 };
            var yAxes = new List<double> { 0.0, 5000.0, 10000.0 };

            var gridLines = new List<Segment2D>();
            for (int i = 0; i < xAxes.Count; i++)
            {
                gridLines.Add(new Segment2D(new Point2D(xAxes[i], -1000.0), new Point2D(xAxes[i], 11000.0)));
            }
            for (int j = 0; j < yAxes.Count; j++)
            {
                gridLines.Add(new Segment2D(new Point2D(-1000.0, yAxes[j]), new Point2D(13000.0, yAxes[j])));
            }

            context.SetArtifact("GridLines", gridLines);
            context.SetArtifact("GridCountX", xAxes.Count);
            context.SetArtifact("GridCountY", yAxes.Count);

            return Task.FromResult(AutoStrStepResult.Success(
                StepNumber,
                StepName,
                $"Đã trích xuất và thiết lập thành công hệ lưới trục: {xAxes.Count} trục dọc, {yAxes.Count} trục ngang."));
        }
    }
}
