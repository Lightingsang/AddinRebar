using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller managing PaperSpace layout viewports creation, scaling, and display lock toggling.
    /// </summary>
    public static class ViewportController
    {
        public static void CreateViewportInteractive(double customScale = 0.01)
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var p1 = ed.GetPoint("\n[HPKata Viewport] Chọn góc thứ nhất của Viewport: ");
            if (p1.Status != PromptStatus.OK) return;

            var p2 = ed.GetCorner("\n[HPKata Viewport] Chọn góc thứ hai của Viewport: ", p1.Value);
            if (p2.Status != PromptStatus.OK) return;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var curBtr = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite);

                double width = Math.Abs(p2.Value.X - p1.Value.X);
                double height = Math.Abs(p2.Value.Y - p1.Value.Y);
                var center = new Point3d(
                    Math.Min(p1.Value.X, p2.Value.X) + width * 0.5,
                    Math.Min(p1.Value.Y, p2.Value.Y) + height * 0.5,
                    0);

                var vp = new Viewport
                {
                    CenterPoint = center,
                    Width = Math.Max(10.0, width),
                    Height = Math.Max(10.0, height),
                    CustomScale = customScale > 0 ? customScale : 0.01
                };

                curBtr.AppendEntity(vp);
                tr.AddNewlyCreatedDBObject(vp, true);
                vp.On = true;

                tr.Commit();
                ed.WriteMessage($"\n[HPKata Viewport] Đã tạo Viewport kích thước {vp.Width:F0}x{vp.Height:F0} mm (Tỷ lệ: {vp.CustomScale:F4}).\n");
            }
        }

        public static void SetViewportLock(bool locked)
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var psr = ed.GetSelection(new SelectionFilter(new[] { new TypedValue(0, "VIEWPORT") }));
            if (psr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\n[HPKata Viewport] Không có Viewport nào được chọn.\n");
                return;
            }

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                int count = 0;
                foreach (SelectedObject so in psr.Value)
                {
                    if (so == null) continue;
                    var vp = tr.GetObject(so.ObjectId, OpenMode.ForWrite) as Viewport;
                    if (vp != null)
                    {
                        vp.Locked = locked;
                        count++;
                    }
                }
                tr.Commit();
                string state = locked ? "Khóa (Locked)" : "Mở khóa (Unlocked)";
                ed.WriteMessage($"\n[HPKata Viewport] Đã {state} {count} Viewport thành công.\n");
            }
        }
    }
}
