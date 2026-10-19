string requestedNodeIdentifier = args.Require("nodeType");
double coordinateX = args.RequireDouble("x");
double coordinateY = args.RequireDouble("y");

if (double.IsNaN(coordinateX) || double.IsNaN(coordinateY) || double.IsInfinity(coordinateX) || double.IsInfinity(coordinateY))
    throw new ArgumentException("Coordinates X and Y must be finite numbers.");

string customDisplayName = args.Str("name", "");
string initialCodeValue = args.Str("initialValue", "");

var dynamoModel = (dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model ?? (dynamo as DynamoModel);

var currentActiveWorkspace = workspace
    ?? dynamoModel?.CurrentWorkspace
    ?? throw new InvalidOperationException("No active workspace is open to place the node.");

NodeModel newlyCreatedNode;

bool isCodeBlockType = requestedNodeIdentifier.Equals("CodeBlock", StringComparison.OrdinalIgnoreCase) ||
    requestedNodeIdentifier.IndexOf("CodeBlockNodeModel", StringComparison.OrdinalIgnoreCase) >= 0;

if (isCodeBlockType)
{
    string scriptContent = !string.IsNullOrWhiteSpace(initialCodeValue) ? initialCodeValue : "// CodeBlock";
    var codeBlockInstance = new CodeBlockNodeModel(
        scriptContent,
        Guid.NewGuid(),
        coordinateX,
        coordinateY,
        dynamoModel!.LibraryServices,
        currentActiveWorkspace.ElementResolver);

    dynamoModel.ExecuteCommand(new DynamoModel.CreateNodeCommand(codeBlockInstance, coordinateX, coordinateY, false, false));
    newlyCreatedNode = codeBlockInstance;
}
else
{
    var matchedEntry = dynamoModel?.SearchModel?.Entries.FirstOrDefault(entryItem =>
        string.Equals(entryItem.CreationName, requestedNodeIdentifier, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(entryItem.FullName, requestedNodeIdentifier, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(entryItem.Name, requestedNodeIdentifier, StringComparison.OrdinalIgnoreCase));

    if (matchedEntry != null)
    {
        var newNodeGuid = Guid.NewGuid();
        string creationTarget = matchedEntry.CreationName ?? matchedEntry.FullName ?? requestedNodeIdentifier;
        dynamoModel!.ExecuteCommand(new DynamoModel.CreateNodeCommand(newNodeGuid.ToString(), creationTarget, coordinateX, coordinateY, false, false));
        newlyCreatedNode = currentActiveWorkspace.Nodes.FirstOrDefault(nodeItem => nodeItem.GUID == newNodeGuid)
            ?? currentActiveWorkspace.Nodes.LastOrDefault()
            ?? throw new InvalidOperationException($"Failed to construct node of type '{requestedNodeIdentifier}'.");
    }
    else
    {
        throw new ArgumentException($"Node type '{requestedNodeIdentifier}' not found in library. Use search_nodes_library to locate creation names.");
    }
}

if (!string.IsNullOrWhiteSpace(customDisplayName))
{
    newlyCreatedNode.Name = customDisplayName;
}

if (!isCodeBlockType && !string.IsNullOrWhiteSpace(initialCodeValue))
{
    newlyCreatedNode.UpdateValue(new Dynamo.Graph.UpdateValueParams("Value", initialCodeValue));
}

log($"Created node '{newlyCreatedNode.Name}' (ID: {newlyCreatedNode.GUID}) at ({coordinateX}, {coordinateY})");

return new
{
    success = true,
    nodeId = newlyCreatedNode.GUID.ToString(),
    name = newlyCreatedNode.Name,
    nodeType = requestedNodeIdentifier,
    x = coordinateX,
    y = coordinateY,
    inputPortsCount = newlyCreatedNode.InPorts.Count,
    outputPortsCount = newlyCreatedNode.OutPorts.Count,
    summary = $"Placed node '{newlyCreatedNode.Name}' ({newlyCreatedNode.GUID}) on canvas at ({coordinateX}, {coordinateY})."
};
