using HPRebar.Mcp.Contracts.Messages;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HPRebar.McpBridge.Core.Scripting;

/// <summary>
///     Finds the readability problems the registry refuses or warns about in a script an AI proposes as a
///     tool (and in every embedded seed): commented-out code, empty catches without a reason, oversize
///     scripts (errors); long blocks, deep nesting, vague names, Boolean parameters, swallowing catches
///     (warnings). Syntax only — no compilation — so every bridge can run it on its pipe thread.
/// </summary>
public static class ScriptQuality
{
    public const string Error = QualityFinding.Error;
    public const string Warning = QualityFinding.Warning;
    public const int MaxScriptLines = 300;
    public const int MaxBlockLines = 50;
    public const int MaxNesting = 3;

    private static readonly CSharpParseOptions ScriptOptions = new(kind: SourceCodeKind.Script);

    private static readonly HashSet<string> VagueNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "data", "tmp", "temp", "obj", "res", "val", "foo", "bar", "stuff", "thing",
    };

    /// <summary>Findings for one script, in source order.</summary>
    public static IReadOnlyList<QualityFinding> Find(string code) => Find(CSharpSyntaxTree.ParseText(code, ScriptOptions), code);

    /// <summary>Findings for a script already parsed in script mode (the analyzer reuses its tree).</summary>
    public static IReadOnlyList<QualityFinding> Find(SyntaxTree tree, string code)
    {
        var findings = new List<QualityFinding>();
        var root = tree.GetRoot();

        ScriptQualityComments.Find(tree, root, findings);

        var lines = LineCount(code);
        if (lines > MaxScriptLines)
            findings.Add(new QualityFinding("Q-B3", Error, 1, 1, $"script has {lines} lines; the limit is {MaxScriptLines} — move logic into smaller tools or the host's engine."));

        var walker = new StructureWalker(tree, findings);
        walker.Visit(root);

        return findings.OrderBy(f => f.Line).ThenBy(f => f.Column).ToList();
    }

    /// <summary>Lines of the trimmed text, so leading and trailing blank lines never count.</summary>
    public static int LineCount(string code)
    {
        var trimmed = code.Trim();
        return trimmed.Length == 0 ? 0 : trimmed.Split('\n').Length;
    }

    internal static QualityFinding At(SyntaxTree tree, SyntaxNodeOrToken where, string ruleId, string severity, string message)
    {
        var position = tree.GetLineSpan(where.Span).StartLinePosition;
        return new QualityFinding(ruleId, severity, position.Line + 1, position.Character + 1, message);
    }

    internal static int SpanLines(SyntaxTree tree, SyntaxNode node)
    {
        var span = tree.GetLineSpan(node.Span);
        return span.EndLinePosition.Line - span.StartLinePosition.Line + 1;
    }

    internal static bool IsVague(string name) => VagueNames.Contains(name);

    private sealed class StructureWalker(SyntaxTree tree, List<QualityFinding> findings) : CSharpSyntaxWalker
    {
        private int _depth;
        private bool _insideLongBlock;
        private bool _insideDeepNesting;

        public override void VisitBlock(BlockSyntax node)
        {
            var lines = SpanLines(tree, node);
            if (lines > MaxBlockLines && !_insideLongBlock)
            {
                findings.Add(At(tree, node, "Q-W1", Warning, $"block of {lines} lines (more than {MaxBlockLines}) — extract named steps."));
                _insideLongBlock = true;
                base.VisitBlock(node);
                _insideLongBlock = false;
                return;
            }

            base.VisitBlock(node);
        }

        public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
        {
            CheckFunction(node.Identifier, node.ParameterList);
            WithFreshNesting(() => base.VisitLocalFunctionStatement(node));
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            // In script mode a top-level function is parsed as a method, not as a local function.
            CheckFunction(node.Identifier, node.ParameterList);
            WithFreshNesting(() => base.VisitMethodDeclaration(node));
        }

        public override void VisitIfStatement(IfStatementSyntax node)
        {
            // `else if` continues a chain; it is not one level deeper.
            if (node.Parent is ElseClauseSyntax) base.VisitIfStatement(node);
            else Nest(node, () => base.VisitIfStatement(node));
        }

        public override void VisitForStatement(ForStatementSyntax node) => Nest(node, () => base.VisitForStatement(node));
        public override void VisitForEachStatement(ForEachStatementSyntax node) { NoteName(node.Identifier); Nest(node, () => base.VisitForEachStatement(node)); }
        public override void VisitWhileStatement(WhileStatementSyntax node) => Nest(node, () => base.VisitWhileStatement(node));
        public override void VisitDoStatement(DoStatementSyntax node) => Nest(node, () => base.VisitDoStatement(node));
        public override void VisitSwitchStatement(SwitchStatementSyntax node) => Nest(node, () => base.VisitSwitchStatement(node));
        public override void VisitLockStatement(LockStatementSyntax node) => Nest(node, () => base.VisitLockStatement(node));

        public override void VisitUsingStatement(UsingStatementSyntax node)
        {
            if (node.Statement is BlockSyntax) Nest(node, () => base.VisitUsingStatement(node));
            else base.VisitUsingStatement(node);
        }

        public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
            if (node.Block is not null) Nest(node, () => base.VisitParenthesizedLambdaExpression(node));
            else base.VisitParenthesizedLambdaExpression(node);
        }

        public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
        {
            if (node.Block is not null) Nest(node, () => base.VisitSimpleLambdaExpression(node));
            else base.VisitSimpleLambdaExpression(node);
        }

        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            NoteName(node.Identifier);
            base.VisitVariableDeclarator(node);
        }

        public override void VisitSingleVariableDesignation(SingleVariableDesignationSyntax node)
        {
            NoteName(node.Identifier);
            base.VisitSingleVariableDesignation(node);
        }

        public override void VisitParameter(ParameterSyntax node)
        {
            NoteName(node.Identifier);
            base.VisitParameter(node);
        }

        public override void VisitCatchClause(CatchClauseSyntax node)
        {
            if (node.Declaration is { } declaration && declaration.Identifier.ValueText.Length > 0) NoteName(declaration.Identifier);
            ScriptQualityCatches.Check(tree, node, findings);
            base.VisitCatchClause(node);
        }

        private void CheckFunction(SyntaxToken identifier, ParameterListSyntax parameters)
        {
            NoteName(identifier);
            foreach (var parameter in parameters.Parameters.Where(p => IsBoolean(p.Type)))
                findings.Add(At(tree, parameter, "Q-W4", Warning, $"bool parameter '{parameter.Identifier.ValueText}' on '{identifier.ValueText}' switches behaviour — use two named functions or an enum."));
        }

        private void NoteName(SyntaxToken identifier)
        {
            if (IsVague(identifier.ValueText))
                findings.Add(At(tree, identifier, "Q-W3", Warning, $"vague name '{identifier.ValueText}' — say what the value is in the domain."));
        }

        private void Nest(SyntaxNode node, Action visit)
        {
            _depth++;
            var reported = false;
            if (_depth > MaxNesting && !_insideDeepNesting)
            {
                findings.Add(At(tree, node, "Q-W2", Warning, $"nesting depth {_depth} (more than {MaxNesting}) — return early or extract a function."));
                _insideDeepNesting = reported = true;
            }

            visit();
            if (reported) _insideDeepNesting = false;
            _depth--;
        }

        private void WithFreshNesting(Action visit)
        {
            var (depth, deep) = (_depth, _insideDeepNesting);
            (_depth, _insideDeepNesting) = (0, false);
            visit();
            (_depth, _insideDeepNesting) = (depth, deep);
        }

        private static bool IsBoolean(TypeSyntax? type) => type switch
        {
            PredefinedTypeSyntax predefined => predefined.Keyword.IsKind(SyntaxKind.BoolKeyword),
            NullableTypeSyntax nullable => IsBoolean(nullable.ElementType),
            IdentifierNameSyntax { Identifier.ValueText: "Boolean" } => true,
            QualifiedNameSyntax { Right.Identifier.ValueText: "Boolean" } => true,
            _ => false,
        };
    }
}
