using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPKata.AutoCad.Interop;
using HPKata.Core.Serialization;
using HPKata.UI.Common;
using HPKata.UI.ViewModels;
using HPKata.UI.Views;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// Controller for editing rebar properties, hooks, lap cuts, dimensions, and colors.
    /// </summary>
    public static class RebarEditController
    {
        public static void MultiplyBarCount()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var opt = new PromptIntegerOptions("\nEnter member multiplier count <1>: ")
            {
                DefaultValue = 1,
                UseDefaultValue = true
            };
            PromptIntegerResult res = ed.GetInteger(opt);
            if (res.Status != PromptStatus.OK || res.Value <= 0) return;
            int count = res.Value;

            List<ObjectId> ids = CadSelectionService.PromptSelectMultiple(ed, "Select rebars to multiply: ");
            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ids)
                {
                    if (tr.GetObject(id, OpenMode.ForWrite) is Entity ent)
                    {
                        KataXDataRecord r = XDataService.ReadXData(ent);
                        r.SoLuongCauKien = (short)count;
                        XDataService.WriteXData(ent, r);
                    }
                }
                tr.Commit();
                ed.WriteMessage($"\n[HPKata] Updated member multiplier to {count} for {ids.Count} items.\n");
            }
        }

        public static void EditRebarInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            ObjectId? selId = CadSelectionService.PromptSelectEntity(ed, "Select rebar to edit: ", typeof(Polyline));
            if (!selId.HasValue) return;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var pl = (Polyline)tr.GetObject(selId.Value, OpenMode.ForWrite);
                KataXDataRecord record = XDataService.ReadXData(pl);

                var vm = new DetailingViewModel();
                if (record.Phi > 0) vm.Diameter = record.Phi;
                if (record.STT > 0) vm.BarMark = record.STT.ToString();
                if (!string.IsNullOrEmpty(record.TenCauKien)) vm.ComponentMark = record.TenCauKien;
                if (!string.IsNullOrEmpty(record.KhoangCach)) vm.Spacing = record.KhoangCach;
                if (record.SoLuongCauKien > 0) vm.MemberCount = record.SoLuongCauKien;
                if (int.TryParse(record.SoLuong, out int parsedCount)) vm.CountPerMember = parsedCount;

                var view = new DetailingView { DataContext = vm };
                ThemeManager.Attach(view, AutocadHostTheme.Instance);

                bool? dialogRes = AcadApp.ShowModalWindow(view);
                if (dialogRes == true)
                {
                    record.Phi = (short)vm.Diameter;
                    record.STT = short.TryParse(vm.BarMark, out short s) ? s : record.STT;
                    record.TenCauKien = vm.ComponentMark;
                    record.KhoangCach = vm.Spacing;
                    record.SoLuong = vm.CountPerMember.ToString();
                    record.SoLuongCauKien = (short)vm.MemberCount;
                    XDataService.WriteXData(pl, record);
                    RebarUpdateService.UpdateRebarAndLinkedTag(pl, tr, false);
                    ed.WriteMessage("\n[HPKata] Rebar properties updated.\n");
                }
                tr.Commit();
            }
        }

        public static void ToggleStirrupHook() => SetStirrupHookAngle(135);

        public static void SetStirrupHookAngle(int angle)
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            List<ObjectId> ids = CadSelectionService.PromptSelectMultiple(ed, "Select stirrup rebars: ");
            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ids)
                {
                    if (tr.GetObject(id, OpenMode.ForWrite) is Polyline pl)
                    {
                        KataXDataRecord r = XDataService.ReadXData(pl);
                        r.GocUon = angle.ToString();
                        XDataService.WriteXData(pl, r);
                    }
                }
                tr.Commit();
                ed.WriteMessage($"\n[HPKata] Updated hook angle to {angle}° for {ids.Count} stirrups.\n");
            }
        }

        public static void AddAnchorHook()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            ObjectId? selId = CadSelectionService.PromptSelectEntity(ed, "Select rebar to add anchor hook: ", typeof(Polyline));
            if (!selId.HasValue) return;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var pl = (Polyline)tr.GetObject(selId.Value, OpenMode.ForWrite);
                KataXDataRecord r = XDataService.ReadXData(pl);
                Point2d lastPt = pl.GetPoint2dAt(pl.NumberOfVertices - 1);
                Point2d prevPt = pl.GetPoint2dAt(pl.NumberOfVertices - 2);
                Vector2d dir = (lastPt - prevPt).GetNormal();
                Vector2d perp = new(-dir.Y, dir.X);

                double hookLen = Math.Max(200.0, r.Phi * 10.0);
                Point2d hookEnd = lastPt + (perp * hookLen);

                pl.AddVertexAt(pl.NumberOfVertices, hookEnd, 0.0, 0.0, 0.0);
                r.ChieuDaiCacDoanMm = RebarGeometryService.MeasureSegmentLengths(pl).ToArray();
                XDataService.WriteXData(pl, r);
                RebarUpdateService.UpdateRebarAndLinkedTag(pl, tr, false);
                tr.Commit();
                ed.WriteMessage($"\n[HPKata] Added {hookLen:F0}mm anchor hook.\n");
            }
        }

        public static void SplitRebarWithLap() => ExecuteSplit(withLap: true);

        public static void SplitRebarArbitrary() => ExecuteSplit(withLap: false);

        private static void ExecuteSplit(bool withLap)
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            ObjectId? selId = CadSelectionService.PromptSelectEntity(ed, "Select rebar polyline to cut: ", typeof(Polyline));
            if (!selId.HasValue) return;

            Point3d? cutPt = CadSelectionService.PromptPoint(ed, "Pick cut point along polyline: ");
            if (!cutPt.HasValue) return;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var pl = (Polyline)tr.GetObject(selId.Value, OpenMode.ForWrite);
                KataXDataRecord r = XDataService.ReadXData(pl);

                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                double lapLen = withLap ? Math.Max(600.0, r.Phi * 30.0) : 0.0;
                var pt2d = new Point2d(cutPt.Value.X, cutPt.Value.Y);

                var pl2 = new Polyline();
                pl2.AddVertexAt(0, pt2d, 0.0, 0.0, 0.0);
                pl2.AddVertexAt(1, pl.GetPoint2dAt(pl.NumberOfVertices - 1), 0.0, 0.0, 0.0);
                pl2.Layer = pl.Layer;

                var r2 = new KataXDataRecord
                {
                    Dang = r.Dang,
                    Phi = r.Phi,
                    STT = r.STT,
                    SoLuong = r.SoLuong,
                    SoLuongCauKien = r.SoLuongCauKien
                };
                XDataService.WriteXData(pl2, r2);

                ms.AppendEntity(pl2);
                tr.AddNewlyCreatedDBObject(pl2, true);
                tr.Commit();
                ed.WriteMessage(withLap ? $"\n[HPKata] Split rebar with {lapLen:F0}mm lap.\n" : "\n[HPKata] Split rebar at cut point.\n");
            }
        }

        public static void DimensionRebarSegments()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            ObjectId? selId = CadSelectionService.PromptSelectEntity(ed, "Select rebar to dimension: ", typeof(Polyline));
            if (!selId.HasValue) return;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var pl = (Polyline)tr.GetObject(selId.Value, OpenMode.ForRead);
                List<double> lengths = RebarGeometryService.MeasureSegmentLengths(pl);
                ed.WriteMessage($"\n[HPKata] Rebar segments: {string.Join(" + ", lengths)} = {pl.Length:F0} mm.\n");
                tr.Commit();
            }
        }

        public static void ColorByDiameter()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            List<ObjectId> ids = CadSelectionService.PromptSelectMultiple(ed, "Select rebars to color-by-diameter: ");
            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ids)
                {
                    if (tr.GetObject(id, OpenMode.ForWrite) is Entity ent)
                    {
                        KataXDataRecord r = XDataService.ReadXData(ent);
                        ent.Color = GetDiameterColor(r.Phi);
                    }
                }
                tr.Commit();
                ed.WriteMessage($"\n[HPKata] Colored {ids.Count} rebars by diameter.\n");
            }
        }

        private static Color GetDiameterColor(int phi) => phi switch
        {
            <= 10 => Color.FromColorIndex(ColorMethod.ByAci, 1), // Red
            <= 14 => Color.FromColorIndex(ColorMethod.ByAci, 2), // Yellow
            <= 18 => Color.FromColorIndex(ColorMethod.ByAci, 3), // Green
            <= 22 => Color.FromColorIndex(ColorMethod.ByAci, 4), // Cyan
            <= 25 => Color.FromColorIndex(ColorMethod.ByAci, 5), // Blue
            _ => Color.FromColorIndex(ColorMethod.ByAci, 6)      // Magenta
        };
    }
}
