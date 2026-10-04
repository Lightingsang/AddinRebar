# Shared by the Navisworks harness scripts. Windows PowerShell 5.1 (powershell.exe): it starts Roamer.exe directly
# with a model and HPNAVIS_MCP_BRIDGE_SHOW_WINDOW=1 (the bridge opens its status window once the GUI is up), then
# uses UI Automation to tick the per-session opt-ins on that window. Dot-source it:
# `. (Join-Path $PSScriptRoot 'harness-common.ps1')`.
# Every script that loads this closes the Roamer.exe it started at the end and never saves the model.
#
# Why not the Automation API: on the dev machine a Roamer started through Autodesk.Navisworks.Automation.dll
# (NavisworksApplication) loads the plugin, logs "ready", then exits within ~15 s, with or without this plugin
# installed; OpenFile then fails with 0x800706BA / 0x800706BE (RPC server unavailable / call failed). Direct launch
# is stable, so the harness uses it and ExecuteAddInPlugin stays unverified here.

if ($PSVersionTable.PSEdition -ne 'Desktop') { throw 'Run the Navisworks harness with Windows PowerShell 5.1 (powershell.exe).' }

$script:NavisDir = (Get-ItemProperty 'HKLM:\SOFTWARE\Autodesk\Navisworks API Runtime\23\Navisworks Manage' -ErrorAction SilentlyContinue).Path
if (-not $script:NavisDir) { $script:NavisDir = 'C:\Program Files\Autodesk\Navisworks Manage 2026\' }
$script:PipeName = 'hpnavis-mcp-2026'
$script:PluginDir = Join-Path $env:APPDATA 'Autodesk\Navisworks Manage 2026\Plugins\HPNavis.McpBridge'
$script:LogDir = Join-Path $env:LOCALAPPDATA 'HPNavis\McpBridge\logs'
$script:navisPid = 0

Add-Type -AssemblyName UIAutomationClient; Add-Type -AssemblyName UIAutomationTypes; Add-Type -AssemblyName System.Windows.Forms

# Win32 for the things UI Automation is unreliable at on a busy desktop: enumerating Roamer's top-level windows,
# reading the main window's enabled state (a modal disables its owner) and closing a dialog with WM_CLOSE.
Add-Type -Namespace HPNavisHarness -Name Win32 -MemberDefinition @'
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool IsWindowEnabled(IntPtr hWnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder name, int max);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int max);
    [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr hWnd, uint cmd);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    public const uint WM_CLOSE = 0x0010;
    public const uint GW_OWNER = 4;
    public static System.Collections.Generic.List<IntPtr> TopLevelWindowsOf(int pid)
    {
        var list = new System.Collections.Generic.List<IntPtr>();
        EnumWindows((h, l) => { uint p; GetWindowThreadProcessId(h, out p); if (p == pid && IsWindowVisible(h)) list.Add(h); return true; }, IntPtr.Zero);
        return list;
    }
    public static string ClassOf(IntPtr h) { var sb = new System.Text.StringBuilder(256); GetClassName(h, sb, 256); return sb.ToString(); }
    public static string TitleOf(IntPtr h) { var sb = new System.Text.StringBuilder(512); GetWindowText(h, sb, 512); return sb.ToString(); }
'@

function Assert-NoNavisworksRunning {
    if (Get-Process Roamer -ErrorAction SilentlyContinue) { throw 'Close every Navisworks before running the harness: it closes Roamer.exe at the end.' }
}

function Assert-PluginDeployed {
    if (-not (Test-Path (Join-Path $script:PluginDir 'HPNavis.McpBridge.dll'))) { throw "Plugin not deployed at $script:PluginDir - run: dotnet build HPNavis/HPNavis.slnx -c Debug (Navisworks closed)" }
}

