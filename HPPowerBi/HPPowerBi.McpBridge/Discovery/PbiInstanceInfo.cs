using System;

namespace HPPowerBi.McpBridge.Discovery;

/// <summary>
///     Information about a detected Power BI Desktop instance and its local Analysis Services engine.
/// </summary>
public sealed record PbiInstanceInfo(
    int ProcessId,
    string WindowTitle,
    string ReportName,
    int Port,
    string? WorkspacePath = null,
    string? DatabaseName = null,
    DateTime? DiscoveredAt = null)
{
    /// <summary>Human-readable display string for UI dropdowns.</summary>
    public string DisplayName => $"[{Port}] {ReportName} (PID {ProcessId})";

    /// <summary>
    ///     Extracts clean report name from Power BI Desktop window title.
    ///     e.g., "FinancialReport2026 - Power BI Desktop" -> "FinancialReport2026".
    /// </summary>
    public static string ExtractReportName(string? windowTitle)
    {
        if (string.IsNullOrWhiteSpace(windowTitle))
            return "Untitled";

        const string suffix = " - Power BI Desktop";
        var trimmed = windowTitle.Trim();
        if (trimmed.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            return trimmed.Substring(0, trimmed.Length - suffix.Length).Trim();

        return trimmed;
    }
}
