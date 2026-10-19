using System.Linq;
using HPKata.Core.Columns.Etabs;
using Xunit;

namespace HPKata.Core.Tests.Columns
{
    public class EtabsColumnImportTests
    {
        [Fact]
        public void EtabsColumnTableMapper_ParsesValidDelimitedText()
        {
            string raw = "Story\tColumn\tWidth\tDepth\tP\tM2\tM3\tAst\tAv/s\n" +
                         "Story1\tC1\t500\t500\t-1500\t120\t240\t2800\t800\n" +
                         "Story2\tC1\t450\t450\t-1100\t90\t180\t2200\t700\n";

            var designs = EtabsColumnTableMapper.MapTableContent(raw);
            Assert.Equal(2, designs.Count);

            Assert.Equal("Story1", designs[0].StoryName);
            Assert.Equal("C1", designs[0].ColumnMark);
            Assert.Equal(500.0, designs[0].WidthMm);
            Assert.Equal(500.0, designs[0].DepthMm);
            Assert.Equal(-1500.0, designs[0].AxialForceP_kN);
            Assert.Equal(2800.0, designs[0].RequiredAstMm2);
        }

        [Fact]
        public void EtabsColumnImportService_BuildsMultiStoryColumnLayout()
        {
            string raw = "Story\tColumn\tWidth\tDepth\tP\tM2\tM3\tAst\tAv/s\n" +
                         "Story1\tC1\t500\t500\t-1500\t120\t240\t2800\t800\n" +
                         "Story2\tC1\t450\t450\t-1100\t90\t180\t2200\t700\n";

            var service = new EtabsColumnImportService();
            var layouts = service.ImportFromTableText(raw);

            Assert.Single(layouts);
            var col = layouts[0];
            Assert.Equal("C1", col.ColumnMark);
            Assert.Equal(2, col.Stories.Count);

            Assert.Equal(500.0, col.Stories[0].WidthMm);
            Assert.Equal(450.0, col.Stories[1].WidthMm);
            Assert.True(col.Stories[0].Section.TotalSteelAreaMm2 >= 2800.0);
            Assert.True(col.LongitudinalBars.Count > 0);
            Assert.Single(col.FoundationDowels);
        }

        [Fact]
        public void EtabsColumnImportService_GroupsMultipleColumns()
        {
            string raw = "Story\tColumn\tWidth\tDepth\tP\tM2\tM3\tAst\tAv/s\n" +
                         "Story1\tC1\t500\t500\t-1500\t120\t240\t2800\t800\n" +
                         "Story1\tC2\t400\t400\t-900\t50\t100\t1600\t600\n";

            var service = new EtabsColumnImportService();
            var layouts = service.ImportFromTableText(raw);

            Assert.Equal(2, layouts.Count);
            Assert.Contains(layouts, l => l.ColumnMark == "C1");
            Assert.Contains(layouts, l => l.ColumnMark == "C2");
        }
    }
}
