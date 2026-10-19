using System;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Steel;
using HPDcePro.Core.Calculations.Wind;

namespace HPDcePro.Excel
{
    /// <summary>
    /// Headless C# compatibility bridge for Structural Steel & Wind Load Excel DCE UDFs.
    /// Provides genuine TCVN 5575:2024 and TCVN 2737:2023 calculation algorithms with zero COM reliance.
    /// </summary>
    public static class SteelWindUdfBridge
    {
        public static double TinhDamThep_TCVN5575(
            double hMm, double bMm, double twMm, double tfMm, double momentMxKnm,
            double unbracedLengthL0M = 6.0, double shearVKn = 0.0,
            double yieldStrengthFyMpa = 235.0, double gammaC = 1.0)
        {
            if (hMm <= 0 || bMm <= 0 || twMm <= 0 || tfMm <= 0) return 0.0;
            if (hMm <= 2.0 * tfMm || bMm <= twMm) return 0.0;
            if (Math.Abs(momentMxKnm) < 1e-4 && Math.Abs(shearVKn) < 1e-4) return 0.0;

            double fy = yieldStrengthFyMpa > 0 ? yieldStrengthFyMpa : 235.0;
            double gc = gammaC > 0 ? gammaC : 1.0;
            double l0 = unbracedLengthL0M > 0 ? unbracedLengthL0M : 6.0;

            var sec = SteelSectionProperties.CreateISection(hMm, bMm, twMm, tfMm);
            var input = new SteelStabilityInput(0.0, momentMxKnm, sec, l0, l0, fy, gc)
            {
                TotalDepthMm = hMm,
                FlangeThicknessMm = tfMm
            };

            var res = SteelStabilityCalculator.Calculate(input);
            double flexureRatio = Math.Max(res.InPlaneRatio, res.OutOfPlaneRatio);

            double shearRatio = 0.0;
            if (Math.Abs(shearVKn) > 1e-4)
            {
                double aw = (hMm - 2.0 * tfMm) * twMm;
                double fv = 0.58 * fy;
                shearRatio = aw > 0 ? (Math.Abs(shearVKn) * 1e3) / (aw * fv * gc) : 0.0;
            }

            return Math.Round(Math.Max(flexureRatio, shearRatio), 3);
        }

        public static double TinhCotThep_TCVN5575(
            double hMm, double bMm, double twMm, double tfMm,
            double axialNKn, double momentMxKnm,
            double effectiveLengthLxM, double effectiveLengthLyM,
            double yieldStrengthFyMpa = 235.0, double gammaC = 1.0)
        {
            if (hMm <= 0 || bMm <= 0 || twMm <= 0 || tfMm <= 0) return 0.0;
            if (hMm <= 2.0 * tfMm || bMm <= twMm) return 0.0;
            if (Math.Abs(axialNKn) < 1e-4 && Math.Abs(momentMxKnm) < 1e-4) return 0.0;

            double fy = yieldStrengthFyMpa > 0 ? yieldStrengthFyMpa : 235.0;
            double gc = gammaC > 0 ? gammaC : 1.0;
            double lx = effectiveLengthLxM > 0 ? effectiveLengthLxM : 3.0;
            double ly = effectiveLengthLyM > 0 ? effectiveLengthLyM : 3.0;

            var sec = SteelSectionProperties.CreateISection(hMm, bMm, twMm, tfMm);
            var input = new SteelStabilityInput(axialNKn, momentMxKnm, sec, lx, ly, fy, gc)
            {
                TotalDepthMm = hMm,
                FlangeThicknessMm = tfMm
            };

            var res = SteelStabilityCalculator.Calculate(input);
            return Math.Round(Math.Max(res.InPlaneRatio, res.OutOfPlaneRatio), 3);
        }

        public static double TinhLienKetMoment_TCVN5575(
            double momentKnm, double shearKn,
            double beamDepthMm, double beamFlangeWidthMm, double beamFlangeThicknessMm, double beamWebThicknessMm,
            double endPlateThicknessMm, double boltDiaMm = 24.0, int boltRows = 2,
            string boltGrade = "8.8", double fyMpa = 345.0)
        {
            if (Math.Abs(momentKnm) < 1e-4 && Math.Abs(shearKn) < 1e-4) return 0.0;
            if (beamDepthMm <= 0 || beamFlangeWidthMm <= 0 || endPlateThicknessMm <= 0 || boltDiaMm <= 0) return 0.0;

            var grade = boltGrade.Contains("10.9") ? BoltGrade.Grade10_9 : BoltGrade.Grade8_8;
            double fy = fyMpa > 0 ? fyMpa : 345.0;

            var input = new SteelMomentConnectionInput
            {
                BeamDepthMm = beamDepthMm,
                BeamFlangeWidthMm = beamFlangeWidthMm,
                BeamFlangeThicknessMm = beamFlangeThicknessMm > 0 ? beamFlangeThicknessMm : 16.0,
                BeamWebThicknessMm = beamWebThicknessMm > 0 ? beamWebThicknessMm : 10.0,
                EndPlateThicknessMm = endPlateThicknessMm,
                EndPlateWidthMm = Math.Max(beamFlangeWidthMm + 20.0, 200.0),
                BoltDiameterMm = boltDiaMm,
                TensionBoltRowCount = boltRows > 0 ? boltRows : 2,
                Grade = grade,
                YieldStrengthMpa = fy,
                ColumnYieldStrengthMpa = fy,
                BendingMomentKnm = momentKnm,
                ShearForceKn = shearKn
            };

            var res = SteelMomentConnectionCalculator.Calculate(input);
            double governing = Math.Max(Math.Max(res.BoltInteractionRatio, res.PlateThicknessRatio), res.WeldStressRatio);
            return Math.Round(governing, 3);
        }

