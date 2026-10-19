using Autodesk.AutoCAD.Runtime;
using HPKata.AutoCad.Controllers;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// Command entry points for column drafting, shop drawings, placement, dimensioning, takeoff, and ETABS loading
    /// (ve_cot/vc, ve_cot_ui/vcui, ve_cot_shop/vcs, raicot, dimcot, tkcot, loadcolumn).
    /// </summary>
    public static class ColumnCommands
    {
        [CommandMethod("ve_cot")]
        [CommandMethod("vc")]
        public static void VeCot() => ColumnDrawController.DrawColumnInteractive(autoReadExcel: true);

        [CommandMethod("ve_cot_ui")]
        [CommandMethod("vcui")]
        public static void VeCotUi() => ColumnDrawController.DrawColumnWithUiDialog(autoReadExcel: true);

        [CommandMethod("ve_cot_shop")]
        [CommandMethod("vcs")]
        public static void VeCotShop() => ColumnShopController.DrawColumnShopInteractive();

        [CommandMethod("raicot")]
        public static void RaiCot() => ColumnPlacementController.PlaceColumnBlocksInteractive();

        [CommandMethod("dimcot")]
        public static void DimCot() => ColumnDimensionController.DimColumnGridsInteractive();

        [CommandMethod("tkcot")]
        public static void TkCot() => ColumnTakeoffController.TakeoffColumnsInteractive();

        [CommandMethod("loadcolumn")]
        public static void LoadColumn() => ColumnExcelController.LoadColumnInteractive();

        [CommandMethod("loadcolumn_etabs")]
        public static void LoadColumnEtabs() => ColumnEtabsController.LoadColumnInteractive();

        [CommandMethod("Update_Block_ColumnWall")]
        [CommandMethod("updatecrank")]
        [CommandMethod("updatedai")]
        [CommandMethod("updateleg")]
        public static void UpdateColumnParameters() => ColumnShopController.DrawColumnShopInteractive();
    }
}
