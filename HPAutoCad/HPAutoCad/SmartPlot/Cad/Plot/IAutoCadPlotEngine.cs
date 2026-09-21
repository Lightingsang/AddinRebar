using Autodesk.AutoCAD.ApplicationServices;
using HPAutoCad.Core.SmartPlot.Models;

namespace HPAutoCad.SmartPlot.Cad.Plot;

/// <summary>
/// Service responsible for driving AutoCAD's PlotEngine pipeline, managing PlotSettings,
/// PlotProgressDialog, document locks, system variables, and PDF generation.
/// </summary>
public interface IAutoCadPlotEngine
{
    /// <summary>
    /// Executes a batch plot job for the specified items using AutoCAD's Publish/Plot Engine on the given document.
    /// </summary>
    /// <param name="doc">The AutoCAD document to plot from.</param>
    /// <param name="config">Plot configuration settings.</param>
    /// <param name="items">List of plot items to publish.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>PlotResult detailing plotted sheets, elapsed time, output files, or errors.</returns>
    Task<PlotResult> PlotAsync(
        Document doc,
        PlotConfiguration config,
        IReadOnlyList<PlotItem> items,
        IProgress<PlotProgressUpdate>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Executes a batch plot job on the active MdiActiveDocument.
    /// </summary>
    Task<PlotResult> PlotAsync(
        PlotConfiguration config,
        IReadOnlyList<PlotItem> items,
        IProgress<PlotProgressUpdate>? progress = null,
        CancellationToken ct = default);
}
