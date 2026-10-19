using System;
using System.IO;
using System.Threading;

namespace HPDcePro.Core.Common
{
    /// <summary>
    /// Canonical asset resolution service for HPDcePro.
    /// Provides zero-dependency asset resolution across Unit Tests, Desktop, AutoCAD, Revit, and Excel.
    /// </summary>
    public static class AssetPathResolver
    {
        public const string AssetsDirectoryName = "assets";
        public const string EnvAssetsDir = "HPDCEPRO_ASSETS_DIR";
        public const string EnvAssetsPath = "HPDCEPRO_ASSETS_PATH";

        private static readonly AsyncLocal<string?> s_ambientRoot = new AsyncLocal<string?>();
        private static string? s_customAssetsRoot;
        private static string? s_cachedDiscoveredRoot;
        private static readonly object s_lock = new object();

        public static string? CustomAssetsRoot
        {
            get => s_customAssetsRoot;
            set
            {
                s_customAssetsRoot = string.IsNullOrWhiteSpace(value) ? null : Path.GetFullPath(value);
                lock (s_lock) { s_cachedDiscoveredRoot = null; }
            }
        }

        public static void SetCustomAssetsRoot(string? rootPath) => CustomAssetsRoot = rootPath;
        public static void SetOverrideAssetsRoot(string? rootPath) => CustomAssetsRoot = rootPath;

        public static void ResetOverride()
        {
            s_customAssetsRoot = null;
            s_ambientRoot.Value = null;
            lock (s_lock) { s_cachedDiscoveredRoot = null; }
        }

        public static IDisposable UseAssetsRoot(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("Asset root cannot be null or whitespace.", nameof(rootPath));
            }

            string? previous = s_ambientRoot.Value;
            s_ambientRoot.Value = Path.GetFullPath(rootPath);
            return new ScopeToken(() => s_ambientRoot.Value = previous);
        }

        public static string GetAssetsRoot()
        {
            if (!string.IsNullOrWhiteSpace(s_ambientRoot.Value) && Directory.Exists(s_ambientRoot.Value))
                return s_ambientRoot.Value!;

            if (!string.IsNullOrWhiteSpace(s_customAssetsRoot) && Directory.Exists(s_customAssetsRoot))
                return s_customAssetsRoot!;

            string? env = Environment.GetEnvironmentVariable(EnvAssetsDir) ?? Environment.GetEnvironmentVariable(EnvAssetsPath);
            if (!string.IsNullOrWhiteSpace(env) && Directory.Exists(env))
                return Path.GetFullPath(env);

            if (s_cachedDiscoveredRoot != null && Directory.Exists(s_cachedDiscoveredRoot))
                return s_cachedDiscoveredRoot;

            lock (s_lock)
            {
                if (s_cachedDiscoveredRoot != null && Directory.Exists(s_cachedDiscoveredRoot))
                    return s_cachedDiscoveredRoot;

                s_cachedDiscoveredRoot = DiscoverAssetsRoot();
                return s_cachedDiscoveredRoot;
            }
        }

        public static string ResolveCadTemplate(string filename)
        {
            string root = GetAssetsRoot();
            string cadPath = Path.Combine(root, "Cad", filename);
            if (File.Exists(cadPath)) return cadPath;

            string autoStrPath = Path.Combine(root, "AutoStr", "00.FileDungChung", filename);
            if (File.Exists(autoStrPath)) return autoStrPath;

            throw new FileNotFoundException($"CAD template '{filename}' not found at '{cadPath}'.", cadPath);
        }

        public static string ResolveAutoStrDwg(string filename)
        {
            string path = Path.Combine(GetAssetsRoot(), "AutoStr", "00.FileDungChung", filename);
            if (!File.Exists(path))
                throw new FileNotFoundException($"AutoStr DWG '{filename}' not found at '{path}'.", path);
            return path;
        }

        public static string ResolveExcelWorkbook(string relativePath)
        {
            string root = GetAssetsRoot();
            string clean = relativePath.TrimStart('\\', '/');

            string directPath = Path.Combine(root, "Excel", clean);
            if (File.Exists(directPath)) return directPath;

            string v2020Path = Path.Combine(root, "Excel", "File V.2020", clean);
            if (File.Exists(v2020Path)) return v2020Path;

            string steelPath = Path.Combine(root, "Excel", "DceSteel", clean);
            if (File.Exists(steelPath)) return steelPath;

            throw new FileNotFoundException($"Excel workbook '{relativePath}' not found at '{directPath}'.", directPath);
        }

