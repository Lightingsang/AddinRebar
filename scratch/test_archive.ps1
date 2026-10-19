
$a = [System.Reflection.Assembly]::LoadFrom('C:\Program Files\Rhino 8\Plug-ins\Grasshopper\GH_IO.dll')
$arch = [Activator]::CreateInstance($a.GetType('GH_IO.Serialization.GH_Archive'))
$root = $arch.CreateTopLevelNode("Root")
$def = $root.CreateChunk("Definition")
$def.SetString("Name", "MathSlider")
$xml = $arch.Serialize_Xml()
Write-Host "XML Length: $($xml.Length)"
Write-Host ($xml.Substring(0, [Math]::Min(300, $xml.Length)))
$bin = $arch.Serialize_Binary()
Write-Host "Binary Length: $($bin.Length)"
