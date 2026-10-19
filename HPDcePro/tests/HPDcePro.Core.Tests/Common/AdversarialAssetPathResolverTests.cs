using System;
using System.IO;
using System.Threading.Tasks;
using HPDcePro.Core.Common;
using Xunit;

namespace HPDcePro.Core.Tests.Common
{
    [Collection("AssetPathTests")]
    public class AdversarialAssetPathResolverTests
    {
        [Fact]
        public void NonExistentAsset_ThrowsDescriptiveFileNotFoundException_NotNRE()
        {
            var exCad = Assert.Throws<FileNotFoundException>(() =>
                AssetPathResolver.ResolveCadTemplate("FakeTemplate_NonExistent.dwg"));
            Assert.Contains("FakeTemplate_NonExistent.dwg", exCad.Message);
            Assert.False(string.IsNullOrWhiteSpace(exCad.FileName));

            var exAutoStr = Assert.Throws<FileNotFoundException>(() =>
                AssetPathResolver.ResolveAutoStrDwg("FakeAutoStr_NonExistent.dwg"));
            Assert.Contains("FakeAutoStr_NonExistent.dwg", exAutoStr.Message);

            var exExcel = Assert.Throws<FileNotFoundException>(() =>
                AssetPathResolver.ResolveExcelWorkbook("FakeDir/FakeWorkbook.xlsm"));
            Assert.Contains("FakeWorkbook.xlsm", exExcel.Message);

            var exSys = Assert.Throws<FileNotFoundException>(() =>
                AssetPathResolver.ResolveSystemConfig("NonExistentConfig.txt"));
            Assert.Contains("NonExistentConfig.txt", exSys.Message);

            var exLang = Assert.Throws<FileNotFoundException>(() =>
                AssetPathResolver.ResolveLanguageDict("NonExistentLang.txt"));
            Assert.Contains("NonExistentLang.txt", exLang.Message);

            var exMenu = Assert.Throws<FileNotFoundException>(() =>
                AssetPathResolver.ResolveMenu("NonExistentMenu.cuix"));
            Assert.Contains("NonExistentMenu.cuix", exMenu.Message);
        }

