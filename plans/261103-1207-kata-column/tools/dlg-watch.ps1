param([int]$AcadPid,[string]$StopFile)
Add-Type @'
using System; using System.Text; using System.Collections.Generic; using System.Runtime.InteropServices;
public static class D5 {
  public delegate bool P(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumWindows(P p, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr parent, P p, IntPtr l);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr h, StringBuilder t, int n);
  [DllImport("user32.dll")] public static extern int GetClassName(IntPtr h, StringBuilder t, int n);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
  public static string T(IntPtr h){ var t=new StringBuilder(512); GetWindowText(h,t,512); return t.ToString(); }
  public static string C(IntPtr h){ var t=new StringBuilder(256); GetClassName(h,t,256); return t.ToString(); }
  public static int Dismiss(uint pid){ int n=0; var tops=new List<IntPtr>(); EnumWindows((h,l)=>{ uint p; GetWindowThreadProcessId(h,out p); if(p==pid && IsWindowVisible(h) && C(h)=="#32770" && T(h)=="AutoCAD Error") tops.Add(h); return true;}, IntPtr.Zero);
    foreach(var d in tops){ bool match=false; IntPtr ok=IntPtr.Zero; EnumChildWindows(d,(h,l)=>{ var t=T(h); if(t=="There was an error opening the file None") match=true; if(t=="OK") ok=h; return true;}, IntPtr.Zero);
      if(match && ok!=IntPtr.Zero){ SendMessage(ok,0x00F5,IntPtr.Zero,IntPtr.Zero); n++; } } return n; }
  public static string AnyOk(uint pid){ var tops=new List<IntPtr>(); EnumWindows((h,l)=>{ uint p; GetWindowThreadProcessId(h,out p); if(p==pid && IsWindowVisible(h) && C(h)=="#32770" && T(h)!="Security - Unsigned Executable File") tops.Add(h); return true;}, IntPtr.Zero);
    var log=new StringBuilder(); foreach(var d in tops){ IntPtr ok=IntPtr.Zero; var txt=new StringBuilder(T(d)+": "); EnumChildWindows(d,(h,l)=>{ var t=T(h); if(t=="OK") ok=h; else if(t.Length>0) txt.Append(t+" | "); return true;}, IntPtr.Zero);
      if(ok!=IntPtr.Zero){ SendMessage(ok,0x00F5,IntPtr.Zero,IntPtr.Zero); log.AppendLine(txt.ToString()); } } return log.ToString(); }
  public static int Continue(uint pid){ int n=0; var tops=new List<IntPtr>(); EnumWindows((h,l)=>{ uint p; GetWindowThreadProcessId(h,out p); if(p==pid && IsWindowVisible(h)) tops.Add(h); return true;}, IntPtr.Zero);
    foreach(var d in tops){ bool ex=false; IntPtr c=IntPtr.Zero; EnumChildWindows(d,(h,l)=>{ var t=T(h); if(t.Contains("Unhandled exception")) ex=true; if(t=="Continue") c=h; return true;}, IntPtr.Zero);
      if(ex && c!=IntPtr.Zero){ SendMessage(c,0x00F5,IntPtr.Zero,IntPtr.Zero); n++; } } return n; }
}
'@
$total=0
$exc=0
while(-not (Test-Path $StopFile)){ $total+=[D5]::Dismiss([uint32]$AcadPid); $c=[D5]::Continue([uint32]$AcadPid); $m=[D5]::AnyOk([uint32]$AcadPid); if($m){ Add-Content (Join-Path (Split-Path $StopFile) 'dialogs.log') ((Get-Date -Format s)+' '+$m) }; if($c){ $exc+=$c; Add-Content (Join-Path (Split-Path $StopFile) 'exceptions.log') ((Get-Date -Format s)) }; Start-Sleep -Milliseconds 400 }
"dismissed $total none-dialogs, $exc kata exceptions"
