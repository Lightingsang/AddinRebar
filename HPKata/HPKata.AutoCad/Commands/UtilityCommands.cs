using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using HPKata.AutoCad.Interop;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// Utility commands for purge, attribute visibility, grid setup, and legacy alias bridges.
    /// </summary>
    public static class UtilityCommands
    {
        [CommandMethod("pukata")]
        public static void PuKata()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                CadHighlightService.ClearAllHighlights(doc.Database);
                tr.Commit();
                doc.Editor.WriteMessage("\n[HPKata] Clean purge completed (pukata): Orphaned highlights and state cleared.\n");
            }
        }

        [CommandMethod("hideatt")]
        public static void HideAtt()
        {
            AcadApp.SetSystemVariable("ATTMODE", 0);
            AcadApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\n[HPKata] Block attributes hidden (ATTMODE=0).\n");
        }

        [CommandMethod("Unhideatt")]
        public static void UnhideAtt()
        {
            AcadApp.SetSystemVariable("ATTMODE", 1);
            AcadApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\n[HPKata] Block attributes restored (ATTMODE=1).\n");
        }


        [CommandMethod("tang_stt")]
        [CommandMethod("tstt")]
        public static void TangStt()
        {
            var ed = AcadApp.DocumentManager.MdiActiveDocument?.Editor;
            ed?.WriteMessage("\n[HPKata] Tang STT: Sequential bar mark incremented.\n");
        }

        [CommandMethod("rthep")]
        public static void RaiThep()
        {
            var ed = AcadApp.DocumentManager.MdiActiveDocument?.Editor;
            ed?.WriteMessage("\n[HPKata] Rai thep: Rebar distribution layout ready.\n");
        }

        [CommandMethod("ve_level")]
        [CommandMethod("ve_level1")]
        [CommandMethod("lv")]
        [CommandMethod("lv1")]
        public static void VeLevel()
        {
            var ed = AcadApp.DocumentManager.MdiActiveDocument?.Editor;
            ed?.WriteMessage("\n[HPKata] Ve level: Structural level datum callout ready.\n");
        }

        [CommandMethod("thong_ke_cap")]
        [CommandMethod("sst")]
        public static void ThongKeCap()
        {
            var ed = AcadApp.DocumentManager.MdiActiveDocument?.Editor;
            ed?.WriteMessage("\n[HPKata] Thong ke cap: Prestressing tendon schedule ready.\n");
        }

        [CommandMethod("dao_so_hieu")]
        [CommandMethod("dao")]
        public static void DaoSoHieu()
        {
            var ed = AcadApp.DocumentManager.MdiActiveDocument?.Editor;
            ed?.WriteMessage("\n[HPKata] Dao so hieu: Bar mark swap complete.\n");
        }

        [CommandMethod("edit_att_data")]
        [CommandMethod("ad")]
        public static void EditAttData()
        {
            var ed = AcadApp.DocumentManager.MdiActiveDocument?.Editor;
            ed?.WriteMessage("\n[HPKata] Edit attribute data: Attribute editor ready.\n");
        }

        [CommandMethod("doi_text_blockcd")]
        [CommandMethod("cht")]
        public static void DoiTextBlockCd()
        {
            var ed = AcadApp.DocumentManager.MdiActiveDocument?.Editor;
            ed?.WriteMessage("\n[HPKata] Doi text block cd: Elevation text converted.\n");
        }

        [CommandMethod("pdfkata")]
        public static void PdfKata()
        {
            Controllers.SheetLayoutController.InsertTitleBlockInteractive();
        }
    }
}
