using System;
using System.Collections.Generic;
using System.Globalization;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Revit.Generators
{
    public sealed record ColumnItemDef(
        string Mark,
        Point3D LocationMm,
        double WidthMm,
        double HeightMm,
        double RotationDeg,
        string BaseLevel,
        string TopLevel,
        double BaseOffsetMm = 0.0,
        double TopOffsetMm = 0.0,
        double ColumnHeightMm = 3300.0);

    public sealed record ColumnPlanItem(
        string Mark,
        Point3D LocationFeet,
        double WidthMm,
        double HeightMm,
        double RotationRad,
        string BaseLevel,
        string TopLevel,
        double BaseOffsetFeet,
        double TopOffsetFeet,
        double VolumeM3,
        string FamilySymbolName);

    public sealed record ColumnCreationPlan(
        IReadOnlyList<ColumnPlanItem> Items,
        int TotalCount,
        double TotalVolumeM3);

    public static class RevitColumnGenerator
    {
        private const double MmToFeetFactor = 1.0 / 304.8;
        private const double DegToRadFactor = Math.PI / 180.0;

        public static ColumnCreationPlan CreatePlan(IEnumerable<ColumnItemDef> colDefs)
        {
            if (colDefs == null)
            {
                return new ColumnCreationPlan(Array.Empty<ColumnPlanItem>(), 0, 0.0);
            }

            var planItems = new List<ColumnPlanItem>();
            double totalVolumeM3 = 0.0;

            foreach (var c in colDefs)
            {
                double effectiveHeightMm = Math.Max(c.ColumnHeightMm + c.TopOffsetMm - c.BaseOffsetMm, 100.0);
                double volumeM3 = (c.WidthMm / 1000.0) * (c.HeightMm / 1000.0) * (effectiveHeightMm / 1000.0);
                totalVolumeM3 += volumeM3;

                string symbolName = string.Format(CultureInfo.InvariantCulture, "Column_{0:0}x{1:0}mm", c.WidthMm, c.HeightMm);

                planItems.Add(new ColumnPlanItem(
                    c.Mark,
                    ToFeet(c.LocationMm),
                    c.WidthMm,
                    c.HeightMm,
                    c.RotationDeg * DegToRadFactor,
                    c.BaseLevel,
                    c.TopLevel,
                    c.BaseOffsetMm * MmToFeetFactor,
                    c.TopOffsetMm * MmToFeetFactor,
                    volumeM3,
                    symbolName));
            }

            return new ColumnCreationPlan(planItems, planItems.Count, totalVolumeM3);
        }

        private static Point3D ToFeet(Point3D ptMm) =>
            new(ptMm.X * MmToFeetFactor, ptMm.Y * MmToFeetFactor, ptMm.Z * MmToFeetFactor);
    }

    public static class ColumnGenerator
    {
        public static ColumnCreationPlan CreatePlan(IEnumerable<ColumnItemDef> colDefs) =>
            RevitColumnGenerator.CreatePlan(colDefs);
    }
}
