<#
.SYNOPSIS
    Drives Revit's Options dialog (a Win32 property sheet) of the harness's own Revit by window messages only — no keys,
    no mouse — so nothing can land in another window:
      page   -Page <name>                selects a page in the left list (owner-drawn: UIA does not see its items)
      dump                               prints "id | class | text" for every visible control of the dialog
      combo  -Id <ctrl id> -Item <text>  selects the item in that combo box and notifies the page (CBN_SELCHANGE)
      combo-text -Id <ctrl id>           prints the combo box's current item
      button -Button <text>              clicks the dialog's button (OK, Cancel)
    The dialog must already be open (File ▸ Options, see revit-uia.ps1).
.NOTES
    Windows PowerShell 5.1 or pwsh. Never touches a window of another process.
#>
param(
    [Parameter(Mandatory)] [int] $RevitPid,
    [Parameter(Mandatory)] [ValidateSet('page', 'dump', 'combo', 'combo-text', 'button')] [string] $Action,
    [string] $Page = '',
    [int] $Id = 0,
    [string] $Item = '',
    [string] $Button = 'OK'
)
$ErrorActionPreference = 'Stop'
$sig = @'
using System; using System.Text; using System.Runtime.InteropServices;
public static class RvOpt {
  public delegate bool Proc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] static extern bool EnumWindows(Proc p, IntPtr l);
  [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent, Proc p, IntPtr l);
  [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder t, int n);
  [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, StringBuilder t, int n);
  [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern int GetDlgCtrlID(IntPtr h);
  [DllImport("user32.dll")] public static extern IntPtr GetParent(IntPtr h);
  [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
  [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr SendMessage(IntPtr h, uint m, IntPtr w, StringBuilder l);
  [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr SendMessage(IntPtr h, uint m, IntPtr w, string l);
  public static IntPtr FindTop(uint pid, string title) {
    IntPtr found = IntPtr.Zero;
    EnumWindows((h, l) => { uint p; GetWindowThreadProcessId(h, out p); if (p != pid || !IsWindowVisible(h)) return true;
      var sb = new StringBuilder(256); GetWindowText(h, sb, 256); if (sb.ToString() == title) { found = h; return false; } return true; }, IntPtr.Zero);
    return found; }
  public static IntPtr FindChild(IntPtr parent, string className, int id, string text) {
    IntPtr found = IntPtr.Zero;
    EnumChildWindows(parent, (h, l) => { if (!IsWindowVisible(h)) return true;
      var c = new StringBuilder(64); GetClassName(h, c, 64); var t = new StringBuilder(256); GetWindowText(h, t, 256);
      if ((className == null || c.ToString() == className) && (id == 0 || GetDlgCtrlID(h) == id) && (text == null || t.ToString().Replace("&", "") == text)) { found = h; return false; }
      return true; }, IntPtr.Zero);
    return found; }
  public static string Dump(IntPtr parent) {
    var o = new StringBuilder();
    EnumChildWindows(parent, (h, l) => { if (!IsWindowVisible(h)) return true;
      var t = new StringBuilder(256); GetWindowText(h, t, 256); var c = new StringBuilder(64); GetClassName(h, c, 64);
      o.AppendLine(GetDlgCtrlID(h) + " | " + c + " | " + t.ToString().Replace("\r", " ").Replace("\n", " ")); return true; }, IntPtr.Zero);
    return o.ToString(); }
  public static string ItemText(IntPtr list, int index, uint lenMsg, uint textMsg) {
    var len = SendMessage(list, lenMsg, (IntPtr)index, IntPtr.Zero).ToInt32();
    if (len <= 0) return "";
    var sb = new StringBuilder(len + 1); SendMessage(list, textMsg, (IntPtr)index, sb); return sb.ToString(); }
}
'@
if (-not ('RvOpt' -as [type])) { Add-Type -TypeDefinition $sig }

$dialog = [RvOpt]::FindTop([uint32]$RevitPid, 'Options')
if ($dialog -eq [IntPtr]::Zero) { throw "Options dialog of Revit $RevitPid is not open" }
$WM_COMMAND = 0x0111

function Notify([IntPtr] $control, [int] $code) {
    # The owning page handles WM_COMMAND(code, id) exactly as if the user had changed the control.
    $id = [RvOpt]::GetDlgCtrlID($control)
    [RvOpt]::SendMessage([RvOpt]::GetParent($control), $WM_COMMAND, [IntPtr](($code -shl 16) -bor $id), $control) | Out-Null
}

switch ($Action) {
    'page' {
        $list = [RvOpt]::FindChild($dialog, 'ListBox', 103, $null)
        if ($list -eq [IntPtr]::Zero) { throw 'Options page list (ListBox 103) not found' }
        $count = [RvOpt]::SendMessage($list, 0x018B, [IntPtr]::Zero, [IntPtr]::Zero).ToInt32()
        $index = (0..($count - 1) | Where-Object { [RvOpt]::ItemText($list, $_, 0x018A, 0x0189) -eq $Page } | Select-Object -First 1)
        if ($null -eq $index) { throw "page '$Page' not in the Options list" }
        [RvOpt]::SendMessage($list, 0x0186, [IntPtr]$index, [IntPtr]::Zero) | Out-Null   # LB_SETCURSEL
        Notify $list 1                                                                    # LBN_SELCHANGE
        "PAGE $Page"
    }
    'dump' { [RvOpt]::Dump($dialog) }
    'combo' {
        $combo = [RvOpt]::FindChild($dialog, 'ComboBox', $Id, $null)
        if ($combo -eq [IntPtr]::Zero) { throw "combo box $Id not found" }
        $index = [RvOpt]::SendMessage($combo, 0x0158, [IntPtr](-1), $Item).ToInt32()       # CB_FINDSTRINGEXACT
        if ($index -lt 0) { throw "combo box $Id has no item '$Item'" }
        [RvOpt]::SendMessage($combo, 0x014E, [IntPtr]$index, [IntPtr]::Zero) | Out-Null    # CB_SETCURSEL
        Notify $combo 1                                                                     # CBN_SELCHANGE
        "COMBO $Id = $Item"
    }
    'combo-text' {
        $combo = [RvOpt]::FindChild($dialog, 'ComboBox', $Id, $null)
        if ($combo -eq [IntPtr]::Zero) { throw "combo box $Id not found" }
        $index = [RvOpt]::SendMessage($combo, 0x0147, [IntPtr]::Zero, [IntPtr]::Zero).ToInt32()   # CB_GETCURSEL
        [RvOpt]::ItemText($combo, $index, 0x0149, 0x0148)
    }
    'button' {
        $btn = [RvOpt]::FindChild($dialog, 'Button', 0, $Button)
        if ($btn -eq [IntPtr]::Zero) { throw "button '$Button' not found" }
        [RvOpt]::SendMessage($btn, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null       # BM_CLICK
        "CLICKED $Button"
    }
}
