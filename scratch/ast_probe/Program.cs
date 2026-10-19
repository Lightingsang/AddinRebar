using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

class Program
{
    static void Main()
    {
        Inspect("void Run() { using SubTransaction s = new(doc); }");
        Inspect("void Run() { using var s = new SubTransaction(doc); }");
        Inspect("void Run() { Transaction t = condition ? new(doc, \"A\") : new(doc, \"B\"); }");
        Inspect("void Run() { Transaction t; t = new(doc, \"name\"); }");
        Inspect("void Run() { var list = new List<Transaction> { new(doc, \"name\") }; }");
    }

    static void Inspect(string code)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("CODE: " + code);
        var tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(kind: SourceCodeKind.Script));
        foreach (var diag in tree.GetDiagnostics())
        {
            Console.WriteLine("  DIAG: " + diag);
        }
        foreach (var node in tree.GetRoot().DescendantNodesAndSelf())
        {
            if (node is ImplicitObjectCreationExpressionSyntax implicitNew)
            {
                Console.WriteLine($"    >>> ImplicitObjectCreation [{implicitNew}] Parent: {implicitNew.Parent?.GetType().Name} -> GrandParent: {implicitNew.Parent?.Parent?.GetType().Name}");
            }
            if (node is ObjectCreationExpressionSyntax objNew)
            {
                Console.WriteLine($"    >>> ObjectCreation [{objNew}] Type: {objNew.Type}");
            }
        }
    }
}
