using System;
using System.Collections.Generic;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPKata.AutoCad.Interop;
using HPKata.Core.QuantitySurveying.Calculators;
using HPKata.Core.QuantitySurveying.Models;
using HPKata.Core.QuantitySurveying.Persistence;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller managing Quantity Surveying (QS) takeoff scanning, intersection deduction,
    /// ModelSpace summary table creation, and Excel workbook export.
    /// </summary>
    public static class QsTakeoffController
    {
        private static readonly List<QsElementModel> SessionElements = new();

        public static void ExecuteMasterWorkflow()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            ScanFramingElements(doc.Editor);
            if (SessionElements.Count == 0) return;

            var summary = QsScheduleAggregator.Aggregate(SessionElements);
            doc.Editor.WriteMessage($"\n[HPKata QS] Đã bóc tách {summary.Items.Count} cấu kiện. Bê tông: {summary.TotalVolumeM3:F2} m³, Cốp pha: {summary.TotalFormworkM2:F2} m²\n");
        }

        public static void GenerateSummaryTable()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var ppr = ed.GetPoint("\n[HPKata QS] Chọn điểm chèn bảng tổng hợp khối lượng (tkqs): ");
            if (ppr.Status != PromptStatus.OK) return;

            if (SessionElements.Count == 0) ScanFramingElements(ed);
            if (SessionElements.Count == 0) return;

            var summary = QsScheduleAggregator.Aggregate(SessionElements);
            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                Table tbl = BuildQsTable(ppr.Value, summary);
                ms.AppendEntity(tbl);
                tr.AddNewlyCreatedDBObject(tbl, true);
                tr.Commit();
                ed.WriteMessage($"\n[HPKata QS] Đã tạo bảng thống kê QS với {summary.Items.Count} hạng mục.\n");
            }
        }

        public static void ExportExcel()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            if (SessionElements.Count == 0) ScanFramingElements(ed);
            if (SessionElements.Count == 0) return;

            var summary = QsScheduleAggregator.Aggregate(SessionElements);
            string tempPath = Path.Combine(Path.GetTempPath(), $"Kata_QS_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            QsWorkbookGenerator.ExportToFile(summary, tempPath);
            ed.WriteMessage($"\n[HPKata QS] Đã xuất file Excel thành công: {tempPath}\n");
        }

        public static void FindElement()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var pso = new PromptStringOptions("\n[HPKata QS] Nhập ký hiệu cấu kiện cần tìm (e.g. C1, D1): ") { AllowSpaces = false };
            var psr = ed.GetString(pso);
            if (psr.Status != PromptStatus.OK) return;

            string targetMark = psr.StringResult.Trim();
            if (string.IsNullOrEmpty(targetMark)) return;

            SearchAndHighlightEntity(doc, ed, targetMark);
        }

        private static void SearchAndHighlightEntity(Document doc, Editor ed, string targetMark)
        {
            int matchCount = 0;
            Entity? firstMatch = null;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                foreach (ObjectId id in ms)
                {
                    if (id.IsNull || id.IsErased) continue;
                    if (tr.GetObject(id, OpenMode.ForRead) is Entity ent && IsEntityMatch(ent, targetMark, tr))
                    {
                        matchCount++;
                        CadHighlightService.HighlightEntity(id, tr);
                        firstMatch ??= ent;
                    }
                }
                if (firstMatch != null) CadHighlightService.ZoomToEntity(ed, firstMatch);
                tr.Commit();
            }

            if (matchCount > 0)
                ed.WriteMessage($"\n[HPKata QS] Tìm thấy {matchCount} đối tượng phù hợp với '{targetMark}'. Đã zoom và highlight trên bản vẽ.\n");
            else
                ed.WriteMessage($"\n[HPKata QS] Không tìm thấy cấu kiện hoặc nhãn nào có ký hiệu '{targetMark}' trên bản vẽ.\n");
        }

        private static bool IsEntityMatch(Entity ent, string targetMark, Transaction tr)
        {
            if (ent is DBText txt && !string.IsNullOrWhiteSpace(txt.TextString))
                return txt.TextString.IndexOf(targetMark, StringComparison.OrdinalIgnoreCase) >= 0;
            if (ent is MText mtxt && !string.IsNullOrWhiteSpace(mtxt.Contents))
                return mtxt.Contents.IndexOf(targetMark, StringComparison.OrdinalIgnoreCase) >= 0;
            if (ent is BlockReference blk)
            {
                if (blk.Name.IndexOf(targetMark, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                foreach (ObjectId attId in blk.AttributeCollection)
                {
                    if (tr.GetObject(attId, OpenMode.ForRead) is AttributeReference att &&
                        att.TextString.IndexOf(targetMark, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }
            return false;
        }

        private static void ScanFramingElements(Editor ed)
        {
            SessionElements.Clear();
            var targetIds = CollectTargetEntityIds(ed);
            if (targetIds.Count == 0) return;

            var boundsMap = new Dictionary<string, Extents3d>();
            int colIdx = 1, beamIdx = 1, slabIdx = 1;

            using (var tr = ed.Document.Database.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in targetIds)
                {
                    if (id.IsNull || id.IsErased) continue;
                    if (tr.GetObject(id, OpenMode.ForRead) is Entity ent)
                    {
                        var model = ExtractModelFromEntity(ent, ref colIdx, ref beamIdx, ref slabIdx, out var bounds);
                        if (model != null)
                        {
                            SessionElements.Add(model);
                            if (bounds.HasValue) boundsMap[model.Id] = bounds.Value;
                        }
                    }
                }
                tr.Commit();
            }

            ResolveIntersections(SessionElements, boundsMap);
            ed.WriteMessage($"\n[HPKata QS] Đã nhận diện {SessionElements.Count} cấu kiện từ bản vẽ.\n");
        }

        private static List<ObjectId> CollectTargetEntityIds(Editor ed)
        {
            var ids = new List<ObjectId>();
            var implied = ed.SelectImplied();
            if (implied.Status == PromptStatus.OK && implied.Value != null && implied.Value.Count > 0)
            {
                ids.AddRange(implied.Value.GetObjectIds());
                return ids;
            }

            var pso = new PromptSelectionOptions { MessageForAdding = "\n[HPKata QS] Chọn các cấu kiện dầm/cột/sàn (hoặc Enter để quét toàn bộ bản vẽ): " };
            var psr = ed.GetSelection(pso);
            if (psr.Status == PromptStatus.OK && psr.Value != null && psr.Value.Count > 0)
            {
                ids.AddRange(psr.Value.GetObjectIds());
                return ids;
            }

            using var tr = ed.Document.Database.TransactionManager.StartTransaction();
            var bt = (BlockTable)tr.GetObject(ed.Document.Database.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
            foreach (ObjectId id in ms) ids.Add(id);
            tr.Commit();
            return ids;
        }

        private static QsElementModel? ExtractModelFromEntity(
            Entity ent, ref int colIdx, ref int beamIdx, ref int slabIdx, out Extents3d? bounds)
        {
            bounds = null;
            string layer = (ent.Layer ?? string.Empty).ToUpperInvariant();

            if (layer.Contains("COT") || layer.Contains("COLUMN") || layer.Contains("TRU"))
            {
                bounds = SafeGetExtents(ent);
                double dx = bounds.HasValue ? Math.Abs(bounds.Value.MaxPoint.X - bounds.Value.MinPoint.X) : 400.0;
                double dy = bounds.HasValue ? Math.Abs(bounds.Value.MaxPoint.Y - bounds.Value.MinPoint.Y) : 400.0;
                return new QsElementModel { Mark = $"C{colIdx++}", MemberType = QsMemberType.Column, WidthMm = Math.Max(200.0, Math.Min(dx, dy)), HeightMm = Math.Max(200.0, Math.Max(dx, dy)), LengthMm = 3600.0 };
            }
            if (layer.Contains("DAM") || layer.Contains("BEAM") || layer.Contains("FRAMING"))
            {
                bounds = SafeGetExtents(ent);
                double len = ent is Curve cv ? cv.GetDistanceAtParameter(cv.EndParam) : 5000.0;
                if (len < 300.0) return null;
                return new QsElementModel { Mark = $"D{beamIdx++}", MemberType = QsMemberType.Beam, WidthMm = 250.0, HeightMm = 500.0, LengthMm = Math.Round(len, 0), IsPrimary = true };
            }
            if (layer.Contains("SAN") || layer.Contains("SLAB"))
            {
                bounds = SafeGetExtents(ent);
                double dx = bounds.HasValue ? Math.Abs(bounds.Value.MaxPoint.X - bounds.Value.MinPoint.X) : 6000.0;
                double dy = bounds.HasValue ? Math.Abs(bounds.Value.MaxPoint.Y - bounds.Value.MinPoint.Y) : 4000.0;
                return new QsElementModel { Mark = $"S{slabIdx++}", MemberType = QsMemberType.Slab, LengthMm = Math.Max(dx, dy), WidthMm = Math.Min(dx, dy), ThicknessMm = 120.0, PlanAreaMm2 = dx * dy };
            }
            return null;
        }

        private static Extents3d? SafeGetExtents(Entity ent)
        {
            try { return ent.GeometricExtents; }
            catch (Exception)
            {
                // GeometricExtents can throw if entity geometry is uncalculated or degenerate
                return null;
            }
        }

        private static void ResolveIntersections(List<QsElementModel> elements, Dictionary<string, Extents3d> bounds)
        {
            for (int i = 0; i < elements.Count; i++)
            {
                for (int j = i + 1; j < elements.Count; j++)
                {
                    var a = elements[i];
                    var b = elements[j];
                    if (!bounds.TryGetValue(a.Id, out var bA) || !bounds.TryGetValue(b.Id, out var bB)) continue;

                    bool overlap = bA.MinPoint.X <= bB.MaxPoint.X && bA.MaxPoint.X >= bB.MinPoint.X &&
                                  bA.MinPoint.Y <= bB.MaxPoint.Y && bA.MaxPoint.Y >= bB.MinPoint.Y;
                    if (overlap) { a.IntersectingElementIds.Add(b.Id); b.IntersectingElementIds.Add(a.Id); }
                }
            }
        }

        private static Table BuildQsTable(Point3d insertPt, QsTakeoffSummary summary)
        {
            var tbl = new Table { Position = insertPt };
            tbl.SetSize(Math.Max(3, summary.Items.Count + 3), 6);
            tbl.Cells[0, 0].TextString = "BẢNG TỔNG HỢP KHỐI LƯỢNG BÊ TÔNG & CỐP PHA (KATA_QS)";
            tbl.Rows[0].Height = 12.0;

            string[] headers = { "STT", "HẠNG MỤC", "CẤU KIỆN", "SỐ LƯỢNG", "BÊ TÔNG (m3)", "CỐP PHA (m2)" };
            for (int c = 0; c < headers.Length; c++) tbl.Cells[1, c].TextString = headers[c];

            for (int i = 0; i < summary.Items.Count; i++)
            {
                var it = summary.Items[i];
                int r = 2 + i;
                tbl.Cells[r, 0].TextString = (i + 1).ToString();
                tbl.Cells[r, 1].TextString = it.ItemMark;
                tbl.Cells[r, 2].TextString = it.MemberType.ToString();
                tbl.Cells[r, 3].TextString = it.Count.ToString();
                tbl.Cells[r, 4].TextString = it.TotalVolumeM3.ToString("F3");
                tbl.Cells[r, 5].TextString = it.FormworkAreaM2.ToString("F2");
            }

            int totalRow = 2 + summary.Items.Count;
            tbl.Cells[totalRow, 1].TextString = "TỔNG CỘNG";
            tbl.Cells[totalRow, 4].TextString = summary.TotalVolumeM3.ToString("F3");
            tbl.Cells[totalRow, 5].TextString = summary.TotalFormworkM2.ToString("F2");
            tbl.GenerateLayout();
            return tbl;
        }
    }
}
