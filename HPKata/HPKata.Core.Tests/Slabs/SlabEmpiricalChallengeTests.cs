using System;
using System.Collections.Generic;
using HPKata.Core.Slabs.Calculators;
using HPKata.Core.Slabs.Models;
using Xunit;

namespace HPKata.Core.Tests.Slabs
{
    /// <summary>
    /// Empirical challenge and stress tests for Slab domain calculators (Milestone M3 Phase 4).
    /// Author: challenger_m3_1 (Empirical Challenger).
    /// </summary>
    public sealed class SlabEmpiricalChallengeTests
    {
        [Theory]
        [InlineData(70.0)]
        [InlineData(60.0)]
        [InlineData(50.0)]
        [InlineData(40.0)]
        [InlineData(30.0)]
        [InlineData(20.0)]
        [InlineData(10.0)]
        public void SlabChair_StoolHeightClamping_DegenerateThinSlabsClampTo30mm(double slabThicknessMm)
        {
            var stool = SlabChairCalculator.CalculateStool(
                slabThicknessMm,
                topCoverMm: 15.0,
                bottomCoverMm: 15.0,
                topMeshDiaMm: 10,
                bottomMeshDiaMm: 10);

            Assert.Equal(30.0, stool.StoolHeightMm);
            Assert.True(stool.FootLengthMm >= 150.0);
            Assert.True(stool.SaddleLengthMm >= 150.0);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(-50.0)]
        public void SlabChair_ZeroOrNegativeThickness_ThrowsArgumentOutOfRangeException(double slabThicknessMm)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SlabChairCalculator.CalculateStool(slabThicknessMm));
        }

        [Theory]
        [InlineData(100.0, 15.0, 15.0, 10, 10, 30.0)]
        [InlineData(120.0, 15.0, 15.0, 10, 10, 50.0)]
        [InlineData(150.0, 15.0, 15.0, 10, 10, 80.0)]
        [InlineData(200.0, 20.0, 20.0, 12, 12, 110.0)]
        [InlineData(250.0, 20.0, 20.0, 12, 12, 160.0)]
        public void SlabChair_StandardAndThickSlabs_CalculatesCorrectRoundedHeight(
            double hs, double cTop, double cBot, int dTop, int dBot, double expectedHeight)
        {
            var stool = SlabChairCalculator.CalculateStool(
                hs, topCoverMm: cTop, bottomCoverMm: cBot, topMeshDiaMm: dTop, bottomMeshDiaMm: dBot,
                densityPerM2: 1.5, slabAreaM2: 30.0);

            Assert.Equal(expectedHeight, stool.StoolHeightMm);
            Assert.Equal(0, stool.StoolHeightMm % 5.0);
            Assert.Equal(45, stool.TotalCount); // ceil(30 * 1.5) = 45
            Assert.Equal(200.0, stool.SaddleLengthMm);
        }

        [Fact]
        public void SlabPolylineStepping_QuantizationDeltaL50_AllChordsAreMultiplesOf50()
        {
            var trapezoid = new List<(double X, double Y)>
            {
                (0.0, 0.0),
                (5000.0, 0.0),
                (3500.0, 4000.0),
                (0.0, 4000.0)
            };

            var items = SlabPolylineSteppingCalculator.CalculateSteppingRebar(
                "S_TRAP", trapezoid, spacingMm: 150.0, coverMm: 15.0, deltaLMm: 50.0);

            Assert.NotEmpty(items);
            foreach (var item in items)
            {
                Assert.Equal(0.0, item.CuttingLengthMm % 50.0);
                Assert.True(item.CuttingLengthMm >= 200.0, "Short chords < 200mm must be filtered");
            }

            // Verify clustering 1a, 1b, 1c...
            Assert.Equal("1a", items[0].BarMark);
            if (items.Count > 1) Assert.Equal("1b", items[1].BarMark);
            if (items.Count > 2) Assert.Equal("1c", items[2].BarMark);
        }

        [Fact]
        public void SlabPolylineStepping_InvalidInputs_ThrowsExpectedExceptions()
        {
            Assert.Throws<ArgumentException>(() =>
                SlabPolylineSteppingCalculator.CalculateSteppingRebar(
                    "S1", new List<(double X, double Y)> { (0, 0), (100, 100) }));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SlabPolylineSteppingCalculator.CalculateSteppingRebar(
                    "S1", new List<(double X, double Y)> { (0, 0), (100, 0), (50, 100) }, spacingMm: 0));
        }

        [Fact]
        public void SlabFourWay_OrthogonalAndDiagonals_CalculatesCorrectGeometryAndCounts()
        {
            double len = 5000.0, wid = 4000.0, thick = 150.0, cover = 15.0;
            var items = SlabFourWayCalculator.CalculateFourWayRebar(
                "S-4D", lengthMm: len, widthMm: wid, thicknessMm: thick,
                coverMm: cover, orthoSpacingMm: 150.0, diagSpacingMm: 200.0);

            Assert.Equal(4, items.Count);

            // Layer 1 (0 deg)
            Assert.Equal(len - 2.0 * cover, items[0].CuttingLengthMm);
            int expCount1 = (int)Math.Floor((wid - 2.0 * cover) / 150.0) + 1;
            Assert.Equal(expCount1, items[0].CountPerMember);

            // Layer 2 (90 deg)
            Assert.Equal(wid - 2.0 * cover, items[1].CuttingLengthMm);
            int expCount2 = (int)Math.Floor((len - 2.0 * cover) / 150.0) + 1;
            Assert.Equal(expCount2, items[1].CountPerMember);

            // Diagonals (+45 and -45 deg)
            double expDiagSpan = Math.Sqrt(len * len + wid * wid) - 2.0 * cover;
            int expDiagCount = (int)Math.Floor(Math.Min(len, wid) / 200.0) + 1;

            Assert.Equal(Math.Round(expDiagSpan, 1), items[2].CuttingLengthMm);
            Assert.Equal(expDiagCount, items[2].CountPerMember);
            Assert.Equal(items[2].CuttingLengthMm, items[3].CuttingLengthMm);
            Assert.Equal(items[2].CountPerMember, items[3].CountPerMember);
        }

        [Fact]
        public void SlabDropPanel_SteppedTransitionsAndShearTie_CalculatesExactLengths()
        {
            double size = 1800.0, depth = 150.0, cover = 20.0;
            var items = SlabFourWayCalculator.CalculateDropPanelRebar(
                "DP-1800", panelSizeMm: size, dropDepthMm: depth, slabThicknessMm: 150.0, coverMm: cover);

            Assert.Equal(3, items.Count);

            double slope = Math.Sqrt(depth * depth + Math.Pow(3.0 * depth, 2));
            double flat = size - 2.0 * cover;
            double expCut = Math.Round(flat + 2.0 * slope + 300.0, 1);

            Assert.Equal(expCut, items[0].CuttingLengthMm);
            Assert.Equal(expCut, items[1].CuttingLengthMm);

            double expTie = 4.0 * (size - 2.0 * cover) + 200.0;
            Assert.Equal(expTie, items[2].CuttingLengthMm);
            Assert.Equal("51", items[2].ShapeCode);
        }
    }
}
