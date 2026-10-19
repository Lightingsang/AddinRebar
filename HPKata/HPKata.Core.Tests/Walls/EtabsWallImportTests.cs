using System.Linq;
using HPKata.Core.Walls.Etabs;
using Xunit;

namespace HPKata.Core.Tests.Walls
{
    public class EtabsWallImportTests
    {
        [Fact]
        public void EtabsWallTableMapper_ParsesValidDelimitedText()
        {
            string raw = "Story\tPier\tThickness\tLength\tP\tV2\tM3\tAsb\tAvh\n" +
                         "Story1\tP1\t300\t3200\t-3500\t450\t1800\t2400\t600\n" +
                         "Story2\tP1\t300\t3200\t-2800\t380\t1400\t1800\t500\n";

            var designs = EtabsWallTableMapper.MapTableContent(raw);
            Assert.Equal(2, designs.Count);

            Assert.Equal("Story1", designs[0].StoryName);
            Assert.Equal("P1", designs[0].PierMark);
            Assert.Equal(300.0, designs[0].ThicknessMm);
            Assert.Equal(3200.0, designs[0].LengthMm);
            Assert.Equal(2400.0, designs[0].RequiredBoundaryAsbMm2);
        }

        [Fact]
        public void EtabsWallImportService_BuildsCompleteShearWallModel()
        {
            string raw = "Story\tPier\tThickness\tLength\tP\tV2\tM3\tAsb\tAvh\n" +
                         "Story1\tP1\t300\t3200\t-3500\t450\t1800\t2400\t600\n" +
                         "Story2\tP1\t300\t3200\t-2800\t380\t1400\t1800\t500\n";

            var service = new EtabsWallImportService();
            var walls = service.ImportFromTableText(raw);

            Assert.Single(walls);
            var wall = walls[0];

            Assert.Equal("P1", wall.WallMark);
            Assert.Equal(2, wall.Stories.Count);
            Assert.Equal(300.0, wall.ThicknessMm);
            Assert.Equal(3200.0, wall.LengthMm);

            // Boundary elements left and right
            Assert.Equal(2, wall.BoundaryElements.Count);
            Assert.True(wall.BoundaryElements[0].TotalSteelAreaMm2 >= 2400.0);

            // Web mesh
            Assert.True(wall.WebReinforcement.IsDoubleCurtain);
        }

        [Fact]
        public void EtabsWallImportService_GroupsMultiplePiers()
        {
            string raw = "Story\tPier\tThickness\tLength\tP\tV2\tM3\tAsb\tAvh\n" +
                         "Story1\tP1\t300\t3200\t-3500\t450\t1800\t2400\t600\n" +
                         "Story1\tP2\t350\t4000\t-4200\t520\t2200\t3000\t700\n";

            var service = new EtabsWallImportService();
            var walls = service.ImportFromTableText(raw);

            Assert.Equal(2, walls.Count);
            Assert.Contains(walls, w => w.WallMark == "P1");
            Assert.Contains(walls, w => w.WallMark == "P2");
        }
    }
}
