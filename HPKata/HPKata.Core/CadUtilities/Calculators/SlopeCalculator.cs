using System;
using HPKata.Core.CadUtilities.Models;

namespace HPKata.Core.CadUtilities.Calculators
{
    /// <summary>
    /// Calculates slope/gradient percentage, ratio, and quadrant angle normalization
    /// ensuring text is always legible and never upside-down.
    /// </summary>
    public static class SlopeCalculator
    {
        public static SlopeAnnotationModel Calculate(Point2D start, Point2D end)
        {
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double absDx = Math.Abs(dx);
            double absDy = Math.Abs(dy);

            var model = new SlopeAnnotationModel
            {
                StartPoint = start,
                EndPoint = end
            };

            if (absDx < 1e-6)
            {
                model.IsVertical = true;
                model.GradientPercent = double.PositiveInfinity;
                model.RatioText = "1:0";
                model.FormattedText = "Đứng";
                model.TextAngleRad = Math.PI / 2.0;
                return model;
            }

            double percent = (absDy / absDx) * 100.0;
            model.GradientPercent = Math.Round(percent, 2);

            if (absDy > 1e-6)
            {
                double ratioN = Math.Round(absDx / absDy, 2);
                model.RatioText = $"1:{ratioN:0.##}";
            }
            else
            {
                model.RatioText = "Bằng";
            }

            model.FormattedText = $"i = {model.GradientPercent:0.##}%";
            model.TextAngleRad = NormalizeTextAngle(Math.Atan2(dy, dx));

            return model;
        }

        public static double NormalizeTextAngle(double angleRad)
        {
            // Normalize to [0, 2pi)
            double a = angleRad % (2.0 * Math.PI);
            if (a < 0.0) a += 2.0 * Math.PI;

            // If angle is pointing into quadrants 2 or 3 (between pi/2 and 3pi/2), flip by pi (180 deg)
            if (a > Math.PI / 2.0 && a <= 3.0 * Math.PI / 2.0)
            {
                a += Math.PI;
            }

            return a % (2.0 * Math.PI);
        }
    }
}
