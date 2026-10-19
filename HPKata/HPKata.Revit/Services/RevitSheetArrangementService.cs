using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using HPKata.Core.DrawingLayout.Calculators;
using HPKata.Core.DrawingLayout.Models;
using HPKata.Revit.Units;

namespace HPKata.Revit.Services
{
    /// <summary>
    /// Service arranging and placing Revit views onto ViewSheets using 2D shelf-packing.
    /// </summary>
    public static class RevitSheetArrangementService
    {
        public static List<ViewSheet> ArrangeViewsOnSheets(
            Document doc,
            IEnumerable<View> views,
            ElementId titleBlockTypeId,
            SheetFormat format = SheetFormat.A1)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (views == null) throw new ArgumentNullException(nameof(views));

            var sheetDef = SheetSizeDefinition.Create(format, true);
            var (blocks, viewMap) = ConvertViewsToBlocks(views);
            var arrangedResults = SheetArrangerCalculator.ArrangeBlocks(blocks, sheetDef);
            var createdSheets = new List<ViewSheet>();

            for (int i = 0; i < arrangedResults.Count; i++)
            {
                var sheet = ViewSheet.Create(doc, titleBlockTypeId);
                sheet.SheetNumber = $"KC-{i + 1:D2}";
                sheet.Name = $"BẢN VẼ KẾT CẤU {i + 1}";
                createdSheets.Add(sheet);
                PlaceBlocksOnSheet(doc, sheet, arrangedResults[i], viewMap);
            }

            return createdSheets;
        }

        private static (List<DrawingBlockItem> blocks, Dictionary<string, View> viewMap) ConvertViewsToBlocks(
            IEnumerable<View> views)
        {
            var blocks = new List<DrawingBlockItem>();
            var viewMap = new Dictionary<string, View>();

            foreach (var v in views)
            {
                if (v.IsTemplate || v is View3D) continue;
                double wMm = 300.0;
                double hMm = 200.0;
                var outline = v.Outline;
                if (outline != null)
                {
                    double wFt = Math.Abs(outline.Max.U - outline.Min.U);
                    double hFt = Math.Abs(outline.Max.V - outline.Min.V);
                    wMm = Math.Max(100.0, RevitUnitConverter.FeetToMm(wFt));
                    hMm = Math.Max(100.0, RevitUnitConverter.FeetToMm(hFt));
                }

                var block = new DrawingBlockItem(v.Name, wMm, hMm);
                blocks.Add(block);
                viewMap[block.Id] = v;
            }

            return (blocks, viewMap);
        }

        private static void PlaceBlocksOnSheet(
            Document doc,
            ViewSheet sheet,
            ArrangedSheetResult res,
            Dictionary<string, View> viewMap)
        {
            foreach (var b in res.PlacedBlocks)
            {
                if (viewMap.TryGetValue(b.Id, out var targetView))
                {
                    var ptFt = RevitUnitConverter.PointMmToFeet(b.X + b.WidthMm * 0.5, b.Y + b.HeightMm * 0.5, 0.0);
                    if (Viewport.CanAddViewToSheet(doc, sheet.Id, targetView.Id))
                    {
                        Viewport.Create(doc, sheet.Id, targetView.Id, ptFt);
                    }
                }
            }
        }
    }
}
