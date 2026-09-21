using Autodesk.AutoCAD.ApplicationServices;
using HPAutoCad.Core.SmartPlot.Models;

namespace HPAutoCad.SmartPlot.Cad.Providers;

/// <summary>
/// Unified frame provider that delegates frame scanning to BlockFrameProvider,
/// LayerFrameProvider, or LayoutFrameProvider based on the requested FrameSourceType.
/// </summary>
public sealed class AutoCadFrameProvider : IFrameProvider
{
    private readonly BlockFrameProvider _blockProvider;
    private readonly LayerFrameProvider _layerProvider;
    private readonly LayoutFrameProvider _layoutProvider;

    public AutoCadFrameProvider(
        BlockFrameProvider? blockProvider = null,
        LayerFrameProvider? layerProvider = null,
        LayoutFrameProvider? layoutProvider = null)
    {
        _blockProvider = blockProvider ?? new BlockFrameProvider();
        _layerProvider = layerProvider ?? new LayerFrameProvider();
        _layoutProvider = layoutProvider ?? new LayoutFrameProvider();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PlotItem>> ScanFramesAsync(
        Document doc,
        FrameSourceType sourceType,
        FrameScanOptions options,
        CancellationToken ct = default)
    {
        return sourceType switch
        {
            FrameSourceType.Block => _blockProvider.ScanFramesAsync(doc, options, ct),
            FrameSourceType.Layer => _layerProvider.ScanFramesAsync(doc, options, ct),
            FrameSourceType.Layout => _layoutProvider.ScanFramesAsync(doc, options, ct),
            _ => Task.FromResult<IReadOnlyList<PlotItem>>([])
        };
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PlotItem>> ScanFramesAsync(
        Document doc,
        FrameScanOptions options,
        CancellationToken ct = default)
    {
        return _blockProvider.ScanFramesAsync(doc, options, ct);
    }
}
