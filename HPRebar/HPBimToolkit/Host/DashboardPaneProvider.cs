using Autodesk.Revit.UI;

namespace HPBimToolkit.Host;

/// <summary>Describes the dockable pane Revit hosts the WebView in: its content, where it docks and that it starts hidden.</summary>
public sealed class DashboardPaneProvider(WebViewHost host) : IDockablePaneProvider
{
    public static readonly DockablePaneId PaneId = new(new Guid("7F79FE1E-1A63-44AA-8A12-2BDB688CC48E"));

    public const string Title = "HP BIM Toolkit";

    public void SetupDockablePane(DockablePaneProviderData data)
    {
        data.FrameworkElement = host;
        data.VisibleByDefault = false;
        data.InitialState = new DockablePaneState
        {
            DockPosition = DockPosition.Right,
            MinimumWidth = 320,
        };
    }
}
