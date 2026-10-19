using System;
using HPDcePro.Core.XData;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public class XDataTests
    {
        [Fact]
        public void RebarXDataCodec_RoundTrip_PreservesAll12Elements()
        {
            var codec = new RebarXDataCodec();
            var original = new RebarXdataDceModel
            {
                TenCauKien = "D1",
                SoHieu = "1a",
                MaThep = "/KC",
                DuongKinh = "22",
                SoLuong = "4",
                LoaiThep = "CBot",
                DoanA = 300.0,
                DoanB = 5800.0,
                DoanC = 300.0,
                DoanD = 0.0,
                ChieuDaiL = 6400.0,
                HinhDang = 2
            };

            XDataPacket packet = codec.Encode(original);
            Assert.Equal(12, packet.Values.Count);
            Assert.Equal(RebarXDataCodec.ApplicationName, packet.RegAppName);

            bool decoded = codec.TryDecode(packet, out RebarXdataDceModel? result);
            Assert.True(decoded);
            Assert.NotNull(result);

            Assert.Equal("D1", result.TenCauKien);
            Assert.Equal("1a", result.SoHieu);
            Assert.Equal("/KC", result.MaThep);
            Assert.Equal("22", result.DuongKinh);
            Assert.Equal("4", result.SoLuong);
            Assert.Equal("CBot", result.LoaiThep);
            Assert.Equal(300.0, result.DoanA);
            Assert.Equal(5800.0, result.DoanB);
            Assert.Equal(300.0, result.DoanC);
            Assert.Equal(0.0, result.DoanD);
            Assert.Equal(6400.0, result.ChieuDaiL);
            Assert.Equal(2, result.HinhDang);
        }

        [Fact]
        public void ScheduleXDataCodec_CanonicalString_RoundTripExact()
        {
            var codec = new ScheduleXDataCodec();
            var original = new DceRebarScheduleModel
            {
                SoLuong = 3,
                DuongKinh = 20,
                KC1 = 250.0,
                KC2 = 5400.0,
                KC3 = 250.0,
                KC4 = 0.0,
                SoHieu = "1b",
                ChieuDaiL = 5900.0
            };

            string canon = original.ToCanonicalString();
            Assert.Equal("SL=3;DK=20;KC1=250;KC2=5400;KC3=250;KC4=0;SH=1b;L=5900", canon);

            XDataPacket packet = codec.Encode(original);
            bool success = codec.TryDecode(packet, out DceRebarScheduleModel? result);

            Assert.True(success);
            Assert.NotNull(result);
            Assert.Equal(3, result.SoLuong);
            Assert.Equal(20, result.DuongKinh);
            Assert.Equal(250.0, result.KC1);
            Assert.Equal(5400.0, result.KC2);
            Assert.Equal("1b", result.SoHieu);
            Assert.Equal(5900.0, result.ChieuDaiL);
        }

        [Fact]
        public void LinksXDataCodec_RoundTrip_PreservesHandles()
        {
            var codec = new LinksXDataCodec();
            var original = new LinksXDataModel
            {
                HandleThep = "1A2F",
                HandleTag = "1A30",
                HandleDim = "1A31",
                HandleLeader = "1A32",
                LuaChonField = "%<\\AcObjProp Object(%<\\_ObjId ...)>%",
                TrangThaiKhoa = 1
            };

            XDataPacket packet = codec.Encode(original);
            bool decoded = codec.TryDecode(packet, out LinksXDataModel? result);

            Assert.True(decoded);
            Assert.NotNull(result);
            Assert.Equal("1A2F", result.HandleThep);
            Assert.Equal("1A30", result.HandleTag);
            Assert.Equal("1A31", result.HandleDim);
            Assert.Equal("1A32", result.HandleLeader);
            Assert.Equal(1, result.TrangThaiKhoa);
        }

        [Fact]
        public void XDataRegistry_ContainsAll13RegApps_AndRecoversMalformedPackets()
        {
            Assert.Equal(13, XDataRegistry.KnownRegApps.Count);
            Assert.True(XDataRegistry.IsDceRegApp("RebarXdataDCE"));
            Assert.True(XDataRegistry.IsDceRegApp("DceRebarXDataTKT"));
            Assert.True(XDataRegistry.IsDceRegApp("Links_XDataObject"));
            Assert.True(XDataRegistry.IsDceRegApp("XDATA_APP_SODOCAT"));
            Assert.True(XDataRegistry.IsDceRegApp("XDATA_APP_KHOTHEPDU"));
            Assert.False(XDataRegistry.IsDceRegApp("SomeUnknownApp"));

            // Packet with unclosed brace
            var packet = new XDataPacket("RebarXdataDCE");
            packet.AddControlString("{");
            packet.AddString("data");

            var recovered = XDataRegistry.RecoverMalformedPacket(packet);
            Assert.Equal(3, recovered.Values.Count);
            Assert.Equal("}", recovered.Values[2].Value.ToString());
        }
    }
}
