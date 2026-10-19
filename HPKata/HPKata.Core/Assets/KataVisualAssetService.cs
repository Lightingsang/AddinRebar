using System;
using System.IO;
using HPKata.Core.Configuration;

namespace HPKata.Core.Assets
{
    /// <summary>
    /// Service providing typed asset resolution and stream/file access for HPKata diagrams and icons.
    /// Accesses dangthep\*.wmf, Cot\*.wmf, Icon\Ribbon\*.png, and IMG\*.png relative to the active installation environment.
    /// </summary>
    public class KataVisualAssetService : IKataVisualAssetService
    {
        private readonly IKataInstallEnvironment _environment;

        public KataVisualAssetService(IKataInstallEnvironment? environment = null)
        {
            _environment = environment ?? KataInstallEnvironment.Current;
        }

        public string? GetShapeDiagramWmfPath(string shapeCode)
        {
            if (string.IsNullOrWhiteSpace(shapeCode)) return null;

            string normalizedCode = NormalizeCode(shapeCode);
            string fileName = normalizedCode.EndsWith(".wmf", StringComparison.OrdinalIgnoreCase)
                ? normalizedCode
                : $"{normalizedCode}.wmf";

            string path = Path.Combine(_environment.DangThepDirectory, fileName);
            return File.Exists(path) ? path : path;
        }

        public string? GetShapeDiagramPngPath(string shapeCode)
        {
            if (string.IsNullOrWhiteSpace(shapeCode)) return null;

            string normalizedCode = NormalizeCode(shapeCode);

            // Candidate 1: IMG\Kata-00.png
            string imgName = $"Kata-{normalizedCode}.png";
            string imgPath = Path.Combine(_environment.ImgDirectory, imgName);
            if (File.Exists(imgPath)) return imgPath;

            // Candidate 2: dangthep\00.png
            string dangThepName = $"{normalizedCode}.png";
            string dangThepPath = Path.Combine(_environment.DangThepDirectory, dangThepName);
            if (File.Exists(dangThepPath)) return dangThepPath;

            return imgPath;
        }

        public string? GetColumnSpliceDiagramPath(string spliceDiagramName)
        {
            if (string.IsNullOrWhiteSpace(spliceDiagramName)) return null;

            string fileName = spliceDiagramName.EndsWith(".wmf", StringComparison.OrdinalIgnoreCase)
                ? spliceDiagramName
                : $"{spliceDiagramName}.wmf";

            string path = Path.Combine(_environment.CotDirectory, fileName);
            return path;
        }

        public string? GetRibbonIconPath(string iconFileName)
        {
            if (string.IsNullOrWhiteSpace(iconFileName)) return null;

            string fileName = iconFileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                ? iconFileName
                : $"{iconFileName}.png";

            string ribbonPath = Path.Combine(_environment.RibbonIconDirectory, fileName);
            if (File.Exists(ribbonPath)) return ribbonPath;

            string iconPath = Path.Combine(_environment.IconDirectory, fileName);
            return File.Exists(iconPath) ? iconPath : ribbonPath;
        }

        public string? GetGuideDiagramPath(string diagramFileName)
        {
            if (string.IsNullOrWhiteSpace(diagramFileName)) return null;

            string fileName = diagramFileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                ? diagramFileName
                : $"{diagramFileName}.png";

            return Path.Combine(_environment.ImgDirectory, fileName);
        }

        public string ResolveAssetPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentException("Asset path cannot be null or whitespace.", nameof(relativePath));
            }

            return _environment.GetCanonicalPath(relativePath);
        }

        public bool AssetExists(string relativeOrAbsolutePath)
        {
            if (string.IsNullOrWhiteSpace(relativeOrAbsolutePath)) return false;

            string fullPath = Path.IsPathRooted(relativeOrAbsolutePath)
                ? relativeOrAbsolutePath
                : _environment.GetCanonicalPath(relativeOrAbsolutePath);

            return File.Exists(fullPath) || Directory.Exists(fullPath);
        }

        public Stream? OpenAssetStream(string relativeOrAbsolutePath)
        {
            if (string.IsNullOrWhiteSpace(relativeOrAbsolutePath)) return null;

            string fullPath = Path.IsPathRooted(relativeOrAbsolutePath)
                ? relativeOrAbsolutePath
                : _environment.GetCanonicalPath(relativeOrAbsolutePath);

            if (!File.Exists(fullPath)) return null;

            try
            {
                return File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch (Exception)
            {
                // Annotated catch per Rule Q-B2: file lock or security exception when opening asset stream
                return null;
            }
        }

        public byte[]? GetAssetBytes(string relativeOrAbsolutePath)
        {
            if (string.IsNullOrWhiteSpace(relativeOrAbsolutePath)) return null;

            string fullPath = Path.IsPathRooted(relativeOrAbsolutePath)
                ? relativeOrAbsolutePath
                : _environment.GetCanonicalPath(relativeOrAbsolutePath);

            if (!File.Exists(fullPath)) return null;

            try
            {
                return File.ReadAllBytes(fullPath);
            }
            catch (Exception)
            {
                // Annotated catch per Rule Q-B2: file I/O exception when reading asset bytes
                return null;
            }
        }

        private static string NormalizeCode(string code)
        {
            string clean = code.Trim();
            if (clean.StartsWith("dang", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean.Substring(4);
            }
            if (int.TryParse(clean, out int num) && num >= 0 && clean.Length < 2)
            {
                return num.ToString("D2");
            }
            return clean;
        }
    }
}
