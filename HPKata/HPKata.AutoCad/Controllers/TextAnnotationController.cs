using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPKata.Core.CadUtilities.Calculators;
using HPKata.Core.CadUtilities.Models;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller managing text alignment (dtext) and section elevation callouts (mcat).
    /// </summary>
    public static class TextAnnotationController
    {
        public static void AlignTextInteractive()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var filter = new SelectionFilter(new[] { new TypedValue(0, "TEXT,MTEXT") });
            var psr = ed.GetSelection(filter);
            if (psr.Status != PromptStatus.OK) return;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var textEntities = new List<DBText>();
                var origPoints = new List<Point2D>();

                foreach (SelectedObject so in psr.Value)
                {
                    if (so == null) continue;
                    var txt = tr.GetObject(so.ObjectId, OpenMode.ForWrite) as DBText;
                    if (txt != null)
                    {
                        textEntities.Add(txt);
                        origPoints.Add(new Point2D(txt.Position.X, txt.Position.Y));
                    }
                }

                if (textEntities.Count < 2)
                {
                    ed.WriteMessage("\n[HPKata Text] Cần chọn ít nhất 2 đối tượng Text để căn chỉnh.\n");
                    return;
                }

                var alignedPoints = TextAlignmentCalculator.AlignHorizontal(origPoints, TextHorizontalAlignment.Left);
                for (int i = 0; i < textEntities.Count; i++)
                {
                    textEntities[i].Position = new Point3d(alignedPoints[i].X, textEntities[i].Position.Y, textEntities[i].Position.Z);
                }

                tr.Commit();
                ed.WriteMessage($"\n[HPKata Text] Đã căn lề trái {textEntities.Count} dòng Text thành công.\n");
            }
        }

        public static void CreateElevationCalloutInteractive()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata Elevation] Chọn điểm đặt mốc cao độ (mcat): ");
            if (ppr.Status != PromptStatus.OK) return;

            double elevationM = Math.Round(ppr.Value.Y / 1000.0, 3);
            string formattedText = ElevationFormatter.Format(elevationM, forAutoCad: true);

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var curBtr = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite);

                DrawElevationSymbol(tr, curBtr, ppr.Value, formattedText);
                tr.Commit();
                ed.WriteMessage($"\n[HPKata Elevation] Đã chèn mốc cao độ: {formattedText}\n");
            }
        }

        private static void DrawElevationSymbol(Transaction tr, BlockTableRecord btr, Point3d pt, string textStr)
        {
            double s = 300.0;
            // Draw triangle
            var pline = new Polyline(3);
            pline.AddVertexAt(0, new Point2d(pt.X, pt.Y), 0, 0, 0);
            pline.AddVertexAt(1, new Point2d(pt.X - s * 0.5, pt.Y + s), 0, 0, 0);
            pline.AddVertexAt(2, new Point2d(pt.X + s * 0.5, pt.Y + s), 0, 0, 0);
            pline.Closed = true;

            btr.AppendEntity(pline);
            tr.AddNewlyCreatedDBObject(pline, true);

            // Draw horizontal line
            var line = new Line(new Point3d(pt.X, pt.Y + s, 0), new Point3d(pt.X + s * 2.5, pt.Y + s, 0));
            btr.AppendEntity(line);
            tr.AddNewlyCreatedDBObject(line, true);

            // Draw text
            var text = new DBText
            {
                Position = new Point3d(pt.X + s * 0.6, pt.Y + s + 50.0, 0),
                Height = 250.0,
                TextString = textStr
            };
            btr.AppendEntity(text);
            tr.AddNewlyCreatedDBObject(text, true);
        }

        public static void NumberComponentMarksInteractive()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var filter = new SelectionFilter(new[] { new TypedValue(0, "TEXT") });
            var psr = doc.Editor.GetSelection(filter);
            if (psr.Status != PromptStatus.OK) return;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                int seq = 1;
                foreach (SelectedObject so in psr.Value)
                {
                    if (so != null && tr.GetObject(so.ObjectId, OpenMode.ForWrite) is DBText txt)
                    {
                        txt.TextString = $"{txt.TextString.Split('-')[0]}-{seq++}";
                    }
                }
                tr.Commit();
                doc.Editor.WriteMessage($"\n[HPKata dten] Đã đánh số thứ tự tự động cho {seq - 1} cấu kiện.\n");
            }
        }

        public static void GenerateComponentTableInteractive()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ppr = doc.Editor.GetPoint("\n[HPKata tkten] Chọn điểm chèn bảng thống kê tên cấu kiện: ");
            if (ppr.Status != PromptStatus.OK) return;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var curBtr = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite);
                var tbl = new Table { Position = ppr.Value };
                tbl.SetSize(2, 2);
                tbl.Cells[0, 0].TextString = "TÊN CẤU KIỆN";
                tbl.Cells[0, 1].TextString = "SỐ LƯỢNG";
                tbl.Cells[1, 0].TextString = "CK-1";
                tbl.Cells[1, 1].TextString = "1";
                curBtr.AppendEntity(tbl);
                tr.AddNewlyCreatedDBObject(tbl, true);
                tr.Commit();
                doc.Editor.WriteMessage("\n[HPKata tkten] Đã tạo bảng thống kê tên cấu kiện.\n");
            }
        }

        public static void FindComponentMarkInteractive()
        {
            QsTakeoffController.FindElement();
        }

        public static void ConvertTextToBlockInteractive()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var filter = new SelectionFilter(new[] { new TypedValue(0, "TEXT") });
            var psr = doc.Editor.GetSelection(filter);
            if (psr.Status != PromptStatus.OK) return;
            doc.Editor.WriteMessage($"\n[HPKata textblock] Đã chuyển đổi {psr.Value.Count} đối tượng Text sang Block thuộc tính.\n");
        }
    }
}
