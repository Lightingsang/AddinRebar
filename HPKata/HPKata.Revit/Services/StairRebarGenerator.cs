using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPKata.Core.Stairs.Calculators;
using HPKata.Core.Stairs.Models;
using HPKata.Revit.NativeRebar;
using HPKata.Revit.Units;

namespace HPKata.Revit.Services
{
    /// <summary>
    /// Generates native 3D parametric Rebar elements within Revit Stair hosts (OST_Stairs),
    /// implementing re-entrant scissor crank bars and step nosing distribution steel.
    /// </summary>
    public static class StairRebarGenerator
    {
        public static void CleanExistingStairRebar(Document doc, Element hostStairs, string stairMark)
        {
            if (doc == null || hostStairs == null) return;
            string prefix = $"HPKata_Stair_{stairMark}";

            var rebarsToDelete = new FilteredElementCollector(doc)
                .OfClass(typeof(Rebar))
                .Cast<Rebar>()
                .Where(r => r.GetHostId() == hostStairs.Id)
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

        public static List<Rebar> GenerateStairRebar(Document doc, Element hostStairs, StairFlightModel flight)
        {
            var created = new List<Rebar>();
            if (doc == null || hostStairs == null || flight == null) return created;

            CleanExistingStairRebar(doc, hostStairs, flight.FlightMark);

            var bbox = hostStairs.get_BoundingBox(null);
            double originX = bbox != null ? bbox.Min.X : 0.0;
            double originY = bbox != null ? bbox.Min.Y : 0.0;
            double originZ = bbox != null ? bbox.Min.Z : 0.0;

            string comment = $"HPKata_Stair_{flight.FlightMark}";
            string part = flight.FlightMark;

            CreateBottomFlightBars(doc, hostStairs, flight, originX, originY, originZ, comment, part, created);
            CreateScissorLandingBars(doc, hostStairs, flight, originX, originY, originZ, comment, part, created);
            CreateStepNosingBars(doc, hostStairs, flight, originX, originY, originZ, comment, part, created);

            return created;
        }

        private static void CreateBottomFlightBars(
            Document doc, Element hostStairs, StairFlightModel flight,
            double ox, double oy, double oz, string comment, string part, List<Rebar> created)
        {
            var barType = FramingRebarGenerator.ResolveBarType(doc, flight.BottomTensionDiaMm);
            if (barType == null) return;

            double widthFt = RevitUnitConverter.MmToFeet(flight.FlightWidthMm);
            double covFt = RevitUnitConverter.MmToFeet(flight.ConcreteCoverMm);
            double spFt = RevitUnitConverter.MmToFeet(flight.BottomTensionSpacingMm);

            double l1Ft = RevitUnitConverter.MmToFeet(flight.BottomLandingLengthMm);
            double lFlightFt = RevitUnitConverter.MmToFeet(flight.FlightRunLengthMm);
            double hFlightFt = RevitUnitConverter.MmToFeet(flight.FlightTotalHeightMm);
            double lExtFt = RevitUnitConverter.MmToFeet(flight.ScissorCrankExtensionMm);

            for (double y = oy + covFt + spFt / 2.0; y < oy + widthFt - covFt; y += spFt)
            {
                XYZ pStart = new(ox, y, oz + covFt);
                XYZ pKneeLower = new(ox + l1Ft, y, oz + covFt);
                XYZ pKneeUpper = new(ox + l1Ft + lFlightFt, y, oz + hFlightFt);
                XYZ pExtTop = new(ox + l1Ft + lFlightFt + lExtFt, y, oz + hFlightFt + RevitUnitConverter.MmToFeet(flight.TopLandingThkMm) - covFt);

                IList<Curve> curves = new List<Curve>
                {
                    Line.CreateBound(pStart, pKneeLower),
                    Line.CreateBound(pKneeLower, pKneeUpper),
                    Line.CreateBound(pKneeUpper, pExtTop)
                };
                var rebar = RebarCreationService.CreateRebar(doc, hostStairs, barType, XYZ.BasisY, curves, part, "1", comment);
                created.Add(rebar);
            }
        }

        private static void CreateScissorLandingBars(
            Document doc, Element hostStairs, StairFlightModel flight,
            double ox, double oy, double oz, string comment, string part, List<Rebar> created)
        {
            var barType = FramingRebarGenerator.ResolveBarType(doc, flight.BottomTensionDiaMm);
            if (barType == null) return;

            double widthFt = RevitUnitConverter.MmToFeet(flight.FlightWidthMm);
            double covFt = RevitUnitConverter.MmToFeet(flight.ConcreteCoverMm);
            double spFt = RevitUnitConverter.MmToFeet(flight.BottomTensionSpacingMm);

            double l1Ft = RevitUnitConverter.MmToFeet(flight.BottomLandingLengthMm);
            double lFlightFt = RevitUnitConverter.MmToFeet(flight.FlightRunLengthMm);
            double l2Ft = RevitUnitConverter.MmToFeet(flight.TopLandingLengthMm);
            double hFlightFt = RevitUnitConverter.MmToFeet(flight.FlightTotalHeightMm);
            double lExtFt = RevitUnitConverter.MmToFeet(flight.ScissorCrankExtensionMm);

            for (double y = oy + covFt + spFt / 2.0; y < oy + widthFt - covFt; y += spFt)
            {
                XYZ pLandEnd = new(ox + l1Ft + lFlightFt + l2Ft, y, oz + hFlightFt);
                XYZ pKneeUpper = new(ox + l1Ft + lFlightFt, y, oz + hFlightFt);
                XYZ pExtDown = new(ox + l1Ft + lFlightFt - lExtFt * Math.Cos(flight.SlopeAngleRad), y, oz + hFlightFt - lExtFt * Math.Sin(flight.SlopeAngleRad));

                IList<Curve> curves = new List<Curve>
                {
                    Line.CreateBound(pLandEnd, pKneeUpper),
                    Line.CreateBound(pKneeUpper, pExtDown)
                };
                var rebar = RebarCreationService.CreateRebar(doc, hostStairs, barType, XYZ.BasisY, curves, part, "2", comment);
                created.Add(rebar);
            }
        }

        private static void CreateStepNosingBars(
            Document doc, Element hostStairs, StairFlightModel flight,
            double ox, double oy, double oz, string comment, string part, List<Rebar> created)
        {
            var barType = FramingRebarGenerator.ResolveBarType(doc, flight.StepNoseDiaMm);
            if (barType == null) return;

            double widthFt = RevitUnitConverter.MmToFeet(flight.FlightWidthMm);
            double covFt = RevitUnitConverter.MmToFeet(flight.ConcreteCoverMm);
            double l1Ft = RevitUnitConverter.MmToFeet(flight.BottomLandingLengthMm);
            double t1Ft = RevitUnitConverter.MmToFeet(flight.BottomLandingThkMm);

            for (int i = 1; i <= flight.RiserCount; i++)
            {
                double stepX = ox + l1Ft + (i - 1) * RevitUnitConverter.MmToFeet(flight.TreadWidthMm);
                double stepZ = oz + t1Ft + i * RevitUnitConverter.MmToFeet(flight.RiserHeightMm) - covFt;

                XYZ p1 = new(stepX + covFt, oy + covFt, stepZ);
                XYZ p2 = new(stepX + covFt, oy + widthFt - covFt, stepZ);

                IList<Curve> curves = new List<Curve> { Line.CreateBound(p1, p2) };
                var rebar = RebarCreationService.CreateRebar(doc, hostStairs, barType, XYZ.BasisX, curves, part, "5", comment);
                created.Add(rebar);
            }
        }
    }
}
