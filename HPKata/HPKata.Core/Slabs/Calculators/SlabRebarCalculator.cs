using System;
using System.Collections.Generic;
using HPKata.Core.Calculators;
using HPKata.Core.Models;
using HPKata.Core.Slabs.Models;

namespace HPKata.Core.Slabs.Calculators
{
    /// <summary>
    /// Computes two-way slab reinforcement cutting lengths, bar counts, and placement arrays
    /// for bottom continuous mesh, top support negative bars, and cantilever projections.
    /// </summary>
    public static class SlabRebarCalculator
    {
        public static List<RebarItem> CalculateSlabRebar(SlabPanelModel panel)
        {
            if (panel == null) throw new ArgumentNullException(nameof(panel));
            panel.Validate();

            var items = new List<RebarItem>();
            string mark = panel.PanelMark;
            int members = panel.MemberCount;

            // 1. Bottom Mesh X (Layer B1 along short span)
            double ancX1 = Math.Max(15.0 * panel.BottomMeshXDiaMm, panel.SupportWidthLeftMm - panel.CoverMm);
            double ancX2 = Math.Max(15.0 * panel.BottomMeshXDiaMm, panel.SupportWidthRightMm - panel.CoverMm);
            double cogX = Math.Max(50.0, panel.ThicknessMm - 2.0 * panel.CoverMm);
            double cutMeshX = panel.SpanXMm + ancX1 + ancX2 + 2.0 * cogX - 4.0 * panel.BottomMeshXDiaMm;
            int countX = (int)Math.Floor(Math.Max(1.0, panel.SpanYMm - 2.0 * panel.CoverMm) / panel.BottomMeshXSpacingMm) + 1;
            items.Add(CreateItem(mark, "1", "02", panel.BottomMeshXDiaMm, countX, members, cutMeshX, $"a{panel.BottomMeshXSpacingMm:F0}", "Thép sàn lớp dưới phương X"));

            // 2. Bottom Mesh Y (Layer B2 along long span)
            double ancY1 = Math.Max(15.0 * panel.BottomMeshYDiaMm, panel.SupportWidthBottomMm - panel.CoverMm);
            double ancY2 = Math.Max(15.0 * panel.BottomMeshYDiaMm, panel.SupportWidthTopMm - panel.CoverMm);
            double cogY = Math.Max(50.0, cogX - panel.BottomMeshXDiaMm);
            double cutMeshY = panel.SpanYMm + ancY1 + ancY2 + 2.0 * cogY - 4.0 * panel.BottomMeshYDiaMm;
            int countY = (int)Math.Floor(Math.Max(1.0, panel.SpanXMm - 2.0 * panel.CoverMm) / panel.BottomMeshYSpacingMm) + 1;
            items.Add(CreateItem(mark, "2", "02", panel.BottomMeshYDiaMm, countY, members, cutMeshY, $"a{panel.BottomMeshYSpacingMm:F0}", "Thép sàn lớp dưới phương Y"));

            AddTopNegativeBars(items, panel, members);
            AddCantileverAndCornerBars(items, panel, members);

            return items;
        }

        private static void AddTopNegativeBars(List<RebarItem> items, SlabPanelModel panel, int members)
        {
            double cogDown = Math.Max(50.0, panel.ThicknessMm - 2.0 * panel.CoverMm - 10.0);
            double lExtX = Math.Max(panel.SpanXMm * panel.TopSupportRatio, 40.0 * panel.TopSupportDiaMm);
            double cutTopX = 2.0 * lExtX + panel.SupportWidthLeftMm + 2.0 * cogDown - 4.0 * panel.TopSupportDiaMm;
            int countTopX = (int)Math.Floor(Math.Max(1.0, panel.SpanYMm - 2.0 * panel.CoverMm) / panel.TopSupportSpacingMm) + 1;
            items.Add(CreateItem(panel.PanelMark, "3", "02", panel.TopSupportDiaMm, countTopX, members, cutTopX, $"a{panel.TopSupportSpacingMm:F0}", "Thép mũ gối phương X"));

            double lExtY = Math.Max(panel.SpanYMm * panel.TopSupportRatio, 40.0 * panel.TopSupportDiaMm);
            double cutTopY = 2.0 * lExtY + panel.SupportWidthBottomMm + 2.0 * cogDown - 4.0 * panel.TopSupportDiaMm;
            int countTopY = (int)Math.Floor(Math.Max(1.0, panel.SpanXMm - 2.0 * panel.CoverMm) / panel.TopSupportSpacingMm) + 1;
            items.Add(CreateItem(panel.PanelMark, "4", "02", panel.TopSupportDiaMm, countTopY, members, cutTopY, $"a{panel.TopSupportSpacingMm:F0}", "Thép mũ gối phương Y"));
        }

        private static void AddCantileverAndCornerBars(List<RebarItem> items, SlabPanelModel panel, int members)
        {
            if (panel.IsCantilever && panel.CantileverLengthMm > 0)
            {
                double lCant = panel.CantileverLengthMm - panel.CoverMm;
                double edgeDown = Math.Max(50.0, panel.ThicknessMm - 2.0 * panel.CoverMm);
                double backSpan = Math.Max(1.5 * panel.CantileverLengthMm, 40.0 * panel.TopSupportDiaMm);
                double cutCant = lCant + edgeDown + 150.0 + backSpan - 2.0 * panel.TopSupportDiaMm;
                int countCant = (int)Math.Floor(Math.Max(1.0, panel.SpanYMm - 2.0 * panel.CoverMm) / panel.TopSupportSpacingMm) + 1;
                items.Add(CreateItem(panel.PanelMark, "5", "05", panel.TopSupportDiaMm, countCant, members, cutCant, $"a{panel.TopSupportSpacingMm:F0}", "Thép mũ công xôn"));
            }

            if (panel.HasCornerCrackBars)
            {
                double cornerZone = Math.Min(panel.SpanXMm, panel.SpanYMm) / 5.0;
                double cutCorner = Math.Sqrt(2.0) * cornerZone;
                int countCorner = Math.Max(4, (int)Math.Floor(cornerZone / panel.CornerBarSpacingMm) * 4);
                items.Add(CreateItem(panel.PanelMark, "6", "01", panel.CornerBarDiaMm, countCorner, members, cutCorner, $"a{panel.CornerBarSpacingMm:F0}", "Thép gia cường góc sàn"));
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
