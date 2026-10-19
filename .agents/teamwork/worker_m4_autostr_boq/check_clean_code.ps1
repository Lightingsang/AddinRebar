$m4Files = @(
    "src\HPDcePro.Core\AutoStr\IAutoStrStep.cs",
    "src\HPDcePro.Core\AutoStr\AutoStrStepBase.cs",
    "src\HPDcePro.Core\AutoStr\AutoStrStepContext.cs",
    "src\HPDcePro.Core\AutoStr\AutoStrStepResult.cs",
    "src\HPDcePro.Core\AutoStr\AutoStrLogEntry.cs",
    "src\HPDcePro.Core\AutoStr\AutoStrCheckpoint.cs",
    "src\HPDcePro.Core\AutoStr\AutoStrProgress.cs",
    "src\HPDcePro.Core\AutoStr\AutoStrPipelineResult.cs",
    "src\HPDcePro.Core\AutoStr\AutoStrOrchestrator.cs",
    "src\HPDcePro.Core\Calculations\Takeoff\QuantityTakeoffModels.cs",
    "src\HPDcePro.Core\Calculations\Takeoff\QuantityTakeoffCalculator.cs",
    "src\HPDcePro.Excel\ClosedXmlBoqExporter.cs",
    "src\HPDcePro.AutoCad\Commands\AutoStrCommands.cs",
    "src\HPDcePro.AutoCad\Commands\RebarCommands.cs",
    "tests\HPDcePro.Core.Tests\AutoStrPipelineTests.cs",
    "tests\HPDcePro.Core.Tests\AutoStrResizingLoopTests.cs",
    "tests\HPDcePro.Core.Tests\QuantityTakeoffTests.cs",
    "tests\HPDcePro.Core.Tests\BoqExcelExportTests.cs"
)

$baseDir = "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro"
$stepFiles = Get-ChildItem -Path "$baseDir\src\HPDcePro.Core\AutoStr\Steps" -Filter *.cs

$allFiles = @()
foreach ($f in $m4Files) { $allFiles += "$baseDir\$f" }
foreach ($f in $stepFiles) { $allFiles += $f.FullName }

Write-Host "Total M4 files to audit: $($allFiles.Count)"

$violations = 0
foreach ($path in $allFiles) {
    if (-not (Test-Path $path)) {
        Write-Host "Missing file: $path"
        $violations++
        continue
    }
    $lines = Get-Content $path
    $lineCount = $lines.Count
    if ($lineCount -gt 300) {
        Write-Host "C2 VIOLATION (>300 lines): $path ($lineCount lines)"
        $violations++
    }
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $l = $lines[$i].Trim()
        if ($l -match "^//\s*(var|if|for|while|return|public|private|protected|class|using)\s+") {
            Write-Host "Q-B1 VIOLATION (commented code) at line $($i+1): $path - '$l'"
            $violations++
        }
        if ($l -match "catch\s*\([^\)]*\)\s*\{\s*\}") {
            Write-Host "Q-B2 VIOLATION (empty catch) at line $($i+1): $path"
            $violations++
        }
    }
}
Write-Host "Audit completed. Total violations: $violations"
