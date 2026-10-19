using Autodesk.AutoCAD.Runtime;
using HPKata.AutoCad.Controllers;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// Command entry points for slab detailing, polyline slabs, 4-way reinforcement, splitting, and takeoff
    /// (ve_san/vesan, vesanpl, vesan4d, ssan, pick_diem_san/dsan, chia_san/chiasan, thong_ke_block_san/tksan).
    /// </summary>
    public static class SlabCommands
    {
        [CommandMethod("ve_san")]
        [CommandMethod("vesan")]
        public static void VeSan() => SlabDrawController.DrawSlabInteractive();

        [CommandMethod("vesanpl")]
        public static void VeSanPl() => SlabDrawController.DrawPolylineSlabInteractive();

        [CommandMethod("vesan4d")]
        public static void VeSan4D() => SlabShopController.DrawFourWaySlabInteractive();

        [CommandMethod("ssan")]
        public static void Ssan() => SlabDrawController.InspectSlabInteractive();

        [CommandMethod("pick_diem_san")]
        [CommandMethod("dsan")]
        public static void Dsan() => SlabDrawController.PickPointSlabInteractive();

        [CommandMethod("chia_san")]
        [CommandMethod("chiasan")]
        public static void ChiaSan() => SlabDrawController.SplitSlabInteractive();

        [CommandMethod("thong_ke_block_san")]
        [CommandMethod("tksan")]
        public static void TkSan() => SlabTakeoffController.TakeoffSlabsInteractive();

        [CommandMethod("chuyen")]
        public static void ChuyenBlockSan() => SlabShopController.ConvertSlabBlockInteractive();

        [CommandMethod("show_thep_rai")]
        [CommandMethod("str")]
        public static void ShowThepRai() => SlabShopController.ShowDistributionBandsInteractive();

        [CommandMethod("drpn")]
        public static void DropPanel() => SlabShopController.DrawDropPanelInteractive();

        [CommandMethod("openning")]
        public static void SlabOpening() => SlabShopController.DrawSlabOpeningInteractive();
    }
}
