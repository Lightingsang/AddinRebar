using System;
using System.Collections.Generic;
using HPKata.Core.Foundations.Models;

namespace HPKata.Core.Foundations.Calculators
{
    /// <summary>
    /// Generates canonical symmetric pile coordinates for 1 to 9+ piles centered at (0, 0)
    /// adhering to TCVN 10304 spacing constraints (s >= 3D, edge distance >= 1.0D).
    /// </summary>
    public static class PileCoordinateGenerator
    {
        public static (List<PileCoordinate> Piles, double RecommendedLengthMm, double RecommendedWidthMm)
            GenerateLayout(int pileCount, double pileDiaMm = 400.0, double? customSpacingMm = null, double? customEdgeDistMm = null, bool isSquare = false)
        {
            if (pileCount < 1) throw new ArgumentOutOfRangeException(nameof(pileCount), "Pile count must be at least 1.");
            double d = pileDiaMm > 0 ? pileDiaMm : 400.0;
            double s = customSpacingMm ?? (3.0 * d);
            double ce = customEdgeDistMm ?? Math.Max(250.0, 1.0 * d);

            var piles = new List<PileCoordinate>();
            double recL, recB;

            if (pileCount <= 5)
            {
                GenerateSmallGroupPiles(pileCount, s, d, ce, isSquare, piles, out recL, out recB);
            }
            else if (pileCount <= 9)
            {
                GenerateMediumGroupPiles(pileCount, s, d, ce, isSquare, piles, out recL, out recB);
            }
            else
            {
                GenerateArbitraryGridPiles(pileCount, s, d, ce, isSquare, piles, out recL, out recB);
            }

            return (piles, recL, recB);
        }

        private static void GenerateSmallGroupPiles(
            int count, double s, double d, double ce, bool isSquare, List<PileCoordinate> piles, out double recL, out double recB)
        {
            recL = recB = d + 2.0 * ce;
            switch (count)
            {
                case 1:
                    piles.Add(new PileCoordinate(1, 0, 0, d, isSquare));
                    break;
                case 2:
                    piles.Add(new PileCoordinate(1, -s / 2.0, 0, d, isSquare));
                    piles.Add(new PileCoordinate(2, s / 2.0, 0, d, isSquare));
                    recL = s + d + 2.0 * ce;
                    break;
                case 3:
                    piles.Add(new PileCoordinate(1, 0, s / Math.Sqrt(3.0), d, isSquare));
                    piles.Add(new PileCoordinate(2, -s / 2.0, -s / (2.0 * Math.Sqrt(3.0)), d, isSquare));
                    piles.Add(new PileCoordinate(3, s / 2.0, -s / (2.0 * Math.Sqrt(3.0)), d, isSquare));
                    recL = s + d + 2.0 * ce;
                    recB = (Math.Sqrt(3.0) / 2.0) * s + d + 2.0 * ce;
                    break;
                case 4:
                    AddGridPiles(piles, new[] { -s / 2.0, s / 2.0 }, new[] { -s / 2.0, s / 2.0 }, d, isSquare);
                    recL = recB = s + d + 2.0 * ce;
                    break;
                case 5:
                    piles.Add(new PileCoordinate(1, 0, 0, d, isSquare));
                    AddGridPiles(piles, new[] { -s / 2.0, s / 2.0 }, new[] { -s / 2.0, s / 2.0 }, d, isSquare, 2);
                    recL = recB = s + d + 2.0 * ce;
                    break;
            }
        }

        private static void GenerateMediumGroupPiles(
            int count, double s, double d, double ce, bool isSquare, List<PileCoordinate> piles, out double recL, out double recB)
        {
            recL = recB = 2.0 * s + d + 2.0 * ce;
            switch (count)
            {
                case 6:
                    AddGridPiles(piles, new[] { -s / 2.0, s / 2.0 }, new[] { -s, 0.0, s }, d, isSquare);
                    recL = 2.0 * s + d + 2.0 * ce;
                    recB = s + d + 2.0 * ce;
                    break;
                case 7:
                    piles.Add(new PileCoordinate(1, 0, 0, d, isSquare));
                    for (int k = 0; k < 6; k++)
                    {
                        double ang = k * Math.PI / 3.0;
                        piles.Add(new PileCoordinate(k + 2, s * Math.Cos(ang), s * Math.Sin(ang), d, isSquare));
                    }
                    break;
                case 8:
                    AddGridPiles(piles, new[] { -s / 2.0, s / 2.0 }, new[] { -1.5 * s, -0.5 * s, 0.5 * s, 1.5 * s }, d, isSquare);
                    recL = 3.0 * s + d + 2.0 * ce;
                    recB = s + d + 2.0 * ce;
                    break;
                case 9:
                    AddGridPiles(piles, new[] { -s, 0.0, s }, new[] { -s, 0.0, s }, d, isSquare);
                    break;
            }
        }

        private static void GenerateArbitraryGridPiles(
            int pileCount, double s, double d, double ce, bool isSquare, List<PileCoordinate> piles, out double recL, out double recB)
        {
            int nx = (int)Math.Ceiling(Math.Sqrt(pileCount));
            int ny = (int)Math.Ceiling((double)pileCount / nx);
            recL = (nx - 1) * s + d + 2.0 * ce;
            recB = (ny - 1) * s + d + 2.0 * ce;

            int idx = 1;
            double startX = -((nx - 1) * s) / 2.0;
            double startY = -((ny - 1) * s) / 2.0;

            for (int r = 0; r < ny && idx <= pileCount; r++)
            {
                for (int c = 0; c < nx && idx <= pileCount; c++)
                {
                    piles.Add(new PileCoordinate(idx++, startX + c * s, startY + r * s, d, isSquare));
                }
            }
        }

        private static void AddGridPiles(List<PileCoordinate> piles, double[] xs, double[] ys, double d, bool isSquare, int startIdx = 1)
        {
            int idx = startIdx;
            foreach (double y in ys)
            {
                foreach (double x in xs)
                {
                    piles.Add(new PileCoordinate(idx++, x, y, d, isSquare));
                }
            }
        }
    }
}
