using System;
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
    /// Controller managing dimension editing, witness line trimming (catdim),
    /// dimension splitting (ngatdim), continuous chains (dnoi), and slope annotations (sl).
    /// </summary>
    public static class DimensionUtilityController
    {
        public static void TrimDimensionWitnessLinesInteractive(double offsetMm = 50.0)
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var filter = new SelectionFilter(new[] { new TypedValue(0, "DIMENSION") });
            var psr = ed.GetSelection(filter);
            if (psr.Status != PromptStatus.OK) return;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                int modifiedCount = 0;
                foreach (SelectedObject so in psr.Value)
                {
                    if (so == null) continue;
                    var dim = tr.GetObject(so.ObjectId, OpenMode.ForWrite) as AlignedDimension;
                    if (dim != null)
                    {
                        var p1 = new Point2D(dim.XLine1Point.X, dim.XLine1Point.Y);
                        var p2 = new Point2D(dim.XLine2Point.X, dim.XLine2Point.Y);
                        var pDim = new Point2D(dim.DimLinePoint.X, dim.DimLinePoint.Y);

                        var (new1, new2) = VectorProjectionHelper.TrimWitnessLines(p1, p2, pDim, offsetMm);
                        dim.XLine1Point = new Point3d(new1.X, new1.Y, dim.XLine1Point.Z);
                        dim.XLine2Point = new Point3d(new2.X, new2.Y, dim.XLine2Point.Z);
                        modifiedCount++;
                    }
                }
                tr.Commit();
                ed.WriteMessage($"\n[HPKata Dim] Đã cắt chân {modifiedCount} đường kích thước (offset {offsetMm}mm).\n");
            }
        }

        public static void SplitDimensionInteractive()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var peo = new PromptEntityOptions("\n[HPKata Dim] Chọn đường kích thước cần ngắt (ngatdim): ");
            peo.SetRejectMessage("\nChỉ chọn đối tượng Dimension.");
            peo.AddAllowedClass(typeof(AlignedDimension), true);
            var per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            var ppr = ed.GetPoint("\n[HPKata Dim] Chọn điểm ngắt trên đường kích thước: ");
            if (ppr.Status != PromptStatus.OK) return;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var origDim = (AlignedDimension)tr.GetObject(per.ObjectId, OpenMode.ForWrite);
                var curBtr = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite);

                var splitPt = ppr.Value;
                var dim1 = new AlignedDimension(origDim.XLine1Point, splitPt, origDim.DimLinePoint, "", origDim.DimensionStyle);
                var dim2 = new AlignedDimension(splitPt, origDim.XLine2Point, origDim.DimLinePoint, "", origDim.DimensionStyle);

                curBtr.AppendEntity(dim1);
                curBtr.AppendEntity(dim2);
                tr.AddNewlyCreatedDBObject(dim1, true);
                tr.AddNewlyCreatedDBObject(dim2, true);

                origDim.Erase();
                tr.Commit();
                ed.WriteMessage("\n[HPKata Dim] Đã tách đường kích thước thành 2 đoạn liên tiếp.\n");
            }
        }

        public static void DrawSlopeAnnotationInteractive()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var p1 = ed.GetPoint("\n[HPKata Slope] Chọn điểm đầu đoạn dốc (sl): ");
            if (p1.Status != PromptStatus.OK) return;

            var p2 = ed.GetPoint("\n[HPKata Slope] Chọn điểm cuối đoạn dốc: ");
            if (p2.Status != PromptStatus.OK) return;

            var pt1 = new Point2D(p1.Value.X, p1.Value.Y);
            var pt2 = new Point2D(p2.Value.X, p2.Value.Y);
            var slope = SlopeCalculator.Calculate(pt1, pt2);

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var curBtr = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite);

                var line = new Line(p1.Value, p2.Value);
                curBtr.AppendEntity(line);
                tr.AddNewlyCreatedDBObject(line, true);

                var midPt = new Point3d((p1.Value.X + p2.Value.X) * 0.5, (p1.Value.Y + p2.Value.Y) * 0.5 + 50.0, 0);
                var text = new DBText
                {
                    Position = midPt,
                    Height = 250.0,
                    TextString = slope.FormattedText,
                    Rotation = slope.TextAngleRad
                };
                curBtr.AppendEntity(text);
                tr.AddNewlyCreatedDBObject(text, true);

                tr.Commit();
                ed.WriteMessage($"\n[HPKata Slope] Đã ghi độ dốc: {slope.FormattedText} ({slope.RatioText}).\n");
            }
        }

        public static void ContinueDimensionInteractive()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                if (!TryGetBaseDimensionInfo(doc, ed, tr, out var baseInfo, out bool initialCreated))
                {
                    return;
                }

                var curBtr = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite);
                int createdCount = LoopContinuousDimensions(ed, tr, curBtr, baseInfo);

                if (createdCount > 0 || initialCreated)
                {
                    tr.Commit();
                    ed.WriteMessage($"\n[HPKata Dim] Đã tạo nối tiếp {createdCount} đường kích thước.\n");
                }
            }
        }

        private static bool TryGetBaseDimensionInfo(Document doc, Editor ed, Transaction tr, out BaseDimInfo baseInfo, out bool initialCreated)
        {
            baseInfo = default;
            initialCreated = false;

            var peo = new PromptEntityOptions("\n[HPKata Dim] Chọn đường kích thước cơ sở (hoặc Enter để chọn 2 điểm mới): ")
            {
                AllowNone = true
            };
            peo.SetRejectMessage("\nChỉ chọn đối tượng Dimension.");
            peo.AddAllowedClass(typeof(Dimension), true);
            var per = ed.GetEntity(peo);

            if (per.Status == PromptStatus.OK)
            {
                var ent = tr.GetObject(per.ObjectId, OpenMode.ForRead);
                if (ent is RotatedDimension rotDim)
                {
                    baseInfo = new BaseDimInfo(rotDim.XLine2Point, rotDim.DimLinePoint, rotDim.DimensionStyle, rotDim.Layer, rotDim.Rotation);
                    return true;
                }
                if (ent is AlignedDimension alDim)
                {
                    baseInfo = new BaseDimInfo(alDim.XLine2Point, alDim.DimLinePoint, alDim.DimensionStyle, alDim.Layer);
                    return true;
                }
            }

            if (per.Status == PromptStatus.None)
            {
                return TryPromptNewBaseDimension(doc, ed, tr, out baseInfo, out initialCreated);
            }

            return false;
        }

        private static bool TryPromptNewBaseDimension(Document doc, Editor ed, Transaction tr, out BaseDimInfo baseInfo, out bool initialCreated)
        {
            baseInfo = default;
            initialCreated = false;

            var p1 = ed.GetPoint("\n[HPKata Dim] Chọn điểm gốc thứ nhất: ");
            if (p1.Status != PromptStatus.OK) return false;

            var p2 = ed.GetPoint("\n[HPKata Dim] Chọn điểm gốc thứ hai: ");
            if (p2.Status != PromptStatus.OK) return false;

            var pDim = ed.GetPoint("\n[HPKata Dim] Chọn vị trí đường kích thước: ");
            if (pDim.Status != PromptStatus.OK) return false;

            var curBtr = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite);
            var firstDim = new AlignedDimension(p1.Value, p2.Value, pDim.Value, "", doc.Database.Dimstyle);
            curBtr.AppendEntity(firstDim);
            tr.AddNewlyCreatedDBObject(firstDim, true);

            baseInfo = new BaseDimInfo(p2.Value, pDim.Value, doc.Database.Dimstyle, firstDim.Layer);
            initialCreated = true;
            return true;
        }

        private static int LoopContinuousDimensions(Editor ed, Transaction tr, BlockTableRecord curBtr, BaseDimInfo baseInfo)
        {
            int count = 0;
            var currentStart = baseInfo.StartPt;

            while (true)
            {
                var ppo = new PromptPointOptions("\n[HPKata Dim] Chọn điểm tiếp theo (hoặc Enter/ESC để kết thúc): ")
                {
                    UseBasePoint = true,
                    BasePoint = currentStart
                };
                var ppr = ed.GetPoint(ppo);
                if (ppr.Status != PromptStatus.OK) break;

                var nextPt = ppr.Value;
                if (nextPt.DistanceTo(currentStart) < 1e-4) continue;

                Dimension newDim = baseInfo.Rotation.HasValue
                    ? new RotatedDimension(baseInfo.Rotation.Value, currentStart, nextPt, baseInfo.DimLinePt, "", baseInfo.DimStyleId)
                    : new AlignedDimension(currentStart, nextPt, baseInfo.DimLinePt, "", baseInfo.DimStyleId);

                if (!string.IsNullOrEmpty(baseInfo.Layer))
                {
                    newDim.Layer = baseInfo.Layer;
                }

                curBtr.AppendEntity(newDim);
                tr.AddNewlyCreatedDBObject(newDim, true);
                count++;
                currentStart = nextPt;
            }

            return count;
        }

        private readonly struct BaseDimInfo
        {
            public Point3d StartPt { get; }
            public Point3d DimLinePt { get; }
            public ObjectId DimStyleId { get; }
            public string Layer { get; }
            public double? Rotation { get; }

            public BaseDimInfo(Point3d startPt, Point3d dimLinePt, ObjectId dimStyleId, string layer, double? rotation = null)
            {
                StartPt = startPt;
                DimLinePt = dimLinePt;
                DimStyleId = dimStyleId;
                Layer = layer;
                Rotation = rotation;
            }
        }
    }
}
