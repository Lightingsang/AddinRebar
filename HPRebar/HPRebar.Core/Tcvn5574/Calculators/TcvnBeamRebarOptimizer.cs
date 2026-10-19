using System;
using System.Collections.Generic;
using HPRebar.Core.Tcvn5574.Models;

namespace HPRebar.Core.Tcvn5574.Calculators;

/// <summary>
/// Rebar selection optimizer and clear spacing validator per TCVN 5574:2018 Clause 10.3.1.2.
/// Evaluates candidate bar combinations, verifies spacing in 1 or 2 layers, and selects the most efficient layout.
/// </summary>
public static class TcvnBeamRebarOptimizer
{
    private static readonly double[] StandardDiameters = [14.0, 16.0, 18.0, 20.0, 22.0, 25.0, 28.0, 32.0];

    /// <summary>
    /// Validates clear horizontal spacing for a single layer of uniform bars.
    /// </summary>
    public static (double Spacing, bool IsCompliant, double RequiredSpacing) ValidateClearSpacing(
        int barCount,
        double barDiameter,
        double b,
        double cover = 25.0,
        double stirrupDiameter = 8.0,
        bool isTop = false)
    {
        double requiredSpacing = isTop ? Math.Max(30.0, barDiameter) : Math.Max(25.0, barDiameter);

        if (barCount <= 0 || b <= 0.0)
            return (0.0, false, requiredSpacing);

        double clearWidth = b - (2.0 * (cover + stirrupDiameter));
        if (barCount == 1)
        {
            double singleSpacing = clearWidth - barDiameter;
            return (singleSpacing, singleSpacing >= requiredSpacing, requiredSpacing);
        }

        double totalBarWidth = barCount * barDiameter;
        double spacing = (clearWidth - totalBarWidth) / (barCount - 1);
        bool isCompliant = spacing >= requiredSpacing;

        return (spacing, isCompliant, requiredSpacing);
    }

    /// <summary>
    /// Selects the optimal rebar layout satisfying required area and TCVN 5574:2018 clear spacing.
    /// </summary>
    public static RebarOption Optimize(
        double asRequired,
        double b,
        double h,
        double cover = 25.0,
        double stirrupDiameter = 8.0,
        bool isTop = false)
    {
        var candidates = GenerateCandidates(asRequired, b, cover, stirrupDiameter, isTop);

        RebarOption? bestValid = null;
        double bestScore = double.MaxValue;

        foreach (var candidate in candidates)
        {
            if (!candidate.IsValid) continue;

            double score = CalculateOptionScore(candidate);
            if (score < bestScore)
            {
                bestScore = score;
                bestValid = candidate;
            }
        }

        return bestValid ?? CreateCrowdedFallback(asRequired, b, cover, stirrupDiameter, isTop);
    }

    /// <summary>
    /// Generates candidate 1-layer and 2-layer rebar arrangements meeting or exceeding required area.
    /// </summary>
    public static IReadOnlyList<RebarOption> GenerateCandidates(
        double asRequired,
        double b,
        double cover = 25.0,
        double stirrupDiameter = 8.0,
        bool isTop = false)
    {
        var results = new List<RebarOption>();
        AddSingleDiameterCandidates(results, asRequired, b, cover, stirrupDiameter, isTop);
        AddDualDiameterCandidates(results, asRequired, b, cover, stirrupDiameter, isTop);
        return results;
    }

    private static void AddSingleDiameterCandidates(
        List<RebarOption> list, double asReq, double b, double cover, double dsw, bool isTop)
    {
        foreach (double d in StandardDiameters)
        {
            double singleArea = Math.PI * d * d / 4.0;
            for (int count = 2; count <= 8; count++)
            {
                double provArea = count * singleArea;
                if (provArea < asReq) continue;

                var opt1 = EvaluateSingleLayer(count, d, provArea, asReq, b, cover, dsw, isTop);
                if (opt1.IsValid)
                {
                    list.Add(opt1);
                }
                else if (count >= 4)
                {
                    var opt2 = EvaluateTwoLayersUniform(count, d, provArea, asReq, b, cover, dsw, isTop);
                    list.Add(opt2);
                }
            }
        }
    }

    private static void AddDualDiameterCandidates(
        List<RebarOption> list, double asReq, double b, double cover, double dsw, bool isTop)
    {
        for (int i = 0; i < StandardDiameters.Length - 1; i++)
        {
            double d1 = StandardDiameters[i + 1]; // larger outer
            double d2 = StandardDiameters[i];     // smaller inner
            if (d1 - d2 > 4.0) continue;          // maximum adjacent difference 4mm

            double area1 = Math.PI * d1 * d1 / 4.0;
            double area2 = Math.PI * d2 * d2 / 4.0;

            // Common symmetric layouts: 2 corner d1 + 1 or 2 inner d2
            for (int n2 = 1; n2 <= 2; n2++)
            {
                double provArea = (2 * area1) + (n2 * area2);
                if (provArea < asReq) continue;

                var opt = EvaluateDualLayer1(2, d1, n2, d2, provArea, asReq, b, cover, dsw, isTop);
                list.Add(opt);
            }
        }
    }

