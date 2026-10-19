using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

class Program
{
    static int Main(string[] args)
    {
        string rootDir = @"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar";

        var targetFiles = new List<string>
        {
            @"HPBim\src\HPBim.Core\Coordination\CompositeSourceKeyJsonConverter.cs",
            @"HPBim\src\HPBim.Core\Coordination\CompositeSourceKey.cs",
            @"HPBim\src\HPBim.Core\Coordination\BimCoordinationLedgerService.cs",
            @"HPBim\src\HPBim.Core\Coordination\BimCoordinationLedgerService.Mappings.cs",
            @"HPBim\src\HPBim.Core\Coordination\BimCoordinationLedgerSerializer.cs",
            @"HPBim\src\HPBim.Core\Execution\ElementExecutionStatus.cs",
            @"HPBim\src\HPBim.Core\Execution\ElementExecutionRecord.cs",
            @"HPBim\src\HPBim.Client\Pipeline\BimPipelineResult.cs",
            @"HPBim\src\HPBim.Client\Pipeline\BimPipelineEngine.ElementRecords.cs",
            @"HPBim\src\HPBim.Client\Pipeline\BimPipelineEngine.PlanExecution.cs",
            @"HPBim\src\HPBim.Client\Pipeline\BimPlanRunnerScriptTemplate.cs",
            @"HPBim\src\HPBim.Client\Pipeline\BimPlanRunnerScriptTemplate.Receipt.cs",
            @"HPBim\src\HPBim.Client\Pipeline\BimPlanRunnerScriptTemplate.Columns.cs",
            @"HPBim\src\HPBim.Client\Pipeline\BimPlanRunnerScriptTemplate.MepAndRebar.cs",
            @"HPBim\src\HPBim.Client\ScriptGenerators\Stage7MepGenerator.cs",
            @"HPBim\src\HPBim.Client\ScriptGenerators\CompositeBimGenerator.Stages7To8.cs",
            @"HPBim\src\HPBim.Client\TypeResolver\TypeResolutionRules.cs",
            @"HPBim\src\HPBim.Client\Pipeline\ExecutionPlanBuilder.ArchitectureAndMep.cs"
        };

        Console.WriteLine("================================================================================");
        Console.WriteLine($"ROSLYN AST METHOD & FILE LENGTH AUDIT FOR MILESTONE M1 REMEDIATION ({targetFiles.Count} Files)");
        Console.WriteLine("================================================================================");

        int totalFilesChecked = 0;
        int fileLengthViolations = 0;
        int totalMethodsChecked = 0;
        int bodyOver50 = 0;

        foreach (var relPath in targetFiles)
        {
            string fullPath = Path.Combine(rootDir, relPath);
            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"[MISSING] {relPath}");
                continue;
            }

            totalFilesChecked++;
            var lines = File.ReadAllLines(fullPath);
            bool fileOver300 = lines.Length > 300;
            if (fileOver300) fileLengthViolations++;

            Console.WriteLine($"\nFile: {relPath} ({lines.Length} lines) {(fileOver300 ? "--> [FAIL C2 > 300]" : "[PASS C2]")}");

            string source = File.ReadAllText(fullPath);
            var tree = CSharpSyntaxTree.ParseText(source, path: fullPath);
            var root = tree.GetCompilationUnitRoot();
            var textLines = tree.GetText().Lines;

            var methods = root.DescendantNodes().Where(n =>
                n is MethodDeclarationSyntax ||
                n is ConstructorDeclarationSyntax ||
                n is LocalFunctionStatementSyntax);

            foreach (var node in methods)
            {
                totalMethodsChecked++;
                var fullSpan = textLines.GetLinePositionSpan(node.Span);
                int declLen = fullSpan.End.Line - fullSpan.Start.Line + 1;

                BlockSyntax? bodyBlock = node switch
                {
                    MethodDeclarationSyntax m => m.Body,
                    ConstructorDeclarationSyntax c => c.Body,
                    LocalFunctionStatementSyntax l => l.Body,
                    _ => null
                };

                int bodyLen = 0;
                if (bodyBlock != null)
                {
                    var bodySpan = textLines.GetLinePositionSpan(bodyBlock.Span);
                    bodyLen = bodySpan.End.Line - bodySpan.Start.Line + 1;
                }
                else
                {
                    // Expression-bodied method (arrow expression)
                    bodyLen = declLen;
                }

                string name = node switch
                {
                    MethodDeclarationSyntax m => m.Identifier.Text,
                    ConstructorDeclarationSyntax c => c.Identifier.Text,
                    LocalFunctionStatementSyntax l => l.Identifier.Text,
                    _ => "Unknown"
                };

                if (bodyLen > 50)
                {
                    bodyOver50++;
                    Console.WriteLine($"  [FAIL M3 > 50] {name}() | Body lines: {bodyLen} (Lines {fullSpan.Start.Line + 1} - {fullSpan.End.Line + 1})");
                }
                else
                {
                    Console.WriteLine($"  [PASS M3] {name}(): {bodyLen} lines");
                }
            }
        }

        Console.WriteLine("\n================================================================================");
        Console.WriteLine("AUDIT SUMMARY:");
        Console.WriteLine($"Total files audited:                 {totalFilesChecked}");
        Console.WriteLine($"Files exceeding 300 lines (Rule C2): {fileLengthViolations}");
        Console.WriteLine($"Total methods audited:               {totalMethodsChecked}");
        Console.WriteLine($"Methods with Body > 50 lines (M3):   {bodyOver50}");
        Console.WriteLine("================================================================================");

        return (fileLengthViolations == 0 && bodyOver50 == 0) ? 0 : 1;
    }
}
