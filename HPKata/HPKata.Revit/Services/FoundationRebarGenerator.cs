using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPKata.Core.Foundations.Models;
using HPKata.Revit.NativeRebar;
using HPKata.Revit.Units;

namespace HPKata.Revit.Services
{
    /// <summary>
    /// Generates native 3D parametric Rebar elements within Revit Structural Foundation hosts (OST_StructuralFoundation).
    /// </summary>
    public static class FoundationRebarGenerator
    {
        public static void CleanExistingFoundationRebar(Document doc, Element hostFoundation, string foundationMark)
        {
            if (doc == null || hostFoundation == null) return;
            string prefix = $"HPKata_Foundation_{foundationMark}";

            var rebarsToDelete = new FilteredElementCollector(doc)
                .OfClass(typeof(Rebar))
                .Cast<Rebar>()
                .Where(r => r.GetHostId() == hostFoundation.Id)
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

        public static List<Rebar> GenerateFootingRebar(Document doc, FamilyInstance host, IsolatedFootingModel model)
        {
            var created = new List<Rebar>();
            if (doc == null || host == null || model == null) return created;

            CleanExistingFoundationRebar(doc, host, model.FootingMark);

            var bbox = host.get_BoundingBox(null);
            if (bbox == null) return created;

            double minX = bbox.Min.X;
            double maxX = bbox.Max.X;
            double minY = bbox.Min.Y;
            double maxY = bbox.Max.Y;
            double minZ = bbox.Min.Z;
            double maxZ = bbox.Max.Z;

            double covBotFt = RevitUnitConverter.MmToFeet(model.CoverBottomMm);
            double covSideFt = RevitUnitConverter.MmToFeet(model.CoverSideMm);
            double covTopFt = RevitUnitConverter.MmToFeet(model.CoverTopMm);

            string comment = $"HPKata_Foundation_{model.FootingMark}";
            string part = model.FootingMark;

            CreateFootingBottomMesh(doc, host, model, minX, maxX, minY, maxY, minZ, maxZ, covBotFt, covSideFt, covTopFt, comment, part, created);
            CreateFootingStarterDowels(doc, host, model, minX, maxX, minY, maxY, minZ, maxZ, covBotFt, comment, part, created);

            return created;
        }

        private static void CreateFootingBottomMesh(
            Document doc, FamilyInstance host, IsolatedFootingModel model,
            double minX, double maxX, double minY, double maxY, double minZ, double maxZ,
            double covBotFt, double covSideFt, double covTopFt, string comment, string part, List<Rebar> created)
        {
            var barTypeX = FramingRebarGenerator.ResolveBarType(doc, model.BottomMeshXDiaMm);
            var barTypeY = FramingRebarGenerator.ResolveBarType(doc, model.BottomMeshYDiaMm);
            if (barTypeX == null || barTypeY == null) return;

            double botZ = minZ + covBotFt;
            double hookZFt = Math.Max(botZ + RevitUnitConverter.MmToFeet(150.0), maxZ - covTopFt);

            CreateFootingMeshX(doc, host, barTypeX, minX, maxX, minY, maxY, botZ, hookZFt, covSideFt, model, comment, part, created);
            CreateFootingMeshY(doc, host, barTypeY, minX, maxX, minY, maxY, botZ, hookZFt, covSideFt, model, comment, part, created);
        }

        private static void CreateFootingMeshX(
            Document doc, FamilyInstance host, RebarBarType barTypeX,
            double minX, double maxX, double minY, double maxY, double botZ, double hookZFt,
            double covSideFt, IsolatedFootingModel model, string comment, string part, List<Rebar> created)
        {
            double spXFt = RevitUnitConverter.MmToFeet(model.BottomMeshXSpacingMm);
            for (double y = minY + covSideFt + spXFt / 2.0; y < maxY - covSideFt; y += spXFt)
            {
                XYZ p1 = new(minX + covSideFt, y, hookZFt);
                XYZ p2 = new(minX + covSideFt, y, botZ);
                XYZ p3 = new(maxX - covSideFt, y, botZ);
                XYZ p4 = new(maxX - covSideFt, y, hookZFt);

                IList<Curve> curves = new List<Curve> { Line.CreateBound(p1, p2), Line.CreateBound(p2, p3), Line.CreateBound(p3, p4) };
                var rebar = RebarCreationService.CreateRebar(doc, host, barTypeX, XYZ.BasisY, curves, part, "1", comment);
                created.Add(rebar);
            }
        }

        private static void CreateFootingMeshY(
            Document doc, FamilyInstance host, RebarBarType barTypeY,
            double minX, double maxX, double minY, double maxY, double botZ, double hookZFt,
            double covSideFt, IsolatedFootingModel model, string comment, string part, List<Rebar> created)
        {
            double spYFt = RevitUnitConverter.MmToFeet(model.BottomMeshYSpacingMm);
            double botY_Z = botZ + RevitUnitConverter.MmToFeet(model.BottomMeshXDiaMm);
            for (double x = minX + covSideFt + spYFt / 2.0; x < maxX - covSideFt; x += spYFt)
            {
                XYZ p1 = new(x, minY + covSideFt, hookZFt);
                XYZ p2 = new(x, minY + covSideFt, botY_Z);
                XYZ p3 = new(x, maxY - covSideFt, botY_Z);
                XYZ p4 = new(x, maxY - covSideFt, hookZFt);

                IList<Curve> curves = new List<Curve> { Line.CreateBound(p1, p2), Line.CreateBound(p2, p3), Line.CreateBound(p3, p4) };
                var rebar = RebarCreationService.CreateRebar(doc, host, barTypeY, XYZ.BasisX, curves, part, "2", comment);
                created.Add(rebar);
            }
        }

        private static void CreateFootingStarterDowels(
            Document doc, FamilyInstance host, IsolatedFootingModel model,
            double minX, double maxX, double minY, double maxY, double minZ, double maxZ,
            double covBotFt, string comment, string part, List<Rebar> created)
        {
            if (model.StarterDowelCount <= 0) return;
            var barType = FramingRebarGenerator.ResolveBarType(doc, model.StarterDowelDiaMm);
            if (barType == null) return;

            double midX = (minX + maxX) / 2.0;
            double midY = (minY + maxY) / 2.0;
            double botZ = minZ + covBotFt;
            double topZ = maxZ + RevitUnitConverter.MmToFeet(model.StarterDowelLapMm);
            double legFt = RevitUnitConverter.MmToFeet(Math.Max(200.0, 12.0 * model.StarterDowelDiaMm));

            XYZ[] offsets =
            {
                new(-legFt / 2.0, -legFt / 2.0, 0),
                new(legFt / 2.0, -legFt / 2.0, 0),
                new(legFt / 2.0, legFt / 2.0, 0),
                new(-legFt / 2.0, legFt / 2.0, 0)
            };

            for (int i = 0; i < Math.Min(model.StarterDowelCount, offsets.Length); i++)
            {
                XYZ pCorner = new(midX + offsets[i].X, midY + offsets[i].Y, botZ);
                XYZ pLeg = new(pCorner.X + (offsets[i].X > 0 ? legFt : -legFt), pCorner.Y, botZ);
                XYZ pTop = new(pCorner.X, pCorner.Y, topZ);

                IList<Curve> curves = new List<Curve>
                {
                    Line.CreateBound(pLeg, pCorner),
                    Line.CreateBound(pCorner, pTop)
                };
                var rebar = RebarCreationService.CreateRebar(doc, host, barType, XYZ.BasisY, curves, part, "3", comment);
                created.Add(rebar);
            }
        }
    }
}
