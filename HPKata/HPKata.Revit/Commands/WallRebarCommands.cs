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
    /// External command allowing users to detail rebar on selected Revit Structural Walls (OST_Walls).
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class WallRebarCommands : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            if (uidoc == null) return Result.Cancelled;

            try
            {
                var refWall = uidoc.Selection.PickObject(
                    ObjectType.Element,
                    new WallSelectionFilter(),
                    "Select a Structural Wall to detail with HPKata:");

                if (uidoc.Document.GetElement(refWall) is not Wall wall)
                {
                    return Result.Cancelled;
                }

                ShowDialogAndGenerateRebar(uiapp, uidoc.Document, wall);
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

        private static void ShowDialogAndGenerateRebar(UIApplication uiapp, Document doc, Wall wall)
        {
            var vm = CreateViewModel(wall);
            var view = new WallDetailingWindow(vm);
            RevitWindowHelper.ShowModal(view, uiapp);

            if (vm.ResultModel != null)
            {
                using var tr = new Transaction(doc, $"HPKata: Generate Rebar for {wall.Name}");
                tr.Start();
                WallRebarGenerator.GenerateWallRebar(doc, wall, vm.ResultModel);
                tr.Commit();
                TaskDialog.Show("HPKata", $"Successfully generated 3D rebar in shear wall '{wall.Name}'.");
            }
        }

        private static WallDetailingViewModel CreateViewModel(Wall wall)
        {
            var adapter = KataExcelAdapterProvider.Current;
            var vm = new WallDetailingViewModel(adapter)
            {
                WallMark = wall.Name
            };

            string kataPath = KataInstallEnvironment.Current.KataWorkbookPath;
            if (File.Exists(kataPath) && adapter.ReadWallSheet(kataPath, out var sheetData))
            {
                if (string.Equals(sheetData.WallMark, wall.Name, StringComparison.OrdinalIgnoreCase))
                {
                    vm.ReadExcelCommand.Execute(null);
                }
            }

            return vm;
        }

        private sealed class WallSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem) =>
                elem.Category != null && elem.Category.Id.Value == (long)BuiltInCategory.OST_Walls;

            public bool AllowReference(Reference reference, XYZ position) => false;
        }
    }
}
