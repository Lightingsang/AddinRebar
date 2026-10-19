using System.Linq;
using HPKata.AutoCad.Controllers;
using HPKata.AutoCad.Interop;
using HPKata.Core.Walls.Etabs;
using HPKata.Core.Walls.Models;
using HPKata.Core.Walls.Persistence;
using Xunit;

namespace HPKata.AutoCad.Tests.Controllers
{
    public sealed class WallControllerTests
    {
        [Fact]
        public void WallTakeoffRegistry_RegistersAndRetrievesModels()
        {
            WallTakeoffRegistry.Clear();
            var wall = new ShearWallModel { WallMark = "V1-TEST" };

            WallTakeoffRegistry.Register(wall);

            Assert.True(WallTakeoffRegistry.TryGet("V1-TEST", out var retrieved));
            Assert.NotNull(retrieved);
            Assert.Equal("V1-TEST", retrieved.WallMark);

            var all = WallTakeoffRegistry.GetAll();
            Assert.Single(all);

            WallTakeoffRegistry.Clear();
            Assert.Empty(WallTakeoffRegistry.GetAll());
        }

        [Fact]
        public void WallDrawController_CreateDefaultWallModel_ProducesValidBoundaryAndWeb()
        {
            var wall = WallDrawController.CreateDefaultWallModel();

            Assert.Equal("V1", wall.WallMark);
            Assert.Equal(600.0, wall.ThicknessMm);
            Assert.Equal(3000.0, wall.LengthMm);
            Assert.Equal(6, wall.Stories.Count);
            Assert.Equal(2, wall.BoundaryElements.Count);
            Assert.True(wall.WebReinforcement.IsDoubleCurtain);
        }

        [Fact]
        public void KataBlueprintRoot_LoadWallDefault_Loads627KataProWallEntities()
        {
            var bp = KataBlueprintRoot.LoadWallDefault();
            Assert.NotNull(bp);
            Assert.Equal(627, bp!.Items.Count);
            Assert.All(bp.Items.Where(i => i.Type == "TXT"),
                txt => Assert.True(txt.LinePoints == null || txt.LinePoints.Length <= 6 || (int)txt.LinePoints[6] != 10));

            var wall = WallDrawController.CreateDefaultWallModel();
            Assert.True(KataBeamBlueprintRenderer.CanRenderWallBlueprint(wall));
        }

        [Fact]
        public void WallCoreController_CreateDefaultCoreModel_ProducesValidCShapeCore()
        {
            var core = WallCoreController.CreateDefaultCoreModel();

            Assert.Equal("CORE-1", core.WallMark);
            Assert.Equal(WallShapeType.C_Shape, core.ShapeType);
            Assert.Equal(2600.0, core.LengthMm);
            Assert.Single(core.BoundaryElements);
        }

        [Fact]
        public void WallEtabsController_GetSampleEtabsWallText_ParsesCorrectly()
        {
            string sample = WallEtabsController.GetSampleEtabsWallText();
            var service = new EtabsWallImportService();
            var walls = service.ImportFromTableText(sample);

            Assert.Equal(2, walls.Count);
            Assert.Contains(walls, w => w.WallMark == "P1");
            Assert.Contains(walls, w => w.WallMark == "P2");
        }
    }
}
