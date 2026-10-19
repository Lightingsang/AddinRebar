using System;
using HPKata.Core.QuantitySurveying.Models;

namespace HPKata.Core.QuantitySurveying.Calculators
{
    /// <summary>
    /// Computes gross concrete volume and gross formwork surface area for structural components.
    /// Operates in metric millimeters and converts results to cubic meters (m³) and square meters (m²).
    /// </summary>
    public static class QsTakeoffCalculator
    {
        private const double Mm3ToM3 = 1e-9;
        private const double Mm2ToM2 = 1e-6;

        public static double CalculateGrossVolume(QsElementModel element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            int count = Math.Max(1, element.MemberCount);
            var (w, h, l, t, planArea, _) = SanitizeDimensions(element);

            double volumeMm3 = element.MemberType switch
            {
                QsMemberType.Footing => planArea > 0.0 ? planArea * h : l * w * h,
                QsMemberType.Column => w * h * l,
                QsMemberType.Wall => (t > 0.0 ? t : w) * l * h,
                QsMemberType.Beam => w * h * l,
                QsMemberType.Slab => planArea > 0.0 ? planArea * (t > 0.0 ? t : h) : l * w * (t > 0.0 ? t : h),
                QsMemberType.Stair => CalculateStairVolume(w, h, l, t),
                _ => 0.0
            };

            return Math.Max(0.0, volumeMm3 * Mm3ToM3 * count);
        }

        public static (double soffitM2, double sideM2) CalculateGrossFormwork(QsElementModel element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            int count = Math.Max(1, element.MemberCount);
            var (w, h, l, t, planArea, perim) = SanitizeDimensions(element);

            var (soffitMm2, sideMm2) = element.MemberType switch
            {
                QsMemberType.Footing => (0.0, 2.0 * (l + w) * h),
                QsMemberType.Column => (0.0, 2.0 * (w + h) * l),
                QsMemberType.Wall => (0.0, 2.0 * (l + (t > 0.0 ? t : w)) * h),
                QsMemberType.Beam => CalculateBeamFormwork(element, w, h, l),
                QsMemberType.Slab => CalculateSlabFormwork(w, h, l, t, planArea, perim),
                QsMemberType.Stair => CalculateStairFormwork(w, h, l),
                _ => (0.0, 0.0)
            };

            return (Math.Max(0.0, soffitMm2 * Mm2ToM2 * count), Math.Max(0.0, sideMm2 * Mm2ToM2 * count));
        }

        private static (double w, double h, double l, double t, double planArea, double perim) SanitizeDimensions(QsElementModel element)
        {
            double w = Math.Max(0.0, element.WidthMm);
            double h = Math.Max(0.0, element.HeightMm);
            double l = Math.Max(0.0, element.LengthMm);
            double t = Math.Max(0.0, element.ThicknessMm);
            double planArea = Math.Max(0.0, element.PlanAreaMm2);
            double perim = Math.Max(0.0, element.PerimeterMm);
            return (w, h, l, t, planArea, perim);
        }

        private static double CalculateStairVolume(double w, double h, double l, double t)
        {
            double width = w > 0.0 ? w : 1000.0;
            double thick = t > 0.0 ? t : 150.0;
            double waistVol = width * l * thick;
            double stepsVol = 0.5 * l * h * width;
            return waistVol + stepsVol;
        }

        private static (double soffitMm2, double sideMm2) CalculateBeamFormwork(
            QsElementModel element, double w, double h, double l)
        {
            double soffit = w * l;
            double hLeft = element.HasSlabLeft
                ? Math.Max(0.0, h - Math.Max(0.0, element.SlabThicknessLeftMm))
                : h;
            double hRight = element.HasSlabRight
                ? Math.Max(0.0, h - Math.Max(0.0, element.SlabThicknessRightMm))
                : h;
            double sides = (hLeft + hRight) * l;
            return (soffit, sides);
        }

        private static (double soffitMm2, double sideMm2) CalculateSlabFormwork(
            double w, double h, double l, double t, double planArea, double perim)
        {
            double thick = t > 0.0 ? t : h;
            double soffit = planArea > 0.0 ? planArea : l * w;
            double sides = (perim > 0.0 ? perim : 2.0 * (l + w)) * thick;
            return (soffit, sides);
        }

        private static (double soffitMm2, double sideMm2) CalculateStairFormwork(double w, double h, double l)
        {
            double width = w > 0.0 ? w : 1000.0;
            double soffit = width * l;
            double risers = h * width;
            return (soffit, risers);
        }
    }
}
