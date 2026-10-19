using System.Collections.Generic;
using HPDcePro.Core.AutoStr;
using HPDcePro.Core.Calculations.Takeoff;
using HPDcePro.Core.Serialization;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public class QuantityTakeoffTests
    {
        [Fact]
        public void Takeoff_ConcreteVolume_CalculatesCorrectly()
        {
            var elements = new List<ElementTakeoffItem>
            {
                new ElementTakeoffItem
                {
                    StoryName = "TẦNG 1",
                    ElementType = TakeoffElementType.Beam,
                    Mark = "D1",
                    Count = 2,
                    WidthMm = 200,
                    HeightMm = 400,
                    LengthMm = 5000,
                    ConcreteVolumeM3 = 2 * (0.2 * 0.4 * 5.0), // 0.8 m3
                    FormworkAreaM2 = 2 * (2 * 0.4 + 0.2) * 5.0
                },
                new ElementTakeoffItem
                {
                    StoryName = "TẦNG 1",
                    ElementType = TakeoffElementType.Column,
                    Mark = "C1",
                    Count = 4,
                    WidthMm = 300,
                    HeightMm = 300,
                    LengthMm = 3600,
                    ConcreteVolumeM3 = 4 * (0.3 * 0.3 * 3.6), // 1.296 m3
                    FormworkAreaM2 = 4 * (4 * 0.3) * 3.6
                }
            };

            var result = QuantityTakeoffCalculator.CalculateFromElements(elements, new List<RebarTakeoffItem>());

            Assert.Equal(2.096, result.TotalConcreteVolumeM3);
            Assert.Equal(2, result.Elements.Count);
        }

        [Fact]
        public void Takeoff_FormworkArea_CalculatesCorrectly()
        {
            var elements = new List<ElementTakeoffItem>
            {
                new ElementTakeoffItem
                {
                    StoryName = "TẦNG 2",
                    ElementType = TakeoffElementType.Slab,
                    Mark = "S1",
                    Count = 1,
                    WidthMm = 10000,
                    HeightMm = 120,
                    LengthMm = 10000,
                    ConcreteVolumeM3 = 10.0 * 10.0 * 0.12,
                    FormworkAreaM2 = 100.0 // 100 m2
                }
            };

            var result = QuantityTakeoffCalculator.CalculateFromElements(elements, new List<RebarTakeoffItem>());

            Assert.Equal(100.0, result.TotalFormworkAreaM2);
        }

        [Fact]
        public void Takeoff_RebarTonnage_GroupsByDiameterCategory()
        {
            Assert.Equal(RebarDiameterCategory.SmallLe10, QuantityTakeoffCalculator.CategorizeDiameter(8.0));
            Assert.Equal(RebarDiameterCategory.SmallLe10, QuantityTakeoffCalculator.CategorizeDiameter(10.0));
            Assert.Equal(RebarDiameterCategory.MediumLe18, QuantityTakeoffCalculator.CategorizeDiameter(12.0));
            Assert.Equal(RebarDiameterCategory.MediumLe18, QuantityTakeoffCalculator.CategorizeDiameter(18.0));
            Assert.Equal(RebarDiameterCategory.LargeGt18, QuantityTakeoffCalculator.CategorizeDiameter(20.0));
            Assert.Equal(RebarDiameterCategory.LargeGt18, QuantityTakeoffCalculator.CategorizeDiameter(25.0));

            var rebars = new List<RebarTakeoffItem>
            {
                new RebarTakeoffItem { StoryName = "TẦNG 1", Mark = "d10", DiameterMm = 10, TotalLengthM = 100, WeightKg = 61.65, Category = RebarDiameterCategory.SmallLe10 },
                new RebarTakeoffItem { StoryName = "TẦNG 1", Mark = "d16", DiameterMm = 16, TotalLengthM = 100, WeightKg = 157.83, Category = RebarDiameterCategory.MediumLe18 },
                new RebarTakeoffItem { StoryName = "TẦNG 1", Mark = "d22", DiameterMm = 22, TotalLengthM = 100, WeightKg = 298.41, Category = RebarDiameterCategory.LargeGt18 }
            };

            var result = QuantityTakeoffCalculator.CalculateFromElements(new List<ElementTakeoffItem>(), rebars);

            Assert.Equal(61.65, result.TotalRebarKgLe10);
            Assert.Equal(157.83, result.TotalRebarKgLe18);
            Assert.Equal(298.41, result.TotalRebarKgGt18);
            Assert.Equal(517.89, result.TotalRebarKg);
            Assert.Equal(0.518, result.TotalRebarTons, 3);
        }

        [Fact]
        public void Takeoff_StorySummaries_AggregatesAccurately()
        {
            var elements = new List<ElementTakeoffItem>
            {
                new ElementTakeoffItem { StoryName = "TẦNG 1", ConcreteVolumeM3 = 15.0, FormworkAreaM2 = 80.0 },
                new ElementTakeoffItem { StoryName = "TẦNG 2", ConcreteVolumeM3 = 20.0, FormworkAreaM2 = 95.0 }
            };
            var rebars = new List<RebarTakeoffItem>
            {
                new RebarTakeoffItem { StoryName = "TẦNG 1", WeightKg = 1200.0, Category = RebarDiameterCategory.MediumLe18 },
                new RebarTakeoffItem { StoryName = "TẦNG 2", WeightKg = 1500.0, Category = RebarDiameterCategory.MediumLe18 }
            };

            var result = QuantityTakeoffCalculator.CalculateFromElements(elements, rebars);

            Assert.Equal(2, result.StorySummaries.Count);
            Assert.Equal(35.0, result.TotalConcreteVolumeM3);
            Assert.Equal(175.0, result.TotalFormworkAreaM2);
            Assert.Equal(2700.0, result.TotalRebarKg);
        }

        [Fact]
        public void Takeoff_FromContext_GeneratesCompleteProjectBOQ()
        {
            var config = new AutoStrConfig();
            var context = new AutoStrStepContext(config, string.Empty);

            var result = QuantityTakeoffCalculator.CalculateTakeoff(context);

            Assert.NotNull(result);
            Assert.NotEmpty(result.Elements);
            Assert.NotEmpty(result.Rebars);
            Assert.NotEmpty(result.StorySummaries);
            Assert.True(result.TotalConcreteVolumeM3 > 0.0);
            Assert.True(result.TotalFormworkAreaM2 > 0.0);
            Assert.True(result.TotalRebarTons > 0.0);
        }

        [Fact]
        public void Takeoff_EmptyOrZeroGeometry_HandledGracefully()
        {
            var result = QuantityTakeoffCalculator.CalculateFromElements(
                new List<ElementTakeoffItem>(), new List<RebarTakeoffItem>());

            Assert.Empty(result.Elements);
            Assert.Empty(result.Rebars);
            Assert.Empty(result.StorySummaries);
            Assert.Equal(0.0, result.TotalConcreteVolumeM3);
            Assert.Equal(0.0, result.TotalFormworkAreaM2);
            Assert.Equal(0.0, result.TotalRebarKg);
            Assert.Equal(0.0, result.TotalRebarTons);
        }
    }
}
