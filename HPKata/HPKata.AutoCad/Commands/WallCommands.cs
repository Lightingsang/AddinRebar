using Autodesk.AutoCAD.Runtime;
using HPKata.AutoCad.Controllers;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// Command entry points for shear wall drafting, shop drawings, elevator core walls, and ETABS loading
    /// (ve_vach/vv, ve_vach_ui/vvui, ve_vach_shop/vvs, ve_loi_vach/vlv, loadwall).
    /// </summary>
    public static class WallCommands
    {
        [CommandMethod("ve_vach")]
        [CommandMethod("vv")]
        public static void VeVach() => WallDrawController.DrawWallInteractive(autoReadExcel: true);

        [CommandMethod("ve_vach_ui")]
        [CommandMethod("vvui")]
        public static void VeVachUi() => WallDrawController.DrawWallWithUiDialog(autoReadExcel: true);

        [CommandMethod("ve_vach_shop")]
        [CommandMethod("vvs")]
        public static void VeVachShop() => WallShopController.DrawWallShopInteractive();

        [CommandMethod("ve_loi_vach")]
        [CommandMethod("vlv")]
        public static void VeLoiVach() => WallCoreController.DrawWallCoreInteractive();

        [CommandMethod("loadwall")]
        public static void LoadWall() => WallExcelController.LoadWallInteractive();

        [CommandMethod("loadwall_etabs")]
        public static void LoadWallEtabs() => WallEtabsController.LoadWallInteractive();
    }
}
