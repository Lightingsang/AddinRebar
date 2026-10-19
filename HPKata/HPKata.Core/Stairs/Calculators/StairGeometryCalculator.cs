using System;
using System.Collections.Generic;
using HPKata.Core.Stairs.Models;

namespace HPKata.Core.Stairs.Calculators
{
    /// <summary>
    /// Computes staircase trigonometric parameters, concrete boundary polygon vertices,
    /// concrete volumes, formwork contact areas, and ergonomic checks.
    /// </summary>
    public static class StairGeometryCalculator
    {
        public static bool CheckErgonomics(double riserMm, double treadMm, out double blondelValue)
        {
            blondelValue = 2.0 * riserMm + treadMm;
            return blondelValue >= 580.0 && blondelValue <= 660.0;
        }

        public static double CalculateConcreteVolume(StairFlightModel flight)
        {
            if (flight == null) throw new ArgumentNullException(nameof(flight));
            flight.Validate();

            double w = flight.FlightWidthMm;
            double slopeLen = flight.SlopeLengthMm;
            double waistVolMm3 = w * flight.ThroatThicknessMm * slopeLen;

            double stepsVolMm3 = flight.TreadCount * (0.5 * flight.TreadWidthMm * flight.RiserHeightMm) * w;
            double land1VolMm3 = w * flight.BottomLandingLengthMm * flight.BottomLandingThkMm;
            double land2VolMm3 = w * flight.TopLandingLengthMm * flight.TopLandingThkMm;

            return (waistVolMm3 + stepsVolMm3 + land1VolMm3 + land2VolMm3) / 1e9 * flight.MemberCount;
        }

        public static double CalculateFormworkArea(StairFlightModel flight)
        {
            if (flight == null) throw new ArgumentNullException(nameof(flight));
            flight.Validate();

            double w = flight.FlightWidthMm;
            double soffitAreaMm2 = w * (flight.SlopeLengthMm + flight.BottomLandingLengthMm + flight.TopLandingLengthMm);
            double risersAreaMm2 = flight.RiserCount * flight.RiserHeightMm * w;
            double sideFacesMm2 = (flight.SlopeLengthMm * flight.ThroatThicknessMm) * 2.0;

            return (soffitAreaMm2 + risersAreaMm2 + sideFacesMm2) / 1e6 * flight.MemberCount;
        }

        public static List<(double X, double Y)> GenerateSectionProfile(StairFlightModel flight)
        {
            if (flight == null) throw new ArgumentNullException(nameof(flight));
            flight.Validate();

            var pts = new List<(double X, double Y)>();
            double l1 = flight.BottomLandingLengthMm;
            double l2 = flight.TopLandingLengthMm;
            double lFlight = flight.FlightRunLengthMm;
            double hFlight = flight.FlightTotalHeightMm;
            double t1 = flight.BottomLandingThkMm;
            double t2 = flight.TopLandingThkMm;

            // 1. Bottom landing soffit
            pts.Add((0.0, 0.0));
            pts.Add((l1, 0.0));

            // 2. Inclined soffit to top landing soffit
            double topSoffitY = hFlight + t1 - t2;
            pts.Add((l1 + lFlight, topSoffitY));
            pts.Add((l1 + lFlight + l2, topSoffitY));

            // 3. Top landing end and top surface
            pts.Add((l1 + lFlight + l2, hFlight + t1));
            pts.Add((l1 + lFlight, hFlight + t1));

            // 4. Stepped risers & treads
            for (int i = flight.RiserCount; i >= 1; i--)
            {
                double stepX = l1 + (i - 1) * flight.TreadWidthMm;
                double stepY = t1 + i * flight.RiserHeightMm;
                pts.Add((stepX, stepY));
                pts.Add((stepX, stepY - flight.RiserHeightMm));
            }

            // 5. Bottom landing top surface back to start
            pts.Add((0.0, t1));

            return pts;
        }
    }
}
