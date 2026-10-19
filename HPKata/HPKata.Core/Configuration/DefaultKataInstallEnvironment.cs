using System;
using System.IO;

namespace HPKata.Core.Configuration
{
    /// <summary>
    /// Default implementation of <see cref="IKataInstallEnvironment"/> resolving all canonical paths
    /// relative to a specified root directory (defaulting to @"C:\HPKata").
    /// </summary>
    public class DefaultKataInstallEnvironment : IKataInstallEnvironment
    {
        public string RootDirectory { get; }

        public DefaultKataInstallEnvironment(string? rootDirectory = null)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                RootDirectory = KataInstallEnvironment.DefaultRootDirectory;
            }
            else
            {
                var full = Path.GetFullPath(rootDirectory!.Trim());
                var root = Path.GetPathRoot(full);
                if (!string.Equals(full, root, StringComparison.OrdinalIgnoreCase))
                {
                    full = full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }
                RootDirectory = full;
            }
        }

        public string KataWorkbookPath => Path.Combine(RootDirectory, "Kata.xlsm");

        public string KataEngWorkbookPath => Path.Combine(RootDirectory, "Kata ENG.xlsm");

        public string IsoQhPlusPath => Path.Combine(RootDirectory, "Iso_QHPlus.xls");

        public string IsoKataDwgPath => Path.Combine(RootDirectory, "Iso_kata.dwg");

        public string IsoKataEdbPath => Path.Combine(RootDirectory, "ISO_KATA.EDB");

        public string KataIconPath => Path.Combine(RootDirectory, "kata.ico");

        public string GoiLenhLspPath => Path.Combine(RootDirectory, "Goi lenh.lsp");

        public string GoiLenhHpKataLspPath => Path.Combine(RootDirectory, "GoiLenh_HPKata.lsp");

        public string MenuKataCuixPath => Path.Combine(RootDirectory, "menukata.cuix");

        public string MenuKataCuiPath => Path.Combine(RootDirectory, "menukata.cui");

        public string MenuKataMnrPath => Path.Combine(RootDirectory, "menukata.mnr");

        public string MenuKataLightMnrPath => Path.Combine(RootDirectory, "menukata_light.mnr");

        public string DangThepDirectory => Path.Combine(RootDirectory, "dangthep");

        public string CotDirectory => Path.Combine(RootDirectory, "Cot");

        public string IconDirectory => Path.Combine(RootDirectory, "Icon");

        public string RibbonIconDirectory => Path.Combine(RootDirectory, "Icon", "Ribbon");

        public string ImgDirectory => Path.Combine(RootDirectory, "IMG");

        public string TempDirectory => Path.Combine(RootDirectory, "Temp");

        public string GetCanonicalPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentException("Path cannot be null or whitespace.", nameof(relativePath));
            }

            if (Path.IsPathRooted(relativePath))
            {
                return Path.GetFullPath(relativePath);
            }

            return Path.GetFullPath(Path.Combine(RootDirectory, relativePath));
        }

        public bool IsInstalled =>
            Directory.Exists(RootDirectory) &&
            File.Exists(IsoKataDwgPath) &&
            File.Exists(KataWorkbookPath);

        public void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(RootDirectory);
            Directory.CreateDirectory(DangThepDirectory);
            Directory.CreateDirectory(CotDirectory);
            Directory.CreateDirectory(IconDirectory);
            Directory.CreateDirectory(RibbonIconDirectory);
            Directory.CreateDirectory(ImgDirectory);
            Directory.CreateDirectory(TempDirectory);
        }
    }
}
