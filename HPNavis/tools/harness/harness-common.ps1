# Shared by the Navisworks harness scripts. Windows PowerShell 5.1 (powershell.exe): it starts Roamer.exe directly
# with a model and HPNAVIS_MCP_BRIDGE_SHOW_WINDOW=1 (the bridge opens its status window once the GUI is up), then
# uses UI Automation to tick the per-session opt-ins on that window. Dot-source it:
# `. (Join-Path $PSScriptRoot 'harness-common.ps1')`.
# Every script that loads this closes the Roamer.exe it started at the end and never saves the model.
#
# Why not the Automation API: on the dev machine a Roamer started through Autodesk.Navisworks.Automation.dll
# (NavisworksApplication) loads the plugin, logs "ready", then exits within ~15 s, with or without this plugin
# installed; OpenFile then fails with 0x800706BA / 0x800706BE (RPC server unavailable / call failed). Direct launch
# is stable, so the harness uses it and ExecuteAddInPlugin stays unverified here.

if ($PSVersionTable.PSEdition -ne 'Desktop') { throw 'Run the Navisworks harness with Windows PowerShell 5.1 (powershell.exe).' }

$script:NavisDir = (Get-ItemProperty 'HKLM:\SOFTWARE\Autodesk\Navisworks API Runtime\23\Navisworks Manage' -ErrorAction SilentlyContinue).Path
if (-not $script:NavisDir) { $script:NavisDir = 'C:\Program Files\Autodesk\Navisworks Manage 2026\' }
$script:PipeName = 'hpnavis-mcp-2026'
$script:PluginDir = Join-Path $env:APPDATA 'Autodesk\Navisworks Manage 2026\Plugins\HPNavis.McpBridge'
$script:LogDir = Join-Path $env:LOCALAPPDATA 'HPNavis\McpBridge\logs'
$script:navisPid = 0

Add-Type -AssemblyName UIAutomationClient; Add-Type -AssemblyName UIAutomationTypes; Add-Type -AssemblyName System.Windows.Forms

function Assert-NoNavisworksRunning {
    if (Get-Process Roamer -ErrorAction SilentlyContinue) { throw 'Close every Navisworks before running the harness: it closes Roamer.exe at the end.' }
}

function Assert-PluginDeployed {
    if (-not (Test-Path (Join-Path $script:PluginDir 'HPNavis.McpBridge.dll'))) { throw "Plugin not deployed at $script:PluginDir - run: dotnet build HPNavis/HPNavis.slnx -c Debug (Navisworks closed)" }
}

# Starts Roamer.exe with the model on its command line and the show-window variable set for that process only.
# Returns the Process; waits until the main window title carries the model name (load finished) or the timeout.
function Start-NavisworksWithModel([string]$modelPath, [int]$timeoutSec = 180) {
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = Join-Path $script:NavisDir 'Roamer.exe'
    $psi.Arguments = if ($modelPath) { '"' + $modelPath + '"' } else { '' }
    $psi.UseShellExecute = $false
    $psi.EnvironmentVariables['HPNAVIS_MCP_BRIDGE_SHOW_WINDOW'] = '1'
    $proc = [System.Diagnostics.Process]::Start($psi)
    $script:navisPid = $proc.Id
    Write-Host "Roamer pid $script:navisPid started $(Get-Date -Format HH:mm:ss) with $modelPath"
    $expect = if ($modelPath) { [IO.Path]::GetFileNameWithoutExtension($modelPath) } else { 'Autodesk Navisworks' }
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $timeoutSec) {
        Start-Sleep -Seconds 2
        $proc.Refresh()
        if ($proc.HasExited) { throw "Roamer exited during startup with code $($proc.ExitCode)" }
        if ($proc.MainWindowTitle -like "*$expect*") { Write-Host "main window '$($proc.MainWindowTitle)' after $([int]$sw.Elapsed.TotalSeconds) s"; return $proc }
    }
    Write-Host "main window title still '$($proc.MainWindowTitle)' after $timeoutSec s"
    return $proc
}

