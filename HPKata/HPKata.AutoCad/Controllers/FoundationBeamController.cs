using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPKata.Core.Beams.Models;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller orchestrating foundation grade beams and ground tie beams (vdm, vgm, vgms).
    /// </summary>
    public static class FoundationBeamController
    {
        public static void DrawFoundationGradeBeam()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata] Pick insertion point for foundation grade beam (ve_dam_mong): ");
            if (ppr.Status != PromptStatus.OK) return;

            DrawFoundationBeamInternal(doc, ppr.Value, FoundationBeamType.FoundationGradeBeam, isShop: false);
        }

        public static void DrawFoundationTieBeam()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata] Pick insertion point for ground tie beam (ve_giang_mong): ");
            if (ppr.Status != PromptStatus.OK) return;

            DrawFoundationBeamInternal(doc, ppr.Value, FoundationBeamType.FoundationTieBeam, isShop: false);
        }

        public static void DrawFoundationTieBeamShop()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata] Pick insertion point for tie beam shop drawing (ve_giang_shop): ");
            if (ppr.Status != PromptStatus.OK) return;

            DrawFoundationBeamInternal(doc, ppr.Value, FoundationBeamType.FoundationTieBeam, isShop: true);
        }

        private static void DrawFoundationBeamInternal(
            Document doc, Point3d origin, FoundationBeamType type, bool isShop)
        {
            double span = 4500.0;
            double b = 300.0;
            double h = 500.0;
            double cover = 50.0; // Foundation thick cover

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // Outline
                Polyline outline = new Polyline();
                outline.AddVertexAt(0, new Point2d(origin.X, origin.Y), 0, 0, 0);
                outline.AddVertexAt(1, new Point2d(origin.X + span, origin.Y), 0, 0, 0);
                outline.AddVertexAt(2, new Point2d(origin.X + span, origin.Y - h), 0, 0, 0);
                outline.AddVertexAt(3, new Point2d(origin.X, origin.Y - h), 0, 0, 0);
                outline.Closed = true;
                ms.AppendEntity(outline);
                tr.AddNewlyCreatedDBObject(outline, true);

                // Top rebar with 90 deg footing hook
                Polyline topBar = new Polyline();
                topBar.AddVertexAt(0, new Point2d(origin.X, origin.Y - cover - 200.0), 0, 0, 0);
                topBar.AddVertexAt(1, new Point2d(origin.X, origin.Y - cover), 0, 0, 0);
                topBar.AddVertexAt(2, new Point2d(origin.X + span, origin.Y - cover), 0, 0, 0);
                topBar.AddVertexAt(3, new Point2d(origin.X + span, origin.Y - cover - 200.0), 0, 0, 0);
                ms.AppendEntity(topBar);
                tr.AddNewlyCreatedDBObject(topBar, true);

                // Bottom rebar with 90 deg footing hook
                Polyline botBar = new Polyline();
                botBar.AddVertexAt(0, new Point2d(origin.X, origin.Y - h + cover + 200.0), 0, 0, 0);
                botBar.AddVertexAt(1, new Point2d(origin.X, origin.Y - h + cover), 0, 0, 0);
                botBar.AddVertexAt(2, new Point2d(origin.X + span, origin.Y - h + cover), 0, 0, 0);
                botBar.AddVertexAt(3, new Point2d(origin.X + span, origin.Y - h + cover + 200.0), 0, 0, 0);
                ms.AppendEntity(botBar);
                tr.AddNewlyCreatedDBObject(botBar, true);

                tr.Commit();
                string label = type == FoundationBeamType.FoundationGradeBeam ? "Grade Beam" : "Tie Beam";
                if (isShop) label += " (Shop 11.7m)";
                doc.Editor.WriteMessage($"\n[HPKata] Drafted {label} {b:F0}x{h:F0} L={span:F0} mm.\n");
            }
        }
    }
}
