$dir = "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_slab\decompiled"
$files = Get-ChildItem -Path $dir -Filter "*.cs"
foreach ($f in $files) {
    $txt = [System.IO.File]::ReadAllText($f.FullName)
    [System.IO.File]::WriteAllText($f.FullName, $txt, [System.Text.Encoding]::UTF8)
}
Write-Host "Re-encoded $($files.Count) files to UTF-8"
