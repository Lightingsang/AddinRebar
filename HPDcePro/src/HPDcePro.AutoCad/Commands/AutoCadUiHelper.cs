using System;
using System.Windows;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPDcePro.AutoCad.Commands
{
    /// <summary>
    /// UI and messaging helper for AutoCAD 2026 commands.
    /// </summary>
    public static class AutoCadUiHelper
    {
        public static void WriteMessage(string message)
        {
            try
            {
                var doc = AcadApp.DocumentManager?.MdiActiveDocument;
                doc?.Editor.WriteMessage($"\n[HPDcePro] {message}");
            }
            catch (Exception)
            {
                // Reason: Outside acad.exe process (e.g. unit tests or headless runners), AutoCAD DocumentManager throws InvalidProgramException.
            }
        }

        public static void ShowModal(Window window)
        {
            if (window == null) return;
            try
            {
                if (AcadApp.DocumentManager?.MdiActiveDocument != null)
                {
                    AcadApp.ShowModalWindow(window);
                }
            }
            catch (Exception)
            {
                // Reason: Outside active AutoCAD GUI (e.g. headless unit tests), skip modal dialog blocking.
            }
        }
    }
}
