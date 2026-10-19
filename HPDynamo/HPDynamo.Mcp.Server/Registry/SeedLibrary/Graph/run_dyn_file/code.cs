string targetGraphPath = args.Require("filePath");
if (string.IsNullOrWhiteSpace(targetGraphPath))
    throw new ArgumentException("Parameter filePath cannot be empty.");

if (!targetGraphPath.EndsWith(".dyn", StringComparison.OrdinalIgnoreCase))
    throw new ArgumentException($"File path '{targetGraphPath}' must have a .dyn extension.");

bool forceHeadlessMode = args.Bool("runHeadless", false);
bool shouldExtractOutputs = args.Bool("extractOutputs", true);
var inputOverrides = args.Obj("inputs");

log($"Beginning execution of Dynamo graph '{targetGraphPath}'");

var dynamoViewModel = dynamo as Dynamo.ViewModels.DynamoViewModel;
var isCanvasAvailable = dynamoViewModel != null && !forceHeadlessMode;

int totalNodesEvaluated = 0;
int errorNodesCount = 0;
var extractedOutputs = new Dictionary<string, string>();
string executionMode = isCanvasAvailable ? "Canvas" : "Headless";

void ApplyOverrides(HomeWorkspaceModel canvasWorkspace)
{
    if (inputOverrides.IsEmpty) return;

    foreach (var nodeItem in canvasWorkspace.Nodes)
    {
        ct.ThrowIfCancellationRequested();
        string nodeIdentifier = nodeItem.Name;
        string nodeGuidString = nodeItem.GUID.ToString();

        if (inputOverrides.Has(nodeIdentifier))
        {
            string overrideText = inputOverrides.Str(nodeIdentifier, "");
            nodeItem.UpdateValue(new Dynamo.Graph.UpdateValueParams("Value", overrideText));
            log($"Applied input override to node '{nodeIdentifier}': {overrideText}");
        }
        else if (inputOverrides.Has(nodeGuidString))
        {
            string overrideText = inputOverrides.Str(nodeGuidString, "");
            nodeItem.UpdateValue(new Dynamo.Graph.UpdateValueParams("Value", overrideText));
            log($"Applied input override to node GUID '{nodeGuidString}': {overrideText}");
        }
    }
}

void ExtractTerminalOutputs(HomeWorkspaceModel canvasWorkspace, Dynamo.ViewModels.DynamoViewModel activeVm)
{
    if (!shouldExtractOutputs) return;

    var engineCtrl = activeVm.Model.EngineController;
    if (engineCtrl == null) return;

    var upstreamOwners = new HashSet<NodeModel>(
        canvasWorkspace.Connectors
            .Select(wireItem => wireItem.Start?.Owner)
            .Where(owner => owner != null)!);

    foreach (var nodeItem in canvasWorkspace.Nodes)
    {
        bool isTerminalNode = nodeItem.OutPorts.Count > 0 && !upstreamOwners.Contains(nodeItem);
        if (!isTerminalNode) continue;

        string outputValueText = nodeItem.GetValue(0, engineCtrl)?.Data?.ToString() ?? "null";
        string key = $"{nodeItem.Name} ({nodeItem.GUID.ToString().Substring(0, 8)})";
        extractedOutputs[key] = outputValueText;
    }
}

if (isCanvasAvailable)
{
    dynamoViewModel!.OpenIfSavedCommand.Execute(new DynamoModel.OpenFileCommand(targetGraphPath, false));
    var activeCanvasWs = dynamoViewModel.Model.CurrentWorkspace as HomeWorkspaceModel
        ?? throw new InvalidOperationException("Failed to open graph onto active HomeWorkspace canvas.");

    ApplyOverrides(activeCanvasWs);

    bool evalSuccess = globals.RunAndWait(() => activeCanvasWs.Run());
    if (!evalSuccess)
    {
        if (ct.IsCancellationRequested)
            throw new OperationCanceledException("Dynamo evaluation was cancelled.", ct);
        throw new InvalidOperationException($"Dynamo evaluation failed or timed out for graph '{targetGraphPath}'.");
    }

    totalNodesEvaluated = activeCanvasWs.Nodes.Count();
    errorNodesCount = activeCanvasWs.Nodes.Count(n => n.State == ElementState.Error);

    ExtractTerminalOutputs(activeCanvasWs, dynamoViewModel);
}
else
{
    var revitDynamo = (dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model ?? (dynamo as DynamoModel);
    if (revitDynamo == null)
        throw new InvalidOperationException("Dynamo model instance is not available for headless execution.");

    revitDynamo.ExecuteCommand(new DynamoModel.OpenFileCommand(targetGraphPath, false));
    var headlessWorkspace = revitDynamo.CurrentWorkspace as HomeWorkspaceModel
        ?? throw new InvalidOperationException("Failed to open graph onto headless HomeWorkspace.");

    ApplyOverrides(headlessWorkspace);

    bool evalSuccess = globals.RunAndWait(() => headlessWorkspace.Run());
    if (!evalSuccess)
    {
        if (ct.IsCancellationRequested)
            throw new OperationCanceledException("Dynamo evaluation was cancelled.", ct);
        throw new InvalidOperationException($"Dynamo evaluation failed or timed out for graph '{targetGraphPath}'.");
    }

    totalNodesEvaluated = headlessWorkspace.Nodes.Count();
    errorNodesCount = headlessWorkspace.Nodes.Count(n => n.State == ElementState.Error);
}

if (errorNodesCount > 0)
{
    throw new InvalidOperationException($"Graph '{targetGraphPath}' execution failed with {errorNodesCount} error node(s).");
}

log($"Graph execution completed ({executionMode}): {totalNodesEvaluated} nodes, {errorNodesCount} errors");

return new
{
    success = errorNodesCount == 0,
    filePath = targetGraphPath,
    mode = executionMode,
    nodeCount = totalNodesEvaluated,
    errorCount = errorNodesCount,
    outputs = extractedOutputs,
    summary = $"Graph '{targetGraphPath}' executed ({executionMode}) with {totalNodesEvaluated} nodes and {errorNodesCount} errors."
};
