using System;
using System.Collections.Generic;
using HPDcePro.Core.Calculations.Beam;

namespace HPDcePro.Core.Calculations.Column
{
    public static class ColumnPmmInteractionCalculator
    {
        public static ColumnPmmResult Calculate(ColumnPmmInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            (double areaC, double ix, double iy) = ComputeConcreteSectionProperties(input);
            (double areaS, double isx, double isy) = ComputeSteelSectionProperties(input.Bars);

            (double etaX, double etaY) = ComputeSlendernessMagnification(input, areaC, ix, iy, isx, isy);
            double mxd = etaX * Math.Abs(input.DesignMomentMxKnm);
            double myd = etaY * Math.Abs(input.DesignMomentMyKnm);

            double n0 = (input.Concrete.Rb * areaC + input.Rebar.Rsc * areaS) * 1e-3;
            double mux = CalculateUniaxialMomentCapacity(input, true, input.DesignAxialNKn);
            double muy = CalculateUniaxialMomentCapacity(input, false, input.DesignAxialNKn);

            double ur = CalculateUtilizationRatio(input.DesignAxialNKn, mxd, myd, n0, mux, muy);
            bool isSafe = ur <= 1.0 && input.DesignAxialNKn < n0;

            var curve = GenerateInteractionCurve(input, areaC, areaS, n0);
            string summary = isSafe
                ? $"PASS (UR = {ur:F2}, Mxd={mxd:F1}kNm, Myd={myd:F1}kNm, N0={n0:F1}kN)"
                : $"FAIL (UR = {ur:F2}, Mxd={mxd:F1}kNm, Myd={myd:F1}kNm, N0={n0:F1}kN)";

            return new ColumnPmmResult(
                input.ColumnMark, input.DesignAxialNKn, input.DesignMomentMxKnm, input.DesignMomentMyKnm,
                etaX, etaY, mxd, myd, n0, mux, muy, ur, isSafe, curve, summary);
        }

        private static (double Area, double Ix, double Iy) ComputeConcreteSectionProperties(ColumnPmmInput input)
        {
            if (input.SectionShape == ColumnSectionShape.Circular)
            {
                double d = input.HeightHMm;
                double area = Math.PI * d * d / 4.0;
                double i = Math.PI * Math.Pow(d, 4) / 64.0;
                return (area, i, i);
            }
            double b = input.WidthBMm;
            double h = input.HeightHMm;
            return (b * h, b * Math.Pow(h, 3) / 12.0, h * Math.Pow(b, 3) / 12.0);
        }

        private static (double Area, double Isx, double Isy) ComputeSteelSectionProperties(IReadOnlyList<ColumnBarPlacement> bars)
        {
            double totArea = 0.0, isx = 0.0, isy = 0.0;
            for (int i = 0; i < bars.Count; i++)
            {
                double a = bars[i].AreaMm2;
                totArea += a;
                isx += a * bars[i].Y * bars[i].Y;
                isy += a * bars[i].X * bars[i].X;
            }
            return (totArea, isx, isy);
        }

        private static (double EtaX, double EtaY) ComputeSlendernessMagnification(
            ColumnPmmInput input, double areaC, double ix, double iy, double isx, double isy)
        {
            double l0 = input.EffectiveLengthL0Mm;
            double rx = Math.Sqrt(ix / areaC);
            double ry = Math.Sqrt(iy / areaC);

            double lambdaX = l0 / rx;
            double lambdaY = l0 / ry;

            double etaX = 1.0;
            double etaY = 1.0;

            if (lambdaX > 14.0 && input.DesignAxialNKn > 0)
            {
                double ncrX = CalculateCriticalEulerLoad(input, ix, isx, input.HeightHMm, input.DesignMomentMxKnm);
                etaX = input.DesignAxialNKn >= ncrX ? 999.0 : Math.Max(1.0, 1.0 / (1.0 - input.DesignAxialNKn / ncrX));
            }

            if (lambdaY > 14.0 && input.DesignAxialNKn > 0)
            {
                double ncrY = CalculateCriticalEulerLoad(input, iy, isy, input.WidthBMm, input.DesignMomentMyKnm);
                etaY = input.DesignAxialNKn >= ncrY ? 999.0 : Math.Max(1.0, 1.0 / (1.0 - input.DesignAxialNKn / ncrY));
            }

            return (etaX, etaY);
        }

        private static double CalculateCriticalEulerLoad(
            ColumnPmmInput input, double ic, double isSteel, double dim, double momentKnm)
        {
            double e0 = input.DesignAxialNKn > 0
                ? Math.Max(Math.Abs(momentKnm) * 1e3 / input.DesignAxialNKn, Math.Max(dim / 30.0, 10.0))
                : Math.Max(dim / 30.0, 10.0);

            double deltaE = e0 / dim;
            double kb = 0.15 / (input.LongTermFactorPhiL * (0.3 + deltaE));
            if (kb < 0.05) kb = 0.05;
            if (kb > 0.25) kb = 0.25;

            double ks = 0.7;
            double es = 200000.0;
            double d = kb * input.Concrete.Eb * ic + ks * es * isSteel;
            double l0 = input.EffectiveLengthL0Mm;

            return (Math.PI * Math.PI * d / (l0 * l0)) * 1e-3;
        }

