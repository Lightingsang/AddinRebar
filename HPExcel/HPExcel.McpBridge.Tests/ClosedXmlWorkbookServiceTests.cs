using System;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using HPExcel.McpBridge.Headless;
using Xunit;

namespace HPExcel.McpBridge.Tests;

/// <summary>
///     Comprehensive unit and integration tests for ClosedXmlWorkbookService.
///     Verifies headless 2D range read/write across diverse data types, table creation,
///     formula evaluation, worksheet metadata inspection, and edge case error handling.
/// </summary>
public sealed class ClosedXmlWorkbookServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly ClosedXmlWorkbookService _service;

    public ClosedXmlWorkbookServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "ClosedXmlTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _service = new ClosedXmlWorkbookService();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch { }
    }

    private string CreateSampleWorkbook(string fileName, Action<IXLWorksheet> populate)
    {
        var filePath = Path.Combine(_testDir, fileName);
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("DataSheet");
        populate(ws);
        workbook.SaveAs(filePath);
        return filePath;
    }

    #region 1. 2D Range Read & Write Data Types

    [Fact]
    public void WriteAndReadRange_PreservesAllPrimitiveDataTypes()
    {
        var filePath = Path.Combine(_testDir, "types_test.xlsx");
        var now = new DateTime(2026, 9, 21, 10, 30, 0);

        object[,] inputData = new object[,]
        {
            { "StringCol", "IntCol", "DoubleCol", "DecimalCol", "BoolCol", "DateCol", "NullCol" },
            { "Beam B1", 120, 45.75, 199.99m, true, now, null! }
        };

        // Write
        var writeResult = _service.WriteRange(filePath, "TypesSheet", "A1", inputData, autoFit: true);
        Assert.True(writeResult.Success);
        Assert.Equal(2, writeResult.RowsWritten);
        Assert.Equal(7, writeResult.ColsWritten);
        Assert.Equal("TypesSheet", writeResult.Sheet);

        // Read with headers
        var readResult = _service.ReadRange(filePath, "TypesSheet", "A1:G2", hasHeaders: true);
        Assert.True(readResult.HasHeaders);
        Assert.Equal(7, readResult.Headers!.Count);
        Assert.Single(readResult.Data);

        var row = (Dictionary<string, object?>)readResult.Data[0];
        Assert.Equal("Beam B1", row["StringCol"]?.ToString());
        Assert.Equal(120.0, Convert.ToDouble(row["IntCol"]));
        Assert.Equal(45.75, Convert.ToDouble(row["DoubleCol"]));
        Assert.Equal(199.99, Convert.ToDouble(row["DecimalCol"]));
        Assert.True(Convert.ToBoolean(row["BoolCol"]));
        Assert.Contains("2026-09-21", row["DateCol"]?.ToString());
        Assert.Null(row["NullCol"]);
    }

    [Fact]
    public void ReadRange_WithoutHeaders_ReturnsRawObjectGrid()
    {
        var filePath = CreateSampleWorkbook("raw_grid.xlsx", ws =>
        {
            ws.Cell("A1").Value = "X";
            ws.Cell("B1").Value = 10;
            ws.Cell("A2").Value = "Y";
            ws.Cell("B2").Value = 20;
        });

        var result = _service.ReadRange(filePath, "DataSheet", "A1:B2", hasHeaders: false);
        Assert.False(result.HasHeaders);
        Assert.Null(result.Headers);
        Assert.Equal(2, result.RowCount);
        Assert.Equal(2, result.ColCount);

        var row1 = (List<object?>)result.Data[0];
        var row2 = (List<object?>)result.Data[1];
        Assert.Equal("X", row1[0]);
        Assert.Equal(10.0, Convert.ToDouble(row1[1]));
        Assert.Equal("Y", row2[0]);
        Assert.Equal(20.0, Convert.ToDouble(row2[1]));
    }

    [Fact]
    public void ReadRange_RespectsMaxRowsConstraint()
    {
        var filePath = CreateSampleWorkbook("many_rows.xlsx", ws =>
        {
            for (int r = 1; r <= 50; r++)
            {
                ws.Cell(r, 1).Value = $"Row_{r}";
            }
        });

        var result = _service.ReadRange(filePath, "DataSheet", "A1:A50", hasHeaders: false, maxRows: 10);
        Assert.Equal(10, result.RowCount);
        Assert.Equal(10, result.Data.Count);
    }

    [Fact]
    public void WriteRange_WithFormula_SavesFormulaCorrectly()
    {
        var filePath = Path.Combine(_testDir, "formulas_write.xlsx");
        object[,] data = new object[,]
        {
            { 10, 20, "=A1+B1" }
        };

        var writeResult = _service.WriteRange(filePath, "Sheet1", "A1", data, isFormula: true);
        Assert.True(writeResult.Success);

        // Verify with ClosedXML workbook that formula is evaluated
        using var wb = new XLWorkbook(filePath);
        var cell = wb.Worksheet("Sheet1").Cell("C1");
        Assert.True(cell.HasFormula);
        Assert.Equal("A1+B1", cell.FormulaA1);
    }

    #endregion

    #region 2. Table Creation

    [Fact]
    public void CreateTable_ConvertsRangeToExcelTableWithCustomStyleAndTotals()
    {
        var filePath = CreateSampleWorkbook("table_source.xlsx", ws =>
        {
            ws.Cell("A1").Value = "Item";
            ws.Cell("B1").Value = "Quantity";
            ws.Cell("A2").Value = "Rebar D16";
            ws.Cell("B2").Value = 50;
            ws.Cell("A3").Value = "Rebar D20";
            ws.Cell("B3").Value = 30;
        });

        var tableResult = _service.CreateTable(
            filePath,
            "DataSheet",
            "A1:B3",
            "RebarTable",
            styleName: "TableStyleMedium2",
            showTotals: true);

        Assert.Equal("RebarTable", tableResult.TableName);
        Assert.Equal("DataSheet", tableResult.Sheet);
        Assert.Equal(2, tableResult.ColumnsCount);
        Assert.Equal(2, tableResult.RowsCount);
        Assert.True(tableResult.HasTotalsRow);

        // Verify table persisted in file
        using var wb = new XLWorkbook(filePath);
        var table = wb.Worksheet("DataSheet").Tables.FirstOrDefault(t => t.Name == "RebarTable");
        Assert.NotNull(table);
        Assert.True(table.ShowTotalsRow);
        Assert.Equal(XLTableTheme.TableStyleMedium2, table.Theme);
    }

    #endregion

    #region 3. Formula Evaluation

    [Fact]
    public void EvaluateFormula_ComplexFunctions_ReturnsAccurateResults()
    {
        var filePath = CreateSampleWorkbook("calc_tests.xlsx", ws =>
        {
            ws.Cell("A1").Value = 10;
            ws.Cell("A2").Value = 20;
            ws.Cell("A3").Value = 30;
            ws.Cell("A4").Value = 40;
            ws.Cell("A5").Value = 50;
        });

        // 1. SUM
        var sumResult = _service.EvaluateFormula(filePath, "DataSheet", "=SUM(A1:A5)");
        Assert.Equal(150.0, Convert.ToDouble(sumResult));

        // 2. AVERAGE
        var avgResult = _service.EvaluateFormula(filePath, "DataSheet", "AVERAGE(A1:A5)");
        Assert.Equal(30.0, Convert.ToDouble(avgResult));

        // 3. Logical IF
        var ifResult = _service.EvaluateFormula(filePath, "DataSheet", "=IF(A1 > 5, \"Large\", \"Small\")");
        Assert.Equal("Large", ifResult?.ToString());

        // 4. In context of target cell
        var ctxResult = _service.EvaluateFormula(filePath, "DataSheet", "=A1*A2", contextCell: "B1");
        Assert.Equal(200.0, Convert.ToDouble(ctxResult));
    }

    #endregion

    #region 4. Worksheet Metadata Inspection

    [Fact]
    public void GetWorksheetInfo_ReturnsDetailedMetadataForAllSheets()
    {
        var filePath = Path.Combine(_testDir, "info_test.xlsx");
        using (var wb = new XLWorkbook())
        {
            var ws1 = wb.Worksheets.Add("VisibleSheet");
            ws1.Cell("A1").Value = "Test";
            ws1.Range("A1:B2").CreateTable("TableOne");

            var ws2 = wb.Worksheets.Add("HiddenSheet");
            ws2.Visibility = XLWorksheetVisibility.Hidden;
            ws2.Cell("A1").Value = 123;

            wb.SaveAs(filePath);
        }

        var sheetsInfo = _service.GetWorksheetInfo(filePath);
        Assert.Equal(2, sheetsInfo.Count);

        var sheet1 = sheetsInfo.Single(s => s.Name == "VisibleSheet");
        Assert.Equal("Visible", sheet1.Visibility);
        Assert.Contains("TableOne", sheet1.Tables);

        var sheet2 = sheetsInfo.Single(s => s.Name == "HiddenSheet");
        Assert.Equal("Hidden", sheet2.Visibility);
    }

    #endregion

    #region 5. Error Edge Cases

    [Fact]
    public void Operations_OnNonExistentFile_ThrowFileNotFoundException()
    {
        var nonExistent = Path.Combine(_testDir, "ghost_file.xlsx");

        Assert.Throws<FileNotFoundException>(() => _service.ReadRange(nonExistent));
        Assert.Throws<FileNotFoundException>(() => _service.GetWorksheetInfo(nonExistent));
        Assert.Throws<FileNotFoundException>(() => _service.CreateTable(nonExistent, "Sheet1", "A1:B2", "T1"));
        Assert.Throws<FileNotFoundException>(() => _service.EvaluateFormula(nonExistent, "Sheet1", "=1+1"));
    }

    [Fact]
    public void WriteRange_WithEmptyValues_ThrowsArgumentException()
    {
        var filePath = Path.Combine(_testDir, "empty_values.xlsx");
        var emptyData = new object[0, 0];

        Assert.Throws<ArgumentException>(() => _service.WriteRange(filePath, "Sheet1", "A1", emptyData));
    }

    [Fact]
    public void ReadRange_WhenWorksheetNotFound_ThrowsArgumentException()
    {
        var filePath = CreateSampleWorkbook("missing_sheet.xlsx", ws => ws.Cell("A1").Value = 1);

        Assert.Throws<ArgumentException>(() => _service.ReadRange(filePath, sheetName: "NoSuchSheet"));
    }

    #endregion
}
