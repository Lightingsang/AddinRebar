using System;
using System.IO;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using HPKata.Core.Configuration;
using HPKata.Core.Excel;
using HPKata.Revit.Interop;
using HPKata.Revit.Services;
using HPKata.Revit.Units;
using HPKata.UI.ViewModels;
using HPKata.UI.Views;

namespace HPKata.Revit.Commands
{
    /// <summary>
    /// External command allowing users to detail rebar on selected Revit Structural Framing beams.
    /// Supports both pre-selection (reads real b x h x L dimensions) and dialog-first -> post-Apply selection.
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
                var doc = uidoc.Document;
                FamilyInstance? beam = FindPreSelectedBeam(uidoc, doc);

                var vm = CreateViewModel(beam);
                var view = new BeamDetailingWindow(vm);
                RevitWindowHelper.ShowModal(view, uiapp);

                if (!vm.IsConfirmed || vm.ResultLayout == null)
                {
                    return Result.Cancelled;
                }

                beam ??= PickBeamInteractive(uidoc, doc);
                if (beam == null)
                {
                    return Result.Cancelled;
                }

                using var tr = new Transaction(doc, $"HPKata: Generate Rebar for {beam.Name}");
                tr.Start();
                var created = FramingRebarGenerator.GenerateFramingRebar(doc, beam, vm.ResultLayout);
                tr.Commit();

                TaskDialog.Show("HPKata — Thép Dầm 3D",
                    $"Đã bố trí thành công {created.Count} thanh cốt thép 3D cho dầm '{beam.Name}' (Mã dầm: {vm.ResultLayout.BeamName}).");
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

        private static FamilyInstance? FindPreSelectedBeam(UIDocument uidoc, Document doc)
        {
            var ids = uidoc.Selection.GetElementIds();
            foreach (var id in ids)
            {
                if (doc.GetElement(id) is FamilyInstance fi &&
                    fi.Category != null &&
                    fi.Category.Id.Value == (long)BuiltInCategory.OST_StructuralFraming)
                {
                    return fi;
                }
            }
            return null;
        }

        private static FamilyInstance? PickBeamInteractive(UIDocument uidoc, Document doc)
        {
            try
            {
                var refBeam = uidoc.Selection.PickObject(
                    ObjectType.Element,
                    new FramingSelectionFilter(),
                    "Chọn dầm (Structural Framing) để bố trí cốt thép 3D HPKata:");
                return doc.GetElement(refBeam) as FamilyInstance;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return null;
            }
        }

        private static BeamDetailingViewModel CreateViewModel(FamilyInstance? beam)
        {
            var adapter = KataExcelAdapterProvider.Current;
            var vm = new BeamDetailingViewModel(adapter);

            if (beam != null)
            {
                vm.BeamMark = beam.Name;
                ReadBeamDimensionsIntoSpan(beam, vm);
            }

            string kataPath = KataInstallEnvironment.Current.KataWorkbookPath;
            if (File.Exists(kataPath) && adapter.ReadBeamSheet(kataPath, out var sheetData))
            {
                if (beam == null || string.Equals(sheetData.BeamMark, beam.Name, StringComparison.OrdinalIgnoreCase))
                {
                    vm.ReadExcelCommand.Execute(null);
                }
            }

            return vm;
        }

        private static void ReadBeamDimensionsIntoSpan(FamilyInstance beam, BeamDetailingViewModel vm)
        {
            if (vm.Spans.Count == 0) return;
            var span0 = vm.Spans[0];

            if (beam.Location is LocationCurve loc && loc.Curve != null)
            {
                double lenMm = Math.Round(RevitUnitConverter.FeetToMm(loc.Curve.Length) / 10.0) * 10.0;
                if (lenMm > 100.0) span0.ClearLengthMm = lenMm;
            }

            var symbol = beam.Symbol;
            if (symbol != null)
            {
                var pB = symbol.get_Parameter(BuiltInParameter.STRUCTURAL_SECTION_COMMON_WIDTH) ?? symbol.LookupParameter("b");
                var pH = symbol.get_Parameter(BuiltInParameter.STRUCTURAL_SECTION_COMMON_HEIGHT) ?? symbol.LookupParameter("h");
                if (pB != null && pB.HasValue && pB.AsDouble() > 0)
                    span0.WidthMm = Math.Round(RevitUnitConverter.FeetToMm(pB.AsDouble()));
                if (pH != null && pH.HasValue && pH.AsDouble() > 0)
                    span0.HeightMm = Math.Round(RevitUnitConverter.FeetToMm(pH.AsDouble()));
            }
        }

        private sealed class FramingSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem) =>
                elem.Category != null && elem.Category.Id.Value == (long)BuiltInCategory.OST_StructuralFraming;

            public bool AllowReference(Reference reference, XYZ position) => false;
        }
    }
}
