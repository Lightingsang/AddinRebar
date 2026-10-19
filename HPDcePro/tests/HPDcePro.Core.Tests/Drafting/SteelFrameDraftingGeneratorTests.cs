using System.Linq;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;
using Xunit;

namespace HPDcePro.Core.Tests.Drafting
{
    public sealed class SteelFrameDraftingGeneratorTests
    {
        private readonly SteelFrameDraftingGenerator _generator = new();

        [Fact]
        public void GenerateElevationMiddleFrameBT_DefaultOptions_ProducesPrimitivesAndLayers()
        {
            var buffer = _generator.GenerateElevationMiddleFrameBT();

            Assert.NotNull(buffer);
            Assert.True(buffer.Count > 10, "Should generate frame, rafter, and axis primitives");

            var layers = buffer.Entities.Select(e => e.Layer).Distinct().ToHashSet();
            Assert.Contains("DCE_Thep_Khung", layers);
            Assert.Contains("DCE_Thep_Keo", layers);
            Assert.Contains("DCE_Thep_Truc", layers);
            Assert.Contains("DCE_Thep_Text", layers);

            var (min, max) = buffer.GetBoundingBox();
            Assert.True(max.X > min.X, "Bounding box width should be positive");
            Assert.True(max.Y > min.Y, "Bounding box height should be positive");
        }

        [Fact]
        public void GenerateElevationMiddleFrame3_ValidOptions_ProducesThreeColumns()
        {
            var options = new SteelFrameDraftingOptions
            {
                SpanMm = 30000.0,
                EaveHeightMm = 8000.0
            };

            var buffer = _generator.GenerateElevationMiddleFrame3(options);

            Assert.True(buffer.Count > 15);
            var texts = buffer.Entities
                .Where(e => e.EntityType == CadEntityType.Text)
                .Select(e => e.TextString)
                .ToList();

            Assert.Contains(texts, t => t.Contains("MIDDLEFRAME3"));
            Assert.Contains(texts, t => t == "A");
            Assert.Contains(texts, t => t == "B");
            Assert.Contains(texts, t => t == "C");
        }

        [Fact]
        public void GenerateElevationMiddleFrame3BT_ValidOptions_ProducesConcreteColumns()
        {
            var buffer = _generator.GenerateElevationMiddleFrame3BT();

            Assert.True(buffer.Count > 15);
            var polylines = buffer.Entities.Where(e => e.EntityType == CadEntityType.Polyline).ToList();
            Assert.True(polylines.Count >= 3, "MiddleFrame3BT should draw 3 concrete column rectangles");

            var texts = buffer.Entities.Where(e => e.EntityType == CadEntityType.Text).Select(e => e.TextString);
            Assert.Contains(texts, t => t.Contains("MIDDLEFRAME3BT"));
        }

        [Fact]
        public void GenerateElevationMiddleFrame5_ValidOptions_ProducesFiveColumnAxes()
        {
            var options = new SteelFrameDraftingOptions
            {
                SpanMm = 40000.0,
                EaveHeightMm = 9000.0
            };

            var buffer = _generator.GenerateElevationMiddleFrame5(options);

            Assert.True(buffer.Count > 25);
            var texts = buffer.Entities.Where(e => e.EntityType == CadEntityType.Text).Select(e => e.TextString).ToHashSet();
            Assert.Contains("1", texts);
            Assert.Contains("2", texts);
            Assert.Contains("3", texts);
            Assert.Contains("4", texts);
            Assert.Contains("5", texts);
        }

        [Fact]
        public void GenerateElevationMiddleFrame5a_ValidOptions_IncludesCraneAndMonitor()
        {
            var buffer = _generator.GenerateElevationMiddleFrame5a();

            var texts = buffer.Entities
                .Where(e => e.EntityType == CadEntityType.Text)
                .Select(e => e.TextString)
                .ToList();

            Assert.Contains(texts, t => t.Contains("CỬA TRỜI THÔNG GIÓ"));
            Assert.True(buffer.Count > 30, "Should include base frame, crane brackets, and roof monitor");
        }

