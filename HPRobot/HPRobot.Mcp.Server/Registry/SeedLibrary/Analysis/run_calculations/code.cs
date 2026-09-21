var project = robot.Project;
bool autoGen = args.Bool("autoGenerateModel", true);

log("Starting structural calculation solver in Robot...");
project.CalcEngine.AutoGenerateModel = autoGen;
int ret = project.CalcEngine.Calculate();

if (ret != 0)
    throw new InvalidOperationException($"Robot calculation solver returned error code {ret}. Inspect model geometry and boundary conditions.");

bool available = structure.Results.Available != 0;
string status = structure.Results.Status.ToString();

log($"Calculations finished. Results available: {available}, Status: {status}");

return new
{
    success = true,
    returnCode = ret,
    resultsAvailable = available,
    status = status,
    summary = $"Calculation completed with status: {status}."
};
