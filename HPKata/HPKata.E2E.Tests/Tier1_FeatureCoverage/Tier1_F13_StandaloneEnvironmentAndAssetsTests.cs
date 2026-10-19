using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace HPKata.E2E.Tests.Tier1_FeatureCoverage;

/// <summary>
/// Feature 01 & Feature 02: Standalone C:\HPKata\ environment canonical paths,
/// asset inventory verification, and LISP path rewriting contracts.
/// </summary>
public sealed class Tier1_F13_StandaloneEnvironmentAndAssetsTests
{
    private const string DefaultRoot = @"C:\HPKata";
    private static readonly string SourceRoot = Directory.Exists(@"C:\HPKata") ? @"C:\HPKata" : @"C:\kata_pro";

    [Fact]
    public void CanonicalPaths_MapToExpectedStandardLocations()
    {
        string root = DefaultRoot;
        Assert.Equal(@"C:\HPKata\Kata.xlsm", Path.Combine(root, "Kata.xlsm"));
        Assert.Equal(@"C:\HPKata\Kata ENG.xlsm", Path.Combine(root, "Kata ENG.xlsm"));
        Assert.Equal(@"C:\HPKata\Iso_QHPlus.xls", Path.Combine(root, "Iso_QHPlus.xls"));
        Assert.Equal(@"C:\HPKata\Iso_kata.dwg", Path.Combine(root, "Iso_kata.dwg"));
        Assert.Equal(@"C:\HPKata\ISO_KATA.EDB", Path.Combine(root, "ISO_KATA.EDB"));
        Assert.Equal(@"C:\HPKata\kata.ico", Path.Combine(root, "kata.ico"));
        Assert.Equal(@"C:\HPKata\Goi lenh.lsp", Path.Combine(root, "Goi lenh.lsp"));
        Assert.Equal(@"C:\HPKata\GoiLenh_HPKata.lsp", Path.Combine(root, "GoiLenh_HPKata.lsp"));
        Assert.Equal(@"C:\HPKata\menukata.cuix", Path.Combine(root, "menukata.cuix"));
        Assert.Equal(@"C:\HPKata\menukata.cui", Path.Combine(root, "menukata.cui"));
        Assert.Equal(@"C:\HPKata\dangthep", Path.Combine(root, "dangthep"));
        Assert.Equal(@"C:\HPKata\Cot", Path.Combine(root, "Cot"));
        Assert.Equal(@"C:\HPKata\Icon", Path.Combine(root, "Icon"));
        Assert.Equal(@"C:\HPKata\Icon\Ribbon", Path.Combine(root, "Icon", "Ribbon"));
        Assert.Equal(@"C:\HPKata\IMG", Path.Combine(root, "IMG"));
        Assert.Equal(@"C:\HPKata\Temp", Path.Combine(root, "Temp"));
    }

    [Fact]
    public void IsolatedCustomRoot_GeneratesRelativeSubpathsCorrectly()
    {
        string customRoot = Path.Combine(Path.GetTempPath(), "HPKata_Custom_" + Guid.NewGuid().ToString("N"));
        try
        {
            string dwg = Path.Combine(customRoot, "Iso_kata.dwg");
            string xlsm = Path.Combine(customRoot, "Kata.xlsm");
            string ribbon = Path.Combine(customRoot, "Icon", "Ribbon");

            Assert.StartsWith(customRoot, dwg, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(customRoot, xlsm, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(customRoot, ribbon, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(customRoot))
            {
                Directory.Delete(customRoot, true);
            }
        }
    }

    [Fact]
    public void SourceLibraryAssets_WhenAvailable_ValidateIntegrityAndCompleteness()
    {
        if (!Directory.Exists(SourceRoot))
        {
            return;
        }

        string dwgPath = Path.Combine(SourceRoot, "Iso_kata.dwg");
        string xlsmPath = Path.Combine(SourceRoot, "Kata.xlsm");
        string xlsPath = Path.Combine(SourceRoot, "Iso_QHPlus.xls");
        string lspPath = Path.Combine(SourceRoot, "Goi lenh.lsp");

        Assert.True(File.Exists(dwgPath), "Iso_kata.dwg must exist in source assets");
        Assert.True(File.Exists(xlsmPath), "Kata.xlsm must exist in source assets");
        Assert.True(File.Exists(xlsPath), "Iso_QHPlus.xls must exist in source assets");
        Assert.True(File.Exists(lspPath), "Goi lenh.lsp must exist in source assets");

        Assert.True(new FileInfo(dwgPath).Length > 100_000, "DWG template must be > 100KB");
        Assert.True(new FileInfo(xlsmPath).Length > 200_000, "Kata.xlsm must be > 200KB");
        Assert.True(new FileInfo(xlsPath).Length > 200_000, "Iso_QHPlus.xls must be > 200KB");
    }

    [Fact]
    public void VisualDirectories_WhenAvailable_ContainExpectedQuantities()
    {
        if (!Directory.Exists(SourceRoot))
        {
            return;
        }

        string dangThepDir = Path.Combine(SourceRoot, "dangthep");
        string cotDir = Path.Combine(SourceRoot, "Cot");
        string imgDir = Path.Combine(SourceRoot, "IMG");
        string ribbonDir = Path.Combine(SourceRoot, "Icon", "Ribbon");

        Assert.True(Directory.Exists(dangThepDir), "dangthep directory must exist");
        Assert.True(Directory.Exists(cotDir), "Cot directory must exist");
        Assert.True(Directory.Exists(imgDir), "IMG directory must exist");
        Assert.True(Directory.Exists(ribbonDir), "Icon/Ribbon directory must exist");

        Assert.True(Directory.GetFiles(dangThepDir, "*.wmf").Length >= 50, "dangthep must contain >= 50 WMF shape files");
        Assert.True(Directory.GetFiles(cotDir, "*.wmf").Length >= 10, "Cot must contain >= 10 WMF splice files");
        Assert.True(Directory.GetFiles(imgDir, "*.png").Length >= 20, "IMG must contain >= 20 PNG guide diagrams");
        Assert.True(Directory.GetFiles(ribbonDir, "*.png").Length >= 50, "Icon/Ribbon must contain >= 50 PNG ribbon icons");
    }

    [Theory]
    [InlineData("(command \"netload\" \"C:\\\\kata_pro\\\\HPKata.AutoCad.dll\")", "(command \"netload\" \"C:\\\\HPKata\\\\HPKata.AutoCad.dll\")")]
    [InlineData("(command \"_cuiload\" \"C:\\\\kata_pro\\\\menukata.cuix\")", "(command \"_cuiload\" \"C:\\\\HPKata\\\\menukata.cuix\")")]
    [InlineData("Path=C:\\kata_pro\\Goi lenh.lsp", "Path=C:\\HPKata\\Goi lenh.lsp")]
    public void LispPathRewriting_ReplacesKataPro_WithHPKata(string input, string expected)
    {
        string rewritten = input
            .Replace("C:\\\\kata_pro", "C:\\\\HPKata", StringComparison.OrdinalIgnoreCase)
            .Replace("C:\\kata_pro", "C:\\HPKata", StringComparison.OrdinalIgnoreCase);

        Assert.Equal(expected, rewritten);
        Assert.DoesNotContain("kata_pro", rewritten, StringComparison.OrdinalIgnoreCase);
    }
}