# Starts Roamer.exe with the model on its command line and (by default) the show-window variable set for that process
# only; the Ribbon check passes -showWindow:$false so the window must come from the button.
# Returns the Process; waits until the main window title carries the model name (load finished) or the timeout.
function Start-NavisworksWithModel([string]$modelPath, [int]$timeoutSec = 180, [bool]$showWindow = $true) {
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = Join-Path $script:NavisDir 'Roamer.exe'
    $psi.Arguments = if ($modelPath) { '"' + $modelPath + '"' } else { '' }
    $psi.UseShellExecute = $false
    if ($showWindow) { $psi.EnvironmentVariables['HPNAVIS_MCP_BRIDGE_SHOW_WINDOW'] = '1' }
    $proc = [System.Diagnostics.Process]::Start($psi)
    $script:navisPid = $proc.Id
    $script:bridgeWindow = $null
    Write-Host "Roamer pid $script:navisPid started $(Get-Date -Format HH:mm:ss) with $modelPath"
    $expect = if ($modelPath) { [IO.Path]::GetFileNameWithoutExtension($modelPath) } else { 'Autodesk Navisworks' }
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $timeoutSec) {
        Start-Sleep -Seconds 2
        $proc.Refresh()
        if ($proc.HasExited) { throw "Roamer exited during startup with code $($proc.ExitCode)" }
        if ($proc.MainWindowTitle -like "*$expect*") { Write-Host "main window '$($proc.MainWindowTitle)' after $([int]$sw.Elapsed.TotalSeconds) s"; return $proc }
        # After a killed Roamer, "Reload last file?" comes back after every WM_CLOSE and the model never loads: answer No.
        Answer-ReloadPromptNo
        if ($sw.Elapsed.TotalSeconds -gt 30 -and ($sw.Elapsed.TotalSeconds % 10) -lt 2) {
            # a startup prompt (autosave recovery after a killed Roamer, licensing) blocks the load: report and dismiss it
            foreach ($d in Get-RoamerDialogs) { Write-Host "startup dialog: '$($d.Title)' ($($d.Class))" }
            $null = Close-RoamerDialogs
        }
    }
    Write-Host "main window title still '$($proc.MainWindowTitle)' after $timeoutSec s"
    return $proc
}

# Top-level windows of the Roamer we started only (TreeScope.Children + ProcessId): a Descendants search from the
# desktop root walks every application's tree and times out whenever any other app's UI thread is busy.
function Find-BridgeWindow([int]$retries = 5) {
    # cached per Roamer: every desktop-root query can time out when some unrelated app's UI thread is busy
    if ($script:bridgeWindow) {
        try { if ($script:bridgeWindow.Current.ProcessId -eq $script:navisPid) { return $script:bridgeWindow } } catch { }
        $script:bridgeWindow = $null
    }
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $byPid = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $script:navisPid)
    $byName = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'HPNavis MCP Bridge')
    for ($i = 0; $i -lt $retries; $i++) {
        try {
            # the owned WPF window may sit beside or below Roamer's main window in the UIA tree: check both, Roamer only
            foreach ($top in $root.FindAll([System.Windows.Automation.TreeScope]::Children, $byPid)) {
                if ($top.Current.Name -eq 'HPNavis MCP Bridge') { $script:bridgeWindow = $top; return $top }
                $w = $top.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $byName)
                if ($w) { $script:bridgeWindow = $w; return $w }
            }
        }
        catch { Write-Host "UIA lookup retry $($i + 1): $($_.Exception.Message)" }
        Start-Sleep -Seconds 2
    }
    return $null
}

function Invoke-BridgeButton([string]$automationId) {
    $win = Find-BridgeWindow
    if (-not $win) { Write-Host "button ${automationId}: bridge window not found"; return $false }
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, $automationId)
    $btn = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
    if (-not $btn) { Write-Host "button ${automationId}: not found"; return $false }
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

# Roamer's own main window handle (WinForms, title carries "Navisworks").
function Get-RoamerMainWindowHandle {
    foreach ($h in [HPNavisHarness.Win32]::TopLevelWindowsOf($script:navisPid)) {
        if ([HPNavisHarness.Win32]::ClassOf($h) -like 'WindowsForms10.Window*' -and [HPNavisHarness.Win32]::TitleOf($h) -like '*Navisworks*') { return $h }
    }
    return [IntPtr]::Zero
}

# Every other visible top-level window of the Roamer we started: a file dialog, a message box, a recovery prompt.
# The bridge's own status window is excluded.
function Get-RoamerDialogs {
    $main = Get-RoamerMainWindowHandle
    if ($main -eq [IntPtr]::Zero) { return @() }   # no main window known: nothing may be closed
    $list = @()
    foreach ($h in [HPNavisHarness.Win32]::TopLevelWindowsOf($script:navisPid)) {
        if ($h -eq $main) { continue }
        # only real dialogs (#32770: file dialogs, message boxes, recovery prompts) — never a floating dock pane
        $class = [HPNavisHarness.Win32]::ClassOf($h)
        if ($class -ne '#32770') { continue }
        $list += [pscustomobject]@{ Handle = $h; Title = [HPNavisHarness.Win32]::TitleOf($h); Class = $class }
    }
    return $list
}

# Keyboard focus to Roamer's main window itself (the bridge's owned WPF window otherwise keeps it and eats the keys).
function Set-RoamerMainWindowForeground {
    $main = Get-RoamerMainWindowHandle
    if ($main -eq [IntPtr]::Zero) { return $false }
    return [HPNavisHarness.Win32]::SetForegroundWindow($main)
}

