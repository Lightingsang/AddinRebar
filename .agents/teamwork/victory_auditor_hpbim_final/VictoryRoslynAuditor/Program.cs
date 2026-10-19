using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

class Program
{
    static readonly string[] ProductionDirs = new[]
    {
        @"HPBim\src\HPBim.Core\Validation",
        @"HPBim\src\HPBim.Core\Execution",
        @"HPBim\src\HPBim.Core\Coordination",
        @"HPBim\src\HPBim.Core\Verification",
        @"HPBim\src\HPBim.Client\Connection",
        @"HPBim\src\HPBim.Client\Pipeline",
        @"HPBim\src\HPBim.Client\Recovery",
        @"HPBim\src\HPBim.Client\Verification",
    };

    static readonly string[] TestFiles = new[]
    {
        @"HPBim\tests\HPBim.Core.Tests\Adversarial\BimModelSchemaValidatorAdversarialCoverageTests.cs",
        @"HPBim\tests\HPBim.Core.Tests\Adversarial\IdempotentElementResolverAdversarialCoverageTests.cs",
        @"HPBim\tests\HPBim.Core.Tests\Adversarial\CompositeSourceKeyAdversarialCoverageTests.cs",
        @"HPBim\tests\HPBim.Core.Tests\Adversarial\MeasuredGeometricVerifierAdversarialCoverageTests.cs",
        @"HPBim\tests\HPBim.Client.Tests\Connection\IntentJournalTier5AdversarialTests.cs",
        @"HPBim\tests\HPBim.Client.Tests\Connection\TimeoutPolicyTier5AdversarialTests.cs",
        @"HPBim\tests\HPBim.Client.Tests\Recovery\ReceiptReaderTier5AdversarialTests.cs",
        @"HPBim\tests\HPBim.Client.Tests\Recovery\ReconcileServiceTier5AdversarialTests.cs",
        @"HPBim\tests\HPBim.Client.Tests\Pipeline\BimPlanRunnerScriptTemplateTier5AdversarialTests.cs",
        @"HPBim\tests\HPBim.Client.Tests\Pipeline\BimPipelineEngineTier5AdversarialTests.cs",
    };

    public class Violation
    {
        public string File { get; set; } = "";
        public string Member { get; set; } = "";
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public int LineCount { get; set; }
    }

    static int Main()
    {
        string rootDir = @"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar";
        var allFiles = new List<string>();

        foreach (var dir in ProductionDirs)
        {
            string fullDir = Path.Combine(rootDir, dir);
            if (Directory.Exists(fullDir))
            {
                allFiles.AddRange(Directory.GetFiles(fullDir, "*.cs", SearchOption.TopDirectoryOnly));
            }
        }

        foreach (var tf in TestFiles)
        {
            string fullTestPath = Path.Combine(rootDir, tf);
            if (File.Exists(fullTestPath))
            {
                allFiles.Add(fullTestPath);
            }
        }

        Console.WriteLine($"Total files to analyze: {allFiles.Count}");

        int c2Violations = 0;
        var m3Violations = new List<Violation>();
        int totalMembers = 0;
        int totalLines = 0;
        int qb2Violations = 0;

        foreach (var file in allFiles)
        {
            string text = File.ReadAllText(file);
            var tree = CSharpSyntaxTree.ParseText(text, path: file);
            var root = tree.GetCompilationUnitRoot();
            var textLines = tree.GetText().Lines;
            int fileLines = textLines.Count;
            totalLines += fileLines;

            string relPath = Path.GetRelativePath(rootDir, file);

            if (fileLines > 300)
            {
                c2Violations++;
                Console.WriteLine($"[C2 VIOLATION] {relPath}: {fileLines} lines");
            }

            // Check members
            foreach (var node in root.DescendantNodes())
            {
                if (node is MethodDeclarationSyntax method)
                {
                    totalMembers++;
                    int attrStart = method.AttributeLists.Count > 0 ? method.AttributeLists[0].SpanStart : method.SpanStart;
                    var spanAttr = textLines.GetLinePositionSpan(new Microsoft.CodeAnalysis.Text.TextSpan(attrStart, method.Span.End - attrStart));
                    int startLine = spanAttr.Start.Line + 1;
                    int endLine = spanAttr.End.Line + 1;
                    int lineCount = endLine - startLine + 1;

                    if (lineCount > 50)
                    {
                        m3Violations.Add(new Violation
                        {
                            File = relPath,
                            Member = method.Identifier.Text,
                            StartLine = startLine,
                            EndLine = endLine,
                            LineCount = lineCount
                        });
                    }
                }
                else if (node is ConstructorDeclarationSyntax ctor)
                {
                    totalMembers++;
                    int attrStart = ctor.AttributeLists.Count > 0 ? ctor.AttributeLists[0].SpanStart : ctor.SpanStart;
                    var spanAttr = textLines.GetLinePositionSpan(new Microsoft.CodeAnalysis.Text.TextSpan(attrStart, ctor.Span.End - attrStart));
                    int startLine = spanAttr.Start.Line + 1;
                    int endLine = spanAttr.End.Line + 1;
                    int lineCount = endLine - startLine + 1;

                    if (lineCount > 50)
                    {
                        m3Violations.Add(new Violation
                        {
                            File = relPath,
                            Member = ctor.Identifier.Text,
                            StartLine = startLine,
                            EndLine = endLine,
                            LineCount = lineCount
                        });
                    }
                }
                else if (node is CatchClauseSyntax catchClause)
                {
                    if (catchClause.Block.Statements.Count == 0)
                    {
                        bool hasComment = catchClause.Block.DescendantTrivia().Any(t =>
                            t.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                            t.IsKind(SyntaxKind.MultiLineCommentTrivia));

                        if (!hasComment)
                        {
                            qb2Violations++;
                            var span = textLines.GetLinePositionSpan(catchClause.Span);
                            Console.WriteLine($"[Q-B2 VIOLATION] {relPath}: Empty catch without comment at line {span.Start.Line + 1}");
                        }
                    }
                }
            }
        }

        Console.WriteLine("\n==================================================");
        Console.WriteLine("VICTORY AUDITOR ROSLYN SYNTAX AUDIT REPORT");
        Console.WriteLine("==================================================");
        Console.WriteLine($"Total Files Analyzed: {allFiles.Count}");
        Console.WriteLine($"Total Physical Lines: {totalLines}");
        Console.WriteLine($"Total Methods & Constructors: {totalMembers}");
        Console.WriteLine($"Rule C2 Violations (> 300 lines): {c2Violations}");
        Console.WriteLine($"Rule M3 Violations (> 50 lines): {m3Violations.Count}");
        Console.WriteLine($"Rule Q-B2 Violations (uncommented empty catch): {qb2Violations}");

        if (m3Violations.Count > 0)
        {
            Console.WriteLine("\nM3 VIOLATIONS DETAIL:");
            foreach (var v in m3Violations)
            {
                Console.WriteLine($"  {v.File} :: {v.Member} (L{v.StartLine}-L{v.EndLine}, {v.LineCount} lines)");
            }
        }
        else
        {
            Console.WriteLine("ZERO Rule M3 violations found across all analyzed deliverable files!");
        }

        return (c2Violations == 0 && m3Violations.Count == 0 && qb2Violations == 0) ? 0 : 1;
    }
}
