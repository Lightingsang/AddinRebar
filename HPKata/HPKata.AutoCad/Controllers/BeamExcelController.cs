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
    /// Controller handling 'loadbeam' command loading beam detailing layouts from Kata.xlsm sheet 'Dam'
    /// and drawing directly in AutoCAD when an interactive editor is active.
    /// </summary>
    public static class BeamExcelController
    {
        public static void LoadBeamInteractive(IKataExcelAdapter? adapter = null)
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
                ed.WriteMessage($"\n[HPKata] loadbeam: Không tìm thấy workbook tại {path}\n");
                return;
            }

            var excel = adapter ?? KataExcelAdapterProvider.Current;
            if (!excel.ReadBeamSheet(path, out var sheetData))
            {
                ed.WriteMessage($"\n[HPKata] loadbeam: Không thể đọc sheet 'Dam' từ {path}\n");
                return;
            }

            var layout = KataBeamDataMapper.ToRebarLayout(sheetData);
            BeamTakeoffRegistry.Register(layout);
            ed.WriteMessage($"\n[HPKata] loadbeam: Đã tải dầm '{layout.BeamName}' ({layout.Spans.Count} nhịp) từ Kata.xlsm vào bộ nhớ.\n");

            BeamDrawController.PromptAndDrawBeamLayout(doc, ed, layout, isShopMode: false);
        }
    }
}
