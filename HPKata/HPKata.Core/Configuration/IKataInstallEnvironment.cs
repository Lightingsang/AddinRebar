using System;

namespace HPKata.Core.Configuration
{
    /// <summary>
    /// Provides canonical paths and directory management for all HPKata library assets.
    /// Centralized installation root defaults to @"C:\HPKata".
    /// </summary>
    public interface IKataInstallEnvironment
    {
        /// <summary>
        /// Gets the root installation directory (e.g. @"C:\HPKata").
        /// </summary>
        string RootDirectory { get; }

        /// <summary>
        /// Gets the canonical path to the primary detailing workbook (Kata.xlsm).
        /// </summary>
        string KataWorkbookPath { get; }

        /// <summary>
        /// Gets the canonical path to the English detailing workbook (Kata ENG.xlsm).
        /// </summary>
        string KataEngWorkbookPath { get; }

        /// <summary>
        /// Gets the canonical path to the QHPlus takeoff/schedule template (Iso_QHPlus.xls).
        /// </summary>
        string IsoQhPlusPath { get; }

        /// <summary>
        /// Gets the canonical path to the master CAD template drawing (Iso_kata.dwg).
        /// </summary>
        string IsoKataDwgPath { get; }

        /// <summary>
        /// Gets the canonical path to the ETABS sample/template model (ISO_KATA.EDB).
        /// </summary>
        string IsoKataEdbPath { get; }

        /// <summary>
        /// Gets the canonical path to the application icon (kata.ico).
        /// </summary>
        string KataIconPath { get; }

        /// <summary>
        /// Gets the canonical path to the 86-parameter configuration and LISP alias file (Goi lenh.lsp).
        /// </summary>
        string GoiLenhLspPath { get; }

        /// <summary>
        /// Gets the canonical path to the HPKata-specific loader and command alias script (GoiLenh_HPKata.lsp).
        /// </summary>
        string GoiLenhHpKataLspPath { get; }

        /// <summary>
        /// Gets the canonical path to the compiled AutoCAD ribbon/menu file (menukata.cuix).
        /// </summary>
        string MenuKataCuixPath { get; }

        /// <summary>
        /// Gets the canonical path to the XML-based AutoCAD customization file (menukata.cui).
        /// </summary>
        string MenuKataCuiPath { get; }

        /// <summary>
        /// Gets the canonical path to the AutoCAD menu resource file (menukata.mnr).
        /// </summary>
        string MenuKataMnrPath { get; }

        /// <summary>
        /// Gets the canonical path to the light-theme AutoCAD menu resource file (menukata_light.mnr).
        /// </summary>
        string MenuKataLightMnrPath { get; }

        /// <summary>
        /// Gets the directory path for standard rebar shape diagrams (dangthep\).
        /// </summary>
        string DangThepDirectory { get; }

        /// <summary>
        /// Gets the directory path for column splice diagrams (Cot\).
        /// </summary>
        string CotDirectory { get; }

        /// <summary>
        /// Gets the directory path for UI and CAD icons (Icon\).
        /// </summary>
        string IconDirectory { get; }

        /// <summary>
        /// Gets the directory path for Ribbon button icons (Icon\Ribbon\).
        /// </summary>
        string RibbonIconDirectory { get; }

        /// <summary>
        /// Gets the directory path for visual guide diagrams (IMG\).
        /// </summary>
        string ImgDirectory { get; }

        /// <summary>
        /// Gets the directory path for temporary files and calculation caches (Temp\).
        /// </summary>
        string TempDirectory { get; }

        /// <summary>
        /// Resolves a relative asset path into a fully qualified canonical path within RootDirectory.
        /// If relativePath is already rooted, returns its normalized full path.
        /// </summary>
        /// <param name="relativePath">The relative path to resolve.</param>
        /// <returns>The normalized full path.</returns>
        string GetCanonicalPath(string relativePath);

        /// <summary>
        /// Gets a value indicating whether the environment is installed at RootDirectory
        /// with all critical core assets present.
        /// </summary>
        bool IsInstalled { get; }

        /// <summary>
        /// Ensures all standard directories under RootDirectory exist on disk, creating them if necessary.
        /// </summary>
        void EnsureDirectoriesExist();
    }
}
