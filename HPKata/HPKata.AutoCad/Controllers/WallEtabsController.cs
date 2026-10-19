using System;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using HPKata.Core.Walls.Etabs;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller orchestrating ETABS shear wall pier design table import (loadwall).
    /// </summary>
    public static class WallEtabsController
    {
        public static void LoadWallInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var pso = new PromptStringOptions("\n[HPKata] Enter ETABS pier text file path (or press Enter for sample data): ")
            {
                AllowSpaces = true
            };
            var psr = ed.GetString(pso);
            if (psr.Status != PromptStatus.OK) return;

            string content = psr.StringResult.Trim();
            if (string.IsNullOrWhiteSpace(content) || !File.Exists(content))
            {
                content = GetSampleEtabsWallText();
            }
            else
            {
                content = File.ReadAllText(content);
            }

            var service = new EtabsWallImportService();
            var walls = service.ImportFromTableText(content);

            foreach (var w in walls)
            {
                WallTakeoffRegistry.Register(w);
            }

            ed.WriteMessage($"\n[HPKata] loadwall: Successfully parsed and cached {walls.Count} shear wall models from ETABS design data.\n");
        }

        public static string GetSampleEtabsWallText() =>
            "Story\tPier\tThickness\tLength\tP\tV2\tM3\tAsb\tAvh\n" +
            "Story1\tP1\t300\t3200\t-3500\t450\t1800\t2400\t600\n" +
            "Story2\tP1\t300\t3200\t-2800\t380\t1400\t1800\t500\n" +
            "Story1\tP2\t350\t4000\t-4200\t520\t2200\t3000\t700\n";
    }
}
