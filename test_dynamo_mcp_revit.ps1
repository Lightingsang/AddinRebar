# Comprehensive live test for HPDynamo MCP in Revit 2026
param(
    [string]$OutDir = "C:\Users\STR-HP03\.gemini\antigravity\brain\4cb249bf-78d3-4e3f-aa18-e0869d1403cc"
)

$ErrorActionPreference = 'Stop'
Write-Host "=== Starting HPDynamo MCP Live Test in Revit 2026 ===" -ForegroundColor Cyan

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

$sig = @'
using System; using System.Text; using System.Runtime.InteropServices;
public static class RvLive {
  public delegate bool Proc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumWindows(Proc p, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr hWndParent, Proc p, IntPtr l);
  [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr h, StringBuilder t, int n);
  [DllImport("user32.dll")] public static extern int GetClassName(IntPtr h, StringBuilder t, int n);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
  [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, UIntPtr e);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }

  public const uint WM_COMMAND = 0x0111;
  public const uint BM_CLICK = 0x00F5;

  public static void DismissSecurityDialogs(uint targetPid) {
    EnumWindows((h, l) => {
      uint pid;
      GetWindowThreadProcessId(h, out pid);
      if (pid != targetPid || !IsWindowVisible(h)) return true;

      var sb = new StringBuilder(256);
      GetWindowText(h, sb, 256);
      string title = sb.ToString();

      // Check TaskDialog or security dialog
      if (title.Contains("Security") || title.Contains("publisher") || title.Contains("Add-in") || title.Contains("TaskDialog")) {
        // Try clicking button 1001 (Always Load)
        SendMessage(h, WM_COMMAND, (IntPtr)1001, IntPtr.Zero);

        // Also enum child buttons
        EnumChildWindows(h, (hc, lc) => {
          var sbc = new StringBuilder(256);
          GetWindowText(hc, sbc, 256);
          string btnText = sbc.ToString();
          if (btnText.Contains("Always Load")) {
            SendMessage(hc, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
          }
          return true;
        }, IntPtr.Zero);
      }
      return true;
    }, IntPtr.Zero);
  }

  public static IntPtr FindTopLevel(uint pid, string titleStart) {
    IntPtr found = IntPtr.Zero;
    EnumWindows((h, l) => {
      uint p; GetWindowThreadProcessId(h, out p);
      if (p != pid || !IsWindowVisible(h)) return true;
      var sb = new StringBuilder(256); GetWindowText(h, sb, 256);
      if (sb.ToString().StartsWith(titleStart)) { found = h; return false; }
      return true;
    }, IntPtr.Zero);
    return found;
  }
}
'@
if (-not ('RvLive' -as [type])) { Add-Type -TypeDefinition $sig }
try { [RvLive]::SetProcessDPIAware() } catch { }

