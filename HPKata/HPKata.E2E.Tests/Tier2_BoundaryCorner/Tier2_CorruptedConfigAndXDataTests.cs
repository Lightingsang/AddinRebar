using System;
using System.Collections.Generic;
using HPKata.Core.Configuration;
using HPKata.Core.Serialization;
using Xunit;

namespace HPKata.E2E.Tests.Tier2_BoundaryCorner;

/// <summary>
/// Tier 2: Boundary tests for malformed configuration files and corrupted XData streams.
/// </summary>
public sealed class Tier2_CorruptedConfigAndXDataTests
{
    [Fact]
    public void ParseText_WithNonNumericValueInFloatLine_RetainsSafeDefault()
    {
        // Arrange: Line 16 (ConcreteCoverMm) contains invalid text "NOT_A_NUMBER"
        var lines = KataConfigParser.SerializeToLines(new KataConfig());
        lines[15] = ";NOT_A_NUMBER"; // line 16 is 0-indexed 15
        string malformed = string.Join(Environment.NewLine, lines);

        // Act
        var config = KataConfigParser.ParseText(malformed);

        // Assert: ConcreteCoverMm defaults safely to 30.0
        Assert.Equal(30.0, config.ConcreteCoverMm);
    }

    [Fact]
    public void ParseText_WithExtraOrLeadingSpacesInLines_TrimsCleanly()
    {
        // Arrange
        var lines = KataConfigParser.SerializeToLines(new KataConfig());
        lines[0] = "   ;   custom_layer_with_spaces   ";
        string text = string.Join(Environment.NewLine, lines);

        // Act
        var config = KataConfigParser.ParseText(text);

        // Assert
        Assert.Equal("custom_layer_with_spaces", config.LayerDim);
    }

    [Fact]
    public void ParseText_WithCorruptedLinesWithoutSemicolons_ParsesTokensGracefully()
    {
        // Arrange: lines without leading semicolons
        string raw = "layer_dim_plain\r\nnet_thay_plain\r\nnet_khung_plain";

        // Act
        var config = KataConfigParser.ParseText(raw);

        // Assert
        Assert.Equal("layer_dim_plain", config.LayerDim);
        Assert.Equal("net_thay_plain", config.LayerNetThay);
    }

    [Fact]
    public void XDataRecord_ParseLengths_WithCorruptedDelimiter_IgnoresInvalidTokens()
    {
        // Arrange: mix of semicolons, letters, and numbers
        var record = new KataXDataRecord();

        // Act
        record.ParseLengths("500;abc;600;;xyz;700");

        // Assert: only valid numbers (500, 600, 700) are extracted
        Assert.Equal(3, record.ChieuDaiCacDoanMm.Length);
        Assert.Equal(500.0, record.ChieuDaiCacDoanMm[0]);
        Assert.Equal(600.0, record.ChieuDaiCacDoanMm[1]);
        Assert.Equal(700.0, record.ChieuDaiCacDoanMm[2]);
    }

    [Fact]
    public void DeserializeFromDxf_WithMissingRegApp_ReturnsDefaultRecord()
    {
        // Arrange: Dxf stream missing the 1001 "KATA" header
        var values = new List<KataTypedValue>
        {
            new(1000, "01"),
            new(1070, (short)1)
        };

        // Act
        var record = KataXDataSerializer.DeserializeFromDxf(values);

        // Assert: returns default record without throwing
        Assert.NotNull(record);
        Assert.Equal("01", record.Dang);
    }

    [Fact]
    public void DeserializeFromDxf_WithEmptyList_ReturnsDefaultRecord()
    {
        // Act
        var record = KataXDataSerializer.DeserializeFromDxf(new List<KataTypedValue>());

        // Assert
        Assert.NotNull(record);
        Assert.Equal("01", record.Dang);
    }
}
