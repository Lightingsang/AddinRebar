# Detailed Method-by-Method check for BimModelSchemaValidator and BimModelSchemaValidator.Elements
$files = @(
    "HPBim/src/HPBim.Core/Validation/BimModelSchemaValidator.cs",
    "HPBim/src/HPBim.Core/Validation/BimModelSchemaValidator.Elements.cs"
)

foreach ($filePath in $files) {
    Write-Host "`nChecking: $filePath"
    $lines = Get-Content $filePath
    # Check that file lines <= 300
    Write-Host "File line count: $($lines.Count)"
    if ($lines.Count -gt 300) {
        Write-Error "C2 VIOLATION: File exceeds 300 lines ($($lines.Count))"
    }
}
