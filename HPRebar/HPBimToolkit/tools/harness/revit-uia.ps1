<#
.SYNOPSIS
    UI Automation for the harness's own Revit (scoped to -RevitPid), walking the tree by hand and never entering a
    WebView2/Chromium subtree (a FindAll over an open toolkit pane enumerates Chromium's accessibility tree and hangs).
      list   -Depth n [-Filter text]   prints "depth | controlType | name | automationId | class" lines
      invoke -Name text [-Nth k] [-After text]  invokes (or clicks) the k-th element named text (after -After)
.NOTES
    Windows PowerShell 5.1 (self-relaunch from pwsh).
#>
param(
    [Parameter(Mandatory)] [int] $RevitPid,
    [Parameter(Mandatory)] [ValidateSet('list', 'invoke')] [string] $Action,
    [int] $Depth = 12,
    [string] $Filter = '',
    [string] $Name = '',
    [int] $Nth = 0,
    # invoke: only count matches that come after (in walk order) the first element named this
    [string] $After = ''
)
if ($PSVersionTable.PSEdition -ne 'Desktop') {
    $ps = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    & $ps -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath -RevitPid $RevitPid -Action $Action -Depth $Depth -Filter $Filter -Name $Name -Nth $Nth -After $After
    exit $LASTEXITCODE
}
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient; Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms; Add-Type -AssemblyName System.Drawing
$sig = 'using System; using System.Runtime.InteropServices; public static class UiaClick { [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, UIntPtr e); }'
if (-not ('UiaClick' -as [type])) { Add-Type -TypeDefinition $sig }

$A = [System.Windows.Automation.AutomationElement]
$pidCond = New-Object System.Windows.Automation.PropertyCondition($A::ProcessIdProperty, $RevitPid)
$main = $A::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Children, $pidCond)
if ($null -eq $main) { throw "Revit $RevitPid main window not found" }
$walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker

function Walk([System.Windows.Automation.AutomationElement] $element, [int] $level, [scriptblock] $visit) {
    if ($level -gt $Depth) { return }
    $class = $element.Current.ClassName
    if ($class -like 'Chrome_*' -or $class -like '*WebView2*') { return }
    & $visit $element $level
    $child = $walker.GetFirstChild($element)
    while ($null -ne $child) {
        Walk $child ($level + 1) $visit
        $child = $walker.GetNextSibling($child)
    }
}

switch ($Action) {
    'list' {
        Walk $main 0 {
            param($e, $level)
            $line = '{0} | {1} | {2} | {3} | {4}' -f $level, $e.Current.ControlType.ProgrammaticName.Replace('ControlType.', ''), $e.Current.Name, $e.Current.AutomationId, $e.Current.ClassName
            if (-not $Filter -or $line -like "*$Filter*") { $line }
        }
    }
    'invoke' {
        $script:matches = New-Object System.Collections.Generic.List[System.Windows.Automation.AutomationElement]
        $script:armed = -not $After
        Walk $main 0 {
            param($e, $level)
            if (-not $script:armed -and $e.Current.Name -eq $After) { $script:armed = $true; return }
            if ($script:armed -and $e.Current.Name -eq $Name) { $script:matches.Add($e) }
        }
        if ($script:matches.Count -le $Nth) { throw "'$Name' #$Nth not found ($($script:matches.Count) matches)" }
        $target = $script:matches[$Nth]
        $pattern = $null
        if ($target.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$pattern)) { $pattern.Invoke(); "INVOKED $Name"; break }
        $rect = $target.Current.BoundingRectangle
        [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point([int]($rect.X + $rect.Width / 2), [int]($rect.Y + $rect.Height / 2))
        [UiaClick]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80; [UiaClick]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
        "CLICKED $Name"
    }
}
