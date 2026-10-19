using System;
using System.Collections.Generic;
using System.Linq;
using HPKata.Core.DrawingLayout.Calculators;
using HPKata.Core.DrawingLayout.Models;
using HPKata.Core.QuantitySurveying.Models;
using HPKata.Core.QuantitySurveying.Persistence;
using HPKata.Revit.Abstractions;

namespace HPKata.Revit.Runners
{
    /// <summary>
    /// Headless runner executing QS takeoff aggregation and sheet layout arrangement
    /// using the abstracted Revit document context.
    /// </summary>
    public static class RevitQsLayoutRunner
    {
        public static string ExportQsTakeoff(
            IRevitDocumentContext context,
            string filePath,
            IEnumerable<QsElementModel>? customElements = null)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Path required.", nameof(filePath));

            var elements = customElements?.ToList() ?? new List<QsElementModel>();
            if (elements.Count == 0)
            {
                // Fallback default sample elements from context title
                elements.Add(new QsElementModel
                {
                    Id = "E1",
                    Mark = "C1",
                    MemberType = QsMemberType.Column,
                    Story = context.ActiveViewName ?? "Level 1",
                    LengthMm = 3600,
                    WidthMm = 400,
                    HeightMm = 400
                });
            }

            var summary = QsScheduleAggregator.Aggregate(elements);
            QsWorkbookGenerator.ExportToFile(summary, filePath);
            context.WriteNotification($"[HPKata QS] Đã xuất khối lượng ra file: {filePath}");
            return filePath;
        }

        public static List<ArrangedSheetResult> ArrangeSheets(
            IRevitDocumentContext context,
            IEnumerable<DrawingBlockItem> blocks,
            SheetFormat format = SheetFormat.A1)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (blocks == null) throw new ArgumentNullException(nameof(blocks));

            var sheetDef = SheetSizeDefinition.Create(format, true);
            var results = SheetArrangerCalculator.ArrangeBlocks(blocks, sheetDef);
            context.WriteNotification($"[HPKata Layout] Đã sắp xếp chi tiết lên {results.Count} bản vẽ khổ {format}.");
            return results;
        }
    }
}
