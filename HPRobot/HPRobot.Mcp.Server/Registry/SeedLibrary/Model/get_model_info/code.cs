var project = robot.Project;
bool includeCases = args.Bool("includeCases", true);
string filePath = project.FileName ?? "";
bool hasFile = !string.IsNullOrWhiteSpace(filePath) && filePath.Contains("\\");
string fileName = hasFile ? System.IO.Path.GetFileName(filePath) : "(unsaved model)";

int nodeCount = structure.Nodes.GetAll().Count;
int barCount = structure.Bars.GetAll().Count;
int panelCount = structure.Objects.GetAll().Count;
int caseCount = structure.Cases.GetAll().Count;
bool resultsAvailable = structure.Results.Available != 0;
string projectType = project.Type.ToString();

object casesList = null;
if (includeCases)
{
    var cCol = structure.Cases.GetAll();
    var list = new List<object>();
    for (int i = 1; i <= cCol.Count; i++)
    {
        if (cCol.Get(i) is IRobotCase c)
        {
            list.Add(new { number = c.Number, name = c.Name, type = c.Type.ToString(), nature = c.Nature.ToString() });
        }
    }
    casesList = list;
}

log($"Model: {fileName} | Type: {projectType} | Nodes: {nodeCount}, Bars: {barCount}, Panels: {panelCount}, Cases: {caseCount}, ResultsAvailable: {resultsAvailable}");

return new
{
    success = true,
    hasModelFile = hasFile,
    modelName = fileName,
    modelPath = hasFile ? filePath : null,
    projectType = projectType,
    counts = new
    {
        nodes = nodeCount,
        bars = barCount,
        panels = panelCount,
        cases = caseCount
    },
    resultsAvailable = resultsAvailable,
    units = units != null ? "m, kN, kN·m, MPa" : "Metric",
    cases = casesList,
    summary = $"{fileName} ({projectType}): {nodeCount} nodes, {barCount} bars, {panelCount} panels. Calculations: {(resultsAvailable ? "Available" : "Not Available")}."
};
