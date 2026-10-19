using System;
using System.Collections.Generic;
using HPKata.Revit.Abstractions;
using HPKata.Revit.Services;
using HPKata.Revit.Units;
using Xunit;

namespace HPKata.Revit.Tests.Services
{
    public sealed class FramingEmpiricalChallengeTests
    {
        [Theory]
        [InlineData(4000.0, 300.0, 20.0)]
        [InlineData(3500.0, 500.0, 25.0)]
        [InlineData(3000.0, 600.0, 30.0)]
        [InlineData(2800.0, 800.0, 40.0)]
        [InlineData(6000.0, 1200.0, 50.0)]
        public void CalculateBottomBarEndpoints_StrictFormulaCheck_SatisfiesZbotEqualsZ0MinusHPlusC(
            double z0Mm, double heightMm, double coverMm)
        {
            RevitPoint3D startPt = new(0.0, 0.0, RevitUnitConverter.MmToFeet(z0Mm));
            RevitPoint3D endPt = new(RevitUnitConverter.MmToFeet(6000.0), 0.0, RevitUnitConverter.MmToFeet(z0Mm));
            RevitPoint3D normal = new(0.0, 1.0, 0.0);

            double heightFt = RevitUnitConverter.MmToFeet(heightMm);
            double coverFt = RevitUnitConverter.MmToFeet(coverMm);

            var (botStart, botEnd) = FramingRebarGeometryCalculator.CalculateBottomBarEndpoints(
                startPt, endPt, normal, 0.0, heightFt, coverFt);

            double actualBotZMm = RevitUnitConverter.FeetToMm(botStart.Z);
            double expectedBotZMm = z0Mm - heightMm + coverMm;

            Assert.True(RevitUnitConverter.AreCloseMm(expectedBotZMm, actualBotZMm, 0.01),
                $"Elevation mismatch: expected {expectedBotZMm:F3} mm, got {actualBotZMm:F3} mm");

            // Verify start and end have exact same elevation
            Assert.Equal(botStart.Z, botEnd.Z);
        }

        [Theory]
        [InlineData(250.0, 25.0, 2)]
        [InlineData(300.0, 30.0, 3)]
        [InlineData(400.0, 30.0, 4)]
        [InlineData(500.0, 35.0, 5)]
        [InlineData(600.0, 40.0, 8)]
        public void CalculateTransverseOffsets_AllBarsStayStrictlyWithinClearWidthBoundary(
            double totalWidthMm, double coverMm, int barCount)
        {
            double clearWidthMm = totalWidthMm - (2.0 * coverMm);
            double clearWidthFt = RevitUnitConverter.MmToFeet(clearWidthMm);

            var offsets = FramingRebarGeometryCalculator.CalculateTransverseOffsets(clearWidthFt, barCount);

            Assert.Equal(barCount, offsets.Count);

            double halfClearWidthFt = clearWidthFt / 2.0;
            for (int i = 0; i < offsets.Count; i++)
            {
                double offset = offsets[i];
                // Offset must be within [-halfClearWidth, +halfClearWidth]
                Assert.True(offset >= -halfClearWidthFt - 1e-6 && offset <= halfClearWidthFt + 1e-6,
                    $"Offset {offset} out of bounds [{-halfClearWidthFt}, {halfClearWidthFt}] for bar {i}");
            }

            // Verify first bar is at left boundary and last bar is at right boundary
            Assert.True(Math.Abs(offsets[0] - (-halfClearWidthFt)) < 1e-6);
            Assert.True(Math.Abs(offsets[offsets.Count - 1] - halfClearWidthFt) < 1e-6);

            // Verify uniform spacing
            if (barCount > 1)
            {
                double expectedStep = clearWidthFt / (barCount - 1);
                for (int i = 1; i < offsets.Count; i++)
                {
                    double actualStep = offsets[i] - offsets[i - 1];
                    Assert.True(Math.Abs(expectedStep - actualStep) < 1e-6,
                        $"Non-uniform spacing at step {i}: expected {expectedStep}, got {actualStep}");
                }
            }
        }