function Find-BridgeWindow([int]$retries = 5) {
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $byName = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'HPNavis MCP Bridge')
    for ($i = 0; $i -lt $retries; $i++) {
        try { $w = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $byName); if ($w) { return $w } }
        catch { Write-Host "UIA lookup retry $($i + 1): $($_.Exception.Message)" }
        Start-Sleep -Seconds 2
    }
    return $null
}

function Invoke-BridgeButton([string]$automationId) {
    $win = Find-BridgeWindow
    if (-not $win) { Write-Host "button ${automationId}: bridge window not found"; return $false }
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, $automationId)
    $btn = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
    if (-not $btn) { Write-Host "button ${automationId}: not found"; return $false }
    $btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Write-Host "button ${automationId}: invoked"
    return $true
}

function Set-OptIn([string]$automationId, [bool]$on) {
    $win = Find-BridgeWindow
    if (-not $win) { Write-Host "opt-in ${automationId}: bridge window not found"; return $false }
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, $automationId)
    $box = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
    if (-not $box) { Write-Host "opt-in ${automationId}: checkbox not found"; return $false }
    $toggle = $box.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
    $isOn = $toggle.Current.ToggleState -eq [System.Windows.Automation.ToggleState]::On
    if ($isOn -ne $on) { $toggle.Toggle(); Start-Sleep -Milliseconds 700 }
    $isOn = $toggle.Current.ToggleState -eq [System.Windows.Automation.ToggleState]::On
    Write-Host "opt-in ${automationId} now $isOn"
    return ($isOn -eq $on)
}

function Read-BridgeText([string]$automationId) {
    $win = Find-BridgeWindow 1
    if (-not $win) { return $null }
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, $automationId)
    $el = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
    if ($el) { return $el.Current.Name } else { return $null }
}

function Wait-Pipe([int]$timeoutSec = 60) {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $timeoutSec) {
        if (Test-Path "\\.\pipe\$script:PipeName") { Write-Host "pipe up after $([int]$sw.Elapsed.TotalSeconds) s"; return $true }
        Start-Sleep -Seconds 1
    }
    Write-Host 'pipe did not appear'
    return $false
}

function Get-BridgeLogTail([int]$lines = 60) {
    $log = Get-ChildItem $script:LogDir -Filter 'mcpbridge-*.log' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime | Select-Object -Last 1
    if ($log) { Get-Content $log.FullName -Tail $lines } else { @('<no bridge log>') }
}

# Navisworks asks "Do you want to save changes?" when the harness edited the model; answer No.
function Answer-SavePromptNo {
    try {
        $root = [System.Windows.Automation.AutomationElement]::RootElement
        $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $script:navisPid)
        foreach ($w in $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)) {
            $btnCond = New-Object System.Windows.Automation.AndCondition(
                (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)),
                (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'No')))
            $no = $w.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $btnCond)
            if ($no) { $no.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke(); Write-Host "answered 'No' to '$($w.Current.Name)'" }
        }
    } catch { }
}

# Graceful first: WM_CLOSE makes Navisworks unload plugins (OnUnloading -> Dispose); a save prompt is answered No.
# Force-kill only when it does not exit in time. Touches only the process this harness started.
function Stop-Navisworks($proc) {
    if (-not $proc) { return }
    try {
        $proc.Refresh()
        if (-not $proc.HasExited) {
            $null = $proc.CloseMainWindow()
            $sw = [Diagnostics.Stopwatch]::StartNew()
            while (-not $proc.HasExited -and $sw.Elapsed.TotalSeconds -lt 25) {
                Start-Sleep -Milliseconds 800
                $proc.Refresh()
                Answer-SavePromptNo
            }
        }
    } catch { Write-Host "close failed: $($_.Exception.Message)" }
    $proc.Refresh()
    if (-not $proc.HasExited) { Write-Host 'Roamer did not exit after CloseMainWindow; killing'; Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue }
    else { Write-Host "Roamer exited with code $($proc.ExitCode)" }
}
