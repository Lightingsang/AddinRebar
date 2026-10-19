$scratch = "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\dce_rvt_unpacked"
if (-not (Test-Path $scratch)) {
    New-Item -ItemType Directory -Path $scratch -Force | Out-Null
}
Write-Output "Scratch dir: $scratch"
Write-Output "Exists: $(Test-Path $scratch)"

$exe = "C:\Dce Pro\AddinSetup_RVT\DBIM_RVT_26.exe"
$logFile = Join-Path $scratch "install.log"

Write-Output "Running installer to extract into scratch..."
$proc = Start-Process -FilePath $exe -ArgumentList "/DIR=`"$scratch`"", "/VERYSILENT", "/SUPPRESSMSGBOXES", "/NORESTART", "/SP-", "/LOG=`"$logFile`"" -PassThru -Wait

Write-Output "Process exit code: $($proc.ExitCode)"

Get-ChildItem -Path $scratch -Recurse | Select-Object FullName, Length, LastWriteTime
