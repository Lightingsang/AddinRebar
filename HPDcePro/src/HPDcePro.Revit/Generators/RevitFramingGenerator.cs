using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Revit.Generators
{
    public sealed record BeamItemDef(
        string Mark,
        Point3D StartMm,
        Point3D EndMm,
        double WidthMm,
        double HeightMm,
        string LevelName,
        double ZOffsetMm = 0.0);

    public sealed record BeamPlanItem(
        string Mark,
        Point3D StartFeet,
        Point3D EndFeet,
        double WidthMm,
        double HeightMm,
        string LevelName,
        double ZOffsetFeet,
        double LengthMm,
        string FamilySymbolName);

    public sealed record BeamCreationPlan(
        IReadOnlyList<BeamPlanItem> Items,
        double TotalLengthM,
        double TotalConcreteVolumeM3);

    public sealed record ContinuousBeamDef(
        string Mark,
        Point3D StartMm,
        Point3D EndMm,
        double WidthMm,
        double HeightMm,
        string LevelName);

    public sealed record BeamBreakResultItem(
        string SubMark,
        Point3D StartMm,
        Point3D EndMm,
        double SpanLengthMm);

    public sealed record BeamBreakPlan(
        string OriginalMark,
        IReadOnlyList<BeamBreakResultItem> Spans);

    public static class RevitFramingGenerator
    {
        private const double MmToFeetFactor = 1.0 / 304.8;

        public static BeamCreationPlan CreatePlan(IEnumerable<BeamItemDef> beamDefs)
        {
            if (beamDefs == null)
            {
                return new BeamCreationPlan(Array.Empty<BeamPlanItem>(), 0.0, 0.0);
            }

            var planItems = new List<BeamPlanItem>();
            double totalLengthMm = 0.0;
            double totalVolumeM3 = 0.0;

            foreach (var b in beamDefs)
            {
                double lenMm = b.StartMm.DistanceTo(b.EndMm);
                double lenM = lenMm / 1000.0;
                double volM3 = (b.WidthMm / 1000.0) * (b.HeightMm / 1000.0) * lenM;
                totalLengthMm += lenMm;
                totalVolumeM3 += volM3;

                string symbolName = string.Format(CultureInfo.InvariantCulture, "{0:0}x{1:0}mm", b.WidthMm, b.HeightMm);

                planItems.Add(new BeamPlanItem(
                    b.Mark,
                    ToFeet(b.StartMm),
                    ToFeet(b.EndMm),
                    b.WidthMm,
                    b.HeightMm,
                    b.LevelName,
                    b.ZOffsetMm * MmToFeetFactor,
                    lenMm,
                    symbolName));
            }

            return new BeamCreationPlan(planItems, totalLengthMm / 1000.0, totalVolumeM3);
        }

        public static BeamBreakPlan BreakBeamAtSupports(ContinuousBeamDef beam, IEnumerable<Point3D> supportPointsMm)
        {
            if (beam == null) throw new ArgumentNullException(nameof(beam));
            var dir = beam.EndMm - beam.StartMm;
            double totalLen = beam.StartMm.DistanceTo(beam.EndMm);
            if (totalLen < 1e-4)
            {
                return new BeamBreakPlan(beam.Mark, Array.Empty<BeamBreakResultItem>());
            }

            var projections = new List<(double T, Point3D Pt)> { (0.0, beam.StartMm), (1.0, beam.EndMm) };

            if (supportPointsMm != null)
            {
                foreach (var sup in supportPointsMm)
                {
                    var v = sup - beam.StartMm;
                    double dot = (v.X * dir.X + v.Y * dir.Y + v.Z * dir.Z) / (totalLen * totalLen);
                    if (dot > 0.01 && dot < 0.99)
                    {
                        var projPt = beam.StartMm + new Vector3D(dir.X * dot, dir.Y * dot, dir.Z * dot);
                        projections.Add((dot, projPt));
                    }
                }
            }

            projections.Sort((a, b) => a.T.CompareTo(b.T));

            var uniqueProjections = new List<(double T, Point3D Pt)>(projections.Count);
            for (int i = 0; i < projections.Count; i++)
            {
                if (uniqueProjections.Count == 0 || Math.Abs(projections[i].T - uniqueProjections[uniqueProjections.Count - 1].T) > 1e-4)
                {
                    uniqueProjections.Add(projections[i]);
                }
            }

            var spans = new List<BeamBreakResultItem>();
            for (int i = 0; i < uniqueProjections.Count - 1; i++)
            {
                var p1 = uniqueProjections[i].Pt;
                var p2 = uniqueProjections[i + 1].Pt;
                double spanLen = p1.DistanceTo(p2);
                string subMark = string.Format(CultureInfo.InvariantCulture, "{0}_N{1}", beam.Mark, i + 1);
                spans.Add(new BeamBreakResultItem(subMark, p1, p2, spanLen));
            }

            return new BeamBreakPlan(beam.Mark, spans);
        }

        public static Point3D ToFeet(Point3D ptMm) =>
            new(ptMm.X * MmToFeetFactor, ptMm.Y * MmToFeetFactor, ptMm.Z * MmToFeetFactor);

        public static Point3D ToMm(Point3D ptFeet) =>
            new(ptFeet.X * 304.8, ptFeet.Y * 304.8, ptFeet.Z * 304.8);
    }

    public static class BeamGenerator
    {
        public static BeamCreationPlan CreatePlan(IEnumerable<BeamItemDef> beamDefs) =>
            RevitFramingGenerator.CreatePlan(beamDefs);
    }

    public static class BeamBreakGenerator
    {
        public static BeamBreakPlan BreakBeamAtSupports(ContinuousBeamDef beam, IEnumerable<Point3D> supportPointsMm) =>
            RevitFramingGenerator.BreakBeamAtSupports(beam, supportPointsMm);
    }
}
