using System;
using System.IO;

namespace HPKata.Core.Assets
{
    /// <summary>
    /// Contract for typed resolution and stream/file access to HPKata visual assets
    /// (dangthep\*.wmf, Cot\*.wmf, Icon\Ribbon\*.png, and IMG\*.png).
    /// </summary>
    public interface IKataVisualAssetService
    {
        /// <summary>
        /// Resolves the file path for a standard rebar shape WMF diagram (e.g. dangthep\00.wmf).
        /// </summary>
        string? GetShapeDiagramWmfPath(string shapeCode);

        /// <summary>
        /// Resolves the file path for a rebar shape PNG diagram (e.g. IMG\Kata-00.png or dangthep\00.png).
        /// </summary>
        string? GetShapeDiagramPngPath(string shapeCode);

        /// <summary>
        /// Resolves the file path for a column splice diagram WMF (e.g. Cot\Crank Lap Bar.wmf).
        /// </summary>
        string? GetColumnSpliceDiagramPath(string spliceDiagramName);

        /// <summary>
        /// Resolves the file path for a Ribbon icon PNG (e.g. Icon\Ribbon\ve_dam.png).
        /// </summary>
        string? GetRibbonIconPath(string iconFileName);

        /// <summary>
        /// Resolves the file path for a guide diagram PNG (e.g. IMG\*.png).
        /// </summary>
        string? GetGuideDiagramPath(string diagramFileName);

        /// <summary>
        /// Resolves an asset path relative to the environment root directory.
        /// </summary>
        string ResolveAssetPath(string relativePath);

        /// <summary>
        /// Determines whether the specified asset file exists on disk.
        /// </summary>
        bool AssetExists(string relativeOrAbsolutePath);

        /// <summary>
        /// Opens a read-only stream for the asset file, or null if the file does not exist.
        /// </summary>
        Stream? OpenAssetStream(string relativeOrAbsolutePath);

        /// <summary>
        /// Reads all bytes for the asset file, or null if the file does not exist.
        /// </summary>
        byte[]? GetAssetBytes(string relativeOrAbsolutePath);
    }
}
