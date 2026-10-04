# Golden-run UI helpers for Revit 2026, scoped to one Revit pid ($script:revitPid): unsigned add-in prompts, ribbon
# buttons, the MCP bridge opt-in, feature windows, their inputs and the result TaskDialog. Dot-source from Windows
# PowerShell 5.1: `. (Join-Path $PSScriptRoot 'golden-ui.ps1')`. Nothing here touches another process.

if ($PSVersionTable.PSEdition -ne 'Desktop') { throw 'Run the golden-run scripts with Windows PowerShell 5.1 (powershell.exe).' }
Add-Type -AssemblyName UIAutomationClient; Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms; Add-Type -AssemblyName System.Drawing
if (-not ('GoldenWin32' -as [type])) {
    Add-Type -TypeDefinition @'
using System; using System.Text; using System.Collections.Generic; using System.Runtime.InteropServices;
public static class GoldenWin32 {
  public delegate bool Proc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] static extern bool EnumWindows(Proc p, IntPtr l);
  [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent, Proc p, IntPtr l);
  [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder t, int n);
  [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, StringBuilder t, int n);
  [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
  [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, UIntPtr e);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  public static string Title(IntPtr h) { var sb = new StringBuilder(512); GetWindowText(h, sb, 512); return sb.ToString(); }
  public static string Class(IntPtr h) { var sb = new StringBuilder(256); GetClassName(h, sb, 256); return sb.ToString(); }
  public static List<IntPtr> TopLevel(uint pid) {
    var list = new List<IntPtr>();
    EnumWindows((h, l) => { uint p; GetWindowThreadProcessId(h, out p); if (p == pid && IsWindowVisible(h)) list.Add(h); return true; }, IntPtr.Zero);
    return list; }
  public static IntPtr ChildButton(IntPtr parent, string text) {
    IntPtr found = IntPtr.Zero;
    EnumChildWindows(parent, (h, l) => { if (Title(h).Replace("&", "") == text) { found = h; return false; } return true; }, IntPtr.Zero);
    return found; }
}
'@
}
$UIA = [System.Windows.Automation.AutomationElement]

function Get-RevitWindows { return @([GoldenWin32]::TopLevel([uint32]$script:revitPid) | ForEach-Object { [pscustomobject]@{ Handle = $_; Title = [GoldenWin32]::Title($_); Class = [GoldenWin32]::Class($_) } }) }

# "Security - Unsigned Add-In": Load Once (this session only), only in our Revit. Returns the number answered.
function Answer-UnsignedAddins([int]$timeoutSec = 120) {
    $answered = 0; $sw = [Diagnostics.Stopwatch]::StartNew(); $quiet = 0
    while ($sw.Elapsed.TotalSeconds -lt $timeoutSec) {
        $dialog = Get-RevitWindows | Where-Object { $_.Title -eq 'Security - Unsigned Add-In' } | Select-Object -First 1
        if ($dialog) {
            $button = [GoldenWin32]::ChildButton($dialog.Handle, 'Load Once')
            if ($button -ne [IntPtr]::Zero) { [GoldenWin32]::SendMessage($button, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null; $answered++; "unsigned add-in prompt answered Load Once ($answered)"; $quiet = 0 }
        }
        elseif ($answered -gt 0) { $quiet++; if ($quiet -ge 5) { break } }
        Start-Sleep -Seconds 1
    }
    return $answered
}

function Get-RevitMain {
    $cond = New-Object System.Windows.Automation.PropertyCondition($UIA::ProcessIdProperty, [int]$script:revitPid)
    return $UIA::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
}

# Ribbon items carry their caption as Name (tab header, button); the first invokable/selectable one wins, else a click.
function Invoke-RevitNamed([string]$name) {
    $main = Get-RevitMain
    if (-not $main) { throw 'Revit main window not found' }
    $cond = New-Object System.Windows.Automation.PropertyCondition($UIA::NameProperty, $name)
    foreach ($element in @($main.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond))) {
        $pattern = $null
        if ($element.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$pattern)) { $pattern.Invoke(); return "$name invoked" }
        if ($element.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$pattern)) { $pattern.Select(); return "$name selected" }
    }
    throw "'$name' not found on the Revit ribbon"
}

function Invoke-RibbonButton([string]$button, [int]$attempts = 4) {
    for ($i = 1; $i -le $attempts; $i++) {
        try { Invoke-RevitNamed 'HPRebar' | Out-Null; Start-Sleep -Milliseconds 900; return (Invoke-RevitNamed $button) }
        catch { "ribbon attempt ${i}: $($_.Exception.Message)"; Start-Sleep -Seconds 2 }
    }
    throw "ribbon button '$button' could not be pressed"
}