        private static double CalculateUniaxialMomentCapacity(ColumnPmmInput input, bool aboutX, double targetAxialKn)
        {
            double dim = aboutX ? input.HeightHMm : input.WidthBMm;
            double width = aboutX ? input.WidthBMm : input.HeightHMm;
            double rb = input.Concrete.Rb;
            double rs = input.Rebar.Rs;
            double rsc = input.Rebar.Rsc;
            double es = 200000.0;
            double epsCu = 0.0035;

            double lowXc = 0.0;
            double highXc = dim * 1.8;
            double bestXc = dim * 0.5;

            for (int iter = 0; iter < 24; iter++)
            {
                double xc = 0.5 * (lowXc + highXc);
                double nInt = EvaluateInternalAxialForce(input, aboutX, dim, width, rb, rs, rsc, es, epsCu, xc);

                if (nInt < targetAxialKn)
                    lowXc = xc;
                else
                    highXc = xc;

                bestXc = xc;
            }

            return EvaluateInternalMoment(input, aboutX, dim, width, rb, rs, rsc, es, epsCu, bestXc);
        }

        private static double EvaluateInternalAxialForce(
            ColumnPmmInput input, bool aboutX, double dim, double width,
            double rb, double rs, double rsc, double es, double epsCu, double xc)
        {
            double concForce = 0.0;
            if (xc > 1e-4)
            {
                double depthComp = Math.Min(dim, 0.8 * xc);
                concForce = rb * depthComp * width;
            }

            double steelForce = 0.0;
            for (int i = 0; i < input.Bars.Count; i++)
            {
                double pos = aboutX ? input.Bars[i].Y : input.Bars[i].X;
                double distFromTop = (dim * 0.5) - pos;
                double strain = (xc > 1e-4) ? epsCu * (1.0 - distFromTop / xc) : -rs / es;
                double stress = strain >= 0 ? Math.Min(rsc, strain * es) : Math.Max(-rs, strain * es);
                steelForce += stress * input.Bars[i].AreaMm2;
            }

            return (concForce + steelForce) * 1e-3;
        }

        private static double EvaluateInternalMoment(
            ColumnPmmInput input, bool aboutX, double dim, double width,
            double rb, double rs, double rsc, double es, double epsCu, double xc)
        {
            double concMoment = 0.0;
            if (xc > 1e-4)
            {
                double depthComp = Math.Min(dim, 0.8 * xc);
                double concForce = rb * depthComp * width;
                double arm = (dim * 0.5) - (depthComp * 0.5);
                concMoment = concForce * arm;
            }

            double steelMoment = 0.0;
            for (int i = 0; i < input.Bars.Count; i++)
            {
                double pos = aboutX ? input.Bars[i].Y : input.Bars[i].X;
                double distFromTop = (dim * 0.5) - pos;
                double strain = (xc > 1e-4) ? epsCu * (1.0 - distFromTop / xc) : -rs / es;
                double stress = strain >= 0 ? Math.Min(rsc, strain * es) : Math.Max(-rs, strain * es);
                steelMoment += stress * input.Bars[i].AreaMm2 * pos;
            }

            return Math.Max(1.0, (concMoment + steelMoment) * 1e-6);
        }

        private static double CalculateUtilizationRatio(
            double n, double mxd, double myd, double n0, double mux, double muy)
        {
            if (n >= n0) return n / Math.Max(1.0, n0);
            if (mux <= 1e-3) mux = 1e-3;
            if (muy <= 1e-3) muy = 1e-3;

            double ratioN = Math.Max(0.0, n / Math.Max(1.0, n0));
            double alpha = ratioN <= 0.1 ? 1.0 : (ratioN >= 0.7 ? 2.0 : 1.0 + (ratioN - 0.1) * (1.0 / 0.6));

            double rx = Math.Pow(mxd / mux, alpha);
            double ry = Math.Pow(myd / muy, alpha);
            double bresler = Math.Pow(rx + ry, 1.0 / alpha);

            return Math.Max(ratioN, bresler);
        }

        private static List<PmmPoint3D> GenerateInteractionCurve(
            ColumnPmmInput input, double areaC, double areaS, double n0)
        {
            var points = new List<PmmPoint3D>();
            int steps = 12;
            for (int s = 0; s <= steps; s++)
            {
                double axialFraction = (double)s / steps;
                double axN = axialFraction * n0;
                double capX = CalculateUniaxialMomentCapacity(input, true, axN);
                double capY = CalculateUniaxialMomentCapacity(input, false, axN);
                points.Add(new PmmPoint3D(axN, capX, capY));
            }
            return points;
        }
    }
}
