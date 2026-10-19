using System.IO;
using System.Linq;
using HPKata.Core.Excel;
using HPKata.Core.Excel.Data;
using HPKata.Installer.Services;
using Xunit;

namespace HPKata.E2E.Tests.Tier1_FeatureCoverage;

/// <summary>
/// Tier 1 F19 — Pure C# verification of reading Beam B01 from C:\HPKata\Kata.xlsm
/// and building the AutoCAD MCP C# execution payload.
/// </summary>
public sealed class Tier1_F19_AutocadMcpBeamFromExcelTests
{
    private const string InstalledKataXlsm = @"C:\HPKata\Kata.xlsm";

    [Fact]
    public void ReadBeamB01FromKataXlsm_ProducesExactSixSpansAndParameters()
    {
        if (!File.Exists(InstalledKataXlsm)) return;

        bool ok = KataExcelAdapterProvider.Current.ReadBeamSheet(
            InstalledKataXlsm, out KataBeamSheetData data);

        Assert.True(ok);
        Assert.Equal("B01", data.BeamMark);
        Assert.Equal(500.0, data.DefaultWidthMm);
        Assert.Equal(1100.0, data.DefaultHeightMm);
        Assert.Equal("6f25", data.TopContinuousRebar);
        Assert.Equal("6f25", data.BotContinuousRebar);
        Assert.Equal(6, data.Spans.Count);
        Assert.Equal(31600.0, data.Spans.Sum(s => s.ClearLengthMm));
        Assert.Equal(7, data.Supports.Count);

        var parameters = AutocadBeamScriptBuilder.BuildParameters(data, @"C:\HPKata\HPKata_Beam_B01_Demo.dwg");
        Assert.Equal("B01", parameters["beamMark"]);
        Assert.Equal(6, parameters["topCount"]);
        Assert.Equal(25, parameters["topDia"]);

        string script = AutocadBeamScriptBuilder.BuildAutoCadCSharpScript();
        Assert.Contains("KATA_THEP_CHU", script);
        Assert.Contains("MẶT CẮT 1-1 (GỐI)", script);
        Assert.Contains("MẶT CẮT 2-2 (NHỊP)", script);
        Assert.Contains("BẢNG THỐNG KÊ CỐT THÉP DẦM", script);
    }
}
