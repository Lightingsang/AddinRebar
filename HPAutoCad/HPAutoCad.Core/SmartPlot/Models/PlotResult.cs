namespace HPAutoCad.Core.SmartPlot.Models;

/// <summary>
/// Execution result of a plot session.
/// </summary>
public sealed record PlotResult
{
    public bool Success { get; init; }
    public int TotalSheets { get; init; }
    public int PlottedSheets { get; init; }
    public int FailedSheets { get; init; }
    public IReadOnlyList<string> OutputFilePaths { get; init; } = [];
    public string? MergedPdfPath { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public TimeSpan ElapsedTime { get; init; } = TimeSpan.Zero;

    public static PlotResult Succeeded(
        IReadOnlyList<string> files,
        string? merged = null,
        TimeSpan elapsed = default,
        int? totalSheets = null,
        int? plottedSheets = null) =>
        new()
        {
            Success = true,
            TotalSheets = totalSheets ?? files.Count,
            PlottedSheets = plottedSheets ?? files.Count,
            FailedSheets = Math.Max(0, (totalSheets ?? files.Count) - (plottedSheets ?? files.Count)),
            OutputFilePaths = files,
            MergedPdfPath = merged,
            ElapsedTime = elapsed
        };

    public static PlotResult Failed(string error, int total = 0, int plotted = 0) =>
        new()
        {
            Success = false,
            TotalSheets = total,
            PlottedSheets = plotted,
            FailedSheets = Math.Max(0, total - plotted),
            Errors = [error]
        };
}
