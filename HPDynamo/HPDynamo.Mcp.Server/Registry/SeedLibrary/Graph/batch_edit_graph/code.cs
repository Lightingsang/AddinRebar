var nodesList = args.List("nodes");
var wiresList = args.List("wires");
var propsList = args.List("properties");
bool runEvaluation = args.Bool("runEvaluation", false);
bool dryRun = args.Bool("dryRun", false) || (globals != null && globals.dryRun);

var dynamoModel = (dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model ?? (dynamo as DynamoModel);
var currentWorkspace = workspace ?? dynamoModel?.CurrentWorkspace ?? throw new InvalidOperationException("No active Dynamo workspace is open for batch editing.");
var homeWs = currentWorkspace as HomeWorkspaceModel;
var prevRunType = homeWs?.RunSettings.RunType;
if (homeWs != null) homeWs.RunSettings.RunType = RunType.Manual;

var tempIdMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
var createdNodeGuids = new List<Guid>();
var createdWiresBetweenExistingNodes = new List<Guid>();
var replacedWires = new List<(Guid Source, int SPort, Guid Target, int TPort)>();
var modifiedProperties = new List<(Guid NodeGuid, string Property, string? OriginalValue)>();
var touchedTargetPorts = new HashSet<(Guid Target, int Port)>();
var codeBlockWires = new List<(Guid SGuid, int SPort, Guid TGuid, int TPort)>();
var createdNodesSummary = new List<object>();
int connectedWiresCount = 0; int updatedPropertiesCount = 0;

string? GetNodeValue(NodeModel node)
{
    if (node is CodeBlockNodeModel cbn) return cbn.Code;
    try
    {
        var controller = dynamoModel?.EngineController;
        if (controller != null && node.OutPorts.Count > 0)
        {
            var nodeData = node.GetValue(0, controller)?.Data;
            if (nodeData != null) return nodeData is IFormattable formattable ? formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture) : nodeData.ToString();
        }
    }
    catch { /* Engine output unavailable */ }
    return null;
}

void SnapshotCodeBlockWires(NodeModel cbnNode)
{
    foreach (var c in currentWorkspace.Connectors)
    {
        if (c.Start?.Owner == cbnNode || c.End?.Owner == cbnNode)
            codeBlockWires.Add((c.Start.Owner.GUID, c.Start.Index, c.End.Owner.GUID, c.End.Index));
    }
}

