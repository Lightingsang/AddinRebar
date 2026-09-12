using System.Collections.Generic;
using HPRebar.Core.BeamRebar.Models;

namespace HPRebar.Core.Tests.BeamRebar;

/// <summary>
/// Shared fixtures and fluent builder factory for BeamRebar unit tests.
/// All dimensions are expressed in millimetres (double).
/// </summary>
internal static class TestBeamData
{
    public const double DefaultCover = 25.0;
    public const double DefaultStirrupDiameter = 8.0;
    public const double DefaultMainTopDiameter = 20.0;
    public const double DefaultMainBottomDiameter = 20.0;
    public const double DefaultSideBarDiameter = 12.0;
    public const double DefaultColumnWidth = 400.0;
    public const int DefaultPrecision = 6;

    /// <summary>Creates a standard single-span beam stack between two exterior columns.</summary>
    public static BeamContinuousStack SingleSpan(
        double length = 6000,
        double width = 300,
        double height = 600,
        double leftCol = DefaultColumnWidth,
        double rightCol = DefaultColumnWidth)
    {
        var supports = new List<BeamSupportNode>
        {
            new(0, "Col-0", 0, leftCol, SupportType.ExteriorColumn),
            new(1, "Col-1", length, rightCol, SupportType.ExteriorColumn)
        };

        var spans = new List<BeamSpan>
        {
            new(0, "Span-1", length, width, height, TopOffsetMm: 0, CoverMm: DefaultCover,
                ClearLengthMm: length - (leftCol / 2.0) - (rightCol / 2.0))
            {
                StartX = leftCol / 2.0
            }
        };

        return new BeamContinuousStack(spans, supports);
    }

    /// <summary>Creates a standard 2-span continuous beam stack.</summary>
    public static BeamContinuousStack TwoSpan(
        double l1 = 6000,
        double l2 = 6000,
        double width = 300,
        double height = 600,
        double colWidth = DefaultColumnWidth)
    {
        var supports = new List<BeamSupportNode>
        {
            new(0, "Col-0", 0, colWidth, SupportType.ExteriorColumn),
            new(1, "Col-1", l1, colWidth, SupportType.InteriorColumn),
            new(2, "Col-2", l1 + l2, colWidth, SupportType.ExteriorColumn)
        };

        var spans = new List<BeamSpan>
        {
            new(0, "Span-1", l1, width, height, 0, DefaultCover, l1 - colWidth)
            {
                StartX = colWidth / 2.0
            },
            new(1, "Span-2", l2, width, height, 0, DefaultCover, l2 - colWidth)
            {
                StartX = l1 + (colWidth / 2.0)
            }
        };

        return new BeamContinuousStack(spans, supports);
    }

    /// <summary>Creates a standard 3-span continuous beam stack (Tier 4 framing case).</summary>
    public static BeamContinuousStack ThreeSpan(
        double l1 = 6000,
        double l2 = 5000,
        double l3 = 6000,
        double width = 300,
        double height = 600,
        double colWidth = DefaultColumnWidth)
    {
        double x0 = 0;
        double x1 = l1;
        double x2 = l1 + l2;
        double x3 = l1 + l2 + l3;

        var supports = new List<BeamSupportNode>
        {
            new(0, "Col-0", x0, colWidth, SupportType.ExteriorColumn),
            new(1, "Col-1", x1, colWidth, SupportType.InteriorColumn),
            new(2, "Col-2", x2, colWidth, SupportType.InteriorColumn),
            new(3, "Col-3", x3, colWidth, SupportType.ExteriorColumn)
        };

        var spans = new List<BeamSpan>
        {
            new(0, "Span-1", l1, width, height, 0, DefaultCover, l1 - colWidth)
            {
                StartX = x0 + (colWidth / 2.0)
            },
            new(1, "Span-2", l2, width, height, 0, DefaultCover, l2 - colWidth)
            {
                StartX = x1 + (colWidth / 2.0)
            },
            new(2, "Span-3", l3, width, height, 0, DefaultCover, l3 - colWidth)
            {
                StartX = x2 + (colWidth / 2.0)
            }
        };

        return new BeamContinuousStack(spans, supports);
    }

