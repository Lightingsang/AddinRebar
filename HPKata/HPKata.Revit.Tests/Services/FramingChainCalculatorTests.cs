using System.Collections.Generic;
using HPKata.Revit.Abstractions;
using HPKata.Revit.Services;
using HPKata.Revit.Units;
using Xunit;

namespace HPKata.Revit.Tests.Services
{
    public sealed class FramingChainCalculatorTests
    {
        [Fact]
        public void ChainCollinearSegments_InteriorSeed_ExpandsBothDirections()
        {
            var seg1 = new FramingSegment
            {
                Id = "B1",
                StartPt = new RevitPoint3D(RevitUnitConverter.MmToFeet(0.0), 0.0, 0.0),
                EndPt = new RevitPoint3D(RevitUnitConverter.MmToFeet(6000.0), 0.0, 0.0)
            };
            var seg2 = new FramingSegment
            {
                Id = "B2",
                StartPt = new RevitPoint3D(RevitUnitConverter.MmToFeet(6000.0), 0.0, 0.0),
                EndPt = new RevitPoint3D(RevitUnitConverter.MmToFeet(12000.0), 0.0, 0.0)
            };
            var seg3 = new FramingSegment
            {
                Id = "B3",
                StartPt = new RevitPoint3D(RevitUnitConverter.MmToFeet(12000.0), 0.0, 0.0),
                EndPt = new RevitPoint3D(RevitUnitConverter.MmToFeet(18000.0), 0.0, 0.0)
            };

            var candidates = new List<FramingSegment> { seg1, seg2, seg3 };
            var chain = FramingChainCalculator.ChainCollinearSegments(
                seg2, candidates, angleToleranceDeg: 1.0, touchToleranceFt: RevitUnitConverter.MmToFeet(50.0));

            Assert.Equal(3, chain.Count);
            Assert.Equal("B1", chain[0].Id);
            Assert.Equal("B2", chain[1].Id);
            Assert.Equal("B3", chain[2].Id);
        }

        [Fact]
        public void ChainCollinearSegments_ReversedCurveDirection_BridgesAccurately()
        {
            var seg1Reversed = new FramingSegment
            {
                Id = "B1_REV",
                StartPt = new RevitPoint3D(RevitUnitConverter.MmToFeet(6000.0), 0.0, 0.0),
                EndPt = new RevitPoint3D(RevitUnitConverter.MmToFeet(0.0), 0.0, 0.0)
            };
            var seg2Seed = new FramingSegment
            {
                Id = "B2_SEED",
                StartPt = new RevitPoint3D(RevitUnitConverter.MmToFeet(6000.0), 0.0, 0.0),
                EndPt = new RevitPoint3D(RevitUnitConverter.MmToFeet(12000.0), 0.0, 0.0)
            };
            var seg3Reversed = new FramingSegment
            {
                Id = "B3_REV",
                StartPt = new RevitPoint3D(RevitUnitConverter.MmToFeet(18000.0), 0.0, 0.0),
                EndPt = new RevitPoint3D(RevitUnitConverter.MmToFeet(12000.0), 0.0, 0.0)
            };

            var candidates = new List<FramingSegment> { seg1Reversed, seg2Seed, seg3Reversed };
            var chain = FramingChainCalculator.ChainCollinearSegments(
                seg2Seed, candidates, angleToleranceDeg: 1.0, touchToleranceFt: RevitUnitConverter.MmToFeet(50.0));

            Assert.Equal(3, chain.Count);
            Assert.Contains(chain, s => s.Id == "B1_REV");
            Assert.Contains(chain, s => s.Id == "B2_SEED");
            Assert.Contains(chain, s => s.Id == "B3_REV");
        }

        [Fact]
        public void ChainCollinearSegments_NonCollinearAndGapped_ExcludesOutliers()
        {
            var seg1 = new FramingSegment
            {
                Id = "B1",
                StartPt = new RevitPoint3D(RevitUnitConverter.MmToFeet(0.0), 0.0, 0.0),
                EndPt = new RevitPoint3D(RevitUnitConverter.MmToFeet(6000.0), 0.0, 0.0)
            };
            var segOrthogonal = new FramingSegment
            {
                Id = "B_ORTHO",
                StartPt = new RevitPoint3D(RevitUnitConverter.MmToFeet(6000.0), 0.0, 0.0),
                EndPt = new RevitPoint3D(RevitUnitConverter.MmToFeet(6000.0), RevitUnitConverter.MmToFeet(6000.0), 0.0)
            };
            var segGapped = new FramingSegment
            {
                Id = "B_GAP",
                StartPt = new RevitPoint3D(RevitUnitConverter.MmToFeet(6500.0), 0.0, 0.0),
                EndPt = new RevitPoint3D(RevitUnitConverter.MmToFeet(12000.0), 0.0, 0.0)
            };

            var candidates = new List<FramingSegment> { seg1, segOrthogonal, segGapped };
            var chain = FramingChainCalculator.ChainCollinearSegments(
                seg1, candidates, angleToleranceDeg: 1.0, touchToleranceFt: RevitUnitConverter.MmToFeet(50.0));

            Assert.Single(chain);
            Assert.Equal("B1", chain[0].Id);
        }
    }
}
