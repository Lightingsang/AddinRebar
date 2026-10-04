using System;
using System.Collections.Generic;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.Shared;

namespace HPRebar.Core.BeamRebar.Calculators;

/// <summary>
/// Calculates closed stirrup distributions across clear spans, cantilevers, and support nodes.
/// Supports uniform and 3-zone layouts (L/4 and L/3) and enforces Revit API limits.
/// </summary>
public static class BeamStirrupDistributionCalculator
{
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
        // NaN and infinity pass a plain <= 0 test and then poison every stirrup position.
        if (!FiniteNumber.IsPositive(clearSpanMm))
        {
            throw new ArgumentOutOfRangeException(nameof(clearSpanMm), "Clear span must be strictly positive.");
        }

        if (!FiniteNumber.IsPositive(spec.SpacingDense))
        {
            throw new ArgumentOutOfRangeException(nameof(spec), "Dense spacing must be strictly positive.");
        }

        if (!FiniteNumber.IsPositive(spec.SpacingSparse))
        {
            throw new ArgumentOutOfRangeException(nameof(spec), "Sparse spacing must be strictly positive.");
        }

        if (!FiniteNumber.IsFinite(spec.StartOffset))
        {
            throw new ArgumentOutOfRangeException(nameof(spec), "Start offset must be a finite number.");
        }

        // Check if spacing is so small that bar count exceeds Revit max positions
        if ((clearSpanMm / spec.SpacingDense) > RevitRebarLimits.MaxBarPositions
            || (clearSpanMm / spec.SpacingSparse) > RevitRebarLimits.MaxBarPositions)
            throw new ArgumentOutOfRangeException(
                nameof(spec), $"Requested spacing produces bar count exceeding maximum {RevitRebarLimits.MaxBarPositions}.");

        // A cantilever is dense over its whole length, from the support to the cover at its tip.
        if (isCantilever)
        {
            return SingleDenseRun(spec.StartOffset, clearSpanMm - spec.StartOffset - spec.Cover, spec.SpacingDense);
        }

        if (spec.Layout == StirrupLayout.Uniform)
        {
            return SingleDenseRun(spec.StartOffset, clearSpanMm - (2.0 * spec.StartOffset), spec.SpacingDense);
        }

        // 3-Zone layouts: short spans collapse to uniform dense layout
        double zoneLength = spec.Layout == StirrupLayout.ThreeZoneL4 ? (clearSpanMm / 4.0) : (clearSpanMm / 3.0);
        if (clearSpanMm < MinimumThreeZoneSpanMm || zoneLength <= spec.StartOffset)
        {
            return ComputeSpanRuns(
                clearSpanMm, spec with { Layout = StirrupLayout.Uniform, SpacingSparse = spec.SpacingDense }, isCantilever: false);
        }

