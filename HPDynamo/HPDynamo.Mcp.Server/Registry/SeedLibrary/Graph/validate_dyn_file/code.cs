string fileContent = args.Str("fileContent", "");
string filePath = args.Str("filePath", "");
bool checkPackages = args.Bool("checkPackages", true);

var activeWorkspaceModel = workspace
    ?? (dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model.CurrentWorkspace
    ?? (dynamo as DynamoModel)?.CurrentWorkspace;

var issues = new List<string>();
var warnings = new List<string>();
int nodeCount = 0;
int wireCount = 0;
string graphName = "Unknown";
string graphId = string.Empty;
var packageDependencies = new List<string>();

void InspectJsonNodes(System.Text.Json.JsonElement rootElement)
{
    if (!rootElement.TryGetProperty("Nodes", out var nodesProp) || nodesProp.ValueKind != System.Text.Json.JsonValueKind.Array)
    {
        issues.Add("Missing or invalid 'Nodes' array in graph JSON.");
        return;
    }

    nodeCount = nodesProp.GetArrayLength();
    foreach (var nodeElement in nodesProp.EnumerateArray())
    {
        if (!nodeElement.TryGetProperty("NodeType", out var nodeTypeProp) || nodeTypeProp.GetString() != "DummyNodeModel")
            continue;

        string dummyName = nodeElement.TryGetProperty("Name", out var nameProp) ? nameProp.GetString() ?? "Unknown" : "Unknown";
        string dummyId = nodeElement.TryGetProperty("Id", out var idProp) ? idProp.GetString() ?? "Unknown" : "Unknown";
        issues.Add($"Unresolved/Dummy node detected: {dummyName} ({dummyId})");
    }
}

void InspectJsonConnectors(System.Text.Json.JsonElement rootElement)
{
    if (!rootElement.TryGetProperty("Connectors", out var connsProp) || connsProp.ValueKind != System.Text.Json.JsonValueKind.Array)
        return;

    wireCount = connsProp.GetArrayLength();
    foreach (var connectorElement in connsProp.EnumerateArray())
    {
        if (!connectorElement.TryGetProperty("Start", out var start) || !connectorElement.TryGetProperty("End", out var end))
            continue;

        string startId = start.ValueKind == System.Text.Json.JsonValueKind.String
            ? start.GetString()
            : (start.ValueKind == System.Text.Json.JsonValueKind.Object && start.TryGetProperty("Id", out var sid) ? sid.GetString() : null);

        string endId = end.ValueKind == System.Text.Json.JsonValueKind.String
            ? end.GetString()
            : (end.ValueKind == System.Text.Json.JsonValueKind.Object && end.TryGetProperty("Id", out var eid) ? eid.GetString() : null);

        if (!string.IsNullOrEmpty(startId) && startId == endId)
            issues.Add($"Cyclic self-connection detected on node {startId}");
    }
}

void InspectJsonPackages(System.Text.Json.JsonElement rootElement)
{
    if (!checkPackages) return;
    if (!rootElement.TryGetProperty("NodeLibraryDependencies", out var pkgProp) || pkgProp.ValueKind != System.Text.Json.JsonValueKind.Array)
        return;

    foreach (var packageElement in pkgProp.EnumerateArray())
    {
        if (!packageElement.TryGetProperty("Name", out var packageNameProp))
            continue;

        string packageName = packageNameProp.GetString();
        if (!string.IsNullOrEmpty(packageName))
            packageDependencies.Add(packageName);
    }
}

if (!string.IsNullOrWhiteSpace(fileContent))
{
    try
    {
        using var jsonDocument = System.Text.Json.JsonDocument.Parse(fileContent);
        var rootElement = jsonDocument.RootElement;

        if (rootElement.TryGetProperty("Name", out var nameProp)) graphName = nameProp.GetString() ?? graphName;
        if (rootElement.TryGetProperty("Uuid", out var uuidProp)) graphId = uuidProp.GetString() ?? string.Empty;

        InspectJsonNodes(rootElement);
        InspectJsonConnectors(rootElement);
        InspectJsonPackages(rootElement);
    }
    catch (System.Text.Json.JsonException parseException)
    {
        issues.Add($"Invalid JSON syntax in .dyn file: {parseException.Message}");
    }
}
else if (activeWorkspaceModel != null && (string.IsNullOrWhiteSpace(filePath) || string.Equals(activeWorkspaceModel.FileName, filePath, StringComparison.OrdinalIgnoreCase)))
{
    graphName = activeWorkspaceModel.Name;
    graphId = activeWorkspaceModel.FileName ?? "ActiveCanvas";
    nodeCount = activeWorkspaceModel.Nodes.Count();
    wireCount = activeWorkspaceModel.Connectors.Count();

    foreach (var nodeModel in activeWorkspaceModel.Nodes)
    {
        string nodeMsg = nodeModel.NodeInfos.Any()
            ? string.Join("; ", nodeModel.NodeInfos.Select(i => i.Message))
            : (nodeModel.Description ?? "");

        if (nodeModel.State == Dynamo.Graph.Nodes.ElementState.Error)
            issues.Add($"Node in error state: '{nodeModel.Name}' ({nodeModel.GUID}): {nodeMsg}");
        else if (nodeModel.State == Dynamo.Graph.Nodes.ElementState.Warning)
            warnings.Add($"Node warning: '{nodeModel.Name}' ({nodeModel.GUID}): {nodeMsg}");
    }

    if (checkPackages && activeWorkspaceModel.Dependencies != null)
    {
        foreach (var dependencyItem in activeWorkspaceModel.Dependencies)
        {
            packageDependencies.Add(dependencyItem.ToString());
        }
    }
}
else
{
    throw new ArgumentException("Provide 'fileContent' (raw JSON of the .dyn graph) or ensure the graph is loaded in the active Dynamo workspace.");
}

bool isValid = issues.Count == 0;
log($"Validated graph '{graphName}': Valid={isValid}, Issues={issues.Count}, Warnings={warnings.Count}, Nodes={nodeCount}, Wires={wireCount}");

return new
{
    success = true,
    isValid,
    graphName,
    graphId,
    nodeCount,
    wireCount,
    issuesCount = issues.Count,
    issues,
    warningsCount = warnings.Count,
    warnings,
    packageDependencies,
    summary = isValid ? $"Graph '{graphName}' is valid ({nodeCount} nodes, {wireCount} wires)" : $"Graph '{graphName}' has {issues.Count} validation issue(s)"
};
