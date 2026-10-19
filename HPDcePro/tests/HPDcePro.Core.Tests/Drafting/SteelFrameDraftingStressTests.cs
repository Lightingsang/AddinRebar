using System;
using System.Linq;
using HPDcePro.Core.Drafting;
using Xunit;

namespace HPDcePro.Core.Tests.Drafting
{
    public sealed class SteelFrameDraftingStressTests
    {
        private readonly SteelFrameDraftingGenerator _generator = new();

        private static void AssertAllEntitiesValid(CadDraftingBuffer buffer, string context)
        {
            Assert.NotNull(buffer);
            Assert.True(buffer.Count > 0, $"{context}: buffer should contain primitives");

            foreach (var entity in buffer.Entities)
            {
                AssertEntityValid(entity, context);
            }

            var (min, max) = buffer.GetBoundingBox();
            Assert.False(double.IsNaN(min.X) || double.IsInfinity(min.X), $"{context}: BoundingBox min X invalid");
            Assert.False(double.IsNaN(min.Y) || double.IsInfinity(min.Y), $"{context}: BoundingBox min Y invalid");
            Assert.False(double.IsNaN(max.X) || double.IsInfinity(max.X), $"{context}: BoundingBox max X invalid");
            Assert.False(double.IsNaN(max.Y) || double.IsInfinity(max.Y), $"{context}: BoundingBox max Y invalid");
        }

        private static void AssertEntityValid(CadEntityPrimitive entity, string context)
        {
            Assert.False(string.IsNullOrWhiteSpace(entity.Layer), $"{context}: Layer must not be null/empty");
            switch (entity.EntityType)
            {
                case CadEntityType.Line:
                case CadEntityType.Dimension:
                    Assert.False(double.IsNaN(entity.StartPoint.X) || double.IsInfinity(entity.StartPoint.X), $"{context}: Start X invalid");
                    Assert.False(double.IsNaN(entity.StartPoint.Y) || double.IsInfinity(entity.StartPoint.Y), $"{context}: Start Y invalid");
                    Assert.False(double.IsNaN(entity.EndPoint.X) || double.IsInfinity(entity.EndPoint.X), $"{context}: End X invalid");
                    Assert.False(double.IsNaN(entity.EndPoint.Y) || double.IsInfinity(entity.EndPoint.Y), $"{context}: End Y invalid");
                    break;
                case CadEntityType.Circle:
                    Assert.False(double.IsNaN(entity.Center.X) || double.IsInfinity(entity.Center.X), $"{context}: Center X invalid");
                    Assert.False(double.IsNaN(entity.Center.Y) || double.IsInfinity(entity.Center.Y), $"{context}: Center Y invalid");
                    Assert.False(double.IsNaN(entity.Radius) || double.IsInfinity(entity.Radius) || entity.Radius <= 0, $"{context}: Radius invalid");
                    break;
                case CadEntityType.Polyline:
                    Assert.NotNull(entity.Points);
                    Assert.True(entity.Points.Count >= 2, $"{context}: Polyline must have >= 2 vertices");
                    foreach (var v in entity.Points)
                    {
                        Assert.False(double.IsNaN(v.X) || double.IsInfinity(v.X), $"{context}: Vertex X invalid");
                        Assert.False(double.IsNaN(v.Y) || double.IsInfinity(v.Y), $"{context}: Vertex Y invalid");
                    }
                    break;
                case CadEntityType.Text:
                    Assert.False(double.IsNaN(entity.Position.X) || double.IsInfinity(entity.Position.X), $"{context}: Text X invalid");
                    Assert.False(double.IsNaN(entity.Position.Y) || double.IsInfinity(entity.Position.Y), $"{context}: Text Y invalid");
                    Assert.False(double.IsNaN(entity.TextHeight) || double.IsInfinity(entity.TextHeight) || entity.TextHeight <= 0, $"{context}: Text height invalid");
                    Assert.NotNull(entity.TextString);
                    break;
            }
        }

        [Fact]
        public void ZeroSpanAndZeroHeight_AllRoutines_GenerateValidEntitiesWithoutCrashing()
        {
            var options = new SteelFrameDraftingOptions
            {
                SpanMm = 0.0,
                EaveHeightMm = 0.0,
                RoofSlopePercent = 0.0,
                BuildingLengthMm = 0.0,
                BaySpacingMm = 6000.0,
                PurlinSpacingMm = 1200.0
            };

            AssertAllEntitiesValid(_generator.GenerateElevationMiddleFrameBT(options), "MiddleFrameBT");
            AssertAllEntitiesValid(_generator.GenerateElevationMiddleFrame3(options), "MiddleFrame3");
            AssertAllEntitiesValid(_generator.GenerateElevationMiddleFrame3BT(options), "MiddleFrame3BT");
            AssertAllEntitiesValid(_generator.GenerateElevationMiddleFrame5(options), "MiddleFrame5");
            AssertAllEntitiesValid(_generator.GenerateElevationMiddleFrame5a(options), "MiddleFrame5a");
            AssertAllEntitiesValid(_generator.GenerateElevationVDS1(options), "VDS1");
            AssertAllEntitiesValid(_generator.GeneratePlanPurlinRoof(options), "PlanPurlinRoof");
            AssertAllEntitiesValid(_generator.GeneratePlanPurlinMonitor(options), "PlanPurlinMonitor");
            AssertAllEntitiesValid(_generator.GenerateDetailAnchorBolt(options), "DetailAnchorBolt");
        }

