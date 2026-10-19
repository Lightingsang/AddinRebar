Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase

$deployedDir = Join-Path $env:APPDATA 'Autodesk\Revit\Addins\2026\HPDynamo.McpBridge'
$mcpCorePath = Join-Path $deployedDir 'HPRebar.McpBridge.Core.dll'
$bridgePath = Join-Path $deployedDir 'HPDynamo.McpBridge.dll'

Write-Host "Loading assemblies..."
[System.Reflection.Assembly]::LoadFrom($mcpCorePath) | Out-Null
$bridgeAsm = [System.Reflection.Assembly]::LoadFrom($bridgePath)

Write-Host "Creating window on STA thread..."
$code = @"
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

public static class WindowTester
{
    public static string Test()
    {
        string result = "OK";
        var thread = new Thread(() =>
        {
            try
            {
                var viewType = Type.GetType("HPDynamo.McpBridge.View.DynamoBridgeStatusView, HPDynamo.McpBridge");
                if (viewType == null) { result = "ViewType not found"; return; }

                // Fake runner for ViewModel
                // Instantiate window with null or fake
                var ctors = viewType.GetConstructors();
                result = "Constructors found: " + ctors.Length;
            }
            catch (Exception ex)
            {
                result = ex.ToString();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        return result;
    }
}
"@
Add-Type -TypeDefinition $code -ReferencedAssemblies "PresentationFramework", "PresentationCore", "WindowsBase", $mcpCorePath, $bridgePath
$res = [WindowTester]::Test()
Write-Host "Result: $res"
