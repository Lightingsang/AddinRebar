using System;
using System.Collections.Generic;
using System.Linq;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Revit.Generators
{
    public sealed record SlabItemDef(
        string Mark,
        IReadOnlyList<Point3D> OuterBoundaryMm,
        IReadOnlyList<IReadOnlyList<Point3D>>? OpeningsMm,
        double ThicknessMm,
        string LevelName,
        double ElevationOffsetMm = 0.0);

    public sealed record SlabPlanItem(
        string Mark,
        IReadOnlyList<Point3D> OuterBoundaryFeet,
        IReadOnlyList<IReadOnlyList<Point3D>> OpeningsFeet,
        double ThicknessMm,
        string LevelName,
        double ElevationOffsetFeet,
        double AreaM2,
        double VolumeM3);

    public sealed record SlabCreationPlan(
        IReadOnlyList<SlabPlanItem> Items,
        double TotalAreaM2,
        double TotalVolumeM3);

    public sealed record WallItemDef(
        string Mark,
        Point3D StartMm,
        Point3D EndMm,
        double ThicknessMm,
        double HeightMm,
        string BaseLevel,
        string TopLevel);

    public sealed record WallPlanItem(
        string Mark,
        Point3D StartFeet,
        Point3D EndFeet,
        double ThicknessMm,
        double HeightFeet,
        string BaseLevel,
        string TopLevel,
        double AreaM2,
        double VolumeM3);

    public sealed record WallCreationPlan(
        IReadOnlyList<WallPlanItem> Items,
        double TotalLengthM,
        double TotalVolumeM3);

    public static class RevitSlabWallGenerator
    {
        private const double MmToFeetFactor = 1.0 / 304.8;

        public static SlabCreationPlan CreateSlabPlan(IEnumerable<SlabItemDef> slabDefs)
        {
            if (slabDefs == null)
            {
                return new SlabCreationPlan(Array.Empty<SlabPlanItem>(), 0.0, 0.0);
            }

            var planItems = new List<SlabPlanItem>();
            double totalAreaM2 = 0.0;
            double totalVolumeM3 = 0.0;

            foreach (var s in slabDefs)
            {
                double outerAreaMm2 = CalculatePolygonArea(s.OuterBoundaryMm);
                double openingsAreaMm2 = 0.0;
                var openingsFeet = new List<IReadOnlyList<Point3D>>();

                if (s.OpeningsMm != null)
                {
                    foreach (var op in s.OpeningsMm)
                    {
                        openingsAreaMm2 += CalculatePolygonArea(op);
                        openingsFeet.Add(op.Select(ToFeet).ToList());
                    }
                }

                double netAreaM2 = Math.Max(0.0, (outerAreaMm2 - openingsAreaMm2) / 1000000.0);
                double volumeM3 = netAreaM2 * (s.ThicknessMm / 1000.0);

                totalAreaM2 += netAreaM2;
                totalVolumeM3 += volumeM3;

                planItems.Add(new SlabPlanItem(
                    s.Mark,
                    s.OuterBoundaryMm.Select(ToFeet).ToList(),
                    openingsFeet,
                    s.ThicknessMm,
                    s.LevelName,
                    s.ElevationOffsetMm * MmToFeetFactor,
                    netAreaM2,
                    volumeM3));
            }

            return new SlabCreationPlan(planItems, totalAreaM2, totalVolumeM3);
        }

        public static WallCreationPlan CreateWallPlan(IEnumerable<WallItemDef> wallDefs)
        {
            if (wallDefs == null)
            {
                return new WallCreationPlan(Array.Empty<WallPlanItem>(), 0.0, 0.0);
            }

            var planItems = new List<WallPlanItem>();
            double totalLengthM = 0.0;
            double totalVolumeM3 = 0.0;

            foreach (var w in wallDefs)
            {
                double lenM = w.StartMm.DistanceTo(w.EndMm) / 1000.0;
                double areaM2 = lenM * (w.HeightMm / 1000.0);
                double volumeM3 = areaM2 * (w.ThicknessMm / 1000.0);

                totalLengthM += lenM;
                totalVolumeM3 += volumeM3;

                planItems.Add(new WallPlanItem(
                    w.Mark,
                    ToFeet(w.StartMm),
                    ToFeet(w.EndMm),
                    w.ThicknessMm,
                    w.HeightMm * MmToFeetFactor,
                    w.BaseLevel,
                    w.TopLevel,
                    areaM2,
                    volumeM3));
            }

            return new WallCreationPlan(planItems, totalLengthM, totalVolumeM3);
        }

        public static double CalculatePolygonArea(IReadOnlyList<Point3D>? points)
        {
            if (points == null || points.Count < 3) return 0.0;
            double sum = 0.0;
            int n = points.Count;
            for (int i = 0; i < n; i++)
            {
                var curr = points[i];
                var next = points[(i + 1) % n];
                sum += (curr.X * next.Y) - (next.X * curr.Y);
            }
            return Math.Abs(sum) * 0.5;
        }

        private static Point3D ToFeet(Point3D ptMm) =>
            new(ptMm.X * MmToFeetFactor, ptMm.Y * MmToFeetFactor, ptMm.Z * MmToFeetFactor);
    }

    public static class SlabGenerator
    {
        public static SlabCreationPlan CreatePlan(IEnumerable<SlabItemDef> slabDefs) =>
            RevitSlabWallGenerator.CreateSlabPlan(slabDefs);
    }

    public static class WallGenerator
    {
        public static WallCreationPlan CreatePlan(IEnumerable<WallItemDef> wallDefs) =>
            RevitSlabWallGenerator.CreateWallPlan(wallDefs);
    }
}
