namespace HPBim.Core.Tests;

using System;
using System.Collections.Generic;
using HPBim.Core.Models;
using HPBim.Core.Tcvn;
using Xunit;

public sealed class TcvnRulesTests
{
    [Theory]
    [InlineData(6, 0.222)]
    [InlineData(8, 0.395)]
    [InlineData(10, 0.617)]
    [InlineData(12, 0.888)]
    [InlineData(14, 1.208)]
    [InlineData(16, 1.578)]
    [InlineData(18, 1.998)]
    [InlineData(20, 2.466)]
    [InlineData(22, 2.984)]
    [InlineData(25, 3.853)]
    public void Tcvn_UnitWeightFormula_MatchesNominalLookup(int diameterMm, double expectedNominal)
    {
        double nominal = TcvnRebarCalculator.GetNominalUnitWeight(diameterMm);
        Assert.Equal(expectedNominal, nominal, precision: 3);

        double continuous = TcvnRebarCalculator.CalculateUnitWeight(diameterMm);
        Assert.Equal((diameterMm * diameterMm) / 162.2, continuous, precision: 5);
    }

    [Fact]
    public void Tcvn_TotalWeightCalculation_MatchesFormulas()
    {
        double d = 16.0;
        double lengthMm = 5200.0;
        int qty = 6;

        double unitWeight = TcvnRebarCalculator.CalculateUnitWeight(d);
        double singleBarWeight = TcvnRebarCalculator.CalculateWeightFromMm(d, lengthMm);
        Assert.Equal(unitWeight * (lengthMm / 1000.0), singleBarWeight, precision: 5);

        double totalWeight = TcvnRebarCalculator.CalculateTotalWeight(d, lengthMm, qty);
        Assert.Equal(singleBarWeight * qty, totalWeight, precision: 5);
    }

    [Fact]
    public void Tcvn_WeightPartitioning_SeparatesSmallAndLargeBars()
    {
        var rebars = new List<RebarDef>
        {
            new() { DiameterMm = 8, LengthMm = 1000, Quantity = 10 },
            new() { DiameterMm = 10, LengthMm = 2000, Quantity = 5 },
            new() { DiameterMm = 16, LengthMm = 3000, Quantity = 4 },
            new() { DiameterMm = 18, LengthMm = 4000, Quantity = 2 }
        };

        var (small, large) = TcvnRebarCalculator.PartitionWeightByDiameter(rebars, 10.0);

        double expectedSmall = TcvnRebarCalculator.CalculateTotalWeight(8, 1000, 10) +
                               TcvnRebarCalculator.CalculateTotalWeight(10, 2000, 5);
        double expectedLarge = TcvnRebarCalculator.CalculateTotalWeight(16, 3000, 4) +
                               TcvnRebarCalculator.CalculateTotalWeight(18, 4000, 2);

        Assert.Equal(expectedSmall, small, precision: 4);
        Assert.Equal(expectedLarge, large, precision: 4);
    }

    [Theory]
    [InlineData(StructuralElementKind.Footing, 50.0)]
    [InlineData(StructuralElementKind.Beam, 25.0)]
    [InlineData(StructuralElementKind.Column, 25.0)]
    [InlineData(StructuralElementKind.Stair, 20.0)]
    [InlineData(StructuralElementKind.Slab, 15.0)]
    [InlineData(StructuralElementKind.Wall, 20.0)]
    public void Tcvn_ConcreteCoverRules_FollowsTable19(StructuralElementKind kind, double expectedMinCover)
    {
        Assert.Equal(expectedMinCover, ConcreteCoverRules.GetMinimumCoverMm(kind));

        Assert.True(ConcreteCoverRules.IsCoverCompliant(kind, expectedMinCover));
        Assert.True(ConcreteCoverRules.IsCoverCompliant(kind, expectedMinCover + 5.0));
        Assert.False(ConcreteCoverRules.IsCoverCompliant(kind, expectedMinCover - 5.0));

        var validRes = ConcreteCoverRules.ValidateCover(kind, expectedMinCover);
        Assert.True(validRes.IsCompliant);
        Assert.Equal(expectedMinCover, validRes.MinimumCoverMm);

        var invalidRes = ConcreteCoverRules.ValidateCover(kind, expectedMinCover - 5.0);
        Assert.False(invalidRes.IsCompliant);
    }

