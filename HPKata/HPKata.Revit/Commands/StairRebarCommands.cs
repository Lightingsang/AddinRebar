using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using HPKata.Revit.Interop;
using HPKata.Revit.Services;
using HPKata.UI.ViewModels;
using HPKata.UI.Views;

namespace HPKata.Revit.Commands
{
    /// <summary>
    /// External command allowing users to detail rebar on selected Revit Stairs (OST_Stairs).
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class StairRebarCommands : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            if (uidoc == null) return Result.Cancelled;
            var doc = uidoc.Document;

            try
            {
                var refStair = uidoc.Selection.PickObject(
                    ObjectType.Element,
                    new StairSelectionFilter(),
                    "Select a Stair to detail rebar with HPKata:");

                var stair = doc.GetElement(refStair);
                if (stair == null) return Result.Cancelled;

                var vm = new StairLintelDetailingViewModel { StairMark = stair.Name };
                var view = new StairLintelDetailingWindow(vm);
                RevitWindowHelper.ShowModal(view, uiapp);

                if (vm.ResultFlight != null)
                {
                    using var tr = new Transaction(doc, $"HPKata: Generate Rebar for {stair.Name}");
                    tr.Start();
                    StairRebarGenerator.GenerateStairRebar(doc, stair, vm.ResultFlight);
                    tr.Commit();
                    TaskDialog.Show("HPKata", $"Successfully generated 3D rebar in stair '{stair.Name}'.");
                }

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

        private sealed class StairSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem) =>
                elem.Category != null && elem.Category.Id.Value == (long)BuiltInCategory.OST_Stairs;

            public bool AllowReference(Reference reference, XYZ position) => false;
        }
    }
}
