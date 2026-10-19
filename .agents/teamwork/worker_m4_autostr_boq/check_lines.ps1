$files = Get-ChildItem -Path "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro" -Recurse -File -Include *.cs,*.xaml | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
$overCount = 0
foreach ($f in $files) {
    $count = (Get-Content $f.FullName | Measure-Object -Line).Lines
    if ($count -gt 300) {
        Write-Host "$($f.FullName): $count lines (EXCEEDS 300)"
        $overCount++
    }
}
Write-Host "Total files > 300 lines: $overCount"
