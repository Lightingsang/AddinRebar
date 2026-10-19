using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HPDcePro.UI.Common;
using HPDcePro.UI.Views;

namespace HPDcePro.Revit
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class DceProControlCenterCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                ThemeManager.EnsureAssembliesLoaded();
                var window = new MainControlCenterWindow();
                ThemeManager.Apply(window, dark: true);
                window.ShowDialog();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Reason: Report diagnostic message to Revit UI if dialog fails to initialize
                message = $"Lỗi khởi chạy HPDcePro Control Center: {ex.Message}";
                return Result.Failed;
            }
        }
    }
}
