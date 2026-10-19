using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AstTool;

public class MethodMetric
{
    public string FilePath { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public string MemberType { get; set; } = ""; // Method, Constructor, LocalFunction, Accessor
    public string Signature { get; set; } = "";
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public int TotalSpanLines { get; set; }
    public int BodyStartLine { get; set; }
    public int BodyEndLine { get; set; }
    public int BodySpanLines { get; set; }
    public bool HasBlockBody { get; set; }
    public bool HasExpressionBody { get; set; }
}

public class FileMetric
{
    public string FilePath { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public int TotalLines { get; set; }
    public List<MethodMetric> Methods { get; set; } = new();
}

public class MetricWalker : CSharpSyntaxWalker
{
    private readonly SyntaxTree _tree;
    private readonly string _filePath;
    private readonly string _relativePath;
    public List<MethodMetric> Methods { get; } = new();

    public MetricWalker(SyntaxTree tree, string filePath, string relativePath)
        : base(SyntaxWalkerDepth.Node)
    {
        _tree = tree;
        _filePath = filePath;
        _relativePath = relativePath;
    }

    private MethodMetric RecordMember(SyntaxNode node, SyntaxNode? body, ArrowExpressionClauseSyntax? expressionBody, string memberType, string signature)
    {
        var totalSpan = _tree.GetLineSpan(node.Span);
        int startLine = totalSpan.StartLinePosition.Line + 1;
        int endLine = totalSpan.EndLinePosition.Line + 1;
        int totalSpanLines = endLine - startLine + 1;

        int bodyStart = startLine;
        int bodyEnd = endLine;
        int bodySpanLines = totalSpanLines;
        bool hasBlock = body != null;
        bool hasExpr = expressionBody != null;

        if (body != null)
        {
            var bSpan = _tree.GetLineSpan(body.Span);
            bodyStart = bSpan.StartLinePosition.Line + 1;
            bodyEnd = bSpan.EndLinePosition.Line + 1;
            bodySpanLines = bodyEnd - bodyStart + 1;
        }
        else if (expressionBody != null)
        {
            var eSpan = _tree.GetLineSpan(expressionBody.Span);
            bodyStart = eSpan.StartLinePosition.Line + 1;
            bodyEnd = eSpan.EndLinePosition.Line + 1;
            bodySpanLines = bodyEnd - bodyStart + 1;
        }

        var metric = new MethodMetric
        {
            FilePath = _filePath,
            RelativePath = _relativePath,
            MemberType = memberType,
            Signature = signature,
            StartLine = startLine,
            EndLine = endLine,
            TotalSpanLines = totalSpanLines,
            BodyStartLine = bodyStart,
            BodyEndLine = bodyEnd,
            BodySpanLines = bodySpanLines,
            HasBlockBody = hasBlock,
            HasExpressionBody = hasExpr
        };

        Methods.Add(metric);
        return metric;
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        string sig = $"{node.ReturnType} {node.Identifier}{node.ParameterList}";
        RecordMember(node, node.Body, node.ExpressionBody, "Method", sig);
        base.VisitMethodDeclaration(node);
    }

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        string sig = $"{node.Identifier}{node.ParameterList}";
        RecordMember(node, node.Body, node.ExpressionBody, "Constructor", sig);
        base.VisitConstructorDeclaration(node);
    }

    public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
    {
        string sig = $"{node.ReturnType} {node.Identifier}{node.ParameterList}";
        RecordMember(node, node.Body, node.ExpressionBody, "LocalFunction", sig);
        base.VisitLocalFunctionStatement(node);
    }

    public override void VisitAccessorDeclaration(AccessorDeclarationSyntax node)
    {
        string parentName = "Unknown";
        if (node.Parent?.Parent is PropertyDeclarationSyntax prop)
            parentName = prop.Identifier.Text;
        else if (node.Parent?.Parent is IndexerDeclarationSyntax)
            parentName = "this[]";

        string sig = $"{parentName}.{node.Keyword.Text}";
        RecordMember(node, node.Body, node.ExpressionBody, "Accessor", sig);
        base.VisitAccessorDeclaration(node);
    }

    public override void VisitOperatorDeclaration(OperatorDeclarationSyntax node)
    {
        string sig = $"{node.ReturnType} operator {node.OperatorToken}{node.ParameterList}";
        RecordMember(node, node.Body, node.ExpressionBody, "Operator", sig);
        base.VisitOperatorDeclaration(node);
    }

