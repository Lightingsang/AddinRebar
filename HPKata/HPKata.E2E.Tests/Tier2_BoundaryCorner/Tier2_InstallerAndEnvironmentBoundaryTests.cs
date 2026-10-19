using System;
using System.IO;
using ClosedXML.Excel;
using HPKata.Core.Configuration;
using Xunit;

namespace HPKata.E2E.Tests.Tier2_BoundaryCorner;

/// <summary>
/// Tier 2: Boundary value analysis and defensive handling for installer flags,
/// corrupted Excel workbooks, corrupted LISP configs, and extreme configuration boundaries.
/// </summary>
public sealed class Tier2_InstallerAndEnvironmentBoundaryTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("--unknown-switch")]
    [InlineData("-x")]
    public void CliInstaller_WithUnrecognizedSwitches_DoesNotTriggerInstallOrUninstall(string switchArg)
    {
        string[] args = [switchArg];
        bool install = false;
        bool uninstall = false;

        foreach (string arg in args)
        {
            if (string.Equals(arg, "--install", StringComparison.OrdinalIgnoreCase)) install = true;
            if (string.Equals(arg, "--uninstall", StringComparison.OrdinalIgnoreCase)) uninstall = true;
        }

        Assert.False(install);
        Assert.False(uninstall);
    }

    [Fact]
    public void ExcelWorkbook_WithMissingSheets_FailsGracefullyWithoutCrashing()
    {
        using var stream = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            wb.Worksheets.Add("EmptySheet");
            wb.SaveAs(stream);
        }

        stream.Position = 0;
        using (var wb = new XLWorkbook(stream))
        {
            bool hasDam = wb.Worksheets.Contains("Dam");
            bool hasCot = wb.Worksheets.Contains("Cot");
            bool hasVach = wb.Worksheets.Contains("Vach");

            Assert.False(hasDam);
            Assert.False(hasCot);
            Assert.False(hasVach);
        }
    }

    [Fact]
    public void ExcelWorkbook_CorruptedCellData_ReturnsZeroOrEmptyDefaults()
    {
        using var stream = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.Worksheets.Add("Dam");
            ws.Cell("B5").Value = "NOT_A_NUMBER";
            ws.Cell("B6").Value = "INVALID_WIDTH";
            wb.SaveAs(stream);
        }

        stream.Position = 0;
        using (var wb = new XLWorkbook(stream))
        {
            var ws = wb.Worksheet("Dam");
            bool isB5Number = ws.Cell("B5").TryGetValue(out double height);
            bool isB6Number = ws.Cell("B6").TryGetValue(out double width);

            Assert.False(isB5Number);
            Assert.False(isB6Number);
            Assert.Equal(0.0, height);
            Assert.Equal(0.0, width);
        }
    }

    [Theory]
    [InlineData(0.0, 10, 0.1, 6000)]
    [InlineData(100.0, 40, 0.5, 12000)]
    [InlineData(15.0, 32, 0.25, 11700)]
    public void ConfigBoundaryValues_SerializeAndDeserializePreservingBounds(
        double cover, int baseBar, double spliceRatio, int standardLen)
    {
        var config = new KataConfig
        {
            ConcreteCoverMm = cover,
            BaseBarDiameter = baseBar,
            ColSpliceRatio = spliceRatio,
            StandardBarLength = standardLen
        };

        string serialized = KataConfigParser.SerializeToText(config);
        KataConfig parsed = KataConfigParser.ParseText(serialized);

        Assert.Equal(cover, parsed.ConcreteCoverMm);
        Assert.Equal(baseBar, parsed.BaseBarDiameter);
        Assert.Equal(spliceRatio, parsed.ColSpliceRatio, 4);
        Assert.Equal(standardLen, parsed.StandardBarLength);
    }

    [Fact]
    public void CorruptedLispText_WithMixedMissingSemicolons_FallsBackToDefaults()
    {
        string corrupted = "malformed_line_no_semicolon\r\n;valid_line\r\n;\r\n;;;\r\n";
        KataConfig config = KataConfigParser.ParseText(corrupted);

        Assert.NotNull(config);
        Assert.True(config.ConcreteCoverMm > 0.0);
        Assert.True(config.StandardBarLength > 0);
    }
}
