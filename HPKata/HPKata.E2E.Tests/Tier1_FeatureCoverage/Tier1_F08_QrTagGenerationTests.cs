using HPKata.Core.Tagging;
using Xunit;

namespace HPKata.E2E.Tests.Tier1_FeatureCoverage;

/// <summary>
/// Feature 08: QR Tag payload serialization, parsing, ECC Level Q, and PNG generation.
/// </summary>
public sealed class Tier1_F08_QrTagGenerationTests
{
    [Fact]
    public void FormatPayload_GeneratesExactPipeDelimitedContract()
    {
        // Arrange
        var payload = new QrTagPayload
        {
            Project = "LandmarkTower",
            Client = "Vingroup",
            Order = "PO-2026-001",
            Component = "Beam_B101",
            Mark = "1",
            ShapeCode = "02",
            DiameterMm = 25,
            CutLengthMm = 6500.0,
            Quantity = 4,
            WeightKg = 100.1,
            SteelGrade = "CB500-V",
            SegmentsMm = [500.0, 5500.0, 500.0]
        };

        // Act
        string formatted = QrTagGenerator.FormatPayload(payload);

        // Assert
        Assert.StartsWith("KATA|", formatted);
        Assert.Contains("|LandmarkTower|Vingroup|PO-2026-001|Beam_B101|1|02|25|6500|4|100.1|CB500-V|", formatted);
        Assert.EndsWith("500,5500,500", formatted);
    }

    [Fact]
    public void ParsePayload_RestoresAllFieldsLosslessly()
    {
        // Arrange
        string raw = "KATA|ProjA|ClientB|OrdC|Col1|3|22|16|1560|20|49.3|CB400-V|500,200,500,200,80,80";

        // Act
        var p = QrTagGenerator.ParsePayload(raw);

        // Assert
        Assert.Equal("ProjA", p.Project);
        Assert.Equal("ClientB", p.Client);
        Assert.Equal("OrdC", p.Order);
        Assert.Equal("Col1", p.Component);
        Assert.Equal("3", p.Mark);
        Assert.Equal("22", p.ShapeCode);
        Assert.Equal(16, p.DiameterMm);
        Assert.Equal(1560.0, p.CutLengthMm);
        Assert.Equal(20, p.Quantity);
        Assert.Equal(49.3, p.WeightKg);
        Assert.Equal("CB400-V", p.SteelGrade);
        Assert.Equal(6, p.SegmentsMm.Length);
        Assert.Equal(80.0, p.SegmentsMm[4]);
    }

    [Fact]
    public void GeneratePngBytes_ProducesValidPngBinaryHeader()
    {
        // Arrange
        string payload = "KATA|ProjA|ClientB|OrdC|D1|1|01|20|5000|2|24.7|CB400-V|5000,0,0,0,0,0";

        // Act
        byte[] pngBytes = QrTagGenerator.GeneratePngBytes(payload);

        // Assert: Valid PNG magic bytes 0x89, 'P', 'N', 'G' (137, 80, 78, 71)
        Assert.NotNull(pngBytes);
        Assert.True(pngBytes.Length > 64);
        Assert.Equal(0x89, pngBytes[0]);
        Assert.Equal(0x50, pngBytes[1]);
        Assert.Equal(0x4E, pngBytes[2]);
        Assert.Equal(0x47, pngBytes[3]);
    }

    [Fact]
    public void RoundTrip_FormatAndParse_PreservesAllData()
    {
        // Arrange
        var original = new QrTagPayload
        {
            Project = "MetroLine",
            Client = "MAUR",
            Order = "PO-09",
            Component = "PierP1",
            Mark = "10A",
            ShapeCode = "01",
            DiameterMm = 32,
            CutLengthMm = 11700.0,
            Quantity = 16,
            WeightKg = 1181.4,
            SteelGrade = "CB500-V",
            SegmentsMm = [11700.0]
        };

        // Act
        string text = QrTagGenerator.FormatPayload(original);
        var parsed = QrTagGenerator.ParsePayload(text);

        // Assert
        Assert.Equal(original.Project, parsed.Project);
        Assert.Equal(original.Component, parsed.Component);
        Assert.Equal(original.Mark, parsed.Mark);
        Assert.Equal(original.DiameterMm, parsed.DiameterMm);
        Assert.Equal(original.CutLengthMm, parsed.CutLengthMm);
        Assert.Equal(original.Quantity, parsed.Quantity);
        Assert.Equal(original.WeightKg, parsed.WeightKg, 1);
    }

    [Fact]
    public void ParsePayload_WithCorruptedHeader_ReturnsEmptyPayload()
    {
        // Arrange
        string invalid = "INVALID_HEADER|ProjA|ClientB|...";

        // Act
        var result = QrTagGenerator.ParsePayload(invalid);

        // Assert: returns empty payload without crashing
        Assert.Empty(result.Project);
        Assert.Equal(0, result.DiameterMm);
    }
}
