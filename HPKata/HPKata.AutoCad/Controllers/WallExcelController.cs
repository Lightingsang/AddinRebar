using System;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using HPKata.Core.Configuration;
using HPKata.Core.Excel;
using HPKata.Core.Excel.Mappers;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller handling 'loadwall' command loading shear wall detailing models from Kata.xlsm sheet 'Vach'
    /// and drawing directly in AutoCAD when an interactive editor is active.
    /// </summary>
    public static class WallExcelController
    {
        public static void LoadWallInteractive(IKataExcelAdapter? adapter = null)
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            string path = KataInstallEnvironment.Current.KataWorkbookPath;
            if (!File.Exists(path) && File.Exists(@"C:\HPKata\Kata.xlsm"))
            {
                path = @"C:\HPKata\Kata.xlsm";
            }

            if (!File.Exists(path))
            {
                ed.WriteMessage($"\n[HPKata] loadwall: Không tìm thấy workbook tại {path}\n");
                return;
            }

            var excel = adapter ?? KataExcelAdapterProvider.Current;
            if (!excel.ReadWallSheet(path, out var sheetData))
            {
                ed.WriteMessage($"\n[HPKata] loadwall: Không thể đọc sheet 'Vach' từ {path}\n");
                return;
            }

            var model = KataWallDataMapper.ToShearWallModel(sheetData);
            WallTakeoffRegistry.Register(model);
            ed.WriteMessage($"\n[HPKata] loadwall: Đã tải vách '{model.WallMark}' ({model.Stories.Count} tầng) từ Kata.xlsm vào bộ nhớ.\n");

            WallDrawController.PromptAndDrawWallModel(doc, ed, model);
        }
    }
}
