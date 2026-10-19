using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HPKata.Core.Beams.Models;
using HPKata.Revit.Interop;
using HPKata.Revit.Services;
using HPKata.UI.ViewModels;
using HPKata.UI.Views;

namespace HPKata.Revit.Commands
{
    /// <summary>
    /// External command importing ETABS concrete beam design results and applying them to Revit Framing beams.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class EtabsFramingImportCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            if (uidoc == null) return Result.Cancelled;
            var doc = uidoc.Document;

            try
            {
                var vm = new EtabsImportViewModel();
                var view = new EtabsImportWindow(vm);

                bool? dialogResult = RevitWindowHelper.ShowModal(view, uiapp);
                if (vm.ResultLayouts.Count == 0) return Result.Cancelled;

                int generated = GenerateRebarForLayouts(doc, vm.ResultLayouts);
                TaskDialog.Show("HPKata", $"Imported and generated rebar for {generated} framing beams.");
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

        private static int GenerateRebarForLayouts(Document doc, IList<BeamRebarLayout> layouts)
        {
            var allBeams = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .ToList();

            int generated = 0;
            using var tr = new Transaction(doc, "HPKata: Batch ETABS Framing Rebar");
            tr.Start();
            foreach (var layout in layouts)
            {
                var matchingBeam = allBeams.FirstOrDefault(b =>
                    b.Name.Equals(layout.BeamName, StringComparison.OrdinalIgnoreCase));

                if (matchingBeam != null)
                {
                    FramingRebarGenerator.GenerateFramingRebar(doc, matchingBeam, layout);
                    generated++;
                }
            }
            tr.Commit();
            return generated;
        }
    }
}
