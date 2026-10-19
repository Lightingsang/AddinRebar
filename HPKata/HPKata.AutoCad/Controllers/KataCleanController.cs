using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using HPKata.AutoCad.Interop;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller managing database maintenance, cleaning orphaned XRecords,
    /// and purging temporary drawing state (clean_kata, xoa_xrecord).
    /// </summary>
    public static class KataCleanController
    {
        public static void CleanKataDatabase()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                CadHighlightService.ClearAllHighlights(doc.Database);
                int removedCount = PurgeKataDictionaryRecords(tr, doc.Database);

                tr.Commit();
                ed.WriteMessage($"\n[HPKata Clean] Đã dọn dẹp {removedCount} mục dữ liệu tạm và làm sạch bản vẽ.\n");
            }
        }

        private static int PurgeKataDictionaryRecords(Transaction tr, Database db)
        {
            int count = 0;
            var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForWrite);

            string dictName = "HPKATA_STATE_DICT";
            if (nod.Contains(dictName))
            {
                nod.Remove(dictName);
                count++;
            }

            return count;
        }
    }
}
