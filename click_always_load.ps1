Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$sig = '[DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, UIntPtr e);'
if (-not ('Win32Util.MouseClick' -as [type])) {
    Add-Type -MemberDefinition $sig -Name MouseClick -Namespace Win32Util
}

$revit = Get-Process Revit -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $revit) { Write-Host 'No Revit process'; exit 0 }

$root = [System.Windows.Automation.AutomationElement]::RootElement
$pidCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $revit.Id)
$children = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $pidCond)
Write-Host "Found $($children.Count) top-level elements for Revit PID $($revit.Id)"

foreach ($child in $children) {
    Write-Host "Element: '$($child.Current.Name)' Class: '$($child.Current.ClassName)'"
    $btnCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'Always Load')
    $btn = $child.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $btnCond)
    if ($btn) {
        $rect = $btn.Current.BoundingRectangle
        Write-Host "Found 'Always Load' button at ($($rect.X), $($rect.Y), $($rect.Width), $($rect.Height))" -ForegroundColor Green
        $x = [int]($rect.X + $rect.Width / 2)
        $y = [int]($rect.Y + $rect.Height / 2)
        [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point($x, $y)
        [Win32Util.MouseClick]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
        Start-Sleep -Milliseconds 60
        [Win32Util.MouseClick]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
        Write-Host "Clicked at ($x, $y)!" -ForegroundColor Green
    }
}
