using System;
using System.Collections.Generic;
using HPDcePro.Core.Etabs;
using HPDcePro.Core.Geometry;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public sealed class EtabsSyncTests
    {
        private readonly EtabsModelSynchronizer _sync = new EtabsModelSynchronizer();

        [Fact]
        public void EtabsModelSynchronizer_Synchronize_MatchesFramesWithinTolerance()
        {
            var model = new EtabsExchangeModel();
            model.Frames.Add(new EtabsFrameMember
            {
                UniqueName = "B101",
                SectionName = "300x600",
                Point1 = new Point3D(0, 0, 3600),
                Point2 = new Point3D(6000, 0, 3600),
                LengthMm = 6000
            });

            var cadBeams = new List<(string CadMark, Point3D P1, Point3D P2)>
            {
                ("D1", new Point3D(50, 0, 3600), new Point3D(6050, 0, 3600))
            };

            var report = _sync.Synchronize(model, cadBeams, toleranceMm: 200.0);

            Assert.Equal(1, report.MatchedCount);
            Assert.Equal(0, report.OrphanCount);
            Assert.Equal(EtabsSyncItemStatus.Matched, report.Items[0].Status);
            Assert.Equal("B101", report.Items[0].EtabsUniqueName);
        }

        [Fact]
        public void EtabsModelSynchronizer_Synchronize_IdentifiesOrphanedCadBeams()
        {
            var model = new EtabsExchangeModel();
            model.Frames.Add(new EtabsFrameMember
            {
                UniqueName = "B102",
                Point1 = new Point3D(0, 0, 3600),
                Point2 = new Point3D(6000, 0, 3600)
            });

            var cadBeams = new List<(string CadMark, Point3D P1, Point3D P2)>
            {
                ("D_Orphan", new Point3D(10000, 0, 3600), new Point3D(16000, 0, 3600))
            };

            var report = _sync.Synchronize(model, cadBeams, toleranceMm: 200.0);

            Assert.Equal(0, report.MatchedCount);
            Assert.True(report.OrphanCount >= 1);
            Assert.Contains(report.Items, i => i.Status == EtabsSyncItemStatus.OrphanedInCad);
            Assert.NotEmpty(report.Warnings);
        }

        [Fact]
        public void EtabsModelSynchronizer_Synchronize_IdentifiesOrphanedEtabsFrames()
        {
            var model = new EtabsExchangeModel();
            model.Frames.Add(new EtabsFrameMember
            {
                UniqueName = "B_ETABS_ORPHAN",
                Point1 = new Point3D(0, 5000, 3600),
                Point2 = new Point3D(6000, 5000, 3600)
            });

            var cadBeams = new List<(string CadMark, Point3D P1, Point3D P2)>();

            var report = _sync.Synchronize(model, cadBeams, toleranceMm: 200.0);

            Assert.Equal(1, report.OrphanCount);
            Assert.Contains(report.Items, i => i.Status == EtabsSyncItemStatus.OrphanedInEtabs);
        }

        [Fact]
        public void EtabsModelSynchronizer_ExtractEnvelopeMoments_ExtractsMaxPosAndNegMoments()
        {
            var model = new EtabsExchangeModel();
            model.Forces.Add(new EtabsMemberForce { UniqueName = "B1", StationMm = 0, M3_Knm = -120.0, V2_Kn = 110.0 });
            model.Forces.Add(new EtabsMemberForce { UniqueName = "B1", StationMm = 3000, M3_Knm = 95.0, V2_Kn = 10.0 });
            model.Forces.Add(new EtabsMemberForce { UniqueName = "B1", StationMm = 6000, M3_Knm = -115.0, V2_Kn = -105.0 });

            var (maxPos, maxNeg, maxV) = _sync.ExtractEnvelopeMoments(model, "B1");

            Assert.Equal(95.0, maxPos);
            Assert.Equal(120.0, maxNeg);
            Assert.Equal(110.0, maxV);
        }

        [Fact]
        public void EtabsModelSynchronizer_ConvertRequiredAs_FindsOptimalBarArrangement()
        {
            double requiredAs = 1200.0; // mm2

            var arrangement = _sync.ConvertRequiredAsToBarArrangement(requiredAs, minBars: 2, maxBars: 6);

            Assert.NotNull(arrangement);
            Assert.True(arrangement.TotalAsMm2 >= requiredAs);
            Assert.True(arrangement.BarCount >= 2 && arrangement.BarCount <= 6);
            Assert.Contains("D", arrangement.Designation);
        }

        [Fact]
        public void EtabsModelSynchronizer_ConvertRequiredAs_ZeroOrNegative_ReturnsMinimumTwoBars()
        {
            var arrangement = _sync.ConvertRequiredAsToBarArrangement(0.0);

            Assert.Equal(2, arrangement.BarCount);
            Assert.Equal(16.0, arrangement.DiameterMm);
            Assert.Equal("2D16", arrangement.Designation);
        }

        [Fact]
        public void EtabsExchangeModel_Collections_StoreStoriesAndGrids()
        {
            var model = new EtabsExchangeModel { ProjectName = "TestTower" };
            model.Stories.Add(new EtabsStory { Name = "Story1", ElevationMm = 3600.0, HeightMm = 3600.0 });
            model.Grids.Add(new EtabsGridLine { AxisId = "A", CoordinateMm = 0.0, IsXDirection = true });
            model.Grids.Add(new EtabsGridLine { AxisId = "1", CoordinateMm = 0.0, IsXDirection = false });

            Assert.Single(model.Stories);
            Assert.Equal(2, model.Grids.Count);
            Assert.Equal("TestTower", model.ProjectName);
        }

        [Fact]
        public void EtabsModelSynchronizer_NullInput_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _sync.Synchronize(null!, new List<(string, Point3D, Point3D)>()));
            Assert.Throws<ArgumentNullException>(() => _sync.Synchronize(new EtabsExchangeModel(), null!));
        }
    }
}
