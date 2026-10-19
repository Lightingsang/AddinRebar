using System;
using System.Collections.Generic;
using HPKata.Core.Calculators;
using HPKata.Core.Foundations.Models;
using HPKata.Core.Models;

namespace HPKata.Core.Foundations.Calculators
{
    /// <summary>
    /// Computes cutting lengths, bar counts, and weight takeoff for pile cap and pad footing rebar cages.
    /// </summary>
    public static class PileCapRebarCalculator
    {
        public static List<RebarItem> CalculatePileCapRebar(PileCapModel cap)
        {
            if (cap == null) throw new ArgumentNullException(nameof(cap));
            cap.Validate();

            var items = new List<RebarItem>();
            string mark = cap.CapMark;
            int members = cap.MemberCount;

            // 1. Bottom Mesh X (Shape 02)
            double lNet = Math.Max(200.0, cap.LengthMm - 2.0 * cap.CoverSideMm);
            double hookX = Math.Max(150.0, cap.TotalHeightMm - cap.CoverBottomMm - cap.CoverTopMm);
            double cutMeshX = lNet + 2.0 * hookX - 4.0 * cap.BottomMeshXDiaMm;
            int countX = (int)Math.Floor(Math.Max(1.0, cap.WidthMm - 2.0 * cap.CoverSideMm) / cap.BottomMeshXSpacingMm) + 1;
            items.Add(CreateItem(mark, "1", "02", cap.BottomMeshXDiaMm, countX, members, cutMeshX, $"a{cap.BottomMeshXSpacingMm:F0}", "Thép đáy phương X"));

            // 2. Bottom Mesh Y (Shape 02)
            double bNet = Math.Max(200.0, cap.WidthMm - 2.0 * cap.CoverSideMm);
            double hookY = Math.Max(150.0, hookX - cap.BottomMeshXDiaMm);
            double cutMeshY = bNet + 2.0 * hookY - 4.0 * cap.BottomMeshYDiaMm;
            int countY = (int)Math.Floor(Math.Max(1.0, cap.LengthMm - 2.0 * cap.CoverSideMm) / cap.BottomMeshYSpacingMm) + 1;
            items.Add(CreateItem(mark, "2", "02", cap.BottomMeshYDiaMm, countY, members, cutMeshY, $"a{cap.BottomMeshYSpacingMm:F0}", "Thép đáy phương Y"));

            AddTopMeshAndSkinTies(items, cap, lNet, bNet, hookX, hookY, members);
            AddDowelsAndPileTies(items, cap, members);

            return items;
        }

        private static void AddTopMeshAndSkinTies(
            List<RebarItem> items, PileCapModel cap, double lNet, double bNet, double hookX, double hookY, int members)
        {
            if (cap.HasTopMesh)
            {
                double cutTopX = lNet + 2.0 * hookX - 4.0 * cap.TopMeshXDiaMm;
                int countTopX = (int)Math.Floor(Math.Max(1.0, cap.WidthMm - 2.0 * cap.CoverSideMm) / cap.TopMeshXSpacingMm) + 1;
                items.Add(CreateItem(cap.CapMark, "3", "02", cap.TopMeshXDiaMm, countTopX, members, cutTopX, $"a{cap.TopMeshXSpacingMm:F0}", "Thép trên phương X"));

                double cutTopY = bNet + 2.0 * hookY - 4.0 * cap.TopMeshYDiaMm;
                int countTopY = (int)Math.Floor(Math.Max(1.0, cap.LengthMm - 2.0 * cap.CoverSideMm) / cap.TopMeshYSpacingMm) + 1;
                items.Add(CreateItem(cap.CapMark, "4", "02", cap.TopMeshYDiaMm, countTopY, members, cutTopY, $"a{cap.TopMeshYSpacingMm:F0}", "Thép trên phương Y"));
            }

            if (cap.HasSkinTies && cap.TotalHeightMm >= 600.0)
            {
                double perimeter = 2.0 * (lNet + bNet);
                double cutSkin = perimeter + 2.0 * RebarCalculator.Hook135Length(cap.SkinTieDiaMm) - 8.0 * cap.SkinTieDiaMm;
                int tieLevels = Math.Max(1, (int)Math.Floor((cap.TotalHeightMm - cap.CoverBottomMm - cap.CoverTopMm) / cap.SkinTieSpacingMm));
                items.Add(CreateItem(cap.CapMark, "5", "51", cap.SkinTieDiaMm, tieLevels, members, cutSkin, $"a{cap.SkinTieSpacingMm:F0}", "Đai thành đài"));
            }
        }

        private static void AddDowelsAndPileTies(List<RebarItem> items, PileCapModel cap, int members)
        {
            if (cap.StarterDowelCount > 0)
            {
                double leg = Math.Max(200.0, 12.0 * cap.StarterDowelDiaMm);
                double vert = cap.TotalHeightMm - cap.CoverBottomMm + cap.StarterDowelLapMm;
                double cutDowel = leg + vert - 2.0 * cap.StarterDowelDiaMm;
                items.Add(CreateItem(cap.CapMark, "6", "02", cap.StarterDowelDiaMm, cap.StarterDowelCount, members, cutDowel, "-", "Thép chờ cột"));
            }

            if (cap.Piles.Count > 0 && cap.PileConfinementCount > 0)
            {
                double tieLen = Math.PI * (cap.PileDiameterMm + 50.0) + 150.0;
                int totalTies = cap.Piles.Count * cap.PileConfinementCount;
                items.Add(CreateItem(cap.CapMark, "7", "74", cap.PileConfinementDiaMm, totalTies, members, tieLen, "3 đai/cọc", "Đai đầu cọc"));
            }
        }

        private static RebarItem CreateItem(
            string compMark, string barMark, string shape, int dia, int count, int members, double cutLen, string spacing, string notes)
        {
            return new RebarItem
            {
                ComponentMark = compMark,
                BarMark = barMark,
                ShapeCode = shape,
                DiameterMm = dia,
                CountPerMember = count,
                MemberCount = members,
                CuttingLengthMm = Math.Round(cutLen, 1),
                StructuralLengthMm = Math.Round(cutLen, 1),
                Spacing = spacing,
                UnitMassKgPerM = RebarCalculator.GetUnitMass(dia),
                Notes = notes
            };
        }
    }
}
