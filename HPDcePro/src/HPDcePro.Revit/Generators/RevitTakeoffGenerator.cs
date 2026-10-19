using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace HPDcePro.Revit.Generators
{
    public sealed record TakeoffItemSummary(
        string Category,
        int ElementCount,
        double ConcreteVolumeM3,
        double FormworkAreaM2,
        double SteelWeightKg);

    public sealed record TakeoffReportResult(
        double TotalConcreteM3,
        double TotalFormworkM2,
        double TotalSteelKg,
        double SteelDiaSmallKg,   // <= 10mm
        double SteelDiaMediumKg,  // 12mm - 18mm
        double SteelDiaLargeKg,   // > 18mm
        IReadOnlyList<TakeoffItemSummary> Summaries,
        string FormattedTextTable);

    public static class RevitTakeoffGenerator
    {
        public static TakeoffReportResult GenerateReport(
            IEnumerable<RevitFramingElement>? framings = null,
            IEnumerable<RevitColumnElement>? columns = null,
            IEnumerable<SlabPlanItem>? slabs = null,
            IEnumerable<WallPlanItem>? walls = null,
            IEnumerable<RevitRebarDescriptor>? rebars = null)
        {
            var summaries = new List<TakeoffItemSummary>();
            var (framingVol, framingFormwork) = ProcessFramings(framings, summaries);
            var (colVol, colFormwork) = ProcessColumns(columns, summaries);
            var (slabVol, slabFormwork) = ProcessSlabs(slabs, summaries);
            var (wallVol, wallFormwork) = ProcessWalls(walls, summaries);
            var (steelSmall, steelMed, steelLarge, totalSteel) = ProcessRebars(rebars, summaries);

            double totalConc = framingVol + colVol + slabVol + wallVol;
            double totalForm = framingFormwork + colFormwork + slabFormwork + wallFormwork;
            string table = FormatReportTable(totalConc, totalForm, totalSteel, steelSmall, steelMed, steelLarge);

            return new TakeoffReportResult(
                totalConc, totalForm, totalSteel, steelSmall, steelMed, steelLarge, summaries, table);
        }

        private static (double vol, double form) ProcessFramings(
            IEnumerable<RevitFramingElement>? items, List<TakeoffItemSummary> summaries)
        {
            if (items == null) return (0.0, 0.0);
            double vol = 0.0, form = 0.0;
            int count = 0;
            foreach (var f in items)
            {
                count++;
                double lenM = f.LengthMm / 1000.0, bM = f.WidthMm / 1000.0, hM = f.HeightMm / 1000.0;
                vol += bM * hM * lenM;
                form += (2.0 * hM + bM) * lenM;
            }
            if (count > 0) summaries.Add(new TakeoffItemSummary("Dầm (Framing)", count, vol, form, 0.0));
            return (vol, form);
        }

        private static (double vol, double form) ProcessColumns(
            IEnumerable<RevitColumnElement>? items, List<TakeoffItemSummary> summaries)
        {
            if (items == null) return (0.0, 0.0);
            double vol = 0.0, form = 0.0;
            int count = 0;
            foreach (var c in items)
            {
                count++;
                double bM = c.WidthMm / 1000.0, hM = c.HeightMm / 1000.0;
                vol += bM * hM * 3.3;
                form += 2.0 * (bM + hM) * 3.3;
            }
            if (count > 0) summaries.Add(new TakeoffItemSummary("Cột (Columns)", count, vol, form, 0.0));
            return (vol, form);
        }

        private static (double vol, double form) ProcessSlabs(
            IEnumerable<SlabPlanItem>? items, List<TakeoffItemSummary> summaries)
        {
            if (items == null) return (0.0, 0.0);
            double vol = 0.0, form = 0.0;
            int count = 0;
            foreach (var s in items) { count++; vol += s.VolumeM3; form += s.AreaM2; }
            if (count > 0) summaries.Add(new TakeoffItemSummary("Sàn (Slabs)", count, vol, form, 0.0));
            return (vol, form);
        }

        private static (double vol, double form) ProcessWalls(
            IEnumerable<WallPlanItem>? items, List<TakeoffItemSummary> summaries)
        {
            if (items == null) return (0.0, 0.0);
            double vol = 0.0, form = 0.0;
            int count = 0;
            foreach (var w in items) { count++; vol += w.VolumeM3; form += 2.0 * w.AreaM2; }
            if (count > 0) summaries.Add(new TakeoffItemSummary("Vách (Walls)", count, vol, form, 0.0));
            return (vol, form);
        }

        private static (double sm, double med, double lg, double tot) ProcessRebars(
            IEnumerable<RevitRebarDescriptor>? items, List<TakeoffItemSummary> summaries)
        {
            if (items == null) return (0.0, 0.0, 0.0, 0.0);
            double sm = 0.0, med = 0.0, lg = 0.0;
            int count = 0;
            foreach (var r in items)
            {
                count++;
                double wt = 0.00617 * r.DiameterMm * r.DiameterMm * (r.LengthMm / 1000.0) * Math.Max(1, r.Quantity);
                if (r.DiameterMm <= 10.0) sm += wt;
                else if (r.DiameterMm <= 18.0) med += wt;
                else lg += wt;
            }
            double tot = sm + med + lg;
            if (count > 0) summaries.Add(new TakeoffItemSummary("Cốt thép (Rebar)", count, 0.0, 0.0, tot));
            return (sm, med, lg, tot);
        }

        private static string FormatReportTable(double conc, double form, double st, double sm, double med, double lg)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== BẢNG BÓC TÁCH KHỐI LƯỢNG HPDCEPRO ===");
            sb.AppendFormat(CultureInfo.InvariantCulture, "Tổng bê tông: {0:F2} m3\n", conc);
            sb.AppendFormat(CultureInfo.InvariantCulture, "Tổng ván khuôn: {0:F2} m2\n", form);
            sb.AppendFormat(CultureInfo.InvariantCulture, "Tổng cốt thép: {0:F2} kg (<=d10: {1:F2} kg, d12-d18: {2:F2} kg, >d18: {3:F2} kg)\n",
                st, sm, med, lg);
            return sb.ToString();
        }
    }

    public static class TakeoffReportGenerator
    {
        public static TakeoffReportResult GenerateReport(
            IEnumerable<RevitFramingElement>? framings = null,
            IEnumerable<RevitColumnElement>? columns = null,
            IEnumerable<SlabPlanItem>? slabs = null,
            IEnumerable<WallPlanItem>? walls = null,
            IEnumerable<RevitRebarDescriptor>? rebars = null)
        {
            return RevitTakeoffGenerator.GenerateReport(framings, columns, slabs, walls, rebars);
        }
    }
}
