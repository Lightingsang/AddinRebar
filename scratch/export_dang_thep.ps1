$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$wb = $excel.Workbooks.Open('C:\kata_pro\Iso_QHPlus.xls')
$ws = $wb.Sheets.Item('Dang_thep')

$usedRange = $ws.UsedRange
$rowCount = $usedRange.Rows.Count
$colCount = $usedRange.Columns.Count

$rows = @()

for ($r = 1; $r -le $rowCount; $r++) {
    $rowObj = [ordered]@{}
    $rowObj["Row"] = $r
    for ($c = 1; $c -le $colCount; $c++) {
        $cell = $ws.Cells.Item($r, $c)
        $text = $cell.Text
        $formula = $cell.Formula
        $colLetter = [char](64 + $c)
        if ($c -gt 26) {
            $colLetter = [char](64 + [int]($c / 26)) + [char](64 + ($c % 26))
        }
        $rowObj[$colLetter + "_text"] = $text
        if ($formula -ne $text -and $formula -like "=*") {
            $rowObj[$colLetter + "_formula"] = $formula
        }
    }
    $rows += $rowObj
}

$wb.Close($false)
$excel.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null

$rows | ConvertTo-Json -Depth 4 | Out-File -FilePath 'scratch/iso_qhplus_dang_thep.json' -Encoding utf8
Write-Output "Successfully exported $rowCount rows to scratch/iso_qhplus_dang_thep.json"
