using System;
using System.Collections.Generic;
using HPKata.Core.Beams.Models;

namespace HPKata.Core.Beams.Calculators
{
    /// <summary>
    /// Calculates longitudinal web skin / side face reinforcement (thep_gia / thep_suon) for deep beams.
    /// Governed by TCVN 5574:2018 §10.3.1.2 when overall beam depth h >= 700 mm.
    /// </summary>
    public static class BeamSkinBarCalculator
    {
        public const double MinimumDepthForSkinBarsMm = 700.0;
        public const double MaximumVerticalSpacingMm = 400.0;

        public static bool IsSkinReinforcementRequired(double beamHeightMm) =>
            beamHeightMm >= MinimumDepthForSkinBarsMm;

        public static int CalculateRequiredSideBarPairs(double beamHeightMm, double coverMm = 30.0)
        {
            if (!IsSkinReinforcementRequired(beamHeightMm))
            {
                return 0;
            }

            double webCoreDepth = beamHeightMm - 2.0 * coverMm - 100.0;
            int pairs = (int)Math.Ceiling(webCoreDepth / MaximumVerticalSpacingMm) - 1;
            return Math.Max(1, pairs);
        }

        public static List<double> CalculateSkinBarElevations(
            double beamHeightMm,
            double coverMm = 30.0,
            int defaultDiameterMm = 12)
        {
            var elevations = new List<double>();
            int pairs = CalculateRequiredSideBarPairs(beamHeightMm, coverMm);
            if (pairs <= 0) return elevations;

            double webBottom = coverMm + 50.0;
            double webTop = beamHeightMm - coverMm - 50.0;
            double spacing = (webTop - webBottom) / (pairs + 1);

            for (int i = 1; i <= pairs; i++)
            {
                elevations.Add(Math.Round(webBottom + i * spacing, 1));
            }

            return elevations;
        }

        public static List<RebarRunModel> GenerateSkinBarRuns(
            double totalSpanLengthMm,
            double beamHeightMm,
            double coverMm = 30.0,
            int diameterMm = 12)
        {
            var runs = new List<RebarRunModel>();
            var elevations = CalculateSkinBarElevations(beamHeightMm, coverMm, diameterMm);

            int markCounter = 1;
            foreach (double elev in elevations)
            {
                // Each elevation has 2 bars: 1 on the left side face, 1 on the right side face
                runs.Add(new RebarRunModel
                {
                    Mark = $"G{markCounter}",
                    DiameterMm = diameterMm,
                    BarCount = 2,
                    StartXMm = 0.0,
                    LengthMm = totalSpanLengthMm,
                    ElevationZMm = elev,
                    IsTopBar = false,
                    ShapeName = "dang01"
                });
                markCounter++;
            }

            return runs;
        }
    }
}
