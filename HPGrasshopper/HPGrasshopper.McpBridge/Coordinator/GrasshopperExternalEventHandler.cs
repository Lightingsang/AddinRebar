namespace HPGrasshopper.McpBridge.Coordinator;

using Autodesk.Revit.UI;

/// <summary>
///     Revit ExternalEventHandler that dispatches execution requests to GrasshopperBridgeCoordinator.
/// </summary>
public sealed class GrasshopperExternalEventHandler : IExternalEventHandler
{
    private readonly GrasshopperBridgeCoordinator _coordinator;

    public GrasshopperExternalEventHandler(GrasshopperBridgeCoordinator coordinator)
    {
        _coordinator = coordinator;
    }

    public void Execute(UIApplication application)
    {
        _coordinator.Execute(application);
    }

    public string GetName() => _coordinator.GetName();
}
