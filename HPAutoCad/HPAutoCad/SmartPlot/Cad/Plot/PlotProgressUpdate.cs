namespace HPAutoCad.SmartPlot.Cad.Plot;

/// <summary>
/// Status and progress update reported during an active batch plotting session.
/// </summary>
public sealed record PlotProgressUpdate
{
    public int CurrentIndex { get; init; }
    public int TotalCount { get; init; }
    public string SheetName { get; init; } = string.Empty;
    public string? OutputPath { get; init; }
    public string StatusMessage { get; init; } = string.Empty;
    public bool IsCompleted { get; init; }

    public double Percent => TotalCount > 0
        ? Math.Clamp((double)CurrentIndex / TotalCount * 100.0, 0.0, 100.0)
        : 0.0;
}
