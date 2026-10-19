using System;
using System.Collections.Generic;
using HPKata.Revit.Abstractions;

namespace HPKata.Revit.Services
{
    /// <summary>
    /// Pure geometry and coordinate calculation service for Revit framing reinforcement.
    /// Fully decoupled from Revit Document transactions to enable headless verification.
    /// </summary>
    public static class FramingRebarGeometryCalculator
    {
        public static (RevitPoint3D Start, RevitPoint3D End) CalculateTopBarEndpoints(
            RevitPoint3D startPt, RevitPoint3D endPt, RevitPoint3D normal, double offsetFt, double coverFt)
        {
            RevitPoint3D zOffset = RevitPoint3D.BasisZ * -coverFt;
            RevitPoint3D transverseOffset = normal * offsetFt;
            return (startPt + zOffset + transverseOffset, endPt + zOffset + transverseOffset);
        }

        public static (RevitPoint3D Start, RevitPoint3D End) CalculateBottomBarEndpoints(
            RevitPoint3D startPt, RevitPoint3D endPt, RevitPoint3D normal, double offsetFt, double heightFt, double coverFt)
        {
            RevitPoint3D zOffset = RevitPoint3D.BasisZ * (-heightFt + coverFt);
            RevitPoint3D transverseOffset = normal * offsetFt;
            return (startPt + zOffset + transverseOffset, endPt + zOffset + transverseOffset);
        }

        public static List<double> CalculateTransverseOffsets(double clearWidthFt, int barCount)
        {
            var offsets = new List<double>();
            if (barCount <= 0)
            {
                return offsets;
            }
            if (barCount == 1)
            {
                offsets.Add(0.0);
                return offsets;
            }

            double halfWidth = clearWidthFt / 2.0;
            double step = clearWidthFt / (barCount - 1);
            for (int i = 0; i < barCount; i++)
            {
                offsets.Add(-halfWidth + (i * step));
            }
            return offsets;
        }

        public static List<RevitPoint3D> CalculateStirrupCorners(
            RevitPoint3D stationCenter, RevitPoint3D normal, double widthFt, double heightFt, double coverFt)
        {
            if (widthFt <= 2.0 * coverFt || heightFt <= 2.0 * coverFt)
            {
                return new List<RevitPoint3D>();
            }

            double halfW = (widthFt / 2.0) - coverFt;
            double topZ = -coverFt;
            double botZ = -heightFt + coverFt;

            RevitPoint3D tl = stationCenter + (normal * -halfW) + (RevitPoint3D.BasisZ * topZ);
            RevitPoint3D tr = stationCenter + (normal * halfW) + (RevitPoint3D.BasisZ * topZ);
            RevitPoint3D br = stationCenter + (normal * halfW) + (RevitPoint3D.BasisZ * botZ);
            RevitPoint3D bl = stationCenter + (normal * -halfW) + (RevitPoint3D.BasisZ * botZ);

            return new List<RevitPoint3D> { tl, tr, br, bl };
        }
    }
}