    private static RebarOption EvaluateSingleLayer(
        int count, double d, double provArea, double asReq, double b, double cover, double dsw, bool isTop)
    {
        var (spacing, isCompliant, _) = ValidateClearSpacing(count, d, b, cover, dsw, isTop);
        double waste = asReq > 0 ? ((provArea - asReq) / asReq) * 100.0 : 0.0;

        return new RebarOption
        {
            CountLayer1 = count,
            DiameterLayer1 = d,
            CountLayer2 = 0,
            DiameterLayer2 = 0.0,
            ProvidedArea = provArea,
            ClearSpacingLayer1 = spacing,
            ClearSpacingLayer2 = 0.0,
            WasteRatioPercent = waste,
            IsSpacingCompliant = isCompliant,
            IsValid = isCompliant && provArea >= asReq,
            Description = $"{count}d{d:0}"
        };
    }

    private static RebarOption EvaluateTwoLayersUniform(
        int totalCount, double d, double provArea, double asReq, double b, double cover, double dsw, bool isTop)
    {
        int count1 = (int)Math.Ceiling(totalCount / 2.0);
        int count2 = totalCount - count1;

        var (s1, c1, _) = ValidateClearSpacing(count1, d, b, cover, dsw, isTop);
        var (s2, c2, _) = ValidateClearSpacing(count2, d, b, cover, dsw, isTop);
        bool spacingOk = c1 && (count2 <= 1 || c2);
        double waste = asReq > 0 ? ((provArea - asReq) / asReq) * 100.0 : 0.0;

        return new RebarOption
        {
            CountLayer1 = count1,
            DiameterLayer1 = d,
            CountLayer2 = count2,
            DiameterLayer2 = d,
            ProvidedArea = provArea,
            ClearSpacingLayer1 = s1,
            ClearSpacingLayer2 = s2,
            WasteRatioPercent = waste,
            IsSpacingCompliant = spacingOk,
            IsValid = spacingOk && provArea >= asReq,
            Description = $"{count1}d{d:0} + {count2}d{d:0} (2 lớp)"
        };
    }

    private static RebarOption EvaluateDualLayer1(
        int n1, double d1, int n2, double d2, double provArea, double asReq, double b, double cover, double dsw, bool isTop)
    {
        int totalCount = n1 + n2;
        double clearWidth = b - (2.0 * (cover + dsw));
        double totalBarsWidth = (n1 * d1) + (n2 * d2);
        double spacing = (clearWidth - totalBarsWidth) / (totalCount - 1);
        double reqSpacing = isTop ? Math.Max(30.0, Math.Max(d1, d2)) : Math.Max(25.0, Math.Max(d1, d2));
        bool isCompliant = spacing >= reqSpacing;
        double waste = asReq > 0 ? ((provArea - asReq) / asReq) * 100.0 : 0.0;

        return new RebarOption
        {
            CountLayer1 = totalCount,
            DiameterLayer1 = d1,
            CountLayer2 = 0,
            DiameterLayer2 = 0.0,
            ProvidedArea = provArea,
            ClearSpacingLayer1 = spacing,
            ClearSpacingLayer2 = 0.0,
            WasteRatioPercent = waste,
            IsSpacingCompliant = isCompliant,
            IsValid = isCompliant && provArea >= asReq,
            Description = $"{n1}d{d1:0} + {n2}d{d2:0}"
        };
    }

    private static double CalculateOptionScore(RebarOption option)
    {
        double score = option.WasteRatioPercent; // base score: waste percentage

        if (option.LayerCount > 1)
            score += 25.0; // prefer single layer for concrete compaction

        // Prefer standard diameters 18, 20, 22
        if (option.DiameterLayer1 is >= 18.0 and <= 22.0)
            score -= 5.0;

        return score;
    }

    private static RebarOption CreateCrowdedFallback(
        double asReq, double b, double cover, double dsw, bool isTop)
    {
        // Fallback for extreme cases where no standard layout fits
        double d = 25.0;
        int count = Math.Max(2, (int)Math.Ceiling(asReq / (Math.PI * d * d / 4.0)));
        var (s, _, req) = ValidateClearSpacing(count, d, b, cover, dsw, isTop);

        return new RebarOption
        {
            CountLayer1 = count,
            DiameterLayer1 = d,
            CountLayer2 = 0,
            DiameterLayer2 = 0.0,
            ProvidedArea = count * (Math.PI * d * d / 4.0),
            ClearSpacingLayer1 = s,
            ClearSpacingLayer2 = 0.0,
            WasteRatioPercent = 0.0,
            IsSpacingCompliant = false,
            IsValid = false,
            Description = $"{count}d{d:0} (Cốt thép quá dày: t = {s:0.1}mm < {req:0.1}mm)"
        };
    }
}
