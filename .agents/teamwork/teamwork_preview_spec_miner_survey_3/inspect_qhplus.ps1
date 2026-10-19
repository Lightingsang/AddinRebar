$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$excel.DisplayAlerts = $false

$wb = $excel.Workbooks.Open("C:\kata_pro\Iso_QHPlus.xls")
$outPath = "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_3\iso_qhplus_analysis.txt"
$sw = New-Object System.IO.StreamWriter($outPath, $false, [System.Text.Encoding]::UTF8)

$sw.WriteLine("================================================================================")
$sw.WriteLine("FILE: C:\kata_pro\Iso_QHPlus.xls")
$sw.WriteLine("================================================================================")
$sw.WriteLine("Sheet Count: " + $wb.Sheets.Count)

foreach ($ws in $wb.Sheets) {
    $sw.WriteLine("`n########################################")
    $sw.WriteLine("SHEET: " + $ws.Name)
    $sw.WriteLine("########################################")
    
    $usedRange = $ws.UsedRange
    $sw.WriteLine("UsedRange: " + $usedRange.Address)
    
    for ($r = 1; $r -le [Math]::Min(35, $usedRange.Rows.Count); $r++) {
        $rowVals = @()
        for ($c = 1; $c -le [Math]::Min(30, $usedRange.Columns.Count); $c++) {
            $cell = $ws.Cells.Item($r, $c)
            $v = $cell.Text
            if ($cell.Formula -and $cell.Formula -ne $v -and $cell.Formula -like "=*") {
                $rowVals += ("[$r,$c] " + $v + " (Formula: " + $cell.Formula + ")")
            } elseif ($v -and $v.Trim().Length -gt 0) {
                $rowVals += ("[$r,$c] " + $v)
            }
        }
        if ($rowVals.Count -gt 0) {
            $sw.WriteLine("Row " + $r + ": " + ($rowVals -join " | "))
        }
    }
}

$sw.Close()
$wb.Close($false)
$excel.Quit()
[System.Runtime.InteropServices.Marshal]::ReleaseComObject($excel) | Out-Null
Write-Host "Done inspecting Iso_QHPlus.xls"
