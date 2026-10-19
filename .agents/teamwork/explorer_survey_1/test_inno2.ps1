$scratch = "C:\Users\STR-HP03\AppData\Local\Temp\dce_rvt_unpacked"
if (-not (Test-Path $scratch)) {
    New-Item -ItemType Directory -Path $scratch -Force | Out-Null
}
$exe = "C:\Dce Pro\AddinSetup_RVT\DBIM_RVT_26.exe"
$logFile = "$scratch\install.log"

$argList = "/CURRENTUSER /DIR=`"$scratch`" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /LOG=`"$logFile`""

Write-Output "Running: $exe $argList"
$proc = Start-Process -FilePath $exe -ArgumentList $argList -PassThru -Wait
Write-Output "ExitCode: $($proc.ExitCode)"

if (Test-Path $logFile) {
    Write-Output "--- Log Content ---"
    Get-Content $logFile
} else {
    Write-Output "No log file created."
}
