using System;
using System.Collections.Generic;
using HPKata.Core.Calculators;
using HPKata.Core.Models;
using HPKata.Core.Walls.Calculators;
using HPKata.Core.Walls.Models;

namespace HPKata.Core.Walls.Persistence
{
    /// <summary>
    /// Aggregates shear wall reinforcement components into schedule items and steel summaries.
    /// </summary>
    public static class WallScheduleAggregator
    {
        public static List<RebarItem> AggregateSchedule(ShearWallModel wall, int memberCount = 1)
        {
            var items = new List<RebarItem>();
            if (wall == null) return items;

            int markCounter = 1;
            string compMark = string.IsNullOrWhiteSpace(wall.WallMark) ? "WALL" : wall.WallMark;
            double totalH = wall.TotalHeightMm > 0.0 ? wall.TotalHeightMm : 3600.0;

            AddBoundaryItems(items, compMark, ref markCounter, wall.BoundaryElements, totalH, memberCount);
            AddWebMeshItems(items, compMark, ref markCounter, wall, totalH, memberCount);
            AddOpeningItems(items, compMark, ref markCounter, wall.Openings, memberCount);

            return items;
        }

        private static void AddBoundaryItems(
            List<RebarItem> items, string compMark, ref int mark,
            IEnumerable<WallBoundaryElementModel> boundaryElements, double totalHeight, int count)
        {
            foreach (var be in boundaryElements)
            {
                items.Add(new RebarItem
                {
                    ComponentMark = $"{compMark}-{be.Mark}",
                    BarMark = mark++.ToString(),
                    ShapeCode = "01",
                    DiameterMm = be.VerticalBarDiameterMm,
                    CountPerMember = be.VerticalBarCount,
                    MemberCount = count,
                    CuttingLengthMm = Math.Round(totalHeight, 1),
                    StructuralLengthMm = totalHeight,
                    UnitMassKgPerM = RebarCalculator.GetUnitMass(be.VerticalBarDiameterMm)
                });

                int tieCount = be.TieSpacingMm > 0.0 ? (int)Math.Ceiling(totalHeight / be.TieSpacingMm) : 0;
                double perimeter = 2.0 * (be.LengthBbMm + be.ThicknessTbMm) + 150.0;
                items.Add(new RebarItem
                {
                    ComponentMark = $"{compMark}-{be.Mark}-Tie",
                    BarMark = mark++.ToString(),
                    ShapeCode = "51",
                    DiameterMm = be.TieDiameterMm,
                    CountPerMember = tieCount,
                    MemberCount = count,
                    CuttingLengthMm = Math.Round(perimeter, 1),
                    StructuralLengthMm = perimeter,
                    UnitMassKgPerM = RebarCalculator.GetUnitMass(be.TieDiameterMm)
                });
            }
        }

        private static void AddWebMeshItems(
            List<RebarItem> items, string compMark, ref int mark, ShearWallModel wall, double totalHeight, int count)
        {
            double bLeft = wall.BoundaryElements.Count > 0 ? wall.BoundaryElements[0].LengthBbMm : 400.0;
            double bRight = wall.BoundaryElements.Count > 1 ? wall.BoundaryElements[1].LengthBbMm : 400.0;

            var mesh = WallWebMeshCalculator.CalculateMesh(
                wall.LengthMm, totalHeight, wall.ThicknessMm, bLeft, bRight,
                wall.WebReinforcement.VerticalBarSpacingMm, wall.WebReinforcement.HorizontalBarSpacingMm,
                wall.WebReinforcement.VerticalBarDiameterMm, wall.ConcreteCoverMm);

            AddVerticalWebRebar(items, compMark, ref mark, wall, mesh, count);
            AddHorizontalWebRebar(items, compMark, ref mark, wall, mesh, count);
            AddCrossTieItem(items, compMark, ref mark, wall, mesh, count);
            AddUBarItems(items, compMark, ref mark, wall, mesh, count);
        }

        private static void AddVerticalWebRebar(
            List<RebarItem> items, string compMark, ref int mark, ShearWallModel wall, WallMeshSummary mesh, int count)
        {
            items.Add(new RebarItem
            {
                ComponentMark = $"{compMark}-VWeb",
                BarMark = mark++.ToString(),
                ShapeCode = "01",
                DiameterMm = wall.WebReinforcement.VerticalBarDiameterMm,
                CountPerMember = mesh.VerticalBarCount,
                MemberCount = count,
                CuttingLengthMm = Math.Round(mesh.VerticalBarLengthMm, 1),
                StructuralLengthMm = mesh.VerticalBarLengthMm,
                UnitMassKgPerM = RebarCalculator.GetUnitMass(wall.WebReinforcement.VerticalBarDiameterMm)
            });
        }

        private static void AddHorizontalWebRebar(
            List<RebarItem> items, string compMark, ref int mark, ShearWallModel wall, WallMeshSummary mesh, int count)
        {
            items.Add(new RebarItem
            {
                ComponentMark = $"{compMark}-HWeb",
                BarMark = mark++.ToString(),
                ShapeCode = "01",
                DiameterMm = wall.WebReinforcement.HorizontalBarDiameterMm,
                CountPerMember = mesh.HorizontalBarCount,
                MemberCount = count,
                CuttingLengthMm = Math.Round(mesh.HorizontalBarLengthMm, 1),
                StructuralLengthMm = mesh.HorizontalBarLengthMm,
                UnitMassKgPerM = RebarCalculator.GetUnitMass(wall.WebReinforcement.HorizontalBarDiameterMm)
            });
        }

