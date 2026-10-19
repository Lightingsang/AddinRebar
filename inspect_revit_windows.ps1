Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$procs = Get-Process Revit -ErrorAction SilentlyContinue
Write-Host "Revit processes count: $($procs.Count)"
foreach ($p in $procs) {
    Write-Host "PID: $($p.Id) MainWindowHandle: $($p.MainWindowHandle) Title: '$($p.MainWindowTitle)'"
}

$root = [System.Windows.Automation.AutomationElement]::RootElement
foreach ($p in $procs) {
    $pidCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $p.Id)
    $children = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $pidCond)
    Write-Host "PID $($p.Id) has $($children.Count) top-level UIAutomation children:"
    foreach ($c in $children) {
        Write-Host "  Name: '$($c.Current.Name)' Class: '$($c.Current.ClassName)' Rect: $($c.Current.BoundingRectangle)"
    }
}
