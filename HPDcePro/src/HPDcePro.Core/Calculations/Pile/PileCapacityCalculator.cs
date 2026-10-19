using System;
using System.Collections.Generic;
using System.Linq;

namespace HPDcePro.Core.Calculations.Pile
{
    public enum PileInstallationType
    {
        DrivenPile,
        BoredPile
    }

    public enum SoilConsistency
    {
        Cohesive,
        Cohesionless
    }

    public sealed class SoilLayer
    {
        public string Name { get; set; } = string.Empty;
        public SoilConsistency SoilType { get; set; }
        public double TopDepthM { get; set; }
        public double BottomDepthM { get; set; }
        public double N60 { get; set; }

        public SoilLayer(string name, SoilConsistency soilType, double topDepthM, double bottomDepthM, double n60)
        {
            Name = name;
            SoilType = soilType;
            TopDepthM = topDepthM;
            BottomDepthM = bottomDepthM;
            N60 = n60;
        }
    }

    public sealed class PileGeometry
    {
        public bool IsCircular { get; set; } = true;
        public double DiameterM { get; set; } = 0.4;
        public double WidthM { get; set; } = 0.4;
        public double HeightM { get; set; } = 0.4;
        public double TopDepthM { get; set; } = 0.0;
        public double TipDepthM { get; set; } = 20.0;

        public double AreaM2 => IsCircular
            ? Math.PI * DiameterM * DiameterM / 4.0
            : WidthM * HeightM;

        public double PerimeterM => IsCircular
            ? Math.PI * DiameterM
            : 2.0 * (WidthM + HeightM);
    }

    public sealed class LayerShaftContribution
    {
        public string LayerName { get; set; } = string.Empty;
        public double EffectiveThicknessM { get; set; }
        public double UnitShaftResistanceKpa { get; set; }
        public double ShaftCapacityKn { get; set; }
    }

    public sealed class PileCapacityResult
    {
        public double UnitTipResistanceQbKpa { get; set; }
        public double TipCapacityRbKn { get; set; }
        public double ShaftCapacityRsKn { get; set; }
        public double UltimateCapacityRuKn { get; set; }
        public double DesignCapacityRcKn { get; set; }
        public List<LayerShaftContribution> LayerContributions { get; set; } = new();
    }

    /// <summary>
    /// Deep Foundation SPT Pile Bearing Capacity Calculator per TCVN 10304:2025.
    /// </summary>
    public static class PileCapacityCalculator
    {
        public static PileCapacityResult Calculate(
            PileGeometry pile,
            IEnumerable<SoilLayer> layers,
            PileInstallationType installType,
            double tipSoilN60,
            SoilConsistency tipSoilType,
            double gammaK = 1.4,
            double gammaN = 1.15)
        {
            ValidateInputs(pile, layers, gammaK, gammaN);

            double qb = CalculateUnitTipResistance(installType, tipSoilType, tipSoilN60);
            double rb = qb * pile.AreaM2;

            var contributions = CalculateShaftContributions(pile, layers, installType);
            double rs = contributions.Sum(c => c.ShaftCapacityKn);
            double ru = rb + rs;
            double rc = ru / (gammaK * gammaN);

            return new PileCapacityResult
            {
                UnitTipResistanceQbKpa = qb,
                TipCapacityRbKn = rb,
                ShaftCapacityRsKn = rs,
                UltimateCapacityRuKn = ru,
                DesignCapacityRcKn = rc,
                LayerContributions = contributions
            };
        }

        private static void ValidateInputs(
            PileGeometry pile, IEnumerable<SoilLayer> layers, double gammaK, double gammaN)
        {
            if (pile == null) throw new ArgumentNullException(nameof(pile));
            if (layers == null) throw new ArgumentNullException(nameof(layers));
            if (pile.TipDepthM <= pile.TopDepthM)
            {
                throw new ArgumentException("Tip depth must exceed top depth.", nameof(pile));
            }
            if (gammaK <= 0 || gammaN <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gammaK), "Safety factors must be positive.");
            }
        }

        public static double CalculateUnitTipResistance(
            PileInstallationType installType, SoilConsistency soilType, double n60)
        {
            double safeN = Math.Max(0.0, n60);
            if (installType == PileInstallationType.DrivenPile)
            {
                if (soilType == SoilConsistency.Cohesionless)
                {
                    return Math.Min(350.0 * safeN, 10000.0);
                }
                return Math.Min(200.0 * safeN, 6000.0);
            }

            // Bored pile
            if (soilType == SoilConsistency.Cohesionless)
            {
                return Math.Min(150.0 * safeN, 5000.0);
            }
            return Math.Min(120.0 * safeN, 3500.0);
        }

        public static double CalculateUnitShaftFriction(
            PileInstallationType installType, SoilConsistency soilType, double n60)
        {
            double safeN = Math.Max(0.0, n60);
            if (soilType == SoilConsistency.Cohesive)
            {
                return Math.Min(10.0 * Math.Pow(safeN, 0.6), 100.0);
            }

            double ks = installType == PileInstallationType.DrivenPile ? 2.0 : 1.5;
            return Math.Min(ks * safeN, 120.0);
        }

        private static List<LayerShaftContribution> CalculateShaftContributions(
            PileGeometry pile, IEnumerable<SoilLayer> layers, PileInstallationType installType)
        {
            var list = new List<LayerShaftContribution>();
            double u = pile.PerimeterM;

            foreach (var lyr in layers)
            {
                double overlapTop = Math.Max(pile.TopDepthM, lyr.TopDepthM);
                double overlapBottom = Math.Min(pile.TipDepthM, lyr.BottomDepthM);
                double length = Math.Max(0.0, overlapBottom - overlapTop);

                if (length > 1e-4)
                {
                    double fi = CalculateUnitShaftFriction(installType, lyr.SoilType, lyr.N60);
                    double capKn = u * fi * length;
                    list.Add(new LayerShaftContribution
                    {
                        LayerName = lyr.Name,
                        EffectiveThicknessM = length,
                        UnitShaftResistanceKpa = fi,
                        ShaftCapacityKn = capKn
                    });
                }
            }
            return list;
        }
    }
}
