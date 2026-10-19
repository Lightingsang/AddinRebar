using System;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPKata.AutoCad.Interop;
using HPKata.Core.Beams.Models;
using HPKata.Core.Configuration;
using HPKata.Core.Excel;
using HPKata.Core.Excel.Mappers;
using HPKata.UI.ViewModels;
using HPKata.UI.Views;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller orchestrating continuous beam parametric drafting (ve_dam, vd, ve_dam_ui, ve_dam_all).
    /// Matches KataPro's direct Excel-to-CAD workflow on 've_dam'/'vd' while offering 've_dam_ui' for interactive UI editing.
    /// </summary>
    public static class BeamDrawController
    {
        public static void DrawContinuousBeamInteractive(bool isShopMode = false, bool autoReadExcel = true)
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            KataCadAssemblyResolver.Initialize();

            if (autoReadExcel && TryLoadBeamFromExcel(out var excelLayout) && excelLayout != null)
            {
                BeamTakeoffRegistry.Register(excelLayout);
                PromptAndDrawBeamLayout(doc, ed, excelLayout, isShopMode);
                return;
            }

            DrawContinuousBeamWithUiDialog(isShopMode, autoReadExcel: true);
        }

        public static void DrawContinuousBeamWithUiDialog(bool isShopMode = false, bool autoReadExcel = true)
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            KataCadAssemblyResolver.Initialize();
            var vm = new BeamDetailingViewModel { IsShopMode = isShopMode };
            if (autoReadExcel)
            {
                vm.ReadExcel();
            }

            var window = new BeamDetailingWindow(vm);
            HPKata.UI.Common.ThemeManager.Attach(window, HPKata.AutoCad.Commands.AutocadHostTheme.Instance);
            AcadApp.ShowModalWindow(window);

            if (!vm.IsConfirmed || vm.ResultLayout == null)
            {
                return;
            }

            BeamTakeoffRegistry.Register(vm.ResultLayout);
            PromptAndDrawBeamLayout(doc, ed, vm.ResultLayout, vm.IsShopMode);
        }

        public static bool TryLoadBeamFromExcel(out BeamRebarLayout? layout, IKataExcelAdapter? adapter = null)
        {
            layout = null;
            var excel = adapter ?? KataExcelAdapterProvider.Current;
            if (!excel.ReadBeamSheet(null, out var sheetData))
            {
                return false;
            }

            layout = KataBeamDataMapper.ToRebarLayout(sheetData);
            return layout.Spans.Count > 0;
        }

        public static void PromptAndDrawBeamLayout(
            Document doc, Editor ed, BeamRebarLayout layout, bool isShopMode)
        {
            var ppr = ed.GetPoint($"\n[HPKata] Chọn điểm đặt bản vẽ chi tiết dầm '{layout.BeamName}': ");
            if (ppr.Status != PromptStatus.OK) return;

            DrawBeamLayoutAtPoint(doc, ppr.Value, layout, isShopMode);
            int sectionCount = layout.Spans.Count * 2;
            ed.WriteMessage(
                $"\n[HPKata] Đã vẽ hoàn chỉnh mặt cắt dọc + {sectionCount} mặt cắt ngang (chuẩn KataPro) cho dầm '{layout.BeamName}' ({layout.Spans.Count} nhịp).\n");
        }

        public static void DrawBeamLayoutAtPoint(
            Document doc, Point3d origin, BeamRebarLayout layout, bool isShopMode)
        {
            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                KataIsoTemplateImporter.EnsureIsoTemplates(doc.Database, tr);
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                BeamShopDrawingBuilder.DrawCompleteBeamShopDrawing(ms, tr, doc.Database, origin, layout, isShopMode);
                tr.Commit();
            }
        }

        public static void DrawContinuousBeamAllBatch()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var allBeams = BeamTakeoffRegistry.GetAll();
            if (allBeams.Count == 0)
            {
                DrawContinuousBeamInteractive();
                return;
            }

            var ppr = ed.GetPoint("\n[HPKata] Chọn điểm gốc vẽ hàng loạt các dầm: ");
            if (ppr.Status != PromptStatus.OK) return;

            Point3d currentOrigin = ppr.Value;
            foreach (var beam in allBeams)
            {
                DrawBeamLayoutAtPoint(doc, currentOrigin, beam, isShopMode: false);
                currentOrigin = new Point3d(currentOrigin.X, currentOrigin.Y - 5200.0, 0);
            }
            ed.WriteMessage($"\n[HPKata] Đã vẽ hàng loạt {allBeams.Count} dầm.\n");
        }
    }
}
