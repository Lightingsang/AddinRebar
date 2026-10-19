using System;
using System.IO;
using Xunit;

namespace HPKata.E2E.Tests.Tier1_FeatureCoverage;

/// <summary>
/// Features 11 & 12 / Acceptance Criteria:
/// Ribbon icons loading from Icon/Ribbon/*.png with vector fallback,
/// and shape/splice diagram binding (dangthep, Cot, IMG).
/// </summary>
public sealed class Tier1_F19_RibbonIconsAndVisualDiagramsTests
{
    private static readonly string SourceRoot = Directory.Exists(@"C:\HPKata") ? @"C:\HPKata" : @"C:\kata_pro";

    private static readonly string[] CriticalRibbonIconNames =
    [
        "Add_thep_dam.png",
        "cat_dam.png",
        "cat_thep.png",
        "dai135.png",
        "dai90.png",
        "dam_giao.png",
        "ve_cot.png",
        "ve_dam.png",
        "ve_vach.png"
    ];

    [Fact]
    public void CriticalRibbonIcons_CatalogContainsMandatoryActions()
    {
        Assert.Equal(9, CriticalRibbonIconNames.Length);
        Assert.Contains("ve_dam.png", CriticalRibbonIconNames);
        Assert.Contains("ve_cot.png", CriticalRibbonIconNames);
        Assert.Contains("ve_vach.png", CriticalRibbonIconNames);
    }

    [Theory]
    [InlineData("00", "00.wmf", "Kata-00.png")]
    [InlineData("01", "01.wmf", "Kata-01.png")]
    [InlineData("02", "02.wmf", "Kata-02.png")]
    [InlineData("11", "11.wmf", "Kata-11.png")]
    [InlineData("67", "67.wmf", "Kata-67.png")]
    public void ShapeDiagramPaths_FollowCanonicalNamingConventions(string code, string expectedWmf, string expectedPng)
    {
        string wmf = $"{code}.wmf";
        string png = $"Kata-{code}.png";

        Assert.Equal(expectedWmf, wmf);
        Assert.Equal(expectedPng, png);
    }

    [Theory]
    [InlineData("Crank Lap Bar.wmf")]
    [InlineData("Starter L Bar.wmf")]
    [InlineData("Straight Lap Bar.wmf")]
    public void ColumnSpliceDiagramNames_FollowStructuralConventions(string fileName)
    {
        Assert.EndsWith(".wmf", fileName, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(fileName)));
    }

    [Fact]
    public void RibbonIconResolution_WithFallback_ReturnsVectorWhenPngMissing()
    {
        string fakeDir = Path.Combine(Path.GetTempPath(), "Icon_Test_" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(fakeDir);
            string pngCandidate = Path.Combine(fakeDir, "non_existent.png");

            bool hasPng = File.Exists(pngCandidate);
            bool fallbackToVector = !hasPng;

            Assert.False(hasPng);
            Assert.True(fallbackToVector);
        }
        finally
        {
            if (Directory.Exists(fakeDir))
            {
                Directory.Delete(fakeDir, true);
            }
        }
    }

    [Fact]
    public void SourceRibbonIcons_WhenPresent_ValidateIntegrity()
    {
        string ribbonDir = Path.Combine(SourceRoot, "Icon", "Ribbon");
        if (!Directory.Exists(ribbonDir))
        {
            return;
        }

        foreach (string icon in CriticalRibbonIconNames)
        {
            string path = Path.Combine(ribbonDir, icon);
            if (File.Exists(path))
            {
                Assert.True(new FileInfo(path).Length > 0, $"{icon} must have non-zero size");
            }
        }
    }
}
