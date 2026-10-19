using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPKata.Core.Walls.Models;
using HPKata.Revit.NativeRebar;
using HPKata.Revit.Units;

namespace HPKata.Revit.Services
{
    /// <summary>
    /// Generates native 3D parametric Rebar elements within Revit Structural Wall hosts (OST_Walls).
    /// </summary>
    public static class WallRebarGenerator
    {
        public static void CleanExistingWallRebar(Document doc, Element hostWall, string wallMark)
        {
            if (doc == null || hostWall == null) return;
            string prefix = $"HPKata_Wall_{wallMark}";

            var rebarsToDelete = new FilteredElementCollector(doc)
                .OfClass(typeof(Rebar))
                .Cast<Rebar>()
                .Where(r => r.GetHostId() == hostWall.Id)
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

        public static List<Rebar> GenerateWallRebar(
            Document doc, Wall hostWall, ShearWallModel model)
        {
            var createdRebars = new List<Rebar>();
            if (doc == null || hostWall == null || model == null) return createdRebars;
            if (hostWall.Location is not LocationCurve loc) return createdRebars;

            CleanExistingWallRebar(doc, hostWall, model.WallMark);

            XYZ p1 = loc.Curve.GetEndPoint(0);
            XYZ p2 = loc.Curve.GetEndPoint(1);
            XYZ wallDir = (p2 - p1).Normalize();
            XYZ normal = XYZ.BasisZ.CrossProduct(wallDir).Normalize();

            double wallLengthFt = loc.Curve.Length;
            double wallHeightFt = ResolveWallHeight(hostWall, model);
            double thicknessFt = hostWall.Width;

            string comment = $"HPKata_Wall_{model.WallMark}";
            string partition = model.WallMark;

            CreateBoundaryVerticalBars(doc, hostWall, model, p1, wallDir, normal, wallLengthFt, wallHeightFt, thicknessFt, comment, partition, createdRebars);
            CreateWebMeshBars(doc, hostWall, model, p1, wallDir, normal, wallLengthFt, wallHeightFt, thicknessFt, comment, partition, createdRebars);

            return createdRebars;
        }

        private static double ResolveWallHeight(Wall hostWall, ShearWallModel model)
        {
            var pHeight = hostWall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
            if (pHeight != null && pHeight.HasValue && pHeight.AsDouble() > 0)
            {
                return pHeight.AsDouble();
            }
            return RevitUnitConverter.MmToFeet(model.TotalHeightMm > 0.0 ? model.TotalHeightMm : 3600.0);
        }

        private static void CreateBoundaryVerticalBars(
            Document doc, Wall hostWall, ShearWallModel model,
            XYZ origin, XYZ wallDir, XYZ normal, double wallLenFt, double wallHtFt, double thickFt,
            string comment, string partition, List<Rebar> createdRebars)
        {
            if (model.BoundaryElements.Count == 0) return;
            var be = model.BoundaryElements[0];
            var barType = FramingRebarGenerator.ResolveBarType(doc, be.VerticalBarDiameterMm);
            if (barType == null) return;

            double coverFt = RevitUnitConverter.MmToFeet(model.ConcreteCoverMm);
            double offsetOutFt = (thickFt / 2.0) - coverFt;

            // Left boundary bar
            XYZ startLeft = origin + (wallDir * coverFt) + (normal * offsetOutFt);
            XYZ endLeft = startLeft + (XYZ.BasisZ * wallHtFt);
            IList<Curve> leftCurves = new List<Curve> { Line.CreateBound(startLeft, endLeft) };
            var rebarL = RebarCreationService.CreateRebar(doc, hostWall, barType, normal, leftCurves, partition, "BE-L", comment);
            createdRebars.Add(rebarL);

            // Right boundary bar
            XYZ startRight = origin + (wallDir * (wallLenFt - coverFt)) + (normal * offsetOutFt);
            XYZ endRight = startRight + (XYZ.BasisZ * wallHtFt);
            IList<Curve> rightCurves = new List<Curve> { Line.CreateBound(startRight, endRight) };
            var rebarR = RebarCreationService.CreateRebar(doc, hostWall, barType, normal, rightCurves, partition, "BE-R", comment);
            createdRebars.Add(rebarR);
        }

        private static void CreateWebMeshBars(
            Document doc, Wall hostWall, ShearWallModel model,
            XYZ origin, XYZ wallDir, XYZ normal, double wallLenFt, double wallHtFt, double thickFt,
            string comment, string partition, List<Rebar> createdRebars)
        {
            var web = model.WebReinforcement;
            var barType = FramingRebarGenerator.ResolveBarType(doc, web.VerticalBarDiameterMm);
            if (barType == null) return;

            double coverFt = RevitUnitConverter.MmToFeet(model.ConcreteCoverMm);
            double offsetFt = (thickFt / 2.0) - coverFt;
            double sVFt = RevitUnitConverter.MmToFeet(web.VerticalBarSpacingMm);

            for (double d = sVFt; d < wallLenFt - sVFt; d += sVFt)
            {
                XYZ vStart = origin + (wallDir * d) + (normal * offsetFt);
                XYZ vEnd = vStart + (XYZ.BasisZ * wallHtFt);
                IList<Curve> vCurves = new List<Curve> { Line.CreateBound(vStart, vEnd) };
                var rebar = RebarCreationService.CreateRebar(doc, hostWall, barType, normal, vCurves, partition, "VWeb", comment);
                createdRebars.Add(rebar);
            }
        }
    }
}
