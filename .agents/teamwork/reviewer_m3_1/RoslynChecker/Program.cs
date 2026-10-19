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
        var rootDir = @"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata";
        var files = Directory.GetFiles(rootDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(@"\bin\") && !f.Contains(@"\obj\") && !f.Contains(@"\.vs\"))
            .ToList();

        Console.WriteLine($"Examining {files.Count} files with Roslyn AST Parser...");
        int totalMethods = 0;
        int violationCount = 0;

        var phase4Keywords = new[] { "foundation", "footing", "pile", "slab", "stair", "lintel", "sectioncut", "genericsection", "tier3", "phase4" };

        foreach (var file in files)
        {
            var rel = Path.GetRelativePath(rootDir, file);
            bool isPhase4 = phase4Keywords.Any(k => rel.ToLowerInvariant().Contains(k));

            var text = File.ReadAllText(file);
            var tree = CSharpSyntaxTree.ParseText(text);
            var root = tree.GetRoot();

            // Find all methods, constructors, operators, local functions
            var methods = root.DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Cast<SyntaxNode>()
                .Concat(root.DescendantNodes().OfType<LocalFunctionStatementSyntax>());

            foreach (var m in methods)
            {
                totalMethods++;
                var lineSpan = m.GetLocation().GetLineSpan();
                int startLine = lineSpan.StartLinePosition.Line + 1;
                int endLine = lineSpan.EndLinePosition.Line + 1;
                int totalLines = endLine - startLine + 1;

                string name = m switch
                {
                    MethodDeclarationSyntax mds => mds.Identifier.Text,
                    ConstructorDeclarationSyntax cds => cds.Identifier.Text + " (.ctor)",
                    DestructorDeclarationSyntax dds => "~" + dds.Identifier.Text,
                    LocalFunctionStatementSyntax lfs => lfs.Identifier.Text + " (local fn)",
                    _ => m.GetType().Name
                };

                if (totalLines > 50)
                {
                    violationCount++;
                    string phaseTag = isPhase4 ? "[PHASE 4 VIOLATION]" : "[OTHER PHASE VIOLATION]";
                    Console.WriteLine($"{phaseTag} {rel} -> {name}: {totalLines} lines (lines {startLine}-{endLine})");
                }
            }
        }

        Console.WriteLine($"\nScanned {totalMethods} methods across {files.Count} files. Found {violationCount} violations > 50 lines.");
    }
}
