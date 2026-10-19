using System;
using System.Collections.Generic;

namespace HPKata.Core.Beams.Calculators
{
    /// <summary>
    /// Result of TCVN rebar optimization for a beam section station.
    /// </summary>
    public sealed class RebarOptimizationResult
    {
        public string Notation { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public int PrimaryDiameterMm { get; set; }
        public int SecondaryDiameterMm { get; set; }
        public int PrimaryCount { get; set; }
        public int SecondaryCount { get; set; }
        public double AreaProvidedMm2 { get; set; }
        public double AreaRequiredMm2 { get; set; }
        public double ClearGapMm { get; set; }
        public int LayerCount { get; set; } = 1;
        public bool IsCompliant { get; set; }
    }

    /// <summary>
    /// Optimizes and auto-selects longitudinal beam rebar configurations according to TCVN 5574:2018.
    /// </summary>
    public static class TcvnBeamRebarOptimizer
    {
        private static readonly int[] CandidateDiameters = { 16, 18, 20, 22, 25, 28, 32 };

        public static RebarOptimizationResult Optimize(
            double requiredAsMm2,
            double beamWidthMm,
            bool isTopBar,
            double coverMm = 25.0,
            int stirrupDiaMm = 8,
            double beamHeightMm = 0.0)
        {
            if (requiredAsMm2 <= 0.0)
            {
                return CreateDefaultMinimum(isTopBar, beamWidthMm, coverMm, stirrupDiaMm);
            }

            // TCVN 5574:2018 §10.3.1.1 minimum flexural reinforcement ratio: As_min >= 0.001 * b * h0
            double effHeight = beamHeightMm > 0.0 ? beamHeightMm : (beamWidthMm * 2.3);
            double h0 = Math.Max(100.0, effHeight - coverMm - stirrupDiaMm - 10.0);
            double asMinTcvn = 0.001 * beamWidthMm * h0;
            double effectiveAs = Math.Max(requiredAsMm2, asMinTcvn);

            double usableWidth = beamWidthMm - 2.0 * (coverMm + stirrupDiaMm);
            double minGap = isTopBar ? 30.0 : 25.0;

            RebarOptimizationResult? bestResult = null;
            double bestScore = double.MaxValue;

            // 1. Try single diameter options (2 to 6 bars)
            TrySingleDiameters(effectiveAs, usableWidth, minGap, ref bestResult, ref bestScore);

            // 2. Try dual-diameter pairings in 1 layer: 2 corner bars + m interior bars
            TryDualDiameterPairings(effectiveAs, usableWidth, minGap, ref bestResult, ref bestScore);

            // 3. If no 1-layer configuration fits, split into 2 layers
            if (bestResult == null)
            {
                bestResult = CreateTwoLayerFallback(effectiveAs, usableWidth, minGap);
            }

            return bestResult;
        }

        private static void TrySingleDiameters(
            double requiredAsMm2,
            double usableWidth,
            double minGap,
            ref RebarOptimizationResult? bestResult,
            ref double bestScore)
        {
            foreach (int dia in CandidateDiameters)
            {
                double singleArea = Math.PI * dia * dia / 4.0;
                int count = Math.Max(2, (int)Math.Ceiling(requiredAsMm2 / singleArea));
                if (count > 6) continue;

                double gap = (usableWidth - (count * dia)) / (count - 1);
                double totalArea = count * singleArea;

                if (gap >= Math.Max(minGap, dia))
                {
                    double wastePercent = ((totalArea - requiredAsMm2) / requiredAsMm2) * 100.0;
                    double score = wastePercent + (count * 2.0) + (dia >= 18 && dia <= 22 ? 0.0 : 10.0);

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestResult = new RebarOptimizationResult
                        {
                            Notation = $"{count}Ø{dia}",
                            TotalCount = count,
                            PrimaryDiameterMm = dia,
                            PrimaryCount = count,
                            AreaProvidedMm2 = Math.Round(totalArea, 1),
                            AreaRequiredMm2 = requiredAsMm2,
                            ClearGapMm = Math.Round(gap, 1),
                            LayerCount = 1,
                            IsCompliant = true
                        };
                    }
                }
            }
        }

        private static void TryDualDiameterPairings(
            double requiredAsMm2,
            double usableWidth,
            double minGap,
            ref RebarOptimizationResult? bestResult,
            ref double bestScore)
        {
            for (int i = 0; i < CandidateDiameters.Length; i++)
            {
                int diaCorner = CandidateDiameters[i];
                for (int j = 0; j < CandidateDiameters.Length; j++)
                {
                    int diaMid = CandidateDiameters[j];
                    if (Math.Abs(diaCorner - diaMid) > 4 || diaCorner <= diaMid) continue;
                    CheckCornerAndMidBars(diaCorner, diaMid, requiredAsMm2, usableWidth, minGap, ref bestResult, ref bestScore);
                }
            }
        }

