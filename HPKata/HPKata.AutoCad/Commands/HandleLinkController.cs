using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPKata.AutoCad.Abstractions;
using HPKata.AutoCad.Interop;
using HPKata.Core.Serialization;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// Controller orchestrating dynamic handle linking, cloning, unlinking, and updating.
    /// </summary>
    public static class HandleLinkController
    {
        public static MarkNotationMode ActiveMarkNotationMode { get; set; } = MarkNotationMode.Sequential;

        public static void LinkInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            ObjectId? rebarId = CadSelectionService.PromptSelectEntity(ed, "Select rebar polyline: ", typeof(Polyline));
            if (!rebarId.HasValue) return;

            ObjectId? tagId = CadSelectionService.PromptSelectEntity(ed, "Select tag callout block: ", typeof(BlockReference));
            if (!tagId.HasValue) return;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var rebar = (Entity)tr.GetObject(rebarId.Value, OpenMode.ForWrite);
                var tag = (BlockReference)tr.GetObject(tagId.Value, OpenMode.ForWrite);

                HandleLinkService.CreateLink(rebar, tag, tr);
                tr.Commit();
                ed.WriteMessage($"\n[HPKata] Linked rebar ({rebar.Handle}) <-> tag ({tag.Handle}) successfully.\n");
            }
        }

        public static void CopyLinkInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            List<ObjectId> selIds = CadSelectionService.PromptSelectMultiple(ed, "Select linked rebar and tag blocks to copy: ");
            if (selIds.Count == 0) return;

            Point3d? pt1 = CadSelectionService.PromptPoint(ed, "Specify base point: ");
            if (!pt1.HasValue) return;
            Point3d? pt2 = CadSelectionService.PromptPoint(ed, "Specify second point: ");
            if (!pt2.HasValue) return;

            Vector3d displacement = pt2.Value - pt1.Value;
            Matrix3d xform = Matrix3d.Displacement(displacement);

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var cloneMap = new Dictionary<string, Entity>(StringComparer.OrdinalIgnoreCase);

                foreach (ObjectId id in selIds)
                {
                    var orig = (Entity)tr.GetObject(id, OpenMode.ForRead);
                    var clone = (Entity)orig.Clone();
                    clone.TransformBy(xform);
                    ms.AppendEntity(clone);
                    tr.AddNewlyCreatedDBObject(clone, true);
                    cloneMap[orig.Handle.ToString()] = clone;
                }

                HandleLinkService.RebindClonedEntities(cloneMap, tr);
                tr.Commit();
                ed.WriteMessage($"\n[HPKata] Copied and rebound links for {cloneMap.Count} entities.\n");
            }
        }

        public static void DeleteLinkInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            ObjectId? selId = CadSelectionService.PromptSelectEntity(ed, "Select entity to sever link: ");
            if (!selId.HasValue) return;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var ent = (Entity)tr.GetObject(selId.Value, OpenMode.ForWrite);
                bool removed = HandleLinkService.SeverLink(ent, tr);
                tr.Commit();
                ed.WriteMessage(removed ? "\n[HPKata] Link severed successfully.\n" : "\n[HPKata] Entity had no active link.\n");
            }
        }

        public static void FindLinkInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            ObjectId? selId = CadSelectionService.PromptSelectEntity(ed, "Select entity to view link: ");
            if (!selId.HasValue) return;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var ent = (Entity)tr.GetObject(selId.Value, OpenMode.ForRead);
                KataXDataRecord r = XDataService.ReadXData(ent);
                if (string.IsNullOrEmpty(r.LinkedHandle))
                {
                    ed.WriteMessage("\n[HPKata] Selected entity is not linked.\n");
                    return;
                }

                Entity? partner = HandleLinkService.FindLinkedEntity(doc.Database, r.LinkedHandle, tr);
                if (partner != null)
                {
                    CadHighlightService.HighlightEntity(ent.ObjectId, tr);
                    CadHighlightService.HighlightEntity(partner.ObjectId, tr);
                    CadHighlightService.ZoomToEntity(ed, partner);
                    ed.WriteMessage($"\n[HPKata] Linked partner: {partner.GetType().Name} (Handle: {partner.Handle}).\n");
                }
                tr.Commit();
            }
        }

        public static void ClearHighlightInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            CadHighlightService.ClearAllHighlights(doc.Database);
            doc.Editor.WriteMessage("\n[HPKata] Highlights cleared.\n");
        }

        public static void UpdateThepInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            List<ObjectId> ids = CadSelectionService.PromptSelectMultiple(ed, "Select rebar polylines to update: ");
            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                int count = 0;
                foreach (ObjectId id in ids)
                {
                    if (tr.GetObject(id, OpenMode.ForWrite) is Polyline pl)
                    {
                        if (RebarUpdateService.UpdateRebarAndLinkedTag(pl, tr, false)) count++;
                    }
                }
                tr.Commit();
                ed.WriteMessage($"\n[HPKata] Updated geometry and linked tags for {count} of {ids.Count} rebars.\n");
            }
        }

        public static void GhiThepInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            ObjectId? rebarId = CadSelectionService.PromptSelectEntity(ed, "Select rebar polyline to tag: ", typeof(Polyline));
            if (!rebarId.HasValue) return;

            Point3d? pt = CadSelectionService.PromptPoint(ed, "Specify tag placement point: ");
            if (!pt.HasValue) return;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var poly = (Polyline)tr.GetObject(rebarId.Value, OpenMode.ForWrite);
                BlockReference tagRef = RebarTagService.InsertRebarTag(doc.Database, pt.Value, poly, tr);
                tr.Commit();
                ed.WriteMessage($"\n[HPKata] Tag placed at {pt.Value} and linked to rebar handle {poly.Handle} (Tag: {tagRef.Handle}).\n");
            }
        }

        public static void AssignMarkNotation(bool isSecondary)
        {
            ActiveMarkNotationMode = isSecondary ? MarkNotationMode.Suffix : MarkNotationMode.Sequential;
            string mode = isSecondary ? "Secondary suffix (1a, 1b...)" : "Primary sequential (1, 2, 3...)";

            try
            {
                Document? doc = AcadApp.DocumentManager?.MdiActiveDocument;
                if (doc != null)
                {
                    doc.Editor.WriteMessage($"\n[HPKata] Mark notation mode configured to {mode}.\n");
                }
            }
            catch (Exception)
            {
                // AutoCAD DocumentManager is unavailable in headless unit test runners
            }
        }

        public static void SearchByMarkInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var opt = new PromptIntegerOptions("\nEnter bar mark (STT) to find: ");
            PromptIntegerResult res = ed.GetInteger(opt);
            if (res.Status != PromptStatus.OK) return;
            int mark = res.Value;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                foreach (ObjectId id in ms)
                {
                    if (tr.GetObject(id, OpenMode.ForRead) is Entity ent && XDataService.HasKataXData(ent))
                    {
                        KataXDataRecord r = XDataService.ReadXData(ent);
                        if (r.STT == mark)
                        {
                            CadHighlightService.HighlightEntity(id, tr);
                            CadHighlightService.ZoomToEntity(ed, ent);
                            ed.WriteMessage($"\n[HPKata] Found bar mark {mark} (Handle: {ent.Handle}).\n");
                            tr.Commit();
                            return;
                        }
                    }
                }
                tr.Commit();
                ed.WriteMessage($"\n[HPKata] Bar mark {mark} not found in active drawing.\n");
            }
        }

        public static void CheckDuplicateMarksInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            RebarMarkAuditService.CheckDuplicateMarksInDatabase(doc.Database, doc.Editor);
        }

        public static int CheckDuplicateMarks(
            IEnumerable<KataXDataRecord> records,
            out List<string> conflictReports)
        {
            return RebarMarkAuditService.CheckDuplicateMarks(records, out conflictReports);
        }

        public static int CheckDuplicateMarks(
            ICadDocumentContext context,
            out List<string> conflictReports)
        {
            return RebarMarkAuditService.CheckDuplicateMarks(context, out conflictReports);
        }
    }
}