        [Fact]
        public void TestSeam_UseAssetsRoot_OverridesAndRevertsCleanly()
        {
            string baselineRoot = AssetPathResolver.GetAssetsRoot();
            string tempDir = Path.Combine(Path.GetTempPath(), "HpDce_Seam_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(Path.Combine(tempDir, "Cad"));
            File.WriteAllText(Path.Combine(tempDir, "Cad", "SeamTemplate.dwg"), "DWG_MOCK");

            try
            {
                using (AssetPathResolver.UseAssetsRoot(tempDir))
                {
                    Assert.Equal(tempDir, AssetPathResolver.GetAssetsRoot());
                    string resolved = AssetPathResolver.ResolveCadTemplate("SeamTemplate.dwg");
                    Assert.Equal(Path.Combine(tempDir, "Cad", "SeamTemplate.dwg"), resolved);
                }

                Assert.Equal(baselineRoot, AssetPathResolver.GetAssetsRoot());
                Assert.Throws<FileNotFoundException>(() =>
                    AssetPathResolver.ResolveCadTemplate("SeamTemplate.dwg"));
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
        public void TestSeam_NestedScopes_RevertsToParentScope()
        {
            string baseRoot = AssetPathResolver.GetAssetsRoot();
            string dir1 = Path.Combine(Path.GetTempPath(), "HpDce_Scope1_" + Guid.NewGuid().ToString("N"));
            string dir2 = Path.Combine(Path.GetTempPath(), "HpDce_Scope2_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir1);
            Directory.CreateDirectory(dir2);

            try
            {
                using (AssetPathResolver.UseAssetsRoot(dir1))
                {
                    Assert.Equal(dir1, AssetPathResolver.GetAssetsRoot());

                    using (AssetPathResolver.UseAssetsRoot(dir2))
                    {
                        Assert.Equal(dir2, AssetPathResolver.GetAssetsRoot());
                    }

                    Assert.Equal(dir1, AssetPathResolver.GetAssetsRoot());
                }

                Assert.Equal(baseRoot, AssetPathResolver.GetAssetsRoot());
            }
            finally
            {
                if (Directory.Exists(dir1)) Directory.Delete(dir1, true);
                if (Directory.Exists(dir2)) Directory.Delete(dir2, true);
            }
        }

        [Fact]
        public async Task TestSeam_UseAssetsRoot_AsyncLocalFlowsAcrossTasks()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "HpDce_Async_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                using (AssetPathResolver.UseAssetsRoot(tempDir))
                {
                    string rootInTask = await Task.Run(() => AssetPathResolver.GetAssetsRoot());
                    Assert.Equal(tempDir, rootInTask);
                }
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void EnvironmentVariableOverride_DirectsResolutionAndCleansUp()
        {
            string baselineRoot = AssetPathResolver.GetAssetsRoot();
            string tempEnvDir = Path.Combine(Path.GetTempPath(), "HpDce_Env_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempEnvDir);

            try
            {
                Environment.SetEnvironmentVariable(AssetPathResolver.EnvAssetsDir, tempEnvDir);
                AssetPathResolver.ResetOverride();
                Assert.Equal(tempEnvDir, AssetPathResolver.GetAssetsRoot());

                Environment.SetEnvironmentVariable(AssetPathResolver.EnvAssetsDir, null);
                Environment.SetEnvironmentVariable(AssetPathResolver.EnvAssetsPath, tempEnvDir);
                AssetPathResolver.ResetOverride();
                Assert.Equal(tempEnvDir, AssetPathResolver.GetAssetsRoot());
            }
            finally
            {
                Environment.SetEnvironmentVariable(AssetPathResolver.EnvAssetsDir, null);
                Environment.SetEnvironmentVariable(AssetPathResolver.EnvAssetsPath, null);
                AssetPathResolver.ResetOverride();
                Assert.Equal(baselineRoot, AssetPathResolver.GetAssetsRoot());
                if (Directory.Exists(tempEnvDir)) Directory.Delete(tempEnvDir, true);
            }
        }

        [Fact]
        public void MultiHostSimulation_WorkingDirIndependence()
        {
            string originalCwd = Environment.CurrentDirectory;
            string root = AssetPathResolver.GetAssetsRoot();
            string[] testDirs = { Path.GetTempPath(), @"C:\", AppContext.BaseDirectory };

            foreach (var testDir in testDirs)
            {
                if (!Directory.Exists(testDir)) continue;
                try
                {
                    Environment.CurrentDirectory = testDir;
                    AssetPathResolver.ResetOverride();
                    string currentRoot = AssetPathResolver.GetAssetsRoot();
                    Assert.Equal(root, currentRoot);
                    Assert.True(File.Exists(AssetPathResolver.ResolveMachineLearningDatabase()));
                }
                finally
                {
                    Environment.CurrentDirectory = originalCwd;
                }
            }
        }

        [Fact]
        public void PathTraversal_CanEscapeAssetsDirectory_ConfirmedSecurityRisk()
        {
            // Adversarial test: Probe whether path traversal can escape assets directory
            // Path: ../../src/HPDcePro.Core/Common/AssetPathResolver.cs
            string escapeAttempt = "../../src/HPDcePro.Core/Common/AssetPathResolver.cs";
            
            // Empirical observation: ResolveCadTemplate does NOT restrict paths to assets directory
            // Nor does it validate file extensions. It returns the escaped path because File.Exists evaluates to true!
            string resolved = AssetPathResolver.ResolveCadTemplate(escapeAttempt);
            Assert.True(File.Exists(resolved));
            Assert.EndsWith("AssetPathResolver.cs", Path.GetFullPath(resolved));

            // Similarly for ResolveExcelWorkbook
            string resolvedWb = AssetPathResolver.ResolveExcelWorkbook(escapeAttempt);
            Assert.True(File.Exists(resolvedWb));
            Assert.EndsWith("AssetPathResolver.cs", Path.GetFullPath(resolvedWb));
        }

        [Fact]
        public void NullInput_ThrowsNullReferenceException_InTrimStartMethods()
        {
            // Methods using relativePath.TrimStart('\\', '/') throw NullReferenceException instead of ArgumentNullException
            Assert.Throws<NullReferenceException>(() => AssetPathResolver.ResolveExcelWorkbook(null!));
            Assert.Throws<NullReferenceException>(() => AssetPathResolver.ResolveLanguageDict(null!));
            Assert.Throws<NullReferenceException>(() => AssetPathResolver.ResolveIcon(null!));
            Assert.Throws<NullReferenceException>(() => AssetPathResolver.ResolveAsset(null!));

            // Methods using Path.Combine directly throw ArgumentNullException
            Assert.Throws<ArgumentNullException>(() => AssetPathResolver.ResolveCadTemplate(null!));
            Assert.Throws<ArgumentNullException>(() => AssetPathResolver.ResolveAutoStrDwg(null!));
        }

        [Fact]
        public void NonExistentOverrideDirectory_IsSilentlyIgnoredByGetAssetsRoot()
        {
            string baselineRoot = AssetPathResolver.GetAssetsRoot();
            string nonExistentPath = Path.Combine(Path.GetTempPath(), "HpDce_NonExistent_" + Guid.NewGuid().ToString("N"));

            // 1. Ambient root with non-existent path
            using (AssetPathResolver.UseAssetsRoot(nonExistentPath))
            {
                // Empirically observes that Directory.Exists check causes GetAssetsRoot to silently ignore ambient override
                Assert.Equal(baselineRoot, AssetPathResolver.GetAssetsRoot());
            }

            // 2. Custom root with non-existent path
            try
            {
                AssetPathResolver.CustomAssetsRoot = nonExistentPath;
                Assert.Equal(baselineRoot, AssetPathResolver.GetAssetsRoot());
            }
            finally
            {
                AssetPathResolver.CustomAssetsRoot = null;
            }

            // 3. Env var with non-existent path
            try
            {
                Environment.SetEnvironmentVariable(AssetPathResolver.EnvAssetsDir, nonExistentPath);
                AssetPathResolver.ResetOverride();
                Assert.Equal(baselineRoot, AssetPathResolver.GetAssetsRoot());
            }
            finally
            {
                Environment.SetEnvironmentVariable(AssetPathResolver.EnvAssetsDir, null);
                AssetPathResolver.ResetOverride();
            }
        }

        [Fact]
        public void PathTraversal_SubPathsNormalization_StressTest()
        {
            // Normal relative path within Cad
            string normal = AssetPathResolver.ResolveCadTemplate("Form 1.dwg");
            Assert.True(File.Exists(normal));

            // Sub-path with inner redundant navigation "./"
            string dotSlash = AssetPathResolver.ResolveExcelWorkbook("./Menu DCE_2020.xlsm");
            Assert.True(File.Exists(dotSlash));

            // Non-existent path traversal throws FileNotFoundException
            Assert.Throws<FileNotFoundException>(() =>
                AssetPathResolver.ResolveCadTemplate("../../NonExistentOutside.dwg"));

            // Verify ResolveAsset handles backslash and slash trimming
            string asset1 = Path.GetFullPath(AssetPathResolver.ResolveAsset("/MayHoc/mauhoc.db"));
            string asset2 = Path.GetFullPath(AssetPathResolver.ResolveAsset("\\MayHoc\\mauhoc.db"));
            Assert.Equal(asset1, asset2);
            Assert.True(AssetPathResolver.AssetExists("/MayHoc/mauhoc.db"));
        }
    }
}
