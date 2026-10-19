using System;
using System.IO;
using System.Text.Json;
using HPDcePro.Core.Common;
using HPDcePro.Core.Serialization;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public class AutoStrSerializerExtendedTests
    {
        private static string RealProjectFilePath => AssetPathResolver.ResolveAutoStrProject("DuAnMau.autostr");

        [Fact]
        public void Deserialize_RealDuAnMauFile_VerifiesAllProjectSettings()
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
            Assert.Equal("B20", config.MacBtDamSan);
            Assert.Equal("CB400-V", config.MacThep);

            // Floors check
            Assert.NotNull(config.Floors);
            Assert.Equal(2, config.Floors.Count);
            Assert.Equal("TẦNG 1", config.Floors[0].TenTang);
            Assert.Equal("TẦNG 2", config.Floors[1].TenTang);

            // Step configurations
            Assert.NotNull(config.StepEnabled);
            Assert.Equal(25, config.StepEnabled.Count);
            Assert.All(config.StepEnabled, step => Assert.True(step));

            Assert.True(config.AiNghiemThuMoiBuoc);
            Assert.Equal("4,5,6,7,13,14,15,20", config.AiNghiemThuCacBuoc);
        }

        [Fact]
        public void Deserialize_RealDuAnMauFile_VerifiesUtf8BomStripping()
        {
            if (!File.Exists(RealProjectFilePath))
            {
                return;
            }

            byte[] rawBytes = File.ReadAllBytes(RealProjectFilePath);
            Assert.True(rawBytes.Length > 3);
            Assert.Equal(0xEF, rawBytes[0]);
            Assert.Equal(0xBB, rawBytes[1]);
            Assert.Equal(0xBF, rawBytes[2]);

            var config = AutoStrProjectSerializer.DeserializeFromBytes(rawBytes);
            Assert.NotNull(config);
            Assert.Equal("Dự án mẫu", config.TenDuAn);
        }

        [Fact]
        public void Deserialize_EmptyJsonObject_InitializesDefaultConfig()
        {
            var config = AutoStrProjectSerializer.DeserializeFromString("{}");

            Assert.NotNull(config);
            Assert.Equal("Dự án mới", config.TenDuAn);
            Assert.Equal(220.0, config.BeRongDamDienHinh);
            Assert.Equal(400.0, config.ChieuCaoDamDienHinh);
            Assert.Equal("CB400-V", config.MacThep);
            Assert.NotNull(config.Layers);
            Assert.NotNull(config.Folders);
        }

        [Fact]
        public void Deserialize_MissingOptionalKeys_PopulatesSensibleDefaults()
        {
            string partialJson = "{\"TenDuAn\":\"Dự án riêng\",\"MacBtDamSan\":\"B30\"}";
            var config = AutoStrProjectSerializer.DeserializeFromString(partialJson);

            Assert.NotNull(config);
            Assert.Equal("Dự án riêng", config.TenDuAn);
            Assert.Equal("B30", config.MacBtDamSan);
            Assert.Equal(220.0, config.BeRongDamDienHinh);
            Assert.NotNull(config.Floors);
            Assert.Empty(config.Floors);
        }

        [Fact]
        public void Deserialize_JsonWithCommentsAndTrailingCommas_Succeeds()
        {
            string jsonWithComments = @"
            {
                // Thông tin dự án
                ""TenDuAn"": ""Dự án có chú thích"",
                /* Cấu hình dầm sàn */
                ""BeRongDamDienHinh"": 300.0,
                ""ChieuCaoDamDienHinh"": 600.0,
            }";

            var config = AutoStrProjectSerializer.DeserializeFromString(jsonWithComments);

            Assert.NotNull(config);
            Assert.Equal("Dự án có chú thích", config.TenDuAn);
            Assert.Equal(300.0, config.BeRongDamDienHinh);
            Assert.Equal(600.0, config.ChieuCaoDamDienHinh);
        }

        [Fact]
        public void Deserialize_NullOrWhitespace_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => AutoStrProjectSerializer.DeserializeFromString(null!));
            Assert.Throws<ArgumentNullException>(() => AutoStrProjectSerializer.DeserializeFromString(string.Empty));
            Assert.Throws<ArgumentNullException>(() => AutoStrProjectSerializer.DeserializeFromString("   \t\n"));
            Assert.Throws<ArgumentException>(() => AutoStrProjectSerializer.DeserializeFromBytes(Array.Empty<byte>()));
        }

        [Fact]
        public void Deserialize_MalformedJson_ThrowsJsonException()
        {
            string brokenJson = "{ \"TenDuAn\": \"Không đóng ngoặc...";
            Assert.Throws<JsonException>(() => AutoStrProjectSerializer.DeserializeFromString(brokenJson));
        }
    }
}
