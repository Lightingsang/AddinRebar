using System;
using HPDcePro.Core.XData;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public class XDataAllCodecsTests
    {
        [Fact]
        public void RebarXDataCodec_RoundtripsAccurately()
        {
            var codec = new RebarXDataCodec();
            var model = new RebarXdataDceModel
            {
                TenCauKien = "D1-L1",
                SoHieu = "02",
                MaThep = "T1",
                DuongKinh = "22",
                SoLuong = "4",
                LoaiThep = "CB400",
                DoanA = 200,
                DoanB = 5000,
                DoanC = 200,
                ChieuDaiL = 5400,
                HinhDang = 2
            };

            var packet = codec.Encode(model);
            Assert.Equal("RebarXdataDCE", packet.RegAppName);

            var decoded = codec.Decode(packet);
            Assert.Equal(model.TenCauKien, decoded.TenCauKien);
            Assert.Equal(model.SoHieu, decoded.SoHieu);
            Assert.Equal(model.DuongKinh, decoded.DuongKinh);
            Assert.Equal(model.ChieuDaiL, decoded.ChieuDaiL);
        }

        [Fact]
        public void ScheduleXDataCodec_RoundtripsAccurately()
        {
            var codec = new ScheduleXDataCodec();
            var model = new DceRebarScheduleModel
            {
                SoLuong = 6,
                DuongKinh = 18,
                KC1 = 150,
                KC2 = 200,
                KC3 = 150,
                KC4 = 0,
                SoHieu = "03",
                ChieuDaiL = 4500
            };

            var packet = codec.Encode(model);
            Assert.Equal("DceRebarXDataTKT", packet.RegAppName);

            var decoded = codec.Decode(packet);
            Assert.Equal(6, decoded.SoLuong);
            Assert.Equal(18, decoded.DuongKinh);
            Assert.Equal(150, decoded.KC1);
            Assert.Equal("03", decoded.SoHieu);
        }

        [Fact]
        public void LinksXDataCodec_RoundtripsAccurately()
        {
            var codec = new LinksXDataCodec();
            var model = new LinksXDataModel
            {
                HandleThep = "2A1B",
                HandleTag = "3C2D",
                HandleDim = "4E5F",
                HandleLeader = "5A6B",
                LuaChonField = "CUSTOM_OPT",
                TrangThaiKhoa = 1
            };

            var packet = codec.Encode(model);
            Assert.Equal("Links_XDataObject", packet.RegAppName);

            var decoded = codec.Decode(packet);
            Assert.Equal("2A1B", decoded.HandleThep);
            Assert.Equal("3C2D", decoded.HandleTag);
            Assert.Equal(1, decoded.TrangThaiKhoa);
        }

        [Fact]
        public void ThepKepXDataCodec_RoundtripsAccurately()
        {
            var codec = new ThepKepXDataCodec();
            var model = new ThepKepModel
            {
                BundleCount = 3,
                SpacingMm = 35.0,
                ClearanceMm = 30.0,
                PrimaryBarHandle = "7F8E"
            };

            var packet = codec.Encode(model);
            Assert.Equal(XDataRegistry.RegAppDceThepKep, packet.RegAppName);

            var decoded = codec.Decode(packet);
            Assert.Equal(3, decoded.BundleCount);
            Assert.Equal(35.0, decoded.SpacingMm);
            Assert.Equal("7F8E", decoded.PrimaryBarHandle);
        }

        [Fact]
        public void SoDoCatXDataCodec_RoundtripsAccurately()
        {
            var codec = new SoDoCatXDataCodec();
            var model = new SoDoCatModel
            {
                StockBarId = "STOCK-12",
                PatternIndex = 3,
                CutEquation = "3500+3500+4500=11500",
                WasteMm = 200.0,
                WasteRatio = 0.0171
            };

            var packet = codec.Encode(model);
            Assert.Equal(XDataRegistry.RegAppSoDoCat, packet.RegAppName);

            var decoded = codec.Decode(packet);
            Assert.Equal("STOCK-12", decoded.StockBarId);
            Assert.Equal(3, decoded.PatternIndex);
            Assert.Equal("3500+3500+4500=11500", decoded.CutEquation);
            Assert.Equal(200.0, decoded.WasteMm);
        }

        [Fact]
        public void KhoThepDuXDataCodec_RoundtripsAccurately()
        {
            var codec = new KhoThepDuXDataCodec();
            var model = new KhoThepDuModel
            {
                OffcutId = "OFF-001",
                DiameterMm = 20,
                LengthMm = 3200.0,
                StorageRack = "RACK-B2"
            };

            var packet = codec.Encode(model);
            Assert.Equal(XDataRegistry.RegAppKhoThepDu, packet.RegAppName);

            var decoded = codec.Decode(packet);
            Assert.Equal("OFF-001", decoded.OffcutId);
            Assert.Equal(20, decoded.DiameterMm);
            Assert.Equal(3200.0, decoded.LengthMm);
            Assert.Equal("RACK-B2", decoded.StorageRack);
        }

        [Fact]
        public void MatBangMongXDataCodec_RoundtripsAccurately()
        {
            var codec = new MatBangMongXDataCodec();
            var model = new MatBangMongModel
            {
                GridMark = "DC2",
                TopElevationM = -1.8,
                BottomElevationM = -2.7,
                PileType = "D400",
                PileCount = 4
            };

            var packet = codec.Encode(model);
            Assert.Equal(XDataRegistry.RegAppMatBangMong, packet.RegAppName);

            var decoded = codec.Decode(packet);
            Assert.Equal("DC2", decoded.GridMark);
            Assert.Equal(-1.8, decoded.TopElevationM);
            Assert.Equal(4, decoded.PileCount);
        }

        [Fact]
        public void MongCocXDataCodec_RoundtripsAccurately()
        {
            var codec = new MongCocXDataCodec();
            var model = new MongCocModel
            {
                CapMark = "DC-01",
                DimX = 2200.0,
                DimY = 2200.0,
                HeightH = 900.0,
                RebarBottomX = "Phi16 a150",
                RebarBottomY = "Phi16 a150",
                ConcreteVolumeM3 = 4.356
            };

            var packet = codec.Encode(model);
            Assert.Equal(XDataRegistry.RegAppMongCoc, packet.RegAppName);

            var decoded = codec.Decode(packet);
            Assert.Equal("DC-01", decoded.CapMark);
            Assert.Equal(2200.0, decoded.DimX);
            Assert.Equal(900.0, decoded.HeightH);
            Assert.Equal(4.356, decoded.ConcreteVolumeM3);
        }

        [Fact]
        public void ThangMoiXDataCodec_RoundtripsAccurately()
        {
            var codec = new ThangMoiXDataCodec();
            var model = new ThangMoiModel
            {
                StairMark = "ST-01",
                FloorHeightMm = 3600.0,
                RiserCount = 21,
                TreadWidthMm = 280.0,
                WaistThicknessMm = 120.0,
                MainRebar = "Phi12 a150"
            };

            var packet = codec.Encode(model);
            Assert.Equal(XDataRegistry.RegAppThangMoi, packet.RegAppName);

            var decoded = codec.Decode(packet);
            Assert.Equal("ST-01", decoded.StairMark);
            Assert.Equal(21, decoded.RiserCount);
            Assert.Equal(280.0, decoded.TreadWidthMm);
            Assert.Equal("Phi12 a150", decoded.MainRebar);
        }

        [Fact]
        public void SysAppXDataCodec_RoundtripsAccurately()
        {
            var codec = new SysAppXDataCodec();
            var model = new SysAppModel
            {
                AppVersion = "HPDcePro_2026",
                ProjectCode = "PRJ-01",
                Timestamp = "2026-10-18T10:00:00Z"
            };

            var packet = codec.Encode(model);
            Assert.Equal(XDataRegistry.RegAppSysApp, packet.RegAppName);

            var decoded = codec.Decode(packet);
            Assert.Equal("HPDcePro_2026", decoded.AppVersion);
            Assert.Equal("PRJ-01", decoded.ProjectCode);
            Assert.Equal("2026-10-18T10:00:00Z", decoded.Timestamp);
        }

        [Fact]
        public void NhanCodecs_RoundtripAccurately()
        {
            var cocCodec = new NhanCocXDataCodec();
            var cocModel = new NhanCocModel { PileMark = "C5", DiameterMm = 400.0, CapacityTons = 65.0, CutoffElevationM = -2.4, CoordX = 1000.0, CoordY = 2000.0 };
            var cocPacket = cocCodec.Encode(cocModel);
            Assert.Equal(XDataRegistry.RegAppNhanCoc, cocPacket.RegAppName);
            var cocDecoded = cocCodec.Decode(cocPacket);
            Assert.Equal("C5", cocDecoded.PileMark);
            Assert.Equal(400.0, cocDecoded.DiameterMm);

            var damCodec = new NhanDamCongXDataCodec();
            var damModel = new NhanDamCongModel { BeamMark = "DC1", RadiusMm = 4500.0, ArcAngleDeg = 45.0, SectionWidthMm = 220.0, SectionHeightMm = 400.0 };
            var damPacket = damCodec.Encode(damModel);
            Assert.Equal(XDataRegistry.RegAppNhanDamCong, damPacket.RegAppName);
            var damDecoded = damCodec.Decode(damPacket);
            Assert.Equal(4500.0, damDecoded.RadiusMm);
            Assert.Equal(220.0, damDecoded.SectionWidthMm);

            var mbCodec = new NhanMbXDataCodec();
            var mbModel = new NhanMbModel { PlanId = "MBKC_T2", ElevationM = 3.6, MemberType = "BEAM", StoryName = "TẦNG 2" };
            var mbPacket = mbCodec.Encode(mbModel);
            Assert.Equal(XDataRegistry.RegAppNhanMb, mbPacket.RegAppName);
            var mbDecoded = mbCodec.Decode(mbPacket);
            Assert.Equal("MBKC_T2", mbDecoded.PlanId);
            Assert.Equal("TẦNG 2", mbDecoded.StoryName);
        }

        [Fact]
        public void Codec_ThrowsOnMismatchedRegApp()
        {
            var codec = new MongCocXDataCodec();
            var foreignPacket = new XDataPacket("OTHER_APP");
            Assert.Throws<InvalidOperationException>(() => codec.Decode(foreignPacket));
        }
    }
}
