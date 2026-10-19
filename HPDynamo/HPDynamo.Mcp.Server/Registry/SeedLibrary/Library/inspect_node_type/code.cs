string requestedTypeName = args.Require("nodeType");
if (string.IsNullOrWhiteSpace(requestedTypeName))
    throw new ArgumentException("Parameter nodeType cannot be empty.");

var dynamoModel = (dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model ?? (dynamo as DynamoModel);

if (dynamoModel?.SearchModel == null)
    throw new InvalidOperationException("Dynamo SearchModel is not available in the current session.");

var targetEntry = dynamoModel.SearchModel.Entries.FirstOrDefault(entryItem =>
    string.Equals(entryItem.CreationName, requestedTypeName, StringComparison.OrdinalIgnoreCase) ||
    string.Equals(entryItem.FullName, requestedTypeName, StringComparison.OrdinalIgnoreCase) ||
    string.Equals(entryItem.Name, requestedTypeName, StringComparison.OrdinalIgnoreCase));

if (targetEntry == null)
{
    var partialMatches = dynamoModel.SearchModel.Entries
        .Where(entryCandidate => (entryCandidate.Name ?? "").IndexOf(requestedTypeName, StringComparison.OrdinalIgnoreCase) >= 0)
        .Take(5)
        .Select(entryCandidate => entryCandidate.CreationName ?? entryCandidate.FullName)
        .ToList();

    string suggestionHint = partialMatches.Count > 0
        ? $" Did you mean: {string.Join(", ", partialMatches)}?"
        : " Use search_nodes_library to find available node creation names.";

    throw new ArgumentException($"Node type '{requestedTypeName}' was not found in the loaded Dynamo library.{suggestionHint}");
}

var inputPortDescriptions = new List<object>();
var outputPortDescriptions = new List<object>();

if (targetEntry.InputParameters != null)
{
    int portIndex = 0;
    foreach (var inParam in targetEntry.InputParameters)
    {
        inputPortDescriptions.Add(new
        {
            index = portIndex++,
            name = inParam.Item1,
            toolTip = inParam.Item2 ?? "",
            hasDefaultValue = false
        });
    }
}

if (targetEntry.OutputParameters != null)
{
    int portIndex = 0;
    foreach (var outParam in targetEntry.OutputParameters)
    {
        outputPortDescriptions.Add(new
        {
            index = portIndex++,
            name = outParam,
            toolTip = ""
        });
    }
}

string fullCategoryPath = string.Join(" > ", targetEntry.Categories ?? Enumerable.Empty<string>());

log($"Inspected node type '{targetEntry.Name}' ({inputPortDescriptions.Count} inputs, {outputPortDescriptions.Count} outputs)");

return new
{
    success = true,
    name = targetEntry.Name,
    fullName = targetEntry.FullName,
    creationName = targetEntry.CreationName,
    category = fullCategoryPath,
    description = targetEntry.Description ?? "",
    assembly = targetEntry.FullName ?? "DynamoCore",
    inputPorts = inputPortDescriptions,
    outputPorts = outputPortDescriptions,
    summary = $"Node '{targetEntry.Name}' in '{fullCategoryPath}' ({inputPortDescriptions.Count} inputs, {outputPortDescriptions.Count} outputs)."
};
