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
    /// External command allowing users to detail rebar on selected Revit Floors (OST_Floors).
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class SlabRebarCommands : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            if (uidoc == null) return Result.Cancelled;
            var doc = uidoc.Document;

            try
            {
                var refFloor = uidoc.Selection.PickObject(
                    ObjectType.Element,
                    new FloorSelectionFilter(),
                    "Select a Floor to detail rebar with HPKata:");

                var floor = doc.GetElement(refFloor) as Floor;
                if (floor == null) return Result.Cancelled;

                var vm = new SlabDetailingViewModel { PanelMark = floor.Name };
                var view = new SlabDetailingWindow(vm);
                RevitWindowHelper.ShowModal(view, uiapp);

                if (vm.ResultLayout != null && vm.ResultLayout.Panel != null)
                {
                    using var tr = new Transaction(doc, $"HPKata: Generate Rebar for {floor.Name}");
                    tr.Start();
                    SlabRebarGenerator.GenerateSlabRebar(doc, floor, vm.ResultLayout.Panel);
                    tr.Commit();
                    TaskDialog.Show("HPKata", $"Successfully generated 3D rebar in floor '{floor.Name}'.");
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

        private sealed class FloorSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem) =>
                elem.Category != null && elem.Category.Id.Value == (long)BuiltInCategory.OST_Floors;

            public bool AllowReference(Reference reference, XYZ position) => false;
        }
    }
}
