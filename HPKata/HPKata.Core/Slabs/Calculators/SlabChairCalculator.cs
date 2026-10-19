using System;
using HPKata.Core.Calculators;
using HPKata.Core.Models;
using HPKata.Core.Slabs.Models;

namespace HPKata.Core.Slabs.Calculators
{
    /// <summary>
    /// Computes slab chair bar stool heights, segment dimensions, and quantity takeoff
    /// based on slab thickness, concrete covers, and bar diameters.
    /// </summary>
    public static class SlabChairCalculator
    {
        public static SlabChairStoolModel CalculateStool(
            double slabThicknessMm,
            double topCoverMm = 15.0,
            double bottomCoverMm = 15.0,
            int topMeshDiaMm = 10,
            int bottomMeshDiaMm = 10,
            int chairDiaMm = 10,
            double densityPerM2 = 1.2,
            double slabAreaM2 = 20.0,
            string shapeCode = "35")
        {
            if (slabThicknessMm <= 0) throw new ArgumentOutOfRangeException(nameof(slabThicknessMm));

            double rawHeight = slabThicknessMm - topCoverMm - bottomCoverMm - (2.0 * topMeshDiaMm) - (2.0 * bottomMeshDiaMm);
            double clampedHeight = Math.Max(30.0, rawHeight);
            double roundedHeight = Math.Round(clampedHeight / 5.0) * 5.0;

            double footLen = Math.Max(150.0, 15.0 * chairDiaMm);
            double saddleLen = Math.Max(150.0, 200.0);
            int count = Math.Max(1, (int)Math.Ceiling(slabAreaM2 * (densityPerM2 > 0 ? densityPerM2 : 1.2)));

            return new SlabChairStoolModel
            {
                SlabThicknessMm = slabThicknessMm,
                TopCoverMm = topCoverMm,
                BottomCoverMm = bottomCoverMm,
                TopMeshDiaMm = topMeshDiaMm,
                BottomMeshDiaMm = bottomMeshDiaMm,
                ChairDiaMm = chairDiaMm,
                StoolHeightMm = roundedHeight,
                FootLengthMm = footLen,
                SaddleLengthMm = saddleLen,
                DensityPerM2 = densityPerM2,
                TotalCount = count,
                ShapeCode = shapeCode
            };
        }

        public static RebarItem CreateChairRebarItem(string componentMark, SlabChairStoolModel stool, int memberCount = 1)
        {
            if (stool == null) throw new ArgumentNullException(nameof(stool));

            return new RebarItem
            {
                ComponentMark = componentMark,
                BarMark = "CK",
                ShapeCode = stool.ShapeCode,
                DiameterMm = stool.ChairDiaMm,
                CountPerMember = stool.TotalCount,
                MemberCount = memberCount,
                CuttingLengthMm = Math.Round(stool.CuttingLengthMm, 1),
                StructuralLengthMm = Math.Round(stool.CuttingLengthMm, 1),
                Spacing = $"{stool.DensityPerM2:F1} cái/m2",
                UnitMassKgPerM = RebarCalculator.GetUnitMass(stool.ChairDiaMm),
                Notes = $"Chân chó kê sàn H={stool.StoolHeightMm:F0}mm"
            };
        }
    }
}
