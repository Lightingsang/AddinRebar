using HPKata.Core.Models;
using HPKata.Core.Scheduling;
using HPKata.Core.Serialization;
using HPKata.Core.Tagging;
using HPKata.Revit.Abstractions;
using HPKata.Revit.Bridge;
using Xunit;

namespace HPKata.E2E.Tests.Tier2_BoundaryCorner;

/// <summary>
/// Tier 2: Boundary tests for Vietnamese diacritics, special notation marks, null partitions, and long strings.
/// </summary>
public sealed class Tier2_DiacriticsAndNamingTests
{
    [Fact]
    public void Schedule_WithVietnameseDiacritics_PreservesExactUnicodeCharacters()
    {
        // Arrange
        var items = new[]
        {
            new RebarItem
            {
                ComponentMark = "DẦM_MÓNG_Đ1",
                BarMark = "1a*",
                ShapeCode = "01",
                DiameterMm = 20,
                CountPerMember = 4,
                MemberCount = 1,
                ParametersMm = [6000.0],
                CuttingLengthMm = 6000.0,
                Notes = "Thép chịu lực lớp dưới"
            }
        };

        // Act
        var report = RebarScheduleAggregator.GenerateReport(items);

        // Assert
        Assert.Equal("DẦM_MÓNG_Đ1", report.Items[0].ComponentMark);
        Assert.Equal("1a*", report.Items[0].BarMark);
        Assert.Equal("Thép chịu lực lớp dưới", report.Items[0].Notes);
    }

    [Fact]
    public void QrTag_WithVietnameseCharacters_FormatsAndParsesWithoutLoss()
    {
        // Arrange
        var payload = new QrTagPayload
        {
            Project = "Dự án Khu đô thị Mới",
            Client = "Tập đoàn Xây dựng Việt Nam",
            Component = "ĐÀI_MÓNG_ĐM1",
            Mark = "10_đặc_biệt",
            DiameterMm = 25,
            Quantity = 10,
            SteelGrade = "CB500-V"
        };

        // Act
        string text = QrTagGenerator.FormatPayload(payload);
        var parsed = QrTagGenerator.ParsePayload(text);

        // Assert
        Assert.Equal(payload.Project, parsed.Project);
        Assert.Equal(payload.Client, parsed.Client);
        Assert.Equal(payload.Component, parsed.Component);
        Assert.Equal(payload.Mark, parsed.Mark);
    }

    [Fact]
    public void RevitParameterBridge_WithNullPartition_FallbacksToDefaultCleanly()
    {
        // Arrange: null partition and null mark
        var dto = new RevitRebarDto
        {
            ElementId = 999,
            Partition = null!,
            Mark = null!,
            DiameterMm = 16,
            Quantity = 2,
            SingleBarLengthMm = 3000.0
        };

        // Act
        var item = RevitParameterBridge.ToRebarItem(dto);

        // Assert: fallback partition "CK", fallback mark "1"
        Assert.Equal("CK", item.ComponentMark);
        Assert.Equal("1", item.BarMark);
    }

    [Fact]
    public void XDataRecord_WithSpecialCharacters_SerializesAndRestoresAccurately()
    {
        // Arrange
        var record = new KataXDataRecord
        {
            Dang = "02",
            STT = 99,
            TenCauKien = "Dầm D1-2/3 (Trục A-B)",
            KhoangCach = "@150/@200",
            ChuThich = "Gia cường gối tựa & cắt thép"
        };

        // Act
        var dxf = KataXDataSerializer.SerializeToDxf(record);
        var restored = KataXDataSerializer.DeserializeFromDxf(dxf);

        // Assert
        Assert.Equal(record.TenCauKien, restored.TenCauKien);
        Assert.Equal(record.KhoangCach, restored.KhoangCach);
        Assert.Equal(record.ChuThich, restored.ChuThich);
    }

    [Fact]
    public void LongStringMark_250Characters_PreservesIntegrityWithoutBufferOverflow()
    {
        // Arrange
        string longName = new string('A', 250);
        var item = new RebarItem
        {
            ComponentMark = longName,
            BarMark = "LONG",
            DiameterMm = 20,
            CountPerMember = 1,
            CuttingLengthMm = 1000.0
        };

        // Act
        var report = RebarScheduleAggregator.GenerateReport([item]);

        // Assert
        Assert.Equal(longName, report.Items[0].ComponentMark);
    }

    [Fact]
    public void RebarDto_WithEmptyStrings_MaintainsSafeDefaults()
    {
        // Arrange
        var item = new RebarItem
        {
            ComponentMark = string.Empty,
            BarMark = "   ",
            DiameterMm = 16,
            CuttingLengthMm = 2000.0
        };

        // Act
        var dto = RevitParameterBridge.ToRebarDto(item, 100);

        // Assert
        Assert.NotNull(dto.Partition);
        Assert.NotNull(dto.Mark);
    }
}
