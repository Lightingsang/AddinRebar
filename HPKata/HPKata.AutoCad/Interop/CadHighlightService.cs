using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace HPKata.AutoCad.Interop
{
    /// <summary>
    /// Transient graphics, highlighting, and viewport zooming service for 'tim' and 'xoa' commands.
    /// </summary>
    public static class CadHighlightService
    {
        private static readonly HashSet<ObjectId> HighlightedIds = new();

        public static void HighlightEntity(ObjectId id, Transaction tr)
        {
            if (id.IsNull || id.IsErased || tr == null) return;

            try
            {
                if (tr.GetObject(id, OpenMode.ForRead) is Entity ent)
                {
                    ent.Highlight();
                    HighlightedIds.Add(id);
                }
            }
            catch (Exception)
            {
                // Silently ignore if entity cannot be highlighted in active viewport
            }
        }

        public static void UnhighlightEntity(ObjectId id, Transaction tr)
        {
            if (id.IsNull || id.IsErased || tr == null) return;

            try
            {
                if (tr.GetObject(id, OpenMode.ForRead) is Entity ent)
                {
                    ent.Unhighlight();
                    HighlightedIds.Remove(id);
                }
            }
            catch (Exception)
            {
                // Silently ignore if entity cannot be unhighlighted
            }
        }

        public static void ClearAllHighlights(Database db)
        {
            if (db == null || HighlightedIds.Count == 0) return;

            try
            {
                using var tr = db.TransactionManager.StartTransaction();
                foreach (ObjectId id in HighlightedIds)
                {
                    if (!id.IsNull && !id.IsErased)
                    {
                        if (tr.GetObject(id, OpenMode.ForRead) is Entity ent)
                        {
                            ent.Unhighlight();
                        }
                    }
                }
                tr.Commit();
            }
            catch (Exception)
            {
                // Ignore transient unhighlighting errors
            }
            finally
            {
                HighlightedIds.Clear();
            }
        }

        public static void ZoomToEntity(Editor ed, Entity ent)
        {
            if (ed == null || ent == null) return;

            try
            {
                Extents3d ext = ent.GeometricExtents;
                using var view = new ViewTableRecord
                {
                    CenterPoint = new Autodesk.AutoCAD.Geometry.Point2d(
                        (ext.MinPoint.X + ext.MaxPoint.X) / 2.0,
                        (ext.MinPoint.Y + ext.MaxPoint.Y) / 2.0),
                    Width = Math.Max(ext.MaxPoint.X - ext.MinPoint.X, 100.0) * 1.5,
                    Height = Math.Max(ext.MaxPoint.Y - ext.MinPoint.Y, 100.0) * 1.5
                };
                ed.SetCurrentView(view);
            }
            catch (Exception)
            {
                // View zooming fallback if extents unavailable
            }
        }
    }
}
