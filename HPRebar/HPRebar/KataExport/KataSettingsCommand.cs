using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HPRebar.KataExport.Service;
using HPRebar.KataExport.View.Web.Settings;
using JetBrains.Annotations;
using Nice3point.Revit.Toolkit.External;

namespace HPRebar.KataExport;

/// <summary>
/// Ribbon entry for the Kata settings window (HPRebar ▸ Rebar ▸ Kata Settings, Revit 2026 only: a web page, ADR-0013).
/// The settings belong to the machine, not to a model, so the window opens without a selection and without a document.
/// It is modal: Revit's ribbon is disabled while it is open, so a second click cannot open a rival window.
/// </summary>
[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public sealed class KataSettingsCommand : ExternalCommand
{
    public override void Execute()
    {
        var window = new KataSettingsWebWindow();
        new WindowInteropHelper(window).Owner = Application.MainWindowHandle;
        window.ShowDialog();

        if (window.FileFailed)
            RevitDialogs.Error("Kata Settings", "Không ghi được file thiết lập; thông số chỉ dùng trong phiên Revit này.");
    }

    /// <summary>Keeps the button enabled with no document open (Revit greys external commands there by default).</summary>
    [UsedImplicitly]
    public sealed class Availability : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories) => true;
    }
}
