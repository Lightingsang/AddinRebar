using HPRebar.Mcp.Contracts.Messages;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HPRebar.McpBridge.Core.Scripting;

/// <summary>
///     Tells commented-out code from prose: a comment counts as code only when its text ends like a statement
///     or block (<c>;</c>, <c>{</c>, <c>}</c>), carries a code marker (call, assignment, brace, indexer) and
///     parses as C# script without a single error. Consecutive line comments are judged together first, so a
///     commented-out block is found as one finding; then each line alone, so code after a prose line is found.
/// </summary>
internal static class ScriptQualityComments
{
    private static readonly CSharpParseOptions ScriptOptions = new(kind: SourceCodeKind.Script);
    private static readonly char[] CodeMarkers = ['(', '=', '{', '}', '['];
    private static readonly string[] ProseTags = ["TODO", "NOTE", "HACK", "FIXME"];

    /// <summary>Words that introduce an example or a reference: what follows is shown, not disabled code.</summary>
    private static readonly string[] ProseLeads = ["e.g.", "i.e.", "example", "usage", "see ", "see:", "for example"];

    public static void Find(SyntaxTree tree, SyntaxNode root, List<QualityFinding> findings)
    {
        var lineComments = new List<(int Line, SyntaxTrivia Trivia, string Text)>();
        foreach (var trivia in root.DescendantTrivia(descendIntoTrivia: true))
        {
            if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                var line = tree.GetLineSpan(trivia.Span).StartLinePosition.Line;
                lineComments.Add((line, trivia, trivia.ToString().Substring(2)));
            }
            else if (trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) && LooksLikeCode(BlockCommentText(trivia.ToString())))
            {
                findings.Add(Finding(tree, trivia));
            }
        }

        foreach (var group in ConsecutiveRuns(lineComments))
        {
            if (LooksLikeCode(string.Join("\n", group.Select(c => c.Text))))
            {
                findings.Add(Finding(tree, group[0].Trivia));
                continue;
            }

            foreach (var comment in group.Where(c => LooksLikeCode(c.Text)))
                findings.Add(Finding(tree, comment.Trivia));
        }
    }

    /// <summary>True when the comment text is a complete C# statement or block rather than prose.</summary>
    public static bool LooksLikeCode(string text)
    {
        var candidate = text.Trim();
        if (candidate.Length < 3) return false;
        if (candidate.Contains("://")) return false;
        if (ProseTags.Concat(ProseLeads).Any(tag => candidate.StartsWith(tag, StringComparison.OrdinalIgnoreCase))) return false;
        if (candidate.IndexOfAny(CodeMarkers) < 0) return false;
        if (candidate[candidate.Length - 1] is not (';' or '{' or '}')) return false;

        var parsed = CSharpSyntaxTree.ParseText(candidate, ScriptOptions);
        if (parsed.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error)) return false;

        // "Label: statement" is how prose such as "Default: Compute(x);" parses; commented-out code is almost never labelled.
        return !parsed.GetRoot().DescendantNodes().OfType<LabeledStatementSyntax>().Any();
    }

    private static QualityFinding Finding(SyntaxTree tree, SyntaxTrivia trivia)
    {
        var position = tree.GetLineSpan(trivia.Span).StartLinePosition;
        return new QualityFinding("Q-B1", ScriptQuality.Error, position.Line + 1, position.Character + 1,
            "commented-out code — delete it (git keeps history) or turn it into a comment that explains why.");
    }

    private static string BlockCommentText(string comment)
    {
        var body = comment.Substring(2, Math.Max(0, comment.Length - 4));
        return string.Join("\n", body.Split('\n').Select(line => line.TrimStart().TrimStart('*')));
    }

    private static IEnumerable<List<(int Line, SyntaxTrivia Trivia, string Text)>> ConsecutiveRuns(List<(int Line, SyntaxTrivia Trivia, string Text)> comments)
    {
        var run = new List<(int Line, SyntaxTrivia Trivia, string Text)>();
        foreach (var comment in comments)
        {
            if (run.Count > 0 && comment.Line != run[run.Count - 1].Line + 1)
            {
                yield return run;
                run = [];
            }

            run.Add(comment);
        }

        if (run.Count > 0) yield return run;
    }
}
