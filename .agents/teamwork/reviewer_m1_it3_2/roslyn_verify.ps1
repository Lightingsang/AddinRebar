$roslynCommon = "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\tests\HPBim.Client.Tests\bin\Debug\net10.0\Microsoft.CodeAnalysis.dll"
$roslynCSharp = "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\tests\HPBim.Client.Tests\bin\Debug\net10.0\Microsoft.CodeAnalysis.CSharp.dll"

Add-Type -Path $roslynCommon
Add-Type -Path $roslynCSharp

$files = @(
    "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Core\Validation\BimModelSchemaValidator.cs",
    "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Core\Validation\BimModelSchemaValidator.Elements.cs",
    "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Client\Pipeline\BimPipelineEngine.PlanExecution.cs",
    "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Client\Pipeline\ExecutionPlanBuilder.cs",
    "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Client\Pipeline\ExecutionPlanBuilder.ArchitectureAndMep.cs"
)

Write-Host "=== ROSLYN AST METHOD AND FILE LENGTH AUDIT ===" -ForegroundColor Cyan

$violationsFound = 0

foreach ($f in $files) {
    $code = [System.IO.File]::ReadAllText($f)
    $lines = [System.IO.File]::ReadAllLines($f)
    $fileLineCount = $lines.Length
    $leaf = [System.IO.Path]::GetFileName($f)
    
    $c2Status = if ($fileLineCount -le 300) { "PASS" } else { "FAIL" }
    if ($c2Status -eq "FAIL") { $violationsFound++ }
    
    Write-Host "`nFile: $leaf | Total Lines: $fileLineCount | C2 (<= 300): $c2Status" -ForegroundColor Yellow

    $tree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText($code)
    $root = $tree.GetRoot()

    $methods = $root.DescendantNodes() | Where-Object {
        $_ -is [Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax] -or
        $_ -is [Microsoft.CodeAnalysis.CSharp.Syntax.ConstructorDeclarationSyntax] -or
        $_ -is [Microsoft.CodeAnalysis.CSharp.Syntax.LocalFunctionStatementSyntax]
    }

    foreach ($m in $methods) {
        $span = $m.GetLocation().GetLineSpan()
        $startLine = $span.StartLinePosition.Line + 1
        $endLine = $span.EndLinePosition.Line + 1
        $length = $endLine - $startLine + 1
        $name = if ($m.Identifier) { $m.Identifier.Text } else { "ctor" }
        $kind = $m.Kind()
        
        $m3Status = if ($length -le 50) { "PASS" } else { "FAIL (> 50)" }
        $color = if ($length -le 35) { "Green" } elseif ($length -le 50) { "Yellow" } else { "Red" }
        if ($length -gt 50) { $violationsFound++ }

        Write-Host ("  {0,-30} [{1,-25}] L{2,3}-L{3,3} ({4,2} lines) -> {5}" -f $name, $kind, $startLine, $endLine, $length, $m3Status) -ForegroundColor $color
    }
}

Write-Host "`nTotal Violations: $violationsFound" -ForegroundColor (if ($violationsFound -eq 0) { "Green" } else { "Red" })
if ($violationsFound -gt 0) { exit 1 } else { exit 0 }
