<#
.SYNOPSIS
    Toggles the "Allow AI code execution in this Revit session" checkbox of the open HPRebar MCP Bridge window through
    UI Automation (Toggle pattern) and prints its state. -State on|off. Windows PowerShell 5.1 (self-relaunch).
#>
param([ValidateSet('on', 'off')] [string] $State = 'on')
if ($PSVersionTable.PSEdition -ne 'Desktop') {
    & (Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe') -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath -State $State
    exit $LASTEXITCODE
}
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient; Add-Type -AssemblyName UIAutomationTypes
$revit = Get-Process Revit | Select-Object -First 1
# Owned WPF windows do not always appear among the UIA root's children; find the HWND by pid + title (Win32) instead.
$sig = @'
using System; using System.Text; using System.Runtime.InteropServices;
public static class RvFind {
  public delegate bool Proc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] static extern bool EnumWindows(Proc p, IntPtr l);
  [DllImport("user32.dll")] static extern int GetWindowText(IntPtr h, StringBuilder t, int n);
  [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
  public static IntPtr FindTopLevel(uint pid, string titleStart) {
    IntPtr found = IntPtr.Zero;
    EnumWindows((h, l) => { uint p; GetWindowThreadProcessId(h, out p); if (p != pid || !IsWindowVisible(h)) return true; var sb = new StringBuilder(256); GetWindowText(h, sb, 256); if (sb.ToString().StartsWith(titleStart)) { found = h; return false; } return true; }, IntPtr.Zero);
    return found; }
}
'@
if (-not ('RvFind' -as [type])) { Add-Type -TypeDefinition $sig }
$hwnd = [RvFind]::FindTopLevel([uint32]$revit.Id, 'HPRebar MCP Bridge')
if ($hwnd -eq [IntPtr]::Zero) { throw 'bridge window not found (open it first)' }
$window = [System.Windows.Automation.AutomationElement]::FromHandle($hwnd)
$cb = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'Allow AI code execution in this Revit session')))
if ($null -eq $cb) { throw 'opt-in checkbox not found by name' }
$toggle = $cb.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
$want = $(if ($State -eq 'on') { 'On' } else { 'Off' })
if ($toggle.Current.ToggleState.ToString() -ne $want) { $toggle.Toggle(); Start-Sleep -Milliseconds 400 }
"opt-in checkbox ($($cb.Current.ControlType.ProgrammaticName)): $($toggle.Current.ToggleState)"
