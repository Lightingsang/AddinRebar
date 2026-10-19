using System;
using System.Collections.Generic;
using System.Linq;
using HPKata.Core.DrawingLayout.Models;

namespace HPKata.Core.DrawingLayout.Calculators
{
    /// <summary>
    /// 2D shelf-packing drawing detail block arranger with boundary clamping,
    /// bottom-right TitleBlock collision avoidance, and multi-sheet overflow pagination.
    /// </summary>
    public static class SheetArrangerCalculator
    {
        public static List<ArrangedSheetResult> ArrangeBlocks(
            IEnumerable<DrawingBlockItem> blocks,
            SheetSizeDefinition sheetDef,
            TitleBlockModel? titleBlock = null,
            double gutterX = 20.0,
            double gutterY = 30.0)
        {
            if (blocks == null) throw new ArgumentNullException(nameof(blocks));
            if (sheetDef == null) throw new ArgumentNullException(nameof(sheetDef));

            var tb = titleBlock ?? new TitleBlockModel();
            var sortedBlocks = blocks.OrderByDescending(b => b.HeightMm).ToList();
            var sheets = new List<ArrangedSheetResult>();

            if (sortedBlocks.Count == 0) return sheets;

            double pWidth = sheetDef.PrintableWidthMm;
            double pHeight = sheetDef.PrintableHeightMm;

            ArrangedSheetResult? currentSheet = null;
            double cursorX = sheetDef.MarginLeftMm;
            double cursorY = sheetDef.MarginBottomMm;
            double currentShelfHeight = 0.0;

            foreach (var block in sortedBlocks)
            {
                if (block.WidthMm <= 0 || block.HeightMm <= 0 ||
                    block.WidthMm > pWidth || block.HeightMm > pHeight)
                {
                    continue; // Skip physically unplaceable oversized blocks
                }

                if (currentSheet == null)
                {
                    currentSheet = CreateNewSheet(sheets.Count, sheetDef, tb);
                    sheets.Add(currentSheet);
                }

                PlaceBlock(block, sheets, ref currentSheet, sheetDef, tb,
                    ref cursorX, ref cursorY, ref currentShelfHeight, gutterX, gutterY);
            }

            PruneAndNormalizeSheets(sheets);
            return sheets;
        }

        private static void PlaceBlock(
            DrawingBlockItem block,
            List<ArrangedSheetResult> sheets,
            ref ArrangedSheetResult currentSheet,
            SheetSizeDefinition sheetDef,
            TitleBlockModel tb,
            ref double cursorX,
            ref double cursorY,
            ref double currentShelfHeight,
            double gutterX,
            double gutterY)
        {
            if (TryPlaceOnShelf(block, currentSheet, sheetDef, tb, ref cursorX, cursorY, ref currentShelfHeight, gutterX))
            {
                return;
            }

            // Try next shelf on current sheet
            double nextY = GetNextShelfY(cursorY, currentShelfHeight, gutterY, sheetDef, tb);
            if (nextY + block.HeightMm <= sheetDef.HeightMm - sheetDef.MarginTopMm)
            {
                cursorY = nextY;
                cursorX = sheetDef.MarginLeftMm;
                currentShelfHeight = 0.0;
                if (TryPlaceOnShelf(block, currentSheet, sheetDef, tb, ref cursorX, cursorY, ref currentShelfHeight, gutterX))
                {
                    return;
                }
            }

            // Current sheet cannot accommodate block; allocate new sheet
            FinalizeSheet(currentSheet);
            currentSheet = CreateNewSheet(sheets.Count, sheetDef, tb);
            sheets.Add(currentSheet);

            cursorX = sheetDef.MarginLeftMm;
            cursorY = sheetDef.MarginBottomMm;
            currentShelfHeight = 0.0;

            if (!TryPlaceOnShelf(block, currentSheet, sheetDef, tb, ref cursorX, cursorY, ref currentShelfHeight, gutterX))
            {
                double freshNextY = GetNextShelfY(cursorY, currentShelfHeight, gutterY, sheetDef, tb);
                if (freshNextY + block.HeightMm <= sheetDef.HeightMm - sheetDef.MarginTopMm)
                {
                    cursorY = freshNextY;
                    cursorX = sheetDef.MarginLeftMm;
                    currentShelfHeight = 0.0;
                    TryPlaceOnShelf(block, currentSheet, sheetDef, tb, ref cursorX, cursorY, ref currentShelfHeight, gutterX);
                }
            }
        }

