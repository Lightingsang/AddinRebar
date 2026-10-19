using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ClosedXML.Excel;
using HPRebar.Core.Tcvn5574.Models;

namespace HPRebar.BeamRebar.Service;

/// <summary>
/// Reads beam internal force envelopes from Excel workbooks (.xlsx) using ClosedXML.
/// Parses schedule tables and maps rows to <see cref="BeamForceEnvelope"/> records.
/// </summary>
public static class ClosedXmlBeamForceReader
{
    public static IReadOnlyList<BeamForceEnvelope> Read(string filePath, string? sheetName = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Excel force file not found: {filePath}", filePath);

        using var workbook = new XLWorkbook(filePath);
        var worksheet = ResolveWorksheet(workbook, sheetName);
        return Read(worksheet);
    }

    public static IReadOnlyList<BeamForceEnvelope> Read(Stream stream, string? sheetName = null)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        using var workbook = new XLWorkbook(stream);
        var worksheet = ResolveWorksheet(workbook, sheetName);
        return Read(worksheet);
    }

    public static IReadOnlyList<BeamForceEnvelope> Read(IXLWorksheet worksheet)
    {
        if (worksheet is null)
            throw new ArgumentNullException(nameof(worksheet));

        var headerMap = DetectHeaders(worksheet, out int startRow);
        var envelopes = new List<BeamForceEnvelope>();
        int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        for (int r = startRow; r <= lastRow; r++)
        {
            var row = worksheet.Row(r);
            if (row.IsEmpty())
                continue;

            string mark = ReadString(row, headerMap.BeamMarkCol);
            if (string.IsNullOrWhiteSpace(mark))
                continue;

            envelopes.Add(new BeamForceEnvelope
            {
                BeamMark = mark.Trim(),
                SpanIndex = ReadInt(row, headerMap.SpanIndexCol, 0),
                SpanLengthMm = ReadDouble(row, headerMap.LengthCol, 0.0),
                MomentSupportLeftKnm = ReadDouble(row, headerMap.MLeftCol, 0.0),
                MomentMidSpanKnm = ReadDouble(row, headerMap.MMidCol, 0.0),
                MomentSupportRightKnm = ReadDouble(row, headerMap.MRightCol, 0.0),
                ShearSupportLeftKn = ReadDouble(row, headerMap.QLeftCol, 0.0),
                ShearMidSpanKn = ReadDouble(row, headerMap.QMidCol, 0.0),
                ShearSupportRightKn = ReadDouble(row, headerMap.QRightCol, 0.0)
            });
        }

        return envelopes;
    }

    private static IXLWorksheet ResolveWorksheet(IXLWorkbook workbook, string? sheetName)
    {
        if (!string.IsNullOrWhiteSpace(sheetName) && workbook.TryGetWorksheet(sheetName, out var target))
            return target;

        return workbook.Worksheets.Count > 0
            ? workbook.Worksheets.Worksheet(1)
            : throw new InvalidOperationException("Excel workbook does not contain any worksheets.");
    }

    private static ColumnHeaderMap DetectHeaders(IXLWorksheet ws, out int dataStartRow)
    {
        var map = new ColumnHeaderMap();
        int searchMax = Math.Min(10, ws.LastRowUsed()?.RowNumber() ?? 1);

        for (int r = 1; r <= searchMax; r++)
        {
            var row = ws.Row(r);
            int matched = 0;
            int lastCol = row.LastCellUsed()?.Address.ColumnNumber ?? 0;

            for (int c = 1; c <= lastCol; c++)
            {
                string text = row.Cell(c).GetString().Trim().ToLowerInvariant();
                if (MatchHeader(text, map, c))
                    matched++;
            }

            if (matched >= 3)
            {
                dataStartRow = r + 1;
                return map;
            }
        }

        dataStartRow = 2;
        return ColumnHeaderMap.DefaultPositional();
    }

    private static bool MatchHeader(string text, ColumnHeaderMap map, int col)
    {
        if (text.Contains("dầm") || text.Contains("beam") || text.Contains("mark"))
        { map.BeamMarkCol = col; return true; }
        if (text.Contains("m_left") || text.Contains("m_gối_trái") || text.Contains("m_goi_trai") || text.Contains("m1"))
        { map.MLeftCol = col; return true; }
        if (text.Contains("m_mid") || text.Contains("m_nhịp") || text.Contains("m_nhip") || text.Contains("m2"))
        { map.MMidCol = col; return true; }
        if (text.Contains("m_right") || text.Contains("m_gối_phải") || text.Contains("m_goi_phai") || text.Contains("m3"))
        { map.MRightCol = col; return true; }
        if (text.Contains("q_left") || text.Contains("q_gối_trái") || text.Contains("v_left") || text.Contains("q1"))
        { map.QLeftCol = col; return true; }
        if (text.Contains("q_mid") || text.Contains("q_nhịp") || text.Contains("v_mid") || text.Contains("q2"))
        { map.QMidCol = col; return true; }
        if (text.Contains("q_right") || text.Contains("q_gối_phải") || text.Contains("v_right") || text.Contains("q3"))
        { map.QRightCol = col; return true; }
        if (text.Contains("dài") || text.Contains("length") || text.Contains("spanlength"))
        { map.LengthCol = col; return true; }
        if (text.Equals("nhịp") || text.Equals("span") || (text.Contains("nhịp") && !text.Contains("m_") && !text.Contains("q_") && !text.Contains("v_")))
        { map.SpanIndexCol = col; return true; }

        return false;
    }

    private static string ReadString(IXLRow row, int col)
    {
        if (col <= 0) return string.Empty;
        var cell = row.Cell(col);
        return cell.IsEmpty() ? string.Empty : cell.GetString().Trim();
    }

    private static double ReadDouble(IXLRow row, int col, double defaultValue)
    {
        if (col <= 0) return defaultValue;
        var cell = row.Cell(col);
        if (cell.IsEmpty()) return defaultValue;

        if (cell.DataType == XLDataType.Number && cell.TryGetValue<double>(out double val))
            return double.IsNaN(val) || double.IsInfinity(val) ? defaultValue : val;

        string str = cell.GetString().Trim();
        if (double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedInv) &&
            !double.IsNaN(parsedInv) && !double.IsInfinity(parsedInv))
            return parsedInv;

        if (double.TryParse(str, NumberStyles.Float, CultureInfo.GetCultureInfo("vi-VN"), out double parsedVi) &&
            !double.IsNaN(parsedVi) && !double.IsInfinity(parsedVi))
            return parsedVi;

        return defaultValue;
    }

    private static int ReadInt(IXLRow row, int col, int defaultValue)
    {
        if (col <= 0) return defaultValue;
        var cell = row.Cell(col);
        if (cell.IsEmpty()) return defaultValue;

        if (cell.TryGetValue<int>(out int val))
            return val;

        string str = cell.GetString().Trim();
        return int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
            ? parsed
            : defaultValue;
    }

    private sealed class ColumnHeaderMap
    {
        public int BeamMarkCol { get; set; } = 1;
        public int SpanIndexCol { get; set; } = 2;
        public int LengthCol { get; set; } = 3;
        public int MLeftCol { get; set; } = 4;
        public int MMidCol { get; set; } = 5;
        public int MRightCol { get; set; } = 6;
        public int QLeftCol { get; set; } = 7;
        public int QMidCol { get; set; } = 8;
        public int QRightCol { get; set; } = 9;

        public static ColumnHeaderMap DefaultPositional() => new()
        {
            BeamMarkCol = 1,
            SpanIndexCol = 2,
            LengthCol = 3,
            MLeftCol = 4,
            MMidCol = 5,
            MRightCol = 6,
            QLeftCol = 7,
            QMidCol = 8,
            QRightCol = 9
        };
    }
}
