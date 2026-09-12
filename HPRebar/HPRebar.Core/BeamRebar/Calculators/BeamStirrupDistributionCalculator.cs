using System;
using System.Collections.Generic;
using HPRebar.Core.BeamRebar.Models;

namespace HPRebar.Core.BeamRebar.Calculators;

/// <summary>
/// Calculates closed stirrup distributions across clear spans, cantilevers, and support nodes.
/// Supports uniform and 3-zone layouts (L/4 and L/3) and enforces Revit API limits.
/// </summary>
public static class BeamStirrupDistributionCalculator
{
    public const int MaxBarPositions = 1002;
    public const double DefaultStartOffsetMm = 50.0;
    public const double MinimumThreeZoneSpanMm = 600.0;

    /// <summary>
    /// Computes stirrup runs across a single clear span.
    /// </summary>
    public static IReadOnlyList<StirrupRun> ComputeSpanRuns(
        double clearSpanMm,
        BeamStirrupSpec spec,
        bool isCantilever = false)
    {
        if (clearSpanMm <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(clearSpanMm), "Clear span must be strictly positive.");

        if (spec.SpacingDense <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(spec), "Dense spacing must be strictly positive.");

        if (spec.SpacingSparse <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(spec), "Sparse spacing must be strictly positive.");

        // Check if spacing is so small that bar count exceeds Revit max positions
        if ((clearSpanMm / spec.SpacingDense) > MaxBarPositions || (clearSpanMm / spec.SpacingSparse) > MaxBarPositions)
            throw new ArgumentOutOfRangeException(nameof(spec), $"Requested spacing produces bar count exceeding maximum {MaxBarPositions}.");

        // Cantilever spans: uniform dense spacing across entire length
        if (isCantilever)
        {
            double lDist = clearSpanMm - spec.StartOffset - spec.Cover;
            if (lDist < 0.0)
                return Array.Empty<StirrupRun>();

            int intervals = (int)Math.Floor(lDist / spec.SpacingDense);
            int count = intervals + 1;
            if (count > MaxBarPositions)
                throw new ArgumentOutOfRangeException(nameof(spec), $"Stirrup count {count} exceeds maximum {MaxBarPositions}.");

            double delta = (lDist - (intervals * spec.SpacingDense)) / 2.0;
            double startX = spec.StartOffset + delta;
            var positions = new List<double>(count);
            for (int i = 0; i < count; i++)
                positions.Add(startX + (i * spec.SpacingDense));

            return new[]
            {
                new StirrupRun
                {
                    Count = count,
                    Spacing = spec.SpacingDense,
                    StartOffset = startX,
                    Length = intervals * spec.SpacingDense,
                    StartX = startX,
                    EndX = startX + (intervals * spec.SpacingDense),
                    Positions = positions
                }
            };
        }

        // Uniform layout
        if (spec.Layout == StirrupLayout.Uniform)
        {
            double lDist = clearSpanMm - (2.0 * spec.StartOffset);
            if (lDist < 0.0)
                return Array.Empty<StirrupRun>();

            int intervals = (int)Math.Floor(lDist / spec.SpacingDense);
            int count = intervals + 1;
            if (count > MaxBarPositions)
                throw new ArgumentOutOfRangeException(nameof(spec), $"Stirrup count {count} exceeds maximum {MaxBarPositions}.");

            double delta = (lDist - (intervals * spec.SpacingDense)) / 2.0;
            double startX = spec.StartOffset + delta;
            var positions = new List<double>(count);
            for (int i = 0; i < count; i++)
                positions.Add(startX + (i * spec.SpacingDense));

            return new[]
            {
                new StirrupRun
                {
                    Count = count,
                    Spacing = spec.SpacingDense,
                    StartOffset = startX,
                    Length = intervals * spec.SpacingDense,
                    StartX = startX,
                    EndX = startX + (intervals * spec.SpacingDense),
                    Positions = positions
                }
            };
        }

        // 3-Zone layouts: short spans collapse to uniform dense layout
        double zoneLength = spec.Layout == StirrupLayout.ThreeZoneL4 ? (clearSpanMm / 4.0) : (clearSpanMm / 3.0);
        if (clearSpanMm < MinimumThreeZoneSpanMm || zoneLength <= spec.StartOffset)
        {
            return ComputeSpanRuns(clearSpanMm, spec with { Layout = StirrupLayout.Uniform, SpacingSparse = spec.SpacingDense }, isCantilever: false);
        }

        double l1 = zoneLength;
        double l3 = zoneLength;
        double l2 = clearSpanMm - l1 - l3;

        // Zone 1 (Left Support Zone)
        double lDist1 = l1 - spec.StartOffset;
        int intervals1 = (int)Math.Floor(lDist1 / spec.SpacingDense);
        int count1 = intervals1 + 1;
        if (count1 > MaxBarPositions)
            throw new ArgumentOutOfRangeException(nameof(spec), $"Zone 1 stirrup count {count1} exceeds maximum {MaxBarPositions}.");

        double delta1 = (lDist1 - (intervals1 * spec.SpacingDense)) / 2.0;
        double startX1 = spec.StartOffset + delta1;
        var positions1 = new List<double>(count1);
        for (int i = 0; i < count1; i++)
            positions1.Add(startX1 + (i * spec.SpacingDense));

        var run1 = new StirrupRun
        {
            Count = count1,
            Spacing = spec.SpacingDense,
            StartOffset = startX1,
            Length = intervals1 * spec.SpacingDense,
            StartX = startX1,
            EndX = startX1 + (intervals1 * spec.SpacingDense),
            Positions = positions1
        };

        // Zone 3 (Right Support Zone) computed first to establish exact right boundary
        int count3 = count1;
        double startX3 = (clearSpanMm - l3) + delta1;
        var positions3 = new List<double>(count3);
        for (int i = 0; i < count3; i++)
            positions3.Add(startX3 + (i * spec.SpacingDense));

        var run3 = new StirrupRun
        {
            Count = count3,
            Spacing = spec.SpacingDense,
            StartOffset = startX3,
            Length = intervals1 * spec.SpacingDense,
            StartX = startX3,
            EndX = startX3 + (intervals1 * spec.SpacingDense),
            Positions = positions3
        };

        // Zone 2 (Midspan Sparse Zone)
        // Positioned symmetrically within the physical gap between Zone 1 and Zone 3:
        // gap = startX3 - lastX1.
        // Guarantees boundary transition spacing dBoundary satisfies: s2/2 < dBoundary <= s2,
        // eliminating duplicate/clashing stirrups at zone transitions.
        double lastX1 = startX1 + (intervals1 * spec.SpacingDense);
        double gap = startX3 - lastX1;

        int count2;
        int intervals2;
        double startX2;
        var positions2 = new List<double>();

        if (gap <= 0.0)
        {
            count2 = 0;
            intervals2 = 0;
            startX2 = lastX1;
        }
        else
        {
            double y2 = (gap / spec.SpacingSparse) - 2.0;
            intervals2 = (int)Math.Ceiling(y2 - 1e-9);
            if (intervals2 < 0)
                intervals2 = 0;

            if (gap < 2.0 * Math.Min(spec.SpacingDense, spec.SpacingSparse))
            {
                if (gap >= 2.0 * DefaultStartOffsetMm)
                {
                    count2 = 1;
                    intervals2 = 0;
                    startX2 = (lastX1 + startX3) / 2.0;
                    positions2.Add(startX2);
                }
                else
                {
                    count2 = 0;
                    intervals2 = 0;
                    startX2 = lastX1;
                }
            }
            else
            {
                count2 = intervals2 + 1;
                if (count2 > MaxBarPositions)
                    throw new ArgumentOutOfRangeException(nameof(spec), $"Zone 2 stirrup count {count2} exceeds maximum {MaxBarPositions}.");

                double delta2 = (gap - (intervals2 * spec.SpacingSparse)) / 2.0;
                startX2 = lastX1 + delta2;
                for (int i = 0; i < count2; i++)
                    positions2.Add(startX2 + (i * spec.SpacingSparse));
            }
        }

        var run2 = new StirrupRun
        {
            Count = count2,
            Spacing = spec.SpacingSparse,
            StartOffset = startX2,
            Length = count2 > 0 ? intervals2 * spec.SpacingSparse : 0.0,
            StartX = startX2,
            EndX = count2 > 0 ? startX2 + (intervals2 * spec.SpacingSparse) : startX2,
            Positions = positions2
        };

        return new[] { run1, run2, run3 };
    }

