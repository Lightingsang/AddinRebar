using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using HPAutoCad.Core.SmartPlot.Models;
using HPAutoCad.Core.SmartPlot.Services;

namespace HPAutoCad.SmartPlot.Cad.Providers;

/// <summary>
/// Scans AutoCAD BlockReference entities, handling Dynamic Blocks via DynamicBlockTableRecord
/// (resolving user-defined EffectiveName instead of anonymous *U... handles),
/// extracting sheet metadata from attributes, and computing 2D bounding boxes.
/// </summary>
public sealed class BlockFrameProvider : IFrameProvider
{
    private readonly IPlotOrderService _orderService;

    public BlockFrameProvider(IPlotOrderService? orderService = null)
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
        if (sourceType != FrameSourceType.Block)
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

        var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead);
        string layoutName = "Model";
        if (space.IsLayout)
        {
            var layout = (Layout)tr.GetObject(space.LayoutId, OpenMode.ForRead);
            layoutName = layout.LayoutName;
        }

        var filterSet = BuildFilterSet(options);
        var items = new List<PlotItem>();

        foreach (ObjectId entId in space)
        {
            ct.ThrowIfCancellationRequested();

            if (!entId.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(BlockReference))))
            {
                continue;
            }

            if (tr.GetObject(entId, OpenMode.ForRead) is not BlockReference blkRef)
            {
                continue;
            }

            // Resolve true EffectiveName for dynamic blocks
            string effectiveName;
            if (blkRef.IsDynamicBlock)
            {
                var btr = (BlockTableRecord)tr.GetObject(blkRef.DynamicBlockTableRecord, OpenMode.ForRead);
                effectiveName = btr.Name;
            }
            else
            {
                effectiveName = blkRef.Name;
            }

            // Skip non-matching blocks if a filter is active
            if (filterSet.Count > 0 && !filterSet.Contains(effectiveName))
            {
                continue;
            }

            PlotBounds bounds;
            try
            {
                var ext = blkRef.GeometricExtents;
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

            string? sheetNumber = null;
            string? sheetTitle = null;

            foreach (ObjectId attId in blkRef.AttributeCollection)
            {
                if (tr.GetObject(attId, OpenMode.ForRead) is AttributeReference attRef)
                {
                    var tag = attRef.Tag.Trim();
                    var val = attRef.TextString.Trim();

                    if (!string.IsNullOrEmpty(options.SheetNumberAttributeTag) &&
                        string.Equals(tag, options.SheetNumberAttributeTag, StringComparison.OrdinalIgnoreCase))
                    {
                        sheetNumber = val;
                    }
                    else if (!string.IsNullOrEmpty(options.SheetTitleAttributeTag) &&
                             string.Equals(tag, options.SheetTitleAttributeTag, StringComparison.OrdinalIgnoreCase))
                    {
                        sheetTitle = val;
                    }
                }
            }

            // Heuristic fallbacks if explicit attribute tags were not found or empty
            if (string.IsNullOrWhiteSpace(sheetNumber))
            {
                sheetNumber = TryFindAttribute(blkRef, tr, ["SOHIEU", "SO_BV", "SHEET_NO", "SHEET", "NO", "DWG_NO"]);
            }
            if (string.IsNullOrWhiteSpace(sheetTitle))
            {
                sheetTitle = TryFindAttribute(blkRef, tr, ["TENTIEUDE", "TEN_BV", "SHEET_TITLE", "TITLE", "TIEUDE", "NAME"]);
            }

            string displayName = !string.IsNullOrWhiteSpace(sheetNumber) && !string.IsNullOrWhiteSpace(sheetTitle)
                ? $"{sheetNumber} - {sheetTitle}"
                : (!string.IsNullOrWhiteSpace(sheetTitle) ? sheetTitle : (!string.IsNullOrWhiteSpace(sheetNumber) ? sheetNumber : effectiveName));

            double rotationDeg = blkRef.Rotation * (180.0 / Math.PI);

            items.Add(new PlotItem
            {
                Id = Guid.NewGuid().ToString("N"),
                Bounds = bounds,
                LayoutName = layoutName,
                DisplayName = displayName,
                SheetNumber = sheetNumber,
                SheetTitle = sheetTitle,
                Rotation = rotationDeg,
                SourceHandle = blkRef.Handle.ToString()
            });
        }

        tr.Commit();

        var sorted = _orderService.Sort(items, options.ToleranceBandYRatio);
        return Task.FromResult(sorted);
    }

    private static HashSet<string> BuildFilterSet(FrameScanOptions options)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (options.SelectedBlockNames.Count > 0)
        {
            foreach (var name in options.SelectedBlockNames)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    set.Add(name.Trim());
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(options.BlockName))
        {
            var parts = options.BlockName.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                set.Add(part);
            }
        }

        return set;
    }

    private static string? TryFindAttribute(BlockReference blkRef, Transaction tr, string[] candidateTags)
    {
        foreach (ObjectId attId in blkRef.AttributeCollection)
        {
            if (tr.GetObject(attId, OpenMode.ForRead) is AttributeReference attRef)
            {
                var tag = attRef.Tag.Trim();
                foreach (var candidate in candidateTags)
                {
                    if (string.Equals(tag, candidate, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(attRef.TextString))
                    {
                        return attRef.TextString.Trim();
                    }
                }
            }
        }
        return null;
    }
}
