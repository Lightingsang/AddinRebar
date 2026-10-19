$phase5Dirs = @(
    'HPKata/HPKata.Core/QuantitySurveying',
    'HPKata/HPKata.Core/DrawingLayout',
    'HPKata/HPKata.Core/CadUtilities',
    'HPKata/HPKata.UI/ViewModels/QsTakeoffViewModel.cs',
    'HPKata/HPKata.UI/ViewModels/DrawingUtilityViewModel.cs',
    'HPKata/HPKata.UI/Views/QsTakeoffWindow.xaml.cs',
    'HPKata/HPKata.UI/Views/DrawingUtilityWindow.xaml.cs',
    'HPKata/HPKata.AutoCad/Controllers/QsTakeoffController.cs',
    'HPKata/HPKata.AutoCad/Controllers/GridDrawController.cs',
    'HPKata/HPKata.AutoCad/Controllers/SheetLayoutController.cs',
    'HPKata/HPKata.AutoCad/Controllers/ViewportController.cs',
    'HPKata/HPKata.AutoCad/Controllers/DrawingIndexController.cs',
    'HPKata/HPKata.AutoCad/Controllers/DimensionUtilityController.cs',
    'HPKata/HPKata.AutoCad/Controllers/TextAnnotationController.cs',
    'HPKata/HPKata.AutoCad/Controllers/LayerUtilityController.cs',
    'HPKata/HPKata.AutoCad/Controllers/KataCleanController.cs',
    'HPKata/HPKata.AutoCad/Commands/QsCommands.cs',
    'HPKata/HPKata.AutoCad/Commands/LayoutCommands.cs',
    'HPKata/HPKata.AutoCad/Commands/DimensionUtilityCommands.cs',
    'HPKata/HPKata.AutoCad/Commands/TextAnnotationCommands.cs',
    'HPKata/HPKata.AutoCad/Commands/LayerUtilityCommands.cs',
    'HPKata/HPKata.AutoCad/Commands/KataCleanCommands.cs',
    'HPKata/HPKata.Revit/Services/RevitQsTakeoffService.cs',
    'HPKata/HPKata.Revit/Services/RevitSheetArrangementService.cs',
    'HPKata/HPKata.Revit/Runners/RevitQsLayoutRunner.cs',
    'HPKata/HPKata.Revit/Commands/RevitQsCommands.cs',
    'HPKata/HPKata.Revit/Commands/RevitLayoutCommands.cs'
)

$files = @()
foreach ($p in $phase5Dirs) {
    if (Test-Path $p) {
        if ((Get-Item $p) -is [System.IO.DirectoryInfo]) {
            $files += (Get-ChildItem -Recurse $p -Filter *.cs).FullName
        } else {
            $files += (Get-Item $p).FullName
        }
    }
}

Write-Host "Total Phase 5 files to scan for M3: $($files.Count)"

$longMethods = @()
foreach ($file in $files) {
    $lines = Get-Content $file
    $inMethod = $false
    $depth = 0
    $startLine = 0
    $methodHeader = ""
    
    for ($i = 0; $i -lt $lines.Length; $i++) {
        $line = $lines[$i]
        $trimmed = $line.Trim()
        
        $openBraces = ($line.ToCharArray() | Where-Object { $_ -eq '{' }).Count
        $closeBraces = ($line.ToCharArray() | Where-Object { $_ -eq '}' }).Count
        
        if (-not $inMethod) {
            if ($trimmed -match '^(public|private|protected|internal|static|override|virtual|async).*\(' -and $trimmed -notmatch '\b(class|interface|record|struct|delegate)\b' -and $trimmed -notmatch ';$') {
                $methodHeader = $trimmed
                $startLine = $i + 1
                $inMethod = $true
                $depth = $openBraces - $closeBraces
                if ($depth -eq 0 -and $openBraces -gt 0) {
                    $inMethod = $false
                }
            }
        } else {
            $depth += ($openBraces - $closeBraces)
            if ($depth -le 0) {
                $methodLength = $i - $startLine + 2
                if ($methodLength -gt 50) {
                    $longMethods += [PSCustomObject]@{
                        File = [System.IO.Path]::GetFileName($file)
                        Start = $startLine
                        End = ($i + 1)
                        Length = $methodLength
                        Header = $methodHeader
                    }
                }
                $inMethod = $false
                $depth = 0
            }
        }
    }
}

if ($longMethods.Count -eq 0) {
    Write-Host "PASS: 0 methods exceed 50 physical lines."
} else {
    Write-Host "FAIL: Found $($longMethods.Count) long methods:"
    $longMethods | Format-Table -AutoSize
}
