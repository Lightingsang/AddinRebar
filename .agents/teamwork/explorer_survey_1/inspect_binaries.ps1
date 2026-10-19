$targetFiles = @(
    'C:\Dce Pro\DCE Pro.dll',
    'C:\Dce Pro\DceAnalyssiss64.dll',
    'C:\Dce Pro\AddinSteel\DceSteel.dll',
    'C:\Dce Pro\AddinSteelCad\DbimSteelAutocad.dll',
    'C:\Dce Pro\V.2020\DCE Excel.dll',
    'C:\Dce Pro\AddinSetup_RVT\DBIM_RVT_26.exe',
    'C:\Dce Pro\DceAnalyssiss32.dll',
    'C:\Dce Pro\V.2020\DceAnalyssiss64.dll',
    'C:\Dce Pro\V.2020\DceAnalyssiss32.dll',
    'C:\Dce Pro\ZwDCE Pro.dll',
    'C:\Dce Pro\IcDCE Pro.dll'
)

$results = foreach ($f in $targetFiles) {
    if (Test-Path $f) {
        $item = Get-Item $f
        $hash = (Get-FileHash -Path $f -Algorithm SHA256).Hash
        [PSCustomObject]@{
            Path = $f
            Exists = $true
            Length = $item.Length
            LastWriteTime = $item.LastWriteTime
            SHA256 = $hash
        }
    } else {
        [PSCustomObject]@{
            Path = $f
            Exists = $false
            Length = 0
            LastWriteTime = $null
            SHA256 = 'MISSING'
        }
    }
}

$results | Format-Table -AutoSize
$results | Export-Clixml -Path "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_1\file_hashes.xml"
