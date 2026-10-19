using System;
using System.Collections.Generic;
using System.IO;
using HPKata.AutoCad.Abstractions;
using HPKata.Core.Configuration;
using HPKata.Core.Excel;
using HPKata.Core.Export;
using HPKata.Core.Models;
using HPKata.Core.Scheduling;
using HPKata.Core.Tagging;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// Pure command runner for Excel export ('toexcel', 'toexcelqh') and QR bundle tag generation ('itt').
    /// </summary>
    public static class ExportCommandRunner
    {
        public static string ExecuteExcelExport(
            ICadDocumentContext context,
            bool isQhPlus = false,
            string? overridePath = null,
            IKataExcelAdapter? adapter = null)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            IReadOnlyList<CadEntityDto> entities = context.GetAllRebarEntities();
            List<RebarItem> items = ScheduleCommandRunner.ExtractRebarItems(entities);

            string targetPath = overridePath ?? (isQhPlus
                ? KataInstallEnvironment.Current.IsoQhPlusPath
                : (File.Exists(KataInstallEnvironment.Current.KataWorkbookPath)
                    ? KataInstallEnvironment.Current.KataWorkbookPath
                    : ResolveDefaultExportPath(context, isQhPlus)));

            var excel = adapter ?? KataExcelAdapterProvider.Current;
            bool success = isQhPlus
                ? excel.ExportQhPlusSchedule(targetPath, items)
                : excel.ExportSchedule(targetPath, items);

            if (success)
            {
                context.WriteMessage($"\n[HPKata] Xuất bảng thống kê thành công: {targetPath}\n");
                return targetPath;
            }

            string fallbackPath = overridePath ?? ResolveDefaultExportPath(context, isQhPlus);
            RebarScheduleReport report = RebarScheduleAggregator.GenerateReport(items);
            ClosedXmlExporter.ExportScheduleToFile(report, fallbackPath);
            context.WriteMessage($"\n[HPKata] Xuất bảng thống kê dự phòng thành công: {fallbackPath}\n");
            return fallbackPath;
        }

        public static List<QrTagPayload> ExecuteTagPrint(ICadDocumentContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            IReadOnlyList<CadEntityDto> entities = context.GetAllRebarEntities();
            List<RebarItem> items = ScheduleCommandRunner.ExtractRebarItems(entities);
            var payloads = new List<QrTagPayload>();

            foreach (RebarItem item in items)
            {
                var payload = new QrTagPayload
                {
                    Project = context.DocumentName,
                    Component = item.ComponentMark,
                    Mark = item.BarMark.ToString(),
                    ShapeCode = item.ShapeCode,
                    DiameterMm = item.DiameterMm,
                    CutLengthMm = item.CuttingLengthMm,
                    Quantity = item.TotalCount,
                    WeightKg = item.TotalWeightKg,
                    SteelGrade = "CB400-V",
                    SegmentsMm = item.ParametersMm.ToArray()
                };

                string rawPayload = QrTagGenerator.FormatPayload(payload);
                byte[] qrBytes = QrTagGenerator.GeneratePngBytes(rawPayload, pixelsPerModule: 6);
                if (qrBytes != null && qrBytes.Length > 0)
                {
                    payloads.Add(payload);
                }
            }

            context.WriteMessage($"\n[HPKata] In tem thép: Đã tạo mã QR cho {payloads.Count} thẻ bó thép.\n");
            return payloads;
        }

        private static string ResolveDefaultExportPath(ICadDocumentContext context, bool isQhPlus)
        {
            string dir = Path.GetDirectoryName(context.DocumentPath) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
            {
                dir = Path.GetTempPath();
            }

            string baseName = Path.GetFileNameWithoutExtension(context.DocumentName);
            if (string.IsNullOrWhiteSpace(baseName)) baseName = "Drawing";

            string suffix = isQhPlus ? "_ThongKe_QHPlus.xlsx" : "_ThongKe.xlsx";
            return Path.Combine(dir, $"{baseName}{suffix}");
        }
    }
}
