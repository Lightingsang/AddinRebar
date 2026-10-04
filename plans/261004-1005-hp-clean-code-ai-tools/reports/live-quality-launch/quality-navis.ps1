# Navisworks run of the script-quality live check: copy of a sample model under output/, the HPNavis harness helpers
# for start / opt-in / close, then the shared Python harness. Windows PowerShell 5.1.
$ErrorActionPreference = 'Stop'
$repo = 'F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar'
. (Join-Path $repo 'HPNavis\tools\harness\harness-common.ps1')
Assert-NoNavisworksRunning
$scratch = Join-Path $repo 'HPNavis\output\live-quality'
New-Item -ItemType Directory -Force $scratch | Out-Null
$model = Join-Path $scratch 'gatehouse_pub.nwd'
Copy-Item (Join-Path $script:NavisDir 'Samples\gatehouse\gatehouse_pub.nwd') $model -Force
# Start like Start-NavisworksWithModel, but answer the "Reload last file?" recovery prompt (left by a killed Roamer)
# with No instead of WM_CLOSE, which only makes it come back.
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = Join-Path $script:NavisDir 'Roamer.exe'
$psi.Arguments = '"' + $model + '"'
$psi.UseShellExecute = $false
$psi.EnvironmentVariables['HPNAVIS_MCP_BRIDGE_SHOW_WINDOW'] = '1'
$proc = [System.Diagnostics.Process]::Start($psi)
$script:navisPid = $proc.Id
$script:bridgeWindow = $null
"Roamer pid $($proc.Id) started $(Get-Date -Format HH:mm:ss)"
$sw = [Diagnostics.Stopwatch]::StartNew()
while ($sw.Elapsed.TotalSeconds -lt 180) {
    Start-Sleep -Seconds 2
    $proc.Refresh()
    if ($proc.MainWindowTitle -like '*gatehouse_pub*') { "main window '$($proc.MainWindowTitle)' after $([int]$sw.Elapsed.TotalSeconds) s"; break }
    Answer-SavePromptNo
}
try {
    if (-not (Wait-Pipe 60)) { throw 'pipe never appeared' }
    Start-Sleep -Seconds 3
    if (-not (Set-OptIn 'AllowExecution' $true)) { throw 'could not tick Allow AI code execution' }
    & python (Join-Path $repo 'McpShared\tools\live-verify-quality.py') --host navis --out (Join-Path $repo 'plans\261004-1005-hp-clean-code-ai-tools\reports\live-quality')
    "harness exit $LASTEXITCODE"
    "roamer $((Get-Item 'C:\Program Files\Autodesk\Navisworks Manage 2026\Roamer.exe').VersionInfo.ProductVersion)"
}
finally {
    $null = Set-OptIn 'AllowExecution' $false
    Stop-Navisworks $proc
}
