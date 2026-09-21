namespace HPAutoCad.Core.SmartPlot.Services;

using HPAutoCad.Core.SmartPlot.Models;

/// <summary>
/// Service for generating sanitized file names and resolving template tokens.
/// </summary>
public interface IFileNameService
{
    /// <summary>
    /// Formats a file name by substituting template tokens ({Prefix}, {Layout}, {SheetNo}, {Title}, {Order}, {DwgName}, {Date})
    /// and sanitizing invalid characters.
    /// </summary>
    string FormatFileName(string template, PlotItem item, string? prefix = null, string? dwgName = null);

    /// <summary>
    /// Alias for <see cref="FormatFileName"/>.
    /// </summary>
    string Format(string template, PlotItem item, string? prefix = null, string? dwgName = null);

    /// <summary>
    /// Strips or replaces characters disallowed by Windows file systems.
    /// </summary>
    string SanitizeFileName(string rawName, char replacementChar = '_');

    /// <summary>
    /// Combines an output folder and file name, ensuring valid extension.
    /// </summary>
    string BuildFullFilePath(string outputFolder, string fileNameWithoutExt, string extension = ".pdf");
}
