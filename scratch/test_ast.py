import os

cs_code = """
using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

class Program {
    static void Main() {
        var code = @"using SubTransaction s = new(doc);";
        var tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(kind: SourceCodeKind.Script));
        Console.WriteLine("Diagnostics: " + string.Join("; ", tree.GetDiagnostics()));
        foreach (var n in tree.GetRoot().DescendantNodesAndSelf()) {
            Console.WriteLine(n.GetType().Name + ": [" + n.ToString() + "]");
        }
    }
}
"""

with open("scratch/test_ast.cs", "w", encoding="utf-8") as f:
    f.write(cs_code)
