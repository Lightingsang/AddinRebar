using System.Collections.Generic;
using HPDcePro.Core.AutoStr;
using HPDcePro.Core.AutoStr.Steps;
using HPDcePro.Core.Geometry;
using HPDcePro.Core.Serialization;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public class AutoStrResizingLoopTests
    {
        [Fact]
        public void ResizingLoop_IncreasesColumnSection_WhenUtilizationExceeds1()
        {
            var config = new AutoStrConfig
            {
                CotChoTangTietDien = true,
                CotBuocTang = 50.0,
                CotHeSoTangToiDa = 2.0
            };
            var orch = new AutoStrOrchestrator(config);
            var context = new AutoStrStepContext(config, string.Empty);

            var colDefs = new List<ColumnSectionDefinition>
            {
                new ColumnSectionDefinition { Mark = "C1", WidthMm = 300.0, HeightMm = 300.0, UtilizationRatio = 1.25 }
            };
            var overstressed = new List<ColumnDesignCheckResult>
            {
                new ColumnDesignCheckResult { Mark = "C1", WidthMm = 300.0, HeightMm = 300.0, UtilizationRatio = 1.25 }
            };
            context.SetArtifact("ColumnDefinitions", colDefs);
            context.SetArtifact("OverstressedColumns", overstressed);

            bool resized = orch.TryResizeOverstressedColumns(context);

            Assert.True(resized);
            Assert.Equal(350.0, colDefs[0].WidthMm);
            Assert.Equal(350.0, colDefs[0].HeightMm);
            Assert.True(colDefs[0].UtilizationRatio < 1.25);
        }

        [Fact]
        public void ResizingLoop_ClampsColumnSection_ToMaxFactor()
        {
            var config = new AutoStrConfig
            {
                CotChoTangTietDien = true,
                CotBuocTang = 50.0,
                CotHeSoTangToiDa = 2.0 // Max is 2.0 * 300 = 600mm
            };
            var orch = new AutoStrOrchestrator(config);
            var context = new AutoStrStepContext(config, string.Empty);

            var colDefs = new List<ColumnSectionDefinition>
            {
                new ColumnSectionDefinition { Mark = "C1", WidthMm = 600.0, HeightMm = 600.0, UtilizationRatio = 1.10 }
            };
            var overstressed = new List<ColumnDesignCheckResult>
            {
                new ColumnDesignCheckResult { Mark = "C1", WidthMm = 600.0, HeightMm = 600.0, UtilizationRatio = 1.10 }
            };
            context.SetArtifact("ColumnDefinitions", colDefs);
            context.SetArtifact("OverstressedColumns", overstressed);

            bool resized = orch.TryResizeOverstressedColumns(context);

            Assert.False(resized);
            Assert.Equal(600.0, colDefs[0].WidthMm);
            Assert.Equal(600.0, colDefs[0].HeightMm);
        }

        [Fact]
        public void ResizingLoop_DoesNotResizeColumn_WhenUtilizationIsSafe()
        {
            var config = new AutoStrConfig { CotChoTangTietDien = true };
            var orch = new AutoStrOrchestrator(config);
            var context = new AutoStrStepContext(config, string.Empty);

            var colDefs = new List<ColumnSectionDefinition>
            {
                new ColumnSectionDefinition { Mark = "C1", WidthMm = 300.0, HeightMm = 300.0, UtilizationRatio = 0.85 }
            };
            context.SetArtifact("ColumnDefinitions", colDefs);
            context.SetArtifact("OverstressedColumns", new List<ColumnDesignCheckResult>());

            bool resized = orch.TryResizeOverstressedColumns(context);

            Assert.False(resized);
            Assert.Equal(300.0, colDefs[0].WidthMm);
            Assert.Equal(300.0, colDefs[0].HeightMm);
        }

        [Fact]
        public void ResizingLoop_IncrementsPileCount_WhenReactionExceedsCapacity()
        {
            var config = new AutoStrConfig { CocTuTangKhiKhongDat = true };
            var orch = new AutoStrOrchestrator(config);
            var context = new AutoStrStepContext(config, string.Empty);

            var caps = new List<PileCapLayoutDefinition>
            {
                new PileCapLayoutDefinition
                {
                    Mark = "ĐC.1",
                    PileCount = 4,
                    PileCapacityKn = 350.0,
                    MaxPileReactionKn = 400.0
                }
            };
            context.SetArtifact("PileCapDefinitions", caps);
            context.SetArtifact("OverloadedPileCaps", new List<PileCapLayoutDefinition> { caps[0] });

            bool resized = orch.TryResizeOverloadedPiles(context);

            Assert.True(resized);
            Assert.Equal(5, caps[0].PileCount);
            Assert.True(caps[0].MaxPileReactionKn < 400.0);
        }

        [Fact]
        public void ResizingLoop_DoesNotIncrementPiles_WhenReactionIsSafe()
        {
            var config = new AutoStrConfig { CocTuTangKhiKhongDat = true };
            var orch = new AutoStrOrchestrator(config);
            var context = new AutoStrStepContext(config, string.Empty);

            var caps = new List<PileCapLayoutDefinition>
            {
                new PileCapLayoutDefinition
                {
                    Mark = "ĐC.1",
                    PileCount = 4,
                    PileCapacityKn = 350.0,
                    MaxPileReactionKn = 300.0
                }
            };
            context.SetArtifact("PileCapDefinitions", caps);
            context.SetArtifact("OverloadedPileCaps", new List<PileCapLayoutDefinition>());

            bool resized = orch.TryResizeOverloadedPiles(context);

            Assert.False(resized);
            Assert.Equal(4, caps[0].PileCount);
        }

        [Fact]
        public void ResizingLoop_DisabledFlags_PreventAnyAutoResizing()
        {
            var config = new AutoStrConfig
            {
                CotChoTangTietDien = false,
                CocTuTangKhiKhongDat = false
            };
            Assert.False(config.CotChoTangTietDien);
            Assert.False(config.CocTuTangKhiKhongDat);
        }
    }
}
