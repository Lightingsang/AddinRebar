<#
.SYNOPSIS
    Win32 helpers for the live harness, always scoped to ONE Revit process id (the harness's own copy):
      wait-dialog  -Title <exact>            prints FOUND / NOT_FOUND within -TimeoutSec
      close-dialog -Title <exact> [-Button]  clicks the button (default Cancel), else sends WM_CLOSE
      keys         -Keys <SendKeys text>     brings the Revit main window to the front and sends the keys
.NOTES
    Windows PowerShell 5.1 or pwsh. Never touches a window of another process.
#>
param(
    [Parameter(Mandatory)] [int] $RevitPid,
    [Parameter(Mandatory)] [ValidateSet('wait-dialog', 'close-dialog', 'keys')] [string] $Action,
    [string] $Title = '',
    [string] $Button = 'Cancel',
    [string] $Keys = '',
    [int] $TimeoutSec = 20
)
$ErrorActionPreference = 'Stop'
$sig = @'
using System; using System.Text; using System.Runtime.InteropServices;
public static class HkWin {
  public delegate bool Proc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] static extern bool EnumWindows(Proc p, IntPtr l);
  [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent, Proc p, IntPtr l);
  [DllImport("user32.dll")] static extern int GetWindowText(IntPtr h, StringBuilder t, int n);
  [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
  [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  public static uint ProcessOf(IntPtr h) { uint p; GetWindowThreadProcessId(h, out p); return p; }
  public static IntPtr Find(uint pid, string title, bool contains) {
    IntPtr found = IntPtr.Zero;
    EnumWindows((h, l) => { uint p; GetWindowThreadProcessId(h, out p); if (p != pid || !IsWindowVisible(h)) return true;
      var sb = new StringBuilder(512); GetWindowText(h, sb, 512); var t = sb.ToString();
      if (contains ? t.Contains(title) : t == title) { found = h; return false; } return true; }, IntPtr.Zero);
    return found; }
  public static IntPtr FindButton(IntPtr parent, string text) {
    IntPtr found = IntPtr.Zero;
    EnumChildWindows(parent, (h, l) => { var sb = new StringBuilder(256); GetWindowText(h, sb, 256);
      if (sb.ToString().Replace("&", "") == text) { found = h; return false; } return true; }, IntPtr.Zero);
    return found; }
}
'@
if (-not ('HkWin' -as [type])) { Add-Type -TypeDefinition $sig }
$revitPid = [uint32]$RevitPid

function Wait-Dialog {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $TimeoutSec) {
        $h = [HkWin]::Find($revitPid, $Title, $false)
        if ($h -ne [IntPtr]::Zero) { return $h }
        Start-Sleep -Milliseconds 300
    }
    return [IntPtr]::Zero
}

switch ($Action) {
    'wait-dialog' {
        if ((Wait-Dialog) -ne [IntPtr]::Zero) { 'FOUND' } else { 'NOT_FOUND' }
    }
    'close-dialog' {
        $h = Wait-Dialog
        if ($h -eq [IntPtr]::Zero) { 'NOT_FOUND'; break }
        $btn = [HkWin]::FindButton($h, $Button)
        if ($btn -ne [IntPtr]::Zero) { [HkWin]::SendMessage($btn, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null; "CLICKED $Button" }
        else { [HkWin]::PostMessage($h, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null; 'WM_CLOSE' }
    }
    'keys' {
        $main = [HkWin]::Find($revitPid, 'Autodesk Revit', $true)
        if ($main -eq [IntPtr]::Zero) { throw "Revit $RevitPid main window not found" }
        Add-Type -AssemblyName System.Windows.Forms
        [HkWin]::SetForegroundWindow($main) | Out-Null
        Start-Sleep -Milliseconds 400
        # Keys go to whatever window is in front: refuse unless it is this Revit, never type into the user's apps.
        $front = [HkWin]::GetForegroundWindow()
        if ([HkWin]::ProcessOf($front) -ne $revitPid) { throw "foreground window is not Revit $RevitPid; keys not sent" }
        [System.Windows.Forms.SendKeys]::SendWait($Keys)
        "SENT $Keys"
    }
}