    [Fact]
    public void Tcvn_DetailingMultipliers_AnchorageLapAndHook()
    {
        double d = 20.0;
        Assert.Equal(600.0, TcvnRebarCalculator.CalculateAnchorageLengthMm(d)); // 30D
        Assert.Equal(240.0, TcvnRebarCalculator.CalculateHook90LengthMm(d));   // 12D
        Assert.Equal(800.0, TcvnRebarCalculator.CalculateTensionLapLengthMm(d)); // 40D
        Assert.Equal(600.0, TcvnRebarCalculator.CalculateCompressionLapLengthMm(d)); // 30D

        // Stirrup hook: max(10D, 75mm)
        Assert.Equal(75.0, TcvnRebarCalculator.CalculateStirrupHookLengthMm(6.0)); // 10*6 = 60 < 75 -> 75
        Assert.Equal(80.0, TcvnRebarCalculator.CalculateStirrupHookLengthMm(8.0)); // 10*8 = 80 > 75 -> 80
        Assert.Equal(100.0, TcvnRebarCalculator.CalculateStirrupHookLengthMm(10.0));
    }

    [Fact]
    public void Tcvn_CommercialBarSplices_HandlesLengthsBeyond11_7M()
    {
        double d = 16.0;
        double tensionLap = 40.0 * d; // 640mm

        // Shorter than 11.7m -> 0 splices
        var res1 = TcvnRebarCalculator.CalculateCommercialBarSplices(10000.0, d, LapZone.Tension);
        Assert.Equal(0, res1.SpliceCount);
        Assert.Equal(10000.0, res1.DevelopedLengthMm);

        // 18.0m -> 1 splice
        var res2 = TcvnRebarCalculator.CalculateCommercialBarSplices(18000.0, d, LapZone.Tension);
        Assert.Equal(1, res2.SpliceCount);
        Assert.Equal(tensionLap, res2.SingleLapLengthMm);
        Assert.Equal(18000.0 + tensionLap, res2.DevelopedLengthMm);

        // 25.0m -> 2 splices
        var res3 = TcvnRebarCalculator.CalculateCommercialBarSplices(25000.0, d, LapZone.Tension);
        Assert.Equal(2, res3.SpliceCount);
        Assert.Equal(25000.0 + (2 * tensionLap), res3.DevelopedLengthMm);
    }

    [Fact]
    public void Tcvn_BeamStirrupZones_DenseAndSparseDistribution()
    {
        double clearSpan = 5000.0;
        double beamHeight = 400.0;
        var zones = TcvnRebarCalculator.CalculateBeamStirrupZones(clearSpan, beamHeight, 100.0, 200.0);

        // Dense zone = min(2 * 400 = 800, 5000 / 3 = 1666.7) = 800mm
        Assert.Equal(800.0, zones.DenseZoneLengthMm);
        Assert.Equal(8, zones.DenseZoneCountStart);
        Assert.Equal(8, zones.DenseZoneCountEnd);

        // Mid-span = 5000 - 1600 = 3400mm / 200 = 17 stirrups
        Assert.Equal(17, zones.SparseZoneCount);
    }

    [Fact]
    public void Tcvn_ColumnStirrupZones_BottomMiddleTopDistribution()
    {
        double storeyHeight = 3600.0;
        var zones = TcvnRebarCalculator.CalculateColumnStirrupZones(storeyHeight, 100.0, 200.0);

        // Dense zone = 3600 / 4 = 900mm
        Assert.Equal(900.0, zones.DenseZoneLengthMm);
        Assert.Equal(9, zones.DenseCountBottom);
        Assert.Equal(9, zones.DenseCountTop);

        // Mid-height = 3600 - 1800 = 1800mm / 200 = 9 stirrups
        Assert.Equal(9, zones.SparseCountMiddle);
    }

    [Fact]
    public void Tcvn_Standards_ConstantsIntegrity()
    {
        Assert.Equal(220.0, TcvnStandards.WallExteriorThicknessMm);
        Assert.Equal(110.0, TcvnStandards.WallInteriorThicknessMm);
        Assert.Equal(200.0, TcvnStandards.BeamMainWidthMm);
        Assert.Equal(400.0, TcvnStandards.BeamMainHeightMm);
        Assert.Equal(200.0, TcvnStandards.BeamSubWidthMm);
        Assert.Equal(300.0, TcvnStandards.BeamSubHeightMm);
        Assert.Equal(100.0, TcvnStandards.SlabThicknessMm);
        Assert.Equal(11700.0, TcvnStandards.CommercialBarMaxLengthMm);
        Assert.Equal(30.0, TcvnStandards.AnchorageMultiplier);
        Assert.Equal(40.0, TcvnStandards.LapTensionMultiplier);
    }
}
