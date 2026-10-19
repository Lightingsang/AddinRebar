using System;
using HPDcePro.Excel;
using Xunit;

namespace HPDcePro.Adapters.Tests
{
    public sealed class ChallengerUdfStressTests
    {
        [Fact]
        public void Udf_SingularAndNegativeGeometries_ReturnsZero_NeverThrows()
        {
            Assert.Equal(0.0, CalculationUdfBridge.TinhThepDam_M(-200.0, 400.0, 50.0));
            Assert.Equal(0.0, CalculationUdfBridge.TinhThepDam_V(200.0, -400.0, 50.0));
            Assert.Equal(0.0, CalculationUdfBridge.SucChiuTaiCoc_SPT(0.0, 20.0, 15.0, 30.0));
            Assert.Equal(0.0, CalculationUdfBridge.TrongLuongThep(-10, 10.0));
            Assert.Equal(0.0, CalculationUdfBridge.DienTichThep(4, -18));
            Assert.Equal(0.0, CalculationUdfBridge.HamLuongThep(1000.0, -200.0, 400.0));
            Assert.Equal(0, CalculationUdfBridge.KiemTraUon(-200.0, 400.0, 50.0, 1000.0));
            Assert.Equal(0.0, CalculationUdfBridge.TinhCotNeUonXien_PMM(-300.0, 300.0, 500.0, 40.0, 30.0, 1500.0));
            Assert.Equal(0.0, CalculationUdfBridge.TinhVachCung(-2000.0, 250.0, 3000.0, 1000.0, 300.0, 500.0));
            Assert.Equal(0.0, CalculationUdfBridge.TinhSanHaiPhuong(0.0, 5000.0, 120.0, 3.0, 2.0));
            Assert.Equal(0.0, CalculationUdfBridge.TinhDaiCoc(0.0, 2000.0, 900.0, 400.0, 400.0, 1500.0, 50.0, 50.0, 600.0));
            Assert.Equal(0.0, CalculationUdfBridge.TinhMongDon(1500.0, 0.0, 500.0, 300.0, 300.0, 500.0, 50.0, 30.0));
            Assert.Equal(0.0, CalculationUdfBridge.SucChiuTaiCoc_MaSat(-0.4, 15.0, 20.0));
            Assert.Equal(0.0, CalculationUdfBridge.BeRongVetNutDam(0.0, 400.0, 50.0, 1200.0));
            Assert.Equal(0.0, CalculationUdfBridge.TinhDamThep_TCVN5575(-400.0, 200.0, 8.0, 12.0, 100.0));
            Assert.Equal(0.0, CalculationUdfBridge.TinhCotThep_TCVN5575(350.0, -250.0, 10.0, 16.0, 500.0, 50.0, 3.0, 3.0));
            Assert.Equal(0.0, CalculationUdfBridge.TinhLienKetMoment_TCVN5575(150.0, 80.0, 0.0, 200.0, 14.0, 8.0, 20.0));
            Assert.Equal(0.0, CalculationUdfBridge.TinhBuLongNeo_TCVN5575(300.0, 80.0, 40.0, -600.0, 400.0, 25.0));
        }

        [Fact]
        public void Udf_LegacyBeamUdf_InvalidCoversAndStirrups_Behavior()
        {
            Assert.Equal(0.0, CalculationUdfBridge.TinhThepDam_M(200.0, 400.0, 50.0, a: 450.0));
            Assert.Equal(0.0, CalculationUdfBridge.TinhThepDam_M(200.0, 70.0, 50.0, a: 40.0));
            Assert.Equal(0.0, CalculationUdfBridge.TinhThepDam_V(200.0, 400.0, 50.0, phiDai: 0.0));
            Assert.Equal(0.0, CalculationUdfBridge.TinhThepDam_V(200.0, 400.0, 50.0, soNhanh: 0));
        }

