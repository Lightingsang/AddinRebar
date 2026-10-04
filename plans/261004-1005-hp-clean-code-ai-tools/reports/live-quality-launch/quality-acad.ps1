# AutoCAD / Civil 3D run of the script-quality live check: a new drawing from the default template, the deployed
# bundle, the product's harness helpers for start / opt-in, then the shared Python harness. Windows PowerShell 5.1.
param([ValidateSet('autocad', 'civil3d')] [string]$Product = 'autocad')
$ErrorActionPreference = 'Stop'
$repo = 'F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar'
$folder = if ($Product -eq 'autocad') { 'HPAutoCad' } else { 'HPCivil3d' }
. (Join-Path $repo "$folder\tools\harness\harness-common.ps1")
if (Get-Process acad -ErrorAction SilentlyContinue) { throw 'an AutoCAD is already running - not ours' }

# SECURELOAD: answer Load Once (this session only), never the permanent Always Load. The Civil 3D helpers already do
# exactly that, scoped to their own pid; the AutoCAD helpers answer Always Load, so they are overridden here.
if ($Product -eq 'autocad') { function Answer-SecureLoad {
    $dlg = [Native]::FindWindow('#32770', 'Security - Unsigned Executable File')
    if ($dlg -eq [IntPtr]::Zero) { return $false }
    $btn = [Native]::FindButton($dlg, 'Load Once')
    if ($btn -eq [IntPtr]::Zero) { $btn = [Native]::FindButton($dlg, '&Load Once') }
    if ($btn -ne [IntPtr]::Zero) { [Native]::SendMessage($btn, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null; "SECURELOAD answered 'Load Once' at $(Get-Date -Format HH:mm:ss)"; return $true }
    return $false
}
# The AutoCAD helper looks only under acad's first top-level window; the bridge window can be another one. Try them all.
function Set-OptIn([bool]$on) {
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $byPid = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, [int]$script:acadPid)
    $byId = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'AllowExecution')
    for ($i = 0; $i -lt 10; $i++) {
        foreach ($w in @($root.FindAll([System.Windows.Automation.TreeScope]::Children, $byPid))) {
            $box = $w.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $byId)
            if ($box) {
                $toggle = $box.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
                $want = if ($on) { [System.Windows.Automation.ToggleState]::On } else { [System.Windows.Automation.ToggleState]::Off }
                if ($toggle.Current.ToggleState -ne $want) { $toggle.Toggle(); Start-Sleep -Milliseconds 700 }
                Write-Host "opt-in under '$($w.Current.Name)': $($toggle.Current.ToggleState)"
                return ($toggle.Current.ToggleState -eq $want)
            }
        }
        Start-Sleep -Seconds 3
    }
    Write-Host "opt-in: checkbox not found in any window of pid $script:acadPid: $((@($root.FindAll([System.Windows.Automation.TreeScope]::Children, $byPid)) | ForEach-Object { $_.Current.Name }) -join ' | ')"
    return $false
} }

# COM in a child Windows PowerShell (GetActiveObject), refusing unless exactly our acad.exe is running.
function Invoke-AcadCom([string]$body) {
    $guard = "`$ids = @(Get-Process acad -ErrorAction SilentlyContinue | % Id); if (`$ids.Count -ne 1 -or [string]`$ids[0] -ne '$($script:acadPid)') { throw 'refusing COM: not exactly our acad' }; `$a = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application'); "
    for ($i = 1; $i -le 6; $i++) {
        $out = & powershell.exe -NoProfile -Command ("try { " + $guard + $body + " } catch { 'COMFAIL ' + `$_.Exception.Message }") 2>&1
        "COM[$i]: $out"
        if (-not ("$out" -match 'COMFAIL')) { return }
        Start-Sleep -Seconds 10
    }
}

