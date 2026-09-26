using System;
using HPRebar.Core.KataExport.Calculators;
using HPRebar.Core.KataExport.Models;
using Xunit;
using static HPRebar.Core.Tests.KataExport.TestKataData;

namespace HPRebar.Core.Tests.KataExport;

/// <summary>
/// Tie beams between footings: the footing is the support (row 11 = its width along the tie beam) and the
/// column standing on it is the column above (row 19 = its width and offset from the footing centre).
/// </summary>
public sealed class KataTieBeamTests
{
    private static KataSupport FootingWithColumn(double start, double end, double columnStart, double columnEnd, string key) =>
        Footing(start, end, key) with { Upper = new Interval1D(columnStart, columnEnd) };

    [Fact]
    public void FootingIsTheSupportAndTheColumnOnItIsTheColumnAbove()
    {
        var input = new KataRunInput(
            new[] { Piece(0, 6000, b: 250, h: 400) },
            new[]
            {
                FootingWithColumn(-750, 750, -150, 150, "F1"),       // column centred on the footing
                FootingWithColumn(5100, 6900, 5750, 6050, "F2")      // column centre 100 mm before the footing centre (6000)
            },
            Array.Empty<KataGridCrossing>(),
            Header);

        var sheet = KataRowBuilder.Build(input);

        Assert.Equal(new object?[] { 1500.0, 4350.0, 1800.0 }, sheet.Row11);
        Assert.Equal(new object?[] { "300;0", 0.0, "300;-100" }, sheet.Row19);
    }

    [Fact]
    public void ColumnPassingThroughTheTieBeamMergesWithItsFootingAndKeepsTheFootingWidth()
    {
        var input = new KataRunInput(
            new[] { Piece(0, 6000, b: 250, h: 400) },
            new[]
            {
                Footing(-750, 750, "F1"),
                Column(-150, 150, upper: new Interval1D(-150, 150), key: "C1"),
                Footing(5250, 6750, "F2")
            },
            Array.Empty<KataGridCrossing>(),
            Header);

        var sheet = KataRowBuilder.Build(input);

        Assert.Equal(1500.0, sheet.Row11[0]);
        Assert.Equal("300;0", sheet.Row19[0]);
        Assert.Equal(4500.0, sheet.Row11[1]);
    }

    [Fact]
    public void CrossingTieBeamAtAFootingGivesItsOffsetFromTheFootingCentreInRow21()
    {
        var input = new KataRunInput(
            new[] { Piece(0, 6000, b: 250, h: 400) },
            new[]
            {
                Footing(-750, 750, "F1"),
                Girder(-75, 175, "250x400", "GM2"),                  // perpendicular tie beam centred at +50
                Footing(5250, 6750, "F2")
            },
            new[] { Grid("1", 0) },
            Header);

        var sheet = KataRowBuilder.Build(input);

        Assert.Equal(1500.0, sheet.Row11[0]);
        Assert.Equal(50.0, sheet.Row21[0]);
        Assert.Equal(0.0, sheet.Row23[0]);
    }
}
