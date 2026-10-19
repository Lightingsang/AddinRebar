using Autodesk.AutoCAD.Runtime;
using HPKata.AutoCad.Controllers;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// AutoCAD command methods for grid axis generation (vg), sheet arrangement (sbv),
    /// title block placement (ibv), PaperSpace viewports (taomv/khoav), and drawing index (danhmuc).
    /// </summary>
    public static class LayoutCommands
    {
        [CommandMethod("vg")]
        [CommandMethod("ve_grid")]
        [CommandMethod("ve_luoi_truc")]
        public static void DrawGrid()
        {
            GridDrawController.DrawGridInteractive();
        }

        [CommandMethod("sbv")]
        [CommandMethod("sap_xep_ban_ve")]
        [CommandMethod("sap_ban_ve")]
        public static void ArrangeSheets()
        {
            SheetLayoutController.ArrangeDrawingBlocksInteractive();
        }

        [CommandMethod("ibv")]
        [CommandMethod("chen_khung_ten")]
        [CommandMethod("in_ban_ve")]
        public static void InsertTitleBlock()
        {
            SheetLayoutController.InsertTitleBlockInteractive();
        }

        [CommandMethod("taomv")]
        public static void CreateViewportDefault()
        {
            ViewportController.CreateViewportInteractive(0.01);
        }

        [CommandMethod("taomv1")]
        public static void CreateViewportDetail()
        {
            ViewportController.CreateViewportInteractive(0.02);
        }

        [CommandMethod("khoav")]
        public static void LockViewport()
        {
            ViewportController.SetViewportLock(true);
        }

        [CommandMethod("mokhoav")]
        public static void UnlockViewport()
        {
            ViewportController.SetViewportLock(false);
        }

        [CommandMethod("danhmuc")]
        public static void GenerateDrawingIndex()
        {
            DrawingIndexController.GenerateDrawingIndexTableInteractive();
        }
    }
}
