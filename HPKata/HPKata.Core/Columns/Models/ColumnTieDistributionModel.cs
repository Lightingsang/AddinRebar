using System;

namespace HPKata.Core.Columns.Models
{
    public enum ColumnInnerHoopType
    {
        None,
        CrossTie,
        Rectangular,
        Diamond
    }

    /// <summary>
    /// Encapsulates perimeter ties, confinement dense/sparse zones, and internal tie configurations.
    /// </summary>
    public sealed class ColumnTieDistributionModel
    {
        public int TieDiameterMm { get; set; } = 8;
        public double DenseSpacingMm { get; set; } = 100.0;
        public double SparseSpacingMm { get; set; } = 200.0;
        public double JointSpacingMm { get; set; } = 150.0;
        public double ConfinementHeightMm { get; set; } = 500.0;

        public ColumnInnerHoopType InnerHoopType { get; set; } = ColumnInnerHoopType.CrossTie;
        public int CrossTieDiameterMm { get; set; } = 8;
        public int CrossTieCountPerSet { get; set; } = 2;

        public int CalculateTotalSets(double clearHeightMm, double beamDepthMm)
        {
            if (clearHeightMm <= 0.0 || DenseSpacingMm <= 0.0 || SparseSpacingMm <= 0.0)
                return 0;

            double l0 = Math.Min(ConfinementHeightMm, clearHeightMm / 2.0);
            double midHeight = Math.Max(0.0, clearHeightMm - 2.0 * l0);

            int bottomSets = (int)Math.Ceiling(l0 / DenseSpacingMm);
            int topSets = (int)Math.Ceiling(l0 / DenseSpacingMm);
            int midSets = midHeight > 0.0 ? (int)Math.Ceiling(midHeight / SparseSpacingMm) : 0;
            int jointSets = beamDepthMm > 0.0 ? (int)Math.Ceiling(beamDepthMm / JointSpacingMm) : 0;

            return bottomSets + topSets + midSets + jointSets;
        }
    }
}
