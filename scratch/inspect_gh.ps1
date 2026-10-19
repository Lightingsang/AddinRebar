Add-Type -Path "C:\Program Files\Rhino 8\System\netcore\RhinoCommon.dll"
Add-Type -Path "C:\Program Files\Rhino 8\Plug-ins\Grasshopper\GH_IO.dll"
Add-Type -Path "C:\Program Files\Rhino 8\Plug-ins\Grasshopper\Grasshopper.dll"

Write-Host "=== IGH_Param Properties & Methods ==="
$t = [Grasshopper.Kernel.IGH_Param]
Write-Host "Interface: $($t.FullName)"

try {
    $p = New-Object Grasshopper.Kernel.Parameters.Param_Number
    Write-Host "Param_Number created successfully: $($p.GetType().FullName)"
    Write-Host "NickName: $($p.NickName)"
} catch {
    Write-Host "Param_Number creation failed: $($_.Exception.Message)"
}
