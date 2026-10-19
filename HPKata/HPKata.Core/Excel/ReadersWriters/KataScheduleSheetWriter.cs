using System;
using System.Collections.Generic;
using HPKata.Core.Excel.Abstractions;
using HPKata.Core.Excel.Data;

namespace HPKata.Core.Excel.ReadersWriters
{
    /// <summary>
    /// Writes rebar schedule takeoff and summary bins into sheet 'Thong_ke' of Kata.xlsm.
    /// </summary>
    public static class KataScheduleSheetWriter
    {
        public const string SheetName = "Thong_ke";

        public static bool Write(IKataExcelSession session, IEnumerable<KataRebarScheduleItem> items)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (items == null) throw new ArgumentNullException(nameof(items));

            session.EnsureSheet(SheetName);
            WriteHeader(session);
            int row = 4;
            double wSmall = 0.0;
            double wMedium = 0.0;
            double wLarge = 0.0;
            int couplerCount = 0;

            foreach (var item in items)
            {
                WriteScheduleRow(session, row++, item);
                AccumulateSummaries(item, ref wSmall, ref wMedium, ref wLarge, ref couplerCount);
            }

            WriteSummaryBins(session, wSmall, wMedium, wLarge, couplerCount);
            session.Save();
            return true;
        }

        private static void WriteHeader(IKataExcelSession session)
        {
            session.SetCellValue(SheetName, 1, 1, "BẢNG THỐNG KÊ CỐT THÉP");
            string[] headers =
            {
                "TÊN CẤU KIỆN", "SỐ HIỆU", "DẠNG",
                "L1", "L2", "L3", "L4", "L5", "L6",
                "Ghi chú", "Ø (mm)", "Dài ko kể nối", "CHIỀU DÀI 1 THANH",
                "SỐ C.KIỆN", "SỐ THANH MỘT CK", "SỐ THANH TOÀN BỘ",
                "TỔNG CHIỀU DÀI", "TỔNG TRỌNG LƯỢNG", "TỔNG/CẤU KIỆN"
            };
            for (int col = 0; col < headers.Length; col++)
            {
                session.SetCellValue(SheetName, 3, col + 1, headers[col]);
            }
        }

        private static void WriteScheduleRow(IKataExcelSession session, int row, KataRebarScheduleItem item)
        {
            session.SetCellValue(SheetName, row, 1, item.ComponentMark);
            session.SetCellValue(SheetName, row, 2, item.BarMark);
            session.SetCellValue(SheetName, row, 3, item.ShapeCode);

            for (int i = 0; i < 6; i++)
            {
                double seg = (item.SegmentsMm != null && i < item.SegmentsMm.Length) ? item.SegmentsMm[i] : 0.0;
                session.SetCellValue(SheetName, row, 4 + i, seg > 0.0 ? (object)seg : string.Empty);
            }

            session.SetCellValue(SheetName, row, 10, item.Notes);
            session.SetCellValue(SheetName, row, 11, item.DiameterMm);
            session.SetCellValue(SheetName, row, 12, item.UnlappedLengthMm > 0 ? item.UnlappedLengthMm : item.CuttingLengthMm);
            session.SetCellValue(SheetName, row, 13, item.CuttingLengthMm);
            session.SetCellValue(SheetName, row, 14, item.MemberCount);
            session.SetCellValue(SheetName, row, 15, item.CountPerMember);
            session.SetCellValue(SheetName, row, 16, item.TotalCount);
            session.SetCellValue(SheetName, row, 17, Math.Round(item.TotalLengthM, 2));
            session.SetCellValue(SheetName, row, 18, Math.Round(item.TotalWeightKg, 2));
            session.SetCellValue(SheetName, row, 19, Math.Round(item.WeightPerMemberKg, 2));
        }

        private static void AccumulateSummaries(
            KataRebarScheduleItem item, ref double wSmall, ref double wMedium, ref double wLarge, ref int couplers)
        {
            if (item.IsCoupler)
            {
                couplers += item.TotalCount;
                return;
            }

            if (item.DiameterMm <= 10) wSmall += item.TotalWeightKg;
            else if (item.DiameterMm <= 18) wMedium += item.TotalWeightKg;
            else wLarge += item.TotalWeightKg;
        }

        private static void WriteSummaryBins(
            IKataExcelSession session, double wSmall, double wMedium, double wLarge, int couplers)
        {
            double totalWeight = wSmall + wMedium + wLarge;
            session.SetCellValue(SheetName, 4, 21, Math.Round(wSmall, 2));
            session.SetCellValue(SheetName, 5, 21, Math.Round(wMedium, 2));
            session.SetCellValue(SheetName, 6, 21, Math.Round(wLarge, 2));
            session.SetCellValue(SheetName, 7, 21, Math.Round(totalWeight, 2));
            session.SetCellValue(SheetName, 9, 21, couplers);
        }
    }
}
