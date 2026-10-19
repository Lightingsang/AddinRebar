$targetFiles = @(
    'C:\kata_pro\Goi lenh.lsp',
    'C:\kata_pro\Goi lenh - Gstarcad.lsp',
    'C:\kata_pro\Goi lenh - Zwcad.lsp',
    'C:\kata_pro\menukata.cui',
    'C:\kata_pro\menukata.cuix',
    'C:\kata_pro\menukata.mnr',
    'C:\kata_pro\menukata_light.mnr',
    'C:\kata_pro\Iso_kata.dwg',
    'C:\kata_pro\ISO_KATA.EDB',
    'C:\kata_pro\Kata.xlsm',
    'C:\kata_pro\Kata ENG.xlsm',
    'C:\kata_pro\Kata-GCAD.xlsm',
    'C:\kata_pro\Kata-ZwCad.xlsm',
    'C:\kata_pro\Iso_QHPlus.xls'
)

$results = foreach ($f in $targetFiles) {
    if (Test-Path $f) {
        $item = Get-Item $f
        $hash = (Get-FileHash -Algorithm SHA256 -Path $f).Hash
        [PSCustomObject]@{
            Name = $item.Name
            FullName = $item.FullName
            Length = $item.Length
            SHA256 = $hash
        }
    } else {
        [PSCustomObject]@{
            Name = [System.IO.Path]::GetFileName($f)
            FullName = $f
            Length = -1
            SHA256 = "NOT_FOUND"
        }
    }
}

$results | Format-Table -AutoSize
$results | ConvertTo-Json -Depth 3 | Set-Content -Path "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m1_3\file_hashes.json"
