using Autodesk.AutoCAD.Runtime;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// Command entry points for dynamic rebar-to-tag linking, clone re-binding, search, and update.
    /// </summary>
    public static class LinkCommands
    {
        [CommandMethod("tao_lien_ket")]
        [CommandMethod("link")]
        public static void TaoLienKet() => HandleLinkController.LinkInteractive();

        [CommandMethod("copy_tao_lien_ket")]
        [CommandMethod("cl")]
        public static void CopyTaoLienKet() => HandleLinkController.CopyLinkInteractive();

        [CommandMethod("xoa_lien_ket")]
        [CommandMethod("dl")]
        public static void XoaLienKet() => HandleLinkController.DeleteLinkInteractive();

        [CommandMethod("xem_lien_ket")]
        [CommandMethod("tim")]
        public static void XemLienKet() => HandleLinkController.FindLinkInteractive();

        [CommandMethod("xoa_highlight")]
        [CommandMethod("xoa")]
        public static void XoaHighlight() => HandleLinkController.ClearHighlightInteractive();

        [CommandMethod("update_thep")]
        [CommandMethod("ud")]
        public static void UpdateThep() => HandleLinkController.UpdateThepInteractive();

        [CommandMethod("ghi_thep")]
        [CommandMethod("gt")]
        public static void GhiThep() => HandleLinkController.GhiThepInteractive();

        [CommandMethod("stt0")]
        public static void Stt0() => HandleLinkController.AssignMarkNotation(isSecondary: true);

        [CommandMethod("stt1")]
        public static void Stt1() => HandleLinkController.AssignMarkNotation(isSecondary: false);

        [CommandMethod("timthep")]
        public static void TimThep() => HandleLinkController.SearchByMarkInteractive();

        [CommandMethod("check_lap")]
        public static void CheckLap() => HandleLinkController.CheckDuplicateMarksInteractive();
    }
}
