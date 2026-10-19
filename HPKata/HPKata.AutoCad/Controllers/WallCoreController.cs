using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPKata.Core.Walls.Models;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller orchestrating elevator core wall drafting (ve_loi_vach / vlv)
    /// including C-shaped, U-shaped, and closed box elevator core walls.
    /// </summary>
    public static class WallCoreController
    {
        public static void DrawWallCoreInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata] Pick insertion point for elevator core wall (ve_loi_vach): ");
            if (ppr.Status != PromptStatus.OK) return;

            Point3d origin = ppr.Value;
            var coreWall = CreateDefaultCoreModel();
            WallTakeoffRegistry.Register(coreWall);

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                DrawCoreProfile(ms, tr, origin, coreWall);

                tr.Commit();
                ed.WriteMessage($"\n[HPKata] Successfully drafted elevator core '{coreWall.WallMark}' ({coreWall.ShapeType}).\n");
            }
        }

        public static ShearWallModel CreateDefaultCoreModel()
        {
            var wall = new ShearWallModel
            {
                WallMark = "CORE-1",
                ShapeType = WallShapeType.C_Shape,
                ThicknessMm = 300.0,
                LengthMm = 2600.0
            };

            wall.Stories.Add(new WallStorySegment
            {
                StoryIndex = 0,
                StoryName = "Tầng 1",
                BottomElevationMm = 0.0,
                StoryHeightMm = 3600.0,
                ThicknessMm = 300.0,
                LengthMm = 2600.0
            });

            wall.BoundaryElements.Add(new WallBoundaryElementModel
            {
                Mark = "CORNER-1",
                Position = WallBoundaryPosition.WebCorner,
                LengthBbMm = 500.0,
                ThicknessTbMm = 300.0,
                VerticalBarCount = 8,
                VerticalBarDiameterMm = 20,
                TieDiameterMm = 8,
                TieSpacingMm = 100.0
            });

            return wall;
        }

        private static void DrawCoreProfile(
            BlockTableRecord ms, Transaction tr, Point3d origin, ShearWallModel core)
        {
            double w = core.LengthMm;
            double flange = 2000.0;
            double tw = core.ThicknessMm;

            // Outer C-shape boundary
            var outerPoly = new Polyline(8);
            outerPoly.AddVertexAt(0, new Point2d(origin.X, origin.Y), 0, 0, 0);
            outerPoly.AddVertexAt(1, new Point2d(origin.X + w, origin.Y), 0, 0, 0);
            outerPoly.AddVertexAt(2, new Point2d(origin.X + w, origin.Y + flange), 0, 0, 0);
            outerPoly.AddVertexAt(3, new Point2d(origin.X + w - tw, origin.Y + flange), 0, 0, 0);
            outerPoly.AddVertexAt(4, new Point2d(origin.X + w - tw, origin.Y + tw), 0, 0, 0);
            outerPoly.AddVertexAt(5, new Point2d(origin.X + tw, origin.Y + tw), 0, 0, 0);
            outerPoly.AddVertexAt(6, new Point2d(origin.X + tw, origin.Y + flange), 0, 0, 0);
            outerPoly.AddVertexAt(7, new Point2d(origin.X, origin.Y + flange), 0, 0, 0);
            outerPoly.Closed = true;

            ms.AppendEntity(outerPoly);
            tr.AddNewlyCreatedDBObject(outerPoly, true);
        }
    }
}
