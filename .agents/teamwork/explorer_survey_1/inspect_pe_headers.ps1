$targetFiles = @(
    'C:\Dce Pro\DCE Pro.dll',
    'C:\Dce Pro\DceAnalyssiss64.dll',
    'C:\Dce Pro\AddinSteel\DceSteel.dll',
    'C:\Dce Pro\AddinSteelCad\DbimSteelAutocad.dll',
    'C:\Dce Pro\V.2020\DCE Excel.dll',
    'C:\Dce Pro\AddinSetup_RVT\DBIM_RVT_26.exe',
    'C:\Dce Pro\DceAnalyssiss32.dll',
    'C:\Dce Pro\ZwDCE Pro.dll',
    'C:\Dce Pro\IcDCE Pro.dll'
)

function Get-PEHeaderInfo ($filePath) {
    $fs = [System.IO.File]::OpenRead($filePath)
    $br = New-Object System.IO.BinaryReader($fs)
    try {
        $mz = $br.ReadUInt16()
        if ($mz -ne 0x5A4D) { return @{ Error = "Not a DOS PE" } }
        $fs.Position = 0x3C
        $peOffset = $br.ReadUInt32()
        $fs.Position = $peOffset
        $peSig = $br.ReadUInt32()
        if ($peSig -ne 0x00004550) { return @{ Error = "Not a valid PE" } }
        
        $machine = $br.ReadUInt16()
        $numSections = $br.ReadUInt16()
        $timeDateStamp = $br.ReadUInt32()
        $ptrSymbolTable = $br.ReadUInt32()
        $numSymbols = $br.ReadUInt32()
        $sizeOptHeader = $br.ReadUInt16()
        $characteristics = $br.ReadUInt16()
        
        $machineStr = switch ($machine) {
            0x014c { "x86 (i386)" }
            0x8664 { "x64 (AMD64)" }
            0x0200 { "IA64" }
            0xaa64 { "ARM64" }
            default { "0x{0:X4}" -f $machine }
        }
        
        $isManaged = $false
        $magic = $br.ReadUInt16()
        $clrHeaderRva = 0
        if ($magic -eq 0x10b) { # PE32
            $fs.Position = $peOffset + 24 + 208 # DataDirectory[14] is CLR
            $clrHeaderRva = $br.ReadUInt32()
        } elseif ($magic -eq 0x20b) { # PE32+
            $fs.Position = $peOffset + 24 + 224 # DataDirectory[14] is CLR
            $clrHeaderRva = $br.ReadUInt32()
        }
        $isManaged = ($clrHeaderRva -ne 0)

        return @{
            Machine = $machineStr
            IsManaged = $isManaged
            Characteristics = "0x{0:X4}" -f $characteristics
            Magic = "0x{0:X4}" -f $magic
            TimeDateStamp = $timeDateStamp
        }
    } finally {
        $br.Close()
        $fs.Close()
    }
}

foreach ($f in $targetFiles) {
    $item = Get-Item $f
    $hash = (Get-FileHash -Path $f -Algorithm SHA256).Hash
    $pe = Get-PEHeaderInfo $f
    
    Write-Host "=========================================================="
    Write-Host "File: $f"
    Write-Host "Size: $($item.Length) bytes ($([Math]::Round($item.Length / 1MB, 2)) MB)"
    Write-Host "LastWriteTime: $($item.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss'))"
    Write-Host "SHA256: $hash"
    Write-Host "PE Machine: $($pe.Machine)"
    Write-Host "PE Magic: $($pe.Magic)"
    Write-Host "IsManaged (.NET): $($pe.IsManaged)"
}
