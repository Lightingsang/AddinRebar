$files = Get-ChildItem -Path "HPKata\HPKata.AutoCad", "HPKata\HPKata.AutoCad.Tests" -Recurse -Include *.cs, *.lsp, *.csproj | 
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }

$results = foreach ($f in $files) {
    $lines = (Get-Content $f.FullName).Count
    [PSCustomObject]@{
        Name = $f.Name
        Lines = $lines
        RelPath = $f.FullName.Substring((Get-Location).Path.Length + 1)
        Over300 = ($lines -gt 300)
    }
}

$results | Sort-Object Lines -Descending | Format-Table -AutoSize
$over = $results | Where-Object { $_.Over300 }
Write-Host "Total files scanned: $($results.Count)"
Write-Host "Files over 300 lines: $($over.Count)"
