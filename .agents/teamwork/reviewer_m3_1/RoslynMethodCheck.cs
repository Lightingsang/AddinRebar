using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

class Program
{
    static void Main()
    {
        var bridgeDir = @"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge";
        var files = Directory.GetFiles(bridgeDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(@"\bin\") && !f.Contains(@"\obj\"))
            .ToList();

        Console.WriteLine($"Examining {files.Count} files with Roslyn AST Parser...");
        int violationCount = 0;

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            var tree = CSharpSyntaxTree.ParseText(text);
            var root = tree.GetRoot();

            var methods = root.DescendantNodes().OfType<BaseMethodDeclarationSyntax>();

            foreach (var m in methods)
            {
                var lineSpan = m.GetLocation().GetLineSpan();
                int startLine = lineSpan.StartLinePosition.Line + 1;
                int endLine = lineSpan.EndLinePosition.Line + 1;
                int totalLines = endLine - startLine + 1;

                int bodyLines = 0;
                if (m.Body != null)
                {
                    var bodySpan = m.Body.GetLocation().GetLineSpan();
                    bodyLines = bodySpan.EndLinePosition.Line - bodySpan.StartLinePosition.Line + 1;
                }

                string name = m switch
                {
                    MethodDeclarationSyntax mds => mds.Identifier.Text,
                    ConstructorDeclarationSyntax cds => cds.Identifier.Text + " (ctor)",
                    DestructorDeclarationSyntax dds => "~" + dds.Identifier.Text,
                    _ => m.ToString()
                };

                var rel = Path.GetRelativePath(bridgeDir, file);

                if (totalLines > 50 || bodyLines > 50)
                {
                    violationCount++;
                    Console.WriteLine($"[VIOLATION] {rel} -> {name}: Total={totalLines} lines (Body={bodyLines} lines) [Lines {startLine}-{endLine}]");
                }
            }
        }

        Console.WriteLine($"\nTotal Roslyn Method Violations (> 50 lines): {violationCount}");
    }
}
