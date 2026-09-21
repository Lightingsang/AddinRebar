namespace HPAutoCad.Core.SmartPlot.Services;

using HPAutoCad.Core.SmartPlot.Models;

/// <summary>
/// Service for sorting detected plot frames in spatial reading order (Top to Bottom, Left to Right).
/// </summary>
public interface IPlotOrderService
{
    /// <summary>
    /// Sorts plot frames in natural reading order: Top to Bottom, Left to Right,
    /// clustering items into row bands based on a vertical overlap tolerance ratio.
    /// </summary>
    /// <param name="items">The collection of detected plot items.</param>
    /// <param name="overlapRatioThreshold">Vertical overlap fraction (0.0 to 1.0) required to consider two frames in the same row. Default is 0.5.</param>
    /// <returns>A new ordered list of items with sequential 1-based Order values.</returns>
    IReadOnlyList<PlotItem> OrderFrames(IEnumerable<PlotItem> items, double overlapRatioThreshold = 0.5);

    /// <summary>
    /// Alias for <see cref="OrderFrames"/> with toleranceRatio parameter name.
    /// </summary>
    IReadOnlyList<PlotItem> Sort(IEnumerable<PlotItem> items, double toleranceRatio = 0.5);
}
