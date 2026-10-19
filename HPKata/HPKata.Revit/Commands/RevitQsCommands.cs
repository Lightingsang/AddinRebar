using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HPKata.Revit.Interop;
using HPKata.Revit.Services;
using HPKata.UI.ViewModels;
using HPKata.UI.Views;

namespace HPKata.Revit.Commands
{
    /// <summary>
    /// External command launching the Quantity Surveying (QS) takeoff dialog and Excel exporter in Revit.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class RevitQsCommands : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            if (uidoc == null) return Result.Cancelled;
            var doc = uidoc.Document;

            try
            {
                var qsElements = RevitQsTakeoffService.ExtractElements(doc);
                var vm = new QsTakeoffViewModel();
                if (qsElements.Count > 0)
                {
                    vm.SetElements(qsElements);
                }

                var view = new QsTakeoffWindow(vm);
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
