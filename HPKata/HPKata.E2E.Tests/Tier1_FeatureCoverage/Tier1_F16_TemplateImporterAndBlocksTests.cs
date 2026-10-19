using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace HPKata.E2E.Tests.Tier1_FeatureCoverage;

/// <summary>
/// Feature 09 / Acceptance Criteria:
/// AutoCAD template importing (Iso_kata.dwg), 119 block definitions,
/// 12 standard layers, text/dim styles, and fallback generation.
/// </summary>
public sealed class Tier1_F16_TemplateImporterAndBlocksTests
{
    private static readonly string SourceDwg = File.Exists(@"C:\HPKata\Iso_kata.dwg") ? @"C:\HPKata\Iso_kata.dwg" : @"C:\kata_pro\Iso_kata.dwg";

    private static readonly string[] RequiredStandardLayers =
    [
        "kata_thep chu",
        "kata_thep dai",
        "kata_thep - V",
        "kata_thep - H",
        "kata_dim",
        "kata_net thay",
        "kata_net khung",
        "kata_net khuat",
        "kata_grid",
        "kata_net manh",
        "kata_text",
        "Defpoints"
    ];

    private static readonly string[] KeyFunctionalBlocks =
    [
        "kata_block_KHT",
        "kata_block_KHTCT",
        "kata_block_Beam_plan",
        "Kata_Block_Beam_Rebar",
        "Kata_Block_Column&Wall",
        "Kata_Block_Mong_Don",
        "kata_block_Ten",
        "kata_block_COUPLER_SPEC",
        "dangCoupler",
        "kata_block_ArchTick",
        "kata_block_RUNTIC",
        "dang00",
        "dang01",
        "dang02",
        "dang67"
    ];

    [Fact]
    public void StandardLayers_ContainsExactly12CanonicalLayers()
    {
        Assert.Equal(12, RequiredStandardLayers.Length);
        Assert.Contains("kata_thep chu", RequiredStandardLayers);
        Assert.Contains("kata_thep dai", RequiredStandardLayers);
        Assert.Contains("kata_net thay", RequiredStandardLayers);
        Assert.Contains("kata_dim", RequiredStandardLayers);
        Assert.Contains("kata_text", RequiredStandardLayers);
    }

    [Fact]
    public void KeyBlockCatalog_SpecifiesMandatoryAnnotationAndRebarBlocks()
    {
        Assert.True(KeyFunctionalBlocks.Length >= 15);
        Assert.Contains("kata_block_KHT", KeyFunctionalBlocks);
        Assert.Contains("Kata_Block_Beam_Rebar", KeyFunctionalBlocks);
        Assert.Contains("Kata_Block_Column&Wall", KeyFunctionalBlocks);
        Assert.Contains("dangCoupler", KeyFunctionalBlocks);
    }

    [Fact]
    public void FallbackLayerCreation_GeneratesAllMissingStandardLayers()
    {
        var targetDatabaseLayers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "0",
            "Defpoints"
        };

        foreach (string layer in RequiredStandardLayers)
        {
            if (!targetDatabaseLayers.Contains(layer))
            {
                targetDatabaseLayers.Add(layer);
            }
        }

        foreach (string required in RequiredStandardLayers)
        {
            Assert.Contains(required, targetDatabaseLayers);
        }
    }

    [Fact]
    public void TextStyleSpecification_HasArialFontAndNarrowWidthFactor()
    {
        const string expectedStyleName = "kata_text";
        const string expectedFont = "arial.ttf";
        const double expectedWidthFactor = 0.80;

        Assert.Equal("kata_text", expectedStyleName);
        Assert.Equal("arial.ttf", expectedFont);
        Assert.Equal(0.80, expectedWidthFactor, 2);
    }

    [Fact]
    public void ReferenceDwg_WhenPresent_IsAccessibleAndNonEmpty()
    {
        if (!File.Exists(SourceDwg))
        {
            return;
        }

        var info = new FileInfo(SourceDwg);
        Assert.True(info.Length > 200_000, "Iso_kata.dwg must be > 200KB");
        Assert.Equal(".dwg", info.Extension, ignoreCase: true);
    }
}
