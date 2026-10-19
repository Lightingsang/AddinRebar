using System;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using HPKata.Core.Configuration;
using HPKata.Core.Excel;
using HPKata.Revit.Interop;
using HPKata.Revit.Services;
using HPKata.UI.ViewModels;
using HPKata.UI.Views;

namespace HPKata.Revit.Commands
{
    /// <summary>
    /// External command allowing users to detail rebar on selected Revit Structural Framing beams.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class FramingRebarCommands : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            if (uidoc == null) return Result.Cancelled;

            try
            {
                var refBeam = uidoc.Selection.PickObject(
                    ObjectType.Element,
                    new FramingSelectionFilter(),
                    "Select a Structural Framing beam to detail with HPKata:");

                if (uidoc.Document.GetElement(refBeam) is not FamilyInstance beam)
                {
                    return Result.Cancelled;
                }

                ShowDialogAndGenerateRebar(uiapp, uidoc.Document, beam);
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

        private static void ShowDialogAndGenerateRebar(UIApplication uiapp, Document doc, FamilyInstance beam)
        {
            var vm = CreateViewModel(beam);
            var view = new BeamDetailingWindow(vm);
            RevitWindowHelper.ShowModal(view, uiapp);

            if (vm.ResultLayout != null)
            {
                using var tr = new Transaction(doc, $"HPKata: Generate Rebar for {beam.Name}");
                tr.Start();
                FramingRebarGenerator.GenerateFramingRebar(doc, beam, vm.ResultLayout);
                tr.Commit();
                TaskDialog.Show("HPKata", $"Successfully generated 3D rebar in beam '{beam.Name}'.");
            }
        }

        private static BeamDetailingViewModel CreateViewModel(FamilyInstance beam)
        {
            var adapter = KataExcelAdapterProvider.Current;
            var vm = new BeamDetailingViewModel(adapter)
            {
                BeamMark = beam.Name
            };

            string kataPath = KataInstallEnvironment.Current.KataWorkbookPath;
            if (File.Exists(kataPath) && adapter.ReadBeamSheet(kataPath, out var sheetData))
            {
                if (string.Equals(sheetData.BeamMark, beam.Name, StringComparison.OrdinalIgnoreCase))
                {
                    vm.ReadExcelCommand.Execute(null);
                }
            }

            return vm;
        }

        private sealed class FramingSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem) =>
                elem.Category != null && elem.Category.Id.Value == (long)BuiltInCategory.OST_StructuralFraming;

            public bool AllowReference(Reference reference, XYZ position) => false;
        }
    }
}
