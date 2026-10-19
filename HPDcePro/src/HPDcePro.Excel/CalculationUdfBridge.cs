using System;
using System.Collections.Generic;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Pile;
using HPDcePro.Core.Rebar;

namespace HPDcePro.Excel
{
    /// <summary>
    /// Headless C# compatibility bridge for legacy Excel DCE XLL / UDF calculation functions.
    /// Executes genuine TCVN 5574:2018 and TCVN 10304:2025 calculation algorithms with zero COM reliance.
    /// </summary>
    public static class CalculationUdfBridge
    {
        public static double TinhThepDam_M(
            double b, double h, double momentKnm, string macBt = "B25", string macThep = "CB400-V", double a = 40.0)
        {
            if (b <= 0 || h <= 0 || a <= 0 || h <= 2.0 * a) return 0.0;
            var concrete = ResolveConcrete(macBt);
            var rebar = ResolveRebar(macThep);

            var input = new BeamFlexureInput(b, h, momentKnm, concrete, rebar)
            {
                CoverTensionMm = a,
                CoverCompressionMm = a
            };

            var res = BeamFlexureCalculator.Calculate(input);
            return Math.Round(res.AsRequiredMm2, 1);
        }

        public static double TinhThepDam_V(
            double b, double h, double shearKn, string macBt = "B25", string macThep = "CB400-V", double phiDai = 8.0, int soNhanh = 2)
        {
            if (b <= 0 || h <= 0 || phiDai <= 0 || soNhanh < 1) return 0.0;
            var concrete = ResolveConcrete(macBt);
            var rebar = ResolveRebar(macThep);

            var input = new BeamShearInput(b, h, shearKn, concrete, rebar)
            {
                StirrupDiameterMm = phiDai,
                StirrupLegs = soNhanh,
                IsNearSupport = true
            };

            var res = BeamShearCalculator.Calculate(input);
            return Math.Round(res.RecommendedSpacingMm, 0);
        }

        public static double SucChiuTaiCoc_SPT(
            double diameterM, double lengthM, double n60Shaft, double n60Tip, bool isDriven = true)
        {
            if (diameterM <= 0 || lengthM <= 0) return 0.0;

            var pile = new PileGeometry
            {
                IsCircular = true,
                DiameterM = diameterM,
                TopDepthM = 0.0,
                TipDepthM = lengthM
            };

            var layers = new List<SoilLayer>
            {
                new("LopDatThan", SoilConsistency.Cohesive, 0.0, lengthM, n60Shaft)
            };

            var installType = isDriven ? PileInstallationType.DrivenPile : PileInstallationType.BoredPile;
            var res = PileCapacityCalculator.Calculate(
                pile, layers, installType, n60Tip, SoilConsistency.Cohesive);

            return Math.Round(res.DesignCapacityRcKn, 1);
        }

        public static double TrongLuongThep(int diameterMm, double lengthM)
        {
            if (diameterMm <= 0 || lengthM <= 0) return 0.0;
            double unitMass = RebarScheduleAggregator.GetUnitMass(diameterMm);
            return Math.Round(unitMass * lengthM, 2);
        }

        public static double DienTichThep(int count, int diameterMm)
        {
            if (count <= 0 || diameterMm <= 0) return 0.0;
            double a1 = Math.PI * diameterMm * diameterMm / 4.0;
            return Math.Round(count * a1, 1);
        }

        public static double HamLuongThep(double asMm2, double b, double h, double a = 40.0)
        {
            double h0 = h - a;
            if (b <= 0 || h0 <= 0 || asMm2 <= 0) return 0.0;
            return Math.Round((asMm2 / (b * h0)) * 100.0, 2);
        }

        public static int KiemTraUon(
            double b, double h, double momentKnm, double asMm2, string macBt = "B25", string macThep = "CB400-V", double a = 40.0)
        {
            if (b <= 0 || h <= 0 || a <= 0 || h <= 2.0 * a || asMm2 <= 0) return 0;
            double asReq = TinhThepDam_M(b, h, momentKnm, macBt, macThep, a);
            return asReq > 0 && asMm2 >= asReq ? 1 : 0;
        }

        public static double TinhCotNeUonXien_PMM(
            double b, double h, double axialNKn, double momentMxKnm, double momentMyKnm,
            double asTotalMm2, double effectiveLengthL0Mm = 3000.0,
            string macBt = "B25", string macThep = "CB400-V", double a = 40.0)
            => RcGeotechUdfBridge.TinhCotNeUonXien_PMM(
                b, h, axialNKn, momentMxKnm, momentMyKnm, asTotalMm2, effectiveLengthL0Mm, macBt, macThep, a);

        public static double TinhVachCung(
            double lengthLwMm, double thicknessTwMm, double heightHwMm,
            double axialNKn, double shearVuKn, double momentMuKnm,
            string macBt = "B25", string macThep = "CB400-V",
            int webBarDia = 12, double webBarSpacingMm = 200.0)
            => RcGeotechUdfBridge.TinhVachCung(
                lengthLwMm, thicknessTwMm, heightHwMm, axialNKn, shearVuKn, momentMuKnm, macBt, macThep, webBarDia, webBarSpacingMm);

        public static double TinhSanHaiPhuong(
            double lxMm, double lyMm, double thicknessHMm,
            double deadLoadGKnM2, double liveLoadPKnM2,
            string macBt = "B25", string macThep = "CB400-V",
            int boundaryCondition = 1, int returnMode = 0)
            => RcGeotechUdfBridge.TinhSanHaiPhuong(
                lxMm, lyMm, thicknessHMm, deadLoadGKnM2, liveLoadPKnM2, macBt, macThep, boundaryCondition, returnMode);

