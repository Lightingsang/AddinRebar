string primaryNodeIdentifier = args.Require("nodeId");
int portIndex = args.Int("portIndex", 0);
string portType = args.Str("portType", "input").ToLowerInvariant();
string targetNodeId = args.Str("targetNodeId", "");
int targetPortIndex = args.Int("targetPortIndex", -1);

var activeWorkspaceModel = workspace
    ?? (dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model.CurrentWorkspace
    ?? (dynamo as DynamoModel)?.CurrentWorkspace;

if (activeWorkspaceModel == null)
    throw new InvalidOperationException("No active Dynamo workspace is currently open.");

bool isPrimaryGuid = Guid.TryParse(primaryNodeIdentifier, out Guid primaryGuid);
var primaryNode = activeWorkspaceModel.Nodes.FirstOrDefault(nodeCandidate =>
    (isPrimaryGuid && nodeCandidate.GUID == primaryGuid) ||
    string.Equals(nodeCandidate.Name, primaryNodeIdentifier, StringComparison.OrdinalIgnoreCase))
    ?? throw new ArgumentException($"Node '{primaryNodeIdentifier}' not found in active workspace.");

bool isInputPort = portType == "input" || portType == "in";
int availablePortCount = isInputPort ? primaryNode.InPorts.Count : primaryNode.OutPorts.Count;
if (portIndex < 0 || portIndex >= availablePortCount)
    throw new IndexOutOfRangeException($"Port index {portIndex} is out of range for node '{primaryNode.Name}' ({portType} port count: {availablePortCount}).");

NodeModel targetNode = null;
if (!string.IsNullOrWhiteSpace(targetNodeId))
{
    bool isTargetGuid = Guid.TryParse(targetNodeId, out Guid targetGuid);
    targetNode = activeWorkspaceModel.Nodes.FirstOrDefault(nodeCandidate =>
        (isTargetGuid && nodeCandidate.GUID == targetGuid) ||
        string.Equals(nodeCandidate.Name, targetNodeId, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException($"Target node '{targetNodeId}' not found in active workspace.");
}

var matchingWires = activeWorkspaceModel.Connectors.Where(connectorItem =>
{
    if (isInputPort)
    {
        if (connectorItem.End == null || connectorItem.End.Owner != primaryNode || connectorItem.End.Index != portIndex) return false;
        if (targetNode != null && (connectorItem.Start == null || connectorItem.Start.Owner != targetNode)) return false;
        if (targetPortIndex >= 0 && (connectorItem.Start == null || connectorItem.Start.Index != targetPortIndex)) return false;
        return true;
    }
    else
    {
        if (connectorItem.Start == null || connectorItem.Start.Owner != primaryNode || connectorItem.Start.Index != portIndex) return false;
        if (targetNode != null && (connectorItem.End == null || connectorItem.End.Owner != targetNode)) return false;
        if (targetPortIndex >= 0 && (connectorItem.End == null || connectorItem.End.Index != targetPortIndex)) return false;
        return true;
    }
}).ToList();

if (matchingWires.Count == 0)
{
    log($"No matching wires found for node '{primaryNode.Name}' port {portIndex} ({portType}).");
    return new
    {
        success = true,
        nodeId = primaryNode.GUID.ToString(),
        nodeName = primaryNode.Name,
        portIndex,
        portType,
        disconnectedCount = 0,
        summary = $"No wires connected to {primaryNode.Name} port {portIndex} ({portType}) matched criteria."
    };
}

var dynamoModel = dynamo as DynamoModel ?? (dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model;
var disconnectedWires = new List<object>();

foreach (var connectorItem in matchingWires)
{
    ct.ThrowIfCancellationRequested();
    disconnectedWires.Add(new
    {
        wireId = connectorItem.GUID.ToString(),
        sourceNode = connectorItem.Start.Owner.Name,
        sourceNodeId = connectorItem.Start.Owner.GUID.ToString(),
        sourcePort = connectorItem.Start.Index,
        targetNode = connectorItem.End.Owner.Name,
        targetNodeId = connectorItem.End.Owner.GUID.ToString(),
        targetPort = connectorItem.End.Index
    });

    if (dynamoModel != null)
    {
        dynamoModel.ExecuteCommand(new DynamoModel.DeleteModelCommand(connectorItem.GUID));
    }
}

log($"Disconnected {disconnectedWires.Count} wire(s) on node '{primaryNode.Name}' port {portIndex} ({portType}).");

return new
{
    success = true,
    nodeId = primaryNode.GUID.ToString(),
    nodeName = primaryNode.Name,
    portIndex,
    portType,
    disconnectedCount = disconnectedWires.Count,
    disconnectedWires,
    summary = $"Disconnected {disconnectedWires.Count} wire(s) from {primaryNode.Name} port {portIndex} ({portType})"
};