# A modal dialog disables its owner: the same test the bridge's quiescence check makes.
function Test-RoamerMainWindowEnabled {
    $main = Get-RoamerMainWindowHandle
    if ($main -eq [IntPtr]::Zero) { return $null }
    return [HPNavisHarness.Win32]::IsWindowEnabled($main)
}

# Closes every dialog Roamer has up with WM_CLOSE (a file dialog cancels, a message box takes its cancel/close path).
function Close-RoamerDialogs {
    $closed = @()
    foreach ($d in Get-RoamerDialogs) {
        $null = [HPNavisHarness.Win32]::PostMessage($d.Handle, [HPNavisHarness.Win32]::WM_CLOSE, [IntPtr]::Zero, [IntPtr]::Zero)
        $closed += "$($d.Title) ($($d.Class))"
    }
    if ($closed.Count -gt 0) { Write-Host "closed dialog(s): $($closed -join ' | ')"; Start-Sleep -Milliseconds 700 }
    return $closed
}

# Navisworks offers "Reload last file?" after a Roamer was killed; only that prompt is answered No (by its title).
function Answer-ReloadPromptNo {
    try {
        $root = [System.Windows.Automation.AutomationElement]::RootElement
        $cond = New-Object System.Windows.Automation.AndCondition(
            (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $script:navisPid)),
            (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'Reload last file?')))
        $dialog = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
        if (-not $dialog) { return }
        $btnCond = New-Object System.Windows.Automation.AndCondition(
            (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)),
            (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'No')))
        $no = $dialog.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $btnCond)
        if ($no) { $no.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke(); Write-Host "answered 'No' to 'Reload last file?'" }
    } catch { }
}

# Navisworks asks "Do you want to save changes?" when the harness edited the model; answer No.
function Answer-SavePromptNo {
    try {
        $root = [System.Windows.Automation.AutomationElement]::RootElement
        $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $script:navisPid)
        foreach ($w in $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)) {
            $btnCond = New-Object System.Windows.Automation.AndCondition(
                (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)),
                (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'No')))
            $no = $w.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $btnCond)
            if ($no) { $no.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke(); Write-Host "answered 'No' to '$($w.Current.Name)'" }
        }
    } catch { }
}

# Graceful first: WM_CLOSE makes Navisworks unload plugins (OnUnloading -> Dispose); a save prompt is answered No.
# Force-kill only when it does not exit in time. Touches only the process this harness started.
function Stop-Navisworks($proc) {
    if (-not $proc) { return }
    try {
        $proc.Refresh()
        if (-not $proc.HasExited) {
            # a dialog left open (file dialog, message box) would swallow WM_CLOSE and force a kill — and a killed
            # Roamer greets the next start with a recovery prompt that blocks the whole next run
            $null = Close-RoamerDialogs
            Start-Sleep -Milliseconds 500
            $null = $proc.CloseMainWindow()
            $sw = [Diagnostics.Stopwatch]::StartNew()
            while (-not $proc.HasExited -and $sw.Elapsed.TotalSeconds -lt 25) {
                Start-Sleep -Milliseconds 800
                $proc.Refresh()
                Answer-SavePromptNo
            }
        }
    } catch { Write-Host "close failed: $($_.Exception.Message)" }
    $proc.Refresh()
    if (-not $proc.HasExited) { Write-Host 'Roamer did not exit after CloseMainWindow; killing'; Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue }
    else { Write-Host "Roamer exited with code $($proc.ExitCode)" }
}

# ---- Ribbon (UI Automation scoped to the Roamer we started; never a desktop-wide walk) -----------------------------------

# The Roamer main window as a UIA element, or $null before the window exists.
function Get-RoamerMainAutomationElement {
    $h = Get-RoamerMainWindowHandle
    if ($h -eq [IntPtr]::Zero) { return $null }
    try { return [System.Windows.Automation.AutomationElement]::FromHandle($h) } catch { return $null }
}

# Ribbon tab headers of the Roamer main window matching the tab id (AdWindows exposes a header as a Button whose
# AutomationId is the tab id) or the visible title; Buttons and TabItems are both accepted.
function Find-RibbonTabHeaders([string]$tabId, [string]$title) {
    $main = Get-RoamerMainAutomationElement
    if (-not $main) { return @() }
    $byId = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, $tabId)
    $byName = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $title)
    $isButton = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
    $isTab = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::TabItem)
    $cond = New-Object System.Windows.Automation.AndCondition(
        (New-Object System.Windows.Automation.OrCondition($isButton, $isTab)),
        (New-Object System.Windows.Automation.OrCondition($byId, $byName)))
    $list = @()
    try { foreach ($t in $main.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)) { $list += $t } }
    catch { Write-Host "ribbon tab lookup failed: $($_.Exception.Message)" }
    return $list
}

