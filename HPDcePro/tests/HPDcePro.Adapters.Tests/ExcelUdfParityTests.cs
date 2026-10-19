using HPDcePro.Excel;
using Xunit;

namespace HPDcePro.Adapters.Tests
{
    public sealed class ExcelUdfParityTests
    {
        [Fact]
        public void TinhCotNeUonXien_PMM_ValidAndEdgeCases()
        {
            double ratio = CalculationUdfBridge.TinhCotNeUonXien_PMM(
                300.0, 300.0, 800.0, 60.0, 45.0, 2000.0, 3300.0, "B25", "CB400-V", 40.0);
            Assert.True(ratio > 0.0, "Valid column PMM should return positive utilization ratio");

            double zeroB = CalculationUdfBridge.TinhCotNeUonXien_PMM(
                0.0, 300.0, 800.0, 60.0, 45.0, 2000.0);
            Assert.Equal(0.0, zeroB);

            double zeroForce = CalculationUdfBridge.TinhCotNeUonXien_PMM(
                300.0, 300.0, 0.0, 0.0, 0.0, 2000.0);
            Assert.Equal(0.0, zeroForce);
        }

        [Fact]
        public void TinhVachCung_ValidAndEdgeCases()
        {
            double ratio = CalculationUdfBridge.TinhVachCung(
                2000.0, 250.0, 3300.0, 1500.0, 500.0, 1000.0, "B25", "CB400-V");
            Assert.True(ratio > 0.0, "Valid shear wall should return positive shear ratio");

            double zeroL = CalculationUdfBridge.TinhVachCung(
                0.0, 250.0, 3300.0, 1500.0, 500.0, 1000.0);
            Assert.Equal(0.0, zeroL);
        }

        [Fact]
        public void TinhSanHaiPhuong_ValidAndEdgeCases()
        {
            double asBotX = CalculationUdfBridge.TinhSanHaiPhuong(
                4000.0, 5000.0, 120.0, 4.0, 2.5, "B25", "CB400-V", 1, 0);
            Assert.True(asBotX > 0.0, "Two way slab should compute positive bottom X rebar");

            double momentX = CalculationUdfBridge.TinhSanHaiPhuong(
                4000.0, 5000.0, 120.0, 4.0, 2.5, "B25", "CB400-V", 1, 4);
            Assert.True(momentX > 0.0, "Span positive moment should be positive");

            double invalidLx = CalculationUdfBridge.TinhSanHaiPhuong(
                -100.0, 5000.0, 120.0, 4.0, 2.5);
            Assert.Equal(0.0, invalidLx);
        }

        [Fact]
        public void TinhDaiCoc_ValidAndEdgeCases()
        {
            double capRatio = CalculationUdfBridge.TinhDaiCoc(
                2200.0, 2200.0, 900.0, 400.0, 400.0, 2000.0, 120.0, 80.0, 800.0, 4, 900.0, "B25", "CB400-V", 0);
            Assert.True(capRatio > 0.0, "Pile cap should compute positive pile capacity ratio");

            double maxReaction = CalculationUdfBridge.TinhDaiCoc(
                2200.0, 2200.0, 900.0, 400.0, 400.0, 2000.0, 120.0, 80.0, 800.0, 4, 900.0, "B25", "CB400-V", 4);
            Assert.True(maxReaction > 0.0, "Maximum pile reaction should be positive");

            double zeroH = CalculationUdfBridge.TinhDaiCoc(
                2200.0, 2200.0, 0.0, 400.0, 400.0, 2000.0, 120.0, 80.0, 800.0);
            Assert.Equal(0.0, zeroH);
        }

        [Fact]
        public void TinhMongDon_SoilCapacityAndUplift()
        {
            double soilRatio = CalculationUdfBridge.TinhMongDon(
                1800.0, 2000.0, 600.0, 300.0, 300.0, 600.0, 60.0, 40.0, 250.0, 1.5, "B25", "CB400-V", 0);
            Assert.True(soilRatio > 0.0, "Pad footing should compute soil capacity ratio");

            double pMax = CalculationUdfBridge.TinhMongDon(
                1800.0, 2000.0, 600.0, 300.0, 300.0, 600.0, 60.0, 40.0, 250.0, 1.5, "B25", "CB400-V", 1);
            Assert.True(pMax > 0.0, "Maximum base pressure should be positive");

            // Large moment with small axial load produces uplift
            double upliftFlag = CalculationUdfBridge.TinhMongDon(
                1500.0, 1500.0, 500.0, 300.0, 300.0, 100.0, 250.0, 0.0, 200.0, 1.5, "B25", "CB400-V", 7);
            Assert.Equal(1.0, upliftFlag);

            double zeroB = CalculationUdfBridge.TinhMongDon(
                0.0, 2000.0, 600.0, 300.0, 300.0, 600.0, 60.0, 40.0);
            Assert.Equal(0.0, zeroB);
        }

        [Fact]
        public void SucChiuTaiCoc_MaSat_DrivenAndBoredPiles()
        {
            double drivenCap = CalculationUdfBridge.SucChiuTaiCoc_MaSat(0.4, 20.0, 15.0, isDriven: true, isCohesive: true);
            Assert.True(drivenCap > 0.0, "Driven pile friction capacity should be positive");

            double boredCap = CalculationUdfBridge.SucChiuTaiCoc_MaSat(0.8, 25.0, 20.0, isDriven: false, isCohesive: false);
            Assert.True(boredCap > 0.0, "Bored pile friction capacity should be positive");

            double zeroN = CalculationUdfBridge.SucChiuTaiCoc_MaSat(0.4, 20.0, 0.0);
            Assert.Equal(0.0, zeroN);
        }

