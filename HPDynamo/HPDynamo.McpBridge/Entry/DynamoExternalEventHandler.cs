namespace HPDynamo.McpBridge.Entry;

using Autodesk.Revit.UI;
using HPDynamo.McpBridge.Coordinator;

/// <summary>
///     Revit ExternalEventHandler that dispatches execution requests to DynamoBridgeCoordinator.
/// </summary>
public sealed class DynamoExternalEventHandler : IExternalEventHandler
{
    private readonly DynamoBridgeCoordinator _coordinator;

    public DynamoExternalEventHandler(DynamoBridgeCoordinator coordinator)
    {
        _coordinator = coordinator;
    }

    public void Execute(UIApplication application)
    {
        _coordinator.Execute(application);
    }

    public string GetName() => _coordinator.GetName();
}
