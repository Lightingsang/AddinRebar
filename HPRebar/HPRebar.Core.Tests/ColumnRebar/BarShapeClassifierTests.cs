using System;
using HPRebar.Core.ColumnRebar;
using HPRebar.Core.ColumnRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.ColumnRebar;

public sealed class BarShapeClassifierTests
{
    private const int Precision = 6;

    private static BarPolyline Bar(SpliceSpec splice, params Point3[] points) => new()
    {
        BarNumber = 1,
        Diameter = 20,
        Points = points,
        Splice = splice
    };

    [Fact]
    public void AStraightBarIsTheSimplestShapeAndItsWholeLengthIsOneLeg()
    {
        var bar = Bar(new SpliceSpec { IsTopDowels = false },
            new Point3(0, 0, 0),
            new Point3(0, 0, 3000));

        var item = BarShapeClassifier.Classify(bar, 4);

        Assert.Equal(BarShape.DS00, item.Shape);
        Assert.Equal(3000d, item.L, Precision);
        Assert.Equal(0d, item.La, Precision);
        Assert.Equal(0d, item.Lb, Precision);
        Assert.Equal(3000d, item.Length, Precision);
        Assert.Equal(4, item.Count);
    }

    [Fact]
    public void ABarThatCrossesOverGetsTheTransitionShapeWithItsSlopeRecorded()
    {
        var splice = new SpliceSpec { IsTopDowels = true, TopStyle = TopDowelStyle.BendIntoColumnAbove, LbTop = 700 };

        var bar = Bar(splice,
            new Point3(0, 0, 0),
            new Point3(0, 0, 2400),
            new Point3(0, 300, 3000),
            new Point3(0, 300, 3700));

        var item = BarShapeClassifier.Classify(bar, 1);

        Assert.Equal(BarShape.DS07, item.Shape);
        Assert.Equal(600d, item.SlopeX, Precision);
        Assert.Equal(300d, item.SlopeY, Precision);
        Assert.Equal(Math.Sqrt(600d * 600 + 300 * 300), item.L, Precision);
        Assert.Equal(2400d, item.La, Precision);
        Assert.Equal(700d, item.Lb, Precision);
    }

    [Fact]
    public void ATopDowelThatNeverActuallyMovesInPlanStaysStraight()
    {
        var splice = new SpliceSpec { IsTopDowels = true, TopStyle = TopDowelStyle.BendIntoColumnAbove, LbTop = 700 };

        var bar = Bar(splice,
            new Point3(0, 0, 0),
            new Point3(0, 0, 2400),
            new Point3(0, 0, 3000),
            new Point3(0, 0, 3700));

        Assert.Equal(BarShape.DS00, BarShapeClassifier.Classify(bar, 1).Shape);
    }

    [Fact]
    public void AHookedBottomUnderATransitionGetsItsOwnShapeAndKeepsTheHookLength()
    {
        var splice = new SpliceSpec
        {
            IsTopDowels = true,
            TopStyle = TopDowelStyle.BendIntoColumnAbove,
            LbTop = 700,
            IsBottomDowels = true,
            BottomStyle = BottomDowelStyle.RunPastBase,
            LaBottom = 250,
            LbBottom = 400
        };

        var bar = Bar(splice,
            new Point3(0, -250, -400),
            new Point3(0, 0, -400),
            new Point3(0, 0, 2400),
            new Point3(0, 300, 3000),
            new Point3(0, 300, 3700));

        var item = BarShapeClassifier.Classify(bar, 1);

        Assert.Equal(BarShape.DS07A, item.Shape);
        Assert.Equal(250d, item.L1, Precision);
    }

    [Fact]
    public void ANegativeBottomHookPicksTheMirroredTransitionShape()
    {
        var splice = new SpliceSpec
        {
            IsTopDowels = true,
            TopStyle = TopDowelStyle.BendIntoColumnAbove,
            LbTop = 700,
            IsBottomDowels = true,
            BottomStyle = BottomDowelStyle.RunPastBase,
            LaBottom = -250,
            LbBottom = 400
        };

        var bar = Bar(splice,
            new Point3(0, 250, -400),
            new Point3(0, 0, -400),
            new Point3(0, 0, 2400),
            new Point3(0, 300, 3000),
            new Point3(0, 300, 3700));

        Assert.Equal(BarShape.DS07B, BarShapeClassifier.Classify(bar, 1).Shape);
    }

    [Fact]
    public void ATopHookOnAnUnhookedBottomSplitsOffItsLegFromTheMainLength()
    {
        var splice = new SpliceSpec { IsTopDowels = true, TopStyle = TopDowelStyle.StopUnderBeam, LaTop = 300 };

        var bar = Bar(splice,
            new Point3(0, 0, 0),
            new Point3(0, 0, 2900),
            new Point3(0, -300, 2900));

        var item = BarShapeClassifier.Classify(bar, 1);

        Assert.Equal(BarShape.DS05, item.Shape);
        Assert.Equal(0d, item.La, Precision);
        Assert.Equal(300d, item.Lb, Precision);
        Assert.Equal(2900d, item.L, Precision);
    }

    [Fact]
    public void ANegativeTopHookPicksTheMirroredShape()
    {
        var splice = new SpliceSpec { IsTopDowels = true, TopStyle = TopDowelStyle.StopUnderBeam, LaTop = -300 };

        var bar = Bar(splice,
            new Point3(0, 0, 0),
            new Point3(0, 0, 2900),
            new Point3(0, 300, 2900));

        Assert.Equal(BarShape.DS02, BarShapeClassifier.Classify(bar, 1).Shape);
    }

    [Fact]
    public void HooksAtBothEndsGiveTheDoubleBentShape()
    {
        var splice = new SpliceSpec
        {
            IsTopDowels = true,
            TopStyle = TopDowelStyle.StopUnderBeam,
            LaTop = 300,
            IsBottomDowels = true,
            BottomStyle = BottomDowelStyle.RunPastBase,
            LaBottom = 250,
            LbBottom = 400
        };

        var bar = Bar(splice,
            new Point3(0, -250, -400),
            new Point3(0, 0, -400),
            new Point3(0, 0, 2900),
            new Point3(0, -300, 2900));

        var item = BarShapeClassifier.Classify(bar, 1);

        Assert.Equal(BarShape.DS06, item.Shape);
        Assert.Equal(250d, item.La, Precision);
        Assert.Equal(300d, item.Lb, Precision);
        Assert.Equal(3300d, item.L, Precision);
    }

    [Fact]
    public void ABottomHookUnderAPlainTopGivesTheSingleBentShape()
    {
        var splice = new SpliceSpec
        {
            IsTopDowels = false,
            IsBottomDowels = true,
            BottomStyle = BottomDowelStyle.RunPastBase,
            LaBottom = 250,
            LbBottom = 400
        };

        var bar = Bar(splice,
            new Point3(0, -250, -400),
            new Point3(0, 0, -400),
            new Point3(0, 0, 2975));

        var item = BarShapeClassifier.Classify(bar, 1);

        Assert.Equal(BarShape.DS04, item.Shape);
        Assert.Equal(250d, item.La, Precision);
        Assert.Equal(3375d, item.L, Precision);
    }

    [Fact]
    public void CutLengthIsAlwaysTheThreeLegsAddedUp()
    {
        var item = new BarScheduleItem { L = 3000, La = 250, Lb = 300 };

        Assert.Equal(3550d, item.Length, Precision);
    }
}
