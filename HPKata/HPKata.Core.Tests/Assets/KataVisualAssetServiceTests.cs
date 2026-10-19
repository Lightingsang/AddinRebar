using System;
using System.IO;
using System.Text;
using HPKata.Core.Assets;
using HPKata.Core.Configuration;
using Xunit;

namespace HPKata.Core.Tests.Assets
{
    public class KataVisualAssetServiceTests
    {
        [Theory]
        [InlineData("00", "00.wmf")]
        [InlineData("01", "01.wmf")]
        [InlineData("11", "11.wmf")]
        [InlineData("67", "67.wmf")]
        [InlineData("dang00", "00.wmf")]
        public void GetShapeDiagramWmfPath_ResolvesCanonicalFileName(string inputCode, string expectedFileName)
        {
            var service = new KataVisualAssetService();
            string? path = service.GetShapeDiagramWmfPath(inputCode);

            Assert.NotNull(path);
            Assert.EndsWith(expectedFileName, path, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("dangthep", path, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetShapeDiagramPngPath_ResolvesCanonicalNaming()
        {
            var service = new KataVisualAssetService();
            string? path = service.GetShapeDiagramPngPath("00");

            Assert.NotNull(path);
            Assert.True(path.EndsWith("Kata-00.png", StringComparison.OrdinalIgnoreCase) ||
                        path.EndsWith("00.png", StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [InlineData("Crank Lap Bar", "Crank Lap Bar.wmf")]
        [InlineData("Starter L Bar.wmf", "Starter L Bar.wmf")]
        public void GetColumnSpliceDiagramPath_ResolvesCotDirectory(string inputName, string expectedFileName)
        {
            var service = new KataVisualAssetService();
            string? path = service.GetColumnSpliceDiagramPath(inputName);

            Assert.NotNull(path);
            Assert.EndsWith(expectedFileName, path, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Cot", path, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("ve_dam.png", "ve_dam.png")]
        [InlineData("ve_cot", "ve_cot.png")]
        [InlineData("ve_vach.png", "ve_vach.png")]
        public void GetRibbonIconPath_ResolvesRibbonDirectory(string inputName, string expectedFileName)
        {
            var service = new KataVisualAssetService();
            string? path = service.GetRibbonIconPath(inputName);

            Assert.NotNull(path);
            Assert.EndsWith(expectedFileName, path, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Ribbon", path, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void StreamAndBytes_WithTemporaryAsset_ReadsContentAccurately()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "Kata_AssetTest_" + Guid.NewGuid().ToString("N"));
            try
            {
                var customEnv = new DefaultKataInstallEnvironment(tempDir);
                customEnv.EnsureDirectoriesExist();

                string sampleShape = Path.Combine(customEnv.DangThepDirectory, "99.wmf");
                byte[] testBytes = Encoding.UTF8.GetBytes("WMF_SAMPLE_HEADER_AND_RECORD_DATA");
                File.WriteAllBytes(sampleShape, testBytes);

                var service = new KataVisualAssetService(customEnv);

                Assert.True(service.AssetExists(sampleShape));
                Assert.True(service.AssetExists(Path.Combine("dangthep", "99.wmf")));

                using (var stream = service.OpenAssetStream(sampleShape))
                {
                    Assert.NotNull(stream);
                    Assert.Equal(testBytes.Length, stream!.Length);
                }

                byte[]? readBytes = service.GetAssetBytes(sampleShape);
                Assert.NotNull(readBytes);
                Assert.Equal(testBytes, readBytes);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public void CentralInstalledAssets_WhenAvailable_AreResolvable()
        {
            if (!Directory.Exists(@"C:\HPKata")) return;

            var service = new KataVisualAssetService();
            string? wmf00 = service.GetShapeDiagramWmfPath("00");
            Assert.NotNull(wmf00);
            if (File.Exists(wmf00))
            {
                Assert.True(service.AssetExists(wmf00));
                using var stream = service.OpenAssetStream(wmf00);
                Assert.NotNull(stream);
            }
        }
    }
}