    public override void VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
    {
        string sig = $"{node.ImplicitOrExplicitKeyword} operator {node.Type}{node.ParameterList}";
        RecordMember(node, node.Body, node.ExpressionBody, "ConversionOperator", sig);
        base.VisitConversionOperatorDeclaration(node);
    }
}

public class Program
{
    public static int Main(string[] args)
    {
        string rootDir = args.Length > 0
            ? args[0]
            : @"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge";

        if (!Directory.Exists(rootDir))
        {
            Console.WriteLine($"Error: Directory not found: {rootDir}");
            return 1;
        }

        var csFiles = Directory.GetFiles(rootDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) &&
                        !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            .OrderBy(f => f)
            .ToList();

        Console.WriteLine($"Scanned {csFiles.Count} C# source files in {rootDir}");

        var fileMetrics = new List<FileMetric>();
        var allMethods = new List<MethodMetric>();

        foreach (var file in csFiles)
        {
            string relPath = Path.GetRelativePath(rootDir, file).Replace('\\', '/');
            string text = File.ReadAllText(file);
            int lineCount = text.Split('\n').Length;

            var tree = CSharpSyntaxTree.ParseText(text, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest));
            var root = tree.GetRoot();

            var walker = new MetricWalker(tree, file, relPath);
            walker.Visit(root);

            var fm = new FileMetric
            {
                FilePath = file,
                RelativePath = relPath,
                TotalLines = lineCount,
                Methods = walker.Methods
            };

            fileMetrics.Add(fm);
            allMethods.AddRange(walker.Methods);
        }

        // Summary counts
        Console.WriteLine($"Total methods/members found: {allMethods.Count}");

        // Rule C2: Files > 300 lines
        var filesOver300 = fileMetrics.Where(f => f.TotalLines > 300).ToList();
        Console.WriteLine($"\n=== RULE C2 CHECK: Files > 300 lines ({filesOver300.Count} violations) ===");
        foreach (var f in filesOver300)
        {
            Console.WriteLine($"  [VIOLATION C2] {f.RelativePath} -> {f.TotalLines} lines");
        }
        if (filesOver300.Count == 0)
        {
            Console.WriteLine("  PASS: 0 files exceed 300 lines.");
        }

        // Methods > 40 lines (Span or Body)
        var methodsOver40 = allMethods.Where(m => m.TotalSpanLines > 40 || m.BodySpanLines > 40)
            .OrderByDescending(m => Math.Max(m.TotalSpanLines, m.BodySpanLines))
            .ToList();

        Console.WriteLine($"\n=== METHODS WITH TOTAL SPAN OR BODY > 40 LINES ({methodsOver40.Count} methods found) ===");
        foreach (var m in methodsOver40)
        {
            string flag = (m.TotalSpanLines > 50 || m.BodySpanLines > 50) ? "[RULE M3 VIOLATION > 50]" : "[BORDERLINE 41-50]";
            Console.WriteLine($"{flag,-25} {m.RelativePath}:{m.StartLine}-{m.EndLine} (Total: {m.TotalSpanLines} lines, Body: {m.BodySpanLines} lines, lines {m.BodyStartLine}-{m.BodyEndLine})");
            Console.WriteLine($"   Signature: {m.Signature.Trim()}");
        }

        // Methods > 50 lines (Span or Body)
        var methodsOver50 = allMethods.Where(m => m.TotalSpanLines > 50 || m.BodySpanLines > 50).ToList();
        Console.WriteLine($"\n=== STRICT RULE M3 VIOLATIONS (> 50 LINES) ({methodsOver50.Count} violations) ===");
        foreach (var m in methodsOver50)
        {
            Console.WriteLine($"  FAIL: {m.RelativePath}:{m.StartLine}-{m.EndLine} (Total: {m.TotalSpanLines}, Body: {m.BodySpanLines}) => {m.Signature.Trim()}");
        }

        // Write detailed JSON to disk for report generation
        string jsonOutput = JsonSerializer.Serialize(new
        {
            ScannedFiles = csFiles.Count,
            TotalMethods = allMethods.Count,
            FilesOver300 = filesOver300,
            MethodsOver40 = methodsOver40,
            MethodsOver50 = methodsOver50,
            AllFileMetrics = fileMetrics
        }, new JsonSerializerOptions { WriteIndented = true });

        string jsonPath = Path.Combine(AppContext.BaseDirectory, "metrics_result.json");
        File.WriteAllText(jsonPath, jsonOutput);
        Console.WriteLine($"\nJSON report written to: {jsonPath}");

        return 0;
    }
}
