using System.Collections.Generic;
using HPKata.Core.Beams.Etabs;
using Xunit;

namespace HPKata.Core.Tests.Beams
{
    public class EtabsImportTests
    {
        [Theory]
        [InlineData("Story\tBeam\tStation\tTopArea", '\t')]
        [InlineData("Story,Beam,Station,TopArea", ',')]
        [InlineData("Story;Beam;Station;TopArea", ';')]
        public void DetectDelimiter_StandardHeaders_DetectsCorrectDelimiter(string header, char expected)
        {
            char delim = EtabsTextTableParser.DetectDelimiter(header);
            Assert.Equal(expected, delim);
        }

        [Fact]
        public void ParseDelimitedRows_SampleTable_ParsesRows()
        {
            string content = "Story\tBeam\tStation\tTopArea\tBotArea\n" +
                             "Story 1\tB1\t0.0\t850.5\t420.0\n" +
                             "Story 1\tB1\t3000.0\t350.0\t890.2\n" +
                             "Story 1\tB1\t6000.0\t920.0\t410.0\n";

            var rows = EtabsTextTableParser.ParseDelimitedRows(content);
            Assert.Equal(4, rows.Count); // Header + 3 data rows
            Assert.Equal("Story 1", rows[1][0]);
            Assert.Equal("B1", rows[1][1]);
        }

        [Fact]
        public void EtabsDesignAreaMapper_ValidTable_MapsFrameStationsAndProperties()
        {
            string content = "Story\tBeam\tStation\tTopArea\tBotArea\tShearRebar\n" +
                             "Story 1\tB1\t0.0\t950.0\t400.0\t0.50\n" +
                             "Story 1\tB1\t3000.0\t300.0\t850.0\t0.25\n" +
                             "Story 1\tB1\t6000.0\t1100.0\t400.0\t0.55\n";

            var rows = EtabsTextTableParser.ParseDelimitedRows(content);
            var frames = EtabsDesignAreaMapper.MapDesignTable(rows);

            Assert.Single(frames);
            Assert.True(frames.ContainsKey("B1"));

            var b1 = frames["B1"];
            Assert.Equal(6000.0, b1.LengthMm);
            Assert.Equal(950.0, b1.AsTopLeftRequired);
            Assert.Equal(850.0, b1.AsBotMidRequired);
            Assert.Equal(1100.0, b1.AsTopRightRequired);
            Assert.Equal(0.55, b1.AvOverSMax);
        }

        [Fact]
        public void EtabsFrameChainBuilder_CollinearFrames_BuildsContinuousRun()
        {
            var f1 = new EtabsFrameGeometry
            {
                FrameName = "B1", Story = "Story 1",
                StartX = 0, StartY = 0, StartZ = 3000,
                EndX = 6000, EndY = 0, EndZ = 3000
            };
            var f2 = new EtabsFrameGeometry
            {
                FrameName = "B2", Story = "Story 1",
                StartX = 6000, StartY = 0, StartZ = 3000,
                EndX = 12000, EndY = 0, EndZ = 3000
            };

            var frames = new List<EtabsFrameGeometry> { f1, f2 };
            var chains = EtabsFrameChainBuilder.BuildChains(frames);

            Assert.Single(chains);
            Assert.Equal(2, chains[0].Frames.Count);
            Assert.Equal(12000.0, chains[0].TotalLengthMm);
        }

        [Fact]
        public void EtabsBeamImportService_EndToEnd_ConvertsTableToRebarLayout()
        {
            string content = "Story\tBeam\tStation\tTopArea\tBotArea\n" +
                             "Story 2\tB101\t0.0\t800.0\t400.0\n" +
                             "Story 2\tB101\t5000.0\t800.0\t950.0\n";

            var layouts = EtabsBeamImportService.ImportFromTableContent(content);
            Assert.Single(layouts);

            var layout = layouts[0];
            Assert.Equal("B101", layout.BeamName);
            Assert.Single(layout.Spans);
            Assert.NotEmpty(layout.TopContinuousBars);
            Assert.NotEmpty(layout.BottomContinuousBars);
            Assert.True(layout.StirrupZones.ContainsKey(0));
        }
    }
}
