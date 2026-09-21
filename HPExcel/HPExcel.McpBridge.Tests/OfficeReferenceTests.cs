using HPExcel.McpBridge.Host;
using Xunit;

namespace HPExcel.McpBridge.Tests;

public class OfficeReferenceTests
{
    [Fact]
    public void OfficeAssemblies_ResolveWithoutFileNotFoundException()
    {
        var excelAsm = typeof(Microsoft.Office.Interop.Excel.Application).Assembly;
        Assert.NotNull(excelAsm);

        var officeAsm = typeof(Microsoft.Office.Core.MsoTriState).Assembly;
        Assert.NotNull(officeAsm);
    }

    [Fact]
    public void CreateDefaultCompiler_InitializesWithoutException()
    {
        var compiler = ExcelBridgeExecutor.CreateDefaultCompiler();
        Assert.NotNull(compiler);
    }
}