        public static double TinhBuLongNeo_TCVN5575(
            double axialNKn, double momentKnm, double shearKn,
            double plateLengthMm = 600.0, double plateWidthMm = 400.0, double plateThicknessMm = 25.0,
            int boltCount = 4, double boltDiaMm = 24.0,
            string macBtPedestal = "B25", double anchorFyMpa = 300.0, int returnMode = 0)
        {
            if (plateLengthMm <= 0 || plateWidthMm <= 0 || plateThicknessMm <= 0 || boltDiaMm <= 0) return 0.0;

            var grade = ResolveConcreteGrade(macBtPedestal);
            var input = new AnchorBoltBasePlateInput
            {
                PlateLengthLMm = plateLengthMm,
                PlateWidthBMm = plateWidthMm,
                PlateThicknessMm = plateThicknessMm,
                AnchorBoltCount = boltCount > 0 ? boltCount : 4,
                AnchorBoltDiameterMm = boltDiaMm,
                PedestalGrade = grade,
                AnchorYieldStrengthMpa = anchorFyMpa > 0 ? anchorFyMpa : 300.0,
                AxialForceKn = axialNKn,
                BendingMomentKnm = momentKnm,
                ShearForceKn = shearKn
            };

            var res = AnchorBoltBasePlateCalculator.Calculate(input);
            double governing = Math.Max(Math.Max(res.BearingStressRatio, res.PlateThicknessRatio), Math.Max(res.AnchorTensionRatio, res.BaseShearRatio));

            return returnMode switch
            {
                0 => Math.Round(governing, 3),
                1 => Math.Round(res.BearingStressRatio, 3),
                2 => Math.Round(res.PlateThicknessRatio, 3),
                3 => Math.Round(res.AnchorTensionRatio, 3),
                4 => Math.Round(res.BaseShearRatio, 3),
                5 => Math.Round(res.RequiredPlateThickness, 1),
                6 => Math.Round(res.AnchorTensionForceKn, 1),
                7 => Math.Round(res.EmbedmentLength, 0),
                _ => Math.Round(governing, 3)
            };
        }

        public static double TinhTaiTrongGio_TCVN2737(
            string vungGio, string diaHinh, double caoDoZeM,
            double cpe = 0.8, double cpi = 0.0,
            bool isDesign = true, double gustFactorG = 1.0)
        {
            var zone = ResolveWindZone(vungGio);
            var terrain = ResolveTerrain(diaHinh);
            double g = gustFactorG > 0 ? gustFactorG : 1.0;

            var res = WindLoadCalculator.CalculateByZone(zone, terrain, caoDoZeM, cpe, cpi, g);
            double val = isDesign ? res.DesignPressureWKnm2 : res.CharacteristicPressureWkKnm2;

            return Math.Round(val, 3);
        }

        private static ConcreteGrade ResolveConcreteGrade(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return ConcreteGrade.B25;
            string clean = name.Trim().ToUpperInvariant();
            if (clean.Contains("B15")) return ConcreteGrade.B15;
            if (clean.Contains("B20")) return ConcreteGrade.B20;
            if (clean.Contains("B25")) return ConcreteGrade.B25;
            if (clean.Contains("B30")) return ConcreteGrade.B30;
            if (clean.Contains("B35")) return ConcreteGrade.B35;
            if (clean.Contains("B40")) return ConcreteGrade.B40;
            return ConcreteGrade.B25;
        }

        private static WindZone ResolveWindZone(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return WindZone.ZoneII;
            string clean = name.Trim().ToUpperInvariant();
            if (clean.Contains("IV") || clean.Contains("4")) return WindZone.ZoneIV;
            if (clean.Contains("V") || clean.Contains("5")) return WindZone.ZoneV;
            if (clean.Contains("III") || clean.Contains("3")) return WindZone.ZoneIII;
            if (clean.Contains("II") || clean.Contains("2")) return WindZone.ZoneII;
            if (clean.Contains("I") || clean.Contains("1")) return WindZone.ZoneI;
            return WindZone.ZoneII;
        }

        private static TerrainCategory ResolveTerrain(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return TerrainCategory.B;
            string clean = name.Trim().ToUpperInvariant();
            if (clean.Contains("A")) return TerrainCategory.A;
            if (clean.Contains("B")) return TerrainCategory.B;
            if (clean.Contains("C")) return TerrainCategory.C;
            if (clean.Contains("D")) return TerrainCategory.D;
            return TerrainCategory.B;
        }
    }
}
