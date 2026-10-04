# Verifies the Navisworks harness start after a killed Roamer: the "Reload last file?" prompt must be answered No and
# the model must load; then the shared quality harness runs. Windows PowerShell 5.1.
$ErrorActionPreference = 'Stop'
$repo = 'F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar'
. (Join-Path $repo 'HPNavis\tools\harness\harness-common.ps1')
Assert-NoNavisworksRunning
$model = Join-Path $repo 'HPNavis\output\live-quality\gatehouse_pub.nwd'

# 1. provoke the recovery prompt: start Roamer on the model, then kill it
$first = Start-NavisworksWithModel $model
Start-Sleep -Seconds 5
Stop-Process -Id $first.Id -Force
Start-Sleep -Seconds 5
# the 12:35 prompt came after a Roamer killed while it showed Untitled (a failed load): reproduce that state too
$first = Start-NavisworksWithModel '' 60
Start-Sleep -Seconds 5
Stop-Process -Id $first.Id -Force
"killed Roamer $($first.Id) to provoke the reload prompt"
Start-Sleep -Seconds 5

# 2. start again through the fixed helper
$proc = Start-NavisworksWithModel $model
try {
    if ($proc.MainWindowTitle -notlike '*gatehouse_pub*') { throw "model did not load: '$($proc.MainWindowTitle)'" }
    if (-not (Wait-Pipe 60)) { throw 'pipe never appeared' }
    Start-Sleep -Seconds 3
    if (-not (Set-OptIn 'AllowExecution' $true)) { throw 'could not tick Allow AI code execution' }
    & python (Join-Path $repo 'McpShared\tools\live-verify-quality.py') --host navis
    "harness exit $LASTEXITCODE"
}
finally {
    try { $null = Set-OptIn 'AllowExecution' $false } catch { }
    Stop-Navisworks $proc
}