# Selects the tab and proves it: after each pattern the header offers (Invoke first — the one that worked live —
# then SelectionItem, which reports success without switching) the button named by $verifyButton must come on screen.
# (The managed UIA client has no LegacyIAccessiblePattern; a mouse click would be the next fallback.)
# Roamer is brought to the foreground first: AdWindows ignores a selection while another application covers it.
# The header element goes stale while the Ribbon is still being built right after start-up, so the lookup repeats.
function Select-RibbonTab([string]$tabId, [string]$title, [string]$verifyButton, [int]$attempts = 4) {
    for ($attempt = 1; $attempt -le $attempts; $attempt++) {
        $null = Set-RoamerMainWindowForeground
        Start-Sleep -Milliseconds 300
        $tabs = @(Find-RibbonTabHeaders $tabId $title)
        if ($tabs.Count -eq 0) { Start-Sleep -Seconds 1; continue }
        $t = $tabs[0]
        # (a `continue` inside a PowerShell switch only leaves the switch, so the pattern is resolved before acting)
        $candidates = @(
            @{ Name = 'Invoke';        Id = [System.Windows.Automation.InvokePattern]::Pattern;        Act = { param($p) $p.Invoke() } },
            @{ Name = 'SelectionItem'; Id = [System.Windows.Automation.SelectionItemPattern]::Pattern; Act = { param($p) $p.Select() } })
        foreach ($c in $candidates) {
            $pattern = $null
            if (-not $t.TryGetCurrentPattern($c.Id, [ref]$pattern)) { continue }
            try {
                & $c.Act $pattern
                if (-not $verifyButton) { Start-Sleep -Milliseconds 1500; return $true }
                if (Wait-RibbonButtonVisible $verifyButton 4) { return $true }
                Write-Host "select tab attempt $attempt via $($c.Name) ran but the tab did not display"
            } catch { Write-Host "select tab attempt $attempt via $($c.Name) failed: $($_.Exception.Message.Split([char]10)[0])" }
        }
        Start-Sleep -Seconds 2
    }
    return $false
}

# Waits until a Ribbon button with that name is in the tree and on screen (its tab is the displayed one).
function Wait-RibbonButtonVisible([string]$namePattern, [int]$timeoutSec = 10) {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $timeoutSec) {
        $b = Find-RibbonButton $namePattern
        if ($b) { try { if (-not $b.Current.IsOffscreen) { return $b } } catch { } }
        Start-Sleep -Milliseconds 500
    }
    return $null
}

# First Button under the Roamer main window whose name (line breaks collapsed) matches the pattern; $null if none.
function Find-RibbonButton([string]$namePattern) {
    $main = Get-RoamerMainAutomationElement
    if (-not $main) { return $null }
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
    try {
        foreach ($b in $main.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)) {
            $name = ($b.Current.Name -replace "`r?`n", ' ')
            if ($name -match $namePattern) { return $b }
        }
    } catch { Write-Host "ribbon button lookup failed: $($_.Exception.Message)" }
    return $null
}

function Invoke-RibbonButton([string]$namePattern) {
    $button = Find-RibbonButton $namePattern
    if (-not $button) { Write-Host "ribbon button /$namePattern/ not found"; return $false }
    try { $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke(); return $true }
    catch { Write-Host "invoke /$namePattern/ failed: $($_.Exception.Message)"; return $false }
}

# Number of visible top-level windows of our Roamer titled like the bridge window (proves Activate, not a twin).
function Count-BridgeWindows {
    $n = 0
    foreach ($h in [HPNavisHarness.Win32]::TopLevelWindowsOf($script:navisPid)) {
        if ([HPNavisHarness.Win32]::TitleOf($h) -eq 'HPNavis MCP Bridge') { $n++ }
    }
    return $n
}

# A picture of the top of the Roamer main window (title, Ribbon tabs, the selected tab's panels) so the icon and
# layout can be checked from a log folder without a person at the screen.
function Save-RibbonScreenshot([string]$path, [int]$height = 260) {
    try {
        Add-Type -AssemblyName System.Drawing
        $main = Get-RoamerMainAutomationElement
        if (-not $main) { return $false }
        $r = $main.Current.BoundingRectangle
        $h = [Math]::Min($height, [int]$r.Height)
        $bmp = New-Object System.Drawing.Bitmap([int]$r.Width, $h)
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.CopyFromScreen([int]$r.X, [int]$r.Y, 0, 0, $bmp.Size)
        $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
        $g.Dispose(); $bmp.Dispose()
        Write-Host "ribbon screenshot: $path"
        return $true
    } catch { Write-Host "screenshot failed: $($_.Exception.Message)"; return $false }
}
