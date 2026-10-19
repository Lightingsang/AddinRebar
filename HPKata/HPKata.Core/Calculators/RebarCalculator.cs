using System;
using System.Collections.Generic;

namespace HPKata.Core.Calculators
{
    /// <summary>
    /// Calculates hook lengths, bend deductions, lap splices, and unit weights per TCVN 5574:2018 / TCVN 1651:2018.
    /// </summary>
    public static class RebarCalculator
    {
        private static readonly Dictionary<int, double> TcvnUnitMassTable = new()
        {
            { 6, 0.222 },
            { 8, 0.395 },
            { 10, 0.617 },
            { 12, 0.888 },
            { 14, 1.210 },
            { 16, 1.580 },
            { 18, 2.000 },
            { 20, 2.470 },
            { 22, 2.980 },
            { 25, 3.850 },
            { 28, 4.830 },
            { 32, 6.310 },
            { 36, 7.990 },
            { 38, 8.900 },
            { 40, 9.860 },
            { 41, 10.360 },
            { 43, 11.400 },
            { 50, 15.420 },
            { 51, 16.040 }
        };

        public static double GetUnitMass(int diameterMm)
        {
            if (diameterMm <= 0) return 0.0;
            if (TcvnUnitMassTable.TryGetValue(diameterMm, out double mass))
            {
                return mass;
            }
            return (diameterMm * diameterMm) / 162.2;
        }

        public static double CalculateWeight(double lengthM, int diameterMm)
        {
            return lengthM * GetUnitMass(diameterMm);
        }

        public static double Hook90Length(int diameterMm, double multiplier = 10.0)
        {
            if (diameterMm <= 0) return 0.0;
            double len = multiplier * diameterMm;
            return Math.Max(len, 75.0);
        }

        public static double Hook135Length(int diameterMm, double multiplier = 7.5)
        {
            if (diameterMm <= 0) return 0.0;
            double len = multiplier * diameterMm;
            return Math.Max(len, 75.0);
        }

        public static double Hook180Length(int diameterMm)
        {
            if (diameterMm <= 0) return 0.0;
            double len = 9.0 * diameterMm;
            return Math.Max(len, 60.0);
        }

        public static double BendRadiusInner(int diameterMm, double angleDeg)
        {
            if (diameterMm <= 0) return 0.0;
            if (Math.Abs(angleDeg - 90.0) < 1.0)
            {
                return diameterMm <= 16 ? 2.0 * diameterMm : 3.5 * diameterMm;
            }
            if (Math.Abs(angleDeg - 135.0) < 1.0)
            {
                return diameterMm <= 10 ? 1.5 * diameterMm : 2.0 * diameterMm;
            }
            return 1.25 * diameterMm;
        }

        public static double BendDeduction(int diameterMm, double angleDeg, bool applyDeduction)
        {
            if (!applyDeduction || diameterMm <= 0) return 0.0;

            double rInner = BendRadiusInner(diameterMm, angleDeg);
            double d = diameterMm;

            if (Math.Abs(angleDeg - 90.0) < 1.0)
            {
                double arcCenterline = 0.5 * Math.PI * (rInner + 0.5 * d);
                return 2.0 * (rInner + d) - arcCenterline;
            }
            if (Math.Abs(angleDeg - 135.0) < 1.0)
            {
                return 1.5 * d;
            }

            double rad = angleDeg * Math.PI / 180.0;
            double tangentDist = (rInner + d) * Math.Tan(rad / 2.0);
            double arcLen = rad * (rInner + 0.5 * d);
            return 2.0 * tangentDist - arcLen;
        }

        public static double LapSpliceLength(int diameterMm, double multiplier = 30.0)
        {
            return diameterMm > 0 ? multiplier * diameterMm : 0.0;
        }

        public static double TensionAnchorageLength(int diameterMm, double multiplier = 40.0)
        {
            return diameterMm > 0 ? multiplier * diameterMm : 0.0;
        }

        public static double RoundLength(double lengthMm, double stepMm = 10.0)
        {
            if (stepMm <= 0.0) return lengthMm;
            return Math.Ceiling(lengthMm / stepMm) * stepMm;
        }
    }
}
