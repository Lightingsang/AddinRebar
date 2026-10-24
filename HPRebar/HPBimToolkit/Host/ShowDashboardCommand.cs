using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.External;

namespace HPBimToolkit.Host;

/// <summary>Ribbon entry: shows the dashboard pane (Revit keeps its position and docking between sessions).</summary>
[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public sealed class ShowDashboardCommand : ExternalCommand
{
    public override void Execute()
    {
        if (!DockablePane.PaneIsRegistered(DashboardPaneProvider.PaneId))
        {
            TaskDialog.Show(DashboardPaneProvider.Title,
                "The dashboard pane was not registered when Revit started. See %LocalAppData%\\HPBimToolkit\\logs for the reason.");
            return;
        }

        // Application is the inherited UIApplication of ExternalCommand.
        Application.GetDockablePane(DashboardPaneProvider.PaneId).Show();
    }
}
