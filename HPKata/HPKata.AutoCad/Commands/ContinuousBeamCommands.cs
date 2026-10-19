using Autodesk.AutoCAD.Runtime;
using HPKata.AutoCad.Controllers;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// Command entry points for continuous beam detailing, shop drawings, and cross-sections
    /// (vd, vdui, vda, vds, vdas, vdmc, vdmca, loadbeam).
    /// </summary>
    public static class ContinuousBeamCommands
    {
        [CommandMethod("ve_dam")]
        [CommandMethod("vd")]
        public static void VeDam() => BeamDrawController.DrawContinuousBeamInteractive(isShopMode: false, autoReadExcel: true);

        [CommandMethod("ve_dam_ui")]
        [CommandMethod("vdui")]
        public static void VeDamUi() => BeamDrawController.DrawContinuousBeamWithUiDialog(isShopMode: false, autoReadExcel: true);

        [CommandMethod("ve_dam_all")]
        [CommandMethod("vda")]
        public static void VeDamAll() => BeamDrawController.DrawContinuousBeamAllBatch();

        [CommandMethod("ve_dam_shop")]
        [CommandMethod("vds")]
        public static void VeDamShop() => BeamShopController.DrawBeamShopInteractive();

        [CommandMethod("ve_dam_all_shop")]
        [CommandMethod("vdas")]
        public static void VeDamAllShop() => BeamShopController.DrawBeamAllShopBatch();

        [CommandMethod("ve_dam_mc")]
        [CommandMethod("vdmc")]
        public static void VeDamMc() => BeamSectionController.DrawBeamSectionInteractive();

        [CommandMethod("ve_damMC_all")]
        [CommandMethod("vdmca")]
        public static void VeDamMcAll() => BeamSectionController.DrawBeamSectionAllBatch();

        [CommandMethod("loadbeam")]
        public static void LoadBeam() => BeamExcelController.LoadBeamInteractive();
    }
}
