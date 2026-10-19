using Autodesk.AutoCAD.Runtime;
using HPKata.AutoCad.Controllers;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// Command entry points for foundation plan layout, placement, pad footings, and pile caps
    /// (ve_mat_bang_mong/vmbm, rai_mong/rm, ve_mong_don/vmd, ve_mong_coc/vmc, ve_mong_coc_giat_cap, phamong, tkmong).
    /// </summary>
    public static class FoundationCommands
    {
        [CommandMethod("ve_mat_bang_mong")]
        [CommandMethod("vmbm")]
        [CommandMethod("VMBM_LIGHT")]
        public static void VeMatBangMong() => FoundationDrawController.DrawFoundationPlanInteractive();

        [CommandMethod("rai_mong")]
        [CommandMethod("rm")]
        public static void RaiMong() => FoundationDrawController.PlaceFootingBlockInteractive();

        [CommandMethod("ve_mong_don")]
        [CommandMethod("vmd")]
        public static void VeMongDon() => FoundationDrawController.DrawIsolatedFootingInteractive();

        [CommandMethod("ve_mong_coc")]
        [CommandMethod("vmc")]
        public static void VeMongCoc() => FoundationDrawController.DrawPileCapInteractive();

        [CommandMethod("ve_mong_coc_giat_cap")]
        [CommandMethod("vmcg")]
        public static void VeMongCocGiatCap() => FoundationShopController.DrawSteppedPileCapInteractive();

        [CommandMethod("phamong")]
        public static void PhaMong() => FoundationDrawController.ExplodeFoundationBlockInteractive();

        [CommandMethod("thong_ke_mong")]
        [CommandMethod("tkmong")]
        public static void TkMong() => FoundationTakeoffController.TakeoffFoundationsInteractive();
    }
}
