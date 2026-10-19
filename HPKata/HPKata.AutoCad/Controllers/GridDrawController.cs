using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPKata.Core.DrawingLayout.Calculators;
using HPKata.Core.DrawingLayout.Models;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller managing drawing of structural grid axes, bubble circles, and labels in ModelSpace.
    /// </summary>
    public static class GridDrawController
    {
        public static void DrawGridInteractive()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata Grid] Chọn điểm gốc hệ lưới trục: ");
            if (ppr.Status != PromptStatus.OK) return;

            var xSpans = new List<double> { 4000, 5000, 6000, 4000 };
            var ySpans = new List<double> { 5000, 6000, 5000 };

            var system = GridAxisGenerator.GenerateOrthogonal(xSpans, ySpans, 100.0);
            DrawGridSystem(doc, ppr.Value, system);

            ed.WriteMessage($"\n[HPKata Grid] Đã vẽ hệ lưới trục ({system.XAxes.Count} trục X, {system.YAxes.Count} trục Y).\n");
        }

        public static void DrawGridSystem(Document doc, Point3d origin, GridSystemModel system)
        {
            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                foreach (var axis in system.XAxes)
                {
                    DrawAxisWithBubbles(tr, ms, origin, axis);
                }

                foreach (var axis in system.YAxes)
                {
                    DrawAxisWithBubbles(tr, ms, origin, axis);
                }

                tr.Commit();
            }
        }

        private static void DrawAxisWithBubbles(
            Transaction tr,
            BlockTableRecord ms,
            Point3d origin,
            GridLineModel axis)
        {
            var p1 = new Point3d(origin.X + axis.StartX, origin.Y + axis.StartY, 0);
            var p2 = new Point3d(origin.X + axis.EndX, origin.Y + axis.EndY, 0);

            var line = new Line(p1, p2);
            ms.AppendEntity(line);
            tr.AddNewlyCreatedDBObject(line, true);

            // Start bubble
            if (axis.BubblePlacement == GridBubbleOrientation.BothEnds ||
                axis.BubblePlacement == GridBubbleOrientation.StartOnly)
            {
                DrawBubbleAtPoint(tr, ms, p1, axis.Label, axis.BubbleRadiusMm, axis.TextHeightMm);
            }

            // End bubble
            if (axis.BubblePlacement == GridBubbleOrientation.BothEnds ||
                axis.BubblePlacement == GridBubbleOrientation.EndOnly)
            {
                DrawBubbleAtPoint(tr, ms, p2, axis.Label, axis.BubbleRadiusMm, axis.TextHeightMm);
            }
        }

        private static void DrawBubbleAtPoint(
            Transaction tr,
            BlockTableRecord ms,
            Point3d center,
            string label,
            double radius,
            double textHeight)
        {
            var circle = new Circle(center, Vector3d.ZAxis, radius);
            ms.AppendEntity(circle);
            tr.AddNewlyCreatedDBObject(circle, true);

            var text = new DBText
            {
                Position = center,
                Height = textHeight,
                TextString = label,
                HorizontalMode = TextHorizontalMode.TextCenter,
                VerticalMode = TextVerticalMode.TextVerticalMid,
                AlignmentPoint = center
            };
            ms.AppendEntity(text);
            tr.AddNewlyCreatedDBObject(text, true);
        }
    }
}
