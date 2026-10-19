using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Revit.Generators
{
    public sealed record GridItemDef(
        string Name,
        Point3D StartMm,
        Point3D EndMm,
        bool IsCurved = false,
        Point3D? ArcCenterMm = null);

    public sealed record GridPlanItem(
        string Name,
        Point3D StartFeet,
        Point3D EndFeet,
        bool IsCurved,
        Point3D? ArcCenterFeet,
        double LengthMm);

    public sealed record GridCreationPlan(
        IReadOnlyList<GridPlanItem> Items,
        double TotalLengthM);

    public static class RevitGridGenerator
    {
        private const double MmToFeetFactor = 1.0 / 304.8;

        public static GridCreationPlan CreatePlan(IEnumerable<GridItemDef> gridDefs)
        {
            if (gridDefs == null)
            {
                return new GridCreationPlan(Array.Empty<GridPlanItem>(), 0.0);
            }

            var planItems = new List<GridPlanItem>();
            double totalLengthMm = 0.0;

            foreach (var def in gridDefs)
            {
                var startFeet = ToFeet(def.StartMm);
                var endFeet = ToFeet(def.EndMm);
                Point3D? centerFeet = def.ArcCenterMm.HasValue ? ToFeet(def.ArcCenterMm.Value) : null;
                double lenMm = def.StartMm.DistanceTo(def.EndMm);

                totalLengthMm += lenMm;
                planItems.Add(new GridPlanItem(
                    def.Name,
                    startFeet,
                    endFeet,
                    def.IsCurved,
                    centerFeet,
                    lenMm));
            }

            return new GridCreationPlan(planItems, totalLengthMm / 1000.0);
        }

        public static IReadOnlyList<string> GenerateAxisNames(int count, bool isAlphabetical)
        {
            var names = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                names.Add(isAlphabetical ? ((char)('A' + (i % 26))).ToString() : (i + 1).ToString(CultureInfo.InvariantCulture));
            }
            return names;
        }

        public static double CalculateGridSpacing(Point3D p1Mm, Point3D p2Mm)
        {
            return p1Mm.DistanceTo(p2Mm);
        }

        private static Point3D ToFeet(Point3D ptMm)
        {
            return new Point3D(
                ptMm.X * MmToFeetFactor,
                ptMm.Y * MmToFeetFactor,
                ptMm.Z * MmToFeetFactor);
        }
    }

    public static class GridGenerator
    {
        public static GridCreationPlan CreatePlan(IEnumerable<GridItemDef> gridDefs) =>
            RevitGridGenerator.CreatePlan(gridDefs);
    }
}