        private static void CheckCornerAndMidBars(
            int diaCorner,
            int diaMid,
            double requiredAsMm2,
            double usableWidth,
            double minGap,
            ref RebarOptimizationResult? bestResult,
            ref double bestScore)
        {
            double cornerArea = 2.0 * (Math.PI * diaCorner * diaCorner / 4.0);
            double midArea = Math.PI * diaMid * diaMid / 4.0;

            for (int m = 1; m <= 3; m++)
            {
                int totalBars = 2 + m;
                double totalArea = cornerArea + (m * midArea);
                if (totalArea < requiredAsMm2) continue;

                double totalBarWidth = (2.0 * diaCorner) + (m * diaMid);
                double gap = (usableWidth - totalBarWidth) / (totalBars - 1);

                if (gap >= Math.Max(minGap, Math.Max(diaCorner, diaMid)))
                {
                    double waste = ((totalArea - requiredAsMm2) / requiredAsMm2) * 100.0;
                    double score = waste + 5.0 + (totalBars * 2.0);

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestResult = new RebarOptimizationResult
                        {
                            Notation = $"2Ø{diaCorner} + {m}Ø{diaMid}",
                            TotalCount = totalBars,
                            PrimaryDiameterMm = diaCorner,
                            PrimaryCount = 2,
                            SecondaryDiameterMm = diaMid,
                            SecondaryCount = m,
                            AreaProvidedMm2 = Math.Round(totalArea, 1),
                            AreaRequiredMm2 = requiredAsMm2,
                            ClearGapMm = Math.Round(gap, 1),
                            LayerCount = 1,
                            IsCompliant = true
                        };
                    }
                }
            }
        }

        private static RebarOptimizationResult CreateTwoLayerFallback(
            double requiredAsMm2, double usableWidth, double minGap)
        {
            RebarOptimizationResult? bestFallback = null;
            double bestGap = double.MinValue;

            for (int i = CandidateDiameters.Length - 1; i >= 0; i--)
            {
                int dia = CandidateDiameters[i];
                double singleArea = Math.PI * dia * dia / 4.0;
                int totalBars = Math.Max(4, (int)Math.Ceiling(requiredAsMm2 / singleArea));
                int layer1 = (totalBars + 1) / 2;
                int layer2 = totalBars - layer1;

                double gap = usableWidth > 0.0 && layer1 > 1
                    ? (usableWidth - (layer1 * dia)) / (layer1 - 1)
                    : (usableWidth - dia);
                double totalArea = totalBars * singleArea;
                bool compliant = usableWidth > 0.0 && gap >= Math.Max(20.0, minGap);

                var candidate = new RebarOptimizationResult
                {
                    Notation = $"{layer1}Ø{dia} + {layer2}Ø{dia} (2 lớp)",
                    TotalCount = totalBars,
                    PrimaryDiameterMm = dia,
                    PrimaryCount = layer1,
                    SecondaryDiameterMm = dia,
                    SecondaryCount = layer2,
                    AreaProvidedMm2 = Math.Round(totalArea, 1),
                    AreaRequiredMm2 = requiredAsMm2,
                    ClearGapMm = Math.Round(gap, 1),
                    LayerCount = 2,
                    IsCompliant = compliant
                };

                if (compliant) return candidate;

                if (bestFallback == null || gap > bestGap)
                {
                    bestGap = gap;
                    bestFallback = candidate;
                }
            }

            return bestFallback!;
        }

        private static RebarOptimizationResult CreateDefaultMinimum(
            bool isTopBar, double beamWidthMm, double coverMm, int stirrupDiaMm)
        {
            double minGap = isTopBar ? 30.0 : 25.0;
            double usableWidth = beamWidthMm - 2.0 * (coverMm + stirrupDiaMm);
            int[] minCandidates = { 18, 16, 14, 12, 10 };

            int selectedDia = 18;
            double bestGap = usableWidth - (2.0 * selectedDia);

            foreach (int dia in minCandidates)
            {
                double gap = usableWidth - (2.0 * dia);
                if (gap >= minGap)
                {
                    selectedDia = dia;
                    bestGap = gap;
                    break;
                }
                if (gap > bestGap)
                {
                    selectedDia = dia;
                    bestGap = gap;
                }
            }

            double singleArea = Math.PI * selectedDia * selectedDia / 4.0;
            bool compliant = usableWidth > 0.0 && bestGap >= minGap;

            return new RebarOptimizationResult
            {
                Notation = $"2Ø{selectedDia}",
                TotalCount = 2,
                PrimaryDiameterMm = selectedDia,
                PrimaryCount = 2,
                AreaProvidedMm2 = Math.Round(2.0 * singleArea, 1),
                AreaRequiredMm2 = 0.0,
                ClearGapMm = Math.Round(bestGap, 1),
                LayerCount = 1,
                IsCompliant = compliant
            };
        }
    }
}
