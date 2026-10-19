using System;
using System.Collections.Generic;
using HPKata.Core.Calculators;
using HPKata.Core.Foundations.Calculators;
using HPKata.Core.Foundations.Models;
using HPKata.Core.Models;

namespace HPKata.Core.Foundations.Persistence
{
    /// <summary>
    /// Aggregates pad footing and pile cap rebar layouts into standard List of RebarItem records.
    /// </summary>
    public static class FoundationScheduleAggregator
    {
        public static List<RebarItem> AggregateFooting(IsolatedFootingModel footing)
        {
            if (footing == null) throw new ArgumentNullException(nameof(footing));
            footing.Validate();

            var items = new List<RebarItem>();
            string mark = footing.FootingMark;
            int members = footing.MemberCount;

            // 1. Bottom Mesh X (Shape 02)
            double lNet = Math.Max(200.0, footing.LengthMm - 2.0 * footing.CoverSideMm);
            double hookX = Math.Max(150.0, footing.BaseSlabHeightMm - footing.CoverBottomMm);
            double cutMeshX = lNet + 2.0 * hookX - 4.0 * footing.BottomMeshXDiaMm;
            int countX = (int)Math.Floor(Math.Max(1.0, footing.WidthMm - 2.0 * footing.CoverSideMm) / footing.BottomMeshXSpacingMm) + 1;
            items.Add(CreateItem(mark, "1", "02", footing.BottomMeshXDiaMm, countX, members, cutMeshX, $"a{footing.BottomMeshXSpacingMm:F0}", "Thép móng phương X"));

            // 2. Bottom Mesh Y (Shape 02)
            double bNet = Math.Max(200.0, footing.WidthMm - 2.0 * footing.CoverSideMm);
            double hookY = Math.Max(150.0, hookX - footing.BottomMeshXDiaMm);
            double cutMeshY = bNet + 2.0 * hookY - 4.0 * footing.BottomMeshYDiaMm;
            int countY = (int)Math.Floor(Math.Max(1.0, footing.LengthMm - 2.0 * footing.CoverSideMm) / footing.BottomMeshYSpacingMm) + 1;
            items.Add(CreateItem(mark, "2", "02", footing.BottomMeshYDiaMm, countY, members, cutMeshY, $"a{footing.BottomMeshYSpacingMm:F0}", "Thép móng phương Y"));

            // 3. Starter Dowels
            if (footing.StarterDowelCount > 0)
            {
                double leg = Math.Max(200.0, 12.0 * footing.StarterDowelDiaMm);
                double vert = footing.TotalHeightMm - footing.CoverBottomMm + footing.StarterDowelLapMm;
                double cutDowel = leg + vert - 2.0 * footing.StarterDowelDiaMm;
                items.Add(CreateItem(mark, "3", "02", footing.StarterDowelDiaMm, footing.StarterDowelCount, members, cutDowel, "-", "Thép chờ cổ móng"));
            }

            // 4. Pedestal Ties (Shape 51)
            double hPed = footing.PedestalHeightMm;
            if (hPed > 0 && footing.PedestalTieSpacingMm > 0)
            {
                double lcNet = Math.Max(100.0, footing.PedestalLengthMm - 2.0 * footing.CoverSideMm);
                double bcNet = Math.Max(100.0, footing.PedestalWidthMm - 2.0 * footing.CoverSideMm);
                double cutTie = 2.0 * (lcNet + bcNet) + 2.0 * RebarCalculator.Hook135Length(footing.PedestalTieDiaMm) - 8.0 * footing.PedestalTieDiaMm;
                int tieCount = Math.Max(2, (int)Math.Floor(hPed / footing.PedestalTieSpacingMm) + 1);
                items.Add(CreateItem(mark, "4", "51", footing.PedestalTieDiaMm, tieCount, members, cutTie, $"a{footing.PedestalTieSpacingMm:F0}", "Đai cổ móng"));
            }

            return items;
        }

        public static List<RebarItem> AggregatePileCap(PileCapModel cap)
        {
            return PileCapRebarCalculator.CalculatePileCapRebar(cap);
        }

        public static List<RebarItem> AggregateLayout(FoundationRebarLayout layout)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (layout.IsPileCap && layout.PileCap != null)
                return AggregatePileCap(layout.PileCap);
            if (layout.Footing != null)
                return AggregateFooting(layout.Footing);
            return new List<RebarItem>();
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
