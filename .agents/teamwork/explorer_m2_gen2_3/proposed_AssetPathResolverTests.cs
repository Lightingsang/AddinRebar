using System;
using System.IO;
using HPDcePro.Core.Common;
using Xunit;

namespace HPDcePro.Core.Tests.Common
{
    public class AssetPathResolverTests
    {
        [Fact]
        public void GetAssetsRoot_LocatesExistingDirectory()
        {
            string root = AssetPathResolver.GetAssetsRoot();
            Assert.True(Directory.Exists(root), $"Assets root '{root}' does not exist.");
            Assert.True(File.Exists(Path.Combine(root, "MayHoc", "mauhoc.db")), "Assets root must contain MayHoc/mauhoc.db.");
        }

        [Fact]
        public void ResolveCadTemplate_All13CadDwgs_ExistAndNonEmpty()
        {
            string[] cadDwgs =
            {
                "Form 1.dwg", "Form 1_Steel.dwg", "Form 2.dwg", "Form 2_2.dwg",
                "Form 2_2_Circle3.2.dwg", "Form 2_Circle3.2.dwg", "Form 2_Steel.dwg",
                "Form 3.dwg", "Form 3_Steel.dwg", "Form 4.dwg", "Form 4_Steel.dwg",
                "Form 5.dwg", "Form 5_Steel.dwg"
            };

            foreach (var dwg in cadDwgs)
            {
                string path = AssetPathResolver.ResolveCadTemplate(dwg);
                Assert.True(File.Exists(path), $"DWG '{dwg}' does not exist at '{path}'.");
                Assert.True(new FileInfo(path).Length > 0, $"DWG '{dwg}' is empty.");
            }
        }

        [Fact]
        public void ResolveAutoStrDwg_All4AutoStrDwgs_ExistAndNonEmpty()
        {
            string[] autoStrDwgs =
            {
                "BlockTenBanVe.dwg", "Chi Tiet Coc 250x250.dwg",
                "GhiChuChung 5574 _ A3.dwg", "Khung A3.dwg"
            };

            foreach (var dwg in autoStrDwgs)
            {
                string path = AssetPathResolver.ResolveAutoStrDwg(dwg);
                Assert.True(File.Exists(path), $"AutoStr DWG '{dwg}' does not exist at '{path}'.");
                Assert.True(new FileInfo(path).Length > 0, $"AutoStr DWG '{dwg}' is empty.");
            }
        }

        [Fact]
        public void ResolveMachineLearning_MauHocDbAndModelZip_ExistAndMatchSize()
        {
            string dbPath = AssetPathResolver.ResolveMachineLearningDatabase();
            Assert.True(File.Exists(dbPath), $"Database not found at '{dbPath}'.");
            Assert.True(new FileInfo(dbPath).Length > 8_000_000, "mauhoc.db must be > 8 MB.");

            string zipPath = AssetPathResolver.ResolveMachineLearningModelZip();
            Assert.True(File.Exists(zipPath), $"Model archive not found at '{zipPath}'.");
            Assert.True(new FileInfo(zipPath).Length > 20_000, "model.zip must be > 20 KB.");
        }

        [Fact]
        public void ResolveExcelWorkbook_RepresentativeWorkbooks_ExistAndNonEmpty()
        {
            string[] representativeXlsms =
            {
                "Menu DCE_2020.xlsm",
                "DceSteel/01 TCVN 2737 2023 Loading.xlsm",
                "DceSteel/02 CreateLoadCombination.xlsm",
                "DceSteel/100 DceSteelCad.xlsm",
                "File V.2020/08. DCE_ColumnWall_2020.xlsm",
                "File V.2020/09. DCE_Beam_2020.xlsm",
                "File V.2020/13. DCE_DaiCoc_2020.xlsm"
            };

            foreach (var xlsm in representativeXlsms)
            {
                string path = AssetPathResolver.ResolveExcelWorkbook(xlsm);
                Assert.True(File.Exists(path), $"Workbook '{xlsm}' does not exist at '{path}'.");
                Assert.True(new FileInfo(path).Length > 0, $"Workbook '{xlsm}' is empty.");
            }
        }

        [Fact]
        public void ResolveLanguageDict_All8Dictionaries_Exist()
        {
            string[] dicts =
            {
                "en.txt", "vi.txt", "zh.txt", "_HUONGDAN.txt",
                "Cad/en.txt", "Cad/vi.txt", "Cad/zh.txt", "Cad/_HUONGDAN.txt"
            };

            foreach (var dict in dicts)
            {
                string path = AssetPathResolver.ResolveLanguageDict(dict);
                Assert.True(File.Exists(path), $"Dictionary '{dict}' does not exist at '{path}'.");
                Assert.True(new FileInfo(path).Length > 0, $"Dictionary '{dict}' is empty.");
            }
        }

        [Fact]
        public void ResolveSystemConfig_CriticalConfigs_Exist()
        {
            string[] criticalConfigs =
            {
                "AcSmComponents2024.txt",
                "AcSmComponents2025.txt",
                "AdsLi.txt",
                "configDb.txt"
            };

            foreach (var cfg in criticalConfigs)
            {
                string path = AssetPathResolver.ResolveSystemConfig(cfg);
                Assert.True(File.Exists(path), $"Config '{cfg}' does not exist at '{path}'.");
            }

            string cachePath = AssetPathResolver.ResolveSystemCache("dcpcore.bin");
            Assert.True(File.Exists(cachePath), $"Cache 'dcpcore.bin' does not exist at '{cachePath}'.");
        }

        [Fact]
        public void ResolveMenu_All7Cuix_Exist()
        {
            string[] menus =
            {
                "DBimApps.cuix", "MenuDCE.cuix", "MenuDCE_ACAD.cuix",
                "MenuDCE_BCAD.cuix", "MenuDCE_GCAD.cuix", "MenuDCE_ICAD.cuix",
                "MenuDCE_ZWCAD.cuix"
            };

            foreach (var menu in menus)
            {
                string path = AssetPathResolver.ResolveMenu(menu);
                Assert.True(File.Exists(path), $"CUIX menu '{menu}' does not exist at '{path}'.");
            }
        }

        [Fact]
        public void ResolveNonExistentAsset_ThrowsFileNotFoundException()
        {
            Assert.Throws<FileNotFoundException>(() => AssetPathResolver.ResolveCadTemplate("NonExistent_999.dwg"));
            Assert.Throws<FileNotFoundException>(() => AssetPathResolver.ResolveExcelWorkbook("NonExistent_999.xlsm"));
        }

        [Fact]
        public void CustomAssetsRoot_Override_RedirectsResolution()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "HPDcePro_AssetTest_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(tempDir);
                AssetPathResolver.CustomAssetsRoot = tempDir;
                Assert.Equal(tempDir, AssetPathResolver.GetAssetsRoot());
            }
            finally
            {
                AssetPathResolver.CustomAssetsRoot = null;
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
    }
}
