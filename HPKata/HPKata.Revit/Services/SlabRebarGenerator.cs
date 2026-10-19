using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPKata.Core.Slabs.Calculators;
using HPKata.Core.Slabs.Models;
using HPKata.Revit.NativeRebar;
using HPKata.Revit.Units;

namespace HPKata.Revit.Services
{
    /// <summary>
    /// Generates native 3D parametric Rebar elements within Revit Floor hosts (OST_Floors).
    /// </summary>
    public static class SlabRebarGenerator
    {
        public static void CleanExistingSlabRebar(Document doc, Element hostFloor, string slabMark)
        {
            if (doc == null || hostFloor == null) return;
            string prefix = $"HPKata_Slab_{slabMark}";

            var rebarsToDelete = new FilteredElementCollector(doc)
                .OfClass(typeof(Rebar))
                .Cast<Rebar>()
                .Where(r => r.GetHostId() == hostFloor.Id)
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

        public static List<Rebar> GenerateSlabRebar(Document doc, Floor hostFloor, SlabPanelModel model)
        {
            var created = new List<Rebar>();
            if (doc == null || hostFloor == null || model == null) return created;

            CleanExistingSlabRebar(doc, hostFloor, model.PanelMark);

            var bbox = hostFloor.get_BoundingBox(null);
            if (bbox == null) return created;

            double minX = bbox.Min.X;
            double maxX = bbox.Max.X;
            double minY = bbox.Min.Y;
            double maxY = bbox.Max.Y;
            double minZ = bbox.Min.Z;
            double maxZ = bbox.Max.Z;

            double covFt = RevitUnitConverter.MmToFeet(model.CoverMm);
            string comment = $"HPKata_Slab_{model.PanelMark}";
            string part = model.PanelMark;

            CreateSlabBottomMesh(doc, hostFloor, model, minX, maxX, minY, maxY, minZ, covFt, comment, part, created);
            CreateSlabTopSupportBars(doc, hostFloor, model, minX, maxX, minY, maxY, maxZ, covFt, comment, part, created);

            return created;
        }

        private static void CreateSlabBottomMesh(
            Document doc, Floor hostFloor, SlabPanelModel model,
            double minX, double maxX, double minY, double maxY, double minZ, double covFt,
            string comment, string part, List<Rebar> created)
        {
            var barTypeX = FramingRebarGenerator.ResolveBarType(doc, model.BottomMeshXDiaMm);
            var barTypeY = FramingRebarGenerator.ResolveBarType(doc, model.BottomMeshYDiaMm);
            if (barTypeX == null || barTypeY == null) return;

            double spXFt = RevitUnitConverter.MmToFeet(model.BottomMeshXSpacingMm);
            double spYFt = RevitUnitConverter.MmToFeet(model.BottomMeshYSpacingMm);
            double botZ = minZ + covFt;

            // X bars
            for (double y = minY + covFt + spXFt / 2.0; y < maxY - covFt; y += spXFt)
            {
                XYZ p1 = new(minX + covFt, y, botZ);
                XYZ p2 = new(maxX - covFt, y, botZ);
                IList<Curve> curves = new List<Curve> { Line.CreateBound(p1, p2) };
                var rebar = RebarCreationService.CreateRebar(doc, hostFloor, barTypeX, XYZ.BasisY, curves, part, "B1", comment);
                created.Add(rebar);
            }

            // Y bars
            double botY_Z = botZ + RevitUnitConverter.MmToFeet(model.BottomMeshXDiaMm);
            for (double x = minX + covFt + spYFt / 2.0; x < maxX - covFt; x += spYFt)
            {
                XYZ p1 = new(x, minY + covFt, botY_Z);
                XYZ p2 = new(x, maxY - covFt, botY_Z);
                IList<Curve> curves = new List<Curve> { Line.CreateBound(p1, p2) };
                var rebar = RebarCreationService.CreateRebar(doc, hostFloor, barTypeY, XYZ.BasisX, curves, part, "B2", comment);
                created.Add(rebar);
            }
        }

        private static void CreateSlabTopSupportBars(
            Document doc, Floor hostFloor, SlabPanelModel model,
            double minX, double maxX, double minY, double maxY, double maxZ, double covFt,
            string comment, string part, List<Rebar> created)
        {
            var barType = FramingRebarGenerator.ResolveBarType(doc, model.TopSupportDiaMm);
            if (barType == null) return;

            double topZ = maxZ - covFt;
            double cogDownFt = RevitUnitConverter.MmToFeet(Math.Max(50.0, model.ThicknessMm - 2.0 * model.CoverMm - 10.0));
            double lExtFt = RevitUnitConverter.MmToFeet(Math.Max(model.SpanXMm * model.TopSupportRatio, 40.0 * model.TopSupportDiaMm));
            double spFt = RevitUnitConverter.MmToFeet(model.TopSupportSpacingMm);

            // Left support top bars
            for (double y = minY + covFt + spFt / 2.0; y < maxY - covFt; y += spFt)
            {
                XYZ p1 = new(minX + covFt + lExtFt, y, topZ - cogDownFt);
                XYZ p2 = new(minX + covFt + lExtFt, y, topZ);
                XYZ p3 = new(minX + covFt, y, topZ);
                XYZ p4 = new(minX + covFt, y, topZ - cogDownFt);

                IList<Curve> curves = new List<Curve>
                {
                    Line.CreateBound(p1, p2),
                    Line.CreateBound(p2, p3),
                    Line.CreateBound(p3, p4)
                };
                var rebar = RebarCreationService.CreateRebar(doc, hostFloor, barType, XYZ.BasisY, curves, part, "T1", comment);
                created.Add(rebar);
            }
        }
    }
}
