using System;
using System.Collections.Generic;
using HPKata.Core.Calculators;
using HPKata.Core.Models;
using HPKata.Core.Stairs.Models;

namespace HPKata.Core.Stairs.Calculators
{
    /// <summary>
    /// Computes universal cross-section cuts (vemc), calculating concrete outlines,
    /// longitudinal rebar points, stirrup hoops, and associative dimensions.
    /// </summary>
    public static class GenericSectionCalculator
    {
        public static GenericSectionModel CreateRectangularSection(
            string compMark,
            double widthMm,
            double heightMm,
            double coverMm,
            int topBarCount,
            int topBarDia,
            int botBarCount,
            int botBarDia,
            int stirrupDia)
        {
            var sec = new GenericSectionModel
            {
                ComponentMark = compMark,
                SectionTitle = $"MẶT CẮT {compMark}",
                WidthMm = widthMm,
                HeightMm = heightMm,
                ConcreteCoverMm = coverMm
            };

            double innerMinX = coverMm + stirrupDia + (topBarDia / 2.0);
            double innerMaxX = widthMm - coverMm - stirrupDia - (topBarDia / 2.0);
            double topY = heightMm - coverMm - stirrupDia - (topBarDia / 2.0);
            double botY = coverMm + stirrupDia + (botBarDia / 2.0);

            AddTopBars(sec, topBarCount, topBarDia, widthMm, innerMinX, innerMaxX, topY);
            AddBottomBars(sec, botBarCount, botBarDia, widthMm, innerMinX, innerMaxX, botY);
            AddStirrupHoop(sec, stirrupDia, coverMm, widthMm, heightMm);

            return sec;
        }

        private static void AddTopBars(
            GenericSectionModel sec, int count, int dia, double width, double minX, double maxX, double y)
        {
            if (count == 1)
            {
                sec.LongitudinalBars.Add(new SectionBarPointModel { X = width / 2.0, Y = y, DiameterMm = dia, BarMark = "T1" });
            }
            else if (count > 1)
            {
                double step = (maxX - minX) / (count - 1);
                for (int i = 0; i < count; i++)
                {
                    sec.LongitudinalBars.Add(new SectionBarPointModel { X = minX + i * step, Y = y, DiameterMm = dia, BarMark = $"T{i + 1}" });
                }
            }
        }

        private static void AddBottomBars(
            GenericSectionModel sec, int count, int dia, double width, double minX, double maxX, double y)
        {
            if (count == 1)
            {
                sec.LongitudinalBars.Add(new SectionBarPointModel { X = width / 2.0, Y = y, DiameterMm = dia, BarMark = "B1" });
            }
            else if (count > 1)
            {
                double step = (maxX - minX) / (count - 1);
                for (int i = 0; i < count; i++)
                {
                    sec.LongitudinalBars.Add(new SectionBarPointModel { X = minX + i * step, Y = y, DiameterMm = dia, BarMark = $"B{i + 1}" });
                }
            }
        }

        private static void AddStirrupHoop(
            GenericSectionModel sec, int dia, double cover, double width, double height)
        {
            var hoop = new SectionHoopModel { DiameterMm = dia, Mark = "d1" };
            hoop.Vertices.Add((cover, cover));
            hoop.Vertices.Add((width - cover, cover));
            hoop.Vertices.Add((width - cover, height - cover));
            hoop.Vertices.Add((cover, height - cover));
            sec.StirrupHoops.Add(hoop);
        }

        public static List<RebarItem> SectionToRebarItems(GenericSectionModel sec, double lengthMm = 1000.0, int memberCount = 1)
        {
            if (sec == null) throw new ArgumentNullException(nameof(sec));
            var items = new List<RebarItem>();

            foreach (var b in sec.LongitudinalBars)
            {
                items.Add(new RebarItem
                {
                    ComponentMark = sec.ComponentMark,
                    BarMark = b.BarMark,
                    ShapeCode = "01",
                    DiameterMm = b.DiameterMm,
                    CountPerMember = 1,
                    MemberCount = memberCount,
                    CuttingLengthMm = lengthMm,
                    StructuralLengthMm = lengthMm,
                    UnitMassKgPerM = RebarCalculator.GetUnitMass(b.DiameterMm),
                    Notes = $"Thép dọc {sec.SectionTitle}"
                });
            }

            double bSt = Math.Max(50.0, sec.WidthMm - 2.0 * sec.ConcreteCoverMm);
            double hSt = Math.Max(50.0, sec.HeightMm - 2.0 * sec.ConcreteCoverMm);
            foreach (var h in sec.StirrupHoops)
            {
                double cutTie = 2.0 * (bSt + hSt) + 2.0 * RebarCalculator.Hook135Length(h.DiameterMm) - 8.0 * h.DiameterMm;
                items.Add(new RebarItem
                {
                    ComponentMark = sec.ComponentMark,
                    BarMark = h.Mark,
                    ShapeCode = "51",
                    DiameterMm = h.DiameterMm,
                    CountPerMember = (int)Math.Max(1, Math.Floor(lengthMm / 150.0)),
                    MemberCount = memberCount,
                    CuttingLengthMm = Math.Round(cutTie, 1),
                    StructuralLengthMm = Math.Round(cutTie, 1),
                    Spacing = "a150",
                    UnitMassKgPerM = RebarCalculator.GetUnitMass(h.DiameterMm),
                    Notes = $"Đai {sec.SectionTitle}"
                });
            }

            return items;
        }
    }
}
