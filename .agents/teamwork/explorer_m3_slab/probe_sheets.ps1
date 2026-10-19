$xlsmPath = "C:\kata_pro\Kata.xlsm"
Add-Type -AssemblyName System.IO.Compression.FileSystem

$zip = [System.IO.Compression.ZipFile]::OpenRead($xlsmPath)
$entry = $zip.GetEntry("xl/workbook.xml")
$reader = New-Object System.IO.StreamReader($entry.Open())
$xml = $reader.ReadToEnd()
$reader.Close()
$zip.Dispose()

$pattern = '<sheet [^>]+/>'
$matches = [regex]::Matches($xml, $pattern)
foreach ($m in $matches) {
    Write-Host $m.Value
}
