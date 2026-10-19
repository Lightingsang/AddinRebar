$source = "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRebar\HPRebar\bin\Debug.R26\HPRebar.dll"
$dest = Join-Path $env:APPDATA "Autodesk\Revit\Addins\2026\HPRebar\HPRebar.dll"

Write-Host "Dang kiem tra tien trinh Revit..."
$revit = Get-Process Revit -ErrorAction SilentlyContinue
if ($revit) {
    Write-Host "Revit dang chay (PID: $($revit.Id)). Cho Revit dong de cap nhat HPRebar.dll..."
    $revit | Wait-Process
}

Copy-Item $source $dest -Force
Write-Host "Da deploy thanh cong HPRebar.dll vao: $dest"
