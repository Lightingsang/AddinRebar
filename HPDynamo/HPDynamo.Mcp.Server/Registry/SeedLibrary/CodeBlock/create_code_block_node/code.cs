string code = args.Require("code");
double x = args.Double("x", 100.0);
double y = args.Double("y", 100.0);
string title = args.Str("title", "");

if (double.IsNaN(x) || double.IsNaN(y) || double.IsInfinity(x) || double.IsInfinity(y))
    throw new ArgumentException("Coordinates x and y must be finite numbers.");

var activeWorkspaceModel = workspace
    ?? (dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model.CurrentWorkspace
    ?? (dynamo as DynamoModel)?.CurrentWorkspace;

if (activeWorkspaceModel == null)
    throw new InvalidOperationException("No active Dynamo workspace is currently open.");

var dynamoModel = dynamo as DynamoModel ?? (dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model;
if (dynamoModel == null)
    throw new InvalidOperationException("DynamoModel is not available.");

var codeBlockNode = new CodeBlockNodeModel(code, Guid.NewGuid(), x, y, dynamoModel.LibraryServices, activeWorkspaceModel.ElementResolver);
if (!string.IsNullOrWhiteSpace(title))
{
    codeBlockNode.Name = title;
}

dynamoModel.ExecuteCommand(new DynamoModel.CreateNodeCommand(codeBlockNode, x, y, false, false));

var inputPortNames = codeBlockNode.InPorts.Select(p => p.Name).ToList();
var outputPortNames = codeBlockNode.OutPorts.Select(p => p.Name).ToList();

log($"Created Code Block node '{codeBlockNode.GUID}' at ({x:F1}, {y:F1}) with {inputPortNames.Count} input(s) and {outputPortNames.Count} output(s).");

return new
{
    success = true,
    nodeId = codeBlockNode.GUID.ToString(),
    name = codeBlockNode.Name,
    nickName = codeBlockNode.Name,
    x,
    y,
    code,
    inPorts = inputPortNames,
    outPorts = outputPortNames,
    summary = $"Created CodeBlockNode '{codeBlockNode.GUID}' at ({x:F1}, {y:F1}) with {inputPortNames.Count} inputs and {outputPortNames.Count} outputs"
};
