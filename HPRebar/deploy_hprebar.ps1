# Script to deploy the updated HPRebar.dll when Revit is closed
$source = "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRebar\HPRebar\bin\Debug.R26\HPRebar.dll"
$dest = "C:\Users\STR-HP03\AppData\Roaming\Autodesk\Revit\Addins\2026\HPRebar\HPRebar.dll"

Write-Host "Waiting for Revit process to release file lock..."
$maxAttempts = 30
$attempt = 0

while ($attempt -lt $maxAttempts) {
    try {
        Copy-Item -Path $source -Destination $dest -Force -ErrorAction Stop
        Write-Host "SUCCESS: Deployed updated HPRebar.dll (including Rebar Partition and Join Elements) to Revit 2026 Addins!" -ForegroundColor Green
        break
    }
    catch {
        $attempt++
        Write-Host "Revit is currently running (attempt $attempt/$maxAttempts). Please close Revit to finish deployment..." -ForegroundColor Yellow
        Start-Sleep -Seconds 2
    }
}
