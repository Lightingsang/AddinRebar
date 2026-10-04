# UI Automation helpers for a standalone HP bridge app window, scoped to one pid. Dot-source from Windows PowerShell 5.1.
Add-Type -AssemblyName UIAutomationClient; Add-Type -AssemblyName UIAutomationTypes
$UIA = [System.Windows.Automation.AutomationElement]

function Get-AppWindow([int]$procId, [int]$timeoutSec = 40) {
    $cond = New-Object System.Windows.Automation.PropertyCondition($UIA::ProcessIdProperty, $procId)
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $timeoutSec) {
        $w = $UIA::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
        if ($w) { return $w }
        Start-Sleep -Milliseconds 700
    }
    throw "no window for pid $procId"
}

function Get-Controls($window, $controlType) {
    $cond = New-Object System.Windows.Automation.PropertyCondition($UIA::ControlTypeProperty, $controlType)
    return @($window.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond))
}

function Describe-Controls($window) {
    foreach ($kind in @([System.Windows.Automation.ControlType]::CheckBox, [System.Windows.Automation.ControlType]::Button)) {
        foreach ($c in Get-Controls $window $kind) { "  $($kind.ProgrammaticName.Replace('ControlType.', '')) '$($c.Current.Name)' id='$($c.Current.AutomationId)' enabled=$($c.Current.IsEnabled)" }
    }
}

function Invoke-ButtonNamed($window, [string]$pattern) {
    $b = Get-Controls $window ([System.Windows.Automation.ControlType]::Button) | Where-Object { $_.Current.Name -match $pattern -and $_.Current.IsEnabled } | Select-Object -First 1
    if (-not $b) { return $false }
    $b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    "button '$($b.Current.Name)' invoked"
    Start-Sleep -Milliseconds 800
    return $true
}

# The execution opt-in is the checkbox whose text names AI code execution; falls back to the first checkbox.
function Set-ExecutionOptIn($window, [bool]$on) {
    $boxes = Get-Controls $window ([System.Windows.Automation.ControlType]::CheckBox)
    $box = $boxes | Where-Object { $_.Current.Name -match 'AI code|execution' } | Select-Object -First 1
    if (-not $box) { $box = $boxes | Select-Object -First 1 }
    if (-not $box) { throw 'no checkbox in the bridge window' }
    $toggle = $box.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
    $want = if ($on) { [System.Windows.Automation.ToggleState]::On } else { [System.Windows.Automation.ToggleState]::Off }
    if ($toggle.Current.ToggleState -ne $want) { $toggle.Toggle(); Start-Sleep -Milliseconds 700 }
    "opt-in '$($box.Current.Name)' = $($toggle.Current.ToggleState)"
}

function Read-Texts($window, [string]$pattern) {
    Get-Controls $window ([System.Windows.Automation.ControlType]::Text) | ForEach-Object { $_.Current.Name } | Where-Object { $_ -match $pattern }
}
