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
/// External command generating rebar schedules and opening the schedule viewer dialog.
/// </summary>
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public sealed class ScheduleCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var context = new RevitDocumentContext(commandData.Application.ActiveUIDocument);
        RevitScheduleRunner.Execute(context);

        var vm = new ScheduleViewModel();
        var rebars = context.GetAllRebars();
        if (rebars.Count > 0)
        {
            vm.Items.Clear();
            foreach (var r in rebars)
            {
                vm.Items.Add(RevitParameterBridge.ToRebarItem(r));
            }
            vm.RecalculateTotals();
        }

        var view = new ScheduleView(vm);
        RevitWindowHelper.ShowModal(view, commandData.Application);
        return Result.Succeeded;
    }
}
