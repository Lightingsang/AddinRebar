string sourceNodeIdentifier = args.Require("sourceNodeId");
int sourceOutputPortIndex = args.Int("sourcePortIndex", 0);
string targetNodeIdentifier = args.Require("targetNodeId");
int targetInputPortIndex = args.Int("targetPortIndex", 0);

if (sourceOutputPortIndex < 0 || targetInputPortIndex < 0)
    throw new IndexOutOfRangeException($"Port indices must be non-negative (received source: {sourceOutputPortIndex}, target: {targetInputPortIndex}).");

if (string.Equals(sourceNodeIdentifier, targetNodeIdentifier, StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Cyclic self-connection is not permitted: source and target node cannot be the same.");

var dynamoModel = (dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model ?? (dynamo as DynamoModel);

var currentActiveWorkspace = workspace
    ?? dynamoModel?.CurrentWorkspace
    ?? throw new InvalidOperationException("No active workspace is open to connect wires.");

var upstreamSourceNode = currentActiveWorkspace.Nodes.FirstOrDefault(nodeCandidate =>
    string.Equals(nodeCandidate.GUID.ToString(), sourceNodeIdentifier, StringComparison.OrdinalIgnoreCase) ||
    string.Equals(nodeCandidate.Name, sourceNodeIdentifier, StringComparison.OrdinalIgnoreCase))
    ?? throw new ArgumentException($"Source node '{sourceNodeIdentifier}' was not found in the active workspace.");

var downstreamTargetNode = currentActiveWorkspace.Nodes.FirstOrDefault(nodeCandidate =>
    string.Equals(nodeCandidate.GUID.ToString(), targetNodeIdentifier, StringComparison.OrdinalIgnoreCase) ||
    string.Equals(nodeCandidate.Name, targetNodeIdentifier, StringComparison.OrdinalIgnoreCase))
    ?? throw new ArgumentException($"Target node '{targetNodeIdentifier}' was not found in the active workspace.");

if (upstreamSourceNode.GUID == downstreamTargetNode.GUID)
    throw new InvalidOperationException("Cyclic self-connection is not permitted: source and target node cannot be the same.");

if (sourceOutputPortIndex >= upstreamSourceNode.OutPorts.Count)
    throw new IndexOutOfRangeException(
        $"Source port index {sourceOutputPortIndex} is out of bounds for node '{upstreamSourceNode.Name}' which has {upstreamSourceNode.OutPorts.Count} output ports.");

if (targetInputPortIndex >= downstreamTargetNode.InPorts.Count)
    throw new IndexOutOfRangeException(
        $"Target port index {targetInputPortIndex} is out of bounds for node '{downstreamTargetNode.Name}' which has {downstreamTargetNode.InPorts.Count} input ports.");

dynamoModel!.ExecuteCommand(new DynamoModel.MakeConnectionCommand(
    upstreamSourceNode.GUID,
    sourceOutputPortIndex,
    PortType.Output,
    DynamoModel.MakeConnectionCommand.Mode.Begin));

dynamoModel.ExecuteCommand(new DynamoModel.MakeConnectionCommand(
    downstreamTargetNode.GUID,
    targetInputPortIndex,
    PortType.Input,
    DynamoModel.MakeConnectionCommand.Mode.End));

int updatedTotalWiresCount = currentActiveWorkspace.Connectors.Count();

log($"Connected {upstreamSourceNode.Name}[{sourceOutputPortIndex}] -> {downstreamTargetNode.Name}[{targetInputPortIndex}] (Total wires: {updatedTotalWiresCount})");

return new
{
    success = true,
    sourceNodeId = upstreamSourceNode.GUID.ToString(),
    sourceNodeName = upstreamSourceNode.Name,
    sourcePortIndex = sourceOutputPortIndex,
    targetNodeId = downstreamTargetNode.GUID.ToString(),
    targetNodeName = downstreamTargetNode.Name,
    targetPortIndex = targetInputPortIndex,
    totalWiresCount = updatedTotalWiresCount,
    summary = $"Connected {upstreamSourceNode.Name}[{sourceOutputPortIndex}] to {downstreamTargetNode.Name}[{targetInputPortIndex}]. Active wires: {updatedTotalWiresCount}."
};
