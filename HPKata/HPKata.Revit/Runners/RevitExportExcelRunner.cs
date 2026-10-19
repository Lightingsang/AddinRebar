using System;
using System.Collections.Generic;
using System.IO;
using HPKata.Core.Configuration;
using HPKata.Core.Excel;
using HPKata.Core.Export;
using HPKata.Core.Models;
using HPKata.Core.Scheduling;
using HPKata.Revit.Abstractions;
using HPKata.Revit.Bridge;

namespace HPKata.Revit.Runners;

/// <summary>
/// Domain runner exporting rebar schedule, diameter summary, and coupler tables to Excel.
/// Routes through IKataExcelAdapter into Kata.xlsm sheet 'Thong_ke' or independent workbook fallback.
/// </summary>
public static class RevitExportExcelRunner
{
    /// <summary>
    /// Executes Excel export of the rebar schedule for all or selected rebars.
    /// </summary>
    public static string Execute(
        IRevitDocumentContext context,
        string? outputFilePath = null,
        bool selectedOnly = false,
        bool applyBendDeduction = false,
        IKataExcelAdapter? adapter = null)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        var dtos = selectedOnly && context.GetSelectedRebars().Count > 0
            ? context.GetSelectedRebars()
            : context.GetAllRebars();

        var items = new List<RebarItem>(dtos.Count);
        foreach (var dto in dtos)
        {
            items.Add(RevitParameterBridge.ToRebarItem(dto));
        }

        string resolvedPath = ResolveOutputPath(context, outputFilePath);
        var excel = adapter ?? KataExcelAdapterProvider.Current;
        bool success = excel.ExportSchedule(resolvedPath, items);

        if (!success)
        {
            string fallbackPath = IsMasterLibraryAsset(resolvedPath)
                ? ResolveDocumentFallbackPath(context)
                : resolvedPath;

            var report = RebarScheduleAggregator.GenerateReport(items, applyBendDeduction);
            ClosedXmlExporter.ExportScheduleToFile(report, fallbackPath);
            resolvedPath = fallbackPath;
        }

        context.WriteNotification($"Đã xuất bảng thống kê thép ra Excel: {resolvedPath}");
        return resolvedPath;
    }

    private static string ResolveOutputPath(IRevitDocumentContext context, string? outputFilePath)
    {
        if (!string.IsNullOrWhiteSpace(outputFilePath))
        {
            return outputFilePath!;
        }

        if (!string.IsNullOrWhiteSpace(context.DocumentPath))
        {
            string? dir = Path.GetDirectoryName(context.DocumentPath);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                string title = Path.GetFileNameWithoutExtension(context.DocumentPath);
                return Path.Combine(dir, $"{title}_ThongKeThep.xlsx");
            }
        }

        string safeTitle = string.IsNullOrWhiteSpace(context.DocumentTitle) ? "RevitDoc" : context.DocumentTitle;
        return Path.Combine(Path.GetTempPath(), $"{safeTitle}_ThongKeThep.xlsx");
    }

    private static bool IsMasterLibraryAsset(string path)
    {
        var env = KataInstallEnvironment.Current;
        return string.Equals(path, env.KataWorkbookPath, StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, env.KataEngWorkbookPath, StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, env.IsoQhPlusPath, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveDocumentFallbackPath(IRevitDocumentContext context)
    {
        string safeTitle = string.IsNullOrWhiteSpace(context.DocumentTitle) ? "RevitDoc" : context.DocumentTitle;
        return Path.Combine(Path.GetTempPath(), $"{safeTitle}_ThongKeThep_Fallback.xlsx");
    }
}
