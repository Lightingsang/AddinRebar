using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using HPKata.AutoCad.Interop;
using HPKata.Core.Serialization;
using Xunit;

namespace HPKata.AutoCad.Tests
{
    public class XDataServiceTests
    {
        [Fact]
        public void XData_ToTypedValues_HasRegApp1001AtFirstIndex()
        {
            var record = new KataXDataRecord
            {
                Dang = "01",
                Phi = 16,
                STT = 5
            };

            TypedValue[] array = XDataService.ToTypedValues(record);

            Assert.True(array.Length >= 13);
            Assert.Equal((int)DxfCode.ExtendedDataRegAppName, array[0].TypeCode);
            Assert.Equal("KATA", array[0].Value?.ToString());
        }

        [Fact]
        public void XData_Roundtrip_TypedValues_PreservesAll12StandardFields()
        {
            var original = new KataXDataRecord
            {
                Dang = "22",
                STT = 7,
                Phi = 20,
                KhoangCach = "@150",
                SoLuong = "8",
                SoLuongCauKien = 2,
                GocUon = "135",
                TenCauKien = "D1-TANG2",
                SttThepChinh = "T",
                ChuThich = "Coupler 1 dau",
                Z = "+3.600"
            };
            original.ChieuDaiCacDoanMm = new[] { 400.0, 1200.0, 400.0 };

            TypedValue[] array = XDataService.ToTypedValues(original);
            KataXDataRecord deserialized = XDataService.FromTypedValues(array);

            Assert.Equal("22", deserialized.Dang);
            Assert.Equal(7, deserialized.STT);
            Assert.Equal(20, deserialized.Phi);
            Assert.Equal("@150", deserialized.KhoangCach);
            Assert.Equal("8", deserialized.SoLuong);
            Assert.Equal(2, deserialized.SoLuongCauKien);
            Assert.Equal("135", deserialized.GocUon);
            Assert.Equal("D1-TANG2", deserialized.TenCauKien);
            Assert.Equal("T", deserialized.SttThepChinh);
            Assert.Equal("Coupler 1 dau", deserialized.ChuThich);
            Assert.Equal("+3.600", deserialized.Z);
            Assert.Equal(3, deserialized.ChieuDaiCacDoanMm.Length);
            Assert.Equal(400.0, deserialized.ChieuDaiCacDoanMm[0]);
            Assert.Equal(1200.0, deserialized.ChieuDaiCacDoanMm[1]);
        }

        [Fact]
        public void XData_WithLinkedHandle_Maintains1005Field()
        {
            var original = new KataXDataRecord
            {
                Dang = "02",
                Phi = 12,
                LinkedHandle = "3F2A",
                LinkType = "THEP_DON"
            };

            TypedValue[] array = XDataService.ToTypedValues(original);
            KataXDataRecord deserialized = XDataService.FromTypedValues(array);

            Assert.Equal("3F2A", deserialized.LinkedHandle);
            Assert.Equal("THEP_DON", deserialized.LinkType);
        }

        [Fact]
        public void XData_WithLinkedDimHandle_MaintainsBothHandles()
        {
            var original = new KataXDataRecord
            {
                Dang = "22",
                Phi = 10,
                LinkedHandle = "3F2A",
                LinkType = "THEP_DAI",
                LinkedDimHandle = "3F2C"
            };

            TypedValue[] array = XDataService.ToTypedValues(original);
            KataXDataRecord deserialized = XDataService.FromTypedValues(array);

            Assert.Equal("3F2A", deserialized.LinkedHandle);
            Assert.Equal("3F2C", deserialized.LinkedDimHandle);
            Assert.Equal("THEP_DAI", deserialized.LinkType);
        }

        [Fact]
        public void XData_NullResultBuffer_ReturnsEmptySafeInstance()
        {
            KataXDataRecord deserialized = XDataService.FromResultBuffer(null);

            Assert.NotNull(deserialized);
            Assert.Equal("01", deserialized.Dang);
            Assert.Equal(16, deserialized.Phi);
        }
    }
}
