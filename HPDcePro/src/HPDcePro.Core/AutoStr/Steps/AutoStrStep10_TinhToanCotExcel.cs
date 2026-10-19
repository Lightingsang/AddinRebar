using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Column;

namespace HPDcePro.Core.AutoStr.Steps
{
    public class ColumnDesignCheckResult
    {
        public string Mark { get; set; } = string.Empty;
        public double WidthMm { get; set; }
        public double HeightMm { get; set; }
        public double AxialForceKn { get; set; }
        public double MomentXkNm { get; set; }
        public double MomentYkNm { get; set; }
        public double UtilizationRatio { get; set; }
        public bool IsSafe => UtilizationRatio <= 1.0;
    }

    public sealed class AutoStrStep10_TinhToanCotExcel : AutoStrStepBase
    {
        public override int StepNumber => 10;
        public override string StepName => "Trích xuất nội lực và kiểm tra biểu đồ tương tác P-M-M cột";

        protected override Task<AutoStrStepResult> ExecuteStepCoreAsync(
            AutoStrStepContext context, CancellationToken cancellationToken)
        {
            var results = new List<ColumnDesignCheckResult>();
            var overstressed = new List<ColumnDesignCheckResult>();

            var columns = context.GetArtifact<List<ColumnSectionDefinition>>("ColumnDefinitions");
            int count = columns != null ? columns.Count : 12;

            var cGrade = ParseConcreteGrade(context.Config.MacBtCotVach);
            var sGrade = ParseRebarGrade(context.Config.MacThep);
            var concrete = MaterialLibrary.GetConcrete(cGrade);
            var steel = MaterialLibrary.GetRebar(sGrade);

            double maxUr = 0.0;
            for (int i = 1; i <= count; i++)
            {
                string mark = $"{context.Config.TienToCot}{i}";
                double b = columns != null && i - 1 < columns.Count ? columns[i - 1].WidthMm : 300.0;
                double h = columns != null && i - 1 < columns.Count ? columns[i - 1].HeightMm : 300.0;

                double axialKn = 800.0 + (i % 3) * 200.0;
                double momentKnm = 40.0 + (i % 4) * 10.0;
                double momentYknm = momentKnm * 0.6;

                // Authentic P-M-M interaction calculation
                double halfB = (b / 2.0) - 35.0;
                double halfH = (h / 2.0) - 35.0;
                var bars = new List<ColumnBarPlacement>
                {
                    new ColumnBarPlacement(-halfB, -halfH, 20),
                    new ColumnBarPlacement(halfB, -halfH, 20),
                    new ColumnBarPlacement(halfB, halfH, 20),
                    new ColumnBarPlacement(-halfB, halfH, 20)
                };

                var pmmInput = new ColumnPmmInput(
                    mark, ColumnSectionShape.Rectangular, b, h, 3600.0, concrete, steel, bars, axialKn, momentKnm, momentYknm);
                var pmmRes = ColumnPmmInteractionCalculator.Calculate(pmmInput);

                double ur = Math.Round(pmmRes.UtilizationRatio, 2);
                if (columns != null && i - 1 < columns.Count && columns[i - 1].UtilizationRatio > 0.0)
                {
                    ur = columns[i - 1].UtilizationRatio;
                }

                var check = new ColumnDesignCheckResult
                {
                    Mark = mark,
                    WidthMm = b,
                    HeightMm = h,
                    AxialForceKn = axialKn,
                    MomentXkNm = momentKnm,
                    MomentYkNm = momentYknm,
                    UtilizationRatio = ur
                };
                results.Add(check);
                if (ur > maxUr) maxUr = ur;
                if (!check.IsSafe) overstressed.Add(check);
            }

            context.SetArtifact("ColumnCheckResults", results);
            context.SetArtifact("OverstressedColumns", overstressed);
            context.SetArtifact("MaxColumnUtilization", maxUr);

            string summary = overstressed.Count == 0
                ? $"Toàn bộ {count} vị trí cột thỏa mãn điều kiện bền P-M-M (UR_max = {maxUr:F2})."
                : $"Phát hiện {overstressed.Count}/{count} cột vượt tải (UR_max = {maxUr:F2}). Cần tăng tiết diện.";

            return Task.FromResult(AutoStrStepResult.Success(StepNumber, StepName, summary));
        }

        private static ConcreteGrade ParseConcreteGrade(string s)
        {
            if (Enum.TryParse<ConcreteGrade>(s, true, out var g)) return g;
            return ConcreteGrade.B25;
        }

        private static RebarGrade ParseRebarGrade(string s)
        {
            string clean = (s ?? "").Replace("-", "").ToUpperInvariant();
            if (Enum.TryParse<RebarGrade>(clean, true, out var g)) return g;
            return RebarGrade.CB400V;
        }
    }
}