        [Fact]
        public void GenerateElevationVDS1_ValidOptions_ProducesTrussWebsAndChords()
        {
            var buffer = _generator.GenerateElevationVDS1();

            Assert.True(buffer.Count >= 20);
            var bracings = buffer.Entities.Where(e => e.Layer == "DCE_Thep_Giang").ToList();
            Assert.True(bracings.Count >= 6, "VDS1 truss should produce multiple diagonal web members");

            var title = buffer.Entities.FirstOrDefault(e => e.EntityType == CadEntityType.Text && e.TextString.Contains("VDS1"));
            Assert.NotNull(title);
        }

        [Fact]
        public void GeneratePlanPurlinRoof_ValidOptions_ProducesPurlinsAndSagRods()
        {
            var options = new SteelFrameDraftingOptions
            {
                BuildingLengthMm = 36000.0,
                BaySpacingMm = 6000.0,
                PurlinSpacingMm = 1200.0
            };

            var buffer = _generator.GeneratePlanPurlinRoof(options);

            var purlinLines = buffer.Entities.Where(e => e.Layer == options.PurlinLayer).ToList();
            Assert.True(purlinLines.Count > 10, "Should generate purlin lines on purlin layer");

            var bracingLines = buffer.Entities.Where(e => e.Layer == options.BracingLayer).ToList();
            Assert.True(bracingLines.Count > 0, "Should generate roof sag rods and cross bracing");
        }

        [Fact]
        public void GeneratePlanPurlinMonitor_ValidOptions_ProducesLouversAndPurlins()
        {
            var options = new SteelFrameDraftingOptions
            {
                BuildingLengthMm = 30000.0,
                MonitorWidthMm = 4500.0,
                MonitorPurlinSpacingMm = 900.0
            };

            var buffer = _generator.GeneratePlanPurlinMonitor(options);

            var texts = buffer.Entities.Where(e => e.EntityType == CadEntityType.Text).Select(e => e.TextString).ToList();
            Assert.Contains(texts, t => t.Contains("CỬA TRỜI THÔNG GIÓ"));

            var (min, max) = buffer.GetBoundingBox();
            Assert.True(max.Y - min.Y >= options.BuildingLengthMm);
        }

        [Fact]
        public void GenerateDetailAnchorBolt_ValidOptions_ProducesPlanAndSection()
        {
            var options = new SteelFrameDraftingOptions
            {
                ColumnMark = "C2",
                AnchorBoltCount = 4,
                AnchorBoltDiameterMm = 28.0
            };

            var buffer = _generator.GenerateDetailAnchorBolt(options);

            var circles = buffer.Entities.Where(e => e.EntityType == CadEntityType.Circle).ToList();
            Assert.True(circles.Count >= 4, "Should draw anchor bolt holes");

            var texts = buffer.Entities.Where(e => e.EntityType == CadEntityType.Text).Select(e => e.TextString).ToList();
            Assert.Contains(texts, t => t.Contains("BU LÔNG NEO - CỘT C2"));
            Assert.Contains(texts, t => t.Contains("CHI TIẾT CHÂN CỘT & BU LÔNG NEO"));
        }

        [Fact]
        public void GenerateElevation_CustomLayersAndOrigin_AppliesCorrectly()
        {
            var options = new SteelFrameDraftingOptions
            {
                Origin = new Point2D(5000, 10000),
                FrameLayer = "Custom_Khung",
                RafterLayer = "Custom_Keo"
            };

            var buffer = _generator.GenerateElevationMiddleFrameBT(options);

            var layers = buffer.Entities.Select(e => e.Layer).Distinct().ToHashSet();
            Assert.Contains("Custom_Khung", layers);
            Assert.Contains("Custom_Keo", layers);
            Assert.DoesNotContain("DCE_Thep_Khung", layers);

            var (min, _) = buffer.GetBoundingBox();
            Assert.True(min.X >= 4000.0, "Origin X offset should be applied");
            Assert.True(min.Y >= 8000.0, "Origin Y offset should be applied");
        }

        [Fact]
        public void BufferTransformBy_AppliesTranslationAndScale()
        {
            var buffer = _generator.GenerateDetailAnchorBolt();
            int initialCount = buffer.Count;
            var (min1, max1) = buffer.GetBoundingBox();

            buffer.TransformBy(1000.0, 2000.0, scale: 2.0);

            Assert.Equal(initialCount, buffer.Count);
            var (min2, max2) = buffer.GetBoundingBox();
            Assert.True((max2.X - min2.X) > (max1.X - min1.X), "Scaled bounding box should be wider");
        }
    }
}
