using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using HPAutoCad.Core.SmartPlot.Models;
using HPAutoCad.Core.SmartPlot.Services;

namespace HPAutoCad.SmartPlot.Cad.Providers;

/// <summary>
/// Scans AutoCAD drawing for closed Polyline, Polyline2d, and Polyline3d entities
/// residing on a designated layer, extracting their 2D bounding boxes as plot frames.
/// </summary>
public sealed class LayerFrameProvider : IFrameProvider
{
    private readonly IPlotOrderService _orderService;

    public LayerFrameProvider(IPlotOrderService? orderService = null)
    {
        _orderService = orderService ?? new PlotOrderService();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PlotItem>> ScanFramesAsync(
        Document doc,
        FrameSourceType sourceType,
        FrameScanOptions options,
        CancellationToken ct = default)
    {
        if (sourceType != FrameSourceType.Layer)
        {
            return Task.FromResult<IReadOnlyList<PlotItem>>([]);
        }

        return ScanFramesAsync(doc, options, ct);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PlotItem>> ScanFramesAsync(
        Document doc,
        FrameScanOptions options,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(options);

        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(options.LayerName))
        {
            return Task.FromResult<IReadOnlyList<PlotItem>>([]);
        }

        using var docLock = doc.LockDocument();
        var db = doc.Database;
        using var tr = db.TransactionManager.StartTransaction();

        var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead);
        string layoutName = "Model";
        if (space.IsLayout)
        {
            var layout = (Layout)tr.GetObject(space.LayoutId, OpenMode.ForRead);
            layoutName = layout.LayoutName;
        }

        var items = new List<PlotItem>();
        var targetLayer = options.LayerName.Trim();

        foreach (ObjectId entId in space)
        {
            ct.ThrowIfCancellationRequested();

            if (tr.GetObject(entId, OpenMode.ForRead) is not Entity ent)
            {
                continue;
            }

            if (!string.Equals(ent.Layer, targetLayer, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            bool isClosed = ent switch
            {
                Polyline pl => pl.Closed,
                Polyline2d pl2d => pl2d.Closed,
                Polyline3d pl3d => pl3d.Closed,
                _ => false
            };

            if (!isClosed)
            {
                continue;
            }

            PlotBounds bounds;
            try
            {
                var ext = ent.GeometricExtents;
                bounds = new PlotBounds(ext.MinPoint.X, ext.MinPoint.Y, ext.MaxPoint.X, ext.MaxPoint.Y);
            }
            catch
            {
                continue;
            }

            if (!bounds.IsValid || bounds.Width <= 1e-3 || bounds.Height <= 1e-3)
            {
                continue;
            }

            items.Add(new PlotItem
            {
                Id = Guid.NewGuid().ToString("N"),
                Bounds = bounds,
                LayoutName = layoutName,
                DisplayName = $"{targetLayer} Frame",
                SourceHandle = ent.Handle.ToString()
            });
        }

        tr.Commit();

        var sorted = _orderService.Sort(items, options.ToleranceBandYRatio);
        var finalItems = sorted.Select(i => i with
        {
            DisplayName = $"{targetLayer} - {i.Order:D2}",
            SheetNumber = i.Order.ToString()
        }).ToList();

        return Task.FromResult<IReadOnlyList<PlotItem>>(finalItems);
    }
}
