using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HPKata.Revit.Bridge;
using HPKata.Revit.Interop;
using HPKata.Revit.Runners;
using HPKata.UI.ViewModels;
using HPKata.UI.Views;

namespace HPKata.Revit.Commands;

/// <summary>
/// External command running 1D cutting stock optimization and opening the cutting pattern dialog.
/// </summary>
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public sealed class CuttingStockCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var context = new RevitDocumentContext(commandData.Application.ActiveUIDocument);
        RevitCuttingStockRunner.Execute(context);

        var vm = new CuttingStockViewModel();
        var items = context.GetAllRebars().Select(RevitParameterBridge.ToRebarItem);
        vm.LoadFromRebarItems(items);

        var view = new CuttingStockView(vm);
        RevitWindowHelper.ShowModal(view, commandData.Application);
        return Result.Succeeded;
    }
}
