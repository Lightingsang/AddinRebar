string outputPath = args.Str("outputPath", "");
string format = args.Str("format", "PNG").ToUpperInvariant();

var activeWorkspaceModel = workspace
    ?? (dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model.CurrentWorkspace
    ?? (dynamo as DynamoModel)?.CurrentWorkspace;

if (activeWorkspaceModel == null)
    throw new InvalidOperationException("No active Dynamo workspace is currently open.");

var dynamoViewModel = dynamo as Dynamo.ViewModels.DynamoViewModel;
if (dynamoViewModel == null)
    throw new InvalidOperationException("Exporting canvas image requires an active Dynamo UI session (DynamoViewModel).");

string fileExtension = format == "JPEG" ? ".jpg" : ".png";
if (string.IsNullOrWhiteSpace(outputPath))
{
    string temporaryDirectoryPath = System.IO.Path.GetTempPath();
    outputPath = System.IO.Path.Combine(temporaryDirectoryPath, $"dynamo_canvas_{DateTime.UtcNow:yyyyMMdd_HHmmss}{fileExtension}");
}
else if (!outputPath.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase))
{
    outputPath += fileExtension;
}

dynamoViewModel.OnRequestSaveImage(dynamoViewModel, new Dynamo.ViewModels.ImageSaveEventArgs(outputPath));

log($"Exported Dynamo canvas image to '{outputPath}' (Workspace: '{activeWorkspaceModel.Name}', Nodes: {activeWorkspaceModel.Nodes.Count()}).");

return new
{
    success = true,
    filePath = outputPath,
    format,
    workspaceName = activeWorkspaceModel.Name,
    nodeCount = activeWorkspaceModel.Nodes.Count(),
    wireCount = activeWorkspaceModel.Connectors.Count(),
    summary = $"Exported canvas workspace '{activeWorkspaceModel.Name}' to {outputPath} ({format})"
};
