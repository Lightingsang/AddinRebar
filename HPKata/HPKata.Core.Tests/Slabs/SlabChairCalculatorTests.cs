using System;
using HPKata.Core.Slabs.Calculators;
using Xunit;

namespace HPKata.Core.Tests.Slabs
{
    public class SlabChairCalculatorTests
    {
        [Fact]
        public void CalculateStool_Standard120mmSlab_Calculates70mmHeight()
        {
            // hs = 120, cover = 15, top dia = 10, bot dia = 10
            // raw = 120 - 15 - 15 - 2*10 - 2*10 = 50mm, rounded to 50mm
            var stool = SlabChairCalculator.CalculateStool(
                slabThicknessMm: 120.0,
                topCoverMm: 15.0,
                bottomCoverMm: 15.0,
                topMeshDiaMm: 10,
                bottomMeshDiaMm: 10,
                densityPerM2: 1.2,
                slabAreaM2: 20.0);

            Assert.Equal(50.0, stool.StoolHeightMm);
            Assert.Equal(24, stool.TotalCount); // 20 * 1.2 = 24
            Assert.True(stool.CuttingLengthMm > 0);
        }

        [Fact]
        public void CalculateStool_ThinSlab_ClampsTo30mmMinimum()
        {
            var stool = SlabChairCalculator.CalculateStool(
                slabThicknessMm: 70.0,
                topCoverMm: 15.0,
                bottomCoverMm: 15.0,
                topMeshDiaMm: 10,
                bottomMeshDiaMm: 10);

            Assert.Equal(30.0, stool.StoolHeightMm);
        }

        [Fact]
        public void CreateChairRebarItem_SetsShape35AndCorrectMass()
        {
            var stool = SlabChairCalculator.CalculateStool(150.0);
            var item = SlabChairCalculator.CreateChairRebarItem("S1", stool, memberCount: 1);

            Assert.Equal("S1", item.ComponentMark);
            Assert.Equal("CK", item.BarMark);
            Assert.Equal("35", item.ShapeCode);
            Assert.True(item.UnitMassKgPerM > 0);
            Assert.True(item.TotalWeightKg > 0);
        }
    }
}