NodeModel CreateSingleNode(string nodeType, double x, double y, string initialValue)
{
    bool isCodeBlock = nodeType.Equals("CodeBlock", StringComparison.OrdinalIgnoreCase) || nodeType.IndexOf("CodeBlockNodeModel", StringComparison.OrdinalIgnoreCase) >= 0;
    if (isCodeBlock)
    {
        string content = !string.IsNullOrWhiteSpace(initialValue) ? initialValue : "// CodeBlock";
        var cbn = new CodeBlockNodeModel(content, Guid.NewGuid(), x, y, dynamoModel!.LibraryServices, currentWorkspace.ElementResolver);
        dynamoModel.ExecuteCommand(new DynamoModel.CreateNodeCommand(cbn, x, y, false, false));
        return cbn;
    }
    var entry = dynamoModel?.SearchModel?.Entries.FirstOrDefault(e =>
        string.Equals(e.CreationName, nodeType, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(e.FullName, nodeType, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(e.Name, nodeType, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException($"Node type '{nodeType}' not found in library.");
    var newGuid = Guid.NewGuid();
    string target = entry.CreationName ?? entry.FullName ?? nodeType;
    dynamoModel!.ExecuteCommand(new DynamoModel.CreateNodeCommand(newGuid.ToString(), target, x, y, false, false));
    return currentWorkspace.Nodes.FirstOrDefault(n => n.GUID == newGuid) ?? currentWorkspace.Nodes.LastOrDefault()
        ?? throw new InvalidOperationException($"Failed to construct node of type '{nodeType}'.");
}

Guid ResolveNodeGuid(string identifier)
{
    if (tempIdMap.TryGetValue(identifier, out var mappedGuid)) return mappedGuid;
    if (Guid.TryParse(identifier, out var parsedGuid)) return parsedGuid;
    var foundNode = currentWorkspace.Nodes.FirstOrDefault(n => string.Equals(n.Name, identifier, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException($"Node identifier '{identifier}' could not be resolved.");
    return foundNode.GUID;
}

void ProcessNodes()
{
    foreach (var nodeArg in nodesList)
    {
        ct.ThrowIfCancellationRequested();
        string nodeType = nodeArg.Require("nodeType");
        double coordinateX = nodeArg.RequireDouble("x");
        double coordinateY = nodeArg.RequireDouble("y");
        if (double.IsNaN(coordinateX) || double.IsNaN(coordinateY) || double.IsInfinity(coordinateX) || double.IsInfinity(coordinateY))
            throw new ArgumentException("Coordinates X and Y must be finite numbers.");

        string tempId = nodeArg.Str("tempId", "");
        string customName = nodeArg.Str("name", "");
        string initialValue = nodeArg.Str("initialValue", "");

        var createdNode = CreateSingleNode(nodeType, coordinateX, coordinateY, initialValue);
        if (!string.IsNullOrWhiteSpace(customName)) createdNode.Name = customName;
        if (createdNode is not CodeBlockNodeModel && !string.IsNullOrWhiteSpace(initialValue))
            createdNode.UpdateValue(new Dynamo.Graph.UpdateValueParams("Value", initialValue));

        createdNodeGuids.Add(createdNode.GUID);
        if (!string.IsNullOrWhiteSpace(tempId)) tempIdMap[tempId] = createdNode.GUID;
        createdNodesSummary.Add(new { tempId = string.IsNullOrWhiteSpace(tempId) ? null : tempId, nodeId = createdNode.GUID.ToString(), name = createdNode.Name, nodeType = nodeType, x = coordinateX, y = coordinateY });
    }
}

void ConnectSingleWire(ScriptArgs wireArg)
{
    ct.ThrowIfCancellationRequested();
    string fromId = wireArg.Require("fromNode");
    int fromPort = wireArg.Int("fromPort", 0);
    string toId = wireArg.Require("toNode");
    int toPort = wireArg.Int("toPort", 0);
    if (fromPort < 0 || toPort < 0) throw new IndexOutOfRangeException($"Port indices must be non-negative ({fromPort}, {toPort}).");

    var sourceGuid = ResolveNodeGuid(fromId);
    var targetGuid = ResolveNodeGuid(toId);
    if (sourceGuid == targetGuid) throw new InvalidOperationException("Cyclic self-connection is not permitted.");

    var srcNode = currentWorkspace.Nodes.FirstOrDefault(n => n.GUID == sourceGuid) ?? throw new ArgumentException($"Source node '{fromId}' not found.");
    var dstNode = currentWorkspace.Nodes.FirstOrDefault(n => n.GUID == targetGuid) ?? throw new ArgumentException($"Target node '{toId}' not found.");
    if (fromPort >= srcNode.OutPorts.Count) throw new IndexOutOfRangeException($"Source port index {fromPort} out of bounds for '{srcNode.Name}'.");
    if (toPort >= dstNode.InPorts.Count) throw new IndexOutOfRangeException($"Target port index {toPort} out of bounds for '{dstNode.Name}'.");

    var oldWire = currentWorkspace.Connectors.FirstOrDefault(c => c.End != null && c.End.Owner.GUID == targetGuid && c.End.Index == toPort);
    if (oldWire?.Start != null && touchedTargetPorts.Add((targetGuid, toPort)))
        replacedWires.Add((oldWire.Start.Owner.GUID, oldWire.Start.Index, targetGuid, toPort));

    dynamoModel!.ExecuteCommand(new DynamoModel.MakeConnectionCommand(sourceGuid, fromPort, PortType.Output, DynamoModel.MakeConnectionCommand.Mode.Begin));
    dynamoModel.ExecuteCommand(new DynamoModel.MakeConnectionCommand(targetGuid, toPort, PortType.Input, DynamoModel.MakeConnectionCommand.Mode.End));

    if (!createdNodeGuids.Contains(sourceGuid) && !createdNodeGuids.Contains(targetGuid))
    {
        var newWire = currentWorkspace.Connectors.FirstOrDefault(c => c.Start != null && c.Start.Owner.GUID == sourceGuid && c.Start.Index == fromPort && c.End != null && c.End.Owner.GUID == targetGuid && c.End.Index == toPort);
        if (newWire != null) createdWiresBetweenExistingNodes.Add(newWire.GUID);
    }
    connectedWiresCount++;
}

void ProcessWires() { foreach (var wireArg in wiresList) ConnectSingleWire(wireArg); }

void ApplyPropertyUpdate(NodeModel nodeModel, ScriptArgs propArg, Guid targetGuid)
{
    bool isExisting = !createdNodeGuids.Contains(targetGuid);
    string newName = propArg.Str("name", "");
    if (!string.IsNullOrWhiteSpace(newName))
    {
        if (isExisting) modifiedProperties.Add((targetGuid, "name", nodeModel.Name));
        nodeModel.Name = newName;
    }

    string newValue = propArg.Str("value", "");
    if (!string.IsNullOrWhiteSpace(newValue))
    {
        string? origVal = GetNodeValue(nodeModel);
        if (isExisting) modifiedProperties.Add((targetGuid, "value", origVal));
        if (nodeModel is CodeBlockNodeModel cbnModel)
        {
            SnapshotCodeBlockWires(cbnModel);
            cbnModel.SetCodeContent(newValue, currentWorkspace.ElementResolver);
        }
        else nodeModel.UpdateValue(new Dynamo.Graph.UpdateValueParams("Value", newValue));
    }

    if (propArg.Has("isFrozen"))
    {
        if (isExisting) modifiedProperties.Add((targetGuid, "isFrozen", nodeModel.IsFrozen.ToString()));
        nodeModel.IsFrozen = propArg.Bool("isFrozen", false);
    }

    double? newX = propArg.DoubleOrNull("x");
    double? newY = propArg.DoubleOrNull("y");
    if (newX.HasValue)
    {
        if (isExisting) modifiedProperties.Add((targetGuid, "x", nodeModel.X.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        nodeModel.X = newX.Value; nodeModel.ReportPosition();
    }
    if (newY.HasValue)
    {
        if (isExisting) modifiedProperties.Add((targetGuid, "y", nodeModel.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        nodeModel.Y = newY.Value; nodeModel.ReportPosition();
    }
}

void ProcessProperties()
{
    foreach (var propArg in propsList)
    {
        ct.ThrowIfCancellationRequested();
        string targetId = propArg.Require("nodeId");
        var targetGuid = ResolveNodeGuid(targetId);
        var nodeModel = currentWorkspace.Nodes.FirstOrDefault(n => n.GUID == targetGuid) ?? throw new ArgumentException($"Target node '{targetId}' not found.");
        ApplyPropertyUpdate(nodeModel, propArg, targetGuid);
        updatedPropertiesCount++;
    }
}

void RestoreProperty(Guid guid, string prop, string? origVal)
{
    var node = currentWorkspace.Nodes.FirstOrDefault(n => n.GUID == guid);
    if (node == null) return;
    if (prop == "name") node.Name = origVal ?? "";
    else if (prop == "isFrozen" && bool.TryParse(origVal, out bool frozenVal)) node.IsFrozen = frozenVal;
    else if (prop == "value")
    {
        if (node is CodeBlockNodeModel cbn && origVal != null) cbn.SetCodeContent(origVal, currentWorkspace.ElementResolver);
        else if (origVal != null) node.UpdateValue(new Dynamo.Graph.UpdateValueParams("Value", origVal));
    }
    else if ((prop == "x" || prop == "y") && double.TryParse(origVal, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double coordVal))
    {
        if (prop == "x") node.X = coordVal; else node.Y = coordVal;
        node.ReportPosition();
    }
}

void PerformRollback()
{
    foreach (var wireGuid in createdWiresBetweenExistingNodes)
        dynamoModel?.ExecuteCommand(new DynamoModel.DeleteModelCommand(wireGuid));

    for (int i = replacedWires.Count - 1; i >= 0; i--)
    {
        var rw = replacedWires[i];
        dynamoModel?.ExecuteCommand(new DynamoModel.MakeConnectionCommand(rw.Source, rw.SPort, PortType.Output, DynamoModel.MakeConnectionCommand.Mode.Begin));
        dynamoModel?.ExecuteCommand(new DynamoModel.MakeConnectionCommand(rw.Target, rw.TPort, PortType.Input, DynamoModel.MakeConnectionCommand.Mode.End));
    }

    for (int i = modifiedProperties.Count - 1; i >= 0; i--)
    {
        var (guid, prop, origVal) = modifiedProperties[i];
        RestoreProperty(guid, prop, origVal);
    }

    foreach (var (sg, sp, tg, tp) in codeBlockWires)
    {
        dynamoModel?.ExecuteCommand(new DynamoModel.MakeConnectionCommand(sg, sp, PortType.Output, DynamoModel.MakeConnectionCommand.Mode.Begin));
        dynamoModel?.ExecuteCommand(new DynamoModel.MakeConnectionCommand(tg, tp, PortType.Input, DynamoModel.MakeConnectionCommand.Mode.End));
    }

    for (int i = createdNodeGuids.Count - 1; i >= 0; i--)
        dynamoModel?.ExecuteCommand(new DynamoModel.DeleteModelCommand(createdNodeGuids[i]));
}

try
{
    ProcessNodes();
    ProcessWires();
    ProcessProperties();

    if (runEvaluation && homeWs != null && !dryRun)
    {
        bool evalSuccess = RunAndWait(() => homeWs.Run());
        if (!evalSuccess) throw new InvalidOperationException("Dynamo evaluation failed or timed out.");
        if (homeWs.Nodes.Any(n => n.State == ElementState.Error))
            throw new InvalidOperationException("Graph evaluation encountered node errors.");
    }

    if (dryRun) PerformRollback();
}
catch (Exception)
{
    PerformRollback();
    throw;
}
finally
{
    if (homeWs != null && prevRunType.HasValue) homeWs.RunSettings.RunType = prevRunType.Value;
}

log($"Batch graph edit finished: {createdNodesSummary.Count} nodes created, {connectedWiresCount} wires connected, {updatedPropertiesCount} properties updated.");

return new
{
    success = true,
    dryRun = dryRun,
    createdNodesCount = createdNodesSummary.Count,
    connectedWiresCount = connectedWiresCount,
    updatedPropertiesCount = updatedPropertiesCount,
    nodes = createdNodesSummary,
    summary = $"Batch applied {createdNodesSummary.Count} nodes, {connectedWiresCount} wires, {updatedPropertiesCount} property updates."
};
