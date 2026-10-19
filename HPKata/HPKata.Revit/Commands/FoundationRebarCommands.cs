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
    /// External command allowing users to detail rebar on selected Revit Structural Foundations (OST_StructuralFoundation).
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class FoundationRebarCommands : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            if (uidoc == null) return Result.Cancelled;
            var doc = uidoc.Document;

            try
            {
                var refFound = uidoc.Selection.PickObject(
                    ObjectType.Element,
                    new FoundationSelectionFilter(),
                    "Select a Structural Foundation to detail with HPKata:");

                var found = doc.GetElement(refFound) as FamilyInstance;
                if (found == null) return Result.Cancelled;

                var vm = new FoundationDetailingViewModel { FoundationMark = found.Name };
                var view = new FoundationDetailingWindow(vm);
                RevitWindowHelper.ShowModal(view, uiapp);

                if (vm.ResultLayout != null)
                {
                    using var tr = new Transaction(doc, $"HPKata: Generate Rebar for {found.Name}");
                    tr.Start();
                    if (vm.ResultLayout.IsPileCap && vm.ResultLayout.PileCap != null)
                    {
                        FoundationRebarGenerator.GenerateFootingRebar(doc, found, vm.BuildFootingModel());
                    }
                    else if (vm.ResultLayout.Footing != null)
                    {
                        FoundationRebarGenerator.GenerateFootingRebar(doc, found, vm.ResultLayout.Footing);
                    }
                    tr.Commit();
                    TaskDialog.Show("HPKata", $"Successfully generated 3D rebar in foundation '{found.Name}'.");
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

        private sealed class FoundationSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem) =>
                elem.Category != null && elem.Category.Id.Value == (long)BuiltInCategory.OST_StructuralFoundation;

            public bool AllowReference(Reference reference, XYZ position) => false;
        }
    }
}
