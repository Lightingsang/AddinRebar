namespace HPGrasshopper.McpBridge.Execution;

using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
///     Syntax-only facts about a script with support for C# 9+ target-typed new syntax
///     (ImplicitObjectCreationExpressionSyntax) for transaction detection.
/// </summary>
public static class ScriptAnalyzer
{
    private const int ContextLength = 80;

    public static AnalyzeResult Run(ScriptCompiler compiler, string code, GuardProfile guard, AnalyzerProfile analyzer)
    {
        var result = Analyze(code, analyzer);
        result.GuardViolations = ScriptGuard.Check(code, guard);

        if (result.GuardViolations.Count == 0)
        {
            var compiled = compiler.GetOrCompile(code);
            result.Diagnostics = compiled.Diagnostics;
            result.Compiles = compiled.Succeeded;
            result.CacheHit = compiled.CacheHit;
        }

        return result;
    }

    public static AnalyzeResult Analyze(string code) => Analyze(code, AnalyzerProfile.Grasshopper);

    public static AnalyzeResult Analyze(string code, AnalyzerProfile profile)
    {
        var tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(kind: SourceCodeKind.Script));
        var walker = new FactsWalker(tree, profile);
        walker.Visit(tree.GetRoot());
        var quality = FindQuality(tree, code);

