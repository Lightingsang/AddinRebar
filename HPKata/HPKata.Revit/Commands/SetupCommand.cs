using System;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HPKata.Core.Configuration;
using HPKata.Revit.Interop;
using HPKata.UI.ViewModels;
using HPKata.UI.Views;

namespace HPKata.Revit.Commands;

/// <summary>
/// External command opening the Kata rebar detailing setup dialog with bidirectional 86-parameter sync.
/// </summary>
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public sealed class SetupCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        string configPath = KataInstallEnvironment.GetConfigFilePath();
        KataConfig config = File.Exists(configPath)
            ? KataConfigParser.ParseFile(configPath)
            : KataConfigDefaults.CreateDefault();

        var vm = new SetupViewModel(config);
        var view = new SetupView(vm);

        bool? dialogResult = RevitWindowHelper.ShowModal(view, commandData.Application);
        if (dialogResult == true || vm.IsDirty)
        {
            try
            {
                KataConfigParser.SaveToFile(configPath, vm.ToConfig());
            }
            catch (Exception ex)
            {
                // Annotated catch per Rule Q-B2: file I/O error logging during configuration save
                message = $"Warning saving configuration to '{configPath}': {ex.Message}";
            }
        }

        return Result.Succeeded;
    }
}
