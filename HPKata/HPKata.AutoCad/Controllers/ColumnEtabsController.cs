using System;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using HPKata.Core.Columns.Etabs;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller orchestrating ETABS column design table import (loadcolumn).
    /// </summary>
    public static class ColumnEtabsController
    {
        public static void LoadColumnInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var pso = new PromptStringOptions("\n[HPKata] Enter ETABS column text file path (or press Enter for sample data): ")
            {
                AllowSpaces = true
            };
            var psr = ed.GetString(pso);
            if (psr.Status != PromptStatus.OK) return;

            string content = psr.StringResult.Trim();
            if (string.IsNullOrWhiteSpace(content) || !File.Exists(content))
            {
                content = GetSampleEtabsColumnText();
            }
            else
            {
                content = File.ReadAllText(content);
            }

            var service = new EtabsColumnImportService();
            var layouts = service.ImportFromTableText(content);

            foreach (var l in layouts)
            {
                ColumnTakeoffRegistry.Register(l);
            }

            ed.WriteMessage($"\n[HPKata] loadcolumn: Successfully parsed and cached {layouts.Count} column layouts from ETABS design data.\n");
        }

        public static string GetSampleEtabsColumnText() =>
            "Story\tColumn\tWidth\tDepth\tP\tM2\tM3\tAst\tAv/s\n" +
            "Story1\tC1\t500\t500\t-1500\t120\t240\t3200\t800\n" +
            "Story2\tC1\t450\t450\t-1100\t90\t180\t2600\t700\n" +
            "Story1\tC2\t400\t400\t-950\t60\t110\t1800\t600\n";
    }
}