        [Fact]
        public void Udf_EnormousForces_10Power7_ComputesOrBounds_NeverThrows()
        {
            double massive = 1e7;
            double pmm = CalculationUdfBridge.TinhCotNeUonXien_PMM(400.0, 400.0, massive, massive, massive, 2500.0);
            Assert.True(pmm > 1.0, "Utilization ratio for 10^7 kN must exceed 1.0");

            double wall = CalculationUdfBridge.TinhVachCung(3000.0, 300.0, 3300.0, massive, massive, massive);
            Assert.True(wall > 1.0);

            double capRatio = CalculationUdfBridge.TinhDaiCoc(2500.0, 2500.0, 1000.0, 500.0, 500.0, massive, massive, massive, 600.0);
            Assert.True(capRatio > 1.0);

            double footingRatio = CalculationUdfBridge.TinhMongDon(2000.0, 2000.0, 600.0, 400.0, 400.0, massive, massive, massive, 200.0);
            Assert.True(footingRatio > 1.0);

            double steelBeam = CalculationUdfBridge.TinhDamThep_TCVN5575(500.0, 200.0, 10.0, 16.0, massive, unbracedLengthL0M: 6.0, shearVKn: massive);
            Assert.True(steelBeam > 1.0);

            double steelCol = CalculationUdfBridge.TinhCotThep_TCVN5575(400.0, 300.0, 12.0, 20.0, massive, massive, 4.0, 4.0);
            Assert.True(steelCol > 1.0);

            double momentConn = CalculationUdfBridge.TinhLienKetMoment_TCVN5575(massive, massive, 500.0, 200.0, 16.0, 10.0, 25.0);
            Assert.True(momentConn > 1.0);

            double basePlate = CalculationUdfBridge.TinhBuLongNeo_TCVN5575(massive, massive, massive);
            Assert.True(basePlate > 1.0);
        }

        [Fact]
        public void Udf_PadFooting_UpliftAndOverturningSingularities_HandlesSafely()
        {
            double upliftUdf = CalculationUdfBridge.TinhMongDon(
                1500.0, 1500.0, 500.0, 300.0, 300.0, 10.0, 500.0, 0.0, 200.0, 1.5, returnMode: 7);
            Assert.Equal(1.0, upliftUdf);

            double pMin = CalculationUdfBridge.TinhMongDon(
                1500.0, 1500.0, 500.0, 300.0, 300.0, 10.0, 500.0, 0.0, 200.0, 1.5, returnMode: 2);
            Assert.Equal(0.0, pMin);

            double pureTension = CalculationUdfBridge.TinhMongDon(
                1500.0, 1500.0, 500.0, 300.0, 300.0, -1000.0, 50.0, 0.0, 200.0, 1.5, returnMode: 0);
            Assert.False(double.IsNaN(pureTension));
            Assert.False(double.IsInfinity(pureTension));

            double zeroRtc = CalculationUdfBridge.TinhMongDon(
                1500.0, 1500.0, 500.0, 300.0, 300.0, 500.0, 30.0, 20.0, allowableSoilRtcKpa: 0.0);
            Assert.True(zeroRtc > 0.0);
        }

        [Fact]
        public void Udf_PileCap_UpliftTension_ZeroPiles_HandlesSafely()
        {
            double tensionCap = CalculationUdfBridge.TinhDaiCoc(
                2200.0, 2200.0, 900.0, 400.0, 400.0, -5000.0, 200.0, 100.0, 800.0, pileCount: 4);
            Assert.False(double.IsNaN(tensionCap));
            Assert.False(double.IsInfinity(tensionCap));

            double minReaction = CalculationUdfBridge.TinhDaiCoc(
                2200.0, 2200.0, 900.0, 400.0, 400.0, -5000.0, 200.0, 100.0, 800.0, pileCount: 4, returnMode: 5);
            Assert.True(minReaction < 0.0, "Tension pile cap should result in negative reaction (tension)");

            double zeroCap = CalculationUdfBridge.TinhDaiCoc(
                2200.0, 2200.0, 900.0, 400.0, 400.0, 1000.0, 50.0, 50.0, pileCapacityKn: 0.0);
            Assert.Equal(0.0, zeroCap);
        }

