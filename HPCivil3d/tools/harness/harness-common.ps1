# Shared by the HPCivil3d harness scripts: the SECURELOAD auto-answer, the UI Automation opt-in toggle and the
# guarded start of acad.exe as Civil 3D (or, for the isolation checks, as AutoCAD / Advance Steel). Dot-source it: `. (Join-Path $PSScriptRoot 'harness-common.ps1')`.
# Every script that loads this closes drawings without saving and kills acad.exe at the end, so it only
# ever touches the AutoCAD it started itself.

function Assert-NoAutocadRunning {
    if (Get-Process acad -ErrorAction SilentlyContinue) { throw 'Close every AutoCAD / Civil 3D / Advance Steel (acad.exe) before running the harness: it discards drawings and kills acad.exe.' }
}

$sig = @'
using System; using System.Text; using System.Runtime.InteropServices;
public static class Native {
  public delegate bool Proc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern IntPtr FindWindow(string cls, string title);
  [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent, Proc p, IntPtr l);
  [DllImport("user32.dll")] static extern int GetWindowText(IntPtr h, StringBuilder t, int n);
  [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  public static IntPtr FindButton(IntPtr parent, string text) {
    IntPtr found = IntPtr.Zero;
    EnumChildWindows(parent, (h, l) => { var sb = new StringBuilder(256); GetWindowText(h, sb, 256); if (sb.ToString() == text) { found = h; return false; } return true; }, IntPtr.Zero);
    return found; }
}
'@
Add-Type -TypeDefinition $sig
Add-Type -AssemblyName UIAutomationClient; Add-Type -AssemblyName UIAutomationTypes

function Answer-SecureLoad {
    # Civil 3D / AutoCAD 2026 prompt once per unsigned DLL hash - including the bridge DLL the loader loads into its own
    # load context - so every rebuild prompts again. The harness answers "Load Once": the load proceeds for this session
    # and no permanent trust is granted by automation (the user grants "Always Load" in person). Only the acad.exe this
    # harness started is answered; the dialog does not expose the file name (UIA sees the buttons and the help link
    # only), so the pid is the whole scope - a prompt in any other process is left alone. Every answer is logged.
    $dlg = [Native]::FindWindow('#32770', 'Security - Unsigned Executable File')
    if ($dlg -eq [IntPtr]::Zero) { return $false }
    $owner = [uint32]0; [Native]::GetWindowThreadProcessId($dlg, [ref]$owner) | Out-Null
    if (-not $script:acadPid -or [int]$owner -ne [int]$script:acadPid) { Write-Host "SECURELOAD dialog belongs to pid $owner, not ours ($($script:acadPid)) - left alone"; return $false }
    $btn = [Native]::FindButton($dlg, 'Load Once')
    if ($btn -eq [IntPtr]::Zero) { Write-Host "SECURELOAD dialog (pid $owner) has no 'Load Once' button - left alone"; return $false }
    [Native]::SendMessage($btn, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
    Write-Host "SECURELOAD answered 'Load Once' for pid $owner at $(Get-Date -Format HH:mm:ss) (no permanent trust)"
    return $true
}

function Find-OptInCheckbox {
    # The modeless WPF window is owned by AutoCAD's frame, so UIA lists it under that frame, not under the root - but a
    # tooltip or dynamic-input popup of acad.exe can be listed first, so every top-level window of the process is tried.
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $byPid = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, [int]$script:acadPid)
    $byId = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'AllowExecution')
    # 8 x 3 s: the bridge window loads its MaterialDesign dictionaries on first open, a second or two after the pipe is up.
    for ($i = 0; $i -lt 8; $i++) {
        try {
            foreach ($w in @($root.FindAll([System.Windows.Automation.TreeScope]::Children, $byPid))) {
                $box = $w.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $byId)
                if ($box) { Write-Host "opt-in: checkbox under window '$($w.Current.Name)' class $($w.Current.ClassName)"; return $box }
            }
        }
        catch { Write-Host "UIA lookup retry $($i + 1): $($_.Exception.Message)" }
        Start-Sleep -Seconds 3
    }
    return $null
}

