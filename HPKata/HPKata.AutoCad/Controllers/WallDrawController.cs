using System;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPKata.AutoCad.Interop;
using HPKata.Core.Configuration;
using HPKata.Core.Excel;
using HPKata.Core.Excel.Mappers;
using HPKata.Core.Walls.Calculators;
using HPKata.Core.Walls.Models;
using HPKata.Core.Walls.Persistence;
using HPKata.UI.ViewModels;
using HPKata.UI.Views;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller orchestrating shear wall parametric drafting (ve_vach / vv, ve_vach_ui / vvui, loadwall).
    /// Matches KataPro's direct Excel-to-CAD workflow on 've_vach'/'vv' while offering 've_vach_ui' for interactive UI editing.
    /// </summary>
    public static class WallDrawController
    {
        public static void DrawWallInteractive(bool isShopMode = false, bool autoReadExcel = true)
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            KataCadAssemblyResolver.Initialize();

            if (autoReadExcel && TryLoadWallFromExcel(out var excelWall) && excelWall != null)
            {
                WallTakeoffRegistry.Register(excelWall);
                PromptAndDrawWallModel(doc, ed, excelWall, isShopMode);
                return;
            }

            DrawWallWithUiDialog(isShopMode, autoReadExcel: true);
        }

        public static void DrawWallWithUiDialog(bool isShopMode = false, bool autoReadExcel = true)
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            KataCadAssemblyResolver.Initialize();
            var vm = new WallDetailingViewModel();
            if (autoReadExcel)
            {
                vm.ReadExcel();
            }

            var window = new WallDetailingWindow(vm);
            HPKata.UI.Common.ThemeManager.Attach(window, HPKata.AutoCad.Commands.AutocadHostTheme.Instance);
            AcadApp.ShowModalWindow(window);

            if (vm.ResultModel == null)
            {
                return;
            }

            WallTakeoffRegistry.Register(vm.ResultModel);
            PromptAndDrawWallModel(doc, ed, vm.ResultModel, isShopMode);
        }

        public static bool TryLoadWallFromExcel(out ShearWallModel? wall, IKataExcelAdapter? adapter = null)
        {
            wall = null;
            var excel = adapter ?? KataExcelAdapterProvider.Current;
            if (!excel.ReadWallSheet(null, out var sheetData))
            {
                return false;
            }

            wall = KataWallDataMapper.ToShearWallModel(sheetData);
            return wall.Stories.Count > 0;
        }

        public static void PromptAndDrawWallModel(
            Document doc, Editor ed, ShearWallModel wall, bool isShopMode = false)
        {
            var ppr = ed.GetPoint($"\n[HPKata] Chọn điểm đặt bản vẽ chi tiết vách '{wall.WallMark}': ");
            if (ppr.Status != PromptStatus.OK) return;

            DrawWallLayoutAtPoint(doc, ppr.Value, wall, isShopMode);
            ed.WriteMessage(
                $"\n[HPKata] Đã vẽ hoàn chỉnh mặt cắt dọc + mặt cắt ngang 1-1..3-3 (chuẩn KataPro) cho vách '{wall.WallMark}' ({wall.Stories.Count} tầng).\n");
        }

        public static void DrawWallLayoutAtPoint(
            Document doc, Point3d origin, ShearWallModel wall, bool isShopMode = false)
        {
            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                KataIsoTemplateImporter.EnsureIsoTemplates(doc.Database, tr);
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                WallShopDrawingBuilder.DrawCompleteWallShopDrawing(ms, tr, doc.Database, origin, wall);
                tr.Commit();
            }
        }

        public static ShearWallModel CreateDefaultWallModel()
        {
            if (TryLoadWallFromExcel(out var excelWall) && excelWall != null)
            {
                return excelWall;
            }

            var wall = new ShearWallModel
            {
                WallMark = "V1",
                ThicknessMm = 600.0,
                LengthMm = 3000.0
            };

            string[] names = { "PILECAPS", "BASEMENT", "FLOOR 1", "FLOOR 2", "FLOOR 3", "FLOOR 4" };
            double[] heights = { 1500.0, 3000.0, 3500.0, 3500.0, 4000.0, 4000.0 };
            double elev = 0.0;
            for (int i = 0; i < names.Length; i++)
            {
                wall.Stories.Add(new WallStorySegment
                {
                    StoryIndex = i,
                    StoryName = names[i],
                    BottomElevationMm = elev,
                    StoryHeightMm = heights[i],
                    ThicknessMm = 600.0,
                    LengthMm = 3000.0
                });
                elev += heights[i];
            }

            wall.BoundaryElements.Add(WallBoundaryCalculator.CreateBoundaryElement("BE-L", WallBoundaryPosition.Left, 3000.0, 600.0));
            wall.BoundaryElements.Add(WallBoundaryCalculator.CreateBoundaryElement("BE-R", WallBoundaryPosition.Right, 3000.0, 600.0));

            wall.WebReinforcement = new WallWebReinforcementModel
            {
                IsDoubleCurtain = true,
                VerticalBarDiameterMm = 18,
                VerticalBarSpacingMm = 150.0,
                HorizontalBarDiameterMm = 10,
                HorizontalBarSpacingMm = 200.0,
                CrossTieDiameterMm = 8
            };

            return wall;
        }
    }
}
