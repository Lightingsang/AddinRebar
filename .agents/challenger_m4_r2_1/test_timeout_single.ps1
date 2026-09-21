$ErrorActionPreference = "Stop"
$project = "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.Mcp.Server.Tests\HPRobot.Mcp.Server.Tests.csproj"

for ($i = 1; $i -le 10; $i++) {
    Write-Host "--- Test Iteration $i / 10 ---" -ForegroundColor Cyan
    & dotnet test $project --filter-method "*Timeout_InformsModelThatChangesMayHavePersisted*" --no-build
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed on iteration $i with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }
}
Write-Host "`nALL 10 CONSECUTIVE RUNS OF Timeout_InformsModelThatChangesMayHavePersisted PASSED!" -ForegroundColor Green