        [Fact]
        public void CalculateStirrupCorners_VerifyClosedFourSegmentCoreLoop()
        {
            RevitPoint3D center = new(RevitUnitConverter.MmToFeet(1500.0), 0.0, RevitUnitConverter.MmToFeet(3000.0));
            RevitPoint3D normal = new(0.0, 1.0, 0.0);
            double widthMm = 350.0;
            double heightMm = 700.0;
            double coverMm = 25.0;

            double widthFt = RevitUnitConverter.MmToFeet(widthMm);
            double heightFt = RevitUnitConverter.MmToFeet(heightMm);
            double coverFt = RevitUnitConverter.MmToFeet(coverMm);

            var corners = FramingRebarGeometryCalculator.CalculateStirrupCorners(
                center, normal, widthFt, heightFt, coverFt);

            Assert.Equal(4, corners.Count);

            // 4 segments: (0->1), (1->2), (2->3), (3->0)
            var p0 = corners[0]; // TL
            var p1 = corners[1]; // TR
            var p2 = corners[2]; // BR
            var p3 = corners[3]; // BL

            // Check non-zero lengths
            double len01Mm = RevitUnitConverter.FeetToMm(p0.DistanceTo(p1));
            double len12Mm = RevitUnitConverter.FeetToMm(p1.DistanceTo(p2));
            double len23Mm = RevitUnitConverter.FeetToMm(p2.DistanceTo(p3));
            double len30Mm = RevitUnitConverter.FeetToMm(p3.DistanceTo(p0));

            double expectedCoreWidthMm = widthMm - (2.0 * coverMm);   // 300 mm
            double expectedCoreHeightMm = heightMm - (2.0 * coverMm); // 650 mm

            Assert.True(RevitUnitConverter.AreCloseMm(expectedCoreWidthMm, len01Mm, 0.01));
            Assert.True(RevitUnitConverter.AreCloseMm(expectedCoreHeightMm, len12Mm, 0.01));
            Assert.True(RevitUnitConverter.AreCloseMm(expectedCoreWidthMm, len23Mm, 0.01));
            Assert.True(RevitUnitConverter.AreCloseMm(expectedCoreHeightMm, len30Mm, 0.01));

            // Verify coplanarity (all 4 corners share the same X station coordinate)
            Assert.Equal(center.X, p0.X);
            Assert.Equal(center.X, p1.X);
            Assert.Equal(center.X, p2.X);
            Assert.Equal(center.X, p3.X);
        }

        [Fact]
        public void CalculateTransverseOffsets_ZeroBarCount_ExposesPhantomBarDefect()
        {
            // When barCount is 0, no bars should be returned.
            // But the current implementation checks `if (barCount <= 1)` and returns `[0.0]`,
            // which causes FramingRebarGenerator to create a phantom centerline bar!
            double clearWidthFt = RevitUnitConverter.MmToFeet(240.0);
            var offsets = FramingRebarGeometryCalculator.CalculateTransverseOffsets(clearWidthFt, 0);

            // Verified: returns empty list on zero bar count!
            Assert.Empty(offsets);
        }

        [Fact]
        public void FramingChainCalculator_BranchBeams_DoNotCorruptCollinearChain()
        {
            // Main beam: B1 (0..6000), B2 (6000..12000). Branch beam: B_Branch (6000..6000, Y: 0..6000)
            var b1 = new FramingSegment
            {
                Id = "B1",
                StartPt = new RevitPoint3D(RevitUnitConverter.MmToFeet(0.0), 0.0, 0.0),
                EndPt = new RevitPoint3D(RevitUnitConverter.MmToFeet(6000.0), 0.0, 0.0)
            };
            var b2 = new FramingSegment
            {
                Id = "B2",
                StartPt = new RevitPoint3D(RevitUnitConverter.MmToFeet(6000.0), 0.0, 0.0),
                EndPt = new RevitPoint3D(RevitUnitConverter.MmToFeet(12000.0), 0.0, 0.0)
            };
            var bBranch = new FramingSegment
            {
                Id = "B_BRANCH",
                StartPt = new RevitPoint3D(RevitUnitConverter.MmToFeet(6000.0), 0.0, 0.0),
                EndPt = new RevitPoint3D(RevitUnitConverter.MmToFeet(6000.0), RevitUnitConverter.MmToFeet(6000.0), 0.0)
            };

            var candidates = new List<FramingSegment> { b1, bBranch, b2 };
            var chain = FramingChainCalculator.ChainCollinearSegments(
                b1, candidates, angleToleranceDeg: 1.0, touchToleranceFt: RevitUnitConverter.MmToFeet(50.0));

            // Chain must strictly contain B1 and B2, excluding B_BRANCH
            Assert.Equal(2, chain.Count);
            Assert.Equal("B1", chain[0].Id);
            Assert.Equal("B2", chain[1].Id);
            Assert.DoesNotContain(chain, s => s.Id == "B_BRANCH");
        }
    }
}
