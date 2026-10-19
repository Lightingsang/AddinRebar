using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPKata.Core.Beams.Models;
using HPKata.Revit.Abstractions;
using HPKata.Revit.Units;

namespace HPKata.Revit.Services
{
    /// <summary>
    /// Analyzes Revit Structural Framing elements, chains collinear beams bidirectionally,
    /// and detects support columns and span properties.
    /// </summary>
    public static class RevitFramingAnalyzer
    {
        public static List<FamilyInstance> FindCollinearFramingChain(
            Document doc, FamilyInstance seedBeam, double angleToleranceDeg = 1.0)
        {
            if (seedBeam.Location is not LocationCurve seedLoc) return new List<FamilyInstance> { seedBeam };

            ElementId levelId = seedBeam.LevelId;
            var allBeams = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(b => b.LevelId == levelId)
                .ToList();

            var beamMap = allBeams.ToDictionary(b => b.UniqueId);
            var segments = new List<FramingSegment>();
            FramingSegment? seedSegment = null;

            foreach (var beam in allBeams)
            {
                if (beam.Location is not LocationCurve loc) continue;
                XYZ p0 = loc.Curve.GetEndPoint(0);
                XYZ p1 = loc.Curve.GetEndPoint(1);
                var seg = new FramingSegment
                {
                    Id = beam.UniqueId,
                    StartPt = new RevitPoint3D(p0.X, p0.Y, p0.Z),
                    EndPt = new RevitPoint3D(p1.X, p1.Y, p1.Z)
                };
                segments.Add(seg);
                if (beam.Id == seedBeam.Id) seedSegment = seg;
            }

            if (seedSegment == null) return new List<FamilyInstance> { seedBeam };

            double touchTol = RevitUnitConverter.MmToFeet(50.0);
            var chainedSegments = FramingChainCalculator.ChainCollinearSegments(
                seedSegment, segments, angleToleranceDeg, touchTol);

            return chainedSegments.Select(s => beamMap[s.Id]).ToList();
        }

        public static BeamSpanModel CreateSpanModelFromFraming(
            FamilyInstance beam, int spanIndex, double defaultSupportWidthMm = 400.0)
        {
            if (beam.Location is not LocationCurve loc)
            {
                throw new ArgumentException("Beam does not have a valid LocationCurve.", nameof(beam));
            }

            double totalLengthFt = loc.Curve.Length;
            double totalLengthMm = RevitUnitConverter.FeetToMm(totalLengthFt);
            double clearSpanMm = Math.Max(500.0, totalLengthMm - defaultSupportWidthMm);

            double widthMm = 300.0;
            double heightMm = 600.0;

            var symbol = beam.Symbol;
            if (symbol != null)
            {
                var pB = symbol.get_Parameter(BuiltInParameter.STRUCTURAL_SECTION_COMMON_WIDTH);
                var pH = symbol.get_Parameter(BuiltInParameter.STRUCTURAL_SECTION_COMMON_HEIGHT);
                if (pB != null && pB.HasValue) widthMm = RevitUnitConverter.FeetToMm(pB.AsDouble());
                if (pH != null && pH.HasValue) heightMm = RevitUnitConverter.FeetToMm(pH.AsDouble());
            }

            return new BeamSpanModel
            {
                SpanIndex = spanIndex,
                SpanName = beam.Name,
                ClearLengthMm = Math.Round(clearSpanMm, 1),
                WidthMm = Math.Round(widthMm, 1),
                HeightMm = Math.Round(heightMm, 1),
                LeftSupportWidthMm = defaultSupportWidthMm,
                RightSupportWidthMm = defaultSupportWidthMm
            };
        }
    }
}
