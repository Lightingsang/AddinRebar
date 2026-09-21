$ErrorActionPreference = "Stop"
$solutionPath = "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.slnx"

for ($i = 1; $i -le 5; $i++) {
    Write-Host "=== EMPIRICAL STRESS RUN $i / 5 ===" -ForegroundColor Cyan
    & dotnet test $solutionPath --no-build
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Stress run $i failed with exit code $LASTEXITCODE"
        exit 1
    }
}
Write-Host "ALL 5 STRESS RUNS COMPLETED SUCCESSFULLY WITH 0 FAILURES." -ForegroundColor Green
