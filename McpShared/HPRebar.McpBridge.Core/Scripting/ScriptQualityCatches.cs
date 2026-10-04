using HPRebar.Mcp.Contracts.Messages;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HPRebar.McpBridge.Core.Scripting;

/// <summary>
///     The two catch rules: an empty catch must say why it ignores the failure (error); a catch of
///     <c>Exception</c> that neither rethrows, returns, nor uses the exception hides failures (warning).
///     Recording the exception — <c>errors.Add(... ex.Message ...)</c> — counts as reporting it.
/// </summary>
internal static class ScriptQualityCatches
{
    public static void Check(SyntaxTree tree, CatchClauseSyntax node, List<QualityFinding> findings)
    {
        var block = node.Block;
        var empty = block.Statements.Count == 0;
        var explained = block.DescendantTrivia().Any(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia));

        if (empty && !explained)
        {
            findings.Add(ScriptQuality.At(tree, node.CatchKeyword, "Q-B2", ScriptQuality.Error,
                "empty catch without a comment giving the reason — handle the failure, or catch the narrowest type and say why it is safe to ignore."));
            return;
        }

        if (empty || !CatchesEverything(node) || Reports(node)) return;

        findings.Add(ScriptQuality.At(tree, node.CatchKeyword, "Q-W5", ScriptQuality.Warning,
            "catch (Exception) neither rethrows, returns nor uses the exception — the failure disappears."));
    }

    private static bool CatchesEverything(CatchClauseSyntax node) => node.Declaration?.Type switch
    {
        null => true,
        IdentifierNameSyntax { Identifier.ValueText: "Exception" } => true,
        QualifiedNameSyntax { Right.Identifier.ValueText: "Exception", Left: var left } => left.ToString() is "System" or "global::System",
        _ => false,
    };

    private static bool Reports(CatchClauseSyntax node)
    {
        var nodes = node.Block.DescendantNodes().ToList();
        if (nodes.Any(n => n is ThrowStatementSyntax
                or ThrowExpressionSyntax
                or ReturnStatementSyntax)) return true;

        var name = node.Declaration?.Identifier.ValueText;
        return !string.IsNullOrEmpty(name)
            && nodes.OfType<IdentifierNameSyntax>().Any(i => i.Identifier.ValueText == name);
    }
}