        public static double TinhDaiCoc(
            double widthBMm, double lengthLMm, double thicknessHMm,
            double colBMm, double colHMm, double axialNKn, double momentMxKnm, double momentMyKnm,
            double pileCapacityKn, int pileCount = 4, double pileSpacingMm = 900.0,
            string macBt = "B25", string macThep = "CB400-V", int returnMode = 0)
            => RcGeotechUdfBridge.TinhDaiCoc(
                widthBMm, lengthLMm, thicknessHMm, colBMm, colHMm, axialNKn, momentMxKnm, momentMyKnm,
                pileCapacityKn, pileCount, pileSpacingMm, macBt, macThep, returnMode);

        public static double TinhMongDon(
            double widthBMm, double lengthLMm, double thicknessHMm,
            double colBMm, double colHMm, double axialNKn, double momentMxKnm, double momentMyKnm,
            double allowableSoilRtcKpa = 250.0, double depthM = 1.5,
            string macBt = "B25", string macThep = "CB400-V", int returnMode = 0)
            => RcGeotechUdfBridge.TinhMongDon(
                widthBMm, lengthLMm, thicknessHMm, colBMm, colHMm, axialNKn, momentMxKnm, momentMyKnm,
                allowableSoilRtcKpa, depthM, macBt, macThep, returnMode);

        public static double SucChiuTaiCoc_MaSat(
            double diameterM, double lengthM, double n60Shaft,
            bool isDriven = true, bool isCohesive = true, bool isDesign = true)
            => RcGeotechUdfBridge.SucChiuTaiCoc_MaSat(
                diameterM, lengthM, n60Shaft, isDriven, isCohesive, isDesign);

        public static double BeRongVetNutDam(
            double b, double h, double serviceMomentKnm, double asTensionMm2,
            double phiBarMm = 20.0, string macBt = "B25", string macThep = "CB400-V",
            double a = 40.0, bool isLongTerm = true)
            => RcGeotechUdfBridge.BeRongVetNutDam(
                b, h, serviceMomentKnm, asTensionMm2, phiBarMm, macBt, macThep, a, isLongTerm);

        public static double TinhDamThep_TCVN5575(
            double hMm, double bMm, double twMm, double tfMm, double momentMxKnm,
            double unbracedLengthL0M = 6.0, double shearVKn = 0.0,
            double yieldStrengthFyMpa = 235.0, double gammaC = 1.0)
            => SteelWindUdfBridge.TinhDamThep_TCVN5575(
                hMm, bMm, twMm, tfMm, momentMxKnm, unbracedLengthL0M, shearVKn, yieldStrengthFyMpa, gammaC);

        public static double TinhCotThep_TCVN5575(
            double hMm, double bMm, double twMm, double tfMm,
            double axialNKn, double momentMxKnm,
            double effectiveLengthLxM, double effectiveLengthLyM,
            double yieldStrengthFyMpa = 235.0, double gammaC = 1.0)
            => SteelWindUdfBridge.TinhCotThep_TCVN5575(
                hMm, bMm, twMm, tfMm, axialNKn, momentMxKnm, effectiveLengthLxM, effectiveLengthLyM, yieldStrengthFyMpa, gammaC);

        public static double TinhLienKetMoment_TCVN5575(
            double momentKnm, double shearKn,
            double beamDepthMm, double beamFlangeWidthMm, double beamFlangeThicknessMm, double beamWebThicknessMm,
            double endPlateThicknessMm, double boltDiaMm = 24.0, int boltRows = 2,
            string boltGrade = "8.8", double fyMpa = 345.0)
            => SteelWindUdfBridge.TinhLienKetMoment_TCVN5575(
                momentKnm, shearKn, beamDepthMm, beamFlangeWidthMm, beamFlangeThicknessMm, beamWebThicknessMm,
                endPlateThicknessMm, boltDiaMm, boltRows, boltGrade, fyMpa);

        public static double TinhBuLongNeo_TCVN5575(
            double axialNKn, double momentKnm, double shearKn,
            double plateLengthMm = 600.0, double plateWidthMm = 400.0, double plateThicknessMm = 25.0,
            int boltCount = 4, double boltDiaMm = 24.0,
            string macBtPedestal = "B25", double anchorFyMpa = 300.0, int returnMode = 0)
            => SteelWindUdfBridge.TinhBuLongNeo_TCVN5575(
                axialNKn, momentKnm, shearKn, plateLengthMm, plateWidthMm, plateThicknessMm,
                boltCount, boltDiaMm, macBtPedestal, anchorFyMpa, returnMode);

        public static double TinhTaiTrongGio_TCVN2737(
            string vungGio, string diaHinh, double caoDoZeM,
            double cpe = 0.8, double cpi = 0.0,
            bool isDesign = true, double gustFactorG = 1.0)
            => SteelWindUdfBridge.TinhTaiTrongGio_TCVN2737(
                vungGio, diaHinh, caoDoZeM, cpe, cpi, isDesign, gustFactorG);

        private static ConcreteProperties ResolveConcrete(string name)
        {
            if (MaterialLibrary.TryGetConcrete(name, out var props) && props != null)
            {
                return props;
            }
            return MaterialLibrary.GetConcrete(ConcreteGrade.B25);
        }

        private static RebarProperties ResolveRebar(string name)
        {
            if (MaterialLibrary.TryGetRebar(name, out var props) && props != null)
            {
                return props;
            }
            return MaterialLibrary.GetRebar(RebarGrade.CB400V);
        }
    }
}