        [Fact]
        public void Udf_AnchorBoltBasePlate_MassiveUpliftAndShear_HandlesSafely()
        {
            double pureTensionUplift = CalculationUdfBridge.TinhBuLongNeo_TCVN5575(
                -2000.0, 300.0, 150.0, 600.0, 400.0, 30.0, 4, 24.0, "B25", 300.0, returnMode: 0);
            Assert.True(pureTensionUplift > 0.0);
            Assert.False(double.IsNaN(pureTensionUplift));

            for (int mode = 0; mode <= 7; mode++)
            {
                double resMode = CalculationUdfBridge.TinhBuLongNeo_TCVN5575(
                    -1000.0, 250.0, 100.0, 600.0, 400.0, 30.0, 4, 24.0, "B25", 300.0, returnMode: mode);
                Assert.False(double.IsNaN(resMode));
                Assert.False(double.IsInfinity(resMode));
            }
        }

        [Fact]
        public void Udf_WindLoad_BoundaryElevationsAndUnknownTokens_HandlesSafely()
        {
            double negZ = CalculationUdfBridge.TinhTaiTrongGio_TCVN2737("II", "B", -15.0);
            Assert.True(negZ > 0.0);

            double extremeZ = CalculationUdfBridge.TinhTaiTrongGio_TCVN2737("II", "B", 100000.0);
            Assert.True(extremeZ > 0.0);

            double unknownTokens = CalculationUdfBridge.TinhTaiTrongGio_TCVN2737("UNKNOWN_ZONE", "UNKNOWN_TERRAIN", 12.0);
            Assert.True(unknownTokens > 0.0);

            double zeroGust = CalculationUdfBridge.TinhTaiTrongGio_TCVN2737("II", "B", 10.0, gustFactorG: 0.0);
            Assert.True(zeroGust > 0.0);
        }

        [Fact]
        public void Udf_CrackWidth_BoundaryCoversAndZeroMoments_HandlesSafely()
        {
            Assert.Equal(0.0, CalculationUdfBridge.BeRongVetNutDam(200.0, 400.0, 0.0, 1200.0));
            Assert.Equal(0.0, CalculationUdfBridge.BeRongVetNutDam(200.0, 400.0, 50.0, 0.0));
            Assert.Equal(0.0, CalculationUdfBridge.BeRongVetNutDam(200.0, 400.0, 50.0, 1200.0, a: 0.0));
            Assert.Equal(0.0, CalculationUdfBridge.BeRongVetNutDam(200.0, 400.0, 50.0, 1200.0, a: 450.0));

            double hugeMomentCrack = CalculationUdfBridge.BeRongVetNutDam(250.0, 500.0, 1e5, 2000.0);
            Assert.True(hugeMomentCrack > 0.0);
            Assert.False(double.IsNaN(hugeMomentCrack));
        }

        [Fact]
        public void Udf_SoilFriction_ExtremeN60AndZeroLength_HandlesSafely()
        {
            Assert.Equal(0.0, CalculationUdfBridge.SucChiuTaiCoc_MaSat(0.4, 0.0, 20.0));
            Assert.Equal(0.0, CalculationUdfBridge.SucChiuTaiCoc_MaSat(0.0, 20.0, 20.0));
            Assert.Equal(0.0, CalculationUdfBridge.SucChiuTaiCoc_MaSat(0.4, 20.0, 0.0));
            Assert.Equal(0.0, CalculationUdfBridge.SucChiuTaiCoc_MaSat(0.4, 20.0, -10.0));

            double clampedFriction = CalculationUdfBridge.SucChiuTaiCoc_MaSat(0.4, 20.0, 1e6);
            Assert.True(clampedFriction > 0.0);
            Assert.False(double.IsInfinity(clampedFriction));
        }

        [Fact]
        public void Udf_TwoWaySlab_InvalidBoundariesAndReturnModes_HandlesSafely()
        {
            double badBoundary = CalculationUdfBridge.TinhSanHaiPhuong(
                4000.0, 5000.0, 120.0, 4.0, 2.5, "B25", "CB400-V", boundaryCondition: 99, returnMode: 0);
            Assert.True(badBoundary > 0.0);

            double badReturnMode = CalculationUdfBridge.TinhSanHaiPhuong(
                4000.0, 5000.0, 120.0, 4.0, 2.5, "B25", "CB400-V", boundaryCondition: 1, returnMode: -10);
            Assert.True(badReturnMode > 0.0);
        }
    }
}
