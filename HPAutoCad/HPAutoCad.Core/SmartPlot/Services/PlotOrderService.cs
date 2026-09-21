namespace HPAutoCad.Core.SmartPlot.Services;

using HPAutoCad.Core.SmartPlot.Models;

/// <summary>
/// Spatial ordering service that groups frames into rows based on vertical overlap tolerance
/// and orders rows Top to Bottom, and frames within each row Left to Right.
/// </summary>
public sealed class PlotOrderService : IPlotOrderService
{
    /// <inheritdoc />
    public IReadOnlyList<PlotItem> Sort(IEnumerable<PlotItem> items, double toleranceRatio = 0.5)
    {
        return OrderFrames(items, toleranceRatio);
    }

    /// <inheritdoc />
    public IReadOnlyList<PlotItem> OrderFrames(IEnumerable<PlotItem> items, double overlapRatioThreshold = 0.5)
    {
        if (items is null) return Array.Empty<PlotItem>();

        var validItems = items.Where(i => i.Bounds.IsValid).ToList();
        if (validItems.Count <= 1)
        {
            return validItems.Select((item, idx) => item with { Order = idx + 1 }).ToList();
        }

        var threshold = Math.Clamp(overlapRatioThreshold, 0.05, 0.95);

        // Sort descending by MaxY (highest top edge first), tie-breaker MinX ascending
        var sortedByTop = validItems
            .OrderByDescending(i => i.MaxY)
            .ThenBy(i => i.MinX)
            .ToList();

        var rows = new List<PlotRow>();

        foreach (var item in sortedByTop)
        {
            PlotRow? bestMatch = null;
            var bestOverlap = 0.0;

            foreach (var row in rows)
            {
                if (row.Matches(item, threshold, out var overlapRatio) && overlapRatio > bestOverlap)
                {
                    bestMatch = row;
                    bestOverlap = overlapRatio;
                }
            }

            if (bestMatch != null)
            {
                bestMatch.Add(item);
            }
            else
            {
                rows.Add(new PlotRow(item));
            }
        }

        // Sort rows Top to Bottom (descending CenterY)
        var orderedRows = rows.OrderByDescending(r => r.CenterY).ToList();

        var result = new List<PlotItem>(validItems.Count);
        var currentOrder = 1;

        foreach (var row in orderedRows)
        {
            // Within each row, sort Left to Right (ascending MinX)
            var sortedInRow = row.Items
                .OrderBy(i => i.MinX)
                .ThenByDescending(i => i.MaxY);

            foreach (var item in sortedInRow)
            {
                result.Add(item with { Order = currentOrder++ });
            }
        }

        return result;
    }

    private sealed class PlotRow
    {
        public double MinY { get; private set; }
        public double MaxY { get; private set; }
        public List<PlotItem> Items { get; } = [];

        public PlotRow(PlotItem firstItem)
        {
            MinY = firstItem.MinY;
            MaxY = firstItem.MaxY;
            Items.Add(firstItem);
        }

        public double Height => Math.Max(0.0, MaxY - MinY);
        public double CenterY => (MinY + MaxY) * 0.5;

        public bool Matches(PlotItem item, double threshold, out double overlapRatio)
        {
            var overlap = Math.Max(0.0, Math.Min(MaxY, item.MaxY) - Math.Max(MinY, item.MinY));
            var minHeight = Math.Min(Height, item.Height);

            if (minHeight <= 1e-6)
            {
                overlapRatio = Math.Abs(CenterY - item.Bounds.CenterY) < 1.0 ? 1.0 : 0.0;
                return overlapRatio > 0;
            }

            overlapRatio = overlap / minHeight;
            return overlapRatio >= threshold;
        }

        public void Add(PlotItem item)
        {
            Items.Add(item);
            MinY = Math.Min(MinY, item.MinY);
            MaxY = Math.Max(MaxY, item.MaxY);
        }
    }
}
