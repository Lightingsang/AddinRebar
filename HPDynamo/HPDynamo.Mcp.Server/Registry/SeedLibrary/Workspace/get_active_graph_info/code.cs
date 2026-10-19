bool shouldIncludeNodeList = args.Bool("includeNodeList", false);
bool shouldIncludeErrors = args.Bool("includeErrors", true);

var dynamoModel = (dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model ?? (dynamo as DynamoModel);
var currentWorkspaceModel = workspace ?? dynamoModel?.CurrentWorkspace;

if (currentWorkspaceModel == null)
{
    return new
    {
        success = false,
        isWorkspaceOpen = false,
        summary = "No active Dynamo workspace is currently open or loaded in Revit session."
    };
}

string graphName = currentWorkspaceModel.Name;
string graphFilePath = string.IsNullOrEmpty(currentWorkspaceModel.FileName) ? "Unsaved / In-Memory" : currentWorkspaceModel.FileName;
bool isCustomNodeWorkspace = currentWorkspaceModel is CustomNodeWorkspaceModel;
string executionRunType = currentWorkspaceModel is HomeWorkspaceModel homeWs
    ? homeWs.RunSettings.RunType.ToString()
    : "Manual";

int totalNodesCount = currentWorkspaceModel.Nodes.Count();
int totalWiresCount = currentWorkspaceModel.Connectors.Count();
bool hasUnsavedModifications = currentWorkspaceModel.HasUnsavedChanges;

var problematicNodes = currentWorkspaceModel.Nodes
    .Where(n => n.State == ElementState.Error || n.State == ElementState.Warning)
    .ToList();

var errorDetailsList = new List<object>();
if (shouldIncludeErrors)
{
    foreach (var problemNode in problematicNodes)
    {
        ct.ThrowIfCancellationRequested();
        string problemMsg = problemNode.NodeInfos.Any()
            ? string.Join("; ", problemNode.NodeInfos.Select(i => i.Message))
            : (problemNode.Description ?? "No error details available");

        errorDetailsList.Add(new
        {
            nodeId = problemNode.GUID.ToString(),
            nodeName = problemNode.Name,
            state = problemNode.State.ToString(),
            message = problemMsg
        });
    }
}

var detailedNodeRecords = new List<object>();
if (shouldIncludeNodeList)
{
    foreach (var nodeElement in currentWorkspaceModel.Nodes)
    {
        ct.ThrowIfCancellationRequested();
        detailedNodeRecords.Add(new
        {
            nodeId = nodeElement.GUID.ToString(),
            name = nodeElement.Name,
            type = nodeElement.GetType().Name,
            state = nodeElement.State.ToString(),
            inputPortsCount = nodeElement.InPorts.Count,
            outputPortsCount = nodeElement.OutPorts.Count
        });
    }
}

log($"Active graph '{graphName}': {totalNodesCount} nodes, {totalWiresCount} wires, {problematicNodes.Count} issues");

return new
{
    success = true,
    isWorkspaceOpen = true,
    name = graphName,
    filePath = graphFilePath,
    isCustomNode = isCustomNodeWorkspace,
    runType = executionRunType,
    hasUnsavedChanges = hasUnsavedModifications,
    nodesCount = totalNodesCount,
    wiresCount = totalWiresCount,
    errorCount = problematicNodes.Count,
    errors = errorDetailsList,
    nodes = detailedNodeRecords,
    summary = $"Active graph '{graphName}' ({graphFilePath}) has {totalNodesCount} nodes, {totalWiresCount} wires, {problematicNodes.Count} errors."
};