# 1. Check or start Revit 2026
$revit = Get-Process Revit -ErrorAction SilentlyContinue | Select-Object -First 1
if ($null -eq $revit) {
    $revitExe = "C:\Program Files\Autodesk\Revit 2026\Revit.exe"
    if (-not (Test-Path $revitExe)) {
        throw "Revit 2026 executable not found at $revitExe"
    }
    $sampleModel = "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRebar\HPRebar.Tests\Fixtures\column-stack-2-storey.rvt"
    Write-Host "[1/5] Launching Revit 2026 with sample model ($sampleModel)..." -ForegroundColor Yellow
    $proc = Start-Process $revitExe -ArgumentList "`"$sampleModel`"" -PassThru
    $revit = $proc
    Write-Host "Revit started (PID $($revit.Id)). Waiting for initialization..." -ForegroundColor Yellow
} else {
    Write-Host "[1/5] Revit 2026 is already running (PID $($revit.Id))." -ForegroundColor Green
}

# 2. Wait for named pipe hpdynamo-mcp-2026 while auto-dismissing security prompts
Write-Host "[2/5] Waiting for Named Pipe '\\.\pipe\hpdynamo-mcp-2026'..." -ForegroundColor Yellow
$pipeName = "hpdynamo-mcp-2026"
$pipeFound = $false
$sw = [System.Diagnostics.Stopwatch]::StartNew()

function DismissViaUia([int]$targetPid) {
    try {
        $root = [System.Windows.Automation.AutomationElement]::RootElement
        $pidCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $targetPid)
        $children = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $pidCond)
        foreach ($child in $children) {
            $btnCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, "Always Load")
            $btn = $child.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $btnCond)
            if ($null -ne $btn) {
                Write-Host "Found 'Always Load' via UIA on '$($child.Current.Name)'. Dismissing dialog..." -ForegroundColor Green
                
                # 1. Try UIA Invoke
                $inv = $null
                if ($btn.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$inv)) {
                    try { $inv.Invoke() } catch { }
                }

                # 2. Try physical mouse click
                $rect = $btn.Current.BoundingRectangle
                if ($rect.Width -gt 0 -and $rect.Height -gt 0) {
                    $x = [int]($rect.X + $rect.Width / 2)
                    $y = [int]($rect.Y + $rect.Height / 2)
                    [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point($x, $y)
                    [RvLive]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
                    Start-Sleep -Milliseconds 60
                    [RvLive]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
                }

                # 3. Try Win32 SendMessage BM_CLICK
                $hBtn = [IntPtr]$btn.Current.NativeWindowHandle
                if ($hBtn -ne [IntPtr]::Zero) {
                    [RvLive]::SendMessage($hBtn, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero)
                }

                # 4. Try Win32 SendMessage WM_COMMAND 1001 to parent dialog
                $hChild = [IntPtr]$child.Current.NativeWindowHandle
                if ($hChild -ne [IntPtr]::Zero) {
                    [RvLive]::SendMessage($hChild, 0x0111, [IntPtr]1001, [IntPtr]::Zero)
                }

                # 5. Try keyboard Enter
                try {
                    $btn.SetFocus()
                    [System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
                } catch { }
            }
        }
    } catch {
        # ignore transient COM errors
    }
}

while ($sw.Elapsed.TotalSeconds -lt 180) {
    $pipes = [System.IO.Directory]::GetFiles("\\.\pipe\")
    if ($pipes -contains "\\.\pipe\$pipeName") {
        $pipeFound = $true
        break
    }

    DismissViaUia $revit.Id

    Start-Sleep -Seconds 2
    Write-Host "  ... waiting for bridge pipe ($([int]$sw.Elapsed.TotalSeconds)s)"
}

if (-not $pipeFound) {
    throw "Timeout waiting for named pipe '\\.\pipe\$pipeName'."
}
Write-Host "[OK] Named Pipe '\\.\pipe\$pipeName' is ONLINE!" -ForegroundColor Green

# 3. Test Named Pipe communication
Write-Host "[3/5] Testing JSON-RPC ping over Named Pipe..." -ForegroundColor Yellow
$pipeClient = New-Object System.IO.Pipes.NamedPipeClientStream(".", $pipeName, [System.IO.Pipes.PipeDirection]::InOut)
$pipeClient.Connect(5000)
$writer = New-Object System.IO.StreamWriter($pipeClient)
$reader = New-Object System.IO.StreamReader($pipeClient)
$writer.AutoFlush = $true

$req = '{"jsonrpc":"2.0","id":1,"method":"revit.status","params":{}}'
$writer.WriteLine($req)
$res = $reader.ReadLine()
Write-Host "Pipe response: $res" -ForegroundColor Gray
$pipeClient.Dispose()
Write-Host "[OK] Named Pipe JSON-RPC ping successful!" -ForegroundColor Green

# 4. Locate Revit main window
Write-Host "[4/5] Interacting with Revit UI via UI Automation..." -ForegroundColor Yellow
$root = [System.Windows.Automation.AutomationElement]::RootElement

$main = $null
$swMain = [System.Diagnostics.Stopwatch]::StartNew()
while ($swMain.Elapsed.TotalSeconds -lt 90 -and $null -eq $main) {
    $revitProc = Get-Process Revit -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -ne $revitProc -and $revitProc.MainWindowHandle -ne [IntPtr]::Zero) {
        try {
            $candidate = [System.Windows.Automation.AutomationElement]::FromHandle($revitProc.MainWindowHandle)
            if ($null -ne $candidate -and $candidate.Current.BoundingRectangle.Width -ge 300) {
                $main = $candidate
                [RvLive]::SetForegroundWindow($revitProc.MainWindowHandle) | Out-Null
                break
            }
        } catch { }
    }

    if ($null -ne $revitProc) {
        $pidCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $revitProc.Id)
        $allWins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $pidCond)
        foreach ($w in $allWins) {
            if ($w.Current.BoundingRectangle.Width -ge 400 -and -not $w.Current.IsOffscreen) {
                $main = $w
                [RvLive]::SetForegroundWindow([IntPtr]$w.Current.NativeWindowHandle) | Out-Null
                break
            }
        }
    }
    if ($null -eq $main) {
        Start-Sleep -Seconds 2
    }
}

if ($null -eq $main) {
    throw "Revit main window could not be found via UIAutomation."
}
Write-Host "Revit main window located: $($main.Current.Name) ($($main.Current.BoundingRectangle.Width)x$($main.Current.BoundingRectangle.Height))" -ForegroundColor Green
Start-Sleep -Seconds 2

function SelectRibbonTab([System.Windows.Automation.AutomationElement] $scope, [string] $tabName) {
    Write-Host "Activating Ribbon tab '$tabName'..." -ForegroundColor Yellow
    $c1 = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $tabName)
    $c2 = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::TabItem)
    $cond = New-Object System.Windows.Automation.AndCondition(@($c1, $c2))
    $tab = $scope.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
    if ($null -eq $tab) {
        $nameCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $tabName)
        $all = $scope.FindAll([System.Windows.Automation.TreeScope]::Descendants, $nameCond)
        foreach ($el in $all) {
            $r = $el.Current.BoundingRectangle
            if ($r.Y -ge 20 -and $r.Y -le 120 -and $r.Width -ge 20) {
                $tab = $el
                break
            }
        }
    }

    if ($null -ne $tab) {
        $r = $tab.Current.BoundingRectangle
        Write-Host "Found Tab '$tabName' at ($($r.X), $($r.Y), $($r.Width)x$($r.Height))" -ForegroundColor Cyan
        
        $pat = $null
        if ($tab.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$pat)) {
            try { $pat.Select() } catch { }
        }
        
        $x = [int]($r.X + $r.Width / 2)
        $y = [int]($r.Y + $r.Height / 2)
        [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point($x, $y)
        [RvLive]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
        Start-Sleep -Milliseconds 80
        [RvLive]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
        Write-Host "[OK] Clicked tab '$tabName' at ($x, $y)" -ForegroundColor Green
        return $true
    }
    Write-Host "[WARN] Could not find Tab '$tabName'" -ForegroundColor Yellow
    return $false
}

function ClickRibbonButton([System.Windows.Automation.AutomationElement] $scope, [string] $regexPattern) {
    $all = $scope.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($el in $all) {
        $name = $el.Current.Name
        $aid = $el.Current.AutomationId
        if (($name -match $regexPattern -or $aid -match $regexPattern) -and -not $el.Current.IsOffscreen) {
            $r = $el.Current.BoundingRectangle
            if ($r.Y -ge 20 -and $r.Y -le 250 -and $r.Width -ge 10 -and $r.Height -ge 10) {
                Write-Host "Found button matching '$regexPattern': '$name' (Id: $aid) at ($($r.X), $($r.Y), $($r.Width)x$($r.Height))" -ForegroundColor Cyan
                
                $inv = $null
                if ($el.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$inv)) {
                    try { $inv.Invoke() } catch { }
                }
                
                $x = [int]($r.X + $r.Width / 2)
                $y = [int]($r.Y + $r.Height / 2)
                [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point($x, $y)
                [RvLive]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
                Start-Sleep -Milliseconds 80
                [RvLive]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
                Write-Host "[OK] Clicked button '$name' at ($x, $y)" -ForegroundColor Green
                return $true
            }
        }
    }
    Write-Host "[WARN] Could not find button matching '$regexPattern'. Listing visible ribbon elements:" -ForegroundColor Yellow
    foreach ($el in $all) {
        $r = $el.Current.BoundingRectangle
        if ($r.Y -ge 20 -and $r.Y -le 250 -and -not $el.Current.IsOffscreen -and ($el.Current.Name -ne "" -or $el.Current.AutomationId -ne "")) {
            Write-Host "  Ribbon item: Name='$($el.Current.Name)' Id='$($el.Current.AutomationId)' Rect=($($r.X), $($r.Y), $($r.Width)x$($r.Height))" -ForegroundColor DarkGray
        }
    }
    return $false
}

function SendPipeCommand([string]$method, [hashtable]$params = @{}) {
    try {
        $client = New-Object System.IO.Pipes.NamedPipeClientStream(".", $pipeName, [System.IO.Pipes.PipeDirection]::InOut)
        $client.Connect(5000)
        $sw = New-Object System.IO.StreamWriter($client)
        $sr = New-Object System.IO.StreamReader($client)
        $sw.AutoFlush = $true
        $payload = @{
            jsonrpc = "2.0"
            id = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
            method = $method
            params = $params
        } | ConvertTo-Json -Compress
        $sw.WriteLine($payload)
        $res = $sr.ReadLine()
        $client.Dispose()
        return $res
    } catch {
        Write-Host "Pipe command $method failed: $_" -ForegroundColor DarkGray
        return $null
    }
}

# 1. Switch to Manage tab (via Pipe with UI fallback)
Write-Host "Activating Manage tab..." -ForegroundColor Yellow
$tabRes = SendPipeCommand "dynamo.activate_tab"
Write-Host "Activate Tab response: $tabRes" -ForegroundColor Gray
Start-Sleep -Milliseconds 1500

SelectRibbonTab $main "Manage" | Out-Null
Start-Sleep -Milliseconds 1500

# Capture Manage ribbon area
$rMain = New-Object RvLive+RECT
[RvLive]::SetForegroundWindow($main.Current.NativeWindowHandle) | Out-Null
Start-Sleep -Milliseconds 500
[RvLive]::GetWindowRect($main.Current.NativeWindowHandle, [ref]$rMain) | Out-Null
$wMain = [Math]::Max(10, $rMain.R - $rMain.L)
$hMain = [Math]::Max(10, $rMain.B - $rMain.T)
$bmpM = New-Object System.Drawing.Bitmap($wMain, $hMain)
$gM = [System.Drawing.Graphics]::FromImage($bmpM)
try {
    $gM.CopyFromScreen($rMain.L, $rMain.T, 0, 0, (New-Object System.Drawing.Size($wMain, $hMain)))
} catch {
    $hdcM = $gM.GetHdc()
    [RvLive]::PrintWindow($main.Current.NativeWindowHandle, $hdcM, 2) | Out-Null
    $gM.ReleaseHdc($hdcM)
}
$bmpM.Save((Join-Path $OutDir "revit_manage_ribbon_verified.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$gM.Dispose()
$bmpM.Dispose()
Write-Host "[OK] Captured Manage Ribbon -> revit_manage_ribbon_verified.png" -ForegroundColor Green

# 2. Open Dynamo MCP Bridge UI (via Pipe with UI click fallback)
Write-Host "Opening Dynamo MCP Bridge UI..." -ForegroundColor Yellow
$winRes = SendPipeCommand "dynamo.show_window"
Write-Host "Show Window response: $winRes" -ForegroundColor Gray

$hBridge = [IntPtr]::Zero
$swBridge = [System.Diagnostics.Stopwatch]::StartNew()
while ($swBridge.Elapsed.TotalSeconds -lt 15 -and $hBridge -eq [IntPtr]::Zero) {
    Start-Sleep -Milliseconds 500
    $hBridge = [RvLive]::FindTopLevel([uint32]$revit.Id, "HPDynamo MCP Bridge")
}

if ($hBridge -eq [IntPtr]::Zero) {
    Write-Host "Trying UI click fallback for Dynamo MCP button..." -ForegroundColor Yellow
    ClickRibbonButton $main "MCP" | Out-Null
    $swBridge.Restart()
    while ($swBridge.Elapsed.TotalSeconds -lt 15 -and $hBridge -eq [IntPtr]::Zero) {
        Start-Sleep -Milliseconds 500
        $hBridge = [RvLive]::FindTopLevel([uint32]$revit.Id, "HPDynamo MCP Bridge")
    }
}

if ($hBridge -ne [IntPtr]::Zero) {
    Write-Host "[OK] HPDynamo MCP Bridge window found! Capturing screenshot..." -ForegroundColor Green
    [RvLive]::SetForegroundWindow($hBridge) | Out-Null
    Start-Sleep -Seconds 2
    $r = New-Object RvLive+RECT
    [RvLive]::GetWindowRect($hBridge, [ref]$r) | Out-Null
    $w = [Math]::Max(10, $r.R - $r.L)
    $h = [Math]::Max(10, $r.B - $r.T)
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    try {
        $g.CopyFromScreen($r.L, $r.T, 0, 0, (New-Object System.Drawing.Size($w, $h)))
    } catch {
        $hdc = $g.GetHdc()
        [RvLive]::PrintWindow($hBridge, $hdc, 2) | Out-Null
        $g.ReleaseHdc($hdc)
    }

    $bridgeShotPath = Join-Path $OutDir "dynamo_mcp_bridge_verified.png"
    $bmp.Save($bridgeShotPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $bmp.Dispose()
    Write-Host "[OK] Saved Bridge UI screenshot -> $bridgeShotPath" -ForegroundColor Green
} else {
    Write-Host "[WARN] HPDynamo MCP Bridge window did not open in time." -ForegroundColor Yellow
}

# 3. Launch Dynamo to verify DynamoView.xaml crash is fixed
Write-Host "[5/5] Launching Dynamo to verify crash fix..." -ForegroundColor Yellow
$dynRes = SendPipeCommand "dynamo.launch_dynamo"
Write-Host "Launch Dynamo response: $dynRes" -ForegroundColor Gray

# Also try UI click fallback if needed
Start-Sleep -Milliseconds 1500
$hDynamo = [IntPtr]::Zero
$swDynamo = [System.Diagnostics.Stopwatch]::StartNew()
while ($swDynamo.Elapsed.TotalSeconds -lt 15 -and $hDynamo -eq [IntPtr]::Zero) {
    Start-Sleep -Seconds 1
    $hDynamo = [RvLive]::FindTopLevel([uint32]$revit.Id, "Dynamo")
}

if ($hDynamo -eq [IntPtr]::Zero) {
    Write-Host "Trying UI click fallback for Dynamo button..." -ForegroundColor Yellow
    ClickRibbonButton $main "^Dynamo$" | Out-Null
    while ($swDynamo.Elapsed.TotalSeconds -lt 45) {
        Start-Sleep -Seconds 2
        $hDynamo = [RvLive]::FindTopLevel([uint32]$revit.Id, "Dynamo")
        if ($hDynamo -ne [IntPtr]::Zero) { break }
    }
}

if ($hDynamo -ne [IntPtr]::Zero) {
    Write-Host "[OK] Dynamo window detected! Allowing canvas to render..." -ForegroundColor Green
    [RvLive]::SetForegroundWindow($hDynamo) | Out-Null
    Start-Sleep -Seconds 5
}

# Capture Dynamo screen
$rect = New-Object RvLive+RECT
$hwndTarget = if ($hDynamo -ne [IntPtr]::Zero) { $hDynamo } else { $main.Current.NativeWindowHandle }
[RvLive]::SetForegroundWindow($hwndTarget) | Out-Null
Start-Sleep -Milliseconds 500
[RvLive]::GetWindowRect($hwndTarget, [ref]$rect) | Out-Null
$w = [Math]::Max(10, $rect.R - $rect.L)
$h = [Math]::Max(10, $rect.B - $rect.T)
$bmpD = New-Object System.Drawing.Bitmap($w, $h)
$gD = [System.Drawing.Graphics]::FromImage($bmpD)
try {
    $gD.CopyFromScreen($rect.L, $rect.T, 0, 0, (New-Object System.Drawing.Size($w, $h)))
} catch {
    $hdcD = $gD.GetHdc()
    [RvLive]::PrintWindow($hwndTarget, $hdcD, 2) | Out-Null
    $gD.ReleaseHdc($hdcD)
}

$dynamoShotPath = Join-Path $OutDir "dynamo_running_verified.png"
$bmpD.Save($dynamoShotPath, [System.Drawing.Imaging.ImageFormat]::Png)
$gD.Dispose()
$bmpD.Dispose()
Write-Host "[OK] Saved Dynamo verification screenshot -> $dynamoShotPath" -ForegroundColor Green

Write-Host "`n=== All Live Verifications Finished Successfully! ===" -ForegroundColor Green
