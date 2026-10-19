using System;
using System.Linq;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Foundation;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;
using HPDcePro.Core.XData;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public sealed class PileCapTests
    {
        private static PileCapInput CreateStandardInput(
            double nKn = 1500.0, double mxKnm = 50.0, double myKnm = 40.0,
            PileLayoutPattern pattern = PileLayoutPattern.FourPileRectangular, double rcKn = 600.0)
        {
            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            var rebar = MaterialLibrary.GetRebar(RebarGrade.CB400V);
            var piles = PileCapCalculator.GenerateStandardLayout(pattern, 400.0);
            return new PileCapInput(
                "DC-1", 1800.0, 1800.0, 900.0, 400.0, 400.0,
                concrete, rebar, piles, nKn, mxKnm, myKnm, rcKn);
        }

        [Fact]
        public void Calculate_FourPileRectangular_SymmetricLoad_EqualReactions()
        {
            var input = CreateStandardInput(nKn: 1200.0, mxKnm: 0.0, myKnm: 0.0);
            var result = PileCapCalculator.Calculate(input);

            Assert.Equal(4, result.PileCount);
            double expectedAvg = (1200.0 + result.SelfWeightAndSoilKn) / 4.0;
            Assert.All(result.Piles, p => Assert.Equal(expectedAvg, p.ReactionKn, 1));
            Assert.Equal(result.MaxReactionKn, result.MinReactionKn, 1);
            Assert.True(result.IsSafe);
        }

        [Fact]
        public void Calculate_BiaxialBending_IncreasesCornerPileReaction()
        {
            var input = CreateStandardInput(nKn: 1200.0, mxKnm: 60.0, myKnm: 50.0);
            var result = PileCapCalculator.Calculate(input);

            Assert.True(result.MaxReactionKn > result.AvgReactionKn);
            Assert.True(result.MinReactionKn < result.AvgReactionKn);
            Assert.True(result.MaxReactionKn > 350.0);
            Assert.All(result.Piles, p => Assert.False(p.IsTension));
        }

        [Fact]
        public void Calculate_PunchingShear_ColumnPyramidEvaluatedCorrectly()
        {
            var input = CreateStandardInput(nKn: 1500.0, mxKnm: 30.0, myKnm: 20.0);
            var result = PileCapCalculator.Calculate(input);

            Assert.True(result.ColumnPunchingCapacityKn > 1000.0);
            Assert.True(result.ColumnPunchingRatio <= 1.0);
            Assert.True(result.IsColumnPunchingSafe);
        }

        [Fact]
        public void Calculate_CornerPilePunching_EvaluatedCorrectly()
        {
            var input = CreateStandardInput(nKn: 1600.0, mxKnm: 40.0, myKnm: 30.0);
            var result = PileCapCalculator.Calculate(input);

            Assert.True(result.CornerPilePunchingCapacityKn > 500.0);
            Assert.True(result.CornerPilePunchingRatio > 0.0);
            Assert.True(result.CornerPilePunchingRatio <= 1.0);
        }

        [Fact]
        public void Calculate_FlexuralRebar_ComputesCantileverMomentsAndAs()
        {
            var input = CreateStandardInput(nKn: 1500.0, mxKnm: 50.0, myKnm: 40.0);
            var result = PileCapCalculator.Calculate(input);

            Assert.True(result.DesignMomentMxKnm > 0.0);
            Assert.True(result.DesignMomentMyKnm > 0.0);
            Assert.True(result.RequiredAsXMm2 > 1000.0);
            Assert.True(result.RequiredAsYMm2 > 1000.0);
        }

        [Fact]
        public void GenerateStandardLayout_GeneratesAllKnownPatterns()
        {
            var patterns = new[]
            {
                (PileLayoutPattern.SinglePile, 1),
                (PileLayoutPattern.TwoPile, 2),
                (PileLayoutPattern.ThreePileTriangular, 3),
                (PileLayoutPattern.FourPileRectangular, 4),
                (PileLayoutPattern.FivePileDice, 5),
                (PileLayoutPattern.SixPileRectangular, 6)
            };

            foreach (var (pat, expectedCount) in patterns)
            {
                var layout = PileCapCalculator.GenerateStandardLayout(pat, 400.0);
                Assert.Equal(expectedCount, layout.Count);
                Assert.All(layout, p => Assert.True(p.DiameterMm == 400.0));
            }
        }

        [Fact]
        public void Calculate_OverloadedPiles_FlagsFailureAndGeneratesSchedule()
        {
            var input = CreateStandardInput(nKn: 4000.0, mxKnm: 250.0, myKnm: 200.0, rcKn: 500.0);
            var result = PileCapCalculator.Calculate(input);

            Assert.False(result.IsSafe);
            Assert.True(result.PileCapacityRatio > 1.0);
            Assert.Contains("FAIL", result.StatusSummary);
            Assert.NotEmpty(result.CoordinateSchedule);
            Assert.Contains(result.CoordinateSchedule, s => s.Status == "FAIL");
        }

        [Fact]
        public void GeneratePileCapDrawing_GeneratesCadPrimitivesWithXData()
        {
            var input = CreateStandardInput();
            var result = PileCapCalculator.Calculate(input);
            var gen = new FoundationDetailingGenerator();

            var buffer = gen.GeneratePileCapDrawing(result, input, new Point2D(0, 0));
            Assert.True(buffer.Count >= 10);

            var polyWithXData = buffer.Entities
                .Where(e => e.AttachedXData.Count > 0)
                .ToList();
            Assert.NotEmpty(polyWithXData);
            Assert.Contains(polyWithXData, e => e.AttachedXData.Any(x => x.RegAppName == XDataRegistry.RegAppMongCoc));
        }
    }
}
