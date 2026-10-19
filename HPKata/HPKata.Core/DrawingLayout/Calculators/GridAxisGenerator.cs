using System;
using System.Collections.Generic;
using HPKata.Core.DrawingLayout.Models;

namespace HPKata.Core.DrawingLayout.Calculators
{
    /// <summary>
    /// Generates architectural/structural grid axis coordinates, bubble callout positions,
    /// and ISO 128 compliant alphanumeric labels.
    /// </summary>
    public static class GridAxisGenerator
    {
        // ISO 128 / TCVN standard sequence skipping confusing characters: I, O, Q, Z
        private static readonly string[] IsoLetters =
        {
            "A", "B", "C", "D", "E", "F", "G", "H", "J",
            "K", "L", "M", "N", "P", "R", "S", "T", "U", "V", "W", "X", "Y"
        };

        public static string GetIsoLabel(int index)
        {
            if (index < 0) return string.Empty;
            if (index < IsoLetters.Length) return IsoLetters[index];
            int repeat = index / IsoLetters.Length + 1;
            int rem = index % IsoLetters.Length;
            return $"{IsoLetters[rem]}{repeat}";
        }

        public static GridSystemModel GenerateOrthogonal(
            IReadOnlyList<double> xSpansMm,
            IReadOnlyList<double> ySpansMm,
            double scale = 100.0)
        {
            if (xSpansMm == null) throw new ArgumentNullException(nameof(xSpansMm));
            if (ySpansMm == null) throw new ArgumentNullException(nameof(ySpansMm));

            var system = new GridSystemModel
            {
                Scale = scale,
                ExtensionLengthMm = 15.0 * scale,
                DimensionOffsetMm = 10.0 * scale,
                OverallDimensionOffsetMm = 8.0 * scale,
                BubbleRadiusMm = 4.5 * scale,
                TextHeightMm = 3.5 * scale
            };

            var xCoords = AccumulateCoordinates(xSpansMm);
            var yCoords = AccumulateCoordinates(ySpansMm);

            double minY = yCoords[0];
            double maxY = yCoords[yCoords.Count - 1];
            double minX = xCoords[0];
            double maxX = xCoords[xCoords.Count - 1];

            // Vertical axes (numbered 1, 2, 3...)
            for (int i = 0; i < xCoords.Count; i++)
            {
                double x = xCoords[i];
                var axis = new GridLineModel((i + 1).ToString(), x, minY - system.ExtensionLengthMm, x, maxY + system.ExtensionLengthMm)
                {
                    BubbleRadiusMm = system.BubbleRadiusMm,
                    TextHeightMm = system.TextHeightMm
                };
                system.YAxes.Add(axis);
            }

            // Horizontal axes (lettered A, B, C...)
            for (int j = 0; j < yCoords.Count; j++)
            {
                double y = yCoords[j];
                var axis = new GridLineModel(GetIsoLabel(j), minX - system.ExtensionLengthMm, y, maxX + system.ExtensionLengthMm, y)
                {
                    BubbleRadiusMm = system.BubbleRadiusMm,
                    TextHeightMm = system.TextHeightMm
                };
                system.XAxes.Add(axis);
            }

            return system;
        }

        public static GridLineModel GenerateRadialRay(
            double originX,
            double originY,
            double radiusStartMm,
            double radiusEndMm,
            double angleRad,
            string label)
        {
            double x1 = originX + radiusStartMm * Math.Cos(angleRad);
            double y1 = originY + radiusStartMm * Math.Sin(angleRad);
            double x2 = originX + radiusEndMm * Math.Cos(angleRad);
            double y2 = originY + radiusEndMm * Math.Sin(angleRad);

            return new GridLineModel(label, x1, y1, x2, y2)
            {
                IsCustomAngle = true,
                AngleRad = angleRad
            };
        }

        private static List<double> AccumulateCoordinates(IReadOnlyList<double> spans)
        {
            var coords = new List<double> { 0.0 };
            double current = 0.0;
            foreach (var span in spans)
            {
                current += span;
                coords.Add(current);
            }
            return coords;
        }
    }
}
