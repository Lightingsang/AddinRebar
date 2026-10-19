using System;
using System.Collections.Generic;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Column;
using HPDcePro.Core.Calculations.Foundation;
using HPDcePro.Core.Calculations.Pile;
using HPDcePro.Core.Calculations.Slab;
using HPDcePro.Core.Calculations.Wall;

namespace HPDcePro.Excel
{
    /// <summary>
    /// Headless C# compatibility bridge for Reinforced Concrete & Geotechnical Excel DCE UDFs.
    /// Provides genuine TCVN 5574:2018 and TCVN 10304:2025 calculation algorithms with zero COM reliance.
    /// </summary>
    public static class RcGeotechUdfBridge
    {
        public static double TinhCotNeUonXien_PMM(
            double b, double h, double axialNKn, double momentMxKnm, double momentMyKnm,
            double asTotalMm2, double effectiveLengthL0Mm = 3000.0,
            string macBt = "B25", string macThep = "CB400-V", double a = 40.0)
        {
            if (b <= 0 || h <= 0 || asTotalMm2 <= 0) return 0.0;
            if (axialNKn == 0.0 && momentMxKnm == 0.0 && momentMyKnm == 0.0) return 0.0;

            var concrete = ResolveConcrete(macBt);
            var rebar = ResolveRebar(macThep);

            double barArea = asTotalMm2 / 4.0;
            int dia = Math.Max(10, (int)Math.Round(Math.Sqrt(4.0 * barArea / Math.PI)));
            double halfB = Math.Max(10.0, (b * 0.5) - a);
            double halfH = Math.Max(10.0, (h * 0.5) - a);

            var bars = new List<ColumnBarPlacement>
            {
                new(-halfB, -halfH, dia),
                new(halfB, -halfH, dia),
                new(halfB, halfH, dia),
                new(-halfB, halfH, dia)
            };

            double l0 = effectiveLengthL0Mm > 0 ? effectiveLengthL0Mm : 3000.0;
            var input = new ColumnPmmInput(
                "C1", ColumnSectionShape.Rectangular, b, h, l0,
                concrete, rebar, bars, axialNKn, momentMxKnm, momentMyKnm);

            var res = ColumnPmmInteractionCalculator.Calculate(input);
            return Math.Round(res.UtilizationRatio, 3);
        }

        public static double TinhVachCung(
            double lengthLwMm, double thicknessTwMm, double heightHwMm,
            double axialNKn, double shearVuKn, double momentMuKnm,
            string macBt = "B25", string macThep = "CB400-V",
            int webBarDia = 12, double webBarSpacingMm = 200.0)
        {
            if (lengthLwMm <= 0 || thicknessTwMm <= 0) return 0.0;

            double tw = Math.Max(150.0, thicknessTwMm);
            double hw = heightHwMm > 0 ? heightHwMm : 3000.0;
            var concrete = ResolveConcrete(macBt);
            var rebar = ResolveRebar(macThep);

            int dWeb = webBarDia > 0 ? webBarDia : 12;
            double sWeb = webBarSpacingMm > 0 ? webBarSpacingMm : 200.0;

            var input = new ShearWallInput(
                "W1", lengthLwMm, tw, hw, concrete, rebar,
                axialNKn, shearVuKn, momentMuKnm, dWeb, sWeb);

            var res = ShearWallCalculator.Calculate(input);
            return Math.Round(res.ShearUtilizationRatio, 3);
        }

        public static double TinhSanHaiPhuong(
            double lxMm, double lyMm, double thicknessHMm,
            double deadLoadGKnM2, double liveLoadPKnM2,
            string macBt = "B25", string macThep = "CB400-V",
            int boundaryCondition = 1, int returnMode = 0)
        {
            if (lxMm <= 0 || lyMm <= 0 || thicknessHMm <= 0) return 0.0;

            var concrete = ResolveConcrete(macBt);
            var rebar = ResolveRebar(macThep);

            var boundary = boundaryCondition is >= 0 and <= 5
                ? (SlabBoundaryCondition)boundaryCondition
                : SlabBoundaryCondition.FourEdgesFixed;

            var input = new TwoWaySlabInput(
                "S1", lxMm, lyMm, thicknessHMm,
                Math.Max(0.0, deadLoadGKnM2), Math.Max(0.0, liveLoadPKnM2),
                boundary, concrete, rebar);

            var res = TwoWaySlabCalculator.Calculate(input);
            return returnMode switch
            {
                0 => Math.Round(res.RequiredAsBotXMm2, 1),
                1 => Math.Round(res.RequiredAsBotYMm2, 1),
                2 => Math.Round(res.RequiredAsTopXMm2, 1),
                3 => Math.Round(res.RequiredAsTopYMm2, 1),
                4 => Math.Round(res.SpanPositiveMomentMxKnm, 2),
                5 => Math.Round(res.SpanPositiveMomentMyKnm, 2),
                6 => Math.Round(res.SupportNegativeMomentMxKnm, 2),
                7 => Math.Round(res.SupportNegativeMomentMyKnm, 2),
                _ => Math.Round(res.RequiredAsBotXMm2, 1)
            };
        }

