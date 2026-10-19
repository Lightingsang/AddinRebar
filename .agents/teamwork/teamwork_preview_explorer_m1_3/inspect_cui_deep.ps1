Add-Type -AssemblyName System.IO.Compression.FileSystem

# 1. Inspect menukata.cui
[xml]$cui = Get-Content 'C:\kata_pro\menukata.cui'
Write-Output "--- menukata.cui Root Elements ---"
$cui.CustSection.MenuGroup | Get-Member -MemberType Property | Select-Object Name

# Let's inspect MacroGroup in menukata.cui
Write-Output "`n--- Macros in menukata.cui ---"
foreach ($m in $cui.CustSection.MenuGroup.MacroGroup.MenuMacro) {
    Write-Output ("UID: {0} | Name: {1} | Command: {2}" -f $m.UID, $m.Macro.Name.InnerText, $m.Macro.Command)
}

# 2. Inspect menukata.cuix extracted in memory
$zip = [System.IO.Compression.ZipFile]::OpenRead('C:\kata_pro\menukata.cuix')
$popEntry = $zip.GetEntry('PopMenuRoot.cui')
if ($popEntry) {
    $stream = $popEntry.Open()
    $reader = New-Object System.IO.StreamReader($stream)
    $popXmlText = $reader.ReadToEnd()
    $reader.Dispose()
    $stream.Dispose()
    [xml]$popXml = $popXmlText
    Write-Output "`n--- PopMenuRoot.cui in menukata.cuix ---"
    foreach ($pop in $popXml.PopMenuRoot.PopMenu) {
        Write-Output ("PopMenu: UID={0}, Name={1}, Title={2}" -f $pop.UID, $pop.Name, $pop.Title)
        foreach ($item in $pop.PopMenuItem) {
            Write-Output ("   Item: Name={0}, MacroRef={1}" -f $item.Name, $item.MacroRef.MenuMacroID)
        }
    }
}

$tbEntry = $zip.GetEntry('ToolbarRoot.cui')
if ($tbEntry) {
    $stream = $tbEntry.Open()
    $reader = New-Object System.IO.StreamReader($stream)
    $tbXmlText = $reader.ReadToEnd()
    $reader.Dispose()
    $stream.Dispose()
    [xml]$tbXml = $tbXmlText
    Write-Output "`n--- ToolbarRoot.cui in menukata.cuix ---"
    foreach ($tb in $tbXml.ToolbarRoot.Toolbar) {
        Write-Output ("Toolbar: UID={0}, Name={1}" -f $tb.UID, $tb.Name)
        foreach ($item in $tb.ToolbarButton) {
            Write-Output ("   Button: Name={0}, MacroRef={1}" -f $item.Name, $item.MacroRef.MenuMacroID)
        }
    }
}

$mgEntry = $zip.GetEntry('MenuGroup.cui')
if ($mgEntry) {
    $stream = $mgEntry.Open()
    $reader = New-Object System.IO.StreamReader($stream)
    $mgXmlText = $reader.ReadToEnd()
    $reader.Dispose()
    $stream.Dispose()
    [xml]$mgXml = $mgXmlText
    Write-Output "`n--- MenuGroup.cui in menukata.cuix ---"
    Write-Output ("MenuGroup Name: " + $mgXml.MenuGroup.Name)
    foreach ($m in $mgXml.MenuGroup.MacroGroup.MenuMacro) {
        Write-Output ("UID: {0} | Name: {1} | Command: {2}" -f $m.UID, $m.Macro.Name.InnerText, $m.Macro.Command)
    }
}

$zip.Dispose()
