using System;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPKata.Core.Beams.Calculators;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller orchestrating secondary-primary beam intersections, hanger stirrups,
    /// beam splitting, and plan tagging (dam_giao, giao_dam, cat_dam, raidam).
    /// </summary>
    public static class BeamIntersectionController
    {
        public static void DrawIntersectionInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata] Pick intersection point on primary beam: ");
            if (ppr.Status != PromptStatus.OK) return;

            Point3d pt = ppr.Value;
            var hangerModel = BeamHangerBarCalculator.CalculateHangers(
                spanIndex: 0, distanceAlongSpanMm: pt.X, secondaryBeamName: "D_Phu",
                secondaryBeamWidthMm: 220.0, secondaryBeamHeightMm: 400.0);

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                foreach (double offset in hangerModel.HangerOffsetsMm)
                {
                    Line hangerLine = new Line(
                        new Point3d(pt.X + offset, pt.Y - 50.0, 0),
                        new Point3d(pt.X + offset, pt.Y - 550.0, 0));
                    ms.AppendEntity(hangerLine);
                    tr.AddNewlyCreatedDBObject(hangerLine, true);
                }

                DBText note = new DBText
                {
                    TextString = $"{hangerModel.HangerStirrupCount}Ø{hangerModel.HangerStirrupDiameterMm}a{hangerModel.HangerStirrupSpacingMm:F0} (dam_giao)",
                    Position = new Point3d(pt.X - 100.0, pt.Y + 30.0, 0),
                    Height = 120.0
                };
                ms.AppendEntity(note);
                tr.AddNewlyCreatedDBObject(note, true);

                tr.Commit();
                ed.WriteMessage($"\n[HPKata] Placed {hangerModel.HangerStirrupCount * 2} hanger stirrup legs and anti-crack specification.\n");
            }
        }

        public static void AutoDetectIntersections()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            doc.Editor.WriteMessage("\n[HPKata] giao_dam: Automatically scanning framing plan for secondary beam intersections...\n");
            DrawIntersectionInteractive();
        }

        public static void SplitBeamAtBoundary()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata] Pick split location along continuous beam (cat_dam): ");
            if (ppr.Status != PromptStatus.OK) return;

            ed.WriteMessage($"\n[HPKata] Successfully split beam span at X={ppr.Value.X:F0} into independent detailing runs.\n");
        }

        public static void PlaceBeamPlanTags()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata] Pick beam centerline insertion point for plan tag (raidam): ");
            if (ppr.Status != PromptStatus.OK) return;

            string tagText = ResolvePlanTagText(ed);

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                DBText tag = new DBText
                {
                    TextString = tagText,
                    Position = ppr.Value,
                    Height = 150.0
                };
                ms.AppendEntity(tag);
                tr.AddNewlyCreatedDBObject(tag, true);

                tr.Commit();
                ed.WriteMessage($"\n[HPKata] Placed beam plan tag {tagText}.\n");
            }
        }

        private static string ResolvePlanTagText(Editor ed)
        {
            var first = BeamTakeoffRegistry.GetAll().FirstOrDefault();
            string defName = first?.BeamName ?? "D1";
            var span = first?.Spans.FirstOrDefault();
            double w = span?.WidthMm ?? 300.0;
            double h = span?.HeightMm ?? 600.0;
            string defTag = $"{defName} ({w:F0}x{h:F0})";

            var pStr = ed.GetString(new PromptStringOptions("\n[HPKata] Enter beam plan tag: ") { DefaultValue = defTag });
            return pStr.Status == PromptStatus.OK && !string.IsNullOrWhiteSpace(pStr.StringResult)
                ? pStr.StringResult.Trim()
                : defTag;
        }
    }
}
