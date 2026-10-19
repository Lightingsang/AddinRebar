namespace HPDynamo.McpBridge.Entry;

using System;
using System.Windows.Controls;
using Dynamo.Graph.Workspaces;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;
using HPDynamo.McpBridge.Coordinator;
using Serilog;

/// <summary>
///     Dynamo 3.7 View Extension entry point.
///     Hooks into DynamoView when Dynamo UI is opened, capturing DynamoViewModel,
///     active WorkspaceModel, and binding status controls into Dynamo's Extensions menu.
/// </summary>
public sealed class DynamoBridgeViewExtension : IViewExtension
{
    private ViewLoadedParams? _viewLoadedParams;
    private MenuItem? _extensionMenuItem;

    public string UniqueId => "C4E72B5F-4E2F-4F18-A875-2E3F51D2A610";

    public string Name => "HPDynamo MCP Bridge";

    public void Startup(ViewStartupParams viewStartupParams)
    {
        Log.Information("HPDynamo ViewExtension Startup invoked");
    }

    public void Loaded(ViewLoadedParams viewLoadedParams)
    {
        _viewLoadedParams = viewLoadedParams;

        try
        {
            var dynamoViewModel = viewLoadedParams.DynamoWindow?.DataContext as DynamoViewModel;
            var workspaceModel = viewLoadedParams.CurrentWorkspaceModel as WorkspaceModel;

            DynamoBridgeCoordinator.Instance.RegisterLiveCanvas(
                viewLoadedParams,
                dynamoViewModel,
                workspaceModel);

            _extensionMenuItem = new MenuItem
            {
                Header = "HPDynamo MCP Status..."
            };
            _extensionMenuItem.Click += (_, _) =>
            {
                DynamoBridgeCoordinator.Instance.ShowStatusDialog();
            };
            viewLoadedParams.AddExtensionMenuItem(_extensionMenuItem);

            viewLoadedParams.CurrentWorkspaceChanged += OnWorkspaceChanged;
            Log.Information("HPDynamo ViewExtension successfully bound to live Dynamo canvas");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to bind HPDynamo ViewExtension to live canvas");
        }
    }

    public void Shutdown()
    {
        UnsubscribeEvents();
        DynamoBridgeCoordinator.Instance.UnregisterLiveCanvas();
        Log.Information("HPDynamo ViewExtension shut down");
    }

    public void Dispose()
    {
        Shutdown();
    }

    private void OnWorkspaceChanged(IWorkspaceModel newWorkspace)
    {
        DynamoBridgeCoordinator.Instance.UpdateActiveWorkspace(newWorkspace as WorkspaceModel);
    }

    private void UnsubscribeEvents()
    {
        if (_viewLoadedParams != null)
        {
            _viewLoadedParams.CurrentWorkspaceChanged -= OnWorkspaceChanged;
            _viewLoadedParams = null;
        }
    }
}
