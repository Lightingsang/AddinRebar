# Excel run of the script-quality live check: a scratch workbook under HPExcel/output/, Excel started on it, the Debug
# bridge app, UIA for listener / opt-in / attach, then the shared Python harness. Windows PowerShell 5.1.
$ErrorActionPreference = 'Stop'
$repo = 'F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar'
. (Join-Path $PSScriptRoot 'bridge-ui.ps1')
if (Get-Process HPExcel.McpBridge -ErrorAction SilentlyContinue) { throw 'an Excel bridge is already running' }

# An Excel left running with no workbook (hidden) would be the one the bridge attaches to: quit it only when it is empty.
foreach ($old in @(Get-Process EXCEL -ErrorAction SilentlyContinue)) {
    $state = & powershell.exe -NoProfile -Command "try { `$x = [Runtime.InteropServices.Marshal]::GetActiveObject('Excel.Application'); if (`$x.Workbooks.Count -eq 0) { `$x.Quit(); 'quit empty Excel' } else { 'BUSY ' + `$x.Workbooks.Count } } catch { 'COM: ' + `$_.Exception.Message }"
    "existing Excel pid $($old.Id): $state"
    if ("$state" -match 'BUSY') { throw 'an Excel with open workbooks is running - stop and ask the user' }
    $null = $old.WaitForExit(15000)
    if (-not $old.HasExited) { throw "Excel pid $($old.Id) did not quit" }
}

# New blank workbook: start Excel, then Esc on its start screen opens Book1 (unsaved, no path).
Add-Type -AssemblyName System.Windows.Forms
Add-Type -Namespace QualityExcel -Name Fg -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);'
$excel = Start-Process 'C:\Program Files\Microsoft Office\root\Office16\EXCEL.EXE' -PassThru
"Excel pid $($excel.Id) started $(Get-Date -Format HH:mm:ss)"
$sw = [Diagnostics.Stopwatch]::StartNew()
while ($sw.Elapsed.TotalSeconds -lt 120) {
    Start-Sleep 3
    $excel.Refresh()
    if ($excel.MainWindowTitle -match '^Book\d') { break }
    if ($excel.MainWindowHandle -ne [IntPtr]::Zero) { [QualityExcel.Fg]::SetForegroundWindow($excel.MainWindowHandle) | Out-Null; [System.Windows.Forms.SendKeys]::SendWait('{ESC}') }
}
"Excel window '$($excel.MainWindowTitle)' after $([int]$sw.Elapsed.TotalSeconds) s"
Start-Sleep -Seconds 5

$bridge = Start-Process (Join-Path $repo 'HPExcel\HPExcel.McpBridge\bin\Debug\net8.0-windows\HPExcel.McpBridge.exe') -PassThru
try {
    $w = Get-AppWindow $bridge.Id
    Start-Sleep -Seconds 4
    Describe-Controls $w
    $null = Invoke-ButtonNamed $w '^Start'
    Set-ExecutionOptIn $w $true
    $null = Invoke-ButtonNamed $w '^(Attach|Connect)$'
    Start-Sleep -Seconds 6
    "status texts: $((Read-Texts $w 'ttach|onnect|Excel|quality') -join ' | ')"
    & python (Join-Path $repo 'McpShared\tools\live-verify-quality.py') --host excel --out (Join-Path $repo 'plans\261004-1005-hp-clean-code-ai-tools\reports\live-quality')
    "harness exit $LASTEXITCODE"
    "excel $((Get-Item 'C:\Program Files\Microsoft Office\root\Office16\EXCEL.EXE').VersionInfo.ProductVersion)"
}
finally {
    try { Set-ExecutionOptIn (Get-AppWindow $bridge.Id 5) $false } catch { }
    if (-not $bridge.HasExited) { $null = $bridge.CloseMainWindow(); if (-not $bridge.WaitForExit(10000)) { Stop-Process -Id $bridge.Id -Force } }
    $excel.Refresh()
    if (-not $excel.HasExited) {
        & powershell.exe -NoProfile -Command "try { `$x = [Runtime.InteropServices.Marshal]::GetActiveObject('Excel.Application'); foreach (`$wb in @(`$x.Workbooks)) { if (`$wb.Path -eq '') { `$wb.Close(`$false) } }; if (`$x.Workbooks.Count -eq 0) { `$x.Quit() } } catch { }" | Out-Null
        if (-not $excel.WaitForExit(20000)) { "Excel did not exit; killing our pid"; Stop-Process -Id $excel.Id -Force }
    }
    "Excel and bridge closed"
}
