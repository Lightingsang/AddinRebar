$scratch = "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\dce_rvt_unpacked"
$exe = "C:\Dce Pro\AddinSetup_RVT\DBIM_RVT_26.exe"
$logFile = Join-Path $scratch "install.log"

$args = @(
    "/CURRENTUSER",
    "/DIR=$scratch",
    "/VERYSILENT",
    "/SUPPRESSMSGBOXES",
    "/NORESTART",
    "/SP-",
    "/LOG=$logFile"
)

Write-Output "Running with args: $($args -join ' ')"
$proc = Start-Process -FilePath $exe -ArgumentList $args -PassThru -Wait
Write-Output "ExitCode: $($proc.ExitCode)"

if (Test-Path $logFile) {
    Write-Output "--- Log Content ---"
    Get-Content $logFile
} else {
    Write-Output "No log file created."
}
