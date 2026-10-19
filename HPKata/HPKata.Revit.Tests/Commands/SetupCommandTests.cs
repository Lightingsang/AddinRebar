using System;
using System.IO;
using HPKata.Core.Configuration;
using HPKata.UI.ViewModels;
using Xunit;

namespace HPKata.Revit.Tests.Commands
{
    public sealed class SetupCommandTests
    {
        [Fact]
        public void GetConfigFilePath_DefaultEnvironment_ReturnsStandardPath()
        {
            string configPath = KataInstallEnvironment.GetConfigFilePath();
            Assert.Equal(@"C:\HPKata\Goi lenh.lsp", configPath);
        }

        [Fact]
        public void GetConfigFilePath_ScopedEnvironment_ReturnsScopedPath()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "Kata_Test_" + Guid.NewGuid().ToString("N"));
            using (KataInstallEnvironment.UseRootDirectory(tempDir))
            {
                string path = KataInstallEnvironment.GetConfigFilePath();
                Assert.Equal(Path.Combine(tempDir, "Goi lenh.lsp"), path);
            }
        }

        [Fact]
        public void SetupCommandConfigSync_LoadsEditsAndSaves86Parameters()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "Kata_Sync_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(tempDir);
                using (KataInstallEnvironment.UseRootDirectory(tempDir))
                {
                    string lspPath = KataInstallEnvironment.GetConfigFilePath();

                    // Step 1: Save initial configuration
                    var initialConfig = KataConfigDefaults.CreateDefault();
                    initialConfig.ConcreteCoverMm = 35.0;
                    initialConfig.StandardBarLength = 11700;
                    initialConfig.BeamTopCutoffL1 = 0.25;
                    KataConfigParser.SaveToFile(lspPath, initialConfig);

                    Assert.True(File.Exists(lspPath));

                    // Step 2: Parse file back
                    var loadedConfig = KataConfigParser.ParseFile(lspPath);
                    Assert.Equal(35.0, loadedConfig.ConcreteCoverMm);
                    Assert.Equal(11700, loadedConfig.StandardBarLength);
                    Assert.Equal(0.25, loadedConfig.BeamTopCutoffL1);

                    // Step 3: Modify via SetupViewModel and save
                    var vm = new SetupViewModel(loadedConfig)
                    {
                        ConcreteCoverMm = 40.0,
                        StandardBarLength = 12000
                    };
                    Assert.True(vm.IsDirty);

                    var updatedConfig = vm.ToConfig();
                    KataConfigParser.SaveToFile(lspPath, updatedConfig);

                    // Step 4: Verify roundtrip fidelity on disk
                    var reloaded = KataConfigParser.ParseFile(lspPath);
                    Assert.Equal(40.0, reloaded.ConcreteCoverMm);
                    Assert.Equal(12000, reloaded.StandardBarLength);
                }
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
    }
}
