# Shared by the ETABS harness scripts. Windows PowerShell 5.1 (powershell.exe): it starts HPEtabs.McpBridge.exe (our own
# desktop app — ETABS itself is never started, stopped or given keystrokes by the harness) and uses UI Automation on that
# window only: tick the two opt-ins, start the listener, click Attach/Detach, read the attach state. Dot-source it:
# `. (Join-Path $PSScriptRoot 'harness-common.ps1')`. Every script that loads this closes the bridge it started at the end.

if ($PSVersionTable.PSEdition -ne 'Desktop') { throw 'Run the ETABS harness with Windows PowerShell 5.1 (powershell.exe).' }

$script:PipeName = 'hpetabs-mcp-22'
$script:WindowTitle = 'HPEtabs MCP Bridge'
$script:LogDir = Join-Path $env:LOCALAPPDATA 'HPEtabs\McpBridge\logs'
$script:bridgePid = 0
$script:bridgeWindow = $null

Add-Type -AssemblyName UIAutomationClient; Add-Type -AssemblyName UIAutomationTypes

function Assert-NoBridgeRunning {
    if (Get-Process HPEtabs.McpBridge -ErrorAction SilentlyContinue) { throw 'Close every HPEtabs.McpBridge.exe before running the harness: it starts and closes its own.' }
}

function Assert-EtabsRunning {
    if (-not (Get-Process ETABS -ErrorAction SilentlyContinue)) { throw 'Start ETABS 22 with a throw-away model first: the harness attaches to it and never starts it.' }
}

# Starts the bridge exe and waits for its window. Returns the Process.
function Start-Bridge([string]$exe, [int]$timeoutSec = 40) {
    $proc = Start-Process -FilePath $exe -PassThru
    $script:bridgePid = $proc.Id
    $script:bridgeWindow = $null
    Write-Host "bridge pid $script:bridgePid started $(Get-Date -Format HH:mm:ss)"
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $timeoutSec) {
        Start-Sleep -Seconds 1
        $proc.Refresh()
        if ($proc.HasExited) { throw "bridge exited during startup with code $($proc.ExitCode)" }
        if (Find-BridgeWindow 1) { Write-Host "bridge window after $([int]$sw.Elapsed.TotalSeconds) s"; return $proc }
    }
    throw "bridge window did not appear within $timeoutSec s"
}

# Top-level windows of the bridge we started only (TreeScope.Children + ProcessId).
function Find-BridgeWindow([int]$retries = 5) {
    if ($script:bridgeWindow) {
        try { if ($script:bridgeWindow.Current.ProcessId -eq $script:bridgePid) { return $script:bridgeWindow } } catch { }
        $script:bridgeWindow = $null
    }
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $byPid = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $script:bridgePid)
    for ($i = 0; $i -lt $retries; $i++) {
        try {
            foreach ($top in $root.FindAll([System.Windows.Automation.TreeScope]::Children, $byPid)) {
                if ($top.Current.Name -eq $script:WindowTitle) { $script:bridgeWindow = $top; return $top }
            }
        }
        catch { Write-Host "UIA lookup retry $($i + 1): $($_.Exception.Message)" }
        Start-Sleep -Seconds 1
    }
    return $null
}

function Invoke-BridgeButton([string]$automationId) {
    $win = Find-BridgeWindow
    if (-not $win) { Write-Host "button ${automationId}: bridge window not found"; return $false }
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, $automationId)
    $btn = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
    if (-not $btn) { Write-Host "button ${automationId}: not found"; return $false }
    if (-not $btn.Current.IsEnabled) { Write-Host "button ${automationId}: disabled"; return $false }
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

function Wait-BridgeText([string]$automationId, [string]$pattern, [int]$timeoutSec = 30) {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $timeoutSec) {
        $t = Read-BridgeText $automationId
        if ($t -and $t -match $pattern) { return $t }
        Start-Sleep -Milliseconds 700
    }
    return $null
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

# Closes only the bridge this harness started: the Close button first (App.OnExit stops the worker), then a kill.
function Stop-Bridge($proc) {
    if (-not $proc) { return }
    try {
        $proc.Refresh()
        if (-not $proc.HasExited) {
            $null = $proc.CloseMainWindow()
            $sw = [Diagnostics.Stopwatch]::StartNew()
            while (-not $proc.HasExited -and $sw.Elapsed.TotalSeconds -lt 15) { Start-Sleep -Milliseconds 500; $proc.Refresh() }
        }
    } catch { Write-Host "close failed: $($_.Exception.Message)" }
    $proc.Refresh()
    if (-not $proc.HasExited) { Write-Host 'bridge did not exit after CloseMainWindow; killing'; Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue }
    else { Write-Host "bridge exited with code $($proc.ExitCode)" }
}
