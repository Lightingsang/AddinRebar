using System;
using System.IO;
using HPKata.Core.Configuration;
using Xunit;

namespace HPKata.E2E.Tests.Tier1_FeatureCoverage;

/// <summary>
/// Feature 01: 86-parameter configuration engine parsing, serialization, and fallback tests.
/// </summary>
public sealed class Tier1_F01_ConfigParametersTests
{
    [Fact]
    public void ParseText_WithComplete86Lines_PopulatesAllKeyProperties()
    {
        // Arrange
        var initial = new KataConfig
        {
            LayerDim = "test_dim_layer",
            ConcreteCoverMm = 35.0,
            BaseBarDiameter = 22,
            LapLengthMultiplier = 45.0,
            TensionAnchorMult = 42.0,
            DefaultHookAngle = "135",
            ApplyBendDeduction = 1,
            StandardBarLength = 12000
        };
        string serialized = KataConfigParser.SerializeToText(initial);

        // Act
        KataConfig parsed = KataConfigParser.ParseText(serialized);

        // Assert
        Assert.Equal("test_dim_layer", parsed.LayerDim);
        Assert.Equal(35.0, parsed.ConcreteCoverMm);
        Assert.Equal(22, parsed.BaseBarDiameter);
        Assert.Equal(45.0, parsed.LapLengthMultiplier);
        Assert.Equal(42.0, parsed.TensionAnchorMult);
        Assert.Equal("135", parsed.DefaultHookAngle);
        Assert.Equal(1, parsed.ApplyBendDeduction);
        Assert.Equal(12000, parsed.StandardBarLength);
    }

    [Fact]
    public void SerializeToLines_ProducesExact86Lines()
    {
        // Arrange
        var config = new KataConfig();

        // Act
        var lines = KataConfigParser.SerializeToLines(config);

        // Assert
        Assert.Equal(86, lines.Count);
        foreach (string line in lines)
        {
            Assert.StartsWith(";", line);
        }
    }

    [Fact]
    public void ParseText_WithEmptyOrNullInput_ReturnsDefaultConfiguration()
    {
        // Act
        KataConfig fromNull = KataConfigParser.ParseText(null!);
        KataConfig fromWhitespace = KataConfigParser.ParseText("   \r\n   ");

        // Assert
        Assert.NotNull(fromNull);
        Assert.NotNull(fromWhitespace);
        Assert.Equal(30.0, fromNull.ConcreteCoverMm);
        Assert.Equal(16, fromNull.BaseBarDiameter);
        Assert.Equal(11700, fromWhitespace.StandardBarLength);
    }

    [Fact]
    public void ParseText_WithPartialLines_AppliesDefaultsToMissingParameters()
    {
        // Arrange
        string partial = ";layer_dim_custom\r\n;net_thay_custom\r\n;net_khung_custom";

        // Act
        KataConfig config = KataConfigParser.ParseText(partial);

        // Assert
        Assert.Equal("layer_dim_custom", config.LayerDim);
        Assert.Equal("net_thay_custom", config.LayerNetThay);
        Assert.Equal("net_khung_custom", config.LayerNetKhung);
        Assert.Equal(30.0, config.ConcreteCoverMm); // default preserved
        Assert.Equal(11700, config.StandardBarLength); // default preserved
    }

    [Fact]
    public void RoundTripSerialization_MaintainsValueFidelity()
    {
        // Arrange
        var original = new KataConfig
        {
            LayerThepChu = "rebar_main_steel",
            SpacingPrefix = "s",
            ConcreteCoverMm = 25.0,
            ColSpliceRatio = 0.25,
            AutoRenumberMarks = 0
        };

        // Act
        string text = KataConfigParser.SerializeToText(original);
        KataConfig restored = KataConfigParser.ParseText(text);

        // Assert
        Assert.Equal(original.LayerThepChu, restored.LayerThepChu);
        Assert.Equal(original.SpacingPrefix, restored.SpacingPrefix);
        Assert.Equal(original.ConcreteCoverMm, restored.ConcreteCoverMm);
        Assert.Equal(original.ColSpliceRatio, restored.ColSpliceRatio, 4);
        Assert.Equal(original.AutoRenumberMarks, restored.AutoRenumberMarks);
    }
}
