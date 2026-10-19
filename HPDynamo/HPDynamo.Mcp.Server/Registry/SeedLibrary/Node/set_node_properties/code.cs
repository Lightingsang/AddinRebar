string targetNodeIdentifier = args.Require("nodeId");
string updatedDisplayName = args.Str("name", "");
string updatedValueString = args.Str("value", "");
bool? freezeOption = args.Has("isFrozen") ? args.Bool("isFrozen", false) : null;
bool? visibilityOption = args.Has("isVisible") ? args.Bool("isVisible", true) : null;
double? targetX = args.DoubleOrNull("x");
double? targetY = args.DoubleOrNull("y");

var activeWorkspaceModel = workspace
    ?? (dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model.CurrentWorkspace
    ?? (dynamo as DynamoModel)?.CurrentWorkspace;

if (activeWorkspaceModel == null)
    throw new InvalidOperationException("No active Dynamo workspace is currently open.");

bool isTargetGuid = Guid.TryParse(targetNodeIdentifier, out Guid targetNodeGuid);
var targetNodeModel = activeWorkspaceModel.Nodes.FirstOrDefault(nodeCandidate =>
    (isTargetGuid && nodeCandidate.GUID == targetNodeGuid) ||
    string.Equals(nodeCandidate.Name, targetNodeIdentifier, StringComparison.OrdinalIgnoreCase))
    ?? throw new ArgumentException($"Node '{targetNodeIdentifier}' not found in active workspace.");

var dynamoModel = dynamo as DynamoModel ?? (dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model;

var updatedPropertiesList = new List<string>();

if (!string.IsNullOrWhiteSpace(updatedDisplayName))
{
    targetNodeModel.Name = updatedDisplayName;
    updatedPropertiesList.Add("name");
}

if (freezeOption.HasValue)
{
    targetNodeModel.IsFrozen = freezeOption.Value;
    updatedPropertiesList.Add("isFrozen");
}

if (visibilityOption.HasValue)
{
    if (dynamoModel != null)
    {
        dynamoModel.ExecuteCommand(new DynamoModel.UpdateModelValueCommand(targetNodeModel.GUID, "IsVisible", visibilityOption.Value.ToString().ToLowerInvariant()));
    }
    else
    {
        targetNodeModel.UpdateValue(new Dynamo.Graph.UpdateValueParams("IsVisible", visibilityOption.Value.ToString().ToLowerInvariant()));
    }
    updatedPropertiesList.Add("isVisible");
}

if (!string.IsNullOrWhiteSpace(updatedValueString))
{
    if (targetNodeModel is CodeBlockNodeModel codeBlockModel)
    {
        codeBlockModel.SetCodeContent(updatedValueString, activeWorkspaceModel.ElementResolver);
        updatedPropertiesList.Add("codeContent");
    }
    else
    {
        targetNodeModel.UpdateValue(new Dynamo.Graph.UpdateValueParams("Value", updatedValueString));
        updatedPropertiesList.Add("value");
    }
}

if (targetX.HasValue || targetY.HasValue)
{
    if ((targetX.HasValue && (double.IsNaN(targetX.Value) || double.IsInfinity(targetX.Value))) ||
        (targetY.HasValue && (double.IsNaN(targetY.Value) || double.IsInfinity(targetY.Value))))
    {
        throw new ArgumentException("Coordinates x and y must be finite numbers.");
    }

    if (targetX.HasValue) targetNodeModel.X = targetX.Value;
    if (targetY.HasValue) targetNodeModel.Y = targetY.Value;
    targetNodeModel.ReportPosition();
    updatedPropertiesList.Add("position");
}

log($"Updated properties on node '{targetNodeModel.Name}' ({targetNodeModel.GUID}): {string.Join(", ", updatedPropertiesList)}");

return new
{
    success = true,
    nodeId = targetNodeModel.GUID.ToString(),
    nodeName = targetNodeModel.Name,
    nickName = targetNodeModel.Name,
    isFrozen = targetNodeModel.IsFrozen,
    isVisible = targetNodeModel.IsVisible,
    updatedProperties = updatedPropertiesList,
    summary = $"Updated {updatedPropertiesList.Count} properties on {targetNodeModel.Name} ({string.Join(", ", updatedPropertiesList)})"
};
