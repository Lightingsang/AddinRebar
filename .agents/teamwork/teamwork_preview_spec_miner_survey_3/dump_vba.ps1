$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$excel.DisplayAlerts = $false
$outDir = "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_3\vba_dump"
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }

try {
    $wb = $excel.Workbooks.Open("C:\kata_pro\Kata.xlsm")
    Write-Host "VBProject Name: $($wb.VBProject.Name)"
    foreach ($comp in $wb.VBProject.VBComponents) {
        $name = $comp.Name
        $count = $comp.CodeModule.CountOfLines
        Write-Host "Component: $name (Type: $($comp.Type), Lines: $count)"
        if ($count -gt 0) {
            $code = $comp.CodeModule.Lines(1, $count)
            $filePath = Join-Path $outDir "$name.bas"
            [System.IO.File]::WriteAllText($filePath, $code, [System.Text.Encoding]::UTF8)
        }
    }
    $wb.Close($false)
} catch {
    Write-Error $_
} finally {
    $excel.Quit()
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($excel) | Out-Null
}
Write-Host "Completed dumping VBA from Kata.xlsm"
