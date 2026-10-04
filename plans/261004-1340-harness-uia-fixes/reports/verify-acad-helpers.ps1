# Verifies the fixed AutoCAD / Civil 3D harness start helpers as they are: Start-AcadWithBridge + Set-OptIn from the
# product's harness-common.ps1 (no overrides), the shared quality harness, then a COM quit. Windows PowerShell 5.1.
param([ValidateSet('autocad', 'civil3d')] [string]$Product = 'autocad')
$ErrorActionPreference = 'Stop'
$repo = 'F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar'
$folder = if ($Product -eq 'autocad') { 'HPAutoCad' } else { 'HPCivil3d' }
. (Join-Path $repo "$folder\tools\harness\harness-common.ps1")
if (Get-Process acad -ErrorAction SilentlyContinue) { throw 'an AutoCAD is already running - not ours' }
$p = Start-AcadWithBridge (Join-Path $repo "$folder\tools\harness\bridge.scr") 420
try {
    if (-not $script:pipeUp) { throw 'pipe never appeared' }
    if (-not (Set-OptIn $true)) { throw 'could not tick Allow AI code execution' }
    & python (Join-Path $repo 'McpShared\tools\live-verify-quality.py') --host $Product
    "harness exit $LASTEXITCODE"
}
finally {
    try { Set-OptIn $false | Out-Null } catch { }
    $ps = "`$ids = @(Get-Process acad -ErrorAction SilentlyContinue | % Id); if (`$ids.Count -ne 1 -or [string]`$ids[0] -ne '$($p.Id)') { throw 'refusing COM' }; `$a = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application'); foreach (`$d in @(`$a.Documents)) { `$d.Close(`$false) }; `$a.Quit(); 'quit sent'"
    & powershell.exe -NoProfile -Command "try { $ps } catch { 'COMFAIL ' + `$_.Exception.Message }"
    if (-not $p.WaitForExit(40000)) { 'acad did not exit; killing'; Stop-Process -Id $p.Id -Force }
    'acad closed'
}
