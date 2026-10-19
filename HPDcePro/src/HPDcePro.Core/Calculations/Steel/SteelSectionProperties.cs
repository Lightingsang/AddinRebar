using System;

namespace HPDcePro.Core.Calculations.Steel
{
    /// <summary>
    /// Geometric section properties for standard structural steel profiles.
    /// </summary>
    public sealed class SteelSectionProperties
    {
        public double AreaMm2 { get; set; }
        public double IxMm4 { get; set; }
        public double IyMm4 { get; set; }
        public double WxMm3 { get; set; }
        public double WyMm3 { get; set; }
        public double RadiusGyrationXmm => AreaMm2 > 0 ? Math.Sqrt(IxMm4 / AreaMm2) : 0.0;
        public double RadiusGyrationYmm => AreaMm2 > 0 ? Math.Sqrt(IyMm4 / AreaMm2) : 0.0;
        public double TorsionalConstantItMm4 { get; set; }
        public double WarpingConstantIwMm6 { get; set; }

        public static SteelSectionProperties CreateISection(
            double hMm, double bMm, double twMm, double tfMm, double rMm = 0.0)
        {
            ValidateIInputs(hMm, bMm, twMm, tfMm);

            double hw = hMm - (2.0 * tfMm);
            double filletArea = (4.0 - Math.PI) * rMm * rMm;
            double area = (2.0 * bMm * tfMm) + (hw * twMm) + filletArea;

            double ix = ((bMm * Math.Pow(hMm, 3)) - ((bMm - twMm) * Math.Pow(hw, 3))) / 12.0;
            double iy = ((2.0 * tfMm * Math.Pow(bMm, 3)) + (hw * Math.Pow(twMm, 3))) / 12.0;
            double wx = 2.0 * ix / hMm;
            double wy = 2.0 * iy / bMm;

            double it = ((2.0 * bMm * Math.Pow(tfMm, 3)) + (hw * Math.Pow(twMm, 3))) / 3.0;
            double iw = (iy * Math.Pow(hMm - tfMm, 2)) / 4.0;

            return new SteelSectionProperties
            {
                AreaMm2 = area,
                IxMm4 = ix,
                IyMm4 = iy,
                WxMm3 = wx,
                WyMm3 = wy,
                TorsionalConstantItMm4 = it,
                WarpingConstantIwMm6 = iw
            };
        }

        public static SteelSectionProperties CreateHSection(
            double sizeMm, double twMm, double tfMm, double rMm = 0.0)
        {
            return CreateISection(sizeMm, sizeMm, twMm, tfMm, rMm);
        }

        public static SteelSectionProperties CreateBoxSection(
            double widthMm, double heightMm, double thicknessMm)
        {
            ValidateBoxInputs(widthMm, heightMm, thicknessMm);

            double bi = widthMm - (2.0 * thicknessMm);
            double hi = heightMm - (2.0 * thicknessMm);
            double area = (widthMm * heightMm) - (bi * hi);

            double ix = ((widthMm * Math.Pow(heightMm, 3)) - (bi * Math.Pow(hi, 3))) / 12.0;
            double iy = ((heightMm * Math.Pow(widthMm, 3)) - (hi * Math.Pow(bi, 3))) / 12.0;
            double wx = 2.0 * ix / heightMm;
            double wy = 2.0 * iy / widthMm;

            return new SteelSectionProperties
            {
                AreaMm2 = area,
                IxMm4 = ix,
                IyMm4 = iy,
                WxMm3 = wx,
                WyMm3 = wy
            };
        }

        public static SteelSectionProperties CreatePipeSection(
            double outerDiameterMm, double thicknessMm)
        {
            ValidatePipeInputs(outerDiameterMm, thicknessMm);

            double d = outerDiameterMm - (2.0 * thicknessMm);
            double area = Math.PI * (Math.Pow(outerDiameterMm, 2) - Math.Pow(d, 2)) / 4.0;
            double i = Math.PI * (Math.Pow(outerDiameterMm, 4) - Math.Pow(d, 4)) / 64.0;
            double w = 2.0 * i / outerDiameterMm;

            return new SteelSectionProperties
            {
                AreaMm2 = area,
                IxMm4 = i,
                IyMm4 = i,
                WxMm3 = w,
                WyMm3 = w
            };
        }

        private static void ValidateIInputs(double h, double b, double tw, double tf)
        {
            if (h <= 0 || b <= 0) throw new ArgumentOutOfRangeException(nameof(h), "Dimensions must be positive.");
            if (tw <= 0 || tf <= 0) throw new ArgumentOutOfRangeException(nameof(tw), "Thicknesses must be positive.");
            if (h <= 2.0 * tf) throw new ArgumentOutOfRangeException(nameof(tf), "Height must exceed 2 * flange thickness.");
            if (b <= tw) throw new ArgumentOutOfRangeException(nameof(tw), "Width must exceed web thickness.");
        }

        private static void ValidateBoxInputs(double w, double h, double t)
        {
            if (w <= 0 || h <= 0 || t <= 0) throw new ArgumentOutOfRangeException(nameof(w), "Dimensions must be positive.");
            if (w <= 2.0 * t || h <= 2.0 * t) throw new ArgumentOutOfRangeException(nameof(t), "Dimensions must exceed 2 * thickness.");
        }

        private static void ValidatePipeInputs(double d, double t)
        {
            if (d <= 0 || t <= 0) throw new ArgumentOutOfRangeException(nameof(d), "Diameter and thickness must be positive.");
            if (d <= 2.0 * t) throw new ArgumentOutOfRangeException(nameof(t), "Diameter must exceed 2 * thickness.");
        }
    }
}
