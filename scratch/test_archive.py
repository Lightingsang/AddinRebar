import subprocess

script = """
$a = [System.Reflection.Assembly]::LoadFrom('C:\\Program Files\\Rhino 8\\Plug-ins\\Grasshopper\\GH_IO.dll')
$arch = [Activator]::CreateInstance($a.GetType('GH_IO.Serialization.GH_Archive'))
$root = $arch.CreateTopLevelNode("Root")
$def = $root.CreateChunk("Definition")
$def.SetString("Name", "MathSlider")
$xml = $arch.Serialize_Xml()
Write-Host "XML Length: $($xml.Length)"
Write-Host ($xml.Substring(0, [Math]::Min(300, $xml.Length)))
$bin = $arch.Serialize_Binary()
Write-Host "Binary Length: $($bin.Length)"
"""

with open("scratch/test_archive.ps1", "w", encoding="utf-8") as f:
    f.write(script)

res = subprocess.run(["powershell", "-NoProfile", "-File", "scratch/test_archive.ps1"], capture_output=True, text=True)
print(res.stdout)
if res.stderr:
    print("ERR:", res.stderr)
