using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using HPRebar.Core.Tcvn5574.Models;

namespace HPRebar.BeamRebar.Service;

/// <summary>
/// Exports comprehensive TCVN 5574:2018 calculation verification reports to Excel (.xlsx) using ClosedXML.
/// Generates both executive summary schedules and detailed structural section verification tables.
/// </summary>
public static class BeamCalculationReportExporter
{
    private static readonly XLColor HeaderBg = XLColor.FromArgb(0x1F, 0x4E, 0x79);
    private static readonly XLColor SubHeaderBg = XLColor.FromArgb(0x2E, 0x75, 0xB6);
    private static readonly XLColor PassBg = XLColor.FromArgb(0xE2, 0xEF, 0xDA);
    private static readonly XLColor PassText = XLColor.FromArgb(0x37, 0x56, 0x23);
    private static readonly XLColor FailBg = XLColor.FromArgb(0xFC, 0xE4, 0xD6);
    private static readonly XLColor FailText = XLColor.FromArgb(0xC6, 0x59, 0x11);

    public static void Export(
        IReadOnlyList<BeamCalculationReportModel> reports,
        string filePath,
        string projectName = "Dự án Tiêu chuẩn",
        string engineer = "Kỹ sư Kết cấu")
    {
        if (reports is null)
            throw new ArgumentNullException(nameof(reports));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));

        using var workbook = CreateWorkbook(reports, projectName, engineer);
        workbook.SaveAs(filePath);
    }

    public static void Export(
        IReadOnlyList<BeamCalculationReportModel> reports,
        Stream stream,
        string projectName = "Dự án Tiêu chuẩn",
        string engineer = "Kỹ sư Kết cấu")
    {
        if (reports is null)
            throw new ArgumentNullException(nameof(reports));

        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        using var workbook = CreateWorkbook(reports, projectName, engineer);
        workbook.SaveAs(stream);
    }

    public static XLWorkbook CreateWorkbook(
        IReadOnlyList<BeamCalculationReportModel> reports,
        string projectName,
        string engineer)
    {
        if (reports is null)
            throw new ArgumentNullException(nameof(reports));

        var workbook = new XLWorkbook();
        BuildSummarySheet(workbook, reports, projectName, engineer);
        BuildDetailSheet(workbook, reports);
        return workbook;
    }

    private static void BuildSummarySheet(
        IXLWorkbook wb,
        IReadOnlyList<BeamCalculationReportModel> reports,
        string projectName,
        string engineer)
    {
        var ws = wb.Worksheets.Add("Tong_Hop");
        ws.Cell("A1").Value = "BÁO CÁO KIỂM TOÁN CỐT THÉP DẦM THEO TCVN 5574:2018";
        ws.Range("A1:O1").Merge();
        ApplyTitleStyle(ws.Range("A1:O1"), 15);

        ws.Cell("A2").Value = $"Dự án: {projectName} | Kỹ sư: {engineer} | Tiêu chuẩn: TCVN 5574:2018 | Ngày: {DateTime.Now:dd/MM/yyyy}";
        ws.Range("A2:O2").Merge();
        ws.Range("A2:O2").Style.Font.Italic = true;

        string[] headers = { "STT", "Tên dầm", "Nhịp", "Tiết diện (mm)", "L (mm)", "Bê tông", "Thép dọc", "Thép đai",
                             "M_gối max (kNm)", "Thép gối chọn", "M_nhịp max (kNm)", "Thép nhịp chọn", "Q_max (kN)", "Cốt đai chọn", "Kết luận" };
        for (int c = 0; c < headers.Length; c++)
            ws.Cell(4, c + 1).Value = headers[c];
        ApplyHeaderStyle(ws.Range(4, 1, 4, headers.Length), HeaderBg);

        int row = 5;
        for (int i = 0; i < reports.Count; i++)
        {
            WriteSummaryRow(ws, row++, i + 1, reports[i]);
        }

        ws.Range(4, 1, row - 1, headers.Length).Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);
        ws.Range(4, 1, row - 1, headers.Length).Style.Border.SetOutsideBorder(XLBorderStyleValues.Medium);
        ws.Columns().AdjustToContents();
    }

    private static void WriteSummaryRow(IXLWorksheet ws, int row, int stt, BeamCalculationReportModel r)
    {
        ws.Cell(row, 1).Value = stt;
        ws.Cell(row, 2).Value = r.BeamMark;
        ws.Cell(row, 3).Value = r.SpanIndex + 1;
        ws.Cell(row, 4).Value = $"{r.WidthMm:0}x{r.HeightMm:0}";
        ws.Cell(row, 5).Value = r.LengthMm;
        ws.Cell(row, 6).Value = r.ConcreteGrade;
        ws.Cell(row, 7).Value = r.LongitudinalSteelGrade;
        ws.Cell(row, 8).Value = r.StirrupSteelGrade;

        double mGoi = Math.Max(Math.Abs(r.FlexureLeft?.MomentKnm ?? 0), Math.Abs(r.FlexureRight?.MomentKnm ?? 0));
        ws.Cell(row, 9).Value = mGoi;
        ws.Cell(row, 10).Value = r.TopSupportRebar?.Description ?? "-";
        ws.Cell(row, 11).Value = Math.Abs(r.FlexureMid?.MomentKnm ?? 0);
        ws.Cell(row, 12).Value = r.BottomSpanRebar?.Description ?? "-";

        double qMax = Math.Max(r.ShearLeft?.ShearKn ?? 0, Math.Max(r.ShearMid?.ShearKn ?? 0, r.ShearRight?.ShearKn ?? 0));
        ws.Cell(row, 13).Value = qMax;
        ws.Cell(row, 14).Value = r.StirrupDescription;

        var statusCell = ws.Cell(row, 15);
        statusCell.Value = r.Status;
        ApplyStatusStyle(statusCell, r.IsCompliant);
    }

    private static void BuildDetailSheet(IXLWorkbook wb, IReadOnlyList<BeamCalculationReportModel> reports)
    {
        var ws = wb.Worksheets.Add("Chi_Tiet_Kiem_Toan");
        ws.Cell("A1").Value = "BẢNG CHI TIẾT KIỂM TOÁN NỘI LỰC & BỐ TRÍ CỐT THÉP TỪNG MẶT CẮT";
        ws.Range("A1:Q1").Merge();
        ApplyTitleStyle(ws.Range("A1:Q1"), 14);

        string[] headers = { "STT", "Tên dầm", "Vị trí", "M (kNm)", "α_m", "ξ", "As,req (mm²)", "Thép chọn", "As,prov (mm²)",
                             "μ (%)", "Q (kN)", "Qb,max (kN)", "Qb+Qsw (kN)", "s,req (mm)", "s,max (mm)", "t (mm)", "Kết luận" };
        for (int c = 0; c < headers.Length; c++)
            ws.Cell(3, c + 1).Value = headers[c];
        ApplyHeaderStyle(ws.Range(3, 1, 3, headers.Length), SubHeaderBg);

        int row = 4;
        int stt = 1;
        foreach (var r in reports)
        {
            WriteDetailStationRow(ws, row++, stt, r.BeamMark, "Gối trái", r.FlexureLeft, r.ShearLeft, r.TopSupportRebar);
            WriteDetailStationRow(ws, row++, stt, r.BeamMark, "Giữa nhịp", r.FlexureMid, r.ShearMid, r.BottomSpanRebar);
            WriteDetailStationRow(ws, row++, stt, r.BeamMark, "Gối phải", r.FlexureRight, r.ShearRight, r.TopSupportRebar);
            stt++;
        }

        ws.Range(3, 1, row - 1, headers.Length).Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);
        ws.Range(3, 1, row - 1, headers.Length).Style.Border.SetOutsideBorder(XLBorderStyleValues.Medium);
        ws.Columns().AdjustToContents();
    }

    private static void WriteDetailStationRow(
        IXLWorksheet ws, int row, int stt, string mark, string station,
        FlexureDesignResult? flx, ShearDesignResult? shr, RebarOption? opt)
    {
        ws.Cell(row, 1).Value = stt;
        ws.Cell(row, 2).Value = mark;
        ws.Cell(row, 3).Value = station;
        ws.Cell(row, 4).Value = flx?.MomentKnm ?? 0.0;
        ws.Cell(row, 5).Value = flx?.AlphaM ?? 0.0;
        ws.Cell(row, 6).Value = flx?.Xi ?? 0.0;
        ws.Cell(row, 7).Value = flx?.AsRequired ?? 0.0;
        ws.Cell(row, 8).Value = opt?.Description ?? "-";
        ws.Cell(row, 9).Value = opt?.ProvidedArea ?? 0.0;
        ws.Cell(row, 10).Value = flx?.MuPercent ?? 0.0;
        ws.Cell(row, 11).Value = shr?.ShearKn ?? 0.0;
        ws.Cell(row, 12).Value = shr?.QbMaxKn ?? 0.0;
        ws.Cell(row, 13).Value = (shr?.QbKn ?? 0.0) + (shr?.QswKn ?? 0.0);
        ws.Cell(row, 14).Value = shr?.CalculatedSpacing ?? 0.0;
        ws.Cell(row, 15).Value = shr?.MaxSpacing ?? 0.0;
        ws.Cell(row, 16).Value = opt?.MinClearSpacing ?? 0.0;

        bool ok = (flx?.IsCompliant ?? false) && (shr?.IsCompliant ?? false) && (opt?.IsValid ?? false);
        var statusCell = ws.Cell(row, 17);
        statusCell.Value = ok ? "ĐẠT" : "NG";
        ApplyStatusStyle(statusCell, ok);
    }

    private static void ApplyTitleStyle(IXLRange range, int fontSize)
    {
        range.Style.Font.Bold = true;
        range.Style.Font.FontSize = fontSize;
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Fill.BackgroundColor = HeaderBg;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    private static void ApplyHeaderStyle(IXLRange range, XLColor bg)
    {
        range.Style.Font.Bold = true;
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Fill.BackgroundColor = bg;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    private static void ApplyStatusStyle(IXLCell cell, bool compliant)
    {
        cell.Style.Font.Bold = true;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cell.Style.Fill.BackgroundColor = compliant ? PassBg : FailBg;
        cell.Style.Font.FontColor = compliant ? PassText : FailText;
    }
}