        return new AnalyzeResult
        {
            Literals = walker.Literals,
            ArgKeys = walker.ArgKeys,
            LineCount = tree.GetText().Lines.Count,
            HasLoops = walker.HasLoops,
            UsesTransaction = walker.UsesTransaction,
            QualityAnalysed = quality is not null,
            QualityFindings = quality ?? [],
        };
    }

    private static IReadOnlyList<QualityFinding>? FindQuality(SyntaxTree tree, string code)
    {
        try
        {
            return ScriptQuality.Find(tree, code);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return null;
        }
    }

    private sealed class FactsWalker(SyntaxTree tree, AnalyzerProfile profile) : CSharpSyntaxWalker
    {
        public List<CodeLiteral> Literals { get; } = [];

        public List<ArgUsage> ArgKeys { get; } = [];

        public bool HasLoops { get; private set; }

        public bool UsesTransaction { get; private set; }

        public override void VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            var kind = node.Kind() switch
            {
                SyntaxKind.NumericLiteralExpression => "number",
                SyntaxKind.StringLiteralExpression => "string",
                _ => null,
            };

            if (kind is not null && !IsArgsKey(node) && !IsTrivialNumber(node, kind))
            {
                var position = tree.GetLineSpan(node.Span).StartLinePosition;
                Literals.Add(new CodeLiteral(
                    position.Line + 1,
                    position.Character + 1,
                    kind,
                    node.Token.ValueText,
                    ContextOf(node),
                    BoundName(node)));
            }

            base.VisitLiteralExpression(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "args" } } access
                && node.ArgumentList.Arguments.Count > 0
                && node.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } key)
            {
                var line = tree.GetLineSpan(node.Span).StartLinePosition.Line + 1;
                ArgKeys.Add(new ArgUsage(key.Token.ValueText, access.Name.Identifier.ValueText, line));
            }

            if (node.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: var invoked } && profile.TransactionMethodNames.Contains(invoked))
                UsesTransaction = true;

            base.VisitInvocationExpression(node);
        }

        private readonly HashSet<string> _transactionVars = [];

        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            NoteTransaction(node.Type);
            base.VisitObjectCreationExpression(node);
        }

        public override void VisitArrayCreationExpression(ArrayCreationExpressionSyntax node)
        {
            NoteTransaction(node.Type);
            base.VisitArrayCreationExpression(node);
        }

        public override void VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node)
        {
            SyntaxNode? current = node.Parent;
            while (current is ParenthesizedExpressionSyntax or CastExpressionSyntax or ConditionalExpressionSyntax
                   or CollectionExpressionSyntax or InitializerExpressionSyntax or LambdaExpressionSyntax
                   or ReturnStatementSyntax or ArrayCreationExpressionSyntax
                   or BlockSyntax { Parent: LambdaExpressionSyntax })
            {
                if (current is CastExpressionSyntax cast)
                    NoteTransaction(cast.Type);
                else if (current is ArrayCreationExpressionSyntax arrayCreation)
                    NoteTransaction(arrayCreation.Type);

                current = current.Parent;
            }

            if (current is EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax varDecl } })
            {
                NoteTransaction(varDecl.Type);
            }
            else if (current is AssignmentExpressionSyntax assign)
            {
                string? targetVar = assign.Left switch
                {
                    IdentifierNameSyntax id => id.Identifier.ValueText,
                    ElementAccessExpressionSyntax { Expression: IdentifierNameSyntax elemId } => elemId.Identifier.ValueText,
                    _ => null,
                };
                if (targetVar is not null && (_transactionVars.Contains(targetVar) || profile.TransactionTypeNames.Contains(targetVar)))
                    UsesTransaction = true;
            }

            base.VisitImplicitObjectCreationExpression(node);
        }

        public override void VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            if (IsTransactionType(node.Type))
            {
                UsesTransaction = true;
                foreach (var declarator in node.Variables)
                    _transactionVars.Add(declarator.Identifier.ValueText);
            }

            base.VisitVariableDeclaration(node);
        }

        public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
            if (node.Name is not null)
                NoteTransaction(node.Name);

            base.VisitUsingDirective(node);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            NoteTransaction(node.Type);
            base.VisitPropertyDeclaration(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            NoteTransaction(node.ReturnType);
            base.VisitMethodDeclaration(node);
        }

        public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
        {
            NoteTransaction(node.ReturnType);
            base.VisitLocalFunctionStatement(node);
        }

        public override void VisitCastExpression(CastExpressionSyntax node)
        {
            NoteTransaction(node.Type);
            base.VisitCastExpression(node);
        }

        public override void VisitForStatement(ForStatementSyntax node) { HasLoops = true; base.VisitForStatement(node); }
        public override void VisitForEachStatement(ForEachStatementSyntax node) { HasLoops = true; base.VisitForEachStatement(node); }
        public override void VisitWhileStatement(WhileStatementSyntax node) { HasLoops = true; base.VisitWhileStatement(node); }
        public override void VisitDoStatement(DoStatementSyntax node) { HasLoops = true; base.VisitDoStatement(node); }

        private bool IsTransactionType(TypeSyntax type)
        {
            if (type is NullableTypeSyntax nullable)
                return IsTransactionType(nullable.ElementType);

            if (type is ArrayTypeSyntax array)
                return IsTransactionType(array.ElementType);

            if (type is GenericNameSyntax generic)
                return generic.TypeArgumentList.Arguments.Any(IsTransactionType);

            if (type is QualifiedNameSyntax qualified)
                return IsTransactionType(qualified.Right);

            if (type is AliasQualifiedNameSyntax alias)
                return IsTransactionType(alias.Name);

            var name = type switch
            {
                IdentifierNameSyntax id => id.Identifier.ValueText,
                _ => null,
            };
            return name is not null && profile.TransactionTypeNames.Contains(name);
        }

        private void NoteTransaction(TypeSyntax type)
        {
            if (IsTransactionType(type))
                UsesTransaction = true;
        }

        private static bool IsArgsKey(LiteralExpressionSyntax node) =>
            node.Parent is ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "args" } } } } };

        private static bool IsTrivialNumber(LiteralExpressionSyntax node, string kind) =>
            kind == "number" && node.Token.ValueText is "0" or "1" or "2" or "0.5" or "1.0" or "-1";

        private static string? BoundName(LiteralExpressionSyntax node)
        {
            SyntaxNode? current = node.Parent;
            while (current is PrefixUnaryExpressionSyntax or ParenthesizedExpressionSyntax or CastExpressionSyntax) current = current.Parent;

            return current switch
            {
                EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax declarator } => declarator.Identifier.ValueText,
                _ => null,
            };
        }

        private string ContextOf(SyntaxNode node)
        {
            var line = tree.GetLineSpan(node.Span).StartLinePosition.Line;
            var text = tree.GetText().Lines[line].ToString().Trim();
            return text.Length <= ContextLength ? text : text[..ContextLength] + "…";
        }
    }
}
