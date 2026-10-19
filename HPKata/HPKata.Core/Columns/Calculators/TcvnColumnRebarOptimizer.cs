using System;
using System.Collections.Generic;
using HPKata.Core.Columns.Models;

namespace HPKata.Core.Columns.Calculators
{
    /// <summary>
    /// Auto-sizes column longitudinal reinforcement satisfying required Ast,
    /// 0.6% <= rho <= 3.5% limits, and clear spacing >= 50mm per TCVN 5574:2018.
    /// </summary>
    public static class TcvnColumnRebarOptimizer
    {
        private static readonly int[] StandardDiameters = { 18, 20, 22, 25, 28, 32 };

        public static ColumnSectionModel Optimize(
            double widthMm,
            double depthMm,
            double requiredAstMm2,
            double minRho = 0.6,
            double maxRho = 3.5,
            double coverMm = 30.0,
            double tieDiaMm = 8.0)
        {
            if (widthMm <= 0.0 || depthMm <= 0.0)
                throw new ArgumentException("Section dimensions must be positive.");

            double sectionArea = widthMm * depthMm;
            double minAst = sectionArea * (minRho / 100.0);
            double maxAst = sectionArea * (maxRho / 100.0);
            double targetAst = Math.Max(requiredAstMm2, minAst);

            ColumnSectionModel? bestCandidate = null;
            double minExcessArea = double.MaxValue;

            foreach (int cornerDia in StandardDiameters)
            {
                for (int nb = 0; nb <= 3; nb++)
                {
                    for (int nh = 0; nh <= 3; nh++)
                    {
                        var candidate = EvaluateCandidate(widthMm, depthMm, cornerDia, nb, nh, coverMm, tieDiaMm);
                        if (candidate == null) continue;

                        double provArea = candidate.TotalSteelAreaMm2;
                        if (provArea >= targetAst && provArea <= maxAst)
                        {
                            double excess = provArea - targetAst;
                            if (excess < minExcessArea)
                            {
                                minExcessArea = excess;
                                bestCandidate = candidate;
                            }
                        }
                    }
                }
            }

            return bestCandidate ?? CreateFallback(widthMm, depthMm, targetAst, coverMm, tieDiaMm);
        }

        private static ColumnSectionModel? EvaluateCandidate(
            double widthMm, double depthMm, int cornerDia, int nb, int nh, double coverMm, double tieDiaMm)
        {
            var sec = new ColumnSectionModel
            {
                WidthMm = widthMm,
                DepthMm = depthMm,
                CornerDiameterMm = cornerDia,
                SideCountB = nb,
                SideDiameterB = cornerDia,
                SideCountH = nh,
                SideDiameterH = cornerDia,
                IsCompliant = true
            };

            double clearSpacing = sec.CalculateMinClearSpacingMm(coverMm, tieDiaMm);
            if (clearSpacing < Math.Max(50.0, cornerDia))
            {
                return null;
            }

            return sec;
        }

        private static ColumnSectionModel CreateFallback(
            double widthMm, double depthMm, double targetAst, double coverMm, double tieDiaMm)
        {
            int dia = 20;
            double barArea = ColumnSectionModel.CalculateBarArea(dia);
            int neededCount = Math.Max(4, (int)Math.Ceiling(targetAst / barArea));
            int extraBars = Math.Max(0, neededCount - 4);
            int requestedNb = (int)Math.Ceiling(extraBars / 4.0);
            int requestedNh = (int)Math.Ceiling(extraBars / 4.0);

            int maxNb = CalculateMaxSideBars(widthMm, coverMm, tieDiaMm, dia, 50.0);
            int maxNh = CalculateMaxSideBars(depthMm, coverMm, tieDiaMm, dia, 50.0);

            var fallback = new ColumnSectionModel
            {
                WidthMm = widthMm,
                DepthMm = depthMm,
                CornerDiameterMm = dia,
                SideCountB = Math.Min(requestedNb, maxNb),
                SideDiameterB = dia,
                SideCountH = Math.Min(requestedNh, maxNh),
                SideDiameterH = dia,
                IsCompliant = false
            };

            EnsureNonOverlapping(fallback, coverMm, tieDiaMm);
            return fallback;
        }

        private static int CalculateMaxSideBars(
            double dimMm, double coverMm, double tieDiaMm, int dia, double minClearMm)
        {
            double rc = dia / 2.0;
            double core = dimMm - 2.0 * (coverMm + tieDiaMm + rc);
            if (core <= 0.0) return 0;

            double requiredPitch = minClearMm + dia;
            int maxSpaces = (int)Math.Floor(core / requiredPitch);
            return Math.Max(0, maxSpaces - 1);
        }

        private static void EnsureNonOverlapping(ColumnSectionModel fallback, double coverMm, double tieDiaMm)
        {
            double clear = fallback.CalculateMinClearSpacingMm(coverMm, tieDiaMm);
            if (clear < 0.0)
            {
                fallback.SideCountB = 0;
                fallback.SideCountH = 0;
            }
        }
    }
}
