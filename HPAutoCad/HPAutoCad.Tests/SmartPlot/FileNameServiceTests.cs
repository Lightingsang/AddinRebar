using HPAutoCad.Core.SmartPlot.Models;
using HPAutoCad.Core.SmartPlot.Services;
using Xunit;

namespace HPAutoCad.Tests.SmartPlot;

public sealed class FileNameServiceTests
{
    private readonly FileNameService _service = new();

    [Fact]
    public void SanitizeFileName_RemovesInvalidWindowsCharacters()
    {
        var raw = "Plan:Level*1?Rev\"A\"<Final>|Print\\Test/01";
        var sanitized = _service.SanitizeFileName(raw);

        Assert.DoesNotContain(":", sanitized);
        Assert.DoesNotContain("*", sanitized);
        Assert.DoesNotContain("?", sanitized);
        Assert.DoesNotContain("\"", sanitized);
        Assert.DoesNotContain("<", sanitized);
        Assert.DoesNotContain(">", sanitized);
        Assert.DoesNotContain("|", sanitized);
        Assert.DoesNotContain("\\", sanitized);
        Assert.DoesNotContain("/", sanitized);
    }

    [Theory]
    [InlineData("CON")]
    [InlineData("con")]
    [InlineData("PRN")]
    [InlineData("AUX")]
    [InlineData("NUL")]
    [InlineData("COM1")]
    [InlineData("LPT1")]
    public void SanitizeFileName_ReservedNames_PrefixesUnderscore(string reserved)
    {
        var sanitized = _service.SanitizeFileName(reserved);
        Assert.StartsWith("_", sanitized);
        Assert.EndsWith(reserved, sanitized, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SanitizeFileName_EmptyOrWhitespace_ReturnsFallback(string? empty)
    {
        var sanitized = _service.SanitizeFileName(empty!);
        Assert.Equal("Plot_Sheet", sanitized);
    }

    [Fact]
    public void FormatFileName_SubstitutesAllTokens()
    {
        var item = new PlotItem
        {
            Id = "frame-1",
            Bounds = new PlotBounds(0, 0, 841, 594),
            LayoutName = "Layout1",
            SheetNumber = "KC-01",
            SheetTitle = "MatBangKetCau",
            Order = 3
        };

        var template = "{Prefix}_{Layout}_{SheetNo}_{Title}_{Order}_{DwgName}";
        var result = _service.FormatFileName(template, item, prefix: "HP", dwgName: "G:\\Drawings\\ProjectA.dwg");

        Assert.Equal("HP_Layout1_KC-01_MatBangKetCau_03_ProjectA", result);
    }

    [Fact]
    public void FormatFileName_EmptyOrMissingTokens_CleansConsecutiveUnderscores()
    {
        var item = new PlotItem
        {
            Id = "frame-1",
            Bounds = new PlotBounds(0, 0, 841, 594),
            LayoutName = "Model",
            SheetNumber = "A-101",
            SheetTitle = null, // Missing title
            Order = 1
        };

        // Missing prefix and title
        var template = "{Prefix}_{Layout}_{SheetNo}_{Title}";
        var result = _service.FormatFileName(template, item, prefix: null, dwgName: null);

        // Should not have double underscore "__" or leading/trailing "_"
        Assert.DoesNotContain("__", result);
        Assert.False(result.StartsWith("_"));
        Assert.False(result.EndsWith("_"));
        Assert.Equal("Model_A-101", result);
    }

    [Fact]
    public void FormatFileName_WhitespaceTemplate_UsesDefaultPattern()
    {
        var item = new PlotItem
        {
            Id = "frame-1",
            Bounds = new PlotBounds(0, 0, 841, 594),
            LayoutName = "Model",
            SheetNumber = "01",
            SheetTitle = "Overview",
            Order = 1
        };

        var result = _service.FormatFileName("", item, prefix: "DRAW");
        Assert.Equal("DRAW_Model_01_Overview", result);
    }

    [Fact]
    public void BuildFullFilePath_ValidPathWithExtension()
    {
        var folder = @"C:\Plots\Output";
        var fileName = "HP_Model_01";

        var fullPath = _service.BuildFullFilePath(folder, fileName);
        Assert.Equal(Path.Combine(folder, "HP_Model_01.pdf"), fullPath);

        // If file name already includes .pdf
        var fullPathWithExt = _service.BuildFullFilePath(folder, "HP_Model_01.pdf");
        Assert.Equal(Path.Combine(folder, "HP_Model_01.pdf"), fullPathWithExt);
    }

    [Theory]
    [InlineData("aux.pdf", "_aux.pdf")]
    [InlineData("CON.txt", "_CON.txt")]
    [InlineData("nul.dat", "_nul.dat")]
    [InlineData("COM1.dwg", "_COM1.dwg")]
    [InlineData("LPT9.log", "_LPT9.log")]
    [InlineData("prn.output.pdf", "_prn.output.pdf")]
    public void SanitizeFileName_ReservedNamesWithExtensions_PrefixesUnderscore(string raw, string expected)
    {
        var result = _service.SanitizeFileName(raw);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildFullFilePath_ReservedNameStem_ProtectsAgainstDeviceName()
    {
        var folder = @"C:\Plots";
        var fullPath = _service.BuildFullFilePath(folder, "aux.pdf", ".pdf");
        Assert.Equal(Path.Combine(folder, "_aux.pdf"), fullPath);

        var fullPath2 = _service.BuildFullFilePath(folder, "CON", ".pdf");
        Assert.Equal(Path.Combine(folder, "_CON.pdf"), fullPath2);
    }

    [Fact]
    public void FormatFileName_ReservedNameStem_ProtectsAgainstDeviceName()
    {
        var item = new PlotItem
        {
            Id = "1",
            Bounds = new PlotBounds(0, 0, 100, 100),
            SheetTitle = "CON",
            Order = 1
        };

        var result = _service.FormatFileName("{Title}", item);
        Assert.Equal("_CON", result);
    }
}
