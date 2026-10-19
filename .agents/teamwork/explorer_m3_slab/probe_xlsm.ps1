$xlsmPath = "C:\kata_pro\Kata.xlsm"
Add-Type -AssemblyName System.IO.Compression.FileSystem

$zip = [System.IO.Compression.ZipFile]::OpenRead($xlsmPath)
Write-Host "=== Sheets in Kata.xlsm ==="
$workbookEntry = $zip.GetEntry("xl/workbook.xml")
if ($workbookEntry) {
    $reader = New-Object System.IO.StreamReader($workbookEntry.Open())
    $wbXml = $reader.ReadToEnd()
    $reader.Close()
    
    # Extract sheet names
    $pattern = 'name="([^"]+)"'
    $matches = [regex]::Matches($wbXml, $pattern)
    foreach ($m in $matches) {
        Write-Host "Sheet: $($m.Groups[1].Value)"
    }
}

$zip.Dispose()
