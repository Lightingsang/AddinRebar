using System;
using System.Collections.Generic;
using HPKata.Core.Beams.Etabs;
using Xunit;

namespace HPKata.Core.Tests.Beams
{
    public class EtabsEmpiricalChallengeTests
    {
        [Fact]
        public void ParseDoubleInvariant_CommaDecimalSeparator_ExposesGroupSeparatorBehavior()
        {
            // In European/Vietnamese formatting, "850,5" means 850.5.
            double parsed = EtabsTextTableParser.ParseDoubleInvariant("850,5");
            Assert.Equal(850.5, parsed);
        }

        [Fact]
        public void ParseDelimitedRows_MixedSeparators_TabHeaderWithSpaceData_ExposesDelimiterLock()
        {
            // Header separated by tabs, but data rows separated by spaces
            string content = "Story\tBeam\tStation\tTopArea\n" +
                             "Story1 B1 0.0 850.5\n";

            var rows = EtabsTextTableParser.ParseDelimitedRows(content);
            Assert.Equal(2, rows.Count);
            // Delimiter fallback handles mixed separators so row 1 is properly split into 4 columns
            Assert.Equal(4, rows[1].Length);
        }

        [Fact]
        public void EtabsFrameChainBuilder_OppositeVectorDirections_ExposesChainingFailure()
        {
            // Two beams along X-axis that meet at X=6000, but drawn in opposite directions:
            // Frame 1: 0 -> 6000 (u1 = (1, 0, 0))
            // Frame 2: 12000 -> 6000 (u2 = (-1, 0, 0)) - reversed drawing direction
            var f1 = new EtabsFrameGeometry
            {
                FrameName = "B1", Story = "Story 1",
                StartX = 0, StartY = 0, StartZ = 3000,
                EndX = 6000, EndY = 0, EndZ = 3000
            };
            var f2 = new EtabsFrameGeometry
            {
                FrameName = "B2", Story = "Story 1",
                StartX = 12000, StartY = 0, StartZ = 3000,
                EndX = 6000, EndY = 0, EndZ = 3000
            };

            var frames = new List<EtabsFrameGeometry> { f1, f2 };
            var chains = EtabsFrameChainBuilder.BuildChains(frames);

            // Successfully chains frames with opposite vector directions into a single run
            Assert.Single(chains);
            Assert.Equal(2, chains[0].Frames.Count);
        }

        [Fact]
        public void EtabsFrameChainBuilder_InteriorSeedSelection_ExposesForwardOnlyFailure()
        {
            // 3 continuous spans along X: B1 (0..6000), B2 (6000..12000), B3 (12000..18000).
            // Input list has interior beam B2 first as the seed!
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
            var f3 = new EtabsFrameGeometry
            {
                FrameName = "B3", Story = "Story 1",
                StartX = 12000, StartY = 0, StartZ = 3000,
                EndX = 18000, EndY = 0, EndZ = 3000
            };

            // Order: B2 first!
            var frames = new List<EtabsFrameGeometry> { f2, f1, f3 };
            var chains = EtabsFrameChainBuilder.BuildChains(frames);

            // Bidirectional expansion attaches both B1 (backward) and B3 (forward) to B2
            Assert.Single(chains);
            Assert.Equal(3, chains[0].Frames.Count);
        }

        [Fact]
        public void EtabsFrameChainBuilder_NonCollinearBranch_DoesNotCorruptContinuousChain()
        {
            // B1 (0..6000), B2 (6000..12000), and a branch B_Branch (6000..6000 in X, 0..6000 in Y)
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
            var fBranch = new EtabsFrameGeometry
            {
                FrameName = "B_BRANCH", Story = "Story 1",
                StartX = 6000, StartY = 0, StartZ = 3000,
                EndX = 6000, EndY = 6000, EndZ = 3000
            };

            var frames = new List<EtabsFrameGeometry> { f1, fBranch, f2 };
            var chains = EtabsFrameChainBuilder.BuildChains(frames);

            // B1 and B2 form a collinear chain. B_Branch is perpendicular and forms its own run.
            Assert.Equal(2, chains.Count);
            var mainChain = chains.Find(c => c.RunName == "B1");
            Assert.NotNull(mainChain);
            Assert.Equal(2, mainChain.Frames.Count);
            Assert.Equal("B1", mainChain.Frames[0].FrameName);
            Assert.Equal("B2", mainChain.Frames[1].FrameName);
        }
    }
}