    /// <summary>
    /// Computes stirrup ties across a support column width.
    /// </summary>
    public static StirrupRun ComputeNodeRun(
        double supportWidthMm,
        double coverMm,
        double spacingMm)
    {
        if (spacingMm <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(spacingMm), "Spacing must be strictly positive.");

        double lNode = supportWidthMm - (2.0 * coverMm);
        if (lNode <= 0.0)
            return new StirrupRun { Count = 0, Spacing = spacingMm };

        int intervals = (int)Math.Floor(lNode / spacingMm);
        int count = intervals + 1;
        if (count > MaxBarPositions)
            throw new ArgumentOutOfRangeException(nameof(spacingMm), $"Node stirrup count {count} exceeds maximum {MaxBarPositions}.");

        double delta = (lNode - (intervals * spacingMm)) / 2.0;
        double startOffset = coverMm + delta;
        var positions = new List<double>(count);
        for (int i = 0; i < count; i++)
            positions.Add(startOffset + (i * spacingMm));

        return new StirrupRun
        {
            Count = count,
            Spacing = spacingMm,
            StartOffset = startOffset,
            Length = intervals * spacingMm,
            StartX = startOffset,
            EndX = startOffset + (intervals * spacingMm),
            Positions = positions
        };
    }

    /// <summary>
    /// Computes theoretical lengths of the three zones for a given clear span.
    /// </summary>
    public static (double L1, double L2, double L3) ComputeZoneLengths(
        double clearSpanMm,
        StirrupDistributionType type)
    {
        return type switch
        {
            StirrupDistributionType.ThreeZoneL4 => (clearSpanMm / 4.0, clearSpanMm / 2.0, clearSpanMm / 4.0),
            StirrupDistributionType.ThreeZoneL3 => (clearSpanMm / 3.0, clearSpanMm / 3.0, clearSpanMm / 3.0),
            _ => (0.0, clearSpanMm, 0.0)
        };
    }

    public static (double L1, double L2, double L3) ComputeZoneLengths(
        double clearSpanMm,
        StirrupLayout layout) =>
        ComputeZoneLengths(clearSpanMm, (StirrupDistributionType)(int)layout);

    /// <summary>
    /// Computes all stirrup runs across all spans and optional support nodes of a continuous beam stack.
    /// </summary>
    public static IReadOnlyList<StirrupRun> ComputeStackRuns(
        BeamContinuousStack stack,
        BeamStirrupSpec spec)
    {
        var allRuns = new List<StirrupRun>();
        for (int i = 0; i < stack.Spans.Count; i++)
        {
            var span = stack.Spans[i];
            var runs = ComputeSpanRuns(span.LengthClear, spec, span.IsCantilever);
            allRuns.AddRange(runs);
        }

        if (spec.IncludeStirrupsInNodes)
        {
            for (int i = 1; i < stack.Supports.Count - 1; i++)
            {
                var support = stack.Supports[i];
                var nodeRun = ComputeNodeRun(support.Width, spec.Cover, spec.NodeSpacing);
                if (nodeRun.Count > 0)
                    allRuns.Add(nodeRun);
            }
        }

        return allRuns;
    }
}
