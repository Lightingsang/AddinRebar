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
    /// Controller handling 'loadcolumn' command loading column detailing layouts from Kata.xlsm sheet 'Cot'
    /// and drawing directly in AutoCAD when an interactive editor is active.
    /// </summary>
    public static class ColumnExcelController
    {
        public static void LoadColumnInteractive(IKataExcelAdapter? adapter = null)
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
                ed.WriteMessage($"\n[HPKata] loadcolumn: Không tìm thấy workbook tại {path}\n");
                return;
            }

            var excel = adapter ?? KataExcelAdapterProvider.Current;
            if (!excel.ReadColumnSheet(path, out var sheetData))
            {
                ed.WriteMessage($"\n[HPKata] loadcolumn: Không thể đọc sheet 'Cot' từ {path}\n");
                return;
            }

            var layout = KataColumnDataMapper.ToRebarLayout(sheetData);
            ColumnTakeoffRegistry.Register(layout);
            ed.WriteMessage($"\n[HPKata] loadcolumn: Đã tải cột '{layout.ColumnMark}' ({layout.Stories.Count} tầng) từ Kata.xlsm vào bộ nhớ.\n");

            ColumnDrawController.PromptAndDrawColumnLayout(doc, ed, layout);
        }
    }
}