        private static void AddCrossTieItem(
            List<RebarItem> items, string compMark, ref int mark, ShearWallModel wall, WallMeshSummary mesh, int count)
        {
            if (mesh.CrossTieCount <= 0) return;
            double tieLen = wall.ThicknessMm + 100.0;
            items.Add(new RebarItem
            {
                ComponentMark = $"{compMark}-CTie",
                BarMark = mark++.ToString(),
                ShapeCode = "11",
                DiameterMm = wall.WebReinforcement.CrossTieDiameterMm,
                CountPerMember = mesh.CrossTieCount,
                MemberCount = count,
                CuttingLengthMm = Math.Round(tieLen, 1),
                StructuralLengthMm = tieLen,
                UnitMassKgPerM = RebarCalculator.GetUnitMass(wall.WebReinforcement.CrossTieDiameterMm)
            });
        }

        private static void AddUBarItems(
            List<RebarItem> items, string compMark, ref int mark, ShearWallModel wall, WallMeshSummary mesh, int count)
        {
            if (mesh.UBarCount <= 0 || !wall.WebReinforcement.HasUBarEdgeCaps) return;

            int ubarDia = wall.WebReinforcement.UBarDiameterMm > 0
                ? wall.WebReinforcement.UBarDiameterMm
                : wall.WebReinforcement.HorizontalBarDiameterMm;

            items.Add(new RebarItem
            {
                ComponentMark = $"{compMark}-UBar",
                BarMark = mark++.ToString(),
                ShapeCode = "05",
                DiameterMm = ubarDia,
                CountPerMember = mesh.UBarCount,
                MemberCount = count,
                CuttingLengthMm = Math.Round(mesh.UBarLengthMm, 1),
                StructuralLengthMm = mesh.UBarLengthMm,
                UnitMassKgPerM = RebarCalculator.GetUnitMass(ubarDia)
            });
        }

        private static void AddOpeningItems(
            List<RebarItem> items, string compMark, ref int mark, IEnumerable<WallOpeningModel> openings, int count)
        {
            foreach (var op in openings)
            {
                var trimmers = WallOpeningTrimmerCalculator.CalculateTrimmers(op);
                AddDiagonalTrimmerItem(items, compMark, ref mark, op, trimmers, count);
                AddJambExtraItem(items, compMark, ref mark, op, trimmers, count);
                AddHeaderExtraItem(items, compMark, ref mark, op, trimmers, count);
            }
        }

        private static void AddDiagonalTrimmerItem(
            List<RebarItem> items, string compMark, ref int mark, WallOpeningModel op, OpeningTrimmerSummary trimmers, int count)
        {
            if (trimmers.DiagonalBarCount <= 0) return;
            items.Add(new RebarItem
            {
                ComponentMark = $"{compMark}-{op.OpeningMark}-Diag",
                BarMark = mark++.ToString(),
                ShapeCode = "01",
                DiameterMm = trimmers.DiagonalBarDiameterMm,
                CountPerMember = trimmers.DiagonalBarCount,
                MemberCount = count,
                CuttingLengthMm = trimmers.DiagonalBarLengthMm,
                StructuralLengthMm = trimmers.DiagonalBarLengthMm,
                UnitMassKgPerM = RebarCalculator.GetUnitMass(trimmers.DiagonalBarDiameterMm)
            });
        }

        private static void AddJambExtraItem(
            List<RebarItem> items, string compMark, ref int mark, WallOpeningModel op, OpeningTrimmerSummary trimmers, int count)
        {
            if (trimmers.JambExtraBarCount <= 0) return;
            items.Add(new RebarItem
            {
                ComponentMark = $"{compMark}-{op.OpeningMark}-Jamb",
                BarMark = mark++.ToString(),
                ShapeCode = "01",
                DiameterMm = trimmers.JambExtraBarDiameterMm,
                CountPerMember = trimmers.JambExtraBarCount,
                MemberCount = count,
                CuttingLengthMm = trimmers.JambExtraBarLengthMm,
                StructuralLengthMm = trimmers.JambExtraBarLengthMm,
                UnitMassKgPerM = RebarCalculator.GetUnitMass(trimmers.JambExtraBarDiameterMm)
            });
        }

        private static void AddHeaderExtraItem(
            List<RebarItem> items, string compMark, ref int mark, WallOpeningModel op, OpeningTrimmerSummary trimmers, int count)
        {
            if (trimmers.HeaderSillExtraBarCount <= 0) return;
            items.Add(new RebarItem
            {
                ComponentMark = $"{compMark}-{op.OpeningMark}-Header",
                BarMark = mark++.ToString(),
                ShapeCode = "01",
                DiameterMm = trimmers.HeaderSillExtraBarDiameterMm,
                CountPerMember = trimmers.HeaderSillExtraBarCount,
                MemberCount = count,
                CuttingLengthMm = trimmers.HeaderSillExtraBarLengthMm,
                StructuralLengthMm = trimmers.HeaderSillExtraBarLengthMm,
                UnitMassKgPerM = RebarCalculator.GetUnitMass(trimmers.HeaderSillExtraBarDiameterMm)
            });
        }
    }
}
