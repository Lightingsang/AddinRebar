using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPKata.Core.Columns.Models;
using HPKata.Revit.NativeRebar;
using HPKata.Revit.Units;

namespace HPKata.Revit.Services
{
    /// <summary>
    /// Generates native 3D parametric Rebar elements within Revit Structural Column hosts (OST_StructuralColumns).
    /// </summary>
    public static class ColumnRebarGenerator
    {
        public static void CleanExistingColumnRebar(Document doc, Element hostColumn, string columnMark)
        {
            if (doc == null || hostColumn == null) return;
            string prefix = $"HPKata_Column_{columnMark}";

            var rebarsToDelete = new FilteredElementCollector(doc)
                .OfClass(typeof(Rebar))
                .Cast<Rebar>()
                .Where(r => r.GetHostId() == hostColumn.Id)
                .Where(r =>
                {
                    var comment = r.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString();
                    return comment != null && comment.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
                })
                .Select(r => r.Id)
                .ToList();

            if (rebarsToDelete.Count > 0)
            {
                doc.Delete(rebarsToDelete);
            }
        }

        public static List<Rebar> GenerateColumnRebar(
            Document doc, FamilyInstance hostColumn, ColumnRebarLayout layout)
        {
            var createdRebars = new List<Rebar>();
            if (doc == null || hostColumn == null || layout == null) return createdRebars;

            CleanExistingColumnRebar(doc, hostColumn, layout.ColumnMark);

            var bbox = hostColumn.get_BoundingBox(null);
            XYZ center = bbox != null
                ? new XYZ((bbox.Min.X + bbox.Max.X) / 2.0, (bbox.Min.Y + bbox.Max.Y) / 2.0, bbox.Min.Z)
                : XYZ.Zero;

            double bottomZ = bbox != null ? bbox.Min.Z : 0.0;
            double topZ = bbox != null ? bbox.Max.Z : bottomZ + RevitUnitConverter.MmToFeet(3600.0);
            double heightFt = Math.Max(0.5, topZ - bottomZ);

            (double widthFt, double depthFt) = ResolveColumnDimensions(hostColumn, layout);
            string comment = $"HPKata_Column_{layout.ColumnMark}";
            string partition = layout.ColumnMark;

            CreateLongitudinalBars(doc, hostColumn, layout, center, bottomZ, heightFt, widthFt, depthFt, comment, partition, createdRebars);
            CreateColumnTies(doc, hostColumn, layout, center, bottomZ, heightFt, widthFt, depthFt, comment, partition, createdRebars);

            return createdRebars;
        }

        private static (double WidthFt, double DepthFt) ResolveColumnDimensions(
            FamilyInstance hostColumn, ColumnRebarLayout layout)
        {
            double widthMm = layout.Stories.Count > 0 ? layout.Stories[0].WidthMm : 400.0;
            double depthMm = layout.Stories.Count > 0 ? layout.Stories[0].DepthMm : 400.0;

            var symbol = hostColumn.Symbol;
            if (symbol != null)
            {
                var pB = symbol.get_Parameter(BuiltInParameter.STRUCTURAL_SECTION_COMMON_WIDTH);
                var pH = symbol.get_Parameter(BuiltInParameter.STRUCTURAL_SECTION_COMMON_HEIGHT);
                if (pB != null && pB.HasValue && pB.AsDouble() > 0) widthMm = RevitUnitConverter.FeetToMm(pB.AsDouble());
                if (pH != null && pH.HasValue && pH.AsDouble() > 0) depthMm = RevitUnitConverter.FeetToMm(pH.AsDouble());
            }

            return (RevitUnitConverter.MmToFeet(widthMm), RevitUnitConverter.MmToFeet(depthMm));
        }

        private static void CreateLongitudinalBars(
            Document doc, FamilyInstance hostColumn, ColumnRebarLayout layout,
            XYZ center, double bottomZ, double heightFt, double widthFt, double depthFt,
            string comment, string partition, List<Rebar> createdRebars)
        {
            if (layout.Stories.Count == 0) return;
            var sec = layout.Stories[0].Section;
            var barType = FramingRebarGenerator.ResolveBarType(doc, sec.CornerDiameterMm);
            if (barType == null) return;

            double coverFt = RevitUnitConverter.MmToFeet(layout.Stories[0].ConcreteCoverMm);
            double halfW = Math.Max(0.05, (widthFt / 2.0) - coverFt);
            double halfD = Math.Max(0.05, (depthFt / 2.0) - coverFt);

            XYZ[] cornerOffsets =
            {
                new(-halfW, -halfD, 0),
                new(halfW, -halfD, 0),
                new(halfW, halfD, 0),
                new(-halfW, halfD, 0)
            };

            for (int i = 0; i < cornerOffsets.Length; i++)
            {
                XYZ p1 = new(center.X + cornerOffsets[i].X, center.Y + cornerOffsets[i].Y, bottomZ);
                XYZ p2 = new(center.X + cornerOffsets[i].X, center.Y + cornerOffsets[i].Y, bottomZ + heightFt);

                IList<Curve> curves = new List<Curve> { Line.CreateBound(p1, p2) };
                var rebar = RebarCreationService.CreateRebar(doc, hostColumn, barType, XYZ.BasisX, curves, partition, $"{i + 1}", comment);
                createdRebars.Add(rebar);
            }
        }

        private static void CreateColumnTies(
            Document doc, FamilyInstance hostColumn, ColumnRebarLayout layout,
            XYZ center, double bottomZ, double heightFt, double widthFt, double depthFt,
            string comment, string partition, List<Rebar> createdRebars)
        {
            if (layout.Stories.Count == 0) return;
            var tieDist = layout.Stories[0].TieDistribution;
            var barType = FramingRebarGenerator.ResolveBarType(doc, tieDist.TieDiameterMm);
            if (barType == null) return;

            double coverFt = RevitUnitConverter.MmToFeet(layout.Stories[0].ConcreteCoverMm);
            double halfW = Math.Max(0.05, (widthFt / 2.0) - coverFt);
            double halfD = Math.Max(0.05, (depthFt / 2.0) - coverFt);

            double a1Ft = RevitUnitConverter.MmToFeet(tieDist.DenseSpacingMm);
            double a2Ft = RevitUnitConverter.MmToFeet(tieDist.SparseSpacingMm);
            double l0Ft = RevitUnitConverter.MmToFeet(tieDist.ConfinementHeightMm);

            for (double z = a1Ft; z < heightFt; z += (z < l0Ft || z > heightFt - l0Ft) ? a1Ft : a2Ft)
            {
                double currentZ = bottomZ + z;
                XYZ c1 = new(center.X - halfW, center.Y - halfD, currentZ);
                XYZ c2 = new(center.X + halfW, center.Y - halfD, currentZ);
                XYZ c3 = new(center.X + halfW, center.Y + halfD, currentZ);
                XYZ c4 = new(center.X - halfW, center.Y + halfD, currentZ);

                IList<Curve> tieCurves = new List<Curve>
                {
                    Line.CreateBound(c1, c2),
                    Line.CreateBound(c2, c3),
                    Line.CreateBound(c3, c4),
                    Line.CreateBound(c4, c1)
                };

                var tieRebar = RebarCreationService.CreateRebar(
                    doc, hostColumn, barType, XYZ.BasisZ, tieCurves, partition, "Tie", comment, RebarStyle.StirrupTie);
                createdRebars.Add(tieRebar);
            }
        }
    }
}
