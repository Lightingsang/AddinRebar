namespace HPDynamo.McpBridge.Coordinator;

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Serilog;

/// <summary>
/// Roslyn AST syntax walker detecting whether script code manages transactions itself
/// or delegates to Dynamo/workspace execution engines.
/// </summary>
internal static class DynamoTransactionDetector
{
    public static bool ManagesOwnTransaction(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        try
        {
            var tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(kind: SourceCodeKind.Script));
            var walker = new TransactionSyntaxWalker();
            walker.Visit(tree.GetRoot());
            return walker.ManagesOwnTransaction;
        }
        catch (Exception ex)
        {
            // Fall back safely if parsing fails on unexpected script input
            Log.Warning(ex, "Failed to parse script AST for transaction detection");
            return false;
        }
    }

    private sealed class TransactionSyntaxWalker : CSharpSyntaxWalker
    {
        public bool ManagesOwnTransaction { get; private set; }

        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            if (ManagesOwnTransaction) return;

            string? typeName = node.Type switch
            {
                IdentifierNameSyntax id => id.Identifier.ValueText,
                QualifiedNameSyntax q => q.Right.Identifier.ValueText,
                _ => null
            };

            if (typeName is "Transaction" or "TransactionGroup" or "SubTransaction")
            {
                ManagesOwnTransaction = true;
                return;
            }

            base.VisitObjectCreationExpression(node);
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (ManagesOwnTransaction) return;

            if (node.Identifier.ValueText == "TransactionManager")
            {
                ManagesOwnTransaction = true;
                return;
            }

            base.VisitIdentifierName(node);
        }

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (ManagesOwnTransaction) return;

            if (node.Expression is IdentifierNameSyntax root)
            {
                if (root.Identifier.ValueText == "Revit" && node.Name.Identifier.ValueText == "Elements")
                {
                    ManagesOwnTransaction = true;
                    return;
                }

                if (root.Identifier.ValueText == "Dynamo" && node.Name.Identifier.ValueText == "Applications")
                {
                    ManagesOwnTransaction = true;
                    return;
                }
            }

            base.VisitMemberAccessExpression(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (ManagesOwnTransaction) return;

            if (node.Expression is MemberAccessExpressionSyntax mae &&
                mae.Name.Identifier.ValueText == "Run" &&
                mae.Expression is IdentifierNameSyntax wsId &&
                wsId.Identifier.ValueText is "ws" or "homeWs" or "activeCanvasWs")
            {
                ManagesOwnTransaction = true;
                return;
            }

            base.VisitInvocationExpression(node);
        }

        public override void VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
        {
            if (ManagesOwnTransaction) return;

            if (node.Expression is IdentifierNameSyntax wsId &&
                wsId.Identifier.ValueText is "ws" or "homeWs" or "activeCanvasWs" &&
                node.WhenNotNull is InvocationExpressionSyntax inv &&
                inv.Expression is MemberBindingExpressionSyntax mbe &&
                mbe.Name.Identifier.ValueText == "Run")
            {
                ManagesOwnTransaction = true;
                return;
            }

            base.VisitConditionalAccessExpression(node);
        }
    }
}
