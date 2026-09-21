using System;
using System.IO;
using ClosedXML.Excel;
using HPExcel.McpBridge.Headless;
using Xunit;

namespace HPExcel.McpBridge.Tests;

public class ClosedXmlFormulaTests : IDisposable
{
    private readonly string _tempFile;

    public ClosedXmlFormulaTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"test_formula_{Guid.NewGuid():N}.xlsx");
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sheet1");
        ws.Cell("A1").Value = 10;
        ws.Cell("A2").Value = 20;
        workbook.SaveAs(_tempFile);
    }

    public void Dispose()
    {
        try { if (File.Exists(_tempFile)) File.Delete(_tempFile); } catch { }
    }

    [Fact]
    public void EvaluateFormula_ReturnsPrimitiveDouble()
    {
        var service = new ClosedXmlWorkbookService();
        var result = service.EvaluateFormula(_tempFile, "Sheet1", "=1+1");

        Assert.NotNull(result);
        Assert.IsType<double>(result);
        Assert.Equal(2.0, (double)result);

        // Verify IConvertible compatibility: Convert.ToDouble must not throw InvalidCastException
        var conv = Convert.ToDouble(result);
        Assert.Equal(2.0, conv);
    }

    [Fact]
    public void EvaluateFormula_CellReference_ReturnsSum()
    {
        var service = new ClosedXmlWorkbookService();
        var result = service.EvaluateFormula(_tempFile, "Sheet1", "=SUM(A1:A2)");

        Assert.NotNull(result);
        var conv = Convert.ToDouble(result);
        Assert.Equal(30.0, conv);
    }
}