        [Theory]
        [InlineData(-5.0)]
        [InlineData(-10.0)]
        [InlineData(-25.0)]
        public void InvertedRoofSlope_EaveHigherThanRidge_GeneratesValidCoordinates(double slopePercent)
        {
            var options = new SteelFrameDraftingOptions
            {
                SpanMm = 24000.0,
                EaveHeightMm = 8000.0,
                RoofSlopePercent = slopePercent
            };

            AssertAllEntitiesValid(_generator.GenerateElevationMiddleFrameBT(options), "MiddleFrameBT");
            AssertAllEntitiesValid(_generator.GenerateElevationMiddleFrame3(options), "MiddleFrame3");
            AssertAllEntitiesValid(_generator.GenerateElevationMiddleFrame3BT(options), "MiddleFrame3BT");
            AssertAllEntitiesValid(_generator.GenerateElevationMiddleFrame5(options), "MiddleFrame5");
            AssertAllEntitiesValid(_generator.GenerateElevationMiddleFrame5a(options), "MiddleFrame5a");
            AssertAllEntitiesValid(_generator.GenerateElevationVDS1(options), "VDS1");
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(5729.0)]
        [InlineData(100.0)]
        public void ExtremeRoofSlopeAngles_GeneratesValidFiniteCoordinates(double slopePercent)
        {
            var options = new SteelFrameDraftingOptions
            {
                SpanMm = 18000.0,
                EaveHeightMm = 6000.0,
                RoofSlopePercent = slopePercent
            };

            AssertAllEntitiesValid(_generator.GenerateElevationMiddleFrameBT(options), "MiddleFrameBT");
            AssertAllEntitiesValid(_generator.GenerateElevationVDS1(options), "VDS1");
        }

        [Theory]
        [InlineData(50000.0, 15000.0)]
        [InlineData(100000.0, 25000.0)]
        public void ExtremeSpansAndHeights_GeneratePositiveNonEmptyBoundingBoxes(double span, double height)
        {
            var options = new SteelFrameDraftingOptions
            {
                SpanMm = span,
                EaveHeightMm = height,
                RoofSlopePercent = 12.0
            };

            var buffer = _generator.GenerateElevationMiddleFrame5(options);
            AssertAllEntitiesValid(buffer, $"MiddleFrame5 ({span}x{height})");

            var (min, max) = buffer.GetBoundingBox();
            Assert.True((max.X - min.X) >= span);
            Assert.True((max.Y - min.Y) >= height);
        }

        [Fact]
        public void ThousandsOfPurlins_DenseSpacing_GeneratesCorrectPurlinCountWithoutOverflow()
        {
            var options = new SteelFrameDraftingOptions
            {
                SpanMm = 30000.0,
                BuildingLengthMm = 60000.0,
                BaySpacingMm = 6000.0,
                PurlinSpacingMm = 50.0
            };

            var buffer = _generator.GeneratePlanPurlinRoof(options);
            AssertAllEntitiesValid(buffer, "PlanPurlinRoof");

            var purlinLines = buffer.Entities.Where(e => e.Layer == options.PurlinLayer).ToList();
            Assert.True(purlinLines.Count >= 500);
        }

        [Fact]
        public void UnicodeAndSpecialCharactersInLayerNames_PreservesLayerIdentity()
        {
            var options = new SteelFrameDraftingOptions
            {
                FrameLayer = "DCE_Khung_Thép_Tiền_Chế_#1_[2026]!@$%",
                RafterLayer = "DCE_Kèo_Thép_Tiết_Diện_Thay_Đổi",
                PurlinLayer = "DCE_Xà_Gồ_Z200_Mái_Tầng_1",
                BracingLayer = "DCE_Hệ_Giằng_Gió_D16_Cáp",
                AnchorLayer = "DCE_Bu_Lông_Neo_M24",
                TextLayer = "DCE_Ghi_Chú_Unicode_Tiếng_Việt",
                DimLayer = "DCE_Kích_Thước_Chi_Tiết",
                AxisLayer = "DCE_Lưới_Trục_A-B-C-1-2-3"
            };

            var b1 = _generator.GenerateElevationMiddleFrameBT(options);
            AssertAllEntitiesValid(b1, "MiddleFrameBT");
            var layers = b1.Entities.Select(e => e.Layer).Distinct().ToHashSet();
            Assert.Contains(options.FrameLayer, layers);
            Assert.Contains(options.RafterLayer, layers);
            Assert.Contains(options.AxisLayer, layers);
            Assert.Contains(options.TextLayer, layers);

            var b2 = _generator.GeneratePlanPurlinRoof(options);
            AssertAllEntitiesValid(b2, "PlanPurlinRoof");
            var layers2 = b2.Entities.Select(e => e.Layer).Distinct().ToHashSet();
            Assert.Contains(options.PurlinLayer, layers2);
            Assert.Contains(options.BracingLayer, layers2);
        }
    }
}
