using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HPRebar.KataExport.Service;
using HPRebar.KataExport.View;
using HPRebar.KataExport.ViewModel;
using HPRebar.KataRebar.Service;
using JetBrains.Annotations;
using Nice3point.Revit.Toolkit.External;

namespace HPRebar.KataExport;

/// <summary>
/// Ribbon entry for the Kata settings dialog (HPRebar ▸ Rebar ▸ Kata Settings). The settings belong to the machine,
/// not to a model, so the dialog opens without a selection and without a document. It is modal: Revit's ribbon is
/// disabled while it is open, so a second click cannot open a rival dialog.
/// </summary>
[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public sealed class KataSettingsCommand : ExternalCommand
{
    public override void Execute()
    {
        KataSettingsView? window = null;
        var viewModel = new KataSettingsViewModel(KataSettingsStore.LoadFile(), _ => window?.Close());
        window = new KataSettingsView(viewModel);
        new WindowInteropHelper(window).Owner = Application.MainWindowHandle;
        window.ShowDialog();

        if (viewModel.SaveFailed)
            RevitDialogs.Error("Kata Settings", "Không ghi được file thiết lập; thông số chỉ dùng trong phiên Revit này.");
    }

    /// <summary>Keeps the button enabled with no document open (Revit greys external commands there by default).</summary>
    [UsedImplicitly]
    public sealed class Availability : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories) => true;
    }
}
