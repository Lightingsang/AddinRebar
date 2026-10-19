<#
.SYNOPSIS
    Standalone static analysis audit verifying zero occurrences of legacy pattern
    across all source files in HPKata.
.DESCRIPTION
    Scans *.cs, *.xaml, *.lsp, *.csproj, *.xml, *.addin, *.ps1 under HPKata/,
    excluding bin, obj, .vs, .git, TestResults directories.
    Exits with code 0 on PASS, code 1 on FAIL.
#>

param(
    [string]$RootDirectory = "$PSScriptRoot\.."
)

$ErrorActionPreference = "Stop"
$resolvedRoot = [System.IO.Path]::GetFullPath($RootDirectory)
$forbidden = "kata" + "_pro"

Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "HPKATA STATIC ANALYSIS: ZERO LEGACY OCCURRENCE AUDIT" -ForegroundColor Cyan
Write-Host "Root Directory: $resolvedRoot" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan

$extensions = @(".cs", ".xaml", ".lsp", ".csproj", ".xml", ".addin", ".ps1")
$excludedDirs = @("bin", "obj", ".vs", ".git", "TestResults", ".idea", "HPKata.E2E.Tests")

$files = Get-ChildItem -Path $resolvedRoot -Recurse -File | Where-Object {
    $ext = $_.Extension.ToLowerInvariant()
    if ($extensions -notcontains $ext) { return $false }
    $relative = $_.FullName.Substring($resolvedRoot.Length).TrimStart('\', '/')
    $parts = $relative -split '[\\/]'
    foreach ($part in $parts) {
        if ($excludedDirs -contains $part) { return $false }
    }
    return $true
}

$violations = @()

foreach ($file in $files) {
    $lines = [System.IO.File]::ReadAllLines($file.FullName)
    for ($i = 0; $i -lt $lines.Length; $i++) {
        if ($lines[$i] -match $forbidden) {
            $rel = $file.FullName.Substring($resolvedRoot.Length).TrimStart('\', '/')
            $violations += [PSCustomObject]@{
                File = $rel
                LineNumber = $i + 1
                LineContent = $lines[$i].Trim()
            }
        }
    }
}

if ($violations.Count -eq 0) {
    Write-Host ""
    Write-Host "PASS: Zero occurrences found across $($files.Count) source files." -ForegroundColor Green
    exit 0
} else {
    Write-Host ""
    Write-Host "FAIL: Found $($violations.Count) forbidden occurrence(s):" -ForegroundColor Red
    foreach ($v in $violations) {
        Write-Host "  [$($v.File):$($v.LineNumber)] $($v.LineContent)" -ForegroundColor Yellow
    }
    exit 1
}