        private static double GetNextShelfY(
            double cursorY, double currentShelfHeight, double gutterY, SheetSizeDefinition sheetDef, TitleBlockModel tb)
        {
            double tbTop = sheetDef.MarginBottomMm + tb.HeightMm;
            if (currentShelfHeight <= 0.0)
            {
                return tb.Placement == TitleBlockPlacement.BottomRight && cursorY < tbTop
                    ? tbTop
                    : cursorY + gutterY;
            }

            double nextY = cursorY + currentShelfHeight + gutterY;
            if (tb.Placement == TitleBlockPlacement.BottomRight && cursorY < tbTop && nextY < tbTop)
            {
                return tbTop;
            }
            return nextY;
        }

        private static bool TryPlaceOnShelf(
            DrawingBlockItem block,
            ArrangedSheetResult sheet,
            SheetSizeDefinition def,
            TitleBlockModel tb,
            ref double cursorX,
            double cursorY,
            ref double shelfHeight,
            double gutterX)
        {
            double maxX = def.WidthMm - def.MarginRightMm;
            double maxY = def.HeightMm - def.MarginTopMm;

            if (cursorX + block.WidthMm > maxX || cursorY + block.HeightMm > maxY)
            {
                return false;
            }

            if (IntersectsTitleBlock(cursorX, cursorY, block.WidthMm, block.HeightMm, def, tb))
            {
                return false;
            }

            block.X = cursorX;
            block.Y = cursorY;
            block.SheetIndex = sheet.SheetIndex;
            sheet.PlacedBlocks.Add(block);

            cursorX += block.WidthMm + gutterX;
            shelfHeight = Math.Max(shelfHeight, block.HeightMm);
            return true;
        }

        private static bool IntersectsTitleBlock(
            double x, double y, double w, double h,
            SheetSizeDefinition def, TitleBlockModel tb)
        {
            if (tb.Placement != TitleBlockPlacement.BottomRight) return false;

            double tbMinX = def.WidthMm - def.MarginRightMm - tb.WidthMm;
            double tbMaxX = def.WidthMm - def.MarginRightMm;
            double tbMinY = def.MarginBottomMm;
            double tbMaxY = def.MarginBottomMm + tb.HeightMm;

            bool xOverlap = x < tbMaxX && (x + w) > tbMinX;
            bool yOverlap = y < tbMaxY && (y + h) > tbMinY;

            return xOverlap && yOverlap;
        }

        private static void PruneAndNormalizeSheets(List<ArrangedSheetResult> sheets)
        {
            sheets.RemoveAll(s => s.PlacedBlocks.Count == 0);

            for (int i = 0; i < sheets.Count; i++)
            {
                sheets[i].SheetIndex = i;
                foreach (var b in sheets[i].PlacedBlocks)
                {
                    b.SheetIndex = i;
                }
                FinalizeSheet(sheets[i]);
            }
        }

        private static ArrangedSheetResult CreateNewSheet(int index, SheetSizeDefinition def, TitleBlockModel tb)
        {
            return new ArrangedSheetResult(index, def, tb);
        }

        private static void FinalizeSheet(ArrangedSheetResult sheet)
        {
            double printableArea = sheet.SheetDefinition.PrintableWidthMm * sheet.SheetDefinition.PrintableHeightMm;
            double tbArea = sheet.TitleBlock.WidthMm * sheet.TitleBlock.HeightMm;
            double netArea = Math.Max(1.0, printableArea - tbArea);

            double usedArea = sheet.PlacedBlocks.Sum(b => b.WidthMm * b.HeightMm);
            sheet.UtilizationRatio = Math.Min(100.0, Math.Round((usedArea / netArea) * 100.0, 2));
        }
    }
}
