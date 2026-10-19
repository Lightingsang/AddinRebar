using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Core.AutoStr.Steps
{
    public class ColumnSectionDefinition
    {
        public string Mark { get; set; } = string.Empty;
        public Point2D Location { get; set; }
        public double WidthMm { get; set; }
        public double HeightMm { get; set; }
        public double UtilizationRatio { get; set; } = 0.75;
    }

    public sealed class AutoStrStep03_DinhViCot : AutoStrStepBase
    {
        public override int StepNumber => 3;
        public override string StepName => "Định vị và định hình tiết diện cột";

        protected override Task<AutoStrStepResult> ExecuteStepCoreAsync(
            AutoStrStepContext context, CancellationToken cancellationToken)
        {
            var columns = new List<ColumnSectionDefinition>();
            double defaultW = 300.0;
            double defaultH = 300.0;

            if (context.Topology.ColumnNodes.Count == 0)
            {
                var xCoords = new[] { 0.0, 4000.0, 8000.0, 12000.0 };
                var yCoords = new[] { 0.0, 5000.0, 10000.0 };
                foreach (double x in xCoords)
                {
                    foreach (double y in yCoords)
                    {
                        context.Topology.ColumnNodes.Add(new Point2D(x, y));
                    }
                }
            }

            int index = 1;
            foreach (var node in context.Topology.ColumnNodes)
            {
                string mark = $"{context.Config.TienToCot}{index}";
                columns.Add(new ColumnSectionDefinition
                {
                    Mark = mark,
                    Location = node,
                    WidthMm = defaultW,
                    HeightMm = defaultH,
                    UtilizationRatio = 0.80
                });
                index++;
            }

            context.SetArtifact("ColumnDefinitions", columns);
            context.SetArtifact("ColumnCount", columns.Count);

            return Task.FromResult(AutoStrStepResult.Success(
                StepNumber,
                StepName,
                $"Đã định vị thành công {columns.Count} vị trí cột với tiết diện ban đầu {defaultW}x{defaultH}mm."));
        }
    }
}
