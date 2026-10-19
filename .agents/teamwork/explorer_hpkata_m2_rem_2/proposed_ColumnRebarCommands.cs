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
    /// External command allowing users to detail rebar on selected Revit Structural Columns (OST_StructuralColumns).
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class ColumnRebarCommands : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            if (uidoc == null) return Result.Cancelled;

            try
            {
                var refCol = uidoc.Selection.PickObject(
                    ObjectType.Element,
                    new ColumnSelectionFilter(),
                    "Select a Structural Column to detail with HPKata:");

                if (uidoc.Document.GetElement(refCol) is not FamilyInstance col)
                {
                    return Result.Cancelled;
                }

                ShowDialogAndGenerateRebar(uiapp, uidoc.Document, col);
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

        private static void ShowDialogAndGenerateRebar(UIApplication uiapp, Document doc, FamilyInstance col)
        {
            var vm = CreateViewModel(col);
            var view = new ColumnDetailingWindow(vm);
            RevitWindowHelper.ShowModal(view, uiapp);

            if (vm.ResultLayout != null)
            {
                using var tr = new Transaction(doc, $"HPKata: Generate Rebar for {col.Name}");
                tr.Start();
                ColumnRebarGenerator.GenerateColumnRebar(doc, col, vm.ResultLayout);
                tr.Commit();
                TaskDialog.Show("HPKata", $"Successfully generated 3D rebar in column '{col.Name}'.");
            }
        }

        private static ColumnDetailingViewModel CreateViewModel(FamilyInstance col)
        {
            var adapter = KataExcelAdapterProvider.Current;
            var vm = new ColumnDetailingViewModel(adapter)
            {
                ColumnMark = col.Name
            };

            string kataPath = KataInstallEnvironment.Current.KataWorkbookPath;
            if (File.Exists(kataPath) && adapter.ReadColumnSheet(kataPath, out var sheetData))
            {
                if (string.Equals(sheetData.ColumnMark, col.Name, StringComparison.OrdinalIgnoreCase))
                {
                    vm.ReadExcelCommand.Execute(null);
                }
            }

            return vm;
        }

        private sealed class ColumnSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem) =>
                elem.Category != null && elem.Category.Id.Value == (long)BuiltInCategory.OST_StructuralColumns;

            public bool AllowReference(Reference reference, XYZ position) => false;
        }
    }
}