        public static double TinhDaiCoc(
            double widthBMm, double lengthLMm, double thicknessHMm,
            double colBMm, double colHMm, double axialNKn, double momentMxKnm, double momentMyKnm,
            double pileCapacityKn, int pileCount = 4, double pileSpacingMm = 900.0,
            string macBt = "B25", string macThep = "CB400-V", int returnMode = 0)
        {
            if (widthBMm <= 0 || lengthLMm <= 0 || thicknessHMm <= 0 || colBMm <= 0 || colHMm <= 0) return 0.0;

            int count = pileCount > 0 ? pileCount : 4;
            double s = pileSpacingMm > 0 ? pileSpacingMm : 900.0;
            var pattern = count switch
            {
                1 => PileLayoutPattern.SinglePile,
                2 => PileLayoutPattern.TwoPile,
                3 => PileLayoutPattern.ThreePileTriangular,
                4 => PileLayoutPattern.FourPileRectangular,
                5 => PileLayoutPattern.FivePileDice,
                6 => PileLayoutPattern.SixPileRectangular,
                _ => PileLayoutPattern.FourPileRectangular
            };

            var piles = PileCapCalculator.GenerateStandardLayout(pattern, 400.0, s / 400.0);
            var concrete = ResolveConcrete(macBt);
            var rebar = ResolveRebar(macThep);

            var input = new PileCapInput(
                "DC-1", widthBMm, lengthLMm, thicknessHMm, colBMm, colHMm,
                concrete, rebar, piles, axialNKn, momentMxKnm, momentMyKnm, pileCapacityKn);

            var res = PileCapCalculator.Calculate(input);
            return returnMode switch
            {
                0 => Math.Round(res.PileCapacityRatio, 3),
                1 => Math.Round(res.ColumnPunchingRatio, 3),
                2 => Math.Round(res.CornerPilePunchingRatio, 3),
                3 => Math.Round(res.OneWayShearRatio, 3),
                4 => Math.Round(res.MaxReactionKn, 1),
                5 => Math.Round(res.MinReactionKn, 1),
                6 => Math.Round(res.RequiredAsXMm2, 1),
                7 => Math.Round(res.RequiredAsYMm2, 1),
                _ => Math.Round(res.PileCapacityRatio, 3)
            };
        }

        public static double TinhMongDon(
            double widthBMm, double lengthLMm, double thicknessHMm,
            double colBMm, double colHMm, double axialNKn, double momentMxKnm, double momentMyKnm,
            double allowableSoilRtcKpa = 250.0, double depthM = 1.5,
            string macBt = "B25", string macThep = "CB400-V", int returnMode = 0)
        {
            if (widthBMm <= 0 || lengthLMm <= 0 || thicknessHMm <= 0 || colBMm <= 0 || colHMm <= 0) return 0.0;

            double rtc = allowableSoilRtcKpa > 0 ? allowableSoilRtcKpa : 250.0;
            double d = depthM > 0 ? depthM : 1.5;
            var concrete = ResolveConcrete(macBt);
            var rebar = ResolveRebar(macThep);

            var input = new PadFootingInput(
                "MD-1", widthBMm, lengthLMm, thicknessHMm, colBMm, colHMm,
                d, concrete, rebar, axialNKn, momentMxKnm, momentMyKnm, rtc);

            var res = PadFootingCalculator.Calculate(input);
            return returnMode switch
            {
                0 => Math.Round(res.SoilCapacityRatio, 3),
                1 => Math.Round(res.BasePressureMaxKpa, 1),
                2 => Math.Round(res.BasePressureMinKpa, 1),
                3 => Math.Round(res.BasePressureAvgKpa, 1),
                4 => Math.Round(res.PunchingRatio, 3),
                5 => Math.Round(res.RequiredAsXMm2, 1),
                6 => Math.Round(res.RequiredAsYMm2, 1),
                7 => res.HasUplift ? 1.0 : 0.0,
                _ => Math.Round(res.SoilCapacityRatio, 3)
            };
        }

        public static double SucChiuTaiCoc_MaSat(
            double diameterM, double lengthM, double n60Shaft,
            bool isDriven = true, bool isCohesive = true, bool isDesign = true)
        {
            if (diameterM <= 0 || lengthM <= 0) return 0.0;

            double safeN = Math.Max(0.0, Math.Min(100.0, n60Shaft));
            if (safeN < 1e-4) return 0.0;

            var installType = isDriven ? PileInstallationType.DrivenPile : PileInstallationType.BoredPile;
            var soilType = isCohesive ? SoilConsistency.Cohesive : SoilConsistency.Cohesionless;

            double u = Math.PI * diameterM;
            double fi = PileCapacityCalculator.CalculateUnitShaftFriction(installType, soilType, safeN);
            double rs = u * fi * lengthM;
            double val = isDesign ? (rs / (1.4 * 1.15)) : rs;

            return Math.Round(val, 1);
        }

        public static double BeRongVetNutDam(
            double b, double h, double serviceMomentKnm, double asTensionMm2,
            double phiBarMm = 20.0, string macBt = "B25", string macThep = "CB400-V",
            double a = 40.0, bool isLongTerm = true)
        {
            if (b <= 0 || h <= 0 || asTensionMm2 <= 0 || a <= 0 || a >= h) return 0.0;
            if (Math.Abs(serviceMomentKnm) < 1e-4) return 0.0;

            var concrete = ResolveConcrete(macBt);
            var rebar = ResolveRebar(macThep);

            var input = new BeamCrackInput(b, h, serviceMomentKnm, asTensionMm2, concrete, rebar)
            {
                CoverTensionMm = a,
                BarDiameterMm = phiBarMm > 0 ? phiBarMm : 20.0,
                IsLongTerm = isLongTerm
            };

            var res = BeamCrackCalculator.Calculate(input);
            return Math.Round(res.CrackWidthMm, 4);
        }

        internal static ConcreteProperties ResolveConcrete(string name)
        {
            if (MaterialLibrary.TryGetConcrete(name, out var props) && props != null)
            {
                return props;
            }
            return MaterialLibrary.GetConcrete(ConcreteGrade.B25);
        }

        internal static RebarProperties ResolveRebar(string name)
        {
            if (MaterialLibrary.TryGetRebar(name, out var props) && props != null)
            {
                return props;
            }
            return MaterialLibrary.GetRebar(RebarGrade.CB400V);
        }
    }
}
