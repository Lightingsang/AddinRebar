using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HPKata.Revit.Interop;
using HPKata.UI.ViewModels;
using HPKata.UI.Views;

namespace HPKata.Revit.Commands;

/// <summary>
/// External command opening the Kata rebar detailing and shape dialog.
/// </summary>
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public sealed class DetailingCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var vm = new DetailingViewModel();
        var view = new DetailingView(vm);

        RevitWindowHelper.ShowModal(view, commandData.Application);
        return Result.Succeeded;
    }
}
