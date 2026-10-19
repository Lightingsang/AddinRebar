using System;
using System.IO;

namespace HPDcePro.Core.Common
{
    /// <summary>
    /// Canonical asset resolution service for HPDcePro.
    /// Resolves CAD templates, Excel calculation workbooks, machine learning models,
    /// language dictionaries, system configuration files, and icon resources
    /// relative to the HPDcePro installation directory with zero legacy hardcoded paths.
    /// </summary>
    public static class AssetPathResolver
    {
        private static string? _customAssetsRoot;
        private static string? _cachedAssetsRoot;

        /// <summary>
        /// Gets or sets an explicit root directory for assets (used for testing or custom deployments).
        /// </summary>
        public static string? CustomAssetsRoot
        {
            get => _customAssetsRoot;
            set
            {
                _customAssetsRoot = value;
                _cachedAssetsRoot = null;
            }
        }

        /// <summary>
        /// Resolves the canonical root directory of HPDcePro assets.
        /// Priority:
        /// 1. CustomAssetsRoot (if explicitly set)
        /// 2. HPDCEPRO_ASSETS_PATH environment variable
        /// 3. AppDomain BaseDirectory / "assets"
        /// 4. Parent directory traversal searching for "HPDcePro/assets" or "assets/MayHoc/mauhoc.db"
        /// </summary>
        public static string GetAssetsRoot()
        {
            if (!string.IsNullOrEmpty(_customAssetsRoot) && Directory.Exists(_customAssetsRoot))
            {
                return _customAssetsRoot!;
            }

            if (_cachedAssetsRoot != null && Directory.Exists(_cachedAssetsRoot))
            {
                return _cachedAssetsRoot;
            }

            string? envPath = Environment.GetEnvironmentVariable("HPDCEPRO_ASSETS_PATH");
            if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
            {
                _cachedAssetsRoot = envPath;
                return envPath;
            }

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string directAssets = Path.Combine(baseDir, "assets");
            if (Directory.Exists(directAssets) && File.Exists(Path.Combine(directAssets, "MayHoc", "mauhoc.db")))
            {
                _cachedAssetsRoot = directAssets;
                return directAssets;
            }

            string? dir = baseDir;
            for (int i = 0; i < 7 && dir != null; i++)
            {
                string candidate = Path.Combine(dir, "assets");
                if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "MayHoc", "mauhoc.db")))
                {
                    _cachedAssetsRoot = candidate;
                    return candidate;
                }

                string hpDceProCandidate = Path.Combine(dir, "HPDcePro", "assets");
                if (Directory.Exists(hpDceProCandidate) && File.Exists(Path.Combine(hpDceProCandidate, "MayHoc", "mauhoc.db")))
                {
                    _cachedAssetsRoot = hpDceProCandidate;
                    return hpDceProCandidate;
                }

                DirectoryInfo? parent = Directory.GetParent(dir);
                dir = parent?.FullName;
            }

            throw new DirectoryNotFoundException(
                "Unable to locate HPDcePro assets directory. Ensure 'HPDcePro/assets' exists or set 'HPDCEPRO_ASSETS_PATH'.");
        }

        public static string ResolveCadTemplate(string filename)
        {
            string path = Path.Combine(GetAssetsRoot(), "Cad", filename);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"CAD template '{filename}' was not found at '{path}'.", path);
            }
            return path;
        }

        public static string ResolveAutoStrDwg(string filename)
        {
            string path = Path.Combine(GetAssetsRoot(), "AutoStr", "00.FileDungChung", filename);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"AutoStr DWG template '{filename}' was not found at '{path}'.", path);
            }
            return path;
        }

        public static string ResolveExcelWorkbook(string relativePath)
        {
            string path = Path.Combine(GetAssetsRoot(), "Excel", relativePath);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Excel workbook '{relativePath}' was not found at '{path}'.", path);
            }
            return path;
        }

        public static string ResolveMachineLearningDatabase()
        {
            string path = Path.Combine(GetAssetsRoot(), "MayHoc", "mauhoc.db");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Machine learning database 'mauhoc.db' was not found at '{path}'.", path);
            }
            return path;
        }

        public static string ResolveMachineLearningModelZip()
        {
            string path = Path.Combine(GetAssetsRoot(), "MayHoc", "model.zip");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Machine learning model archive 'model.zip' was not found at '{path}'.", path);
            }
            return path;
        }

        public static string ResolveSystemConfig(string filename)
        {
            string path = Path.Combine(GetAssetsRoot(), "System", filename);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"System config file '{filename}' was not found at '{path}'.", path);
            }
            return path;
        }

        public static string ResolveSystemCache(string filename)
        {
            string path = Path.Combine(GetAssetsRoot(), "System", "Cache", filename);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"System cache file '{filename}' was not found at '{path}'.", path);
            }
            return path;
        }

        public static string ResolveLanguageDict(string relativePath)
        {
            string path = Path.Combine(GetAssetsRoot(), "Language", relativePath);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Language dictionary '{relativePath}' was not found at '{path}'.", path);
            }
            return path;
        }

        public static string ResolveIcon(string relativePath)
        {
            string path = Path.Combine(GetAssetsRoot(), "Icon", relativePath);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Icon resource '{relativePath}' was not found at '{path}'.", path);
            }
            return path;
        }

        public static string ResolveMenu(string filename)
        {
            string path = Path.Combine(GetAssetsRoot(), "Cuix", filename);
            if (!File.Exists(path))
            {
                // Fallback to assets root if placed directly in assets
                string rootPath = Path.Combine(GetAssetsRoot(), filename);
                if (File.Exists(rootPath))
                {
                    return rootPath;
                }
                throw new FileNotFoundException($"CUIX menu file '{filename}' was not found at '{path}'.", path);
            }
            return path;
        }

        public static string ResolveAutoStrProject(string filename)
        {
            string path = Path.Combine(GetAssetsRoot(), "AutoStr", filename);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"AutoStr project template '{filename}' was not found at '{path}'.", path);
            }
            return path;
        }
    }
}
