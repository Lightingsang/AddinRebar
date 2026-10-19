using System;

namespace HPDcePro.Core.Calculations.Wind
{
    public enum WindZone
    {
        ZoneI,
        ZoneII,
        ZoneIII,
        ZoneIV,
        ZoneV
    }

    public enum TerrainCategory
    {
        A, // Coastal, open water, flat plains without shelter
        B, // Suburban, open country with scattered trees
        C, // Urban, industrial, medium building density
        D  // Dense city centers with high-rise buildings
    }

    public sealed class WindLoadResult
    {
        public double BasicPressureQ0Knm2 { get; set; }
        public double ExposureKze { get; set; }
        public double ExternalCoefficientCpe { get; set; }
        public double InternalCoefficientCpi { get; set; }
        public double NetAerodynamicCoefficientC => ExternalCoefficientCpe - InternalCoefficientCpi;
        public double GustFactorG { get; set; } = 1.0;
        public double CharacteristicPressureWkKnm2 { get; set; }
        public double DesignPressureWKnm2 { get; set; }
    }

    /// <summary>
    /// Aerodynamic wind load calculator per TCVN 2737:2023.
    /// </summary>
    public static class WindLoadCalculator
    {
        public const double DefaultLoadFactorGammaF = 1.3;

        public static double GetBasicPressureByZone(WindZone zone)
        {
            return zone switch
            {
                WindZone.ZoneI => 0.48,   // V10 = 28 m/s
                WindZone.ZoneII => 0.71,  // V10 = 34 m/s
                WindZone.ZoneIII => 0.98, // V10 = 40 m/s
                WindZone.ZoneIV => 1.30,  // V10 = 46 m/s
                WindZone.ZoneV => 1.59,   // V10 = 51 m/s
                _ => throw new ArgumentOutOfRangeException(nameof(zone), $"Unsupported wind zone: {zone}")
            };
        }

        public static double CalculateVelocityPressure(double v10Mps)
        {
            if (v10Mps <= 0) return 0.0;
            // q0 = 0.613 * 10^-3 * V10^2 (kN/m2)
            return 0.613e-3 * Math.Pow(v10Mps, 2);
        }

        public static double CalculateExposureCoefficient(TerrainCategory category, double heightZeM)
        {
            var (alpha, zg, zmin) = GetTerrainParameters(category);
            double effectiveZ = Math.Max(heightZeM, zmin);
            double clampedZ = Math.Min(effectiveZ, zg);

            // k(ze) = 2.01 * (ze / zg)^(2 / alpha)
            return 2.01 * Math.Pow(clampedZ / zg, 2.0 / alpha);
        }

        private static (double Alpha, double Zg, double Zmin) GetTerrainParameters(TerrainCategory category)
        {
            return category switch
            {
                TerrainCategory.A => (9.5, 213.0, 2.0),
                TerrainCategory.B => (7.0, 274.0, 4.0),
                TerrainCategory.C => (4.5, 365.0, 9.0),
                TerrainCategory.D => (3.0, 457.0, 15.0),
                _ => (7.0, 274.0, 4.0)
            };
        }

        public static WindLoadResult Calculate(
            double basicPressureQ0Knm2,
            TerrainCategory category,
            double heightZeM,
            double cpe,
            double cpi = 0.0,
            double gustFactorG = 1.0,
            double loadFactorGammaF = DefaultLoadFactorGammaF)
        {
            if (basicPressureQ0Knm2 < 0) throw new ArgumentOutOfRangeException(nameof(basicPressureQ0Knm2), "Basic pressure must be non-negative.");

            double kze = CalculateExposureCoefficient(category, heightZeM);
            double netC = cpe - cpi;
            double wk = basicPressureQ0Knm2 * kze * netC * gustFactorG;
            double wDesign = wk * loadFactorGammaF;

            return new WindLoadResult
            {
                BasicPressureQ0Knm2 = basicPressureQ0Knm2,
                ExposureKze = kze,
                ExternalCoefficientCpe = cpe,
                InternalCoefficientCpi = cpi,
                GustFactorG = gustFactorG,
                CharacteristicPressureWkKnm2 = wk,
                DesignPressureWKnm2 = wDesign
            };
        }

        public static WindLoadResult CalculateByZone(
            WindZone zone,
            TerrainCategory category,
            double heightZeM,
            double cpe,
            double cpi = 0.0,
            double gustFactorG = 1.0,
            double loadFactorGammaF = DefaultLoadFactorGammaF)
        {
            double q0 = GetBasicPressureByZone(zone);
            return Calculate(q0, category, heightZeM, cpe, cpi, gustFactorG, loadFactorGammaF);
        }
    }
}
