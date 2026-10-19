Add-Type -AssemblyName System.IO.Compression.FileSystem

$cuixPath = 'C:\kata_pro\menukata.cuix'
$zip = [System.IO.Compression.ZipFile]::OpenRead($cuixPath)
Write-Output "=== CUIX ZIP ENTRIES ==="
foreach ($entry in $zip.Entries) {
    Write-Output ("{0,10}  {1}" -f $entry.Length, $entry.FullName)
}
$zip.Dispose()

Write-Output "`n=== MENUKATA.CUI POP MENUS AND TOOLBARS ==="
[xml]$cui = Get-Content 'C:\kata_pro\menukata.cui'
Write-Output ("MenuGroup Name: " + $cui.CustSection.MenuGroup.Name)

$macros = $cui.CustSection.MenuGroup.MacroGroup.MenuMacro
Write-Output ("Total Macros: " + $macros.Count)

$popMenus = $cui.CustSection.MenuGroup.PopMenuRoot.PopMenu
Write-Output ("Total Pop Menus: " + $popMenus.Count)
foreach ($pop in $popMenus) {
    Write-Output ("PopMenu: UID={0}, Name={1}, Items={2}" -f $pop.UID, $pop.Name, $pop.PopMenuItem.Count)
}

$toolbars = $cui.CustSection.MenuGroup.ToolbarRoot.Toolbar
Write-Output ("Total Toolbars: " + $toolbars.Count)
foreach ($tb in $toolbars) {
    Write-Output ("Toolbar: UID={0}, Name={1}, Items={2}" -f $tb.UID, $tb.Name, $tb.ToolbarButton.Count)
}
