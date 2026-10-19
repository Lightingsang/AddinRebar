#Requires -Version 5.1
<#
.SYNOPSIS
    Kịch bản kiểm tra sức khỏe và chẩn đoán kết nối cho HPGrasshopper MCP.
.DESCRIPTION
    Kiểm tra sự tồn tại của file thực thi MCP Server, trạng thái Named Pipe IPC,
    tiến trình Revit 2026 và Rhino.Inside.
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Continue"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "       HPGRASSHOPPER MCP - DIAGNOSTIC HEALTH CHECK        " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$allPassed = $true

# 1. Kiểm tra File thực thi Server
$repoRoot = (Resolve-Path "$PSScriptRoot\..\..\..\..").Path
$serverExeRelease = Join-Path $repoRoot "HPGrasshopper\HPGrasshopper.Mcp.Server\bin\Release\net10.0\HPGrasshopper.Mcp.Server.exe"
$serverExeDebug = Join-Path $repoRoot "HPGrasshopper\HPGrasshopper.Mcp.Server\bin\Debug\net10.0\HPGrasshopper.Mcp.Server.exe"

$serverExe = $null
if (Test-Path $serverExeRelease) {
    $serverExe = $serverExeRelease
    Write-Host "[PASS] Đã tìm thấy Server Exe (Release): $serverExe" -ForegroundColor Green
} elseif (Test-Path $serverExeDebug) {
    $serverExe = $serverExeDebug
    Write-Host "[PASS] Đã tìm thấy Server Exe (Debug): $serverExe" -ForegroundColor Green
} else {
    Write-Host "[FAIL] Không tìm thấy HPGrasshopper.Mcp.Server.exe! Hãy build solution trước." -ForegroundColor Red
    $allPassed = $false
}

# 2. Kiểm tra tiến trình Revit 2026
$revitProcesses = Get-Process -Name "Revit" -ErrorAction SilentlyContinue
if ($revitProcesses) {
    Write-Host "[PASS] Đang có $($revitProcesses.Count) tiến trình Revit đang chạy (PID: $($revitProcesses.Id -join ', '))" -ForegroundColor Green
} else {
    Write-Host "[WARN] Không có tiến trình Revit.exe nào đang chạy. Bridge cần Revit mở để kết nối." -ForegroundColor Yellow
}

# 3. Kiểm tra Named Pipe của Grasshopper Bridge
$pipeName = "hpgrasshopper-mcp-2026"
$pipeExists = $false
try {
    $pipes = [System.IO.Directory]::GetFiles("\\.\pipe\", "*hpgrasshopper*")
    if ($pipes | Where-Object { $_ -like "*$pipeName*" }) {
        $pipeExists = $true
    }
} catch {
    # Fallback kiểm tra
}

if ($pipeExists) {
    Write-Host "[PASS] Đã phát hiện Named Pipe '\\.\pipe\$pipeName' đang mở!" -ForegroundColor Green
} else {
    Write-Host "[INFO] Chưa tìm thấy pipe '\\.\pipe\$pipeName'. Pipe sẽ tự động mở khi khởi động Revit có nạp HPGrasshopper.McpBridge." -ForegroundColor Cyan
}

# 4. Kiểm tra cài đặt Rhino 8 & Grasshopper
$rhino8Dir = "C:\Program Files\Rhino 8"
if (Test-Path $rhino8Dir) {
    Write-Host "[PASS] Đã tìm thấy cài đặt Rhino 8 tại: $rhino8Dir" -ForegroundColor Green
} else {
    Write-Host "[WARN] Không tìm thấy thư mục 'C:\Program Files\Rhino 8'. Hãy kiểm tra đường dẫn Rhino 8." -ForegroundColor Yellow
}

# 5. Kiểm tra Rhino.Inside.Revit
$rhinoInsideAddin = "$env:ProgramData\Autodesk\Revit\Addins\2026\RhinoInside.Revit.addin"
if (Test-Path $rhinoInsideAddin) {
    Write-Host "[PASS] Đã tìm thấy Rhino.Inside.Revit trong Revit 2026 ($rhinoInsideAddin)" -ForegroundColor Green
} else {
    Write-Host "[WARN] Không tìm thấy RhinoInside.Revit.addin trong ProgramData 2026." -ForegroundColor Yellow
}

Write-Host "----------------------------------------------------------" -ForegroundColor Cyan
if ($allPassed) {
    Write-Host "KẾT LUẬN: Hệ thống cơ bản đã sẵn sàng cho HPGrasshopper MCP!" -ForegroundColor Green
} else {
    Write-Host "KẾT LUẬN: Cần hoàn tất một số điều kiện trên trước khi kết nối." -ForegroundColor Yellow
}
Write-Host "==========================================================" -ForegroundColor Cyan
