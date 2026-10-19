using System;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPKata.AutoCad.Interop;
using HPKata.Core.Columns.Models;
using HPKata.Core.Columns.Persistence;
using HPKata.Core.Configuration;
using HPKata.Core.Excel;
using HPKata.Core.Excel.Mappers;
using HPKata.UI.ViewModels;
using HPKata.UI.Views;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller orchestrating column parametric drafting (ve_cot / vc, ve_cot_ui / vcui, loadcolumn).
    /// Matches KataPro's direct Excel-to-CAD workflow on 've_cot'/'vc' while offering 've_cot_ui' for interactive UI editing.
    /// </summary>
    public static class ColumnDrawController
    {
        public static void DrawColumnInteractive(bool isShopMode = false, bool autoReadExcel = true)
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            KataCadAssemblyResolver.Initialize();

            if (autoReadExcel && TryLoadColumnFromExcel(out var excelLayout) && excelLayout != null)
            {
                ColumnTakeoffRegistry.Register(excelLayout);
                PromptAndDrawColumnLayout(doc, ed, excelLayout, isShopMode);
                return;
            }

            DrawColumnWithUiDialog(isShopMode, autoReadExcel: true);
        }

        public static void DrawColumnWithUiDialog(bool isShopMode = false, bool autoReadExcel = true)
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            KataCadAssemblyResolver.Initialize();
            var vm = new ColumnDetailingViewModel { IsShopMode = isShopMode };
            if (autoReadExcel)
            {
                vm.ReadExcel();
            }

            var window = new ColumnDetailingWindow(vm);
            HPKata.UI.Common.ThemeManager.Attach(window, HPKata.AutoCad.Commands.AutocadHostTheme.Instance);
            AcadApp.ShowModalWindow(window);

            if (vm.ResultLayout == null)
            {
                return;
            }

            ColumnTakeoffRegistry.Register(vm.ResultLayout);
            PromptAndDrawColumnLayout(doc, ed, vm.ResultLayout, vm.IsShopMode);
        }

        public static bool TryLoadColumnFromExcel(out ColumnRebarLayout? layout, IKataExcelAdapter? adapter = null)
        {
            layout = null;
            var excel = adapter ?? KataExcelAdapterProvider.Current;
            if (!excel.ReadColumnSheet(null, out var sheetData))
            {
                return false;
            }

            layout = KataColumnDataMapper.ToRebarLayout(sheetData);
            return layout.Stories.Count > 0;
        }

        public static void PromptAndDrawColumnLayout(
            Document doc, Editor ed, ColumnRebarLayout layout, bool isShopMode = false)
        {
            var ppr = ed.GetPoint($"\n[HPKata] Chọn điểm đặt bản vẽ chi tiết cột '{layout.ColumnMark}': ");
            if (ppr.Status != PromptStatus.OK) return;

            DrawColumnLayoutAtPoint(doc, ppr.Value, layout, isShopMode);
            ed.WriteMessage(
                $"\n[HPKata] Đã vẽ hoàn chỉnh mặt cắt dọc + mặt cắt ngang 1-1..3-3 (chuẩn KataPro) cho cột '{layout.ColumnMark}' ({layout.Stories.Count} tầng).\n");
        }

        public static void DrawColumnLayoutAtPoint(
            Document doc, Point3d origin, ColumnRebarLayout layout, bool isShopMode = false)
        {
            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                KataIsoTemplateImporter.EnsureIsoTemplates(doc.Database, tr);
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                ColumnShopDrawingBuilder.DrawCompleteColumnShopDrawing(ms, tr, doc.Database, origin, layout);
                tr.Commit();
            }
        }

        public static ColumnRebarLayout CreateDefaultLayout()
        {
            if (TryLoadColumnFromExcel(out var excelLayout) && excelLayout != null)
            {
                return excelLayout;
            }

            var layout = new ColumnRebarLayout { ColumnMark = "C1" };
            string[] names = { "PILECAPS", "BASEMENT", "FLOOR 1", "FLOOR 2", "FLOOR 3", "FLOOR 4" };
            double[] heights = { 1500.0, 3000.0, 3500.0, 3500.0, 4000.0, 4000.0 };
            double[] bWidths = { 1000.0, 1000.0, 900.0, 800.0, 700.0, 700.0 };
            double[] hDepths = { 800.0, 800.0, 800.0, 700.0, 700.0, 600.0 };
            double elev = 0.0;
            for (int i = 0; i < names.Length; i++)
            {
                var seg = CreateStorySegment(i, names[i], elev, heights[i], 600.0, bWidths[i], hDepths[i]);
                layout.Stories.Add(seg);
                layout.StoryTies[i] = seg.TieDistribution;
                elev += heights[i];
            }
            return layout;
        }

        private static ColumnStorySegment CreateStorySegment(
            int index, string name, double botElev, double height, double beamDepth, double b, double h)
        {
            return new ColumnStorySegment
            {
                StoryIndex = index,
                StoryName = name,
                BottomElevationMm = botElev,
                StoryHeightMm = height,
                BeamDepthMm = beamDepth,
                WidthMm = b,
                DepthMm = h,
                Section = new ColumnSectionModel
                {
                    WidthMm = b,
                    DepthMm = h,
                    CornerDiameterMm = 25,
                    SideCountB = 5,
                    SideDiameterB = 25,
                    SideCountH = 5,
                    SideDiameterH = 25
                },
                TieDistribution = new ColumnTieDistributionModel
                {
                    TieDiameterMm = 10,
                    DenseSpacingMm = 100.0,
                    SparseSpacingMm = 200.0,
                    ConfinementHeightMm = 600.0
                }
            };
        }
    }
}
