namespace HPAutoCad.Core.SmartPlot.Services;

using System.IO;
using System.Text.RegularExpressions;
using HPAutoCad.Core.SmartPlot.Models;

/// <summary>
/// Default implementation of <see cref="IFileNameService"/> with token substitution and Windows filename sanitization.
/// </summary>
public sealed class FileNameService : IFileNameService
{
    private static readonly HashSet<char> InvalidChars = Path.GetInvalidFileNameChars().ToHashSet();

    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    /// <inheritdoc />
    public string Format(string template, PlotItem item, string? prefix = null, string? dwgName = null)
    {
        return FormatFileName(template, item, prefix, dwgName);
    }

    /// <inheritdoc />
    public string FormatFileName(string template, PlotItem item, string? prefix = null, string? dwgName = null)
    {
        var pattern = string.IsNullOrWhiteSpace(template)
            ? "{Prefix}_{Layout}_{SheetNo}_{Title}"
            : template.Trim();

        var prefixVal = prefix ?? string.Empty;
        var layoutVal = item.LayoutName ?? string.Empty;
        var sheetNoVal = item.SheetNumber ?? item.AttributeValue ?? item.Order.ToString();
        var titleVal = item.SheetTitle ?? item.DisplayName ?? string.Empty;
        var orderVal = item.Order.ToString("D2");

        string dwgVal = string.Empty;
        if (!string.IsNullOrWhiteSpace(dwgName))
        {
            try
            {
                dwgVal = Path.GetFileNameWithoutExtension(dwgName) ?? dwgName;
            }
            catch
            {
                dwgVal = dwgName;
            }
        }

        var dateVal = DateTime.Now.ToString("yyyyMMdd");

        var raw = pattern
            .Replace("{Prefix}", prefixVal, StringComparison.OrdinalIgnoreCase)
            .Replace("{Layout}", layoutVal, StringComparison.OrdinalIgnoreCase)
            .Replace("{SheetNo}", sheetNoVal, StringComparison.OrdinalIgnoreCase)
            .Replace("{Title}", titleVal, StringComparison.OrdinalIgnoreCase)
            .Replace("{Order}", orderVal, StringComparison.OrdinalIgnoreCase)
            .Replace("{DwgName}", dwgVal, StringComparison.OrdinalIgnoreCase)
            .Replace("{Date}", dateVal, StringComparison.OrdinalIgnoreCase);

        var sanitized = SanitizeFileName(raw);

        // Collapse multiple consecutive underscores
        sanitized = Regex.Replace(sanitized, @"_+", "_").Trim('_', ' ', '.');

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = $"Plot_Sheet_{item.Order}";
        }
        else
        {
            var stem = Path.GetFileNameWithoutExtension(sanitized);
            var dotIndex = sanitized.IndexOf('.');
            var primaryStem = dotIndex > 0 ? sanitized[..dotIndex] : stem;

            if (ReservedNames.Contains(sanitized)
                || (!string.IsNullOrEmpty(stem) && ReservedNames.Contains(stem))
                || (!string.IsNullOrEmpty(primaryStem) && ReservedNames.Contains(primaryStem)))
            {
                sanitized = $"_{sanitized}";
            }
        }

        return sanitized;
    }

    /// <inheritdoc />
    public string SanitizeFileName(string rawName, char replacementChar = '_')
    {
        if (string.IsNullOrWhiteSpace(rawName)) return "Plot_Sheet";

        var sb = new System.Text.StringBuilder(rawName.Length);
        foreach (var ch in rawName)
        {
            if (InvalidChars.Contains(ch) || char.IsControl(ch))
            {
                sb.Append(replacementChar);
            }
            else
            {
                sb.Append(ch);
            }
        }

        var result = sb.ToString().Trim(' ', '.');
        if (string.IsNullOrWhiteSpace(result)) return "Plot_Sheet";

        var stem = Path.GetFileNameWithoutExtension(result);
        var dotIndex = result.IndexOf('.');
        var primaryStem = dotIndex > 0 ? result[..dotIndex] : stem;

        if (ReservedNames.Contains(result)
            || (!string.IsNullOrEmpty(stem) && ReservedNames.Contains(stem))
            || (!string.IsNullOrEmpty(primaryStem) && ReservedNames.Contains(primaryStem)))
        {
            result = $"{replacementChar}{result}";
        }

        return result;
    }

    /// <inheritdoc />
    public string BuildFullFilePath(string outputFolder, string fileNameWithoutExt, string extension = ".pdf")
    {
        var ext = extension.StartsWith('.') ? extension : $".{extension}";
        var cleanBase = SanitizeFileName(fileNameWithoutExt);
        var fullName = cleanBase.EndsWith(ext, StringComparison.OrdinalIgnoreCase)
            ? cleanBase
            : $"{cleanBase}{ext}";

        return string.IsNullOrWhiteSpace(outputFolder)
            ? fullName
            : Path.Combine(outputFolder, fullName);
    }
}