        public static string ResolveMachineLearningDatabase() => ResolveFile("MayHoc", "mauhoc.db");
        public static string ResolveMachineLearningModelZip() => ResolveFile("MayHoc", "model.zip");
        public static string ResolveSystemConfig(string filename) => ResolveFile("System", filename);
        public static string ResolveSystemCache(string filename) => ResolveFile(Path.Combine("System", "Cache"), filename);
        public static string ResolveLanguageDict(string relativePath) => ResolveFile("Language", relativePath.TrimStart('\\', '/'));
        public static string ResolveIcon(string relativePath) => ResolveFile("Icon", relativePath.TrimStart('\\', '/'));

        public static string ResolveMenu(string filename)
        {
            string root = GetAssetsRoot();
            string cuixPath = Path.Combine(root, "Cuix", filename);
            if (File.Exists(cuixPath)) return cuixPath;

            string cadMenuPath = Path.Combine(root, "Cad", "Menus", filename);
            if (File.Exists(cadMenuPath)) return cadMenuPath;

            string rootPath = Path.Combine(root, filename);
            if (File.Exists(rootPath)) return rootPath;

            throw new FileNotFoundException($"CUIX menu file '{filename}' not found at '{cuixPath}'.", cuixPath);
        }

        public static string ResolveCuixMenu(string filename) => ResolveMenu(filename);
        public static string ResolveAutoStrProject(string filename = "DuAnMau.autostr") => ResolveFile("AutoStr", filename);
        public static string ResolveAutoStrSample() => ResolveAutoStrProject("DuAnMau.autostr");
        public static string ResolveAsset(string relativePath) => Path.Combine(GetAssetsRoot(), relativePath.TrimStart('\\', '/'));

        public static bool AssetExists(string relativePath)
        {
            string fullPath = ResolveAsset(relativePath);
            return File.Exists(fullPath) || Directory.Exists(fullPath);
        }

        private static string ResolveFile(string subDir, string filename)
        {
            string path = Path.Combine(GetAssetsRoot(), subDir, filename);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Asset '{filename}' not found at '{path}'.", path);
            return path;
        }

        private static string DiscoverAssetsRoot()
        {
            try
            {
                string asmLoc = typeof(AssetPathResolver).Assembly.Location;
                if (!string.IsNullOrWhiteSpace(asmLoc))
                {
                    string? found = ProbeAncestors(Path.GetDirectoryName(asmLoc));
                    if (found != null) return found;
                }
            }
            catch (Exception)
            {
                // Reason: Assembly.Location throws in single-file bundles or dynamic ALC
            }

            string baseDir = AppContext.BaseDirectory;
            string? foundFromBase = ProbeAncestors(baseDir);
            if (foundFromBase != null) return foundFromBase;

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string bundleAssets = Path.Combine(appData, "Autodesk", "ApplicationPlugins", "HPDcePro.bundle", "assets");
            if (IsAssetsDirectory(bundleAssets)) return bundleAssets;

            string revitAssets = Path.Combine(appData, "Autodesk", "Revit", "Addins", "2026", "HPDcePro", "assets");
            if (IsAssetsDirectory(revitAssets)) return revitAssets;

            string directFallback = Path.Combine(baseDir, AssetsDirectoryName);
            if (IsAssetsDirectory(directFallback)) return directFallback;

            throw new DirectoryNotFoundException(
                "Unable to locate HPDcePro assets directory. Ensure 'HPDcePro/assets' exists or set 'HPDCEPRO_ASSETS_DIR'.");
        }

        private static string? ProbeAncestors(string? startDir)
        {
            if (string.IsNullOrWhiteSpace(startDir)) return null;

            string? current = Path.GetFullPath(startDir!);
            for (int depth = 0; depth < 8 && current != null; depth++)
            {
                string candidate = Path.Combine(current, AssetsDirectoryName);
                if (IsAssetsDirectory(candidate)) return Path.GetFullPath(candidate);

                string candidateHPDcePro = Path.Combine(current, "HPDcePro", AssetsDirectoryName);
                if (IsAssetsDirectory(candidateHPDcePro)) return Path.GetFullPath(candidateHPDcePro);

                var parent = Directory.GetParent(current);
                current = parent?.FullName;
            }
            return null;
        }

        private static bool IsAssetsDirectory(string path)
        {
            if (!Directory.Exists(path)) return false;

            return File.Exists(Path.Combine(path, "MayHoc", "mauhoc.db"))
                || Directory.Exists(Path.Combine(path, "MayHoc"))
                || Directory.Exists(Path.Combine(path, "Cad"));
        }

        private sealed class ScopeToken : IDisposable
        {
            private Action? _disposeAction;
            public ScopeToken(Action disposeAction) => _disposeAction = disposeAction;
            public void Dispose() => Interlocked.Exchange(ref _disposeAction, null)?.Invoke();
        }
    }
}
