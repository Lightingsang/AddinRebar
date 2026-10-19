namespace HPDynamo.McpBridge.Entry;

using Autodesk.Revit.UI;
using HPDynamo.McpBridge.Coordinator;

/// <summary>
///     Adapts Revit ExternalEvent to IExternalEventRaiser.
/// </summary>
internal sealed class RevitExternalEventRaiser : IExternalEventRaiser
{
    private readonly ExternalEvent _event;

    public RevitExternalEventRaiser(ExternalEvent ev)
    {
        _event = ev;
    }

    public bool Raise()
    {
        var status = _event.Raise();
        return status is ExternalEventRequest.Accepted or ExternalEventRequest.Pending;
    }

    public void Dispose()
    {
        _event.Dispose();
    }
}
