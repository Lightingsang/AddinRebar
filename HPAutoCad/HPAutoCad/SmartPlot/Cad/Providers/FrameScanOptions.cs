using HPAutoCad.Core.SmartPlot.Models;

namespace HPAutoCad.SmartPlot.Cad.Providers;

/// <summary>
/// Options and filtering criteria used by frame providers to detect and extract plot frames.
/// </summary>
public sealed record FrameScanOptions
{
    /// <summary>
    /// Comma- or semicolon-delimited block names, or a single block name to scan for.
    /// </summary>
    public string BlockName { get; init; } = string.Empty;

    /// <summary>
    /// List of explicit block names to match. If empty, matches BlockName or all blocks.
    /// </summary>
    public IReadOnlyList<string> SelectedBlockNames { get; init; } = [];

    /// <summary>
    /// Layer name to scan for closed polyline bounding frames.
    /// </summary>
    public string LayerName { get; init; } = string.Empty;

    /// <summary>
    /// Layout range specification string (e.g. "All", "1-5", "1,3,5", "1-3,5,8-10").
    /// </summary>
    public string LayoutRange { get; init; } = "All";

    /// <summary>
    /// Attribute tag name representing the sheet number (e.g. "SOHIEU", "SHEET_NO").
    /// </summary>
    public string SheetNumberAttributeTag { get; init; } = "SOHIEU";

    /// <summary>
    /// Attribute tag name representing the sheet title (e.g. "TENTIEUDE", "TITLE").
    /// </summary>
    public string SheetTitleAttributeTag { get; init; } = "TENTIEUDE";

    /// <summary>
    /// Vertical overlap ratio threshold (0.05 to 0.95) for row clustering in spatial ordering.
    /// </summary>
    public double ToleranceBandYRatio { get; init; } = 0.5;

    /// <summary>
    /// Constructs scan options from a plot configuration object.
    /// </summary>
    public static FrameScanOptions FromConfiguration(PlotConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var blockNames = string.IsNullOrWhiteSpace(config.BlockName)
            ? []
            : config.BlockName.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new FrameScanOptions
        {
            BlockName = config.BlockName,
            SelectedBlockNames = blockNames,
            LayerName = config.LayerName,
            LayoutRange = config.LayoutRange,
            SheetNumberAttributeTag = config.SheetNumberAttribute,
            SheetTitleAttributeTag = config.SheetTitleAttribute,
            ToleranceBandYRatio = config.ToleranceBandYRatio
        };
    }
}