$scr = Join-Path $repo "$folder\tools\harness\bridge.scr"
if ($true) {
    # Start like the desktop shortcut (no /b script, no /nologo) so AutoCAD builds its ribbon and the loader adds the
    # MCP panel (a /b start never does, and CadAddinManager cancels the script); wait for the loader's "ribbon panel
    # <id> added" line written after this start, then press MCP Bridge on that panel.
    if ($Product -eq 'autocad') {
        $argList = @('/product', 'ACAD', '/language', '"en-US"'); $tab = 'HPAUTOCAD_MCP_TAB'; $panelId = 'HPAUTOCAD_MCP_PANEL'
        $log = Join-Path $env:LOCALAPPDATA 'HPAutoCad\McpBridge\logs\loader.log'; $pipe = 'hpautocad-mcp-2026'
    } else {
        $argList = @('/ld', '"C:\Program Files\Autodesk\AutoCAD 2026\AecBase.dbx"', '/p', '"<<C3D_Metric>>"', '/product', 'C3D', '/language', '"en-US"')
        $tab = 'HPCIVIL3D_MCP_TAB'; $panelId = 'HPCIVIL3D_MCP_PANEL'
        $log = Join-Path $env:LOCALAPPDATA 'HPCivil3d\McpBridge\logs\loader.log'; $pipe = 'hpcivil3d-mcp-2026'
    }
    $since = Get-Date
    $p = Start-Process -FilePath 'C:\Program Files\Autodesk\AutoCAD 2026\acad.exe' -ArgumentList $argList -PassThru
    $script:acadPid = $p.Id
    "acad pid $($p.Id) ($Product) started $(Get-Date -Format HH:mm:ss) (shortcut command line)"
    $sw = [Diagnostics.Stopwatch]::StartNew(); $panel = $false
    while ($sw.Elapsed.TotalSeconds -lt 420 -and -not $panel) {
        Answer-SecureLoad | Out-Null
        Start-Sleep -Seconds 3
        if (Test-Path $log) {
            $panel = [bool](Get-Content $log -Tail 60 | Where-Object { $_ -match "ribbon panel $panelId added" -and $_.Length -gt 19 -and [datetime]::ParseExact($_.Substring(0, 19), 'yyyy-MM-dd HH:mm:ss', $null) -ge $since.AddSeconds(-2) })
        }
    }
    "ribbon panel line after $([int]$sw.Elapsed.TotalSeconds) s: $panel"
    $script:pipeUp = $true
    Start-Sleep -Seconds 5
    Select-RibbonTab $script:acadPid $tab | Out-Null
    Start-Sleep -Seconds 2
    Invoke-RibbonButton $script:acadPid 'MCP Bridge' | Out-Null
    Start-Sleep -Seconds 5
} else {
    $p = Start-AcadWithBridge $scr 600
    $pipe = 'hpcivil3d-mcp-2026'
}
try {
    if (-not $script:pipeUp) { throw "pipe $pipe never appeared" }
    Start-Sleep -Seconds 4
    # The /b script runs before the loader registers its commands, so the window may not be open: send the command
    # over COM, guarded to the one acad.exe this script started.
    $openCmd = if ($Product -eq 'autocad') { 'HPMCPBRIDGE' } else { 'HPC3DMCPBRIDGE' }
    if ($Product -eq 'civil3d') { Start-Sleep -Seconds 20 }   # CadAddinManager cancels /b scripts; let AutoCAD settle before typing
    # COM (GetActiveObject) answers MK_E_UNAVAILABLE here, so type the command into our own acad's command line.
    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -Namespace QualityAcad -Name Fg -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);'
    $p.Refresh()
    [QualityAcad.Fg]::SetForegroundWindow($p.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 800
    [System.Windows.Forms.SendKeys]::SendWait('{ESC}{ESC}')
    [System.Windows.Forms.SendKeys]::SendWait("$openCmd{ENTER}")
    "typed $openCmd into acad pid $($p.Id)"
    Start-Sleep -Seconds 6
    if (-not (Set-OptIn $true)) { throw 'could not tick Allow AI code execution' }
    & python (Join-Path $repo 'McpShared\tools\live-verify-quality.py') --host $Product --out (Join-Path $repo 'plans\261004-1005-hp-clean-code-ai-tools\reports\live-quality')
    "harness exit $LASTEXITCODE"
    "acad $((Get-Item 'C:\Program Files\Autodesk\AutoCAD 2026\acad.exe').VersionInfo.ProductVersion)"
}
finally {
    try { Set-OptIn $false | Out-Null } catch { }
    $p.Refresh()
    if (-not $p.HasExited) {
        Invoke-AcadCom "foreach (`$d in @(`$a.Documents)) { `$d.Close(`$false) }; `$a.Quit(); 'quit sent'"
        $null = $p.WaitForExit(30000)
    }
    $p.Refresh()
    if (-not $p.HasExited) {
        $null = $p.CloseMainWindow()
        $sw = [Diagnostics.Stopwatch]::StartNew()
        while (-not $p.HasExited -and $sw.Elapsed.TotalSeconds -lt 40) {
            Start-Sleep -Seconds 2
            foreach ($title in @('AutoCAD', 'Autodesk AutoCAD 2026', 'Civil 3D', 'Autodesk Civil 3D 2026')) {
                $dlg = [Native]::FindWindow('#32770', $title)
                if ($dlg -ne [IntPtr]::Zero) { $no = [Native]::FindButton($dlg, '&No'); if ($no -eq [IntPtr]::Zero) { $no = [Native]::FindButton($dlg, 'No') }; if ($no -ne [IntPtr]::Zero) { [Native]::SendMessage($no, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null; "answered No to '$title'" } }
            }
            $p.Refresh()
        }
        if (-not $p.HasExited) { "acad did not exit; killing"; Stop-Process -Id $p.Id -Force }
    }
    "acad closed"
}
