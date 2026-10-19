$r = Get-Process Revit -ErrorAction SilentlyContinue | Select-Object -First 1
if ($r) {
    Write-Host "Revit is running: PID $($r.Id), WindowHandle: $($r.MainWindowHandle), Title: '$($r.MainWindowTitle)'"
} else {
    Write-Host "Revit is not running."
}
