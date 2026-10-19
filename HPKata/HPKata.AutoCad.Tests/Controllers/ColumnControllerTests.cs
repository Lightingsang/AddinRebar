using System.Linq;
using HPKata.AutoCad.Controllers;
using HPKata.AutoCad.Interop;
using HPKata.Core.Columns.Etabs;
using HPKata.Core.Columns.Models;
using HPKata.Core.Columns.Persistence;
using Xunit;

namespace HPKata.AutoCad.Tests.Controllers
{
    public sealed class ColumnControllerTests
    {
        [Fact]
        public void ColumnTakeoffRegistry_RegistersAndRetrievesLayouts()
        {
            ColumnTakeoffRegistry.Clear();
            var layout = new ColumnRebarLayout { ColumnMark = "C1-TEST" };

            ColumnTakeoffRegistry.Register(layout);

            Assert.True(ColumnTakeoffRegistry.TryGet("C1-TEST", out var retrieved));
            Assert.NotNull(retrieved);
            Assert.Equal("C1-TEST", retrieved.ColumnMark);

            var all = ColumnTakeoffRegistry.GetAll();
            Assert.Single(all);

            ColumnTakeoffRegistry.Clear();
            Assert.Empty(ColumnTakeoffRegistry.GetAll());
        }

        [Fact]
        public void ColumnDrawController_CreateDefaultLayout_ProducesValidStoriesAndTies()
        {
            var layout = ColumnDrawController.CreateDefaultLayout();

            Assert.Equal("C1", layout.ColumnMark);
            Assert.Equal(6, layout.Stories.Count);
            Assert.Equal(1000.0, layout.Stories[0].WidthMm);
            Assert.Equal(1000.0, layout.Stories[1].WidthMm);
            Assert.Equal(6, layout.StoryTies.Count);
        }

        [Fact]
        public void KataBlueprintRoot_LoadColumnDefault_Loads469KataProColumnEntities()
        {
            var bp = KataBlueprintRoot.LoadColumnDefault();
            Assert.NotNull(bp);
            Assert.Equal(469, bp!.Items.Count);
            Assert.All(bp.Items.Where(i => i.Type == "TXT"),
                txt => Assert.True(txt.LinePoints == null || txt.LinePoints.Length <= 6 || (int)txt.LinePoints[6] != 10));

            var layout = ColumnDrawController.CreateDefaultLayout();
            Assert.True(KataBeamBlueprintRenderer.CanRenderColumnBlueprint(layout));
        }

        [Fact]
        public void ColumnPersistence_RoundtripsLayoutViaSerializer()
        {
            var layout = ColumnDrawController.CreateDefaultLayout();
            string json = ColumnXRecordSerializer.SerializeToJson(layout);
            var deserialized = ColumnXRecordSerializer.DeserializeFromJson(json);

            Assert.NotNull(deserialized);
            Assert.Equal(layout.ColumnMark, deserialized.ColumnMark);
            Assert.Equal(layout.Stories.Count, deserialized.Stories.Count);
        }

        [Fact]
        public void ColumnEtabsController_GetSampleEtabsColumnText_ParsesCorrectly()
        {
            string sample = ColumnEtabsController.GetSampleEtabsColumnText();
            var service = new EtabsColumnImportService();
            var layouts = service.ImportFromTableText(sample);

            Assert.Equal(2, layouts.Count);
            Assert.Contains(layouts, l => l.ColumnMark == "C1");
            Assert.Contains(layouts, l => l.ColumnMark == "C2");
        }
    }
}
