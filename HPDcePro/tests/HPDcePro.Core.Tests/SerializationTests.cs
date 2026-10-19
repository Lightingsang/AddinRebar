using System;
using System.IO;
using HPDcePro.Core.Common;
using HPDcePro.Core.Serialization;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public class SerializationTests
    {
        private static string RealProjectFilePath => AssetPathResolver.ResolveAutoStrProject("DuAnMau.autostr");

        [Fact]
        public void Deserialize_RealDuAnMauFile_ParsesCorrectly()
        {
            if (!File.Exists(RealProjectFilePath))
            {
                return;
            }

            var config = AutoStrProjectSerializer.DeserializeFromFile(RealProjectFilePath);

            Assert.NotNull(config);
            Assert.Equal("Dự án mẫu", config.TenDuAn);
            Assert.True(string.IsNullOrEmpty(config.ProjectRootPath) || config.ProjectRootPath.EndsWith("AutoStr"));
            Assert.Equal("B20", config.MacBtCotVach);
            Assert.Equal("CB400-V", config.MacThep);
            Assert.Equal(220.0, config.BeRongDamDienHinh);
            Assert.Equal(400.0, config.ChieuCaoDamDienHinh);
            Assert.Equal(120.0, config.ChieuDaySanDienHinh);
            Assert.Equal("C", config.TienToCot);

            // Floors
            Assert.NotNull(config.Floors);
            Assert.Equal(2, config.Floors.Count);
            Assert.Equal(1, config.Floors[0].StoryIndex);
            Assert.Equal("TẦNG 1", config.Floors[0].TenTang);
            Assert.Equal("T1", config.Floors[0].KyHieuTang);
            Assert.Equal(3600.0, config.Floors[0].ChieuCaoTang);

            // Slab rebar
            Assert.NotNull(config.SlabRebar);
            Assert.Equal(10, config.SlabRebar.DuongKinhDuoi_X);
            Assert.Equal(200, config.SlabRebar.KhoangCachDuoi_X);

            // PointSpring
            Assert.NotNull(config.PointSpring);
            Assert.Equal(700.0, config.PointSpring.ChieuCaoDaiMong);

            // PointSpringList
            Assert.NotEmpty(config.PointSpringList);
            Assert.Equal("SP1", config.PointSpringList[0].Name);

            // Layers
            Assert.NotNull(config.Layers);
            Assert.Equal("KT_TRUC", config.Layers.LayerLuoiTruc);
            Assert.Equal("DCE_Beam", config.Layers.LayerKcDam);
        }

        [Fact]
        public void RoundTripSerialization_PreservesAllData()
        {
            var config = new AutoStrConfig
            {
                TenDuAn = "Kiểm tra Roundtrip",
                ProjectRootPath = @"D:\Projects\Test",
                MacThep = "CB500-V",
                BeRongDamDienHinh = 300.0,
                ChieuCaoDamDienHinh = 600.0
            };
            config.Floors.Add(new AutoStrFloorInfo
            {
                StoryIndex = 1,
                TenTang = "Tầng trệt",
                KyHieuTang = "T0",
                ChieuCaoTang = 4200.0
            });

            string json = AutoStrProjectSerializer.SerializeToString(config);
            var deserialized = AutoStrProjectSerializer.DeserializeFromString(json);

            Assert.Equal(config.TenDuAn, deserialized.TenDuAn);
            Assert.Equal(config.ProjectRootPath, deserialized.ProjectRootPath);
            Assert.Equal(config.MacThep, deserialized.MacThep);
            Assert.Equal(300.0, deserialized.BeRongDamDienHinh);
            Assert.Single(deserialized.Floors);
            Assert.Equal("Tầng trệt", deserialized.Floors[0].TenTang);
        }

        [Fact]
        public void Deserialize_WithUtf8BomBytes_TrimsBomCleanly()
        {
            string rawJson = "{\"TenDuAn\":\"BOM Test\",\"BeRongDamDienHinh\":250.0}";
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(rawJson);

            // Prepend UTF-8 BOM: 0xEF, 0xBB, 0xBF
            byte[] bomBytes = new byte[jsonBytes.Length + 3];
            bomBytes[0] = 0xEF;
            bomBytes[1] = 0xBB;
            bomBytes[2] = 0xBF;
            Buffer.BlockCopy(jsonBytes, 0, bomBytes, 3, jsonBytes.Length);

            var config = AutoStrProjectSerializer.DeserializeFromBytes(bomBytes);
            Assert.NotNull(config);
            Assert.Equal("BOM Test", config.TenDuAn);
            Assert.Equal(250.0, config.BeRongDamDienHinh);
        }
    }
}
