$acad = [System.Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application')
Write-Host "AutoCAD Version: " $acad.Version
Write-Host "Active Document: " $acad.ActiveDocument.Name
