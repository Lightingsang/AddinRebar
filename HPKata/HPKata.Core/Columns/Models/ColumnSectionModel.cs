using System;
using System.Collections.Generic;

namespace HPKata.Core.Columns.Models
{
    /// <summary>
    /// Represents column cross-section dimensions, bar arrangements, and spatial coordinates.
    /// </summary>
    public sealed class ColumnSectionModel
    {
        public double WidthMm { get; set; } = 400.0;
        public double DepthMm { get; set; } = 400.0;

        public int CornerDiameterMm { get; set; } = 20;
        public int CornerBarCount => 4;

        public int SideCountB { get; set; } = 1;
        public int SideDiameterB { get; set; } = 18;

        public int SideCountH { get; set; } = 1;
        public int SideDiameterH { get; set; } = 18;

        public bool IsCompliant { get; set; } = true;

        public int TotalBarCount => 4 + 2 * SideCountB + 2 * SideCountH;

        public double TotalSteelAreaMm2 =>
            4.0 * CalculateBarArea(CornerDiameterMm) +
            2.0 * SideCountB * CalculateBarArea(SideDiameterB) +
            2.0 * SideCountH * CalculateBarArea(SideDiameterH);

        public double ReinforcementRatioPercent =>
            (WidthMm > 0.0 && DepthMm > 0.0) ? (TotalSteelAreaMm2 / (WidthMm * DepthMm) * 100.0) : 0.0;

        public static double CalculateBarArea(int diameterMm) =>
            Math.PI * diameterMm * diameterMm / 4.0;

        public List<(double X, double Y, int Diameter)> GetBarCoordinates(double coverMm, double tieDiaMm)
        {
            var bars = new List<(double X, double Y, int Diameter)>();
            double rc = CornerDiameterMm / 2.0;
            double coreX = WidthMm / 2.0 - coverMm - tieDiaMm - rc;
            double coreY = DepthMm / 2.0 - coverMm - tieDiaMm - rc;

            if (coreX <= 0.0 || coreY <= 0.0) return bars;

            AddCornerBars(bars, coreX, coreY);
            AddSideBarsAlongB(bars, coreX, coreY);
            AddSideBarsAlongH(bars, coreX, coreY);

            return bars;
        }

        public double CalculateMinClearSpacingMm(double coverMm, double tieDiaMm)
        {
            var coords = GetBarCoordinates(coverMm, tieDiaMm);
            if (coords.Count < 2) return 0.0;

            double minClear = double.MaxValue;
            for (int i = 0; i < coords.Count; i++)
            {
                for (int j = i + 1; j < coords.Count; j++)
                {
                    double dx = coords[i].X - coords[j].X;
                    double dy = coords[i].Y - coords[j].Y;
                    double centerDist = Math.Sqrt(dx * dx + dy * dy);
                    double clear = centerDist - (coords[i].Diameter + coords[j].Diameter) / 2.0;
                    if (clear < minClear) minClear = clear;
                }
            }
            return Math.Round(minClear, 1);
        }

        public bool ValidateClearSpacing(double coverMm, double tieDiaMm, double minClearMm = 50.0) =>
            CalculateMinClearSpacingMm(coverMm, tieDiaMm) >= minClearMm;

        private void AddCornerBars(List<(double X, double Y, int Diameter)> bars, double cx, double cy)
        {
            bars.Add((-cx, -cy, CornerDiameterMm));
            bars.Add((cx, -cy, CornerDiameterMm));
            bars.Add((cx, cy, CornerDiameterMm));
            bars.Add((-cx, cy, CornerDiameterMm));
        }

        private void AddSideBarsAlongB(List<(double X, double Y, int Diameter)> bars, double cx, double cy)
        {
            if (SideCountB <= 0) return;
            double step = (2.0 * cx) / (SideCountB + 1);
            for (int i = 1; i <= SideCountB; i++)
            {
                double x = -cx + i * step;
                bars.Add((x, -cy, SideDiameterB));
                bars.Add((x, cy, SideDiameterB));
            }
        }

        private void AddSideBarsAlongH(List<(double X, double Y, int Diameter)> bars, double cx, double cy)
        {
            if (SideCountH <= 0) return;
            double step = (2.0 * cy) / (SideCountH + 1);
            for (int j = 1; j <= SideCountH; j++)
            {
                double y = -cy + j * step;
                bars.Add((-cx, y, SideDiameterH));
                bars.Add((cx, y, SideDiameterH));
            }
        }
    }
}
