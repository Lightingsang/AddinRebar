using System;
using Autodesk.AutoCAD.DatabaseServices;

namespace HPKata.AutoCad.Interop
{
    /// <summary>
    /// Result record from a DWG template import operation.
    /// </summary>
    public sealed class KataTemplateImportResult
    {
        public bool Success { get; set; }
        public bool UsedFallback { get; set; }
        public int ImportedBlocksCount { get; set; }
        public int ImportedLayersCount { get; set; }
        public int ImportedTextStylesCount { get; set; }
        public int ImportedDimStylesCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Contract for importing standard blocks, layers, text styles, and dimstyles from Iso_kata.dwg.
    /// </summary>
    public interface IKataDwgTemplateImporter
    {
        /// <summary>
        /// Imports template objects from template DWG (defaulting to Iso_kata.dwg) into target database.
        /// </summary>
        KataTemplateImportResult ImportTemplateObjects(Database targetDb, string? templateDwgPath = null);

        /// <summary>
        /// Ensures all 12 canonical HPKata standard layers exist in the target database.
        /// </summary>
        void EnsureStandardLayers(Database targetDb);
    }
}