        return ThreeZoneRuns(clearSpanMm, zoneLength, spec);
    }

    /// <summary>
    /// Computes stirrup ties across a support column width.
    /// </summary>
    public static StirrupRun ComputeNodeRun(
        double supportWidthMm,
        double coverMm,
        double spacingMm)
    {
        if (!FiniteNumber.IsPositive(spacingMm))
        {
            throw new ArgumentOutOfRangeException(nameof(spacingMm), "Spacing must be strictly positive.");
        }

        double lNode = supportWidthMm - (2.0 * coverMm);
        if (lNode <= 0.0)
            return new StirrupRun { Count = 0, Spacing = spacingMm };

        var (intervals, delta) = FitSpacings(lNode, spacingMm, "Node stirrup", nameof(spacingMm));
        int count = intervals + 1;

        return Run(coverMm + delta, intervals, count, spacingMm);
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

    /// <summary>One run at <paramref name="spacing"/>, centered in the distribution length after the start offset; none when that length is negative.</summary>
    private static IReadOnlyList<StirrupRun> SingleDenseRun(double startOffset, double distributionLength, double spacing)
    {
        if (distributionLength < 0.0)
            return Array.Empty<StirrupRun>();

        var (intervals, delta) = FitSpacings(distributionLength, spacing, "Stirrup", "spec");
        int count = intervals + 1;

        return new[] { Run(startOffset + delta, intervals, count, spacing) };
    }

    /// <summary>
    /// Dense support zones of <paramref name="zoneLength"/> at both ends — the right one mirrors the left so the
    /// pattern is symmetric — and a sparse midspan zone fitted into the gap left between them.
    /// </summary>
    private static IReadOnlyList<StirrupRun> ThreeZoneRuns(double clearSpanMm, double zoneLength, BeamStirrupSpec spec)
    {
        var (intervals1, delta1) = FitSpacings(
            zoneLength - spec.StartOffset, spec.SpacingDense, "Zone 1 stirrup", nameof(spec));
        int count1 = intervals1 + 1;

        double startX1 = spec.StartOffset + delta1;
        double startX3 = (clearSpanMm - zoneLength) + delta1;
        var run1 = Run(startX1, intervals1, count1, spec.SpacingDense);
        var run3 = Run(startX3, intervals1, count1, spec.SpacingDense);

        double lastX1 = startX1 + (intervals1 * spec.SpacingDense);
        var run2 = MidspanRun(lastX1, startX3, spec);

        return new[] { run1, run2, run3 };
    }

    /// <summary>
    /// The sparse zone between the last stirrup of zone 1 and the first of zone 3, centered in that gap so the
    /// spacing at each zone boundary d satisfies s2/2 &lt; d &lt;= s2 (no doubled stirrups at a transition).
    /// A gap narrower than two spacings takes one stirrup in its middle, or none when it is under 100 mm.
    /// </summary>
    private static StirrupRun MidspanRun(double lastX1, double startX3, BeamStirrupSpec spec)
    {
        double gap = startX3 - lastX1;
        if (gap <= 0.0)
            return Run(lastX1, intervals: 0, count: 0, spec.SpacingSparse);

        if (gap < 2.0 * Math.Min(spec.SpacingDense, spec.SpacingSparse))
        {
            return gap >= 2.0 * DefaultStartOffsetMm
                ? Run((lastX1 + startX3) / 2.0, intervals: 0, count: 1, spec.SpacingSparse)
                : Run(lastX1, intervals: 0, count: 0, spec.SpacingSparse);
        }

        double y2 = (gap / spec.SpacingSparse) - 2.0;
        double fitted2 = Math.Ceiling(y2 - 1e-9);
        if (!(fitted2 > 0.0))
        {
            // Also catches NaN, which the old int clamp turned into 0.
            fitted2 = 0.0;
        }

        EnsureWithinLimit(fitted2 + 1, "Zone 2 stirrup", nameof(spec));
        int intervals2 = (int)fitted2;
        int count2 = intervals2 + 1;

        double delta2 = (gap - (intervals2 * spec.SpacingSparse)) / 2.0;
        return Run(lastX1 + delta2, intervals2, count2, spec.SpacingSparse);
    }

    /// <summary>
    /// Fits whole spacings into <paramref name="length"/>; <c>Delta</c> is half the length left over, the margin
    /// at each end that centers the stirrups. The count is checked against Revit's limit before it becomes an int,
    /// so a tiny spacing is refused instead of overflowing.
    /// </summary>
    private static (int Intervals, double Delta) FitSpacings(
        double length, double spacing, string what, string paramName)
    {
        double fitted = Math.Floor(length / spacing);
        EnsureWithinLimit(fitted + 1, what, paramName);
        int intervals = (int)fitted;
        double delta = (length - (intervals * spacing)) / 2.0;
        return (intervals, delta);
    }

    private static void EnsureWithinLimit(double count, string what, string paramName)
    {
        if (count > RevitRebarLimits.MaxBarPositions)
        {
            throw new ArgumentOutOfRangeException(
                paramName, $"{what} count {count:0} exceeds maximum {RevitRebarLimits.MaxBarPositions}.");
        }
    }

    /// <summary><paramref name="count"/> stirrups from <paramref name="startX"/>, <paramref name="intervals"/> spacings long.</summary>
    private static StirrupRun Run(double startX, int intervals, int count, double spacing)
    {
        var positions = new List<double>(count);
        for (int i = 0; i < count; i++)
            positions.Add(startX + (i * spacing));

        return new StirrupRun
        {
            Count = count,
            Spacing = spacing,
            StartOffset = startX,
            Length = intervals * spacing,
            StartX = startX,
            EndX = startX + (intervals * spacing),
            Positions = positions
        };
    }
}
