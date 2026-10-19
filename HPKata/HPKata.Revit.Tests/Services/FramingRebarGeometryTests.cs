using System;
using System.Collections.Generic;
using HPKata.Revit.Abstractions;
using HPKata.Revit.Services;
using HPKata.Revit.Units;
using Xunit;

namespace HPKata.Revit.Tests.Services
{
    public sealed class FramingRebarGeometryTests
    {
        [Fact]
        public void CalculateBottomBarEndpoints_CalculatesCorrectElevationBelowTopFace()
        {
            double zStartMm = 3000.0;
            double heightMm = 600.0;
            double coverMm = 30.0;

            RevitPoint3D startPt = new(0.0, 0.0, RevitUnitConverter.MmToFeet(zStartMm));
            RevitPoint3D endPt = new(RevitUnitConverter.MmToFeet(6000.0), 0.0, RevitUnitConverter.MmToFeet(zStartMm));
            RevitPoint3D normal = new(0.0, 1.0, 0.0);

            double heightFt = RevitUnitConverter.MmToFeet(heightMm);
            double coverFt = RevitUnitConverter.MmToFeet(coverMm);

            var (botStart, botEnd) = FramingRebarGeometryCalculator.CalculateBottomBarEndpoints(
                startPt, endPt, normal, 0.0, heightFt, coverFt);

            var (topStart, topEnd) = FramingRebarGeometryCalculator.CalculateTopBarEndpoints(
                startPt, endPt, normal, 0.0, coverFt);

            double expectedBotZMm = zStartMm - heightMm + coverMm; // 2430.0 mm
            double actualBotZMm = RevitUnitConverter.FeetToMm(botStart.Z);

            Assert.True(RevitUnitConverter.AreCloseMm(expectedBotZMm, actualBotZMm, 0.1));
            Assert.True(botStart.Z < topStart.Z, "Bottom rebar must reside strictly below top rebar");
            Assert.True(botStart.Z < startPt.Z, "Bottom rebar must reside strictly below host beam top face");

            double verticalSeparationMm = RevitUnitConverter.FeetToMm(topStart.Z - botStart.Z);
            double expectedSeparationMm = heightMm - (2.0 * coverMm); // 540.0 mm
            Assert.True(RevitUnitConverter.AreCloseMm(expectedSeparationMm, verticalSeparationMm, 0.1));
        }

        [Fact]
        public void CalculateTopBarEndpoints_CalculatesCorrectElevationBelowTopFace()
        {
            double zStartMm = 3000.0;
            double coverMm = 30.0;

            RevitPoint3D startPt = new(0.0, 0.0, RevitUnitConverter.MmToFeet(zStartMm));
            RevitPoint3D endPt = new(RevitUnitConverter.MmToFeet(6000.0), 0.0, RevitUnitConverter.MmToFeet(zStartMm));
            RevitPoint3D normal = new(0.0, 1.0, 0.0);
            double coverFt = RevitUnitConverter.MmToFeet(coverMm);

            var (topStart, topEnd) = FramingRebarGeometryCalculator.CalculateTopBarEndpoints(
                startPt, endPt, normal, 0.0, coverFt);

            double expectedTopZMm = zStartMm - coverMm; // 2970.0 mm
            double actualTopZMm = RevitUnitConverter.FeetToMm(topStart.Z);

            Assert.True(RevitUnitConverter.AreCloseMm(expectedTopZMm, actualTopZMm, 0.1));
            Assert.True(topStart.Z < startPt.Z, "Top rebar must reside below host top surface by cover");
        }

        [Fact]
        public void CalculateTransverseOffsets_SingleBar_ReturnsSingleCenteredOffset()
        {
            double clearWidthFt = RevitUnitConverter.MmToFeet(240.0);
            var offsets = FramingRebarGeometryCalculator.CalculateTransverseOffsets(clearWidthFt, 1);

            Assert.Single(offsets);
            Assert.Equal(0.0, offsets[0]);
        }

        [Fact]
        public void CalculateTransverseOffsets_MultiBar_EvenlyDistributesSymmetricAroundCenter()
        {
            double clearWidthMm = 240.0;
            double clearWidthFt = RevitUnitConverter.MmToFeet(clearWidthMm);

            // Test 3 bars
            var offsets3 = FramingRebarGeometryCalculator.CalculateTransverseOffsets(clearWidthFt, 3);
            Assert.Equal(3, offsets3.Count);
            Assert.True(RevitUnitConverter.AreCloseMm(-120.0, RevitUnitConverter.FeetToMm(offsets3[0]), 0.1));
            Assert.True(RevitUnitConverter.AreCloseMm(0.0, RevitUnitConverter.FeetToMm(offsets3[1]), 0.1));
            Assert.True(RevitUnitConverter.AreCloseMm(120.0, RevitUnitConverter.FeetToMm(offsets3[2]), 0.1));

            // Test 4 bars
            var offsets4 = FramingRebarGeometryCalculator.CalculateTransverseOffsets(clearWidthFt, 4);
            Assert.Equal(4, offsets4.Count);
            Assert.True(RevitUnitConverter.AreCloseMm(-120.0, RevitUnitConverter.FeetToMm(offsets4[0]), 0.1));
            Assert.True(RevitUnitConverter.AreCloseMm(120.0, RevitUnitConverter.FeetToMm(offsets4[3]), 0.1));
        }

        [Fact]
        public void CalculateStirrupCorners_FormsClosedRectangularHoopWithCoreDimensions()
        {
            RevitPoint3D stationCenter = new(RevitUnitConverter.MmToFeet(1000.0), 0.0, RevitUnitConverter.MmToFeet(3000.0));
            RevitPoint3D normal = new(0.0, 1.0, 0.0);
            double widthFt = RevitUnitConverter.MmToFeet(300.0);
            double heightFt = RevitUnitConverter.MmToFeet(600.0);
            double coverFt = RevitUnitConverter.MmToFeet(25.0);

            var corners = FramingRebarGeometryCalculator.CalculateStirrupCorners(
                stationCenter, normal, widthFt, heightFt, coverFt);

            Assert.Equal(4, corners.Count);

            // Width: distance between TL (corners[0]) and TR (corners[1])
            double actualWidthMm = RevitUnitConverter.FeetToMm(corners[0].DistanceTo(corners[1]));
            double expectedWidthMm = 300.0 - (2.0 * 25.0); // 250 mm
            Assert.True(RevitUnitConverter.AreCloseMm(expectedWidthMm, actualWidthMm, 0.1));

            // Height: distance between TL (corners[0]) and BL (corners[3])
            double actualHeightMm = RevitUnitConverter.FeetToMm(corners[0].DistanceTo(corners[3]));
            double expectedHeightMm = 600.0 - (2.0 * 25.0); // 550 mm
            Assert.True(RevitUnitConverter.AreCloseMm(expectedHeightMm, actualHeightMm, 0.1));
        }
    }
}
