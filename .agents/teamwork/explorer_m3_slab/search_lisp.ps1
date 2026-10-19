$lines = Get-Content "C:\kata_pro\Goi lenh.lsp"
for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    if ($line -match "defun c:" -and $line -match "san|cho|ke|vesan") {
        Write-Host "$($i+1): $line"
    }
}
