using System;
using System.IO;
using ClosedXML.Excel;
using HPKata.Core.Configuration;
using HPKata.Revit.Abstractions;
using HPKata.Revit.Runners;
using HPKata.Revit.Tests.Mocks;
using Xunit;

namespace HPKata.Revit.Tests.Runners;

public sealed class RevitExportExcelRunnerTests
{
    private static MockRevitDocumentContext CreateTestContext()
    {
        var context = new MockRevitDocumentContext
        {
            DocumentTitle = "DuAnTest"
        };
        context.AllRebars.Add(new RevitRebarDto
        {
            ElementId = 1,
            Partition = "Dam_Tang2",
            Mark = "1",
            Quantity = 4,
            DiameterMm = 20,
            SingleBarLengthMm = 5800,
            ShapeName = "M_00"
        });
        context.AllRebars.Add(new RevitRebarDto
        {
            ElementId = 2,
            Partition = "Dam_Tang2",
            Mark = "2",
            Quantity = 8,
            DiameterMm = 16,
            SingleBarLengthMm = 2400,
            ShapeName = "M_02"
        });
        return context;
    }

    [Fact]
    public void Execute_GeneratesValidExcelFile()
    {
        var context = CreateTestContext();
        string tempPath = Path.Combine(Path.GetTempPath(), $"TestRebar_{Guid.NewGuid():N}.xlsx");

        try
        {
            string resultPath = RevitExportExcelRunner.Execute(context, tempPath);

            Assert.Equal(tempPath, resultPath);
            Assert.True(File.Exists(tempPath), "Excel output file must exist on disk.");
            Assert.True(new FileInfo(tempPath).Length > 0, "Excel output file must not be empty.");
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public void Execute_ClosedXmlCanOpenAndValidateWorksheet()
    {
        var context = CreateTestContext();
        string tempPath = Path.Combine(Path.GetTempPath(), $"TestRebar_{Guid.NewGuid():N}.xlsx");

        try
        {
            RevitExportExcelRunner.Execute(context, tempPath);

            using var workbook = new XLWorkbook(tempPath);
            var sheet = workbook.Worksheet("Thong_ke");
            Assert.NotNull(sheet);

            // Check header cell
            Assert.Equal("BẢNG THỐNG KÊ CỐT THÉP", sheet.Cell("A1").GetString());
            // Check row contents
            Assert.Equal("Dam_Tang2", sheet.Cell("A4").GetString());
            Assert.Equal("1", sheet.Cell("B4").GetString());
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public void Execute_ResolvesOutputPathFromContextTitle()
    {
        string isolatedDir = Path.Combine(Path.GetTempPath(), $"HPKataTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(isolatedDir);
        using var envScope = KataInstallEnvironment.UseRootDirectory(isolatedDir);

        var context = CreateTestContext();
        string generatedPath = RevitExportExcelRunner.Execute(context, outputFilePath: null);

        try
        {
            Assert.True(File.Exists(generatedPath), "Generated Excel file must exist on disk.");
            Assert.Contains("DuAnTest", generatedPath);
        }
        finally
        {
            if (File.Exists(generatedPath) && !IsMasterInstallationAsset(generatedPath))
            {
                File.Delete(generatedPath);
            }
            if (Directory.Exists(isolatedDir))
            {
                Directory.Delete(isolatedDir, recursive: true);
            }
        }
    }

    private static bool IsMasterInstallationAsset(string path)
    {
        string defaultRoot = KataInstallEnvironment.DefaultRootDirectory;
        return path.StartsWith(defaultRoot, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CleansUpGeneratedArtifacts()
    {
        var context = CreateTestContext();
        string tempPath = Path.Combine(Path.GetTempPath(), $"TestRebar_{Guid.NewGuid():N}.xlsx");

        RevitExportExcelRunner.Execute(context, tempPath);
        Assert.True(File.Exists(tempPath));

        File.Delete(tempPath);
        Assert.False(File.Exists(tempPath));
    }
}
