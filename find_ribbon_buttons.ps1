Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$sig = @'
using System; using System.Text; using System.Runtime.InteropServices;
public static class RvInspect {
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, UIntPtr e);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd, out RECT r);
  public delegate bool Proc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumWindows(Proc p, IntPtr l);
  [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr h, StringBuilder t, int n);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }

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
if (-not ('RvInspect' -as [type])) { Add-Type -TypeDefinition $sig }

$revit = Get-Process Revit -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $revit) { Write-Host "No Revit process"; exit 0 }

$root = [System.Windows.Automation.AutomationElement]::RootElement
$pidCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $revit.Id)
$allWins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $pidCond)
$main = $allWins | Where-Object { $_.Current.BoundingRectangle.Width -ge 400 -and -not $_.Current.IsOffscreen } | Select-Object -First 1
if ($null -eq $main) { throw "Main Revit window not found" }
[RvInspect]::SetForegroundWindow([IntPtr]$main.Current.NativeWindowHandle) | Out-Null

Write-Host "Searching for ribbon elements containing 'Dynamo' or 'MCP'..."

$all = $main.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)

$matches = @()
foreach ($el in $all) {
    $name = $el.Current.Name
    if ($name -match "Dynamo" -or $name -match "MCP") {
        $rect = $el.Current.BoundingRectangle
        $kinds = $el.Current.ControlType.ProgrammaticName
        $off = $el.Current.IsOffscreen
        Write-Host "Found: '$name' ($kinds) Offscreen: $off Rect: ($($rect.X), $($rect.Y), $($rect.Width)x$($rect.Height))"
        if (-not $off -and $rect.Width -gt 5 -and $rect.Height -gt 5) {
            $matches += $el
        }
    }
}

# If we found any on-screen button with MCP or Dynamo, click it!
$mcpBtn = $matches | Where-Object { $_.Current.Name -match "MCP" -and $_.Current.ControlType.ProgrammaticName -eq "ControlType.Button" } | Select-Object -First 1
if ($mcpBtn) {
    Write-Host "`nClicking MCP button: $($mcpBtn.Current.Name)..." -ForegroundColor Green
    $rect = $mcpBtn.Current.BoundingRectangle
    $x = [int]($rect.X + $rect.Width / 2)
    $y = [int]($rect.Y + $rect.Height / 2)
    [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point($x, $y)
    [RvInspect]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 80
    [RvInspect]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Write-Host "Clicked at $x, $y"

    # Wait for bridge window
    Write-Host "Waiting for HPDynamo MCP Bridge window..."
    $h = [IntPtr]::Zero
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt 15 -and $h -eq [IntPtr]::Zero) {
        Start-Sleep -Milliseconds 400
        $h = [RvInspect]::FindTopLevel([uint32]$revit.Id, "HPDynamo MCP Bridge")
    }

    if ($h -ne [IntPtr]::Zero) {
        Write-Host "[SUCCESS] HPDynamo MCP Bridge window is OPEN! Capturing screenshot..." -ForegroundColor Green
        Start-Sleep -Seconds 2
        $r = New-Object RvInspect+RECT
        [RvInspect]::GetWindowRect($h, [ref]$r) | Out-Null
        $w = [Math]::Max(10, $r.R - $r.L)
        $ht = [Math]::Max(10, $r.B - $r.T)
        $bmp = New-Object System.Drawing.Bitmap($w, $ht)
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $hdc = $g.GetHdc()
        [RvInspect]::PrintWindow($h, $hdc, 2) | Out-Null
        $g.ReleaseHdc($hdc)

        $out = "C:\Users\STR-HP03\.gemini\antigravity\brain\4cb249bf-78d3-4e3f-aa18-e0869d1403cc\dynamo_mcp_bridge_verified.png"
        $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
        $g.Dispose()
        $bmp.Dispose()
        Write-Host "[OK] Saved: $out" -ForegroundColor Green
    } else {
        Write-Host "Bridge window not found" -ForegroundColor Yellow
    }
} else {
    Write-Host "No on-screen MCP button found." -ForegroundColor Yellow
}
