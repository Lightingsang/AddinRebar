using System;
using System.Collections.Generic;

namespace HPKata.Core.QuantitySurveying.Calculators
{
    public sealed class PanelTilePiece
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double WidthMm { get; set; }
        public double HeightMm { get; set; }
        public bool IsFullPiece { get; set; }
    }

    public sealed class PanelTileLayoutResult
    {
        public int FullCount { get; set; }
        public int CutCount { get; set; }
        public int TotalPieces => FullCount + CutCount;
        public double GrossAreaM2 { get; set; }
        public double NetAreaM2 { get; set; }
        public double WasteRatio { get; set; }
        public List<PanelTilePiece> Pieces { get; set; } = new();
    }

    /// <summary>
    /// 2D formwork panel (chia_tam) and architectural tile (op_gach) layout optimizer.
    /// Computes full vs cut pieces, grid coordinates, and offcut waste percentage.
    /// </summary>
    public static class PanelTileLayoutCalculator
    {
        public static PanelTileLayoutResult OptimizeLayout(
            double roomWidthMm,
            double roomLengthMm,
            double unitWidthMm,
            double unitLengthMm,
            double jointGapMm = 0.0)
        {
            if (roomWidthMm <= 0) throw new ArgumentOutOfRangeException(nameof(roomWidthMm));
            if (roomLengthMm <= 0) throw new ArgumentOutOfRangeException(nameof(roomLengthMm));
            if (unitWidthMm <= 0) throw new ArgumentOutOfRangeException(nameof(unitWidthMm));
            if (unitLengthMm <= 0) throw new ArgumentOutOfRangeException(nameof(unitLengthMm));

            double stepX = unitWidthMm + Math.Max(0.0, jointGapMm);
            double stepY = unitLengthMm + Math.Max(0.0, jointGapMm);

            int cols = (int)Math.Ceiling(roomWidthMm / stepX);
            int rows = (int)Math.Ceiling(roomLengthMm / stepY);

            var result = new PanelTileLayoutResult
            {
                NetAreaM2 = (roomWidthMm * roomLengthMm) * 1e-6
            };

            PopulateGridPieces(result, roomWidthMm, roomLengthMm, unitWidthMm, unitLengthMm, stepX, stepY, cols, rows);

            double singleUnitAreaM2 = (unitWidthMm * unitLengthMm) * 1e-6;
            result.GrossAreaM2 = result.TotalPieces * singleUnitAreaM2;
            result.WasteRatio = result.GrossAreaM2 > 0
                ? Math.Max(0.0, (result.GrossAreaM2 - result.NetAreaM2) / result.GrossAreaM2)
                : 0.0;

            return result;
        }

        private static void PopulateGridPieces(
            PanelTileLayoutResult result,
            double roomW,
            double roomL,
            double unitW,
            double unitL,
            double stepX,
            double stepY,
            int cols,
            int rows)
        {
            for (int r = 0; r < rows; r++)
            {
                double y = r * stepY;
                double h = Math.Min(unitL, roomL - y);
                if (h <= 1e-3) continue;

                for (int c = 0; c < cols; c++)
                {
                    double x = c * stepX;
                    double w = Math.Min(unitW, roomW - x);
                    if (w <= 1e-3) continue;

                    bool isFull = Math.Abs(w - unitW) < 0.5 && Math.Abs(h - unitL) < 0.5;
                    if (isFull) result.FullCount++;
                    else result.CutCount++;

                    result.Pieces.Add(new PanelTilePiece
                    {
                        X = x,
                        Y = y,
                        WidthMm = w,
                        HeightMm = h,
                        IsFullPiece = isFull
                    });
                }
            }
        }
    }
}
