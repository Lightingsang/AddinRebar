using System;
using HPKata.Core.Foundations.Calculators;
using HPKata.Core.Foundations.Models;
using HPKata.Core.Foundations.Persistence;
using Xunit;

namespace HPKata.Core.Tests.Foundations
{
    public class IsolatedFootingTests
    {
        [Fact]
        public void IsolatedFooting_Validation_ThrowsOnInvalidParameters()
        {
            var f = new IsolatedFootingModel { LengthMm = -100 };
            Assert.Throws<ArgumentException>(() => f.Validate());

            f.LengthMm = 1600;
            f.TotalHeightMm = 300;
            f.BaseSlabHeightMm = 350;
            Assert.Throws<ArgumentException>(() => f.Validate());
        }

        [Fact]
        public void CalculateFootingVolume_SlopedPedestal_CalculatesPrismoidalFormula()
        {
            var f = new IsolatedFootingModel
            {
                LengthMm = 1600.0,
                WidthMm = 1600.0,
                TotalHeightMm = 800.0,
                BaseSlabHeightMm = 350.0,
                PedestalLengthMm = 500.0,
                PedestalWidthMm = 500.0,
                PedestalStyle = PedestalType.Sloped,
                MemberCount = 1
            };

            double vol = FootingGeometryCalculator.CalculateFootingVolume(f);
            // Base: 1.6 * 1.6 * 0.35 = 0.896 m3
            // Pedestal h2=0.45: (0.45 / 6) * [ 1.6*1.6 + 0.5*0.5 + 2.1*2.1 ] = 0.075 * [ 2.56 + 0.25 + 4.41 ] = 0.075 * 7.22 = 0.5415 m3
            // Total approx 1.4375 m3
            Assert.True(vol > 1.40 && vol < 1.48, $"Calculated volume {vol} out of range");
        }

        [Fact]
        public void CalculateFootingVolume_VerticalPedestal_CalculatesRectangular()
        {
            var f = new IsolatedFootingModel
            {
                LengthMm = 1600.0,
                WidthMm = 1600.0,
                TotalHeightMm = 800.0,
                BaseSlabHeightMm = 350.0,
                PedestalLengthMm = 500.0,
                PedestalWidthMm = 500.0,
                PedestalStyle = PedestalType.Vertical,
                MemberCount = 1
            };

            double vol = FootingGeometryCalculator.CalculateFootingVolume(f);
            // Base: 0.896 + Pedestal: 0.5 * 0.5 * 0.45 = 0.1125 => Total = 1.0085 m3
            Assert.Equal(1.0085, vol, 4);
        }

        [Fact]
        public void CalculateFootingFormwork_CalculatesPerimeterAreas()
        {
            var f = new IsolatedFootingModel
            {
                LengthMm = 1600.0,
                WidthMm = 1600.0,
                TotalHeightMm = 800.0,
                BaseSlabHeightMm = 350.0,
                PedestalLengthMm = 500.0,
                PedestalWidthMm = 500.0,
                PedestalStyle = PedestalType.Vertical,
                MemberCount = 1
            };

            double fw = FootingGeometryCalculator.CalculateFootingFormwork(f);
            // Base: 2*(1.6+1.6)*0.35 = 2.24 m2
            // Pedestal: 2*(0.5+0.5)*0.45 = 0.90 m2 => Total = 3.14 m2
            Assert.Equal(3.14, fw, 2);
        }

        [Fact]
        public void AggregateFooting_GeneratesCorrectRebarItems()
        {
            var f = new IsolatedFootingModel
            {
                FootingMark = "M1",
                LengthMm = 1600.0,
                WidthMm = 1600.0,
                TotalHeightMm = 800.0,
                BaseSlabHeightMm = 350.0,
                PedestalLengthMm = 500.0,
                PedestalWidthMm = 500.0,
                BottomMeshXDiaMm = 14,
                BottomMeshXSpacingMm = 150.0,
                StarterDowelCount = 4,
                StarterDowelDiaMm = 18
            };

            var items = FoundationScheduleAggregator.AggregateFooting(f);
            Assert.True(items.Count >= 3);
            Assert.Contains(items, it => it.BarMark == "1" && it.DiameterMm == 14 && it.ShapeCode == "02");
            Assert.Contains(items, it => it.BarMark == "2" && it.DiameterMm == 14 && it.ShapeCode == "02");
            Assert.Contains(items, it => it.BarMark == "3" && it.DiameterMm == 18);
        }
    }
}
