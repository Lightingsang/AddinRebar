using System;
using System.Collections.Generic;

namespace HPExcel.McpBridge.Safety;

/// <summary>
///     Table of known Microsoft Excel COM and ClosedXML member classifications for 3-tier safety analysis.
/// </summary>
public static class ExcelTierTable
{
    private static readonly Dictionary<string, ExcelTier> KnownMembers = new(StringComparer.OrdinalIgnoreCase)
    {
        // Tier R: Read-only members
        ["Range.Value"] = ExcelTier.ReadOnly,
        ["Range.Value2"] = ExcelTier.ReadOnly,
        ["Range.Text"] = ExcelTier.ReadOnly,
        ["Range.Formula"] = ExcelTier.ReadOnly,
        ["Range.Address"] = ExcelTier.ReadOnly,
        ["Range.Rows"] = ExcelTier.ReadOnly,
        ["Range.Columns"] = ExcelTier.ReadOnly,
        ["Range.Cells"] = ExcelTier.ReadOnly,
        ["Range.Find"] = ExcelTier.ReadOnly,
        ["Range.FindNext"] = ExcelTier.ReadOnly,
        ["Worksheet.UsedRange"] = ExcelTier.ReadOnly,
        ["Worksheet.Name"] = ExcelTier.ReadOnly,
        ["Worksheet.Index"] = ExcelTier.ReadOnly,
        ["Worksheet.Visible"] = ExcelTier.ReadOnly,
        ["Worksheet.ProtectContents"] = ExcelTier.ReadOnly,
        ["Worksheets.Count"] = ExcelTier.ReadOnly,
        ["Workbook.Name"] = ExcelTier.ReadOnly,
        ["Workbook.FullName"] = ExcelTier.ReadOnly,
        ["Workbook.Path"] = ExcelTier.ReadOnly,
        ["ListObjects.Count"] = ExcelTier.ReadOnly,
        ["ListObject.Name"] = ExcelTier.ReadOnly,
        ["ListObject.HeaderRowRange"] = ExcelTier.ReadOnly,
        ["ListObject.DataBodyRange"] = ExcelTier.ReadOnly,
        ["ListObject.TotalsRowRange"] = ExcelTier.ReadOnly,
        ["ChartObjects.Count"] = ExcelTier.ReadOnly,

        // Tier W: Write & Formatting members
        ["Range.FormulaR1C1"] = ExcelTier.Write,
        ["Range.NumberFormat"] = ExcelTier.Write,
        ["Range.AutoFit"] = ExcelTier.Write,
        ["Range.Resize"] = ExcelTier.Write,
        ["Range.Sort"] = ExcelTier.Write,
        ["Range.Insert"] = ExcelTier.Write,
        ["Range.Merge"] = ExcelTier.Write,
        ["Range.UnMerge"] = ExcelTier.Write,
        ["Range.Replace"] = ExcelTier.Write,
        ["Range.PasteSpecial"] = ExcelTier.Write,
        ["Worksheet.Protect"] = ExcelTier.Write,
        ["Worksheet.Unprotect"] = ExcelTier.Write,
        ["Workbook.Protect"] = ExcelTier.Write,
        ["Workbook.Unprotect"] = ExcelTier.Write,
        ["Font.Bold"] = ExcelTier.Write,
        ["Font.Italic"] = ExcelTier.Write,
        ["Font.Size"] = ExcelTier.Write,
        ["Font.Color"] = ExcelTier.Write,
        ["Interior.Color"] = ExcelTier.Write,
        ["Borders.LineStyle"] = ExcelTier.Write,
        ["Worksheets.Add"] = ExcelTier.Write,
        ["Worksheet.Copy"] = ExcelTier.Write,
        ["ListObjects.Add"] = ExcelTier.Write,
        ["ChartObjects.Add"] = ExcelTier.Write,
        ["Workbook.Save"] = ExcelTier.Write,
        ["Workbook.SaveAs"] = ExcelTier.Write,
        ["Workbook.SaveCopyAs"] = ExcelTier.Write,

        // Tier D: Destructive members
        ["Worksheet.Delete"] = ExcelTier.Destructive,
        ["Range.Delete"] = ExcelTier.Destructive,
        ["Range.Clear"] = ExcelTier.Destructive,
        ["Range.ClearContents"] = ExcelTier.Destructive,
        ["Range.ClearFormats"] = ExcelTier.Destructive,
        ["Range.ClearComments"] = ExcelTier.Destructive,
        ["Application.Run"] = ExcelTier.Destructive
    };

    /// <summary>
    ///     Classifies a member access or invocation into an ExcelTier.
    /// </summary>
    public static ExcelTier Classify(string memberName)
    {
        if (KnownMembers.TryGetValue(memberName, out var tier))
            return tier;

        // Strip prefix if full qualified
        var dot = memberName.LastIndexOf('.');
        var simpleName = dot >= 0 ? memberName[(dot + 1)..] : memberName;

        if (simpleName.Equals("Delete", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("Clear", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("ClearContents", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("ClearFormats", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("ClearComments", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("Run", StringComparison.OrdinalIgnoreCase))
        {
            return ExcelTier.Destructive;
        }

        if (simpleName.Equals("Add", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("Copy", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("Save", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("SaveAs", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("SaveCopyAs", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("AutoFit", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("NumberFormat", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("Sort", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("Insert", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("Merge", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("UnMerge", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("Replace", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("PasteSpecial", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("Protect", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("Unprotect", StringComparison.OrdinalIgnoreCase))
        {
            return ExcelTier.Write;
        }

        // Fail-closed default: if unknown mutation, classify as Write
        return ExcelTier.ReadOnly;
    }
}
