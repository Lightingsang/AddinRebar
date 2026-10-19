var queriedNodeIds = args.Strings("nodeIds").Where(idText => !string.IsNullOrWhiteSpace(idText)).Distinct().ToList();
bool includeGeometry = args.Bool("includeGeometry", false);
int limit = Math.Clamp(args.Int("limit", 50), 1, 200);

var activeWorkspaceModel = workspace
    ?? (dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model.CurrentWorkspace
    ?? (dynamo as DynamoModel)?.CurrentWorkspace;

if (activeWorkspaceModel == null)
    throw new InvalidOperationException("No active Dynamo workspace is currently open.");

var dynamoModel = dynamo as DynamoModel ?? (dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model;
var engineController = dynamoModel?.EngineController;

IEnumerable<NodeModel> candidates = activeWorkspaceModel.Nodes;
if (queriedNodeIds.Count > 0)
{
    var idLookupSet = new HashSet<string>(queriedNodeIds, StringComparer.OrdinalIgnoreCase);
    candidates = activeWorkspaceModel.Nodes.Where(n => idLookupSet.Contains(n.GUID.ToString()) || idLookupSet.Contains(n.Name));
}

var inspectionResults = new List<object>();
const int MaxDepth = 6;
const int MaxBreadth = 100;
const int MaxTotalItemsPerPort = 500;
const int MaxStringLength = 2000;
const int MaxPayloadBytesPerPort = 64 * 1024;
int portItemCount = 0;
int portByteCount = 0;
bool portTruncated = false;
string? portTruncationReason = null;

object? SerializeDictionary(DesignScript.Builtin.Dictionary dsDict, int depth)
{
    var dictResult = new Dictionary<string, object?>();
    int count = 0;
    foreach (var key in dsDict.Keys)
    {
        ct.ThrowIfCancellationRequested();
        if (count++ >= MaxBreadth || portItemCount++ >= MaxTotalItemsPerPort || portByteCount >= MaxPayloadBytesPerPort)
        {
            portTruncated = true;
            portTruncationReason ??= portByteCount >= MaxPayloadBytesPerPort ? "byte_budget_exceeded" : (portItemCount >= MaxTotalItemsPerPort ? "item_budget_exceeded" : "breadth_limit_exceeded");
            break;
        }
        try
        {
            var entryValue = dsDict.ValueAtKey(key);
            dictResult[key] = SerializePayload(entryValue, depth + 1);
        }
        catch (Exception ex)
        {
            // Port evaluation error for specific dictionary key
            dictResult[key] = $"[Error reading key: {ex.Message}]";
        }
    }
    return dictResult;
}

object? SerializeClrDictionary(System.Collections.IDictionary dict, int depth)
{
    var dictResult = new Dictionary<string, object?>();
    int count = 0;
    foreach (System.Collections.DictionaryEntry entry in dict)
    {
        ct.ThrowIfCancellationRequested();
        if (count++ >= MaxBreadth || portItemCount++ >= MaxTotalItemsPerPort || portByteCount >= MaxPayloadBytesPerPort)
        {
            portTruncated = true;
            portTruncationReason ??= portByteCount >= MaxPayloadBytesPerPort ? "byte_budget_exceeded" : (portItemCount >= MaxTotalItemsPerPort ? "item_budget_exceeded" : "breadth_limit_exceeded");
            break;
        }
        string key = entry.Key?.ToString() ?? "";
        dictResult[key] = SerializePayload(entry.Value, depth + 1);
    }
    return dictResult;
}

object? SerializeEnumerable(System.Collections.IEnumerable enumerable, int depth)
{
    var listResult = new List<object?>();
    int count = 0;
    foreach (var item in enumerable)
    {
        ct.ThrowIfCancellationRequested();
        if (count++ >= MaxBreadth || portItemCount++ >= MaxTotalItemsPerPort || portByteCount >= MaxPayloadBytesPerPort)
        {
            portTruncated = true;
            portTruncationReason ??= portByteCount >= MaxPayloadBytesPerPort ? "byte_budget_exceeded" : (portItemCount >= MaxTotalItemsPerPort ? "item_budget_exceeded" : "breadth_limit_exceeded");
            break;
        }
        listResult.Add(SerializePayload(item, depth + 1));
    }
    return listResult;
}

string TruncateString(string s)
{
    if (s.Length > MaxStringLength)
    {
        portTruncated = true;
        portTruncationReason ??= "string_length_exceeded";
        s = s.Substring(0, MaxStringLength) + "... [truncated]";
    }
    portByteCount += System.Text.Encoding.UTF8.GetByteCount(s);
    return s;
}

object? SerializePayload(object? value, int depth)
{
    if (value == null) return null;
    ct.ThrowIfCancellationRequested();

    if (depth > MaxDepth || portItemCount >= MaxTotalItemsPerPort || portByteCount >= MaxPayloadBytesPerPort)
    {
        portTruncated = true;
        portTruncationReason ??= portByteCount >= MaxPayloadBytesPerPort ? "byte_budget_exceeded" : (portItemCount >= MaxTotalItemsPerPort ? "item_budget_exceeded" : "depth_limit_reached");
        return portByteCount >= MaxPayloadBytesPerPort || portItemCount >= MaxTotalItemsPerPort ? "[Max items reached]" : value.ToString();
    }

    if (includeGeometry && value is Autodesk.DesignScript.Geometry.Point pt)
        return new { type = "Point", x = pt.X, y = pt.Y, z = pt.Z };
    if (includeGeometry && value is Autodesk.DesignScript.Geometry.Curve crv)
        return new { type = crv.GetType().Name, length = crv.Length };

    if (value is string str) return TruncateString(str);
    if (value is bool || value is int || value is double || value is float || value is long)
    {
        portByteCount += 8;
        return value;
    }

    if (value is DesignScript.Builtin.Dictionary dsDict) return SerializeDictionary(dsDict, depth);
    if (value is System.Collections.IDictionary dict) return SerializeClrDictionary(dict, depth);
    if (value is System.Collections.IEnumerable enumerable) return SerializeEnumerable(enumerable, depth);

    return TruncateString(value.ToString() ?? "");
}

object? SerializeMirrorData(ProtoCore.Mirror.MirrorData? mirror, int depth)
{
    if (mirror == null) return null;
    ct.ThrowIfCancellationRequested();

    if (depth > MaxDepth || portItemCount >= MaxTotalItemsPerPort || portByteCount >= MaxPayloadBytesPerPort)
    {
        portTruncated = true;
        portTruncationReason ??= portByteCount >= MaxPayloadBytesPerPort ? "byte_budget_exceeded" : (portItemCount >= MaxTotalItemsPerPort ? "item_budget_exceeded" : "depth_limit_reached");
        return portByteCount >= MaxPayloadBytesPerPort || portItemCount >= MaxTotalItemsPerPort ? "[Max items reached]" : "[Depth limit reached]";
    }

    if (mirror.IsCollection)
    {
        var elements = mirror.GetElements();
        if (elements == null) return new List<object?>();

        var listResult = new List<object?>();
        int count = 0;
        foreach (var elem in elements)
        {
            ct.ThrowIfCancellationRequested();
            if (count++ >= MaxBreadth || portItemCount++ >= MaxTotalItemsPerPort || portByteCount >= MaxPayloadBytesPerPort)
            {
                portTruncated = true;
                portTruncationReason ??= portByteCount >= MaxPayloadBytesPerPort ? "byte_budget_exceeded" : (portItemCount >= MaxTotalItemsPerPort ? "item_budget_exceeded" : "breadth_limit_exceeded");
                break;
            }
            listResult.Add(SerializeMirrorData(elem, depth + 1));
        }
        return listResult;
    }

    return SerializePayload(mirror.Data, depth);
}

object? EvaluatePortOutput(NodeModel inspectedNode, int portIndex, out string outputTypeName)
{
    outputTypeName = "None";
    if (engineController == null) return null;
    portItemCount = 0;
    portByteCount = 0;
    portTruncated = false;
    portTruncationReason = null;

    try
    {
        var mirrorData = inspectedNode.GetValue(portIndex, engineController);
        if (mirrorData == null) return null;

        if (mirrorData.IsCollection)
        {
            outputTypeName = "List";
            return SerializeMirrorData(mirrorData, 0);
        }

        if (mirrorData.Data == null) return null;

        var payload = mirrorData.Data;
        outputTypeName = payload is DesignScript.Builtin.Dictionary ? "Dictionary" : payload.GetType().Name;
        return SerializePayload(payload, 0);
    }
    catch (Exception evaluationException)
    {
        // Port evaluation failure logged into output model
        outputTypeName = "Error";
        return $"[Evaluation error: {evaluationException.Message}]";
    }
}

foreach (var inspectedNode in candidates.Take(limit))
{
    ct.ThrowIfCancellationRequested();
    var portOutputDescriptions = new List<object>();

    for (int portIndex = 0; portIndex < inspectedNode.OutPorts.Count; portIndex++)
    {
        var portModel = inspectedNode.OutPorts[portIndex];
        object? portOutput = EvaluatePortOutput(inspectedNode, portIndex, out string outputTypeName);

        portOutputDescriptions.Add(new
        {
            portIndex,
            portName = portModel.Name,
            dataType = outputTypeName,
            value = portOutput,
            truncated = portTruncated,
            truncationReason = portTruncationReason
        });
    }

    string nodeTooltip = inspectedNode.NodeInfos.Any()
        ? string.Join("; ", inspectedNode.NodeInfos.Select(i => i.Message))
        : inspectedNode.Description;

    inspectionResults.Add(new
    {
        nodeId = inspectedNode.GUID.ToString(),
        name = inspectedNode.Name,
        nickName = inspectedNode.Name,
        state = inspectedNode.State.ToString(),
        tooltip = !string.IsNullOrEmpty(nodeTooltip) ? nodeTooltip : null,
        outPorts = portOutputDescriptions
    });
}

log($"Extracted output values from {inspectionResults.Count} node(s) in workspace '{activeWorkspaceModel.Name}'.");

return new
{
    success = true,
    workspaceName = activeWorkspaceModel.Name,
    nodeCount = inspectionResults.Count,
    nodes = inspectionResults,
    summary = $"Extracted output values from {inspectionResults.Count} node(s) in {activeWorkspaceModel.Name}"
};
