using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPKata.Core.Beams.Calculators;
using HPKata.Core.Beams.Models;
using HPKata.Revit.Abstractions;
using HPKata.Revit.NativeRebar;
using HPKata.Revit.Units;

namespace HPKata.Revit.Services
{
    /// <summary>
    /// Generates native 3D parametric Rebar elements within Revit Structural Framing hosts.
    /// Ensures idempotency by purging previously generated rebar instances on re-run.
    /// </summary>
    public static class FramingRebarGenerator
    {
        public static void CleanExistingFramingRebar(Document doc, Element hostBeam, string beamName)
        {
            if (doc == null || hostBeam == null) return;
            string prefix = $"HPKata_Beam_{beamName}";

            var rebarsToDelete = new FilteredElementCollector(doc)
                .OfClass(typeof(Rebar))
                .Cast<Rebar>()
                .Where(r => r.GetHostId() == hostBeam.Id)
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

        public static RebarBarType? ResolveBarType(Document doc, int diameterMm)
        {
            double targetFt = RevitUnitConverter.MmToFeet(diameterMm);

            var existing = new FilteredElementCollector(doc)
                .OfClass(typeof(RebarBarType))
                .Cast<RebarBarType>()
                .FirstOrDefault(t => RevitUnitConverter.AreCloseFeet(t.BarModelDiameter, targetFt, RevitUnitConverter.MmToFeet(1.0)));

            if (existing != null) return existing;

            var firstType = new FilteredElementCollector(doc)
                .OfClass(typeof(RebarBarType))
                .Cast<RebarBarType>()
                .FirstOrDefault();

            if (firstType != null)
            {
                var newType = (RebarBarType)firstType.Duplicate($"Ø{diameterMm} (HPKata)");
                newType.BarModelDiameter = targetFt;
                return newType;
            }

            return null;
        }

        public static List<Rebar> GenerateFramingRebar(
            Document doc, FamilyInstance hostBeam, BeamRebarLayout layout)
        {
            var createdRebars = new List<Rebar>();
            if (hostBeam.Location is not LocationCurve loc) return createdRebars;

            CleanExistingFramingRebar(doc, hostBeam, layout.BeamName);

            XYZ startPt = loc.Curve.GetEndPoint(0);
            XYZ endPt = loc.Curve.GetEndPoint(1);
            XYZ beamDir = (endPt - startPt).Normalize();
            XYZ normal = XYZ.BasisZ.CrossProduct(beamDir).Normalize();

            (double widthFt, double heightFt) = ResolveBeamDimensions(hostBeam, layout);
            string comment = $"HPKata_Beam_{layout.BeamName}";
            string partition = layout.BeamName;

            CreateLongitudinalBars(doc, hostBeam, layout, startPt, endPt, normal, widthFt, heightFt, comment, partition, createdRebars);
            CreateStirrups(doc, hostBeam, layout, startPt, beamDir, normal, widthFt, heightFt, comment, partition, createdRebars);

            return createdRebars;
        }

        private static (double WidthFt, double HeightFt) ResolveBeamDimensions(
            FamilyInstance hostBeam, BeamRebarLayout layout)
        {
            double widthMm = layout.Spans.Count > 0 && layout.Spans[0].WidthMm > 0 ? layout.Spans[0].WidthMm : 0.0;
            double heightMm = layout.Spans.Count > 0 && layout.Spans[0].HeightMm > 0 ? layout.Spans[0].HeightMm : 0.0;

            if (widthMm <= 0 || heightMm <= 0)
            {
                var symbol = hostBeam.Symbol;
                if (symbol != null)
                {
                    var pB = symbol.get_Parameter(BuiltInParameter.STRUCTURAL_SECTION_COMMON_WIDTH);
                    var pH = symbol.get_Parameter(BuiltInParameter.STRUCTURAL_SECTION_COMMON_HEIGHT);
                    if (widthMm <= 0 && pB != null && pB.HasValue && pB.AsDouble() > 0)
                        widthMm = RevitUnitConverter.FeetToMm(pB.AsDouble());
                    if (heightMm <= 0 && pH != null && pH.HasValue && pH.AsDouble() > 0)
                        heightMm = RevitUnitConverter.FeetToMm(pH.AsDouble());
                }
            }

            if (widthMm <= 0) widthMm = 300.0;
            if (heightMm <= 0) heightMm = 600.0;

            return (RevitUnitConverter.MmToFeet(widthMm), RevitUnitConverter.MmToFeet(heightMm));
        }

        private static void CreateLongitudinalBars(
            Document doc, FamilyInstance hostBeam, BeamRebarLayout layout,
            XYZ startPt, XYZ endPt, XYZ normal, double widthFt, double heightFt,
            string comment, string partition, List<Rebar> createdRebars)
        {
            double coverFt = RevitUnitConverter.MmToFeet(30.0);
            double clearWidthFt = Math.Max(0.1, widthFt - (2.0 * coverFt));

            CreateTopBars(doc, hostBeam, layout, startPt, endPt, normal, clearWidthFt, coverFt, comment, partition, createdRebars);
            CreateBottomBars(doc, hostBeam, layout, startPt, endPt, normal, clearWidthFt, heightFt, coverFt, comment, partition, createdRebars);
        }

        private static void CreateTopBars(
            Document doc, FamilyInstance hostBeam, BeamRebarLayout layout,
            XYZ startPt, XYZ endPt, XYZ normal, double clearWidthFt, double coverFt,
            string comment, string partition, List<Rebar> createdRebars)
        {
            if (layout.TopContinuousBars.Count == 0) return;
            var topRun = layout.TopContinuousBars[0];
            var barType = ResolveBarType(doc, topRun.DiameterMm);
            if (barType == null) return;

            var offsets = FramingRebarGeometryCalculator.CalculateTransverseOffsets(clearWidthFt, topRun.BarCount);
            foreach (double offset in offsets)
            {
                var (s, e) = FramingRebarGeometryCalculator.CalculateTopBarEndpoints(ToPt(startPt), ToPt(endPt), ToPt(normal), offset, coverFt);
                IList<Curve> curves = new List<Curve> { Line.CreateBound(ToXyz(s), ToXyz(e)) };
                var rebar = RebarCreationService.CreateRebar(doc, hostBeam, barType, normal, curves, partition, "1", comment);
                createdRebars.Add(rebar);
            }
        }

        private static void CreateBottomBars(
            Document doc, FamilyInstance hostBeam, BeamRebarLayout layout,
            XYZ startPt, XYZ endPt, XYZ normal, double clearWidthFt, double heightFt, double coverFt,
            string comment, string partition, List<Rebar> createdRebars)
        {
            if (layout.BottomContinuousBars.Count == 0) return;
            var botRun = layout.BottomContinuousBars[0];
            var barType = ResolveBarType(doc, botRun.DiameterMm);
            if (barType == null) return;

            var offsets = FramingRebarGeometryCalculator.CalculateTransverseOffsets(clearWidthFt, botRun.BarCount);
            foreach (double offset in offsets)
            {
                var (s, e) = FramingRebarGeometryCalculator.CalculateBottomBarEndpoints(ToPt(startPt), ToPt(endPt), ToPt(normal), offset, heightFt, coverFt);
                IList<Curve> curves = new List<Curve> { Line.CreateBound(ToXyz(s), ToXyz(e)) };
                var rebar = RebarCreationService.CreateRebar(doc, hostBeam, barType, normal, curves, partition, "2", comment);
                createdRebars.Add(rebar);
            }
        }

        private static void CreateStirrups(
            Document doc, FamilyInstance hostBeam, BeamRebarLayout layout,
            XYZ startPt, XYZ beamDir, XYZ normal, double widthFt, double heightFt,
            string comment, string partition, List<Rebar> createdRebars)
        {
            double coverStirrupFt = RevitUnitConverter.MmToFeet(25.0);
            for (int s = 0; s < layout.Spans.Count; s++)
            {
                var span = layout.Spans[s];
                if (!layout.StirrupZones.TryGetValue(s, out var zone))
                {
                    zone = BeamStirrupZoneCalculator.CalculateZones(s, span.ClearLengthMm, span.WidthMm, span.HeightMm);
                }

                var barType = ResolveBarType(doc, zone.StirrupDiameterMm);
                if (barType == null) continue;

                var stationsMm = BeamStirrupZoneCalculator.GenerateStirrupStations(span.ClearLengthMm, zone);
                foreach (double xMm in stationsMm)
                {
                    double xFt = RevitUnitConverter.MmToFeet(xMm);
                    XYZ stationCenter = startPt + (beamDir * xFt);
                    var corners = FramingRebarGeometryCalculator.CalculateStirrupCorners(
                        ToPt(stationCenter), ToPt(normal), widthFt, heightFt, coverStirrupFt);

                    IList<Curve> stirrupCurves = new List<Curve>
                    {
                        Line.CreateBound(ToXyz(corners[0]), ToXyz(corners[1])),
                        Line.CreateBound(ToXyz(corners[1]), ToXyz(corners[2])),
                        Line.CreateBound(ToXyz(corners[2]), ToXyz(corners[3])),
                        Line.CreateBound(ToXyz(corners[3]), ToXyz(corners[0]))
                    };

                    var stirrup = RebarCreationService.CreateRebar(
                        doc, hostBeam, barType, beamDir, stirrupCurves, partition, "22", comment, RebarStyle.StirrupTie);
                    createdRebars.Add(stirrup);
                }
            }
        }

        private static RevitPoint3D ToPt(XYZ p) => new(p.X, p.Y, p.Z);
        private static XYZ ToXyz(RevitPoint3D p) => new(p.X, p.Y, p.Z);
    }
}
