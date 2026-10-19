using System;
using HPKata.Core.Columns.Models;
using HPKata.Core.Walls.Models;
using HPKata.Revit.Units;
using Xunit;

namespace HPKata.Revit.Tests.Services
{
    public sealed class ColumnWallGeometryTests
    {
        [Fact]
        public void ColumnGeometry_RevitUnitConversion_RoundTripsAccurately()
        {
            double widthMm = 450.0;
            double depthMm = 450.0;
            double coverMm = 30.0;

            double widthFt = RevitUnitConverter.MmToFeet(widthMm);
            double depthFt = RevitUnitConverter.MmToFeet(depthMm);
            double coverFt = RevitUnitConverter.MmToFeet(coverMm);

            double halfWFt = (widthFt / 2.0) - coverFt;
            double halfDFt = (depthFt / 2.0) - coverFt;

            double actualHalfWMm = RevitUnitConverter.FeetToMm(halfWFt);
            double expectedHalfWMm = (widthMm / 2.0) - coverMm; // 195.0 mm

            Assert.True(RevitUnitConverter.AreCloseMm(expectedHalfWMm, actualHalfWMm, 0.01));
            Assert.True(halfWFt > 0.0);
            Assert.True(halfDFt > 0.0);
        }

        [Fact]
        public void WallGeometry_RevitUnitConversion_CalculatesBoundaryOffsets()
        {
            double wallLenMm = 3500.0;
            double thickMm = 300.0;
            double coverMm = 25.0;

            double wallLenFt = RevitUnitConverter.MmToFeet(wallLenMm);
            double thickFt = RevitUnitConverter.MmToFeet(thickMm);
            double coverFt = RevitUnitConverter.MmToFeet(coverMm);

            double lateralOffsetFt = (thickFt / 2.0) - coverFt;
            double actualLateralMm = RevitUnitConverter.FeetToMm(lateralOffsetFt);
            double expectedLateralMm = (thickMm / 2.0) - coverMm; // 125.0 mm

            Assert.True(RevitUnitConverter.AreCloseMm(expectedLateralMm, actualLateralMm, 0.01));
            Assert.True(wallLenFt > thickFt);
        }

        [Fact]
        public void ColumnLayout_ToSectionModel_ConvertsAccurately()
        {
            var sec = new ColumnSectionModel
            {
                WidthMm = 500.0,
                DepthMm = 500.0,
                CornerDiameterMm = 25
            };

            double widthFt = RevitUnitConverter.MmToFeet(sec.WidthMm);
            double recoveredWidthMm = RevitUnitConverter.FeetToMm(widthFt);
            Assert.True(RevitUnitConverter.AreCloseMm(500.0, recoveredWidthMm, 0.01));
        }
    }
}
