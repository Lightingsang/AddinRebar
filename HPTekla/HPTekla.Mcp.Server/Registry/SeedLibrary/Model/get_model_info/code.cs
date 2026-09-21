bool includeProj = args.Bool("includeProjectInfo", true);
bool includePhase = args.Bool("includePhaseInfo", true);

var info = model.GetInfo();
string modelName = info.ModelName ?? "";
string modelPath = info.ModelPath ?? "";
bool isConnected = model.GetConnectionStatus();

object projData = null;
if (includeProj)
{
    var proj = model.GetProjectInfo();
    projData = new
    {
        projectName = proj.Name,
        projectNumber = proj.ProjectNumber,
        designer = proj.Designer,
        builder = proj.Builder
    };
}

int currentPhase = 0;
if (includePhase)
{
    currentPhase = info.CurrentPhase;
}

log($"Tekla Model: {modelName} | Connected: {isConnected} | Phase: {currentPhase}");

return new
{
    success = true,
    isConnected = isConnected,
    modelName = modelName,
    modelPath = modelPath,
    currentPhase = currentPhase,
    project = projData,
    summary = $"Model '{modelName}' connected, current phase {currentPhase}."
};