    /// <summary>Creates a cantilever beam stack (Cantilever Left + Interior Span).</summary>
    public static BeamContinuousStack CantileverLeft(
        double lCant = 2000,
        double lSpan = 6000,
        double width = 300,
        double height = 600,
        double colWidth = DefaultColumnWidth)
    {
        var supports = new List<BeamSupportNode>
        {
            new(0, "Tip-0", 0, 0, SupportType.CantileverEnd),
            new(1, "Col-1", lCant, colWidth, SupportType.InteriorColumn),
            new(2, "Col-2", lCant + lSpan, colWidth, SupportType.ExteriorColumn)
        };

        var spans = new List<BeamSpan>
        {
            new(0, "Cant-1", lCant, width, height, 0, DefaultCover, lCant - (colWidth / 2.0))
            {
                StartX = 0,
                Cantilever = CantileverPosition.Left
            },
            new(1, "Span-2", lSpan, width, height, 0, DefaultCover, lSpan - colWidth)
            {
                StartX = lCant + (colWidth / 2.0)
            }
        };

        return new BeamContinuousStack(spans, supports);
    }

    /// <summary>Creates a beam stack with variable cross-sections across adjacent spans.</summary>
    public static BeamContinuousStack VariableDepth(
        double l1 = 6000, double h1 = 600,
        double l2 = 6000, double h2 = 400,
        double width = 300,
        double colWidth = DefaultColumnWidth)
    {
        var supports = new List<BeamSupportNode>
        {
            new(0, "Col-0", 0, colWidth, SupportType.ExteriorColumn),
            new(1, "Col-1", l1, colWidth, SupportType.InteriorColumn),
            new(2, "Col-2", l1 + l2, colWidth, SupportType.ExteriorColumn)
        };

        var spans = new List<BeamSpan>
        {
            new(0, "Span-1", l1, width, h1, 0, DefaultCover, l1 - colWidth)
            {
                StartX = colWidth / 2.0
            },
            new(1, "Span-2", l2, width, h2, 0, DefaultCover, l2 - colWidth)
            {
                StartX = l1 + (colWidth / 2.0)
            }
        };

        return new BeamContinuousStack(spans, supports);
    }

    /// <summary>Creates a deep beam stack (h >= 700 mm) triggering side/skin reinforcement.</summary>
    public static BeamContinuousStack DeepBeam(
        double length = 6000,
        double width = 400,
        double height = 800,
        double colWidth = DefaultColumnWidth)
    {
        return SingleSpan(length, width, height, colWidth, colWidth);
    }

    /// <summary>Factory for uniform stirrup specification.</summary>
    public static BeamStirrupSpec UniformStirrupSpec(double spacing = 150, double diameter = DefaultStirrupDiameter) =>
        new(StirrupLayout.Uniform, diameter, DefaultCover, S1: spacing, S2: spacing, StartOffsetMm: 50.0);

    /// <summary>Factory for 3-zone L/4 stirrup specification.</summary>
    public static BeamStirrupSpec ThreeZoneL4StirrupSpec(
        double s1 = 100,
        double s2 = 200,
        double diameter = DefaultStirrupDiameter) =>
        new(StirrupLayout.ThreeZoneL4, diameter, DefaultCover, S1: s1, S2: s2, StartOffsetMm: 50.0);

    /// <summary>Factory for 3-zone L/3 stirrup specification.</summary>
    public static BeamStirrupSpec ThreeZoneL3StirrupSpec(
        double s1 = 100,
        double s2 = 200,
        double diameter = DefaultStirrupDiameter) =>
        new(StirrupLayout.ThreeZoneL3, diameter, DefaultCover, S1: s1, S2: s2, StartOffsetMm: 50.0);

    /// <summary>Factory for continuous main longitudinal bar specification.</summary>
    public static BeamMainBarSpec MainBarSpec(
        int topCount = 3,
        double topDiameter = DefaultMainTopDiameter,
        int bottomCount = 3,
        double bottomDiameter = DefaultMainBottomDiameter,
        double hookLength = 350.0) =>
        new(topCount, topDiameter, bottomCount, bottomDiameter,
            LeftHookLengthMm: hookLength, RightHookLengthMm: hookLength,
            MaxStockLengthMm: 11700.0, LapLengthMultiplier: 40.0, StaggerSplice: true);
}
