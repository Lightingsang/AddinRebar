using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using HPAutoCad.Core.SmartPlot.Models;
using HPAutoCad.Core.SmartPlot.Services;

namespace HPAutoCad.SmartPlot.Cad.Providers;

/// <summary>
/// Scans PaperSpace layouts in an AutoCAD drawing, sorted by their TabOrder,
/// and filtered by layout range specifications (e.g. "All", "1-5", "1,3,5").
/// </summary>
public sealed class LayoutFrameProvider : IFrameProvider
{
    /// <inheritdoc />
    public Task<IReadOnlyList<PlotItem>> ScanFramesAsync(
        Document doc,
        FrameSourceType sourceType,
        FrameScanOptions options,
        CancellationToken ct = default)
    {
        if (sourceType != FrameSourceType.Layout)
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

        using var docLock = doc.LockDocument();
        var db = doc.Database;
        using var tr = db.TransactionManager.StartTransaction();

        var layoutDict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
        var paperLayouts = new List<Layout>();

        foreach (DBDictionaryEntry entry in layoutDict)
        {
            if (tr.GetObject(entry.Value, OpenMode.ForRead) is Layout layout && !layout.ModelType)
            {
                paperLayouts.Add(layout);
            }
        }

        paperLayouts.Sort((a, b) => a.TabOrder.CompareTo(b.TabOrder));

        int totalLayouts = paperLayouts.Count;
        if (totalLayouts == 0)
        {
            tr.Commit();
            return Task.FromResult<IReadOnlyList<PlotItem>>([]);
        }

        var selectedIndices = LayoutRangeParser.Parse(options.LayoutRange, totalLayouts);
        var items = new List<PlotItem>(selectedIndices.Count);

        for (int order = 1; order <= selectedIndices.Count; order++)
        {
            ct.ThrowIfCancellationRequested();

            int index1Based = selectedIndices[order - 1];
            if (index1Based < 1 || index1Based > totalLayouts)
            {
                continue;
            }

            var layout = paperLayouts[index1Based - 1];

            PlotBounds bounds;
            try
            {
                var min = layout.Extents.MinPoint;
                var max = layout.Extents.MaxPoint;
                if (max.X > min.X && max.Y > min.Y)
                {
                    bounds = new PlotBounds(min.X, min.Y, max.X, max.Y);
                }
                else
                {
                    bounds = new PlotBounds(0, 0, layout.PlotPaperSize.X, layout.PlotPaperSize.Y);
                    if (!bounds.IsValid || bounds.Width <= 0 || bounds.Height <= 0)
                    {
                        bounds = new PlotBounds(0, 0, 841, 594);
                    }
                }
            }
            catch
            {
                bounds = new PlotBounds(0, 0, 841, 594);
            }

            items.Add(new PlotItem
            {
                Id = Guid.NewGuid().ToString("N"),
                Bounds = bounds,
                LayoutName = layout.LayoutName,
                DisplayName = layout.LayoutName,
                SheetNumber = index1Based.ToString(),
                SheetTitle = layout.LayoutName,
                Order = order,
                SourceHandle = layout.Handle.ToString()
            });
        }

        tr.Commit();
        return Task.FromResult<IReadOnlyList<PlotItem>>(items);
    }
}
