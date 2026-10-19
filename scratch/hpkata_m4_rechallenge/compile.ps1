$netDir = 'C:\Program Files\dotnet\shared\Microsoft.NETCore.App\8.0.30'
$binDir = 'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.UI.Tests\bin\Debug\net8.0-windows'
$refs = @(
    "$netDir\System.Private.CoreLib.dll",
    "$netDir\System.Runtime.dll",
    "$netDir\System.Collections.dll",
    "$netDir\System.Linq.dll",
    "$netDir\System.Console.dll",
    "$netDir\System.IO.FileSystem.dll",
    "$netDir\System.ComponentModel.Primitives.dll",
    "$netDir\System.ObjectModel.dll",
    "$binDir\HPKata.Core.dll",
    "$binDir\HPKata.UI.dll",
    "$binDir\CommunityToolkit.Mvvm.dll"
)
$refArgs = $refs | ForEach-Object { "-r:$_" }
& 'dotnet' 'exec' 'C:\Program Files\dotnet\sdk\10.0.400\Roslyn\bincore\csc.dll' -nullable:enable -target:exe -out:scratch\hpkata_m4_rechallenge\rechallenge.dll $refArgs scratch\hpkata_m4_rechallenge\Program.cs
exit $LASTEXITCODE
