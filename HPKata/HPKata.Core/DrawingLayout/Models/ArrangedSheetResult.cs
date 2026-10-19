using System.Collections.Generic;

namespace HPKata.Core.DrawingLayout.Models
{
    /// <summary>
    /// Result of 2D shelf-packing on an individual drawing sheet.
    /// </summary>
    public class ArrangedSheetResult
    {
        public int SheetIndex { get; set; }
        public SheetSizeDefinition SheetDefinition { get; set; } = SheetSizeDefinition.Create(SheetFormat.A1);
        public TitleBlockModel TitleBlock { get; set; } = new TitleBlockModel();
        public List<DrawingBlockItem> PlacedBlocks { get; set; } = new List<DrawingBlockItem>();
        public double UtilizationRatio { get; set; }

        public ArrangedSheetResult()
        {
        }

        public ArrangedSheetResult(int index, SheetSizeDefinition sheetDef, TitleBlockModel titleBlock)
        {
            SheetIndex = index;
            SheetDefinition = sheetDef;
            TitleBlock = titleBlock;
        }
    }
}
