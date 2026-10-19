using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using HPKata.AutoCad.Interop;
using HPKata.AutoCad.Ribbon;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: ExtensionApplication(typeof(HPKata.AutoCad.Commands.HPKataExtensionApplication))]

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// AutoCAD Extension Application lifecycle controller.
    /// Installs the HPKata Ribbon tab and ensures RegApp 'KATA' is registered in all documents.
    /// </summary>
    public class HPKataExtensionApplication : IExtensionApplication
    {
        public void Initialize()
        {
            try
            {
                KataCadAssemblyResolver.Initialize();
                HPKataRibbonTab.Install();

                var docMgr = AcadApp.DocumentManager;
                if (docMgr.MdiActiveDocument != null)
                {
                    EnsureActiveDocRegApp(docMgr.MdiActiveDocument);
                }

                docMgr.DocumentCreated += OnDocumentCreated;
                docMgr.DocumentActivated += OnDocumentActivated;

                var ed = AcadApp.DocumentManager.MdiActiveDocument?.Editor;
                ed?.WriteMessage("\n[HPKata] HPKata AutoCAD 2026 Add-In (.NET 8) & Ribbon tab 'HPKata' loaded successfully.\n");
            }
            catch (System.Exception ex)
            {
                // Silently catch in host startup, write error to host prompt
                var ed = AcadApp.DocumentManager.MdiActiveDocument?.Editor;
                ed?.WriteMessage($"\n[HPKata] Startup initialization note: {ex.Message}\n");
            }
        }

        public void Terminate()
        {
            try
            {
                HPKataRibbonTab.Uninstall();

                var docMgr = AcadApp.DocumentManager;
                docMgr.DocumentCreated -= OnDocumentCreated;
                docMgr.DocumentActivated -= OnDocumentActivated;

                if (docMgr.MdiActiveDocument != null)
                {
                    CadHighlightService.ClearAllHighlights(docMgr.MdiActiveDocument.Database);
                }
            }
            catch (System.Exception)
            {
                // Graceful termination fallback
            }
        }

        [CommandMethod("HPKATARIBBON")]
        public static void ShowRibbonTab()
        {
            HPKataRibbonTab.Install();
            HPKataRibbonTab.EnsureCreated();
            var ed = AcadApp.DocumentManager.MdiActiveDocument?.Editor;
            ed?.WriteMessage("\n[HPKata] Tab Ribbon 'HPKata' đã được kích hoạt.\n");
        }

        private static void OnDocumentCreated(object? sender, DocumentCollectionEventArgs e)
        {
            if (e.Document != null)
            {
                EnsureActiveDocRegApp(e.Document);
            }
        }

        private static void OnDocumentActivated(object? sender, DocumentCollectionEventArgs e)
        {
            if (e.Document != null)
            {
                EnsureActiveDocRegApp(e.Document);
            }
        }

        private static void EnsureActiveDocRegApp(Document doc)
        {
            try
            {
                XDataService.EnsureRegApp(doc.Database);
            }
            catch (System.Exception)
            {
                // Ignore transient DB lock errors during document switching
            }
        }
    }
}
