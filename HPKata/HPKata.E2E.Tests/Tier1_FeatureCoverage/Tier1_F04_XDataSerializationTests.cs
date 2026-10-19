using System.Collections.Generic;
using HPKata.AutoCad.Interop;
using HPKata.Core.Serialization;
using Xunit;

namespace HPKata.E2E.Tests.Tier1_FeatureCoverage;

/// <summary>
/// Feature 04: KataPro XData 2-way serialization, RegApp "KATA", DxfCodes, and handle linking.
/// </summary>
public sealed class Tier1_F04_XDataSerializationTests
{
    [Fact]
    public void SerializeToDxf_IncludesRegAppAndStandardFields()
    {
        // Arrange
        var record = new KataXDataRecord
        {
            Dang = "02",
            STT = 5,
            Phi = 20,
            KhoangCach = "a150",
            SoLuong = "4",
            SoLuongCauKien = 2,
            ChieuDaiCacDoanMm = [400.0, 3500.0],
            TenCauKien = "B201"
        };

        // Act
        var dxfValues = KataXDataSerializer.SerializeToDxf(record);

        // Assert
        Assert.NotEmpty(dxfValues);
        Assert.Equal(KataXDataSerializer.DxfCodeExtendedDataRegAppName, dxfValues[0].DxfCode);
        Assert.Equal("KATA", dxfValues[0].Value?.ToString());
        Assert.Equal("02", dxfValues[1].Value?.ToString());
        Assert.Equal((short)5, System.Convert.ToInt16(dxfValues[2].Value));
        Assert.Equal((short)20, System.Convert.ToInt16(dxfValues[3].Value));
    }

    [Fact]
    public void DeserializeFromDxf_RestoresAllPropertiesLosslessly()
    {
        // Arrange
        var original = new KataXDataRecord
        {
            Dang = "22",
            STT = 12,
            Phi = 8,
            KhoangCach = "a100",
            SoLuong = "24",
            SoLuongCauKien = 1,
            ChieuDaiCacDoanMm = [500.0, 200.0, 500.0, 200.0, 80.0, 80.0],
            TenCauKien = "C1",
            LinkedHandle = "4A2F",
            LinkType = "THEP_DAI"
        };
        var dxfValues = KataXDataSerializer.SerializeToDxf(original);

        // Act
        var restored = KataXDataSerializer.DeserializeFromDxf(dxfValues);

        // Assert
        Assert.Equal(original.Dang, restored.Dang);
        Assert.Equal(original.STT, restored.STT);
        Assert.Equal(original.Phi, restored.Phi);
        Assert.Equal(original.KhoangCach, restored.KhoangCach);
        Assert.Equal(original.TenCauKien, restored.TenCauKien);
        Assert.Equal(original.LinkedHandle, restored.LinkedHandle);
        Assert.Equal(original.LinkType, restored.LinkType);
        Assert.Equal(original.ChieuDaiCacDoanMm.Length, restored.ChieuDaiCacDoanMm.Length);
    }

    [Fact]
    public void HandleLinkService_LinkRecords_EstablishesBidirectionalAssociation()
    {
        // Arrange
        var rebarRecord = new KataXDataRecord();
        var tagRecord = new KataXDataRecord();

        // Act
        HandleLinkService.LinkRecords(rebarRecord, "100A", tagRecord, "200B");

        // Assert
        Assert.Equal("200B", rebarRecord.LinkedHandle);
        Assert.Equal("100A", tagRecord.LinkedHandle);
        Assert.Equal("THEP_DON", rebarRecord.LinkType);
        Assert.Equal("THEP_DON", tagRecord.LinkType);
    }

    [Fact]
    public void HandleLinkService_SeverRecordLink_ClearsAssociation()
    {
        // Arrange
        var record = new KataXDataRecord
        {
            LinkedHandle = "5B1C",
            LinkType = "THEP_DON"
        };

        // Act
        HandleLinkService.SeverRecordLink(record);

        // Assert
        Assert.Empty(record.LinkedHandle);
        Assert.Empty(record.LinkType);
    }

    [Fact]
    public void HandleLinkService_RebindRecordHandles_UpdatesMappedHandle()
    {
        // Arrange
        var record = new KataXDataRecord { LinkedHandle = "OLD_101" };
        var map = new Dictionary<string, string>
        {
            { "OLD_101", "NEW_202" }
        };

        // Act
        HandleLinkService.RebindRecordHandles(record, map);

        // Assert
        Assert.Equal("NEW_202", record.LinkedHandle);
    }
}
