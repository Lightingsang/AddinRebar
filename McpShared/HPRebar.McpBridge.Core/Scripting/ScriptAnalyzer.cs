using HPRebar.Mcp.Contracts.Messages;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HPRebar.McpBridge.Core.Scripting;

/// <summary>
///     Syntax-only facts about a script: which literals could become parameters, which `args` keys it
///     already reads, whether it loops or opens its own transaction, and its readability findings
///     (<see cref="ScriptQuality"/>). Pure Roslyn, no compilation, no
///     Revit — cheap enough to run on every proposal. <see cref="Run"/> adds the guard and the compiler
///     so the bridge answers `revit.analyze` in one call.
/// </summary>
public static class ScriptAnalyzer
{
    private const int ContextLength = 80;

    /// <summary>Guard + compile + syntax facts, the full `*.analyze` answer, with the Revit profiles.</summary>
    public static AnalyzeResult Run(ScriptCompiler compiler, string code) => Run(compiler, code, GuardProfile.Revit, AnalyzerProfile.Revit);

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

    /// <summary>Syntax facts only — no guard, no compiler — with the Revit profile.</summary>
    public static AnalyzeResult Analyze(string code) => Analyze(code, AnalyzerProfile.Revit);

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

    /// <summary>
    ///     The readability check must never cost the guard and compile verdicts: if it fails on an odd script the
    ///     result says "quality not analysed" (a warning) instead of the whole analysis failing like an offline bridge.
    /// </summary>
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
            // args.Double("spacing") — expression `args`, one string-literal argument
            if (node.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "args" } } access
                && node.ArgumentList.Arguments.Count > 0
                && node.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } key)
            {
                var line = tree.GetLineSpan(node.Span).StartLinePosition.Line + 1;
                ArgKeys.Add(new ArgUsage(key.Token.ValueText, access.Name.Identifier.ValueText, line));
            }

            // db.TransactionManager.StartTransaction() — hosts whose transactions are started by a call, not a constructor
            if (node.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: var invoked } && profile.TransactionMethodNames.Contains(invoked))
                UsesTransaction = true;

            base.VisitInvocationExpression(node);
        }

        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            NoteTransaction(node.Type);
            base.VisitObjectCreationExpression(node);
        }

        public override void VisitForStatement(ForStatementSyntax node) { HasLoops = true; base.VisitForStatement(node); }
        public override void VisitForEachStatement(ForEachStatementSyntax node) { HasLoops = true; base.VisitForEachStatement(node); }
        public override void VisitWhileStatement(WhileStatementSyntax node) { HasLoops = true; base.VisitWhileStatement(node); }
        public override void VisitDoStatement(DoStatementSyntax node) { HasLoops = true; base.VisitDoStatement(node); }

        private void NoteTransaction(TypeSyntax type)
        {
            var name = type switch
            {
                IdentifierNameSyntax id => id.Identifier.ValueText,
                QualifiedNameSyntax q => q.Right.Identifier.ValueText,
                _ => null,
            };
            if (name is not null && profile.TransactionTypeNames.Contains(name)) UsesTransaction = true;
        }

        /// <summary>The string inside `args.X("…")` is a key, not a candidate parameter.</summary>
        private static bool IsArgsKey(LiteralExpressionSyntax node) =>
            node.Parent is ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "args" } } } } };

        /// <summary>0, 1, 2 and friends are loop bookkeeping far more often than design values.</summary>
        private static bool IsTrivialNumber(LiteralExpressionSyntax node, string kind) =>
            kind == "number" && node.Token.ValueText is "0" or "1" or "2" or "0.5" or "1.0" or "-1";

        private static string? BoundName(LiteralExpressionSyntax node)
        {
            SyntaxNode? current = node.Parent;
            // unwrap unary minus / parentheses / casts
            while (current is PrefixUnaryExpressionSyntax or ParenthesizedExpressionSyntax or CastExpressionSyntax) current = current.Parent;

            return current switch
            {
                EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax declarator } => declarator.Identifier.ValueText,
                _ => null,
            };
        }

        /// <summary>The source line holding the literal — a top-level `double x = 150;` is a field in script mode, not a statement, so lines are the stable unit.</summary>
        private string ContextOf(SyntaxNode node)
        {
            var line = tree.GetLineSpan(node.Span).StartLinePosition.Line;
            var text = tree.GetText().Lines[line].ToString().Trim();
            return text.Length <= ContextLength ? text : text[..ContextLength] + "…";
        }
    }
}
