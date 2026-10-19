Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

$sig = @'
using System; using System.Text; using System.Runtime.InteropServices;
public static class WinFinder {
  public delegate bool Proc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumWindows(Proc p, IntPtr l);
  [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr h, StringBuilder t, int n);
  [DllImport("user32.dll")] public static extern int GetClassName(IntPtr h, StringBuilder t, int n);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd, out RECT r);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }

  public static void ListWindows(uint targetPid) {
    EnumWindows((h, l) => {
      uint pid;
      GetWindowThreadProcessId(h, out pid);
      if (pid == targetPid) {
        var sbT = new StringBuilder(256);
        GetWindowText(h, sbT, 256);
        var sbC = new StringBuilder(256);
        GetClassName(h, sbC, 256);
        RECT r;
        GetWindowRect(h, out r);
        bool vis = IsWindowVisible(h);
        Console.WriteLine("HWND: 0x{0:X} Title: '{1}' Class: '{2}' Vis: {3} Rect: ({4},{5})-({6},{7})",
          h.ToInt64(), sbT.ToString(), sbC.ToString(), vis, r.L, r.T, r.R, r.B);
      }
      return true;
    }, IntPtr.Zero);
  }
}
'@
Add-Type -TypeDefinition $sig

$revit = Get-Process Revit -ErrorAction SilentlyContinue | Select-Object -First 1
if ($revit) {
    Write-Host "Listing windows for Revit PID $($revit.Id)..."
    [WinFinder]::ListWindows([uint32]$revit.Id)
}
