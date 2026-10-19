using System;
using System.Collections.Generic;
using HPKata.Core.Serialization;
using Xunit;

namespace HPKata.Core.Tests.Serialization
{
    public class KataXDataSerializerTests
    {
        [Fact]
        public void SerializeToDxf_ContainsRegAppKataAndStandardDxfCodes()
        {
            var record = new KataXDataRecord
            {
                Dang = "22",
                STT = 1,
                Phi = 8,
                KhoangCach = "a150",
                SoLuong = "24",
                SoLuongCauKien = 2,
                ChieuDaiCacDoanMm = new[] { 200.0, 400.0, 75.0, 0.0, 0.0, 0.0 },
                GocUon = "135/135",
                TenCauKien = "D1",
                SttThepChinh = "1",
                ChuThich = "Dai dam",
                Z = "0",
                LinkedHandle = "1A2F",
                LinkType = "THEP_DON",
                LinkedDimHandle = "2B3C"
            };

            var values = KataXDataSerializer.SerializeToDxf(record);

            Assert.NotEmpty(values);
            Assert.Equal(KataXDataSerializer.DxfCodeExtendedDataRegAppName, values[0].DxfCode);
            Assert.Equal(KataXDataRecord.RegAppName, values[0].Value?.ToString());

            // Check codes 1070 for STT and Phi
            Assert.Equal(KataXDataSerializer.DxfCodeExtendedDataInteger16, values[2].DxfCode);
            Assert.Equal((short)1, values[2].Value);
            Assert.Equal(KataXDataSerializer.DxfCodeExtendedDataInteger16, values[3].DxfCode);
            Assert.Equal((short)8, values[3].Value);

            // Check handle codes 1005
            Assert.Contains(values, v => v.DxfCode == KataXDataSerializer.DxfCodeExtendedDataHandle &&
                                         string.Equals(v.Value?.ToString(), "1A2F", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(values, v => v.DxfCode == KataXDataSerializer.DxfCodeExtendedDataHandle &&
                                         string.Equals(v.Value?.ToString(), "2B3C", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void RoundTripDxfSerialization_ReconstructsRecordLosslessly()
        {
            var original = new KataXDataRecord
            {
                Dang = "02",
                STT = 5,
                Phi = 20,
                KhoangCach = "",
                SoLuong = "4",
                SoLuongCauKien = 1,
                ChieuDaiCacDoanMm = new[] { 500.0, 1500.0, 0.0, 0.0, 0.0, 0.0 },
                GocUon = "90/90",
                TenCauKien = "COT_C1",
                SttThepChinh = "2",
                ChuThich = "Thep chu",
                Z = "0",
                LinkedHandle = "ABCD",
                LinkType = "THEP_DON",
                LinkedDimHandle = "EF01"
            };

            var dxfList = KataXDataSerializer.SerializeToDxf(original);
            var restored = KataXDataSerializer.DeserializeFromDxf(dxfList);

            Assert.Equal(original.Dang, restored.Dang);
            Assert.Equal(original.STT, restored.STT);
            Assert.Equal(original.Phi, restored.Phi);
            Assert.Equal(original.SoLuong, restored.SoLuong);
            Assert.Equal(original.SoLuongCauKien, restored.SoLuongCauKien);
            Assert.Equal(original.ChieuDaiCacDoanMm[0], restored.ChieuDaiCacDoanMm[0]);
            Assert.Equal(original.ChieuDaiCacDoanMm[1], restored.ChieuDaiCacDoanMm[1]);
            Assert.Equal(original.GocUon, restored.GocUon);
            Assert.Equal(original.TenCauKien, restored.TenCauKien);
            Assert.Equal(original.LinkedHandle, restored.LinkedHandle);
            Assert.Equal(original.LinkType, restored.LinkType);
            Assert.Equal(original.LinkedDimHandle, restored.LinkedDimHandle);
        }

        [Fact]
        public void DeserializeFromDxf_NullOrEmptyList_ReturnsDefaultRecord()
        {
            var r1 = KataXDataSerializer.DeserializeFromDxf(null!);
            var r2 = KataXDataSerializer.DeserializeFromDxf(new List<KataTypedValue>());

            Assert.NotNull(r1);
            Assert.NotNull(r2);
            Assert.Equal("01", r1.Dang);
            Assert.Equal((short)16, r1.Phi);
        }

        [Fact]
        public void JsonSerialization_RoundTripAndIdempotence()
        {
            var original = new KataXDataRecord
            {
                Dang = "06",
                STT = 3,
                Phi = 18,
                KhoangCach = "a200",
                SoLuong = "6",
                TenCauKien = "DAM_D2",
                LinkedHandle = "55AA"
            };

            string json = KataXDataSerializer.SerializeToJson(original);
            Assert.False(string.IsNullOrWhiteSpace(json));
            Assert.Contains("\"Dang\": \"06\"", json);
            Assert.Contains("\"TenCauKien\": \"DAM_D2\"", json);

            var deserialized = KataXDataSerializer.DeserializeFromJson(json);
            Assert.Equal(original.Dang, deserialized.Dang);
            Assert.Equal(original.STT, deserialized.STT);
            Assert.Equal(original.Phi, deserialized.Phi);
            Assert.Equal(original.TenCauKien, deserialized.TenCauKien);
            Assert.Equal(original.LinkedHandle, deserialized.LinkedHandle);
        }

        [Fact]
        public void DeserializeFromJson_CorruptedJson_FallsBackToDefaultRecord()
        {
            string invalidJson = "{ corrupted json content without closing brace ...";
            var result = KataXDataSerializer.DeserializeFromJson(invalidJson);

            Assert.NotNull(result);
            Assert.Equal("01", result.Dang);
            Assert.Equal((short)16, result.Phi);
        }

        [Fact]
        public void DeserializeFromJson_NullOrWhitespaceString_ReturnsDefaultRecord()
        {
            var r1 = KataXDataSerializer.DeserializeFromJson(null!);
            var r2 = KataXDataSerializer.DeserializeFromJson("   ");

            Assert.NotNull(r1);
            Assert.NotNull(r2);
            Assert.Equal("01", r1.Dang);
        }

        [Fact]
        public void SerializeToJson_NullRecord_ReturnsEmptyObjectJson()
        {
            string json = KataXDataSerializer.SerializeToJson(null!);
            Assert.Equal("{}", json);
        }
    }
}
