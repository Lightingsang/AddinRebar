using Autodesk.AutoCAD.ApplicationServices;
using HPAutoCad.Core.SmartPlot.Models;

namespace HPAutoCad.SmartPlot.Cad.Providers;

/// <summary>
/// Defines the contract for scanning and detecting plot frames in an AutoCAD document.
/// </summary>
public interface IFrameProvider
{
    /// <summary>
    /// Scans the document for plot frames based on the specified source type and options.
    /// </summary>
    /// <param name="doc">The active AutoCAD document.</param>
    /// <param name="sourceType">The frame source type (Block, Layer, or Layout).</param>
    /// <param name="options">Options and filtering criteria.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A list of detected plot frames ordered spatially.</returns>
    Task<IReadOnlyList<PlotItem>> ScanFramesAsync(
        Document doc,
        FrameSourceType sourceType,
        FrameScanOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Scans the document for plot frames using the provider's default source type.
    /// </summary>
    /// <param name="doc">The active AutoCAD document.</param>
    /// <param name="options">Options and filtering criteria.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A list of detected plot frames ordered spatially.</returns>
    Task<IReadOnlyList<PlotItem>> ScanFramesAsync(
        Document doc,
        FrameScanOptions options,
        CancellationToken ct = default);
}