# A feature window (WPF, never a #32770 dialog). With -OrDialog a new #32770 dialog also ends the wait — a command
# that refuses its selection shows a TaskDialog titled "HPRebar - <Feature>" instead of its window.
function Wait-RevitWindow([string]$titlePrefix, [int]$timeoutSec = 30, [switch]$OrDialog, [string[]]$existingDialogs = @()) {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $timeoutSec) {
        $windows = Get-RevitWindows
        $w = $windows | Where-Object { $_.Class -ne '#32770' -and $_.Title -like "$titlePrefix*" } | Select-Object -First 1
        if ($w) { return $w }
        if ($OrDialog) {
            $d = $windows | Where-Object { $_.Class -eq '#32770' -and $existingDialogs -notcontains $_.Title } | Select-Object -First 1
            if ($d) { return $d }
        }
        Start-Sleep -Milliseconds 500
    }
    return $null
}

function Get-WindowElement([IntPtr]$handle) { return $UIA::FromHandle($handle) }

function Set-BridgeOptIn([bool]$on) {
    $w = Wait-RevitWindow 'HPRebar MCP Bridge' 20
    if (-not $w) { throw 'bridge window not found' }
    $cond = New-Object System.Windows.Automation.PropertyCondition($UIA::NameProperty, 'Allow AI code execution in this Revit session')
    $box = (Get-WindowElement $w.Handle).FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
    if (-not $box) { throw 'opt-in checkbox not found' }
    $toggle = $box.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
    $want = if ($on) { [System.Windows.Automation.ToggleState]::On } else { [System.Windows.Automation.ToggleState]::Off }
    if ($toggle.Current.ToggleState -ne $want) { $toggle.Toggle(); Start-Sleep -Milliseconds 500 }
    return "opt-in $($toggle.Current.ToggleState)"
}

function Invoke-WindowButton([IntPtr]$handle, [string]$name) {
    $cond = New-Object System.Windows.Automation.AndCondition(
        (New-Object System.Windows.Automation.PropertyCondition($UIA::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)),
        (New-Object System.Windows.Automation.PropertyCondition($UIA::NameProperty, $name)))
    $window = Get-WindowElement $handle
    $button = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
    if (-not $button) {
        $all = New-Object System.Windows.Automation.PropertyCondition($UIA::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
        $names = @($window.FindAll([System.Windows.Automation.TreeScope]::Descendants, $all) | ForEach-Object { "'$($_.Current.Name)'" }) -join ', '
        throw "button '$name' not found in '$($window.Current.Name)'; buttons: $names"
    }
    $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    return "button '$name' invoked"
}

# Every input of a window by tab: Edit values, ComboBox selections, CheckBox states, keyed by tab + control index
# (labels are not reliable automation names). Tabs are selected in turn and the first one is selected again at the end.
function Read-WindowInputs([IntPtr]$handle) {
    $window = Get-WindowElement $handle
    # Pages are TabItems (Foundation) or the items of the left navigation ListBox (Column, Beam).
    $tabCond = New-Object System.Windows.Automation.PropertyCondition($UIA::ControlTypeProperty, [System.Windows.Automation.ControlType]::TabItem)
    $tabs = @($window.FindAll([System.Windows.Automation.TreeScope]::Descendants, $tabCond))
    if ($tabs.Count -eq 0) {
        $itemCond = New-Object System.Windows.Automation.PropertyCondition($UIA::ControlTypeProperty, [System.Windows.Automation.ControlType]::ListItem)
        $tabs = @($window.FindAll([System.Windows.Automation.TreeScope]::Descendants, $itemCond) | Where-Object {
            $p = $null; $_.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$p) })
    }
    $result = [ordered]@{}
    $pages = New-Object System.Collections.ArrayList
    if ($tabs.Count -gt 0) { [void]$pages.AddRange($tabs) } else { [void]$pages.Add($null) }
    $pageIndex = 0
    foreach ($tab in $pages) {
        $tabName = if ($tab) { "page[$pageIndex] " + $tab.Current.Name } else { '(window)' }
        $pageIndex++
        if ($tab) { $tab.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select(); Start-Sleep -Milliseconds 700 }
        $values = New-Object System.Collections.Generic.List[string]
        foreach ($kind in @([System.Windows.Automation.ControlType]::Edit, [System.Windows.Automation.ControlType]::ComboBox, [System.Windows.Automation.ControlType]::CheckBox)) {
            $cond = New-Object System.Windows.Automation.PropertyCondition($UIA::ControlTypeProperty, $kind)
            $i = 0
            foreach ($c in @($window.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond))) {
                if ($c.Current.IsOffscreen) { continue }
                $value = $null; $pattern = $null
                if ($c.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$pattern)) { $value = $pattern.Current.Value }
                elseif ($c.TryGetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern, [ref]$pattern)) { $value = [string]$pattern.Current.ToggleState }
                # a read-only ComboBox answers an empty Value: its selected item carries the text
                if (-not $value -and $c.TryGetCurrentPattern([System.Windows.Automation.SelectionPattern]::Pattern, [ref]$pattern)) {
                    $value = (@($pattern.Current.GetSelection()) | ForEach-Object { $_.Current.Name }) -join ','
                }
                $values.Add("$($kind.ProgrammaticName.Replace('ControlType.', ''))[$i] $($c.Current.Name) = $value")
                $i++
            }
        }
        $result[$tabName] = $values
    }
    if ($tabs.Count -gt 0) { $tabs[0].GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select() }
    return $result
}

