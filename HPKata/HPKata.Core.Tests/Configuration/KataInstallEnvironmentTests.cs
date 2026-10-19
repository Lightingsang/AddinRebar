using System;
using System.IO;
using System.Threading.Tasks;
using HPKata.Core.Configuration;
using Xunit;

namespace HPKata.Core.Tests.Configuration
{
    public class KataInstallEnvironmentTests
    {
        [Fact]
        public void DefaultKataInstallEnvironment_DefaultRoot_ResolvesToHPKata()
        {
            var env = new DefaultKataInstallEnvironment();

            Assert.Equal(@"C:\HPKata", env.RootDirectory);
            Assert.Equal(@"C:\HPKata\Kata.xlsm", env.KataWorkbookPath);
            Assert.Equal(@"C:\HPKata\Kata ENG.xlsm", env.KataEngWorkbookPath);
            Assert.Equal(@"C:\HPKata\Iso_QHPlus.xls", env.IsoQhPlusPath);
            Assert.Equal(@"C:\HPKata\Iso_kata.dwg", env.IsoKataDwgPath);
            Assert.Equal(@"C:\HPKata\ISO_KATA.EDB", env.IsoKataEdbPath);
            Assert.Equal(@"C:\HPKata\kata.ico", env.KataIconPath);
            Assert.Equal(@"C:\HPKata\Goi lenh.lsp", env.GoiLenhLspPath);
            Assert.Equal(@"C:\HPKata\GoiLenh_HPKata.lsp", env.GoiLenhHpKataLspPath);
            Assert.Equal(@"C:\HPKata\menukata.cuix", env.MenuKataCuixPath);
            Assert.Equal(@"C:\HPKata\menukata.cui", env.MenuKataCuiPath);
            Assert.Equal(@"C:\HPKata\menukata.mnr", env.MenuKataMnrPath);
            Assert.Equal(@"C:\HPKata\menukata_light.mnr", env.MenuKataLightMnrPath);
            Assert.Equal(@"C:\HPKata\dangthep", env.DangThepDirectory);
            Assert.Equal(@"C:\HPKata\Cot", env.CotDirectory);
            Assert.Equal(@"C:\HPKata\Icon", env.IconDirectory);
            Assert.Equal(@"C:\HPKata\Icon\Ribbon", env.RibbonIconDirectory);
            Assert.Equal(@"C:\HPKata\IMG", env.ImgDirectory);
            Assert.Equal(@"C:\HPKata\Temp", env.TempDirectory);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void DefaultKataInstallEnvironment_NullOrWhitespace_DefaultsToHPKata(string? root)
        {
            var env = new DefaultKataInstallEnvironment(root);
            Assert.Equal(KataInstallEnvironment.DefaultRootDirectory, env.RootDirectory);
        }

        [Fact]
        public void DefaultKataInstallEnvironment_CustomRoot_NormalizesAndTrimsTrailingSlash()
        {
            var envWithSlash = new DefaultKataInstallEnvironment(@"D:\CustomKata\");
            var envNoSlash = new DefaultKataInstallEnvironment(@"D:\CustomKata");

            Assert.Equal(@"D:\CustomKata", envWithSlash.RootDirectory);
            Assert.Equal(@"D:\CustomKata", envNoSlash.RootDirectory);
            Assert.Equal(@"D:\CustomKata\Iso_kata.dwg", envWithSlash.IsoKataDwgPath);
        }

        [Fact]
        public void DefaultKataInstallEnvironment_DriveRoot_PreservesTrailingSlash()
        {
            var env = new DefaultKataInstallEnvironment(@"Z:\");
            Assert.Equal(@"Z:\", env.RootDirectory);
            Assert.Equal(@"Z:\Iso_kata.dwg", env.IsoKataDwgPath);
        }

        [Fact]
        public void DefaultKataInstallEnvironment_GetCanonicalPath_ResolvesCorrectly()
        {
            var env = new DefaultKataInstallEnvironment(@"D:\TestRoot");

            var rel = env.GetCanonicalPath(@"dangthep\01.wmf");
            Assert.Equal(@"D:\TestRoot\dangthep\01.wmf", rel);

            var rooted = env.GetCanonicalPath(@"E:\External\file.txt");
            Assert.Equal(@"E:\External\file.txt", rooted);

            Assert.Throws<ArgumentException>(() => env.GetCanonicalPath(null!));
            Assert.Throws<ArgumentException>(() => env.GetCanonicalPath(""));
            Assert.Throws<ArgumentException>(() => env.GetCanonicalPath("   "));
        }

        [Fact]
        public void DefaultKataInstallEnvironment_IsInstalled_ChecksDirectoryAndCoreFiles()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "HPKata_Test_IsInstalled_" + Guid.NewGuid().ToString("N"));
            var env = new DefaultKataInstallEnvironment(tempDir);

            try
            {
                Assert.False(env.IsInstalled);

                Directory.CreateDirectory(tempDir);
                Assert.False(env.IsInstalled);

                File.WriteAllText(env.IsoKataDwgPath, "dwg-stub");
                Assert.False(env.IsInstalled);

                File.WriteAllText(env.KataWorkbookPath, "xlsm-stub");
                Assert.True(env.IsInstalled);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
        }

        [Fact]
        public void DefaultKataInstallEnvironment_EnsureDirectoriesExist_CreatesDirectories()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "HPKata_Test_EnsureDir_" + Guid.NewGuid().ToString("N"));
            var env = new DefaultKataInstallEnvironment(tempDir);

            try
            {
                env.EnsureDirectoriesExist();

                Assert.True(Directory.Exists(env.RootDirectory));
                Assert.True(Directory.Exists(env.DangThepDirectory));
                Assert.True(Directory.Exists(env.CotDirectory));
                Assert.True(Directory.Exists(env.IconDirectory));
                Assert.True(Directory.Exists(env.RibbonIconDirectory));
                Assert.True(Directory.Exists(env.ImgDirectory));
                Assert.True(Directory.Exists(env.TempDirectory));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
        }

        [Fact]
        public void KataInstallEnvironment_Current_DefaultsToHPKata()
        {
            Assert.Equal(KataInstallEnvironment.DefaultRootDirectory, KataInstallEnvironment.Current.RootDirectory);
            Assert.Equal(@"C:\HPKata\Kata.xlsm", KataInstallEnvironment.Current.KataWorkbookPath);
        }

        [Fact]
        public void KataInstallEnvironment_UseRootDirectory_SetsScopeAndRestores()
        {
            Assert.Equal(KataInstallEnvironment.DefaultRootDirectory, KataInstallEnvironment.Current.RootDirectory);

            using (var token = KataInstallEnvironment.UseRootDirectory(@"D:\ScopedRoot"))
            {
                Assert.Equal(@"D:\ScopedRoot", KataInstallEnvironment.Current.RootDirectory);
                Assert.Equal(@"D:\ScopedRoot\Kata.xlsm", KataInstallEnvironment.Current.KataWorkbookPath);

                // Idempotent dispose check
                token.Dispose();
                token.Dispose();
            }

            Assert.Equal(KataInstallEnvironment.DefaultRootDirectory, KataInstallEnvironment.Current.RootDirectory);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void KataInstallEnvironment_UseRootDirectory_Invalid_ThrowsArgumentException(string? invalidRoot)
        {
            Assert.Throws<ArgumentException>(() => KataInstallEnvironment.UseRootDirectory(invalidRoot!));
        }

        [Fact]
        public void KataInstallEnvironment_UseEnvironment_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => KataInstallEnvironment.UseEnvironment(null!));
        }

        [Fact]
        public void KataInstallEnvironment_NestedScopes_RestoreInLifoOrder()
        {
            using (KataInstallEnvironment.UseRootDirectory(@"D:\OuterScope"))
            {
                Assert.Equal(@"D:\OuterScope", KataInstallEnvironment.Current.RootDirectory);

                using (KataInstallEnvironment.UseRootDirectory(@"D:\InnerScope"))
                {
                    Assert.Equal(@"D:\InnerScope", KataInstallEnvironment.Current.RootDirectory);
                }

                Assert.Equal(@"D:\OuterScope", KataInstallEnvironment.Current.RootDirectory);
            }

            Assert.Equal(KataInstallEnvironment.DefaultRootDirectory, KataInstallEnvironment.Current.RootDirectory);
        }

        [Fact]
        public void KataInstallEnvironment_SetGlobal_OverridesDefaultAndResetRestores()
        {
            try
            {
                var custom = new DefaultKataInstallEnvironment(@"E:\GlobalEnv");
                KataInstallEnvironment.SetGlobal(custom);
                Assert.Same(custom, KataInstallEnvironment.Current);

                using (KataInstallEnvironment.UseRootDirectory(@"F:\LocalScope"))
                {
                    Assert.Equal(@"F:\LocalScope", KataInstallEnvironment.Current.RootDirectory);
                }

                Assert.Same(custom, KataInstallEnvironment.Current);
            }
            finally
            {
                KataInstallEnvironment.ResetGlobal();
            }

            Assert.Equal(KataInstallEnvironment.DefaultRootDirectory, KataInstallEnvironment.Current.RootDirectory);
        }

        [Fact]
        public async Task KataInstallEnvironment_AsyncLocal_IsolatesConcurrentTasks()
        {
            var task1 = Task.Run(async () =>
            {
                using (KataInstallEnvironment.UseRootDirectory(@"D:\Task1Root"))
                {
                    await Task.Delay(30);
                    Assert.Equal(@"D:\Task1Root", KataInstallEnvironment.Current.RootDirectory);
                }
            });

            var task2 = Task.Run(async () =>
            {
                using (KataInstallEnvironment.UseRootDirectory(@"D:\Task2Root"))
                {
                    await Task.Delay(30);
                    Assert.Equal(@"D:\Task2Root", KataInstallEnvironment.Current.RootDirectory);
                }
            });

            await Task.WhenAll(task1, task2);
            Assert.Equal(KataInstallEnvironment.DefaultRootDirectory, KataInstallEnvironment.Current.RootDirectory);
        }
    }
}