function Test-PipeUp([string]$PipeName) {
    # A directory listing, not Test-Path: Test-Path opens the pipe, which the bridge counts as a client and drops -
    # the name vanishes for a moment while it re-listens, and a second check races it.
    try { return [bool]([System.IO.Directory]::GetFiles('\\.\pipe\') | Where-Object { [System.IO.Path]::GetFileName($_) -eq $PipeName }) } catch { return $false }
}

function Set-OptIn([bool]$on) {
    $box = Find-OptInCheckbox
    if (-not $box) { Write-Host "opt-in: checkbox not found in any window of acad.exe"; return $false }
    $toggle = $box.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
    # A toggle right after another one can be swallowed while the window re-binds: poll, then try again.
    for ($attempt = 0; $attempt -lt 3; $attempt++) {
        $isOn = $toggle.Current.ToggleState -eq [System.Windows.Automation.ToggleState]::On
        if ($isOn -eq $on) { break }
        $toggle.Toggle()
        for ($wait = 0; $wait -lt 10; $wait++) {
            Start-Sleep -Milliseconds 300
            $isOn = $toggle.Current.ToggleState -eq [System.Windows.Automation.ToggleState]::On
            if ($isOn -eq $on) { break }
        }
    }
    Write-Host "opt-in now $isOn"
    return ($isOn -eq $on)
}


# Stops the acad.exe this harness started: first the polite way over COM (close every drawing without saving, then
# Quit — no Drawing Recovery entries, no stale .dwl locks), pid-guarded like every COM call; Stop-Process only when
# the process is still alive after the grace (a modal dialog or a running command refuses COM).
function Stop-Acad([System.Diagnostics.Process]$p, [int]$graceSec = 45) {
    if (-not $p) { return }
    $p.Refresh()
    if ($p.HasExited) { return }
    $ps = "`$ids = @(Get-Process acad -ErrorAction SilentlyContinue | % Id); if (`$ids.Count -ne 1 -or [string]`$ids[0] -ne '$($p.Id)') { throw 'refusing COM: not the harness acad.exe' }; " +
          "`$a = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application'); " +
          "for (`$i = 0; `$i -lt 20 -and `$a.Documents.Count -gt 0; `$i++) { `$a.Documents.Item(0).Close(`$false) }; `$a.Quit()"
    $out = powershell -NoProfile -Command $ps 2>&1
    if ($LASTEXITCODE -ne 0) { Write-Host "acad pid $($p.Id): graceful quit refused ($((($out -join ' ') -replace '\s+', ' ').Substring(0, [Math]::Min(120, ($out -join ' ').Length)))) - killing it"; Stop-Process -Id $p.Id -Force -Confirm:$false -ErrorAction SilentlyContinue; Start-Sleep -Seconds 3; return }
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $graceSec) { $p.Refresh(); if ($p.HasExited) { Write-Host "acad pid $($p.Id) quit gracefully after $([int]$sw.Elapsed.TotalSeconds) s"; return }; Start-Sleep -Seconds 1 }
    Write-Host "acad pid $($p.Id) did not quit within $graceSec s - killing it"
    Stop-Process -Id $p.Id -Force -Confirm:$false -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 3
}

# Starts acad.exe with the given script, answers SECURELOAD, waits for the bridge pipe. Returns the process.
function Start-AcadWithBridge([string]$scriptPath, [int]$timeoutSec = 420, [string]$Product = 'C3D', [string]$ProfileName = '<<C3D_Metric>>', [string]$PipeName = 'hpcivil3d-mcp-2026', [switch]$NoAecBase) {
    # The Autodesk shortcut for "Civil 3D 2026 Metric" is exactly this command line (plus /nologo and the script):
    #   acad.exe /ld "...\AecBase.dbx" /p "<<C3D_Metric>>" /product C3D /language en-US
    # Plain AutoCAD is /product ACAD, Advance Steel /product "ADVS" /p "<<ADVS>>". The isolation checks start those
    # products with the same helper and expect the Civil bundle to stay out.
    $acadDir = 'C:\Program Files\Autodesk\AutoCAD 2026'
    $argList = @('/nologo')
    if ($Product -eq 'C3D' -and -not $NoAecBase) { $argList += @('/ld', "`"$acadDir\AecBase.dbx`"") }
    if ($ProfileName) { $argList += @('/p', "`"$ProfileName`"") }
    $argList += @('/product', $Product, '/language', '"en-US"')
    if ($scriptPath) { $argList += @('/b', "`"$scriptPath`"") }
    $p = Start-Process -FilePath "$acadDir\acad.exe" -ArgumentList $argList -PassThru
    $script:acadPid = $p.Id
    $env:HP_HARNESS_ACAD_PID = $p.Id   # every COM call checks it talks to this process and no other
    Write-Host "acad pid $($p.Id) ($Product) started $(Get-Date -Format HH:mm:ss): $($argList -join ' ')"

    $sw = [Diagnostics.Stopwatch]::StartNew()
    $script:pipeUp = $false
    while ($sw.Elapsed.TotalSeconds -lt $timeoutSec) {
        Answer-SecureLoad | Out-Null
        if ($PipeName -and (Test-PipeUp $PipeName)) { $script:pipeUp = $true; Write-Host "pipe $PipeName up after $([int]$sw.Elapsed.TotalSeconds) s"; break }
        if ($p.HasExited) { Write-Host "acad exited early (code $($p.ExitCode))"; break }
        Start-Sleep -Seconds 2
    }
    return $p
}

# ---- Ribbon (UI Automation) -------------------------------------------------------------------------------------
# AdWindows exposes the Ribbon through WPF automation peers: a tab header is a Button whose AutomationId is the
# RibbonTab.Id and whose Name is its title; a RibbonButton is a Button named after its text (newlines become
# spaces) once its tab is selected. Everything is looked up under the AutoCAD frame of the harness's own pid.

function Get-AcadFrame([int]$processId) {
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $byPid = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $processId)
    $frames = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $byPid)
    foreach ($f in $frames) { if ($f.Current.ClassName -like 'Afx*') { return $f } }
    if ($frames.Count -gt 0) { return $frames[0] }
    return $null
}

function Find-RibbonTabs([int]$processId, [string]$tabId) {
    $frame = Get-AcadFrame $processId
    if (-not $frame) { return @() }
    $cond = New-Object System.Windows.Automation.AndCondition(
        (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)),
        (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, $tabId)))
    $found = $frame.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
    $list = @(); foreach ($t in $found) { $list += $t }
    return $list
}

function Select-RibbonTab([int]$processId, [string]$tabId) {
    $tabs = Find-RibbonTabs $processId $tabId
    if ($tabs.Count -eq 0) { return $false }
    try { $tabs[0].GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke(); Start-Sleep -Milliseconds 1500; return $true } catch { Write-Host "select tab failed: $($_.Exception.Message)"; return $false }
}

function Find-RibbonButton([int]$processId, [string]$namePattern) {
    $frame = Get-AcadFrame $processId
    if (-not $frame) { return $null }
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
    foreach ($b in $frame.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)) {
        $name = ($b.Current.Name -replace "`r?`n", ' ')
        if ($name -match $namePattern) { return $b }
    }
    return $null
}

function Invoke-RibbonButton([int]$processId, [string]$namePattern) {
    $button = Find-RibbonButton $processId $namePattern
    if (-not $button) { Write-Host "ribbon button /$namePattern/ not found"; return $false }
    try { $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke(); return $true }
    catch { Write-Host "invoke /$namePattern/ failed: $($_.Exception.Message)"; return $false }
}

function Save-RibbonScreenshot([int]$processId, [string]$path) {
    # A picture of the AutoCAD frame (top 260 px: title, Ribbon tabs, the selected tab's panels) so icons and
    # layout can be checked from a log folder without a person at the screen.
    try {
        Add-Type -AssemblyName System.Drawing
        $frame = Get-AcadFrame $processId
        if (-not $frame) { return $false }
        $r = $frame.Current.BoundingRectangle
        $h = [Math]::Min(260, [int]$r.Height)
        $bmp = New-Object System.Drawing.Bitmap([int]$r.Width, $h)
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.CopyFromScreen([int]$r.X, [int]$r.Y, 0, 0, $bmp.Size)
        $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
        $g.Dispose(); $bmp.Dispose()
        Write-Host "ribbon screenshot: $path"
        return $true
    } catch { Write-Host "screenshot failed: $($_.Exception.Message)"; return $false }
}
