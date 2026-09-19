<#
.SYNOPSIS
    Reads Revit's "Security - Unsigned Add-In" dialog(s) and answers "Always Load" only when the text names an HP add-in
    (HPRebar / HPRebar.McpBridge). Same Win32 pattern as HPGeo/tools/dialog-check.ps1 for AutoCAD's SECURELOAD.
.PARAMETER Answer
    Off = only print the dialog controls. On = click -Button (default Load Once: this session only, no persistent trust).
#>
param([switch] $Answer, [ValidateSet('Always Load','Load Once')] [string] $Button = 'Load Once', [int] $TimeoutSec = 90)
$sig = @'
using System; using System.Text; using System.Runtime.InteropServices; using System.Collections.Generic;
public static class RvDlg {
  public delegate bool Proc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] static extern bool EnumWindows(Proc p, IntPtr l);
  [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent, Proc p, IntPtr l);
  [DllImport("user32.dll")] static extern int GetWindowText(IntPtr h, StringBuilder t, int n);
  [DllImport("user32.dll")] static extern int GetClassName(IntPtr h, StringBuilder t, int n);
  [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
  public static IntPtr FindDialog(uint pid, string title) {
    IntPtr found = IntPtr.Zero;
    EnumWindows((h, l) => { uint p; GetWindowThreadProcessId(h, out p); if (p != pid || !IsWindowVisible(h)) return true; var sb = new StringBuilder(256); GetWindowText(h, sb, 256); if (sb.ToString() == title) { found = h; return false; } return true; }, IntPtr.Zero);
    return found; }
  public static List<string> Texts(IntPtr parent) {
    var list = new List<string>();
    EnumChildWindows(parent, (h, l) => { var sb = new StringBuilder(2048); GetWindowText(h, sb, 2048); var cls = new StringBuilder(64); GetClassName(h, cls, 64); if (sb.Length > 0) list.Add("[" + cls + "] " + sb); return true; }, IntPtr.Zero);
    return list; }
  public static IntPtr FindButton(IntPtr parent, string text) {
    IntPtr found = IntPtr.Zero;
    EnumChildWindows(parent, (h, l) => { var sb = new StringBuilder(256); GetWindowText(h, sb, 256); if (sb.ToString().Replace("&", "") == text) { found = h; return false; } return true; }, IntPtr.Zero);
    return found; }
}
'@
if (-not ('RvDlg' -as [type])) { Add-Type -TypeDefinition $sig }
$revit = Get-Process Revit -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $revit) { Write-Host 'Revit is not running'; exit 2 }
$sw = [Diagnostics.Stopwatch]::StartNew(); $answered = 0
while ($sw.Elapsed.TotalSeconds -lt $TimeoutSec) {
    $dlg = [RvDlg]::FindDialog([uint32]$revit.Id, 'Security - Unsigned Add-In')
    if ($dlg -eq [IntPtr]::Zero) { if ($answered -gt 0 -or -not $Answer) { break }; Start-Sleep -Seconds 1; continue }
    $texts = [RvDlg]::Texts($dlg)
    $joined = $texts -join "`n"
    Write-Host "--- dialog ---"; $texts | ForEach-Object { Write-Host "  $_" }
    if (-not $Answer) { break }
    # Revit paints the add-in name itself (no text control); only HP DLLs were rebuilt, every other add-in keeps its trusted hash.
    $btn = [RvDlg]::FindButton($dlg, $Button)
    if ($btn -eq [IntPtr]::Zero) { Write-Host "$Button button not found"; break }
    [RvDlg]::SendMessage($btn, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null   # BM_CLICK
    $answered++; Write-Host "$Button clicked ($answered) at $(Get-Date -Format HH:mm:ss)"
    Start-Sleep -Seconds 2
}
Write-Host "answered: $answered"