        [Fact]
        public void BeRongVetNutDam_ValidAndEdgeCases()
        {
            double crackWidth = CalculationUdfBridge.BeRongVetNutDam(
                220.0, 400.0, 50.0, 1200.0, 20.0, "B25", "CB400-V", 40.0, isLongTerm: true);
            Assert.True(crackWidth > 0.0, "Crack width should be positive for flexural beam");

            double zeroMoment = CalculationUdfBridge.BeRongVetNutDam(
                220.0, 400.0, 0.0, 1200.0);
            Assert.Equal(0.0, zeroMoment);

            double invalidCover = CalculationUdfBridge.BeRongVetNutDam(
                220.0, 400.0, 50.0, 1200.0, 20.0, "B25", "CB400-V", a: 450.0);
            Assert.Equal(0.0, invalidCover);
        }

        [Fact]
        public void TinhDamThep_TCVN5575_ValidAndEdgeCases()
        {
            double ratio = CalculationUdfBridge.TinhDamThep_TCVN5575(
                400.0, 200.0, 8.0, 12.0, 150.0, unbracedLengthL0M: 5.0, shearVKn: 80.0, yieldStrengthFyMpa: 235.0);
            Assert.True(ratio > 0.0, "Steel beam should compute positive flexure/shear ratio");

            double zeroForces = CalculationUdfBridge.TinhDamThep_TCVN5575(
                400.0, 200.0, 8.0, 12.0, 0.0, unbracedLengthL0M: 5.0, shearVKn: 0.0);
            Assert.Equal(0.0, zeroForces);

            double invalidGeometry = CalculationUdfBridge.TinhDamThep_TCVN5575(
                20.0, 200.0, 8.0, 12.0, 100.0);
            Assert.Equal(0.0, invalidGeometry);
        }

        [Fact]
        public void TinhCotThep_TCVN5575_ValidAndEdgeCases()
        {
            double ratio = CalculationUdfBridge.TinhCotThep_TCVN5575(
                350.0, 250.0, 10.0, 16.0, 600.0, 80.0, 4.0, 4.0, 235.0);
            Assert.True(ratio > 0.0, "Steel column should compute positive stability ratio");

            double zeroLoad = CalculationUdfBridge.TinhCotThep_TCVN5575(
                350.0, 250.0, 10.0, 16.0, 0.0, 0.0, 4.0, 4.0);
            Assert.Equal(0.0, zeroLoad);
        }

        [Fact]
        public void TinhLienKetMoment_TCVN5575_ValidAndEdgeCases()
        {
            double ratio = CalculationUdfBridge.TinhLienKetMoment_TCVN5575(
                180.0, 100.0, 450.0, 200.0, 14.0, 8.0, 25.0, 24.0, 2, "8.8", 345.0);
            Assert.True(ratio > 0.0, "Steel moment connection should compute positive governing ratio");

            double zeroForces = CalculationUdfBridge.TinhLienKetMoment_TCVN5575(
                0.0, 0.0, 450.0, 200.0, 14.0, 8.0, 25.0);
            Assert.Equal(0.0, zeroForces);
        }

        [Fact]
        public void TinhBuLongNeo_TCVN5575_BearingAndAnchorTension()
        {
            double governing = CalculationUdfBridge.TinhBuLongNeo_TCVN5575(
                300.0, 120.0, 50.0, 600.0, 400.0, 25.0, 4, 24.0, "B25", 300.0, 0);
            Assert.True(governing > 0.0, "Anchor bolt base plate should compute governing ratio");

            double anchorTension = CalculationUdfBridge.TinhBuLongNeo_TCVN5575(
                100.0, 150.0, 30.0, 600.0, 400.0, 25.0, 4, 24.0, "B25", 300.0, 6);
            Assert.True(anchorTension >= 0.0, "Anchor bolt tension force should be non-negative");

            double zeroPlate = CalculationUdfBridge.TinhBuLongNeo_TCVN5575(
                300.0, 120.0, 50.0, 0.0, 400.0, 25.0);
            Assert.Equal(0.0, zeroPlate);
        }

        [Fact]
        public void TinhTaiTrongGio_TCVN2737_AerodynamicPressure()
        {
            double pZone2B = CalculationUdfBridge.TinhTaiTrongGio_TCVN2737("II", "B", 10.0, 0.8, 0.0, isDesign: true);
            Assert.True(pZone2B > 0.0, "Design wind pressure for Zone II Terrain B should be positive");

            double pZone4C = CalculationUdfBridge.TinhTaiTrongGio_TCVN2737("IV", "C", 20.0, 0.8, 0.0, isDesign: true);
            Assert.True(pZone4C > pZone2B, "Zone IV high elevation should have higher pressure than Zone II");

            double pChar = CalculationUdfBridge.TinhTaiTrongGio_TCVN2737("II", "B", 10.0, 0.8, 0.0, isDesign: false);
            Assert.True(pChar < pZone2B, "Characteristic pressure should be lower than design pressure");
        }
    }
}