# Waits for a Win32 dialog (#32770) of our Revit, returns its UIA text and closes it with its first button.
function Close-ResultDialog([int]$timeoutSec = 300, [string[]]$exclude = @()) {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $timeoutSec) {
        $dialog = Get-RevitWindows | Where-Object { $_.Class -eq '#32770' -and $exclude -notcontains $_.Title } | Select-Object -First 1
        if ($dialog) {
            Start-Sleep -Milliseconds 800
            $element = Get-WindowElement $dialog.Handle
            $textCond = New-Object System.Windows.Automation.PropertyCondition($UIA::ControlTypeProperty, [System.Windows.Automation.ControlType]::Text)
            $texts = @($element.FindAll([System.Windows.Automation.TreeScope]::Descendants, $textCond) | ForEach-Object { $_.Current.Name } | Where-Object { $_ })
            if (-not (Invoke-DialogButton $dialog.Handle '^(Close|OK|Ok)$')) { [GoldenWin32]::PostMessage($dialog.Handle, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null }
            return [pscustomobject]@{ Title = $dialog.Title; Text = ($texts -join "`n"); Seconds = [int]$sw.Elapsed.TotalSeconds }
        }
        Start-Sleep -Milliseconds 700
    }
    return $null
}

# Task dialogs expose their buttons as Panes (command buttons) or Buttons: press the first whose name matches.
function Invoke-DialogButton([IntPtr]$handle, [string]$pattern) {
    $element = Get-WindowElement $handle
    foreach ($x in @($element.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition))) {
        if ($x.Current.Name -notmatch $pattern) { continue }
        if ($x.Current.ControlType -ne [System.Windows.Automation.ControlType]::Button -and $x.Current.ControlType -ne [System.Windows.Automation.ControlType]::Pane) { continue }
        $invoke = $null
        if ($x.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$invoke)) { $invoke.Invoke(); return $x.Current.Name }
        # TaskDialog command buttons (DirectUI panes) expose no Invoke: click the middle of the button on the dialog
        $rect = $x.Current.BoundingRectangle
        if ($rect.Width -gt 0 -and $rect.Height -gt 0) {
            [GoldenWin32]::SetForegroundWindow($handle) | Out-Null
            Start-Sleep -Milliseconds 300
            [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point([int]($rect.X + $rect.Width / 2), [int]($rect.Y + $rect.Height / 2))
            [GoldenWin32]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80; [GoldenWin32]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
            return $x.Current.Name + ' (clicked)'
        }
    }
    return $null
}

function Close-RevitWindow([IntPtr]$handle) { [GoldenWin32]::PostMessage($handle, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null }

function Save-WindowShot([IntPtr]$handle, [string]$path) {
    $r = New-Object GoldenWin32+RECT; [GoldenWin32]::GetWindowRect($handle, [ref]$r) | Out-Null
    $bmp = New-Object System.Drawing.Bitmap ([Math]::Max(1, $r.R - $r.L)), ([Math]::Max(1, $r.B - $r.T))
    $g = [System.Drawing.Graphics]::FromImage($bmp); $hdc = $g.GetHdc()
    [GoldenWin32]::PrintWindow($handle, $hdc, 2) | Out-Null
    $g.ReleaseHdc($hdc); $g.Dispose(); $bmp.Save($path); $bmp.Dispose()
}
