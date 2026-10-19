using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HPKata.Revit.Interop;
using HPKata.UI.ViewModels;
using HPKata.UI.Views;

namespace HPKata.Revit.Commands
{
    /// <summary>
    /// External command launching the Drawing Layout & Grid Utilities dialog in Revit.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class RevitLayoutCommands : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            if (uidoc == null) return Result.Cancelled;

            try
            {
                var vm = new DrawingUtilityViewModel();
                var view = new DrawingUtilityWindow(vm);
                RevitWindowHelper.ShowModal(view, uiapp);

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
