using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller managing layer operations: hide/isolate layer (anlayer),
    /// unhide all layers (hienlayer), and switch entity layers (doilayer).
    /// </summary>
    public static class LayerUtilityController
    {
        public static void HideSelectedEntityLayer()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var peo = new PromptEntityOptions("\n[HPKata Layer] Chọn đối tượng trên layer cần ẩn (anlayer): ");
            var per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var ent = (Entity)tr.GetObject(per.ObjectId, OpenMode.ForRead);
                var lt = (LayerTable)tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead);

                if (lt.Has(ent.Layer))
                {
                    var ltr = (LayerTableRecord)tr.GetObject(lt[ent.Layer], OpenMode.ForWrite);
                    if (doc.Database.Clayer == ltr.ObjectId)
                    {
                        // Switch current layer to "0" safely before turning off
                        if (lt.Has("0"))
                        {
                            doc.Database.Clayer = lt["0"];
                        }
                    }
                    ltr.IsOff = true;
                    ed.WriteMessage($"\n[HPKata Layer] Đã ẩn layer '{ent.Layer}'.\n");
                }
                tr.Commit();
            }
        }

        public static void UnhideAllLayers()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead);
                int count = 0;

                foreach (ObjectId layerId in lt)
                {
                    var ltr = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForWrite);
                    if (ltr.IsOff || ltr.IsFrozen)
                    {
                        ltr.IsOff = false;
                        if (layerId != doc.Database.Clayer)
                        {
                            ltr.IsFrozen = false;
                        }
                        count++;
                    }
                }

                tr.Commit();
                ed.WriteMessage($"\n[HPKata Layer] Đã mở lại {count} layer đang bị ẩn/đóng băng.\n");
            }
        }

        public static void ChangeEntitiesLayer(string targetLayerName = "kata_thep chu")
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var psr = ed.GetSelection();
            if (psr.Status != PromptStatus.OK) return;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead);
                EnsureLayerExists(tr, lt, targetLayerName);

                int count = 0;
                foreach (SelectedObject so in psr.Value)
                {
                    if (so == null) continue;
                    var ent = tr.GetObject(so.ObjectId, OpenMode.ForWrite) as Entity;
                    if (ent != null)
                    {
                        ent.Layer = targetLayerName;
                        count++;
                    }
                }

                tr.Commit();
                ed.WriteMessage($"\n[HPKata Layer] Đã chuyển {count} đối tượng sang layer '{targetLayerName}'.\n");
            }
        }

        private static void EnsureLayerExists(Transaction tr, LayerTable lt, string layerName)
        {
            if (lt.Has(layerName)) return;

            lt.UpgradeOpen();
            var ltr = new LayerTableRecord { Name = layerName };
            lt.Add(ltr);
            tr.AddNewlyCreatedDBObject(ltr, true);
        }
    }
}
