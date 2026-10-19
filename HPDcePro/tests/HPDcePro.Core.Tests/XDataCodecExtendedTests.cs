using System;
using HPDcePro.Core.XData;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public class XDataCodecExtendedTests
    {
        [Fact]
        public void XDataRegistry_All13RegApps_AreRecognizedCaseInsensitively()
        {
            var expected13 = new[]
            {
                XDataRegistry.RegAppRebarXdataDCE,
                XDataRegistry.RegAppDceRebarXDataTKT,
                XDataRegistry.RegAppLinksXDataObject,
                XDataRegistry.RegAppDceThepKep,
                XDataRegistry.RegAppSoDoCat,
                XDataRegistry.RegAppKhoThepDu,
                XDataRegistry.RegAppMatBangMong,
                XDataRegistry.RegAppMongCoc,
                XDataRegistry.RegAppThangMoi,
                XDataRegistry.RegAppSysApp,
                XDataRegistry.RegAppNhanCoc,
                XDataRegistry.RegAppNhanDamCong,
                XDataRegistry.RegAppNhanMb
            };

            Assert.Equal(13, expected13.Length);
            Assert.Equal(13, XDataRegistry.KnownRegApps.Count);

            foreach (string app in expected13)
            {
                Assert.True(XDataRegistry.IsDceRegApp(app));
                Assert.True(XDataRegistry.IsDceRegApp(app.ToLowerInvariant()));
                Assert.True(XDataRegistry.IsDceRegApp(app.ToUpperInvariant()));
            }

            Assert.False(XDataRegistry.IsDceRegApp(null));
            Assert.False(XDataRegistry.IsDceRegApp(string.Empty));
            Assert.False(XDataRegistry.IsDceRegApp("   "));
            Assert.False(XDataRegistry.IsDceRegApp("NON_EXISTENT_APP"));
        }

        [Fact]
        public void XDataPacket_TruncatedOrEmptyBuffer_ReturnsDefaultValuesGracefully()
        {
            var packet = new XDataPacket(XDataRegistry.RegAppRebarXdataDCE);
            Assert.Empty(packet.Values);

            Assert.Equal("defaultStr", packet.GetString(0, "defaultStr"));
            Assert.Equal(99.5, packet.GetDouble(0, 99.5));
            Assert.Equal((short)7, packet.GetInt16(0, 7));
            Assert.Equal("defHandle", packet.GetHandle(0, "defHandle"));

            // Negative indices
            Assert.Equal("fallback", packet.GetString(-1, "fallback"));
            Assert.Equal(-1.0, packet.GetDouble(-5, -1.0));
            Assert.Equal((short)-2, packet.GetInt16(-3, -2));
            Assert.Equal("0", packet.GetHandle(-1, "0"));
        }

        [Fact]
        public void XDataPacket_CorruptedDataTypes_FallsBackGracefully()
        {
            var packet = new XDataPacket(XDataRegistry.RegAppRebarXdataDCE);
            packet.AddString("NOT_A_DOUBLE");
            packet.AddString("NOT_AN_INT");

            double d = packet.GetDouble(0, 42.0);
            Assert.Equal(42.0, d);

            short s = packet.GetInt16(1, 15);
            Assert.Equal(15, s);

            // Valid numeric strings
            packet.AddString("123.45");
            packet.AddString("789");
            Assert.Equal(123.45, packet.GetDouble(2, 0.0));
            Assert.Equal((short)789, packet.GetInt16(3, 0));
        }

        [Fact]
        public void XDataRegistry_RecoverMalformedPacket_RepairsUnclosedBraces()
        {
            var packet = new XDataPacket(XDataRegistry.RegAppLinksXDataObject);
            packet.AddControlString("{");
            packet.AddString("Item1");
            packet.AddControlString("{");
            packet.AddString("Item2");

            var recovered = XDataRegistry.RecoverMalformedPacket(packet);

            Assert.Equal(6, recovered.Values.Count);
            Assert.Equal("}", recovered.Values[4].Value.ToString());
            Assert.Equal("}", recovered.Values[5].Value.ToString());

            Assert.Throws<ArgumentNullException>(() => XDataRegistry.RecoverMalformedPacket(null!));
        }

        [Fact]
        public void RebarXDataCodec_TruncatedPacket_DecodesWithSensibleDefaults()
        {
            var codec = new RebarXDataCodec();
            var packet = new XDataPacket(RebarXDataCodec.ApplicationName);
            packet.AddString("D_TRUNC");
            packet.AddString("1_T");

            bool success = codec.TryDecode(packet, out var model);
            Assert.True(success);
            Assert.NotNull(model);

            Assert.Equal("D_TRUNC", model!.TenCauKien);
            Assert.Equal("1_T", model.SoHieu);
            Assert.Equal(string.Empty, model.MaThep);
            Assert.Equal("20", model.DuongKinh);
            Assert.Equal("1", model.SoLuong);
            Assert.Equal("CTop", model.LoaiThep);
            Assert.Equal(0.0, model.DoanA);
            Assert.Equal(1, model.HinhDang);
        }

        [Fact]
        public void RebarXDataCodec_NegativeDiameterAndZeroCount_PreservesValuesWithoutCrashing()
        {
            var codec = new RebarXDataCodec();
            var original = new RebarXdataDceModel
            {
                TenCauKien = "D_EDGE",
                SoHieu = "99z",
                DuongKinh = "-25",
                SoLuong = "0",
                DoanA = -300.0,
                ChieuDaiL = -1200.0
            };

            XDataPacket packet = codec.Encode(original);
            bool decoded = codec.TryDecode(packet, out var result);

            Assert.True(decoded);
            Assert.NotNull(result);
            Assert.Equal("-25", result!.DuongKinh);
            Assert.Equal("0", result.SoLuong);
            Assert.Equal(-300.0, result.DoanA);
            Assert.Equal(-1200.0, result.ChieuDaiL);
        }

        [Fact]
        public void RebarXDataCodec_Decode_InvalidRegApp_ThrowsInvalidOperationException()
        {
            var codec = new RebarXDataCodec();
            var foreignPacket = new XDataPacket("SomeForeignApp");
            foreignPacket.AddString("Value1");

            Assert.False(codec.TryDecode(foreignPacket, out var result));
            Assert.Null(result);

            Assert.Throws<InvalidOperationException>(() => codec.Decode(foreignPacket));
        }
    }
}
