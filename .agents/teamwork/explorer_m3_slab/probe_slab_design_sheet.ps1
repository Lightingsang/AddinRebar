$xlsmPath = "C:\kata_pro\Kata.xlsm"
Add-Type -AssemblyName System.IO.Compression.FileSystem

$zip = [System.IO.Compression.ZipFile]::OpenRead($xlsmPath)
# Find sheet relationship for rId12
$relsEntry = $zip.GetEntry("xl/_rels/workbook.xml.rels")
$reader = New-Object System.IO.StreamReader($relsEntry.Open())
$relsXml = $reader.ReadToEnd()
$reader.Close()

$match = [regex]::Match($relsXml, 'Id="rId12"[^>]+Target="([^"]+)"')
$target = $match.Groups[1].Value
Write-Host "Target sheet file: $target"

$sheetEntry = $zip.GetEntry("xl/$target")
if ($sheetEntry) {
    # Read shared strings
    $ssEntry = $zip.GetEntry("xl/sharedStrings.xml")
    $strings = @()
    if ($ssEntry) {
        $reader = New-Object System.IO.StreamReader($ssEntry.Open())
        $ssXml = $reader.ReadToEnd()
        $reader.Close()
        $sMatches = [regex]::Matches($ssXml, '<t[^>]*>(.*?)</t>')
        foreach ($sm in $sMatches) {
            $strings += $sm.Groups[1].Value
        }
    }

    $reader = New-Object System.IO.StreamReader($sheetEntry.Open())
    $sheetXml = $reader.ReadToEnd()
    $reader.Close()

    Write-Host "=== First 50 cells in SLAB DESIGN ==="
    $cMatches = [regex]::Matches($sheetXml, '<c r="([A-Z0-9]+)"([^>]*)>(.*?)</c>')
    $count = 0
    foreach ($cm in $cMatches) {
        $r = $cm.Groups[1].Value
        $attrs = $cm.Groups[2].Value
        $inner = $cm.Groups[3].Value
        
        $val = ""
        if ($attrs -match 't="s"') {
            if ($inner -match '<v>(\d+)</v>') {
                $idx = [int]$matches[1]
                if ($idx -lt $strings.Count) { $val = $strings[$idx] }
            }
        } elseif ($inner -match '<v>([^<]+)</v>') {
            $val = $matches[1]
        }
        
        $formula = ""
        if ($inner -match '<f>([^<]+)</f>') {
            $formula = " [=" + $matches[1] + "]"
        }
        
        if ($val -ne "" -or $formula -ne "") {
            Write-Host "$r : $val $formula"
            $count++
            if ($count -ge 60) { break }
        }
    }
}
$zip.Dispose()
